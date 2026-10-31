using LocalServices.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

connectionString = connectionString.Replace("\r", "").Replace("\n", "").Trim();

if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) || connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');
    connectionString = $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true;";
}
else
{
    connectionString = connectionString.Replace(",", ";");
    if (!connectionString.Contains("Trust Server Certificate", StringComparison.OrdinalIgnoreCase))
    {
        connectionString += ";Trust Server Certificate=true;";
    }
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<LocalServices.Services.ITelegramNotificationService, LocalServices.Services.TelegramNotificationService>();

builder.Services.AddScoped<LocalServices.Services.IBlobStorageService, LocalServices.Services.CloudinaryBlobStorageService>();

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new LocalServices.Infrastructure.InvariantDecimalModelBinderProvider());
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
    options.HttpsPort = 443;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        int retries = 5;
        while (retries > 0)
        {
            try
            {
                context.Database.Migrate();
                logger.LogInformation("Міграції бази даних успішно застосовано.");
                break;
            }
            catch (Exception ex) when (retries > 1)
            {
                retries--;
                logger.LogWarning(ex, "Очікування підключення до PostgreSQL... Залишилося спроб: {Retries}", retries);
                Thread.Sleep(3000);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Помилка при застосуванні міграцій бази даних.");
    }
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var host = context.Request.Host.Host;
    if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
    {
        var newHost = host.Substring(4);
        var port = context.Request.Host.Port.HasValue ? $":{context.Request.Host.Port}" : "";
        var newUrl = $"{context.Request.Scheme}://{newHost}{port}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
        context.Response.Headers.Location = newUrl;
        return;
    }
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
