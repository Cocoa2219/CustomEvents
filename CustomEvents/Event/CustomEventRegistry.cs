using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ADOFAI;
using ADOFAI.Serialization;
using GDMiniJSON;
using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;
using PropertyInfo = ADOFAI.PropertyInfo;

namespace CustomEvents.Event;

/// <summary>Registers, maps, and caches runtime metadata declarations for custom events and custom categories.</summary>
public static class CustomEventRegistry
{
    private static Harmony _harmony;

    /// <summary>Gets the registered custom event definitions indexed by their calculated runtime <see cref="LevelEventType"/>.</summary>
    public static IReadOnlyDictionary<LevelEventType, CustomEvent> CustomEvents => _customEvents;
    private static readonly Dictionary<LevelEventType, CustomEvent> _customEvents = new();

    /// <summary>Gets the compiled property parsing metadata indices mapped by their runtime <see cref="LevelEventType"/>.</summary>
    public static IReadOnlyDictionary<LevelEventType, CustomEventData> CustomEventData => _customEventData;
    private static readonly Dictionary<LevelEventType, CustomEventData> _customEventData = new();

    /// <summary>Gets the lookup index that translates fully qualified custom string names directly into explicit <see cref="LevelEventType"/> values.</summary>
    public static IReadOnlyDictionary<string, LevelEventType> NameToIdCache => _nameToIdCache;
    private static readonly Dictionary<string, LevelEventType> _nameToIdCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the configured metadata parameters indexed by their dynamically allocated <see cref="LevelEventCategory"/> key.</summary>
    public static IReadOnlyDictionary<LevelEventCategory, (Sprite icon, string localizationKey)> CategoryCache => _categoryCache;
    private static readonly Dictionary<LevelEventCategory, (Sprite, string)> _categoryCache = new();

    /// <summary>Gets the registered group icons used to customize inspector panels, indexed by composite keys of <see cref="LevelEventType"/> and group name strings.</summary>
    public static IReadOnlyDictionary<(LevelEventType info, string group), Sprite> GroupIconCache => _groupIconCache;
    private static readonly Dictionary<(LevelEventType info, string group), Sprite> _groupIconCache = new();
    
    internal static Dictionary<string, (LevelEventInfo info, List<LevelEventCategory> categoryIds)> EventsToAdd { get; } =
        new();

    internal static Dictionary<LevelEventCategory, Sprite> EventCategoryIconsToAdd { get; } = new();

    internal static Dictionary<LevelEventType, string> KeysToAdd { get; } = new();
    internal static Dictionary<Assembly, Dictionary<PropertyInfo, string>> DefaultValueToAdd { get; } = new();
    
    /// <summary>Registers a <see cref="CustomEvent"/> instance into the engine state pipelines and initializes runtime tracking hooks.</summary>
    /// <param name="ev">The custom event instance to register and map across the engine systems.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ev"/> evaluates as null.</exception>
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
        
        if (ev == null) throw new ArgumentNullException(nameof(ev));

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

        if (GCS.levelEventTypeString == null)
            KeysToAdd[id] = name;
        else
            GCS.levelEventTypeString[id] = name;

        _customEvents[id] = ev;
    }

    /// <summary>Loops through an ordered sequence of <see cref="CustomEvent"/> items to register them into the system in batch.</summary>
    /// <param name="events">The incoming collection containing target custom events models.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="events"/> evaluates as null.</exception>
    public static void RegisterCustomEvents(IEnumerable<CustomEvent> events)
    {
        if (events == null) throw new ArgumentNullException(nameof(events));
        foreach (var ev in events) RegisterCustomEvent(ev);
    }

    /// <summary>Reflectively gathers and initializes all concrete, non-abstract classes extending <see cref="CustomEvent"/> present inside the calling assembly.</summary>
    public static void RegisterAll()
    {
        var allEvents = Assembly.GetCallingAssembly().GetTypes()
            .Where(type => typeof(CustomEvent).IsAssignableFrom(type) && !type.IsAbstract)
            .Select(type => (CustomEvent)Activator.CreateInstance(type));

        RegisterCustomEvents(allEvents);
    }

    /// <summary>Registers a localized category into the system and links it to the provided visual icon and localization key for editor display.</summary>
    /// <param name="categoryName">The tracking key string assigned to represent the unique grouping identifier.</param>
    /// <param name="icon">The asset tracking texture used to illustrate tab views inside the editor interface.</param>
    /// <param name="localizationKey">The target localization key used to resolve the display string for this category header.</param>
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

/// <summary>Harmony patch container targeting <see cref="ADOStartup.DecodeLevelEventInfoList"/>.</summary>
/// <remarks>This patch intercepts static enum reading steps to let the system accurately process unrecognized extension event types parameters entries paths indices mappings.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(ADOStartup), nameof(ADOStartup.DecodeLevelEventInfoList))]
public class DecodeLevelEventInfoListPatch
{
    /// <summary>Transpiles the text parsing routines, replacing native enum conversion instructions with loose custom formatting identifiers parsers.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
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

/// <summary>Harmony patch container targeting <see cref="scnEditor.LoadEditorProperties"/>.</summary>
/// <remarks>This patch injects dynamic lookup bounds definitions, allowing custom collection segments to map without triggering structural array errors.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.LoadEditorProperties))]
public class scnEditorLoadEditorPropertiesPatch
{
    /// <summary>Alters internal level category indexing arrays generation steps, linking our registry data lists seamlessly inside engine generation loops blocks.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
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

/// <summary>Harmony patch container targeting <see cref="scnEditor.SetupFavoritesCategory"/>.</summary>
/// <remarks>This patch hooks internal configuration storage updates, ensuring custom elements map consistently into save files and active configuration trackers profiles parameters references.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.SetupFavoritesCategory))]
public class scnEditorSetupFavoritesCategory
{
    /// <summary>Rewrites validation checks parameters filters, enabling extended selection listings tracking layers to parse validation entries correctly variables paths elements tracking.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
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

/// <summary>Harmony patch container targeting <see cref="InspectorPanel.Init"/>.</summary>
/// <remarks>This patch intercepts editor drawing logic to load dynamic tab elements and handle asset tracking indicators correctly.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.Init))]
public class InspectorPanelInitPatch
{
    /// <summary>Alters initialization data flow logic to prevent out-of-bounds selection index runtime bugs when processing extension elements variables blocks.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var method = typeof(RDUtils).GetMethod(nameof(RDUtils.ParseEnum));
        var closedMethod = method!.MakeGenericMethod(typeof(LevelEventType));
        var idx = newInstructions.FindIndex(i => i.Calls(closedMethod));
        newInstructions[idx].operand = Method(typeof(Utils), nameof(Utils.LooselyParseEventType));

        foreach (var instruction in newInstructions) yield return instruction;
    }

    /// <summary>Injects registered visual icon segments into generated custom event inspector panels.</summary>
    /// <param name="__instance">The active instance reference of the targeted initializing UI <see cref="InspectorPanel"/> controller.</param>
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

/// <summary>Harmony patch container targeting internal level event image assets loading routines blocks.</summary>
/// <remarks>This patch dynamically appends missing key entries onto active runtime dictionary collections to register icon maps dependencies rules elements lines.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch]
public class LoadLevelEventSpritesPatch
{
    /// <summary>Resolves the target asset loading method context structure dynamically using criteria matching operations steps parameters logs.</summary>
    /// <returns>Returns the verified <see cref="MethodBase"/> handle tracking targeting parameters context locations layers pathways structural markers mappings entries indexes markers indicators.</returns>
    public static MethodBase TargetMethod()
    {
        return FirstMethod(typeof(scnEditor), m =>
            m.Name.Contains("LoadLevelEventSprites") && m.GetParameters().Length == 0);
    }

    /// <summary>Appends all registered custom events icon indicators texture profiles variables onto global engine caches tables.</summary>
    /// <param name="__instance">The active instance reference of the executing <see cref="scnEditor"/> lifecycle manager.</param>
    public static void Postfix(scnEditor __instance)
    {
        foreach (var kvp in CustomEventRegistry.CustomEvents)
        {
            if (GCS.levelEventIcons.ContainsKey(kvp.Key)) continue;
            GCS.levelEventIcons.Add(kvp.Key, kvp.Value.GetIcon());
        }
    }
}

/// <summary>Harmony patch container targeting <see cref="LevelEventButton.Init"/>.</summary>
/// <remarks>This patch adjusts label loading systems to correctly populate extended event display descriptors blocks lists.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(LevelEventButton), nameof(LevelEventButton.Init))]
public class LevelEventButtonInitPatch
{
    /// <summary>Replaces the native structural string lookup call instructions with our multi-assembly localization conversion systems pipelines markers layers pathways indices.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
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

/// <summary>Harmony patch container targeting <see cref="ADOStartup.SetupLevelEventsInfo"/>.</summary>
/// <remarks>This patch safely pushes extension properties and dynamic configurations into native collection frameworks after initialization.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(ADOStartup), nameof(ADOStartup.SetupLevelEventsInfo))]
public class ADOStartupSetupLevelEventsInfoPatch
{
    /// <summary>Flushes delayed initialization objects data maps onto active game caches maps pools layers cleanly after internal initialization logic steps wrap.</summary>
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

/// <summary>Harmony patch container targeting the <see cref="LevelEvent"/> instance construction rules workflows pipeline.</summary>
/// <remarks>This patch ensures unique tracking key string signatures remain completely pristine when passing down initialization blocks pipelines metrics.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(LevelEvent), MethodType.Constructor, typeof(int), typeof(LevelEventType), typeof(LevelEventInfo),
    typeof(Dictionary<string, object>), typeof(Dictionary<string, bool>), typeof(bool), typeof(bool), typeof(bool))]
public class LevelEventConstructorPatch
{
    /// <summary>Alters bytecode mapping pathways logic, using dynamic custom string conversion steps instead of standard enum-to-index tracking logic blocks parameters operations trackers context layers structures entries.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
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

/// <summary>Harmony patch container targeting <see cref="InspectorPanel.ShowPanel"/>.</summary>
/// <remarks>This patch updates inspector UI panel logic to prevent title translation string resolution errors during selection changes.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.ShowPanel))]
public class InspectorPanelShowPanelPatch
{
    /// <summary>Intercepts structural header text assignments, inserting loose validation parameters handling string mapping structures variables elements paths tracking indices indicators options profiles context layers profiles tags.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
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

/// <summary>Harmony patch container targeting <see cref="LevelEvent.Encode"/>.</summary>
/// <remarks>This patch updates serialization logic to prevent custom type information from being lost or corrupted during save operations.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(LevelEvent), nameof(LevelEvent.Encode))]
public class LevelEventEncodePatch
{
    /// <summary>Rewrites serialization streams bytecode operations markers, forcing structural identifier translation steps to preserve string values configurations parameters checks indices lines.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
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

/// <summary>Harmony patch container targeting <see cref="LevelEvent.Decode"/>.</summary>
/// <remarks>This patch intercepts level loading data deserialization streams to map custom types back into memory safely.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(LevelEvent), nameof(LevelEvent.Decode))]
public class LevelEventDecodePatch
{
    /// <summary>Adjusts structural parser initialization pathways, directing dynamic parsing operations through our loose string-matching pipeline hooks markers layers.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
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

/// <summary>Harmony patch container targeting internal level category icon loading methods configurations routines.</summary>
/// <remarks>This patch appends extension layout visual sprites into active memory arrays context lines securely.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch]
public class scnEditorLoadLevelCategorySpritesPatch
{
    /// <summary>Resolves the target asset rendering logic module context structures using criteria mapping logic patterns records values checks indices indicators markers elements profiles.</summary>
    /// <returns>Returns the verified target <see cref="MethodBase"/> parameters markers identifiers context locations indicators mappings paths blocks registries profiles logs entries options pipelines.</returns>
    public static MethodBase TargetMethod()
    {
        return FirstMethod(typeof(scnEditor), m =>
            m.Name.Contains("LoadLevelCategorySprites") && m.GetParameters().Length == 0);
    }

    /// <summary>Populates active system categories texture lookup blocks pools structures with registered custom extension icons data profiles parameters checks entries objects maps trackers lists.</summary>
    public static void Postfix()
    {
        foreach (var kvp in CustomEventRegistry.EventCategoryIconsToAdd) GCS.eventCategoryIcons.Add(kvp.Key, kvp.Value);
        CustomEventRegistry.EventCategoryIconsToAdd.Clear();
    }
}

/// <summary>Harmony patch container targeting <see cref="CategoryTab"/> layouts workflows modules control parameters tracking entries indicators flags.</summary>
/// <remarks>This patch intercepts layout icon population pipelines to inject visual indicators for custom categories.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(CategoryTab))]
public class CategoryTabPatch
{
    /// <summary>Alters layout drawing code instructions, replacing native cache assignments with safe verification wrapper calls to resolve textures securely context parameters markers options variables flags elements structures layers profiles context parameters tracking.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
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

    /// <summary>Resolves a category icon, falling back to <see cref="LevelEventCategory.Gameplay"/> if no specific texture asset path is assigned.</summary>
    /// <param name="category">The targeting level event category data parameters entries flags tracking values indices maps metrics objects layers context rules.</param>
    /// <returns>Returns the verified sprite element object configuration container profile parameters references.</returns>
    public static Sprite SafelyGetCategorySprite(LevelEventCategory category)
    {
        if (GCS.eventCategoryIcons.TryGetValue(category, out var sprite)) return sprite;
        if (CustomEventRegistry.CategoryCache.TryGetValue(category, out var customSprite)) return customSprite.icon;
        
        Debug.LogWarning($"No sprite found for category {category}, using default icon.");
        return GCS.eventCategoryIcons[LevelEventCategory.Gameplay];
    }
}

/// <summary>Harmony patch container targeting editor category layout pagination execution routines paths modules trackers parameters structures mappings profiles definitions.</summary>
/// <remarks>This patch adjusts bounds validation checks during editor pagination loops to prevent array exceptions when iterating custom categories.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(scnEditor), "CycleEventsPage")]
public class scnEditorCycleEventsPagePatch
{
    /// <summary>Modifies internal page index calculation math blocks logic variables parameters metrics pipelines data pathways entries tracking indices flags options controls.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
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

/// <summary>Harmony patch container targeting <see cref="scnEditor.RemoveEventAtSelected"/>.</summary>
/// <remarks>This patch ensures custom events display their correct names instead of falling back to default placeholders when removed from a floor sequence.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(scnEditor), nameof(scnEditor.RemoveEventAtSelected))]
public class scnEditorRemoveEventAtSelectedPatch
{
    /// <summary>Rewrites deletion logging pipelines bytecode operations to safely preserve dynamic display names entries markers pathways configurations parameters maps checks lines tracking.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
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

/// <summary>Harmony patch container targeting <see cref="InspectorPanel.ShowTabsForFloor"/>.</summary>
/// <remarks>This patch adjusts runtime type collection logic to render tracking markers for custom events alongside built-in categories.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(InspectorPanel), nameof(InspectorPanel.ShowTabsForFloor))]
public class InspectorPanelShowTabsForFloorPatch
{
    /// <summary>Alters data array generation pathways, inserting our extended enumeration array builders to expose custom types options variables elements markers tracks maps pipelines.</summary>
    /// <param name="instructions">The active IL compilation instructions sequence undergoing manipulation steps paths metrics.</param>
    /// <returns>Returns the modified bytecode streams tracking instructions updated structure parameters references.</returns>
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var newInstructions = new List<CodeInstruction>(instructions);
        var idx = newInstructions.FindIndex(i => i.Calls(Method(typeof(Enum), nameof(Enum.GetValues))));
        newInstructions[idx].operand = Method(typeof(InspectorPanelShowTabsForFloorPatch), nameof(GetExtendedValues));

        foreach (var instruction in newInstructions) yield return instruction;
    }

    /// <summary>Combines native level event types with our active registry keys to provide a unified array for UI binding containers.</summary>
    /// <param name="type">The incoming system reflection enum type parameter model undergoing processing validations calculations metrics layers.</param>
    /// <returns>Returns an array containing both standard and extended custom event types.</returns>
    public static LevelEventType[] GetExtendedValues(Type type)
    {
        var original = (LevelEventType[])Enum.GetValues(type);

        var result = original.ToList();
        result.AddRange(CustomEventRegistry.CustomEvents.Keys.Select(id => id));
        return result.ToArray();
    }
}

/// <summary>Harmony patch container targeting <see cref="LevelData.EncodeToDictionary"/>.</summary>
/// <remarks>This patch strips out custom events before native serialization and packages them as editor comments to allow backward compatibility without breaking vanilla game saves.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(LevelData), nameof(LevelData.EncodeToDictionary))]
public class LevelDataEncodePatch
{
    private static readonly Dictionary<int, LevelEvent> _customEventsToAdd = new();

    /// <summary>Isolates custom events from the native levels list map to safeguard vanilla level compilation blocks pipelines variables.</summary>
    /// <param name="__instance">The active instance reference of the serializing <see cref="LevelData"/> container model parameters.</param>
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

    /// <summary>Appends safety notice remarks cards blocks and serializes custom event objects arrays directly inside level files dictionaries structures entries.</summary>
    /// <param name="__instance">The active instance reference of the serializing <see cref="LevelData"/> container model parameters.</param>
    /// <param name="__result">The output dictionary tracking serialization key-value parameters references produced by the default execution flow pipeline.</param>
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

        // Restore original working levelEvents array order so native runtime data calculations remain uninterrupted
        foreach (var kvp in _customEventsToAdd)
        {
            var index = kvp.Key;
            var levelEvent = kvp.Value;

            __instance.levelEvents.Insert(index, levelEvent);
        }

        _customEventsToAdd.Clear();
    }
}

/// <summary>Harmony patch container targeting <see cref="LevelData.Decode"/>.</summary>
/// <remarks>This patch intercepts post-deserialization data states to capture the structured text markup strings within <see cref="LevelEventType.EditorComment"/> fields and extract hidden custom events back into memory.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(LevelData), nameof(LevelData.Decode))]
public class LevelDataDecodePatch
{
    /// <summary>Iterates through decoded level entries to isolate, unpack, and reconstitute serialized custom events from embedded comment headers.</summary>
    /// <param name="__instance">The active structural reference of the deserialized <see cref="LevelData"/> container framework undergoing recovery operations.</param>
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

    /// <summary>Strips escaping backslash artifacts added during textual multi-line JSON encoding layers inside the comment block parameters.</summary>
    /// <param name="str">The escaped raw payload text stream extracted from the level file structure.</param>
    /// <returns>A normalized structural string ready for generic key-value dictionary tokenization parsing.</returns>
    private static string UnescapeJsonString(string str)
    {
        return str.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}

/// <summary>Harmony patch container targeting the property setter for <see cref="LevelEventButton.enableButton"/>.</summary>
/// <remarks>This patch captures activation toggles to force real-time visual UI layout components synchronizations when selections clear layout trees.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(LevelEventButton), nameof(LevelEventButton.enableButton), MethodType.Setter)]
public class LevelEventButtonEnableButtonPatch
{
    /// <summary>Forcibly overrides visual focus parameters state metrics when an action button state transitions into an inactive/disabled condition profile.</summary>
    /// <param name="__instance">The active instance tracking reference of the undergoing state assignment layout component <see cref="LevelEventButton"/>.</param>
    public static void Postfix(LevelEventButton __instance)
    {
        __instance.ShowAsSelected(false);
    }
}