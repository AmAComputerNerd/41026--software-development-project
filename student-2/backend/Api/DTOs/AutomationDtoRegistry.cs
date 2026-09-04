using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Api.DTOs;

public static class AutomationDtoRegistry
{
    private static readonly Type[] BaseTypes =
    [
        typeof(AutomationDto),
        typeof(SaveAutomationRequestDto),
        typeof(AutomationRunDto)
    ];

    private static readonly IReadOnlyDictionary<Type, IReadOnlyList<Type>> DerivedTypes = BaseTypes
        .ToDictionary(
            baseType => baseType,
            baseType => (IReadOnlyList<Type>)baseType.Assembly
                .GetTypes()
                .Where(type => !type.IsAbstract && baseType.IsAssignableFrom(type))
                .Where(type => type.GetCustomAttribute<AutomationDiscriminatorAttribute>() is not null)
                .ToArray());

    public static IReadOnlyList<Type> GetDerivedTypes(Type baseType)
    {
        return DerivedTypes.GetValueOrDefault(baseType) ?? [];
    }

    public static string GetDiscriminator(Type type)
    {
        return type.GetCustomAttribute<AutomationDiscriminatorAttribute>()?.Value
            ?? throw new InvalidOperationException($"{type.Name} has no automation discriminator.");
    }

    public static void ConfigureJsonTypeInfo(JsonTypeInfo typeInfo)
    {
        var derivedTypes = GetDerivedTypes(typeInfo.Type);
        if (derivedTypes.Count == 0)
        {
            return;
        }

        typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "$type",
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
        };

        foreach (var derivedType in derivedTypes)
        {
            typeInfo.PolymorphismOptions.DerivedTypes.Add(
                new JsonDerivedType(derivedType, GetDiscriminator(derivedType)));
        }
    }
}