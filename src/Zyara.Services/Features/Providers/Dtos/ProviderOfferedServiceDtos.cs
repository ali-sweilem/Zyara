using System.ComponentModel.DataAnnotations;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Providers.Dtos;

/// <summary>A service this provider already offers (one ProviderService row).</summary>
public sealed class OfferedServiceDto
{
    public int Id { get; set; }                 // ProviderService.Id
    public int ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceType Type { get; set; }
    public decimal Fee { get; set; }
    /// <summary>False = the admin disabled this service, so patients can't see or book it right now.</summary>
    public bool IsActive { get; set; }
}

/// <summary>A catalog service the provider is allowed to add and doesn't offer yet.</summary>
public sealed class AvailableServiceDto
{
    public int ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ServiceType Type { get; set; }
}

public sealed class ProviderServicesPageDto
{
    public List<OfferedServiceDto> Offered { get; set; } = new();
    public List<AvailableServiceDto> Available { get; set; } = new();
    public decimal MaxFee { get; set; }
}

public sealed class AddOfferedServiceDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Choose a service.")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Enter the price.")]
    [Range(0.01, 100000, ErrorMessage = "Enter a valid price.")]
    public decimal Fee { get; set; }
}

public sealed class UpdateOfferedServiceDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Enter the price.")]
    [Range(0.01, 100000, ErrorMessage = "Enter a valid price.")]
    public decimal Fee { get; set; }
}
