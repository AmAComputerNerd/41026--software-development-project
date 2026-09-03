using System.Net.Http.Json;
using System.Text.Json;
using GradesManager.Configuration;
using GradesManager.DTOs;
using Microsoft.Extensions.Options;

namespace GradesManager.Services;

public sealed class SharedCanvasClient(
    HttpClient httpClient,
    IOptions<SharedServiceOptions> options) : ISharedCanvasClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SharedServiceOptions _options = options.Value;

    public Task<IReadOnlyList<SharedCanvasCourseDto>> GetCoursesAsync(
        CancellationToken cancellationToken)
    {
        return GetAsync<SharedCanvasCourseDto>(
            "GradesManager/canvas/courses",
            cancellationToken);
    }

    public Task<IReadOnlyList<SharedCanvasAssignmentDto>> GetAssignmentsAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return GetAsync<SharedCanvasAssignmentDto>(
            $"GradesManager/canvas/courses/{courseId}/assignments",
            cancellationToken);
    }

    private async Task<IReadOnlyList<T>> GetAsync<T>(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        ConfigureClient();

        using var response = await httpClient.GetAsync(relativeUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new SharedServiceException(
                $"The shared service returned HTTP {(int)response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<List<T>>(
            JsonOptions,
            cancellationToken) ?? [];
    }

    private void ConfigureClient()
    {
        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new SharedServiceConfigurationException(
                "SharedService:BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        httpClient.BaseAddress ??= new Uri(
            baseUri.AbsoluteUri.EndsWith('/') ? baseUri.AbsoluteUri : $"{baseUri.AbsoluteUri}/");
    }
}
