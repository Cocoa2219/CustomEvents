using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ADOFAI;
using ADOFAI.LevelEditor.Controls;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using PropertyInfo = ADOFAI.PropertyInfo;

namespace CustomEvents.Event;

public static class LocalizationRegistry
{
    private static readonly Dictionary<SystemLanguage, string> _languageToTag = new()
    {
        { SystemLanguage.Afrikaans, "af" },
        { SystemLanguage.Arabic, "ar" },
        { SystemLanguage.Basque, "eu" },
        { SystemLanguage.Belarusian, "be" },
        { SystemLanguage.Bulgarian, "bg" },
        { SystemLanguage.Catalan, "ca" },
        { SystemLanguage.Chinese, "zh" },
        { SystemLanguage.Czech, "cs" },
        { SystemLanguage.Danish, "da" },
        { SystemLanguage.Dutch, "nl" },
        { SystemLanguage.English, "en" },
        { SystemLanguage.Estonian, "et" },
        { SystemLanguage.Faroese, "fo" },
        { SystemLanguage.Finnish, "fi" },
        { SystemLanguage.French, "fr" },
        { SystemLanguage.German, "de" },
        { SystemLanguage.Greek, "el" },
        { SystemLanguage.Hebrew, "he" },
        { SystemLanguage.Hungarian, "hu" },
        { SystemLanguage.Icelandic, "is" },
        { SystemLanguage.Indonesian, "id" },
        { SystemLanguage.Italian, "it" },
        { SystemLanguage.Japanese, "ja" },
        { SystemLanguage.Korean, "ko" },
        { SystemLanguage.Latvian, "lv" },
        { SystemLanguage.Lithuanian, "lt" },
        { SystemLanguage.Norwegian, "no" },
        { SystemLanguage.Polish, "pl" },
        { SystemLanguage.Portuguese, "pt" },
        { SystemLanguage.Romanian, "ro" },
        { SystemLanguage.Russian, "ru" },
        { SystemLanguage.SerboCroatian, "sr" },
        { SystemLanguage.Slovak, "sk" },
        { SystemLanguage.Slovenian, "sl" },
        { SystemLanguage.Spanish, "es" },
        { SystemLanguage.Swedish, "sv" },
        { SystemLanguage.Thai, "th" },
        { SystemLanguage.Turkish, "tr" },
        { SystemLanguage.Ukrainian, "uk" },
        { SystemLanguage.Vietnamese, "vi" },
        { SystemLanguage.ChineseSimplified, "zh" },
        { SystemLanguage.ChineseTraditional, "zh" },
        { SystemLanguage.Hindi, "hi" },
        { SystemLanguage.Unknown, string.Empty }
    };

    private static readonly Dictionary<Assembly, Dictionary<string, Dictionary<string, string>>> _localizationData =
        new();

    private static readonly Dictionary<string, Dictionary<string, string>> _generalLocalizationData = new();

    private static string NormalizeLocaleTag(string localeOrEnumName)
    {
        if (string.IsNullOrWhiteSpace(localeOrEnumName)) return string.Empty;
        var s = localeOrEnumName.Trim();

        if (Enum.TryParse<SystemLanguage>(s, true, out var parsed))
            if (_languageToTag.TryGetValue(parsed, out var tag))
                return tag.ToLowerInvariant();

        var parts = s.Split(['-', '_'], 2);
        var left = parts[0].ToLowerInvariant();
        if (left.Length > 2) left = left.Substring(0, 2);
        return left;
    }

    private static string CultureToTag(CultureInfo culture)
    {
        if (culture == null || culture.Equals(CultureInfo.InvariantCulture)) return string.Empty;
        return NormalizeLocaleTag(culture.Name);
    }

    private static CultureInfo CultureFromSystemLanguage(SystemLanguage lang)
    {
        if (lang == SystemLanguage.Unknown) return CultureInfo.InvariantCulture;
        var tag = _languageToTag.TryGetValue(lang, out var t) ? t : string.Empty;
        try
        {
            return string.IsNullOrEmpty(tag) ? CultureInfo.InvariantCulture : new CultureInfo(tag);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }

    public static bool TryGetLocalizedString(Assembly assembly, string key, CultureInfo culture, out string localized)
    {
        localized = null;
        if (string.IsNullOrEmpty(key)) return false;

        var candidates = new List<string>();

        var primary = CultureToTag(culture);
        if (!string.IsNullOrEmpty(primary)) candidates.Add(primary);

        var persistenceTag = _languageToTag.TryGetValue(Persistence.language, out var ptag) ? ptag : string.Empty;
        if (!string.IsNullOrEmpty(persistenceTag) && (candidates.Count == 0 || candidates[0] != persistenceTag))
            candidates.Add(persistenceTag);

        if (!candidates.Contains("en"))
            candidates.Add("en");

        if (assembly != null && _localizationData.TryGetValue(assembly, out var assemblyMap) &&
            assemblyMap.TryGetValue(key, out var locales))
            foreach (var c in candidates)
                if (locales.TryGetValue(c, out localized) && !string.IsNullOrEmpty(localized))
                    return true;

        if (_generalLocalizationData.TryGetValue(key, out var generalLocales))
            foreach (var c in candidates)
                if (generalLocales.TryGetValue(c, out localized) && !string.IsNullOrEmpty(localized))
                    return true;

        return false;
    }

    public static string GetLocalizedString(Assembly assembly, string key, SystemLanguage language)
    {
        var culture = CultureFromSystemLanguage(language);
        return TryGetLocalizedString(assembly, key, culture, out var res) ? res : null;
    }

    public static string GetLocalizedString(string assemblyName, string key, SystemLanguage language)
    {
        var culture = CultureFromSystemLanguage(language);
        return _localizationData
            .Where(kv => kv.Key.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
            .Select(kv => TryGetLocalizedString(kv.Key, key, culture, out var res) ? res : null).FirstOrDefault();
    }

    public static string GetGeneralLocalizedString(string key, SystemLanguage language)
    {
        var culture = CultureFromSystemLanguage(language);
        return TryGetLocalizedString(null, key, culture, out var res) ? res : null;
    }

    public static void RegisterJson(string json)
    {
        var assembly = Assembly.GetCallingAssembly();
        if (!_localizationData.ContainsKey(assembly))
            _localizationData[assembly] = new Dictionary<string, Dictionary<string, string>>();

        JObject root;
        try
        {
            root = string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse localization JSON: {ex.Message}");
            return;
        }

        RegisterAllInOneObject(assembly, root, string.Empty);
    }

    public static void RegisterPerLanguageJson(string locale, string json)
    {
        var assembly = Assembly.GetCallingAssembly();
        var tag = NormalizeLocaleTag(locale);
        if (string.IsNullOrEmpty(tag)) return;

        var flattened = FlattenJson(json);

        if (!_localizationData.ContainsKey(assembly))
            _localizationData[assembly] = new Dictionary<string, Dictionary<string, string>>();

        foreach (var prop in flattened.Properties())
        {
            var key = prop.Name;
            var value = prop.Value.Type == JTokenType.Null ? null : prop.Value.ToString();
            if (value == null) continue;

            if (!_localizationData[assembly].ContainsKey(key))
                _localizationData[assembly][key] = new Dictionary<string, string>();
            _localizationData[assembly][key][tag] = value;

            if (!_generalLocalizationData.ContainsKey(key))
                _generalLocalizationData[key] = new Dictionary<string, string>();
            _generalLocalizationData[key][tag] = value;
        }
    }

    private static void RegisterAllInOneObject(Assembly assembly, JToken token, string prefix)
    {
        if (token == null) return;

        if (token.Type == JTokenType.Object)
        {
            var objectToken = (JObject)token;
            if (IsLocaleMap(objectToken))
            {
                RegisterLocalizedStringMap(assembly, prefix, objectToken);
                return;
            }

            foreach (var property in objectToken.Properties())
            {
                var nextPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                RegisterAllInOneObject(assembly, property.Value, nextPrefix);
            }
        }
    }

    private static bool IsLocaleMap(JObject obj)
    {
        if (obj.Count == 0) return false;

        foreach (var property in obj.Properties())
        {
            if (property.Value.Type != JTokenType.String &&
                property.Value.Type != JTokenType.Null &&
                property.Value.Type != JTokenType.Undefined)
                return false;

            if (string.IsNullOrEmpty(NormalizeLocaleTag(property.Name))) return false;
        }

        return true;
    }

    private static void RegisterLocalizedStringMap(Assembly assembly, string key, JObject localeMap)
    {
        if (string.IsNullOrEmpty(key)) return;

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in localeMap.Properties())
        {
            var tag = NormalizeLocaleTag(property.Name);
            if (string.IsNullOrEmpty(tag)) continue;

            if (property.Value.Type == JTokenType.Null || property.Value.Type == JTokenType.Undefined) continue;

            normalized[tag] = property.Value.ToString();
        }

        if (normalized.Count == 0) return;

        _localizationData[assembly][key] = normalized;

        if (!_generalLocalizationData.ContainsKey(key))
            _generalLocalizationData[key] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in normalized) _generalLocalizationData[key][kv.Key] = kv.Value;
    }

    private static JObject FlattenJson(string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            return new JObject();

        var jsonObject = JObject.Parse(jsonString);
        var flattenedResult = new JObject();

        FlattenToken(jsonObject, flattenedResult, "");

        return flattenedResult;
    }

    private static void FlattenToken(JToken token, JObject result, string prefix)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var property in token.Children<JProperty>())
                {
                    var newPrefix = string.IsNullOrEmpty(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";

                    FlattenToken(property.Value, result, newPrefix);
                }

                break;

            case JTokenType.Array:
                var index = 0;
                foreach (var value in token.Children())
                {
                    FlattenToken(value, result, $"{prefix}.{index}");
                    index++;
                }

                break;

            default:
                result[prefix] = token;
                break;
        }
    }
}

[HarmonyPatch(typeof(CategoryTab))]
public class CategoryTabLocalizationPatch
{
    [HarmonyPatch(nameof(CategoryTab.OnPointerEnter))]
    [HarmonyPostfix]
    public static void OnPointerEnterPostfix(CategoryTab __instance)
    {
        if (!__instance.levelEventCategory.IsExtendedCategory()) return;
        var key = CustomEventRegistry.CategoryCache.TryGetValue(__instance.levelEventCategory, out var category) &&
                  !string.IsNullOrWhiteSpace(category.localizationKey)
            ? category.localizationKey
            : $"category.{__instance.levelEventCategory.ToSafeName()}";

        ADOBase.editor.categoryText.text =
            LocalizationRegistry.GetGeneralLocalizedString(key,
                Persistence.language) ?? __instance.levelEventCategory.ToSafeName();
    }

    [HarmonyPatch(nameof(CategoryTab.SetSelected))]
    [HarmonyPostfix]
    public static void SetSelectedPostfix(CategoryTab __instance, bool selected)
    {
        if (!selected) return;
        if (!__instance.levelEventCategory.IsExtendedCategory()) return;
        var key = CustomEventRegistry.CategoryCache.TryGetValue(__instance.levelEventCategory, out var category) &&
                  !string.IsNullOrWhiteSpace(category.localizationKey)
            ? category.localizationKey
            : $"category.{__instance.levelEventCategory.ToSafeName()}";

        ADOBase.editor.categoryText.text =
            LocalizationRegistry.GetGeneralLocalizedString(key,
                Persistence.language) ?? __instance.levelEventCategory.ToSafeName();
    }
}

[HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.ShowPanel))]
public class InspectorPanelShowPanelLocalizationPatch
{
    public static void Postfix(InspectorPanel __instance, LevelEventType eventType)
    {
        if (eventType == LevelEventType.None) return;
        if (eventType.IsVanillaEventType()) return;

        if (CustomEventRegistry.CustomEvents.TryGetValue(eventType, out var customEvent) && customEvent != null)
        {
            var assembly = customEvent.Assembly;
            if (assembly != null)
            {
                string localizedString;
                if (CustomEventRegistry.CustomEventData.TryGetValue(eventType, out var eventData) &&
                    eventData != null && !string.IsNullOrEmpty(eventData.NameKey))
                    localizedString =
                        LocalizationRegistry.GetLocalizedString(assembly, eventData.NameKey, Persistence.language);
                else
                    localizedString = LocalizationRegistry.GetLocalizedString(assembly, $"{customEvent.Name}.name",
                        Persistence.language);

                __instance.title.text = !string.IsNullOrEmpty(localizedString) ? localizedString : customEvent.Name;
            }
        }
    }
}

[HarmonyPatch(typeof(LevelEventButton), nameof(LevelEventButton.ShowAsSelected))]
public class LevelEventButtonLocalizationPatch
{
    public static void Postfix(LevelEventButton __instance, bool selected)
    {
        if (__instance.type.IsVanillaEventType()) return;

        if (selected)
        {
            var assembly =
                CustomEventRegistry.CustomEvents.TryGetValue(__instance.type, out var customEvent) &&
                customEvent != null
                    ? customEvent.Assembly
                    : null;
            if (assembly != null)
            {
                string localizedString;
                if (CustomEventRegistry.CustomEventData.TryGetValue(__instance.type, out var eventData) &&
                    eventData != null && !string.IsNullOrEmpty(eventData.NameKey))
                    localizedString =
                        LocalizationRegistry.GetLocalizedString(assembly, eventData.NameKey, Persistence.language);
                else
                    localizedString = LocalizationRegistry.GetLocalizedString(assembly, $"{customEvent.Name}.name",
                        Persistence.language);

                ADOBase.editor.eventPickerText.text =
                    !string.IsNullOrEmpty(localizedString) ? localizedString : customEvent.Name;
            }
        }
        else
        {
            ADOBase.editor.eventPickerText.text = "";
        }
    }
}

[HarmonyPatch(typeof(PropertiesPanel), nameof(PropertiesPanel.RenderControl))]
public class PropertiesPanelRenderControlLocalizationPatch
{
    public static void Postfix(PropertiesPanel __instance, string propertyKey, PropertyInfo propertyInfo)
    {
        if (propertyInfo.levelEventInfo.type.IsVanillaEventType()) return;

        if (!__instance.properties.TryGetValue(propertyInfo.name, out var property)) return;

        var ev = CustomEventRegistry.CustomEvents[propertyInfo.levelEventInfo.type];

        if (ev != null && ev.Assembly != null)
        {
            if (property.control is PropertyControl_Text control)
            {
                string localizedPlaceholder;
                if (CustomEventRegistry.CustomEventData.TryGetValue(propertyInfo.levelEventInfo.type,
                        out var eventData) && eventData != null &&
                    eventData.PropertiesByName.TryGetValue(propertyInfo.name, out var propData) && propData != null &&
                    !string.IsNullOrEmpty(propData.Placeholder))
                    localizedPlaceholder =
                        LocalizationRegistry.GetLocalizedString(ev.Assembly, propData.Placeholder,
                            Persistence.language);
                else
                    localizedPlaceholder = LocalizationRegistry.GetLocalizedString(ev.Assembly,
                        $"{ev.Name}.{propertyKey}.placeholder", Persistence.language);

                if (!string.IsNullOrEmpty(localizedPlaceholder))
                    (control.inputField.placeholder as TMP_Text)?.text = localizedPlaceholder;
            }

            string helpKey, buttonURL, buttonTextKey;
            if (CustomEventRegistry.CustomEventData.TryGetValue(propertyInfo.levelEventInfo.type, out var evData) &&
                evData != null && evData.HelpMap.TryGetValue(propertyInfo.name, out var helpData) && helpData != null)
            {
                helpKey = string.IsNullOrEmpty(helpData.TextKey)
                    ? $"{ev.Name}.{propertyKey}.help.text"
                    : helpData.TextKey;
                buttonURL = string.IsNullOrEmpty(helpData.ButtonURL)
                    ? $"{ev.Name}.{propertyKey}.help.buttonURL"
                    : helpData.ButtonURL;
                buttonTextKey = string.IsNullOrEmpty(helpData.ButtonTextKey)
                    ? $"{ev.Name}.{propertyKey}.help.buttonText"
                    : helpData.ButtonTextKey;
            }
            else
            {
                helpKey = $"{ev.Name}.{propertyKey}.help.text";
                buttonURL = $"{ev.Name}.{propertyKey}.help.buttonURL";
                buttonTextKey = $"{ev.Name}.{propertyKey}.help.buttonText";
            }

            var localizedHelp = LocalizationRegistry.GetLocalizedString(ev.Assembly, helpKey, Persistence.language);
            if (!string.IsNullOrEmpty(localizedHelp))
            {
                var localizedButtonURL =
                    LocalizationRegistry.GetLocalizedString(ev.Assembly, buttonURL, Persistence.language);
                var localizedButtonText =
                    LocalizationRegistry.GetLocalizedString(ev.Assembly, buttonTextKey, Persistence.language);

                var helpButton = property.helpButton;
                helpButton.transform.parent.gameObject.SetActive(true);
                helpButton.onClick.AddListener(() => ADOBase.editor.ShowPropertyHelp(true, helpButton.transform,
                    localizedHelp, localizedButtonURL, localizedButtonText));
            }
        }
    }
}

[HarmonyPatch(typeof(Property), nameof(Property.info), MethodType.Setter)]
public class PropertyInfoSetterLocalizationPatch
{
    public static void Postfix(Property __instance, PropertyInfo value)
    {
        if (value.levelEventInfo.type.IsVanillaEventType()) return;

        if (CustomEventRegistry.CustomEvents.TryGetValue(value.levelEventInfo.type, out var customEvent) &&
            customEvent != null)
        {
            var assembly = customEvent.Assembly;
            if (assembly != null)
            {
                // var localizedString = LocalizationRegistry.GetLocalizedString(assembly, $"{customEvent.Name}.{value.name}.name", Persistence.language);
                string localizedString;
                if (CustomEventRegistry.CustomEventData.TryGetValue(value.levelEventInfo.type, out var eventData) &&
                    eventData != null && eventData.PropertiesByName.TryGetValue(value.name, out var propData) &&
                    propData != null)
                {
                    if (!string.IsNullOrEmpty(propData.LocalizationKey))
                        localizedString = LocalizationRegistry.GetLocalizedString(assembly, propData.LocalizationKey,
                            Persistence.language);
                    else if (!string.IsNullOrEmpty(propData.CustomLabel))
                        localizedString = propData.CustomLabel;
                    else
                        localizedString = LocalizationRegistry.GetLocalizedString(assembly,
                            $"{customEvent.Name}.{value.name}.name",
                            Persistence.language);
                }
                else
                {
                    localizedString = LocalizationRegistry.GetLocalizedString(assembly,
                        $"{customEvent.Name}.{value.name}.name",
                        Persistence.language);
                }

                if (!string.IsNullOrEmpty(localizedString))
                    __instance.label.text = localizedString + (value.required ? " <color=red>*</color>" : "");
                else
                    __instance.label.text = value.name + (value.required ? " <color=red>*</color>" : "");
            }
        }
    }
}

[HarmonyPatch(typeof(PropertyControl_Note), nameof(PropertyControl_Note.Setup))]
public class PropertyControlNoteLocalizationPatch
{
    public static void Postfix(PropertyControl_Note __instance)
    {
        if (__instance.propertyInfo == null) return;
        if (__instance.propertyInfo.levelEventInfo.type.IsVanillaEventType()) return;

        if (CustomEventRegistry.CustomEvents.TryGetValue(__instance.propertyInfo.levelEventInfo.type,
                out var customEvent) && customEvent != null)
        {
            var assembly = customEvent.Assembly;
            if (assembly != null)
            {
                string localizedString;
                if (CustomEventRegistry.CustomEventData.TryGetValue(__instance.propertyInfo.levelEventInfo.type,
                        out var eventData) && eventData != null &&
                    eventData.PropertiesByName.TryGetValue(__instance.propertyInfo.name, out var propData) &&
                    propData is NoteProperty noteProperty)
                {
                    localizedString = LocalizationRegistry.GetLocalizedString(assembly, !string.IsNullOrEmpty(noteProperty.NoteKey) ? noteProperty.NoteKey : $"{customEvent.Name}.{__instance.propertyInfo.name}", Persistence.language);
                }
                else
                {
                    localizedString = LocalizationRegistry.GetLocalizedString(assembly,
                        $"{customEvent.Name}.{__instance.propertyInfo.name}", Persistence.language);
                }

                __instance.noteText.text = !string.IsNullOrEmpty(localizedString) ? localizedString : __instance.propertyInfo.name;
            }
        }
    }
}

[HarmonyPatch]
public class PropertyControlUnitLocalizationPatch
{
    [HarmonyPatch(typeof(PropertyControl_FloatPair), nameof(PropertyControl_FloatPair.Setup))]
    [HarmonyPostfix]
    public static void Postfix_FloatPair(PropertyControl_FloatPair __instance)
    {
        if (__instance.propertyInfo == null) return;
        if (__instance.propertyInfo.levelEventInfo.type.IsVanillaEventType()) return;
        if (string.IsNullOrEmpty(__instance.propertyInfo.unit)) return;

        if (CustomEventRegistry.CustomEvents.TryGetValue(__instance.propertyInfo.levelEventInfo.type,
                out var customEvent) && customEvent != null)
        {
            var assembly = customEvent.Assembly;
            if (assembly != null)
            {
                var localizedString = LocalizationRegistry.GetLocalizedString(assembly,
                    $"unit.{__instance.propertyInfo.unit}", Persistence.language);
                if (!string.IsNullOrEmpty(localizedString))
                {
                    var unitText = __instance.control.startInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = localizedString;
                    unitText = __instance.control.endInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = localizedString;
                }
                else
                {
                    var unitText = __instance.control.startInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = __instance.propertyInfo.unit;
                    unitText = __instance.control.endInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = __instance.propertyInfo.unit;
                }
            }
        }
    }

    [HarmonyPatch(typeof(PropertyControl_Text), nameof(PropertyControl_Text.Setup))]
    [HarmonyPostfix]
    public static void Postfix_Text(PropertyControl_Text __instance)
    {
        if (__instance.propertyInfo == null) return;

        if (__instance.propertyInfo.levelEventInfo.type.IsVanillaEventType()) return;

        if (string.IsNullOrEmpty(__instance.propertyInfo.unit)) return;

        if (CustomEventRegistry.CustomEvents.TryGetValue(__instance.propertyInfo.levelEventInfo.type,
                out var customEvent) && customEvent != null)
        {
            var assembly = customEvent.Assembly;
            if (assembly != null)
            {
                var localizedString = LocalizationRegistry.GetLocalizedString(assembly,
                    $"unit.{__instance.propertyInfo.unit}", Persistence.language);
                if (!string.IsNullOrEmpty(localizedString))
                {
                    var unitText = __instance.unit;
                    unitText.gameObject.SetActive(true);
                    unitText.text = localizedString;
                }
                else
                {
                    var unitText = __instance.unit;
                    unitText.gameObject.SetActive(true);
                    unitText.text = __instance.propertyInfo.unit;
                }
            }
        }
    }

    [HarmonyPatch(typeof(PropertyControl_Vector2), nameof(PropertyControl_Vector2.Setup))]
    [HarmonyPostfix]
    public static void Postfix_Vector2(PropertyControl_Vector2 __instance)
    {
        if (__instance.propertyInfo == null) return;
        if (__instance.propertyInfo.levelEventInfo.type.IsVanillaEventType()) return;
        if (string.IsNullOrEmpty(__instance.propertyInfo.unit)) return;

        if (CustomEventRegistry.CustomEvents.TryGetValue(__instance.propertyInfo.levelEventInfo.type,
                out var customEvent) && customEvent != null)
        {
            var assembly = customEvent.Assembly;
            if (assembly != null)
            {
                var localizedString = LocalizationRegistry.GetLocalizedString(assembly,
                    $"unit.{__instance.propertyInfo.unit}", Persistence.language);
                if (!string.IsNullOrEmpty(localizedString))
                {
                    var unitText = __instance.unitX;
                    unitText.gameObject.SetActive(true);
                    unitText.text = localizedString;
                    unitText = __instance.unitY;
                    unitText.gameObject.SetActive(true);
                    unitText.text = localizedString;
                }
                else
                {
                    var unitText = __instance.unitX;
                    unitText.gameObject.SetActive(true);
                    unitText.text = __instance.propertyInfo.unit;
                    unitText = __instance.unitY;
                    unitText.gameObject.SetActive(true);
                    unitText.text = __instance.propertyInfo.unit;
                }
            }
        }
    }

    [HarmonyPatch(typeof(PropertyControl_Vector2Range), nameof(PropertyControl_Vector2Range.Setup))]
    [HarmonyPostfix]
    public static void Postfix_Vector2Range(PropertyControl_Vector2Range __instance)
    {
        if (__instance.propertyInfo == null) return;
        if (__instance.propertyInfo.levelEventInfo.type.IsVanillaEventType()) return;
        if (string.IsNullOrEmpty(__instance.propertyInfo.unit)) return;

        if (CustomEventRegistry.CustomEvents.TryGetValue(__instance.propertyInfo.levelEventInfo.type,
                out var customEvent) && customEvent != null)
        {
            var assembly = customEvent.Assembly;
            if (assembly != null)
            {
                var localizedString = LocalizationRegistry.GetLocalizedString(assembly,
                    $"unit.{__instance.propertyInfo.unit}", Persistence.language);
                if (!string.IsNullOrEmpty(localizedString))
                {
                    var unitText = __instance.controlX.startInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = localizedString;
                    unitText = __instance.controlX.endInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = localizedString;
                    unitText = __instance.controlY.startInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = localizedString;
                    unitText = __instance.controlY.endInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = localizedString;
                }
                else
                {
                    var unitText = __instance.controlX.startInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = __instance.propertyInfo.unit;
                    unitText = __instance.controlX.endInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = __instance.propertyInfo.unit;
                    unitText = __instance.controlY.startInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = __instance.propertyInfo.unit;
                    unitText = __instance.controlY.endInput.unitText;
                    unitText.gameObject.SetActive(true);
                    unitText.text = __instance.propertyInfo.unit;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(RDString), "GetEnumValue", typeof(string), typeof(string))]
public class RDStringGetEnumValueLocalizationPatch
{
    public static bool Prefix(string type, string value, ref string __result)
    {
        var split = type.Split(',');
        if (split.Length != 2) return true;

        var assembly = split[1].TrimStart();
        var enumTypeName = split[0].Split('.').Last();
        var localized =
            LocalizationRegistry.GetLocalizedString(assembly, $"enum.{enumTypeName}.{value}", Persistence.language);

        __result = localized ?? value;

        return false;
    }
}