using Api.DTOs;
using Api.Models;

namespace Api.Extensions;

public static class DtoExtensions
{
    public static NotificationDto ToDto(this Notification notification)
    {
        return new NotificationDto(
            Id: notification.Id,
            StudentId: notification.StudentId,
            Type: notification.Type.ToString(),
            SourceMicroservice: notification.SourceMicroservice,
            Message: notification.Message,
            IsRead: notification.IsRead,
            CreatedAtUtc: notification.CreatedAtUtc,
            RelatedEntityType: notification.RelatedEntityType,
            RelatedEntityId: notification.RelatedEntityId,
            ActionPayload: notification.ActionPayload
        );
    }

    public static NotificationPreferenceDto ToDto(this NotificationPreference preference)
    {
        return new NotificationPreferenceDto(
            Id: preference.Id,
            StudentId: preference.StudentId,
            Type: preference.Type.ToString(),
            Channel: preference.Channel.ToString(),
            Enabled: preference.Enabled,
            UpdatedAtUtc: preference.UpdatedAtUtc
        );
    }

    public static AiDigestDto ToDto(this AiDigest digest)
    {
        return new AiDigestDto(
            Id: digest.Id,
            StudentId: digest.StudentId,
            Summary: digest.Summary,
            GeneratedAtUtc: digest.GeneratedAtUtc
        );
    }
}
