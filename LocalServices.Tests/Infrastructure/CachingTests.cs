using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using LocalServices.Controllers;
using LocalServices.Infrastructure;
using LocalServices.Models;
using LocalServices.Tests.Api;
using Xunit;

namespace LocalServices.Tests.Infrastructure;

public class CachingTests
{
    private static IMemoryCache CreateCache() => new MemoryCache(new MemoryCacheOptions());

    [Fact]
    public async Task CategoriesApi_GetCategories_UsesCacheAndInvalidatesOnPost()
    {
        using var context = TestDbContextFactory.Create();
        var cache = CreateCache();

        context.Categories.Add(new Category { Name = "Кафе" });
        await context.SaveChangesAsync();

        var controller = new CategoriesApiController(context, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        // 1. Перший запит: CACHE MISS
        var result1 = await controller.GetCategories(0, 10);
        var ok1 = Assert.IsType<OkObjectResult>(result1.Result);
        var paged1 = Assert.IsType<PagedResult<Category>>(ok1.Value);
        Assert.Single(paged1.Items);
        Assert.Equal("MISS", controller.Response.Headers["X-Cache"]);

        // 2. Додаємо категорію прямо в БД (в обхід API)
        context.Categories.Add(new Category { Name = "Ресторани" });
        await context.SaveChangesAsync();

        // 3. Другий запит: CACHE HIT (повертає закешований результат з 1 елементом, не знаючи про новий)
        var controller2 = new CategoriesApiController(context, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result2 = await controller2.GetCategories(0, 10);
        var ok2 = Assert.IsType<OkObjectResult>(result2.Result);
        var paged2 = Assert.IsType<PagedResult<Category>>(ok2.Value);
        Assert.Single(paged2.Items); // Все ще 1 елемент з кешу!
        Assert.Equal("HIT", controller2.Response.Headers["X-Cache"]);

        // 4. Створюємо нову категорію через API (має спрацювати інвалідація кешу)
        await controller2.PostCategory(new Category { Name = "Спорт" });

        // 5. Третій запит: CACHE MISS, оскільки кеш очищено, і тепер підтягнуто всі 3 категорії
        var controller3 = new CategoriesApiController(context, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result3 = await controller3.GetCategories(0, 10);
        var ok3 = Assert.IsType<OkObjectResult>(result3.Result);
        var paged3 = Assert.IsType<PagedResult<Category>>(ok3.Value);
        Assert.Equal(3, paged3.Items.Count()); // Всі 3 категорії
        Assert.Equal("MISS", controller3.Response.Headers["X-Cache"]);
    }

    [Fact]
    public async Task FacilitiesApi_GetFacilities_UsesCacheAndInvalidatesOnPost()
    {
        using var context = TestDbContextFactory.Create();
        var cache = CreateCache();

        var cat = new Category { Name = "Заклади" };
        context.Categories.Add(cat);
        context.Facilities.Add(new Facility { Name = "Заклад 1", CategoryId = cat.Id });
        await context.SaveChangesAsync();

        var controller = new FacilitiesApiController(context, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        // 1. Перший запит: CACHE MISS
        var result1 = await controller.GetFacilities(0, 10);
        var ok1 = Assert.IsType<OkObjectResult>(result1.Result);
        var paged1 = Assert.IsType<PagedResult<Facility>>(ok1.Value);
        Assert.Single(paged1.Items);
        Assert.Equal("MISS", controller.Response.Headers["X-Cache"]);

        // 2. Другий запит: CACHE HIT
        var controller2 = new FacilitiesApiController(context, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result2 = await controller2.GetFacilities(0, 10);
        var ok2 = Assert.IsType<OkObjectResult>(result2.Result);
        var paged2 = Assert.IsType<PagedResult<Facility>>(ok2.Value);
        Assert.Single(paged2.Items);
        Assert.Equal("HIT", controller2.Response.Headers["X-Cache"]);

        // 3. Інвалідація через створення закладу
        await controller2.PostFacility(new Facility { Name = "Заклад 2", CategoryId = cat.Id });

        // 4. Наступний запит: CACHE MISS та 2 заклади
        var controller3 = new FacilitiesApiController(context, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result3 = await controller3.GetFacilities(0, 10);
        var ok3 = Assert.IsType<OkObjectResult>(result3.Result);
        var paged3 = Assert.IsType<PagedResult<Facility>>(ok3.Value);
        Assert.Equal(2, paged3.Items.Count());
        Assert.Equal("MISS", controller3.Response.Headers["X-Cache"]);
    }
}

