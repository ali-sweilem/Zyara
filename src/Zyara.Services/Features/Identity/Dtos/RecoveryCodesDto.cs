namespace Zyara.Services.Features.Identity.Dtos;

/// <summary>
/// The one-time list of recovery codes. Shown once right after 2FA is enabled
/// (Identity stores only hashes of these codes).
/// </summary>
public sealed class RecoveryCodesDto
{
    public IReadOnlyList<string> Codes { get; set; } = Array.Empty<string>();
}
