using Microsoft.AspNetCore.Mvc;
using LocalServices.Models;
using Xunit;

namespace LocalServices.Tests.Controllers;

public class AnalyticsControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewResult_WithPopulatedAnalyticsViewModel()
    {
        using var context = TestDbContextFactory.Create();

        var catRestaurant = new Category { Name = "Ресторан" };
        var catAuto = new Category { Name = "Автосервіс" };
        context.Categories.AddRange(catRestaurant, catAuto);
        await context.SaveChangesAsync();

        var fac1 = new Facility { Name = "Заклад 1", City = "Київ", CategoryId = catRestaurant.Id };
        var fac2 = new Facility { Name = "Заклад 2", City = "Київ", CategoryId = catRestaurant.Id };
        var fac3 = new Facility { Name = "Заклад 3", City = "Львів", CategoryId = catAuto.Id };
        context.Facilities.AddRange(fac1, fac2, fac3);
        await context.SaveChangesAsync();

        var s1 = new Service { Name = "Послуга 1", Price = 100m, FacilityId = fac1.Id };
        var s2 = new Service { Name = "Послуга 2", Price = 300m, FacilityId = fac2.Id };
        context.Services.AddRange(s1, s2);
        await context.SaveChangesAsync();

        var controller = new AnalyticsController(context);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<AnalyticsViewModel>(viewResult.Model);

        Assert.Equal(3, model.TotalFacilities);
        Assert.Equal(2, model.TotalCategories);
        Assert.Equal(2, model.TotalServices);
        Assert.Equal(200.0, model.AverageServicePrice);
        Assert.NotEmpty(model.FacilitiesByCategory);
        Assert.NotEmpty(model.FacilitiesByCity);
        Assert.NotEmpty(model.PriceByCategory);
    }
}

