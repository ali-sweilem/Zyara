using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Repositories.Data;
using Zyara.Services.Common;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Providers;

namespace Zyara.Services.Features.Bookings;

public sealed class BookingService : IBookingService
{
    private readonly ZyaraDbContext _db;
    private readonly TimeProvider _timeProvider;

    public BookingService(ZyaraDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public Task<Booking?> GetBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return _db.Bookings
            .AsNoTracking()
            .Include(b => b.TimeSlot)
            .Include(b => b.Service)
            .Include(b => b.Payment)
            .Include(b => b.Provider)
            .Include(b => b.Patient)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
    }

    public async Task<IReadOnlyList<BookingSlotDto>> GetAvailableSlotsAsync(
        int providerId,
        CancellationToken cancellationToken = default)
    {
        var egyptNow = EgyptTime.Now(_timeProvider);

        return await _db.TimeSlots
            .AsNoTracking()
            .Where(ts => ts.ProviderId == providerId)
            .BookableSlots(egyptNow)
            .OrderBy(ts => ts.Date)
            .ThenBy(ts => ts.StartTime)
            .Select(ts => new BookingSlotDto
            {
                Id = ts.Id,
                Date = ts.Date,
                StartTime = ts.StartTime,
                EndTime = ts.EndTime
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AddressDto>> GetPatientAddressesAsync(
        int patientId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Addresses
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderBy(a => a.Id)
            .Select(a => new AddressDto
            {
                Id = a.Id,
                Label = a.Label,
                City = a.City,
                District = a.District,
                StreetAndBuilding = a.StreetAndBuilding,
                Latitude = a.Latitude,
                Longitude = a.Longitude
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<CreateBookingResult>> CreateBookingAsync(
        CreateBookingCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate provider exists and is publicly visible
        var provider = await _db.Providers
            .AsNoTracking()
            .PubliclyVisible()
            .FirstOrDefaultAsync(p => p.Id == command.ProviderId, cancellationToken);

        if (provider is null)
        {
            return ServiceResult<CreateBookingResult>.Fail("Provider not found or is not currently accepting bookings.");
        }

        // 2. Validate patient exists
        var patientExists = await _db.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == command.PatientId, cancellationToken);

        if (!patientExists)
        {
            return ServiceResult<CreateBookingResult>.Fail("Patient account not found. Please log in and try again.");
        }

        // 3. Validate slot belongs to provider and passes bookable rule
        var egyptNow = EgyptTime.Now(_timeProvider);
        var slot = await _db.TimeSlots
            .BookableSlots(egyptNow)
            .FirstOrDefaultAsync(ts => ts.Id == command.TimeSlotId && ts.ProviderId == command.ProviderId, cancellationToken);

        if (slot is null)
        {
            return ServiceResult<CreateBookingResult>.Fail("The selected time slot is no longer available or does not meet booking lead time requirements. Please choose another window.");
        }

        // 4. Validate ProviderService belongs to this provider and is active
        var providerService = await _db.ProviderServices
            .AsNoTracking()
            .Include(ps => ps.Service)
            .FirstOrDefaultAsync(ps =>
                ps.Id == command.ProviderServiceId &&
                ps.ProviderId == command.ProviderId &&
                ps.Service != null &&
                ps.Service.IsActive,
                cancellationToken);

        if (providerService is null || providerService.Service is null)
        {
            return ServiceResult<CreateBookingResult>.Fail("The selected service is not offered by this provider or is inactive.");
        }

        var service = providerService.Service;
        var isHomeVisit = service.Type != ServiceType.RemoteConsultation;

        string? visitAddress = null;
        double? visitLat = null;
        double? visitLng = null;
        string otpCode = string.Empty;

        // 5. Handle address and OTP for home visit services
        if (isHomeVisit)
        {
            if (command.AddressId.HasValue && command.AddressId.Value > 0)
            {
                var existingAddress = await _db.Addresses
                    .FirstOrDefaultAsync(a => a.Id == command.AddressId.Value && a.PatientId == command.PatientId, cancellationToken);

                if (existingAddress is null)
                {
                    return ServiceResult<CreateBookingResult>.Fail("The selected saved address was not found or does not belong to you.");
                }

                visitAddress = $"{existingAddress.StreetAndBuilding}, {existingAddress.District}, {existingAddress.City}";
                visitLat = existingAddress.Latitude;
                visitLng = existingAddress.Longitude;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(command.StreetAndBuilding) ||
                    string.IsNullOrWhiteSpace(command.District) ||
                    string.IsNullOrWhiteSpace(command.City))
                {
                    return ServiceResult<CreateBookingResult>.Fail("Please provide a complete visit address (Street & building, District, and City) for this home-visit service.");
                }

                var street = command.StreetAndBuilding.Trim();
                var district = command.District.Trim();
                var city = command.City.Trim();

                visitAddress = $"{street}, {district}, {city}";
                visitLat = command.Latitude;
                visitLng = command.Longitude;

                if (command.SaveNewAddress)
                {
                    var newAddress = new Address
                    {
                        PatientId = command.PatientId,
                        Label = string.IsNullOrWhiteSpace(command.Label) ? "Home" : command.Label.Trim(),
                        City = city,
                        District = district,
                        StreetAndBuilding = street,
                        Latitude = command.Latitude,
                        Longitude = command.Longitude
                    };
                    _db.Addresses.Add(newAddress);
                }
            }

            // Secure 4-digit OTP for home visit
            otpCode = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
        }

        // 6. Pricing and commissions
        var settings = await _db.PlatformSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            ?? new PlatformSettings();

        var commission = CommissionCalculator.Calculate(
            providerService.Fee,
            settings.CommissionPercentage / 100m);

        var totalPrice = providerService.Fee + settings.ServiceFee;

        // 7. Initial Status based on payment method
        var bookingStatus = command.PaymentMethod == PaymentMethod.CashOnArrival
            ? BookingStatus.PendingProviderApproval
            : BookingStatus.PendingPayment;

        var payment = new Payment
        {
            Method = command.PaymentMethod,
            Status = PaymentStatus.Pending,
            Amount = totalPrice,
            RefundedAmount = 0m
        };

        var booking = new Booking
        {
            PatientId = command.PatientId,
            ProviderId = command.ProviderId,
            ServiceId = service.Id,
            TimeSlotId = slot.Id,
            Status = bookingStatus,
            ServiceFee = settings.ServiceFee,
            TotalPrice = totalPrice,
            Commission = commission,
            OtpCode = otpCode,
            Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
            VisitAddress = visitAddress,
            VisitLatitude = visitLat,
            VisitLongitude = visitLng,
            CreatedAt = DateTime.UtcNow,
            Payment = payment
        };

        // Lock slot
        slot.Status = TimeSlotStatus.Locked;

        _db.Bookings.Add(booking);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return ServiceResult<CreateBookingResult>.Fail(
                "This slot was just taken by another patient. Please refresh and choose a different slot.");
        }

        return ServiceResult<CreateBookingResult>.Ok(new CreateBookingResult
        {
            BookingId = booking.Id,
            NeedsPayment = command.PaymentMethod == PaymentMethod.OnlinePayment
        });
    }
}
