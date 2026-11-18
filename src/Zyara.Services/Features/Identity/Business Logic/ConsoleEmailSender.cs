using System.Net;
using System.Text.RegularExpressions;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

/// <summary>
/// DEVELOPMENT ONLY: prints the email (and every link in it) to the console,
/// so nobody has to wait for SMTP to test confirm-email / reset-password.
/// </summary>
public sealed partial class ConsoleEmailSender : IAppEmailSender
{
    [GeneratedRegex("href\\s*=\\s*[\"'](?<url>[^\"']+)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex LinkRegex();

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        Console.WriteLine();
        Console.WriteLine("================ [DEV EMAIL] ================");
        Console.WriteLine($"To:      {to}");
        Console.WriteLine($"Subject: {subject}");
        foreach (Match m in LinkRegex().Matches(htmlBody ?? string.Empty))
        {
            Console.WriteLine($"Link:    {WebUtility.HtmlDecode(m.Groups["url"].Value)}");
        }
        Console.WriteLine("=============================================");
        Console.WriteLine();
        return Task.CompletedTask;
    }
}
