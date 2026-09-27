using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dsg.Hrms.Api.Validation;

/// <summary>
/// Eylem parametrelerini FluentValidation ile dogrular (ADR-0010: tek dogruluk kaynagi).
/// </summary>
/// <remarks>
/// Gecersiz istek <see cref="ValidationException"/> firlatir; hata ara katmani bunu
/// alan bazli <c>400</c> Problem Details yanitina cevirir. Dogrulayicisi olmayan
/// parametreler oldugu gibi gecer.
/// </remarks>
public sealed class ValidateRequestsFilter : IAsyncActionFilter
{
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is IValidator validator)
            {
                var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
                if (!result.IsValid)
                {
                    throw new ValidationException(result.Errors);
                }
            }
        }

        await next();
    }
}
