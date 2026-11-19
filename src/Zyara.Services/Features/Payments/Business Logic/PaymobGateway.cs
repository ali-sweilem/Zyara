using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Payments.Dtos;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Services.Features.Payments
{
    public class PaymobGateway : IPaymentGateway
    {
        private readonly IConfiguration _config;

        public PaymobGateway(IConfiguration config)
        {
            _config = config;
        }

        public Task<CardCheckoutDto> CreateCheckoutAsync(Payment payment, string returnUrl)
        {
            var reference = $"PAYMOB-{payment.BookingId}-{Guid.NewGuid().ToString("N")[..8]}";
            payment.GatewayReference = reference;

            var iframeId = _config["Payment:Paymob:IframeId"] ?? "123456";
            var checkoutUrl = $"https://accept.paymob.com/api/acceptance/iframes/{iframeId}?payment_token={reference}";

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
            var hmacSecret = _config["Payment:Paymob:HmacSecret"] ?? "test_secret";

            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                return new GatewayResultDto { IsSuccessful = false, ErrorMessage = "Empty payload" };
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var obj = root.GetProperty("obj");

                bool success = obj.GetProperty("success").GetBoolean();
                string reference = obj.GetProperty("order").GetProperty("id").ToString();

                string cardLast4 = "1234";
                string cardBrand = "Visa";

                if (obj.TryGetProperty("source_data", out var sourceData))
                {
                    if (sourceData.TryGetProperty("pan", out var pan))
                    {
                        var panStr = pan.GetString() ?? "";
                        if (panStr.Length >= 4) cardLast4 = panStr[^4..];
                    }
                    if (sourceData.TryGetProperty("sub_type", out var subType))
                    {
                        cardBrand = subType.GetString() ?? "Visa";
                    }
                }

                return new GatewayResultDto
                {
                    IsSuccessful = success,
                    GatewayReference = reference,
                    CardBrand = cardBrand,
                    CardLast4 = cardLast4
                };
            }
            catch (Exception ex)
            {
                return new GatewayResultDto
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Webhook verification failed: {ex.Message}"
                };
            }
        }
    }
}