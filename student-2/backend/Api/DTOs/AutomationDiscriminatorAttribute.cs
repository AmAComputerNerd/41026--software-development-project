namespace Api.DTOs;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AutomationDiscriminatorAttribute(string value) : Attribute
{
    public string Value { get; } = value;
}