using Microsoft.AspNetCore.Mvc;
using LocalServices.Models;
using Xunit;

namespace LocalServices.Tests.Controllers;

public class CategoriesControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewResult_WithListOfCategories()
    {
        using var context = TestDbContextFactory.Create();
        context.Categories.AddRange(
            new Category { Name = "Кафе", Description = "Заклади харчування" },
            new Category { Name = "Спорт", Description = "Спортивні зали" }
        );
        await context.SaveChangesAsync();

        var controller = new CategoriesController(context);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Category>>(viewResult.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenIdIsNull()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesController(context);

        var result = await controller.Details(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenCategoryDoesNotExist()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesController(context);

        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsViewWithCategory_WhenFound()
    {
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Готелі", Description = "Проживання" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var controller = new CategoriesController(context);

        var result = await controller.Details(category.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Category>(viewResult.Model);
        Assert.Equal("Готелі", model.Name);
    }

    [Fact]
    public void Create_Get_ReturnsViewResult()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesController(context);

        var result = controller.Create();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_Post_ValidModel_AddsCategoryAndRedirectsToIndex()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesController(context);
        var category = new Category { Name = "Нова Категорія", Description = "Тестовий опис" };

        var result = await controller.Create(category);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Single(context.Categories);
        Assert.Equal("Нова Категорія", context.Categories.First().Name);
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsViewWithModel()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesController(context);
        controller.ModelState.AddModelError("Name", "Обов'язкове поле");
        var category = new Category { Name = "" };

        var result = await controller.Create(category);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(category, viewResult.Model);
        Assert.Empty(context.Categories);
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_WhenIdIsNull()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesController(context);

        var result = await controller.Edit(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ReturnsViewWithCategory_WhenFound()
    {
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Краса", Description = "Салони" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var controller = new CategoriesController(context);

        var result = await controller.Edit(category.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Category>(viewResult.Model);
        Assert.Equal("Краса", model.Name);
    }

    [Fact]
    public async Task Edit_Post_ReturnsNotFound_WhenIdMismatch()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesController(context);
        var category = new Category { Id = 1, Name = "Оновлена" };

        var result = await controller.Edit(2, category);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ValidModel_UpdatesCategoryAndRedirectsToIndex()
    {
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Стара назва", Description = "Опис" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var controller = new CategoriesController(context);
        category.Name = "Нова назва";

        var result = await controller.Edit(category.Id, category);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Нова назва", context.Categories.Find(category.Id)!.Name);
    }

    [Fact]
    public async Task Delete_Get_ReturnsNotFound_WhenIdIsNull()
    {
        using var context = TestDbContextFactory.Create();
        var controller = new CategoriesController(context);

        var result = await controller.Delete(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Get_ReturnsViewWithCategory_WhenFound()
    {
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Для видалення" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var controller = new CategoriesController(context);

        var result = await controller.Delete(category.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Category>(viewResult.Model);
        Assert.Equal("Для видалення", model.Name);
    }

    [Fact]
    public async Task DeleteConfirmed_RemovesCategory_AndRedirectsToIndex()
    {
        using var context = TestDbContextFactory.Create();
        var category = new Category { Name = "Видалити мене" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var controller = new CategoriesController(context);

        var result = await controller.DeleteConfirmed(category.Id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Empty(context.Categories);
    }
}
