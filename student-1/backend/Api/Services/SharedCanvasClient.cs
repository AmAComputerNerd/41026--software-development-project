using System.Net.Http.Json;
using System.Text.Json;
using Api.DTOs;

namespace Api.Services;

public sealed class SharedCanvasClient(HttpClient httpClient) : ISharedCanvasClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<SharedCanvasCourseDto>> GetCoursesAsync(
        CancellationToken cancellationToken)
    {
        return GetAsync<SharedCanvasCourseDto>(
            "api/canvas/courses",
            cancellationToken);
    }

    public Task<IReadOnlyList<SharedCanvasAssignmentDto>> GetAssignmentsAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        return GetAsync<SharedCanvasAssignmentDto>(
            $"api/canvas/courses/{courseId}/assignments",
            cancellationToken);
    }

    private async Task<IReadOnlyList<T>> GetAsync<T>(
        string relativeUrl,
        CancellationToken cancellationToken)
    {
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
}
