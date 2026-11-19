using Microsoft.AspNetCore.Mvc;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Identity;
using Zyara.Services.Features.Identity.Dtos;
using Zyara.Services.Features.Identity.Interfaces;

namespace Zyara.Web.Controllers
{
    /// <summary>
    /// Public registration for Patients and Providers (Doctor / Nurse).
    /// Admin accounts are never created here (seeded only - SRS 3.3.1).
    /// The role is decided by the endpoint, never by a form field.
    /// </summary>
    public class RegisterController : Controller
    {
        private readonly IRegistrationService _registration;

        public RegisterController(IRegistrationService registration)
        {
            _registration = registration;
        }

        // GET /Register : choose the account type
        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true) return Redirect("/");
            return View();
        }

        // ---------- Patient ----------

        // GET /Register/Patient
        [HttpGet]
        public IActionResult Patient()
        {
            if (User.Identity?.IsAuthenticated == true) return Redirect("/");
            return View(new RegisterPatientDto());
        }

        // POST /Register/Patient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Patient(RegisterPatientDto dto)
        {
            if (User.Identity?.IsAuthenticated == true) return Redirect("/");
            if (!ModelState.IsValid) return View(dto);

            var result = await _registration.RegisterPatientAsync(dto);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(dto);
            }

            return AfterRegister(result);
        }

        // ---------- Provider (Doctor / Nurse) ----------

        // GET /Register/Provider?type=Doctor|Nurse
        [HttpGet]
        public IActionResult Provider(UserRole? type)
        {
            if (User.Identity?.IsAuthenticated == true) return Redirect("/");

            var dto = new RegisterProviderDto
            {
                ProviderType = type is UserRole.Doctor or UserRole.Nurse ? type : UserRole.Doctor
            };
            return View(dto);
        }

        // POST /Register/Provider
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Provider(RegisterProviderDto dto)
        {
            if (User.Identity?.IsAuthenticated == true) return Redirect("/");
            if (!ModelState.IsValid) return View(dto);

            var result = await _registration.RegisterProviderAsync(dto);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(dto);
            }

            return AfterRegister(result);
        }

        // ---------- helpers ----------

        private void AddErrors(RegistrationResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }
        }

        private IActionResult AfterRegister(RegistrationResult result)
        {
            // Email confirmation is required (T7): the user isn't signed in yet,
            // a confirmation link was emailed.
            if (!result.SignedIn)
            {
                if (HttpContext.Items[EmailConfirmationService.DevLinkKey] is string devLink)
                {
                    TempData["DevConfirmLink"] = devLink;   // set in Development only
                }

                return RedirectToAction("Sent", "EmailConfirmation");
            }

            // Providers go straight to uploading their credentials (verification starts as Pending).
            // providerId is only kept in the URL because ProviderCredentialsController still reads it
            // (Mostafa T6 moves it to ICurrentUser, after which this value is simply ignored).
            if (result.Role is UserRole.Doctor or UserRole.Nurse)
            {
                return RedirectToAction("Index", "ProviderCredentials", new { providerId = result.UserId });
            }

            return RedirectToAction("Index", "Landing");
        }
    }
}
