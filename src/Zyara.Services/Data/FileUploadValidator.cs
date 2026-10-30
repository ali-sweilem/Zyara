using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Zyara.Services.Data;

public interface IFileUploadValidator
{
    bool IsAllowedExtension(string fileName);
    bool IsAllowedContentType(string contentType);
    bool IsWithinSizeLimit(long length);
    bool IsSafeFileName(string fileName);
}

public class FileUploadValidator : IFileUploadValidator
{
    private readonly string[] _allowedExt = { ".pdf", ".jpg", ".jpeg", ".png" };
    private readonly string[] _allowedContent = { "application/pdf", "image/jpeg", "image/png" };
    private readonly long _maxBytes = 5L * 1024 * 1024; // 5 MB

    public bool IsAllowedExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return _allowedExt.Contains(ext);
    }

    public bool IsAllowedContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        return _allowedContent.Contains(contentType.ToLowerInvariant());
    }

    public bool IsWithinSizeLimit(long length) => length > 0 && length <= _maxBytes;

    public bool IsSafeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        // Reject path traversal and original name used as path
        if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\")) return false;
        return true;
    }
}
