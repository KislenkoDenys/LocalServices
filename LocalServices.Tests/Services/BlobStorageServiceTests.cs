using System.Text;
using LocalServices.Controllers;
using LocalServices.Data;
using LocalServices.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LocalServices.Tests.Services;

public class BlobStorageServiceTests
{
    private static IFormFile CreateTestFormFile(string content, string fileName = "test.jpg", string contentType = "image/jpeg")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    [Fact]
    public void IsConfigured_ReturnsFalse_WhenNoCredentials()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Cloudinary:CloudName", "" },
                { "Cloudinary:ApiKey", "" },
                { "Cloudinary:ApiSecret", "" }
            })
            .Build();

        var service = new CloudinaryBlobStorageService(config);

        Assert.False(service.IsConfigured);
    }

    [Fact]
    public async Task UploadImageAsync_ReturnsNull_WhenFileIsNull()
    {
        var config = new ConfigurationBuilder().Build();
        var service = new CloudinaryBlobStorageService(config);

        var result = await service.UploadImageAsync((IFormFile)null!);

        Assert.Null(result);
    }

    [Fact]
    public async Task UploadImageAsync_ReturnsBase64Fallback_WhenCloudinaryNotConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Cloudinary:CloudName", "" }
            })
            .Build();

        var service = new CloudinaryBlobStorageService(config);
        var file = CreateTestFormFile("fake-image-content");

        var result = await service.UploadImageAsync(file);

        Assert.NotNull(result);
        Assert.StartsWith("data:image/jpeg;base64,", result);
    }

    [Fact]
    public async Task UploadImage_Endpoint_ReturnsBadRequest_WhenFileIsEmpty()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var controller = new FacilitiesApiController(context);

        var response = await controller.UploadImage(null!);

        Assert.IsType<BadRequestObjectResult>(response);
    }
}

