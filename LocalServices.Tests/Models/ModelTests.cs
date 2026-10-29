using LocalServices.Models;
using Xunit;

namespace LocalServices.Tests.Models;

public class ModelTests
{
    [Fact]
    public void Category_DefaultValues_AreCorrect()
    {
        var category = new Category();

        Assert.Equal(0, category.Id);
        Assert.Equal(string.Empty, category.Name);
        Assert.Null(category.Description);
        Assert.NotNull(category.Facilities);
        Assert.Empty(category.Facilities);
    }

    [Fact]
    public void Category_CanSetAndGetProperties()
    {
        var facility = new Facility { Name = "Кафе" };
        var category = new Category
        {
            Id = 1,
            Name = "Ресторани",
            Description = "Опис категорії",
            Facilities = new List<Facility> { facility }
        };

        Assert.Equal(1, category.Id);
        Assert.Equal("Ресторани", category.Name);
        Assert.Equal("Опис категорії", category.Description);
        Assert.Single(category.Facilities);
        Assert.Same(facility, category.Facilities[0]);
    }

    [Fact]
    public void Facility_DefaultValues_AreCorrect()
    {
        var facility = new Facility();

        Assert.Equal(0, facility.Id);
        Assert.Equal(string.Empty, facility.Name);
        Assert.Null(facility.City);
        Assert.Null(facility.Street);
        Assert.Null(facility.Description);
        Assert.Null(facility.Phone);
        Assert.Null(facility.WorkingHours);
        Assert.Null(facility.ImagePath);
        Assert.Null(facility.ImageFile);
        Assert.Equal(0, facility.CategoryId);
        Assert.Null(facility.Category);
        Assert.NotNull(facility.Services);
        Assert.Empty(facility.Services);
    }

    [Fact]
    public void Facility_CanSetAndGetProperties()
    {
        var category = new Category { Id = 10, Name = "Спорт" };
        var service = new Service { Id = 5, Name = "Абонемент" };
        var facility = new Facility
        {
            Id = 1,
            Name = "Спортлайф",
            City = "Київ",
            Street = "вул. Перемоги, 10",
            Description = "Тестовий опис",
            Phone = "+380501112233",
            WorkingHours = "09:00 - 21:00",
            ImagePath = "data:image/png;base64,abc",
            CategoryId = 10,
            Category = category,
            Services = new List<Service> { service }
        };

        Assert.Equal(1, facility.Id);
        Assert.Equal("Спортлайф", facility.Name);
        Assert.Equal("Київ", facility.City);
        Assert.Equal("вул. Перемоги, 10", facility.Street);
        Assert.Equal("Тестовий опис", facility.Description);
        Assert.Equal("+380501112233", facility.Phone);
        Assert.Equal("09:00 - 21:00", facility.WorkingHours);
        Assert.Equal("data:image/png;base64,abc", facility.ImagePath);
        Assert.Equal(10, facility.CategoryId);
        Assert.Same(category, facility.Category);
        Assert.Single(facility.Services);
        Assert.Same(service, facility.Services[0]);
    }

    [Fact]
    public void Service_DefaultValues_AreCorrect()
    {
        var service = new Service();

        Assert.Equal(0, service.Id);
        Assert.Equal(string.Empty, service.Name);
        Assert.Equal(0m, service.Price);
        Assert.Equal(0, service.FacilityId);
        Assert.Null(service.Facility);
    }

    [Fact]
    public void Service_CanSetAndGetProperties()
    {
        var facility = new Facility { Id = 3, Name = "СТО" };
        var service = new Service
        {
            Id = 1,
            Name = "Ремонт двигуна",
            Price = 2500.50m,
            FacilityId = 3,
            Facility = facility
        };

        Assert.Equal(1, service.Id);
        Assert.Equal("Ремонт двигуна", service.Name);
        Assert.Equal(2500.50m, service.Price);
        Assert.Equal(3, service.FacilityId);
        Assert.Same(facility, service.Facility);
    }
}
