using System.Net;
using System.Text.Json;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Application.Exceptions;
using Microsoft.AspNetCore.Http;

namespace VehicleRegistration.API.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        ApiErrorResponse errorResponse;

        switch (exception)
        {
            case NotFoundException notFound:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse = ApiErrorResponse.FromMessage(notFound.Message);
                _logger.LogWarning("Not found: {Message}", notFound.Message);
                break;

            case ValidationException validation:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = ApiErrorResponse.FromValidation(validation.Errors);
                _logger.LogWarning("Validation failed: {Errors}", validation.Errors);
                break;

            case BusinessRuleException businessRule:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = ApiErrorResponse.FromMessage(businessRule.Message);
                _logger.LogWarning("Business rule violation: {Message}", businessRule.Message);
                break;
            case ConflictException conflict:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse = ApiErrorResponse.FromMessage(conflict.Message);
                _logger.LogWarning("Conflict: {Message}", conflict.Message);
                break;
            case BadHttpRequestException badRequest
                when badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge:
                response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                errorResponse = ApiErrorResponse.FromMessage(
                    "Icon upload requests must be 2 MB or smaller.");
                _logger.LogWarning("Request body exceeded its size limit.");
                break;
            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse = ApiErrorResponse.FromMessage("An unexpected error occurred. Please try again later.");
                _logger.LogError(exception, "Unhandled exception occurred");
                break;
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }
}