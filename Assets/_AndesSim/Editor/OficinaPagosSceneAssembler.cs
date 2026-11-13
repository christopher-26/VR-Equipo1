using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AndesSim.Audio;
using AndesSim.Core;
using AndesSim.Data;
using AndesSim.EditorTools;
using AndesSim.Environment;
using AndesSim.Scenario01;
using AndesSim.UI;
using AndesSim.VR;

namespace AndesSim.Editor
{
    /// <summary>
    /// Ensamblador oficial y automatizado del 100% de la escena de Realidad Virtual:
    /// 'Assets/_AndesSim/Scenes/Oficina_Pagos.unity'.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// 
    /// Al hacer clic en el menú 'ANDES-SIM/05. GENERAR ESCENA COMPLETA: Oficina_Pagos (VR)':
    /// 1. Configura la capa "Interactive" (Layer 8) y genera los ScriptableObjects de datos oficiales.
    /// 2. Instancia el objeto '_SYSTEMS' con todos los managers centrales del juego.
    /// 3. Instancia la geometría 3D completa de la oficina bancaria nocturna.
    /// 4. Instancia el rig 'Player' en el vestíbulo frente al Portal_Entrada con retícula y controles VR.
    /// 5. Instancia el Portal_Entrada y los 3 TeleportPoints en la capa "Interactive".
    /// 6. Instancia los 3 monitores interactivos con Canvases World Space, correo de Rasel Ahlam,
    ///    las 3 zonas de phishing, Post-It de Carla Mendoza, visualizador de malware y terminal SIEM.
    /// 7. Instancia el panel de defensas (3 switches), tablero de 4 sospechosos, pantalla final y modal de créditos.
    /// 8. Guarda la escena en 'Oficina_Pagos.unity' y la establece como escena principal (índice 0).
    /// </summary>
    public static class OficinaPagosSceneAssembler
    {
        public const string ScenePath = "Assets/_AndesSim/Scenes/Oficina_Pagos.unity";
        private const string MenuPath = "ANDES-SIM/05. GENERAR ESCENA COMPLETA: Oficina_Pagos (VR)";
        private const string LogPrefix = "[ANDES-SIM - SceneAssembler]";

        [MenuItem(MenuPath, priority = 5)]
        public static void GenerateCompleteOficinaPagosScene()
        {
            AssembleCompleteSceneInternal(showDialog: false);
        }

        [MenuItem("ANDES-SIM/06. Refrescar Iluminacion y Atmosfera (Luz Clara y Vestibulo)", priority = 6)]
        public static void RefreshLightingInCurrentScene()
        {
            SocLightingController.ApplyNocturnalAmbientSettings();

            GameObject lightingRoot = GameObject.Find("--- ILUMINACION SOC & ATMOSFERA ---");
            if (lightingRoot != null)
            {
                SocLightingController ctrl = lightingRoot.GetComponent<SocLightingController>();
                if (ctrl != null)
                {
                    ctrl.EnsureCompleteLightingIntegrity();
                }
            }
            else
            {
                SocLightingController.SetupLightingAtmosphere();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"{LogPrefix} <color=#38F28A><b>[ILUMINACIÓN ACTUALIZADA] Atmósfera clara, luz cenital en vestíbulo y centro aplicadas exitosamente.</b></color>");
        }

        /// <summary>
        /// Método de ensamblado completo ejecutable sin argumentos desde Unity Batchmode (-executeMethod).
        /// </summary>
        public static void AssembleCompleteScene()
        {
            AssembleCompleteSceneInternal(showDialog: false);
        }

        /// <summary>
        /// Método de ensamblado completo interno con opción de diálogo.
        /// </summary>
        public static void AssembleCompleteSceneInternal(bool showDialog)
        {
            Debug.Log($"{LogPrefix} <b>==================================================================</b>");
            Debug.Log($"{LogPrefix} <b>INICIANDO ENSAMBLADO AUTOMATIZADO 100%: Oficina_Pagos.unity (VR)</b>");
            Debug.Log($"{LogPrefix} <b>==================================================================</b>");

            // -------------------------------------------------------------------------
            // PASO 1: Configurar Capa Interactive (Layer 8), Tags y Assets de Datos
            // -------------------------------------------------------------------------
            Debug.Log($"{LogPrefix} [1/8] Verificando capas, tags y ScriptableObjects de datos...");
            LayerAndTagConfigurator.ConfigureLayersAndTags();
            ScenarioDataAssetCreator.ExecuteDataAssetGeneration(showDialog: false);

            ScenarioData scenarioData = Resources.Load<ScenarioData>("Data/Scenario01_Data");
            if (scenarioData == null)
            {
                scenarioData = AssetDatabase.LoadAssetAtPath<ScenarioData>("Assets/_AndesSim/Resources/Data/Scenario01_Data.asset");
            }

            // -------------------------------------------------------------------------
            // PASO 2: Cargar Escenario Base de Nuevas Oficinas (VNB - Office Set)
            // -------------------------------------------------------------------------
            string vnbScenePath = "Assets/VNB - Office Set/Scenes/Demo 1 - Office Set 1.unity";
            Scene scene;
            GameObject envRoot;
            if (File.Exists(vnbScenePath))
            {
                Debug.Log($"{LogPrefix} [2/8] Cargando escenario de oficina moderna (VNB - Office Set: Demo 1)...");
                scene = EditorSceneManager.OpenScene(vnbScenePath, OpenSceneMode.Single);
                GameObject oldCam = GameObject.Find("Main Camera");
                if (oldCam != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldCam);
                }
                envRoot = GameObject.Find("Floor 0");
                if (envRoot == null) envRoot = new GameObject("--- ENTORNO 3D: BANCO ANDINO ---");
                CompleteVnbOfficeEnclosureAndCeiling(envRoot);
            }
            else
            {
                Debug.Log($"{LogPrefix} [2/8] Creando nueva escena base...");
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                envRoot = new GameObject("--- ENTORNO 3D: BANCO ANDINO ---");
                ProceduralOfficeEnvironmentBuilder.BuildOfficeGeometry(envRoot.transform, false, false);
            }

            // Asegurar EventSystem con soporte para touch y mouse
            if (UnityEngine.EventSystems.EventSystem.current == null && UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            // -------------------------------------------------------------------------
            // PASO 3: Instanciar el Objeto _SYSTEMS con todos los Core Managers
            // -------------------------------------------------------------------------
            Debug.Log($"{LogPrefix} [3/8] Instanciando el objeto '_SYSTEMS' con GameManager, Clock, Flow, UIManager, AudioManager y Cardboard Lifecycle...");
            GameObject systemsObj = BuildSystemsObject(scenarioData);

            // -------------------------------------------------------------------------
            // PASO 4: Atmosfera Cibernetica y Efectos
            // -------------------------------------------------------------------------

            // Efecto de Rejilla e Iluminación Cibernética Ambiental (Ventanal y Portal - Optimizado Mobile)
            GameObject cyberGridObj = new GameObject("AmbientCyberGridEffect");
            cyberGridObj.transform.SetParent(envRoot.transform, false);
            cyberGridObj.AddComponent<AmbientCyberGridEffect>();

            // Partículas Atmosféricas Cibernéticas (Dust Motes flotantes para inmersión VR móvil)
            GameObject motesObj = new GameObject("Atmospheric_CyberMotes");
            motesObj.transform.SetParent(envRoot.transform, false);
            motesObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            BuildAtmosphericParticles(motesObj);

            // -------------------------------------------------------------------------
            // PASO 5: Instanciar el Player VR en el Vestíbulo frente al Portal
            // -------------------------------------------------------------------------
            Debug.Log($"{LogPrefix} [5/8] Instanciando rig del Player VR en la puerta de entrada de la oficina...");
            GameObject playerRig = BuildVrPlayerRig();

            // Enlazar referencias de cámara y PlayerRig en CardboardXrLifecycleManager y UIManager
            CardboardXrLifecycleManager xrLife = systemsObj.GetComponent<CardboardXrLifecycleManager>();
            Camera playerCam = playerRig.GetComponentInChildren<Camera>();
            if (xrLife != null && playerCam != null)
            {
                SerializedObject sXr = new SerializedObject(xrLife);
                sXr.FindProperty("playerRigRoot").objectReferenceValue = playerRig.transform;
                sXr.FindProperty("playerCamera").objectReferenceValue = playerCam;
                sXr.ApplyModifiedProperties();
            }

            UIManager uiManager = systemsObj.GetComponent<UIManager>();
            if (uiManager != null && playerCam != null)
            {
                SerializedObject sUi = new SerializedObject(uiManager);
                sUi.FindProperty("targetCamera").objectReferenceValue = playerCam.transform;
                sUi.ApplyModifiedProperties();
            }

            // -------------------------------------------------------------------------
            // PASO 6: Instanciar los 3 Puestos de Trabajo, Monitores, Correo, Post-It y Terminal SIEM
            // -------------------------------------------------------------------------
            Debug.Log($"{LogPrefix} [6/8] Construyendo los 3 puestos de trabajo, monitores, correo phishing, Post-It y terminal SIEM...");
            GameObject stationsRoot = new GameObject("--- PUESTOS DE TRABAJO (RF-01) ---");
            BuildWorkstationsAndMonitors(stationsRoot.transform, null);

            // -------------------------------------------------------------------------
            // PASO 7: Menú de Inicio VR Simple y Flotante (Sin muro ni arco intrusivo)
            // -------------------------------------------------------------------------
            Debug.Log($"{LogPrefix} [7/8] Instanciando Menú de Inicio VR Simple y Flotante...");
            GameObject startMenuObj = BuildSimpleVrStartMenu(envRoot.transform);

            // -------------------------------------------------------------------------
            // PASO 8: Instanciar Tablero de Sospechosos, Defensas, Pantalla Final, HUD y Créditos
            // -------------------------------------------------------------------------
            Debug.Log($"{LogPrefix} [8/8] Construyendo tablero de sospechosos, panel de defensas, HUD, pantalla final y créditos...");
            GameObject uiRoot = new GameObject("--- INTERFACES DE USUARIO (HUD & FINAL) ---");
            BuildSuspectBoard(uiRoot.transform);
            // BuildWorldSpaceHud y BuildFinalEvaluationScreen desactivados para evitar duplicados o bloques flotantes
            BuildEndScenarioHolographicPanel(uiRoot.transform, playerRig);
            BuildCreditsAndEthicsModal(uiRoot.transform, playerRig);

            // Configurar e inyectar iluminación completa del SOC / Oficina VNB después de colocar puestos y menú
            SetupEnvironmentLighting();

            // -------------------------------------------------------------------------
            // GUARDAR Y REGISTRAR ESCENA
            // -------------------------------------------------------------------------
            string scenesDir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"{LogPrefix} <color=#38F28A><b>¡Escena guardada exitosamente en: {ScenePath}!</b></color>");

            RegisterSceneInBuildSettings(ScenePath);

            Debug.Log($"{LogPrefix} <b>==================================================================</b>");
            Debug.Log($"{LogPrefix} <color=#38F28A><b>¡ENSAMBLADO 100% COMPLETADO SIN INTERVENCIÓN MANUAL!</b></color>");
            Debug.Log($"{LogPrefix} <b>==================================================================</b>");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("ANDES-SIM: Ensamblado Exitoso",
                    $"La escena VR '{ScenePath}' ha sido construida y enlazada al 100%:\n\n" +
                    "• Capas, tags y ScriptableObjects de datos configurados.\n" +
                    "• Objeto '_SYSTEMS' con los 7 managers oficiales listos.\n" +
                    "• Rig del Player con retícula central interactiva y teletransporte confort.\n" +
                    "• Portal_Entrada y los 3 TeleportPoints oficiales.\n" +
                    "• Monitor 01 (correo Rasel Ahlam, 3 zonas de phishing, Post-It, teléfono y token MFA).\n" +
                    "• Monitor 02 (cinemática de propagación de Lazarus con pulsos de red).\n" +
                    "• Monitor 03 (terminal SIEM con botón para registrar evidencia de red).\n" +
                    "• Consola física de defensas (3 switches), Tablero de Sospechosos, HUD y Créditos.\n" +
                    "• Escena guardada y fijada como principal (índice 0) en Build Settings.",
                    "Entendido");
            }
        }

        #region Paso 2: Iluminación Ambiental

        private static void SetupEnvironmentLighting()
        {
            // Atmósfera lumínica SOC (Luz de luna tenue, 3 luces de escritorios y balizas de alerta roja)
            SocLightingController.SetupLightingAtmosphere();
        }

        #endregion

        #region Paso 3: Objeto _SYSTEMS

        private static GameObject BuildSystemsObject(ScenarioData scenarioData)
        {
            GameObject systemsObj = new GameObject("_SYSTEMS");
            systemsObj.transform.position = Vector3.zero;

            // 1. GameManager
            GameManager gm = systemsObj.AddComponent<GameManager>();
            gm.mode = SimMode.Ataque;
            if (scenarioData != null)
            {
                SerializedObject sGm = new SerializedObject(gm);
                sGm.FindProperty("data").objectReferenceValue = scenarioData;
                sGm.ApplyModifiedProperties();
            }

            // 2. SimulationClock
            systemsObj.AddComponent<SimulationClock>();

            // 3. ScenarioFlowController
            systemsObj.AddComponent<ScenarioFlowController>();

            // 4. ProgressTracker
            systemsObj.AddComponent<ProgressTracker>();

            // 5. AudioManager (Sintetizador Procedural de Audio Central y Soundtrack)
            AudioManager audioMgr = systemsObj.AddComponent<AudioManager>();
            SerializedObject sAudio = new SerializedObject(audioMgr);
            sAudio.FindProperty("menuIntroMusicClip").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/Music/Music_VrStartMenu_Intro.wav");
            sAudio.FindProperty("investigationMusicClip").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/Music/Music_CyberInvestigation_Theme.wav");
            sAudio.FindProperty("attackTensionMusicClip").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Horror_Music_Free_Starter_Kit/WAV_LIGHT_TENSION_LOOP_The_Urge_to_Kill.wav") ??
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Horror_Music_Free_Starter_Kit/WAV_LIGHT_TENSION_LOOP_Run_for_Your_Life.wav");
            sAudio.FindProperty("infectionVoiceAlertClip").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/The Alerted NPC/04. What's That.wav");
            sAudio.FindProperty("defenseVictoryMusicClip").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/Music/Music_DefenseVictory_Theme.wav");

            sAudio.FindProperty("phoneRingClip").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/SFX/SFX_Phone_Ring_3D.wav");
            sAudio.FindProperty("keyboardTypingClip").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/SFX/SFX_Keyboard_Typing.wav");
            sAudio.FindProperty("switchToggleClip").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/SFX/SFX_Switch_Toggle.wav");
            sAudio.FindProperty("laserScanClip").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/SFX/SFX_Cyber_LaserScan.wav");
            sAudio.FindProperty("teleportWhooshClip").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/SFX/SFX_Teleport_Whoosh.wav");
            sAudio.FindProperty("ambientNightOfficeClip").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/Ambience/SFX_Office_Atmosphere_Loop.wav");
            sAudio.ApplyModifiedProperties();

            // 6. CardboardXrLifecycleManager
            systemsObj.AddComponent<CardboardXrLifecycleManager>();

            // 7. UIManager
            systemsObj.AddComponent<UIManager>();

            // 8. VrInspectionOverlayManager (Sistema de Inspección y Zoom VR RF-01 a RF-05)
            systemsObj.AddComponent<AndesSim.UI.VrInspectionOverlayManager>();

            // Ambiente sonoro secundario
            GameObject audioObj = new GameObject("OfficeAudioEnvironment");
            audioObj.transform.SetParent(systemsObj.transform, false);
            audioObj.AddComponent<OfficeAudioEnvironment>();

            return systemsObj;
        }

        #endregion

        #region Paso 5: VR Player Rig

        private static GameObject BuildVrPlayerRig()
        {
            GameObject playerObj = new GameObject("Player");
            playerObj.tag = "PlayerRig";
            // Jugador posicionado en la puerta de entrada de la oficina (z = 3.40f, x = -2.60f) mirando al interior
            playerObj.transform.position = new Vector3(-2.60f, 0.0f, 3.40f);
            playerObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            playerObj.AddComponent<PlayerTeleportController>();

            // Main Camera a 1.65m estándar de altura ocular VR
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(playerObj.transform, false);
            camObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camObj.transform.localRotation = Quaternion.identity;

            Camera cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.Skybox;

            camObj.AddComponent<AudioListener>();

            // Retícula central para Google Cardboard XR configurada sobre la capa "Interactive"
            CardboardReticleAdapter reticle = camObj.AddComponent<CardboardReticleAdapter>();
            int interactiveLayer = LayerMask.NameToLayer("Interactive");
            if (interactiveLayer != -1)
            {
                SerializedObject sReticle = new SerializedObject(reticle);
                sReticle.FindProperty("interactiveMask").intValue = 1 << interactiveLayer;
                sReticle.ApplyModifiedProperties();
            }

            // Simulador de controles en Unity Editor (Mouse + WASD)
            camObj.AddComponent<EditorVrCameraSimulator>();

            return playerObj;
        }

        #endregion

        

        #region Paso 7: Puestos de Trabajo, Monitores y Periféricos

        private static GameObject CreateTeleportPoint(Transform parent, string name, Vector3 localPos, string label, string associatedWorkstationId, Quaternion? rotation = null)
        {
            GameObject tpObj = new GameObject(name);
            tpObj.tag = "TeleportPoint";
            tpObj.transform.SetParent(parent, false);
            tpObj.transform.localPosition = localPos;
            if (rotation.HasValue)
            {
                tpObj.transform.localRotation = rotation.Value;
            }
            tpObj.layer = LayerMask.NameToLayer("Interactive");

            // Disco en el suelo (Aro verde esmeralda #38f28a)
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Aro_Luminoso";
            disc.transform.SetParent(tpObj.transform, false);
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localScale = new Vector3(0.75f, 0.015f, 0.75f);

            Material ringMat = CreateColorMaterial("Mat_TeleportRing_" + name, new Color(0.22f, 0.95f, 0.54f));
            disc.GetComponent<Renderer>().sharedMaterial = ringMat;

            // Collider de mirada
            BoxCollider col = tpObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.1f, 0f);
            col.size = new Vector3(0.9f, 0.3f, 0.9f);

            GazeInteractable gaze = tpObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = "Teletransportarse a: " + label;

            TeleportPoint tp = tpObj.AddComponent<TeleportPoint>();

            SerializedObject sTp = new SerializedObject(tp);
            sTp.FindProperty("pointName").stringValue = label;
            sTp.FindProperty("associatedWorkstationId").stringValue = associatedWorkstationId;
            sTp.FindProperty("standPosition").objectReferenceValue = tpObj.transform;
            sTp.FindProperty("ringRenderer").objectReferenceValue = disc.GetComponent<Renderer>();
            sTp.FindProperty("ringColor").colorValue = new Color(0.22f, 0.95f, 0.54f, 1.0f);
            sTp.ApplyModifiedProperties();

            return tpObj;
        }

        private static void BuildWorkstationsAndMonitors(Transform parent, GameObject portalObj)
        {
            // -------------------------------------------------------------------------
            // PASO 6: Configurar las 3 Estaciones de Trabajo REALES del Escenario (VNB - Office Set)
            // (Reutilizando los 3 PCs ya presentes en la oficina, eliminando escritorios procedurales innecesarios)
            // -------------------------------------------------------------------------
            Debug.Log($"{LogPrefix} [6/8] Configurando las 3 estaciones de trabajo reales existentes en el escenario...");

            // 1. Identificar los 3 monitores existentes del escenario (new_monitor)
            GameObject monitor01 = null;
            GameObject monitor02 = null;
            GameObject monitor03 = null;

            GameObject mCandidate1 = GameObject.Find("new_monitor");
            GameObject mCandidate2 = GameObject.Find("new_monitor (4)");
            GameObject mCandidate3 = GameObject.Find("new_monitor (1)");

            List<GameObject> allMonitors = new List<GameObject>();
            if (mCandidate1 != null) allMonitors.Add(mCandidate1);
            if (mCandidate2 != null) allMonitors.Add(mCandidate2);
            if (mCandidate3 != null) allMonitors.Add(mCandidate3);

            if (allMonitors.Count < 3)
            {
                MeshFilter[] filters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
                foreach (var mf in filters)
                {
                    if (mf.sharedMesh != null && mf.sharedMesh.name.IndexOf("monitor", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!allMonitors.Contains(mf.gameObject))
                        {
                            allMonitors.Add(mf.gameObject);
                        }
                    }
                }
            }

            // Clasificar por coordenadas X:
            // x < -2.0   -> Monitor_03 (SIEM / Defensas en oficina privada SOC a x ≈ -6.37)
            // -2.0 a 3.0 -> Monitor_01 (Carla Mendoza / SWIFT a x ≈ 1.37)
            // x > 3.0    -> Monitor_02 (Analista Comercio Exterior a x ≈ 5.33)
            foreach (var mon in allMonitors)
            {
                float x = mon.transform.position.x;
                if (x < -2.0f) monitor03 = mon;
                else if (x > 3.0f) monitor02 = mon;
                else monitor01 = mon;
            }

            Material bezelMat = CreateColorMaterial("Mat_MonitorBezel_Black", new Color(0.12f, 0.12f, 0.14f));
            if (monitor01 == null)
            {
                monitor01 = BuildMonitorDisplay(parent, "Monitor_01", "Puesto 1: Carla Mendoza (SWIFT)", bezelMat);
                monitor01.transform.position = new Vector3(1.372f, 0.949f, -1.624f);
                monitor01.transform.rotation = Quaternion.Euler(0f, 171.3f, 0f);
            }
            if (monitor02 == null)
            {
                monitor02 = BuildMonitorDisplay(parent, "Monitor_02", "Puesto 2: Analista Comercio Exterior", bezelMat);
                monitor02.transform.position = new Vector3(5.335f, 0.949f, -1.478f);
                monitor02.transform.rotation = Quaternion.Euler(0f, 183.0f, 0f);
            }
            if (monitor03 == null)
            {
                monitor03 = BuildMonitorDisplay(parent, "Monitor_03", "Puesto 3: Consola SIEM (Logs)", bezelMat);
                monitor03.transform.position = new Vector3(-6.372f, 0.949f, -4.241f);
                monitor03.transform.rotation = Quaternion.Euler(0f, 159.5f, 0f);
            }

            // =========================================================================
            // PUESTO 1: Carla Mendoza (Operadora SWIFT)
            // =========================================================================
            ConfigureExistingMonitorObject(monitor01, "Monitor_01", "Puesto 1: Carla Mendoza (SWIFT)", "Monitor_01");
            MonitorMeshBuilder.BuildTerminalWorldSpaceCanvas(monitor01.transform, "Monitor_01",
                new Vector3(0f, 0.28f, 0.02f), Quaternion.Euler(0f, 180f, 0f), new Vector2(800f, 480f), new Vector3(0.00055f, 0.00055f, 0.00055f));

            PhishingInspector phishingInspector = monitor01.AddComponent<PhishingInspector>();
            MailQuarantine mailQuarantine = monitor01.AddComponent<MailQuarantine>();
            GameObject panelCorreo = BuildPhishingEmailPanel(monitor01, phishingInspector, mailQuarantine);

            // Post-It adherido al marco derecho del monitor de Carla
            GameObject postItObj = BuildPostItNote(monitor01.transform, new Vector3(0.24f, 0.18f, 0.025f));

            // Token MFA que sustituye al Post-It en Modo Defensa
            BuildTokenMfaBoveda(monitor01.transform, postItObj);

            // Cajón interactivo en el escritorio de Carla
            Material deskMat = OfficeFurnitureBuilder.CreatePbrMaterial("Mat_DeskDarkGray_111816", OfficeFurnitureBuilder.DeskDarkGrayColor, 0.42f, 0.12f);
            GameObject drawerObj = new GameObject("Cajon_Credenciales_Puesto1");
            drawerObj.transform.SetParent(parent, false);
            drawerObj.transform.position = new Vector3(1.70f, 0.50f, -1.70f);
            BuildDeskDrawer(drawerObj.transform, deskMat);

            // Teléfono IP para llamar al Anexo SOC (404)
            GameObject phoneObj = new GameObject("Telefono_Puesto1");
            phoneObj.transform.SetParent(parent, false);
            phoneObj.transform.position = new Vector3(0.95f, 0.95f, -1.75f);
            BuildSocTelephone(phoneObj.transform);

            // TeleportPoint Puesto 1 (Carla Mendoza) - ubicado frente al escritorio
            GameObject tp1 = CreateTeleportPoint(parent, "Teleport_Puesto1_Carla",
                new Vector3(1.40f, 0.01f, -2.50f), "Puesto 1: Carla Mendoza (Operadora SWIFT)", "Monitor_01", Quaternion.identity);

            // =========================================================================
            // PUESTO 2: Analista de Comercio Exterior (Destinatario #7)
            // =========================================================================
            ConfigureExistingMonitorObject(monitor02, "Monitor_02", "Puesto 2: Analista Comercio Exterior", "Monitor_02");
            MonitorMeshBuilder.BuildTerminalWorldSpaceCanvas(monitor02.transform, "Monitor_02",
                new Vector3(0f, 0.28f, 0.02f), Quaternion.Euler(0f, 180f, 0f), new Vector2(800f, 480f), new Vector3(0.00055f, 0.00055f, 0.00055f));
            MonitorMeshBuilder.BuildMonitor2TradeFinanceDisplay(monitor02);

            CreateTeleportPoint(parent, "Teleport_Puesto2_Analista",
                new Vector3(5.33f, 0.01f, -2.40f), "Puesto 2: Analista de Comercio Exterior", "Monitor_02", Quaternion.identity);

            // =========================================================================
            // PUESTO 3: Consola SIEM y Panel de Defensas (Oficina Privada / SOC)
            // =========================================================================
            ConfigureExistingMonitorObject(monitor03, "Monitor_03", "Puesto 3: Consola SIEM (Logs)", "Monitor_03");
            MonitorMeshBuilder.BuildTerminalWorldSpaceCanvas(monitor03.transform, "Monitor_03",
                new Vector3(0f, 0.28f, 0.02f), Quaternion.Euler(0f, 180f, 0f), new Vector2(800f, 480f), new Vector3(0.00055f, 0.00055f, 0.00055f));

            monitor03.AddComponent<DefenseTerminalController>();
            AccessLogTerminal siemTerminal = monitor03.AddComponent<AccessLogTerminal>();
            MonitorMeshBuilder.BuildMonitor3SiemDisplay(monitor03, siemTerminal);

            CreateTeleportPoint(parent, "Teleport_Puesto3_LogsYDefensa",
                new Vector3(-6.26f, 0.01f, -5.00f), "Puesto 3: Terminal de Registros y Panel de Defensa", "Monitor_03", Quaternion.Euler(0f, 20f, 0f));

            // Consola física de contramedidas en el escritorio de Monitor 3
            GameObject defSwitchesObj = new GameObject("Consola_Contramedidas_Puesto3");
            defSwitchesObj.transform.SetParent(parent, false);
            defSwitchesObj.transform.position = new Vector3(-5.85f, 0.95f, -4.35f);
            defSwitchesObj.transform.rotation = Quaternion.Euler(0f, 160f, 0f);
            BuildDefenseSwitches(defSwitchesObj.transform);

            // Consola de Cuarentena y Sandbox en Monitor_01 sincronizada con los 3 puestos
            BuildQuarantineConsolePanel(monitor01, monitor02, monitor03, mailQuarantine, panelCorreo);

            // Componente cinemático oficial de propagación de infección en red (InfectionSpreadVisualizer)
            BuildInfectionSpreadVisualizer(parent, monitor01, monitor02, monitor03);

            // TeleportPoint de retorno a la Puerta de Entrada (Acceso Principal)
            CreateTeleportPoint(parent, "Teleport_PuertaEntrada",
                new Vector3(-2.60f, 0.01f, 3.40f), "Puerta de Entrada (Acceso Principal)", "", Quaternion.Euler(0f, 180f, 0f));
        }

        private static void ConfigureExistingMonitorObject(GameObject monitorObj, string newName, string label, string workstationId)
        {
            monitorObj.name = newName;
            monitorObj.tag = "Monitor";
            monitorObj.layer = LayerMask.NameToLayer("Interactive");

            BoxCollider col = monitorObj.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = monitorObj.AddComponent<BoxCollider>();
            }
            col.center = new Vector3(0f, 0.28f, 0.02f);
            col.size = new Vector3(0.48f, 0.38f, 0.15f);
            col.isTrigger = false;

            InteractiveMonitorController monCtrl = monitorObj.GetComponent<InteractiveMonitorController>();
            if (monCtrl == null) monCtrl = monitorObj.AddComponent<InteractiveMonitorController>();
            SerializedObject sCtrl = new SerializedObject(monCtrl);
            sCtrl.FindProperty("monitorId").stringValue = newName;
            sCtrl.ApplyModifiedProperties();
        }

        private static GameObject BuildMonitorDisplay(Transform parent, string name, string label, Material bezelMat)
        {
            return MonitorMeshBuilder.BuildMonitor3D(parent, name, label, new Vector3(0f, 0.75f, 0.45f), Quaternion.identity);
        }

        private static GameObject BuildPhishingEmailPanel(GameObject monitor01, PhishingInspector phishingInspector, MailQuarantine mailQuarantine)
        {
            return MonitorMeshBuilder.BuildMonitor1EmailInbox(monitor01, phishingInspector, mailQuarantine);
        }

        private static GameObject CreatePhishingHotspot(string name, Transform parent, Vector2 pos, Vector2 size,
            string labelText, string signalId, string description, PhishingInspector inspector)
        {
            return MonitorMeshBuilder.CreatePhishingHotspot(name, parent, pos, size, labelText, signalId, description, inspector);
        }

        private static GameObject BuildPostItNote(Transform parent, Vector3 localPos)
        {
            Dictionary<string, Material> mats = OfficeFurnitureBuilder.CreateFurnitureMaterialPalette();
            // Post-It amarillo fluorescente (#ffc24a) adherido con credenciales SWIFT expuestas
            return OfficeFurnitureBuilder.BuildCarlaPostIt(parent, localPos, Quaternion.Euler(0f, -12f, 0f), mats);
        }

        private static void BuildDeskDrawer(Transform parent, Material deskMat)
        {
            GameObject drawerMoving = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drawerMoving.name = "Cajon_Credenciales";
            drawerMoving.transform.SetParent(parent, false);
            drawerMoving.transform.localPosition = new Vector3(0.42f, 0.58f, -0.05f);
            drawerMoving.transform.localScale = new Vector3(0.32f, 0.18f, 0.44f);
            drawerMoving.layer = LayerMask.NameToLayer("Interactive");
            drawerMoving.GetComponent<Renderer>().sharedMaterial = deskMat;

            drawerMoving.AddComponent<GazeInteractable>().interactableLabel = "Cajón de Carla Mendoza (Abrir / Cerrar)";
            drawerMoving.AddComponent<CredencialesDeskDrawer>();
        }

        private static void BuildSocTelephone(Transform parent)
        {
            GameObject phoneObj = new GameObject("Telefono_Anexo_SOC");
            phoneObj.transform.SetParent(parent, false);
            phoneObj.transform.localPosition = new Vector3(-0.52f, 0.78f, 0.05f);
            phoneObj.layer = LayerMask.NameToLayer("Interactive");

            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "Base_Telefono";
            baseObj.transform.SetParent(phoneObj.transform, false);
            baseObj.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            baseObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            baseObj.transform.localScale = new Vector3(0.22f, 0.06f, 0.20f);
            Material phoneMat = CreateColorMaterial("Mat_PhoneDark", new Color(0.12f, 0.13f, 0.16f));
            baseObj.GetComponent<Renderer>().sharedMaterial = phoneMat;

            phoneObj.AddComponent<BoxCollider>().size = new Vector3(0.26f, 0.15f, 0.24f);
            phoneObj.AddComponent<GazeInteractable>().interactableLabel = "Teléfono IP: Marcar Anexo 404 (Seguridad TI)";
            phoneObj.AddComponent<TelefonoAnexoSoc>();
        }

        private static void BuildTokenMfaBoveda(Transform parent, GameObject postItObj)
        {
            GameObject tokenObj = new GameObject("Token_MFA_Boveda");
            tokenObj.transform.SetParent(parent, false);
            tokenObj.transform.localPosition = new Vector3(0.20f, 0.78f, 0.12f);
            tokenObj.layer = LayerMask.NameToLayer("Interactive");

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Carcasa_Token";
            body.transform.SetParent(tokenObj.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            body.transform.localScale = new Vector3(0.13f, 0.024f, 0.07f);
            Material tokenMat = CreateColorMaterial("Mat_TokenDarkCasing", new Color(0.12f, 0.14f, 0.18f));
            body.GetComponent<Renderer>().sharedMaterial = tokenMat;

            tokenObj.AddComponent<BoxCollider>().size = new Vector3(0.15f, 0.04f, 0.09f);
            tokenObj.AddComponent<GazeInteractable>().interactableLabel = "Token MFA Físico: Generar Código Dinámico";
            TokenMfaBoveda mfaScript = tokenObj.AddComponent<TokenMfaBoveda>();

            SerializedObject sMfa = new SerializedObject(mfaScript);
            sMfa.FindProperty("postItPhysicalToReplace").objectReferenceValue = postItObj;
            sMfa.ApplyModifiedProperties();
        }

        private static void BuildQuarantineConsolePanel(GameObject monitor01, GameObject monitor02, GameObject monitor03,
            MailQuarantine mailQuarantine, GameObject panelCorreo)
        {
            Transform canvasTransform = monitor01.transform.Find("Canvas_Pantalla");
            if (canvasTransform == null) return;

            GameObject quarantinePanel = new GameObject("Panel_Cuarentena_Sandbox");
            quarantinePanel.transform.SetParent(canvasTransform, false);
            RectTransform rect = quarantinePanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image bg = quarantinePanel.AddComponent<Image>();
            bg.color = new Color(0.01f, 0.08f, 0.05f, 0.98f); // Fondo verde oscuro protegido

            // Título
            GameObject titleObj = new GameObject("Txt_TituloConsola");
            titleObj.transform.SetParent(quarantinePanel.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 185f);
            tRect.sizeDelta = new Vector2(760f, 40f);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b><color=#38F28A>CONSOLA DE CUARENTENA Y SANDBOX - BANCO ANDINO</color></b>";
            titleTmp.fontSize = 15f;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Log del Sandbox
            GameObject logObj = new GameObject("Txt_LogSandbox");
            logObj.transform.SetParent(quarantinePanel.transform, false);
            RectTransform lRect = logObj.AddComponent<RectTransform>();
            lRect.anchoredPosition = new Vector2(0f, 20f);
            lRect.sizeDelta = new Vector2(750f, 240f);
            TextMeshProUGUI logTmp = logObj.AddComponent<TextMeshProUGUI>();
            logTmp.text = "<color=#38F28A>[SOC-DEFENSE] 09:18:04</color> Correo de Rasel Ahlam interceptado.\n" +
                          "<color=#38F28A>[SANDBOX-ISOLATION]</color> Enlace detonado en sandbox aislado.\n" +
                          "<color=#FFC24A>[DETECCION]</color> Payload malicioso 'Rasel_Ahlam_CV_2015.scr' detectado.\n" +
                          "<color=#38F28A>[CONTENCION]</color> Bloqueo en 16 buzones. 0 equipos comprometidos.";
            logTmp.fontSize = 12f;
            logTmp.alignment = TextAlignmentOptions.TopLeft;

            // Botón interactivo: Confirmar Contención
            GameObject btnConfirmObj = CreateWorldGazeButton("Btn_ConfirmarContencion", quarantinePanel.transform,
                new Vector2(0f, -165f), new Vector2(580f, 65f), "CONFIRMAR CONTENCION Y FINALIZAR", null, null, 14f);
            Button btnConfirm = btnConfirmObj.GetComponent<Button>();

            quarantinePanel.SetActive(false); // Inicia oculto, se activa en Modo Defensa

            // Cablear en MailQuarantine
            SerializedObject sQuar = new SerializedObject(mailQuarantine);
            sQuar.FindProperty("normalEmailPanel").objectReferenceValue = panelCorreo;
            sQuar.FindProperty("quarantineConsolePanel").objectReferenceValue = quarantinePanel;
            sQuar.FindProperty("sandboxTerminalLogText").objectReferenceValue = logTmp;
            sQuar.FindProperty("confirmContainmentButton").objectReferenceValue = btnConfirm;
            sQuar.FindProperty("monitor01Controller").objectReferenceValue = monitor01.GetComponent<InteractiveMonitorController>();
            sQuar.FindProperty("monitor02Controller").objectReferenceValue = monitor02.GetComponent<InteractiveMonitorController>();
            sQuar.FindProperty("monitor03Controller").objectReferenceValue = monitor03.GetComponent<InteractiveMonitorController>();
            sQuar.ApplyModifiedProperties();

            if (btnConfirm != null)
            {
                btnConfirm.onClick.AddListener(mailQuarantine.ConfirmContainmentAndEndScenario);
            }
        }

        private static void BuildInfectionSpreadVisualizer(Transform parent, GameObject monitor01, GameObject monitor02, GameObject monitor03)
        {
            GameObject visualizerObj = new GameObject("Visualizador_Propagacion_Infeccion");
            visualizerObj.transform.SetParent(parent, false);
            visualizerObj.transform.position = Vector3.zero;

            Material laserMat = CreateColorMaterial("Mat_InfectionLaserBeam", new Color(1.0f, 0.22f, 0.20f, 1.0f));

            // LineRenderer Desk1 -> Desk2
            GameObject lineObj1 = new GameObject("Line_RedPlana_Desk1_Desk2");
            lineObj1.transform.SetParent(visualizerObj.transform, false);
            LineRenderer lr1 = lineObj1.AddComponent<LineRenderer>();
            lr1.sharedMaterial = laserMat;
            lr1.startWidth = 0.045f;
            lr1.endWidth = 0.045f;
            lr1.useWorldSpace = true;
            lr1.positionCount = 16;
            lr1.enabled = false;

            // LineRenderer Desk1 -> Desk3
            GameObject lineObj2 = new GameObject("Line_RedPlana_Desk1_Desk3");
            lineObj2.transform.SetParent(visualizerObj.transform, false);
            LineRenderer lr2 = lineObj2.AddComponent<LineRenderer>();
            lr2.sharedMaterial = laserMat;
            lr2.startWidth = 0.045f;
            lr2.endWidth = 0.045f;
            lr2.useWorldSpace = true;
            lr2.positionCount = 16;
            lr2.enabled = false;

            // Proyector fijado al techo (Y = 2.97m) + letrero holográfico legible debajo (Y = 2.52m)
            Material matRod = CreateColorMaterial("Mat_CeilingMountRod", new Color(0.18f, 0.21f, 0.26f));
            GameObject projectorBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projectorBox.name = "Proyector_Holograma_Techo";
            projectorBox.transform.SetParent(visualizerObj.transform, false);
            projectorBox.transform.position = new Vector3(3.35f, 2.97f, -1.55f);
            projectorBox.transform.localScale = new Vector3(0.65f, 0.06f, 0.16f);
            projectorBox.GetComponent<Renderer>().sharedMaterial = matRod;

            GameObject holoPanel = new GameObject("Holographic_Breach_Sign");
            holoPanel.transform.SetParent(visualizerObj.transform, false);
            holoPanel.transform.position = new Vector3(3.35f, 2.52f, -1.55f);
            holoPanel.transform.rotation = Quaternion.identity;

            Canvas holoCanvas = holoPanel.AddComponent<Canvas>();
            holoCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform hRect = holoPanel.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(420f, 95f);
            hRect.localScale = new Vector3(0.007f, 0.007f, 0.007f);

            CanvasGroup holoCg = holoPanel.AddComponent<CanvasGroup>();
            holoCg.alpha = 0f;

            Image hBg = holoPanel.AddComponent<Image>();
            hBg.color = new Color(0.08f, 0.02f, 0.02f, 0.95f);

            GameObject hTitleObj = new GameObject("Txt_HoloTitle");
            hTitleObj.transform.SetParent(holoPanel.transform, false);
            RectTransform htRect = hTitleObj.AddComponent<RectTransform>();
            htRect.anchoredPosition = new Vector2(0f, 22f);
            htRect.sizeDelta = new Vector2(390f, 30f);
            TextMeshProUGUI htTmp = hTitleObj.AddComponent<TextMeshProUGUI>();
            htTmp.text = "<b><color=#FF5A4E>ALERTA: RED COMPROMETIDA</color></b>";
            htTmp.fontSize = 18f;
            htTmp.alignment = TextAlignmentOptions.Center;

            GameObject hBodyObj = new GameObject("Txt_HoloBody");
            hBodyObj.transform.SetParent(holoPanel.transform, false);
            RectTransform hbRect = hBodyObj.AddComponent<RectTransform>();
            hbRect.anchoredPosition = new Vector2(0f, -14f);
            hbRect.sizeDelta = new Vector2(390f, 40f);
            TextMeshProUGUI hbTmp = hBodyObj.AddComponent<TextMeshProUGUI>();
            hbTmp.text = "29 ENE 2015: Malware Lazarus activo en los 3 puestos.";
            hbTmp.fontSize = 13.5f;
            hbTmp.alignment = TextAlignmentOptions.Center;

            InfectionSpreadVisualizer visualizer = visualizerObj.AddComponent<InfectionSpreadVisualizer>();
            SerializedObject sVis = new SerializedObject(visualizer);
            sVis.FindProperty("monitor01Point").objectReferenceValue = monitor01.transform;
            sVis.FindProperty("monitor02Point").objectReferenceValue = monitor02.transform;
            sVis.FindProperty("monitor03Point").objectReferenceValue = monitor03.transform;
            sVis.FindProperty("lineDesk1ToDesk2").objectReferenceValue = lr1;
            sVis.FindProperty("lineDesk1ToDesk3").objectReferenceValue = lr2;
            sVis.FindProperty("holographicSignPanel").objectReferenceValue = holoPanel;
            sVis.FindProperty("holographicSignCanvasGroup").objectReferenceValue = holoCg;
            sVis.FindProperty("holographicSignTitle").objectReferenceValue = htTmp;
            sVis.FindProperty("holographicSignBody").objectReferenceValue = hbTmp;
            sVis.FindProperty("monitor01Controller").objectReferenceValue = monitor01.GetComponent<InteractiveMonitorController>();
            sVis.FindProperty("monitor02Controller").objectReferenceValue = monitor02.GetComponent<InteractiveMonitorController>();
            sVis.FindProperty("monitor03Controller").objectReferenceValue = monitor03.GetComponent<InteractiveMonitorController>();
            sVis.ApplyModifiedProperties();
        }

        private static void BuildDeskAndChair(Transform parent, int stationIndex)
        {
            Dictionary<string, Material> mats = OfficeFurnitureBuilder.CreateFurnitureMaterialPalette();

            // 1. Escritorio moderno en color gris oscuro (#111816) con patas metálicas, faldón y cajonera
            OfficeFurnitureBuilder.BuildModernDesk(parent, new Vector3(0f, 0f, 0.35f), Quaternion.identity, mats);

            // 2. Silla ergonómica de oficina con respaldo y asiento en color verde oscuro corporativo (#072618)
            float chairAngle = (stationIndex == 0) ? -12f : (stationIndex == 2) ? 12f : 0f;
            OfficeFurnitureBuilder.BuildErgonomicChair(parent, new Vector3(0f, 0f, -0.42f), Quaternion.Euler(0f, chairAngle, 0f), mats);

            // 3. Periféricos sobre cada escritorio: Teclado ergonómico inclinado, alfombrilla, mouse óptico y taza de café nocturno
            OfficeFurnitureBuilder.BuildDeskAccessories(parent, new Vector3(0f, 0.745f, 0.35f), mats);
        }

        #endregion

        #region Paso 8: Panel de Defensas, Sospechosos, HUD, Pantalla Final y Créditos

        private static void BuildDefenseSwitches(Transform parent)
        {
            // Construir la consola física 3D de contramedidas con los 3 interruptores tridimensionales (RF-04)
            SuspectBoardMeshBuilder.BuildDefenseConsole(parent, new Vector3(-0.65f, 0.88f, 0.1f), Quaternion.Euler(-15f, 15f, 0f));
        }

        private static void BuildSuspectBoard(Transform parent)
        {
            // Construir el panel táctico de corcho 3D con las 4 fichas de los sospechosos y líneas rojas (RF-02)
            // Anclado sólidamente a la pared sur de la oficina (z = -5.92f) con soporte estructural al piso (RF-05 / QA)
            GameObject boardObj = SuspectBoardMeshBuilder.BuildTacticalCorkBoard(parent, new Vector3(2.20f, 1.55f, -5.82f), Quaternion.Euler(0f, 180f, 0f));
            boardObj.name = "Tablero_Sospechosos_VR";

            // TeleportPoint específico frente al tablero de sospechosos, mirando hacia el sur
            CreateTeleportPoint(parent, "Teleport_TableroSospechosos", new Vector3(2.20f, 0.01f, -4.40f),
                "Examinar Tablero Táctico de Sospechosos", "SuspectBoard", Quaternion.Euler(0f, 180f, 0f));
        }

        private static void BuildWorldSpaceHud(Transform parent, GameObject playerRig, UIManager uiManager)
        {
            // Erradicado WristOrDeskHUD para evitar colisiones con muros y texto invertido.
            // Solo mantenemos el banner flotante NotificationToast para feedback de evidencias.
            GameObject toastObj = new GameObject("NotificationToast");
            toastObj.transform.SetParent(parent, false);
            toastObj.transform.position = new Vector3(-3.0f, 1.85f, 2.05f);
            toastObj.transform.rotation = Quaternion.identity;

            Canvas toastCanvas = toastObj.AddComponent<Canvas>();
            toastCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform toastRect = toastObj.GetComponent<RectTransform>();
            toastRect.sizeDelta = new Vector2(300f, 65f);
            toastRect.localScale = new Vector3(0.0035f, 0.0035f, 0.0035f);
            toastObj.AddComponent<CanvasScaler>();

            CanvasGroup toastCg = toastObj.AddComponent<CanvasGroup>();
            toastCg.alpha = 0f;

            Image toastBg = toastObj.AddComponent<Image>();
            toastBg.color = UIManager.ColorDarkBg;

            GameObject tTitleObj = new GameObject("Txt_ToastTitle");
            tTitleObj.transform.SetParent(toastObj.transform, false);
            RectTransform ttRect = tTitleObj.AddComponent<RectTransform>();
            ttRect.anchoredPosition = new Vector2(0f, 14f);
            ttRect.sizeDelta = new Vector2(280f, 18f);
            TextMeshProUGUI ttTmp = tTitleObj.AddComponent<TextMeshProUGUI>();
            ttTmp.text = "<b><color=#38F28A>EVIDENCIA REGISTRADA</color></b>";
            ttTmp.fontSize = 8.5f;
            ttTmp.alignment = TextAlignmentOptions.Center;

            GameObject tMsgObj = new GameObject("Txt_ToastMessage");
            tMsgObj.transform.SetParent(toastObj.transform, false);
            RectTransform tmRect = tMsgObj.AddComponent<RectTransform>();
            tmRect.anchoredPosition = new Vector2(0f, -8f);
            tmRect.sizeDelta = new Vector2(280f, 24f);
            TextMeshProUGUI tmTmp = tMsgObj.AddComponent<TextMeshProUGUI>();
            tmTmp.text = "Se ha añadido la evidencia al informe forense.";
            tmTmp.fontSize = 7f;
            tmTmp.alignment = TextAlignmentOptions.Center;

            // Cablear UIManager
            if (uiManager != null)
            {
                SerializedObject sUi = new SerializedObject(uiManager);
                sUi.FindProperty("wristOrDeskHudPanel").objectReferenceValue = null;
                sUi.FindProperty("modeStatusText").objectReferenceValue = null;
                sUi.FindProperty("toggleModeButton").objectReferenceValue = null;
                sUi.FindProperty("toggleModeButtonText").objectReferenceValue = null;
                sUi.FindProperty("countdownClockText").objectReferenceValue = null;
                sUi.FindProperty("currentStepTitleText").objectReferenceValue = null;
                sUi.FindProperty("phishingSignalsChecklistText").objectReferenceValue = null;
                sUi.FindProperty("evidencesChecklistText").objectReferenceValue = null;
                sUi.FindProperty("defensesChecklistText").objectReferenceValue = null;
                sUi.FindProperty("notificationToastPanel").objectReferenceValue = toastObj;
                sUi.FindProperty("toastCanvasGroup").objectReferenceValue = toastCg;
                sUi.FindProperty("toastTitleText").objectReferenceValue = ttTmp;
                sUi.FindProperty("toastMessageText").objectReferenceValue = tmTmp;
                sUi.FindProperty("toastBackground").objectReferenceValue = toastBg;
                sUi.ApplyModifiedProperties();
            }
        }

        private static void BuildFinalEvaluationScreen(Transform parent)
        {
            GameObject evalObj = new GameObject("Pantalla_Final_Evaluacion");
            evalObj.transform.SetParent(parent, false);
            evalObj.transform.position = new Vector3(0f, 1.5f, 2.5f);

            Canvas canvas = evalObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = evalObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500f, 350f);
            rect.localScale = new Vector3(0.003f, 0.003f, 0.003f);
            evalObj.AddComponent<CanvasScaler>();
            evalObj.AddComponent<GraphicRaycaster>();

            evalObj.AddComponent<FinalEvaluationScreen>();
            evalObj.SetActive(false); // Se activa dinámicamente al finalizar
        }

        private static void BuildEndScenarioHolographicPanel(Transform parent, GameObject playerRig)
        {
            GameObject endObj = new GameObject("Pantalla_Final_EndScenario");
            endObj.transform.SetParent(parent, false);
            endObj.transform.position = new Vector3(0f, 1.45f, 0.5f);

            Canvas canvas = endObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = endObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(480f, 300f);
            rect.localScale = new Vector3(0.0035f, 0.0035f, 0.0035f);
            endObj.AddComponent<CanvasScaler>();
            endObj.AddComponent<GraphicRaycaster>();

            endObj.AddComponent<EndScenarioPanelUI>();
        }

        private static void BuildCreditsAndEthicsModal(Transform parent, GameObject playerRig)
        {
            GameObject modalObj = new GameObject("Modal_Creditos_Etica");
            modalObj.transform.SetParent(parent, false);
            modalObj.transform.position = new Vector3(0f, 1.45f, -1.8f);

            Canvas canvas = modalObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = modalObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(960f, 650f);
            rect.localScale = new Vector3(0.0019f, 0.0019f, 0.0019f);
            modalObj.AddComponent<CanvasScaler>();
            modalObj.AddComponent<GraphicRaycaster>();

            modalObj.AddComponent<CreditsAndEthicsModalUI>();
        }

        #endregion

        #region Utilidades de UI y Materiales

        private static GameObject CreateWorldGazeButton(string name, Transform parent, Vector2 pos, Vector2 size, string text,
            Color? normalColor = null, Color? highlightedColor = null, float fontSize = 8f, Color? textColor = null)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.layer = LayerMask.NameToLayer("Interactive");

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Color nColor = normalColor ?? new Color(0.12f, 0.28f, 0.42f, 0.9f);
            Color hColor = highlightedColor ?? new Color(0.22f, 0.95f, 0.54f, 0.95f);
            Color pColor = new Color(hColor.r * 0.7f, hColor.g * 0.7f, hColor.b * 0.7f, 1f);

            Image img = btnObj.AddComponent<Image>();
            img.color = nColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = nColor;
            cb.highlightedColor = hColor;
            cb.pressedColor = pColor;
            btn.colors = cb;

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor ?? Color.white;

            BoxCollider col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 45f);
            col.center = new Vector3(0f, 0f, -15f);
            col.isTrigger = true;

            GazeInteractable gaze = btnObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = text.Replace("<b>", "").Replace("</b>", "").Split('\n')[0];
            gaze.dwellTime = 1.1f;

            WorldSpaceUiGazeButton wsBtn = btnObj.AddComponent<WorldSpaceUiGazeButton>();

            return btnObj;
        }

        #region Menú de Inicio VR Simple y Flotante (Sin muro ni arco intrusivo)

        private static void AddUiEdgeBar(string name, Transform parent, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(parent, false);
            RectTransform r = bar.AddComponent<RectTransform>();
            r.anchoredPosition = anchoredPos;
            r.sizeDelta = sizeDelta;
            Image img = bar.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static GameObject BuildSimpleVrStartMenu(Transform parent)
        {
            GameObject menuRoot = new GameObject("MenuInicio_Simple_VR");
            menuRoot.transform.SetParent(parent, false);
            menuRoot.transform.position = new Vector3(-2.60f, 1.45f, 2.35f);
            menuRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            menuRoot.layer = LayerMask.NameToLayer("Interactive");

            Material matKioskBase = CreateColorMaterial("Mat_Kiosk_MetalDark", new Color(0.06f, 0.09f, 0.14f));
            Material matKioskNeon = CreateColorMaterial("Mat_Kiosk_NeonCyan", new Color(0f, 0.85f, 1f));
            matKioskNeon.EnableKeyword("_EMISSION");
            matKioskNeon.SetColor("_EmissionColor", new Color(0f, 0.85f, 1f) * 1.2f);

            GameObject kioskBaseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kioskBaseObj.name = "Quiosco_Base_Piso";
            kioskBaseObj.transform.SetParent(menuRoot.transform, false);
            kioskBaseObj.transform.localPosition = new Vector3(0f, -1.42f, 0f);
            kioskBaseObj.transform.localScale = new Vector3(0.85f, 0.06f, 0.45f);
            kioskBaseObj.GetComponent<Renderer>().sharedMaterial = matKioskBase;

            GameObject kioskTrimObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kioskTrimObj.name = "Quiosco_Trim_Neon";
            kioskTrimObj.transform.SetParent(menuRoot.transform, false);
            kioskTrimObj.transform.localPosition = new Vector3(0f, -1.385f, 0f);
            kioskTrimObj.transform.localScale = new Vector3(0.88f, 0.015f, 0.48f);
            kioskTrimObj.GetComponent<Renderer>().sharedMaterial = matKioskNeon;

            GameObject kioskColObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kioskColObj.name = "Quiosco_Columna_Soporte";
            kioskColObj.transform.SetParent(menuRoot.transform, false);
            kioskColObj.transform.localPosition = new Vector3(0f, -0.92f, 0f);
            kioskColObj.transform.localScale = new Vector3(0.18f, 0.96f, 0.10f);
            kioskColObj.GetComponent<Renderer>().sharedMaterial = matKioskBase;

            GameObject kioskStripeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kioskStripeObj.name = "Quiosco_Franja_Luminosa";
            kioskStripeObj.transform.SetParent(menuRoot.transform, false);
            kioskStripeObj.transform.localPosition = new Vector3(0f, -0.92f, -0.055f);
            kioskStripeObj.transform.localScale = new Vector3(0.04f, 0.94f, 0.015f);
            kioskStripeObj.GetComponent<Renderer>().sharedMaterial = matKioskNeon;

            // Canvas World Space amplio y de alta legibilidad VR
            GameObject canvasObj = new GameObject("Canvas_MenuInicio_Simple");
            canvasObj.transform.SetParent(menuRoot.transform, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform cRect = canvasObj.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(860f, 520f);
            cRect.localScale = new Vector3(0.0019f, 0.0019f, 0.0019f);
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();

            // Fondo Dark Glass
            GameObject bgObj = new GameObject("Panel_Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.06f, 0.11f, 0.96f);

            // Borde perimetral real con 4 barras cian (sin tapar el fondo ni el texto)
            Color frameCyan = new Color(0f, 0.88f, 1f, 0.85f);
            AddUiEdgeBar("Border_Top", bgObj.transform, new Vector2(0f, 258f), new Vector2(860f, 4f), frameCyan);
            AddUiEdgeBar("Border_Bottom", bgObj.transform, new Vector2(0f, -258f), new Vector2(860f, 4f), frameCyan);
            AddUiEdgeBar("Border_Left", bgObj.transform, new Vector2(-428f, 0f), new Vector2(4f, 520f), frameCyan);
            AddUiEdgeBar("Border_Right", bgObj.transform, new Vector2(428f, 0f), new Vector2(4f, 520f), frameCyan);

            // Título y Subtítulo
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(canvasObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 206f);
            tRect.sizeDelta = new Vector2(810f, 44f);
            TextMeshProUGUI tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "<b><color=#00F2FE>BANCO ANDINO</color> | SIMULADOR VR</b>";
            tTmp.fontSize = 26f;
            tTmp.alignment = TextAlignmentOptions.Center;
            tTmp.color = Color.white;

            GameObject subObj = new GameObject("SubTitle");
            subObj.transform.SetParent(canvasObj.transform, false);
            RectTransform sRect = subObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 164f);
            sRect.sizeDelta = new Vector2(810f, 30f);
            TextMeshProUGUI sTmp = subObj.AddComponent<TextMeshProUGUI>();
            sTmp.text = "Elige el modo para empezar:";
            sTmp.fontSize = 18f;
            sTmp.alignment = TextAlignmentOptions.Center;
            sTmp.color = new Color(0.75f, 0.84f, 0.94f, 1f);

            // Vista 1: Selección de Modos
            GameObject panelModos = new GameObject("Panel_VistaModos");
            panelModos.transform.SetParent(canvasObj.transform, false);
            RectTransform pmRect = panelModos.AddComponent<RectTransform>();
            pmRect.anchorMin = Vector2.zero;
            pmRect.anchorMax = Vector2.one;
            pmRect.sizeDelta = Vector2.zero;

            // Marco exterior Ataque (evita teñir todo el fondo del botón)
            GameObject frameAtkObj = new GameObject("Frame_ModoAtaque");
            frameAtkObj.transform.SetParent(panelModos.transform, false);
            RectTransform faRect = frameAtkObj.AddComponent<RectTransform>();
            faRect.anchoredPosition = new Vector2(-205f, 55f);
            faRect.sizeDelta = new Vector2(380f, 150f);
            Image imgAtkBorder = frameAtkObj.AddComponent<Image>();
            imgAtkBorder.color = new Color(1f, 0.35f, 0.31f, 1f);

            GameObject btnAtkObj = CreateWorldGazeButton("Btn_SelectAtaque", frameAtkObj.transform,
                Vector2.zero, new Vector2(368f, 138f),
                "<b><size=19><color=#FF5A4E>MODO ATAQUE</color></size></b>\n<size=15><color=#E2E8F0>Mira el phishing y el robo\n($81M en vivo)</color></size>",
                new Color(0.16f, 0.05f, 0.07f, 0.98f), new Color(1f, 0.35f, 0.31f, 1f), 16f);
            Button btnAtk = btnAtkObj.GetComponent<Button>();

            // Marco exterior Defensa
            GameObject frameDefObj = new GameObject("Frame_ModoDefensa");
            frameDefObj.transform.SetParent(panelModos.transform, false);
            RectTransform fdRect = frameDefObj.AddComponent<RectTransform>();
            fdRect.anchoredPosition = new Vector2(205f, 55f);
            fdRect.sizeDelta = new Vector2(380f, 150f);
            Image imgDefBorder = frameDefObj.AddComponent<Image>();
            imgDefBorder.color = new Color(0.18f, 0.30f, 0.44f, 0.70f);

            GameObject btnDefObj = CreateWorldGazeButton("Btn_SelectDefensa", frameDefObj.transform,
                Vector2.zero, new Vector2(368f, 138f),
                "<b><size=19><color=#38F28A>MODO DEFENSA</color></size></b>\n<size=15><color=#E2E8F0>Detecta pistas, bloquea\ny activa MFA</color></size>",
                new Color(0.05f, 0.16f, 0.10f, 0.98f), new Color(0.22f, 0.95f, 0.54f, 1f), 16f);
            Button btnDef = btnDefObj.GetComponent<Button>();

            GameObject badgeObj = new GameObject("Txt_ModoBadge");
            badgeObj.transform.SetParent(panelModos.transform, false);
            RectTransform bRect = badgeObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(0f, -42f);
            bRect.sizeDelta = new Vector2(790f, 34f);
            TextMeshProUGUI bTmp = badgeObj.AddComponent<TextMeshProUGUI>();
            bTmp.text = "MODO ACTIVO: ATAQUE";
            bTmp.fontSize = 19f;
            bTmp.fontStyle = FontStyles.Bold;
            bTmp.alignment = TextAlignmentOptions.Center;
            bTmp.color = new Color(1f, 0.35f, 0.31f, 1f);

            GameObject descObj = new GameObject("Txt_ModoDescripcion");
            descObj.transform.SetParent(panelModos.transform, false);
            RectTransform dRect = descObj.AddComponent<RectTransform>();
            dRect.anchoredPosition = new Vector2(0f, -92f);
            dRect.sizeDelta = new Vector2(790f, 68f);
            TextMeshProUGUI dTmp = descObj.AddComponent<TextMeshProUGUI>();
            dTmp.text = "1. Revisa el correo en PC 1\n2. Abre el enlace y mira la infeccion\n3. Ubica al responsable en el tablero";
            dTmp.fontSize = 15.5f;
            dTmp.alignment = TextAlignmentOptions.Center;
            dTmp.color = new Color(0.88f, 0.94f, 1f, 1f);

            // Vista 2: Guía Visual de 3 Pasos (Oculta al inicio)
            GameObject panelPasos = new GameObject("Panel_VistaPasos");
            panelPasos.transform.SetParent(canvasObj.transform, false);
            RectTransform ppRect = panelPasos.AddComponent<RectTransform>();
            ppRect.anchorMin = Vector2.zero;
            ppRect.anchorMax = Vector2.one;
            ppRect.sizeDelta = Vector2.zero;

            GameObject pasosCard = new GameObject("Card_GuiaPasos");
            pasosCard.transform.SetParent(panelPasos.transform, false);
            RectTransform pcRect = pasosCard.AddComponent<RectTransform>();
            pcRect.anchoredPosition = new Vector2(0f, 14f);
            pcRect.sizeDelta = new Vector2(780f, 236f);
            Image pcBg = pasosCard.AddComponent<Image>();
            pcBg.color = new Color(0.06f, 0.11f, 0.18f, 0.96f);

            GameObject pasosTxtObj = new GameObject("Txt_GuiaPasos");
            pasosTxtObj.transform.SetParent(pasosCard.transform, false);
            RectTransform ptRect = pasosTxtObj.AddComponent<RectTransform>();
            ptRect.sizeDelta = new Vector2(740f, 210f);
            TextMeshProUGUI pTmp = pasosTxtObj.AddComponent<TextMeshProUGUI>();
            pTmp.text =
                "<b><color=#00F2FE>COMO FUNCIONA EL SIMULADOR (3 PASOS)</color></b>\n\n" +
                "<b>1. ENTRA Y MIRA PC 1:</b> Se abrira el correo sospechoso y el Post-It.\n" +
                "<b>2. PRESIONA LAS PISTAS:</b> Revisa 1ra parte, marca pistas o ve a PC 3 (SIEM).\n" +
                "<b>3. TABLERO / DEFENSA:</b> Elige sospechoso o activa MFA y Sandbox.";
            pTmp.fontSize = 18f;
            pTmp.alignment = TextAlignmentOptions.MidlineLeft;
            pTmp.color = Color.white;
            panelPasos.SetActive(false);

            // Botón Principal: Iniciar Simulación
            GameObject btnStartObj = CreateWorldGazeButton("Btn_IniciarSimulacion", canvasObj.transform,
                new Vector2(0f, -166f), new Vector2(540f, 60f),
                "<b>[ > INICIAR SIMULACION ]</b>",
                new Color(0.08f, 0.45f, 0.55f, 0.98f), new Color(0f, 0.95f, 1f, 1f), 20f);
            Button btnStart = btnStartObj.GetComponent<Button>();

            // Fila Inferior de Utilidades (Cómo Funciona + Créditos)
            GameObject btnPasosObj = CreateWorldGazeButton("Btn_TogglePasos", canvasObj.transform,
                new Vector2(-210f, -226f), new Vector2(360f, 44f),
                "[ ? COMO FUNCIONA ]",
                new Color(0.10f, 0.20f, 0.34f, 0.95f), new Color(0f, 0.90f, 1f, 1f), 16f);
            Button btnPasos = btnPasosObj.GetComponent<Button>();
            TextMeshProUGUI txtPasosLabel = btnPasosObj.GetComponentInChildren<TextMeshProUGUI>();

            GameObject btnCreditosObj = CreateWorldGazeButton("Btn_AbrirCreditos", canvasObj.transform,
                new Vector2(210f, -226f), new Vector2(360f, 44f),
                "[ CREDITOS Y ETICA ]",
                new Color(0.14f, 0.16f, 0.28f, 0.95f), new Color(0.35f, 0.85f, 1f, 1f), 16f);
            Button btnCreditos = btnCreditosObj.GetComponent<Button>();

            // Configurar VrStartMenuController
            VrStartMenuController startController = menuRoot.AddComponent<VrStartMenuController>();
            startController.BtnModoAtaque = btnAtk;
            startController.BtnModoDefensa = btnDef;
            startController.ImgModoAtaqueBorder = imgAtkBorder;
            startController.ImgModoDefensaBorder = imgDefBorder;
            startController.TxtModoSeleccionadoBadge = bTmp;
            startController.TxtModoDescripcion = dTmp;
            startController.BtnIniciarSimulacion = btnStart;
            startController.MenuCanvasGroup = canvasGroup;
            startController.BtnTogglePasos = btnPasos;
            startController.TxtBtnTogglePasosLabel = txtPasosLabel;
            startController.PanelVistaModos = panelModos;
            startController.PanelVistaPasos = panelPasos;
            startController.BtnAbrirCreditos = btnCreditos;

            GameObject tp1Obj = GameObject.Find("Teleport_Puesto1_Carla");
            if (tp1Obj != null)
            {
                startController.TeleportDestination = tp1Obj.GetComponent<TeleportPoint>();
            }

            Debug.Log($"{LogPrefix} <color=#38F28A>Menú de Inicio VR Simple y Flotante creado exitosamente.</color>");
            return menuRoot;
        }

        private static void InstantiateWallPrefabOrCube(GameObject prefab, GameObject fallbackPrefab, Transform parent,
            string name, Vector3 pos, Quaternion rot, Material fallbackMat)
        {
            GameObject chosen = prefab != null ? prefab : fallbackPrefab;
            if (chosen != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(chosen, parent) as GameObject;
                if (instance != null)
                {
                    instance.name = name;
                    instance.transform.position = pos;
                    instance.transform.rotation = rot;
                    return;
                }
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = pos + (rot * new Vector3(-1f, 1.5f, 0f));
            cube.transform.rotation = rot;
            cube.transform.localScale = new Vector3(2f, 3f, 0.2f);
            cube.GetComponent<Renderer>().sharedMaterial = fallbackMat;
        }

        private static void CompleteVnbOfficeEnclosureAndCeiling(GameObject envRoot)
        {
            if (envRoot == null) return;

            GameObject existing = GameObject.Find("Estructura_Cerramiento_Techo_VR");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

            GameObject root = new GameObject("Estructura_Cerramiento_Techo_VR");
            root.transform.SetParent(envRoot.transform, false);

            GameObject wallStdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VNB - Office Set/Prefabs/Building/wall_standard.prefab");
            GameObject wallWoodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VNB - Office Set/Prefabs/Building/wall_standard_wood_alt_1.prefab");

            Material matWallFallback = RoomArchitectureBuilder.CreatePbrMaterial("Mat_Oficina_ParedCierre", new Color(0.80f, 0.82f, 0.86f), 0.2f, 0.0f);
            Material matCeilingSlab = RoomArchitectureBuilder.CreatePbrMaterial("Mat_Oficina_TechoAcustico", new Color(0.84f, 0.86f, 0.89f), 0.15f, 0.0f);
            Material matBeamDark = RoomArchitectureBuilder.CreatePbrMaterial("Mat_Oficina_VigaOscura", new Color(0.18f, 0.21f, 0.26f), 0.35f, 0.2f);
            Material matTrofferFrame = RoomArchitectureBuilder.CreatePbrMaterial("Mat_Luminaria_Marco", new Color(0.12f, 0.15f, 0.20f), 0.45f, 0.35f);
            Material matTrofferGlow = RoomArchitectureBuilder.CreatePbrMaterial("Mat_Luminaria_DifusorLED", new Color(0.98f, 0.99f, 1.0f), 0.90f, 0.0f, new Color(0.98f, 0.99f, 1.0f) * 2.4f);
            Material matCoveNeon = RoomArchitectureBuilder.CreatePbrMaterial("Mat_Techo_CoveNeon", new Color(0.30f, 0.88f, 0.98f), 0.90f, 0.0f, new Color(0.30f, 0.88f, 0.98f) * 1.15f);
            Material matClerestory = RoomArchitectureBuilder.CreatePbrMaterial("Mat_Cerramiento_SocSuperior", new Color(0.66f, 0.76f, 0.84f), 0.72f, 0.1f);

            // 1. Muro Este completo (X = +8.00, rotY = -90 -> Z cubre [-6 .. +4])
            Transform eastGroup = new GameObject("Muro_Este_Cerramiento").transform;
            eastGroup.SetParent(root.transform, false);
            float[] eastZ = { -4f, -2f, 0f, 2f, 4f };
            for (int i = 0; i < eastZ.Length; i++)
            {
                bool useWood = (i == 1 || i == 3);
                InstantiateWallPrefabOrCube(useWood ? wallWoodPrefab : wallStdPrefab, wallStdPrefab, eastGroup,
                    $"Pared_Este_0{i + 1}", new Vector3(8.00f, 0f, eastZ[i]), Quaternion.Euler(0f, -90f, 0f), matWallFallback);
            }

            // 2. Muro Sur completo (Z = -6.00, rotY = 0 -> X cubre [-8 .. +8])
            Transform southGroup = new GameObject("Muro_Sur_Cerramiento").transform;
            southGroup.SetParent(root.transform, false);
            float[] southX = { -6f, -4f, -2f, 0f, 2f, 4f, 6f, 8f };
            for (int i = 0; i < southX.Length; i++)
            {
                // Mantener muro liso detras del Tablero de Sospechosos (X = 0..4)
                bool useWood = (i == 1 || i == 6);
                InstantiateWallPrefabOrCube(useWood ? wallWoodPrefab : wallStdPrefab, wallStdPrefab, southGroup,
                    $"Pared_Sur_0{i + 1}", new Vector3(southX[i], 0f, -6.00f), Quaternion.identity, matWallFallback);
            }

            // 3. Columnas estructurales en las 4 esquinas exteriores
            Vector3[] cornerPositions =
            {
                new Vector3(-8.00f, 1.50f, -6.00f),
                new Vector3(8.00f, 1.50f, -6.00f),
                new Vector3(8.00f, 1.50f, 4.00f),
                new Vector3(-8.00f, 1.50f, 4.00f)
            };
            for (int c = 0; c < cornerPositions.Length; c++)
            {
                GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cube);
                col.name = $"Columna_Esquina_0{c + 1}";
                col.transform.SetParent(root.transform, false);
                col.transform.position = cornerPositions[c];
                col.transform.localScale = new Vector3(0.24f, 3.04f, 0.24f);
                col.GetComponent<Renderer>().sharedMaterial = matBeamDark;
            }

            // 4. Cerramiento superior (2.50m .. 3.00m) sobre las paredes medianas del Cubiculo SOC Oeste
            BuildTransomSegment(root.transform, "Cerramiento_SOC_Norte", new Vector3(-6.00f, 2.75f, 0.00f), new Vector3(4.00f, 0.50f, 0.12f), matClerestory, matBeamDark);
            BuildTransomSegment(root.transform, "Cerramiento_SOC_Este", new Vector3(-4.00f, 2.75f, -3.00f), new Vector3(0.12f, 0.50f, 6.00f), matClerestory, matBeamDark);

            // 5. Techo Arquitectonico Completo (cara inferior exacta en Y = 3.00m)
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Techo_Oficina_Losa";
            slab.transform.SetParent(root.transform, false);
            slab.transform.position = new Vector3(0.00f, 3.04f, -1.00f);
            slab.transform.localScale = new Vector3(16.24f, 0.08f, 10.24f);
            Renderer slabRend = slab.GetComponent<Renderer>();
            slabRend.sharedMaterial = matCeilingSlab;
            slabRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Vigas transversales y molduras perimetrales del techo
            float[] beamXs = { -4.00f, 0.00f, 4.00f };
            for (int b = 0; b < beamXs.Length; b++)
            {
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = $"Viga_Techo_0{b + 1}";
                beam.transform.SetParent(root.transform, false);
                beam.transform.position = new Vector3(beamXs[b], 2.97f, -1.00f);
                beam.transform.localScale = new Vector3(0.18f, 0.06f, 9.88f);
                beam.GetComponent<Renderer>().sharedMaterial = matBeamDark;
            }

            // Moldura perimetral y linea de luz ambiental (Cove Neon)
            CreateCeilingCoveTrim(root.transform, "Moldura_Norte", new Vector3(0f, 2.97f, 3.86f), new Vector3(15.76f, 0.06f, 0.14f), new Vector3(15.60f, 0.015f, 0.03f), new Vector3(0f, 2.938f, 3.86f), matBeamDark, matCoveNeon);
            CreateCeilingCoveTrim(root.transform, "Moldura_Sur", new Vector3(0f, 2.97f, -5.86f), new Vector3(15.76f, 0.06f, 0.14f), new Vector3(15.60f, 0.015f, 0.03f), new Vector3(0f, 2.938f, -5.86f), matBeamDark, matCoveNeon);

            // 6. 8 Luminarias LED empotradas alineadas exactamente con los Point Lights del SOC
            Vector3[] trofferPositions =
            {
                new Vector3(-2.60f, 2.985f, 2.75f),
                new Vector3(1.40f, 2.985f, 1.80f),
                new Vector3(-5.80f, 2.985f, 1.60f),
                new Vector3(-5.60f, 2.985f, -3.20f),
                new Vector3(5.20f, 2.985f, 1.60f),
                new Vector3(2.20f, 2.985f, -4.40f),
                new Vector3(1.37f, 2.985f, -2.07f),
                new Vector3(5.34f, 2.985f, -1.93f)
            };

            for (int t = 0; t < trofferPositions.Length; t++)
            {
                GameObject fixture = new GameObject($"Luminaria_Techo_LED_0{t + 1}");
                fixture.transform.SetParent(root.transform, false);
                fixture.transform.position = trofferPositions[t];

                GameObject bezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bezel.name = "Marco";
                bezel.transform.SetParent(fixture.transform, false);
                bezel.transform.localScale = new Vector3(1.06f, 0.035f, 0.62f);
                Renderer br = bezel.GetComponent<Renderer>();
                br.sharedMaterial = matTrofferFrame;
                br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                GameObject diffuser = GameObject.CreatePrimitive(PrimitiveType.Cube);
                diffuser.name = "Difusor_LED";
                diffuser.transform.SetParent(fixture.transform, false);
                diffuser.transform.localPosition = new Vector3(0f, -0.015f, 0f);
                diffuser.transform.localScale = new Vector3(0.94f, 0.018f, 0.50f);
                Renderer dr = diffuser.GetComponent<Renderer>();
                dr.sharedMaterial = matTrofferGlow;
                dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            Debug.Log($"{LogPrefix} <color=#38F28A>Oficina cerrada con muros Este/Sur, techo arquitectonico y 8 luminarias LED.</color>");
        }

        private static void BuildTransomSegment(Transform parent, string name, Vector3 centerPos, Vector3 size, Material panelMat, Material frameMat)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);

            GameObject pane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pane.name = "Panel";
            pane.transform.SetParent(group.transform, false);
            pane.transform.position = centerPos;
            pane.transform.localScale = new Vector3(size.x, size.y - 0.05f, size.z);
            pane.GetComponent<Renderer>().sharedMaterial = panelMat;

            GameObject bottomBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomBar.name = "Perfil_Inf";
            bottomBar.transform.SetParent(group.transform, false);
            bottomBar.transform.position = new Vector3(centerPos.x, 2.515f, centerPos.z);
            bottomBar.transform.localScale = new Vector3(size.x + 0.02f, 0.035f, size.z + 0.03f);
            bottomBar.GetComponent<Renderer>().sharedMaterial = frameMat;

            GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBar.name = "Perfil_Sup";
            topBar.transform.SetParent(group.transform, false);
            topBar.transform.position = new Vector3(centerPos.x, 2.985f, centerPos.z);
            topBar.transform.localScale = new Vector3(size.x + 0.02f, 0.035f, size.z + 0.03f);
            topBar.GetComponent<Renderer>().sharedMaterial = frameMat;
        }

        private static void CreateCeilingCoveTrim(Transform parent, string name, Vector3 pos, Vector3 scale, Vector3 neonScale, Vector3 neonPos, Material darkMat, Material neonMat)
        {
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = name;
            trim.transform.SetParent(parent, false);
            trim.transform.position = pos;
            trim.transform.localScale = scale;
            trim.GetComponent<Renderer>().sharedMaterial = darkMat;

            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = name + "_LED";
            strip.transform.SetParent(parent, false);
            strip.transform.position = neonPos;
            strip.transform.localScale = neonScale;
            Renderer sr = strip.GetComponent<Renderer>();
            sr.sharedMaterial = neonMat;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        #endregion


        #region Menú de Inicio VR Holográfico 3D (Vestíbulo)

        private static GameObject BuildVrStartMenuTerminal(Transform parent, GameObject portalObj)
        {
            GameObject terminalRoot = new GameObject("Terminal_MenuInicio_VR");
            terminalRoot.transform.SetParent(parent, false);
            terminalRoot.transform.position = new Vector3(0f, 0f, -3.7f); // Centrado en el vestíbulo frente al jugador
            terminalRoot.transform.rotation = Quaternion.identity; // Orientado de frente hacia el jugador en -5.2m (lectura normal no invertida)
            terminalRoot.layer = LayerMask.NameToLayer("Interactive");

            // Materiales de alta tecnología
            Material matMetalDark = CreateColorMaterial("Mat_Terminal_MetalDark", new Color(0.06f, 0.09f, 0.14f));
            Material matFrameSlate = CreateColorMaterial("Mat_Terminal_Frame", new Color(0.14f, 0.18f, 0.25f));
            Material matNeonCyan = CreateColorMaterial("Mat_Terminal_NeonCyan", new Color(0.0f, 0.95f, 1.0f));
            matNeonCyan.EnableKeyword("_EMISSION");
            matNeonCyan.SetColor("_EmissionColor", new Color(0.0f, 0.95f, 1.0f) * 1.5f);

            // 1. Pedestal Físico Futurista
            GameObject pedestal = new GameObject("Pedestal_Fisico");
            pedestal.transform.SetParent(terminalRoot.transform, false);

            // Base biselada
            GameObject basePlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePlat.name = "Plataforma_Base";
            basePlat.transform.SetParent(pedestal.transform, false);
            basePlat.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            basePlat.transform.localScale = new Vector3(1.1f, 0.08f, 0.65f);
            basePlat.GetComponent<Renderer>().sharedMaterial = matMetalDark;

            // Trim Neón Base
            GameObject baseTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseTrim.name = "Neon_Trim_Base";
            baseTrim.transform.SetParent(pedestal.transform, false);
            baseTrim.transform.localPosition = new Vector3(0f, 0.082f, 0f);
            baseTrim.transform.localScale = new Vector3(1.14f, 0.015f, 0.69f);
            baseTrim.GetComponent<Renderer>().sharedMaterial = matNeonCyan;

            // Columna inclinada hacia atrás (ergonomía)
            GameObject colObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            colObj.name = "Columna_Soporte";
            colObj.transform.SetParent(pedestal.transform, false);
            colObj.transform.localPosition = new Vector3(0f, 0.50f, 0.06f);
            colObj.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            colObj.transform.localScale = new Vector3(0.32f, 0.88f, 0.24f);
            colObj.GetComponent<Renderer>().sharedMaterial = matFrameSlate;

            // Tiras neón en la columna
            GameObject stripL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripL.name = "Tira_Neon_L";
            stripL.transform.SetParent(colObj.transform, false);
            stripL.transform.localPosition = new Vector3(-0.52f, 0f, 0.52f);
            stripL.transform.localScale = new Vector3(0.08f, 0.98f, 0.12f);
            stripL.GetComponent<Renderer>().sharedMaterial = matNeonCyan;

            GameObject stripR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripR.name = "Tira_Neon_R";
            stripR.transform.SetParent(colObj.transform, false);
            stripR.transform.localPosition = new Vector3(0.52f, 0f, 0.52f);
            stripR.transform.localScale = new Vector3(0.08f, 0.98f, 0.12f);
            stripR.GetComponent<Renderer>().sharedMaterial = matNeonCyan;

            // Mesa de control / consola inclinada
            GameObject consoleDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            consoleDeck.name = "Mesa_Consola";
            consoleDeck.transform.SetParent(pedestal.transform, false);
            consoleDeck.transform.localPosition = new Vector3(0f, 0.92f, -0.06f);
            consoleDeck.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            consoleDeck.transform.localScale = new Vector3(0.85f, 0.05f, 0.38f);
            consoleDeck.GetComponent<Renderer>().sharedMaterial = matMetalDark;

            // Proyector holográfico (disco emisor)
            GameObject holoPrism = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            holoPrism.name = "Emisor_Holograma";
            holoPrism.transform.SetParent(pedestal.transform, false);
            holoPrism.transform.localPosition = new Vector3(0f, 0.98f, -0.06f);
            holoPrism.transform.localScale = new Vector3(0.20f, 0.02f, 0.20f);
            holoPrism.GetComponent<Renderer>().sharedMaterial = matNeonCyan;

            // Luz suave ambiental de la terminal
            GameObject lightObj = new GameObject("Luz_Holograma_Terminal");
            lightObj.transform.SetParent(terminalRoot.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.8f, -0.1f);
            Light holoLight = lightObj.AddComponent<Light>();
            holoLight.type = LightType.Point;
            holoLight.color = new Color(0f, 0.85f, 1f);
            holoLight.intensity = 1.35f;
            holoLight.range = 3.5f;

            // 2. Canvas World Space de la Terminal
            GameObject canvasObj = new GameObject("Canvas_Menu_Holografico");
            canvasObj.transform.SetParent(terminalRoot.transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 1.40f, -0.06f);
            canvasObj.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform cRect = canvasObj.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(1040f, 660f);
            cRect.localScale = new Vector3(0.00155f, 0.00155f, 0.00155f);

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();

            // Panel de fondo translúcido cyberpunk
            GameObject bgObj = new GameObject("Bg_Terminal");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.06f, 0.11f, 0.95f);

            // Borde neón del panel
            GameObject borderObj = new GameObject("Borde_Neon");
            borderObj.transform.SetParent(bgObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(6f, 6f);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(0f, 0.95f, 1f, 0.35f);
            borderObj.transform.SetAsFirstSibling();

            // HEADER: Título y estado
            GameObject headerObj = new GameObject("Header_Menu");
            headerObj.transform.SetParent(canvasObj.transform, false);
            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 290f);
            hRect.sizeDelta = new Vector2(1000f, 65f);

            GameObject topBadgeObj = new GameObject("TopBadge");
            topBadgeObj.transform.SetParent(headerObj.transform, false);
            RectTransform tbRect = topBadgeObj.AddComponent<RectTransform>();
            tbRect.anchoredPosition = new Vector2(0f, 20f);
            tbRect.sizeDelta = new Vector2(980f, 18f);
            TextMeshProUGUI tbTmp = topBadgeObj.AddComponent<TextMeshProUGUI>();
            tbTmp.text = "<color=#38F28A>[SISTEMA: EN LINEA]</color> · <color=#00F2FE>PROTOCOLO FORENSE VR</color> · <color=#FFC24A>FECHA INCIDENTE: 29/01/2015</color>";
            tbTmp.fontSize = 11f;
            tbTmp.alignment = TextAlignmentOptions.Center;

            GameObject titleObj = new GameObject("MainTitle");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, -2f);
            tRect.sizeDelta = new Vector2(980f, 32f);
            TextMeshProUGUI tTmp = titleObj.AddComponent<TextMeshProUGUI>();
            tTmp.text = "<b><color=#00F2FE>ANDES-SIM: BANCO ANDINO</color> · <color=#FFFFFF>OPERACION LAZARUS 2015</color></b>";
            tTmp.fontSize = 17f;
            tTmp.alignment = TextAlignmentOptions.Center;

            GameObject subObj = new GameObject("SubTitle");
            subObj.transform.SetParent(headerObj.transform, false);
            RectTransform sRect = subObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, -22f);
            sRect.sizeDelta = new Vector2(980f, 22f);
            TextMeshProUGUI sTmp = subObj.AddComponent<TextMeshProUGUI>();
            sTmp.text = "<color=#94A3B8>SISTEMA DE ENTRENAMIENTO FORENSE Y GESTION DE INCIDENTES SOC (VR)</color>";
            sTmp.fontSize = 11.5f;
            sTmp.alignment = TextAlignmentOptions.Center;

            // BARRA DE PESTAÑAS (5 TABS)
            GameObject tabBarObj = new GameObject("Barra_Pestanas");
            tabBarObj.transform.SetParent(canvasObj.transform, false);
            RectTransform tabRect = tabBarObj.AddComponent<RectTransform>();
            tabRect.anchoredPosition = new Vector2(0f, 235f);
            tabRect.sizeDelta = new Vector2(1000f, 38f);

            string[] tabNames = new string[] {
                "1. MISION & MODOS",
                "2. SOSPECHOSOS",
                "3. TUTORIAL VR",
                "4. ETICA & LEYES",
                "5. AJUSTES"
            };

            Button[] tabButtons = new Button[5];
            Image[] tabButtonBgs = new Image[5];
            TextMeshProUGUI[] tabButtonLabels = new TextMeshProUGUI[5];
            float tabWidth = 192f;
            float tabSpacing = 6f;
            float startX = -((tabWidth * 5 + tabSpacing * 4) * 0.5f) + tabWidth * 0.5f;

            for (int i = 0; i < 5; i++)
            {
                float posX = startX + i * (tabWidth + tabSpacing);
                GameObject tBtnObj = CreateWorldGazeButton($"TabBtn_{i}", tabBarObj.transform,
                    new Vector2(posX, 0f), new Vector2(tabWidth, 36f), tabNames[i],
                    new Color(0.10f, 0.16f, 0.25f, 0.85f), new Color(0f, 0.95f, 1f, 0.95f), 12f);

                tabButtons[i] = tBtnObj.GetComponent<Button>();
                tabButtonBgs[i] = tBtnObj.GetComponent<Image>();
                tabButtonLabels[i] = tBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            }

            // CONTENEDOR DE PANELES
            GameObject panelsContainer = new GameObject("Contenedor_Paneles");
            panelsContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform pRect = panelsContainer.AddComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(0f, -25f);
            pRect.sizeDelta = new Vector2(1000f, 460f);

            GameObject[] tabPanels = new GameObject[5];

            // -----------------------------------------------------------------
            // PANEL 0: MISIÓN & MODOS
            // -----------------------------------------------------------------
            GameObject p0 = new GameObject("Panel_0_MisionYModos");
            p0.transform.SetParent(panelsContainer.transform, false);
            RectTransform p0Rect = p0.AddComponent<RectTransform>();
            p0Rect.anchorMin = Vector2.zero;
            p0Rect.anchorMax = Vector2.one;
            p0Rect.sizeDelta = Vector2.zero;
            tabPanels[0] = p0;

            // Briefing Banner
            GameObject briefObj = new GameObject("Briefing_Banner");
            briefObj.transform.SetParent(p0.transform, false);
            RectTransform brRect = briefObj.AddComponent<RectTransform>();
            brRect.anchoredPosition = new Vector2(0f, 185f);
            brRect.sizeDelta = new Vector2(980f, 65f);
            Image brImg = briefObj.AddComponent<Image>();
            brImg.color = new Color(0.08f, 0.14f, 0.22f, 0.85f);

            GameObject brTxtObj = new GameObject("Text");
            brTxtObj.transform.SetParent(briefObj.transform, false);
            RectTransform brtRect = brTxtObj.AddComponent<RectTransform>();
            brtRect.anchorMin = Vector2.zero;
            brtRect.anchorMax = Vector2.one;
            brtRect.sizeDelta = new Vector2(-16f, -8f);
            TextMeshProUGUI brTmp = brTxtObj.AddComponent<TextMeshProUGUI>();
            brTmp.text = "<b><color=#00F2FE>SIMULADOR FORENSE DE CIBERSEGURIDAD · BANCO ANDINO</color></b>\n" +
                         "<size=12><color=#D6F2E2>Investiga el correo de Spear Phishing enviado a Carla Mendoza y ejecuta la respuesta correspondiente.</size>";
            brTmp.fontSize = 11.5f;
            brTmp.alignment = TextAlignmentOptions.Center;

            // Card Modo Ataque (Izquierda)
            GameObject cAttackObj = new GameObject("Card_ModoAtaque");
            cAttackObj.transform.SetParent(p0.transform, false);
            RectTransform caRect = cAttackObj.AddComponent<RectTransform>();
            caRect.anchoredPosition = new Vector2(-248f, 40f);
            caRect.sizeDelta = new Vector2(475f, 175f);
            Image caImg = cAttackObj.AddComponent<Image>();
            caImg.color = new Color(0.12f, 0.06f, 0.08f, 0.9f);

            GameObject caBorderObj = new GameObject("Border");
            caBorderObj.transform.SetParent(cAttackObj.transform, false);
            RectTransform cabRect = caBorderObj.AddComponent<RectTransform>();
            cabRect.anchorMin = Vector2.zero;
            cabRect.anchorMax = Vector2.one;
            cabRect.sizeDelta = new Vector2(4f, 4f);
            Image caBorder = caBorderObj.AddComponent<Image>();
            caBorder.color = new Color(1f, 0.35f, 0.31f, 1f);
            caBorderObj.transform.SetAsFirstSibling();

            GameObject caTxtObj = new GameObject("Text");
            caTxtObj.transform.SetParent(cAttackObj.transform, false);
            RectTransform catRect = caTxtObj.AddComponent<RectTransform>();
            catRect.anchoredPosition = new Vector2(0f, 26f);
            catRect.sizeDelta = new Vector2(450f, 110f);
            TextMeshProUGUI caTmp = caTxtObj.AddComponent<TextMeshProUGUI>();
            caTmp.text = "<size=18><b><color=#FF5A4E>[ ! MODO ATAQUE 2016 ]</color></b></size>\n" +
                         "<size=13.5><color=#D1D5DB>Recreación del ciberataque histórico real de Lazarus</color></size>\n\n" +
                         "• <b>Vector:</b> Spear Phishing con dropper .scr a Carla Mendoza\n" +
                         "• <b>Salto Lateral:</b> Red plana bancaria sin VLANs hacia SWIFT\n" +
                         "• <b>Impacto:</b> Fraude y robo financiero (US$ 81M a Filipinas)";
            caTmp.fontSize = 13.5f;
            caTmp.alignment = TextAlignmentOptions.TopLeft;

            GameObject btnAtkObj = CreateWorldGazeButton("Btn_SelectAtaque", cAttackObj.transform,
                new Vector2(0f, -60f), new Vector2(300f, 38f), "SELECCIONAR MODO ATAQUE",
                new Color(0.45f, 0.12f, 0.15f, 0.95f), new Color(1f, 0.35f, 0.31f, 1f), 13.5f);
            Button btnAtk = btnAtkObj.GetComponent<Button>();

            // Card Modo Defensa (Derecha)
            GameObject cDefObj = new GameObject("Card_ModoDefensa");
            cDefObj.transform.SetParent(p0.transform, false);
            RectTransform cdRect = cDefObj.AddComponent<RectTransform>();
            cdRect.anchoredPosition = new Vector2(248f, 40f);
            cdRect.sizeDelta = new Vector2(475f, 175f);
            Image cdImg = cDefObj.AddComponent<Image>();
            cdImg.color = new Color(0.06f, 0.12f, 0.08f, 0.9f);

            GameObject cdBorderObj = new GameObject("Border");
            cdBorderObj.transform.SetParent(cDefObj.transform, false);
            RectTransform cdbRect = cdBorderObj.AddComponent<RectTransform>();
            cdbRect.anchorMin = Vector2.zero;
            cdbRect.anchorMax = Vector2.one;
            cdbRect.sizeDelta = new Vector2(4f, 4f);
            Image cdBorder = cdBorderObj.AddComponent<Image>();
            cdBorder.color = new Color(0.2f, 0.3f, 0.4f, 0.5f);
            cdBorderObj.transform.SetAsFirstSibling();

            GameObject cdTxtObj = new GameObject("Text");
            cdTxtObj.transform.SetParent(cDefObj.transform, false);
            RectTransform cdtRect = cdTxtObj.AddComponent<RectTransform>();
            cdtRect.anchoredPosition = new Vector2(0f, 26f);
            cdtRect.sizeDelta = new Vector2(450f, 110f);
            TextMeshProUGUI cdTmp = cdTxtObj.AddComponent<TextMeshProUGUI>();
            cdTmp.text = "<size=18><b><color=#38F28A>[ + MODO DEFENSA SOC ]</color></b></size>\n" +
                         "<size=13.5><color=#D1D5DB>Respuesta táctica y contención perimetral del SOC</color></size>\n\n" +
                         "• <b>Detección:</b> Alerta por remitente falso sin SPF/DKIM\n" +
                         "• <b>Sandbox Cuckoo:</b> Aislamiento preventivo del adjunto en Sandbox\n" +
                         "• <b>Resultado:</b> Bloqueo de IP C2 y 100% de fondos protegidos";
            cdTmp.fontSize = 13.5f;
            cdTmp.alignment = TextAlignmentOptions.TopLeft;

            GameObject btnDefObj = CreateWorldGazeButton("Btn_SelectDefensa", cDefObj.transform,
                new Vector2(0f, -60f), new Vector2(300f, 38f), "SELECCIONAR MODO DEFENSA",
                new Color(0.12f, 0.35f, 0.20f, 0.95f), new Color(0.22f, 0.95f, 0.54f, 1f), 13.5f);
            Button btnDef = btnDefObj.GetComponent<Button>();

            // Modo Badge
            GameObject mBadgeObj = new GameObject("Txt_ModoBadge");
            mBadgeObj.transform.SetParent(p0.transform, false);
            RectTransform mbRect = mBadgeObj.AddComponent<RectTransform>();
            mbRect.anchoredPosition = new Vector2(0f, -80f);
            mbRect.sizeDelta = new Vector2(950f, 24f);
            TextMeshProUGUI mbTmp = mBadgeObj.AddComponent<TextMeshProUGUI>();
            mbTmp.text = "MODO SELECCIONADO: <color=#FF5A4E><b>[MODO ATAQUE 2016 - HISTÓRICO]</b></color>";
            mbTmp.fontSize = 14.5f;
            mbTmp.alignment = TextAlignmentOptions.Center;

            // Modo Descripción detallada
            GameObject mDescObj = new GameObject("Txt_ModoDesc");
            mDescObj.transform.SetParent(p0.transform, false);
            RectTransform mdRect = mDescObj.AddComponent<RectTransform>();
            mdRect.anchoredPosition = new Vector2(0f, -112f);
            mdRect.sizeDelta = new Vector2(950f, 40f);
            TextMeshProUGUI mdTmp = mDescObj.AddComponent<TextMeshProUGUI>();
            mdTmp.text = "<b>Objetivo:</b> Recrear la cadena de intrusión histórica. Entra a la oficina para examinar la estación de Carla Mendoza.";
            mdTmp.fontSize = 13f;
            mdTmp.alignment = TextAlignmentOptions.Center;

            // Gran Botón de Inicio de Simulación
            GameObject btnStartObj = CreateWorldGazeButton("Btn_IniciarSimulacion", p0.transform,
                new Vector2(0f, -170f), new Vector2(660f, 60f), "<b><size=17>[ > ENTRAR A LA OFICINA (INICIAR) ]</size></b>",
                new Color(0.08f, 0.45f, 0.55f, 0.98f), new Color(0f, 0.95f, 1f, 1f), 17f);
            Button btnStart = btnStartObj.GetComponent<Button>();

            // -----------------------------------------------------------------
            // PANEL 1: INTELIGENCIA & 4 SOSPECHOSOS
            // -----------------------------------------------------------------
            GameObject p1 = new GameObject("Panel_1_Sospechosos");
            p1.transform.SetParent(panelsContainer.transform, false);
            RectTransform p1Rect = p1.AddComponent<RectTransform>();
            p1Rect.anchorMin = Vector2.zero;
            p1Rect.anchorMax = Vector2.one;
            p1Rect.sizeDelta = Vector2.zero;
            tabPanels[1] = p1;

            CreateSuspectCardUI("Card_Torres", p1.transform, new Vector2(-248f, 95f),
                "EXPEDIENTE 01: ING. SAMUEL TORRES", "JEFE DE SEGURIDAD TI",
                "Permisos Domain Admin en toda la red bancaria.\nOmitio filtrar ejecutables .scr en Exchange.\nMantuvo red plana sin VLANs hacia el switch SWIFT.",
                "Emitio 3 memorandos alertando falta de firewall.\nSus credenciales personales no ejecutaron transferencias.", "#FF5A4E");

            CreateSuspectCardUI("Card_Benavides", p1.transform, new Vector2(248f, 95f),
                "EXPEDIENTE 02: LIC. HUGO BENAVIDES", "GERENTE DE OPERACIONES",
                "Voto en contra de adquirir Cuckoo Sandbox por costos.\nTolero el uso de notas adhesivas con contrasenas.\nIgnoro advertencias de auditoria sobre la impresora SWIFT.",
                "Cero conocimientos tecnicos sobre redes.\nFuera de la sede bancaria durante el incidente.", "#FFC24A");

            CreateSuspectCardUI("Card_Mendoza", p1.transform, new Vector2(-248f, -105f),
                "EXPEDIENTE 03: CARLA MENDOZA", "OPERADORA SENIOR SWIFT (PACIENTE CERO)",
                "Su sesion de usuario autorizo transferencias fraudulentas.\nContrasena personal pegada en Post-It visible en monitor.",
                "Enganada por spear phishing hiper-dirigido de Rasel Ahlam.\nNo se encontraba en la sede a la hora de las transacciones.\nRecord laboral intachable durante 7 anos.", "#38F28A");

            CreateSuspectCardUI("Card_Aliaga", p1.transform, new Vector2(248f, -105f),
                "EXPEDIENTE 04: RENZO ALIAGA", "TECNICO DE SOPORTE NETSOLUTIONS",
                "Deudas bancarias graves en cobranza judicial.\nRegistros SIEM de conexion nocturna anomala a las 23:42.\nMantenimiento fisico bajo mesas sin supervision directa.",
                "Ordenes de trabajo formales para cableado de red.\nNo se detecto insercion de memorias USB en registros.", "#FF5A4E");

            // -----------------------------------------------------------------
            // PANEL 2: TUTORIAL VR & GUÍA DE CONTROLES
            // -----------------------------------------------------------------
            GameObject p2 = new GameObject("Panel_2_TutorialVR");
            p2.transform.SetParent(panelsContainer.transform, false);
            RectTransform p2Rect = p2.AddComponent<RectTransform>();
            p2Rect.anchorMin = Vector2.zero;
            p2Rect.anchorMax = Vector2.one;
            p2Rect.sizeDelta = Vector2.zero;
            tabPanels[2] = p2;

            CreateTutorialColumnUI("Col_EditorPC", p2.transform, new Vector2(-248f, 0f),
                "CONTROLES EN UNITY EDITOR (MODO PC)",
                "[-] <b>Giro de Cabeza (360):</b> Mantener Clic Derecho o Tecla 'Alt' y mover el raton.\n\n" +
                "[-] <b>Inclinacion Lateral (Roll):</b> Mantener Tecla 'Ctrl' y mover raton horizontalmente.\n\n" +
                "[-] <b>Interaccion con Objetos:</b> Apuntar con la reticula central y hacer Clic Izquierdo.\n\n" +
                "[-] <b>Permanencia de Mirada (Dwell):</b> Mantener la reticula sobre cualquier boton durante 1.8 segundos.\n\n" +
                "[-] <b>Atajo de Desarrollo:</b> Tecla 'M' para alternar instantaneamente entre Ataque y Defensa.");

            CreateTutorialColumnUI("Col_MobileVR", p2.transform, new Vector2(248f, 0f),
                "CONTROLES EN MOVIL (GOOGLE CARDBOARD XR)",
                "[OK] <b>Giro Inmersivo:</b> El giroscopio interno del smartphone rastrea tu cabeza a 60 FPS.\n\n" +
                "[OK] <b>Seleccion por Mirada:</b> Mira fijamente cualquier elemento interactivo. El anillo circular se completara en 1.8s.\n\n" +
                "[OK] <b>Gatillo / Toque:</b> Presiona el boton superior del visor Cardboard o toca la pantalla tactil para clic inmediato.\n\n" +
                "[OK] <b>Teletransporte Confort:</b> Mira a los aros luminosos [TELEPORT] en el suelo para trasladarte sin mareo.\n\n" +
                "[OK] <b>Audio Espacial:</b> Usa auriculares estereo para escuchar la direccion de las alarmas.");

            // -----------------------------------------------------------------
            // PANEL 3: MARCO ÉTICO-LEGAL & CRÉDITOS
            // -----------------------------------------------------------------
            GameObject p3 = new GameObject("Panel_3_EticaYLeyes");
            p3.transform.SetParent(panelsContainer.transform, false);
            RectTransform p3Rect = p3.AddComponent<RectTransform>();
            p3Rect.anchorMin = Vector2.zero;
            p3Rect.anchorMax = Vector2.one;
            p3Rect.sizeDelta = Vector2.zero;
            tabPanels[3] = p3;

            CreateLegalBoxUI(p3.transform);

            // -----------------------------------------------------------------
            // PANEL 4: AJUSTES & ACCESIBILIDAD
            // -----------------------------------------------------------------
            GameObject p4 = new GameObject("Panel_4_Ajustes");
            p4.transform.SetParent(panelsContainer.transform, false);
            RectTransform p4Rect = p4.AddComponent<RectTransform>();
            p4Rect.anchorMin = Vector2.zero;
            p4Rect.anchorMax = Vector2.one;
            p4Rect.sizeDelta = Vector2.zero;
            tabPanels[4] = p4;

            CreateSettingsBoxUI(p4.transform, out Button btnFast, out Button btnNorm, out Button btnAcc,
                out TextMeshProUGUI txtDwellBadge, out Button btnTogAudio, out TextMeshProUGUI txtAudioStat);

            // CABLEADO FINAL EN VrStartMenuController
            VrStartMenuController startController = terminalRoot.AddComponent<VrStartMenuController>();
            startController.TabPanels = tabPanels;
            startController.TabButtons = tabButtons;
            startController.TabButtonBgs = tabButtonBgs;
            startController.TabButtonLabels = tabButtonLabels;

            startController.BtnModoAtaque = btnAtk;
            startController.BtnModoDefensa = btnDef;
            startController.ImgModoAtaqueBorder = caBorder;
            startController.ImgModoDefensaBorder = cdBorder;
            startController.TxtModoSeleccionadoBadge = mbTmp;
            startController.TxtModoDescripcion = mdTmp;
            startController.BtnIniciarSimulacion = btnStart;

            startController.BtnDwellRapido = btnFast;
            startController.BtnDwellEstandar = btnNorm;
            startController.BtnDwellAccesible = btnAcc;
            startController.TxtDwellActualBadge = txtDwellBadge;
            startController.BtnToggleAudio = btnTogAudio;
            startController.TxtAudioStatus = txtAudioStat;
            startController.MenuCanvasGroup = canvasGroup;

            // Iniciar por defecto en Panel 0
            for (int i = 0; i < tabPanels.Length; i++)
            {
                tabPanels[i].SetActive(i == 0);
            }

            Debug.Log($"{LogPrefix} <color=#38F28A>Terminal Holográfica 3D del Menú de Inicio VR construida con 5 pestañas interactivas.</color>");
            return terminalRoot;
        }

        private static void CreateSuspectCardUI(string name, Transform parent, Vector2 pos,
            string title, string role, string against, string favor, string colorHex)
        {
            GameObject cardObj = new GameObject(name);
            cardObj.transform.SetParent(parent, false);
            RectTransform cRect = cardObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = pos;
            cRect.sizeDelta = new Vector2(475f, 185f);
            Image img = cardObj.AddComponent<Image>();
            img.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);

            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(cardObj.transform, false);
            RectTransform bRect = borderObj.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.sizeDelta = new Vector2(3f, 3f);
            Image bImg = borderObj.AddComponent<Image>();
            ColorUtility.TryParseHtmlString(colorHex, out Color c);
            bImg.color = c;
            borderObj.transform.SetAsFirstSibling();

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(cardObj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-16f, -12f);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<size=17><b><color={colorHex}>{title}</color></b></size>\n" +
                       $"<size=13.5><color=#94A3B8>{role}</color></size>\n\n" +
                       $"<size=13><color=#FF5A4E><b>[INDICIOS EN CONTRA]:</b></color>\n{against}\n\n" +
                       $"<color=#38F28A><b>[ELEMENTOS A FAVOR]:</b></color>\n{favor}</size>";
            tmp.fontSize = 13.5f;
            tmp.alignment = TextAlignmentOptions.TopLeft;
        }

        private static void CreateTutorialColumnUI(string name, Transform parent, Vector2 pos, string title, string content)
        {
            GameObject colObj = new GameObject(name);
            colObj.transform.SetParent(parent, false);
            RectTransform cRect = colObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = pos;
            cRect.sizeDelta = new Vector2(475f, 390f);
            Image img = colObj.AddComponent<Image>();
            img.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(colObj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-20f, -20f);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<size=19><b><color=#00F2FE>{title}</color></b></size>\n\n" +
                       $"<size=14.5><line-height=125%>{content}</line-height></size>";
            tmp.fontSize = 14.5f;
            tmp.alignment = TextAlignmentOptions.TopLeft;
        }

        private static void CreateLegalBoxUI(Transform parent)
        {
            GameObject boxObj = new GameObject("Legal_Container");
            boxObj.transform.SetParent(parent, false);
            RectTransform bRect = boxObj.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.sizeDelta = new Vector2(-20f, -20f);
            Image img = boxObj.AddComponent<Image>();
            img.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(boxObj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-24f, -24f);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text =
                "<size=19><b><color=#00F2FE>MARCO ETICO-LEGAL Y LEGISLACION APLICABLE (PERU)</color></b></size>\n\n" +
                "<size=14.5><b>[-] LEY NO. 30096 - LEY DE DELITOS INFORMATICOS:</b>\n" +
                "• <b>Articulo 2 (Acceso Ilicito):</b> Sanciona el acceso indebido a sistemas informaticos protegidos.\n" +
                "• <b>Articulo 3 (Atentado a la Integridad de Datos):</b> Penaliza la alteracion o dano a programas o datos.\n" +
                "• <b>Articulo 8 (Fraude Informatico):</b> Sanciona la suplantacion o desvio ilegitimo de transferencias bancarias.\n\n" +
                "<b>[-] LEY NO. 29733 - LEY DE PROTECCION DE DATOS PERSONALES:</b>\n" +
                "• Obliga a entidades financieras a implementar medidas de seguridad organizativas y tecnicas para custodiar informacion bancaria confidencial.\n\n" +
                "<b>[PROPOSITO PEDAGOGICO & CREDITOS ACADEMICOS]:</b>\n" +
                "Este software es un entorno interactivo educativo para el desarrollo de competencias en ciberseguridad defensiva y analisis forense.\n" +
                "<b>Desarrollador:</b> Equipo 1 · Proyecto ANDES-SIM (Unity 6 LTS · Cardboard XR · URP).</size>";
            tmp.fontSize = 14.5f;
            tmp.alignment = TextAlignmentOptions.TopLeft;
        }

        private static void CreateSettingsBoxUI(Transform parent,
            out Button btnFast, out Button btnNorm, out Button btnAcc,
            out TextMeshProUGUI txtDwellBadge, out Button btnTogAudio, out TextMeshProUGUI txtAudioStat)
        {
            GameObject setObj = new GameObject("Settings_Container");
            setObj.transform.SetParent(parent, false);
            RectTransform sRect = setObj.AddComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.sizeDelta = new Vector2(-20f, -20f);
            Image img = setObj.AddComponent<Image>();
            img.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);

            // Título
            GameObject tObj = new GameObject("Title");
            tObj.transform.SetParent(setObj.transform, false);
            RectTransform tRect = tObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 150f);
            tRect.sizeDelta = new Vector2(900f, 32f);
            TextMeshProUGUI tmp = tObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "<b><color=#00F2FE>CONFIGURACION DE ACCESIBILIDAD, MIRADA Y AUDIO VR</color></b>";
            tmp.fontSize = 14f;
            tmp.alignment = TextAlignmentOptions.Center;

            // Dwell section
            GameObject dSubObj = new GameObject("DwellSub");
            dSubObj.transform.SetParent(setObj.transform, false);
            RectTransform dsRect = dSubObj.AddComponent<RectTransform>();
            dsRect.anchoredPosition = new Vector2(0f, 95f);
            dsRect.sizeDelta = new Vector2(900f, 26f);
            TextMeshProUGUI dsTmp = dSubObj.AddComponent<TextMeshProUGUI>();
            dsTmp.text = "<b>TIEMPO DE PERMANENCIA DE MIRADA (GAZE DWELL TIME):</b>";
            dsTmp.fontSize = 12f;
            dsTmp.alignment = TextAlignmentOptions.Center;

            GameObject bFObj = CreateWorldGazeButton("Btn_Dwell12", setObj.transform,
                new Vector2(-240f, 50f), new Vector2(210f, 38f), "1.2s (Rapido)",
                new Color(0.12f, 0.25f, 0.38f, 0.9f), new Color(0f, 0.95f, 1f, 1f), 12f);
            btnFast = bFObj.GetComponent<Button>();

            GameObject bNObj = CreateWorldGazeButton("Btn_Dwell18", setObj.transform,
                new Vector2(0f, 50f), new Vector2(210f, 38f), "1.8s (Estandar)",
                new Color(0.12f, 0.38f, 0.28f, 0.9f), new Color(0.22f, 0.95f, 0.54f, 1f), 12f);
            btnNorm = bNObj.GetComponent<Button>();

            GameObject bAObj = CreateWorldGazeButton("Btn_Dwell25", setObj.transform,
                new Vector2(240f, 50f), new Vector2(210f, 38f), "2.5s (Accesible)",
                new Color(0.38f, 0.25f, 0.12f, 0.9f), new Color(1f, 0.75f, 0.25f, 1f), 12f);
            btnAcc = bAObj.GetComponent<Button>();

            GameObject dBadgeObj = new GameObject("DwellBadge");
            dBadgeObj.transform.SetParent(setObj.transform, false);
            RectTransform dbRect = dBadgeObj.AddComponent<RectTransform>();
            dbRect.anchoredPosition = new Vector2(0f, 10f);
            dbRect.sizeDelta = new Vector2(900f, 24f);
            txtDwellBadge = dBadgeObj.AddComponent<TextMeshProUGUI>();
            txtDwellBadge.text = "TIEMPO DE MIRADA ACTUAL: <b>1.8s</b>";
            txtDwellBadge.fontSize = 11.5f;
            txtDwellBadge.alignment = TextAlignmentOptions.Center;

            // Audio Section
            GameObject aSubObj = new GameObject("AudioSub");
            aSubObj.transform.SetParent(setObj.transform, false);
            RectTransform asRect = aSubObj.AddComponent<RectTransform>();
            asRect.anchoredPosition = new Vector2(0f, -40f);
            asRect.sizeDelta = new Vector2(900f, 26f);
            TextMeshProUGUI asTmp = aSubObj.AddComponent<TextMeshProUGUI>();
            asTmp.text = "<b>CONTROL DE AUDIO GENERAL:</b>";
            asTmp.fontSize = 12f;
            asTmp.alignment = TextAlignmentOptions.Center;

            GameObject bTogAudObj = CreateWorldGazeButton("Btn_ToggleAudio", setObj.transform,
                new Vector2(0f, -80f), new Vector2(280f, 38f), "SILENCIAR / ACTIVAR AUDIO",
                new Color(0.22f, 0.15f, 0.35f, 0.9f), new Color(0.85f, 0.45f, 1f, 1f), 12f);
            btnTogAudio = bTogAudObj.GetComponent<Button>();

            GameObject aStatObj = new GameObject("AudioStatus");
            aStatObj.transform.SetParent(setObj.transform, false);
            RectTransform astRect = aStatObj.AddComponent<RectTransform>();
            astRect.anchoredPosition = new Vector2(0f, -120f);
            astRect.sizeDelta = new Vector2(900f, 24f);
            txtAudioStat = aStatObj.AddComponent<TextMeshProUGUI>();
            txtAudioStat.text = "AUDIO: [ACTIVO 100%]";
            txtAudioStat.fontSize = 11.5f;
            txtAudioStat.alignment = TextAlignmentOptions.Center;
        }

        #endregion

        private static void BuildAtmosphericParticles(GameObject obj)
        {
            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.maxParticles = 50;
            main.startLifetime = 7.0f;
            main.startSpeed = 0.04f;
            main.startSize = 0.025f;
            main.startColor = new Color(0.35f, 0.85f, 1f, 0.25f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.loop = true;

            var emission = ps.emission;
            emission.rateOverTime = 6f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(9f, 2.5f, 8f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(0.01f);
            vel.y = new ParticleSystem.MinMaxCurve(0.015f);
            vel.z = new ParticleSystem.MinMaxCurve(0.01f);

            ParticleSystemRenderer rend = obj.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = CreateColorMaterial("Mat_CyberMoteParticle", new Color(0.4f, 0.9f, 1f, 0.35f));
        }

        private static Material CreateColorMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            return mat;
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Remover si ya existía en otra posición
            scenes.RemoveAll(s => string.Equals(s.path, scenePath, StringComparison.OrdinalIgnoreCase));

            // Insertar al inicio (Índice 0)
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"{LogPrefix} <color=#38F28A>Escena '{scenePath}' fijada como principal en EditorBuildSettings.scenes[0].</color>");
        }

        #endregion
    }
}
