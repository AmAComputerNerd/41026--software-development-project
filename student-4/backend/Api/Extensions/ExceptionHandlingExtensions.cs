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
                if (exception is BadHttpRequestException { InnerException: JsonException })
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
                else if (exception is McpIntegrationDisabledException or McpServiceException or
                    RagIntegrationDisabledException or RagServiceException)
                {
                    var disabled = exception is McpIntegrationDisabledException or RagIntegrationDisabledException;
                    var service = exception is RagIntegrationDisabledException or RagServiceException ? "RAG" : "MCP";
                    await Results.Problem(
                        statusCode: disabled
                            ? StatusCodes.Status503ServiceUnavailable
                            : StatusCodes.Status502BadGateway,
                        title: "AI assistant unavailable",
                        detail: disabled
                            ? $"{service} integration is disabled for this environment."
                            : $"The {service} server could not complete the request. Ensure the {service} service is running and try again."
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
