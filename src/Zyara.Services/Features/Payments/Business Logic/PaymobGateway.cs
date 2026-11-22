using System;
using System.Threading.Tasks;
using Zyara.Services.Features.Payments.Interface;

namespace Zyara.Services.Features.Payments;

public class PaymobGateway : IPaymentGateway
{
    public bool CollectsCardDetailsInApp => false;

    public Task<GatewayResult> AuthorizeAsync(decimal amount, int bookingId, CardInput? card = null)
    {
        // TODO: The real implementation will use a hosted iframe/redirect flow.
        throw new NotSupportedException("Paymob hosted integration is not yet supported. Please use the Fake payment gateway for testing.");
    }

    public Task<GatewayResult> CaptureAsync(string gatewayReference, decimal amount)
    {
        // TODO: Real Paymob transaction capture API call.
        throw new NotSupportedException("Paymob capture is not yet supported.");
    }

    public Task<GatewayResult> ReleaseAsync(string gatewayReference)
    {
        // TODO: Real Paymob void/refund authorization API call.
        throw new NotSupportedException("Paymob release is not yet supported.");
    }
}