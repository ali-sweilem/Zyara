using System;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Data;

namespace Zyara.Services.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ZyaraDbContext _db;

        public AuthService(ZyaraDbContext db)
        {
            _db = db;
        }

        public async Task<ServiceResult<User>> LoginAsync(string email, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
                return ServiceResult<User>.Fail("البريد الإلكتروني أو كلمة المرور غير صحيحة.");

            if (!user.IsActive)
                return ServiceResult<User>.Fail("هذا الحساب موقوف. يرجى التواصل مع الدعم.");

            bool valid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!valid)
                return ServiceResult<User>.Fail("البريد الإلكتروني أو كلمة المرور غير صحيحة.");

            // Providers must be verified before login
            if (user is Provider provider && provider.VerificationStatus != VerificationStatus.Verified)
            {
                if (provider.VerificationStatus == VerificationStatus.Pending)
                    return ServiceResult<User>.Fail("حسابك قيد المراجعة من قِبل الإدارة. سيتم إخطارك عند الاعتماد.");
                if (provider.VerificationStatus == VerificationStatus.Rejected)
                    return ServiceResult<User>.Fail($"تم رفض طلب التسجيل. السبب: {provider.RejectionReason ?? "غير محدد"}");
            }

            return ServiceResult<User>.Ok(user);
        }

        public async Task<ServiceResult<Patient>> RegisterPatientAsync(
            string fullName, string email, string phone, string password, string? dateOfBirth)
        {
            if (await EmailExistsAsync(email))
                return ServiceResult<Patient>.Fail("البريد الإلكتروني مستخدم بالفعل.");

            var patient = new Patient
            {
                FullName = fullName,
                Email = email.ToLowerInvariant(),
                Phone = phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Patient,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                DateOfBirth = dateOfBirth is not null && DateOnly.TryParse(dateOfBirth, out var dob) ? dob : null
            };

            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();
            return ServiceResult<Patient>.Ok(patient);
        }

        public async Task<ServiceResult<Doctor>> RegisterDoctorAsync(RegisterProviderRequest req)
        {
            if (await EmailExistsAsync(req.Email))
                return ServiceResult<Doctor>.Fail("البريد الإلكتروني مستخدم بالفعل.");

            var doctor = new Doctor
            {
                FullName = req.FullName,
                Email = req.Email.ToLowerInvariant(),
                Phone = req.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                Role = UserRole.Doctor,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Specialty = req.Specialty,
                ExperienceYears = req.ExperienceYears,
                licenser = req.LicenseNumber,
                BaseCity = req.BaseCity,
                VerificationStatus = VerificationStatus.Pending
            };

            _db.Doctors.Add(doctor);
            await _db.SaveChangesAsync();
            return ServiceResult<Doctor>.Ok(doctor);
        }

        public async Task<ServiceResult<Nurse>> RegisterNurseAsync(RegisterProviderRequest req)
        {
            if (await EmailExistsAsync(req.Email))
                return ServiceResult<Nurse>.Fail("البريد الإلكتروني مستخدم بالفعل.");

            var nurse = new Nurse
            {
                FullName = req.FullName,
                Email = req.Email.ToLowerInvariant(),
                Phone = req.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                Role = UserRole.Nurse,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Specialty = req.Specialty,
                ExperienceYears = req.ExperienceYears,
                BaseCity = req.BaseCity,
                VerificationStatus = VerificationStatus.Pending
            };

            _db.Nurses.Add(nurse);
            await _db.SaveChangesAsync();
            return ServiceResult<Nurse>.Ok(nurse);
        }

        public async Task<bool> EmailExistsAsync(string email)
            => await _db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());
    }
}

