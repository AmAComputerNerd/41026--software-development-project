using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Services;

internal sealed record CanvasCourseResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("course_code")] string? CourseCode,
    [property: JsonPropertyName("workflow_state")] string WorkflowState
);

internal sealed record CanvasAssignmentResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("course_id")] long CourseId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("due_at")] DateTime? DueAt,
    [property: JsonPropertyName("updated_at")] DateTime? UpdatedAt,
    [property: JsonPropertyName("workflow_state")] string WorkflowState,
    [property: JsonPropertyName("published")] bool Published,
    [property: JsonPropertyName("submission")] CanvasSubmissionResponse? Submission
);

internal sealed record CanvasSubmissionResponse(
    [property: JsonPropertyName("workflow_state")] string WorkflowState,
    [property: JsonPropertyName("submitted_at")] DateTime? SubmittedAt,
    [property: JsonPropertyName("late")] bool Late,
    [property: JsonPropertyName("missing")] bool Missing
);

internal sealed record CanvasUserResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("sis_user_id")] string? SisUserId,
    [property: JsonPropertyName("login_id")] string? LoginId
);

internal sealed record CanvasRecipientResponse(
    [property: JsonPropertyName("id")] JsonElement Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("full_name")] string? FullName,
    [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
    [property: JsonPropertyName("common_courses")] Dictionary<string, string[]>? CommonCourses
);

internal sealed record CanvasConversationResponse(
    [property: JsonPropertyName("id")] JsonElement Id
);

internal sealed record CanvasQuizResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("quiz_type")] string? QuizType,
    [property: JsonPropertyName("time_limit")] int? TimeLimit,
    [property: JsonPropertyName("allowed_attempts")] int? AllowedAttempts,
    [property: JsonPropertyName("question_count")] int? QuestionCount,
    [property: JsonPropertyName("published")] bool? Published,
    [property: JsonPropertyName("locked_for_user")] bool? LockedForUser
);

internal sealed record CanvasQuizSubmissionListResponse(
    [property: JsonPropertyName("quiz_submissions")]
    List<CanvasQuizSubmissionResponse>? QuizSubmissions
);

internal sealed record CanvasQuizSubmissionResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("quiz_id")] long QuizId,
    [property: JsonPropertyName("attempt")] int? Attempt,
    [property: JsonPropertyName("validation_token")] string? ValidationToken,
    [property: JsonPropertyName("workflow_state")] string? WorkflowState
);

internal sealed record CanvasQuizSubmissionQuestionListResponse(
    [property: JsonPropertyName("quiz_submission_questions")]
    List<CanvasQuizSubmissionQuestionResponse>? QuizSubmissionQuestions
);

internal sealed record CanvasQuizSubmissionQuestionResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("question_type")] string? QuestionType,
    [property: JsonPropertyName("question_text")] string? QuestionText,
    [property: JsonPropertyName("answers")] List<CanvasQuizAnswerOptionResponse>? Answers
);

internal sealed record CanvasQuizAnswerOptionResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("html")] string? Html
);
