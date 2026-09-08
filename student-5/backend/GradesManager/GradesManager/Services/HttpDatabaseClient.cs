using GradesManager.Contracts;
using System.Net.Http.Json;
using System.Text.Json;

namespace GradesManager.Services;

public sealed class HttpDatabaseClient : IDatabaseClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpDatabaseClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public HttpDatabaseClient(HttpClient httpClient, ILogger<HttpDatabaseClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // Courses
    public async Task<IReadOnlyList<CourseRecord>?> GetCoursesAsync(
        bool includeInactiveCanvas = false,
        CancellationToken cancellationToken = default)
    {
        var query = includeInactiveCanvas ? "?includeInactiveCanvas=true" : "";
        var response = await _httpClient.GetAsync($"/internal/courses{query}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<CourseRecord>>(_jsonOptions, cancellationToken);
    }

    public async Task<CourseRecord?> GetCourseAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/internal/courses/{id}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CourseRecord>(_jsonOptions, cancellationToken);
    }

    public async Task<CourseRecord?> CreateCourseAsync(
        CreateCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/internal/courses", command, _jsonOptions, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CourseRecord>(_jsonOptions, cancellationToken);
    }

    public async Task<CourseRecord?> UpdateCourseAsync(
        Guid id,
        UpdateCourseCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"/internal/courses/{id}", command, _jsonOptions, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CourseRecord>(_jsonOptions, cancellationToken);
    }

    public async Task<bool> DeleteCourseAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/internal/courses/{id}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        
        response.EnsureSuccessStatusCode();
        return true;
    }

    // Students
    public async Task<IReadOnlyList<StudentRecord>?> GetStudentsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/internal/students", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<StudentRecord>>(_jsonOptions, cancellationToken);
    }

    public async Task<StudentRecord?> GetStudentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/internal/students/{id}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StudentRecord>(_jsonOptions, cancellationToken);
    }

    public async Task<StudentRecord?> CreateStudentAsync(
        CreateStudentCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/internal/students", command, _jsonOptions, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StudentRecord>(_jsonOptions, cancellationToken);
    }

    public async Task<StudentRecord?> UpdateStudentAsync(
        Guid id,
        UpdateStudentCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"/internal/students/{id}", command, _jsonOptions, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StudentRecord>(_jsonOptions, cancellationToken);
    }

    public async Task<bool> DeleteStudentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/internal/students/{id}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        
        response.EnsureSuccessStatusCode();
        return true;
    }

    // Assignments
    public async Task<IReadOnlyList<AssignmentRecord>?> GetAssignmentsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/internal/assignments", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<AssignmentRecord>>(_jsonOptions, cancellationToken);
    }

    public async Task<AssignmentRecord?> GetAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/internal/assignments/{id}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AssignmentRecord>(_jsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<AssignmentRecord>?> GetAssignmentsByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/internal/assignments/student/{studentId}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<AssignmentRecord>>(_jsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<AssignmentRecord>?> GetAssignmentsByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/internal/assignments/course/{courseId}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<AssignmentRecord>>(_jsonOptions, cancellationToken);
    }

    public async Task<AssignmentRecord?> CreateAssignmentAsync(
        CreateAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/internal/assignments", command, _jsonOptions, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AssignmentRecord>(_jsonOptions, cancellationToken);
    }

    public async Task<AssignmentRecord?> UpdateAssignmentAsync(
        Guid id,
        UpdateAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"/internal/assignments/{id}", command, _jsonOptions, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AssignmentRecord>(_jsonOptions, cancellationToken);
    }

    public async Task<bool> DeleteAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/internal/assignments/{id}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        
        response.EnsureSuccessStatusCode();
        return true;
    }

    // Student Assignments
    public async Task<IReadOnlyList<StudentAssignmentRecord>?> GetStudentMarksAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/internal/student-assignments/student/{studentId}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<StudentAssignmentRecord>>(_jsonOptions, cancellationToken);
    }

    public async Task<StudentAssignmentRecord?> CreateStudentAssignmentAsync(
        CreateStudentAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/internal/student-assignments", command, _jsonOptions, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StudentAssignmentRecord>(_jsonOptions, cancellationToken);
    }

    public async Task<StudentAssignmentRecord?> UpdateStudentAssignmentAsync(
        UpdateStudentAssignmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("/internal/student-assignments", command, _jsonOptions, cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StudentAssignmentRecord>(_jsonOptions, cancellationToken);
    }

    public async Task<bool> DeleteStudentAssignmentAsync(
        Guid studentId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/internal/student-assignments/{studentId}/{assignmentId}", cancellationToken);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return false;
        }
        
        response.EnsureSuccessStatusCode();
        return true;
    }
}