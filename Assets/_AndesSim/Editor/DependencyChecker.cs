using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Ventana de verificación y diagnóstico técnico de dependencias para ANDES-SIM (BANCO ANDINO).
    /// Escenario 1: "Oficina de pagos" (Equipo 1).
    /// - Verifica TextMeshPro / uGUI en Unity 6 y sus recursos esenciales.
    /// - Detecta mediante reflexión la presencia del SDK de Google Cardboard XR (CardboardReticlePointer).
    /// - Configura automáticamente Scripting Define Symbols condicionales.
    /// </summary>
    public class DependencyChecker : EditorWindow
    {
        private const string MenuPath = "ANDES-SIM/03. Verificar Dependencias VR y TMP";
        private const string LogPrefix = "[ANDES-SIM - Dependencies]";

        // Símbolos de preprocesador
        public const string SymbolTmpPresent = "TMP_PRESENT";
        public const string SymbolCardboardPresent = "CARDBOARD_XR_PRESENT";

        // Rutas y nombres de tipos para validación por reflexión
        private const string CardboardReticleTypeName = "Google.XR.Cardboard.CardboardReticlePointer";
        private const string CardboardStartupTypeName = "Google.XR.Cardboard.CardboardStartup";
        private const string CardboardApiTypeName = "Google.XR.Cardboard.Api";

        // Estado interno del diagnóstico
        private bool _isTmpPackageAvailable;
        private bool _isTmpEssentialResourcesPresent;
        private bool _isCardboardReticleFound;
        private bool _isCardboardStartupFound;
        private bool _isXriPresent;
        private string _cardboardReticleLocation = string.Empty;
        private Vector2 _scrollPosition;

        [MenuItem(MenuPath, priority = 3)]
        public static void ShowWindow()
        {
            DependencyChecker window = GetWindow<DependencyChecker>("ANDES-SIM | Verificador VR");
            window.minSize = new Vector2(480, 520);
            window.Show();
            window.RunDiagnostic(logToConsole: true);
        }

        private void OnEnable()
        {
            RunDiagnostic(logToConsole: false);
        }

        /// <summary>
        /// Ejecuta el escaneo de paquetes, tipos por reflexión y recursos de proyecto.
        /// </summary>
        public void RunDiagnostic(bool logToConsole)
        {
            // 1. Detección de TextMeshPro (uGUI 2.0 / TextMeshPro en Unity 6)
            Type tmpType = FindTypeAcrossAssemblies("TMPro.TextMeshProUGUI") ??
                           FindTypeAcrossAssemblies("TMPro.TMP_Text");
            _isTmpPackageAvailable = tmpType != null;

            // Comprobar existencia de recursos esenciales en Assets
            _isTmpEssentialResourcesPresent = AssetDatabase.IsValidFolder("Assets/TextMesh Pro") &&
                                              (AssetDatabase.IsValidFolder("Assets/TextMesh Pro/Resources") ||
                                               AssetDatabase.FindAssets("t:TMP_FontAsset").Length > 0);

            // 2. Detección por Reflexión de Google Cardboard XR (sin referencias directas)
            Type reticleType = FindTypeAcrossAssemblies(CardboardReticleTypeName);
            Type startupType = FindTypeAcrossAssemblies(CardboardStartupTypeName) ??
                               FindTypeAcrossAssemblies(CardboardApiTypeName);

            _isCardboardReticleFound = reticleType != null;
            _isCardboardStartupFound = startupType != null;
            _cardboardReticleLocation = reticleType != null ? reticleType.Assembly.GetName().Name : "No detectado";

            // 3. Detección de XR Interaction Toolkit (para arquitecturas híbridas VR)
            Type xriType = FindTypeAcrossAssemblies("UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable") ??
                           FindTypeAcrossAssemblies("UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable");
            _isXriPresent = xriType != null;

            if (logToConsole)
            {
                Debug.Log($"{LogPrefix} <b>Diagnóstico de Dependencias Ejecutado:</b>\n" +
                          $"• TextMeshPro / uGUI: {(_isTmpPackageAvailable ? "<color=#4EC9B0>INSTALADO</color>" : "<color=#FF6B68>FALTANTE</color>")}\n" +
                          $"• Recursos Esenciales TMP: {(_isTmpEssentialResourcesPresent ? "<color=#4EC9B0>PRESENTES</color>" : "<color=#FFC66D>PENDIENTE IMPORTACIÓN</color>")}\n" +
                          $"• Google Cardboard Reticle ({CardboardReticleTypeName}): {(_isCardboardReticleFound ? $"<color=#4EC9B0>DETECTADO ({_cardboardReticleLocation})</color>" : "<color=#FFC66D>NO DETECTADO (Uso de retícula standalone/fallback)</color>")}\n" +
                          $"• XR Interaction Toolkit: {(_isXriPresent ? "<color=#4EC9B0>DISPONIBLE</color>" : "<color=#9CDCFE>NO ACTIVO</color>")}");
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(8);

            DrawTextMeshProSection();
            EditorGUILayout.Space(8);

            DrawCardboardSection();
            EditorGUILayout.Space(8);

            DrawPreprocessorSection();
            EditorGUILayout.Space(12);

            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("BANCO ANDINO - ANDES-SIM (VR)", EditorStyles.boldLabel);
            GUILayout.Label("Escenario 1: Oficina de pagos (Equipo 1) | Verificador Técnico", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawTextMeshProSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("1. TextMeshPro / UI System", EditorStyles.boldLabel);

            DrawStatusItem("Paquete TMP / uGUI disponible:", _isTmpPackageAvailable ? "OK" : "NO ENCONTRADO", _isTmpPackageAvailable);
            DrawStatusItem("Recursos Esenciales (Fonts/Shaders):", _isTmpEssentialResourcesPresent ? "PRESENTES" : "FALTANTES", _isTmpEssentialResourcesPresent);

            if (!_isTmpEssentialResourcesPresent)
            {
                EditorGUILayout.HelpBox("Los recursos esenciales de TMP (fuentes base y shaders) no están completos en 'Assets/TextMesh Pro'. Haz clic en el botón inferior para importarlos.", MessageType.Warning);
            }

            if (GUILayout.Button("Importar Recursos Esenciales de TMP", GUILayout.Height(28)))
            {
                ImportTMPEssentialResources();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCardboardSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("2. Google Cardboard XR (Detección por Reflexión)", EditorStyles.boldLabel);

            DrawStatusItem($"CardboardReticlePointer:", _isCardboardReticleFound ? $"DETECTADO ({_cardboardReticleLocation})" : "NO INSTALADO", _isCardboardReticleFound);
            DrawStatusItem($"Cardboard Core (Startup/API):", _isCardboardStartupFound ? "DETECTADO" : "NO INSTALADO", _isCardboardStartupFound);

            if (!_isCardboardReticleFound)
            {
                EditorGUILayout.HelpBox(
                    "El paquete de Google Cardboard XR no está en el proyecto o no ha compilado aún.\n" +
                    "La arquitectura de ANDES-SIM funcionará en modo emulación/fallback sin romper la compilación.",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox("Cardboard XR detectado. La retícula nativa y el tracking giroscópico están listos para vincularse al PlayerRig.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPreprocessorSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("3. Símbolos de Preprocesador Condicional", EditorStyles.boldLabel);

            bool isTmpSymbolActive = IsSymbolDefined(SymbolTmpPresent);
            bool isCardboardSymbolActive = IsSymbolDefined(SymbolCardboardPresent);

            DrawStatusItem($"Define '{SymbolTmpPresent}':", isTmpSymbolActive ? "ACTIVO" : "INACTIVO", isTmpSymbolActive);
            DrawStatusItem($"Define '{SymbolCardboardPresent}':", isCardboardSymbolActive ? "ACTIVO" : "INACTIVO", isCardboardSymbolActive);

            if (GUILayout.Button("Sincronizar Símbolos de Preprocesador", GUILayout.Height(28)))
            {
                SyncPreprocessorSymbols();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            if (GUILayout.Button("Re-escanear Dependencias", GUILayout.Height(30)))
            {
                RunDiagnostic(logToConsole: true);
            }
        }

        private void DrawStatusItem(string label, string statusText, bool isOk)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(240));
            Color prevColor = GUI.color;
            GUI.color = isOk ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.7f, 0.3f);
            EditorGUILayout.LabelField(statusText, EditorStyles.boldLabel);
            GUI.color = prevColor;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Importa los recursos esenciales de TextMeshPro mediante invocación interna de TMP o paquete de recursos.
        /// </summary>
        public static void ImportTMPEssentialResources()
        {
            Debug.Log($"{LogPrefix} Solicitando importación de recursos esenciales de TMP...");

            // Intento 1: Invocación por reflexión de la utilidad interna de TextMeshPro
            Type packageUtils = FindTypeAcrossAssemblies("TMPro.EditorUtilities.TMP_PackageUtilities");
            if (packageUtils != null)
            {
                MethodInfo importMethod = packageUtils.GetMethod("ImportProjectResourcesMenu", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (importMethod != null)
                {
                    importMethod.Invoke(null, null);
                    Debug.Log($"{LogPrefix} Invocado 'TMP_PackageUtilities.ImportProjectResourcesMenu()'.");
                    AssetDatabase.Refresh();
                    return;
                }
            }

            // Intento 2: Buscar el paquete TMP Essential Resources en Packages
            string[] candidatePackages = new string[]
            {
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage"
            };

            foreach (string packagePath in candidatePackages)
            {
                if (File.Exists(packagePath))
                {
                    Debug.Log($"{LogPrefix} Importando paquete TMP desde: {packagePath}");
                    AssetDatabase.ImportPackage(packagePath, false);
                    AssetDatabase.Refresh();
                    return;
                }
            }

            // Intento 3: Menú nativo del Editor
            if (EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources") ||
                EditorApplication.ExecuteMenuItem("Window/TextMesh Pro/Import TMP Essential Resources"))
            {
                Debug.Log($"{LogPrefix} Ejecutado comando de menú nativo de TextMesh Pro.");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "ANDES-SIM | TMP Resources",
                    "No se pudo localizar el paquete empaquetado de TMP automáticamente.\n" +
                    "Si ya existe la carpeta 'Assets/TextMesh Pro', los recursos ya se encuentran listos.",
                    "Aceptar"
                );
            }
        }

        /// <summary>
        /// Sincroniza los símbolos condicionales según el estado real de detección.
        /// </summary>
        public void SyncPreprocessorSymbols()
        {
            RunDiagnostic(logToConsole: false);

            SetSymbolState(SymbolTmpPresent, _isTmpPackageAvailable);
            SetSymbolState(SymbolCardboardPresent, _isCardboardReticleFound);

            AssetDatabase.SaveAssets();

            Debug.Log($"{LogPrefix} <color=#4EC9B0>Símbolos de preprocesador sincronizados con éxito.</color>\n" +
                      $"• {SymbolTmpPresent}: {_isTmpPackageAvailable}\n" +
                      $"• {SymbolCardboardPresent}: {_isCardboardReticleFound}");

            EditorUtility.DisplayDialog(
                "ANDES-SIM | Símbolos Sincronizados",
                $"Símbolos actualizados en Android y BuildTargetGroup activo:\n\n" +
                $"• {SymbolTmpPresent} = {_isTmpPackageAvailable}\n" +
                $"• {SymbolCardboardPresent} = {_isCardboardReticleFound}",
                "Aceptar"
            );
        }

        private static bool IsSymbolDefined(string symbol)
        {
            NamedBuildTarget target = NamedBuildTarget.Android;
            PlayerSettings.GetScriptingDefineSymbols(target, out string[] currentDefines);
            return currentDefines != null && currentDefines.Contains(symbol);
        }

        private static void SetSymbolState(string symbol, bool enable)
        {
            NamedBuildTarget[] targets = new NamedBuildTarget[]
            {
                NamedBuildTarget.Android,
                NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup)
            };

            foreach (NamedBuildTarget target in targets.Distinct())
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out string[] currentDefines);
                List<string> defineList = currentDefines != null ? new List<string>(currentDefines) : new List<string>();

                if (enable && !defineList.Contains(symbol))
                {
                    defineList.Add(symbol);
                    PlayerSettings.SetScriptingDefineSymbols(target, defineList.ToArray());
                }
                else if (!enable && defineList.Contains(symbol))
                {
                    defineList.Remove(symbol);
                    PlayerSettings.SetScriptingDefineSymbols(target, defineList.ToArray());
                }
            }
        }

        /// <summary>
        /// Busca de forma segura un tipo por nombre completo o corto entre todos los ensamblados cargados.
        /// </summary>
        public static Type FindTypeAcrossAssemblies(string fullOrShortTypeName)
        {
            if (string.IsNullOrEmpty(fullOrShortTypeName)) return null;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    Type t = assemblies[i].GetType(fullOrShortTypeName, false);
                    if (t != null) return t;

                    // Búsqueda alternativa por nombre simple
                    Type[] exportedTypes = assemblies[i].GetExportedTypes();
                    for (int j = 0; j < exportedTypes.Length; j++)
                    {
                        if (exportedTypes[j].FullName == fullOrShortTypeName || exportedTypes[j].Name == fullOrShortTypeName)
                        {
                            return exportedTypes[j];
                        }
                    }
                }
                catch
                {
                    // Ignorar ensamblados dinámicos o que arrojen ReflectionTypeLoadException
                }
            }

            return null;
        }
    }
}
