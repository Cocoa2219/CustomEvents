using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ADOFAI;
using ADOFAI.Editor.Models;
using UnityEngine;
using PropertyInfo = System.Reflection.PropertyInfo;

namespace CustomEvents.Event;

public class CustomEventData
{
    public CustomEventData(CustomEvent ev)
    {
        var type = ev.GetType();

        var attributes = type.GetCustomAttributes(true);

        var categories = new HashSet<LevelEventCategory>();
        var categoryNames = new HashSet<string>();
        var groups = new List<Group>();

        foreach (var t in attributes)
            switch (t)
            {
                case EventNameAttribute nameAttr:
                    NameKey = nameAttr.NameKey;
                    break;
                case EventCategoryAttribute categoryAttr:
                    var cats = categoryAttr.Categories;
                    foreach (var category in cats)
                    {
                        categoryNames.Add(category);
                        var parsedCategory = Utils.LooselyParseCategory(category, -1, true);
                        if (parsedCategory != (LevelEventCategory)(-1))
                            categories.Add(parsedCategory);
                    }

                    break;
                case EventPropGroupDefinitionAttribute groupAttr:
                    Sprite icon = null;
                    if (!string.IsNullOrEmpty(groupAttr.IconPath))
                    {
                        if (File.Exists(groupAttr.IconPath))
                        {
                            var file = File.ReadAllBytes(groupAttr.IconPath);
                            var tex = new Texture2D(2, 2);
                            tex.LoadRawTextureData(file);
                            tex.Apply();
                            icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
                        }
                    }

                    groups.Add(new Group
                    {
                        Name = groupAttr.Name,
                        Icon = icon,
                        IsDefault = groupAttr.IsDefault
                    });
                    break;
            }

        if (categories.Count == 0) categories.Add((LevelEventCategory)CustomCategoryRegistry.Uncategorized);

        CategoryNames = new string[categoryNames.Count];
        categoryNames.CopyTo(CategoryNames);
        Categories = new LevelEventCategory[categories.Count];
        categories.CopyTo(Categories);
        Groups = groups.ToArray();

        var propertiesList = new List<CustomProperty>();

        var props = GetProperties(type);
        foreach (var t in props)
            ProcessMember(ev, t, t.PropertyType, t.Name, propertiesList);

        var fields = GetFields(type);
        foreach (var t in fields)
            ProcessMember(ev, t, t.FieldType, t.Name, propertiesList);
        
        Properties = propertiesList.ToArray();
        foreach (var prop in Properties) _propertiesByName[prop.Name] = prop;
    }

    private IEnumerable<PropertyInfo> GetProperties(Type type)
    {
         var props = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.CanWrite && Attribute.IsDefined(p, typeof(EventPropertyAttribute))).Where(x =>
            {
                // Checking overrides
                var getter = x.GetGetMethod(true);
                return getter.GetBaseDefinition() == getter;
            });
         foreach (var prop in props) yield return prop;
    }
    
    private IEnumerable<FieldInfo> GetFields(Type type)
    {
        var fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
            .Where(f => Attribute.IsDefined(f, typeof(EventPropertyAttribute)));
        foreach (var field in fields) yield return field;
    }
    
    private void ProcessMember(object instance, MemberInfo member, Type t, string memberName,
        List<CustomProperty> propertiesList)
    {
        var attrs = member.GetCustomAttributes(true);

        var defaultValue = GetMemberDefaultValue(instance, member);

        EventPropertyAttribute propAttribute = null;
        PropertyRatingAttribute ratingAttribute = null;
        PropertyIntAttribute intAttribute = null;
        PropertyFloatAttribute floatAttribute = null;
        PropertyLongInputFieldAttribute longInputAttribute = null;
        PropertyNoteAttribute noteAttribute = null;
        PropertyFilePickerAttribute filePickerAttribute = null;
        PropertyStringAttribute stringAttribute = null;
        PropertyColorPickerAttribute colorPickerAttribute = null;
        PropertyFloatPairAttribute floatPairAttribute = null;
        PropertyEnumAttribute enumAttribute = null;
        PropertyTileAttribute tileAttribute = null;
        PropertyVector2Attribute vector2Attribute = null;
        PropertyVector2RangeAttribute vector2RangeAttribute = null;
        PropertyGroupAttribute groupAttribute = null;
        PropertyToggleableAttribute toggleableAttribute = null;
        PropertyRequiredAttribute requiredAttribute = null;
        PropertyLabelAttribute labelAttribute = null;
        PropertyHelpAttribute helpAttribute = null;
        
        var showIfAttributes = new List<PropertyShowIfAttribute>();
        var enableIfAttributes = new List<PropertyEnableIfAttribute>();
        var hideIfAttributes = new List<PropertyHideIfAttribute>();
        var disableIfAttributes = new List<PropertyDisableIfAttribute>();

        foreach (var a in attrs)
        {
            switch (a)
            {
                case EventPropertyAttribute pa:
                    propAttribute = pa;
                    break;
                case PropertyRatingAttribute pr:
                    ratingAttribute = pr;
                    break;
                case PropertyIntAttribute pi:
                    intAttribute = pi;
                    break;
                case PropertyFloatAttribute pf:
                    floatAttribute = pf;
                    break;
                case PropertyLongInputFieldAttribute pl:
                    longInputAttribute = pl;
                    break;
                case PropertyNoteAttribute pn:
                    noteAttribute = pn;
                    break;
                case PropertyFilePickerAttribute pfp:
                    filePickerAttribute = pfp;
                    break;
                case PropertyStringAttribute ps:
                    stringAttribute = ps;
                    break;
                case PropertyColorPickerAttribute pcp:
                    colorPickerAttribute = pcp;
                    break;
                case PropertyFloatPairAttribute pfp2:
                    floatPairAttribute = pfp2;
                    break;
                case PropertyEnumAttribute pe:
                    enumAttribute = pe;
                    break;
                case PropertyTileAttribute pt:
                    tileAttribute = pt;
                    break;
                case PropertyVector2Attribute pv:
                    vector2Attribute = pv;
                    break;
                case PropertyVector2RangeAttribute pvr:
                    vector2RangeAttribute = pvr;
                    break;
                case PropertyGroupAttribute pg:
                    groupAttribute = pg;
                    break;
                case PropertyToggleableAttribute ptg:
                    toggleableAttribute = ptg;
                    break;
                case PropertyRequiredAttribute prq:
                    requiredAttribute = prq;
                    break;
                case PropertyLabelAttribute plb:
                    labelAttribute = plb;
                    break;
                case PropertyHelpAttribute ph:
                    helpAttribute = ph;
                    break;
                case PropertyShowIfAttribute psi:
                    showIfAttributes.Add(psi);
                    break;
                case PropertyEnableIfAttribute pei:
                    enableIfAttributes.Add(pei);
                    break;
                case PropertyHideIfAttribute phi:
                    hideIfAttributes.Add(phi);
                    break;
                case PropertyDisableIfAttribute pdi:
                    disableIfAttributes.Add(pdi);
                    break;
            }
        }

        var name = string.IsNullOrEmpty(propAttribute?.Name) ? memberName : propAttribute.Name;
        CustomProperty prop = null;

        if (t == typeof(int))
        {
            if (ratingAttribute != null)
                prop = new RatingProperty(name, defaultValue is int v ? v : 0);
            else
                prop = new IntProperty(name, intAttribute?.Min ?? int.MinValue, intAttribute?.Max ?? int.MaxValue,
                    defaultValue is int v ? v : 0);
        }
        else if (t == typeof(float))
        {
            prop = new FloatProperty(name, floatAttribute?.Min ?? float.MinValue, floatAttribute?.Max ?? float.MaxValue,
                defaultValue is float v ? v : 0f);
        }
        else if (t == typeof(string))
        {
            if (longInputAttribute != null)
                prop = new TextProperty(name, defaultValue as string ?? "");
            else if (filePickerAttribute != null)
                prop = new FileProperty(name, filePickerAttribute.Type, defaultValue as string ?? "");
            else if (noteAttribute != null)
                prop = new NoteProperty(name, defaultValue as string ?? "");
            else
                prop = new StringProperty(name, stringAttribute?.MinLength ?? int.MinValue,
                    stringAttribute?.MaxLength ?? int.MaxValue, stringAttribute?.NeedsUnicode ?? true,
                    stringAttribute?.Localizable ?? false, defaultValue as string ?? "");
        }
        else if (t == typeof(bool))
        {
            prop = new BoolProperty(name, defaultValue is true);
        }
        else if (t == typeof(Color))
        {
            prop = new ColorProperty(name, colorPickerAttribute?.UseAlpha ?? false, defaultValue is Color color ? color.ToHex(colorPickerAttribute?.UseAlpha ?? false, false) : "ffffff");
        }
        else if (t.IsArray)
        {
            var elementType = t.GetElementType();
            var propType = typeof(ArrayProperty<>).MakeGenericType(elementType);
            prop = (CustomProperty)Activator.CreateInstance(propType, name, defaultValue as Array);
        }
        else if (t == typeof(ValueTuple<float, float>))
        {
            prop = new FloatPairProperty(name, floatPairAttribute?.Min ?? float.MinValue,
                floatPairAttribute?.Max ?? float.MaxValue, .1f,
                floatPairAttribute?.IsRange ?? true, defaultValue is ValueTuple<float, float> v ? v : (0f, 0f));
        }
        else if (t == typeof(SerializedMinMaxGradient))
        {
            prop = new MinMaxGradient(name);
        }
        else if (t.IsEnum)
        {
            var elementType = Enum.GetUnderlyingType(t);
            Array result;

            if (enumAttribute == null)
            {
                result = Array.CreateInstance(elementType, 0);
            }
            else
            {
                var exclusionsList = new List<object>();
                foreach (var t1 in enumAttribute.Exclusions)
                    try
                    {
                        var converted = Convert.ChangeType(t1, elementType);
                        if (converted != null) exclusionsList.Add(converted);
                    }
                    catch
                    {
                        // ignored
                    }

                result = Array.CreateInstance(elementType, exclusionsList.Count);
                for (var i = 0; i < exclusionsList.Count; i++) result.SetValue(exclusionsList[i], i);
            }

            var defaultEnumValue = defaultValue != null && Enum.IsDefined(t, defaultValue)
                ? defaultValue
                : Enum.GetValues(t).GetValue(0);
            var propType = typeof(EnumProperty<>).MakeGenericType(t);
            prop = (CustomProperty)Activator.CreateInstance(propType, name, result, defaultEnumValue);
        }
        else if (t == typeof(ValueTuple<int, TileRelativeTo>))
        {
            prop = new TileProperty(name, tileAttribute?.Min ?? int.MinValue, tileAttribute?.Max ?? int.MaxValue, [],
                defaultValue is ValueTuple<int, TileRelativeTo> v
                    ? v
                    : new ValueTuple<int, TileRelativeTo>(0, TileRelativeTo.ThisTile));
        }
        else if (t == typeof(Vector2))
        {
            prop = new Vector2Property(name, vector2Attribute?.Min, vector2Attribute?.Max,
                vector2Attribute?.AllowEmpty ?? false,
                defaultValue is Vector2 vec ? vec : default);
        }
        else if (t == typeof(ValueTuple<Vector2, Vector2>))
        {
            prop = new Vector2RangeProperty(name, vector2RangeAttribute?.IsRange ?? true,
                defaultValue is ValueTuple<Vector2, Vector2> vecRange ? vecRange : null);
        }
        else
        {
            Debug.LogWarning($"Unsupported property type {t} for member {memberName}");
        }

        if (prop != null)
        {
            if (groupAttribute != null) prop.Group = groupAttribute.Name;
            
            if (labelAttribute != null)
            {
                prop.CustomLabel = labelAttribute.CustomLabel;
                prop.LocalizationKey = labelAttribute.LocalizationKey;
            }

            if (toggleableAttribute != null)
            {
                prop.CanBeDisabled = true;
                prop.StartEnabled = toggleableAttribute.IsOnByDefault;
            }
            
            if (helpAttribute != null) HelpMap[prop.Name] = helpAttribute;

            if (showIfAttributes.Count > 0)
            {
                var arr = new (string, string)[showIfAttributes.Count];
                for (var i = 0; i < showIfAttributes.Count; i++)
                    arr[i] = (showIfAttributes[i].ConditionPropertyName, showIfAttributes[i].ExpectedValue);
                prop.ShowIf = arr;
            }

            if (enableIfAttributes.Count > 0)
            {
                var arr = new (string, string)[enableIfAttributes.Count];
                for (var i = 0; i < enableIfAttributes.Count; i++)
                    arr[i] = (enableIfAttributes[i].ConditionPropertyName, enableIfAttributes[i].ExpectedValue);
                prop.EnableIf = arr;
            }

            if (hideIfAttributes.Count > 0)
            {
                var arr = new (string, string)[hideIfAttributes.Count];
                for (var i = 0; i < hideIfAttributes.Count; i++)
                    arr[i] = (hideIfAttributes[i].ConditionPropertyName, hideIfAttributes[i].ExpectedValue);
                prop.HideIf = arr;
            }

            if (disableIfAttributes.Count > 0)
            {
                var arr = new (string, string)[disableIfAttributes.Count];
                for (var i = 0; i < disableIfAttributes.Count; i++)
                    arr[i] = (disableIfAttributes[i].ConditionPropertyName, disableIfAttributes[i].ExpectedValue);
                prop.DisableIf = arr;
            }

            if (requiredAttribute != null) prop.Required = true;
            if (propAttribute != null)
            {
                prop.IsEnabled = propAttribute.IsEnabled;
                prop.Unit = propAttribute.Unit;
                prop.Placeholder = propAttribute.Placeholder;
                prop.IsInvisible = propAttribute.IsInvisible;
                prop.IgnoreRange = propAttribute.IgnoreRange;
                prop.IsDevOnly = propAttribute.IsDevOnly;
                prop.ShouldEncode = propAttribute.ShouldEncode;
                prop.AffectsFloors = propAttribute.AffectsFloors;
                prop.AffectsPath = propAttribute.AffectsPath;
            }
            
            PropertyMap.Add(prop, member);
            propertiesList.Add(prop);
        }
    }

    private static object GetMemberDefaultValue(object instance, MemberInfo member)
    {
        return member switch
        {
            PropertyInfo { CanRead: true } propertyInfo => propertyInfo.GetValue(instance),
            FieldInfo fieldInfo => fieldInfo.GetValue(instance),
            _ => null
        };
    }

    public string NameKey { get; }
    
    public LevelEventCategory[] Categories { get; }
    public string[] CategoryNames { get; }
    public Group[] Groups { get; }

    public CustomProperty[] Properties { get; }
    private readonly Dictionary<string, CustomProperty> _propertiesByName = new();
    public IReadOnlyDictionary<string, CustomProperty> PropertiesByName => _propertiesByName;

    internal Dictionary<string, PropertyHelpAttribute> HelpMap { get; } = new();
    internal Dictionary<CustomProperty, MemberInfo> PropertyMap { get; } = new();

    public struct Group
    {
        public string Name;
        public Sprite Icon;
        public bool IsDefault;
    }
}