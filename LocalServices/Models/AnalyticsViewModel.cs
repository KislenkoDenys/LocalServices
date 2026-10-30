namespace LocalServices.Models;

public class AnalyticsViewModel
{
    public int TotalFacilities { get; set; }
    public int TotalCategories { get; set; }
    public int TotalServices { get; set; }
    public double AverageServicePrice { get; set; }

    public List<ChartItem> FacilitiesByCategory { get; set; } = new();
    public List<ChartItem> FacilitiesByCity { get; set; } = new();
    public List<PriceChartItem> PriceByCategory { get; set; } = new();
}

public class ChartItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PriceChartItem
{
    public string Category { get; set; } = string.Empty;
    public double AvgPrice { get; set; }
}

