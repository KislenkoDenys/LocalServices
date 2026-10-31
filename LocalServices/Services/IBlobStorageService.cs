using Microsoft.AspNetCore.Http;

namespace LocalServices.Services;

public interface IBlobStorageService
{
    Task<string?> UploadImageAsync(IFormFile file, string folder = "facilities");
    Task<string?> UploadImageAsync(Stream stream, string fileName, string folder = "facilities");
    Task<bool> DeleteImageAsync(string imageUrlOrPublicId);
    bool IsConfigured { get; }
}

