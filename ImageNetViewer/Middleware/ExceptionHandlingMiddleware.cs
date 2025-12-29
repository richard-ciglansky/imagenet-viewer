using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ImageNetViewer.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;
    private readonly IWebHostEnvironment env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        this.next = next;
        this.logger = logger;
        this.env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ArgumentException or ArgumentNullException or BadHttpRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        // Always log server errors; lower levels for client errors
        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning(exception, "Handled exception ({StatusCode}) processing {Method} {Path}: {Message}", statusCode, context.Request.Method, context.Request.Path, exception.Message);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = context.Request.Path,
        };

        // Include more details in development
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (env.IsDevelopment())
        {
            problem.Detail = exception.Message;
            problem.Extensions["exceptionType"] = exception.GetType().FullName;
        }

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = env.IsDevelopment()
        });

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(json);
    }
}