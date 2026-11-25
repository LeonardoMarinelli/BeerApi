using BeerApi.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
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
        catch (ConflictException ex)
        {
            _logger.LogWarning(ex, "Request conflicts with current resource state");
            await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                "Conflito", ex.Message);
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { Number: 1062 or 1451 or 1452 })
        {
            _logger.LogWarning(ex, "Database constraint conflict");
            await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                "Conflito de dados", "A operação conflita com os dados existentes.");
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
