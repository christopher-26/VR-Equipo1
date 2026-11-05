using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Configuración técnica de Build y PlayerSettings para ANDES-SIM (BANCO ANDINO).
    /// Escenario 1: "Oficina de pagos" (Equipo 1).
    /// Optimizado específicamente para despliegue en Google Cardboard XR sobre Android (IL2CPP / ARM64 / OpenGLES3).
    /// </summary>
    public static class AndroidCardboardBuildSettings
    {
        private const string MenuPath = "ANDES-SIM/02. Aplicar Configuración Técnica VR (Android)";
        private const string LogPrefix = "[ANDES-SIM - BuildSettings]";

        private const string ExpectedCompanyName = "BancoAndino_AndesSim";
        private const string ExpectedProductName = "ANDES-SIM_E1_OficinaPagos";

        [MenuItem(MenuPath, priority = 2)]
        public static void ApplyAndroidCardboardConfigurationMenuItem()
        {
            ApplyAndroidCardboardConfiguration(showDialog: true);
        }

        public static void ApplyAndroidCardboardConfiguration(bool showDialog = false)
        {
            Debug.Log($"{LogPrefix} <b>Iniciando aplicación de configuración técnica para Android / Cardboard XR...</b>");

            // 1. Identificación del Producto y Compañía
            PlayerSettings.companyName = ExpectedCompanyName;
            PlayerSettings.productName = ExpectedProductName;
            Debug.Log($"{LogPrefix} Identidad configurada: Company = <color=#9CDCFE>'{ExpectedCompanyName}'</color>, Product = <color=#9CDCFE>'{ExpectedProductName}'</color>");

            // 2. Orientación de Pantalla (Bloquear Portrait y LandscapeRight, forzar LandscapeLeft)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            Debug.Log($"{LogPrefix} Orientación bloqueada a <b>LandscapeLeft</b> (Portrait y LandscapeRight desactivados).");

            // 3. Scripting Backend: IL2CPP para Android
            NamedBuildTarget androidBuildTarget = NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(androidBuildTarget, ScriptingImplementation.IL2CPP);
            Debug.Log($"{LogPrefix} Scripting Backend configurado a: <color=#4EC9B0>IL2CPP</color> (Target: Android)");

            // 4. Arquitectura de CPU: ARM64 exclusivamente (64-bit compliance)
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            Debug.Log($"{LogPrefix} Arquitectura de destino configurada a: <color=#4EC9B0>ARM64</color>");

            // 5. Android Minimum SDK Version: API Level 26 (Android 8.0 Oreo), Target SDK: API Level 34 (Android 14.0)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
            Debug.Log($"{LogPrefix} SDK Mínimo configurado a: <color=#4EC9B0>Android 8.0 (API Level 26)</color>, Target SDK: <color=#4EC9B0>Android 14.0 (API Level 34)</color>");

            // 6. Graphics APIs: Configurar exclusivamente OpenGLES3 y descartar Vulkan (Requisito Cardboard XR)
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            GraphicsDeviceType[] androidGpuApis = new GraphicsDeviceType[]
            {
                GraphicsDeviceType.OpenGLES3
            };
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, androidGpuApis);
            Debug.Log($"{LogPrefix} APIs Gráficas para Android fijadas a: <color=#4EC9B0>[OpenGLES3]</color> (Vulkan excluido)");

            // 7. Multithreaded Rendering (MTRendering)
            // En Google Cardboard XR sobre OpenGLES3, desactivar MTRendering previene fallos de sincronización y pantallas negras
            PlayerSettings.SetMobileMTRendering(androidBuildTarget, false);
            PlayerSettings.MTRendering = false;
            Debug.Log($"{LogPrefix} Multithreaded Rendering (MTRendering) desactivado para estabilidad de Cardboard XR con GLES3.");

            // 8. Desactivar OpenXR / MetaQuest en Android para garantizar compatibilidad con smartphones y visores Cardboard
            var xrSettings = UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (xrSettings != null && xrSettings.Manager != null)
            {
                var manager = xrSettings.Manager;
                var loaders = new System.Collections.Generic.List<UnityEngine.XR.Management.XRLoader>(manager.activeLoaders);
                foreach (var loader in loaders)
                {
                    if (loader != null && (loader.name.Contains("OpenXR") || loader.GetType().FullName.Contains("OpenXR")))
                    {
                        manager.TryRemoveLoader(loader);
                        Debug.Log($"{LogPrefix} Desactivado '{loader.name}' para Android: evita inyección de paquetes Oculus/MetaQuest incompatibles con smartphones estándar.");
                    }
                }
                EditorUtility.SetDirty(xrSettings);
                EditorUtility.SetDirty(manager);
            }

            // 9. EditorUserBuildSettings: Asegurar grupo seleccionado en Android
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;

            // Guardar configuración en disco
            AssetDatabase.SaveAssets();

            // Resumen de verificación
            string summary = "Configuración Técnica VR (Android/Cardboard) aplicada con éxito:\n" +
                             $"• Company: {PlayerSettings.companyName}\n" +
                             $"• Product: {PlayerSettings.productName}\n" +
                             $"• Orientación: {PlayerSettings.defaultInterfaceOrientation} (Locks aplicados)\n" +
                             $"• Scripting Backend: {PlayerSettings.GetScriptingBackend(androidBuildTarget)}\n" +
                             $"• Target Architecture: {PlayerSettings.Android.targetArchitectures}\n" +
                             $"• Min SDK: {PlayerSettings.Android.minSdkVersion}\n" +
                             $"• Graphics API: OpenGLES3 (Exclusivo)\n" +
                             $"• Mobile MTRendering: Desactivado (Cardboard Safe)";

            Debug.Log($"{LogPrefix} <color=#4EC9B0>{summary}</color>");

            // Verificar si la plataforma activa actual en el Editor es Android
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (showDialog)
                {
                    bool switchNow = EditorUtility.DisplayDialog(
                        "ANDES-SIM | Plataforma Activa",
                        $"{summary}\n\nATENCIÓN: La plataforma activa actual del editor es '{EditorUserBuildSettings.activeBuildTarget}'.\n¿Deseas cambiar la plataforma activa a Android ahora?",
                        "Cambiar a Android",
                        "Mantener plataforma actual"
                    );

                    if (switchNow)
                    {
                        Debug.Log($"{LogPrefix} Iniciando cambio de plataforma activa hacia Android...");
                        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                    }
                }
            }
            else
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "ANDES-SIM | Configuración Técnica VR",
                        $"{summary}\n\nLa plataforma activa ya es Android. El proyecto está listo para compilar.",
                        "Aceptar"
                    );
                }
            }
        }
    }
}
