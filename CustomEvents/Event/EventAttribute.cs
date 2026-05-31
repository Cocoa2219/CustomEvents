using System;
using System.Linq;

namespace CustomEvents.Event;

[AttributeUsage(AttributeTargets.Class)]
public class EventNameAttribute : Attribute
{
    public string NameKey { get; }

    public EventNameAttribute(string nameKey)
    {
        NameKey = nameKey;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EventCategoryAttribute : Attribute
{
    public string[] Categories { get; }

    public EventCategoryAttribute(params string[] categories)
    {
        Categories = categories.Where(c => c != "Jank" && c != "Favorites").ToArray();
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EventPropGroupDefinitionAttribute(string name, string iconPath = null, bool isDefault = false)
    : Attribute
{
    public string Name { get; } = name;
    public string IconPath { get; } = iconPath;
    public bool IsDefault { get; } = isDefault;
}