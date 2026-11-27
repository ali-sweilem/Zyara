using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Patients.Dtos;

namespace Zyara.Services.Features.Patients;

public sealed class PatientProfileService : IPatientProfileService
{
    private const int MaxAddresses = 5;

    private readonly IRepository<Patient> _patients;
    private readonly IRepository<Address> _addresses;
    private readonly IRepository<Booking> _bookings;
    private readonly IRepository<Review> _reviews;
    private readonly IRepository<CareRecipient> _careRecipients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<User> _users;
    private readonly ISessionService _sessions;
    private readonly TimeProvider _timeProvider;

    public PatientProfileService(
        IRepository<Patient> patients,
        IRepository<Address> addresses,
        IRepository<Booking> bookings,
        IRepository<Review> reviews,
        IRepository<CareRecipient> careRecipients,
        IUnitOfWork unitOfWork,
        UserManager<User> users,
        ISessionService sessions,
        TimeProvider timeProvider)
    {
        _patients = patients;
        _addresses = addresses;
        _bookings = bookings;
        _reviews = reviews;
        _careRecipients = careRecipients;
        _unitOfWork = unitOfWork;
        _users = users;
        _sessions = sessions;
        _timeProvider = timeProvider;
    }

    public async Task<PatientProfileDto?> GetAsync(int patientId, CancellationToken cancellationToken = default)
    {
        var p = await _patients.Query()
            .AsNoTracking()
            .Where(x => x.Id == patientId)
            .Select(x => new
            {
                x.FullName,
                x.PhoneNumber,
                x.DateOfBirth,
                x.Gender,
                x.Email,
                x.EmailConfirmed,
                x.PhotoUrl,
                x.CreatedAt,
                x.TwoFactorEnabled,
                HasPassword = x.PasswordHash != null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (p is null) return null;

        var totalBookings = await _bookings.Query().CountAsync(b => b.PatientId == patientId, cancellationToken);
        var completed = await _bookings.Query()
            .CountAsync(b => b.PatientId == patientId && b.Status == BookingStatus.Completed, cancellationToken);
        var reviewsWritten = await _reviews.Query().CountAsync(r => r.Booking.PatientId == patientId, cancellationToken);

        var addresses = await _addresses.Query()
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderBy(a => a.Id)
            .Select(a => new PatientAddressItemDto
            {
                Id = a.Id,
                Label = a.Label,
                City = a.City,
                District = a.District,
                StreetAndBuilding = a.StreetAndBuilding
            })
            .ToListAsync(cancellationToken);

        var careRecipients = await _careRecipients.Query()
            .AsNoTracking()
            .Where(c => c.PatientId == patientId && !c.IsArchived)
            .OrderBy(c => c.Slot)
            .Select(c => new CareRecipientDto
            {
                Id = c.Id,
                PatientId = c.PatientId,
                Slot = c.Slot,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                DateOfBirth = c.DateOfBirth,
                Gender = c.Gender,
                Relationship = c.Relationship,
                BloodType = c.BloodType,
                ChronicConditions = c.ChronicConditions,
                Allergies = c.Allergies,
                CurrentMedications = c.CurrentMedications,
                Notes = c.Notes,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var hasSelf = careRecipients.Any(c => c.Relationship == Relationship.Self);

        return new PatientProfileDto
        {
            FullName = p.FullName,
            PhoneNumber = p.PhoneNumber,
            DateOfBirth = p.DateOfBirth,
            Gender = p.Gender,
            Email = p.Email,
            EmailConfirmed = p.EmailConfirmed,
            PhotoUrl = p.PhotoUrl,
            MemberSince = p.CreatedAt,
            TwoFactorEnabled = p.TwoFactorEnabled,
            HasPassword = p.HasPassword,
            TotalBookings = totalBookings,
            CompletedVisits = completed,
            ReviewsWritten = reviewsWritten,
            Addresses = addresses,
            MaxAddresses = MaxAddresses,
            CareRecipients = careRecipients,
            MaxCareRecipients = 3,
            HasSelfCareRecipient = hasSelf
        };
    }

    public async Task<ServiceResult> UpdateAsync(int patientId, EditPatientProfileDto dto, CancellationToken cancellationToken = default)
    {
        var fullName = dto.FullName?.Trim() ?? string.Empty;
        var phone = dto.PhoneNumber?.Trim() ?? string.Empty;

        if (fullName.Length == 0) return ServiceResult.Fail("Full name is required.");
        if (phone.Length == 0) return ServiceResult.Fail("Phone number is required.");
        if (dto.Gender is null || !Enum.IsDefined(dto.Gender.Value)) return ServiceResult.Fail("Please choose your gender.");

        if (dto.DateOfBirth is { } dob)
        {
            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
            if (dob > today) return ServiceResult.Fail("Date of birth cannot be in the future.");
            if (dob < today.AddYears(-120)) return ServiceResult.Fail("Enter a valid date of birth.");
        }

        var patient = await _patients.FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        if (patient is null) return ServiceResult.Fail("Profile not found.");

        patient.FullName = fullName;
        patient.PhoneNumber = phone;
        patient.DateOfBirth = dto.DateOfBirth;
        patient.Gender = dto.Gender.Value;

        // Keep the Self CareRecipient in sync with the account holder profile.
        var selfRecipient = await _careRecipients.Query()
            .FirstOrDefaultAsync(c => c.PatientId == patientId
                                   && c.Relationship == Relationship.Self
                                   && !c.IsArchived,
                                   cancellationToken);
        if (selfRecipient is not null)
        {
            selfRecipient.FullName = fullName;
            selfRecipient.PhoneNumber = phone;
            selfRecipient.DateOfBirth = dto.DateOfBirth;
            selfRecipient.Gender = dto.Gender.Value;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AddAddressAsync(int patientId, AddAddressDto dto, CancellationToken cancellationToken = default)
    {
        var count = await _addresses.Query().CountAsync(a => a.PatientId == patientId, cancellationToken);
        if (count >= MaxAddresses)
        {
            return ServiceResult.Fail($"You can save up to {MaxAddresses} addresses. Delete one first.");
        }

        _addresses.Add(new Address
        {
            PatientId = patientId,
            Label = dto.Label.Trim(),
            City = dto.City.Trim(),
            District = dto.District.Trim(),
            StreetAndBuilding = dto.StreetAndBuilding.Trim()
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAddressAsync(int patientId, int addressId, CancellationToken cancellationToken = default)
    {
        // PatientId in the filter: a patient can only delete his own addresses.
        var address = await _addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.PatientId == patientId, cancellationToken);
        if (address is null) return ServiceResult.Fail("Address not found.");

        _addresses.Remove(address);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ChangePasswordAsync(int patientId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(patientId.ToString());
        if (user is null) return ServiceResult.Fail("Profile not found.");

        if (!await _users.HasPasswordAsync(user))
        {
            return ServiceResult.Fail("This account signs in with Google and has no password.");
        }

        var result = await _users.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            // Identity's messages are safe to show: "Incorrect password.", "Passwords must have at least one digit", ...
            return ServiceResult.Fail(result.Errors.FirstOrDefault()?.Description ?? "Could not change the password.");
        }

        // ChangePassword changed the SecurityStamp (other devices are signed out within ~1 minute);
        // re-issue the cookie of THIS device so the user stays signed in.
        await _sessions.RefreshAsync(user);
        return ServiceResult.Ok();
    }
}
