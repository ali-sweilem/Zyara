namespace Zyara.Services.Features.Identity.Interfaces;

/// <summary>
/// Sends an HTML email. Development: ConsoleEmailSender (Ali). Production: SmtpEmailSender (Abdelrhman, T7).
/// </summary>
public interface IAppEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}
