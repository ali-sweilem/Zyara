using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Identity;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Controllers
{
    /// <summary>Email confirmation: /EmailConfirmation/Confirm · /Resend · /Sent</summary>
    public class EmailConfirmationController : Controller
    {
        private readonly IEmailConfirmationService _confirmation;

        public EmailConfirmationController(IEmailConfirmationService confirmation)
        {
            _confirmation = confirmation;
        }

        // GET /EmailConfirmation/Sent : "check your inbox"
        [HttpGet]
        public IActionResult Sent() => View();

        // GET /EmailConfirmation/Confirm?userId=..&code=..  (link from the email)
        [HttpGet]
        public async Task<IActionResult> Confirm(string? userId, string? code)
        {
            var ok = await _confirmation.ConfirmAsync(userId, code);
            return View("Confirmed", ok);   // model: true = confirmed, false = invalid/expired/used link
        }

        // GET /EmailConfirmation/Resend
        [HttpGet]
        public IActionResult Resend(string? email) => View(new ResendConfirmationDto { Email = email ?? string.Empty });

        // POST /EmailConfirmation/Resend : same response whether the email exists or not
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resend(ResendConfirmationDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            await _confirmation.ResendAsync(dto.Email);

            if (HttpContext.Items[EmailConfirmationService.DevLinkKey] is string devLink)
            {
                TempData["DevConfirmLink"] = devLink;   // Development only
            }

            TempData["ResendInfo"] = "If this email is registered and not confirmed yet, a new confirmation link is on its way.";
            return RedirectToAction(nameof(Sent));
        }
    }
}
