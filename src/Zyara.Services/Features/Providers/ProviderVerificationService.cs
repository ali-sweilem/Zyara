using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Services.Data;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services.Features.Providers
{
    public class ProviderVerificationService : IProviderVerficationService
    {
        private readonly ZyaraDbContext DB;

        public ProviderVerificationService(ZyaraDbContext DB)
        {
            this.DB = DB;
        }

        public async Task<VerificationStatusDto?> GetStatusAsync(int providerId)
        {
            return await DB.Providers
                .AsNoTracking()
                .Where(p => p.Id == providerId)
                .Select(p => new VerificationStatusDto
                {
                    Status = p.VerificationStatus,
                    RejectionReason = p.RejectionReason
                })
                .FirstOrDefaultAsync();
        }

       

        public async Task<bool> IsApprovedAsync(int providerId)
        {
            return await DB.Providers
                .AnyAsync(p => p.Id == providerId
                            && p.VerificationStatus == VerificationStatus.Verified);
        }

    

        public async Task ResetToPendingAsync(int providerId)
        {
            var provider = await DB.Providers.FindAsync(providerId);
            if (provider == null) return;

            // Approved or Rejected -> Pending (new documents need a new review)
            if (provider.VerificationStatus != VerificationStatus.Pending)
            {
                provider.VerificationStatus = VerificationStatus.Pending;
                provider.RejectionReason = null;
                await DB.SaveChangesAsync();
            }
        }
    }
}