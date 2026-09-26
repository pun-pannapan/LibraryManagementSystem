using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = Activity.Current?.Id ?? "none";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();
            logger.LogInformation(
                "MediatR request {RequestName} completed in {ElapsedMilliseconds} ms with correlation {CorrelationId}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                correlationId);
            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.LogError(
                exception,
                "MediatR request {RequestName} failed after {ElapsedMilliseconds} ms with correlation {CorrelationId}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                correlationId);
            throw;
        }
    }
}
