using System.Text;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Services.Features.Payments;
using Zyara.Services.Features.Payments.Dtos;

namespace Zyara.Tests;

public class PaymentGatewayTests
{
    private static Payment PendingOnline() => new()
    {
        Method = PaymentMethod.OnlinePayment,
        Status = PaymentStatus.Pending,
        Amount = 300m
    };

    // ===== Payment state machine =====

    [Fact]
    public void Pending_online_payment_can_be_authorized()
    {
        var p = PendingOnline();
        p.Authorize();
        Assert.Equal(PaymentStatus.Authorized, p.Status);
    }

    [Fact]
    public void Authorize_twice_is_rejected_by_the_entity()
    {
        var p = PendingOnline();
        p.Authorize();
        Assert.Throws<InvalidOperationException>(() => p.Authorize());
    }

    [Fact]
    public void Captured_payment_can_be_partially_refunded()
    {
        var p = PendingOnline();
        p.Authorize();
        p.Capture();
        p.Refund(100m);
        Assert.Equal(PaymentStatus.PartiallyRefunded, p.Status);
        Assert.Equal(100m, p.RefundedAmount);
    }

    [Fact]
    public void Captured_payment_can_be_fully_refunded()
    {
        var p = PendingOnline();
        p.Authorize();
        p.Capture();
        p.Refund(300m);
        Assert.Equal(PaymentStatus.Refunded, p.Status);
    }

    [Fact]
    public void Pending_payment_cannot_be_refunded()
    {
        var p = PendingOnline();
        Assert.Throws<InvalidOperationException>(() => p.Refund(10m));
    }

    [Fact]
    public void Authorized_payment_can_be_released()
    {
        var p = PendingOnline();
        p.Authorize();
        p.Release();
        Assert.Equal(PaymentStatus.Released, p.Status);
    }

    [Fact]
    public void Cash_payment_cannot_use_online_transitions()
    {
        var p = new Payment { Method = PaymentMethod.CashOnArrival, Status = PaymentStatus.Pending };
        Assert.Throws<InvalidOperationException>(() => p.Authorize());
    }

    // ===== Webhook idempotency =====

    [Fact]
    public void Successful_webhook_authorizes_and_stores_only_brand_and_last4()
    {
        var p = PendingOnline();
        var outcome = CardPaymentService.Apply(p, new GatewayWebhookEvent("R1", true, "Visa", "4242"));

        Assert.Equal(WebhookOutcome.Applied, outcome);
        Assert.Equal(PaymentStatus.Authorized, p.Status);
        Assert.Equal("Visa", p.CardBrand);
        Assert.Equal("4242", p.CardLast4);
    }

    [Fact]
    public void Same_webhook_twice_changes_nothing_the_second_time()
    {
        var p = PendingOnline();
        var ev = new GatewayWebhookEvent("R1", true, "Visa", "4242");

        Assert.Equal(WebhookOutcome.Applied, CardPaymentService.Apply(p, ev));
        Assert.Equal(WebhookOutcome.Duplicate, CardPaymentService.Apply(p, ev));
        Assert.Equal(PaymentStatus.Authorized, p.Status);
    }

    [Fact]
    public void Failed_webhook_leaves_the_payment_pending()
    {
        var p = PendingOnline();
        var outcome = CardPaymentService.Apply(p, new GatewayWebhookEvent("R1", false, null, null));

        Assert.Equal(WebhookOutcome.Failed, outcome);
        Assert.Equal(PaymentStatus.Pending, p.Status);
    }

    [Fact]
    public void A_late_failure_cannot_undo_an_authorized_payment()
    {
        var p = PendingOnline();
        CardPaymentService.Apply(p, new GatewayWebhookEvent("R1", true, "Visa", "4242"));

        var outcome = CardPaymentService.Apply(p, new GatewayWebhookEvent("R1", false, null, null));

        Assert.Equal(WebhookOutcome.Duplicate, outcome);
        Assert.Equal(PaymentStatus.Authorized, p.Status);
    }

    [Fact]
    public void Garbage_last4_is_not_stored()
    {
        var p = PendingOnline();
        CardPaymentService.Apply(p, new GatewayWebhookEvent("R1", true, "Visa", "4242424242424242"));
        Assert.Null(p.CardLast4);
    }

    // ===== Fake gateway =====

    [Fact]
    public void Test_card_produces_a_valid_signed_success()
    {
        var gw = new FakeVisaGateway("secret");
        var (body, sig) = gw.Charge("FAKE-1", "4242 4242 4242 4242");

        var ev = gw.VerifyWebhook(body, sig);

        Assert.NotNull(ev);
        Assert.True(ev!.Succeeded);
        Assert.Equal("FAKE-1", ev.Reference);
        Assert.Equal("4242", ev.CardLast4);
        Assert.DoesNotContain("4242424242424242", body);   // the full number never leaves Charge()
    }

    [Fact]
    public void Other_cards_are_declined()
    {
        var gw = new FakeVisaGateway("secret");
        var (body, sig) = gw.Charge("FAKE-1", "4000 0000 0000 0002");

        var ev = gw.VerifyWebhook(body, sig);

        Assert.NotNull(ev);
        Assert.False(ev!.Succeeded);
        Assert.Null(ev.CardLast4);
    }

    [Fact]
    public void Tampered_body_is_rejected()
    {
        var gw = new FakeVisaGateway("secret");
        var (body, sig) = gw.Charge("FAKE-1", "4000 0000 0000 0002");
        var forged = body.Replace("false", "true");

        Assert.Null(gw.VerifyWebhook(forged, sig));
    }

    [Fact]
    public void Wrong_missing_or_non_hex_signature_is_rejected()
    {
        var gw = new FakeVisaGateway("secret");
        var other = new FakeVisaGateway("different-secret");
        var (body, sig) = gw.Charge("FAKE-1", "4242424242424242");

        Assert.Null(other.VerifyWebhook(body, sig));
        Assert.Null(gw.VerifyWebhook(body, null));
        Assert.Null(gw.VerifyWebhook(body, "not-hex"));
        Assert.Null(gw.VerifyWebhook(string.Empty, sig));
    }

    [Fact]
    public async Task Checkout_returns_a_fresh_reference_each_time()
    {
        var gw = new FakeVisaGateway("secret");
        var a = await gw.CreateCheckoutAsync(PendingOnline(), "/return");
        var b = await gw.CreateCheckoutAsync(PendingOnline(), "/return");

        Assert.NotEqual(a.Reference, b.Reference);
        Assert.Contains(a.Reference, a.RedirectUrl);
    }
}
