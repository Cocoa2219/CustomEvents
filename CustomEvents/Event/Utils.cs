using System;
using System.Linq;
using ADOFAI;

namespace CustomEvents.Event
{
    /// <summary>Provides helper methods for event type and category conversion.</summary>
    public static class Utils
    {
        /// <summary>Converts a <see cref="LevelEventType"/> to its canonical string representation.</summary>
        public static string EventTypeToString(LevelEventType eventType)
        {
            if (Enum.IsDefined(typeof(LevelEventType), eventType))
            {
                return eventType.ToString();
            }
            
            if (CustomEventRegistry.CustomEvents.TryGetValue(eventType, out var customEvent) && customEvent != null)
            {
                return customEvent.FullName;
            }

            return eventType.ToString();
        }

        /// <summary>Parses a <see cref="LevelEventType"/> name while falling back to a default value.</summary>
        public static LevelEventType LooselyParseEventType(string name, int defaultValue)
        {
            if (Enum.TryParse(name, true, out LevelEventType result))
            {
                return result;
            }
            
            if (CustomEventRegistry.NameToIdCache.TryGetValue(name, out var customEventId))
            {
                return customEventId;
            }

            return (LevelEventType)defaultValue;
        }
        
        /// <summary>Parses a <see cref="LevelEventCategory"/> name while optionally registering custom categories.</summary>
        public static LevelEventCategory LooselyParseCategory(string name, int defaultValue, bool addIfNotFound = false)
        {
            if (Enum.TryParse(name, true, out LevelEventCategory result))
            {
                return result;
            }

            var customCategoryValue = CustomCategoryRegistry.GetCustomCategoryValue(name);
            if (customCategoryValue != null)
            {
                return customCategoryValue.Value;
            }
            
            if (addIfNotFound)
            {
                return CustomCategoryRegistry.RegisterCustomCategory(name);
            }
            
            return (LevelEventCategory)defaultValue;
        }
        
        /// <summary>Determines whether <paramref name="eventType"/> comes from <see cref="CustomEventRegistry"/>.</summary>
        public static bool IsExtendedEventType(this LevelEventType eventType)
        {
            return CustomEventRegistry.CustomEvents.ContainsKey(eventType);
        }

        /// <summary>Determines whether <paramref name="eventType"/> is a built-in <see cref="LevelEventType"/> value.</summary>
        public static bool IsVanillaEventType(this LevelEventType eventType)
        {
            return Enum.IsDefined(typeof(LevelEventType), eventType);
        }
        
        /// <summary>Determines whether <paramref name="category"/> comes from <see cref="CustomCategoryRegistry"/>.</summary>
        public static bool IsExtendedCategory(this LevelEventCategory category)
        {
            return CustomCategoryRegistry.CustomCategories.Any(kv => kv.Value == category);
        }
    }
}