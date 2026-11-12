using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Services.Common;
using Zyara.Services.Data;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;

namespace Zyara.Services.Features.Administration
{
    public class AdminFeeService : IAdminFeeService
    {
        private readonly ZyaraDbContext _db;

        public AdminFeeService(ZyaraDbContext db)
        {
            _db = db;
        }

        public async Task<PlatformSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _db.PlatformSettings
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            // No row yet: show the entity defaults, nothing is saved until the admin submits.
            settings ??= new PlatformSettings();

            return new PlatformSettingsDto
            {
                ServiceFee = settings.ServiceFee,
                CommissionPercentage = settings.CommissionPercentage
            };
        }

        public async Task<ServiceResult> UpdateSettingsAsync(PlatformSettingsDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.ServiceFee < 0)
            {
                return ServiceResult.Fail("Service fee cannot be negative.");
            }

            if (dto.CommissionPercentage < 0 || dto.CommissionPercentage > 100)
            {
                return ServiceResult.Fail("Commission must be between 0 and 100.");
            }

            var settings = await _db.PlatformSettings
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (settings is null)
            {
                settings = new PlatformSettings();
                _db.PlatformSettings.Add(settings);
            }

            settings.ServiceFee = dto.ServiceFee;
            settings.CommissionPercentage = dto.CommissionPercentage;

            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok();
        }
    }
}
