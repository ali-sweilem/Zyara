using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Patients.Dtos;

namespace Zyara.Services.Features.Patients;

public sealed class CareRecipientService : ICareRecipientService
{
    private const int MaxCareRecipients = 3;

    private readonly IRepository<CareRecipient> _careRecipients;
    private readonly IRepository<Patient> _patients;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CareRecipientService(
        IRepository<CareRecipient> careRecipients,
        IRepository<Patient> patients,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _careRecipients = careRecipients;
        _patients = patients;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<CareRecipientDto>> ListAsync(int patientId, CancellationToken cancellationToken = default)
    {
        return await _careRecipients.Query()
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
    }

    public async Task<CareRecipientDto?> GetAsync(int patientId, int id, CancellationToken cancellationToken = default)
    {
        return await _careRecipients.Query()
            .AsNoTracking()
            .Where(c => c.Id == id && c.PatientId == patientId && !c.IsArchived)
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
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<int>> AddAsync(int patientId, AddCareRecipientDto dto, CancellationToken cancellationToken = default)
    {
        var activeRecipients = await _careRecipients.Query()
            .Where(c => c.PatientId == patientId && !c.IsArchived)
            .Select(c => new { c.Slot, c.Relationship })
            .ToListAsync(cancellationToken);

        if (activeRecipients.Count >= MaxCareRecipients)
        {
            return ServiceResult<int>.Fail("You can add up to 3 patients. Remove one first.");
        }

        var usedSlots = activeRecipients.Select(r => r.Slot).ToHashSet();
        var freeSlot = new[] { 1, 2, 3 }.FirstOrDefault(s => !usedSlots.Contains(s));
        if (freeSlot == 0)
        {
            return ServiceResult<int>.Fail("You can add up to 3 patients. Remove one first.");
        }

        var patient = await _patients.FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        if (patient is null) return ServiceResult<int>.Fail("Patient profile not found.");

        string fullName;
        string phoneNumber;
        Gender gender;
        DateOnly? dob;
        Relationship relationship;

        var isSelf = dto.IsAccountOwner || dto.Relationship == Relationship.Self;

        if (isSelf)
        {
            if (activeRecipients.Any(r => r.Relationship == Relationship.Self))
            {
                return ServiceResult<int>.Fail("A patient profile for the account owner already exists.");
            }

            dob = patient.DateOfBirth ?? dto.DateOfBirth;
            if (dob is null)
            {
                return ServiceResult<int>.Fail("Date of birth is required.");
            }

            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
            if (dob > today) return ServiceResult<int>.Fail("Date of birth cannot be in the future.");
            if (dob < today.AddYears(-120)) return ServiceResult<int>.Fail("Enter a valid date of birth.");

            // Save DateOfBirth on the account holder profile if it was missing
            if (patient.DateOfBirth is null)
            {
                patient.DateOfBirth = dob;
            }

            fullName = patient.FullName;
            phoneNumber = patient.PhoneNumber;
            gender = patient.Gender;
            relationship = Relationship.Self;
        }
        else
        {
            if (dto.Relationship is null || dto.Relationship == Relationship.Self || !Enum.IsDefined(dto.Relationship.Value))
            {
                return ServiceResult<int>.Fail("Please select a relationship (Spouse, Parent, Child, Sibling, or Other).");
            }

            var name = dto.FullName?.Trim() ?? string.Empty;
            if (name.Length == 0) return ServiceResult<int>.Fail("Full name is required.");

            var phone = dto.PhoneNumber?.Trim() ?? string.Empty;
            if (phone.Length == 0) return ServiceResult<int>.Fail("Phone number is required.");

            if (dto.Gender is null || !Enum.IsDefined(dto.Gender.Value))
            {
                return ServiceResult<int>.Fail("Please choose a gender.");
            }

            if (dto.DateOfBirth is null)
            {
                return ServiceResult<int>.Fail("Date of birth is required.");
            }

            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
            if (dto.DateOfBirth > today) return ServiceResult<int>.Fail("Date of birth cannot be in the future.");
            if (dto.DateOfBirth < today.AddYears(-120)) return ServiceResult<int>.Fail("Enter a valid date of birth.");

            fullName = name;
            phoneNumber = phone;
            gender = dto.Gender.Value;
            dob = dto.DateOfBirth;
            relationship = dto.Relationship.Value;
        }

        // Complete data rule: BloodType, ChronicConditions, Allergies, CurrentMedications
        if (dto.BloodType is null || !Enum.IsDefined(dto.BloodType.Value))
        {
            return ServiceResult<int>.Fail("Please choose a blood type.");
        }

        var chronicConditions = dto.ChronicConditions?.Trim() ?? string.Empty;
        if (chronicConditions.Length == 0) return ServiceResult<int>.Fail("Chronic conditions are required. Enter 'None' if not applicable.");

        var allergies = dto.Allergies?.Trim() ?? string.Empty;
        if (allergies.Length == 0) return ServiceResult<int>.Fail("Allergies are required. Enter 'None' if not applicable.");

        var currentMedications = dto.CurrentMedications?.Trim() ?? string.Empty;
        if (currentMedications.Length == 0) return ServiceResult<int>.Fail("Current medications are required. Enter 'None' if not applicable.");

        var careRecipient = new CareRecipient
        {
            PatientId = patientId,
            Slot = freeSlot,
            IsArchived = false,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            DateOfBirth = dob,
            Gender = gender,
            Relationship = relationship,
            BloodType = dto.BloodType.Value,
            ChronicConditions = chronicConditions,
            Allergies = allergies,
            CurrentMedications = currentMedications,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        _careRecipients.Add(careRecipient);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult<int>.Ok(careRecipient.Id);
        }
        catch (DbUpdateException)
        {
            return ServiceResult<int>.Fail("You can add up to 3 patients. Remove one first.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(int patientId, int id, EditCareRecipientDto dto, CancellationToken cancellationToken = default)
    {
        var careRecipient = await _careRecipients.FirstOrDefaultAsync(
            c => c.Id == id && c.PatientId == patientId && !c.IsArchived,
            cancellationToken);

        if (careRecipient is null) return ServiceResult.Fail("Patient not found.");

        if (dto.BloodType is null || !Enum.IsDefined(dto.BloodType.Value))
        {
            return ServiceResult.Fail("Please choose a blood type.");
        }

        var chronicConditions = dto.ChronicConditions?.Trim() ?? string.Empty;
        if (chronicConditions.Length == 0) return ServiceResult.Fail("Chronic conditions are required. Enter 'None' if not applicable.");

        var allergies = dto.Allergies?.Trim() ?? string.Empty;
        if (allergies.Length == 0) return ServiceResult.Fail("Allergies are required. Enter 'None' if not applicable.");

        var currentMedications = dto.CurrentMedications?.Trim() ?? string.Empty;
        if (currentMedications.Length == 0) return ServiceResult.Fail("Current medications are required. Enter 'None' if not applicable.");

        if (careRecipient.Relationship == Relationship.Self)
        {
            careRecipient.BloodType = dto.BloodType.Value;
            careRecipient.ChronicConditions = chronicConditions;
            careRecipient.Allergies = allergies;
            careRecipient.CurrentMedications = currentMedications;
            careRecipient.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

            if (careRecipient.DateOfBirth is null && dto.DateOfBirth.HasValue)
            {
                var patient = await _patients.FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
                if (patient is not null && patient.DateOfBirth is null)
                {
                    patient.DateOfBirth = dto.DateOfBirth;
                }
                careRecipient.DateOfBirth = dto.DateOfBirth;
            }
        }
        else
        {
            if (dto.Relationship == Relationship.Self)
            {
                return ServiceResult.Fail("Cannot change relationship to Self.");
            }

            if (dto.Relationship.HasValue && Enum.IsDefined(dto.Relationship.Value))
            {
                careRecipient.Relationship = dto.Relationship.Value;
            }

            var name = dto.FullName?.Trim() ?? string.Empty;
            if (name.Length == 0) return ServiceResult.Fail("Full name is required.");

            var phone = dto.PhoneNumber?.Trim() ?? string.Empty;
            if (phone.Length == 0) return ServiceResult.Fail("Phone number is required.");

            if (dto.Gender is null || !Enum.IsDefined(dto.Gender.Value))
            {
                return ServiceResult.Fail("Please choose a gender.");
            }

            if (dto.DateOfBirth is null)
            {
                return ServiceResult.Fail("Date of birth is required.");
            }

            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
            if (dto.DateOfBirth > today) return ServiceResult.Fail("Date of birth cannot be in the future.");
            if (dto.DateOfBirth < today.AddYears(-120)) return ServiceResult.Fail("Enter a valid date of birth.");

            careRecipient.FullName = name;
            careRecipient.PhoneNumber = phone;
            careRecipient.Gender = dto.Gender.Value;
            careRecipient.DateOfBirth = dto.DateOfBirth;
            careRecipient.BloodType = dto.BloodType.Value;
            careRecipient.ChronicConditions = chronicConditions;
            careRecipient.Allergies = allergies;
            careRecipient.CurrentMedications = currentMedications;
            careRecipient.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int patientId, int id, CancellationToken cancellationToken = default)
    {
        var careRecipient = await _careRecipients.FirstOrDefaultAsync(
            c => c.Id == id && c.PatientId == patientId && !c.IsArchived,
            cancellationToken);

        if (careRecipient is null) return ServiceResult.Fail("Patient not found.");

        careRecipient.IsArchived = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }
}

