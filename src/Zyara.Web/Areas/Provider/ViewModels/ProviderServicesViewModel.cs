using System.Collections.Generic;
using Zyara.Domain.Enums;

namespace Zyara.Web.Areas.Provider.ViewModels;

public class ProviderServiceItemViewModel
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public decimal BaseFee { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal ProviderNet => BaseFee * (1 - (CommissionPercentage / 100m));
    public bool IsActive { get; set; }
}

public class ProviderServicesViewModel
{
    public UserRole Role { get; set; }
    public List<ProviderServiceItemViewModel> Services { get; set; } = new();
}

