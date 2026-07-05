using FluentValidation;
using MediatR;
using SmartCall.Application.Common;

namespace SmartCall.Application.Common.Behaviors;

/// <summary>Runs all FluentValidation validators for a request before its handler.</summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct))))
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
                throw new AppValidationException(string.Join("; ", failures.Select(f => f.ErrorMessage)));
        }

        return await next();
    }
}
