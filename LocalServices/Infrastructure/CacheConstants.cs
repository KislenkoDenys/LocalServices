namespace LocalServices.Infrastructure;

public static class CacheConstants
{
    public const string CategoriesAllKey = "categories_all";
    public const string FacilitiesAllKey = "facilities_all";
    public static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);
}

