using CustomEvents.Event;
using UnityEngine;

namespace CustomEvents.Example
{
    public enum TestMode
    {
        Basic,
        Advanced,
        Experimental
    }

    public enum TestQuality
    {
        Low,
        Medium,
        High
    }

    [EventName("TestEvent.displayName")]
    [EventCategory("TestEvents", "Gameplay")]
    [EventPropGroupDefinition("Basics", isDefault: true)]
    [EventPropGroupDefinition("Advanced")]
    public class TestEvent : CustomEvent
    {
        public override bool AllowFirstFloor => true;

        [EventProperty(Unit = "beats", Placeholder = "TestEvent.Message.placeholder", AffectsFloors = true)]
        [PropertyString(0, 140, true, true)]
        [PropertyLabel(LocalizationKey = "TestEvent.Message.label")]
        [PropertyRequired]
        [PropertyHelp("TestEvent.Message.help.text", ButtonURL = "TestEvent.Message.help.buttonURL",
            ButtonTextKey = "TestEvent.Message.help.buttonText")]
        [PropertyGroup(Name = "Basics")]
        public string Message { get; set; } = "Hello from TestEvent";

        [EventProperty(AffectsPath = true)]
        [PropertyGroup(Name = "Basics")]
        public bool LogToConsole { get; set; } = true;

        [EventProperty]
        [PropertyEnum]
        [PropertyGroup(Name = "Basics")]
        public TestMode Mode { get; set; } = TestMode.Basic;

        [EventProperty]
        [PropertyEnum]
        [PropertyGroup(Name = "Basics")]
        public TestQuality Quality { get; set; } = TestQuality.Medium;

        [EventProperty(Unit = "seconds")]
        [PropertyFloat(0f, 10f)]
        [PropertyEnableIf("Mode", "Advanced")]
        [PropertyGroup(Name = "Advanced")]
        public float AdvancedDelay { get; set; } = 0.5f;

        [EventProperty]
        [PropertyInt(0, 100)]
        [PropertyDisableIf("Quality", "Low")]
        [PropertyGroup(Name = "Advanced")]
        public int AdvancedPower { get; set; } = 25;

        [EventProperty]
        [PropertyString(0, 32)]
        [PropertyShowIf("Mode", "Experimental")]
        [PropertyGroup(Name = "Advanced")]
        public string ExperimentalTag { get; set; } = "EXP";

        [EventProperty]
        [PropertyLongInputField]
        [PropertyHideIf("Mode", "Basic")]
        [PropertyGroup(Name = "Advanced")]
        public string LongDescription { get; set; } = "Detailed description for testing.";

        [EventProperty]
        [PropertyColorPicker(true)]
        [PropertyToggleable(true)]
        [PropertyGroup(Name = "Advanced")]
        public Color Tint { get; set; } = new Color(1f, 0.3f, 0.3f, 0.6f);

        [PropertyFloatPair(0f, 8f, isRange: true)]
        [EventProperty(Unit = "beats", IgnoreRange = true)]
        [PropertyGroup(Name = "Advanced")]
        public (float, float) BeatRange { get; set; } = (0.5f, 2.5f);

        [EventProperty]
        [PropertyVector2(-5f, 5f, -5f, 5f)]
        [PropertyGroup(Name = "Advanced")]
        public Vector2 Offset { get; set; } = new Vector2(1f, -1f);

        [EventProperty]
        [PropertyVector2Range(true)]
        [PropertyGroup(Name = "Advanced")]
        public (Vector2, Vector2) Bounds { get; set; } =
            (new Vector2(-1f, -1f), new Vector2(1f, 1f));

        [EventProperty]
        [PropertyTile(0, 32)]
        [PropertyGroup(Name = "Basics")]
        public (int, TileRelativeTo) TargetTile { get; set; } = (0, TileRelativeTo.ThisTile);

        [EventProperty]
        [PropertyFilePicker]
        [PropertyGroup(Name = "Basics")]
        public string AudioFile { get; set; } = "";

        [EventProperty]
        [PropertyRating]
        [PropertyLabel("Difficulty Rating")]
        [PropertyGroup(Name = "Basics")]
        public int Rating { get; set; } = 3;

        [EventProperty]
        [PropertyNote]
        [PropertyGroup(Name = "Basics")]
        public string InfoNote { get; set; } = "TestEvent.note";

        [EventProperty(Name = "renamed_value")]
        [PropertyLabel(LocalizationKey = "TestEvent.renamed_value.label")]
        [PropertyGroup(Name = "Basics")]
        public int RenamedValue { get; set; } = 7;

        [EventProperty(IsDevOnly = true, ShouldEncode = false)]
        [PropertyLabel("Dev Only Debug")]
        [PropertyGroup(Name = "Advanced")]
        public string DevOnlyDebug { get; set; } = "Debug";

        public override void OnApply()
        {
            if (LogToConsole)
                Debug.Log($"TestEvent Apply: {Message}");
        }

        public override void OnFloor()
        {
            if (LogToConsole && Mode != TestMode.Basic)
                Debug.Log($"TestEvent Floor: Mode={Mode}, Quality={Quality}, Power={AdvancedPower}");
        }
    }
}