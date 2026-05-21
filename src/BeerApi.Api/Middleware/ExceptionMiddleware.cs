using BeerApi.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BeerApi.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            await WriteProblemAsync(context, StatusCodes.Status404NotFound,
                "Recurso não encontrado", ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business rule violation");
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest,
                "Violação de regra de negócio", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "Erro interno", "Ocorreu um erro inesperado.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        });
    }
}
