using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Zyara.Domain.Entities;
using Zyara.Services.Features.Payments.Dtos;

namespace Zyara.Services.Features.Payments.Interface
{
    public interface IPaymentGateway
    {
        Task<CardCheckoutDto> CreateCheckoutAsync(Payment payment, string returnUrl);
        Task<GatewayResultDto> VerifyWebhookAsync(HttpRequest request);
    }
}