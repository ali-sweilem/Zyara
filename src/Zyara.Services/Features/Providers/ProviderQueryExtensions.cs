using System.Linq;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Providers;

internal static class ProviderQueryExtensions
{
    public static IQueryable<Provider> PubliclyVisible(this IQueryable<Provider> query) =>
        query.Where(p => p.VerificationStatus == VerificationStatus.Verified && p.IsActive);
}
