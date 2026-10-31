using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LocalServices.Infrastructure
{
    public class InvariantDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var type = context.Metadata.ModelType;
            if (type == typeof(double) || type == typeof(double?) ||
                type == typeof(decimal) || type == typeof(decimal?) ||
                type == typeof(float) || type == typeof(float?))
            {
                return new InvariantDecimalModelBinder();
            }

            return null;
        }
    }

    public class InvariantDecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            var rawValue = valueResult.FirstValue;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                if (Nullable.GetUnderlyingType(bindingContext.ModelType) != null)
                {
                    bindingContext.Result = ModelBindingResult.Success(null);
                }
                return Task.CompletedTask;
            }

            // Підтримка як крапки (з карти/JS/HTML5), так і коми (з клавіатури в укр. локалі)
            var normalized = rawValue.Replace(',', '.').Trim();
            var targetType = bindingContext.ModelType;

            if (targetType == typeof(double) || targetType == typeof(double?))
            {
                if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                {
                    bindingContext.Result = ModelBindingResult.Success(d);
                    return Task.CompletedTask;
                }
            }
            else if (targetType == typeof(decimal) || targetType == typeof(decimal?))
            {
                if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var m))
                {
                    bindingContext.Result = ModelBindingResult.Success(m);
                    return Task.CompletedTask;
                }
            }
            else if (targetType == typeof(float) || targetType == typeof(float?))
            {
                if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                {
                    bindingContext.Result = ModelBindingResult.Success(f);
                    return Task.CompletedTask;
                }
            }

            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"Значення '{rawValue}' має бути коректним числом.");
            return Task.CompletedTask;
        }
    }
}

