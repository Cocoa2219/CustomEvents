using System;
using ADOFAI;
using UnityEngine;

namespace CustomEvents.Event;

/// <summary>Specifies how a custom event member appears and behaves when parsed into a <see cref="CustomProperty"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class EventPropertyAttribute : Attribute
{
    /// <summary>Gets or sets the property name override used when initializing the <see cref="CustomProperty"/>.</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the visual unit label displayed alongside the property field.</summary>
    public string Unit { get; set; }

    /// <summary>Gets or sets the placeholder localization key shown when the property value is empty.</summary>
    public string Placeholder { get; set; }

    /// <summary>Gets or sets a value that indicates whether the generated property is hidden in the inspector view.</summary>
    public bool IsInvisible { get; set; }

    /// <summary>Gets or sets a value that indicates whether range validation constraints are bypassed during serialization.</summary>
    public bool IgnoreRange { get; set; }

    /// <summary>Gets or sets a value that indicates whether the property is development-only.</summary>
    /// <remarks>Dev-only properties are hidden in release builds (including frontline builds).</remarks>
    public bool IsDevOnly { get; set; }

    /// <summary>Gets or sets a value that indicates whether the value is encoded when level data is saved.</summary>
    public bool ShouldEncode { get; set; } = true;

    /// <summary>Gets or sets a value that indicates whether changes to this property affect floor layout data structures.</summary>
    public bool AffectsFloors { get; set; }

    /// <summary>Gets or sets a value that indicates whether changes to this property alter the calculated movement path.</summary>
    public bool AffectsPath { get; set; }
}

/// <summary>Assigns the generated <see cref="CustomProperty"/> to a specific inspector group layout block.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyGroupAttribute : Attribute
{
    /// <summary>Gets or sets the target group name.</summary>
    public string Name { get; set; }
}

/// <summary>Defines custom display text or localization behavior for the generated <see cref="CustomProperty"/> label.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class PropertyLabelAttribute : Attribute
{
    /// <summary>Gets the custom literal label text.</summary>
    public string CustomLabel { get; }

    /// <summary>Gets or sets the localization key for the property label.</summary>
    public string LocalizationKey { get; set; }

    /// <summary>Initializes a new instance of the <see cref="PropertyLabelAttribute" /> class.</summary>
    public PropertyLabelAttribute()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PropertyLabelAttribute" /> class with literal text.</summary>
    /// <param name="customLabel">The literal label text to use.</param>
    public PropertyLabelAttribute(string customLabel)
    {
        CustomLabel = customLabel;
    }
}

/// <summary>Maps the target integer member to an <see cref="IntProperty"/>.</summary>
/// <remarks>This serves as the default engine fallback for integer types if no other specialized integer layout attribute is specified.</remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyIntAttribute(int min = int.MinValue, int max = int.MaxValue) : Attribute
{
    /// <summary>Gets or sets the minimum allowed value constraint passed to the <see cref="IntProperty"/>.</summary>
    public int Min { get; set; } = min;

    /// <summary>Gets or sets the maximum allowed value constraint passed to the <see cref="IntProperty"/>.</summary>
    public int Max { get; set; } = max;
}

/// <summary>Maps the target floating-point member to a <see cref="FloatProperty"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyFloatAttribute(float min = float.MinValue, float max = float.MaxValue) : Attribute
{
    /// <summary>Gets or sets the minimum boundary value passed to the <see cref="FloatProperty"/>.</summary>
    public float Min { get; set; } = min;

    /// <summary>Gets or sets the maximum boundary value passed to the <see cref="FloatProperty"/>.</summary>
    public float Max { get; set; } = max;
}

/// <summary>Maps the target string member to a <see cref="StringProperty"/>.</summary>
/// <remarks>This serves as the default fallback layout configuration for string fields unless specialized modifiers are found.</remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyStringAttribute(
    int minLength = int.MinValue,
    int maxLength = int.MaxValue,
    bool needsUnicode = true,
    bool localizable = false) : Attribute
{
    /// <summary>Gets or sets the minimum string length configuration passed to the <see cref="StringProperty"/>.</summary>
    public int MinLength { get; set; } = minLength;

    /// <summary>Gets or sets the maximum string length configuration passed to the <see cref="StringProperty"/>.</summary>
    public int MaxLength { get; set; } = maxLength;

    /// <summary>Gets or sets a value that indicates whether Unicode character sets are parsed by the <see cref="StringProperty"/>.</summary>
    public bool NeedsUnicode { get; set; } = needsUnicode;

    /// <summary>Gets or sets a value that indicates whether the value uses localization mapping rules inside the <see cref="StringProperty"/>.</summary>
    public bool Localizable { get; set; } = localizable;
}

/// <summary>Modifies a numeric property mapping to use a slider control visual element within its generated wrapper.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertySliderAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="PropertySliderAttribute" /> class.</summary>
    /// <param name="min">The minimum slider boundary value.</param>
    /// <param name="max">The maximum slider boundary value.</param>
    public PropertySliderAttribute(float min = float.MinValue, float max = float.MaxValue)
    {
        Min = min;
        Max = max;
    }

    /// <summary>Gets or sets the minimum boundary bound on the editor slider tool.</summary>
    public float Min { get; set; }

    /// <summary>Gets or sets the maximum boundary bound on the editor slider tool.</summary>
    public float Max { get; set; }
}

/// <summary>Modifies a string property mapping to draw options from a manual selection list container.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyDropdownAttribute : Attribute
{
    /// <summary>Gets or sets the explicitly declared selection items array.</summary>
    public string[] Options { get; set; }

    /// <summary>Initializes a new instance of the <see cref="PropertyDropdownAttribute" /> class for dynamic fallback configurations.</summary>
    public PropertyDropdownAttribute()
    {
        Options = [];
    }

    /// <summary>Initializes a new instance of the <see cref="PropertyDropdownAttribute" /> class with preset text option keys.</summary>
    /// <param name="options">The explicit localized options keys populated inside the layout selector UI.</param>
    public PropertyDropdownAttribute(params string[] options)
    {
        Options = options;
    }
}

/// <summary>Maps the target <see cref="Color"/> member to a <see cref="ColorProperty"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyColorAttribute(bool useAlpha = false) : Attribute
{
    /// <summary>Gets or sets a value passed to the <see cref="ColorProperty"/> indicating whether the alpha channel remains editable.</summary>
    public bool UseAlpha { get; set; } = useAlpha;
}

/// <summary>Maps the target string member to a <see cref="FileProperty"/> instead of a standard string layout wrapper.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyFileDialogAttribute(FileType type = FileType.Audio) : Attribute
{
    /// <summary>Gets or sets the system file extension selector filter passed to the <see cref="FileProperty"/>.</summary>
    public FileType Type { get; set; } = type;
}

/// <summary>Maps the target <see cref="ValueTuple{Single, Single}"/> member to a <see cref="FloatPairProperty"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyFloatPairAttribute(
    float min = int.MinValue,
    float max = int.MaxValue,
    float step = 0.1f,
    bool isRange = true) : Attribute
{
    /// <summary>Gets or sets the minimum limit value passed to the <see cref="FloatPairProperty"/>.</summary>
    public float Min { get; set; } = min;

    /// <summary>Gets or sets the maximum limit value passed to the <see cref="FloatPairProperty"/>.</summary>
    public float Max { get; set; } = max;

    /// <summary>Gets or sets the incremental steps scale values mapping configuration parameters.</summary>
    /// <remarks>Step is currently unused and has no effect on the editor UI, but it may be used for validation or UI behavior in the future.</remarks>
    public float Step { get; set; } = step;

    /// <summary>Gets or sets a value that indicates whether the pair represents a bounding range configuration passed to the <see cref="FloatPairProperty"/>.</summary>
    public bool IsRange { get; set; } = isRange;
}

/// <summary>Maps the target string member to a read-only <see cref="NoteProperty"/> instead of an input field wrapper.</summary>
/// <remarks>Note fields use their assigned default values as translation keys, making the layout engine output localized blocks of read-only text markup blocks contextually.</remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyNoteAttribute : Attribute
{
}

/// <summary>Maps the target integer member to a <see cref="RatingProperty"/> instead of a standard input field layout wrapper.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyRatingAttribute : Attribute
{
}

/// <summary>Maps the target string member to a multi-line <see cref="TextProperty"/> wrapper instead of a standard one-line string control block.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyLongInputFieldAttribute : Attribute
{
}

/// <summary>Maps the target custom enum member type structure into a dynamically calculated <see cref="EnumProperty{T}"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyEnumAttribute(params string[] exclusions) : Attribute
{
    /// <summary>Gets or sets the list of enum item text string identifiers excluded from compiling into the <see cref="EnumProperty{T}"/> elements list.</summary>
    public string[] Exclusions { get; set; } = exclusions;
}

/// <summary>Maps the target <see cref="ValueTuple{Int32, TileRelativeTo}"/> member type structure into a <see cref="TileProperty"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyTileAttribute(int min = int.MinValue, int max = int.MaxValue) : Attribute
{
    /// <summary>Gets or sets the minimum bound index value passed to the <see cref="TileProperty"/>.</summary>
    public int Min { get; set; } = min;

    /// <summary>Gets or sets the maximum bound index value passed to the <see cref="TileProperty"/>.</summary>
    public int Max { get; set; } = max;
}

/// <summary>Maps the target <see cref="Vector2"/> member structure into a <see cref="Vector2Property"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyVector2Attribute(
    float minX = float.NegativeInfinity,
    float maxX = float.PositiveInfinity,
    float minY = float.NegativeInfinity,
    float maxY = float.PositiveInfinity,
    bool allowEmpty = false) : Attribute
{
    /// <summary>Gets or sets the lowest positional constraints vectors mapped onto the <see cref="Vector2Property"/> properties boundaries.</summary>
    public Vector2 Min { get; set; } = new(minX, minY);

    /// <summary>Gets or sets the highest positional constraints vectors mapped onto the <see cref="Vector2Property"/> properties boundaries.</summary>
    public Vector2 Max { get; set; } = new(maxX, maxY);

    /// <summary>Gets or sets a value that indicates whether the parsed <see cref="Vector2Property"/> allows processing unassigned blank fields variables fields blocks entries.</summary>
    public bool AllowEmpty { get; set; } = allowEmpty;
}

/// <summary>Maps the target <see cref="ValueTuple{Vector2, Vector2}"/> member structure into a <see cref="Vector2RangeProperty"/>.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyVector2RangeAttribute(bool isRange = true) : Attribute
{
    /// <summary>Gets or sets a value passed to the <see cref="Vector2RangeProperty"/> indicating whether the layout evaluates the vectors as a scaling boundary range map.</summary>
    public bool IsRange { get; set; } = isRange;
}

/// <summary>Maps the target boolean member into a toggle element widget control module inside the <see cref="BoolProperty"/> tracker.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyToggleableAttribute(bool isOnByDefault = false) : Attribute
{
    /// <summary>Gets or sets a value defining whether the mapped <see cref="BoolProperty"/> starts marked active inside inspector layout windows configurations.</summary>
    public bool IsOnByDefault { get; set; } = isOnByDefault;
}

/// <summary>Configures visibility filter logic rules showing the targeting <see cref="CustomProperty"/> if dependency conditions match.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class PropertyShowIfAttribute : Attribute
{
    /// <summary>Gets or sets the name identifier of the controlling field context determining this layout visual states changes.</summary>
    public string ConditionPropertyName { get; set; }

    /// <summary>Gets or sets the target constraint string format criteria values expected to trigger visual layouts visibility rules.</summary>
    public string ExpectedValue { get; set; }

    /// <summary>Initializes a new instance of the <see cref="PropertyShowIfAttribute" /> tracking system layout rules indicators options.</summary>
    /// <param name="conditionPropertyName">The property name that controls visibility.</param>
    /// <param name="expectedValue">The value that makes the property visible.</param>
    public PropertyShowIfAttribute(string conditionPropertyName, string expectedValue)
    {
        ConditionPropertyName = conditionPropertyName;
        ExpectedValue = expectedValue;
    }
}

/// <summary>Configures input interactive activation logic rules enabling the targeting <see cref="CustomProperty"/> field only if condition metrics match targets variables.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class PropertyEnableIfAttribute : Attribute
{
    /// <summary>Gets or sets the tracking string context property parameter handling active locking states overrides on the generated UI control.</summary>
    public string ConditionPropertyName { get; set; }

    /// <summary>Gets or sets the target string criteria flag parameter evaluated to unlock operational edit modes variables checks options system inputs.</summary>
    public string ExpectedValue { get; set; }

    /// <summary>Initializes a new instance of the <see cref="PropertyEnableIfAttribute" /> active condition properties elements parameters profiles blocks configurations system references.</summary>
    /// <param name="conditionPropertyName">The property name that controls enablement.</param>
    /// <param name="expectedValue">The value that enables the property.</param>
    public PropertyEnableIfAttribute(string conditionPropertyName, string expectedValue)
    {
        ConditionPropertyName = conditionPropertyName;
        ExpectedValue = expectedValue;
    }
}

/// <summary>Configures layout masking logic rules systematically hiding the generated <see cref="CustomProperty"/> if specified condition evaluation layers validate true.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class PropertyHideIfAttribute : Attribute
{
    /// <summary>Gets or sets the target tracking parameter name element checking layout visibility criteria configurations paths structures properties tracking systems flags maps indices.</summary>
    public string ConditionPropertyName { get; set; }

    /// <summary>Gets or sets the text parsing criteria condition target parameters triggering total structural omission filters layouts items views controls elements block lists targets properties.</summary>
    public string ExpectedValue { get; set; }

    /// <summary>Initializes a new instance of the <see cref="PropertyHideIfAttribute" /> structural evaluation visibility configurations variables system checks indicators rules trackers blocks mappings.</summary>
    /// <param name="conditionPropertyName">The property name that controls visibility.</param>
    /// <param name="expectedValue">The value that hides the property.</param>
    public PropertyHideIfAttribute(string conditionPropertyName, string expectedValue)
    {
        ConditionPropertyName = conditionPropertyName;
        ExpectedValue = expectedValue;
    }
}

/// <summary>Configures entry constraints locking input components systems disabling the target <see cref="CustomProperty"/> dynamically upon meeting matching dependency rules parameters values.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class PropertyDisableIfAttribute : Attribute
{
    /// <summary>Gets or sets the reference tracking key checking active interactive operation layout blocks configurations flags metrics maps parameters references variables tracking blocks indicators targets.</summary>
    public string ConditionPropertyName { get; set; }

    /// <summary>Gets or sets the condition strings parameters evaluation keys matching inputs calculations pipelines turning inputs interaction locks modes on selectively elements structures objects tracks maps paths components entries.</summary>
    public string ExpectedValue { get; set; }

    /// <summary>Initializes a new instance of the <see cref="PropertyDisableIfAttribute" /> component interaction control systems parameters layouts mapping identifiers blocks definitions markers trackers elements entries options pipelines profiles context layers.</summary>
    /// <param name="conditionPropertyName">The property name that controls enablement.</param>
    /// <param name="expectedValue">The value that disables the property.</param>
    public PropertyDisableIfAttribute(string conditionPropertyName, string expectedValue)
    {
        ConditionPropertyName = conditionPropertyName;
        ExpectedValue = expectedValue;
    }
}

/// <summary>Applies explicit non-default verification requirements boundaries onto the evaluated <see cref="CustomProperty"/> instance to pass validation execution stages layout system checks.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyRequiredAttribute : Attribute;

/// <summary>Configures supplementary tooltip overlays or helper documentation link references tracking indices onto the processed <see cref="CustomProperty"/> container.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PropertyHelpAttribute : Attribute
{
    /// <summary>Gets or sets the localization descriptive help text lookup identifier string key.</summary>
    public string TextKey { get; set; }

    /// <summary>Gets or sets the targeted web documentation navigation link path references parameters properties context mappings profiles identifiers.</summary>
    /// <remarks>The destination path identifier parameter string is routinely evaluated through translation dictionaries context layout configurations blocks processing modules routines pipeline layout flows to yield unique language-specific target sites variations seamlessly.</remarks>
    public string ButtonURL { get; set; }

    /// <summary>Gets or sets the button action display tag text string localization mapping lookup reference key.</summary>
    public string ButtonTextKey { get; set; }

    /// <summary>Initializes a new instance of the <see cref="PropertyHelpAttribute" /> descriptive documentation indicators overlay configurations mappings parameters paths profiles identifiers metadata logs tracker strings registries blocks elements options lines context modules system controls structures entries.</summary>
    /// <param name="textKey">The help text localization key.</param>
    public PropertyHelpAttribute(string textKey)
    {
        TextKey = textKey;
    }
}