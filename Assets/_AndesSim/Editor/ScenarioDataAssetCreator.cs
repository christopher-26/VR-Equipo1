using System.IO;
using AndesSim.Data;
using UnityEditor;
using UnityEngine;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Utilidad del Editor para instanciar, poblar y persistir los ScriptableObjects de datos del simulador ANDES-SIM.
    /// Genera los assets en la carpeta Resources/Data para que puedan ser consumidos en tiempo de ejecución
    /// tanto por Resources.Load como por inyección en el Inspector.
    /// </summary>
    public static class ScenarioDataAssetCreator
    {
        private const string MenuPath = "ANDES-SIM/04. Generar Assets de Datos (ScenarioData y Sospechosos)";
        private const string LogPrefix = "[ANDES-SIM - AssetCreator]";

        private const string TargetDirectory = "Assets/_AndesSim/Resources/Data";
        private const string ScenarioDataFileName = "Scenario01_Data.asset";
        private const string SuspectsDataFileName = "Suspects_Data.asset";
        private const string WorkstationsDataFileName = "Workstations_Data.asset";

        [MenuItem(MenuPath, priority = 4)]
        public static void GenerateOrUpdateDataAssets()
        {
            ExecuteDataAssetGeneration(showDialog: false);
        }

        [InitializeOnLoadMethod]
        private static void AutoGenerateIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                string scenarioAssetPath = $"{TargetDirectory}/{ScenarioDataFileName}";
                if (!System.IO.File.Exists(scenarioAssetPath))
                {
                    ExecuteDataAssetGeneration(showDialog: false);
                }
            };
        }

        public static void ExecuteDataAssetGeneration(bool showDialog)
        {
            Debug.Log($"{LogPrefix} <b>Iniciando generación/actualización de assets de datos para el Escenario 1...</b>");

            EnsureDirectoryExists(TargetDirectory);

            string scenarioAssetPath = $"{TargetDirectory}/{ScenarioDataFileName}";
            string suspectsAssetPath = $"{TargetDirectory}/{SuspectsDataFileName}";
            string workstationsAssetPath = $"{TargetDirectory}/{WorkstationsDataFileName}";

            int createdCount = 0;
            int updatedCount = 0;

            // 1. Generar o actualizar Scenario01_Data.asset
            ScenarioData scenarioData = AssetDatabase.LoadAssetAtPath<ScenarioData>(scenarioAssetPath);
            if (scenarioData == null)
            {
                scenarioData = ScriptableObject.CreateInstance<ScenarioData>();
                AssetDatabase.CreateAsset(scenarioData, scenarioAssetPath);
                createdCount++;
                Debug.Log($"{LogPrefix} Creado nuevo asset: <color=#4EC9B0>{scenarioAssetPath}</color>");
            }
            else
            {
                updatedCount++;
                Debug.Log($"{LogPrefix} Sincronizando asset existente: <color=#9CDCFE>{scenarioAssetPath}</color>");
            }
            EditorUtility.SetDirty(scenarioData);

            // 2. Generar o actualizar Suspects_Data.asset
            SuspectDatabase suspectsDb = AssetDatabase.LoadAssetAtPath<SuspectDatabase>(suspectsAssetPath);
            if (suspectsDb == null)
            {
                suspectsDb = ScriptableObject.CreateInstance<SuspectDatabase>();
                suspectsDb.PopulateDefaultData();
                AssetDatabase.CreateAsset(suspectsDb, suspectsAssetPath);
                createdCount++;
                Debug.Log($"{LogPrefix} Creado nuevo asset: <color=#4EC9B0>{suspectsAssetPath}</color> (4 personajes)");
            }
            else
            {
                suspectsDb.PopulateDefaultData();
                updatedCount++;
                Debug.Log($"{LogPrefix} Actualizados 4 perfiles oficiales en: <color=#9CDCFE>{suspectsAssetPath}</color>");
            }
            EditorUtility.SetDirty(suspectsDb);

            // 3. Generar o actualizar Workstations_Data.asset (Integración de los 3 monitores RF-01)
            WorkstationData workstationData = AssetDatabase.LoadAssetAtPath<WorkstationData>(workstationsAssetPath);
            if (workstationData == null)
            {
                workstationData = ScriptableObject.CreateInstance<WorkstationData>();
                workstationData.PopulateDefaultData();
                AssetDatabase.CreateAsset(workstationData, workstationsAssetPath);
                createdCount++;
                Debug.Log($"{LogPrefix} Creado nuevo asset: <color=#4EC9B0>{workstationsAssetPath}</color> (3 puestos de trabajo)");
            }
            else
            {
                workstationData.PopulateDefaultData();
                updatedCount++;
                Debug.Log($"{LogPrefix} Actualizadas las 3 estaciones oficiales en: <color=#9CDCFE>{workstationsAssetPath}</color>");
            }
            EditorUtility.SetDirty(workstationData);

            // Persistir cambios en disco y refrescar AssetDatabase
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Ping al asset principal en el Project Browser para feedback visual al desarrollador
            Selection.activeObject = scenarioData;
            EditorGUIUtility.PingObject(scenarioData);

            string summary = $"Assets de datos generados y sincronizados correctamente en '{TargetDirectory}':\n" +
                             $"• {ScenarioDataFileName} (Escenario 1: Oficina de pagos - 16 destinatarios / 3 señales / 3 evidencias / 3 defensas)\n" +
                             $"• {SuspectsDataFileName} (4 sospechosos: Samuel Torres, Hugo Benavides, Carla Mendoza, Renzo Aliaga)\n" +
                             $"• {WorkstationsDataFileName} (3 puestos: Monitor_01 [PostIt], Monitor_02 [Descarga], Monitor_03 [Log SIEM])\n\n" +
                             $"Nuevos creados: {createdCount} | Existentes actualizados: {updatedCount}";

            Debug.Log($"{LogPrefix} <color=#4EC9B0>{summary.Replace("\n", " | ")}</color>");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "ANDES-SIM | Assets de Datos",
                    summary,
                    "Aceptar"
                );
            }
        }

        /// <summary>
        /// Asegura que la ruta de carpetas exista usando AssetDatabase.
        /// </summary>
        private static void EnsureDirectoryExists(string folderPath)
        {
            string normalized = folderPath.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(normalized)) return;

            string[] segments = normalized.Split('/');
            string current = segments[0];

            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }
                current = next;
            }
        }
    }
}
