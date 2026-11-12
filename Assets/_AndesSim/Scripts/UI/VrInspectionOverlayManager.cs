using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Scenario01;
using AndesSim.VR;

namespace AndesSim.UI
{
    /// <summary>
    /// Administrador del Sistema de Inspección y Zoom VR (RF-01 a RF-05).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// 
    /// Permite al jugador ampliar a pantalla completa en World Space:
    /// 1. El correo de Rasel Ahlam con sus 3 señales de phishing interactivas.
    /// 2. El Post-It y libreta de credenciales bancarias de Carla Mendoza.
    /// 3. La terminal de transferencias internacionales SWIFT ($951M).
    /// 4. La consola SIEM con los logs de conexión hacia la IP 185.130.44.82.
    /// </summary>
    public class VrInspectionOverlayManager : MonoBehaviour
    {
        public static VrInspectionOverlayManager I
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<VrInspectionOverlayManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("VrInspectionOverlayManager");
                        _instance = go.AddComponent<VrInspectionOverlayManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private static VrInspectionOverlayManager _instance;

        private const string LogPrefix = "[ANDES-SIM - InspectionOverlay]";

        private static readonly Color DarkGlassBgColor = new Color(0.02f, 0.05f, 0.09f, 1.0f);
        private static readonly Color CyanNeonAccent = new Color(0f, 0.95f, 1f, 1f);
        private static readonly Color GreenSuccessColor = new Color(0.22f, 0.95f, 0.54f, 1f);
        private static readonly Color WarningAmberColor = new Color(1f, 0.76f, 0.29f, 1f);
        private static readonly Color DangerRedColor = new Color(1f, 0.35f, 0.31f, 1f);

        [Header("--- Anclaje VR ---")]
        [SerializeField] private float distanceFromCamera = 1.05f;
        [SerializeField] private float smoothFollowSpeed = 8f;

        // Raíz del Canvas de Inspección
        private GameObject _overlayRoot;
        private Canvas _overlayCanvas;
        private RectTransform _mainPanelRect;
        private Camera _targetCamera;
        private bool _isOpen = false;

        // Contenedores dinámicos
        private TextMeshProUGUI _headerTitleText;
        private TextMeshProUGUI _subTitleText;
        private Transform _contentContainer;
        private GameObject _activeContentPanel;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureOverlayCanvas();
        }

        private void Start()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            }
        }

        private void Update()
        {
            if (!_isOpen || _overlayRoot == null) return;

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
                if (_targetCamera == null) return;
            }

            // El panel de inspección se mantiene ESTACIONARIO en el espacio 3D para permitir apuntar
            // libremente la retícula VR hacia los botones sin que el panel se desplace con la cabeza.
            // Si el jugador gira la cabeza más de 65 grados alejándose del panel, se cierra automáticamente.
            Vector3 dirToPanel = (_overlayRoot.transform.position - _targetCamera.transform.position).normalized;
            float angle = Vector3.Angle(_targetCamera.transform.forward, dirToPanel);
            if (angle > 65f)
            {
                CloseOverlay();
            }
        }

        /// <summary>
        /// Crea de forma procedural el Canvas World Space de inspección con estética de vidrio holográfico.
        /// </summary>
        private void EnsureOverlayCanvas()
        {
            if (_overlayRoot != null) return;

            _overlayRoot = new GameObject("VrInspectionOverlayCanvas");
            _overlayRoot.transform.SetParent(null); // Canvas independiente en World Space
            _overlayRoot.layer = LayerMask.NameToLayer("Interactive");

            _overlayCanvas = _overlayRoot.AddComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.WorldSpace;
            _overlayCanvas.sortingOrder = 5000;
            if (_targetCamera == null) _targetCamera = Camera.main;
            if (_targetCamera != null) _overlayCanvas.worldCamera = _targetCamera;

            CanvasScaler scaler = _overlayRoot.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f;

            _overlayRoot.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = _overlayRoot.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000f, 750f);
            canvasRect.localScale = Vector3.one * 0.0016f;

            // Panel Fondo Principal
            EnsureEventSystemExists();

            GameObject panelGO = new GameObject("InspectionFrame");
            panelGO.transform.SetParent(_overlayRoot.transform, false);
            _mainPanelRect = panelGO.AddComponent<RectTransform>();
            _mainPanelRect.anchorMin = Vector2.zero;
            _mainPanelRect.anchorMax = Vector2.one;
            _mainPanelRect.sizeDelta = Vector2.zero;

            Image bgImage = panelGO.AddComponent<Image>();
            bgImage.color = DarkGlassBgColor;

            // Borde estético superior
            GameObject topBar = new GameObject("TopGlowBar");
            topBar.transform.SetParent(panelGO.transform, false);
            RectTransform topBarRect = topBar.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.anchoredPosition = new Vector2(0f, -4f);
            topBarRect.sizeDelta = new Vector2(0f, 6f);
            Image topBarImg = topBar.AddComponent<Image>();
            topBarImg.color = CyanNeonAccent;

            // Título Principal
            GameObject titleGO = new GameObject("HeaderTitle");
            titleGO.transform.SetParent(panelGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 320f);
            titleRect.sizeDelta = new Vector2(920f, 45f);
            _headerTitleText = titleGO.AddComponent<TextMeshProUGUI>();
            _headerTitleText.fontSize = 24f;
            _headerTitleText.alignment = TextAlignmentOptions.Center;
            _headerTitleText.color = CyanNeonAccent;
            _headerTitleText.fontStyle = FontStyles.Bold;

            // Subtítulo
            GameObject subGO = new GameObject("SubTitle");
            subGO.transform.SetParent(panelGO.transform, false);
            RectTransform subRect = subGO.AddComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0f, 280f);
            subRect.sizeDelta = new Vector2(920f, 32f);
            _subTitleText = subGO.AddComponent<TextMeshProUGUI>();
            _subTitleText.fontSize = 15f;
            _subTitleText.alignment = TextAlignmentOptions.Center;
            _subTitleText.color = new Color(0.8f, 0.88f, 0.95f, 0.9f);

            // Contenedor de Contenido
            GameObject contentGO = new GameObject("DynamicContentContainer");
            contentGO.transform.SetParent(panelGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchoredPosition = new Vector2(0f, -10f);
            contentRect.sizeDelta = new Vector2(920f, 520f);
            _contentContainer = contentGO.transform;

            // Botón Superior Derecho: CERRAR INMEDIATO (X)
            CreateInspectionButton(
                "Btn_CerrarTopRight", panelGO.transform,
                new Vector2(430f, 325f), new Vector2(90f, 36f),
                "<b>[ X ]</b>",
                new Color(0.35f, 0.12f, 0.15f, 0.95f),
                Color.white,
                CloseOverlay
            );

            // Botón Inferior: CERRAR INSPECCIÓN
            GameObject closeBtnGO = CreateInspectionButton(
                "Btn_CerrarInspeccion", panelGO.transform,
                new Vector2(0f, -325f), new Vector2(380f, 50f),
                "<b>[ X CERRAR VISTA DE DETALLE ]</b>",
                new Color(0.18f, 0.22f, 0.32f, 0.96f),
                new Color(0.9f, 0.95f, 1f, 1f),
                CloseOverlay
            );

            _overlayRoot.SetActive(false);
            Debug.Log($"{LogPrefix} Canvas de Inspección VR inicializado con éxito.");
        }

        #region Vistas de Inspección Oficiales

        /// <summary>
        /// RF-02 y RF-03: Abre el correo de Rasel Ahlam en vista ampliada con las 3 señales resaltadas.
        /// </summary>
        public void OpenEmailInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b>BANCO ANDINO · BANDEJA DE CORREO CORPORATIVO</b>";
            _subTitleText.text = "Inspección Forense de Spear Phishing · Jueves 29 de Enero de 2015 (09:18 AM)";

            GameObject emailPanel = new GameObject("EmailContentPanel");
            emailPanel.transform.SetParent(_contentContainer, false);
            RectTransform eRect = emailPanel.AddComponent<RectTransform>();
            eRect.anchorMin = Vector2.zero;
            eRect.anchorMax = Vector2.one;
            eRect.sizeDelta = Vector2.zero;

            // Encabezado de correo limpio y de alta visibilidad VR
            CreateLabel("Lbl_From", emailPanel.transform, new Vector2(0f, 210f), new Vector2(900f, 32f),
                "<b>De:</b> Rasel Ahlam <color=#FF5A4E>&lt;rasel.ahlam@career-applicants-bd.com&gt;</color>", 16.5f, TextAlignmentOptions.Left, Color.white);

            CreateLabel("Lbl_To", emailPanel.transform, new Vector2(0f, 180f), new Vector2(900f, 28f),
                "<b>Para:</b> Carla Mendoza &lt;c.mendoza@bancoandino.pe&gt; (Operadora SWIFT)", 15f, TextAlignmentOptions.Left, new Color(0.75f, 0.85f, 0.95f));

            CreateLabel("Lbl_Subject", emailPanel.transform, new Vector2(0f, 145f), new Vector2(900f, 32f),
                "<b>Asunto:</b> <color=#FF5A4E>URGENTE: Postulación CV Especialista SWIFT</color>", 16.5f, TextAlignmentOptions.Left, Color.white);

            // 3 Señales Forenses Claras con tipografía ampliada
            CreateSignalButton("Btn_Signal1", emailPanel.transform, new Vector2(-290f, 85f), new Vector2(280f, 65f),
                "<b>1. REMITENTE FALSO</b>\n<size=12><color=#FFC24A>Dominio Externo no SPF</color></size>",
                PhishingInspector.SignalRemitenteId);

            CreateSignalButton("Btn_Signal2", emailPanel.transform, new Vector2(0f, 85f), new Vector2(280f, 65f),
                "<b>2. URGENCIA FALSA</b>\n<size=12><color=#FFC24A>Presión Psicológica</color></size>",
                PhishingInspector.SignalUrgenciaId);

            CreateSignalButton("Btn_Signal3", emailPanel.transform, new Vector2(290f, 85f), new Vector2(280f, 65f),
                "<b>3. ADJUNTO MALICIOSO</b>\n<size=12><color=#FFC24A>Doble extensión (.scr.zip)</color></size>",
                PhishingInspector.SignalEnlaceId);

            // Cuerpo del correo directo y al grano
            GameObject bodyBox = new GameObject("EmailBodyBox");
            bodyBox.transform.SetParent(emailPanel.transform, false);
            RectTransform bbRect = bodyBox.AddComponent<RectTransform>();
            bbRect.anchoredPosition = new Vector2(0f, -25f);
            bbRect.sizeDelta = new Vector2(900f, 130f);
            Image bodyBg = bodyBox.AddComponent<Image>();
            bodyBg.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);

            CreateLabel("Txt_EmailBody", bodyBox.transform, new Vector2(0f, 25f), new Vector2(870f, 65f),
                "<i>\"Estimada Carla: Adjunto remito mi CV comprimido para la vacante urgente en el área de pagos internacionales SWIFT.\"</i>",
                15.5f, TextAlignmentOptions.TopLeft, new Color(0.92f, 0.96f, 1f));

            // Enlace de descarga interactivo
            CreateInspectionButton(
                "Btn_AttachmentLink", bodyBox.transform,
                new Vector2(0f, -28f), new Vector2(760f, 44f),
                "<b><color=#00F2FE>📎 <u>http://185.130.44.82/Rasel_Ahlam_CV_2015.scr.zip</u></color></b>\n<size=11><color=#FFC24A>▲ Clic con retícula para descargar archivo</color></size>",
                new Color(0.12f, 0.18f, 0.28f, 0.98f),
                Color.white,
                TriggerAttackInfectionSequence
            );

            // Botones de Decisión Claros y Grandes (Ataque vs Defensa)
            CreateInspectionButton(
                "Btn_AttackOpenLink", emailPanel.transform,
                new Vector2(-220f, -150f), new Vector2(400f, 60f),
                "<b><size=15><color=#FF5A4E>[ ⚠ ABRIR ENLACE (ATAQUE) ]</color></size></b>\n<size=12>Recrear infección histórica de 2016</size>",
                new Color(0.5f, 0.12f, 0.12f, 0.98f),
                new Color(1f, 0.4f, 0.35f, 1f),
                TriggerAttackInfectionSequence
            );

            CreateInspectionButton(
                "Btn_DefenseReport", emailPanel.transform,
                new Vector2(220f, -150f), new Vector2(400f, 60f),
                "<b><size=15><color=#38F28A>[ 🛡 AISLAR EN SANDBOX (DEFENSA) ]</color></size></b>\n<size=12>Contener amenaza y proteger fondos</size>",
                new Color(0.08f, 0.35f, 0.2f, 0.98f),
                new Color(0.35f, 1f, 0.6f, 1f),
                () =>
                {
                    if (MailQuarantine.I != null)
                    {
                        MailQuarantine.I.TriggerQuarantineDefense();
                    }
                    else if (PhishingInspector.I != null)
                    {
                        PhishingInspector.I.ExecuteReportPhishingDefenseSequence();
                    }
                    OpenQuarantineInspection();
                }
            );

            _activeContentPanel = emailPanel;
            ShowOverlay();
        }

        /// <summary>
        /// MODO DEFENSA (RF-04): El correo NUNCA llega a abrirse.
        /// La bandeja de entrada es reemplazada visualmente por el panel de cuarentena (simulando filtro Sandbox).
        /// </summary>
        public void OpenQuarantineInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            if (AndesSim.Scenario01.InfectionSpreadVisualizer.I != null)
            {
                AndesSim.Scenario01.InfectionSpreadVisualizer.I.ShowDefenseContainmentSign();
            }

            _headerTitleText.text = "<b><color=#38F28A>CONSOLA DE CUARENTENA Y FILTRO SANDBOX · BANCO ANDINO</color></b>";
            _subTitleText.text = "Protección Perimetral del SOC · Correo Malicioso Aislado Preventivamente";

            GameObject qPanel = new GameObject("QuarantineContentPanel");
            qPanel.transform.SetParent(_contentContainer, false);
            RectTransform qRect = qPanel.AddComponent<RectTransform>();
            qRect.anchorMin = Vector2.zero;
            qRect.anchorMax = Vector2.one;
            qRect.sizeDelta = Vector2.zero;

            GameObject boxGO = new GameObject("SandboxLogBox");
            boxGO.transform.SetParent(qPanel.transform, false);
            RectTransform boxRect = boxGO.AddComponent<RectTransform>();
            boxRect.anchoredPosition = new Vector2(0f, 35f);
            boxRect.sizeDelta = new Vector2(900f, 360f);
            Image boxBg = boxGO.AddComponent<Image>();
            boxBg.color = new Color(0.04f, 0.08f, 0.12f, 0.98f);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b><size=17><color=#38F28A>[REPORTE FORENSE DE MITIGACIÓN SOC — BANCO ANDINO]</color></size></b>");
            sb.AppendLine("────────────────────────────────────────────────────────────────────────────");
            sb.AppendLine("• <b>FILTRO SANDBOX CUCKOO:</b> Adjunto <i>Rasel_Ahlam_CV.scr</i> detonado y aislado preventivamente.");
            sb.AppendLine("• <b>ANÁLISIS SPF/DKIM:</b> Dominio atacante <i>career-applicants-bd.com</i> bloqueado por suplantación.");
            sb.AppendLine("• <b>REGLA FIREWALL:</b> Tráfico hacia Servidor C2 <b>185.130.44.82:443</b> cortado de inmediato.");
            sb.AppendLine("• <b>SEGMENTACIÓN SWIFT:</b> Terminales aisladas en VLAN 100 con autenticación MFA activa.");
            sb.AppendLine("────────────────────────────────────────────────────────────────────────────");
            sb.AppendLine("<size=16><color=#38F28A><b>✓ CONCLUSIÓN TÉCNICA: Intrusión evitada al 100% · Cero pérdidas financieras.</b></color></size>");

            CreateLabel("Txt_QuarantineLog", boxGO.transform, Vector2.zero, new Vector2(860f, 340f),
                sb.ToString(), 15.5f, TextAlignmentOptions.TopLeft, Color.white);

            CreateInspectionButton(
                "Btn_ConfirmDefense", qPanel.transform,
                new Vector2(-190f, -180f), new Vector2(370f, 54f),
                "<b><color=#38F28A>[ FINALIZAR CON ÉXITO ]</color></b>\n<size=11>Confirmar contención y terminar</size>",
                new Color(0.08f, 0.35f, 0.18f, 0.98f),
                Color.white,
                () =>
                {
                    CloseOverlay();
                    if (MailQuarantine.I != null)
                    {
                        MailQuarantine.I.ConfirmContainmentAndEndScenario();
                    }
                    else if (GameManager.I != null)
                    {
                        GameManager.I.ApplyDefense("Defensa_Sandbox");
                        GameManager.I.ApplyDefense("Defensa_BotonReportar");
                        GameManager.I.ApplyDefense("Defensa_MFA");
                        GameManager.I.EndScenario();
                    }
                }
            );

            CreateInspectionButton(
                "Btn_CloseQuarantine", qPanel.transform,
                new Vector2(190f, -180f), new Vector2(370f, 54f),
                "<b>[ X CERRAR VISTA ]</b>\n<size=11>Volver al entorno de la oficina</size>",
                new Color(0.20f, 0.25f, 0.35f, 0.98f),
                Color.white,
                CloseOverlay
            );

            _activeContentPanel = qPanel;
            ShowOverlay();
        }

        /// <summary>
        /// MODO ATAQUE (RF-04):
        /// Al hacer clic en el enlace del CV, activa la animación visual de la infección propagándose
        /// por la pantalla, propaga el ataque a la red y termina mostrando el robo millonario consumado.
        /// </summary>
        public void TriggerAttackInfectionSequence()
        {
            // Reproducir reacción de voz NPC (04. What's That) y música de tensión (Run for Your Life)
            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayInfectionVoiceAlert();
                AndesSim.Audio.AudioManager.I.PlayMusicTrack(AndesSim.Audio.MusicTheme.AttackTension, 0.3f);
            }

            // Ejecutar la propagación en red (Beams 3D + 3 PCs infectadas)
            if (PhishingInspector.I != null)
            {
                PhishingInspector.I.ExecuteOpenLinkAttackSequence();
            }
            else if (InfectionSpreadVisualizer.I != null)
            {
                InfectionSpreadVisualizer.I.PlayInfectionSequence();
            }

            // Iniciar animación visual en pantalla completa
            StartCoroutine(InfectionScreenAnimationRoutine());
        }

        private IEnumerator InfectionScreenAnimationRoutine()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b><color=#FF3834>[ALERTA CRÍTICA: MALWARE LAZARUS EN EJECUCIÓN]</color></b>";
            _subTitleText.text = "Infección de Red Plana Activa · Dropper .scr Ejecutado en Puesto 1";

            GameObject infectPanel = new GameObject("InfectionContentPanel");
            infectPanel.transform.SetParent(_contentContainer, false);
            RectTransform iRect = infectPanel.AddComponent<RectTransform>();
            iRect.anchorMin = Vector2.zero;
            iRect.anchorMax = Vector2.one;
            iRect.sizeDelta = Vector2.zero;

            GameObject terminalBox = new GameObject("InfectionTerminalBox");
            terminalBox.transform.SetParent(infectPanel.transform, false);
            RectTransform tRect = terminalBox.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 30f);
            tRect.sizeDelta = new Vector2(900f, 350f);
            Image tBg = terminalBox.AddComponent<Image>();
            tBg.color = new Color(0.18f, 0.02f, 0.02f, 0.98f);

            CreateLabel("Txt_InfectionTrace", terminalBox.transform, Vector2.zero, new Vector2(860f, 330f), "<color=#FF5A4E><b>[INICIANDO DESCARGA DE EXPLOIT...]</b></color>", 14f, TextAlignmentOptions.TopLeft, Color.white);

            GameObject textObj = terminalBox.transform.Find("Txt_InfectionTrace")?.gameObject;
            TextMeshProUGUI traceTmp = textObj != null ? textObj.GetComponent<TextMeshProUGUI>() : null;
            

            _activeContentPanel = infectPanel;
            ShowOverlay();

            StringBuilder sb = new StringBuilder();

            // Paso 1 (1.0s): Descarga
            sb.AppendLine("<b><color=#FF5A4E>[09:18:44] CONEXIÓN SALIENTE ESTABLECIDA:</color></b>");
            sb.AppendLine("• GET http://185.130.44.82/Rasel_Ahlam_CV_2015.scr.zip -> 200 OK (1.2 MB)");
            sb.AppendLine("• Descompresión en memoria del ejecutable camuflado: <color=#FF5A4E><b>Rasel_Ahlam_CV_2015.scr</b></color>");
            traceTmp.text = sb.ToString();
            yield return new WaitForSeconds(1.2f);

            // Paso 2 (1.2s): Inyección de backdoor
            sb.AppendLine("\n<b><color=#FF3834>[09:18:47] INSTALACIÓN DE BACKDOOR LAZARUS (svchost_upd.exe):</color></b>");
            sb.AppendLine("• Persistencia: HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run");
            sb.AppendLine("• Bypassing UAC & evasión de defensas locales.");
            sb.AppendLine("• Conexión C2 establecida con 185.130.44.82:443 (TLS Encrypted).");
            traceTmp.text = sb.ToString();
            tBg.color = new Color(0.28f, 0.03f, 0.03f, 0.98f);
            yield return new WaitForSeconds(1.4f);

            // Paso 3 (1.4s): Propagación lateral por red plana
            sb.AppendLine("\n<b><color=#FF2222>[09:18:50] MOVIMIENTO LATERAL - RED PLANA COMPROMETIDA:</color></b>");
            sb.AppendLine("• Escaneo ARP por segmento 10.0.0.0/24 sin VLANs de aislamiento.");
            sb.AppendLine("• Salto lateral a Puesto 2: <color=#FF3834>Terminal SWIFT Alliance Access [INFECTADA]</color>");
            sb.AppendLine("• Salto lateral a Puesto 3: <color=#FF3834>Consola SIEM [REGISTROS ALTERADOS]</color>");
            sb.AppendLine("• Manipulación de software de impresión SWIFT para ocultar transferencias MT103.");
            traceTmp.text = sb.ToString();
            yield return new WaitForSeconds(1.8f);

            // =========================================================
            // PANTALLA OFICIAL DE ROBO CONSUMADO (US$ 951,000,000.00)
            // =========================================================
            ClearActiveContent();

            _headerTitleText.text = "<b><color=#FF2222>ROBO CONSUMADO: BANCO ANDINO COMPROMETIDO (ATAQUE 2016)</color></b>";
            _subTitleText.text = "Recreación Histórica del Fraude Bancario de Lazarus a través de la Red SWIFT";

            GameObject robPanel = new GameObject("RobberyResultPanel");
            robPanel.transform.SetParent(_contentContainer, false);
            RectTransform rRect = robPanel.AddComponent<RectTransform>();
            rRect.anchorMin = Vector2.zero;
            rRect.anchorMax = Vector2.one;
            rRect.sizeDelta = Vector2.zero;

            GameObject rBox = new GameObject("RobberyBox");
            rBox.transform.SetParent(robPanel.transform, false);
            RectTransform rbRect = rBox.AddComponent<RectTransform>();
            rbRect.anchoredPosition = new Vector2(0f, 35f);
            rbRect.sizeDelta = new Vector2(900f, 360f);
            Image rbBg = rBox.AddComponent<Image>();
            rbBg.color = new Color(0.08f, 0.02f, 0.02f, 0.98f);

            StringBuilder rsb = new StringBuilder();
            rsb.AppendLine("<b><size=17><color=#FF3834>[INFORME FORENSE: FRAUDE BANCARIO CONSUMADO]</color></size></b>");
            rsb.AppendLine("────────────────────────────────────────────────────────────────────────────");
            rsb.AppendLine("• <b>MONTO TRANSFERIDO:</b> <color=#FF3834><size=18><b>US$ 81,000,000.00</b></size></color> (Lavado en Filipinas)");
            rsb.AppendLine("• <b>MONTO RETENIDO:</b> US$ 870,000,000.00 (Frenado por error ortográfico 'Fandation')");
            rsb.AppendLine("• <b>VECTOR DE ENTRADA:</b> Spear Phishing a Carla Mendoza (Dropper .scr camuflado).");
            rsb.AppendLine("• <b>MOVIMIENTO LATERAL:</b> Red plana sin VLANs permitió salto inmediato a Terminal SWIFT.");
            rsb.AppendLine("• <b>ANÁLISIS DE MALWARE:</b> Backdoor <i>svchost_upd.exe</i> con conexión a IP C2 <b>185.130.44.82</b>.");
            rsb.AppendLine("────────────────────────────────────────────────────────────────────────────");
            rsb.AppendLine("<size=15><color=#FFC24A>▲ <b>CAUSA RAÍZ:</b> Ausencia de Sandbox de correo y falta de segmentación VLAN 100.</color></size>");

            CreateLabel("Txt_RobberySummary", rBox.transform, Vector2.zero, new Vector2(860f, 340f),
                rsb.ToString(), 15.5f, TextAlignmentOptions.TopLeft, Color.white);

            CreateInspectionButton(
                "Btn_FinalizeAttack", robPanel.transform,
                new Vector2(-190f, -180f), new Vector2(370f, 54f),
                "<b><color=#FF5A4E>[ FINALIZAR EVALUACIÓN ]</color></b>\n<size=11>Completar simulación</size>",
                new Color(0.5f, 0.12f, 0.12f, 0.98f),
                Color.white,
                () =>
                {
                    CloseOverlay();
                    if (GameManager.I != null)
                    {
                        GameManager.I.EndScenario();
                    }
                }
            );

            CreateInspectionButton(
                "Btn_CloseRobbery", robPanel.transform,
                new Vector2(190f, -180f), new Vector2(370f, 54f),
                "<b>[ X CERRAR VISTA ]</b>\n<size=11>Volver al entorno de la oficina</size>",
                new Color(0.20f, 0.25f, 0.35f, 0.98f),
                Color.white,
                CloseOverlay
            );

            _activeContentPanel = robPanel;
            ShowOverlay();
        }

        /// <summary>
        /// RF-05: Abre la vista ampliada del Post-It y libreta de credenciales de Carla Mendoza.
        /// </summary>
        public void OpenCredentialsInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b>EVIDENCIA FORENSE #2: CREDENCIALES EXPUESTAS</b>";
            _subTitleText.text = "Post-It y Libreta de Operaciones en el Puesto 1 (Carla Mendoza)";

            GameObject credPanel = new GameObject("CredentialsContentPanel");
            credPanel.transform.SetParent(_contentContainer, false);
            RectTransform cRect = credPanel.AddComponent<RectTransform>();
            cRect.anchorMin = Vector2.zero;
            cRect.anchorMax = Vector2.one;
            cRect.sizeDelta = Vector2.zero;

            // Simulación visual del Post-It Amarillo
            GameObject postItBox = new GameObject("PostItVisual");
            postItBox.transform.SetParent(credPanel.transform, false);
            RectTransform pRect = postItBox.AddComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(-180f, 20f);
            pRect.sizeDelta = new Vector2(400f, 320f);
            Image postItBg = postItBox.AddComponent<Image>();
            postItBg.color = new Color(0.98f, 0.88f, 0.35f, 0.98f); // Amarillo Post-It

            CreateLabel("Txt_PostItTitle", postItBox.transform, new Vector2(0f, 110f), new Vector2(360f, 40f),
                "<b>BANCO ANDINO · SWIFT</b>", 16f, TextAlignmentOptions.Center, new Color(0.1f, 0.1f, 0.1f));

            CreateLabel("Txt_PostItBody", postItBox.transform, new Vector2(0f, -10f), new Vector2(360f, 200f),
                "<b>Terminal:</b> PC-PAGOS-01\n" +
                "<b>Usuario:</b> c.mendoza\n" +
                "<b>Password:</b> <color=#990000>Swift2015$Admin!</color>\n" +
                "<b>Token Bóveda:</b> 849201\n" +
                "<b>PIN Respaldo:</b> 9481\n\n" +
                "<i>(No olvidar actualizar cada 90 días)</i>",
                14f, TextAlignmentOptions.TopLeft, new Color(0.12f, 0.12f, 0.12f));

            // Panel de Diagnóstico Forense
            GameObject diagBox = new GameObject("ForensicDiagBox");
            diagBox.transform.SetParent(credPanel.transform, false);
            RectTransform dRect = diagBox.AddComponent<RectTransform>();
            dRect.anchoredPosition = new Vector2(230f, 20f);
            dRect.sizeDelta = new Vector2(380f, 320f);
            Image diagBg = diagBox.AddComponent<Image>();
            diagBg.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);

            CreateLabel("Txt_DiagTitle", diagBox.transform, new Vector2(0f, 110f), new Vector2(350f, 40f),
                "<b>ANÁLISIS FORENSE ISO 27001</b>", 15f, TextAlignmentOptions.Center, WarningAmberColor);

            CreateLabel("Txt_DiagBody", diagBox.transform, new Vector2(0f, -15f), new Vector2(350f, 220f),
                "<b>Falla Crítica Identificada:</b>\n" +
                "Violación del control A.11.2.9 (Puesto de trabajo despejado).\n\n" +
                "Las credenciales bancarias estaban escritas a mano y a la vista de cualquier visitante.\n\n" +
                "<b>Impacto:</b> Permitió al intruso elevar privilegios de inmediato y operar transferencias SWIFT sin requerir cracking de contraseñas.",
                12.5f, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.92f, 1f));

            // Botón de Registro de Evidencia
            CreateInspectionButton(
                "Btn_LogEvidencePostIt", credPanel.transform,
                new Vector2(0f, -180f), new Vector2(460f, 54f),
                "<b>[ REGISTRAR EVIDENCIA #2 EN EL EXPEDIENTE ]</b>",
                new Color(0.15f, 0.45f, 0.25f, 0.95f),
                GreenSuccessColor,
                () =>
                {
                    if (GameManager.I != null)
                    {
                        GameManager.I.RegisterEvidence("Evidencia_PostIt");
                    }
                    if (AndesSim.Audio.AudioManager.I != null)
                    {
                        AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
                    }
                    CloseOverlay();
                }
            );

            _activeContentPanel = credPanel;
            ShowOverlay();
        }

        /// <summary>
        /// RF-01 / RF-04: Abre la vista ampliada de la Terminal SWIFT (Puesto 2).
        /// </summary>
        public void OpenSwiftInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b>TERMINAL SWIFT ALLIANCE ACCESS · PUESTO 2</b>";
            _subTitleText.text = "Operaciones Internacionales y Auditoría de Red Plana (VLANs Ausentes)";

            GameObject swiftPanel = new GameObject("SwiftContentPanel");
            swiftPanel.transform.SetParent(_contentContainer, false);
            RectTransform sRect = swiftPanel.AddComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.sizeDelta = Vector2.zero;

            GameObject tableBox = new GameObject("SwiftTableBox");
            tableBox.transform.SetParent(swiftPanel.transform, false);
            RectTransform tbRect = tableBox.AddComponent<RectTransform>();
            tbRect.anchoredPosition = new Vector2(0f, 30f);
            tbRect.sizeDelta = new Vector2(880f, 310f);
            Image tbBg = tableBox.AddComponent<Image>();
            tbBg.color = new Color(0.06f, 0.1f, 0.16f, 0.95f);

            CreateLabel("Txt_SwiftTable", tableBox.transform, Vector2.zero, new Vector2(850f, 290f),
                "<b><size=17><color=#00F2FE>SISTEMA SWIFT ALLIANCE ACCESS — MENSAJERÍA MT103</color></size></b>\n" +
                "────────────────────────────────────────────────────────────────────────────\n" +
                "• <b>Transacción:</b> MT103-20150129-948192 (Orden de Transferencia Internacional)\n" +
                "• <b>Banco Emisor:</b> BANCO ANDINO -> Beneficiario: RCBC Filipinas\n" +
                "• <b>Monto en Riesgo:</b> <color=#FF5A4E><b>US$ 951,000,000.00</b></color>\n" +
                "• <b>Vulnerabilidad de Red:</b> <color=#FFC24A>RED PLANA (Sin VLAN 100 ni microsegmentación)</color>\n" +
                "• <b>Falla de Control:</b> Ausencia de segundo factor de autenticación (MFA) en transferencias.\n" +
                "────────────────────────────────────────────────────────────────────────────\n" +
                "<b><size=14><color=#FF5A4E>▲ La ausencia de VLANs permitió que el malware de Carla alcanzara este puesto en segundos.</color></size></b>",
                15.5f, TextAlignmentOptions.TopLeft, new Color(0.88f, 0.95f, 1f));

            _activeContentPanel = swiftPanel;
            ShowOverlay();
        }

        /// <summary>
        /// RF-05: Abre la vista ampliada de la Consola SIEM (Puesto 3) con los registros de accesos al C2.
        /// </summary>
        public void OpenSiemLogInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b>CONSOLA SIEM Y REGISTROS DE ACCESO · PUESTO 3</b>";
            _subTitleText.text = "Auditoría de Tráfico Saliente C2 hacia la IP 185.130.44.82";

            GameObject siemPanel = new GameObject("SiemContentPanel");
            siemPanel.transform.SetParent(_contentContainer, false);
            RectTransform smRect = siemPanel.AddComponent<RectTransform>();
            smRect.anchorMin = Vector2.zero;
            smRect.anchorMax = Vector2.one;
            smRect.sizeDelta = Vector2.zero;

            GameObject logBox = new GameObject("SiemLogBox");
            logBox.transform.SetParent(siemPanel.transform, false);
            RectTransform lRect = logBox.AddComponent<RectTransform>();
            lRect.anchoredPosition = new Vector2(0f, 40f);
            lRect.sizeDelta = new Vector2(880f, 290f);
            Image lBg = logBox.AddComponent<Image>();
            lBg.color = new Color(0.04f, 0.08f, 0.12f, 0.95f);

            CreateLabel("Txt_SiemLogs", logBox.transform, Vector2.zero, new Vector2(850f, 270f),
                "<b><size=17><color=#38F28A>CONSOLA SIEM — INDICADORES DE COMPROMISO (IOCs)</color></size></b>\n" +
                "────────────────────────────────────────────────────────────────────────────\n" +
                "• <b>[09:18:22] CONEXIÓN C2:</b> <color=#FF5A4E>192.168.1.101 -> 185.130.44.82:443 [ESTABLECIDA]</color>\n" +
                "• <b>[09:18:25] PROCESO MALICIOSO:</b> svchost_upd.exe (PID 4820) desde AppData\\Local\n" +
                "• <b>[09:18:29] DOMINIO ATACANTE:</b> career-applicants-bd.com (Dominio Lazarus de 48h)\n" +
                "• <b>[09:18:35] ALERTA PERIMETRAL:</b> Dropper .scr descargado sin pasar por Sandbox Cuckoo.\n" +
                "────────────────────────────────────────────────────────────────────────────\n" +
                "<b><size=15><color=#38F28A>✓ IOC Identificado: Servidor de Mando y Control 185.130.44.82 (Grupo Lazarus)</color></size></b>",
                15f, TextAlignmentOptions.TopLeft, new Color(0.88f, 0.95f, 1f));

            // Botón de Registro de Evidencia SIEM
            CreateInspectionButton(
                "Btn_LogEvidenceSiem", siemPanel.transform,
                new Vector2(0f, -165f), new Vector2(460f, 54f),
                "<b>[ REGISTRAR EVIDENCIA #3: LOG DE ACCESOS ]</b>",
                new Color(0.15f, 0.45f, 0.25f, 0.95f),
                GreenSuccessColor,
                () =>
                {
                    if (GameManager.I != null)
                    {
                        GameManager.I.RegisterEvidence("Evidencia_LogAccesos");
                    }
                    if (AndesSim.Audio.AudioManager.I != null)
                    {
                        AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
                    }
                    CloseOverlay();
                }
            );

            _activeContentPanel = siemPanel;
            ShowOverlay();
        }

        /// <summary>
        /// RF-05: Abre la vista ampliada del expediente del sospechoso seleccionado en el Tablero Táctico.
        /// </summary>
        public void OpenSuspectDossierInspection(int suspectIndex)
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            GameObject dossierPanel = new GameObject("SuspectDossierModalPanel");
            dossierPanel.transform.SetParent(_contentContainer, false);
            RectTransform dr = dossierPanel.AddComponent<RectTransform>();
            dr.sizeDelta = new Vector2(1020f, 580f);

            SuspectBoardVR board = SuspectBoardVR.I != null ? SuspectBoardVR.I : FindFirstObjectByType<SuspectBoardVR>();
            SuspectDossierScenario01 suspect = null;
            if (board != null && board.Suspects != null && suspectIndex >= 0 && suspectIndex < board.Suspects.Count)
            {
                suspect = board.Suspects[suspectIndex];
            }

            string name = suspect != null ? suspect.fullName : "Sospechoso";
            string role = suspect != null ? suspect.roleTitle : "Personal Banco Andino";
            string badge = suspect != null ? suspect.threatLevelBadge : "EN INVESTIGACIÓN";
            string incrim = suspect != null ? suspect.incriminatingPoints : "Sin indicios registrados.";
            string exon = suspect != null ? suspect.exoneratingPoints : "Sin descargos registrados.";

            // Encabezado
            CreateLabel("Txt_Header", dossierPanel.transform, new Vector2(0f, 245f), new Vector2(960f, 35f),
                $"<b><color=#38F28A>CASO BANCO ANDINO · AUDITORÍA INTERNA 2015</color></b> | EXPEDIENTE #{suspectIndex + 1}",
                15f, TextAlignmentOptions.Center, Color.cyan);

            CreateLabel("Txt_SuspectTitle", dossierPanel.transform, new Vector2(0f, 210f), new Vector2(960f, 35f),
                $"<b>{name}</b> — <color=#E2E8F0>{role}</color> | <color=#FF5A4E>[{badge}]</color>",
                13.5f, TextAlignmentOptions.Center, Color.white);

            // Caja Izquierda: Puntos en Contra / Incriminatorios
            GameObject leftBox = new GameObject("LeftBox");
            leftBox.transform.SetParent(dossierPanel.transform, false);
            RectTransform lbRect = leftBox.AddComponent<RectTransform>();
            lbRect.anchoredPosition = new Vector2(-240f, 30f);
            lbRect.sizeDelta = new Vector2(470f, 310f);
            Image lbBg = leftBox.AddComponent<Image>();
            lbBg.color = new Color(0.12f, 0.08f, 0.08f, 0.94f);

            CreateLabel("Txt_LeftTitle", leftBox.transform, new Vector2(0f, 130f), new Vector2(440f, 30f),
                "<b><color=#FF5A4E>▲ PUNTOS EN CONTRA / INCRIMINATORIOS</color></b>",
                12.5f, TextAlignmentOptions.TopLeft, WarningAmberColor);

            CreateLabel("Txt_LeftContent", leftBox.transform, new Vector2(0f, -10f), new Vector2(440f, 240f),
                $"<size=11>{incrim}</size>",
                11f, TextAlignmentOptions.TopLeft, new Color(0.95f, 0.90f, 0.90f));

            // Caja Derecha: Elementos a Favor / Descargo
            GameObject rightBox = new GameObject("RightBox");
            rightBox.transform.SetParent(dossierPanel.transform, false);
            RectTransform rbRect = rightBox.AddComponent<RectTransform>();
            rbRect.anchoredPosition = new Vector2(240f, 30f);
            rbRect.sizeDelta = new Vector2(470f, 310f);
            Image rbBg = rightBox.AddComponent<Image>();
            rbBg.color = new Color(0.08f, 0.14f, 0.10f, 0.94f);

            CreateLabel("Txt_RightTitle", rightBox.transform, new Vector2(0f, 130f), new Vector2(440f, 30f),
                "<b><color=#38F28A>✓ ELEMENTOS A FAVOR / DESCARGO</color></b>",
                12.5f, TextAlignmentOptions.TopLeft, GreenSuccessColor);

            CreateLabel("Txt_RightContent", rightBox.transform, new Vector2(0f, -10f), new Vector2(440f, 240f),
                $"<size=11>{exon}</size>",
                11f, TextAlignmentOptions.TopLeft, new Color(0.90f, 0.98f, 0.92f));

            // Botón Fijar Hipótesis
            CreateInspectionButton(
                "Btn_SelectHypothesis", dossierPanel.transform,
                new Vector2(-150f, -190f), new Vector2(430f, 50f),
                $"<b>[ REGISTRAR A {name.ToUpper()} COMO HIPÓTESIS ]</b>",
                new Color(0.12f, 0.38f, 0.22f, 0.96f),
                GreenSuccessColor,
                () =>
                {
                    if (board != null)
                    {
                        board.DisplaySuspect(suspectIndex);
                        board.OnSelectHypothesisClicked();
                    }
                    if (AndesSim.Audio.AudioManager.I != null)
                    {
                        AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
                    }
                    CloseOverlay();
                }
            );

            // Botón Cerrar
            CreateInspectionButton(
                "Btn_CloseDossier", dossierPanel.transform,
                new Vector2(290f, -190f), new Vector2(220f, 50f),
                "<b>[ CERRAR DOSSIER ]</b>",
                new Color(0.25f, 0.28f, 0.32f, 0.96f),
                Color.white,
                () => CloseOverlay()
            );

            _activeContentPanel = dossierPanel;
            ShowOverlay();
        }

        #endregion

        #region Control de Ventana y Ayudantes de UI

        public void ShowOverlay()
        {
            _isOpen = true;
            if (_overlayRoot != null)
            {
                if (_targetCamera == null) _targetCamera = Camera.main;
                if (_targetCamera != null)
                {
                    if (_overlayCanvas != null) _overlayCanvas.worldCamera = _targetCamera;

                    Vector3 forward = _targetCamera.transform.forward;
                    forward.y = 0f;
                    if (forward.sqrMagnitude < 0.001f) forward = _targetCamera.transform.forward;
                    forward.Normalize();

                    Vector3 spawnPos = _targetCamera.transform.position + (forward * distanceFromCamera);
                    spawnPos.y = _targetCamera.transform.position.y - 0.05f;

                    _overlayRoot.transform.position = spawnPos;
                    _overlayRoot.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }
                _overlayRoot.SetActive(true);
            }

            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayLaserScan(transform.position);
            }
        }

        public void CloseOverlay()
        {
            StopAllCoroutines();
            _isOpen = false;
            if (_overlayRoot != null)
            {
                _overlayRoot.SetActive(false);
            }
            ClearActiveContent();

            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayGazeClick();
            }
        }

        private void ClearActiveContent()
        {
            if (_contentContainer != null)
            {
                for (int i = _contentContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = _contentContainer.GetChild(i);
                    child.gameObject.SetActive(false);
                    child.SetParent(null);
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
            if (_activeContentPanel != null)
            {
                _activeContentPanel.SetActive(false);
                _activeContentPanel.transform.SetParent(null);
                if (Application.isPlaying)
                {
                    Destroy(_activeContentPanel);
                }
                else
                {
                    DestroyImmediate(_activeContentPanel);
                }
                _activeContentPanel = null;
            }
        }

        private GameObject CreateInspectionButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color bgColor, Color textColor, Action onClick)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            btnGO.layer = LayerMask.NameToLayer("Interactive");

            RectTransform rect = btnGO.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image bg = btnGO.AddComponent<Image>();
            bg.color = bgColor;

            Button btn = btnGO.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = bgColor * 1.25f;
            cb.pressedColor = bgColor * 0.8f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());

            // Colisionador BoxCollider para detección precisa con retícula VR
            BoxCollider boxCol = btnGO.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(size.x, size.y, 45f);
            boxCol.center = new Vector3(0f, 0f, -15f);
            boxCol.isTrigger = true;

            // Añadir soporte de retícula VR
            GazeInteractable gaze = btnGO.AddComponent<GazeInteractable>();
            gaze.interactableLabel = text.Replace("<b>", "").Replace("</b>", "").Split('\n')[0];
            gaze.dwellTime = 1.1f;
            if (onClick != null) gaze.onClick.AddListener(() => onClick.Invoke());

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            RectTransform tRect = txtGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;

            return btnGO;
        }

        private void CreateSignalButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, string signalId)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            btnGO.layer = LayerMask.NameToLayer("Interactive");

            RectTransform rect = btnGO.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image bg = btnGO.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.14f, 0.08f, 0.95f);

            Button btn = btnGO.AddComponent<Button>();

            // Colisionador BoxCollider calibrado para el raycast de retícula VR
            BoxCollider boxCol = btnGO.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(size.x, size.y, 25f);
            boxCol.center = new Vector3(0f, 0f, -5f);
            boxCol.isTrigger = true;

            GazeInteractable gaze = btnGO.AddComponent<GazeInteractable>();
            gaze.interactableLabel = $"Inspeccionar {signalId}";
            gaze.dwellTime = 1.0f;

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            RectTransform tRect = txtGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 12f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = WarningAmberColor;

            Action markAction = () =>
            {
                if (ScenarioFlowController.I != null)
                {
                    ScenarioFlowController.I.NotifyPhishingSignalInspected(signalId);
                }
                if (PhishingInspector.I != null)
                {
                    PhishingInspector.I.MarkPhishingSignal(signalId);
                }
                if (AndesSim.Audio.AudioManager.I != null)
                {
                    AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
                }
                bg.color = new Color(0.08f, 0.35f, 0.18f, 0.95f);
                tmp.text = $"<b>[✓ IDENTIFICADA]</b>\n<size=10>{signalId}</size>";
                tmp.color = GreenSuccessColor;
            };

            btn.onClick.AddListener(() => markAction.Invoke());
            gaze.onClick.AddListener(() => markAction.Invoke());
        }

        private void CreateLabel(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
        }

        private void EnsureEventSystemExists()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null && FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }
        }

        #endregion
    }
}
