using Microsoft.AspNetCore.Mvc;
using LocalServices.Models;
using Xunit;

namespace LocalServices.Tests.Api;

public class CategoriesApiControllerTests
{
    [Fact]
    public async Task GetCategories_ReturnsPagedResult_WithDefaults()
    {
        using var context = TestDbContextFactory.Create();
        context.Categories.AddRange(
            new Category { Name = "Категорія 1" },
            new Category { Name = "Категорія 2" }
        );
        await context.SaveChangesAsync();

        var controller = new CategoriesApiController(context);

        var actionResult = await controller.GetCategories();
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var pagedResult = Assert.IsType<PagedResult<Category>>(okResult.Value);

        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(2, pagedResult.Items.Count());
        Assert.Equal(0, pagedResult.Skip);
        Assert.Equal(10, pagedResult.Limit);
    }

    [Fact]
    public async Task GetCategories_SupportsPagination_AndNextLink()
    {
        using var context = TestDbContextFactory.Create();
        for (int i = 1; i <= 15; i++)
        {
            context.Categories.Add(new Category { Name = $"Категорія {i}" });
        }
        await context.SaveChangesAsync();

        var controller = new CategoriesApiController(context);

        var actionResult = await controller.GetCategories(skip: 0, limit: 5);
        var pagedResult = Assert.IsType<PagedResult<Category>>(((OkObjectResult)actionResult.Result!).Value);

        Assert.Equal(15, pagedResult.TotalCount);
        Assert.Equal(5, pagedResult.Items.Count());
        Assert.Equal(3, pagedResult.TotalPages);
        Assert.True(pagedResult.HasNextPage);
        Assert.NotNull(pagedResult.NextLink);
        Assert.Contains("skip=5&limit=5", pagedResult.NextLink);
    }

    [Fact]
    public async Task GetCategory_ReturnsNotFound_WhenCategoryDoesNotExist()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesApiController(context);

        var result = await controller.GetCategory(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetCategory_ReturnsCategory_WhenFound()
    {
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Медицина", Description = "Клініки" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var controller = new CategoriesApiController(context);

        var result = await controller.GetCategory(category.Id);

        Assert.NotNull(result.Value);
        Assert.Equal("Медицина", result.Value.Name);
    }

    [Fact]
    public async Task PostCategory_CreatesCategoryAndReturnsCreatedAtAction()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesApiController(context);
        var newCat = new Category { Name = "Освіта", Description = "Курси та школи" };

        var result = await controller.PostCategory(newCat);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var createdCategory = Assert.IsType<Category>(createdResult.Value);
        Assert.Equal("Освіта", createdCategory.Name);
        Assert.Single(context.Categories);
    }

    [Fact]
    public async Task PutCategory_ReturnsBadRequest_WhenIdMismatch()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesApiController(context);
        var category = new Category { Id = 1, Name = "Тест" };

        var result = await controller.PutCategory(2, category);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PutCategory_UpdatesCategoryAndReturnsNoContent()
    {
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Початкова", Description = "Опис" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var controller = new CategoriesApiController(context);
        category.Name = "Оновлена назва";

        var result = await controller.PutCategory(category.Id, category);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Оновлена назва", context.Categories.Find(category.Id)!.Name);
    }

    [Fact]
    public async Task DeleteCategory_ReturnsNotFound_WhenDoesNotExist()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesApiController(context);

        var result = await controller.DeleteCategory(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteCategory_RemovesCategoryAndReturnsNoContent()
    {
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Видалити API" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var controller = new CategoriesApiController(context);

        var result = await controller.DeleteCategory(category.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.Categories);
    }

    [Fact]
    public async Task Search_ReturnsFilteredCategories_WhenTermMatches()
    {
        using var context = TestDbContextFactory.Create();
        context.Categories.AddRange(
            new Category { Name = "Кафе і ресторани" },
            new Category { Name = "Автосервіси" },
            new Category { Name = "Кав'ярні" }
        );
        await context.SaveChangesAsync();

        var controller = new CategoriesApiController(context);

        var actionResult = await controller.Search(term: "кав", page: 1, pageSize: 10);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);

        using var jsonDoc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(okResult.Value));
        var results = jsonDoc.RootElement.GetProperty("results");

        Assert.Equal(1, results.GetArrayLength());
        Assert.Equal("Кав'ярні", results[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task Search_ReturnsEmpty_WhenTermHasLessThanThreeCharacters()
    {
        using var context = TestDbContextFactory.Create();
        context.Categories.Add(new Category { Name = "Кафе" });
        await context.SaveChangesAsync();

        var controller = new CategoriesApiController(context);

        var actionResult = await controller.Search(term: "ка", page: 1, pageSize: 10);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);

        using var jsonDoc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(okResult.Value));
        var results = jsonDoc.RootElement.GetProperty("results");

        Assert.Equal(0, results.GetArrayLength());
    }

    [Fact]
    public async Task Search_SupportsPagination_WithMoreIndicator()
    {
        using var context = TestDbContextFactory.Create();
        for (int i = 1; i <= 15; i++)
        {
            context.Categories.Add(new Category { Name = $"Категорія {i:D2}" });
        }
        await context.SaveChangesAsync();

        var controller = new CategoriesApiController(context);

        var actionResult = await controller.Search(term: "Кат", page: 1, pageSize: 10);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);

        using var jsonDoc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(okResult.Value));
        var results = jsonDoc.RootElement.GetProperty("results");
        var pagination = jsonDoc.RootElement.GetProperty("pagination");

        Assert.Equal(10, results.GetArrayLength());
        Assert.True(pagination.GetProperty("more").GetBoolean());
    }
}
