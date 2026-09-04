using System.Text.Json;

namespace Api.Extensions;

public static class RecipientJson
{
    public static string Serialize(IReadOnlyList<string> recipients)
    {
        return JsonSerializer.Serialize(recipients);
    }

    public static string[] Deserialize(string recipients)
    {
        return JsonSerializer.Deserialize<string[]>(recipients) ?? [];
    }
}