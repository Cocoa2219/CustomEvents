using System;
using HarmonyLib;

namespace CustomEvents.Event;

public static class HarmonyPatchManager
{
    private static Harmony _harmony;

    public static void TryPatch()
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
    }
}