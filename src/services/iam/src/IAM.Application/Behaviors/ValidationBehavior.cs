using FluentValidation;
using MediatR;
using Platform.BuildingBlocks.Result;

namespace IAM.Application.Behaviors;

/// <summary>
/// Pipeline behavior for validating requests using FluentValidation.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            // Check if response type is Result<T>
            var responseType = typeof(TResponse);
            
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var errors = failures
                    .Select(f => Error.ValidationError(f.PropertyName, f.ErrorMessage))
                    .ToArray();

                var validationError = Error.ValidationError(
                    "Validation.Failed",
                    string.Join("; ", failures.Select(f => f.ErrorMessage)));

                var resultType = responseType.GetGenericArguments()[0];
                var failureMethod = typeof(Result)
                    .GetMethod(nameof(Result.Failure), 1, new[] { typeof(Error) })!
                    .MakeGenericMethod(resultType);

                return (TResponse)failureMethod.Invoke(null, new object[] { validationError })!;
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
