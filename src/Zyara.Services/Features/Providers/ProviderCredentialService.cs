using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Services.Data;
using Zyara.Services.Features.Providers.Dtos;
using Zyara.Services.Features.Providers.Interfaces;

namespace Zyara.Services.Features.Providers
{
    public class ProviderCredentialService : IProviderCredentialService
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long MaxSize = 5 * 1024 * 1024; // 5 MB

        private readonly ZyaraDbContext DB;
        private readonly IWebHostEnvironment _env;
        private readonly IProviderVerficationService _verification;

        public ProviderCredentialService(
            ZyaraDbContext DB,
            IWebHostEnvironment env,
            IProviderVerficationService verification)
        {
            this.DB = DB;
            _env = env;
            _verification = verification;
        }

        // Outside wwwroot => no public URL for these files
        private string UploadFolder =>
            Path.Combine(_env.ContentRootPath, "PrivateUploads", "credentials");

        public async Task<List<ProviderCredentialDto>> GetMyCredentialsAsync(int providerId)
        {
            return await DB.ProviderCredentials
                .AsNoTracking()
                .Where(c => c.ProviderID == providerId)
                .OrderByDescending(c => c.Id)
                .Select(c => new ProviderCredentialDto
                {
                    Id = c.Id,
                    DocumentType = c.DocumentType,
                    FileExtension = Path.GetExtension(c.FileUrl)
                })
                .ToListAsync();
        }

        public async Task<CredentialResultDto> UploadAsync(int providerId, UploadCredentialDto dto)
        {
            var providerExists = await DB.Providers.AnyAsync(p => p.Id == providerId);
            if (!providerExists)
                return CredentialResultDto.Fail("Provider not found.");

            var file = dto.File;
            if (file == null || file.Length == 0)
                return CredentialResultDto.Fail("Please choose a file.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return CredentialResultDto.Fail("Only PDF, JPG, or PNG files are allowed.");

            if (file.Length > MaxSize)
                return CredentialResultDto.Fail("File must be 5 MB or less.");

            Directory.CreateDirectory(UploadFolder);
            var storedName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(UploadFolder, storedName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            DB.ProviderCredentials.Add(new ProviderCredential
            {
                ProviderID = providerId,
                DocumentType = dto.DocumentType.Trim(),
                FileUrl = storedName          // only the stored name, not a public URL
            });

            await DB.SaveChangesAsync();

            // Changing credentials => needs a new review
            await _verification.ResetToPendingAsync(providerId);

            return CredentialResultDto.Ok();
        }

        public async Task<CredentialFileDto?> GetFileAsync(int credentialId, int? providerId, bool isAdmin)
        {
            var credential = await DB.ProviderCredentials
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == credentialId);

            if (credential == null) return null;

            var isOwner = providerId != null && credential.ProviderID == providerId;
            if (!isOwner && !isAdmin)
                return new CredentialFileDto { IsAllowed = false };

            var fullPath = Path.Combine(UploadFolder, credential.FileUrl);
            if (!File.Exists(fullPath)) return null;

            var ext = Path.GetExtension(credential.FileUrl).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                _ => "image/jpeg"
            };

            return new CredentialFileDto
            {
                IsAllowed = true,
                FullPath = fullPath,
                ContentType = contentType,
                FileName = $"{credential.DocumentType}{ext}"
            };
        }
    }
}