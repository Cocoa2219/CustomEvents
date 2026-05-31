using System.Collections.Generic;
using System.Reflection;
using ADOFAI;
using HarmonyLib;
using PropertyInfo = System.Reflection.PropertyInfo;

namespace CustomEvents.Event;

public static class CustomEventRunner
{
    private static readonly Dictionary<int, List<(CustomEvent, CustomEventData)>> _floorEventCache = new();

    public static void ClearCache()
    {
        _floorEventCache.Clear();
    }

    public static void AddEventToFloor(int floorId, LevelEvent ev, CustomEvent customEvent, CustomEventData eventData)
    {
        if (!_floorEventCache.TryGetValue(floorId, out var events))
        {
            events = [];
            _floorEventCache[floorId] = events;
        }

        events.Add((customEvent, eventData));
        
        ApplyProperties(ev, customEvent, eventData);
    }
    
    private static void ApplyProperties(LevelEvent ev, CustomEvent customEvent, CustomEventData eventData)
    {
        var props = eventData.Properties;
        
        foreach (var prop in props)
        {
            var member = eventData.PropertyMap[prop];
            if (member != null)
            {
                prop.Apply(member, ev, customEvent);
            }
        }
    }

    public static void RunFloorEvents(int floorId)
    {
        if (_floorEventCache.TryGetValue(floorId, out var events))
        {
            foreach (var (customEvent, eventData) in events)
            {
                customEvent.OnFloor();
            }
        }
    }
}

[HarmonyPatch(typeof(scnGame), nameof(scnGame.ApplyEventsToFloors), typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>))]
public class scnGameApplyEventsToFloorsPatch
{
    public static void Prefix()
    {
        CustomEventRunner.ClearCache();
    }
}

[HarmonyPatch(typeof(scnGame), nameof(scnGame.ApplyEvent))]
public class scnGameApplyEventPatch
{
    public static bool Prefix(scnGame __instance, LevelEvent evnt, float bpm, float pitch, List<scrFloor> floors, float offset = 0f, int? customFloorID = null)
    {
        if (CustomEventRegistry.CustomEvents.TryGetValue(evnt.eventType, out var customEvent) && customEvent != null)
        {
            if (CustomEventRegistry.CustomEventData.TryGetValue(evnt.eventType, out var eventData) && eventData != null)
            {
                var floorId = customFloorID ?? evnt.floor;
                CustomEventRunner.AddEventToFloor(floorId, evnt, customEvent, eventData);

                customEvent.OnApply();
            }
            
            return false; 
        }

        return true; 
    }
}

[HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
public class scrPlanetMoveToNextFloorPatch
{
    public static void Postfix(scrPlanet __instance, scrFloor floor)
    {
        var id = floor.seqID;
        
        CustomEventRunner.RunFloorEvents(id);
    }
}