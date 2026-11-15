using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Scenario01;
using AndesSim.UI;
using AndesSim.VR;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AndesSim.Environment
{
    /// <summary>
    /// Generador procedural especializado de la estructura física 3D y displays de los 3 monitores
    /// (Monitor_01, Monitor_02, Monitor_03) para la "Oficina de pagos" de BANCO ANDINO.
    /// Simulación VR ANDES-SIM (Unity 6 - URP / Standard / Cardboard XR).
    /// 
    /// Especificaciones de diseño:
    /// 1. Modela el chasis 3D del monitor, marco delgado en negro mate y soporte ajustable de escritorio
    ///    (placa base, columna vertical, articulación VESA con inclinación ergonómica y pasacables).
    /// 2. Canvas en World Space perfectamente alineado frente a cada pantalla con el color de fondo
    ///    corporativo de terminal bancaria (#000000 puro con marco y bordes verdes neón #38f28a).
    /// 3. En Monitor_01, configura visualmente el buzón de correo completo con el mensaje malicioso
    ///    de "Rasel Ahlam", los 3 hotspots de señales de phishing (RF-03), contador 0/3 y botones interactivos
    ///    ('ABRIR ENLACE' / 'REPORTAR PHISHING') con máxima legibilidad en VR Cardboard.
    /// 4. En Monitor_02 y Monitor_03, despliega las consolas de Comercio Exterior y SIEM respectivamente.
    /// </summary>
    public static class MonitorMeshBuilder
    {
        private const string LogPrefix = "[ANDES-SIM - MonitorMeshBuilder]";

        #region Paleta Cromática Oficial de Monitores y Terminal

        /// <summary>
        /// Fondo puro de la terminal bancaria: Negro absoluto (#000000).
        /// </summary>
        public static readonly Color TerminalBgColor = new Color(0.0f, 0.0f, 0.0f, 1.0f); // #000000

        /// <summary>
        /// Borde y acento cibernético corporativo Banco Andino: Verde Neón (#38f28a).
        /// </summary>
        public static readonly Color TerminalBorderGreen = new Color(0.22f, 0.95f, 0.54f, 1.0f); // #38f28a

        /// <summary>
        /// Plástico de bisel y chasis: Negro mate suave anti-reflejo (#0c0d10).
        /// </summary>
        public static readonly Color BezelMatteBlackColor = new Color(0.05f, 0.055f, 0.065f, 1.0f);

        /// <summary>
        /// Soporte y placa base de escritorio: Acero oscuro cepillado / Gunmetal (#1b1e22).
        /// </summary>
        public static readonly Color StandDarkSteelColor = new Color(0.12f, 0.135f, 0.155f, 1.0f);

        /// <summary>
        /// Acento de alerta crítica / Phishing en texto: Rojo (#ff5a4e).
        /// </summary>
        public static readonly Color AlertRedColor = new Color(1.0f, 0.35f, 0.31f, 1.0f); // #ff5a4e

        /// <summary>
        /// Acento de advertencia / Enlace malicioso: Ámbar (#ffc24a).
        /// </summary>
        public static readonly Color WarningAmberColor = new Color(1.0f, 0.76f, 0.29f, 1.0f); // #ffc24a

        /// <summary>
        /// Texto de alta legibilidad en pantalla: Blanco puro / hielo (#edf2f7).
        /// </summary>
        public static readonly Color HighLegibilityTextColor = new Color(0.93f, 0.95f, 0.97f, 1.0f);

        #endregion

        #region Materiales PBR de Monitores (URP / Standard)

        private static Dictionary<string, Material> _cachedMaterials;

        public static Dictionary<string, Material> GetMonitorMaterials()
        {
            if (_cachedMaterials != null && _cachedMaterials.ContainsKey("Bezel") && _cachedMaterials["Bezel"] != null)
            {
                return _cachedMaterials;
            }

            _cachedMaterials = new Dictionary<string, Material>();

            // 1. Bisel y chasis negro mate
            _cachedMaterials["Bezel"] = CreatePbrMaterial("Mat_MonitorBezel_MatteBlack", BezelMatteBlackColor, 0.28f, 0.15f);

            // 2. Soporte y brazo metálico de acero oscuro
            _cachedMaterials["Stand"] = CreatePbrMaterial("Mat_MonitorStand_DarkSteel", StandDarkSteelColor, 0.65f, 0.80f);

            // 3. Panel de pantalla física anti-glare
            _cachedMaterials["DisplayPanel"] = CreatePbrMaterial("Mat_MonitorScreenPanel_Black", TerminalBgColor, 0.15f, 0.0f);

            // 4. LED verde de encendido del monitor (#38f28a emissive)
            _cachedMaterials["PowerLed"] = CreatePbrMaterial("Mat_MonitorPowerLed_Green",
                TerminalBorderGreen, 0.90f, 0.1f, TerminalBorderGreen * 3.0f);

            return _cachedMaterials;
        }

        public static Material CreatePbrMaterial(string name, Color color, float smoothness, float metallic, Color? emission = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Mobile/Diffuse");

            Material mat = new Material(shader)
            {
                name = name,
                color = color
            };

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (emission.HasValue && emission.Value != Color.black)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", emission.Value);
                    mat.EnableKeyword("_EMISSION");
                }
            }

            return mat;
        }

        #endregion

        #region 1. Estructura Física 3D del Monitor y Soporte de Escritorio

        /// <summary>
        /// Modela la estructura 3D física completa del monitor de 24 pulgadas:
        /// - Placa base de escritorio de perfil bajo con almohadillas antideslizantes.
        /// - Columna vertical ajustable con canal de pasacables.
        /// - Articulación VESA trasera con inclinación ergonómica hacia el jugador.
        /// - Chasis posterior con rejillas de ventilación.
        /// - Marco frontal ultra-delgado en negro mate y chin inferior con LED de estado #38f28a.
        /// - Canvas en World Space con marco #000000 y bordes #38f28a.
        /// </summary>
        public static GameObject BuildMonitor3D(Transform parent, string monitorId, string label, Vector3 localPos, Quaternion localRot)
        {
            Dictionary<string, Material> mats = GetMonitorMaterials();

            GameObject monRoot = new GameObject(monitorId);
            monRoot.transform.SetParent(parent, false);
            monRoot.transform.localPosition = localPos;
            monRoot.transform.localRotation = localRot;
            monRoot.layer = LayerMask.NameToLayer("Interactive");
            monRoot.tag = "Monitor";

            // -----------------------------------------------------------------
            // 1.1 Soporte Ajustable de Escritorio
            // -----------------------------------------------------------------
            GameObject standRoot = new GameObject("Soporte_Ajustable_Escritorio");
            standRoot.transform.SetParent(monRoot.transform, false);

            // Placa base de bajo perfil (0.24m x 0.015m x 0.18m)
            CreateBox("Placa_Base_Escritorio", standRoot.transform,
                new Vector3(0f, 0.012f, 0f), new Vector3(0.24f, 0.015f, 0.18f), mats["Stand"]);

            // Bisel / Chaflán de la base
            CreateBox("Bisel_Base", standRoot.transform,
                new Vector3(0f, 0.018f, 0f), new Vector3(0.22f, 0.008f, 0.16f), mats["Bezel"]);

            // Columna vertical telescópica (0.045m x 0.32m x 0.035m)
            CreateBox("Columna_Vertical_Elevacion", standRoot.transform,
                new Vector3(0f, 0.16f, 0.02f), new Vector3(0.045f, 0.31f, 0.035f), mats["Stand"]);

            // Pasacables en la columna
            CreateCylinder("Pasacables_Columna", standRoot.transform,
                new Vector3(0f, 0.10f, 0.02f), new Vector3(0.025f, 0.02f, 0.025f), mats["Bezel"]);

            // -----------------------------------------------------------------
            // 1.2 Articulación VESA y Cabeza del Monitor
            // -----------------------------------------------------------------
            GameObject headRoot = new GameObject("Cabeza_Pantalla_Monitor");
            headRoot.transform.SetParent(monRoot.transform, false);
            // Altura central a Y = 0.32m, con inclinación ergonómica de 5° mirando hacia arriba
            headRoot.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            headRoot.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);

            // Soporte articulado VESA
            CreateBox("Soporte_Articulacion_VESA", headRoot.transform,
                new Vector3(0f, 0f, 0.035f), new Vector3(0.09f, 0.09f, 0.035f), mats["Stand"]);

            // Chasis posterior del monitor (0.58m ancho x 0.35m alto x 0.038m fondo)
            CreateBox("Chasis_Posterior", headRoot.transform,
                new Vector3(0f, 0f, 0.012f), new Vector3(0.58f, 0.35f, 0.038f), mats["Bezel"]);

            // Rejilla de ventilación superior
            CreateBox("Rejilla_Ventilacion_Top", headRoot.transform,
                new Vector3(0f, 0.165f, 0.01f), new Vector3(0.48f, 0.015f, 0.025f), mats["Stand"]);

            // Marco / Bisel frontal ultra-delgado en color negro mate (Pantalla 24" 16:9)
            // Bordes superior, izquierdo y derecho de 8mm de grosor
            CreateBox("Bisel_Superior", headRoot.transform,
                new Vector3(0f, 0.17f, -0.012f), new Vector3(0.57f, 0.014f, 0.012f), mats["Bezel"]);
            CreateBox("Bisel_Lateral_Izq", headRoot.transform,
                new Vector3(-0.28f, 0f, -0.012f), new Vector3(0.014f, 0.34f, 0.012f), mats["Bezel"]);
            CreateBox("Bisel_Lateral_Der", headRoot.transform,
                new Vector3(0.28f, 0f, -0.012f), new Vector3(0.014f, 0.34f, 0.012f), mats["Bezel"]);

            // Chin / Bisel inferior ligeramente más ancho con espacio para el logo
            CreateBox("Bisel_Inferior_Chin", headRoot.transform,
                new Vector3(0f, -0.165f, -0.012f), new Vector3(0.57f, 0.024f, 0.012f), mats["Bezel"]);

            // LED verde de encendido del monitor en la esquina inferior derecha (#38f28a)
            GameObject pwrLed = CreateSphere("LED_Power_Monitor", headRoot.transform,
                new Vector3(0.26f, -0.165f, -0.019f), new Vector3(0.007f, 0.007f, 0.007f), mats["PowerLed"]);

            // Superficie física interior de la pantalla en negro antirreflejo
            CreateBox("Panel_Display_Fisico", headRoot.transform,
                new Vector3(0f, 0.003f, -0.008f), new Vector3(0.55f, 0.325f, 0.005f), mats["DisplayPanel"]);

            // -----------------------------------------------------------------
            // 1.3 Canvas en World Space con Marco #000000 y Bordes #38f28a
            // -----------------------------------------------------------------
            Canvas canvas = BuildTerminalWorldSpaceCanvas(monRoot.transform, monitorId);

            // 1.3.1 Luz sutil de brillo interactivo de pantalla proyectada hacia el escritorio
            GameObject glowObj = new GameObject("Luz_ScreenGlow");
            glowObj.transform.SetParent(headRoot.transform, false);
            glowObj.transform.localPosition = new Vector3(0f, 0f, -0.15f);
            Light glowLight = glowObj.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.range = 1.5f;
            glowLight.intensity = 0.65f;
            glowLight.color = (monitorId == "Monitor_01") ? new Color(0.40f, 0.85f, 1.0f) :
                              (monitorId == "Monitor_02") ? new Color(0.35f, 0.95f, 0.65f) :
                              new Color(0.30f, 0.70f, 1.0f);

            // -----------------------------------------------------------------
            // 1.4 Colisionador y Componentes VR para Cardboard
            // -----------------------------------------------------------------
            BoxCollider col = monRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.32f, 0f);
            col.size = new Vector3(0.58f, 0.36f, 0.15f);

            GazeInteractable gaze = monRoot.AddComponent<GazeInteractable>();
            gaze.interactableLabel = label;

            // Controlador interactivo oficial del monitor
            InteractiveMonitorController monitorCtrl = monRoot.AddComponent<InteractiveMonitorController>();
#if UNITY_EDITOR
            SerializedObject sCtrl = new SerializedObject(monitorCtrl);
            sCtrl.FindProperty("monitorId").stringValue = monitorId;
            sCtrl.FindProperty("worldSpaceScreenCanvas").objectReferenceValue = canvas;
            Transform bgTrans = canvas.transform.Find("Fondo_Terminal_BordeVerde");
            if (bgTrans != null)
            {
                sCtrl.FindProperty("statusBannerBackground").objectReferenceValue = bgTrans.GetComponent<Image>();
            }
            sCtrl.ApplyModifiedProperties();
#endif

            return monRoot;
        }

        #endregion

        #region 2. Canvas en World Space y Marco Terminal (#000000 con Bordes #38f28a)

        /// <summary>
        /// Crea el Canvas en World Space perfectamente alineado frente a la pantalla física del monitor.
        /// Configura el fondo negro puro (#000000) y el marco de líneas perimetrales verde neón (#38f28a).
        /// Alta densidad 800x480 con scale 0.0006875 para máxima legibilidad en VR Cardboard.
        /// </summary>
        public static Canvas BuildTerminalWorldSpaceCanvas(Transform parent, string monitorId, Vector3? localPos = null, Quaternion? localRot = null, Vector2? sizeDelta = null, Vector3? scale = null)
        {
            GameObject canvasObj = new GameObject("Canvas_Pantalla");
            canvasObj.transform.SetParent(parent, false);
            // Colocado frente al chasis a la altura visual de la pantalla
            canvasObj.transform.localPosition = localPos ?? new Vector3(0f, 0.32f, -0.018f);
            canvasObj.transform.localRotation = localRot ?? Quaternion.identity;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            Vector2 resolvedSize = sizeDelta ?? new Vector2(720f, 720f);
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = resolvedSize;
            rect.localScale = scale ?? new Vector3(0.00050f, 0.00050f, 0.00050f);

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // 2.1 Fondo Negro Puro de Terminal (#000000)
            GameObject bgObj = new GameObject("Fondo_Terminal_BordeVerde");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = TerminalBgColor; // #000000
            bgImg.raycastTarget = false;

            // 2.2 Marco / Bordes Perimetrales Verde Neón (#38f28a) ajustados al tamaño real del monitor
            BuildTerminalBorders(bgObj.transform, resolvedSize);

            // 2.3 Barra Superior de Estado de la Terminal
            BuildTerminalHeaderBar(bgObj.transform, monitorId, resolvedSize);

            return canvas;
        }

        /// <summary>
        /// Genera las 4 líneas de marco verde neón (#38f28a) en el perímetro exacto del Canvas del monitor.
        /// </summary>
        private static void BuildTerminalBorders(Transform parent, Vector2 canvasSize)
        {
            float halfW = canvasSize.x * 0.5f;
            float halfH = canvasSize.y * 0.5f;

            // Borde Superior
            CreateUiLine("Borde_Neon_Top", parent,
                new Vector2(0f, halfH - 3f), new Vector2(canvasSize.x - 4f, 6f), TerminalBorderGreen);

            // Borde Inferior
            CreateUiLine("Borde_Neon_Bottom", parent,
                new Vector2(0f, -(halfH - 3f)), new Vector2(canvasSize.x - 4f, 6f), TerminalBorderGreen);

            // Borde Izquierdo
            CreateUiLine("Borde_Neon_Left", parent,
                new Vector2(-(halfW - 3f), 0f), new Vector2(6f, canvasSize.y - 8f), TerminalBorderGreen);

            // Borde Derecho
            CreateUiLine("Borde_Neon_Right", parent,
                new Vector2(halfW - 3f, 0f), new Vector2(6f, canvasSize.y - 8f), TerminalBorderGreen);

            // Línea separadora bajo el encabezado
            CreateUiLine("Separador_Header", parent,
                new Vector2(0f, halfH - 72f), new Vector2(canvasSize.x - 28f, 3f), new Color(0.22f, 0.95f, 0.54f, 0.55f));
        }

        /// <summary>
        /// Construye el encabezado institucional bancario en la parte superior de la pantalla.
        /// </summary>
        private static void BuildTerminalHeaderBar(Transform parent, string monitorId, Vector2 canvasSize)
        {
            GameObject headerObj = new GameObject("Header_Terminal_BancoAndino");
            headerObj.transform.SetParent(parent, false);
            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, canvasSize.y * 0.5f - 38f);
            hRect.sizeDelta = new Vector2(canvasSize.x - 44f, 48f);

            TextMeshProUGUI tmp = headerObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 20f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = HighLegibilityTextColor;

            string stationTitle = monitorId == "Monitor_01" ? "PC 1 · CORREO" :
                                 monitorId == "Monitor_02" ? "PC 2 · SWIFT" :
                                 "PC 3 · SIEM";

            tmp.text = $"<b><color=#38F28A>BANCO ANDINO</color></b>  |  <color=#E2E8F0>{stationTitle}</color>";
        }

        #endregion

        #region 3. Monitor 1: Buzón de Correo de Rasel Ahlam, Hotspots de Phishing y Botones

        /// <summary>
        /// Construye la interfaz completa de alta legibilidad en VR para Monitor_01:
        /// - Bandeja de entrada con correo malicioso de "Rasel Ahlam" del 29 de enero de 2015.
        /// - Contador visual de señales detectadas (0/3) con indicador de progreso.
        /// - 3 Hotspots interactivos de phishing (Remitente no institucional, Urgencia desmedida y Enlace con .scr.zip).
        /// - Botones interactivos 'ABRIR ENLACE' (Modo Ataque) y 'REPORTAR PHISHING' (Modo Defensa).
        /// - Enlaza los componentes PhishingInspector y MailQuarantine.
        /// </summary>
        public static GameObject BuildMonitor1EmailInbox(GameObject monitor01, PhishingInspector phishingInspector = null, MailQuarantine mailQuarantine = null)
        {
            Transform canvasTransform = monitor01.transform.Find("Cabeza_Pantalla_Monitor/Canvas_Pantalla");
            if (canvasTransform == null)
            {
                canvasTransform = monitor01.transform.Find("Canvas_Pantalla");
            }

            if (canvasTransform == null)
            {
                Debug.LogError($"{LogPrefix} No se encontró 'Canvas_Pantalla' en {monitor01.name}");
                return null;
            }

            if (phishingInspector == null)
            {
                phishingInspector = monitor01.GetComponent<PhishingInspector>() ?? monitor01.AddComponent<PhishingInspector>();
            }

            if (mailQuarantine == null)
            {
                mailQuarantine = monitor01.GetComponent<MailQuarantine>() ?? monitor01.AddComponent<MailQuarantine>();
            }

            // 3.1 Contenedor Principal: Panel_Correo
            GameObject panelCorreo = new GameObject("Panel_Correo");
            panelCorreo.transform.SetParent(canvasTransform, false);
            RectTransform rect = panelCorreo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            // 3.2 Contador de Señales Detectadas (0/3)
            GameObject counterObj = new GameObject("Txt_SeñalesContador");
            counterObj.transform.SetParent(panelCorreo.transform, false);
            RectTransform counterRect = counterObj.AddComponent<RectTransform>();
            counterRect.anchoredPosition = new Vector2(0f, 244f);
            counterRect.sizeDelta = new Vector2(660f, 46f);

            TextMeshProUGUI counterTmp = counterObj.AddComponent<TextMeshProUGUI>();
            counterTmp.text = "Pistas: <b><color=#FFC24A>0/3</color></b>";
            counterTmp.fontSize = 23f;
            counterTmp.alignment = TextAlignmentOptions.Center;
            counterTmp.color = HighLegibilityTextColor;

            // 3.3 Las 3 Zonas de Phishing (Hotspots RF-03) ajustadas al panel 720x720
            GameObject remObj = CreatePhishingHotspot(
                "Hotspot_Remitente", panelCorreo.transform,
                new Vector2(0f, 155f), new Vector2(660f, 86f),
                "<b>1. De:</b> <color=#FFC24A>career-applicants-bd.com</color>",
                "Signal_Remitente",
                "Dominio externo",
                phishingInspector);

            GameObject urgObj = CreatePhishingHotspot(
                "Hotspot_Urgencia", panelCorreo.transform,
                new Vector2(0f, 52f), new Vector2(660f, 86f),
                "<b>2. Asunto:</b> <color=#FF5A4E>URGENTE</color> · Revision hoy",
                "Signal_Urgencia",
                "Urgencia artificial",
                phishingInspector);

            GameObject linkObj = CreatePhishingHotspot(
                "Hotspot_Enlace", panelCorreo.transform,
                new Vector2(0f, -51f), new Vector2(660f, 86f),
                "<b>3. Adjunto:</b> <color=#FFC24A>CV_2015.scr.zip</color>",
                "Signal_Enlace",
                "Ejecutable .scr oculto",
                phishingInspector);

            GameObject bodyObj = new GameObject("Txt_CuerpoCorreoExtracto");
            bodyObj.transform.SetParent(panelCorreo.transform, false);
            RectTransform bRect = bodyObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(0f, -135f);
            bRect.sizeDelta = new Vector2(660f, 48f);
            TextMeshProUGUI bodyTmp = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyTmp.text = "<color=#CBD5E1><i>\"Revise mi CV adjunto hoy.\"</i></color>";
            bodyTmp.fontSize = 19f;
            bodyTmp.alignment = TextAlignmentOptions.Center;

            // 3.4 Botones Interactivos de Decisión por Mirada (grandes y fáciles de seleccionar)
            GameObject btnAtaqueObj = CreateWorldGazeButton(
                "Btn_AbrirEnlaceAtaque", panelCorreo.transform,
                new Vector2(-166f, -248f), new Vector2(312f, 108f),
                "ABRIR ENLACE", new Color(0.45f, 0.12f, 0.12f, 0.96f), AlertRedColor);

            Button btnAtaque = btnAtaqueObj.GetComponent<Button>();

            GameObject btnDefensaObj = CreateWorldGazeButton(
                "Btn_ReportarPhishing", panelCorreo.transform,
                new Vector2(166f, -248f), new Vector2(312f, 108f),
                "REPORTAR PHISHING", new Color(0.08f, 0.30f, 0.18f, 0.96f), TerminalBorderGreen);

            Button btnDefensa = btnDefensaObj.GetComponent<Button>();

            if (btnAtaque != null) btnAtaque.onClick.AddListener(phishingInspector.ExecuteOpenLinkAttackSequence);
            if (btnDefensa != null) btnDefensa.onClick.AddListener(mailQuarantine.TriggerQuarantineDefense);

            // 3.5 Serializar Referencias en PhishingInspector
#if UNITY_EDITOR
            SerializedObject sInsp = new SerializedObject(phishingInspector);
            sInsp.FindProperty("signalsCounterText").objectReferenceValue = counterTmp;
            sInsp.FindProperty("openLinkCvButton").objectReferenceValue = btnAtaque;
            sInsp.FindProperty("reportPhishingButton").objectReferenceValue = btnDefensa;
            sInsp.FindProperty("senderHotspot").objectReferenceValue = remObj.GetComponent<PhishingSignalHotspot>();
            sInsp.FindProperty("urgencyHotspot").objectReferenceValue = urgObj.GetComponent<PhishingSignalHotspot>();
            sInsp.FindProperty("linkHotspot").objectReferenceValue = linkObj.GetComponent<PhishingSignalHotspot>();
            sInsp.FindProperty("mailQuarantine").objectReferenceValue = mailQuarantine;
            sInsp.FindProperty("monitor01Controller").objectReferenceValue = monitor01.GetComponent<InteractiveMonitorController>();
            sInsp.ApplyModifiedProperties();
#endif

            return panelCorreo;
        }

        /// <summary>
        /// Crea una zona interactiva (Hotspot) para una de las 3 señales de phishing en capa Interactive.
        /// </summary>
        public static GameObject CreatePhishingHotspot(string name, Transform parent, Vector2 pos, Vector2 size,
            string labelText, string signalId, string description, PhishingInspector inspector)
        {
            GameObject hotspotObj = new GameObject(name);
            hotspotObj.transform.SetParent(parent, false);
            hotspotObj.layer = LayerMask.NameToLayer("Interactive");

            RectTransform rect = hotspotObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            // Fondo oscuro legible acorde a Monitor 1
            Image bg = hotspotObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.12f, 0.18f, 0.90f);

            // Línea de borde izquierda indicadora
            CreateUiLine("Borde_Hotspot", hotspotObj.transform,
                new Vector2(-size.x * 0.5f + 2f, 0f), new Vector2(4f, size.y - 4f), TerminalBorderGreen);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(hotspotObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(18f, 2f);
            txtRect.offsetMax = new Vector2(-140f, -2f);

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 17f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = HighLegibilityTextColor;
            tmp.raycastTarget = false;

            // Etiqueta dedicada de señal detectada (separada del texto principal)
            GameObject badgeObj = new GameObject("Badge_Detectada");
            badgeObj.transform.SetParent(hotspotObj.transform, false);
            RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 0f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 0.5f);
            badgeRect.sizeDelta = new Vector2(124f, size.y - 8f);
            badgeRect.anchoredPosition = new Vector2(-10f, 0f);

            TextMeshProUGUI badgeTmp = badgeObj.AddComponent<TextMeshProUGUI>();
            badgeTmp.text = string.Empty;
            badgeTmp.fontSize = 11.5f;
            badgeTmp.alignment = TextAlignmentOptions.MidlineRight;
            badgeTmp.color = new Color(1f, 0.62f, 0.26f, 1f);
            badgeTmp.raycastTarget = false;
            badgeObj.SetActive(false);

            // BoxCollider en unidades de Canvas (~2.4cm físicos con escala 0.0005) para retícula Cardboard VR
            BoxCollider col = hotspotObj.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 48f);

            GazeInteractable gaze = hotspotObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = description;

            PhishingSignalHotspot hotspot = hotspotObj.AddComponent<PhishingSignalHotspot>();
#if UNITY_EDITOR
            SerializedObject sHotspot = new SerializedObject(hotspot);
            sHotspot.FindProperty("signalId").stringValue = signalId;
            sHotspot.FindProperty("signalTitle").stringValue = description;
            sHotspot.FindProperty("hotspotBorderImage").objectReferenceValue = bg;
            sHotspot.FindProperty("statusBadgeText").objectReferenceValue = badgeTmp;
            sHotspot.ApplyModifiedProperties();
#endif

            return hotspotObj;
        }

        #endregion

        #region 4. Displays de Monitor 2 (Comercio Exterior) y Monitor 3 (Consola SIEM)

        /// <summary>
        /// Configura el display operativo de Monitor_02 (Analista de Comercio Exterior - Operaciones SWIFT).
        /// Alta resolución 800x480 con tipografía nítida para VR.
        /// </summary>
        public static void BuildMonitor2TradeFinanceDisplay(GameObject monitor02)
        {
            Transform canvasTransform = monitor02.transform.Find("Cabeza_Pantalla_Monitor/Canvas_Pantalla") ??
                                        monitor02.transform.Find("Canvas_Pantalla");
            if (canvasTransform == null) return;

            GameObject panel = new GameObject("Panel_ComercioExterior");
            panel.transform.SetParent(canvasTransform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            GameObject cardBg = CreateUiLine("Card_TablaSwift", panel.transform,
                new Vector2(0f, 18f), new Vector2(660f, 380f), new Color(0.06f, 0.11f, 0.17f, 0.92f));
            CreateUiLine("Card_TablaSwift_Borde", cardBg.transform,
                new Vector2(0f, 188f), new Vector2(660f, 4f), TerminalBorderGreen);

            GameObject txtTable = new GameObject("Txt_OperacionesSwift");
            txtTable.transform.SetParent(cardBg.transform, false);
            RectTransform tRect = txtTable.AddComponent<RectTransform>();
            tRect.anchoredPosition = Vector2.zero;
            tRect.sizeDelta = new Vector2(600f, 320f);

            TextMeshProUGUI tmp = txtTable.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 26f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = HighLegibilityTextColor;
            tmp.text = "<color=#38F28A><b>TRANSFERENCIAS SWIFT</b></color>\n\n" +
                       "OP #9481 NY    $1.25M   <color=#38F28A><b>OK</b></color>\n" +
                       "OP #9482 LON   $0.48M   <color=#38F28A><b>OK</b></color>\n" +
                       "<color=#FFC24A><b>OP #9483 MKT  $81.0M   ESPERA</b></color>";

            GameObject alertPill = CreateUiLine("Pill_AlertaSwift", panel.transform,
                new Vector2(0f, -242f), new Vector2(660f, 92f), new Color(0.30f, 0.19f, 0.04f, 0.92f));
            CreateUiLine("Pill_AlertaSwift_Borde", alertPill.transform,
                new Vector2(0f, 44f), new Vector2(660f, 4f), new Color(1f, 0.76f, 0.29f, 0.85f));

            GameObject alertTxtObj = new GameObject("Txt_AlertaDestino");
            alertTxtObj.transform.SetParent(alertPill.transform, false);
            RectTransform aRect = alertTxtObj.AddComponent<RectTransform>();
            aRect.sizeDelta = new Vector2(620f, 70f);
            TextMeshProUGUI alertTmp = alertTxtObj.AddComponent<TextMeshProUGUI>();
            alertTmp.fontSize = 21f;
            alertTmp.alignment = TextAlignmentOptions.Center;
            alertTmp.color = new Color(1f, 0.88f, 0.56f, 1f);
            alertTmp.text = "<b>Destino anomalo: RCBC Makati (IP .10.16)</b>";
        }

        /// <summary>
        /// Configura el display de Monitor_03 (Consola SIEM de Monitoreo de Red y Defensas).
        /// Alta resolución 800x480 con registros forenses legibles y botón interactivo grande.
        /// </summary>
        public static void BuildMonitor3SiemDisplay(GameObject monitor03, AccessLogTerminal siemTerminal = null)
        {
            Transform canvasTransform = monitor03.transform.Find("Cabeza_Pantalla_Monitor/Canvas_Pantalla") ??
                                        monitor03.transform.Find("Canvas_Pantalla");
            if (canvasTransform == null) return;

            GameObject panel = new GameObject("Panel_SiemLogAccesos");
            panel.transform.SetParent(canvasTransform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            GameObject cardBg = CreateUiLine("Card_SiemEventos", panel.transform,
                new Vector2(0f, 44f), new Vector2(660f, 380f), new Color(0.06f, 0.11f, 0.17f, 0.92f));
            CreateUiLine("Card_SiemEventos_Borde", cardBg.transform,
                new Vector2(0f, 188f), new Vector2(660f, 4f), TerminalBorderGreen);

            GameObject txtLog = new GameObject("Txt_LogTerminalSiem");
            txtLog.transform.SetParent(cardBg.transform, false);
            RectTransform lRect = txtLog.AddComponent<RectTransform>();
            lRect.anchoredPosition = Vector2.zero;
            lRect.sizeDelta = new Vector2(600f, 320f);

            TextMeshProUGUI tmp = txtLog.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = HighLegibilityTextColor;
            tmp.text = "<color=#38F28A><b>ALERTAS SIEM · 29 ENE</b></color>\n\n" +
                       "<color=#94A3B8>09:14</color> Phishing -> 16 buzones\n" +
                       "<color=#FFC24A>09:18</color> Descarga CV_2015.scr.zip\n" +
                       "<color=#FF5A4E><b>09:18 ALERTA: Dropper activo</b></color>";

            GameObject logBtnObj = CreateWorldGazeButton(
                "Btn_RegistrarLogEvidencia", panel.transform,
                new Vector2(0f, -238f), new Vector2(660f, 108f),
                "REGISTRAR LOG SIEM", new Color(0.10f, 0.28f, 0.44f, 0.96f), TerminalBorderGreen);

            if (siemTerminal != null)
            {
#if UNITY_EDITOR
                SerializedObject sSiem = new SerializedObject(siemTerminal);
                sSiem.FindProperty("evidenceId").stringValue = "Evidencia_LogAccesos";
                sSiem.FindProperty("evidenceTitle").stringValue = "Log SIEM de Red";
                sSiem.FindProperty("registerLogEvidenceButton").objectReferenceValue = logBtnObj.GetComponent<Button>();
                sSiem.FindProperty("registerButtonLabelText").objectReferenceValue = logBtnObj.GetComponentInChildren<TextMeshProUGUI>();
                sSiem.FindProperty("registerButtonBgImage").objectReferenceValue = logBtnObj.GetComponent<Image>();
                sSiem.ApplyModifiedProperties();
#endif
            }
        }

        #endregion

        #region Utilidades de UI y Primitivas

        private static GameObject CreateUiLine(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            GameObject line = new GameObject(name);
            line.transform.SetParent(parent, false);
            RectTransform rect = line.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Image img = line.AddComponent<Image>();
            img.color = color;
            return line;
        }

        public static GameObject CreateWorldGazeButton(string name, Transform parent, Vector2 pos, Vector2 size,
            string text, Color bgColor, Color borderColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.layer = LayerMask.NameToLayer("Interactive");

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            // Borde superior o lateral indicador
            CreateUiLine("Borde_Boton", btnObj.transform,
                new Vector2(0f, size.y * 0.5f - 1.5f), new Vector2(size.x, 3.0f), borderColor);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.colorMultiplier = 1.18f;
            cb.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.highlightedColor = Color.white;
            cb.selectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.pressedColor = new Color(0.68f, 0.68f, 0.68f, 1f);
            btn.colors = cb;

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            AndesTypographyTheme.StyleButtonLabel(tmp, Color.white, 19f);

            // BoxCollider en unidades locales de Canvas (~2.4cm físicos) para retícula Cardboard VR
            BoxCollider col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 48f);

            GazeInteractable gaze = btnObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = text;

            btnObj.AddComponent<WorldSpaceUiGazeButton>();
            btnObj.AddComponent<HolographicCardHover>().Configure(1.045f, 14f);

            return btnObj;
        }

        public static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        public static GameObject CreateCylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        public static GameObject CreateSphere(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        #endregion
    }
}
