namespace Zyara.Services.Features.Payments.Dtos
{
    public class GatewayResultDto
    {
        public bool IsSuccessful { get; set; }
        public string GatewayReference { get; set; } = string.Empty;
        public string? CardBrand { get; set; }
        public string? CardLast4 { get; set; }
        public string? ErrorMessage { get; set; }
    }
}