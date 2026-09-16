using GottaGo.Application.Abstractions;
using GottaGo.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GottaGo.Api.Common;

/// <summary>
/// Translates the exceptions the inner layers throw into HTTP responses.
///
/// This is the only place that mapping happens, which is what lets the application and domain
/// layers stay ignorant of status codes. They describe what went wrong; the API decides how
/// to say it over HTTP.
/// </summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Not allowed"),
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (0, string.Empty),
        };

        if (status == 0)
        {
            // Not ours: let the default handler deal with it so real bugs still surface
            // as 500s with full logging rather than being quietly swallowed.
            return false;
        }

        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
            },
        });
    }
}
