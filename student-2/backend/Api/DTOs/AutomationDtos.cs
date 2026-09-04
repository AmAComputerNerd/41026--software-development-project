using Api.Extensions;
using Api.Models;

namespace Api.DTOs;

public abstract class AutomationDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public bool Enabled { get; init; }
}

[AutomationDiscriminator("assignmentExtension")]
public sealed class AssignmentExtensionAutomationDto : AutomationDto
{
    public long? SubjectId { get; init; }
    public int BufferMinutes { get; init; }
    public AssignmentExtensionReason Reason { get; init; }
    public string FurtherDetails { get; init; } = string.Empty;
}

[AutomationDiscriminator("scheduledPost")]
public sealed class ScheduledPostAutomationDto : AutomationDto
{
    public DateTime PostTime { get; init; }
    public string ContextCode { get; init; } = string.Empty;
    public IReadOnlyList<string> Recipients { get; init; } = [];
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool GroupConversation { get; init; }
}

[AutomationDiscriminator("quizFiller")]
public sealed class QuizFillerAutomationDto : AutomationDto
{
    public long? SubjectId { get; init; }
    public bool MultipleChoice { get; init; }
    public bool ShortAnswer { get; init; }
    public int NumberOfAttemptsRequired { get; init; }
    public bool AllowForNoTimeLimit { get; init; }
}

public abstract class SaveAutomationRequestDto
{
    public Guid StudentId { get; init; }
    public bool Enabled { get; init; }

    public string? Validate()
    {
        return StudentId == Guid.Empty ? "`studentId` is required." : ValidateValues();
    }

    public abstract bool CanApplyTo(Automation automation);
    public abstract Automation CreateAutomation();
    public abstract void ApplyTo(Automation automation);
    protected abstract string? ValidateValues();
}

public abstract class SaveAutomationRequestDto<TAutomation> : SaveAutomationRequestDto
    where TAutomation : Automation, new()
{
    public sealed override bool CanApplyTo(Automation automation)
    {
        return automation is TAutomation;
    }

    public sealed override Automation CreateAutomation()
    {
        var automation = new TAutomation();
        ApplyTo(automation);
        return automation;
    }

    public sealed override void ApplyTo(Automation automation)
    {
        if (automation is not TAutomation typedAutomation)
        {
            throw new InvalidOperationException("An automation's type cannot be changed.");
        }

        typedAutomation.StudentId = StudentId;
        typedAutomation.Enabled = Enabled;
        ApplyValues(typedAutomation);
    }

    protected abstract void ApplyValues(TAutomation automation);
}

[AutomationDiscriminator("assignmentExtension")]
public sealed class SaveAssignmentExtensionAutomationRequestDto
    : SaveAutomationRequestDto<AssignmentExtensionAutomation>
{
    public long? SubjectId { get; init; }
    public int BufferMinutes { get; init; }
    public AssignmentExtensionReason Reason { get; init; }
    public string FurtherDetails { get; init; } = string.Empty;

    protected override string? ValidateValues()
    {
        if (SubjectId <= 0)
        {
            return "`subjectId` must be a positive Canvas course ID or null.";
        }

        if (BufferMinutes < 0)
        {
            return "`bufferMinutes` must be zero or greater.";
        }

        return FurtherDetails.Length > 2000
            ? "`furtherDetails` cannot exceed 2000 characters."
            : null;
    }

    protected override void ApplyValues(AssignmentExtensionAutomation automation)
    {
        automation.SubjectId = SubjectId;
        automation.BufferMinutes = BufferMinutes;
        automation.Reason = Reason;
        automation.FurtherDetails = FurtherDetails.Trim();
    }
}

[AutomationDiscriminator("scheduledPost")]
public sealed class SaveScheduledPostAutomationRequestDto
    : SaveAutomationRequestDto<ScheduledPostAutomation>
{
    public DateTime PostTime { get; init; }
    public string ContextCode { get; init; } = string.Empty;
    public IReadOnlyList<string> Recipients { get; init; } = [];
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool GroupConversation { get; init; }

    protected override string? ValidateValues()
    {
        if (PostTime == default)
        {
            return "`postTime` is required.";
        }

        if (!TryGetCourseId(ContextCode, out _))
        {
            return "`contextCode` must identify a Canvas course, for example `course_123`.";
        }

        if (Recipients.Count == 0 || Recipients.Any(recipient => !IsCanvasUserId(recipient)))
        {
            return "At least one valid Canvas numeric or `uuid:` recipient ID is required.";
        }

        if (Subject.Length > 255)
        {
            return "`subject` cannot exceed 255 characters.";
        }

        return string.IsNullOrWhiteSpace(Body) || Body.Length > 10000
            ? "`body` must contain between 1 and 10000 characters."
            : null;
    }

    protected override void ApplyValues(ScheduledPostAutomation automation)
    {
        automation.PostTime = PostTime;
        automation.ContextCode = ContextCode;
        automation.Recipients = RecipientJson.Serialize(Recipients.Distinct().ToArray());
        automation.Subject = Subject.Trim();
        automation.Body = Body.Trim();
        automation.GroupConversation = GroupConversation;
    }

    private static bool TryGetCourseId(string contextCode, out long courseId)
    {
        const string prefix = "course_";
        courseId = 0;
        return contextCode.StartsWith(prefix, StringComparison.Ordinal) &&
            long.TryParse(contextCode[prefix.Length..], out courseId) &&
            courseId > 0;
    }

    private static bool IsCanvasUserId(string recipient)
    {
        if (long.TryParse(recipient, out var numericId))
        {
            return numericId > 0;
        }

        const string uuidPrefix = "uuid:";
        return recipient.StartsWith(uuidPrefix, StringComparison.Ordinal) &&
            recipient.Length > uuidPrefix.Length;
    }
}

[AutomationDiscriminator("quizFiller")]
public sealed class SaveQuizFillerAutomationRequestDto
    : SaveAutomationRequestDto<QuizFillerAutomation>
{
    public long? SubjectId { get; init; }
    public bool MultipleChoice { get; init; }
    public bool ShortAnswer { get; init; }
    public int NumberOfAttemptsRequired { get; init; }
    public bool AllowForNoTimeLimit { get; init; }

    protected override string? ValidateValues()
    {
        if (SubjectId <= 0)
        {
            return "`subjectId` must be a positive Canvas course ID or null.";
        }

        if (!MultipleChoice && !ShortAnswer)
        {
            return "Select at least one question type to fill in.";
        }

        return NumberOfAttemptsRequired < 1
            ? "`numberOfAttemptsRequired` must be one or greater."
            : null;
    }

    protected override void ApplyValues(QuizFillerAutomation automation)
    {
        automation.SubjectId = SubjectId;
        automation.MultipleChoice = MultipleChoice;
        automation.ShortAnswer = ShortAnswer;
        automation.NumberOfAttemptsRequired = NumberOfAttemptsRequired;
        automation.AllowForNoTimeLimit = AllowForNoTimeLimit;
    }
}

public abstract class AutomationRunDto
{
    public Guid Id { get; init; }
    public Guid AutomationId { get; init; }
    public DateTime ExecutionTimeStamp { get; init; }
    public string Result { get; init; } = string.Empty;
}

[AutomationDiscriminator("assignmentExtension")]
public sealed class AssignmentExtensionAutomationRunDto : AutomationRunDto
{
    public string AssignmentId { get; init; } = string.Empty;
}

[AutomationDiscriminator("scheduledPost")]
public sealed class ScheduledPostAutomationRunDto : AutomationRunDto
{
    public DateTime PostTime { get; init; }
    public string ContextCode { get; init; } = string.Empty;
    public IReadOnlyList<string> Recipients { get; init; } = [];
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool GroupConversation { get; init; }
}

[AutomationDiscriminator("quizFiller")]
public sealed class QuizFillerAutomationRunDto : AutomationRunDto
{
    public long CourseId { get; init; }
    public long QuizId { get; init; }
    public string QuizTitle { get; init; } = string.Empty;
    public int QuestionCount { get; init; }
}