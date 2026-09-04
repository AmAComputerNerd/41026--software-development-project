using Api.DTOs;
using Api.Extensions;

namespace Api.Models;

public abstract class AutomationRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AutomationId { get; set; }
    public string ExecutionKey { get; set; } = string.Empty;
    public DateTime ExecutionTimeStamp { get; set; }
    public string Result { get; set; } = string.Empty;
    public Automation Automation { get; set; } = null!;
    public abstract AutomationRunDto ToDto();
}

public class AssignmentExtensionAutomationRun : AutomationRun
{
    public string AssignmentId { get; set; } = string.Empty;

    public override AutomationRunDto ToDto()
    {
        return new AssignmentExtensionAutomationRunDto
        {
            Id = Id,
            AutomationId = AutomationId,
            ExecutionTimeStamp = ExecutionTimeStamp,
            Result = Result,
            AssignmentId = AssignmentId
        };
    }
}

public class ScheduledPostAutomationRun : AutomationRun
{
    public DateTime PostTime { get; set; }
    public string ContextCode { get; set; } = string.Empty;
    public string Recipients { get; set; } = "[]";
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool GroupConversation { get; set; }

    public override AutomationRunDto ToDto()
    {
        return new ScheduledPostAutomationRunDto
        {
            Id = Id,
            AutomationId = AutomationId,
            ExecutionTimeStamp = ExecutionTimeStamp,
            Result = Result,
            PostTime = PostTime,
            ContextCode = ContextCode,
            Recipients = RecipientJson.Deserialize(Recipients),
            Subject = Subject,
            Body = Body,
            GroupConversation = GroupConversation
        };
    }
}

public class QuizFillerAutomationRun : AutomationRun
{
    public long CourseId { get; set; }
    public long QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int QuestionCount { get; set; }

    public override AutomationRunDto ToDto()
    {
        return new QuizFillerAutomationRunDto
        {
            Id = Id,
            AutomationId = AutomationId,
            ExecutionTimeStamp = ExecutionTimeStamp,
            Result = Result,
            CourseId = CourseId,
            QuizId = QuizId,
            QuizTitle = QuizTitle,
            QuestionCount = QuestionCount
        };
    }
}

public static class AutomationRunResult
{
    public const string Running = "RUN";
    public const string Success = "SUC";
    public const string Failed = "FAI";
}