using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace EatUp.Helpers
{
    public static class SessionExtensions
    {
        // ===== EXISTENTE (le păstrăm) =====
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            session.SetString(key, json);
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            return json == null ? default : JsonSerializer.Deserialize<T>(json);
        }

        // ===== ALIASURI NOI (pentru CartController) =====
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetObject(key, value);
        }

        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            return session.GetObject<T>(key);
        }
    }
}
