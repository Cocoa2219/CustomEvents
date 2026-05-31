using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ADOFAI;
using ADOFAI.Serialization;
using GDMiniJSON;
using HarmonyLib;
using TMPro;
using UnityEngine;
using static HarmonyLib.AccessTools;
using PropertyInfo = ADOFAI.PropertyInfo;

namespace CustomEvents.Event;

public static class CustomEventRegistry
{
    private static Harmony _harmony;

    public static IReadOnlyDictionary<LevelEventType, CustomEvent> CustomEvents => _customEvents;
    private static readonly Dictionary<LevelEventType, CustomEvent> _customEvents = new();
    public static IReadOnlyDictionary<LevelEventType, CustomEventData> CustomEventData => _customEventData;
    private static readonly Dictionary<LevelEventType, CustomEventData> _customEventData = new();

    public static IReadOnlyDictionary<string, LevelEventType> NameToIdCache => _nameToIdCache;
    private static readonly Dictionary<string, LevelEventType> _nameToIdCache = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<LevelEventCategory, (Sprite icon, string localizationKey)> CategoryCache => _categoryCache;
    private static readonly Dictionary<LevelEventCategory, (Sprite, string)> _categoryCache = new();

    public static IReadOnlyDictionary<(LevelEventType info, string group), Sprite> GroupIconCache => _groupIconCache;
    private static readonly Dictionary<(LevelEventType info, string group), Sprite> _groupIconCache = new();
    
    internal static Dictionary<string, (LevelEventInfo info, List<LevelEventCategory> categoryIds)> EventsToAdd { get; } =
        new();

    internal static Dictionary<LevelEventCategory, Sprite> EventCategoryIconsToAdd { get; } = new();

    internal static Dictionary<LevelEventType, string> KeysToAdd { get; } = new();
    internal static Dictionary<Assembly, Dictionary<PropertyInfo, string>> DefaultValueToAdd { get; } = new();
    
    public static void RegisterCustomEvent(CustomEvent ev)
    {
        if (_harmony == null)
        {
            _harmony = new Harmony("com.github.cocoa2219.customevents." + DateTime.Now.Ticks);
            _harmony.PatchAll();
            
            PlayerPrefsJson.nonSyncedKeys =
            [
                "globalVolume",
                "musicVolume",
                "hitsoundVolume",
                "sfxVolume",
                "interfaceVolume",
                "language",
                "offset",
                "offset_v",
                "acceptedAgreement",
                "visualQuality",
                "visualEffects",
                "targetFramerate"
            ];
        }

        var evData = new CustomEventData(ev);

        var name = ev.FullName;
        var id = CustomEventTypeRegistry.GetNewID();
        _nameToIdCache[name] = id;
        _customEventData[id] = evData;

        var info = new LevelEventInfo
        {
            name = name,
            type = id,
            categories = evData.Categories.ToList(),
            propertiesInfo = new Dictionary<string, PropertyInfo>(),
            executionTime = ev.ExecutionTime,
            allowFirstFloor = ev.AllowFirstFloor,
            isDecoration = ev.IsDecoration,
            stretchViewport = ev.StretchEditorViewport,
            groups = evData.Groups.Select(g => new LevelEventInfo.Group
            {
                name = g.Name,
                icon = "",
                isDefault = g.IsDefault
            }).ToList(),
            useGroups = evData.Groups.Length > 0,
            taroDLC = ev.NeedsDLC,
            pro = ev.IsDevOnly
        };
        
        if (evData.Groups.Length > 0)
            foreach (var group in evData.Groups)
            {
                var groupKey = (info.type, group.Name);
                if (group.Icon != null) _groupIconCache[groupKey] = group.Icon;
            }

        foreach (var prop in evData.Properties)
        {
            var propInfo = new PropertyInfo(prop.Serialize(), info);
            switch (prop)
            {
                case StringProperty stringProp:
                {
                    var key = string.IsNullOrEmpty(stringProp.Default)
                        ? $"{ev.Name}.{propInfo.name}.default"
                        : stringProp.Default;
                    if (LocalizationRegistry.TryGetLocalizedString(ev.Assembly, key,
                            LocalizationRegistry.CultureFromSystemLanguage(Persistence.language), out var localized))
                    {
                        propInfo.value_default = localized;
                        break;
                    }

                    DefaultValueToAdd.TryAdd(ev.Assembly, new Dictionary<PropertyInfo, string>());
                    
                    DefaultValueToAdd[ev.Assembly][propInfo] = key;
                    break;
                }
                case TextProperty longStringProp:
                {
                    var key = string.IsNullOrEmpty(longStringProp.Default)
                        ? $"{ev.Name}.{propInfo.name}.default"
                        : longStringProp.Default;
                    if (LocalizationRegistry.TryGetLocalizedString(ev.Assembly, key,
                            LocalizationRegistry.CultureFromSystemLanguage(Persistence.language), out var localized))
                    {
                        propInfo.value_default = localized;
                        break;
                    }

                    DefaultValueToAdd.TryAdd(ev.Assembly, new Dictionary<PropertyInfo, string>());
                    DefaultValueToAdd[ev.Assembly][propInfo] = key;
                    break;
                }
            }
            
            info.propertiesInfo[prop.Name] = propInfo;
        }
        
        if (GCS.levelEventsInfo != null)
        {
            GCS.levelEventsInfo.Add(name, info);

            var levelEventInfo = GCS.levelEventsInfo[name];
            foreach (var category in evData.Categories.Where(category => !levelEventInfo.categories.Contains(category)))
                levelEventInfo.categories.Add(category);
        }
        else
        {
            EventsToAdd[name] = (info, evData.Categories.ToList());
        }

        // timing issue, plugin was loaded before the ADOStartup started
        if (GCS.levelEventTypeString == null)
            KeysToAdd[id] = name;
        else
            GCS.levelEventTypeString[id] = name;

        _customEvents[id] = ev;
    }

    public static void RegisterCustomEvents(IEnumerable<CustomEvent> events)
    {
        foreach (var ev in events) RegisterCustomEvent(ev);
    }

    public static void RegisterAll()
    {
        var allEvents = Assembly.GetCallingAssembly().GetTypes()
            .Where(type => typeof(CustomEvent).IsAssignableFrom(type) && !type.IsAbstract)
            .Select(type => (CustomEvent)Activator.CreateInstance(type));

        RegisterCustomEvents(allEvents);
    }

    public static void RegisterCategory(string categoryName, Sprite icon = null, string localizationKey = null)
    {
        if (_harmony == null)
        {
            _harmony = new Harmony("com.github.cocoa2219.customevents." + DateTime.Now.Ticks);
            _harmony.PatchAll();
            
            PlayerPrefsJson.nonSyncedKeys =
            [
                "globalVolume",
                "musicVolume",
                "hitsoundVolume",
                "sfxVolume",
                "interfaceVolume",
                "language",
                "offset",
                "offset_v",
                "acceptedAgreement",
                "visualQuality",
                "visualEffects",
                "targetFramerate"
            ];
        }

        var categoryId = CustomCategoryRegistry.GetOrAddCustomCategoryValue(categoryName);

        if (icon != null)
        {
            if (GCS.eventCategoryIcons != null)
                GCS.eventCategoryIcons.Add(categoryId, icon);
            else
                EventCategoryIconsToAdd[categoryId] = icon;
        }
        
        _categoryCache[categoryId] = (icon, localizationKey);
    }
}

[HarmonyPatch(typeof(ADOStartup), nameof(ADOStartup.DecodeLevelEventInfoList))]
public class DecodeLevelEventInfoListPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var method = typeof(RDUtils).GetMethod(nameof(RDUtils.ParseEnum));
        var closedMethod = method!.MakeGenericMethod(typeof(LevelEventType));
        var idx = newInstructions.FindIndex(i => i.Calls(closedMethod));
        newInstructions[idx].operand = Method(typeof(Utils), nameof(Utils.LooselyParseEventType));
        foreach (var instruction in newInstructions) yield return instruction;
    }
}

[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.LoadEditorProperties))]
public class scnEditorLoadEditorPropertiesPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var method = typeof(RDUtils).GetMethod(nameof(RDUtils.ParseEnum));
        var closedMethod = method!.MakeGenericMethod(typeof(LevelEventType));
        var idx = newInstructions.FindIndex(i => i.Calls(closedMethod));
        newInstructions[idx].operand = Method(typeof(Utils), nameof(Utils.LooselyParseEventType));

        idx = newInstructions.FindIndex(i => i.Calls(Method(typeof(Enum), nameof(Enum.GetValues))));
        idx -= 2;
        newInstructions.RemoveRange(idx, 3);
        newInstructions.InsertRange(idx, [
            new CodeInstruction(OpCodes.Call,
                Method(typeof(CustomCategoryRegistry), nameof(CustomCategoryRegistry.GetValues)))
        ]);

        idx = newInstructions.FindIndex(i => i.Calls(Method(typeof(Enum), nameof(Enum.GetValues))));
        idx -= 2;
        newInstructions.RemoveRange(idx, 3);
        newInstructions.InsertRange(idx, [
            new CodeInstruction(OpCodes.Call,
                Method(typeof(CustomCategoryRegistry), nameof(CustomCategoryRegistry.GetValues)))
        ]);

        foreach (var instruction in newInstructions) yield return instruction;
    }
}

[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.SetupFavoritesCategory))]
public class scnEditorSetupFavoritesCategory
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var method = typeof(RDUtils).GetMethod(nameof(RDUtils.ParseEnum));
        var closedMethod = method!.MakeGenericMethod(typeof(LevelEventType));
        var idx = newInstructions.FindIndex(i => i.Calls(closedMethod));
        newInstructions[idx].operand = Method(typeof(Utils), nameof(Utils.LooselyParseEventType));
        foreach (var instruction in newInstructions) yield return instruction;
    }
}

[HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.Init))]
public class InspectorPanelInitPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var method = typeof(RDUtils).GetMethod(nameof(RDUtils.ParseEnum));
        var closedMethod = method!.MakeGenericMethod(typeof(LevelEventType));
        var idx = newInstructions.FindIndex(i => i.Calls(closedMethod));
        newInstructions[idx].operand = Method(typeof(Utils), nameof(Utils.LooselyParseEventType));

        foreach (var instruction in newInstructions) yield return instruction;
    }

    public static void Postfix(InspectorPanel __instance)
    {
        var panels = __instance.panelsList;
        foreach (var panel in panels)
        {
            if (!panel.levelEventType.IsExtendedEventType())
            {
                continue;
            }
            
            var tabButtons = panel.tabButtons;
            foreach (var kvp in tabButtons)
            {
                var i =
                    CustomEventRegistry.GroupIconCache.TryGetValue((panel.levelEventType, kvp.Key), out var icon) &&
                    icon != null
                        ? icon
                        : GCS.eventCategoryIcons[LevelEventCategory.Gameplay];
                kvp.Value.SetIcon(i);
            }
        }
    }
}

[HarmonyPatch]
public class LoadLevelEventSpritesPatch
{
    public static MethodBase TargetMethod()
    {
        return FirstMethod(typeof(scnEditor), m =>
            m.Name.Contains("LoadLevelEventSprites") && m.GetParameters().Length == 0);
    }

    public static void Postfix(scnEditor __instance)
    {
        foreach (var kvp in CustomEventRegistry.CustomEvents)
        {
            if (GCS.levelEventIcons.ContainsKey(kvp.Key)) continue;
            GCS.levelEventIcons.Add(kvp.Key, kvp.Value.GetIcon());
        }
    }
}

[HarmonyPatch(typeof(LevelEventButton), nameof(LevelEventButton.Init))]
public class LevelEventButtonInitPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var idx = newInstructions.FindIndex(i => i.opcode == OpCodes.Constrained);
        newInstructions.RemoveRange(idx - 1, 3);
        newInstructions.InsertRange(idx - 1, [
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, Method(typeof(Utils), nameof(Utils.EventTypeToString)))
        ]);
        foreach (var instruction in newInstructions) yield return instruction;
    }
}

[HarmonyPatch(typeof(ADOStartup), nameof(ADOStartup.SetupLevelEventsInfo))]
public class ADOStartupSetupLevelEventsInfoPatch
{
    public static void Postfix()
    {
        foreach (var kvp in CustomEventRegistry.EventsToAdd)
        {
            GCS.levelEventsInfo[kvp.Key] = kvp.Value.info;
            var levelEventInfo = GCS.levelEventsInfo[kvp.Key];
            foreach (var category in kvp.Value.categoryIds.Where(category =>
                         !levelEventInfo.categories.Contains(category)))
                levelEventInfo.categories.Add(category);
        }

        CustomEventRegistry.EventsToAdd.Clear();

        foreach (var kvp in CustomEventRegistry.KeysToAdd) GCS.levelEventTypeString[kvp.Key] = kvp.Value;

        CustomEventRegistry.KeysToAdd.Clear();
    }
}

[HarmonyPatch(typeof(LevelEvent), MethodType.Constructor, typeof(int), typeof(LevelEventType), typeof(LevelEventInfo),
    typeof(Dictionary<string, object>), typeof(Dictionary<string, bool>), typeof(bool), typeof(bool), typeof(bool))]
public class LevelEventConstructorPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var idx = newInstructions.FindIndex(i => i.LoadsField(Field(typeof(GCS), nameof(GCS.levelEventsInfo))));
        idx++;
        newInstructions.RemoveAt(idx);
        newInstructions.InsertRange(idx, [
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, Field(typeof(LevelEvent), nameof(LevelEvent.eventType))),
            new CodeInstruction(OpCodes.Call, Method(typeof(Utils), nameof(Utils.EventTypeToString)))
        ]);
        foreach (var instruction in newInstructions) yield return instruction;
    }
}

[HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.ShowPanel))]
public class InspectorPanelShowPanelPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);

        var idx = newInstructions.FindIndex(i => i.LoadsField(Field(typeof(GCS), nameof(GCS.levelEventsInfo))));
        idx++;
        newInstructions.RemoveRange(idx, 3);
        newInstructions.InsertRange(idx, [
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, Method(typeof(Utils), nameof(Utils.EventTypeToString)))
        ]);

        foreach (var instruction in newInstructions) yield return instruction;
    }
}

[HarmonyPatch(typeof(LevelEvent), nameof(LevelEvent.Encode))]
public class LevelEventEncodePatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var idx = newInstructions.FindIndex(i => i.opcode == OpCodes.Constrained);

        newInstructions.RemoveRange(idx - 1, 3);
        newInstructions.InsertRange(idx - 1, [
            new CodeInstruction(OpCodes.Ldfld, Field(typeof(LevelEvent), nameof(LevelEvent.eventType))),
            new CodeInstruction(OpCodes.Call, Method(typeof(Utils), nameof(Utils.EventTypeToString)))
        ]);

        foreach (var instruction in newInstructions) yield return instruction;
    }
}

[HarmonyPatch(typeof(LevelEvent), nameof(LevelEvent.Decode))]
public class LevelEventDecodePatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var method = typeof(RDUtils).GetMethod(nameof(RDUtils.ParseEnum));
        var closedMethod = method!.MakeGenericMethod(typeof(LevelEventType));
        var idx = newInstructions.FindIndex(i => i.Calls(closedMethod));
        newInstructions[idx].operand = Method(typeof(Utils), nameof(Utils.LooselyParseEventType));

        foreach (var instruction in newInstructions) yield return instruction;
    }
}

[HarmonyPatch]
public class scnEditorLoadLevelCategorySpritesPatch
{
    public static MethodBase TargetMethod()
    {
        return FirstMethod(typeof(scnEditor), m =>
            m.Name.Contains("LoadLevelCategorySprites") && m.GetParameters().Length == 0);
    }

    public static void Postfix()
    {
        foreach (var kvp in CustomEventRegistry.EventCategoryIconsToAdd) GCS.eventCategoryIcons.Add(kvp.Key, kvp.Value);
        CustomEventRegistry.EventCategoryIconsToAdd.Clear();
    }
}

[HarmonyPatch(typeof(CategoryTab))]
public class CategoryTabPatch
{
    [HarmonyPatch(nameof(CategoryTab.Init))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> InitTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var idx = newInstructions.FindIndex(i => i.LoadsField(Field(typeof(GCS), nameof(GCS.eventCategoryIcons))));
        newInstructions.RemoveRange(idx, 3);
        newInstructions.InsertRange(idx, [
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, Method(typeof(CategoryTabPatch), nameof(SafelyGetCategorySprite)))
        ]);
        foreach (var instruction in newInstructions) yield return instruction;
    }

    public static Sprite SafelyGetCategorySprite(LevelEventCategory category)
    {
        if (GCS.eventCategoryIcons.TryGetValue(category, out var sprite)) return sprite;
        if (CustomEventRegistry.CategoryCache.TryGetValue(category, out var customSprite)) return customSprite.icon;
        
        Debug.LogWarning($"No sprite found for category {category}, using default icon.");
        return GCS.eventCategoryIcons[LevelEventCategory.Gameplay];
    }
}

[HarmonyPatch(typeof(scnEditor), "CycleEventsPage")]
public class scnEditorCycleEventsPagePatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var idx = newInstructions.FindIndex(i => i.Calls(Method(typeof(Enum), nameof(Enum.GetValues))));
        idx -= 2;

        newInstructions.RemoveRange(idx, 3);
        newInstructions.InsertRange(idx, [
            new CodeInstruction(OpCodes.Call,
                Method(typeof(CustomCategoryRegistry), nameof(CustomCategoryRegistry.GetValues)))
        ]);
        
        idx = newInstructions.FindIndex(i => i.Calls(Method(typeof(Mathf), nameof(Mathf.Clamp), [typeof(int), typeof(int), typeof(int)])));
        idx += 2;
        
        newInstructions.RemoveRange(idx, 2);
        newInstructions.InsertRange(idx, [
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Ldloc_1),
            new CodeInstruction(OpCodes.Callvirt, Method(typeof(Array), nameof(Array.GetValue), [typeof(int)])),
            new CodeInstruction(OpCodes.Unbox_Any, typeof(LevelEventCategory)),
            new CodeInstruction(OpCodes.Stloc_2)    
        ]);
        
        foreach (var instruction in newInstructions) yield return instruction;
    }
}

[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.RemoveEventAtSelected))]
public class scnEditorRemoveEventAtSelectedPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var idx = newInstructions.FindIndex(i => i.LoadsField(Field(typeof(GCS), nameof(GCS.levelEventsInfo))));

        idx++;

        newInstructions.RemoveRange(idx, 3);
        newInstructions.InsertRange(idx, [
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, Method(typeof(Utils), nameof(Utils.EventTypeToString)))
        ]);
        foreach (var instruction in newInstructions) yield return instruction;
    }
}

[HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.ShowTabsForFloor))]
public class InspectorPanelShowTabsForFloorPatch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var idx = newInstructions.FindIndex(i => i.Calls(Method(typeof(Enum), nameof(Enum.GetValues))));
        newInstructions[idx].operand = Method(typeof(InspectorPanelShowTabsForFloorPatch), nameof(GetExtendedValues));

        foreach (var instruction in newInstructions) yield return instruction;
    }

    public static LevelEventType[] GetExtendedValues(Type type)
    {
        var original = (LevelEventType[])Enum.GetValues(type);

        var result = original.ToList();
        result.AddRange(CustomEventRegistry.CustomEvents.Keys.Select(id => id));
        return result.ToArray();
    }
}

[HarmonyPatch(typeof(LevelData), nameof(LevelData.EncodeToDictionary))]
public class LevelDataEncodePatch
{
    private static readonly Dictionary<int, LevelEvent> _customEventsToAdd = new();

    public static void Prefix(LevelData __instance)
    {
        _customEventsToAdd.Clear();
        foreach (var levelEvent in __instance.levelEvents
                     .Where(levelEvent => levelEvent.eventType.IsExtendedEventType()).ToList())
        {
            _customEventsToAdd.Add(__instance.levelEvents.IndexOf(levelEvent), levelEvent);
            __instance.levelEvents.Remove(levelEvent);
        }
    }

    public static void Postfix(LevelData __instance, ref Dictionary<string, object> __result)
    {
        if (__result.TryGetValue("actions", out var events))
        {
            if (events is List<Dictionary<string, object>> dict)
            {
                if (_customEventsToAdd.Count > 0)
                    dict.Add(new Dictionary<string, object>
                    {
                        { "floor", 0 },
                        { "eventType", "EditorComment" },
                        {
                            "comment",
                            "!NOTICE\nThese events were added by CustomEvents.\nErasing these events will also erase the custom events.\n\n이 이벤트들은 CustomEvents에서 추가되었습니다.\n이 이벤트들을 지우면 커스텀 이벤트들도 같이 지워집니다."
                        }
                    });

                foreach (var ev in _customEventsToAdd.Values.OrderBy(x => x.floor).Where(x => x.info.isActive))
                {
                    dict.Add(new Dictionary<string, object>
                    {
                        { "floor", ev.floor },
                        { "eventType", "EditorComment" },
                        {
                            "comment", "!EVENT\n" + JsonSerializer.Serialize(ev.Encode(), new JsonSerializerOptions
                            {
                                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                                Converters =
                                {
                                    new LevelArrayConverter()
                                },
                                WriteIndented = true
                            })
                        }
                    });
                }
            }
        }

        foreach (var kvp in _customEventsToAdd)
        {
            var index = kvp.Key;
            var levelEvent = kvp.Value;

            __instance.levelEvents.Insert(index, levelEvent);
        }

        _customEventsToAdd.Clear();
    }
}

[HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
public class LevelDataDecodePatch
{
    public static void Postfix(LevelData __instance)
    {
        var commentEvents = __instance.levelEvents.Where(e => e.eventType == LevelEventType.EditorComment).ToList();
        foreach (var commentEvent in commentEvents)
            if (commentEvent.data.TryGetValue("comment", out var commentObj) && commentObj is string comment)
                if (comment.StartsWith("!"))
                {
                    __instance.levelEvents.Remove(commentEvent);
                    var lines = comment.Split('\n');
                    if (lines.Length >= 2)
                    {
                        var type = lines[0];
                        var content = string.Join("\n", lines.Skip(1));
                        if (type == "!EVENT")
                            try
                            {
                                var deserialized =
                                    (Dictionary<string, object>)Json.Deserialize(UnescapeJsonString(content));
                                var levelEvent = new LevelEvent(deserialized);
                                __instance.levelEvents.Add(levelEvent);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Failed to decode custom event from comment: {ex}");
                            }
                    }
                }
    }

    private static string UnescapeJsonString(string str)
    {
        return str.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}

[HarmonyPatch(typeof(LevelEventButton), nameof(LevelEventButton.enableButton), MethodType.Setter)]
public class LevelEventButtonEnableButtonPatch
{
    public static void Postfix(LevelEventButton __instance)
    {
        __instance.ShowAsSelected(false);
    }
}