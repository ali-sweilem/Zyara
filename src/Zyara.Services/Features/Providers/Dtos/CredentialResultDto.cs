namespace Zyara.Services.Features.Providers.Dtos
{
    public class CredentialResultDto
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        public static CredentialResultDto Ok() => new() { Success = true };
        public static CredentialResultDto Fail(string error) => new() { Success = false, Error = error };
    }
}