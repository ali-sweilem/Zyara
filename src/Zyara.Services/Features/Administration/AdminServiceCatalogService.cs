using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Services.Data;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;

namespace Zyara.Services.Features.Administration
{
    public class AdminServiceCatalogService : IAdminServiceCatalogService
    {
        private readonly ZyaraDbContext _db;

        public AdminServiceCatalogService(ZyaraDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ServiceItemDto>> GetServicesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Services
                .AsNoTracking()
                .OrderBy(s => s.Type)
                .ThenBy(s => s.Name)
                .Select(s => new ServiceItemDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Type = s.Type,
                    IsActive = s.IsActive
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ToggleServiceAsync(int id, CancellationToken cancellationToken = default)
        {
            var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (service is null)
            {
                return false;
            }

            service.IsActive = !service.IsActive;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
