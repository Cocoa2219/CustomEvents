using System.Collections.Generic;
using System.ComponentModel;
using ADOFAI;
using HarmonyLib;

namespace CustomEvents.Event;

/// <summary>Tracks custom events while floors are being processed.</summary>
public static class CustomEventRunner
{
    private static readonly Dictionary<int, List<(CustomEvent, CustomEventData)>> _floorEventCache = new();

    /// <summary>Clears the cached floor event state.</summary>
    public static void ClearCache()
    {
        _floorEventCache.Clear();
    }

    /// <summary>Adds a custom event to a floor and applies its serialized members.</summary>
    /// <param name="floorId">The floor identifier to cache against.</param>
    /// <param name="ev">The level event that receives the applied property values.</param>
    /// <param name="customEvent">The custom event instance to cache.</param>
    /// <param name="eventData">The generated metadata for <paramref name="customEvent"/>.</param>
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

    /// <summary>Runs every cached custom event for a floor.</summary>
    /// <param name="floorId">The floor identifier whose cached events are executed.</param>
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

/// <summary>Harmony patch container targeting <see cref="scnGame.ApplyEventsToFloors(List{scrFloor}, LevelData, scrLevelMaker, List{LevelEvent})"/>.</summary>
/// <remarks>This patch resets the floor event cache completely before new level data calculations begin.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(scnGame), nameof(scnGame.ApplyEventsToFloors), typeof(List<scrFloor>), typeof(LevelData), typeof(scrLevelMaker), typeof(List<LevelEvent>))]
public class scnGameApplyEventsToFloorsPatch
{
    /// <summary>Clears the custom floor event cache immediately before the game applies level event structures.</summary>
    public static void Prefix()
    {
        CustomEventRunner.ClearCache();
    }
}

/// <summary>Harmony patch container targeting <see cref="scnGame.ApplyEvent"/>.</summary>
/// <remarks>This patch intercepts standard event application routines to redirect recognized custom events into our custom mapping runner pipeline.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(scnGame), nameof(scnGame.ApplyEvent))]
public class scnGameApplyEventPatch
{
    /// <summary>Intercepts event applications, routing matching identifiers into the custom execution cache.</summary>
    /// <param name="__instance">The active instance of the <see cref="scnGame"/> controller.</param>
    /// <param name="evnt">The target <see cref="LevelEvent"/> currently undergoing application checks.</param>
    /// <param name="bpm">The current active Beats Per Minute calculation factor.</param>
    /// <param name="pitch">The active pitch speed modifier value.</param>
    /// <param name="floors">The list containing level sequence track <see cref="scrFloor"/> segments.</param>
    /// <param name="offset">The timing offset parameter value applied to calculations.</param>
    /// <param name="customFloorID">An optional override tracking sequence identifier values instead of standard event sequences.</param>
    /// <returns>Returns <see langword="false"/> if a custom event matches and original execution is skipped; otherwise, <see langword="true"/> to allow fallback default tracking processing.</returns>
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

/// <summary>Harmony patch container targeting <see cref="scrPlanet.MoveToNextFloor"/>.</summary>
/// <remarks>This patch captures post-movement frames to trigger stored event calculations contextually alongside standard mechanical actions.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
public class scrPlanetMoveToNextFloorPatch
{
    /// <summary>Triggers custom tracked events assigned to the floor directly after the planet advances its positional indexing.</summary>
    /// <param name="__instance">The active instance of the moving <see cref="scrPlanet"/> object structure.</param>
    /// <param name="floor">The target destination track segment <see cref="scrFloor"/>.</param>
    public static void Postfix(scrPlanet __instance, scrFloor floor)
    {
        var id = floor.seqID;
        
        CustomEventRunner.RunFloorEvents(id);
    }
}