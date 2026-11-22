using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zyara.Web.Models;

namespace Zyara.Web.Controllers;

[AllowAnonymous]
public class ContactController : Controller
{
    private readonly ILogger<ContactController> _logger;

    public ContactController(ILogger<ContactController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ContactInquiryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(ContactInquiryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _logger.LogInformation("Contact Inquiry received from {Name} ({Email}): {Type} - {Subject}",
            model.FullName, model.Email, model.InquiryType, model.Subject);

        TempData["SuccessMessage"] = "Thank you! Your inquiry has been received. Our medical coordination team will contact you within 2 hours.";

        return RedirectToAction(nameof(Index));
    }
}

