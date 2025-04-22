using System.Text.Json;

public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetObject<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        if (string.IsNullOrEmpty(value)) return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException)
        {
            session.Remove(key);
            return default;
        }
    }
}
