using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace CustomUIFonts
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.customuifonts";
        public const string PluginName = "CustomUIFonts";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        public static ConfigEntry<string> SelectedFont;

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

            SelectedFont.SettingChanged += (sender, args) => FontApplier.UpdateAllTextObjects(SelectedFont.Value);

            new Harmony(PluginGuid).PatchAll();

            Log.LogInfo("[CustomUIFonts] Loaded. Active font: " + SelectedFont.Value);
        }

        private void Start()
        {
            FontApplier.UpdateAllTextObjects(SelectedFont.Value);
        }
    }
}
