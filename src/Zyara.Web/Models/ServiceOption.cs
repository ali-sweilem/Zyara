namespace Zyara.Web.Models;

public sealed record ServiceOption(
    string Id,
    string Label,
    string Description,
    string Icon,
    bool Enabled,
    bool ComingSoon = false,
    string? Service = null);

public static class ServiceCatalog
{
    public static IReadOnlyList<ServiceOption> Options { get; } =
    [
        new("doctors", "Doctors", "Book an appointment with leading doctors", "bi-heart-pulse", true, Service: "doctors"),
        new("nursing", "Nursing", "Home nursing services from certified nurses", "bi-bandaid", true, Service: "nursing"),
        new("online", "Online Consultation", "Talk to your doctor from anywhere", "bi-camera-video", false, ComingSoon: true)
    ];
}
