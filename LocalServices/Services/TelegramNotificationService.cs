using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LocalServices.Models;

namespace LocalServices.Services;

public class TelegramNotificationService : ITelegramNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramNotificationService>? _logger;

    public TelegramNotificationService(HttpClient httpClient, IConfiguration configuration, ILogger<TelegramNotificationService>? logger = null)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendFacilityCreatedAsync(Facility facility, string? categoryName = null)
    {
        var categoryText = !string.IsNullOrWhiteSpace(categoryName)
            ? categoryName
            : (facility.Category?.Name ?? "Без категорії");

        var locationText = string.Join(", ", new[] { facility.City, facility.Street }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(locationText)) locationText = "Не вказано";

        var sb = new StringBuilder();
        sb.AppendLine("<b>Новий заклад на LocalServices!</b>");
        sb.AppendLine();
        sb.AppendLine($"<b>Назва:</b> {facility.Name}");
        sb.AppendLine($"<b>Категорія:</b> {categoryText}");
        sb.AppendLine($"<b>Адреса:</b> {locationText}");

        if (!string.IsNullOrWhiteSpace(facility.Phone))
        {
            sb.AppendLine($"<b>Телефон:</b> {facility.Phone}");
        }

        if (!string.IsNullOrWhiteSpace(facility.WorkingHours))
        {
            sb.AppendLine($"<b>Години роботи:</b> {facility.WorkingHours}");
        }

        await SendMessageAsync(sb.ToString());
    }

    public async Task SendServiceCreatedAsync(Service service, string? facilityName = null)
    {
        var facText = !string.IsNullOrWhiteSpace(facilityName)
            ? facilityName
            : (service.Facility?.Name ?? "Невідомий заклад");

        var sb = new StringBuilder();
        sb.AppendLine("<b>Нова послуга на LocalServices!</b>");
        sb.AppendLine();
        sb.AppendLine($"<b>Послуга:</b> {service.Name}");
        sb.AppendLine($"<b>Ціна:</b> {service.Price:N2} грн");
        sb.AppendLine($"<b>Заклад:</b> {facText}");

        await SendMessageAsync(sb.ToString());
    }

    public async Task SendCategoryCreatedAsync(Category category)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<b>Нова категорія на LocalServices!</b>");
        sb.AppendLine();
        sb.AppendLine($"<b>Назва:</b> {category.Name}");

        if (!string.IsNullOrWhiteSpace(category.Description))
        {
            sb.AppendLine($"<b>Опис:</b> {category.Description}");
        }

        await SendMessageAsync(sb.ToString());
    }

    public async Task<bool> SendMessageAsync(string message)
    {
        var botToken = _configuration["Telegram:BotToken"];
        var chatId = _configuration["Telegram:ChatId"];

        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[TELEGRAM NOTIFICATION] Токен або ChatId не вказано в appsettings.json. Сповіщення:\n{message}");
            Console.ResetColor();
            _logger?.LogInformation("[TELEGRAM NOTIFICATION] Сповіщення (симуляція):\n{Message}", message);
            return false;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = message,
                parse_mode = "HTML"
            };

            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(payload, options);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[TELEGRAM NOTIFICATION] Сповіщення успішно надіслано в Telegram!");
                Console.ResetColor();
                _logger?.LogInformation("[TELEGRAM NOTIFICATION] Успішно надіслано в Telegram.");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[TELEGRAM NOTIFICATION] Помилка надсилання в Telegram: {response.StatusCode} - {error}");
                Console.ResetColor();
                _logger?.LogWarning("[TELEGRAM NOTIFICATION] Помилка надсилання: {Error}", error);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[TELEGRAM NOTIFICATION] Виняток при надсиланні в Telegram: {ex.Message}");
            Console.ResetColor();
            _logger?.LogError(ex, "[TELEGRAM NOTIFICATION] Виняток при надсиланні.");
            return false;
        }
    }
}
