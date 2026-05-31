using System.IO;
using CustomEvents.Event;
using UnityModManagerNet;

namespace CustomEvents.Example
{
    public static class Loader
    {
        public static bool Setup(UnityModManager.ModEntry entry)
        {
            CustomEventRegistry.RegisterAll();
            LocalizationRegistry.RegisterJson(File.ReadAllText(Path.Combine(entry.Path, "TestEvent.Localization.json")));

            return true;
        }
    }
}