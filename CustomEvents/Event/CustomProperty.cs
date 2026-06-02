using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ADOFAI;
using ADOFAI.Editor.Models;
using DG.Tweening;
using UnityEngine;
using PropertyInfo = System.Reflection.PropertyInfo;

namespace CustomEvents.Event;

/// <summary>
/// Contains configuration metadata for initializing a custom property.
/// </summary>
public struct PropertyData
{
    /// <summary>Gets or sets the property group name.</summary>
    public string Group { get; init; }

    /// <summary>Gets or sets the unit label displayed next to the value.</summary>
    public string Unit { get; init; }

    /// <summary>Gets or sets the localization key for UI text.</summary>
    public string LocalizationKey { get; init; }

    /// <summary>Gets or sets the custom label text shown in the editor.</summary>
    public string CustomLabel { get; init; }

    /// <summary>Gets or sets the placeholder text shown when the field is empty.</summary>
    public string Placeholder { get; init; }

    /// <summary>Gets or sets a value that indicates whether the property uses a slider UI control.</summary>
    public bool IsSlider { get; init; }

    /// <summary>Gets or sets a value that indicates whether dropdown options are enabled.</summary>
    public bool DropdownEnabled { get; init; }

    /// <summary>Gets or sets a value that indicates whether the property is invisible in the inspector.</summary>
    public bool IsInvisible { get; init; }

    /// <summary>Gets or sets a value that indicates whether range validation is ignored.</summary>
    public bool IgnoreRange { get; init; }

    /// <summary>Gets or sets a value that indicates whether the property is development-only.</summary>
    public bool IsDevOnly { get; init; }

    /// <summary>Gets or sets a value that indicates whether the property can be dynamically disabled in the UI.</summary>
    public bool CanBeDisabled { get; init; }

    /// <summary>Gets or sets a value that indicates whether the property starts enabled by default.</summary>
    public bool StartEnabled { get; init; }

    /// <summary>Gets or sets a value that indicates whether the property is required to have a value.</summary>
    public bool Required { get; init; }

    /// <summary>Gets or sets a value that indicates whether the property value should be encoded during serialization.</summary>
    public bool ShouldEncode { get; init; }

    /// <summary>Gets or sets a value that indicates whether modifications to this property affect floor layouts.</summary>
    public bool AffectsFloors { get; init; }

    /// <summary>Gets or sets a value that indicates whether modifications to this property affect navigation pathfinding.</summary>
    public bool AffectsPath { get; init; }

    /// <summary>Gets or sets the conditional rules that enable this property.</summary>
    public (string key, string value)[] EnableIf { get; init; }

    /// <summary>Gets or sets the conditional rules that disable this property.</summary>
    public (string key, string value)[] DisableIf { get; init; }

    /// <summary>Gets or sets the conditional rules that display this property.</summary>
    public (string key, string value)[] ShowIf { get; init; }

    /// <summary>Gets or sets the conditional rules that hide this property.</summary>
    public (string key, string value)[] HideIf { get; init; }
}

/// <summary>Describes a custom event property.</summary>
public abstract class CustomProperty
{
    /// <summary>Initializes a new instance of the <see cref="CustomProperty"/> class.</summary>
    /// <param name="name">The serialized property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    protected CustomProperty(string name, PropertyData data)
    {
        Name = name;
        Data = data;
    }

    /// <summary>Gets the serialized property name.</summary>
    public string Name { get; }
    /// <summary>Gets the serialized property type.</summary>
    public abstract PropertyType Type { get; }
    /// <summary>Gets the editor control type.</summary>
    public abstract ControlType ControlType { get; }

    /// <summary>Gets the property metadata.</summary>
    public PropertyData Data { get; }

    /// <summary>Gets the serialized type name.</summary>
    public virtual string GetTypeName()
    {
        return Type.ToString();
    }
    
    /// <summary>Gets the default serialized value.</summary>
    public abstract object DefaultValue { get; }

    /// <summary>Serializes the property metadata.</summary>
    /// <returns>A dictionary that contains the serialized property metadata.</returns>
    public Dictionary<string, object> Serialize()
    {
        var dict = new Dictionary<string, object>
        {
            { "name", Name },
            { "group", Data.Group },
            { "type", GetTypeName() },
            { "unit", Data.Unit },
            { "key", Data.LocalizationKey },
            { "customLabel", Data.CustomLabel },
            { "pro", Data.IsDevOnly },
            { "canBeDisabled", Data.CanBeDisabled },
            { "placeholder", Data.Placeholder },
            { "stringDropdown", Data.DropdownEnabled },
            { "invisible", Data.IsInvisible },
            { "slider", Data.IsSlider },
            { "ignoreRange", Data.IgnoreRange },
            { "startEnabled", Data.StartEnabled },
            { "required", Data.Required },
            { "encode", Data.ShouldEncode },
            { "affectsFloors", Data.AffectsFloors },
            { "affectsPath", Data.AffectsPath },
            { "control", ControlType.ToString() }
        };

        if (DefaultValue != null)
            dict.Add("default", DefaultValue);

        AddConditions(dict, "enableIf", Data.EnableIf);
        AddConditions(dict, "disableIf", Data.DisableIf);
        AddConditions(dict, "showIf", Data.ShowIf);
        AddConditions(dict, "hideIf", Data.HideIf);

        var baseDict = SerializeBase();
        if (baseDict != null)
        {
            foreach (var kvp in baseDict)
                dict.Add(kvp.Key, kvp.Value);
        }

        return dict;
    }

    private void AddConditions(Dictionary<string, object> dict, string key, (string, string)[] conditions)
    {
        if (conditions == null || conditions.Length == 0) return;
        
        var list = new List<object>();
        foreach (var (k, v) in conditions)
        {
            list.Add(k);
            list.Add(v);
        }
        dict.Add(key, list);
    }

    /// <summary>Serializes additional metadata for derived property types.</summary>
    /// <returns>A dictionary that contains derived-type metadata.</returns>
    protected virtual Dictionary<string, object> SerializeBase()
    {
        return new Dictionary<string, object>();
    }

    /// <summary>Applies the property value to the target member.</summary>
    /// <param name="member">The target member to receive the value.</param>
    /// <param name="levelEvent">The level event that supplies the value.</param>
    /// <param name="customEvent">The custom event instance that receives the applied value.</param>
    public abstract void Apply(MemberInfo member, LevelEvent levelEvent, CustomEvent customEvent);
    
    /// <summary>Reads the value from a level event.</summary>
    /// <param name="levelEvent">The level event to inspect.</param>
    /// <returns>The value stored in the level event.</returns>
    protected virtual object GetValue(LevelEvent levelEvent)
    {
        return levelEvent.data[Name];
    }
}

/// <summary>Describes a typed custom event property.</summary>
/// <typeparam name="T">The property value type.</typeparam>
public abstract class CustomProperty<T> : CustomProperty
{
    /// <summary>Initializes a new instance of the <see cref="CustomProperty{T}"/> class.</summary>
    /// <param name="name">The serialized property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="default">The default typed value for the property.</param>
    protected CustomProperty(string name, PropertyData data, T @default = default) : base(name, data)
    {
        Default = @default;
    }

    /// <summary>Gets the default value.</summary>
    public T Default { get; }

    /// <inheritdoc />
    public override object DefaultValue => Default;
    /// <inheritdoc />
    public override ControlType ControlType => ControlType.NotAssigned;
    
    /// <inheritdoc />
    public override void Apply(MemberInfo member, LevelEvent levelEvent, CustomEvent customEvent)
    {
        var value = GetValue(levelEvent);
        switch (member)
        {
            case PropertyInfo propertyInfo:
                propertyInfo.SetValue(customEvent, value);
                break;
            case FieldInfo fieldInfo:
                fieldInfo.SetValue(customEvent, value);
                break;
        }
    }
}

/// <summary>Describes an array property.</summary>
/// <typeparam name="T">The array element type.</typeparam>
public class ArrayProperty<T> : CustomProperty<T[]>
{
    /// <inheritdoc />
    public ArrayProperty(string name, PropertyData data, T[] @default = null) : base(name, data, @default)
    {
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Array;
}

/// <summary>Describes a boolean property.</summary>
public class BoolProperty : CustomProperty<bool>
{
    /// <inheritdoc />
    public BoolProperty(string name, PropertyData data, bool @default = false) : base(name, data, @default)
    {
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Bool;

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.ToggleGroup;
}

/// <summary>Describes a color property.</summary>
public sealed class ColorProperty : CustomProperty<string>
{
    /// <summary>Initializes a new instance of the <see cref="ColorProperty"/> class.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="usesAlpha">A value that indicates whether alpha editing is enabled.</param>
    /// <param name="default">The default serialized hex color value.</param>
    public ColorProperty(string name, PropertyData data, bool usesAlpha = false, string @default = null)
        : base(name, data, @default)
    {
        UsesAlpha = usesAlpha;
    }

    /// <summary>Gets a value that indicates whether alpha is editable.</summary>
    public bool UsesAlpha { get; }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Color;

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.ColorPicker;

    /// <inheritdoc />
    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("usesAlpha", UsesAlpha);
        return dict;
    }

    /// <inheritdoc />
    protected override object GetValue(LevelEvent levelEvent)
    {
        var value = levelEvent.GetColor(Name);
        return value;
    }
}

/// <summary>Describes a file path property.</summary>
public class FileProperty : CustomProperty<string>
{
    /// <summary>Initializes a new instance of the <see cref="FileProperty"/> class.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="fileType">The restrictive target file type layout filter.</param>
    /// <param name="default">The default system file path value string.</param>
    public FileProperty(string name, PropertyData data, FileType fileType = FileType.Audio, string @default = "")
        : base(name, data, @default)
    {
        FileType = fileType;
    }

    /// <summary>Gets the file type filter.</summary>
    public FileType FileType { get; }

    /// <inheritdoc />
    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("fileType", FileType.ToString());
        return dict;
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.File;

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.File;
}

/// <summary>Describes a floating-point property.</summary>
public class FloatProperty : CustomProperty<float>
{
    /// <summary>Initializes a new instance of the <see cref="FloatProperty"/> class.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="min">The minimum restrictive boundary scale value.</param>
    /// <param name="max">The maximum restrictive boundary scale value.</param>
    /// <param name="default">The default mathematical float value state.</param>
    public FloatProperty(string name, PropertyData data, float min = float.NegativeInfinity, float max = float.PositiveInfinity,
        float @default = 0)
        : base(name, data, @default)
    {
        Min = min;
        Max = max;
    }

    /// <summary>Gets the minimum value.</summary>
    public float Min { get; }
    /// <summary>Gets the maximum value.</summary>
    public float Max { get; }

    /// <inheritdoc />
    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("min", Min);
        dict.Add("max", Max);
        return dict;
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Float;

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.InputField;
}

/// <summary>Describes a floating-point pair property.</summary>
public sealed class FloatPairProperty : CustomProperty<(float, float)>
{
    /// <summary>Initializes a new instance of the <see cref="FloatPairProperty"/> class.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="min">The minimum evaluation boundary constraints value.</param>
    /// <param name="max">The maximum evaluation boundary constraints value.</param>
    /// <param name="step">The interface slider snap adjustments increments.</param>
    /// <param name="isRange">A value indicating if values behave as an inclusive range sequence.</param>
    /// <param name="default">The default values state tuple block.</param>
    public FloatPairProperty(string name, PropertyData data, float min = int.MinValue, float max = int.MaxValue, float step = .1f,
        bool isRange = true, (float, float)? @default = null)
        : base(name, data, @default ?? (float.NaN, float.NaN))
    {
        Min = min;
        Max = max;
        Step = step;
        IsRange = isRange;
    }

    /// <summary>Gets the minimum value.</summary>
    public float Min { get; }
    /// <summary>Gets the maximum value.</summary>
    public float Max { get; }
    /// <summary>Gets the step value.</summary>
    public float Step { get; }
    /// <summary>Gets a value that indicates whether the pair represents a range.</summary>
    public bool IsRange { get; }

    /// <inheritdoc />
    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("min", Min);
        dict.Add("max", Max);
        dict.Add("step", Step);
        dict.Add("isRange", IsRange);
        return dict;
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.FloatPair;

    /// <inheritdoc />
    public override object DefaultValue
    {
        get
        {
            var (default1, default2) = Default;
            if (float.IsNaN(default1) && float.IsNaN(default2))
                return null;
            return new List<object> { default1, default2 };
        }
    }

    /// <inheritdoc />
    protected override object GetValue(LevelEvent levelEvent)
    {
        var value = levelEvent.GetFloatPair(Name).ToValueTuple();
        return value;
    }

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.FloatPair;
}

/// <summary>Describes an integer property.</summary>
public class IntProperty : CustomProperty<int>
{
    /// <summary>Initializes a new instance of the <see cref="IntProperty"/> class.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="min">The minimum validation constraints limit.</param>
    /// <param name="max">The maximum validation constraints limit.</param>
    /// <param name="default">The default discrete integer value.</param>
    public IntProperty(string name, PropertyData data, int min = int.MinValue, int max = int.MaxValue, int @default = 0)
        : base(name, data, @default)
    {
        Min = min;
        Max = max;
    }

    /// <summary>Gets the minimum value.</summary>
    public int Min { get; }
    /// <summary>Gets the maximum value.</summary>
    public int Max { get; }
    
    /// <inheritdoc />
    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("min", Min);
        dict.Add("max", Max);
        return dict;
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Int;

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.InputField;
}

/// <summary>Describes a min-max gradient property.</summary>
public class MinMaxGradient : CustomProperty<SerializedMinMaxGradient>
{
    /// <inheritdoc />
    public MinMaxGradient(string name, PropertyData data, SerializedMinMaxGradient? @default = null) : base(name, data,
        @default ?? SerializedMinMaxGradient.Default())
    {
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.MinMaxGradient;

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.MinMaxGradient;
}

/// <summary>Describes a note property.</summary>
public class NoteProperty : CustomProperty<string>
{
    /// <summary>Initializes a new instance of the <see cref="NoteProperty"/> class.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="noteKey">The note localization key.</param>
    public NoteProperty(string name, PropertyData data, string noteKey = "") : base(name, data)
    {
        NoteKey = noteKey;
    }

    /// <summary>Gets the note localization key.</summary>
    public string NoteKey { get; }

    /// <inheritdoc />
    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("noteKey", NoteKey);
        return dict;
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Note;
}

/// <summary>Describes a rating property.</summary>
public class RatingProperty : CustomProperty<int>
{
    /// <inheritdoc />
    public RatingProperty(string name, PropertyData data, int @default = 0) : base(name, data, @default)
    {
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Rating;
}

/// <summary>Describes a string property.</summary>
public class StringProperty : CustomProperty<string>
{
    /// <summary>Initializes a new instance of the <see cref="StringProperty"/> class.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="minLength">The minimum characters length constraint threshold.</param>
    /// <param name="maxLength">The maximum characters length constraint threshold.</param>
    /// <param name="needsUnicode">A value indicating if Unicode layout compliance execution formatting rules apply.</param>
    /// <param name="localizable">A value indicating if alternative language localization keys resolution processing applies.</param>
    /// <param name="default">The default plain text string content reference value.</param>
    public StringProperty(string name, PropertyData data, int minLength = int.MinValue, int maxLength = int.MaxValue,
        bool needsUnicode = true, bool localizable = false, string @default = "")
        : base(name, data, @default)
    {
        MinLength = minLength;
        MaxLength = maxLength;
        NeedsUnicode = needsUnicode;
        Localizable = localizable;
    }

    /// <summary>Gets the minimum length.</summary>
    public int MinLength { get; }
    /// <summary>Gets the maximum length.</summary>
    public int MaxLength { get; }
    /// <summary>Gets a value that indicates whether Unicode is required.</summary>
    public bool NeedsUnicode { get; }
    /// <summary>Gets a value that indicates whether the string is localizable.</summary>
    public bool Localizable { get; }

    /// <inheritdoc />
    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("minLength", MinLength);
        dict.Add("maxLength", MaxLength);
        dict.Add("needsUnicode", NeedsUnicode);
        dict.Add("localizable", Localizable);
        return dict;
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.String;

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.InputField;
}

/// <summary>Describes a long string property.</summary>
public class TextProperty : CustomProperty<string>
{
    /// <inheritdoc />
    public TextProperty(string name, PropertyData data, string @default = "") : base(name, data, @default)
    {
    }

    /// <inheritdoc />
    public override string GetTypeName()
    {
        return "Text";
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.LongString;

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.LongInputField;
}

/// <summary>Describes an enum property.</summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
public class EnumProperty<TEnum> : CustomProperty<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>Initializes a new instance of the <see cref="EnumProperty{TEnum}"/> class.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="exceptions">The explicit array list stack mapping exclusions configurations elements.</param>
    /// <param name="default">The default enumerator indexing selection state fallback marker.</param>
    public EnumProperty(string name, PropertyData data, TEnum[] exceptions = null, TEnum @default = default) : base(name, data, @default)
    {
        Exceptions = exceptions;
    }

    /// <summary>Gets the excluded enum values.</summary>
    public TEnum[] Exceptions { get; }

    /// <inheritdoc />
    public override object DefaultValue => Default.ToString();

    /// <inheritdoc />
    public override string GetTypeName()
    {
        var type = typeof(TEnum);
        string name;
        if (type == typeof(Ease))
            name = "Ease";
        else if (type == typeof(ParticleSystemShapeMultiModeValue))
            name = "ParticleArcMode";
        else
            name = $"{type.FullName}, {type.Assembly.GetName().Name}";
        return "Enum:" + name;
    }

    /// <inheritdoc />
    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("except", Exceptions?.Select(e => e.ToString()).Cast<object>().ToList());
        return dict;
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Enum;

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.Dropdown;
}

/// <summary>Describes a tile-relative property.</summary>
public class TileProperty : CustomProperty<(int, TileRelativeTo)>
{
    /// <summary>Initializes a new instance of the <see cref="TileProperty"/> class.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="min">The minimum relative space layout values bounds index.</param>
    /// <param name="max">The maximum relative space layout values bounds index.</param>
    /// <param name="default">The default layout index assignment calculations mapping context structural layout pair definition.</param>
    public TileProperty(string name, PropertyData data, int min = int.MinValue, int max = int.MaxValue,
        (int, TileRelativeTo)? @default = null)
        : base(name, data, @default ?? (0, TileRelativeTo.ThisTile))
    {
        Min = min;
        Max = max;
    }

    /// <summary>Gets the minimum value.</summary>
    public int Min { get; }
    /// <summary>Gets the maximum value.</summary>
    public int Max { get; }
    
    /// <inheritdoc />
    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("min", Min);
        dict.Add("max", Max);
        return dict;
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Tile;

    /// <inheritdoc />
    public override object DefaultValue
    {
        get
        {
            var (default1, default2) = Default;
            return new List<object> { default1, default2.ToString() };
        } 
    }

    /// <inheritdoc />
    protected override object GetValue(LevelEvent levelEvent)
    {
        var value = levelEvent.GetTile(Name).ToValueTuple();
        return value;
    }

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.Dropdown;
}

/// <summary>Describes a <see cref="Vector2"/> property.</summary>
public class Vector2Property : CustomProperty<Vector2>
{
    /// <summary>Initializes a new instance of the <see cref="Vector2Property"/> class.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="min">The vector spatial lower coordinate range threshold boundary values metrics block configuration.</param>
    /// <param name="max">The vector spatial upper coordinate range threshold boundary values metrics block configuration.</param>
    /// <param name="allowEmpty">A value indicating if incomplete coordinates are allowed.</param>
    /// <param name="default">The default spatial positioning vector definitions.</param>
    public Vector2Property(string name, PropertyData data, Vector2? min = null, Vector2? max = null, bool allowEmpty = false,
        Vector2? @default = null)
        : base(name, data, @default ?? new Vector2(float.NaN, float.NaN))
    {
        Min = min ?? new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        Max = max ?? new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        AllowEmpty = allowEmpty;
    }

    /// <summary>Gets the minimum vector.</summary>
    public Vector2 Min { get; }
    /// <summary>Gets the maximum vector.</summary>
    public Vector2 Max { get; }
    /// <summary>Gets a value that indicates whether empty values are allowed.</summary>
    public bool AllowEmpty { get; }

    /// <inheritdoc />
    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("min", new List<object> { Min.x, Min.y });
        dict.Add("max", new List<object> { Max.x, Max.y });
        dict.Add("allowEmpty", AllowEmpty);
        return dict;
    }

    /// <inheritdoc />
    public override object DefaultValue
    {
        get
        {
            var defaultValue = Default;
            return defaultValue == Vector2.zero ? null : new List<object> { defaultValue.x, defaultValue.y };
        }
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Vector2;
}

/// <summary>Describes a vector range property.</summary>
public class Vector2RangeProperty : CustomProperty<(Vector2, Vector2)>
{
    /// <summary>Initializes a new instance of the <see cref="Vector2RangeProperty"/> class.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="data">The configuration metadata container.</param>
    /// <param name="isRange">A value indicating if vectors construct bounded range calculations thresholds configurations limits.</param>
    /// <param name="default">The default double positional vector configuration boundaries.</param>
    public Vector2RangeProperty(string name, PropertyData data, bool isRange = true, (Vector2, Vector2)? @default = null) : base(name, data,
        @default ?? (new Vector2(0f, 0f), new Vector2(0f, 0f)))
    {
        IsRange = isRange;
    }

    /// <summary>Gets a value that indicates whether the pair represents a range.</summary>
    public bool IsRange { get; }
    
    /// <inheritdoc />
    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("isRange", IsRange);
        return dict;
    }

    /// <inheritdoc />
    public override object DefaultValue
    {
        get
        {
            var (default1, default2) = Default;
            if (default1 == Vector2.zero && default2 == Vector2.zero)
                return null;
            return new List<object>
            {
                new List<object> { default1.x, default1.y },
                new List<object> { default2.x, default2.y }
            };
        }
    }

    /// <inheritdoc />
    protected override object GetValue(LevelEvent levelEvent)
    {
        return levelEvent.data[Name] is not Tuple<Vector2, Vector2> value ? null : new ValueTuple<Vector2, Vector2>(value.Item1, value.Item2);
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Vector2Range;
}