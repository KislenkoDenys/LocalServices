using Microsoft.AspNetCore.Mvc;
using LocalServices.Models;
using Xunit;

namespace LocalServices.Tests.Api;

public class FacilitiesApiControllerTests
{
    [Fact]
    public async Task GetFacilities_ReturnsAllFacilities()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        context.Facilities.AddRange(
            new Facility { Name = "Заклад 1", CategoryId = cat.Id },
            new Facility { Name = "Заклад 2", CategoryId = cat.Id }
        );
        await context.SaveChangesAsync();

        var controller = new FacilitiesApiController(context);

        var result = await controller.GetFacilities();

        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public async Task GetFacility_ReturnsNotFound_WhenDoesNotExist()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new FacilitiesApiController(context);

        var result = await controller.GetFacility(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetFacility_ReturnsFacility_WhenFound()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var facility = new Facility { Name = "Ресторан Маяк", City = "Київ", CategoryId = cat.Id };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        var controller = new FacilitiesApiController(context);

        var result = await controller.GetFacility(facility.Id);

        Assert.NotNull(result.Value);
        Assert.Equal("Ресторан Маяк", result.Value.Name);
    }

    [Fact]
    public async Task PostFacility_CreatesFacilityAndReturnsCreatedAtAction()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var controller = new FacilitiesApiController(context);
        var newFacility = new Facility { Name = "Новий заклад", CategoryId = cat.Id };

        var result = await controller.PostFacility(newFacility);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var created = Assert.IsType<Facility>(createdResult.Value);
        Assert.Equal("Новий заклад", created.Name);
        Assert.Single(context.Facilities);
    }

    [Fact]
    public async Task PutFacility_ReturnsBadRequest_WhenIdMismatch()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new FacilitiesApiController(context);
        var facility = new Facility { Id = 1, Name = "Тест" };

        var result = await controller.PutFacility(2, facility);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PutFacility_UpdatesFacilityAndReturnsNoContent()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var facility = new Facility { Name = "Стара назва", CategoryId = cat.Id };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        var controller = new FacilitiesApiController(context);
        facility.Name = "Оновлена назва";

        var result = await controller.PutFacility(facility.Id, facility);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Оновлена назва", context.Facilities.Find(facility.Id)!.Name);
    }

    [Fact]
    public async Task DeleteFacility_ReturnsNotFound_WhenDoesNotExist()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new FacilitiesApiController(context);

        var result = await controller.DeleteFacility(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteFacility_RemovesFacilityAndReturnsNoContent()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        var facility = new Facility { Name = "Заклад для видалення", CategoryId = cat.Id };
        context.Facilities.Add(facility);
        await context.SaveChangesAsync();

        var controller = new FacilitiesApiController(context);

        var result = await controller.DeleteFacility(facility.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.Facilities);
    }
}
