using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace CustomUIFonts
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.hj.customuifonts";
        public const string PluginName = "CustomUIFonts";
        public const string PluginVersion = "1.2.0";

        internal static ManualLogSource Log;

        public const float DefaultFontSizeMultiplier = 1.0f;

        public static ConfigEntry<string> SelectedFont;
        public static ConfigEntry<float> FontSizeMultiplier;

        private static bool suppressSizeUpdate;

        private void Awake()
        {
            Log = Logger;

            var fontNames = FontLibrary.DiscoverFontNames();

            SelectedFont = Config.Bind(
                "Font",
                "UI Font",
                FontLibrary.DefaultOption,
                new ConfigDescription(
                    "The Font used for the UI.",
                    new AcceptableValueList<string>(fontNames.ToArray())
                )
            );

            FontSizeMultiplier = Config.Bind(
                "Font",
                "Font Size",
                DefaultFontSizeMultiplier,
                new ConfigDescription(
                    "Scales every UI text up or down relative to its own default size. " +
                    "1.0 is the vanilla size. Resets to 1.0 whenever you change the font.",
                    new AcceptableValueRange<float>(0.5f, 2.0f)
                )
            );

            SelectedFont.SettingChanged += (sender, args) =>
            {
                suppressSizeUpdate = true;
                FontSizeMultiplier.Value = DefaultFontSizeMultiplier;
                suppressSizeUpdate = false;

                FontApplier.UpdateAllTextObjects(SelectedFont.Value);
            };

            FontSizeMultiplier.SettingChanged += (sender, args) =>
            {
                if (suppressSizeUpdate) return;
                FontApplier.UpdateAllFontSizes();
            };

            new Harmony(PluginGuid).PatchAll();

            Log.LogInfo("[CustomUIFonts] Loaded. Active font: " + SelectedFont.Value);
        }

        private void Start()
        {
            FontApplier.UpdateAllTextObjects(SelectedFont.Value);
        }
    }
}
