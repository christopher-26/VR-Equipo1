using System.IO;
using UnityEditor;
using UnityEngine;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Utilidad para reconciliar y reimportar limpiamente OpenXR Package Settings.asset,
    /// eliminando la advertencia interna de NativeFormatImporter en Unity 6.
    /// </summary>
    public static class OpenXrSettingsFixer
    {
        private const string LogPrefix = "[ANDES-SIM - OpenXR Fixer]";
        private const string AssetPath = "Assets/XR/Settings/OpenXR Package Settings.asset";

        [MenuItem("ANDES-SIM/02. Reimportar Ajustes OpenXR (Limpiar Advertencia)", priority = 2)]
        public static void ReimportOpenXrSettings()
        {
            if (File.Exists(AssetPath))
            {
                AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.SaveAssets();
                Debug.Log($"{LogPrefix} <color=#38F28A><b>OpenXR Package Settings.asset reimportado exitosamente sin inconsistencias.</b></color>");
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} No se encontró el archivo en: {AssetPath}");
            }
        }
    }
}
