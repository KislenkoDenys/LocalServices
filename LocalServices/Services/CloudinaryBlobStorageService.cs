using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LocalServices.Services;

public class CloudinaryBlobStorageService : IBlobStorageService
{
    private readonly Cloudinary? _cloudinary;
    private readonly ILogger<CloudinaryBlobStorageService>? _logger;
    public bool IsConfigured => _cloudinary != null;

    public CloudinaryBlobStorageService(IConfiguration configuration, ILogger<CloudinaryBlobStorageService>? logger = null)
    {
        _logger = logger;

        var cloudinaryUrl = configuration["CLOUDINARY_URL"] ?? configuration["Cloudinary:Url"];
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (!string.IsNullOrWhiteSpace(cloudinaryUrl) && cloudinaryUrl.StartsWith("cloudinary://", StringComparison.OrdinalIgnoreCase))
        {
            _cloudinary = new Cloudinary(cloudinaryUrl);
            _cloudinary.Api.Secure = true;
        }
        else if (!string.IsNullOrWhiteSpace(cloudName) &&
                 !string.IsNullOrWhiteSpace(apiKey) &&
                 !string.IsNullOrWhiteSpace(apiSecret) &&
                 cloudName != "your-cloud-name" &&
                 apiKey != "your-api-key")
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }
    }

    public async Task<string?> UploadImageAsync(IFormFile file, string folder = "facilities")
    {
        if (file == null || file.Length == 0) return null;

        if (_cloudinary == null)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType;
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = $"localservices/{folder}",
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger?.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                return null;
            }

            return uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception while uploading to Cloudinary");
            return null;
        }
    }

    public async Task<string?> UploadImageAsync(Stream stream, string fileName, string folder = "facilities")
    {
        if (stream == null || stream.Length == 0) return null;

        if (_cloudinary == null)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        }

        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = $"localservices/{folder}",
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger?.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                return null;
            }

            return uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Exception while uploading to Cloudinary");
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(string imageUrlOrPublicId)
    {
        if (string.IsNullOrWhiteSpace(imageUrlOrPublicId) || _cloudinary == null)
        {
            return false;
        }

        try
        {
            var publicId = ExtractPublicId(imageUrlOrPublicId);
            if (string.IsNullOrWhiteSpace(publicId)) return false;

            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            return result.Result == "ok";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to delete image from Cloudinary: {Id}", imageUrlOrPublicId);
            return false;
        }
    }

    private static string? ExtractPublicId(string urlOrId)
    {
        if (!urlOrId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !urlOrId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return urlOrId;
        }

        try
        {
            var uri = new Uri(urlOrId);
            var path = uri.AbsolutePath;
            var uploadIdx = path.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
            if (uploadIdx == -1) return null;

            var sub = path[(uploadIdx + 8)..];
            if (sub.StartsWith('v') && sub.IndexOf('/') is int vSlash && vSlash > 0 && long.TryParse(sub[1..vSlash], out _))
            {
                sub = sub[(vSlash + 1)..];
            }

            var dotIdx = sub.LastIndexOf('.');
            return dotIdx > 0 ? sub[..dotIdx] : sub;
        }
        catch
        {
            return null;
        }
    }
}

