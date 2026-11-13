using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using AndesSim.UI;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Configura e instala las fuentes tipográficas modernas (Inter + Roboto Bold + Roboto Medium)
    /// en formato TextMesh Pro SDF Dinámico con soporte completo de español (tildes, eñes, símbolos).
    /// Evita cualquier advertencia de glifos faltantes en consola.
    /// </summary>
    public static class AndesFontSetupUtility
    {
        private const string LogPrefix = "[ANDES-SIM - Fuentes]";
        private const string ResourcesFontsDir = "Assets/_AndesSim/Resources/Fonts";

        private const string InterTtfPath        = "Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf";
        private const string RobotoBoldTtfPath   = "Assets/_AndesSim/Fonts/Roboto-Bold.ttf";
        private const string RobotoMediumTtfPath = "Assets/_AndesSim/Fonts/Roboto-Medium.ttf";

        private const string InterSdfAssetPath        = ResourcesFontsDir + "/Inter-AndesSim SDF.asset";
        private const string RobotoBoldSdfAssetPath   = ResourcesFontsDir + "/Roboto-Bold-AndesSim SDF.asset";
        private const string RobotoMediumSdfAssetPath = ResourcesFontsDir + "/Roboto-Medium-AndesSim SDF.asset";
        private const string LiberationFallbackPath   = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string TmpSettingsPath          = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("ANDES-SIM/Estilos y Fuentes/1. Generar y Activar Fuentes Modernas (Inter + Roboto)")]
        public static void EnsureModernFontsInstalled()
        {
            Directory.CreateDirectory(ResourcesFontsDir);

            TMP_FontAsset interSdf        = BuildOrUpdateDynamicSdf(InterTtfPath, InterSdfAssetPath, "Inter-AndesSim SDF");
            TMP_FontAsset robotoBoldSdf   = BuildOrUpdateDynamicSdf(RobotoBoldTtfPath, RobotoBoldSdfAssetPath, "Roboto-Bold-AndesSim SDF");
            TMP_FontAsset robotoMediumSdf = BuildOrUpdateDynamicSdf(RobotoMediumTtfPath, RobotoMediumSdfAssetPath, "Roboto-Medium-AndesSim SDF");

            UpdateTmpGlobalSettings(interSdf, robotoBoldSdf, robotoMediumSdf);
            AndesTypographyTheme.ResetCache();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"{LogPrefix} Fuentes modernas configuradas: Inter (cuerpo), Roboto Bold (títulos), Roboto Medium (tarjetas).");
        }

        private static TMP_FontAsset BuildOrUpdateDynamicSdf(string sourceTtfPath, string assetPath, string fontName)
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceTtfPath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"{LogPrefix} No se encontró fuente TTF en '{sourceTtfPath}'.");
                return null;
            }

            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                // Garantizar que mantiene fuente origen y modo Dinámico con caracteres en español precalentados
                PrewarmSpanishCharacters(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                64,
                6,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null)
            {
                Debug.LogWarning($"{LogPrefix} No se pudo generar TMP_FontAsset para '{sourceTtfPath}'.");
                return null;
            }

            fontAsset.name = fontName;
            PrewarmSpanishCharacters(fontAsset);

            AssetDatabase.CreateAsset(fontAsset, assetPath);

            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontName + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    Texture2D tex = fontAsset.atlasTextures[i];
                    if (tex != null)
                    {
                        tex.name = fontName + " Atlas " + i;
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
                }
            }

            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        private static void PrewarmSpanishCharacters(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            StringBuilder sb = new StringBuilder(256);
            for (int c = 32; c <= 126; c++)
            {
                sb.Append((char)c);
            }
            sb.Append("áéíóúüñÁÉÍÓÚÜÑ¡¿°·—–$€%()[]{}<>+=-*/:;,!?@#&_\"'`~^|\\…•►●◆■");

            fontAsset.TryAddCharacters(sb.ToString(), out _);
        }

        private static void UpdateTmpGlobalSettings(TMP_FontAsset primary, TMP_FontAsset boldDisplay, TMP_FontAsset mediumUi)
        {
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                Debug.LogWarning($"{LogPrefix} No se encontró '{TmpSettingsPath}'.");
                return;
            }

            SerializedObject so = new SerializedObject(settings);

            TMP_FontAsset defaultFont = primary != null ? primary : (mediumUi != null ? mediumUi : boldDisplay);
            if (defaultFont != null)
            {
                SerializedProperty defProp = so.FindProperty("m_defaultFontAsset");
                if (defProp != null) defProp.objectReferenceValue = defaultFont;
            }

            SerializedProperty fallbackProp = so.FindProperty("m_fallbackFontAssets");
            if (fallbackProp != null)
            {
                List<TMP_FontAsset> fallbacks = new List<TMP_FontAsset>();
                if (mediumUi != null && mediumUi != defaultFont) fallbacks.Add(mediumUi);
                if (boldDisplay != null && boldDisplay != defaultFont) fallbacks.Add(boldDisplay);
                TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationFallbackPath);
                if (liberation != null) fallbacks.Add(liberation);

                fallbackProp.arraySize = fallbacks.Count;
                for (int i = 0; i < fallbacks.Count; i++)
                {
                    fallbackProp.GetArrayElementAtIndex(i).objectReferenceValue = fallbacks[i];
                }
            }

            SerializedProperty kerningProp = so.FindProperty("m_enableKerning");
            if (kerningProp != null) kerningProp.boolValue = true;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }
    }
}
