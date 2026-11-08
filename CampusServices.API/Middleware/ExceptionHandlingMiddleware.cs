using CampusServices.Application.Exceptions;
using CampusServices.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CampusServices.API.Middleware;

// One place that catches errors thrown anywhere below it and converts them to a JSON answer.
// Controllers therefore do not need try/catch blocks.
//
//   NotFoundException   -> 404
//   ForbiddenException  -> 403
//   DomainException     -> 400   (broken business rule, e.g. invalid status change)
//   anything else       -> 500   (details are logged, NOT sent to the client)
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        int statusCode;
        string title;
        string detail;

        if (ex is NotFoundException)
        {
            statusCode = StatusCodes.Status404NotFound;
            title = "Not Found";
            detail = ex.Message;
        }
        else if (ex is ForbiddenException)
        {
            statusCode = StatusCodes.Status403Forbidden;
            title = "Forbidden";
            detail = ex.Message;
        }
        else if (ex is DomainException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            title = "Bad Request";
            detail = ex.Message;
        }
        else
        {
            _logger.LogError(ex, "Unhandled error while processing {Path}", context.Request.Path);
            statusCode = StatusCodes.Status500InternalServerError;
            title = "Internal Server Error";
            detail = "Something went wrong on the server. Please try again later.";
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
