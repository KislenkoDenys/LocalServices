using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using LocalServices.Controllers;
using LocalServices.Data;
using LocalServices.Models;
using LocalServices.Services;
using Xunit;

namespace LocalServices.Tests.Services;

public class PdfExportServiceTests
{
    [Fact]
    public void GenerateFacilityPriceListPdf_ReturnsValidPdfBytes_WhenFacilityHasServices()
    {
        var service = new PdfExportService();
        var facility = new Facility
        {
            Id = 1,
            Name = "Тестовий Заклад",
            City = "Київ",
            Street = "вул. Хрещатик, 1",
            Phone = "+380441234567",
            WorkingHours = "Пн-Нд 09:00-21:00",
            Category = new Category { Id = 1, Name = "Кафе" },
            Services = new List<Service>
            {
                new() { Id = 1, Name = "Капучино", Price = 65.00m },
                new() { Id = 2, Name = "Круасан", Price = 55.00m }
            }
        };

        var pdfBytes = service.GenerateFacilityPriceListPdf(facility);

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 100);
        var header = Encoding.ASCII.GetString(pdfBytes[..4]);
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public void GenerateFacilityPriceListPdf_HandlesFacilityWithoutServices()
    {
        var service = new PdfExportService();
        var facility = new Facility
        {
            Id = 2,
            Name = "Заклад без послуг",
            City = "Львів",
            Category = new Category { Id = 2, Name = "Готелі" },
            Services = new List<Service>()
        };

        var pdfBytes = service.GenerateFacilityPriceListPdf(facility);

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 100);
        var header = Encoding.ASCII.GetString(pdfBytes[..4]);
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public async Task ExportPdf_ReturnsFileResult_WhenFacilityExists()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var category = new Category { Id = 1, Name = "Ресторани" };
        var facility = new Facility
        {
            Id = 10,
            Name = "Кафе Центр",
            Category = category,
            CategoryId = 1,
            Services = new List<Service>
            {
                new() { Id = 1, Name = "Латте", Price = 70.00m, FacilityId = 10 }
            }
        };
        context.Categories.Add(category);
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        var mockEnv = new Mock<IWebHostEnvironment>();
        var controller = new FacilitiesController(context, mockEnv.Object);

        var result = await controller.ExportPdf(10);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Contains("PriceList", fileResult.FileDownloadName);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(fileResult.FileContents[..4]));
    }

    [Fact]
    public async Task ExportPdf_ReturnsNotFound_WhenFacilityDoesNotExist()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var mockEnv = new Mock<IWebHostEnvironment>();
        var controller = new FacilitiesController(context, mockEnv.Object);

        var result = await controller.ExportPdf(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ExportPdf_ReturnsNotFound_WhenIdIsNull()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var mockEnv = new Mock<IWebHostEnvironment>();
        var controller = new FacilitiesController(context, mockEnv.Object);

        var result = await controller.ExportPdf(null);

        Assert.IsType<NotFoundResult>(result);
    }
}

