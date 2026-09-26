using System.Text.Json;
using Api.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace Api.Extensions;

public static class ExceptionHandlingExtensions
{
    public static WebApplication UseApiExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionApp =>
        {
            exceptionApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception is SharedServiceConfigurationException)
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

                    await Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "The shared service is not configured"
                    ).ExecuteAsync(context);
                }
                else if (exception is AiGatewayConfigurationException or
                    McpIntegrationDisabledException or
                    RagIntegrationDisabledException)
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

                    await Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "AI assistant unavailable",
                        detail: "This AI feature is disabled in the current environment."
                    ).ExecuteAsync(context);
                }
                else if (exception is AiGatewayException or
                    McpServiceException or
                    RagServiceException)
                {
                    context.Response.StatusCode = StatusCodes.Status502BadGateway;

                    await Results.Problem(
                        statusCode: StatusCodes.Status502BadGateway,
                        title: "AI assistant unavailable",
                        detail: "The required AI service could not be reached or complete the request. Make sure it is running, then try again."
                    ).ExecuteAsync(context);
                }
                else if (exception is DatabaseServiceException databaseException)
                {
                    var statusCode = databaseException.StatusCode is null
                        ? StatusCodes.Status503ServiceUnavailable
                        : (int)databaseException.StatusCode.Value;
                    context.Response.StatusCode = statusCode;

                    if (statusCode == StatusCodes.Status400BadRequest)
                    {
                        await Results.BadRequest(databaseException.Message).ExecuteAsync(context);
                    }
                    else
                    {
                        await Results.Problem(
                            statusCode: statusCode,
                            title: databaseException.Message
                        ).ExecuteAsync(context);
                    }
                }
                else if (exception is SharedServiceException or HttpRequestException or JsonException)
                {
                    context.Response.StatusCode = StatusCodes.Status502BadGateway;

                    await Results.Problem(
                        statusCode: StatusCodes.Status502BadGateway,
                        title: "The shared service request failed"
                    ).ExecuteAsync(context);
                }
                else if (exception is BadHttpRequestException { InnerException: JsonException })
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;

                    await Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["request"] =
                            [
                                "The request body could not be parsed as valid JSON for this endpoint."
                            ]
                        }
                    ).ExecuteAsync(context);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    await Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "An unexpected error occurred"
                    ).ExecuteAsync(context);
                }
            });
        });

        return app;
    }
}