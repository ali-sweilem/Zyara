using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Repositories.Data;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;
using Zyara.Services.Features.Notifications.Interfaces;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services.Features.Administration
{
    public class AdminProviderReviewService : IAdminProviderReviewService
    {
        private readonly ZyaraDbContext _db;
        private readonly IProviderCredentialService _credentials;
        private readonly INotificationService _notifications;
        private readonly IUnitOfWork _unitOfWork;

        public AdminProviderReviewService(
            ZyaraDbContext db,
            IProviderCredentialService credentials,
            INotificationService notifications,
            IUnitOfWork unitOfWork)
        {
            _db = db;
            _credentials = credentials;
            _notifications = notifications;
            _unitOfWork = unitOfWork;
        }

        public async Task<IReadOnlyList<PendingProviderDto>> GetProvidersAsync(
            VerificationStatus? status = VerificationStatus.Pending,
            CancellationToken cancellationToken = default)
        {
            var query = _db.Providers.AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(p => p.VerificationStatus == status.Value);
            }

            var list = await query
                .OrderBy(p => p.VerificationStatus)
                .ThenBy(p => p.CreatedAt)
                .Select(p => new PendingProviderDto
                {
                    Id = p.Id,
                    FullName = p.FullName,
                    Email = p.Email ?? string.Empty,
                    Role = p.Role,
                    Specialty = p.Specialty,
                    ExperienceYears = p.ExperienceYears,
                    BaseCity = p.BaseCity,
                    VerificationStatus = p.VerificationStatus,
                    RejectionReason = p.RejectionReason
                })
                .ToListAsync(cancellationToken);

            // The documents of each provider (the list is small, so one call each is fine)
            foreach (var p in list)
                p.Credentials = await _credentials.GetMyCredentialsAsync(p.Id);

            return list;
        }

        public async Task<ServiceResult> ReviewProviderAsync(
            int providerId,
            bool approve,
            string? reason,
            int adminId,
            CancellationToken cancellationToken = default)
        {
            var provider = await _db.Providers.FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);
            if (provider is null)
                return ServiceResult.Fail("Provider not found.");

            if (approve && provider.VerificationStatus == VerificationStatus.Verified)
                return ServiceResult.Fail("This provider is already verified.");

            if (!approve && string.IsNullOrWhiteSpace(reason))
                return ServiceResult.Fail("A reason is required to reject or revoke a provider.");

            provider.VerificationStatus = approve ? VerificationStatus.Verified : VerificationStatus.Rejected;
            provider.RejectionReason = approve ? null : reason!.Trim();
            provider.VerifiedAt = DateTime.UtcNow;
            provider.VerifiedByAdminId = adminId;

            _notifications.Notify(
                provider.Id,
                NotificationType.General,
                approve ? "Account Verified" : "Verification Update",
                approve
                    ? "Your provider account has been verified. You can now receive patient bookings."
                    : $"Your provider verification was rejected: {provider.RejectionReason}",
                "/ProviderVerification/Status");

            await _unitOfWork.ExecuteInTransactionAsync(
                ct => _unitOfWork.SaveChangesAsync(ct), cancellationToken);
            return ServiceResult.Ok();
        }
    }
}
