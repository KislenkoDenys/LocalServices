using Microsoft.AspNetCore.Mvc;
using LocalServices.Models;
using Xunit;

namespace LocalServices.Tests.Api;

public class CategoriesApiControllerTests
{
    [Fact]
    public async Task GetCategories_ReturnsAllCategories()
    {
        using var context = TestDbContextFactory.Create();
        context.Categories.AddRange(
            new Category { Name = "Категорія 1" },
            new Category { Name = "Категорія 2" }
        );
        await context.SaveChangesAsync();

        var controller = new CategoriesApiController(context);

        var result = await controller.GetCategories();

        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count());
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
}
