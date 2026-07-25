using System.Collections.Generic;
using System.Linq;
using ADOFAI;
using HarmonyLib;
using UnityEngine;

namespace CustomEvents.Event;

public static class CustomSettingsRegistry
{
    public static IReadOnlyDictionary<LevelEventType, CustomSettingsData> SettingsData => _settingsData;
    private static readonly Dictionary<LevelEventType, CustomSettingsData> _settingsData = new();

    internal static List<CustomSettingsData> SettingsToAdd { get; } = [];

    public static void AddSettings(string id, GameObject prefab, Sprite icon = null)
    {
        HarmonyPatchManager.TryPatch();

        var kvp = ADOStartup.DecodeLevelEventInfoList([new Dictionary<string, object>()
        {
            { "name", id },
            { "stretchViewport", true },
            { "executionTime", nameof(LevelEventExecutionTime.Special) }
        }]).FirstOrDefault();

        if (kvp.Value == null)
        {
            return;
        }

        var type = CustomEventTypeRegistry.GetNewID();
        var settingsData = new CustomSettingsData(kvp.Value, type, id, prefab, icon);
        _settingsData.Add(type, settingsData);
        kvp.Value.type = type;

        if (GCS.settingsInfo == null)
        {
            SettingsToAdd.Add(settingsData);
            return;
        }

        GCS.settingsInfo.Add(kvp.Key, kvp.Value);
        GCS.levelEventTypeString.Add(type, id);
    }

    public static bool TryGetCustomSetting(string name, out CustomSettingsData setting)
    {
        setting = null;
        var s = _settingsData.FirstOrDefault(x => x.Value.Id == name);

        if (s.Value != null)
        {
            setting = s.Value;
            return true;
        }

        return false;
    }
}

public class CustomSettingsData(LevelEventInfo @event, LevelEventType type, string id, GameObject prefab, Sprite icon)
{
    public LevelEventInfo Event { get; } = @event;
    public LevelEventType Type { get; } = type;
    public string Id { get; } = id;
    public GameObject Prefab { get; } = prefab;
    public Sprite Icon { get; } = icon;
}

[HarmonyPatch(typeof(ADOStartup), nameof(ADOStartup.SetupLevelEventsInfo))]
public class ADOStartupSetupLevelEventsInfoSettingsPatch
{
    public static void Postfix()
    {
        foreach (var data in CustomSettingsRegistry.SettingsToAdd)
        {
            GCS.settingsInfo.Add(data.Id, data.Event);
            GCS.levelEventTypeString.Add(data.Type, data.Id);
        }
    }
}
