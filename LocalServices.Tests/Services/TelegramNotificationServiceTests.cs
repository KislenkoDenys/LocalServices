using System.Net;
using LocalServices.Models;
using LocalServices.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace LocalServices.Tests.Services;

public class TelegramNotificationServiceTests
{
    [Fact]
    public async Task SendMessageAsync_ReturnsFalse_WhenBotTokenOrChatIdMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Telegram:BotToken", "" },
                { "Telegram:ChatId", "" }
            })
            .Build();

        var httpClient = new HttpClient();
        var service = new TelegramNotificationService(httpClient, config);

        var result = await service.SendMessageAsync("Тестове повідомлення");

        Assert.False(result);
    }

    [Fact]
    public async Task SendMessageAsync_SendsHttpRequest_WhenConfigured()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Telegram:BotToken", "123456:FAKE_TOKEN" },
                { "Telegram:ChatId", "987654321" }
            })
            .Build();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"ok\":true}")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new TelegramNotificationService(httpClient, config);

        var result = await service.SendMessageAsync("Новий заклад");

        Assert.True(result);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString().Contains("123456:FAKE_TOKEN/sendMessage")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task SendFacilityCreatedAsync_FormatsMessageProperly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Telegram:BotToken", "123456:TOKEN" },
                { "Telegram:ChatId", "123" }
            })
            .Build();

        string capturedContent = "";
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                if (req.Content != null)
                {
                    capturedContent = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}") });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new TelegramNotificationService(httpClient, config);

        var facility = new Facility
        {
            Name = "Кав'ярня Аромат",
            City = "Київ",
            Street = "Хрещатик, 1",
            Phone = "+380441234567"
        };

        await service.SendFacilityCreatedAsync(facility, "Кафе");

        Assert.NotEmpty(capturedContent);
        Assert.Contains("Кав'ярня Аромат", capturedContent);
        Assert.Contains("Кафе", capturedContent);
        Assert.Contains("Київ, Хрещатик, 1", capturedContent);
    }

    [Fact]
    public async Task SendCategoryCreatedAsync_FormatsMessageProperly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Telegram:BotToken", "123456:TOKEN" },
                { "Telegram:ChatId", "123" }
            })
            .Build();

        string capturedContent = "";
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                if (req.Content != null)
                {
                    capturedContent = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}") });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new TelegramNotificationService(httpClient, config);

        var category = new Category
        {
            Name = "Спорт та фітнес",
            Description = "Спортивні клуби, басейни та секції"
        };

        await service.SendCategoryCreatedAsync(category);

        Assert.NotEmpty(capturedContent);
        Assert.Contains("Нова категорія на LocalServices!", capturedContent);
        Assert.Contains("Спорт та фітнес", capturedContent);
        Assert.Contains("Спортивні клуби, басейни та секції", capturedContent);
    }
}
