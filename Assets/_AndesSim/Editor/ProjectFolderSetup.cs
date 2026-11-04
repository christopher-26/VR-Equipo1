using UnityEditor;
using UnityEngine;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Herramienta de automatización del Editor para el simulador VR "ANDES-SIM" (BANCO ANDINO).
    /// Escenario 1: "Oficina de pagos" (Equipo 1).
    /// Genera y valida la arquitectura de carpetas modular del proyecto.
    /// </summary>
    public static class ProjectFolderSetup
    {
        private const string MenuPath = "ANDES-SIM/00. Crear Estructura de Carpetas";
        private const string LogPrefix = "[ANDES-SIM]";

        /// <summary>
        /// Listado oficial de directorios requeridos para la arquitectura de la simulación.
        /// </summary>
        private static readonly string[] RequiredFolders = new string[]
        {
            "Assets/_AndesSim/Scenes",
            "Assets/_AndesSim/Scripts/Core",
            "Assets/_AndesSim/Scripts/Data",
            "Assets/_AndesSim/Scripts/VR",
            "Assets/_AndesSim/Scripts/Scenario01",
            "Assets/_AndesSim/Scripts/UI",
            "Assets/_AndesSim/Scripts/Audio",
            "Assets/_AndesSim/Scripts/Environment",
            "Assets/_AndesSim/Scripts/QA",
            "Assets/_AndesSim/Resources/Data",
            "Assets/_AndesSim/Materials",
            "Assets/_AndesSim/Prefabs"
        };

        [MenuItem(MenuPath, priority = 0)]
        public static void CreateProjectFolders()
        {
            int createdFoldersCount = 0;
            int existingFoldersCount = 0;

            Debug.Log($"{LogPrefix} <b>Iniciando creación/verificación de estructura de carpetas para 'Oficina de Pagos'...</b>");

            foreach (string folderPath in RequiredFolders)
            {
                if (EnsureFolderHierarchyExists(folderPath))
                {
                    createdFoldersCount++;
                }
                else
                {
                    existingFoldersCount++;
                }
            }

            // Sincronizar el AssetDatabase para reflejar inmediatamente los cambios en el Project Browser
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary = $"Configuración finalizada.\n- Carpetas nuevas creadas: {createdFoldersCount}\n- Carpetas ya existentes: {existingFoldersCount}";
            Debug.Log($"{LogPrefix} <color=#4EC9B0>{summary.Replace("\n", " | ")}</color>");

            EditorUtility.DisplayDialog(
                "ANDES-SIM | Estructura de Carpetas",
                $"BANCO ANDINO - Escenario 1: Oficina de pagos (Equipo 1)\n\n{summary}",
                "Aceptar"
            );
        }

        /// <summary>
        /// Valida y crea recursivamente cada nivel jerárquico de la ruta especificada mediante AssetDatabase.CreateFolder.
        /// </summary>
        /// <param name="fullPath">Ruta relativa al proyecto comenzando por 'Assets/...'</param>
        /// <returns>True si se creó alguna carpeta nueva en el camino; False si la ruta completa ya existía.</returns>
        private static bool EnsureFolderHierarchyExists(string fullPath)
        {
            // Normalizar separadores de ruta
            string normalizedPath = fullPath.Replace('\\', '/').TrimEnd('/');

            if (AssetDatabase.IsValidFolder(normalizedPath))
            {
                return false;
            }

            string[] segments = normalizedPath.Split('/');
            if (segments.Length == 0 || segments[0] != "Assets")
            {
                Debug.LogError($"{LogPrefix} Error: La ruta '{fullPath}' debe iniciar con 'Assets'.");
                return false;
            }

            string currentParent = segments[0];
            bool folderWasCreated = false;

            for (int i = 1; i < segments.Length; i++)
            {
                string targetFolder = $"{currentParent}/{segments[i]}";

                if (!AssetDatabase.IsValidFolder(targetFolder))
                {
                    string guid = AssetDatabase.CreateFolder(currentParent, segments[i]);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        Debug.Log($"{LogPrefix} Carpeta creada con éxito: <color=#9CDCFE>{targetFolder}</color>");
                        folderWasCreated = true;
                    }
                    else
                    {
                        Debug.LogError($"{LogPrefix} Fallo al intentar crear la carpeta '{segments[i]}' en '{currentParent}'.");
                    }
                }

                currentParent = targetFolder;
            }

            return folderWasCreated;
        }
    }
}
