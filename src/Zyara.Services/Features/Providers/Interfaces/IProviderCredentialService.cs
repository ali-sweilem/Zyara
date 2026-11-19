
using Zyara.Domain.Entities;
using Zyara.Services.Features.Providers.Dtos;
 
namespace Zyara.Services.Features.Providers.Interfaces
{
    public interface IProviderCredentialService
    {
        Task<ProviderCredential?> GetCredentialAsync(int id);
        Task<List<ProviderCredentialDto>> GetMyCredentialsAsync(int providerId);
        Task<CredentialResultDto> UploadAsync(int providerId, UploadCredentialDto dto);
        Task<CredentialFileDto?> GetFileAsync(int credentialId);
    }
}
