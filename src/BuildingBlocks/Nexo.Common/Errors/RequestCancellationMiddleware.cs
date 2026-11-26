using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Nexo.Common.Errors;

/// <summary>Client aborts end only that request; dependency cancellations remain failures.</summary>
public sealed class RequestCancellationMiddleware(RequestDelegate next, ILogger<RequestCancellationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted) context.Response.StatusCode = 499;
            return;
        }

        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // No body, retry, authentication failure or successful mutation is invented.
            if (!context.Response.HasStarted) context.Response.StatusCode = 499;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("A dependency cancelled request {TraceId} before completion.", context.TraceIdentifier);
            if (context.Response.HasStarted)
            {
                context.Abort();
                return;
            }

            context.Response.Clear();
            context.Response.Headers.CacheControl = "no-store";
            await NexoProblem.From(NexoCodes.ServerDependencyUnavailable, null, context).ExecuteAsync(context);
        }
    }
}
