using UnityEditor;
using UnityEngine;

namespace AndesSim.EditorTools
{
    /// <summary>
    /// Automatización de configuración de Tags y Layers en el TagManager del proyecto.
    /// Requisito técnico para el Escenario 1: "Oficina de pagos" (Equipo 1) - BANCO ANDINO.
    /// Configura Layer 8 (Interactive) para la retícula VR/Cardboard, Layer 9 (Teleport) y los tags del escenario.
    /// </summary>
    public static class LayerAndTagConfigurator
    {
        private const string MenuPath = "ANDES-SIM/01. Configurar Capa Interactive y Tags";
        private const string LogPrefix = "[ANDES-SIM]";
        private const string TagManagerAssetPath = "ProjectSettings/TagManager.asset";

        private const int InteractiveLayerIndex = 8;
        private const string InteractiveLayerName = "Interactive";

        private const int TeleportLayerIndex = 9;
        private const string TeleportLayerName = "Teleport";

        private static readonly string[] RequiredTags = new string[]
        {
            "Evidence",
            "DefenseControl",
            "Monitor",
            "PlayerRig",
            "Interactive"
        };

        [MenuItem(MenuPath, priority = 1)]
        public static void ConfigureLayersAndTagsMenu()
        {
            ExecuteConfigureLayersAndTags(showDialog: true);
        }

        /// <summary>
        /// Método programático de configuración accesible por otros scripts y scene builders (silencioso por defecto).
        /// </summary>
        public static void ConfigureLayersAndTags(bool showDialog = false)
        {
            ExecuteConfigureLayersAndTags(showDialog);
        }

        [InitializeOnLoadMethod]
        private static void AutoConfigureOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                ExecuteConfigureLayersAndTags(showDialog: false);
            };
        }

        private static void ExecuteConfigureLayersAndTags(bool showDialog = false)
        {
            Debug.Log($"{LogPrefix} <b>Iniciando verificación y configuración de Capas y Tags...</b>");

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerAssetPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"{LogPrefix} No se pudo acceder a '{TagManagerAssetPath}'. Asegúrate de que el archivo exista.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            tagManager.Update();

            SerializedProperty layersProp = tagManager.FindProperty("layers");
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            if (layersProp == null || tagsProp == null)
            {
                Debug.LogError($"{LogPrefix} No se pudieron localizar las propiedades 'layers' o 'tags' en TagManager.");
                return;
            }

            int layerChanges = 0;
            int tagChanges = 0;

            // 1. Configurar Capas requeridas
            layerChanges += ConfigureLayer(layersProp, InteractiveLayerIndex, InteractiveLayerName);
            layerChanges += ConfigureLayer(layersProp, TeleportLayerIndex, TeleportLayerName);

            // 2. Configurar Tags requeridos
            foreach (string tag in RequiredTags)
            {
                tagChanges += ConfigureTag(tagsProp, tag);
            }

            // 3. Aplicar y persistir cambios
            if (tagManager.ApplyModifiedProperties())
            {
                AssetDatabase.SaveAssets();
                string summary = $"Configuración aplicada con éxito:\n- Modificaciones en Capas: {layerChanges}\n- Tags nuevos añadidos: {tagChanges}";
                Debug.Log($"{LogPrefix} <color=#4EC9B0>{summary.Replace("\n", " | ")}</color>");

                // Notificación silenciosa en consola para evitar bloquear el hilo del Editor
                Debug.Log($"{LogPrefix} [Capas y Tags] {summary.Replace("\n", " | ")}");
            }
            else
            {
                string info = "Todas las capas (8: Interactive, 9: Teleport) y tags ya estaban configurados de forma idéntica.";
                Debug.Log($"{LogPrefix} <color=#9CDCFE>{info}</color>");
            }
        }

        private static int ConfigureLayer(SerializedProperty layersProp, int index, string layerName)
        {
            if (index < 0 || index >= layersProp.arraySize)
            {
                Debug.LogError($"{LogPrefix} Índice de capa fuera de rango: {index} (Rango válido: 0-{layersProp.arraySize - 1}).");
                return 0;
            }

            SerializedProperty layerElem = layersProp.GetArrayElementAtIndex(index);
            string currentName = layerElem.stringValue;

            if (currentName == layerName)
            {
                Debug.Log($"{LogPrefix} Capa {index} ya configurada correctamente como: <b>{layerName}</b>");
                return 0;
            }

            if (!string.IsNullOrEmpty(currentName))
            {
                Debug.LogWarning($"{LogPrefix} Sobrescribiendo Capa {index}: '{currentName}' -> '<b>{layerName}</b>'.");
            }
            else
            {
                Debug.Log($"{LogPrefix} Asignando Capa {index}: '<b>{layerName}</b>'.");
            }

            layerElem.stringValue = layerName;
            return 1;
        }

        private static int ConfigureTag(SerializedProperty tagsProp, string tagName)
        {
            // Verificar si el tag ya existe
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty tagElem = tagsProp.GetArrayElementAtIndex(i);
                if (tagElem.stringValue == tagName)
                {
                    Debug.Log($"{LogPrefix} Tag ya existente: <b>{tagName}</b>");
                    return 0;
                }
            }

            // Si no existe, agregar un nuevo elemento al array
            int newIndex = tagsProp.arraySize;
            tagsProp.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newTagElem = tagsProp.GetArrayElementAtIndex(newIndex);
            newTagElem.stringValue = tagName;

            Debug.Log($"{LogPrefix} Tag agregado: <color=#9CDCFE>'{tagName}'</color> en índice {newIndex}");
            return 1;
        }
    }
}
