using Microsoft.AspNetCore.Mvc;
using LocalServices.Models;
using Xunit;

namespace LocalServices.Tests.Api;

public class FacilitiesApiControllerTests
{
    [Fact]
    public async Task GetFacilities_ReturnsPagedResult_WithDefaults()
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

        var actionResult = await controller.GetFacilities();
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var pagedResult = Assert.IsType<PagedResult<Facility>>(okResult.Value);

        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count());
        Assert.Equal(0, pagedResult.Skip);
        Assert.Equal(10, pagedResult.Limit);
        Assert.False(pagedResult.HasNextPage);
        Assert.Null(pagedResult.NextLink);
    }

    [Fact]
    public async Task GetFacilities_SupportsPagination_WithNextAndPreviousLinks()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        for (int i = 1; i <= 25; i++)
        {
            context.Facilities.Add(new Facility { Name = $"Заклад {i}", CategoryId = cat.Id });
        }
        await context.SaveChangesAsync();

        var controller = new FacilitiesApiController(context);

        // Сторінка 1
        var page1Result = await controller.GetFacilities(skip: 0, limit: 10);
        var page1 = Assert.IsType<PagedResult<Facility>>(((OkObjectResult)page1Result.Result!).Value);

        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count());
        Assert.True(page1.HasNextPage);
        Assert.False(page1.HasPreviousPage);
        Assert.NotNull(page1.NextLink);
        Assert.Contains("skip=10&limit=10", page1.NextLink);
        Assert.Null(page1.PreviousLink);

        // Сторінка 2
        var page2Result = await controller.GetFacilities(skip: 10, limit: 10);
        var page2 = Assert.IsType<PagedResult<Facility>>(((OkObjectResult)page2Result.Result!).Value);

        Assert.Equal(10, page2.Items.Count());
        Assert.True(page2.HasNextPage);
        Assert.True(page2.HasPreviousPage);
        Assert.Contains("skip=20&limit=10", page2.NextLink!);
        Assert.Contains("skip=0&limit=10", page2.PreviousLink!);
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

    [Fact]
    public async Task Search_ReturnsEmpty_WhenTermHasLessThanThreeCharacters()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new FacilitiesApiController(context);

        var actionResult = await controller.Search(term: "ма", page: 1, pageSize: 10);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);

        using var jsonDoc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(okResult.Value));
        var results = jsonDoc.RootElement.GetProperty("results");

        Assert.Equal(0, results.GetArrayLength());
    }

    [Fact]
    public async Task Search_ReturnsFilteredFacilities_WhenTermMatches()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Ресторани" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        context.Facilities.AddRange(
            new Facility { Name = "Макдональдс", City = "Київ", CategoryId = cat.Id },
            new Facility { Name = "Сушія", City = "Львів", CategoryId = cat.Id },
            new Facility { Name = "МакКафе", City = "Одеса", CategoryId = cat.Id }
        );
        await context.SaveChangesAsync();

        var controller = new FacilitiesApiController(context);

        var actionResult = await controller.Search(term: "мак", page: 1, pageSize: 10);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);

        using var jsonDoc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(okResult.Value));
        var results = jsonDoc.RootElement.GetProperty("results");

        Assert.Equal(2, results.GetArrayLength());
    }

    [Fact]
    public async Task Search_SupportsPagination_WithMoreIndicator()
    {
        using var context = TestDbContextFactory.Create();
        var cat = new Category { Name = "Категорія" };
        context.Categories.Add(cat);
        await context.SaveChangesAsync();

        for (int i = 1; i <= 15; i++)
        {
            context.Facilities.Add(new Facility { Name = $"Заклад {i:D2}", CategoryId = cat.Id });
        }
        await context.SaveChangesAsync();

        var controller = new FacilitiesApiController(context);

        var actionResult = await controller.Search(term: "Зак", page: 1, pageSize: 10);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);

        using var jsonDoc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(okResult.Value));
        var results = jsonDoc.RootElement.GetProperty("results");
        var pagination = jsonDoc.RootElement.GetProperty("pagination");

        Assert.Equal(10, results.GetArrayLength());
        Assert.True(pagination.GetProperty("more").GetBoolean());
    }
}
