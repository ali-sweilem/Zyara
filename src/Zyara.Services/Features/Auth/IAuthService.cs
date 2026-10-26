using System.Threading.Tasks;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Common;

namespace Zyara.Services.Features.Auth
{
    public interface IAuthService
    {
        Task<ServiceResult<User>> LoginAsync(string email, string password);
        Task<ServiceResult<Patient>> RegisterPatientAsync(string fullName, string email, string phone, string password, string? dateOfBirth);
        Task<ServiceResult<Doctor>> RegisterDoctorAsync(RegisterProviderRequest req);
        Task<ServiceResult<Nurse>> RegisterNurseAsync(RegisterProviderRequest req);
        Task<bool> EmailExistsAsync(string email);
    }

    public class RegisterProviderRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;
        public string BaseCity { get; set; } = string.Empty;
    }
}

