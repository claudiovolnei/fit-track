using FitTrack.Application.Exceptions;
using FitTrack.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Adapters.In.Api.Composition;

/// <summary>
/// Translates the errors the inner layers raise into HTTP problem responses, so no
/// endpoint has to know about them and no business rule leaks into the adapter.
/// </summary>
internal sealed class DomainExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var mapped = Map(exception);
            if (mapped is null)
            {
                throw;
            }

            context.Response.StatusCode = mapped.Value.Status;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = mapped.Value.Status,
                    Title = mapped.Value.Title,
                    Detail = exception.Message,
                    Instance = context.Request.Path,
                },
                context.RequestAborted);
        }
    }

    private static (int Status, string Title)? Map(Exception exception) => exception switch
    {
        DomainException domain => (StatusCodes.Status400BadRequest, domain.Code),
        InvalidCommandException => (StatusCodes.Status400BadRequest, "invalid_command"),
        ForbiddenException => (StatusCodes.Status403Forbidden, "forbidden"),
        NotFoundException => (StatusCodes.Status404NotFound, "not_found"),
        _ => null,
    };
}
