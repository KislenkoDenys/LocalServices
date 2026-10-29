using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LocalServices.Models;
using Moq;
using Xunit;

namespace LocalServices.Tests.Controllers;

public class FacilitiesControllerTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;

    public FacilitiesControllerTests()
    {
        _mockEnvironment = new Mock<IWebHostEnvironment>();
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithListOfFacilities()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Кафе" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        context.Facilities.AddRange(
            new Facility { Name = "Кав'ярня 1", City = "Київ", CategoryId = cat.Id },
            new Facility { Name = "Кав'ярня 2", City = "Львів", CategoryId = cat.Id }
        );
        await context.SaveChangesAsync();

        var controller = new FacilitiesController(context, _mockEnvironment.Object);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Facility>>(viewResult.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenIdIsNull()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new FacilitiesController(context, _mockEnvironment.Object);

        var result = await controller.Details(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenFacilityNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new FacilitiesController(context, _mockEnvironment.Object);

        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsViewWithFacility_WhenFound()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Ресторани" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var facility = new Facility { Name = "Піцерія", City = "Одеса", CategoryId = cat.Id };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        var controller = new FacilitiesController(context, _mockEnvironment.Object);

        var result = await controller.Details(facility.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Facility>(viewResult.Model);
        Assert.Equal("Піцерія", model.Name);
    }

    [Fact]
    public void Create_Get_ReturnsViewResult_WithFacilityModel()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new FacilitiesController(context, _mockEnvironment.Object);

        var result = controller.Create(5);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Facility>(viewResult.Model);
        Assert.Equal(5, model.CategoryId);
    }

    [Fact]
    public async Task Create_Post_ValidModel_AddsFacilityAndRedirectsToIndex()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Готелі" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var controller = new FacilitiesController(context, _mockEnvironment.Object);
        var facility = new Facility
        {
            Name = "Прем'єр Готель",
            City = "Київ",
            Street = "Хрещатик, 1",
            CategoryId = cat.Id
        };

        var result = await controller.Create(facility);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Single(context.Facilities);
        Assert.Equal("Прем'єр Готель", context.Facilities.First().Name);
    }

    [Fact]
    public async Task Create_Post_WithImageFile_EncodesBase64DataUri()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Спорт" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var mockFile = new Mock<IFormFile>();
        var content = "dummy image data"u8.ToArray();
        var ms = new MemoryStream(content);
        mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((stream, _) => ms.CopyToAsync(stream));
        mockFile.Setup(f => f.ContentType).Returns("image/png");
        mockFile.Setup(f => f.Length).Returns(content.Length);

        var controller = new FacilitiesController(context, _mockEnvironment.Object);
        var facility = new Facility
        {
            Name = "Фітнес Клуб",
            City = "Дніпро",
            CategoryId = cat.Id,
            ImageFile = mockFile.Object
        };

        var result = await controller.Create(facility);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        var saved = context.Facilities.First();
        Assert.NotNull(saved.ImagePath);
        Assert.StartsWith("data:image/png;base64,", saved.ImagePath);
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_WhenIdIsNull()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new FacilitiesController(context, _mockEnvironment.Object);

        var result = await controller.Edit(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ReturnsViewWithFacility_WhenFound()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Кафе" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var facility = new Facility { Name = "Бістро", CategoryId = cat.Id };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        var controller = new FacilitiesController(context, _mockEnvironment.Object);

        var result = await controller.Edit(facility.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Facility>(viewResult.Model);
        Assert.Equal("Бістро", model.Name);
    }

    [Fact]
    public async Task Edit_Post_ReturnsNotFound_WhenIdMismatch()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new FacilitiesController(context, _mockEnvironment.Object);
        var facility = new Facility { Id = 1, Name = "Заклад" };

        var result = await controller.Edit(2, facility);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesFacility_AndRedirectsToIndex()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Кафе" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var facility = new Facility { Name = "Видалити Заклад", CategoryId = cat.Id };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        var controller = new FacilitiesController(context, _mockEnvironment.Object);

        var result = await controller.DeleteConfirmed(facility.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Empty(context.Facilities);
    }
}
