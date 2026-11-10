namespace Zyara.Services.Features.Providers.Dtos
{
    public class ProviderCredentialDto
    {
        public int Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
    }
}