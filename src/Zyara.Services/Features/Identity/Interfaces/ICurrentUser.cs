using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Identity.Interfaces;

/// <summary>
/// The only way controllers/services get the logged-in user's id.
/// Never read userId / patientId / providerId from a form or the URL.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Null when the visitor is anonymous.</summary>
    int? UserId { get; }

    /// <summary>Null when anonymous (or no known role claim).</summary>
    UserRole? Role { get; }

    /// <summary>Returns the id or throws <see cref="UnauthorizedAccessException"/> if anonymous.</summary>
    int RequireUserId();
}
