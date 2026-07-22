using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace CustomUIFonts
{
    public static class FontLibrary
    {
        public const string DefaultOption = "Default (Vanilla)";

        private static readonly Dictionary<string, TMP_FontAsset> loadedFonts =
            new Dictionary<string, TMP_FontAsset>();

        private static readonly Dictionary<string, string> bundlePaths =
            new Dictionary<string, string>();

        public static string FontsDirectory
        {
            get
            {
                var pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(pluginFolder, "Fonts");
            }
        }

        public static List<string> DiscoverFontNames()
        {
            var names = new List<string> { DefaultOption };

            if (!Directory.Exists(FontsDirectory))
            {
                // tell the user why the dropdown is empty instead of failing silently
                Plugin.Log.LogWarning("[CustomUIFonts] Fonts folder is missing - no custom fonts will be loaded. " +
                    "Created an empty one at: " + FontsDirectory + " - put your font .bundle files in there.");
                Directory.CreateDirectory(FontsDirectory);
                return names;
            }

            foreach (var path in Directory.GetFiles(FontsDirectory, "*.bundle"))
            {
                // the file name (without the .bundle) will pop up in dropdown
                var name = Path.GetFileNameWithoutExtension(path);
                bundlePaths[name] = path;
                names.Add(name);
            }

            // only the vanilla option means the folder had no bundles in it
            if (names.Count == 1)
            {
                Plugin.Log.LogWarning("[CustomUIFonts] No font .bundle files found in: " + FontsDirectory +
                    " - only the vanilla font will be available.");
            }

            return names;
        }

        public static TMP_FontAsset LoadFont(string name)
        {
            if (string.IsNullOrEmpty(name) || name == DefaultOption)
                return null;

            if (loadedFonts.TryGetValue(name, out var alreadyLoaded))
                return alreadyLoaded;

            if (!bundlePaths.TryGetValue(name, out var path))
                return null;

            var bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                Plugin.Log.LogError("[CustomUIFonts] Failed to load asset bundle at " + path +
                    ". It was probably built incorrectly.");
                return null;
            }

            var fontsInBundle = bundle.LoadAllAssets<TMP_FontAsset>();
            if (fontsInBundle.Length == 0)
            {
                Plugin.Log.LogError("[CustomUIFonts] No TMP_FontAsset found inside " + path);
                bundle.Unload(false);
                return null;
            }

            var font = fontsInBundle[0];
            loadedFonts[name] = font;
            return font;
        }
    }
}
