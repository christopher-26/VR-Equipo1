using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AndesSim.Audio;
using AndesSim.Core;
using AndesSim.Data;
using AndesSim.Scenario01;
using AndesSim.UI;
using AndesSim.VR;

namespace AndesSim.Editor
{
    /// <summary>
    /// Generador automatizado de la escena oficial 'Scenario01_OficinaPagos.unity'.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Construye la jerarquía completa: Core Managers, VR Player Rig, Arquitectura 3D de la oficina,
    ///   los 3 Puestos de Trabajo (Carla Mendoza, Comercio Exterior, SIEM), Post-It, Tablero de Sospechosos,
    ///   Consola de Defensas, Puntos de Teletransporte por Mirada y HUD World Space.
    /// - Configura capas, tags, luces y componentes interactivos para Google Cardboard XR.
    /// </summary>
    public static class Scenario01SceneBuilder
    {
        private const string ScenePath = "Assets/_AndesSim/Scenes/Scenario01_OficinaPagos.unity";
        private const string LogPrefix = "[ANDES-SIM - SceneBuilder]";

        [MenuItem("ANDES-SIM/Herramientas/Construir Escena Alternativa (Scenario01)", false, 100)]
        public static void BuildCompleteScenario01Scene()
        {
            ExecuteBuildCompleteScenario01Scene(showDialog: false);
        }

        [InitializeOnLoadMethod]
        private static void AutoBuildSceneIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (!System.IO.File.Exists(ScenePath))
                {
                    Debug.Log($"{LogPrefix} Escena oficial no detectada. Construyendo automáticamente en segundo plano...");
                    ExecuteBuildCompleteScenario01Scene(showDialog: false);
                }
            };
        }

        public static void ExecuteBuildCompleteScenario01Scene(bool showDialog)
        {
            Debug.Log($"{LogPrefix} <b>Iniciando construcción automatizada de la Escena 1...</b>");

            // 1. Crear nueva escena limpia
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Configurar RenderSettings (Iluminación ambiental de oficina)
            SetupLightingAndEnvironment();

            // 3. Crear Jerarquía de Core Managers
            GameObject managersRoot = new GameObject("--- CORE MANAGERS ---");
            BuildCoreManagers(managersRoot);

            // 4. Crear Entorno 3D (Oficina del Banco Andino)
            GameObject envRoot = new GameObject("--- ENTORNO 3D: BANCO ANDINO ---");
            BuildOfficeEnvironment(envRoot);

            // 5. Crear Puestos de Trabajo e Interacciones
            GameObject stationsRoot = new GameObject("--- PUESTOS DE TRABAJO (RF-01) ---");
            BuildWorkstations(stationsRoot.transform);

            // 6. Crear Tablero de Sospechosos (RF-02)
            GameObject suspectRoot = new GameObject("--- TABLERO DE SOSPECHOSOS (RF-02) ---");
            BuildSuspectBoard(suspectRoot.transform);

            // 7. Crear VR Player Rig y Cámaras
            GameObject playerRig = BuildVrPlayerRig();

            // 8. Crear HUD World Space, UIManager oficial, Pantalla Final y Panel Holográfico EndScenario
            GameObject uiRoot = new GameObject("--- INTERFACES DE USUARIO (HUD & FINAL) ---");
            BuildWorldSpaceHud(uiRoot.transform, playerRig);
            BuildOfficialUIManager(uiRoot.transform, playerRig);
            BuildFinalEvaluationScreen(uiRoot.transform);
            BuildEndScenarioHolographicPanel(uiRoot.transform, playerRig);
            BuildCreditsAndEthicsModal(uiRoot.transform, playerRig);

            // 9. Guardar escena en disco
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"{LogPrefix} <color=#38F28A><b>¡Escena creada y guardada exitosamente en: {ScenePath}!</b></color>");

            // 10. Registrar en EditorBuildSettings
            RegisterSceneInBuildSettings(ScenePath);

            if (showDialog)
            {
                EditorUtility.DisplayDialog("ANDES-SIM: Escena Generada",
                    "La escena oficial 'Scenario01_OficinaPagos.unity' ha sido construida con éxito.\n\n" +
                    "• Puestos 1, 2 y 3 configurados con monitores y teletransporte.\n" +
                    "• Inspector de phishing y Post-It interactivo listos en Puesto 1.\n" +
                    "• Consola SIEM y panel de defensas listos en Puesto 3.\n" +
                    "• Rig VR Google Cardboard configurado con simulador de teclado/mouse.",
                    "Excelente");
            }
        }

        private static void SetupLightingAndEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.88f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.65f, 0.68f, 0.72f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.35f, 0.38f);

            // Luz direccional suave cenital
            GameObject sunObj = new GameObject("Directional Light - Ambient");
            Light sunLight = sunObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(0.98f, 0.95f, 0.92f);
            sunLight.intensity = 0.85f;
            sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void BuildCoreManagers(GameObject parent)
        {
            // GameManager
            GameObject gmObj = new GameObject("GameManager");
            gmObj.transform.SetParent(parent.transform);
            GameManager gm = gmObj.AddComponent<GameManager>();
            gm.mode = SimMode.Ataque;

            // Cargar ScenarioData
            ScenarioData dataAsset = Resources.Load<ScenarioData>("Data/Scenario01_Data");
            if (dataAsset != null) gm.data = dataAsset;

            // SimulationClock
            GameObject clockObj = new GameObject("SimulationClock");
            clockObj.transform.SetParent(parent.transform);
            clockObj.AddComponent<SimulationClock>();

            // ScenarioFlowController
            GameObject flowObj = new GameObject("ScenarioFlowController");
            flowObj.transform.SetParent(parent.transform);
            flowObj.AddComponent<ScenarioFlowController>();

            // ProgressTracker
            GameObject trackerObj = new GameObject("ProgressTracker");
            trackerObj.transform.SetParent(parent.transform);
            trackerObj.AddComponent<ProgressTracker>();

            // AudioManager (Sintetizador Procedural de Audio Central)
            GameObject audioMgrObj = new GameObject("AudioManager");
            audioMgrObj.transform.SetParent(parent.transform);
            audioMgrObj.AddComponent<AudioManager>();

            // OfficeAudioEnvironment
            GameObject audioObj = new GameObject("OfficeAudioEnvironment");
            audioObj.transform.SetParent(parent.transform);
            audioObj.AddComponent<OfficeAudioEnvironment>();
        }

        private static void BuildOfficeEnvironment(GameObject parent)
        {
            // Arquitectura 3D Procedural Profesional (Vestíbulo SOC, Salón 10x8, Ventanal Nocturno y Letrero)
            AndesSim.Environment.ProceduralOfficeEnvironmentBuilder.BuildOfficeGeometry(parent.transform, false, false);

            // Portal de Entrada / Lobby (Paso 1)
            Material portalMat = CreateColorMaterial("Mat_PortalFrame", new Color(0.12f, 0.45f, 0.85f));
            BuildPortalEntrada(parent.transform, portalMat);

            // Efecto de Rejilla e Iluminación Cibernética Ambiental (Ventanal y Portal - Optimizado Mobile OpenGLES3)
            GameObject cyberGridObj = new GameObject("AmbientCyberGridEffect");
            cyberGridObj.transform.SetParent(parent.transform, false);
            cyberGridObj.AddComponent<AndesSim.Environment.AmbientCyberGridEffect>();
        }

        private static void CreateWall(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateCeilingLight(string name, Transform parent, Vector3 pos)
        {
            GameObject lightObj = new GameObject(name);
            lightObj.transform.SetParent(parent);
            lightObj.transform.position = pos;
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 7f;
            l.intensity = 1.2f;
            l.color = new Color(0.95f, 0.96f, 1f);
        }

        private static void BuildPortalEntrada(Transform parent, Material portalMat)
        {
            GameObject portalObj = new GameObject("Portal_Entrada");
            portalObj.transform.SetParent(parent);
            portalObj.transform.position = new Vector3(0f, 0f, -4.5f);
            portalObj.layer = LayerMask.NameToLayer("Interactive");

            // Colisionador de interacción por mirada
            BoxCollider col = portalObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.25f, 0f);
            col.size = new Vector3(2.6f, 2.5f, 0.4f);

            GazeInteractable gaze = portalObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = "Cruzar Portal: Entrar a la Oficina 2015";

            // Marco del portal (Arco 3D)
            GameObject postLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postLeft.name = "Marco_Izquierdo";
            postLeft.transform.SetParent(portalObj.transform);
            postLeft.transform.localPosition = new Vector3(-1.2f, 1.25f, 0f);
            postLeft.transform.localScale = new Vector3(0.2f, 2.5f, 0.2f);
            postLeft.GetComponent<Renderer>().sharedMaterial = portalMat;

            GameObject postRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postRight.name = "Marco_Derecho";
            postRight.transform.SetParent(portalObj.transform);
            postRight.transform.localPosition = new Vector3(1.2f, 1.25f, 0f);
            postRight.transform.localScale = new Vector3(0.2f, 2.5f, 0.2f);
            postRight.GetComponent<Renderer>().sharedMaterial = portalMat;

            GameObject topBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBeam.name = "Marco_Superior";
            topBeam.transform.SetParent(portalObj.transform);
            topBeam.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            topBeam.transform.localScale = new Vector3(2.6f, 0.2f, 0.2f);
            topBeam.GetComponent<Renderer>().sharedMaterial = portalMat;

            // Componente oficial PortalEntrada (VR)
            PortalEntrada portalScript = portalObj.AddComponent<PortalEntrada>();

            // Panel de Briefing en World Space frente al jugador en el vestíbulo
            GameObject canvasObj = new GameObject("Canvas_Briefing_Portal");
            canvasObj.transform.SetParent(portalObj.transform);
            canvasObj.transform.localPosition = new Vector3(0f, 1.4f, 0.25f);
            canvasObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250f, 215f);
            rect.localScale = new Vector3(0.008f, 0.008f, 0.008f);
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Título
            GameObject titleObj = new GameObject("Text_Titulo");
            titleObj.transform.SetParent(canvasObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 85f);
            titleRect.sizeDelta = new Vector2(240f, 25f);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "ANDES-SIM · SOC Banco Andino";
            titleTmp.fontSize = 13;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0f, 0.9f, 1f);

            // Resumen de misión
            GameObject briefingObj = new GameObject("Text_ResumenMision");
            briefingObj.transform.SetParent(canvasObj.transform, false);
            RectTransform briefingRect = briefingObj.AddComponent<RectTransform>();
            briefingRect.anchoredPosition = new Vector2(0f, 32f);
            briefingRect.sizeDelta = new Vector2(230f, 65f);
            TextMeshProUGUI briefingTmp = briefingObj.AddComponent<TextMeshProUGUI>();
            briefingTmp.text = "29 de enero de 2015: 16 empleados reciben el correo de Rasel Ahlam.\nObjetivo: detectar las 3 señales de phishing en menos de 3 minutos, recolectar las 3 evidencias y comparar Modo Ataque vs Modo Defensa.";
            briefingTmp.fontSize = 8.5f;
            briefingTmp.alignment = TextAlignmentOptions.Center;
            briefingTmp.color = Color.white;

            // Botón 1: Entrar a la oficina
            GameObject enterBtnObj = CreateWorldGazeButton("Btn_CruzarPortal", canvasObj.transform,
                new Vector2(0f, -32f), new Vector2(195f, 26f), "CRUZAR EL PORTAL (ENTRAR)");

            // Botón 2: Ver Créditos y Marco Ético-Legal
            GameObject creditsBtnObj = CreateWorldGazeButton("Btn_CreditosEtica", canvasObj.transform,
                new Vector2(0f, -68f), new Vector2(195f, 24f), "CRÉDITOS Y MARCO ÉTICO");

            // Punto de retorno en el vestíbulo
            GameObject lobbySpawn = new GameObject("SpawnPoint_Lobby");
            lobbySpawn.transform.SetParent(portalObj.transform);
            lobbySpawn.transform.position = new Vector3(0f, 0f, -4.0f);
            lobbySpawn.transform.rotation = Quaternion.identity;

            // Serializar referencias
            SerializedObject sPortal = new SerializedObject(portalScript);
            sPortal.FindProperty("portalTitleDisplay").objectReferenceValue = titleTmp;
            sPortal.FindProperty("briefingDisplay").objectReferenceValue = briefingTmp;
            sPortal.FindProperty("enterPortalButton").objectReferenceValue = enterBtnObj.GetComponent<Button>();
            sPortal.FindProperty("viewCreditsButton").objectReferenceValue = creditsBtnObj.GetComponent<Button>();
            sPortal.FindProperty("lobbySpawnPoint").objectReferenceValue = lobbySpawn.transform;
            sPortal.ApplyModifiedProperties();
        }

        private static void BuildWorkstations(Transform parent)
        {
            Material deskMat = CreateColorMaterial("Mat_DeskWood", new Color(0.45f, 0.35f, 0.28f));
            Material bezelMat = CreateColorMaterial("Mat_MonitorBezel", new Color(0.12f, 0.12f, 0.14f));

            // =========================================================================
            // PUESTO 1: Carla Mendoza (Operadora SWIFT)
            // =========================================================================
            GameObject ws1Obj = new GameObject("Puesto_01_CarlaMendoza");
            ws1Obj.transform.SetParent(parent);
            ws1Obj.transform.position = new Vector3(-3.0f, 0f, 2.0f);

            BuildDeskAndChair(ws1Obj.transform, deskMat);
            GameObject monitor01 = BuildMonitorDisplay(ws1Obj.transform, "Monitor_01", "Puesto 1: Carla Mendoza (SWIFT)", bezelMat);

            // Agregar PhishingInspector y MailQuarantine a Monitor 01 y las 3 zonas interactivas (PhishingSignalHotspot)
            PhishingInspector phishingInspector = monitor01.AddComponent<PhishingInspector>();
            MailQuarantine mailQuarantine = monitor01.AddComponent<MailQuarantine>();
            GameObject panelCorreo = BuildPhishingEmailPanel(monitor01, phishingInspector, mailQuarantine);

            // Post-It físico con credenciales expuestas
            GameObject postItObj = BuildPostItNote(ws1Obj.transform, new Vector3(0.35f, 0.78f, 0.15f));

            // Cajón interactivo de credenciales en el escritorio de Carla Mendoza (CredencialesDeskDrawer)
            BuildDeskDrawer(ws1Obj.transform, deskMat);

            // Teléfono IP interactivo para reportar al Anexo SOC (Telefono_Anexo_SOC)
            BuildSocTelephone(ws1Obj.transform);

            // Dispositivo Token MFA / Bóveda que reemplaza al Post-It en Modo Defensa (Token_MFA_Boveda)
            BuildTokenMfaBoveda(ws1Obj.transform, postItObj);

            // Punto de teletransporte para Puesto 1 (TeleportPoint_01)
            GameObject tp1 = CreateTeleportPoint(ws1Obj.transform, "TeleportPoint_01",
                new Vector3(0f, 0.01f, -1.2f), "Puesto 1: Carla Mendoza (Operadora SWIFT)");

            PortalEntrada portal = UnityEngine.Object.FindFirstObjectByType<PortalEntrada>();
            if (portal != null)
            {
                SerializedObject sPortal = new SerializedObject(portal);
                sPortal.FindProperty("destinationTeleportPoint").objectReferenceValue = tp1.GetComponent<TeleportPoint>();
                sPortal.ApplyModifiedProperties();
            }

            // =========================================================================
            // PUESTO 2: Analista de Comercio Exterior (Destinatario #7)
            // =========================================================================
            GameObject ws2Obj = new GameObject("Puesto_02_ComercioExterior");
            ws2Obj.transform.SetParent(parent);
            ws2Obj.transform.position = new Vector3(0f, 0f, 2.0f);

            BuildDeskAndChair(ws2Obj.transform, deskMat);
            GameObject monitor02 = BuildMonitorDisplay(ws2Obj.transform, "Monitor_02", "Puesto 2: Analista Comercio Exterior", bezelMat);

            CreateTeleportPoint(ws2Obj.transform, "TeleportPoint_02",
                new Vector3(0f, 0.01f, -1.2f), "Puesto 2: Analista de Comercio Exterior");

            // =========================================================================
            // PUESTO 3: Consola SIEM y Panel de Defensas
            // =========================================================================
            GameObject ws3Obj = new GameObject("Puesto_03_ConsolaDefensas");
            ws3Obj.transform.SetParent(parent);
            ws3Obj.transform.position = new Vector3(3.0f, 0f, 2.0f);

            BuildDeskAndChair(ws3Obj.transform, deskMat);
            GameObject monitor03 = BuildMonitorDisplay(ws3Obj.transform, "Monitor_03", "Puesto 3: Consola SIEM (Logs)", bezelMat);

            // Agregar DefenseTerminalController
            DefenseTerminalController defenseCtrl = monitor03.AddComponent<DefenseTerminalController>();

            // Agregar componente oficial AccessLogTerminal para Monitor_03
            AccessLogTerminal siemTerminal = monitor03.AddComponent<AccessLogTerminal>();

            // Agregar botón interactivo "Registrar Log como Evidencia Forense" en la pantalla del Monitor 03
            Transform canvasTransform = monitor03.transform.Find("Canvas_Pantalla");
            if (canvasTransform != null)
            {
                GameObject logBtnObj = CreateWorldGazeButton("Btn_RegistrarLogEvidencia", canvasTransform,
                    new Vector2(0f, -38f), new Vector2(145f, 18f), "REGISTRAR LOG COMO EVIDENCIA");

                SerializedObject sSiem = new SerializedObject(siemTerminal);
                sSiem.FindProperty("registerLogEvidenceButton").objectReferenceValue = logBtnObj.GetComponent<Button>();
                sSiem.FindProperty("registerButtonLabelText").objectReferenceValue = logBtnObj.GetComponentInChildren<TextMeshProUGUI>();
                sSiem.FindProperty("registerButtonBgImage").objectReferenceValue = logBtnObj.GetComponent<Image>();
                sSiem.ApplyModifiedProperties();
            }

            CreateTeleportPoint(ws3Obj.transform, "TeleportPoint_03",
                new Vector3(0f, 0.01f, -1.2f), "Puesto 3: Terminal de Registros y Panel de Defensa");

            // Construir los 3 interruptores físicos de contramedidas (DefenseControl RF-04)
            BuildDefenseSwitches(ws3Obj.transform);

            // Construir la Consola de Cuarentena y Sandbox en Monitor_01 sincronizada con los 3 puestos
            BuildQuarantineConsolePanel(monitor01, monitor02, monitor03, mailQuarantine, panelCorreo);

            // Componente cinemático oficial de propagación de infección en red (InfectionSpreadVisualizer)
            GameObject visualizerObj = new GameObject("Visualizador_Propagacion_Infeccion");
            visualizerObj.transform.SetParent(parent);
            visualizerObj.transform.position = Vector3.zero;

            Material laserMat = CreateColorMaterial("Mat_InfectionLaserBeam", new Color(1.0f, 0.22f, 0.20f, 1.0f));

            // 1. LineRenderer Desk1 -> Desk2
            GameObject lineObj1 = new GameObject("Line_RedPlana_Desk1_Desk2");
            lineObj1.transform.SetParent(visualizerObj.transform);
            LineRenderer lr1 = lineObj1.AddComponent<LineRenderer>();
            lr1.sharedMaterial = laserMat;
            lr1.startWidth = 0.045f;
            lr1.endWidth = 0.045f;
            lr1.useWorldSpace = true;
            lr1.positionCount = 16;
            lr1.startColor = new Color(1f, 0.35f, 0.31f, 0.95f);
            lr1.endColor = new Color(1f, 0.22f, 0.20f, 1.0f);
            lr1.enabled = false;

            // 2. LineRenderer Desk1 -> Desk3
            GameObject lineObj2 = new GameObject("Line_RedPlana_Desk1_Desk3");
            lineObj2.transform.SetParent(visualizerObj.transform);
            LineRenderer lr2 = lineObj2.AddComponent<LineRenderer>();
            lr2.sharedMaterial = laserMat;
            lr2.startWidth = 0.045f;
            lr2.endWidth = 0.045f;
            lr2.useWorldSpace = true;
            lr2.positionCount = 16;
            lr2.startColor = new Color(1f, 0.35f, 0.31f, 0.95f);
            lr2.endColor = new Color(1f, 0.22f, 0.20f, 1.0f);
            lr2.enabled = false;

            // 3. Letrero Holográfico Superior
            GameObject holoCanvasObj = new GameObject("Canvas_LetreroHolografico_Infeccion");
            holoCanvasObj.transform.SetParent(visualizerObj.transform);
            holoCanvasObj.transform.position = new Vector3(0f, 2.3f, 1.8f);
            holoCanvasObj.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

            Canvas holoCanvas = holoCanvasObj.AddComponent<Canvas>();
            holoCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform holoRect = holoCanvasObj.GetComponent<RectTransform>();
            holoRect.sizeDelta = new Vector2(500f, 140f);
            holoRect.localScale = new Vector3(0.005f, 0.005f, 0.005f);
            holoCanvasObj.AddComponent<CanvasScaler>();
            CanvasGroup holoCg = holoCanvasObj.AddComponent<CanvasGroup>();

            GameObject holoBgObj = new GameObject("Panel_Fondo");
            holoBgObj.transform.SetParent(holoCanvasObj.transform, false);
            RectTransform holoBgRect = holoBgObj.AddComponent<RectTransform>();
            holoBgRect.anchorMin = Vector2.zero;
            holoBgRect.anchorMax = Vector2.one;
            holoBgRect.sizeDelta = Vector2.zero;
            Image holoBgImg = holoBgObj.AddComponent<Image>();
            holoBgImg.color = new Color(0.07f, 0.02f, 0.02f, 0.94f);

            GameObject holoTitleObj = new GameObject("Txt_AlertaTitulo");
            holoTitleObj.transform.SetParent(holoBgObj.transform, false);
            RectTransform holoTitleRect = holoTitleObj.AddComponent<RectTransform>();
            holoTitleRect.anchoredPosition = new Vector2(0f, 42f);
            holoTitleRect.sizeDelta = new Vector2(470f, 32f);
            TextMeshProUGUI holoTitleTmp = holoTitleObj.AddComponent<TextMeshProUGUI>();
            holoTitleTmp.text = "<color=#FF5A4E><b>[ALERTA DE SEGURIDAD CRITICA - RED PLANA COMPROMETIDA]</b></color>";
            holoTitleTmp.fontSize = 17;
            holoTitleTmp.alignment = TextAlignmentOptions.Center;

            GameObject holoBodyObj = new GameObject("Txt_MensajeOficial");
            holoBodyObj.transform.SetParent(holoBgObj.transform, false);
            RectTransform holoBodyRect = holoBodyObj.AddComponent<RectTransform>();
            holoBodyRect.anchoredPosition = new Vector2(0f, -8f);
            holoBodyRect.sizeDelta = new Vector2(460f, 60f);
            TextMeshProUGUI holoBodyTmp = holoBodyObj.AddComponent<TextMeshProUGUI>();
            holoBodyTmp.text =
                "<b><color=#FFFFFF>29 ENE 2015: PUERTA TRASERA ABIERTA EN LA RED DEL BANCO ANDINO. El grupo Lazarus inicia 1 año de espionaje silencioso.</color></b>\n" +
                "<size=75%><color=#FFC66D>Topología plana sin segmentación interna facilitó la intrusión masiva.</color></size>";
            holoBodyTmp.fontSize = 15;
            holoBodyTmp.alignment = TextAlignmentOptions.Center;

            holoCanvasObj.SetActive(false);

            InfectionSpreadVisualizer visualizer = visualizerObj.AddComponent<InfectionSpreadVisualizer>();
            SerializedObject sVis = new SerializedObject(visualizer);
            sVis.FindProperty("monitor01Controller").objectReferenceValue = monitor01.GetComponent<InteractiveMonitorController>();
            sVis.FindProperty("monitor02Controller").objectReferenceValue = monitor02.GetComponent<InteractiveMonitorController>();
            sVis.FindProperty("monitor03Controller").objectReferenceValue = monitor03.GetComponent<InteractiveMonitorController>();
            sVis.FindProperty("monitor01Point").objectReferenceValue = monitor01.transform;
            sVis.FindProperty("monitor02Point").objectReferenceValue = monitor02.transform;
            sVis.FindProperty("monitor03Point").objectReferenceValue = monitor03.transform;
            sVis.FindProperty("lineDesk1ToDesk2").objectReferenceValue = lr1;
            sVis.FindProperty("lineDesk1ToDesk3").objectReferenceValue = lr2;
            sVis.FindProperty("energyLineMaterial").objectReferenceValue = laserMat;
            sVis.FindProperty("holographicSignPanel").objectReferenceValue = holoCanvasObj;
            sVis.FindProperty("holographicSignCanvasGroup").objectReferenceValue = holoCg;
            sVis.FindProperty("holographicSignTitle").objectReferenceValue = holoTitleTmp;
            sVis.FindProperty("holographicSignBody").objectReferenceValue = holoBodyTmp;
            sVis.FindProperty("holographicSignBorder").objectReferenceValue = holoBgImg;
            sVis.ApplyModifiedProperties();

            // Panel en la pared para regresar al vestíbulo / cambiar de modo
            BuildReturnToLobbyConsole(parent);
        }

        private static void BuildDeskAndChair(Transform parent, Material deskMat)
        {
            Material blackMetalMat = CreateColorMaterial("Mat_BlackMetalFrame", new Color(0.08f, 0.09f, 0.11f));
            Material chairMeshMat = CreateColorMaterial("Mat_ChairDarkMesh", new Color(0.10f, 0.12f, 0.14f));
            Material mugMat = CreateColorMaterial("Mat_CeramicMug", new Color(0.85f, 0.88f, 0.90f));
            Material coffeeMat = CreateColorMaterial("Mat_DarkCoffee", new Color(0.12f, 0.07f, 0.03f));

            // 1. Escritorio Principal
            GameObject deskTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskTop.name = "Escritorio_Tablero";
            deskTop.transform.SetParent(parent);
            deskTop.transform.localPosition = new Vector3(0f, 0.74f, 0f);
            deskTop.transform.localScale = new Vector3(1.60f, 0.04f, 0.80f);
            deskTop.GetComponent<Renderer>().sharedMaterial = deskMat;

            // Ala de retorno en L
            GameObject returnWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            returnWing.name = "Escritorio_Ala_L";
            returnWing.transform.SetParent(parent);
            returnWing.transform.localPosition = new Vector3(-0.95f, 0.74f, 0.15f);
            returnWing.transform.localScale = new Vector3(0.50f, 0.04f, 0.90f);
            returnWing.GetComponent<Renderer>().sharedMaterial = deskMat;

            // Patas de escritorio
            CreateDeskLeg(deskTop.transform, new Vector3(-0.72f, -0.36f, 0.32f), blackMetalMat);
            CreateDeskLeg(deskTop.transform, new Vector3(0.72f, -0.36f, 0.32f), blackMetalMat);
            CreateDeskLeg(deskTop.transform, new Vector3(-0.72f, -0.36f, -0.32f), blackMetalMat);
            CreateDeskLeg(deskTop.transform, new Vector3(0.72f, -0.36f, -0.32f), blackMetalMat);

            // 2. Gabinete de PC con LEDs de actividad
            GameObject pcTower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pcTower.name = "Gabinete_PC_Torre";
            pcTower.transform.SetParent(parent);
            pcTower.transform.localPosition = new Vector3(-0.55f, 0.26f, 0.25f);
            pcTower.transform.localScale = new Vector3(0.20f, 0.44f, 0.42f);
            pcTower.GetComponent<Renderer>().sharedMaterial = blackMetalMat;

            // LED verde de encendido
            GameObject ledPwr = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ledPwr.name = "LED_Power_Verde";
            ledPwr.transform.SetParent(pcTower.transform);
            ledPwr.transform.localPosition = new Vector3(-0.05f, 0.18f, -0.51f);
            ledPwr.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            Material ledGreenMat = CreateColorMaterial("Mat_LedGreen", new Color(0.22f, 0.95f, 0.54f));
            ledPwr.GetComponent<Renderer>().sharedMaterial = ledGreenMat;

            // 3. Teclado, alfombrilla y mouse
            GameObject kb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kb.name = "Teclado_Oficina";
            kb.transform.SetParent(parent);
            kb.transform.localPosition = new Vector3(0f, 0.77f, -0.15f);
            kb.transform.localScale = new Vector3(0.44f, 0.018f, 0.15f);
            kb.GetComponent<Renderer>().sharedMaterial = blackMetalMat;

            GameObject mousePad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mousePad.name = "Alfombrilla_Mouse";
            mousePad.transform.SetParent(parent);
            mousePad.transform.localPosition = new Vector3(0.32f, 0.763f, -0.15f);
            mousePad.transform.localScale = new Vector3(0.20f, 0.004f, 0.22f);
            mousePad.GetComponent<Renderer>().sharedMaterial = chairMeshMat;

            GameObject mouse = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mouse.name = "Mouse_Optico";
            mouse.transform.SetParent(parent);
            mouse.transform.localPosition = new Vector3(0.32f, 0.778f, -0.15f);
            mouse.transform.localScale = new Vector3(0.065f, 0.026f, 0.11f);
            mouse.GetComponent<Renderer>().sharedMaterial = blackMetalMat;

            // 4. Taza de café
            GameObject mug = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mug.name = "Taza_Cafe";
            mug.transform.SetParent(parent);
            mug.transform.localPosition = new Vector3(-0.36f, 0.80f, -0.05f);
            mug.transform.localScale = new Vector3(0.08f, 0.05f, 0.08f);
            mug.GetComponent<Renderer>().sharedMaterial = mugMat;

            GameObject coffee = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coffee.name = "Liquido_Cafe";
            coffee.transform.SetParent(mug.transform);
            coffee.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            coffee.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
            coffee.GetComponent<Renderer>().sharedMaterial = coffeeMat;

            // 5. Silla ergonómica de oficina
            GameObject chairSeat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chairSeat.name = "Silla_Asiento";
            chairSeat.transform.SetParent(parent);
            chairSeat.transform.localPosition = new Vector3(0f, 0.46f, -0.65f);
            chairSeat.transform.localScale = new Vector3(0.48f, 0.06f, 0.46f);
            chairSeat.GetComponent<Renderer>().sharedMaterial = chairMeshMat;

            GameObject chairBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chairBack.name = "Silla_Respaldo";
            chairBack.transform.SetParent(parent);
            chairBack.transform.localPosition = new Vector3(0f, 0.78f, -0.85f);
            chairBack.transform.localScale = new Vector3(0.44f, 0.55f, 0.04f);
            chairBack.GetComponent<Renderer>().sharedMaterial = chairMeshMat;

            // Base de pistón y patas
            GameObject chairPiston = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chairPiston.name = "Silla_Piston";
            chairPiston.transform.SetParent(parent);
            chairPiston.transform.localPosition = new Vector3(0f, 0.22f, -0.65f);
            chairPiston.transform.localScale = new Vector3(0.05f, 0.22f, 0.05f);
            chairPiston.GetComponent<Renderer>().sharedMaterial = blackMetalMat;
        }

        private static void CreateDeskLeg(Transform parent, Vector3 localPos, Material mat)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = "Pata";
            leg.transform.SetParent(parent);
            leg.transform.localPosition = localPos;
            leg.transform.localScale = new Vector3(0.04f, 0.37f, 0.04f);
            leg.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static GameObject BuildMonitorDisplay(Transform parent, string id, string label, Material bezelMat)
        {
            GameObject monitorObj = new GameObject(id);
            monitorObj.transform.SetParent(parent);
            monitorObj.transform.localPosition = new Vector3(0f, 0.8f, 0.1f);
            monitorObj.tag = "Monitor";
            monitorObj.layer = LayerMask.NameToLayer("Interactive");

            // Marco del monitor
            GameObject bezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bezel.name = "Marco_Monitor";
            bezel.transform.SetParent(monitorObj.transform);
            bezel.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            bezel.transform.localScale = new Vector3(0.9f, 0.55f, 0.06f);
            bezel.GetComponent<Renderer>().sharedMaterial = bezelMat;

            // Soporte del monitor
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.name = "Soporte_Monitor";
            stand.transform.SetParent(monitorObj.transform);
            stand.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            stand.transform.localScale = new Vector3(0.08f, 0.1f, 0.08f);
            stand.GetComponent<Renderer>().sharedMaterial = bezelMat;

            // Colisionador 3D para la mirada
            BoxCollider box = monitorObj.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.3f, 0f);
            box.size = new Vector3(0.95f, 0.6f, 0.1f);

            // GazeInteractable
            GazeInteractable gaze = monitorObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = label;

            // OfficeMonitorDisplay
            OfficeMonitorDisplay display = monitorObj.AddComponent<OfficeMonitorDisplay>();

            // Canvas en World Space sobre la pantalla
            GameObject canvasObj = new GameObject("Canvas_Pantalla");
            canvasObj.transform.SetParent(monitorObj.transform);
            canvasObj.transform.localPosition = new Vector3(0f, 0.3f, -0.035f);
            canvasObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 100f);
            rect.localScale = new Vector3(0.0055f, 0.0055f, 0.0055f);
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Serializar ID en display
            SerializedObject sDisplay = new SerializedObject(display);
            sDisplay.FindProperty("workstationId").stringValue = id;
            sDisplay.FindProperty("screenCanvas").objectReferenceValue = canvas;
            sDisplay.ApplyModifiedProperties();

            // Agregar InteractiveMonitorController oficial
            InteractiveMonitorController monitorCtrl = monitorObj.AddComponent<InteractiveMonitorController>();
            SerializedObject sCtrl = new SerializedObject(monitorCtrl);
            sCtrl.FindProperty("monitorId").stringValue = id;
            sCtrl.FindProperty("worldSpaceScreenCanvas").objectReferenceValue = canvas;
            sCtrl.ApplyModifiedProperties();

            return monitorObj;
        }

        private static GameObject BuildPostItNote(Transform parent, Vector3 localPos)
        {
            GameObject postItObj = new GameObject("PostIt_Credenciales");
            postItObj.transform.SetParent(parent);
            postItObj.transform.localPosition = localPos;
            postItObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            postItObj.tag = "Evidence";
            postItObj.layer = LayerMask.NameToLayer("Interactive");

            // 1. Malla cuadrada del papel adhesivo (Visual Root Modo Ataque)
            GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Quad);
            paper.name = "Papel_Amarillo_Root";
            paper.transform.SetParent(postItObj.transform);
            paper.transform.localPosition = Vector3.zero;
            paper.transform.localRotation = Quaternion.identity;
            paper.transform.localScale = new Vector3(0.14f, 0.14f, 1f);

            Material postItMat = CreateColorMaterial("Mat_PostItYellow", new Color(1f, 0.95f, 0.3f));
            paper.GetComponent<Renderer>().sharedMaterial = postItMat;

            // Texto manuscrito 3D sobre el Post-It
            GameObject textNoteObj = new GameObject("Text_Manuscrito");
            textNoteObj.transform.SetParent(paper.transform);
            textNoteObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            textNoteObj.transform.localRotation = Quaternion.identity;
            TextMeshPro tmpNote = textNoteObj.AddComponent<TextMeshPro>();
            tmpNote.text = "Usuario SWIFT: cmendoza_op\nClave: Andino2015*Swift\n(No olvidar)";
            tmpNote.fontSize = 1.1f;
            tmpNote.alignment = TextAlignmentOptions.Center;
            tmpNote.color = new Color(0.1f, 0.1f, 0.15f);

            // 2. Dispositivo Token MFA Hardware (Visual Root Modo Defensa)
            GameObject mfaTokenObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mfaTokenObj.name = "Token_MFA_Hardware_Root";
            mfaTokenObj.transform.SetParent(postItObj.transform);
            mfaTokenObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            mfaTokenObj.transform.localRotation = Quaternion.identity;
            mfaTokenObj.transform.localScale = new Vector3(0.12f, 0.06f, 0.02f);

            Material tokenMat = CreateColorMaterial("Mat_TokenHardware", new Color(0.15f, 0.16f, 0.18f));
            mfaTokenObj.GetComponent<Renderer>().sharedMaterial = tokenMat;

            // Pantalla LCD del Token
            GameObject screenObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenObj.name = "Pantalla_LCD";
            screenObj.transform.SetParent(mfaTokenObj.transform);
            screenObj.transform.localPosition = new Vector3(0f, 0.1f, -0.55f);
            screenObj.transform.localScale = new Vector3(0.85f, 0.45f, 1f);
            Material lcdMat = CreateColorMaterial("Mat_TokenLCD", new Color(0.05f, 0.25f, 0.15f));
            screenObj.GetComponent<Renderer>().sharedMaterial = lcdMat;

            GameObject codeObj = new GameObject("Text_OTPCode");
            codeObj.transform.SetParent(screenObj.transform);
            codeObj.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            TextMeshPro codeTmp = codeObj.AddComponent<TextMeshPro>();
            codeTmp.text = "682 914";
            codeTmp.fontSize = 2.4f;
            codeTmp.fontStyle = FontStyles.Bold;
            codeTmp.alignment = TextAlignmentOptions.Center;
            codeTmp.color = new Color(0.2f, 1f, 0.4f); // Verde LCD

            mfaTokenObj.SetActive(false); // Inicialmente oculto hasta activar Defensa_MFA

            // Colisionador para interacción por mirada
            BoxCollider col = postItObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.18f, 0.18f, 0.05f);

            GazeInteractable gaze = postItObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = "Examinar Post-It con Contraseñas Expuestas";

            // Componente oficial PostItCredenciales (hereda de EvidenceItem)
            PostItCredenciales postItScript = postItObj.AddComponent<PostItCredenciales>();

            SerializedObject sPostIt = new SerializedObject(postItScript);
            sPostIt.FindProperty("postItVisualRoot").objectReferenceValue = paper;
            sPostIt.FindProperty("postItNote3DText").objectReferenceValue = tmpNote;
            sPostIt.FindProperty("hardwareMfaTokenRoot").objectReferenceValue = mfaTokenObj;
            sPostIt.FindProperty("mfaTokenCodeText").objectReferenceValue = codeTmp;
            sPostIt.FindProperty("visualIndicatorRenderer").objectReferenceValue = paper.GetComponent<Renderer>();
            sPostIt.ApplyModifiedProperties();

            return postItObj;
        }

        private static void BuildDeskDrawer(Transform parent, Material deskMat)
        {
            // Estructura exterior fija del mueble del cajón
            GameObject drawerFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drawerFrame.name = "Mueble_Cajon_Estructura";
            drawerFrame.transform.SetParent(parent, false);
            drawerFrame.transform.localPosition = new Vector3(0.48f, 0.58f, 0.05f);
            drawerFrame.transform.localScale = new Vector3(0.42f, 0.22f, 0.55f);
            drawerFrame.GetComponent<Renderer>().sharedMaterial = deskMat;

            // Cajón que se desplaza hacia adelante
            GameObject drawerMoving = new GameObject("Cajon_Credenciales_Drawer");
            drawerMoving.transform.SetParent(drawerFrame.transform, false);
            drawerMoving.transform.localPosition = Vector3.zero;
            drawerMoving.layer = LayerMask.NameToLayer("Interactive");

            // Tapa frontal del cajón
            GameObject frontPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontPanel.name = "Frontal_Cajon";
            frontPanel.transform.SetParent(drawerMoving.transform, false);
            frontPanel.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            frontPanel.transform.localScale = new Vector3(0.98f, 0.95f, 0.05f);
            frontPanel.GetComponent<Renderer>().sharedMaterial = deskMat;

            // Tirador metálico del cajón
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Tirador_Cajon";
            handle.transform.SetParent(frontPanel.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            handle.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);
            Material metalMat = CreateColorMaterial("Mat_MetalHandle", new Color(0.8f, 0.82f, 0.85f));
            handle.GetComponent<Renderer>().sharedMaterial = metalMat;

            // Cuaderno de contingencia dentro del cajón
            GameObject notebookObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            notebookObj.name = "Cuaderno_Contingencia";
            notebookObj.transform.SetParent(drawerMoving.transform, false);
            notebookObj.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            notebookObj.transform.localScale = new Vector3(0.65f, 0.08f, 0.65f);
            Material notebookMat = CreateColorMaterial("Mat_NotebookLeather", new Color(0.12f, 0.2f, 0.38f));
            notebookObj.GetComponent<Renderer>().sharedMaterial = notebookMat;

            // Página con credenciales manuscritas
            GameObject pageObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pageObj.name = "Pagina_Manuscrita";
            pageObj.transform.SetParent(notebookObj.transform, false);
            pageObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            pageObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pageObj.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            Material paperMat = CreateColorMaterial("Mat_NotebookPaper", new Color(0.96f, 0.95f, 0.9f));
            pageObj.GetComponent<Renderer>().sharedMaterial = paperMat;

            GameObject pageTextObj = new GameObject("Text_Credenciales");
            pageTextObj.transform.SetParent(pageObj.transform, false);
            pageTextObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            TextMeshPro notebookTmp = pageTextObj.AddComponent<TextMeshPro>();
            notebookTmp.text = "OPERACIONES SWIFT - CONTINGENCIA\nUsuario: cmendoza_op\nClave: Andino2015*Swift\nPIN: 9481\n(No retirar del escritorio)";
            notebookTmp.fontSize = 0.9f;
            notebookTmp.alignment = TextAlignmentOptions.Center;
            notebookTmp.color = new Color(0.1f, 0.12f, 0.18f);

            // Cerrojo digital en Modo Defensa (inicialmente oculto)
            GameObject lockBadge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lockBadge.name = "Insignia_CerrojoDigital";
            lockBadge.transform.SetParent(frontPanel.transform, false);
            lockBadge.transform.localPosition = new Vector3(0f, 0.3f, -0.55f);
            lockBadge.transform.localScale = new Vector3(0.2f, 0.1f, 0.03f);
            Material lockMat = CreateColorMaterial("Mat_DigitalLock", new Color(0.22f, 0.95f, 0.54f));
            lockBadge.GetComponent<Renderer>().sharedMaterial = lockMat;
            lockBadge.SetActive(false);

            // Colisionador para interacción con la mirada
            BoxCollider col = drawerFrame.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, -0.1f);
            col.size = new Vector3(0.45f, 0.25f, 0.6f);

            GazeInteractable gaze = drawerFrame.AddComponent<GazeInteractable>();
            gaze.interactableLabel = "Abrir Cajón del Escritorio (Credenciales)";

            // Panel Flotante Forense 3D (World Space)
            GameObject popupCanvasObj = new GameObject("Canvas_ForensicPopup");
            popupCanvasObj.transform.SetParent(drawerFrame.transform, false);
            popupCanvasObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            popupCanvasObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Canvas canvas = popupCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform pRect = popupCanvasObj.GetComponent<RectTransform>();
            pRect.sizeDelta = new Vector2(220f, 90f);
            pRect.localScale = new Vector3(0.003f, 0.003f, 0.003f);
            popupCanvasObj.AddComponent<CanvasScaler>();

            Image pBg = popupCanvasObj.AddComponent<Image>();
            pBg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

            GameObject pTitleObj = new GameObject("Txt_Title");
            pTitleObj.transform.SetParent(popupCanvasObj.transform, false);
            RectTransform pTitleRect = pTitleObj.AddComponent<RectTransform>();
            pTitleRect.anchoredPosition = new Vector2(0f, 30f);
            pTitleRect.sizeDelta = new Vector2(210f, 18f);
            TextMeshProUGUI pTitleTmp = pTitleObj.AddComponent<TextMeshProUGUI>();
            pTitleTmp.text = "<b><color=#FF9F43>VULNERABILIDAD FÍSICA: LIBRETA DE CREDENCIALES</color></b>";
            pTitleTmp.fontSize = 8.5f;
            pTitleTmp.alignment = TextAlignmentOptions.Center;

            GameObject pDetailObj = new GameObject("Txt_Detail");
            pDetailObj.transform.SetParent(popupCanvasObj.transform, false);
            RectTransform pDetailRect = pDetailObj.AddComponent<RectTransform>();
            pDetailRect.anchoredPosition = new Vector2(0f, -10f);
            pDetailRect.sizeDelta = new Vector2(210f, 54f);
            TextMeshProUGUI pDetailTmp = pDetailObj.AddComponent<TextMeshProUGUI>();
            pDetailTmp.text = "Libreta con credenciales SWIFT en texto claro dentro de cajón sin llave. Facilitó la intrusión interna.";
            pDetailTmp.fontSize = 7.5f;
            pDetailTmp.alignment = TextAlignmentOptions.Center;
            popupCanvasObj.SetActive(false);

            // Componente CredencialesDeskDrawer
            CredencialesDeskDrawer drawerScript = drawerFrame.AddComponent<CredencialesDeskDrawer>();
            SerializedObject sDrawer = new SerializedObject(drawerScript);
            sDrawer.FindProperty("drawerMovingRoot").objectReferenceValue = drawerMoving.transform;
            sDrawer.FindProperty("contingencyNotebookRoot").objectReferenceValue = notebookObj;
            sDrawer.FindProperty("notebookCredentials3DText").objectReferenceValue = notebookTmp;
            sDrawer.FindProperty("defenseLockBadgeRoot").objectReferenceValue = lockBadge;
            sDrawer.FindProperty("forensicPopupPanel").objectReferenceValue = popupCanvasObj;
            sDrawer.FindProperty("popupTitleText").objectReferenceValue = pTitleTmp;
            sDrawer.FindProperty("popupDetailText").objectReferenceValue = pDetailTmp;
            sDrawer.ApplyModifiedProperties();
        }

        private static void BuildSocTelephone(Transform parent)
        {
            GameObject phoneObj = new GameObject("Telefono_Anexo_SOC");
            phoneObj.transform.SetParent(parent, false);
            phoneObj.transform.localPosition = new Vector3(-0.52f, 0.78f, 0.05f);
            phoneObj.tag = "Interactive";
            phoneObj.layer = LayerMask.NameToLayer("Interactive");

            // Base del teléfono
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "Base_Telefono";
            baseObj.transform.SetParent(phoneObj.transform, false);
            baseObj.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            baseObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            baseObj.transform.localScale = new Vector3(0.22f, 0.06f, 0.20f);
            Material phoneMat = CreateColorMaterial("Mat_PhoneDark", new Color(0.12f, 0.13f, 0.16f));
            baseObj.GetComponent<Renderer>().sharedMaterial = phoneMat;

            // Auricular
            GameObject handset = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handset.name = "Auricular_Telefono";
            handset.transform.SetParent(baseObj.transform, false);
            handset.transform.localPosition = new Vector3(-0.065f, 0.6f, 0f);
            handset.transform.localScale = new Vector3(0.25f, 0.4f, 1.05f);
            Material handsetMat = CreateColorMaterial("Mat_HandsetDark", new Color(0.08f, 0.09f, 0.11f));
            handset.GetComponent<Renderer>().sharedMaterial = handsetMat;

            // Pantalla LCD
            GameObject lcdObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            lcdObj.name = "Pantalla_LCD";
            lcdObj.transform.SetParent(baseObj.transform, false);
            lcdObj.transform.localPosition = new Vector3(0.035f, 0.52f, 0.25f);
            lcdObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lcdObj.transform.localScale = new Vector3(0.5f, 0.35f, 1f);
            Material lcdMat = CreateColorMaterial("Mat_PhoneLcd", new Color(0.08f, 0.22f, 0.32f));
            lcdObj.GetComponent<Renderer>().sharedMaterial = lcdMat;

            GameObject lcdTextObj = new GameObject("Text_Lcd");
            lcdTextObj.transform.SetParent(lcdObj.transform, false);
            lcdTextObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            TextMeshPro lcdTmp = lcdTextObj.AddComponent<TextMeshPro>();
            lcdTmp.text = "ANEXO 2401 · LISTO";
            lcdTmp.fontSize = 1.3f;
            lcdTmp.alignment = TextAlignmentOptions.Center;
            lcdTmp.color = new Color(0.3f, 0.85f, 1f);

            // LED indicador de llamada
            GameObject ledObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ledObj.name = "Led_Llamada";
            ledObj.transform.SetParent(baseObj.transform, false);
            ledObj.transform.localPosition = new Vector3(0.08f, 0.53f, -0.3f);
            ledObj.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            Material ledMat = CreateColorMaterial("Mat_PhoneLed", new Color(0.2f, 0.7f, 1f));
            ledObj.GetComponent<Renderer>().sharedMaterial = ledMat;

            Light ledLight = ledObj.AddComponent<Light>();
            ledLight.type = LightType.Point;
            ledLight.color = new Color(0.2f, 0.7f, 1f);
            ledLight.range = 0.4f;
            ledLight.intensity = 0.5f;

            // Colisionador para interacción con la mirada
            BoxCollider col = phoneObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.06f, 0f);
            col.size = new Vector3(0.25f, 0.16f, 0.24f);

            GazeInteractable gaze = phoneObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = "Llamar al Anexo SOC (Ext. 4040) · Reportar Amenaza";

            // Panel Flotante 3D de Diálogo y Subtítulos (World Space)
            GameObject dialogCanvasObj = new GameObject("Canvas_DialogoSubtitulos");
            dialogCanvasObj.transform.SetParent(phoneObj.transform, false);
            dialogCanvasObj.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            dialogCanvasObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Canvas dCanvas = dialogCanvasObj.AddComponent<Canvas>();
            dCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform dRect = dialogCanvasObj.GetComponent<RectTransform>();
            dRect.sizeDelta = new Vector2(280f, 105f);
            dRect.localScale = new Vector3(0.003f, 0.003f, 0.003f);
            dialogCanvasObj.AddComponent<CanvasScaler>();

            Image dBg = dialogCanvasObj.AddComponent<Image>();
            dBg.color = new Color(0.04f, 0.08f, 0.14f, 0.96f);

            GameObject dHeaderObj = new GameObject("Txt_Header");
            dHeaderObj.transform.SetParent(dialogCanvasObj.transform, false);
            RectTransform dHeaderRect = dHeaderObj.AddComponent<RectTransform>();
            dHeaderRect.anchoredPosition = new Vector2(0f, 38f);
            dHeaderRect.sizeDelta = new Vector2(270f, 16f);
            TextMeshProUGUI dHeaderTmp = dHeaderObj.AddComponent<TextMeshProUGUI>();
            dHeaderTmp.text = "<b><color=#4EC9B0>[SOC] CANAL DE EMERGENCIA TI · ANEXO 4040</color></b>";
            dHeaderTmp.fontSize = 8.5f;
            dHeaderTmp.alignment = TextAlignmentOptions.Center;

            GameObject dSpeakerObj = new GameObject("Txt_Speaker");
            dSpeakerObj.transform.SetParent(dialogCanvasObj.transform, false);
            RectTransform dSpeakerRect = dSpeakerObj.AddComponent<RectTransform>();
            dSpeakerRect.anchoredPosition = new Vector2(0f, 22f);
            dSpeakerRect.sizeDelta = new Vector2(270f, 14f);
            TextMeshProUGUI dSpeakerTmp = dSpeakerObj.AddComponent<TextMeshProUGUI>();
            dSpeakerTmp.text = "<b>Carla Mendoza (Ext. 2401):</b>";
            dSpeakerTmp.fontSize = 7.8f;
            dSpeakerTmp.alignment = TextAlignmentOptions.Center;

            GameObject dBodyObj = new GameObject("Txt_Subtitle");
            dBodyObj.transform.SetParent(dialogCanvasObj.transform, false);
            RectTransform dBodyRect = dBodyObj.AddComponent<RectTransform>();
            dBodyRect.anchoredPosition = new Vector2(0f, -12f);
            dBodyRect.sizeDelta = new Vector2(270f, 52f);
            TextMeshProUGUI dBodyTmp = dBodyObj.AddComponent<TextMeshProUGUI>();
            dBodyTmp.text = "Reportando correo malicioso recibido en bandeja de entrada.";
            dBodyTmp.fontSize = 7.0f;
            dBodyTmp.alignment = TextAlignmentOptions.Center;
            dialogCanvasObj.SetActive(false);

            // Componente TelefonoAnexoSoc
            TelefonoAnexoSoc phoneScript = phoneObj.AddComponent<TelefonoAnexoSoc>();
            SerializedObject sPhone = new SerializedObject(phoneScript);
            sPhone.FindProperty("callLedRenderer").objectReferenceValue = ledObj.GetComponent<Renderer>();
            sPhone.FindProperty("callIndicatorLight").objectReferenceValue = ledLight;
            sPhone.FindProperty("lcdScreenText").objectReferenceValue = lcdTmp;
            sPhone.FindProperty("dialogCanvasPanel").objectReferenceValue = dialogCanvasObj;
            sPhone.FindProperty("dialogHeaderTitleText").objectReferenceValue = dHeaderTmp;
            sPhone.FindProperty("speakerNameText").objectReferenceValue = dSpeakerTmp;
            sPhone.FindProperty("subtitleContentText").objectReferenceValue = dBodyTmp;
            sPhone.FindProperty("dialogStatusBadge").objectReferenceValue = dBg;
            sPhone.ApplyModifiedProperties();
        }

        private static void BuildTokenMfaBoveda(Transform parent, GameObject postItObj)
        {
            GameObject tokenObj = new GameObject("Token_MFA_Boveda");
            tokenObj.transform.SetParent(parent, false);
            tokenObj.transform.localPosition = new Vector3(0.20f, 0.78f, 0.12f);
            tokenObj.tag = "Interactive";
            tokenObj.layer = LayerMask.NameToLayer("Interactive");

            // Chasis del dispositivo token
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Carcasa_Token";
            body.transform.SetParent(tokenObj.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            body.transform.localScale = new Vector3(0.13f, 0.024f, 0.07f);
            Material tokenMat = CreateColorMaterial("Mat_TokenDarkCasing", new Color(0.12f, 0.14f, 0.18f));
            body.GetComponent<Renderer>().sharedMaterial = tokenMat;

            // Anillo metálico / llavero
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Anillo_Llavero";
            ring.transform.SetParent(body.transform, false);
            ring.transform.localPosition = new Vector3(0.55f, 0f, 0f);
            ring.transform.localScale = new Vector3(0.2f, 0.1f, 0.35f);
            Material metalMat = CreateColorMaterial("Mat_TokenMetalRing", new Color(0.75f, 0.8f, 0.85f));
            ring.GetComponent<Renderer>().sharedMaterial = metalMat;

            // Pantalla LCD del Token
            GameObject lcd = GameObject.CreatePrimitive(PrimitiveType.Quad);
            lcd.name = "Pantalla_LCD";
            lcd.transform.SetParent(body.transform, false);
            lcd.transform.localPosition = new Vector3(-0.05f, 0.52f, 0f);
            lcd.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lcd.transform.localScale = new Vector3(0.65f, 0.75f, 1f);
            Material lcdMat = CreateColorMaterial("Mat_TokenLcdGreen", new Color(0.04f, 0.20f, 0.10f));
            lcd.GetComponent<Renderer>().sharedMaterial = lcdMat;

            GameObject codeTextObj = new GameObject("Text_OTPCode");
            codeTextObj.transform.SetParent(lcd.transform, false);
            codeTextObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            TextMeshPro codeTmp = codeTextObj.AddComponent<TextMeshPro>();
            codeTmp.text = "739 204";
            codeTmp.fontSize = 2.0f;
            codeTmp.fontStyle = FontStyles.Bold;
            codeTmp.alignment = TextAlignmentOptions.Center;
            codeTmp.color = new Color(0.22f, 0.95f, 0.54f);

            GameObject labelTextObj = new GameObject("Text_ServiceLabel");
            labelTextObj.transform.SetParent(lcd.transform, false);
            labelTextObj.transform.localPosition = new Vector3(0f, 0.32f, -0.01f);
            TextMeshPro labelTmp = labelTextObj.AddComponent<TextMeshPro>();
            labelTmp.text = "BANCO ANDINO · SWIFT";
            labelTmp.fontSize = 0.65f;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = new Color(0.4f, 0.85f, 0.6f);

            // LED indicador de sincronización
            GameObject ledObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ledObj.name = "Led_Sync";
            ledObj.transform.SetParent(body.transform, false);
            ledObj.transform.localPosition = new Vector3(0.38f, 0.52f, 0f);
            ledObj.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            Material ledMat = CreateColorMaterial("Mat_TokenLedGreen", new Color(0.22f, 0.95f, 0.54f));
            ledObj.GetComponent<Renderer>().sharedMaterial = ledMat;

            Light ledLight = ledObj.AddComponent<Light>();
            ledLight.type = LightType.Point;
            ledLight.color = new Color(0.22f, 0.95f, 0.54f);
            ledLight.range = 0.25f;
            ledLight.intensity = 0.4f;

            // Colisionador para interacción con la mirada
            BoxCollider col = tokenObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.015f, 0f);
            col.size = new Vector3(0.16f, 0.05f, 0.10f);

            GazeInteractable gaze = tokenObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = "Token MFA Hardware (Código: 739204)";

            // Panel Flotante 3D de Explicación Forense (World Space)
            GameObject popupCanvasObj = new GameObject("Canvas_MfaExplicacion");
            popupCanvasObj.transform.SetParent(tokenObj.transform, false);
            popupCanvasObj.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            popupCanvasObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            Canvas pCanvas = popupCanvasObj.AddComponent<Canvas>();
            pCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform pRect = popupCanvasObj.GetComponent<RectTransform>();
            pRect.sizeDelta = new Vector2(230f, 90f);
            pRect.localScale = new Vector3(0.003f, 0.003f, 0.003f);
            popupCanvasObj.AddComponent<CanvasScaler>();

            Image pBg = popupCanvasObj.AddComponent<Image>();
            pBg.color = new Color(0.05f, 0.10f, 0.08f, 0.96f);

            GameObject pTitleObj = new GameObject("Txt_Title");
            pTitleObj.transform.SetParent(popupCanvasObj.transform, false);
            RectTransform pTitleRect = pTitleObj.AddComponent<RectTransform>();
            pTitleRect.anchoredPosition = new Vector2(0f, 30f);
            pTitleRect.sizeDelta = new Vector2(220f, 18f);
            TextMeshProUGUI pTitleTmp = pTitleObj.AddComponent<TextMeshProUGUI>();
            pTitleTmp.text = "<b><color=#38F28A>BÓVEDA DE CREDENCIALES & TOKEN MFA (RFC 6238)</color></b>";
            pTitleTmp.fontSize = 8.0f;
            pTitleTmp.alignment = TextAlignmentOptions.Center;

            GameObject pDetailObj = new GameObject("Txt_Detail");
            pDetailObj.transform.SetParent(popupCanvasObj.transform, false);
            RectTransform pDetailRect = pDetailObj.AddComponent<RectTransform>();
            pDetailRect.anchoredPosition = new Vector2(0f, -10f);
            pDetailRect.sizeDelta = new Vector2(220f, 54f);
            TextMeshProUGUI pDetailTmp = pDetailObj.AddComponent<TextMeshProUGUI>();
            pDetailTmp.text = "Mitigación: Doble factor rotativo cada 30 segundos. Neutraliza contraseñas robadas en texto plano.";
            pDetailTmp.fontSize = 7.2f;
            pDetailTmp.alignment = TextAlignmentOptions.Center;
            popupCanvasObj.SetActive(false);

            // Componente TokenMfaBoveda
            TokenMfaBoveda tokenScript = tokenObj.AddComponent<TokenMfaBoveda>();
            SerializedObject sToken = new SerializedObject(tokenScript);
            sToken.FindProperty("tokenHardwareMeshRoot").objectReferenceValue = body;
            sToken.FindProperty("otpCodeDisplayText").objectReferenceValue = codeTmp;
            sToken.FindProperty("serviceLabelText").objectReferenceValue = labelTmp;
            sToken.FindProperty("ledIndicatorRenderer").objectReferenceValue = ledObj.GetComponent<Renderer>();
            sToken.FindProperty("syncStatusLight").objectReferenceValue = ledLight;
            sToken.FindProperty("explanationPopupPanel").objectReferenceValue = popupCanvasObj;
            sToken.FindProperty("popupTitleText").objectReferenceValue = pTitleTmp;
            sToken.FindProperty("popupDetailText").objectReferenceValue = pDetailTmp;
            sToken.FindProperty("postItPhysicalToReplace").objectReferenceValue = postItObj;
            sToken.ApplyModifiedProperties();
        }

        private static GameObject CreateTeleportPoint(Transform parent, string name, Vector3 localPos, string label)
        {
            GameObject tpObj = new GameObject(name);
            tpObj.transform.SetParent(parent);
            tpObj.transform.localPosition = localPos;
            tpObj.layer = LayerMask.NameToLayer("Interactive");

            // Aro de piso visual
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Aro_Verde_Piso";
            ring.transform.SetParent(tpObj.transform);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(0.7f, 0.005f, 0.7f);

            Material ringMat = CreateColorMaterial("Mat_TeleportRing", new Color(0.22f, 0.95f, 0.54f, 0.6f));
            ring.GetComponent<Renderer>().sharedMaterial = ringMat;

            // Colisionador
            BoxCollider col = tpObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.8f, 0.1f, 0.8f);

            tpObj.AddComponent<GazeInteractable>();
            TeleportPoint tp = tpObj.AddComponent<TeleportPoint>();

            SerializedObject sTp = new SerializedObject(tp);
            sTp.FindProperty("pointName").stringValue = label;
            sTp.FindProperty("standPosition").objectReferenceValue = tpObj.transform;
            sTp.ApplyModifiedProperties();

            return tpObj;
        }

        private static void BuildReturnToLobbyConsole(Transform parent)
        {
            GameObject returnPanelObj = new GameObject("Panel_RetornoVestibulo");
            returnPanelObj.transform.SetParent(parent);
            returnPanelObj.transform.position = new Vector3(-6.85f, 1.4f, 0f);
            returnPanelObj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            GameObject canvasObj = new GameObject("Canvas_Retorno");
            canvasObj.transform.SetParent(returnPanelObj.transform);
            canvasObj.transform.localPosition = Vector3.zero;
            canvasObj.transform.localRotation = Quaternion.identity;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 100f);
            rect.localScale = new Vector3(0.006f, 0.006f, 0.006f);
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject btn = CreateWorldGazeButton("Btn_RegresarVestibulo", canvasObj.transform,
                Vector2.zero, new Vector2(190f, 44f), "REGRESAR AL VESTÍBULO\n(CAMBIAR MODO)");

            PortalEntrada portal = UnityEngine.Object.FindFirstObjectByType<PortalEntrada>();
            if (portal != null)
            {
                btn.GetComponent<Button>().onClick.AddListener(portal.ReturnToLobby);
            }
        }

        private static void BuildDefenseSwitches(Transform parent)
        {
            GameObject consoleObj = new GameObject("Panel_Contramedidas_RF04");
            consoleObj.transform.SetParent(parent);
            consoleObj.transform.localPosition = new Vector3(-0.65f, 0.88f, 0.1f);
            consoleObj.transform.localRotation = Quaternion.Euler(-15f, 15f, 0f);

            // Malla de la caja del panel
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Caja_Rack_Defensas";
            box.transform.SetParent(consoleObj.transform);
            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = new Vector3(0.52f, 0.28f, 0.08f);
            Material boxMat = CreateColorMaterial("Mat_DefenseRack", new Color(0.18f, 0.2f, 0.24f));
            box.GetComponent<Renderer>().sharedMaterial = boxMat;

            // Canvas en World Space
            GameObject canvasObj = new GameObject("Canvas_Switches");
            canvasObj.transform.SetParent(consoleObj.transform);
            canvasObj.transform.localPosition = new Vector3(0f, 0f, -0.045f);
            canvasObj.transform.localRotation = Quaternion.identity;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250f, 130f);
            rect.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // 1. Switch Defensa_Sandbox
            CreateDefenseSwitchItem(consoleObj.transform, canvasObj.transform,
                "Switch_Sandbox", "Defensa_Sandbox", "1. Sandbox Perimetral", new Vector2(0f, 38f), new Vector3(-0.18f, 0.075f, -0.045f));

            // 2. Switch Defensa_BotonReportar
            CreateDefenseSwitchItem(consoleObj.transform, canvasObj.transform,
                "Switch_BotonReportar", "Defensa_BotonReportar", "2. Botón Reportar", new Vector2(0f, 0f), new Vector3(-0.18f, 0f, -0.045f));

            // 3. Switch Defensa_MFA
            CreateDefenseSwitchItem(consoleObj.transform, canvasObj.transform,
                "Switch_MFA", "Defensa_MFA", "3. Doble Factor (MFA)", new Vector2(0f, -38f), new Vector3(-0.18f, -0.075f, -0.045f));
        }

        private static void CreateDefenseSwitchItem(Transform parent3D, Transform canvasParent,
            string objName, string defenseId, string label, Vector2 uiPos, Vector3 ledLocalPos)
        {
            GameObject switchObj = new GameObject(objName);
            switchObj.transform.SetParent(parent3D);
            switchObj.transform.localPosition = ledLocalPos;
            switchObj.layer = LayerMask.NameToLayer("Interactive");
            switchObj.tag = "DefenseControl";

            // Foco / LED 3D
            GameObject led = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            led.name = "LED_Indicador";
            led.transform.SetParent(switchObj.transform);
            led.transform.localPosition = Vector3.zero;
            led.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            led.transform.localScale = new Vector3(0.025f, 0.008f, 0.025f);

            Material ledMat = CreateColorMaterial("Mat_LED_" + defenseId, new Color(0.9f, 0.2f, 0.2f));
            led.GetComponent<Renderer>().sharedMaterial = ledMat;

            BoxCollider col = switchObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.08f, 0.05f, 0.04f);

            switchObj.AddComponent<GazeInteractable>();
            DefenseControl defCtrl = switchObj.AddComponent<DefenseControl>();

            // Botón interactivo de UI en el canvas
            GameObject btnObj = CreateWorldGazeButton("Btn_" + defenseId, canvasParent, uiPos, new Vector2(230f, 28f), label);

            SerializedObject sCtrl = new SerializedObject(defCtrl);
            sCtrl.FindProperty("defenseId").stringValue = defenseId;
            sCtrl.FindProperty("defenseTitle").stringValue = label;
            sCtrl.FindProperty("statusIndicatorRenderer").objectReferenceValue = led.GetComponent<Renderer>();
            sCtrl.FindProperty("statusLabelText").objectReferenceValue = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            sCtrl.FindProperty("statusLedImage").objectReferenceValue = btnObj.GetComponent<Image>();
            sCtrl.ApplyModifiedProperties();

            btnObj.GetComponent<Button>().onClick.AddListener(defCtrl.TriggerDefense);
        }

        private static GameObject BuildPhishingEmailPanel(GameObject monitor01, PhishingInspector phishingInspector, MailQuarantine mailQuarantine)
        {
            Transform canvasTransform = monitor01.transform.Find("Canvas_Pantalla");
            if (canvasTransform == null) return null;

            GameObject panelCorreo = new GameObject("Panel_Correo");
            panelCorreo.transform.SetParent(canvasTransform, false);
            RectTransform rect = panelCorreo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // Header con contador de señales
            GameObject counterObj = new GameObject("Txt_SeñalesContador");
            counterObj.transform.SetParent(panelCorreo.transform, false);
            RectTransform counterRect = counterObj.AddComponent<RectTransform>();
            counterRect.anchoredPosition = new Vector2(0f, 42f);
            counterRect.sizeDelta = new Vector2(150f, 12f);
            TextMeshProUGUI counterTmp = counterObj.AddComponent<TextMeshProUGUI>();
            counterTmp.text = "Señales Detectadas: <b>0/3</b>";
            counterTmp.fontSize = 10;
            counterTmp.alignment = TextAlignmentOptions.Center;
            counterTmp.color = Color.yellow;

            // 1. Signal_Remitente
            PhishingSignalHotspot sender = CreateHotspotRegion(panelCorreo.transform, "Signal_Remitente",
                "Remitente sospechoso y dominio no institucional",
                "De: Rasel Ahlam <rasel.ahlam@career-applicants-bd.com>\nDominio no institucional sin verificación SPF/DKIM.",
                new Vector2(0f, 26f), new Vector2(150f, 16f));

            // 2. Signal_Urgencia
            PhishingSignalHotspot urgency = CreateHotspotRegion(panelCorreo.transform, "Signal_Urgencia",
                "Sentido artificial de urgencia y presión psicológica",
                "Ingeniería social apelando a la amabilidad y urgencia antes del cierre inminente.",
                new Vector2(0f, 7f), new Vector2(150f, 18f));

            // 3. Signal_Enlace
            PhishingSignalHotspot link = CreateHotspotRegion(panelCorreo.transform, "Signal_Enlace",
                "Enlace con doble extensión ejecutable (.scr.zip)",
                "URL externa: bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip\nDropper empaquetado.",
                new Vector2(0f, -12f), new Vector2(150f, 16f));

            // Banner de Criterio de Aceptación 1 (inicialmente oculto)
            GameObject bannerObj = new GameObject("Banner_Criterio1");
            bannerObj.transform.SetParent(panelCorreo.transform, false);
            RectTransform bannerRect = bannerObj.AddComponent<RectTransform>();
            bannerRect.anchoredPosition = new Vector2(0f, 8f);
            bannerRect.sizeDelta = new Vector2(154f, 44f);
            Image bannerBg = bannerObj.AddComponent<Image>();
            bannerBg.color = new Color(0.12f, 0.55f, 0.28f, 0.92f);

            GameObject bannerTitleObj = new GameObject("Txt_BannerTitle");
            bannerTitleObj.transform.SetParent(bannerObj.transform, false);
            RectTransform titleRect = bannerTitleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 12f);
            titleRect.sizeDelta = new Vector2(150f, 14f);
            TextMeshProUGUI bannerTitleTmp = bannerTitleObj.AddComponent<TextMeshProUGUI>();
            bannerTitleTmp.text = "<color=#38F28A><b>¡CRITERIO DE ACEPTACIÓN 1 CUMPLIDO!</b></color>";
            bannerTitleTmp.fontSize = 9;
            bannerTitleTmp.alignment = TextAlignmentOptions.Center;

            GameObject bannerDetailObj = new GameObject("Txt_BannerDetail");
            bannerDetailObj.transform.SetParent(bannerObj.transform, false);
            RectTransform detailRect = bannerDetailObj.AddComponent<RectTransform>();
            detailRect.anchoredPosition = new Vector2(0f, -6f);
            detailRect.sizeDelta = new Vector2(150f, 22f);
            TextMeshProUGUI bannerDetailTmp = bannerDetailObj.AddComponent<TextMeshProUGUI>();
            bannerDetailTmp.text = "3 señales detectadas en menos de 3 minutos.\nEvidencia_Correo registrada. Selecciona la acción:";
            bannerDetailTmp.fontSize = 8;
            bannerDetailTmp.alignment = TextAlignmentOptions.Center;

            bannerObj.SetActive(false);

            // Botones de acción en la parte inferior
            GameObject btnOpenObj = CreateWorldGazeButton("Btn_AbrirEnlaceCV", panelCorreo.transform,
                new Vector2(-38f, -34f), new Vector2(72f, 18f), "Abrir enlace CV\n(Modo Ataque)");
            Button btnOpen = btnOpenObj.GetComponent<Button>();
            TextMeshProUGUI btnOpenTmp = btnOpenObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnOpenTmp != null) btnOpenTmp.fontSize = 8;

            GameObject btnReportObj = CreateWorldGazeButton("Btn_ReportarPhishing", panelCorreo.transform,
                new Vector2(38f, -34f), new Vector2(72f, 18f), "Reportar Phishing\n(Modo Defensa)");
            Button btnReport = btnReportObj.GetComponent<Button>();
            TextMeshProUGUI btnReportTmp = btnReportObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnReportTmp != null) btnReportTmp.fontSize = 8;

            btnOpen.onClick.AddListener(phishingInspector.ExecuteOpenLinkAttackSequence);
            btnReport.onClick.AddListener(phishingInspector.ExecuteReportPhishingDefenseSequence);

            // Banners de resultado (Infección vs Cuarentena)
            GameObject infectAlertObj = new GameObject("Banner_InfeccionAlerta");
            infectAlertObj.transform.SetParent(panelCorreo.transform, false);
            RectTransform infectRect = infectAlertObj.AddComponent<RectTransform>();
            infectRect.anchoredPosition = new Vector2(0f, -34f);
            infectRect.sizeDelta = new Vector2(150f, 20f);
            Image infectBg = infectAlertObj.AddComponent<Image>();
            infectBg.color = new Color(0.85f, 0.2f, 0.2f, 0.95f);

            GameObject infectTextObj = new GameObject("Text");
            infectTextObj.transform.SetParent(infectAlertObj.transform, false);
            RectTransform infectTextRect = infectTextObj.AddComponent<RectTransform>();
            infectTextRect.anchorMin = Vector2.zero;
            infectTextRect.anchorMax = Vector2.one;
            infectTextRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI infectTmp = infectTextObj.AddComponent<TextMeshProUGUI>();
            infectTmp.text = "<b><color=white>[ALERTA: INFECCION EN CURSO - BACKDOOR ACTIVO]</color></b>";
            infectTmp.fontSize = 8;
            infectTmp.alignment = TextAlignmentOptions.Center;
            infectAlertObj.SetActive(false);

            GameObject quarantineAlertObj = new GameObject("Banner_CuarentenaExito");
            quarantineAlertObj.transform.SetParent(panelCorreo.transform, false);
            RectTransform quarRect = quarantineAlertObj.AddComponent<RectTransform>();
            quarRect.anchoredPosition = new Vector2(0f, -34f);
            quarRect.sizeDelta = new Vector2(150f, 20f);
            Image quarBg = quarantineAlertObj.AddComponent<Image>();
            quarBg.color = new Color(0.12f, 0.65f, 0.28f, 0.95f);

            GameObject quarTextObj = new GameObject("Text");
            quarTextObj.transform.SetParent(quarantineAlertObj.transform, false);
            RectTransform quarTextRect = quarTextObj.AddComponent<RectTransform>();
            quarTextRect.anchorMin = Vector2.zero;
            quarTextRect.anchorMax = Vector2.one;
            quarTextRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI quarTmp = quarTextObj.AddComponent<TextMeshProUGUI>();
            quarTmp.text = "<b><color=white>[OK] CORREO EN CUARENTENA - 0 INFECTADOS</color></b>";
            quarTmp.fontSize = 8;
            quarTmp.alignment = TextAlignmentOptions.Center;
            quarantineAlertObj.SetActive(false);

            // Serializar propiedades en phishingInspector
            SerializedObject sInsp = new SerializedObject(phishingInspector);
            sInsp.FindProperty("senderHotspot").objectReferenceValue = sender;
            sInsp.FindProperty("urgencyHotspot").objectReferenceValue = urgency;
            sInsp.FindProperty("linkHotspot").objectReferenceValue = link;
            sInsp.FindProperty("signalsCounterText").objectReferenceValue = counterTmp;
            sInsp.FindProperty("criterionBannerPanel").objectReferenceValue = bannerObj;
            sInsp.FindProperty("criterionBannerTitle").objectReferenceValue = bannerTitleTmp;
            sInsp.FindProperty("criterionBannerDetails").objectReferenceValue = bannerDetailTmp;
            sInsp.FindProperty("criterionBannerBackground").objectReferenceValue = bannerBg;
            sInsp.FindProperty("openLinkCvButton").objectReferenceValue = btnOpen;
            sInsp.FindProperty("openLinkButtonText").objectReferenceValue = btnOpenTmp;
            sInsp.FindProperty("reportPhishingButton").objectReferenceValue = btnReport;
            sInsp.FindProperty("reportButtonText").objectReferenceValue = btnReportTmp;
            sInsp.FindProperty("infectionAlertBanner").objectReferenceValue = infectAlertObj;
            sInsp.FindProperty("infectionAlertText").objectReferenceValue = infectTmp;
            sInsp.FindProperty("quarantineNoticeBanner").objectReferenceValue = quarantineAlertObj;
            sInsp.FindProperty("quarantineNoticeText").objectReferenceValue = quarTmp;
            sInsp.FindProperty("mailQuarantine").objectReferenceValue = mailQuarantine;
            sInsp.ApplyModifiedProperties();

            return panelCorreo;
        }

        private static void BuildQuarantineConsolePanel(GameObject monitor01, GameObject monitor02, GameObject monitor03, MailQuarantine mailQuarantine, GameObject panelCorreo)
        {
            Transform canvasTransform = monitor01.transform.Find("Canvas_Pantalla");
            if (canvasTransform == null) return;

            GameObject panelQuarantine = new GameObject("Panel_ConsolaCuarentena");
            panelQuarantine.transform.SetParent(canvasTransform, false);
            RectTransform qRect = panelQuarantine.AddComponent<RectTransform>();
            qRect.anchorMin = Vector2.zero;
            qRect.anchorMax = Vector2.one;
            qRect.sizeDelta = Vector2.zero;

            Image qBg = panelQuarantine.AddComponent<Image>();
            qBg.color = new Color(0.04f, 0.08f, 0.12f, 0.96f);

            // Título de la Consola
            GameObject titleObj = new GameObject("Txt_HeaderTitle");
            titleObj.transform.SetParent(panelQuarantine.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 41f);
            titleRect.sizeDelta = new Vector2(152f, 12f);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b><color=#38F28A>CONSOLA DE CUARENTENA Y SANDBOX</color></b>";
            titleTmp.fontSize = 8.5f;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Indicador de fase
            GameObject statusObj = new GameObject("Txt_FaseStatus");
            statusObj.transform.SetParent(panelQuarantine.transform, false);
            RectTransform statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchoredPosition = new Vector2(0f, 31f);
            statusRect.sizeDelta = new Vector2(152f, 10f);
            TextMeshProUGUI statusTmp = statusObj.AddComponent<TextMeshProUGUI>();
            statusTmp.text = "ESTADO: <color=#4EC9B0>[1/3] AISLAMIENTO EN TRÁNSITO...</color>";
            statusTmp.fontSize = 7.5f;
            statusTmp.alignment = TextAlignmentOptions.Center;

            // Barra de progreso (Fondo)
            GameObject barBgObj = new GameObject("Bar_Fondo");
            barBgObj.transform.SetParent(panelQuarantine.transform, false);
            RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchoredPosition = new Vector2(0f, 23f);
            barBgRect.sizeDelta = new Vector2(148f, 4f);
            Image barBgImage = barBgObj.AddComponent<Image>();
            barBgImage.color = new Color(0.15f, 0.2f, 0.25f, 1f);

            // Barra de progreso (Relleno)
            GameObject barFillObj = new GameObject("Bar_Relleno");
            barFillObj.transform.SetParent(barBgObj.transform, false);
            RectTransform fillRect = barFillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = barFillObj.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;
            fillImg.color = new Color(0.22f, 0.95f, 0.54f, 1f); // #38f28a

            // Caja de terminal log
            GameObject logBgObj = new GameObject("Panel_LogBg");
            logBgObj.transform.SetParent(panelQuarantine.transform, false);
            RectTransform logBgRect = logBgObj.AddComponent<RectTransform>();
            logBgRect.anchoredPosition = new Vector2(0f, 1f);
            logBgRect.sizeDelta = new Vector2(150f, 38f);
            Image logBgImg = logBgObj.AddComponent<Image>();
            logBgImg.color = new Color(0.02f, 0.04f, 0.06f, 0.85f);

            GameObject logTextObj = new GameObject("Txt_TerminalLog");
            logTextObj.transform.SetParent(logBgObj.transform, false);
            RectTransform logTextRect = logTextObj.AddComponent<RectTransform>();
            logTextRect.anchorMin = Vector2.zero;
            logTextRect.anchorMax = Vector2.one;
            logTextRect.offsetMin = new Vector2(3f, 2f);
            logTextRect.offsetMax = new Vector2(-3f, -2f);
            TextMeshProUGUI logTmp = logTextObj.AddComponent<TextMeshProUGUI>();
            logTmp.text = "Iniciando conexión con Secure Email Gateway...";
            logTmp.fontSize = 6.2f;
            logTmp.alignment = TextAlignmentOptions.TopLeft;

            // Insignia de alerta de amenaza detectada
            GameObject threatObj = new GameObject("Badge_Amenaza");
            threatObj.transform.SetParent(panelQuarantine.transform, false);
            RectTransform threatRect = threatObj.AddComponent<RectTransform>();
            threatRect.anchoredPosition = new Vector2(0f, -22f);
            threatRect.sizeDelta = new Vector2(148f, 9f);
            Image threatBg = threatObj.AddComponent<Image>();
            threatBg.color = new Color(0.6f, 0.12f, 0.12f, 0.92f);

            GameObject threatTextObj = new GameObject("Text");
            threatTextObj.transform.SetParent(threatObj.transform, false);
            RectTransform threatTextRect = threatTextObj.AddComponent<RectTransform>();
            threatTextRect.anchorMin = Vector2.zero;
            threatTextRect.anchorMax = Vector2.one;
            threatTextRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI threatTmp = threatTextObj.AddComponent<TextMeshProUGUI>();
            threatTmp.text = "<color=#FF5A4E><b>[ALERTA] AMENAZA LAZARUS IDENTIFICADA - DROPPER .SCR BLOQUEADO</b></color>";
            threatTmp.fontSize = 6.8f;
            threatTmp.alignment = TextAlignmentOptions.Center;
            threatObj.SetActive(false);

            // Sello de Cuarentena exitosa
            GameObject stampObj = new GameObject("Badge_SelloCuarentena");
            stampObj.transform.SetParent(panelQuarantine.transform, false);
            RectTransform stampRect = stampObj.AddComponent<RectTransform>();
            stampRect.anchoredPosition = new Vector2(0f, -22f);
            stampRect.sizeDelta = new Vector2(148f, 9f);
            Image stampBg = stampObj.AddComponent<Image>();
            stampBg.color = new Color(0.12f, 0.45f, 0.22f, 0.92f);

            GameObject stampTextObj = new GameObject("Text");
            stampTextObj.transform.SetParent(stampObj.transform, false);
            RectTransform stampTextRect = stampTextObj.AddComponent<RectTransform>();
            stampTextRect.anchorMin = Vector2.zero;
            stampTextRect.anchorMax = Vector2.one;
            stampTextRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI stampTmp = stampTextObj.AddComponent<TextMeshProUGUI>();
            stampTmp.text = "<color=#38F28A><b>[OK] CORREO EN CUARENTENA PERIMETRAL - AMENAZA AISLADA</b></color>";
            stampTmp.fontSize = 6.8f;
            stampTmp.alignment = TextAlignmentOptions.Center;
            stampObj.SetActive(false);

            // Botón interactivo "Confirmar Contención y Finalizar Defensa"
            GameObject btnConfirmObj = CreateWorldGazeButton("Btn_ConfirmarContencion", panelQuarantine.transform,
                new Vector2(0f, -37f), new Vector2(146f, 16f), "Confirmar Contención y Finalizar Defensa");
            Button btnConfirm = btnConfirmObj.GetComponent<Button>();
            TextMeshProUGUI btnConfirmTmp = btnConfirmObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnConfirmTmp != null)
            {
                btnConfirmTmp.fontSize = 7.5f;
                btnConfirmTmp.text = "<b>Confirmar Contención y Finalizar Defensa</b>";
            }
            btnConfirm.onClick.AddListener(mailQuarantine.ConfirmContainmentAndEndScenario);

            panelQuarantine.SetActive(false);

            // Serializar propiedades en MailQuarantine
            SerializedObject sQuar = new SerializedObject(mailQuarantine);
            sQuar.FindProperty("normalEmailPanel").objectReferenceValue = panelCorreo;
            sQuar.FindProperty("quarantineConsolePanel").objectReferenceValue = panelQuarantine;
            sQuar.FindProperty("consoleHeaderTitleText").objectReferenceValue = titleTmp;
            sQuar.FindProperty("quarantinePhaseStatusText").objectReferenceValue = statusTmp;
            sQuar.FindProperty("sandboxTerminalLogText").objectReferenceValue = logTmp;
            sQuar.FindProperty("sandboxProgressBar").objectReferenceValue = fillImg;
            sQuar.FindProperty("threatBadgeAlert").objectReferenceValue = threatObj;
            sQuar.FindProperty("threatBadgeText").objectReferenceValue = threatTmp;
            sQuar.FindProperty("quarantineSuccessStamp").objectReferenceValue = stampObj;
            sQuar.FindProperty("quarantineSuccessText").objectReferenceValue = stampTmp;
            sQuar.FindProperty("confirmContainmentButton").objectReferenceValue = btnConfirm;
            sQuar.FindProperty("confirmButtonText").objectReferenceValue = btnConfirmTmp;
            sQuar.FindProperty("confirmButtonGlow").objectReferenceValue = btnConfirmObj.GetComponent<Image>();
            sQuar.FindProperty("monitor01Controller").objectReferenceValue = monitor01.GetComponent<InteractiveMonitorController>();
            sQuar.FindProperty("monitor02Controller").objectReferenceValue = monitor02.GetComponent<InteractiveMonitorController>();
            sQuar.FindProperty("monitor03Controller").objectReferenceValue = monitor03.GetComponent<InteractiveMonitorController>();
            sQuar.ApplyModifiedProperties();
        }

        private static PhishingSignalHotspot CreateHotspotRegion(Transform parent, string signalId, string title, string explanation, Vector2 anchoredPos, Vector2 size)
        {
            GameObject hotspotObj = new GameObject("Hotspot_" + signalId);
            hotspotObj.transform.SetParent(parent, false);
            hotspotObj.layer = LayerMask.NameToLayer("Interactive");

            RectTransform rect = hotspotObj.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = hotspotObj.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.08f);

            BoxCollider col = hotspotObj.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x * 0.0055f, size.y * 0.0055f, 0.02f);

            GazeInteractable gaze = hotspotObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = "Inspeccionar " + title;

            PhishingSignalHotspot hotspot = hotspotObj.AddComponent<PhishingSignalHotspot>();
            SerializedObject sH = new SerializedObject(hotspot);
            sH.FindProperty("signalId").stringValue = signalId;
            sH.FindProperty("signalTitle").stringValue = title;
            sH.FindProperty("technicalExplanation").stringValue = explanation;
            sH.FindProperty("hotspotBorderImage").objectReferenceValue = img;
            sH.ApplyModifiedProperties();

            return hotspot;
        }

        private static void BuildSuspectBoard(Transform parent)
        {
            GameObject boardRoot = new GameObject("Tablero_Sospechosos_RF02");
            boardRoot.transform.SetParent(parent);
            boardRoot.transform.position = new Vector3(6.88f, 1.6f, -0.5f);
            boardRoot.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            // Marco decorativo de tablero táctico en la pared Este
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Marco_Tablero_Tactico";
            frame.transform.SetParent(boardRoot.transform);
            frame.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            frame.transform.localScale = new Vector3(3.0f, 1.95f, 0.04f);

            Material frameMat = CreateColorMaterial("Mat_TacticalBoardFrame", new Color(0.04f, 0.10f, 0.07f));
            frame.GetComponent<Renderer>().sharedMaterial = frameMat;

            // Luz cenital focalizada sobre el tablero táctico
            GameObject spotObj = new GameObject("Luz_Focal_Tablero");
            spotObj.transform.SetParent(boardRoot.transform);
            spotObj.transform.localPosition = new Vector3(0f, 1.1f, -0.6f);
            Light spotLight = spotObj.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.range = 3.5f;
            spotLight.spotAngle = 75f;
            spotLight.intensity = 1.2f;
            spotLight.color = new Color(0.85f, 0.98f, 0.90f);
            spotObj.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);

            // Canvas World Space para SuspectBoardVR
            GameObject canvasObj = new GameObject("Canvas_Tablero_Sospechosos");
            canvasObj.transform.SetParent(boardRoot.transform);
            canvasObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            canvasObj.transform.localRotation = Quaternion.identity;

            SuspectBoardVR boardVR = canvasObj.AddComponent<SuspectBoardVR>();
            boardVR.EnsureDefaultDossiers();
            boardVR.InitializeBoard();

            // Mantener el componente SuspectDossierViewer en boardRoot para compatibilidad cruzada
            boardRoot.AddComponent<SuspectDossierViewer>();

            // TeleportPoint específico frente al tablero de sospechosos
            CreateTeleportPoint(parent, "TeleportPoint_TableroSospechosos", new Vector3(4.90f, 0.05f, -0.5f), "Examinar Tablero Táctico de Sospechosos");
        }

        private static GameObject BuildVrPlayerRig()
        {
            GameObject playerObj = new GameObject("Player");
            playerObj.tag = "PlayerRig";
            playerObj.transform.position = new Vector3(0f, 0f, -4.0f); // En el vestíbulo frente al portal
            playerObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            playerObj.AddComponent<PlayerTeleportController>();
            playerObj.AddComponent<CardboardXrLifecycleManager>();

            // Main Camera a 1.65m de altura de ojos
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(playerObj.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camObj.transform.localRotation = Quaternion.identity;

            Camera cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.Skybox;

            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<CardboardReticleAdapter>();
            camObj.AddComponent<EditorVrCameraSimulator>();

            return playerObj;
        }

        private static void BuildWorldSpaceHud(Transform parent, GameObject playerRig)
        {
            GameObject hudObj = new GameObject("VR_WorldSpace_HUD");
            hudObj.transform.SetParent(parent.transform);
            hudObj.transform.position = new Vector3(0f, 1.3f, -2.0f);

            Canvas canvas = hudObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = hudObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500f, 300f);
            rect.localScale = new Vector3(0.002f, 0.002f, 0.002f);
            hudObj.AddComponent<CanvasScaler>();
            hudObj.AddComponent<GraphicRaycaster>();

            hudObj.AddComponent<WorldSpaceHudManager>();
        }

        private static void BuildOfficialUIManager(Transform parent, GameObject playerRig)
        {
            GameObject uiManagerObj = new GameObject("UIManager_Central");
            uiManagerObj.transform.SetParent(parent, false);
            UIManager uiManager = uiManagerObj.AddComponent<UIManager>();

            Transform camTransform = playerRig != null ? playerRig.GetComponentInChildren<Camera>()?.transform : null;

            // 1. WristOrDeskHUD (Panel de estado sobre el escritorio de Carla Mendoza)
            GameObject deskHudObj = new GameObject("WristOrDeskHUD");
            deskHudObj.transform.SetParent(uiManagerObj.transform, false);
            deskHudObj.transform.position = new Vector3(-2.15f, 0.96f, 1.85f);
            deskHudObj.transform.rotation = Quaternion.Euler(15f, -30f, 0f);

            Canvas deskCanvas = deskHudObj.AddComponent<Canvas>();
            deskCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform deskRect = deskHudObj.GetComponent<RectTransform>();
            deskRect.sizeDelta = new Vector2(260f, 140f);
            deskRect.localScale = new Vector3(0.0035f, 0.0035f, 0.0035f);
            deskHudObj.AddComponent<CanvasScaler>();
            deskHudObj.AddComponent<GraphicRaycaster>();

            // Fondo oscuro #020a06
            Image deskBg = deskHudObj.AddComponent<Image>();
            deskBg.color = UIManager.ColorDarkBg;

            // Borde estético verde código #38f28a
            GameObject borderObj = new GameObject("Border_Glow");
            borderObj.transform.SetParent(deskHudObj.transform, false);
            RectTransform bRect = borderObj.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.sizeDelta = new Vector2(4f, 4f);
            Image bImg = borderObj.AddComponent<Image>();
            bImg.color = new Color(0.22f, 0.95f, 0.54f, 0.4f);

            // Título
            GameObject titleObj = new GameObject("Txt_Header");
            titleObj.transform.SetParent(deskHudObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 56f);
            tRect.sizeDelta = new Vector2(250f, 16f);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b><color=#38F28A>ANDES-SIM · BANCO ANDINO (SOC VR)</color></b>";
            titleTmp.fontSize = 8.5f;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Modo activo
            GameObject modeObj = new GameObject("Txt_ModeStatus");
            modeObj.transform.SetParent(deskHudObj.transform, false);
            RectTransform mRect = modeObj.AddComponent<RectTransform>();
            mRect.anchoredPosition = new Vector2(-45f, 38f);
            mRect.sizeDelta = new Vector2(150f, 14f);
            TextMeshProUGUI modeTmp = modeObj.AddComponent<TextMeshProUGUI>();
            modeTmp.text = "<b>MODO: <color=#FF5A4E>ATAQUE 2016 (LAZARUS)</color></b>";
            modeTmp.fontSize = 7.5f;
            modeTmp.alignment = TextAlignmentOptions.Left;

            // Botón interactivo por mirada para alternar modo
            GameObject btnModeObj = CreateWorldGazeButton("Btn_CambiarModoHUD", deskHudObj.transform,
                new Vector2(75f, 38f), new Vector2(90f, 14f), "Cambiar a: CON DEFENSA");
            Button btnMode = btnModeObj.GetComponent<Button>();
            TextMeshProUGUI btnModeTmp = btnModeObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnModeTmp != null)
            {
                btnModeTmp.fontSize = 6.5f;
                btnModeTmp.text = "<b>Cambiar a: CON DEFENSA</b>";
                btnModeTmp.color = UIManager.ColorLightText;
            }
            Image btnGlow = btnModeObj.GetComponent<Image>();
            btnGlow.color = UIManager.ColorCodeGreen;
            btnMode.onClick.AddListener(uiManager.ToggleSimulationMode);

            // Reloj regresivo 03:00
            GameObject clockObj = new GameObject("Txt_CountdownClock");
            clockObj.transform.SetParent(deskHudObj.transform, false);
            RectTransform cRect = clockObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(-45f, 20f);
            cRect.sizeDelta = new Vector2(150f, 14f);
            TextMeshProUGUI clockTmp = clockObj.AddComponent<TextMeshProUGUI>();
            clockTmp.text = "TIEMPO: <color=#38F28A><b>03:00</b></color>";
            clockTmp.fontSize = 7.5f;
            clockTmp.alignment = TextAlignmentOptions.Left;

            // Paso actual del flujo
            GameObject stepObj = new GameObject("Txt_CurrentStep");
            stepObj.transform.SetParent(deskHudObj.transform, false);
            RectTransform sRect = stepObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(75f, 20f);
            sRect.sizeDelta = new Vector2(90f, 14f);
            TextMeshProUGUI stepTmp = stepObj.AddComponent<TextMeshProUGUI>();
            stepTmp.text = "<b>FLUJO:</b> <color=#D6F2E2>Paso 1/6</color>";
            stepTmp.fontSize = 7.0f;
            stepTmp.alignment = TextAlignmentOptions.Right;

            // Barra de progreso de paso
            GameObject barBg = new GameObject("Bar_FlujoFondo");
            barBg.transform.SetParent(deskHudObj.transform, false);
            RectTransform barBgR = barBg.AddComponent<RectTransform>();
            barBgR.anchoredPosition = new Vector2(0f, 9f);
            barBgR.sizeDelta = new Vector2(250f, 3f);
            Image barBgImg = barBg.AddComponent<Image>();
            barBgImg.color = new Color(0.1f, 0.2f, 0.15f, 1f);

            GameObject barFill = new GameObject("Bar_FlujoFill");
            barFill.transform.SetParent(barBg.transform, false);
            RectTransform barFillR = barFill.AddComponent<RectTransform>();
            barFillR.anchorMin = Vector2.zero;
            barFillR.anchorMax = Vector2.one;
            barFillR.sizeDelta = Vector2.zero;
            Image stepFill = barFill.AddComponent<Image>();
            stepFill.type = Image.Type.Filled;
            stepFill.fillMethod = Image.FillMethod.Horizontal;
            stepFill.fillAmount = 0.16f;
            stepFill.color = UIManager.ColorCodeGreen;

            // Checklist en vivo: 3 líneas
            // 1) Señales de Phishing (0/3)
            GameObject checkSigObj = new GameObject("Txt_CheckSignals");
            checkSigObj.transform.SetParent(deskHudObj.transform, false);
            RectTransform csRect = checkSigObj.AddComponent<RectTransform>();
            csRect.anchoredPosition = new Vector2(0f, -8f);
            csRect.sizeDelta = new Vector2(240f, 13f);
            TextMeshProUGUI checkSigTmp = checkSigObj.AddComponent<TextMeshProUGUI>();
            checkSigTmp.text = "[-] Senales de Phishing: <color=#D6F2E2><b>0/3</b></color>";
            checkSigTmp.fontSize = 7.2f;
            checkSigTmp.alignment = TextAlignmentOptions.Left;
            checkSigTmp.color = UIManager.ColorLightText;

            // 2) Evidencias (0/3)
            GameObject checkEviObj = new GameObject("Txt_CheckEvidences");
            checkEviObj.transform.SetParent(deskHudObj.transform, false);
            RectTransform ceRect = checkEviObj.AddComponent<RectTransform>();
            ceRect.anchoredPosition = new Vector2(0f, -23f);
            ceRect.sizeDelta = new Vector2(240f, 13f);
            TextMeshProUGUI checkEviTmp = checkEviObj.AddComponent<TextMeshProUGUI>();
            checkEviTmp.text = "[-] Evidencias Forenses: <color=#D6F2E2><b>0/3</b></color>";
            checkEviTmp.fontSize = 7.2f;
            checkEviTmp.alignment = TextAlignmentOptions.Left;
            checkEviTmp.color = UIManager.ColorLightText;

            // 3) Defensas (0/3)
            GameObject checkDefObj = new GameObject("Txt_CheckDefenses");
            checkDefObj.transform.SetParent(deskHudObj.transform, false);
            RectTransform cdRect = checkDefObj.AddComponent<RectTransform>();
            cdRect.anchoredPosition = new Vector2(0f, -38f);
            cdRect.sizeDelta = new Vector2(240f, 13f);
            TextMeshProUGUI checkDefTmp = checkDefObj.AddComponent<TextMeshProUGUI>();
            checkDefTmp.text = "[-] Defensas Aplicadas: <color=#D6F2E2><b>0/3</b></color>";
            checkDefTmp.fontSize = 7.2f;
            checkDefTmp.alignment = TextAlignmentOptions.Left;
            checkDefTmp.color = UIManager.ColorLightText;

            // 2. NotificationToast (Banner flotante a 2 metros)
            GameObject toastObj = new GameObject("NotificationToast");
            toastObj.transform.SetParent(uiManagerObj.transform, false);
            toastObj.transform.position = new Vector3(0f, 1.5f, 2.0f);

            Canvas toastCanvas = toastObj.AddComponent<Canvas>();
            toastCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform tR = toastObj.GetComponent<RectTransform>();
            tR.sizeDelta = new Vector2(280f, 85f);
            tR.localScale = new Vector3(0.0035f, 0.0035f, 0.0035f);
            toastObj.AddComponent<CanvasScaler>();

            CanvasGroup toastCg = toastObj.AddComponent<CanvasGroup>();
            toastCg.alpha = 0f;

            Image toastBg = toastObj.AddComponent<Image>();
            toastBg.color = UIManager.ColorDarkBg;

            GameObject tBorder = new GameObject("Border_Glow");
            tBorder.transform.SetParent(toastObj.transform, false);
            RectTransform tbR = tBorder.AddComponent<RectTransform>();
            tbR.anchorMin = Vector2.zero;
            tbR.anchorMax = Vector2.one;
            tbR.sizeDelta = new Vector2(4f, 4f);
            Image toastGlowImg = tBorder.AddComponent<Image>();
            toastGlowImg.color = UIManager.ColorCodeGreen;

            GameObject tTitle = new GameObject("Txt_ToastTitle");
            tTitle.transform.SetParent(toastObj.transform, false);
            RectTransform ttR = tTitle.AddComponent<RectTransform>();
            ttR.anchoredPosition = new Vector2(0f, 22f);
            ttR.sizeDelta = new Vector2(265f, 20f);
            TextMeshProUGUI toastTitleTmp = tTitle.AddComponent<TextMeshProUGUI>();
            toastTitleTmp.fontSize = 8.5f;
            toastTitleTmp.alignment = TextAlignmentOptions.Center;

            GameObject tMsg = new GameObject("Txt_ToastMessage");
            tMsg.transform.SetParent(toastObj.transform, false);
            RectTransform tmR = tMsg.AddComponent<RectTransform>();
            tmR.anchoredPosition = new Vector2(0f, -12f);
            tmR.sizeDelta = new Vector2(265f, 44f);
            TextMeshProUGUI toastMsgTmp = tMsg.AddComponent<TextMeshProUGUI>();
            toastMsgTmp.fontSize = 7.2f;
            toastMsgTmp.alignment = TextAlignmentOptions.Center;
            toastMsgTmp.color = UIManager.ColorLightText;

            toastObj.SetActive(false);

            // Serializar en UIManager
            SerializedObject sUi = new SerializedObject(uiManager);
            sUi.FindProperty("wristOrDeskHudPanel").objectReferenceValue = deskHudObj;
            sUi.FindProperty("modeStatusText").objectReferenceValue = modeTmp;
            sUi.FindProperty("modeStatusBadgeBg").objectReferenceValue = deskBg;
            sUi.FindProperty("toggleModeButton").objectReferenceValue = btnMode;
            sUi.FindProperty("toggleModeButtonText").objectReferenceValue = btnModeTmp;
            sUi.FindProperty("toggleModeButtonGlow").objectReferenceValue = btnGlow;
            sUi.FindProperty("countdownClockText").objectReferenceValue = clockTmp;
            sUi.FindProperty("currentStepTitleText").objectReferenceValue = stepTmp;
            sUi.FindProperty("stepProgressBarFill").objectReferenceValue = stepFill;
            sUi.FindProperty("phishingSignalsChecklistText").objectReferenceValue = checkSigTmp;
            sUi.FindProperty("evidencesChecklistText").objectReferenceValue = checkEviTmp;
            sUi.FindProperty("defensesChecklistText").objectReferenceValue = checkDefTmp;
            sUi.FindProperty("notificationToastPanel").objectReferenceValue = toastObj;
            sUi.FindProperty("toastCanvasGroup").objectReferenceValue = toastCg;
            sUi.FindProperty("toastTitleText").objectReferenceValue = toastTitleTmp;
            sUi.FindProperty("toastMessageText").objectReferenceValue = toastMsgTmp;
            sUi.FindProperty("toastBorderGlow").objectReferenceValue = toastGlowImg;
            sUi.FindProperty("toastBackground").objectReferenceValue = toastBg;
            sUi.FindProperty("targetCamera").objectReferenceValue = camTransform;
            sUi.ApplyModifiedProperties();
        }

        private static void BuildFinalEvaluationScreen(Transform parent)
        {
            GameObject finalObj = new GameObject("Canvas_PantallaFinal_Resultado");
            finalObj.transform.SetParent(parent.transform);
            finalObj.transform.position = new Vector3(0f, 1.5f, 0f);

            Canvas canvas = finalObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = finalObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600f, 400f);
            rect.localScale = new Vector3(0.003f, 0.003f, 0.003f);
            finalObj.AddComponent<CanvasScaler>();
            finalObj.AddComponent<GraphicRaycaster>();

            finalObj.AddComponent<FinalEvaluationScreen>();
        }

        private static void BuildEndScenarioHolographicPanel(Transform parent, GameObject playerRig)
        {
            GameObject endPanelObj = new GameObject("Panel_Holografico_EndScenario");
            endPanelObj.transform.SetParent(parent, false);
            endPanelObj.transform.position = new Vector3(0f, 1.5f, 0.5f);
            endPanelObj.transform.rotation = Quaternion.identity;

            Canvas canvas = endPanelObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = endPanelObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(480f, 320f);
            rect.localScale = new Vector3(0.0035f, 0.0035f, 0.0035f);
            endPanelObj.AddComponent<CanvasScaler>();
            endPanelObj.AddComponent<GraphicRaycaster>();
            CanvasGroup cg = endPanelObj.AddComponent<CanvasGroup>();

            // Fondo oscuro oficial #020a06
            Image bg = endPanelObj.AddComponent<Image>();
            bg.color = UIManager.ColorDarkBg;

            // Borde superior / marco brillante
            GameObject borderObj = new GameObject("Border_Glow");
            borderObj.transform.SetParent(endPanelObj.transform, false);
            RectTransform bRect = borderObj.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.sizeDelta = new Vector2(4f, 4f);
            Image bImg = borderObj.AddComponent<Image>();
            bImg.color = UIManager.ColorCodeGreen;

            // 1. Título de la cabecera
            GameObject titleObj = new GameObject("Txt_OutcomeHeaderTitle");
            titleObj.transform.SetParent(endPanelObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 130f);
            tRect.sizeDelta = new Vector2(460f, 26f);
            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<b><color=#FF5A4E>RESULTADO MODO ATAQUE (2016): RED COMPROMETIDA</color></b>";
            titleTmp.fontSize = 11.5f;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // 2. Subtítulo
            GameObject subObj = new GameObject("Txt_OutcomeSubtitle");
            subObj.transform.SetParent(endPanelObj.transform, false);
            RectTransform sRect = subObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 108f);
            sRect.sizeDelta = new Vector2(460f, 16f);
            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = "PERSISTENCIA Y MOVIMIENTO LATERAL · LAZARUS GROUP";
            subTmp.fontSize = 8.5f;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = UIManager.ColorWarningAmber;

            // 3. Resumen técnico de hechos
            GameObject summaryObj = new GameObject("Txt_TechnicalSummary");
            summaryObj.transform.SetParent(endPanelObj.transform, false);
            RectTransform sumRect = summaryObj.AddComponent<RectTransform>();
            sumRect.anchoredPosition = new Vector2(0f, 52f);
            sumRect.sizeDelta = new Vector2(450f, 85f);
            TextMeshProUGUI summaryTmp = summaryObj.AddComponent<TextMeshProUGUI>();
            summaryTmp.text =
                "16 empleados recibieron el correo de \"Rasel Ahlam\" el 29 de enero de 2015; al menos 3 computadoras intentaron abrir el archivo; " +
                "la ausencia de sandbox, el desconocimiento de phishing y las credenciales anotadas de Carla Mendoza permitieron a Lazarus " +
                "abrir una puerta trasera hacia la terminal SWIFT (que culminaría en las 36 órdenes por US$ 951 millones).";
            summaryTmp.fontSize = 7.8f;
            summaryTmp.alignment = TextAlignmentOptions.Center;
            summaryTmp.color = UIManager.ColorLightText;

            // 4. Métricas de impacto
            GameObject metricsContainer = new GameObject("Container_Metricas");
            metricsContainer.transform.SetParent(endPanelObj.transform, false);
            RectTransform mRect = metricsContainer.AddComponent<RectTransform>();
            mRect.anchoredPosition = new Vector2(0f, -8f);
            mRect.sizeDelta = new Vector2(450f, 30f);

            GameObject m1 = new GameObject("Txt_Infected");
            m1.transform.SetParent(metricsContainer.transform, false);
            RectTransform m1R = m1.AddComponent<RectTransform>();
            m1R.anchoredPosition = new Vector2(-150f, 0f);
            m1R.sizeDelta = new Vector2(140f, 26f);
            TextMeshProUGUI m1Tmp = m1.AddComponent<TextMeshProUGUI>();
            m1Tmp.text = "<b>INFECTADOS:</b> <color=#FF5A4E>3 / 3 (100%)</color>";
            m1Tmp.fontSize = 7.2f;
            m1Tmp.alignment = TextAlignmentOptions.Center;

            GameObject m2 = new GameObject("Txt_Money");
            m2.transform.SetParent(metricsContainer.transform, false);
            RectTransform m2R = m2.AddComponent<RectTransform>();
            m2R.anchoredPosition = new Vector2(0f, 0f);
            m2R.sizeDelta = new Vector2(140f, 26f);
            TextMeshProUGUI m2Tmp = m2.AddComponent<TextMeshProUGUI>();
            m2Tmp.text = "<b>ROBO:</b> <color=#FF5A4E>US$ 951,000,000</color>";
            m2Tmp.fontSize = 7.2f;
            m2Tmp.alignment = TextAlignmentOptions.Center;

            GameObject m3 = new GameObject("Txt_Swift");
            m3.transform.SetParent(metricsContainer.transform, false);
            RectTransform m3R = m3.AddComponent<RectTransform>();
            m3R.anchoredPosition = new Vector2(150f, 0f);
            m3R.sizeDelta = new Vector2(140f, 26f);
            TextMeshProUGUI m3Tmp = m3.AddComponent<TextMeshProUGUI>();
            m3Tmp.text = "<b>RED SWIFT:</b> <color=#FF5A4E>Backdoor Activo</color>";
            m3Tmp.fontSize = 7.2f;
            m3Tmp.alignment = TextAlignmentOptions.Center;

            // 5. Criterio de aceptación
            GameObject critObj = new GameObject("Txt_CriterionBadge");
            critObj.transform.SetParent(endPanelObj.transform, false);
            RectTransform critRect = critObj.AddComponent<RectTransform>();
            critRect.anchoredPosition = new Vector2(0f, -34f);
            critRect.sizeDelta = new Vector2(450f, 18f);
            TextMeshProUGUI critTmp = critObj.AddComponent<TextMeshProUGUI>();
            critTmp.text = "[TIEMPO] Tiempo empleado: <b>142.5s / 180s</b> <color=#38F28A>[CRITERIO CUMPLIDO]</color>";
            critTmp.fontSize = 7.5f;
            critTmp.alignment = TextAlignmentOptions.Center;

            // 6. Acciones de Modo Ataque
            GameObject attackActions = new GameObject("Container_AttackActions");
            attackActions.transform.SetParent(endPanelObj.transform, false);
            RectTransform aaRect = attackActions.AddComponent<RectTransform>();
            aaRect.anchoredPosition = new Vector2(0f, -80f);
            aaRect.sizeDelta = new Vector2(450f, 50f);

            GameObject btnTryDefense = CreateWorldGazeButton("Btn_ProbarModoDefensa", attackActions.transform,
                Vector2.zero, new Vector2(280f, 28f), "PROBAR AHORA EL MODO DEFENSA");
            Button btnTryDefenseComponent = btnTryDefense.GetComponent<Button>();
            TextMeshProUGUI btnTryDefTmp = btnTryDefense.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTryDefTmp != null)
            {
                btnTryDefTmp.fontSize = 9.5f;
                btnTryDefTmp.text = "<b>PROBAR AHORA EL MODO DEFENSA</b>";
                btnTryDefTmp.color = Color.white;
            }
            Image btnTryGlow = btnTryDefense.GetComponent<Image>();
            btnTryGlow.color = UIManager.ColorCodeGreen;

            // 7. Acciones de Modo Defensa
            GameObject defenseActions = new GameObject("Container_DefenseActions");
            defenseActions.transform.SetParent(endPanelObj.transform, false);
            RectTransform daRect = defenseActions.AddComponent<RectTransform>();
            daRect.anchoredPosition = new Vector2(0f, -80f);
            daRect.sizeDelta = new Vector2(450f, 50f);

            GameObject btnSuspects = CreateWorldGazeButton("Btn_VerSospechosos", defenseActions.transform,
                new Vector2(-150f, 0f), new Vector2(138f, 26f), "Ver Expediente 4 Sospechosos");
            Button btnSuspectsComponent = btnSuspects.GetComponent<Button>();
            TextMeshProUGUI btnSuspTmp = btnSuspects.GetComponentInChildren<TextMeshProUGUI>();
            if (btnSuspTmp != null) { btnSuspTmp.fontSize = 7.5f; btnSuspTmp.text = "<b>Ver 4 Sospechosos</b>"; }

            GameObject btnCredits = CreateWorldGazeButton("Btn_VerCreditos", defenseActions.transform,
                new Vector2(0f, 0f), new Vector2(138f, 26f), "Ver Créditos y Licencias");
            Button btnCreditsComponent = btnCredits.GetComponent<Button>();
            TextMeshProUGUI btnCredTmp = btnCredits.GetComponentInChildren<TextMeshProUGUI>();
            if (btnCredTmp != null) { btnCredTmp.fontSize = 7.5f; btnCredTmp.text = "<b>Créditos y Licencias</b>"; }

            GameObject btnRestart = CreateWorldGazeButton("Btn_ReiniciarSimulacion", defenseActions.transform,
                new Vector2(150f, 0f), new Vector2(138f, 26f), "Reiniciar Simulación");
            Button btnRestartComponent = btnRestart.GetComponent<Button>();
            TextMeshProUGUI btnRestTmp = btnRestart.GetComponentInChildren<TextMeshProUGUI>();
            if (btnRestTmp != null) { btnRestTmp.fontSize = 7.5f; btnRestTmp.text = "<b>Reiniciar Simulación</b>"; }

            defenseActions.SetActive(false);

            // 8. Modal de Créditos
            GameObject creditsModal = new GameObject("Modal_Creditos");
            creditsModal.transform.SetParent(endPanelObj.transform, false);
            RectTransform cmRect = creditsModal.AddComponent<RectTransform>();
            cmRect.anchorMin = Vector2.zero;
            cmRect.anchorMax = Vector2.one;
            cmRect.sizeDelta = Vector2.zero;

            Image cmBg = creditsModal.AddComponent<Image>();
            cmBg.color = new Color(0.02f, 0.05f, 0.08f, 0.98f);

            GameObject cmTitle = new GameObject("Txt_CreditsTitle");
            cmTitle.transform.SetParent(creditsModal.transform, false);
            RectTransform cmtR = cmTitle.AddComponent<RectTransform>();
            cmtR.anchoredPosition = new Vector2(0f, 120f);
            cmtR.sizeDelta = new Vector2(440f, 22f);
            TextMeshProUGUI cmtTmp = cmTitle.AddComponent<TextMeshProUGUI>();
            cmtTmp.text = "<b><color=#38F28A>CRÉDITOS INSTITUCIONALES Y LICENCIAS</color></b>";
            cmtTmp.fontSize = 10f;
            cmtTmp.alignment = TextAlignmentOptions.Center;

            GameObject cmBody = new GameObject("Txt_CreditsBody");
            cmBody.transform.SetParent(creditsModal.transform, false);
            RectTransform cmbR = cmBody.AddComponent<RectTransform>();
            cmbR.anchoredPosition = new Vector2(0f, 20f);
            cmbR.sizeDelta = new Vector2(440f, 170f);
            TextMeshProUGUI cmbTmp = cmBody.AddComponent<TextMeshProUGUI>();
            cmbTmp.fontSize = 7.5f;
            cmbTmp.alignment = TextAlignmentOptions.Center;
            cmbTmp.color = UIManager.ColorLightText;

            GameObject btnCloseCred = CreateWorldGazeButton("Btn_CerrarCreditos", creditsModal.transform,
                new Vector2(0f, -115f), new Vector2(160f, 24f), "Cerrar Créditos");
            Button btnCloseCredComponent = btnCloseCred.GetComponent<Button>();
            TextMeshProUGUI btnCloseTmp = btnCloseCred.GetComponentInChildren<TextMeshProUGUI>();
            if (btnCloseTmp != null) { btnCloseTmp.fontSize = 7.5f; btnCloseTmp.text = "<b>Regresar al Resumen</b>"; }

            creditsModal.SetActive(false);

            // Componente EndScenarioPanelUI
            EndScenarioPanelUI endScript = endPanelObj.AddComponent<EndScenarioPanelUI>();
            Transform camTransform = playerRig != null ? playerRig.GetComponentInChildren<Camera>()?.transform : null;

            SerializedObject sEnd = new SerializedObject(endScript);
            sEnd.FindProperty("panelRoot").objectReferenceValue = endPanelObj;
            sEnd.FindProperty("panelCanvasGroup").objectReferenceValue = cg;
            sEnd.FindProperty("targetCamera").objectReferenceValue = camTransform;
            sEnd.FindProperty("outcomeHeaderTitleText").objectReferenceValue = titleTmp;
            sEnd.FindProperty("outcomeHeaderBorder").objectReferenceValue = bImg;
            sEnd.FindProperty("outcomeSubtitleText").objectReferenceValue = subTmp;
            sEnd.FindProperty("technicalSummaryBodyText").objectReferenceValue = summaryTmp;
            sEnd.FindProperty("acceptanceCriterionBadgeText").objectReferenceValue = critTmp;
            sEnd.FindProperty("infectedHostsMetricText").objectReferenceValue = m1Tmp;
            sEnd.FindProperty("stolenMoneyMetricText").objectReferenceValue = m2Tmp;
            sEnd.FindProperty("swiftStatusMetricText").objectReferenceValue = m3Tmp;
            sEnd.FindProperty("attackActionContainer").objectReferenceValue = attackActions;
            sEnd.FindProperty("tryDefenseModeButton").objectReferenceValue = btnTryDefenseComponent;
            sEnd.FindProperty("tryDefenseButtonText").objectReferenceValue = btnTryDefTmp;
            sEnd.FindProperty("tryDefenseButtonGlow").objectReferenceValue = btnTryGlow;
            sEnd.FindProperty("defenseActionContainer").objectReferenceValue = defenseActions;
            sEnd.FindProperty("viewSuspectsButton").objectReferenceValue = btnSuspectsComponent;
            sEnd.FindProperty("viewCreditsButton").objectReferenceValue = btnCreditsComponent;
            sEnd.FindProperty("restartSimulationButton").objectReferenceValue = btnRestartComponent;
            sEnd.FindProperty("creditsModalPanel").objectReferenceValue = creditsModal;
            sEnd.FindProperty("creditsBodyText").objectReferenceValue = cmbTmp;
            sEnd.FindProperty("closeCreditsButton").objectReferenceValue = btnCloseCredComponent;
            sEnd.ApplyModifiedProperties();

            btnTryDefenseComponent.onClick.AddListener(endScript.OnTryDefenseModeClicked);
            btnSuspectsComponent.onClick.AddListener(endScript.OnViewSuspectsClicked);
            btnCreditsComponent.onClick.AddListener(endScript.OnViewCreditsClicked);
            btnCloseCredComponent.onClick.AddListener(endScript.OnCloseCreditsClicked);
            btnRestartComponent.onClick.AddListener(endScript.OnRestartSimulationClicked);

            endPanelObj.SetActive(false);
        }

        private static void BuildCreditsAndEthicsModal(Transform parent, GameObject playerRig)
        {
            GameObject modalObj = new GameObject("CreditsAndEthicsModalUI");
            modalObj.transform.SetParent(parent, false);
            modalObj.transform.position = new Vector3(0f, 1.5f, 0f);

            CreditsAndEthicsModalUI modal = modalObj.AddComponent<CreditsAndEthicsModalUI>();
            Transform camTransform = playerRig != null ? playerRig.GetComponentInChildren<Camera>()?.transform : null;
            if (camTransform != null)
            {
                SerializedObject sModal = new SerializedObject(modal);
                sModal.FindProperty("targetCamera").objectReferenceValue = camTransform;
                sModal.ApplyModifiedProperties();
            }
        }

        private static GameObject CreateWorldGazeButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size, string text)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.layer = LayerMask.NameToLayer("Interactive");

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.65f, 0.35f, 0.9f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Texto del botón
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            // Añadir WorldSpaceUiGazeButton
            btnObj.AddComponent<BoxCollider>();
            btnObj.AddComponent<GazeInteractable>();
            btnObj.AddComponent<WorldSpaceUiGazeButton>();

            return btnObj;
        }

        private static Material CreateColorMaterial(string name, Color color)
        {
            Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
            if (standardShader == null) standardShader = Shader.Find("Standard");

            Material mat = new Material(standardShader);
            mat.name = name;
            mat.color = color;
            return mat;
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            bool alreadyRegistered = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (string.Equals(scenes[i].path, scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    alreadyRegistered = true;
                    scenes[i].enabled = true;
                    break;
                }
            }

            if (!alreadyRegistered)
            {
                scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log($"{LogPrefix} Escena registrada en EditorBuildSettings.scenes en posición 0.");
            }
        }
    }
}
