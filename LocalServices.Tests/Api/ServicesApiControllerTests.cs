using Microsoft.AspNetCore.Mvc;
using LocalServices.Models;
using Xunit;

namespace LocalServices.Tests.Api;

public class ServicesApiControllerTests
{
    [Fact]
    public async Task GetServices_ReturnsPagedResult_WithDefaults()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var fac = new Facility { Name = "Заклад", CategoryId = cat.Id };
        context.Facilities.Add(fac);
        await context.SaveChangesAsync();

        context.Services.AddRange(
            new Service { Name = "Послуга 1", Price = 100, FacilityId = fac.Id },
            new Service { Name = "Послуга 2", Price = 200, FacilityId = fac.Id }
        );
        await context.SaveChangesAsync();

        var controller = new ServicesApiController(context);

        var actionResult = await controller.GetServices();
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var pagedResult = Assert.IsType<PagedResult<Service>>(okResult.Value);

        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count());
        Assert.Equal(0, pagedResult.Skip);
        Assert.Equal(10, pagedResult.Limit);
    }

    [Fact]
    public async Task GetServices_SupportsPagination_AndNextLink()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var fac = new Facility { Name = "Заклад", CategoryId = cat.Id };
        context.Facilities.Add(fac);
        await context.SaveChangesAsync();

        for (int i = 1; i <= 15; i++)
        {
            context.Services.Add(new Service { Name = $"Послуга {i}", Price = i * 10, FacilityId = fac.Id });
        }
        await context.SaveChangesAsync();

        var controller = new ServicesApiController(context);

        var actionResult = await controller.GetServices(skip: 0, limit: 5);
        var pagedResult = Assert.IsType<PagedResult<Service>>(((OkObjectResult)actionResult.Result!).Value);

        Assert.Equal(15, pagedResult.TotalCount);
        Assert.Equal(5, pagedResult.Items.Count());
        Assert.Equal(3, pagedResult.TotalPages);
        Assert.True(pagedResult.HasNextPage);
        Assert.NotNull(pagedResult.NextLink);
        Assert.Contains("skip=5&limit=5", pagedResult.NextLink);
    }

    [Fact]
    public async Task GetService_ReturnsNotFound_WhenDoesNotExist()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new ServicesApiController(context);

        var result = await controller.GetService(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetService_ReturnsService_WhenFound()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var fac = new Facility { Name = "Заклад", CategoryId = cat.Id };
        context.Facilities.Add(fac);
        await context.SaveChangesAsync();

        var service = new Service { Name = "Діагностика", Price = 450, FacilityId = fac.Id };
        context.Services.Add(service);
        await context.SaveChangesAsync();

        var controller = new ServicesApiController(context);

        var result = await controller.GetService(service.Id);

        Assert.NotNull(result.Value);
        Assert.Equal("Діагностика", result.Value.Name);
        Assert.Equal(450, result.Value.Price);
    }

    [Fact]
    public async Task PostService_CreatesServiceAndReturnsCreatedAtAction()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var fac = new Facility { Name = "Заклад", CategoryId = cat.Id };
        context.Facilities.Add(fac);
        await context.SaveChangesAsync();

        var controller = new ServicesApiController(context);
        var newService = new Service { Name = "Нова послуга", Price = 300, FacilityId = fac.Id };

        var result = await controller.PostService(newService);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var created = Assert.IsType<Service>(createdResult.Value);
        Assert.Equal("Нова послуга", created.Name);
        Assert.Equal(300, created.Price);
        Assert.Single(context.Services);
    }

    [Fact]
    public async Task PutService_ReturnsBadRequest_WhenIdMismatch()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new ServicesApiController(context);
        var service = new Service { Id = 1, Name = "Тест", Price = 100 };

        var result = await controller.PutService(2, service);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PutService_UpdatesServiceAndReturnsNoContent()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var fac = new Facility { Name = "Заклад", CategoryId = cat.Id };
        context.Facilities.Add(fac);
        await context.SaveChangesAsync();

        var service = new Service { Name = "Стара послуга", Price = 150, FacilityId = fac.Id };
        context.Services.Add(service);
        await context.SaveChangesAsync();

        var controller = new ServicesApiController(context);
        service.Name = "Оновлена послуга";
        service.Price = 250;

        var result = await controller.PutService(service.Id, service);

        Assert.IsType<NoContentResult>(result);
        var updated = context.Services.Find(service.Id)!;
        Assert.Equal("Оновлена послуга", updated.Name);
        Assert.Equal(250, updated.Price);
    }

    [Fact]
    public async Task DeleteService_ReturnsNotFound_WhenDoesNotExist()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new ServicesApiController(context);

        var result = await controller.DeleteService(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteService_RemovesServiceAndReturnsNoContent()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var fac = new Facility { Name = "Заклад", CategoryId = cat.Id };
        context.Facilities.Add(fac);
        await context.SaveChangesAsync();

        var service = new Service { Name = "Послуга на видалення", Price = 100, FacilityId = fac.Id };
        context.Services.Add(service);
        await context.SaveChangesAsync();

        var controller = new ServicesApiController(context);

        var result = await controller.DeleteService(service.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.Services);
    }
}
