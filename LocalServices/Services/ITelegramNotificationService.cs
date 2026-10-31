using LocalServices.Models;

namespace LocalServices.Services;

public interface ITelegramNotificationService
{
    Task SendFacilityCreatedAsync(Facility facility, string? categoryName = null);
    Task SendServiceCreatedAsync(Service service, string? facilityName = null);
    Task SendCategoryCreatedAsync(Category category);
    Task<bool> SendMessageAsync(string message);
}
