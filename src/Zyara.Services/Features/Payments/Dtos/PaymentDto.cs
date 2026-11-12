namespace Zyara.Services.Features.Payments.Dtos
{
    public class PaymentDto
    {
        public int BookingId { get; set; }
        public decimal TotalPrice { get; set; }
        public string Method { get; set; } = "Cash";
    }
}