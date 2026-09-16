using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace CustomUIFonts
{
    public static class FontApplier
    {
        private class VanillaState
        {
            public TMP_FontAsset Font;
            public Material Material;
            public bool HasSpecialMaterial;
            public float FontSize;
            public float FontSizeMin;
            public float FontSizeMax;
            public bool AutoSize;
        }

        private static readonly ConditionalWeakTable<TextMeshProUGUI, VanillaState> vanillaStates =
            new ConditionalWeakTable<TextMeshProUGUI, VanillaState>();

        private static readonly Dictionary<(Material, TMP_FontAsset), Material> materialCache =
            new Dictionary<(Material, TMP_FontAsset), Material>();

        private static readonly Dictionary<Material, (TMP_FontAsset font, Material material)> customMaterialToVanilla =
            new Dictionary<Material, (TMP_FontAsset, Material)>();

        private static VanillaState EnsureVanillaState(TextMeshProUGUI textComponent)
        {
            if (vanillaStates.TryGetValue(textComponent, out var vanilla))
                return vanilla;

            var font = textComponent.font;
            var material = textComponent.fontSharedMaterial;

            if (material != null && customMaterialToVanilla.TryGetValue(material, out var realVanilla))
            {
                font = realVanilla.font;
                material = realVanilla.material;
            }

            vanilla = new VanillaState
            {
                Font = font,
                Material = material,
                HasSpecialMaterial = font != null && material != font.material,
                FontSize = textComponent.fontSize,
                FontSizeMin = textComponent.fontSizeMin,
                FontSizeMax = textComponent.fontSizeMax,
                AutoSize = textComponent.enableAutoSizing
            };
            vanillaStates.Add(textComponent, vanilla);
            return vanilla;
        }

        public static void ApplyFont(TextMeshProUGUI textComponent, string fontName)
        {
            if (textComponent == null) return;

            var vanilla = EnsureVanillaState(textComponent);

            if (vanilla.Font == null || vanilla.Material == null || vanilla.HasSpecialMaterial)
                return;

            if (fontName == FontLibrary.DefaultOption)
            {
                if (textComponent.font != vanilla.Font || textComponent.fontSharedMaterial != vanilla.Material)
                {
                    textComponent.font = vanilla.Font;
                    textComponent.fontSharedMaterial = vanilla.Material;
                    textComponent.ForceMeshUpdate();
                    textComponent.SetAllDirty();
                }
                return;
            }

            var customFont = FontLibrary.LoadFont(fontName);
            if (customFont == null) return;

            var customMaterial = GetMaterialForFont(vanilla.Material, customFont);
            if (customMaterial == null) return;

            customMaterialToVanilla[customMaterial] = (vanilla.Font, vanilla.Material);

            if (textComponent.font != customFont || textComponent.fontSharedMaterial != customMaterial)
            {
                textComponent.font = customFont;
                textComponent.fontSharedMaterial = customMaterial;
                textComponent.ForceMeshUpdate();
                textComponent.SetAllDirty();
            }
        }

        public static void ApplyFontSize(TextMeshProUGUI textComponent, float multiplier)
        {
            if (textComponent == null) return;

            var vanilla = EnsureVanillaState(textComponent);

            var newSize = vanilla.FontSize * multiplier;
            var changed = false;

            if (!Mathf.Approximately(textComponent.fontSize, newSize))
            {
                textComponent.fontSize = newSize;
                changed = true;
            }

            if (vanilla.AutoSize)
            {
                var newMin = vanilla.FontSizeMin * multiplier;
                var newMax = vanilla.FontSizeMax * multiplier;

                if (!Mathf.Approximately(textComponent.fontSizeMin, newMin))
                {
                    textComponent.fontSizeMin = newMin;
                    changed = true;
                }
                if (!Mathf.Approximately(textComponent.fontSizeMax, newMax))
                {
                    textComponent.fontSizeMax = newMax;
                    changed = true;
                }
            }

            if (changed)
            {
                textComponent.ForceMeshUpdate();
                textComponent.SetAllDirty();
            }
        }

        public static void UpdateAllTextObjects(string fontName)
        {
            var multiplier = Plugin.FontSizeMultiplier.Value;

            foreach (var textComponent in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                ApplyFont(textComponent, fontName);
                ApplyFontSize(textComponent, multiplier);
            }

            Canvas.ForceUpdateCanvases();
        }

        public static void UpdateAllFontSizes()
        {
            var multiplier = Plugin.FontSizeMultiplier.Value;

            foreach (var textComponent in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
                ApplyFontSize(textComponent, multiplier);

            Canvas.ForceUpdateCanvases();
        }

        private static Material GetMaterialForFont(Material vanillaMaterial, TMP_FontAsset customFont)
        {
            if (vanillaMaterial == null || customFont == null || customFont.material == null)
                return null;

            var key = (vanillaMaterial, customFont);
            if (materialCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var newMaterial = new Material(vanillaMaterial);
            var fontMaterial = customFont.material;

            if (fontMaterial.HasProperty("_MainTex"))
                newMaterial.SetTexture("_MainTex", fontMaterial.GetTexture("_MainTex"));

            CopyFloat(newMaterial, fontMaterial, "_GradientScale");
            CopyFloat(newMaterial, fontMaterial, "_TextureWidth");
            CopyFloat(newMaterial, fontMaterial, "_TextureHeight");
            CopyFloat(newMaterial, fontMaterial, "_WeightNormal");
            CopyFloat(newMaterial, fontMaterial, "_WeightBold");
            CopyFloat(newMaterial, fontMaterial, "_ScaleRatioA");
            CopyFloat(newMaterial, fontMaterial, "_ScaleRatioB");
            CopyFloat(newMaterial, fontMaterial, "_ScaleRatioC");

            materialCache[key] = newMaterial;
            return newMaterial;
        }

        private static void CopyFloat(Material to, Material from, string propertyName)
        {
            if (from.HasProperty(propertyName))
                to.SetFloat(propertyName, from.GetFloat(propertyName));
        }
    }

    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    public static class TextOnEnablePatch
    {
        static void Postfix(TextMeshProUGUI __instance)
        {
            FontApplier.ApplyFont(__instance, Plugin.SelectedFont.Value);
            FontApplier.ApplyFontSize(__instance, Plugin.FontSizeMultiplier.Value);
        }
    }
}
