using System;
using System.Linq;
using ADOFAI;

namespace CustomEvents.Event;

/// <summary>Maps the target custom event class to its localized display name metadata.</summary>
/// <remarks>The resolved value is parsed and assigned to <see cref="CustomEventData.NameKey"/>.</remarks>
[AttributeUsage(AttributeTargets.Class)]
public class EventNameAttribute : Attribute
{
    /// <summary>Gets the localization key for the event name.</summary>
    public string NameKey { get; }

    /// <summary>Initializes a new instance of the <see cref="EventNameAttribute"/> class with a specific localization key.</summary>
    /// <param name="nameKey">The localization lookup key for the event name display string.</param>
    public EventNameAttribute(string nameKey)
    {
        NameKey = nameKey;
    }
}

/// <summary>Maps the target custom event class to one or more editor categories.</summary>
/// <remarks>The engine filters out internal terms before storing the remaining strings inside <see cref="CustomEventData.Categories"/>.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EventCategoryAttribute : Attribute
{
    /// <summary>Gets the valid category names declared for the event.</summary>
    public string[] Categories { get; }

    /// <summary>Initializes a new instance of the <see cref="EventCategoryAttribute"/> class with specific category tags.</summary>
    /// <param name="categories">The category identifier strings to assign to the event layout template.</param>
    public EventCategoryAttribute(params string[] categories)
    {
        Categories = categories.Where(c => c != "Jank" && c != "Favorites").ToArray();
    }
}

/// <summary>Defines an inspector layout section block for grouping properties within the target custom event class.</summary>
/// <remarks>The parser builds a array stored inside <see cref="CustomEventData.Groups"/> to split properties into clean visual containers.</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EventPropGroupDefinitionAttribute : Attribute
{
    /// <summary>Gets the display header name for the property group container.</summary>
    public string Name { get; }

    /// <summary>Gets the local asset path pointing to the texture file used for the group's visual icon.</summary>
    public string IconPath { get; }

    /// <summary>Gets a value that indicates whether properties default to this specific section container if no alternative group is assigned.</summary>
    public bool IsDefault { get; }

    /// <summary>Initializes a new instance of the <see cref="EventPropGroupDefinitionAttribute"/> class.</summary>
    /// <param name="name">The structural display header name for the property group container.</param>
    /// <param name="iconPath">The local asset path pointing to the texture file used for the group's visual icon.</param>
    /// <param name="isDefault">Indicates whether properties default to this section container if no alternative group is assigned.</param>
    public EventPropGroupDefinitionAttribute(string name, string iconPath = null, bool isDefault = false)
    {
        Name = name;
        IconPath = iconPath;
        IsDefault = isDefault;
    }
}