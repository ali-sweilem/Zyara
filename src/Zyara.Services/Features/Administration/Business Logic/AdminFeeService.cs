using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Services.Common;
using Zyara.Repositories.Data;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;

namespace Zyara.Services.Features.Administration
{
    public class AdminFeeService : IAdminFeeService
    {
        private readonly ZyaraDbContext _db;
        private readonly IUnitOfWork _unitOfWork;

        public AdminFeeService(ZyaraDbContext db, IUnitOfWork unitOfWork)
        {
            _db = db;
            _unitOfWork = unitOfWork;
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
                ServiceFeePercentage = settings.ServiceFeePercentage,
                CommissionPercentage = settings.CommissionPercentage
            };
        }

        public async Task<ServiceResult> UpdateSettingsAsync(PlatformSettingsDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.ServiceFeePercentage < 0 || dto.ServiceFeePercentage > 100)
            {
                return ServiceResult.Fail("Service fee must be between 0 and 100.");
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

            settings.ServiceFeePercentage = dto.ServiceFeePercentage;
            settings.CommissionPercentage = dto.CommissionPercentage;

            await _unitOfWork.ExecuteInTransactionAsync(
                ct => _unitOfWork.SaveChangesAsync(ct), cancellationToken);
            return ServiceResult.Ok();
        }
    }
}

