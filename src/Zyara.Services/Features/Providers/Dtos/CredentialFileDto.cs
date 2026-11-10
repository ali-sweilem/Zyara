namespace Zyara.Services.Features.Providers.Dtos
{
    public class CredentialFileDto
    {
        public bool IsAllowed { get; set; }      // false => Forbid
        public string FullPath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}