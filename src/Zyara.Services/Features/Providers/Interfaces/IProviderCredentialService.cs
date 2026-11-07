
using Zyara.Services.Features.Providers.Dtos;
 
namespace Zyara.Services.Features.Providers.Interfaces
{
    public interface IProviderCredentialService
    {
        Task<List<ProviderCredentialDto>> GetMyCredentialsAsync(int providerId);
        Task<CredentialResultDto> UploadAsync(int providerId, UploadCredentialDto dto);

        // null = not found | IsAllowed=false => Forbid
        Task<CredentialFileDto?> GetFileAsync(int credentialId, int? providerId, bool isAdmin);
    }
}