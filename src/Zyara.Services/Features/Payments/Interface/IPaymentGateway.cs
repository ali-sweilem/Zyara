using System.Threading.Tasks;

namespace Zyara.Services.Features.Payments.Interface;

public record GatewayResult(bool Succeeded, string? Reference, string? FailureReason)
{
    public static GatewayResult Success(string reference) => new(true, reference, null);
    public static GatewayResult Failure(string reason) => new(false, null, reason);
}

public record CardInput(
    string CardHolderName,
    string CardNumber,
    string ExpiryMonth,
    string ExpiryYear,
    string Cvv);

public interface IPaymentGateway
{
    bool CollectsCardDetailsInApp { get; }
    Task<GatewayResult> AuthorizeAsync(decimal amount, int bookingId, CardInput? card = null);
    Task<GatewayResult> CaptureAsync(string gatewayReference, decimal amount);
    Task<GatewayResult> ReleaseAsync(string gatewayReference);
}