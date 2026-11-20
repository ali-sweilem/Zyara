using System.Text.Encodings.Web;

namespace Zyara.Services.Features.Identity.Business_Logic;

public static class ResetPasswordEmailTemplate
{
    public static string Generate(string resetLink)
    {
        var encodedUrl = HtmlEncoder.Default.Encode(resetLink);
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Reset Your Password</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background-color: #f8f9fa; border-radius: 8px; padding: 24px; border: 1px solid #e9ecef;"">
        <h2 style=""color: #0d6efd; margin-top: 0;"">Zyara - Password Reset Request</h2>
        <p>Hello,</p>
        <p>We received a request to reset your password. If you made this request, please click the button below to choose a new password:</p>
        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{encodedUrl}"" style=""background-color: #0d6efd; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold; display: inline-block;"">Reset Password</a>
        </div>
        <p>This password reset link will expire in 30 minutes and can only be used once.</p>
        <p>If you did not request a password reset, you can safely ignore this email.</p>
        <hr style=""border: none; border-top: 1px solid #dee2e6; margin: 20px 0;"">
        <p style=""font-size: 12px; color: #6c757d;"">If the button above doesn't work, copy and paste this link into your browser:</p>
        <p style=""font-size: 12px; color: #6c757d; word-break: break-all;"">{encodedUrl}</p>
    </div>
</body>
</html>";
    }
}

