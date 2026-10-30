using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace LocalServices.Tests.E2E;

public class SeoE2ETests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private string _targetUrl = string.Empty;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        _page = await _browser.NewPageAsync();

        // Перевіряємо локальний сервер або хмарний деплой Render
        var localUrl = "http://localhost:8080/Facilities/Details/1";
        var cloudUrl = "https://localservices-ecv8.onrender.com/Facilities/Details/1";

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var localRes = await client.GetAsync(localUrl);
            _targetUrl = localRes.IsSuccessStatusCode ? localUrl : cloudUrl;
        }
        catch
        {
            _targetUrl = cloudUrl;
        }

        int attempts = 3;
        while (attempts > 0)
        {
            try
            {
                await _page.GotoAsync(_targetUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 60000
                });
                break;
            }
            catch when (attempts > 1)
            {
                attempts--;
                await Task.Delay(5000);
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task Page_HasCorrectLanguageAndFavicon()
    {
        Assert.NotNull(_page);

        var lang = await _page.GetAttributeAsync("html", "lang");
        Assert.Equal("uk", lang);

        var favicon = await _page.GetAttributeAsync("link[rel*='icon']", "href");
        Assert.False(string.IsNullOrWhiteSpace(favicon));
    }

    [Fact]
    public async Task MetaTags_TitleAndDescriptionAndCanonical_AreValid()
    {
        Assert.NotNull(_page);

        var title = await _page.TitleAsync();
        Assert.False(string.IsNullOrWhiteSpace(title));
        Assert.Contains("—", title);

        var metaDescription = await _page.GetAttributeAsync("meta[name='description']", "content");
        Assert.False(string.IsNullOrWhiteSpace(metaDescription));
        Assert.True(metaDescription.Length >= 40, "Meta description має бути достатньо інформативним (>= 40 символів).");

        var canonical = await _page.GetAttributeAsync("link[rel='canonical']", "href");
        Assert.False(string.IsNullOrWhiteSpace(canonical));
        Assert.Contains("/Facilities/Details/", canonical);

        var robots = await _page.GetAttributeAsync("meta[name='robots']", "content");
        Assert.Contains("index", robots ?? "");
        Assert.Contains("follow", robots ?? "");
    }

    [Fact]
    public async Task SocialTags_OpenGraphAndTwitterCards_AreConfigured()
    {
        Assert.NotNull(_page);

        var ogTitle = await _page.GetAttributeAsync("meta[property='og:title']", "content");
        Assert.False(string.IsNullOrWhiteSpace(ogTitle));

        var ogDescription = await _page.GetAttributeAsync("meta[property='og:description']", "content");
        Assert.False(string.IsNullOrWhiteSpace(ogDescription));

        var ogType = await _page.GetAttributeAsync("meta[property='og:type']", "content");
        Assert.Equal("website", ogType);

        var ogUrl = await _page.GetAttributeAsync("meta[property='og:url']", "content");
        Assert.False(string.IsNullOrWhiteSpace(ogUrl));

        var twitterCard = await _page.GetAttributeAsync("meta[name='twitter:card']", "content");
        Assert.Equal("summary_large_image", twitterCard);
    }

    [Fact]
    public async Task StructuredData_SchemaOrg_LocalBusiness_IsValidJson()
    {
        Assert.NotNull(_page);

        var jsonLdHandle = await _page.QuerySelectorAsync("script[type='application/ld+json']");
        Assert.NotNull(jsonLdHandle);

        var jsonText = await jsonLdHandle.InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(jsonText));

        using var doc = JsonDocument.Parse(jsonText);
        var root = doc.RootElement;

        Assert.Equal("https://schema.org", root.GetProperty("@context").GetString());
        Assert.Equal("LocalBusiness", root.GetProperty("@type").GetString());
        Assert.True(root.TryGetProperty("name", out _));
        Assert.True(root.TryGetProperty("telephone", out _));
        Assert.True(root.TryGetProperty("address", out var addr));
        Assert.Equal("PostalAddress", addr.GetProperty("@type").GetString());
    }

    [Fact]
    public async Task LandingPage_Content_HasVisibleH1AndStickyContactSidebar()
    {
        Assert.NotNull(_page);

        var h1 = _page.Locator("h1");
        Assert.True(await h1.IsVisibleAsync());
        var h1Text = await h1.InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(h1Text));

        var contactBox = _page.Locator(".sticky-contact-box");
        Assert.True(await contactBox.IsVisibleAsync());

        var mapsLink = _page.Locator("a[href*='maps.google.com']");
        Assert.True(await mapsLink.IsVisibleAsync());
        Assert.Equal("_blank", await mapsLink.GetAttributeAsync("target"));
        Assert.Contains("noopener", await mapsLink.GetAttributeAsync("rel") ?? "");

        var bodyText = await _page.InnerTextAsync("body");
        var wordCount = bodyText.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.True(wordCount >= 200, $"Кількість слів на сторінці повинна бути >= 200, фактично: {wordCount}");
    }
}

