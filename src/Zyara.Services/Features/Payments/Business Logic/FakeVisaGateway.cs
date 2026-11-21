using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Payments.Dtos;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Services.Features.Payments
{
    public class FakeVisaGateway : IPaymentGateway
    {
        public Task<CardCheckoutDto> CreateCheckoutAsync(Payment payment, string returnUrl)
        {
            var reference = $"FAKE-VISA-{Guid.NewGuid():N}";
            payment.GatewayReference = reference;

            // توجيه المستخدم لصفحة الـ Fake Checkout للمعاينة والتجربة
            var checkoutUrl = $"/CardPayment/FakeCheckout?reference={reference}&returnUrl={Uri.EscapeDataString(returnUrl)}";

            return Task.FromResult(new CardCheckoutDto
            {
                PaymentId = payment.Id,
                BookingId = payment.BookingId,
                Amount = payment.Amount,
                GatewayReference = reference,
                CheckoutUrl = checkoutUrl,
                IsRedirect = true
            });
        }

        public async Task<GatewayResultDto> VerifyWebhookAsync(HttpRequest request)
        {
            string reference = string.Empty;
            string cardLast4 = "4242";
            string cardBrand = "Visa";
            bool isSuccess = true;

            if (request.HasFormContentType)
            {
                var form = await request.ReadFormAsync();
                reference = form["reference"].ToString();
                if (form.ContainsKey("cardLast4")) cardLast4 = form["cardLast4"].ToString();
                if (form.ContainsKey("success")) bool.TryParse(form["success"], out isSuccess);
            }
            else
            {
                using var reader = new StreamReader(request.Body);
                var body = await reader.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("reference", out var refProp)) reference = refProp.GetString() ?? "";
                        if (root.TryGetProperty("cardLast4", out var last4Prop)) cardLast4 = last4Prop.GetString() ?? "4242";
                        if (root.TryGetProperty("success", out var succProp)) isSuccess = succProp.GetBoolean();
                    }
                    catch { }
                }
            }

            if (string.IsNullOrWhiteSpace(reference))
            {
                return new GatewayResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = "Missing gateway reference in webhook payload."
                };
            }

            return new GatewayResultDto
            {
                IsSuccessful = isSuccess,
                GatewayReference = reference,
                CardBrand = cardBrand,
                CardLast4 = cardLast4
            };
        }
    }
}