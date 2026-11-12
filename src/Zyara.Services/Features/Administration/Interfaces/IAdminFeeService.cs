using System.Threading;
using System.Threading.Tasks;
using Zyara.Services.Common;
using Zyara.Services.Features.Administration.Dtos;

namespace Zyara.Services.Features.Administration.Interfaces
{
    public interface IAdminFeeService
    {
        /// <summary>Current platform settings (defaults if no row exists yet).</summary>
        Task<PlatformSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

        /// <summary>Validates and saves the settings (creates the row if it does not exist).</summary>
        Task<ServiceResult> UpdateSettingsAsync(PlatformSettingsDto dto, CancellationToken cancellationToken = default);
    }
}
