using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Authorization;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Services.Features.Payments.Dtos;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Web.Controllers;

/// <summary>
/// T12: pay by card. Flow: Checkout (we create the payment, amount from the DB) -> gateway page ->
/// Webhook (gateway tells us the result, signed) -> Return (we show the result from the DB).
/// The card number is never part of any of our models, logs or tables.
/// </summary>
[Authorize(Policy = PolicyNames.PatientOnly)]
public class CardPaymentController : Controller
{
    private const string ViewFolder = "~/Views/Payment/";

    private readonly ICardPaymentService _card;
    private readonly IBookingService _bookings;
    private readonly IAuthorizationService _auth;
    private readonly IHostedCheckoutSimulator? _simulator;

    public CardPaymentController(
        ICardPaymentService card,
        IBookingService bookings,
        IAuthorizationService auth,
        IHostedCheckoutSimulator? simulator = null)
    {
        _card = card;
        _bookings = bookings;
        _auth = auth;
        _simulator = simulator;
    }

    // POST /CardPayment/Checkout  (from the "Pay by card" button)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int bookingId)
    {
        if (!await OwnsBookingAsync(bookingId)) return NotFound();

        var returnUrl = Url.Action(nameof(Return), "CardPayment", new { bookingId }, Request.Scheme)!;
        var checkout = await _card.StartCheckoutAsync(bookingId, returnUrl);
        if (checkout is null)
        {
            TempData["Error"] = "Payment for this booking was already chosen or completed.";
            return RedirectToAction("Create", "Payment", new { bookingId });
        }

        return Redirect(checkout.RedirectUrl);
    }

    // GET /CardPayment/HostedPage?reference=...   (demo gateway page; a real gateway hosts this on its own domain)
    [HttpGet]
    public async Task<IActionResult> HostedPage(string reference)
    {
        if (_simulator is null) return NotFound();

        var info = await _card.GetCheckoutAsync(reference);
        if (info is null || !await OwnsBookingAsync(info.BookingId)) return NotFound();

        return View(ViewFolder + "CardCheckout.cshtml", info);
    }

    // POST /CardPayment/HostedPage  (demo only: plays the gateway, then sends us the signed webhook)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HostedPage(string reference, string? cardNumber)
    {
        if (_simulator is null) return NotFound();

        var info = await _card.GetCheckoutAsync(reference);
        if (info is null || !await OwnsBookingAsync(info.BookingId)) return NotFound();

        var (body, signature) = _simulator.Charge(reference, cardNumber ?? string.Empty);
        await _card.HandleWebhookAsync(body, signature);   // same code path as the real webhook

        return RedirectToAction(nameof(Return), new { bookingId = info.BookingId });
    }

    // POST /CardPayment/Webhook  (called by the gateway, server to server: no login, no antiforgery, signature instead)
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [HttpPost]
    public async Task<IActionResult> Webhook()
    {
        if (Request.ContentLength is > 16 * 1024) return StatusCode(StatusCodes.Status413PayloadTooLarge);

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Signature"].FirstOrDefault();

        var outcome = await _card.HandleWebhookAsync(body, signature);

        // 200 for everything legitimate (including repeats) so the gateway stops retrying; 400 only for forged calls.
        return outcome == WebhookOutcome.InvalidSignature ? BadRequest() : Ok();
    }

    // GET /CardPayment/Return?bookingId=...   (the result is read from the DB, not from the URL)
    [HttpGet]
    public async Task<IActionResult> Return(int bookingId)
    {
        if (!await OwnsBookingAsync(bookingId)) return NotFound();

        var result = await _card.GetResultAsync(bookingId);
        if (result is null) return NotFound();

        return View(ViewFolder + "CardResult.cshtml", result);
    }

    private async Task<bool> OwnsBookingAsync(int bookingId)
    {
        var booking = await _bookings.GetBookingAsync(bookingId);
        if (booking is null) return false;

        var authorization = await _auth.AuthorizeAsync(User, booking, Operations.Manage);
        return authorization.Succeeded;
    }
}
