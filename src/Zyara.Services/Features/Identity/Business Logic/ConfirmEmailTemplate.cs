using System.Net;

namespace Zyara.Services.Features.Identity;

public static class ConfirmEmailTemplate
{
    public const string Subject = "Confirm your Zyara email";

    public static string Build(string fullName, string link)
    {
        var name = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName);
        var href = WebUtility.HtmlEncode(link);   // '&' in the query string must be encoded inside an attribute

        return $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:520px;margin:auto;padding:24px;color:#1f2937;">
              <h2 style="margin:0 0 12px;color:#0d6efd;">Welcome to Zyara</h2>
              <p>Hi {name},</p>
              <p>Please confirm your email address to activate your account.</p>
              <p style="margin:24px 0;">
                <a href="{href}" style="background:#0d6efd;color:#ffffff;text-decoration:none;padding:12px 24px;border-radius:24px;display:inline-block;">
                  Confirm my email
                </a>
              </p>
              <p style="font-size:13px;color:#6b7280;">This link expires in 30 minutes and can be used once.
              If you didn't create a Zyara account, you can ignore this email.</p>
            </div>
            """;
    }
}
