namespace Zyara.Services.Features.Identity.Dtos;

/// <summary>The 10 one-time recovery codes. Shown once, never stored in clear text by us.</summary>
public sealed record RecoveryCodesDto(IReadOnlyList<string> Codes);
