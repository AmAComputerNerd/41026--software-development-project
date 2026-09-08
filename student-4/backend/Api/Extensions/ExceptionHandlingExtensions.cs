using System.Text.Json;
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
