using Microsoft.AspNetCore.Mvc;
using LocalServices.Models;
using Xunit;

namespace LocalServices.Tests.Controllers;

public class ServicesControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewResult_WithListOfServices()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var facility = new Facility { Name = "Автосервіс", CategoryId = cat.Id };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        context.Services.AddRange(
            new Service { Name = "Заміна мастила", Price = 500, FacilityId = facility.Id },
            new Service { Name = "Шиномонтаж", Price = 800, FacilityId = facility.Id }
        );
        await context.SaveChangesAsync();

        var controller = new ServicesController(context);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Service>>(viewResult.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenIdIsNull()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new ServicesController(context);

        var result = await controller.Details(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenServiceNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new ServicesController(context);

        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsViewWithService_WhenFound()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var facility = new Facility { Name = "Перукарня", CategoryId = cat.Id };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        var service = new Service { Name = "Стрижка", Price = 350, FacilityId = facility.Id };
        context.Services.Add(service);
        await context.SaveChangesAsync();

        var controller = new ServicesController(context);

        var result = await controller.Details(service.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Service>(viewResult.Model);
        Assert.Equal("Стрижка", model.Name);
        Assert.Equal(350, model.Price);
    }

    [Fact]
    public void Create_Get_ReturnsViewResult()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new ServicesController(context);

        var result = controller.Create();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_Post_ValidModel_AddsServiceAndRedirectsToIndex()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var facility = new Facility { Name = "Стоматологія", CategoryId = cat.Id };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        var controller = new ServicesController(context);
        var service = new Service { Name = "Чистка зубів", Price = 1200, FacilityId = facility.Id };

        var result = await controller.Create(service);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Single(context.Services);
        Assert.Equal("Чистка зубів", context.Services.First().Name);
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_WhenIdIsNull()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new ServicesController(context);

        var result = await controller.Edit(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ReturnsNotFound_WhenIdMismatch()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new ServicesController(context);
        var service = new Service { Id = 1, Name = "Тест" };

        var result = await controller.Edit(2, service);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesService_AndRedirectsToIndex()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var facility = new Facility { Name = "Заклад", CategoryId = cat.Id };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        var service = new Service { Name = "Послуга", Price = 100, FacilityId = facility.Id };
        context.Services.Add(service);
        await context.SaveChangesAsync();

        var controller = new ServicesController(context);

        var result = await controller.DeleteConfirmed(service.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Empty(context.Services);
    }
}
