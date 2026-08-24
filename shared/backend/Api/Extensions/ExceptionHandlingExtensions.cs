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

                var (status, title) = exception switch
                {
                    CanvasConfigurationException =>
                        (StatusCodes.Status503ServiceUnavailable, "Canvas is not configured"),
                    CanvasApiException { StatusCode: System.Net.HttpStatusCode.TooManyRequests } =>
                        (StatusCodes.Status503ServiceUnavailable, "Canvas rate limit exceeded"),
                    CanvasApiException =>
                        (StatusCodes.Status502BadGateway, "Canvas request failed"),
                    HttpRequestException =>
                        (StatusCodes.Status502BadGateway, "Canvas is unavailable"),
                    JsonException =>
                        (StatusCodes.Status502BadGateway, "Canvas returned an invalid response"),
                    _ =>
                        (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
                };

                context.Response.StatusCode = status;
                await Results.Problem(statusCode: status, title: title).ExecuteAsync(context);
            });
        });

        return app;
    }
}
