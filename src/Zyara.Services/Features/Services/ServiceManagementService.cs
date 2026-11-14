
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Services.Common;
using Zyara.Services.Data;

namespace Zyara.Services.Features.Services;

public sealed class ServiceManagementService : IServiceManagementService
{
    private readonly ZyaraDbContext _db;

    public ServiceManagementService(ZyaraDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ServiceDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Services
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Type = s.Type,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Services
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Type = s.Type,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<ServiceDto>> CreateAsync(
        CreateServiceDto dto,
        CancellationToken cancellationToken = default)
    {
        var service = new Service
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Type = dto.Type,
            IsActive = true
        };

        _db.Services.Add(service);

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<ServiceDto>.Ok(MapToDto(service));
    }

    public async Task<ServiceResult<ServiceDto>> UpdateAsync(
        int id,
        UpdateServiceDto dto,
        CancellationToken cancellationToken = default)
    {
        var service = await _db.Services
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (service is null)
        {
            return ServiceResult<ServiceDto>.Fail("Service not found.");
        }

        service.Name = dto.Name.Trim();
        service.Description = dto.Description;
        service.Type = dto.Type;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<ServiceDto>.Ok(MapToDto(service));
    }

    public async Task<ServiceResult> SetActiveStatusAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var service = await _db.Services
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (service is null)
        {
            return ServiceResult.Fail("Service not found.");
        }

        service.IsActive = isActive;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok();
    }

    private static ServiceDto MapToDto(Service service)
    {
        return new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            Type = service.Type,
            IsActive = service.IsActive
        };
    }
}
