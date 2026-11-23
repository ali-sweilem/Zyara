using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data.Repositories;
using Zyara.Services.Common;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services.Features.Providers;

public sealed class ProviderServiceCatalogService : IProviderServiceCatalogService
{
    private const decimal MaxFee = 100000m;

    private readonly IRepository<ProviderService> _offers;
    private readonly IRepository<Service> _catalog;
    private readonly IRepository<Provider> _providers;
    private readonly IRepository<PlatformSettings> _settings;
    private readonly IUnitOfWork _unitOfWork;

    public ProviderServiceCatalogService(
        IRepository<ProviderService> offers,
        IRepository<Service> catalog,
        IRepository<Provider> providers,
        IRepository<PlatformSettings> settings,
        IUnitOfWork unitOfWork)
    {
        _offers = offers;
        _catalog = catalog;
        _providers = providers;
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    // ───────────────────────── rules ─────────────────────────

    /// <summary>Which catalog service types a doctor / a nurse may offer. Change it here only.</summary>
    private static bool CanOffer(UserRole role, ServiceType type) => role switch
    {
        UserRole.Doctor => type is ServiceType.DoctorHomeVisit or ServiceType.RemoteConsultation,
        UserRole.Nurse => type is ServiceType.NurseHomeVisit,
        _ => false
    };

    // ───────────────────────── page ─────────────────────────

    public async Task<ProviderServicesPageDto> GetPageAsync(int providerId, CancellationToken cancellationToken = default)
    {
        var role = await GetRoleAsync(providerId, cancellationToken);

        var offered = await _offers.Query()
            .AsNoTracking()
            .Where(ps => ps.ProviderId == providerId)
            .OrderBy(ps => ps.Service!.Name)
            .Select(ps => new OfferedServiceDto
            {
                Id = ps.Id,
                ServiceId = ps.ServiceId,
                Name = ps.Service!.Name,
                Description = ps.Service.Description,
                Type = ps.Service.Type,
                Fee = ps.Fee,
                IsActive = ps.Service.IsActive
            })
            .ToListAsync(cancellationToken);

        var offeredIds = offered.Select(o => o.ServiceId).ToHashSet();

        var catalog = await _catalog.Query()
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Type).ThenBy(s => s.Name)
            .Select(s => new AvailableServiceDto
            {
                ServiceId = s.Id,
                Name = s.Name,
                Description = s.Description,
                Type = s.Type
            })
            .ToListAsync(cancellationToken);

        return new ProviderServicesPageDto
        {
            Offered = offered,
            Available = role is null
                ? new List<AvailableServiceDto>()
                : catalog.Where(s => !offeredIds.Contains(s.ServiceId) && CanOffer(role.Value, s.Type)).ToList(),
            MaxFee = MaxFee
        };
    }

    // ───────────────────────── add / update / remove ─────────────────────────

    public async Task<ServiceResult> AddAsync(int providerId, AddOfferedServiceDto dto, CancellationToken cancellationToken = default)
    {
        var fee = NormalizeFee(dto.Fee);
        if (fee is null) return ServiceResult.Fail($"Enter a price between 0.01 and {MaxFee:0} EGP.");

        var role = await GetRoleAsync(providerId, cancellationToken);
        if (role is null) return ServiceResult.Fail("Provider not found.");

        var service = await _catalog.FirstOrDefaultAsync(s => s.Id == dto.ServiceId && s.IsActive, cancellationToken);
        if (service is null) return ServiceResult.Fail("This service is not available.");

        if (!CanOffer(role.Value, service.Type))
            return ServiceResult.Fail("This service is not available for your profession.");

        var already = await _offers.Query()
            .AnyAsync(ps => ps.ProviderId == providerId && ps.ServiceId == service.Id, cancellationToken);
        if (already) return ServiceResult.Fail("You already offer this service.");

        // commission is calculated from the platform settings at booking time; we store the current rate for reference
        var commission = await _settings.Query().AsNoTracking()
            .Select(s => (decimal?)s.CommissionPercentage)
            .FirstOrDefaultAsync(cancellationToken) ?? new PlatformSettings().CommissionPercentage;

        _offers.Add(new ProviderService
        {
            ProviderId = providerId,
            ServiceId = service.Id,
            Fee = fee.Value,
            CommissionPercentage = commission
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateFeeAsync(int providerId, UpdateOfferedServiceDto dto, CancellationToken cancellationToken = default)
    {
        var fee = NormalizeFee(dto.Fee);
        if (fee is null) return ServiceResult.Fail($"Enter a price between 0.01 and {MaxFee:0} EGP.");

        var offer = await _offers.FirstOrDefaultAsync(ps => ps.Id == dto.Id && ps.ProviderId == providerId, cancellationToken);
        if (offer is null) return ServiceResult.Fail("Service not found.");

        // Existing bookings keep the price they were made with (it is copied into the booking), only new bookings use this.
        offer.Fee = fee.Value;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveAsync(int providerId, int providerServiceId, CancellationToken cancellationToken = default)
    {
        var offer = await _offers.FirstOrDefaultAsync(ps => ps.Id == providerServiceId && ps.ProviderId == providerId, cancellationToken);
        if (offer is null) return ServiceResult.Fail("Service not found.");

        // Bookings point at the catalog Service (not at this row), so removing the offer never breaks an existing booking.
        _offers.Remove(offer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Ok();
    }

    // ───────────────────────── helpers ─────────────────────────

    private async Task<UserRole?> GetRoleAsync(int providerId, CancellationToken ct) =>
        await _providers.Query()
            .AsNoTracking()
            .Where(p => p.Id == providerId)
            .Select(p => (UserRole?)p.Role)
            .FirstOrDefaultAsync(ct);

    private static decimal? NormalizeFee(decimal fee)
    {
        fee = Math.Round(fee, 2, MidpointRounding.AwayFromZero);
        return fee is > 0m and <= MaxFee ? fee : null;
    }
}
