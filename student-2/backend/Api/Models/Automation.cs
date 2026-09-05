using Api.DTOs;
using Api.Extensions;

namespace Api.Models;

public abstract class Automation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public bool Enabled { get; set; }
    public bool Deleted { get; set; }
    public ICollection<AutomationRun> Runs { get; set; } = [];
    public abstract AutomationDto ToDto();
}

public class AssignmentExtensionAutomation : Automation
{
    public long? SubjectId { get; set; }
    public int BufferMinutes { get; set; }
    public AssignmentExtensionReason Reason { get; set; }
    public string FurtherDetails { get; set; } = string.Empty;

    public override AutomationDto ToDto()
    {
        return new AssignmentExtensionAutomationDto
        {
            Id = Id,
            StudentId = StudentId,
            Enabled = Enabled,
            SubjectId = SubjectId,
            BufferMinutes = BufferMinutes,
            Reason = Reason,
            FurtherDetails = FurtherDetails
        };
    }
}

public class ScheduledPostAutomation : Automation
{
    public DateTime PostTime { get; set; }
    public string ContextCode { get; set; } = string.Empty;
    public string Recipients { get; set; } = "[]";
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool GroupConversation { get; set; }

    public override AutomationDto ToDto()
    {
        return new ScheduledPostAutomationDto
        {
            Id = Id,
            StudentId = StudentId,
            Enabled = Enabled,
            PostTime = PostTime,
            ContextCode = ContextCode,
            Recipients = RecipientJson.Deserialize(Recipients),
            Subject = Subject,
            Body = Body,
            GroupConversation = GroupConversation
        };
    }
}

public class QuizFillerAutomation : Automation
{
    public long? SubjectId { get; set; }
    public bool MultipleChoice { get; set; }
    public bool ShortAnswer { get; set; }
    public int NumberOfAttemptsRequired { get; set; }
    public bool AllowForNoTimeLimit { get; set; }

    public override AutomationDto ToDto()
    {
        return new QuizFillerAutomationDto
        {
            Id = Id,
            StudentId = StudentId,
            Enabled = Enabled,
            SubjectId = SubjectId,
            MultipleChoice = MultipleChoice,
            ShortAnswer = ShortAnswer,
            NumberOfAttemptsRequired = NumberOfAttemptsRequired,
            AllowForNoTimeLimit = AllowForNoTimeLimit
        };
    }
}