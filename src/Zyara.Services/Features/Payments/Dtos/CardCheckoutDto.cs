namespace Zyara.Services.Features.Payments.Dtos
{
    public class CardCheckoutDto
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string GatewayReference { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public bool IsRedirect { get; set; } = true;
    }
}