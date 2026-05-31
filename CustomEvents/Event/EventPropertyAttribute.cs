using System;
using ADOFAI;
using UnityEngine;

namespace CustomEvents.Event;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class EventPropertyAttribute : Attribute
{
    public string Name { get; set; }
    public bool IsEnabled { get; set; } = true;
    
    public string Unit { get; set; }
    public string Placeholder { get; set; }
    
    public bool IsInvisible { get; set; }
    public bool IgnoreRange { get; set; }
    
    public bool IsDevOnly { get; set; }

    public bool ShouldEncode { get; set; } = true;
    
    public bool AffectsFloors { get; set; }
    public bool AffectsPath { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyGroupAttribute : Attribute
{
    public string Name { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class PropertyLabelAttribute : Attribute
{
    public string CustomLabel { get; }
    public string LocalizationKey { get; set; }
    
    public PropertyLabelAttribute() { }
    
    public PropertyLabelAttribute(string customLabel)
    {
        CustomLabel = customLabel;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyIntAttribute(int min = int.MinValue, int max = int.MaxValue) : Attribute
{
    public int Min { get; set; } = min;
    public int Max { get; set; } = max;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyFloatAttribute(float min = float.MinValue, float max = float.MaxValue) : Attribute
{
    public float Min { get; set; } = min;
    public float Max { get; set; } = max;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyStringAttribute(int minLength = int.MinValue, int maxLength = int.MaxValue, bool needsUnicode = true, bool localizable = false) : Attribute
{
    public int MinLength { get; set; } = minLength;
    public int MaxLength { get; set; } = maxLength;
    public bool NeedsUnicode { get; set; } = needsUnicode;
    public bool Localizable { get; set; } = localizable;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertySliderAttribute : Attribute
{
    public PropertySliderAttribute(float min = float.MinValue, float max = float.MaxValue)
    {
        Min = min;
        Max = max;
    }
    
    public float Min { get; set; }
    public float Max { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyDropdownAttribute : Attribute
{
    public string[] Options { get; set; }
    
    // Enum Dropdown
    public PropertyDropdownAttribute()
    {
        Options = [];
    }
    
    public PropertyDropdownAttribute(params string[] options)
    {
        Options = options;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyColorPickerAttribute(bool useAlpha = false) : Attribute
{
    public bool UseAlpha { get; set; } = useAlpha;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyFilePickerAttribute(FileType type = FileType.Audio) : Attribute
{
    public FileType Type { get; set; } = type;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyFloatPairAttribute(float min = int.MinValue, float max = int.MaxValue, float step = 0.1f, bool isRange = true) : Attribute
{
    public float Min { get; set; } = min;
    public float Max { get; set; } = max;
    
    [Obsolete("Unused in basegame")]
    public float Step { get; set; } = step;
    
    public bool IsRange { get; set; } = isRange;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyNoteAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyRatingAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyLongInputFieldAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyEnumAttribute(params string[] exclusions) : Attribute
{
    public string[] Exclusions { get; set; } = exclusions;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyTileAttribute(int min = int.MinValue, int max = int.MaxValue) : Attribute
{
    public int Min { get; set; } = min;
    public int Max { get; set; } = max;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyVector2Attribute(
    float minX = float.NegativeInfinity,
    float maxX = float.PositiveInfinity,
    float minY = float.NegativeInfinity,
    float maxY = float.PositiveInfinity,
    bool allowEmpty = false) : Attribute
{
    public Vector2 Min { get; set; } = new Vector2(minX, minY);
    public Vector2 Max { get; set; } = new Vector2(maxX, maxY);
    public bool AllowEmpty { get; set; } = allowEmpty;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyVector2RangeAttribute(bool isRange = true) : Attribute
{
    public bool IsRange { get; set; } = isRange;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyToggleableAttribute(bool isOnByDefault = false) : Attribute
{
    public bool IsOnByDefault { get; set; } = isOnByDefault;
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class PropertyShowIfAttribute : Attribute
{
    public string ConditionPropertyName { get; set; }
    public string ExpectedValue { get; set; }

    public PropertyShowIfAttribute(string conditionPropertyName, string expectedValue)
    {
        ConditionPropertyName = conditionPropertyName;
        ExpectedValue = expectedValue;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class PropertyEnableIfAttribute : Attribute
{
    public string ConditionPropertyName { get; set; }
    public string ExpectedValue { get; set; }

    public PropertyEnableIfAttribute(string conditionPropertyName, string expectedValue)
    {
        ConditionPropertyName = conditionPropertyName;
        ExpectedValue = expectedValue;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class PropertyHideIfAttribute : Attribute
{
    public string ConditionPropertyName { get; set; }
    public string ExpectedValue { get; set; }

    public PropertyHideIfAttribute(string conditionPropertyName, string expectedValue)
    {
        ConditionPropertyName = conditionPropertyName;
        ExpectedValue = expectedValue;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class PropertyDisableIfAttribute : Attribute
{
    public string ConditionPropertyName { get; set; }
    public string ExpectedValue { get; set; }

    public PropertyDisableIfAttribute(string conditionPropertyName, string expectedValue)
    {
        ConditionPropertyName = conditionPropertyName;
        ExpectedValue = expectedValue;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyRequiredAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyHelpAttribute : Attribute
{
    public string TextKey { get; set; }
    public string ButtonURL { get; set; }
    public string ButtonTextKey { get; set; }
    
    public PropertyHelpAttribute(string textKey)
    {
        TextKey = textKey;
    }
}
