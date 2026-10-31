using System.Globalization;
using LocalServices.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace LocalServices.Tests.Infrastructure;

public class InvariantDecimalModelBinderTests
{
    private class SimpleValueProvider : IValueProvider
    {
        private readonly Dictionary<string, ValueProviderResult> _values;
        public SimpleValueProvider(Dictionary<string, ValueProviderResult> values) => _values = values;
        public bool ContainsPrefix(string prefix) => _values.ContainsKey(prefix);
        public ValueProviderResult GetValue(string key) => _values.TryGetValue(key, out var val) ? val : ValueProviderResult.None;
    }

    private static ModelBindingContext CreateContext(string key, string? rawValue, Type modelType)
    {
        var values = new ValueProviderResult(rawValue);
        var dict = new Dictionary<string, ValueProviderResult> { { key, values } };
        var metadataProvider = new EmptyModelMetadataProvider();

        return new DefaultModelBindingContext
        {
            ActionContext = new ActionContext { HttpContext = new DefaultHttpContext() },
            ModelName = key,
            ModelMetadata = metadataProvider.GetMetadataForType(modelType),
            ModelState = new ModelStateDictionary(),
            ValueProvider = new SimpleValueProvider(dict)
        };
    }

    [Theory]
    [InlineData("50.4501", 50.4501)]
    [InlineData("50,4501", 50.4501)]
    [InlineData("  30.5234  ", 30.5234)]
    public async Task BindModelAsync_ParsesDoubleWithDotOrComma(string input, double expected)
    {
        var binder = new InvariantDecimalModelBinder();
        var context = CreateContext("Latitude", input, typeof(double?));

        await binder.BindModelAsync(context);

        Assert.True(context.Result.IsModelSet);
        Assert.Equal(expected, (double)context.Result.Model!, 4);
        Assert.True(context.ModelState.IsValid);
    }

    [Theory]
    [InlineData("120.50", 120.50)]
    [InlineData("120,50", 120.50)]
    public async Task BindModelAsync_ParsesDecimalWithDotOrComma(string input, decimal expected)
    {
        var binder = new InvariantDecimalModelBinder();
        var context = CreateContext("Price", input, typeof(decimal));

        await binder.BindModelAsync(context);

        Assert.True(context.Result.IsModelSet);
        Assert.Equal(expected, (decimal)context.Result.Model!);
        Assert.True(context.ModelState.IsValid);
    }
}
