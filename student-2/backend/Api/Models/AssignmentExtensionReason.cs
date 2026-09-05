using System.Text.Json.Serialization;

namespace Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter<AssignmentExtensionReason>))]
public enum AssignmentExtensionReason
{
    UNW,
    ACL,
    NMT,
    FAM,
    CAR,
    REL,
    WRK,
    TEC,
    BRV,
    OTH
}