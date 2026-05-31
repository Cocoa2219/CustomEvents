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

public abstract class CustomProperty(string name)
{
    public string Name { get; } = name;
    public bool IsEnabled { get; internal set; } = true;
    public abstract PropertyType Type { get; }

    public virtual string GetTypeName()
    {
        return Type.ToString();
    }

    public string Group { get; internal set; }

    public string Unit { get; internal set; }
    public string LocalizationKey { get; internal set; }
    public string CustomLabel { get; internal set; }
    public string Placeholder { get; internal set; }

    public abstract ControlType ControlType { get; }
    public bool IsSlider { get; internal set; }
    public bool DropdownEnabled { get; internal set; }
    public bool IsInvisible { get; internal set; }
    public bool IgnoreRange { get; internal set; }

    public bool IsDevOnly { get; internal set; }
    public bool CanBeDisabled { get; internal set; }
    public bool StartEnabled { get; internal set; }

    public (string key, string value)[] EnableIf { get; internal set; }
    public (string key, string value)[] DisableIf { get; internal set; }
    public (string key, string value)[] ShowIf { get; internal set; }
    public (string key, string value)[] HideIf { get; internal set; }

    public bool Required { get; internal set; }
    public bool ShouldEncode { get; internal set; } = true;
    public bool AffectsFloors { get; internal set; }
    public bool AffectsPath { get; internal set; }

    public abstract object DefaultValue { get; }

    public Dictionary<string, object> Serialize()
    {
        var dict = new Dictionary<string, object>();
        dict.Add("name", Name);
        dict.Add("group", Group);
        dict.Add("type", GetTypeName());
        if (DefaultValue != null)
            dict.Add("default", DefaultValue);
        dict.Add("unit", Unit);
        dict.Add("key", LocalizationKey);
        dict.Add("customLabel", CustomLabel);
        dict.Add("pro", IsDevOnly);
        dict.Add("canBeDisabled", CanBeDisabled);
        dict.Add("placeholder", Placeholder);
        dict.Add("stringDropdown", DropdownEnabled);
        dict.Add("invisible", IsInvisible);
        dict.Add("slider", IsSlider);
        dict.Add("ignoreRange", IgnoreRange);
        dict.Add("startEnabled", StartEnabled);

        var list = new List<object>();
        if (EnableIf != null)
        {
            foreach (var (key, value) in EnableIf)
            {
                list.Add(key);
                list.Add(value);
            }

            dict.Add("enableIf", list.ToList());
        }

        list = new List<object>();
        if (DisableIf != null)
        {
            foreach (var (key, value) in DisableIf)
            {
                list.Add(key);
                list.Add(value);
            }

            dict.Add("disableIf", list.ToList());
        }

        list = new List<object>();
        if (ShowIf != null)
        {
            foreach (var (key, value) in ShowIf)
            {
                list.Add(key);
                list.Add(value);
            }

            dict.Add("showIf", list.ToList());
        }

        list = new List<object>();
        if (HideIf != null)
        {
            foreach (var (key, value) in HideIf)
            {
                list.Add(key);
                list.Add(value);
            }

            dict.Add("hideIf", list.ToList());
        }

        dict.Add("required", Required);
        dict.Add("encode", ShouldEncode);
        dict.Add("affectsFloors", AffectsFloors);
        dict.Add("affectsPath", AffectsPath);

        var baseDict = SerializeBase();
        if (baseDict != null)
            foreach (var kvp in baseDict)
                dict.Add(kvp.Key, kvp.Value);

        dict.Add("control", ControlType.ToString());

        return dict;
    }

    protected virtual Dictionary<string, object> SerializeBase()
    {
        return new Dictionary<string, object>();
    }

    public abstract void Apply(MemberInfo member, LevelEvent levelEvent, CustomEvent customEvent);
    
    protected virtual object GetValue(LevelEvent levelEvent)
    {
        return levelEvent.data[Name];
    }
}

public abstract class CustomProperty<T>(string name, T @default = default) : CustomProperty(name)
{
    public T Default { get; } = @default;

    public override object DefaultValue => Default;
    public override ControlType ControlType => ControlType.NotAssigned;
    
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

public class ArrayProperty<T> : CustomProperty<T[]>
{
    /// <inheritdoc />
    public ArrayProperty(string name, T[] @default = null) : base(name, @default)
    {
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Array;
}

public class BoolProperty : CustomProperty<bool>
{
    /// <inheritdoc />
    public BoolProperty(string name, bool @default = false) : base(name, @default)
    {
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Bool;

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.ToggleGroup;
}

public sealed class ColorProperty : CustomProperty<string>
{
    public ColorProperty(string name, bool usesAlpha = false)
        : base(name, usesAlpha ? "ffffffff" : "ffffff")
    {
        UsesAlpha = usesAlpha;
    }

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

public class FileProperty : CustomProperty<string>
{
    /// <inheritdoc />
    public FileProperty(string name, FileType fileType = FileType.Audio, string @default = "")
        : base(name, @default)
    {
        FileType = fileType;
    }

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

public class FloatProperty : CustomProperty<float>
{
    /// <inheritdoc />
    public FloatProperty(string name, float min = float.NegativeInfinity, float max = float.PositiveInfinity,
        float @default = 0)
        : base(name, @default)
    {
        Min = min;
        Max = max;
    }

    public float Min { get; }
    public float Max { get; }

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

public sealed class FloatPairProperty : CustomProperty<(float, float)>
{
    public FloatPairProperty(string name, float min = int.MinValue, float max = int.MaxValue, float step = .1f,
        bool isRange = true, (float, float)? @default = null)
        : base(name, @default ?? (float.NaN, float.NaN))
    {
        Min = min;
        Max = max;
        Step = step;
        IsRange = isRange;
    }

    public float Min { get; }
    public float Max { get; }
    public float Step { get; }
    public bool IsRange { get; }

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

public class IntProperty : CustomProperty<int>
{
    /// <inheritdoc />
    public IntProperty(string name, int min = int.MinValue, int max = int.MaxValue, int @default = 0)
        : base(name, @default)
    {
        Min = min;
        Max = max;
    }

    public int Min { get; }
    public int Max { get; }
    
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

public class MinMaxGradient : CustomProperty<SerializedMinMaxGradient>
{
    /// <inheritdoc />
    public MinMaxGradient(string name, SerializedMinMaxGradient? @default = null) : base(name,
        @default ?? SerializedMinMaxGradient.Default())
    {
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.MinMaxGradient;

    /// <inheritdoc />
    public override ControlType ControlType => ControlType.MinMaxGradient;
}

public class NoteProperty : CustomProperty<string>
{
    /// <inheritdoc />
    public NoteProperty(string name, string noteKey = "") : base(name)
    {
        NoteKey = noteKey;
    }

    public string NoteKey { get; }

    protected override Dictionary<string, object> SerializeBase()
    {
        var dict = base.SerializeBase();
        dict.Add("noteKey", NoteKey);
        return dict;
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Note;
}

public class RatingProperty : CustomProperty<int>
{
    /// <inheritdoc />
    public RatingProperty(string name, int @default = 0) : base(name, @default)
    {
    }

    /// <inheritdoc />
    public override PropertyType Type => PropertyType.Rating;
}

public class StringProperty : CustomProperty<string>
{
    /// <inheritdoc />
    public StringProperty(string name, int minLength = int.MinValue, int maxLength = int.MaxValue,
        bool needsUnicode = true, bool localizable = false, string @default = "")
        : base(name, @default)
    {
        MinLength = minLength;
        MaxLength = maxLength;
        NeedsUnicode = needsUnicode;
        Localizable = localizable;
    }

    public int MinLength { get; }
    public int MaxLength { get; }
    public bool NeedsUnicode { get; }
    public bool Localizable { get; }

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

public class TextProperty : CustomProperty<string>
{
    /// <inheritdoc />
    public TextProperty(string name, string @default = "") : base(name, @default)
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

public class EnumProperty<TEnum> : CustomProperty<TEnum>
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public EnumProperty(string name, TEnum[] exceptions = null, TEnum @default = default) : base(name, @default)
    {
        Exceptions = exceptions;
    }

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

public class TileProperty : CustomProperty<(int, TileRelativeTo)>
{
    /// <inheritdoc />
    public TileProperty(string name, int min = int.MinValue, int max = int.MaxValue, TileRelativeTo[] exceptions = null,
        (int, TileRelativeTo)? @default = null)
        : base(name, @default ?? (0, TileRelativeTo.ThisTile))
    {
        Min = min;
        Max = max;
        Exceptions = exceptions;
    }

    public int Min { get; }
    public int Max { get; }
    public TileRelativeTo[] Exceptions { get; }
    
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

public class Vector2Property : CustomProperty<Vector2>
{
    /// <inheritdoc />
    public Vector2Property(string name, Vector2? min = null, Vector2? max = null, bool allowEmpty = false,
        Vector2? @default = null)
        : base(name, @default ?? new Vector2(float.NaN, float.NaN))
    {
        Min = min ?? new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        Max = max ?? new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        AllowEmpty = allowEmpty;
    }

    public Vector2 Min { get; }
    public Vector2 Max { get; }
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

public class Vector2RangeProperty : CustomProperty<(Vector2, Vector2)>
{
    /// <inheritdoc />
    public Vector2RangeProperty(string name, bool isRange = true, (Vector2, Vector2)? @default = null) : base(name,
        @default ?? (new Vector2(0f, 0f), new Vector2(0f, 0f)))
    {
        IsRange = isRange;
    }

    public bool IsRange { get; }
    
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