using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Services.Common;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Services.Features.Bookings;

public class BookingTransitions : IBookingTransitions
{
    private readonly ZyaraDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly ILogger<BookingTransitions> _logger;

    public BookingTransitions(
        ZyaraDbContext db,
        IPaymentGateway gateway,
        ILogger<BookingTransitions> logger)
    {
        _db = db;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<ServiceResult<bool>> AcceptAsync(
        Booking booking,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            booking.Accept(utcNow);
            if (booking.TimeSlot != null)
            {
                booking.TimeSlot.Status = TimeSlotStatus.Booked;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult<bool>.Ok(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<bool>.Fail("This request was just updated, please refresh.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> RejectAsync(
        Booking booking,
        string reason,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            booking.Reject(reason, utcNow);
            if (booking.TimeSlot != null)
            {
                booking.TimeSlot.Status = TimeSlotStatus.Available;
            }

            await ReleaseHoldIfAuthorizedAsync(booking);
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult<bool>.Ok(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<bool>.Fail("This request was just updated, please refresh.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> ExpireAsync(
        Booking booking,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            booking.Expire(utcNow);
            if (booking.TimeSlot != null)
            {
                booking.TimeSlot.Status = TimeSlotStatus.Available;
            }

            await ReleaseHoldIfAuthorizedAsync(booking);
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult<bool>.Ok(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<bool>.Fail("This request was just updated, please refresh.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> CancelByProviderAsync(
        Booking booking,
        string reason,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            booking.CancelByProvider(reason, utcNow);
            if (booking.TimeSlot != null)
            {
                booking.TimeSlot.Status = TimeSlotStatus.Available;
            }

            await ReleaseHoldIfAuthorizedAsync(booking);
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult<bool>.Ok(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<bool>.Fail("This request was just updated, please refresh.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> StartAsync(
        Booking booking,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            booking.Start(utcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult<bool>.Ok(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<bool>.Fail("This request was just updated, please refresh.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> CompleteAsync(
        Booking booking,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (booking.Payment != null)
            {
                if (booking.Payment.Method == PaymentMethod.OnlinePayment)
                {
                    if (string.IsNullOrWhiteSpace(booking.Payment.GatewayReference))
                    {
                        return ServiceResult<bool>.Fail("Cannot capture payment: missing gateway reference.");
                    }

                    var captureResult = await _gateway.CaptureAsync(booking.Payment.GatewayReference, booking.Payment.Amount);
                    if (!captureResult.Succeeded)
                    {
                        return ServiceResult<bool>.Fail(captureResult.FailureReason ?? "Failed to capture card payment. Please try again.");
                    }

                    booking.Payment.Capture();
                }
                else if (booking.Payment.Method == PaymentMethod.CashOnArrival)
                {
                    booking.Payment.Collect();
                }
            }

            booking.Complete(utcNow);
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult<bool>.Ok(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<bool>.Fail("This request was just updated, please refresh.");
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Fail(ex.Message);
        }
    }

    private async Task ReleaseHoldIfAuthorizedAsync(Booking booking)
    {
        if (booking.Payment != null &&
            booking.Payment.Method == PaymentMethod.OnlinePayment &&
            booking.Payment.Status == PaymentStatus.Authorized &&
            !string.IsNullOrWhiteSpace(booking.Payment.GatewayReference))
        {
            try
            {
                var releaseResult = await _gateway.ReleaseAsync(booking.Payment.GatewayReference);
                if (releaseResult.Succeeded)
                {
                    booking.Payment.Release();
                }
                else
                {
                    _logger.LogError("Failed to release card hold for booking {BookingId}, ref {Reference}: {Reason}. TODO: Reconciliation needed.",
                        booking.Id, booking.Payment.GatewayReference, releaseResult.FailureReason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception releasing card hold for booking {BookingId}, ref {Reference}. TODO: Reconciliation needed.",
                    booking.Id, booking.Payment.GatewayReference);
            }
        }
    }
}

