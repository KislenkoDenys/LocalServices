using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LocalServices.Data;
using LocalServices.Models;

public class AnalyticsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AnalyticsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Analytics
    public async Task<IActionResult> Index()
    {
        var totalFacilities = await _context.Facilities.CountAsync();
        var totalCategories = await _context.Categories.CountAsync();
        var totalServices = await _context.Services.CountAsync();
        var avgPrice = totalServices > 0
            ? Math.Round((double)await _context.Services.AverageAsync(s => s.Price), 2)
            : 0.0;

        // Завантажуємо базові дані закладів для надійного групування в пам'яті (уникаємо специфічних помилок SQL перекладу EF Core)
        var facilityRecords = await _context.Facilities
            .Include(f => f.Category)
            .Select(f => new
            {
                CategoryName = f.Category != null ? f.Category.Name : null,
                City = f.City
            })
            .ToListAsync();

        var byCategory = facilityRecords
            .Where(f => !string.IsNullOrEmpty(f.CategoryName))
            .GroupBy(f => f.CategoryName!)
            .Select(g => new ChartItem
            {
                Label = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var byCity = facilityRecords
            .GroupBy(f => string.IsNullOrWhiteSpace(f.City) ? "Інше" : f.City.Trim())
            .Select(g => new ChartItem
            {
                Label = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToList();

        // Завантажуємо послуги з назвами категорій для підрахунку середньої вартості
        var serviceRecords = await _context.Services
            .Include(s => s.Facility)
                .ThenInclude(f => f!.Category)
            .Where(s => s.Facility != null && s.Facility.Category != null)
            .Select(s => new
            {
                CategoryName = s.Facility!.Category!.Name,
                Price = (double)s.Price
            })
            .ToListAsync();

        var priceByCategory = serviceRecords
            .GroupBy(s => s.CategoryName)
            .Select(g => new PriceChartItem
            {
                Category = g.Key,
                AvgPrice = Math.Round(g.Average(s => s.Price), 2)
            })
            .OrderByDescending(x => x.AvgPrice)
            .ToList();

        var viewModel = new AnalyticsViewModel
        {
            TotalFacilities = totalFacilities,
            TotalCategories = totalCategories,
            TotalServices = totalServices,
            AverageServicePrice = avgPrice,
            FacilitiesByCategory = byCategory,
            FacilitiesByCity = byCity,
            PriceByCategory = priceByCategory
        };

        return View(viewModel);
    }
}
