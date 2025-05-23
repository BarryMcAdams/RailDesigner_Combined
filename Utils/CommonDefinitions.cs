// Utils/CommonDefinitions.cs
using System.Collections.Generic; // Required for IDictionary

namespace RailDesigner1
{
    public enum ComponentType
    {
        Post,
        Picket,
        TopRail,
        BottomRail,
        IntermediateRail,
        HandRail,
        Mounting,
        UserDefined // Keep UserDefined if it was used or intended
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        // Overload for string specifically for convenience, leveraging the generic version.
        // This can be useful if type inference isn't clear or if explicit call is preferred.
        public static string GetValueOrDefault(this IDictionary<string, string> dictionary, string key, string defaultValue)
        {
             return dictionary.TryGetValue(key, out string value) ? value : defaultValue;
        }
    }

    // Placeholder for other common definitions if needed in the future
}
```
