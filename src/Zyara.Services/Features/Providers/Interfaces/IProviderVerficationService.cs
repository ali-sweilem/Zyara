using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Services.Features.Providers.Dtos;

namespace Zyara.Services.Features.Providers.Interfaces
{
    public interface IProviderVerficationService
    {
        Task<VerificationStatusDto?> GetStatusAsync(int providerId);
        Task<bool> IsApprovedAsync(int providerId);
        Task ResetToPendingAsync(int ProviderProfileId);
    }
}
