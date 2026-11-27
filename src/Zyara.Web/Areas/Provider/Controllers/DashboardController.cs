using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Enums;
using Zyara.Repositories.Data;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Shared;

namespace Zyara.Web.Areas.Provider.Controllers
{
    [Area("Provider")]
    [Authorize(Roles = "Doctor,Nurse")]
    public sealed class DashboardController : Controller
    {
        private readonly ZyaraDbContext _db;
        private readonly ICurrentUser _currentUser;

        public DashboardController(ZyaraDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var providerId = _currentUser.RequireUserId();
            var provider = await _db.Providers
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken);

            if (provider is null) return NotFound();

            var egyptToday = EgyptTime.Now(TimeProvider.System).Date;

            var pendingCount = await _db.Bookings
                .CountAsync(b => b.ProviderId == providerId && b.Status == BookingStatus.PendingProviderApproval, cancellationToken);

            var activeCount = await _db.Bookings
                .CountAsync(b => b.ProviderId == providerId &&
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.ConfirmedCollectionPending || b.Status == BookingStatus.Started),
                    cancellationToken);

            var servicesCount = await _db.ProviderServices
                .CountAsync(s => s.ProviderId == providerId && s.Service!.IsActive, cancellationToken);

            var upcomingSlotsCount = await _db.TimeSlots
                .CountAsync(t => t.ProviderId == providerId && t.Date >= egyptToday && t.Status == TimeSlotStatus.Available, cancellationToken);

            var credentialsCount = await _db.ProviderCredentials
                .CountAsync(c => c.ProviderID == providerId, cancellationToken);

            ViewData["ProviderId"] = provider.Id;
            ViewData["FullName"] = provider.FullName;
            ViewData["VerificationStatus"] = provider.VerificationStatus;
            ViewData["RejectionReason"] = provider.RejectionReason;
            ViewData["WalletBalance"] = provider.WalletBalance;
            ViewData["AverageRating"] = provider.AverageRating;
            ViewData["RatingCount"] = provider.RatingCount;
            ViewData["PendingCount"] = pendingCount;
            ViewData["ActiveCount"] = activeCount;
            ViewData["ServicesCount"] = servicesCount;
            ViewData["UpcomingSlotsCount"] = upcomingSlotsCount;
            ViewData["CredentialsCount"] = credentialsCount;

            return View();
        }
    }
}

