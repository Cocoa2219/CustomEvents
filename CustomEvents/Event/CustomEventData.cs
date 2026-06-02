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

/// <summary>Builds editor metadata for a <see cref="CustomEvent"/>.</summary>
public class CustomEventData
{
    /// <summary>Initializes a new instance of the <see cref="CustomEventData"/> class.</summary>
    /// <param name="ev">The event instance to inspect.</param>
    public CustomEventData(CustomEvent ev)
    {
        var type = ev.GetType();

        var attributes = type.GetCustomAttributes(true);

        var categories = new HashSet<LevelEventCategory>();
        var groups = new List<Group>();

        foreach (var t in attributes)
        {
            switch (t)
            {
                case EventNameAttribute nameAttr:
                    NameKey = nameAttr.NameKey;
                    break;
                case EventCategoryAttribute categoryAttr:
                    var cats = categoryAttr.Categories;
                    foreach (var category in cats)
                    {
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
        }

        if (categories.Count == 0) categories.Add((LevelEventCategory)CustomCategoryRegistry.Uncategorized);

        Categories = categories.ToArray();
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
        PropertyFileDialogAttribute fileDialogAttribute = null;
        PropertyStringAttribute stringAttribute = null;
        PropertyColorAttribute colorAttribute = null;
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
                case EventPropertyAttribute pa: propAttribute = pa; break;
                case PropertyRatingAttribute pr: ratingAttribute = pr; break;
                case PropertyIntAttribute pi: intAttribute = pi; break;
                case PropertyFloatAttribute pf: floatAttribute = pf; break;
                case PropertyLongInputFieldAttribute pl: longInputAttribute = pl; break;
                case PropertyNoteAttribute pn: noteAttribute = pn; break;
                case PropertyFileDialogAttribute pfp: fileDialogAttribute = pfp; break;
                case PropertyStringAttribute ps: stringAttribute = ps; break;
                case PropertyColorAttribute pcp: colorAttribute = pcp; break;
                case PropertyFloatPairAttribute pfp2: floatPairAttribute = pfp2; break;
                case PropertyEnumAttribute pe: enumAttribute = pe; break;
                case PropertyTileAttribute pt: tileAttribute = pt; break;
                case PropertyVector2Attribute pv: vector2Attribute = pv; break;
                case PropertyVector2RangeAttribute pvr: vector2RangeAttribute = pvr; break;
                case PropertyGroupAttribute pg: groupAttribute = pg; break;
                case PropertyToggleableAttribute ptg: toggleableAttribute = ptg; break;
                case PropertyRequiredAttribute prq: requiredAttribute = prq; break;
                case PropertyLabelAttribute plb: labelAttribute = plb; break;
                case PropertyHelpAttribute ph: helpAttribute = ph; break;
                case PropertyShowIfAttribute psi: showIfAttributes.Add(psi); break;
                case PropertyEnableIfAttribute pei: enableIfAttributes.Add(pei); break;
                case PropertyHideIfAttribute phi: hideIfAttributes.Add(phi); break;
                case PropertyDisableIfAttribute pdi: disableIfAttributes.Add(pdi); break;
            }
        }

        var name = string.IsNullOrEmpty(propAttribute?.Name) ? memberName : propAttribute.Name;

        // Collect condition arrays upfront
        (string key, string value)[] showIf = showIfAttributes.Count > 0 
            ? showIfAttributes.Select(x => (x.ConditionPropertyName, x.ExpectedValue)).ToArray() : null;
        (string key, string value)[] enableIf = enableIfAttributes.Count > 0 
            ? enableIfAttributes.Select(x => (x.ConditionPropertyName, x.ExpectedValue)).ToArray() : null;
        (string key, string value)[] hideIf = hideIfAttributes.Count > 0 
            ? hideIfAttributes.Select(x => (x.ConditionPropertyName, x.ExpectedValue)).ToArray() : null;
        (string key, string value)[] disableIf = disableIfAttributes.Count > 0 
            ? disableIfAttributes.Select(x => (x.ConditionPropertyName, x.ExpectedValue)).ToArray() : null;

        // Consolidate metadata values into PropertyData struct assembly
        PropertyData data = new()
        {
            Group = groupAttribute?.Name,
            CustomLabel = labelAttribute?.CustomLabel,
            LocalizationKey = labelAttribute?.LocalizationKey,
            CanBeDisabled = toggleableAttribute != null,
            StartEnabled = toggleableAttribute?.IsOnByDefault ?? false,
            Required = requiredAttribute != null,
            Unit = propAttribute?.Unit,
            Placeholder = propAttribute?.Placeholder,
            IsInvisible = propAttribute?.IsInvisible ?? false,
            IgnoreRange = propAttribute?.IgnoreRange ?? false,
            IsDevOnly = propAttribute?.IsDevOnly ?? false,
            ShouldEncode = propAttribute?.ShouldEncode ?? false,
            AffectsFloors = propAttribute?.AffectsFloors ?? false,
            AffectsPath = propAttribute?.AffectsPath ?? false,
            ShowIf = showIf,
            EnableIf = enableIf,
            HideIf = hideIf,
            DisableIf = disableIf
        };

        CustomProperty prop = null;

        if (t == typeof(int))
        {
            if (ratingAttribute != null)
                prop = new RatingProperty(name, data, defaultValue is int v ? v : 0);
            else
                prop = new IntProperty(name, data, intAttribute?.Min ?? int.MinValue, intAttribute?.Max ?? int.MaxValue, defaultValue is int v ? v : 0);
        }
        else if (t == typeof(float))
        {
            prop = new FloatProperty(name, data, floatAttribute?.Min ?? float.MinValue, floatAttribute?.Max ?? float.MaxValue, defaultValue is float v ? v : 0f);
        }
        else if (t == typeof(string))
        {
            if (longInputAttribute != null)
                prop = new TextProperty(name, data, defaultValue as string ?? "");
            else if (fileDialogAttribute != null)
                prop = new FileProperty(name, data, fileDialogAttribute.Type, defaultValue as string ?? "");
            else if (noteAttribute != null)
                prop = new NoteProperty(name, data, defaultValue as string ?? "");
            else
                prop = new StringProperty(name, data, stringAttribute?.MinLength ?? int.MinValue, stringAttribute?.MaxLength ?? int.MaxValue, stringAttribute?.NeedsUnicode ?? true, stringAttribute?.Localizable ?? false, defaultValue as string ?? "");
        }
        else if (t == typeof(bool))
        {
            prop = new BoolProperty(name, data, defaultValue is true);
        }
        else if (t == typeof(Color))
        {
            bool useAlpha = colorAttribute?.UseAlpha ?? false;
            string fallbackColor = defaultValue is Color color ? color.ToHex(useAlpha, false) : "ffffff";
            prop = new ColorProperty(name, data, useAlpha, fallbackColor);
        }
        else if (t.IsArray)
        {
            var elementType = t.GetElementType();
            var propType = typeof(ArrayProperty<>).MakeGenericType(elementType);
            prop = (CustomProperty)Activator.CreateInstance(propType, name, data, defaultValue as Array);
        }
        else if (t == typeof(ValueTuple<float, float>))
        {
            prop = new FloatPairProperty(name, data, floatPairAttribute?.Min ?? float.MinValue, floatPairAttribute?.Max ?? float.MaxValue, .1f, floatPairAttribute?.IsRange ?? true, defaultValue is ValueTuple<float, float> v ? v : (0f, 0f));
        }
        else if (t == typeof(SerializedMinMaxGradient))
        {
            prop = new MinMaxGradient(name, data);
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
                {
                    try
                    {
                        var converted = Convert.ChangeType(t1, elementType);
                        if (converted != null) exclusionsList.Add(converted);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                result = Array.CreateInstance(elementType, exclusionsList.Count);
                for (var i = 0; i < exclusionsList.Count; i++) result.SetValue(exclusionsList[i], i);
            }

            var defaultEnumValue = defaultValue != null && Enum.IsDefined(t, defaultValue)
                ? defaultValue
                : Enum.GetValues(t).GetValue(0);
            
            var propType = typeof(EnumProperty<>).MakeGenericType(t);
            prop = (CustomProperty)Activator.CreateInstance(propType, name, data, result, defaultEnumValue);
        }
        else if (t == typeof(ValueTuple<int, TileRelativeTo>))
        {
            prop = new TileProperty(name, data, tileAttribute?.Min ?? int.MinValue, tileAttribute?.Max ?? int.MaxValue, defaultValue is ValueTuple<int, TileRelativeTo> tile ? tile : (0, TileRelativeTo.ThisTile));
        }
        else if (t == typeof(Vector2))
        {
            prop = new Vector2Property(name, data, vector2Attribute?.Min, vector2Attribute?.Max, vector2Attribute?.AllowEmpty ?? false, defaultValue is Vector2 vec ? vec : default);
        }
        else if (t == typeof(ValueTuple<Vector2, Vector2>))
        {
            prop = new Vector2RangeProperty(name, data, vector2RangeAttribute?.IsRange ?? true, defaultValue is ValueTuple<Vector2, Vector2> vecRange ? vecRange : null);
        }
        else
        {
            Debug.LogWarning($"Unsupported property type {t} for member {memberName}");
        }

        if (prop != null)
        {
            if (helpAttribute != null) HelpMap[prop.Name] = helpAttribute;
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

    /// <summary>Gets the localization key for the event name.</summary>
    public string NameKey { get; }
    
    /// <summary>Gets the parsed category values for the event.</summary>
    public LevelEventCategory[] Categories { get; }

    /// <summary>Gets the property groups declared on the event.</summary>
    public Group[] Groups { get; }

    /// <summary>Gets the generated property metadata.</summary>
    public CustomProperty[] Properties { get; }
    
    /// <summary>Gets the generated property metadata indexed by property name.</summary>
    public IReadOnlyDictionary<string, CustomProperty> PropertiesByName => _propertiesByName;
    private readonly Dictionary<string, CustomProperty> _propertiesByName = new();
    
    internal Dictionary<string, PropertyHelpAttribute> HelpMap { get; } = new();
    internal Dictionary<CustomProperty, MemberInfo> PropertyMap { get; } = new();

    /// <summary>Describes a property group.</summary>
    public struct Group
    {
        /// <summary>Gets or sets the group name.</summary>
        public string Name;
        /// <summary>Gets or sets the group icon.</summary>
        public Sprite Icon;
        /// <summary>Gets or sets a value that indicates whether the group is the default group.</summary>
        public bool IsDefault;
    }
}