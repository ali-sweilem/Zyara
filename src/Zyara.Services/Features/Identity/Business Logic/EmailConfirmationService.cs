using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Services.Features.Identity;

public sealed class EmailConfirmationService : IEmailConfirmationService
{
    /// <summary>HttpContext.Items key that holds the confirm link (Development only).</summary>
    public const string DevLinkKey = "Zyara.DevConfirmLink";

    private readonly UserManager<User> _users;
    private readonly IAppEmailSender _email;
    private readonly LinkGenerator _links;
    private readonly IHttpContextAccessor _http;
    private readonly IHostEnvironment _env;
    private readonly ILogger<EmailConfirmationService> _logger;

    public EmailConfirmationService(
        UserManager<User> users,
        IAppEmailSender email,
        LinkGenerator links,
        IHttpContextAccessor http,
        IHostEnvironment env,
        ILogger<EmailConfirmationService> logger)
    {
        _users = users;
        _email = email;
        _links = links;
        _http = http;
        _env = env;
        _logger = logger;
    }

    public async Task SendAsync(User user)
    {
        if (user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email)) return;

        var token = await _users.GenerateEmailConfirmationTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var httpContext = _http.HttpContext
            ?? throw new InvalidOperationException("A confirmation link can only be built during a web request.");

        var link = _links.GetUriByAction(
            httpContext, action: "Confirm", controller: "EmailConfirmation",
            values: new { userId = user.Id, code })
            ?? throw new InvalidOperationException("Could not build the confirmation link.");

        // DEVELOPMENT ONLY: hand the link to the page (Sent.cshtml shows a "Confirm now" button),
        // so nobody has to copy it from the console. Never set in other environments.
        if (_env.IsDevelopment())
        {
            httpContext.Items[DevLinkKey] = link;
        }

        await _email.SendAsync(user.Email, ConfirmEmailTemplate.Subject, ConfirmEmailTemplate.Build(user.FullName, link));
        // never log the link / token
    }

    public async Task<bool> ConfirmAsync(string? userId, string? code)
    {
        if (!int.TryParse(userId, out var id) || string.IsNullOrWhiteSpace(code)) return false;

        var user = await _users.FindByIdAsync(id.ToString());
        if (user is null) return false;

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return false;
        }

        var result = await _users.ConfirmEmailAsync(user, token);
        return result.Succeeded;
    }

    public async Task ResendAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return;

        var user = await _users.FindByEmailAsync(email.Trim());
        if (user is null || user.EmailConfirmed || !user.IsActive) return;

        try
        {
            await SendAsync(user);
        }
        catch (Exception ex)
        {
            // Swallow: the caller must not learn whether the address exists or whether sending failed.
            _logger.LogError(ex, "Could not resend the confirmation email.");
        }
    }
}
