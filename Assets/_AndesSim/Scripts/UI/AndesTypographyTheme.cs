using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AndesSim.UI
{
    /// <summary>
    /// Sistema centralizado de tipografía moderna y paleta cyber-corporativa para ANDES-SIM.
    /// Garantiza fuentes limpias (Inter para lectura / Roboto Bold para encabezados y métricas),
    /// alineación elegante centrada y alto contraste legible en Google Cardboard VR.
    /// </summary>
    public static class AndesTypographyTheme
    {
        public static readonly Color BgVoidNavy     = new Color(0.027f, 0.063f, 0.114f, 0.96f); // #07101D
        public static readonly Color BgCardGlass    = new Color(0.055f, 0.118f, 0.196f, 0.95f); // #0E1E32
        public static readonly Color BgCardElevated = new Color(0.082f, 0.169f, 0.278f, 0.98f); // #152B47
        public static readonly Color AccentCyan     = new Color(0.000f, 0.898f, 1.000f, 1.00f); // #00E5FF
        public static readonly Color AccentSky      = new Color(0.220f, 0.741f, 0.973f, 1.00f); // #38BDF8
        public static readonly Color AccentEmerald  = new Color(0.220f, 0.949f, 0.541f, 1.00f); // #38F28A
        public static readonly Color AccentAmber    = new Color(1.000f, 0.663f, 0.251f, 1.00f); // #FFA940
        public static readonly Color AccentRed      = new Color(1.000f, 0.353f, 0.306f, 1.00f); // #FF5A4E
        public static readonly Color AccentViolet   = new Color(0.655f, 0.545f, 0.980f, 1.00f); // #A78BFA
        public static readonly Color TextPrimary    = new Color(0.957f, 0.976f, 1.000f, 1.00f); // #F4F9FF
        public static readonly Color TextMuted      = new Color(0.678f, 0.769f, 0.867f, 1.00f); // #ADC4DD

        private static TMP_FontAsset _cachedPrimaryFont;
        private static TMP_FontAsset _cachedDisplayFont;
        private static TMP_FontAsset _cachedMediumFont;
        private static bool _fontsLookedUp;

        /// <summary>
        /// Fuente principal limpia para lectura cómoda en VR (Inter Dynamic SDF con soporte completo de tildes y eñes).
        /// </summary>
        public static TMP_FontAsset PrimaryBodyFont
        {
            get
            {
                EnsureFontsLoaded();
                return _cachedPrimaryFont != null ? _cachedPrimaryFont : TMP_Settings.defaultFontAsset;
            }
        }

        /// <summary>
        /// Fuente de alto impacto para títulos, botones CTA, insignias y métricas (Roboto Bold SDF).
        /// </summary>
        public static TMP_FontAsset DisplayHeaderFont
        {
            get
            {
                EnsureFontsLoaded();
                if (_cachedDisplayFont != null) return _cachedDisplayFont;
                if (_cachedMediumFont != null) return _cachedMediumFont;
                return PrimaryBodyFont;
            }
        }

        /// <summary>
        /// Fuente equilibrada para subtítulos, fichas y etiquetas interactivas (Roboto Medium SDF).
        /// </summary>
        public static TMP_FontAsset MediumUiFont
        {
            get
            {
                EnsureFontsLoaded();
                if (_cachedMediumFont != null) return _cachedMediumFont;
                return PrimaryBodyFont;
            }
        }

        /// <summary>
        /// Limpia la caché estática tras regenerar los SDF en el Editor.
        /// </summary>
        public static void ResetCache()
        {
            _cachedPrimaryFont = null;
            _cachedDisplayFont = null;
            _cachedMediumFont = null;
            _fontsLookedUp = false;
        }

        private static void EnsureFontsLoaded()
        {
            if (_fontsLookedUp) return;
            _fontsLookedUp = true;

            _cachedPrimaryFont = Resources.Load<TMP_FontAsset>("Fonts/Inter-AndesSim SDF");
            _cachedDisplayFont = Resources.Load<TMP_FontAsset>("Fonts/Roboto-Bold-AndesSim SDF");
            _cachedMediumFont  = Resources.Load<TMP_FontAsset>("Fonts/Roboto-Medium-AndesSim SDF");

#if UNITY_EDITOR
            if (_cachedPrimaryFont == null)
            {
                _cachedPrimaryFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/_AndesSim/Resources/Fonts/Inter-AndesSim SDF.asset");
            }
            if (_cachedDisplayFont == null)
            {
                _cachedDisplayFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/_AndesSim/Resources/Fonts/Roboto-Bold-AndesSim SDF.asset");
            }
            if (_cachedMediumFont == null)
            {
                _cachedMediumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/_AndesSim/Resources/Fonts/Roboto-Medium-AndesSim SDF.asset");
            }
#endif

            if (_cachedPrimaryFont == null)
            {
                _cachedPrimaryFont = TMP_Settings.defaultFontAsset;
            }
            if (_cachedMediumFont == null)
            {
                _cachedMediumFont = _cachedPrimaryFont;
            }
            if (_cachedDisplayFont == null)
            {
                _cachedDisplayFont = _cachedMediumFont;
            }
        }

        public static void StyleDisplayTitle(TMP_Text tmp, Color? color = null, float fontSize = 0f)
        {
            if (tmp == null) return;
            if (DisplayHeaderFont != null) tmp.font = DisplayHeaderFont;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.characterSpacing = 3.2f;
            tmp.wordSpacing = 1.5f;
            tmp.lineSpacing = 6f;
            if (fontSize > 0f) tmp.fontSize = fontSize;
            if (color.HasValue) tmp.color = color.Value;
        }

        public static void StyleSectionHeader(TMP_Text tmp, Color? color = null, float fontSize = 0f)
        {
            if (tmp == null) return;
            if (DisplayHeaderFont != null) tmp.font = DisplayHeaderFont;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.characterSpacing = 2.2f;
            tmp.lineSpacing = 6f;
            if (fontSize > 0f) tmp.fontSize = fontSize;
            if (color.HasValue) tmp.color = color.Value;
        }

        public static void StyleBodyCentered(TMP_Text tmp, Color? color = null, float fontSize = 0f, float lineSpacing = 12f)
        {
            if (tmp == null) return;
            if (PrimaryBodyFont != null) tmp.font = PrimaryBodyFont;
            tmp.fontStyle = FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.characterSpacing = 0.6f;
            tmp.wordSpacing = 1.2f;
            tmp.lineSpacing = lineSpacing;
            if (fontSize > 0f) tmp.fontSize = fontSize;
            if (color.HasValue) tmp.color = color.Value;
        }

        public static void StyleBodyReadable(TMP_Text tmp, TextAlignmentOptions alignment, Color? color = null, float fontSize = 0f, float lineSpacing = 12f)
        {
            if (tmp == null) return;
            if (PrimaryBodyFont != null) tmp.font = PrimaryBodyFont;
            tmp.fontStyle = FontStyles.Normal;
            tmp.alignment = alignment;
            tmp.characterSpacing = 0.5f;
            tmp.wordSpacing = 1.0f;
            tmp.lineSpacing = lineSpacing;
            if (fontSize > 0f) tmp.fontSize = fontSize;
            if (color.HasValue) tmp.color = color.Value;
        }

        public static void StyleButtonLabel(TMP_Text tmp, Color? color = null, float fontSize = 0f)
        {
            if (tmp == null) return;
            if (DisplayHeaderFont != null) tmp.font = DisplayHeaderFont;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.characterSpacing = 1.8f;
            tmp.lineSpacing = 4f;
            if (fontSize > 0f) tmp.fontSize = fontSize;
            if (color.HasValue) tmp.color = color.Value;
        }

        public static void StyleBadge(TMP_Text tmp, Color? color = null, float fontSize = 0f)
        {
            if (tmp == null) return;
            if (DisplayHeaderFont != null) tmp.font = DisplayHeaderFont;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.characterSpacing = 3.5f;
            tmp.lineSpacing = 2f;
            if (fontSize > 0f) tmp.fontSize = fontSize;
            if (color.HasValue) tmp.color = color.Value;
        }

        public static void StyleMetric(TMP_Text tmp, Color? color = null, float fontSize = 0f)
        {
            if (tmp == null) return;
            if (DisplayHeaderFont != null) tmp.font = DisplayHeaderFont;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.characterSpacing = 4.0f;
            if (fontSize > 0f) tmp.fontSize = fontSize;
            if (color.HasValue) tmp.color = color.Value;
        }

        /// <summary>
        /// Aplica de forma automática fuentes modernas y alineación centrada limpia a toda una jerarquía UI.
        /// Respeta áreas técnicas de logs cuando centerBodyText es false.
        /// </summary>
        public static void ApplyTypographyToTree(Transform root, bool centerHeadersAndButtons = true, bool centerBodyText = true)
        {
            if (root == null) return;
            EnsureFontsLoaded();

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text t = texts[i];
                if (t == null) continue;

                string n = t.gameObject.name;
                bool isButtonChild = t.GetComponentInParent<Button>(true) != null;
                bool isTitle = n.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               n.IndexOf("Titulo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               n.IndexOf("Header", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               n.IndexOf("Nombre", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isBadgeOrMetric = n.IndexOf("Badge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       n.IndexOf("Tag", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       n.IndexOf("KPI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       n.IndexOf("Count", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       n.IndexOf("Contador", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       n.IndexOf("Timer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       n.IndexOf("Metric", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       n.IndexOf("Score", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       n.IndexOf("Stat", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isLogConsole = n.IndexOf("Log", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    n.IndexOf("Console", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    n.IndexOf("TerminalBody", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isTitle)
                {
                    if (DisplayHeaderFont != null) t.font = DisplayHeaderFont;
                    t.fontStyle = FontStyles.Bold;
                    t.characterSpacing = Mathf.Max(t.characterSpacing, 2.2f);
                    if (centerHeadersAndButtons) t.alignment = TextAlignmentOptions.Center;
                }
                else if (isButtonChild || isBadgeOrMetric)
                {
                    if (DisplayHeaderFont != null) t.font = DisplayHeaderFont;
                    if ((t.fontStyle & FontStyles.Bold) == 0) t.fontStyle |= FontStyles.Bold;
                    t.characterSpacing = Mathf.Max(t.characterSpacing, 1.5f);
                    if (centerHeadersAndButtons) t.alignment = TextAlignmentOptions.Center;
                }
                else
                {
                    if (PrimaryBodyFont != null) t.font = PrimaryBodyFont;
                    t.lineSpacing = Mathf.Max(t.lineSpacing, 8f);
                    if (centerBodyText && !isLogConsole)
                    {
                        t.alignment = TextAlignmentOptions.Center;
                    }
                }
            }
        }
    }
}
