using System;
using System.Linq;
using ADOFAI;
using UnityEngine;

namespace CustomEvents.Event
{
    public static class Utils
    {
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

            Debug.LogWarning($"Failed to parse {name} as LevelEventType, defaulting to {defaultValue}");
            return (LevelEventType)defaultValue;
        }
        
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
            
            Debug.LogWarning($"Failed to parse {name} as Category, defaulting to {defaultValue}");
            return (LevelEventCategory)defaultValue;
        }
        
        public static bool IsExtendedEventType(this LevelEventType eventType)
        {
            return CustomEventRegistry.CustomEvents.ContainsKey(eventType);
        }

        public static bool IsVanillaEventType(this LevelEventType eventType)
        {
            return Enum.IsDefined(typeof(LevelEventType), eventType);
        }
        
        public static bool IsExtendedCategory(this LevelEventCategory category)
        {
            return CustomCategoryRegistry.CustomCategories.Any(kv => kv.Value == category);
        }
        
        extension(Enum enumVal)
        {
            public T GetEnumAttributeOfType<T>() where T : Attribute
            {
                var type = enumVal.GetType();
                var memInfo = type.GetMember(enumVal.ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
                return attributes.Length > 0 ? (T)attributes[0] : null;
            }

            public T[] GetEnumAttributesOfType<T>() where T : Attribute
            {
                var type = enumVal.GetType();
                var memInfo = type.GetMember(enumVal.ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
                return attributes.Cast<T>().ToArray();
            }
        }
    }
}