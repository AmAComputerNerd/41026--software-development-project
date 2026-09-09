using GradesManager.Contracts;

namespace GradesManager.Services;

public interface IDatabaseClient
{
    // Courses
    Task<IReadOnlyList<CourseRecord>?> GetCoursesAsync(
        bool includeInactiveCanvas = false,
        CancellationToken cancellationToken = default);

    Task<CourseRecord?> GetCourseAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CourseRecord?> CreateCourseAsync(
        CreateCourseCommand command,
        CancellationToken cancellationToken = default);

    Task<CourseRecord?> UpdateCourseAsync(
        Guid id,
        UpdateCourseCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteCourseAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Students
    Task<IReadOnlyList<StudentRecord>?> GetStudentsAsync(
        CancellationToken cancellationToken = default);

    Task<StudentRecord?> GetStudentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StudentRecord?> CreateStudentAsync(
        CreateStudentCommand command,
        CancellationToken cancellationToken = default);

    Task<StudentRecord?> UpdateStudentAsync(
        Guid id,
        UpdateStudentCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteStudentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Assignments
    Task<IReadOnlyList<AssignmentRecord>?> GetAssignmentsAsync(
        CancellationToken cancellationToken = default);

    Task<AssignmentRecord?> GetAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssignmentRecord>?> GetAssignmentsByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssignmentRecord>?> GetAssignmentsByCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task<AssignmentRecord?> CreateAssignmentAsync(
        CreateAssignmentCommand command,
        CancellationToken cancellationToken = default);

    Task<AssignmentRecord?> UpdateAssignmentAsync(
        Guid id,
        UpdateAssignmentCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Student Assignments
    Task<IReadOnlyList<StudentAssignmentRecord>?> GetStudentMarksAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    Task<StudentAssignmentRecord?> CreateStudentAssignmentAsync(
        CreateStudentAssignmentCommand command,
        CancellationToken cancellationToken = default);

    Task<StudentAssignmentRecord?> UpdateStudentAssignmentAsync(
        UpdateStudentAssignmentCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteStudentAssignmentAsync(
        Guid studentId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);
}