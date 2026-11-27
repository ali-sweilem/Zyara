using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Administration.Dtos;
using Zyara.Services.Features.Administration.Interfaces;

namespace Zyara.Services.Features.Administration
{
    public class AdminServiceCatalogService : IAdminServiceCatalogService
    {
        private readonly ZyaraDbContext _db;
        private readonly IUnitOfWork _unitOfWork;

        public AdminServiceCatalogService(ZyaraDbContext db, IUnitOfWork unitOfWork)
        {
            _db = db;
            _unitOfWork = unitOfWork;
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
                    Description = s.Description,
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
            await _unitOfWork.ExecuteInTransactionAsync(
                ct => _unitOfWork.SaveChangesAsync(ct), cancellationToken);
            return true;
        }

        public async Task<ServiceResult> CreateServiceAsync(
            string? name, string? description, ServiceType type, CancellationToken cancellationToken = default)
        {
            name = name?.Trim();
            description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            if (string.IsNullOrEmpty(name) || name.Length < 3 || name.Length > 100)
                return ServiceResult.Fail("The service name must be between 3 and 100 characters.");

            if (description is { Length: > 500 })
                return ServiceResult.Fail("The description is too long (500 characters max).");

            if (!Enum.IsDefined(type))
                return ServiceResult.Fail("Choose a valid service type.");

            var exists = await _db.Services.AnyAsync(s => s.Name == name, cancellationToken);
            if (exists)
                return ServiceResult.Fail("A service with this name already exists.");

            _db.Services.Add(new Service { Name = name, Description = description, Type = type, IsActive = true });
            await _unitOfWork.ExecuteInTransactionAsync(
                ct => _unitOfWork.SaveChangesAsync(ct), cancellationToken);

            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> UpdateServiceAsync(
            int id, string? name, string? description, ServiceType type, CancellationToken cancellationToken = default)
        {
            var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (service is null)
                return ServiceResult.Fail("Service not found.");

            name = name?.Trim();
            description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            if (string.IsNullOrEmpty(name) || name.Length < 3 || name.Length > 100)
                return ServiceResult.Fail("The service name must be between 3 and 100 characters.");

            if (description is { Length: > 500 })
                return ServiceResult.Fail("The description is too long (500 characters max).");

            if (!Enum.IsDefined(type))
                return ServiceResult.Fail("Choose a valid service type.");

            var duplicate = await _db.Services.AnyAsync(s => s.Name == name && s.Id != id, cancellationToken);
            if (duplicate)
                return ServiceResult.Fail("Another service with this name already exists.");

            service.Name = name;
            service.Description = description;
            service.Type = type;

            await _unitOfWork.ExecuteInTransactionAsync(
                ct => _unitOfWork.SaveChangesAsync(ct), cancellationToken);

            return ServiceResult.Ok();
        }
    }
}
