using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Common;
using Zyara.Services.Data;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services.Features.Administration
{
    public class AdminProviderReviewService : IAdminProviderReviewService
    {
        private readonly ZyaraDbContext _db;
        private readonly IProviderCredentialService _credentials;

        public AdminProviderReviewService(ZyaraDbContext db, IProviderCredentialService credentials)
        {
            _db = db;
            _credentials = credentials;
        }

        public async Task<IReadOnlyList<PendingProviderDto>> GetPendingProvidersAsync(CancellationToken cancellationToken = default)
        {
            var list = await _db.Providers
                .AsNoTracking()
                .Where(p => p.VerificationStatus == VerificationStatus.Pending)
                .OrderBy(p => p.CreatedAt)
                .Select(p => new PendingProviderDto
                {
                    Id = p.Id,
                    FullName = p.FullName,
                    Email = p.Email,
                    Role = p.Role,
                    Specialty = p.Specialty,
                    ExperienceYears = p.ExperienceYears,
                    BaseCity = p.BaseCity
                })
                .ToListAsync(cancellationToken);

            // The documents of each provider (the list is small, so one call each is fine)
            foreach (var p in list)
                p.Credentials = await _credentials.GetMyCredentialsAsync(p.Id);

            return list;
        }

        public async Task<ServiceResult> ReviewProviderAsync(int providerId, bool approve, string? reason, CancellationToken cancellationToken = default)
        {
            var provider = await _db.Providers.FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);
            if (provider is null)
                return ServiceResult.Fail("Provider not found.");

            if (provider.VerificationStatus != VerificationStatus.Pending)
                return ServiceResult.Fail("This provider is not waiting for review.");

            if (!approve && string.IsNullOrWhiteSpace(reason))
                return ServiceResult.Fail("A reason is required to reject a provider.");

            // No login yet: the first admin is recorded as the reviewer
            var adminId = await _db.Admins.OrderBy(a => a.Id)
                .Select(a => (int?)a.Id).FirstOrDefaultAsync(cancellationToken);

            provider.VerificationStatus = approve ? VerificationStatus.Verified : VerificationStatus.Rejected;
            provider.RejectionReason = approve ? null : reason!.Trim();
            provider.VerifiedAt = DateTime.UtcNow;
            provider.VerifiedByAdminId = adminId;

            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok();
        }
    }
}