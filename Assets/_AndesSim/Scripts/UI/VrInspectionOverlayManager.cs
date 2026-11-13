using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Environment;
using AndesSim.Scenario01;
using AndesSim.VR;

namespace AndesSim.UI
{
    /// <summary>
    /// Administrador del Sistema de Inspección y Zoom VR (RF-01 a RF-05).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Incluye entradas holográficas EaseOutBack, levitación sutil, tipografía centrada elegante (Inter + Roboto Bold),
    /// barra animada de propagación del malware y navegación paso a paso con acceso directo al Menú VR.
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

        private static readonly Color CyanNeonAccent = AndesTypographyTheme.AccentCyan;
        private static readonly Color GreenSuccessColor = AndesTypographyTheme.AccentEmerald;
        private static readonly Color WarningAmberColor = AndesTypographyTheme.AccentAmber;
        private static readonly Color DangerRedColor = AndesTypographyTheme.AccentRed;

        [Header("--- Anclaje VR ---")]
        [SerializeField] private float distanceFromCamera = 1.32f;
        private int _emailSubPart = 0;

        // Raíz del Canvas de Inspección
        private GameObject _overlayRoot;
        private Canvas _overlayCanvas;
        private CanvasGroup _overlayCanvasGroup;
        private RectTransform _mainPanelRect;
        private HolographicPanelEntrance _panelEntrance;
        private Image _topGlowBarImg;
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

            // Si el usuario gira la cabeza completamente fuera del panel, cerrarlo automáticamente.
            Vector3 dirToPanel = (_overlayRoot.transform.position - _targetCamera.transform.position).normalized;
            float angle = Vector3.Angle(_targetCamera.transform.forward, dirToPanel);
            if (angle > 82f)
            {
                CloseOverlay();
            }
        }

        /// <summary>
        /// Crea de forma procedural el Canvas World Space de inspección con estética holográfica flotante.
        /// </summary>
        private void EnsureOverlayCanvas()
        {
            if (_overlayRoot != null) return;

            _overlayRoot = new GameObject("VrInspectionOverlayCanvas");
            _overlayRoot.transform.SetParent(null);
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
            canvasRect.sizeDelta = new Vector2(1020f, 760f);
            canvasRect.localScale = Vector3.one * 0.00192f;

            EnsureEventSystemExists();

            GameObject panelGO = new GameObject("InspectionFrame");
            panelGO.transform.SetParent(_overlayRoot.transform, false);
            _mainPanelRect = panelGO.AddComponent<RectTransform>();
            _mainPanelRect.anchorMin = Vector2.zero;
            _mainPanelRect.anchorMax = Vector2.one;
            _mainPanelRect.sizeDelta = Vector2.zero;

            _overlayCanvasGroup = panelGO.AddComponent<CanvasGroup>();
            _panelEntrance = panelGO.AddComponent<HolographicPanelEntrance>();
            _panelEntrance.Configure(0.28f, 0.87f, 24f, _overlayCanvasGroup);

            HolographicFloatMotion panelFloat = panelGO.AddComponent<HolographicFloatMotion>();
            panelFloat.Configure(11f, 0.85f, new Vector3(0.55f, 0.85f, 0.35f), 0f, 0f, true);

            Image bgImage = panelGO.AddComponent<Image>();
            bgImage.color = AndesTypographyTheme.BgVoidNavy;

            // Barra luminosa superior e inferior
            GameObject topBar = new GameObject("TopGlowBar");
            topBar.transform.SetParent(panelGO.transform, false);
            RectTransform topBarRect = topBar.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.anchoredPosition = new Vector2(0f, -4f);
            topBarRect.sizeDelta = new Vector2(0f, 7f);
            _topGlowBarImg = topBar.AddComponent<Image>();
            _topGlowBarImg.color = CyanNeonAccent;

            // Botón Superior Izquierdo: VOLVER AL MENÚ VR
            CreateInspectionButton(
                "Btn_ReturnMenuTopLeft", panelGO.transform,
                new Vector2(-416f, 326f), new Vector2(138f, 40f),
                "MENU VR",
                new Color(0.09f, 0.21f, 0.34f, 0.96f),
                CyanNeonAccent,
                NavigateToMainMenu
            );

            // Título Principal Centrado
            GameObject titleGO = new GameObject("HeaderTitle");
            titleGO.transform.SetParent(panelGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 322f);
            titleRect.sizeDelta = new Vector2(670f, 44f);
            _headerTitleText = titleGO.AddComponent<TextMeshProUGUI>();
            AndesTypographyTheme.StyleDisplayTitle(_headerTitleText, CyanNeonAccent, 25f);

            // Subtítulo Centrado
            GameObject subGO = new GameObject("SubTitle");
            subGO.transform.SetParent(panelGO.transform, false);
            RectTransform subRect = subGO.AddComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0f, 282f);
            subRect.sizeDelta = new Vector2(880f, 36f);
            _subTitleText = subGO.AddComponent<TextMeshProUGUI>();
            AndesTypographyTheme.StyleBodyCentered(_subTitleText, AndesTypographyTheme.TextMuted, 17.5f, 6f);

            // Contenedor Dinámico Central
            GameObject contentGO = new GameObject("DynamicContentContainer");
            contentGO.transform.SetParent(panelGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchoredPosition = new Vector2(0f, -8f);
            contentRect.sizeDelta = new Vector2(930f, 520f);
            _contentContainer = contentGO.transform;

            // Botón Superior Derecho: CERRAR INMEDIATO
            CreateInspectionButton(
                "Btn_CerrarTopRight", panelGO.transform,
                new Vector2(436f, 326f), new Vector2(98f, 40f),
                "X",
                new Color(0.36f, 0.12f, 0.15f, 0.96f),
                Color.white,
                CloseOverlay
            );

            // Barra Persistente Guiada (Paso a Paso en VR)
            CreateInspectionButton(
                "Btn_NavCorreo", panelGO.transform,
                new Vector2(-374f, -328f), new Vector2(174f, 52f),
                "1. CORREO",
                new Color(0.08f, 0.22f, 0.36f, 0.96f),
                Color.white,
                () => OpenEmailInspection(0)
            );
            CreateInspectionButton(
                "Btn_NavPostIt", panelGO.transform,
                new Vector2(-187f, -328f), new Vector2(174f, 52f),
                "2. POST-IT",
                new Color(0.08f, 0.22f, 0.36f, 0.96f),
                Color.white,
                OpenCredentialsInspection
            );
            CreateInspectionButton(
                "Btn_NavSiem", panelGO.transform,
                new Vector2(0f, -328f), new Vector2(174f, 52f),
                "3. SIEM",
                new Color(0.08f, 0.22f, 0.36f, 0.96f),
                Color.white,
                OpenSiemLogInspection
            );
            CreateInspectionButton(
                "Btn_NavTablero", panelGO.transform,
                new Vector2(190f, -328f), new Vector2(188f, 52f),
                "4. TABLERO >",
                new Color(0.08f, 0.32f, 0.24f, 0.96f),
                GreenSuccessColor,
                NavigateToSuspectBoardOrDefense
            );
            CreateInspectionButton(
                "Btn_CerrarInspeccion", panelGO.transform,
                new Vector2(382f, -328f), new Vector2(162f, 52f),
                "X SALIR",
                new Color(0.36f, 0.14f, 0.16f, 0.96f),
                Color.white,
                CloseOverlay
            );

            _overlayRoot.SetActive(false);
            Debug.Log($"{LogPrefix} Canvas de Inspección VR inicializado con éxito.");
        }

        private void SetHeaderTheme(Color accentColor, string title, string subtitle)
        {
            if (_topGlowBarImg != null) _topGlowBarImg.color = accentColor;
            if (_headerTitleText != null)
            {
                _headerTitleText.text = title;
                AndesTypographyTheme.StyleDisplayTitle(_headerTitleText, accentColor, 25f);
            }
            if (_subTitleText != null)
            {
                _subTitleText.text = subtitle;
                AndesTypographyTheme.StyleBodyCentered(_subTitleText, AndesTypographyTheme.TextMuted, 17.5f, 6f);
            }
        }

        #region Vistas de Inspección Oficiales

        public void OpenEmailInspection()
        {
            OpenEmailInspection(0);
        }

        public void OpenEmailInspection(int subPart)
        {
            _emailSubPart = Mathf.Clamp(subPart, 0, 1);
            EnsureOverlayCanvas();
            ClearActiveContent();

            SetHeaderTheme(
                _emailSubPart == 0 ? CyanNeonAccent : WarningAmberColor,
                "PC 1 · CORREO DE PHISHING",
                _emailSubPart == 0
                    ? "1ra Parte: Analiza el mensaje recibido por Carla Mendoza"
                    : "2da Parte: Marca las 3 señales y decide tu acción"
            );

            GameObject emailPanel = new GameObject("EmailContentPanel");
            emailPanel.transform.SetParent(_contentContainer, false);
            RectTransform eRect = emailPanel.AddComponent<RectTransform>();
            eRect.anchorMin = Vector2.zero;
            eRect.anchorMax = Vector2.one;
            eRect.sizeDelta = Vector2.zero;

            Color part1Bg = _emailSubPart == 0 ? new Color(0.08f, 0.34f, 0.45f, 0.98f) : AndesTypographyTheme.BgCardGlass;
            Color part2Bg = _emailSubPart == 1 ? new Color(0.38f, 0.25f, 0.08f, 0.98f) : AndesTypographyTheme.BgCardGlass;

            CreateInspectionButton(
                "Btn_TabParte1", emailPanel.transform,
                new Vector2(-230f, 215f), new Vector2(435f, 48f),
                "1RA PARTE: LEER CORREO",
                part1Bg, _emailSubPart == 0 ? CyanNeonAccent : Color.white,
                () => OpenEmailInspection(0)
            );

            CreateInspectionButton(
                "Btn_TabParte2", emailPanel.transform,
                new Vector2(230f, 215f), new Vector2(435f, 48f),
                "2DA PARTE: PISTAS Y DECIDIR",
                part2Bg, _emailSubPart == 1 ? WarningAmberColor : Color.white,
                () => OpenEmailInspection(1)
            );

            if (_emailSubPart == 0)
            {
                GameObject bodyBox = CreateStyledCard("EmailReadCard", emailPanel.transform, new Vector2(0f, 35f), new Vector2(880f, 260f), AndesTypographyTheme.BgCardGlass, CyanNeonAccent);

                CreateLabel("Txt_MailLines", bodyBox.transform, Vector2.zero, new Vector2(820f, 220f),
                    "<b>De:</b> rasel.ahlam@<color=#FFA940>career-applicants-bd.com</color>\n" +
                    "<b>Asunto:</b> <color=#FF5A4E>URGENTE · CV Especialista SWIFT</color>\n\n" +
                    "<i>\"Carla: adjunto mi CV urgente para el área de pagos.\"</i>\n\n" +
                    "<b>Adjunto:</b> <color=#00E5FF>http://185.130.44.82/CV_2015.scr.zip</color>",
                    20f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

                CreateInspectionButton(
                    "Btn_NextToPart2", emailPanel.transform,
                    new Vector2(-225f, -165f), new Vector2(420f, 68f),
                    "VER 2DA PARTE: 3 PISTAS >",
                    new Color(0.10f, 0.34f, 0.44f, 0.98f),
                    CyanNeonAccent,
                    () => OpenEmailInspection(1)
                );

                CreateInspectionButton(
                    "Btn_QuickOpenLink", emailPanel.transform,
                    new Vector2(225f, -165f), new Vector2(420f, 68f),
                    "ABRIR ENLACE (ATAQUE)",
                    new Color(0.48f, 0.12f, 0.14f, 0.98f),
                    Color.white,
                    TriggerAttackInfectionSequence
                );
            }
            else
            {
                CreateLabel("Lbl_StepHint", emailPanel.transform, new Vector2(0f, 155f), new Vector2(880f, 38f),
                    "Apunta a cada señal sospechosa y elige qué hacer con el correo:",
                    19f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

                CreateSignalButton("Btn_Signal1", emailPanel.transform, new Vector2(-300f, 70f), new Vector2(285f, 108f),
                    "<b>1. DOMINIO FALSO</b>\n<size=15><color=#FFA940>@career-applicants</color></size>",
                    PhishingInspector.SignalRemitenteId);

                CreateSignalButton("Btn_Signal2", emailPanel.transform, new Vector2(0f, 70f), new Vector2(285f, 108f),
                    "<b>2. URGENCIA</b>\n<size=15><color=#FFA940>Presión inmediata</color></size>",
                    PhishingInspector.SignalUrgenciaId);

                CreateSignalButton("Btn_Signal3", emailPanel.transform, new Vector2(300f, 70f), new Vector2(285f, 108f),
                    "<b>3. ARCHIVO .SCR</b>\n<size=15><color=#FFA940>IP 185.130.44.82</color></size>",
                    PhishingInspector.SignalEnlaceId);

                CreateInspectionButton(
                    "Btn_AttackOpenLink", emailPanel.transform,
                    new Vector2(-225f, -55f), new Vector2(420f, 74f),
                    "<color=#FF5A4E>[ ! ] ABRIR ENLACE (ATAQUE)</color>\n<size=14><color=#F4F9FF>Ver propagación 3D del virus</color></size>",
                    new Color(0.48f, 0.12f, 0.14f, 0.98f),
                    Color.white,
                    TriggerAttackInfectionSequence
                );

                CreateInspectionButton(
                    "Btn_DefenseReport", emailPanel.transform,
                    new Vector2(225f, -55f), new Vector2(420f, 74f),
                    "<color=#38F28A>[ + ] AISLAR SANDBOX (DEFENSA)</color>\n<size=14><color=#F4F9FF>Bloquear y activar escudos</color></size>",
                    new Color(0.08f, 0.34f, 0.20f, 0.98f),
                    Color.white,
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

                CreateInspectionButton(
                    "Btn_NextStepPostIt", emailPanel.transform,
                    new Vector2(0f, -165f), new Vector2(520f, 58f),
                    "SIGUIENTE PASO: VER POST-IT (PC 1) >",
                    new Color(0.13f, 0.25f, 0.40f, 0.96f),
                    CyanNeonAccent,
                    OpenCredentialsInspection
                );
            }

            _activeContentPanel = emailPanel;
            ShowOverlay();
            AndesSim.Audio.AudioManager.I?.PlayNarration(_emailSubPart == 0 ? "Voice_06_CorreoParte1" : "Voice_07_CorreoParte2", true, 2.0f);
        }

        public void OpenQuarantineInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            if (InfectionSpreadVisualizer.I != null)
            {
                InfectionSpreadVisualizer.I.ShowDefenseContainmentSign();
            }

            SetHeaderTheme(
                GreenSuccessColor,
                "CORREO EN CUARENTENA · SANDBOX",
                "Amenaza detenida antes de infectar la red bancaria"
            );

            GameObject qPanel = new GameObject("QuarantineContentPanel");
            qPanel.transform.SetParent(_contentContainer, false);
            RectTransform qRect = qPanel.AddComponent<RectTransform>();
            qRect.anchorMin = Vector2.zero;
            qRect.anchorMax = Vector2.one;
            qRect.sizeDelta = Vector2.zero;

            GameObject boxGO = CreateStyledCard("SandboxLogBox", qPanel.transform, new Vector2(0f, 35f), new Vector2(880f, 300f),
                new Color(0.03f, 0.11f, 0.11f, 0.98f), GreenSuccessColor);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b><color=#38F28A>REPORTE DE CONTENCIÓN EN TIEMPO REAL</color></b>\n");
            sb.AppendLine("· <b>Sandbox Cuckoo:</b> Adjunto <i>.scr.zip</i> aislado sin ejecución.");
            sb.AppendLine("· <b>Filtro Correo:</b> Dominio falso externo bloqueado.");
            sb.AppendLine("· <b>Firewall SOC:</b> Salida hacia IP 185.130.44.82 cerrada.\n");
            sb.AppendLine("<color=#38F28A><b>[OK] 100% CONTENIDO · US$ 0 ROBADOS</b></color>");

            CreateLabel("Txt_QuarantineLog", boxGO.transform, Vector2.zero, new Vector2(820f, 260f),
                sb.ToString(), 20f, TextAlignmentOptions.Center, Color.white);

            CreateInspectionButton(
                "Btn_GoDefenseConsole", qPanel.transform,
                new Vector2(-305f, -175f), new Vector2(280f, 62f),
                "IR A CONSOLA DEFENSA >",
                new Color(0.08f, 0.28f, 0.36f, 0.98f),
                CyanNeonAccent,
                NavigateToDefenseConsole
            );

            CreateInspectionButton(
                "Btn_ConfirmDefense", qPanel.transform,
                new Vector2(0f, -175f), new Vector2(290f, 62f),
                "FINALIZAR CON EXITO",
                new Color(0.08f, 0.35f, 0.19f, 0.98f),
                GreenSuccessColor,
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
                "Btn_NextSiemDefense", qPanel.transform,
                new Vector2(305f, -175f), new Vector2(260f, 62f),
                "VER SIEM (PC 3) >",
                new Color(0.15f, 0.22f, 0.35f, 0.98f),
                Color.white,
                OpenSiemLogInspection
            );

            _activeContentPanel = qPanel;
            ShowOverlay();
            AndesSim.Audio.AudioManager.I?.PlayNarration("Voice_12_CuarentenaSandbox", true, 2.0f);
        }

        public void TriggerAttackInfectionSequence()
        {
            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayInfectionVoiceAlert();
                AndesSim.Audio.AudioManager.I.PlayMusicTrack(AndesSim.Audio.MusicTheme.AttackTension, 0.3f);
            }

            if (PhishingInspector.I != null)
            {
                PhishingInspector.I.ExecuteOpenLinkAttackSequence();
            }
            else if (InfectionSpreadVisualizer.I != null)
            {
                InfectionSpreadVisualizer.I.PlayInfectionSequence();
            }

            AndesSim.Audio.AudioManager.I?.PlayNarration("Voice_10_AtaqueDetonado", true, 2.0f);
            StopCoroutine(nameof(InfectionScreenAnimationRoutine));
            StartCoroutine(nameof(InfectionScreenAnimationRoutine));
        }

        private IEnumerator InfectionScreenAnimationRoutine()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            SetHeaderTheme(
                DangerRedColor,
                "[ ! ] MALWARE LAZARUS EN EJECUCION",
                "Propagación en vivo por la red corporativa del banco"
            );

            GameObject infectPanel = new GameObject("InfectionContentPanel");
            infectPanel.transform.SetParent(_contentContainer, false);
            RectTransform iRect = infectPanel.AddComponent<RectTransform>();
            iRect.anchorMin = Vector2.zero;
            iRect.anchorMax = Vector2.one;
            iRect.sizeDelta = Vector2.zero;

            GameObject terminalBox = CreateStyledCard("InfectionTerminalBox", infectPanel.transform, new Vector2(0f, 38f), new Vector2(880f, 270f),
                new Color(0.16f, 0.03f, 0.04f, 0.98f), DangerRedColor);
            Image tBg = terminalBox.GetComponent<Image>();

            CreateLabel("Txt_InfectionTrace", terminalBox.transform, new Vector2(0f, 24f), new Vector2(820f, 175f),
                "<color=#FF5A4E><b>FASE 1/3 · DESCARGA EN CURSO</b></color>\n<size=17>Ejecutando Rasel_Ahlam_CV_2015.scr en PC-PAGOS-01...</size>",
                22f, TextAlignmentOptions.Center, Color.white);
            TextMeshProUGUI traceTmp = terminalBox.transform.Find("Txt_InfectionTrace")?.GetComponent<TextMeshProUGUI>();

            // Barra animada de infección
            GameObject barBgObj = new GameObject("InfectionProgressBg");
            barBgObj.transform.SetParent(terminalBox.transform, false);
            RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchoredPosition = new Vector2(0f, -88f);
            barBgRect.sizeDelta = new Vector2(760f, 24f);
            Image barBgImg = barBgObj.AddComponent<Image>();
            barBgImg.color = new Color(0.06f, 0.02f, 0.02f, 0.96f);

            GameObject barFillObj = new GameObject("InfectionProgressFill");
            barFillObj.transform.SetParent(barBgObj.transform, false);
            RectTransform fillRect = barFillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(0f, 0f);
            Image fillImg = barFillObj.AddComponent<Image>();
            fillImg.color = DangerRedColor;

            // 3 Pills inferiores de estado por PC
            GameObject pill1 = CreateStyledCard("Pill_PC1", infectPanel.transform, new Vector2(-285f, -155f), new Vector2(268f, 64f), AndesTypographyTheme.BgCardGlass, WarningAmberColor);
            CreateLabel("Txt_Pill1", pill1.transform, Vector2.zero, new Vector2(246f, 52f), "<b>PC-1 CARLA</b>\n<color=#FF5A4E>Infectando...</color>", 15.5f, TextAlignmentOptions.Center, Color.white);

            GameObject pill2 = CreateStyledCard("Pill_PC2", infectPanel.transform, new Vector2(0f, -155f), new Vector2(268f, 64f), AndesTypographyTheme.BgCardGlass, AndesTypographyTheme.TextMuted);
            CreateLabel("Txt_Pill2", pill2.transform, Vector2.zero, new Vector2(246f, 52f), "<b>PC-2 SWIFT</b>\nEn espera", 15.5f, TextAlignmentOptions.Center, AndesTypographyTheme.TextMuted);

            GameObject pill3 = CreateStyledCard("Pill_PC3", infectPanel.transform, new Vector2(285f, -155f), new Vector2(268f, 64f), AndesTypographyTheme.BgCardGlass, AndesTypographyTheme.TextMuted);
            CreateLabel("Txt_Pill3", pill3.transform, Vector2.zero, new Vector2(246f, 52f), "<b>PC-3 SIEM</b>\nEn espera", 15.5f, TextAlignmentOptions.Center, AndesTypographyTheme.TextMuted);

            TextMeshProUGUI p1Tmp = pill1.transform.Find("Txt_Pill1")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI p2Tmp = pill2.transform.Find("Txt_Pill2")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI p3Tmp = pill3.transform.Find("Txt_Pill3")?.GetComponent<TextMeshProUGUI>();

            _activeContentPanel = infectPanel;
            ShowOverlay();

            const float animDuration = 3.7f;
            float elapsed = 0f;
            int stage = 0;

            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / animDuration);
                fillRect.sizeDelta = new Vector2(760f * u, 0f);
                fillImg.color = Color.Lerp(WarningAmberColor, DangerRedColor, u);

                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 10f);
                tBg.color = new Color(0.20f * pulse, 0.03f, 0.04f, 0.98f);

                if (stage == 0 && u >= 0.30f)
                {
                    stage = 1;
                    if (traceTmp != null)
                    {
                        traceTmp.text =
                            "<color=#38F28A>1/3 [LISTO] Dropper svchost_upd.exe oculto</color>\n\n" +
                            "<color=#FF5A4E><b>2/3 CONECTANDO CON C2 185.130.44.82...</b></color>";
                    }
                    if (p1Tmp != null) p1Tmp.text = "<b>PC-1 CARLA</b>\n<color=#FF3834><b>COMPROMETIDA</b></color>";
                    if (p2Tmp != null)
                    {
                        p2Tmp.color = Color.white;
                        p2Tmp.text = "<b>PC-2 SWIFT</b>\n<color=#FFA940>Recibiendo salto...</color>";
                    }
                }
                else if (stage == 1 && u >= 0.65f)
                {
                    stage = 2;
                    if (traceTmp != null)
                    {
                        traceTmp.text =
                            "<color=#38F28A>1/3 [LISTO] Dropper activo</color>  |  <color=#38F28A>2/3 [LISTO] Túnel C2</color>\n\n" +
                            "<color=#FFA940><b>3/3 EXFILTRANDO HACIA MÓDULO SWIFT Y SIEM...</b></color>";
                    }
                    if (p2Tmp != null) p2Tmp.text = "<b>PC-2 SWIFT</b>\n<color=#FF3834><b>COMPROMETIDA</b></color>";
                    if (p3Tmp != null)
                    {
                        p3Tmp.color = Color.white;
                        p3Tmp.text = "<b>PC-3 SIEM</b>\n<color=#FF3834><b>ALERTA SILENCIADA</b></color>";
                    }
                }

                yield return null;
            }

            ClearActiveContent();

            SetHeaderTheme(
                DangerRedColor,
                "ROBO CONSUMADO · US$ 81M TRANSFERIDOS",
                "Red plana sin segmentación ni MFA permitió el ataque"
            );

            GameObject robPanel = new GameObject("RobberyResultPanel");
            robPanel.transform.SetParent(_contentContainer, false);
            RectTransform rRect = robPanel.AddComponent<RectTransform>();
            rRect.anchorMin = Vector2.zero;
            rRect.anchorMax = Vector2.one;
            rRect.sizeDelta = Vector2.zero;

            GameObject rBox = CreateStyledCard("RobberyBox", robPanel.transform, new Vector2(0f, 35f), new Vector2(880f, 300f),
                new Color(0.11f, 0.02f, 0.03f, 0.98f), DangerRedColor);

            StringBuilder rsb = new StringBuilder();
            rsb.AppendLine("<color=#FF3834><b>· ROBADO: US$ 81,000,000</b></color> (desviados a Filipinas)");
            rsb.AppendLine("· <b>RETENIDO:</b> US$ 870,000,000 (bloqueados por error tipográfico)");
            rsb.AppendLine("· <b>ENTRADA:</b> Phishing <i>.scr.zip</i> ejecutado en PC 1");
            rsb.AppendLine("<color=#FFA940><b>· CAUSA RAÍZ:</b> Sin Sandbox, sin VLAN, sin MFA</color>");

            CreateLabel("Txt_RobberySummary", rBox.transform, Vector2.zero, new Vector2(820f, 250f),
                rsb.ToString(), 20f, TextAlignmentOptions.Center, Color.white);

            CreateInspectionButton(
                "Btn_GoBoardAfterAttack", robPanel.transform,
                new Vector2(-300f, -175f), new Vector2(280f, 62f),
                "IR AL TABLERO >",
                new Color(0.10f, 0.30f, 0.42f, 0.98f),
                CyanNeonAccent,
                NavigateToSuspectBoardOrDefense
            );

            CreateInspectionButton(
                "Btn_FinalizeAttack", robPanel.transform,
                new Vector2(0f, -175f), new Vector2(290f, 62f),
                "FINALIZAR EVALUACION",
                new Color(0.48f, 0.12f, 0.14f, 0.98f),
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
                new Vector2(300f, -175f), new Vector2(250f, 62f),
                "CERRAR VISTA",
                new Color(0.18f, 0.25f, 0.36f, 0.98f),
                Color.white,
                CloseOverlay
            );

            _activeContentPanel = robPanel;
            ShowOverlay();
            AndesSim.Audio.AudioManager.I?.PlayNarration("Voice_11_RoboConsumado", true, 2.0f);
        }

        public void OpenCredentialsInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            SetHeaderTheme(
                WarningAmberColor,
                "EVIDENCIA #2 · POST-IT CON CLAVES",
                "Puesto 1 · Credenciales críticas expuestas a simple vista"
            );

            GameObject credPanel = new GameObject("CredentialsContentPanel");
            credPanel.transform.SetParent(_contentContainer, false);
            RectTransform cRect = credPanel.AddComponent<RectTransform>();
            cRect.anchorMin = Vector2.zero;
            cRect.anchorMax = Vector2.one;
            cRect.sizeDelta = Vector2.zero;

            GameObject postItBox = CreateStyledCard("PostItVisual", credPanel.transform, new Vector2(-215f, 30f), new Vector2(415f, 290f),
                new Color(0.98f, 0.88f, 0.35f, 0.98f), WarningAmberColor);

            CreateLabel("Txt_PostItTitle", postItBox.transform, new Vector2(0f, 96f), new Vector2(370f, 40f),
                "<b>CLAVES PC-PAGOS-01</b>", 20f, TextAlignmentOptions.Center, new Color(0.10f, 0.10f, 0.10f));

            CreateLabel("Txt_PostItBody", postItBox.transform, new Vector2(0f, -20f), new Vector2(370f, 190f),
                "<b>Usuario:</b> c.mendoza\n" +
                "<b>Clave:</b> <color=#990000>Swift2015$Admin!</color>\n" +
                "<b>Token:</b> 849201\n" +
                "<b>PIN:</b> 9481",
                19.5f, TextAlignmentOptions.Center, new Color(0.12f, 0.12f, 0.12f));

            GameObject diagBox = CreateStyledCard("ForensicDiagBox", credPanel.transform, new Vector2(225f, 30f), new Vector2(415f, 290f),
                AndesTypographyTheme.BgCardGlass, WarningAmberColor);

            CreateLabel("Txt_DiagTitle", diagBox.transform, new Vector2(0f, 96f), new Vector2(370f, 40f),
                "<b>FALLA ISO 27001</b>", 20f, TextAlignmentOptions.Center, WarningAmberColor);

            CreateLabel("Txt_DiagBody", diagBox.transform, new Vector2(0f, -20f), new Vector2(370f, 190f),
                "· Clave escrita en papel visible.\n\n" +
                "· Sin bloqueo físico ni gestor seguro, cualquiera accede al módulo SWIFT.",
                18.5f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

            Action saveEvidence = () =>
            {
                if (GameManager.I != null) GameManager.I.RegisterEvidence("Evidencia_PostIt");
                if (AndesSim.Audio.AudioManager.I != null) AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
            };

            CreateInspectionButton(
                "Btn_SaveAndNextSiem", credPanel.transform,
                new Vector2(-215f, -175f), new Vector2(420f, 62f),
                "GUARDAR Y VER SIEM (PC 3) >",
                new Color(0.10f, 0.38f, 0.22f, 0.98f),
                GreenSuccessColor,
                () => { saveEvidence(); OpenSiemLogInspection(); }
            );

            CreateInspectionButton(
                "Btn_LogEvidencePostIt", credPanel.transform,
                new Vector2(225f, -175f), new Vector2(390f, 62f),
                "SOLO GUARDAR Y CERRAR",
                new Color(0.14f, 0.24f, 0.36f, 0.98f),
                Color.white,
                () => { saveEvidence(); CloseOverlay(); }
            );

            _activeContentPanel = credPanel;
            ShowOverlay();
            AndesSim.Audio.AudioManager.I?.PlayNarration("Voice_13_PostItEvidencia", true, 2.0f);
        }

        public void OpenSwiftInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            SetHeaderTheme(
                CyanNeonAccent,
                "PC 2 · TERMINAL SWIFT",
                "Transferencias internacionales de alto valor sin aislamiento"
            );

            GameObject swiftPanel = new GameObject("SwiftContentPanel");
            swiftPanel.transform.SetParent(_contentContainer, false);
            RectTransform sRect = swiftPanel.AddComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.sizeDelta = Vector2.zero;

            GameObject tableBox = CreateStyledCard("SwiftTableBox", swiftPanel.transform, new Vector2(0f, 35f), new Vector2(880f, 300f),
                AndesTypographyTheme.BgCardGlass, CyanNeonAccent);

            CreateLabel("Txt_SwiftTable", tableBox.transform, Vector2.zero, new Vector2(820f, 250f),
                "<b><color=#00E5FF>OPERACIÓN SWIFT MT103</color></b>\n\n" +
                "· <b>Destino sospechoso:</b> RCBC Filipinas\n" +
                "· <b>Monto en riesgo:</b> <color=#FF5A4E><b>US$ 951,000,000</b></color>\n" +
                "· <b>Vulnerabilidad:</b> <color=#FFA940>Red plana sin VLAN ni autenticación MFA</color>",
                20f, TextAlignmentOptions.Center, Color.white);

            CreateInspectionButton(
                "Btn_SwiftToMail", swiftPanel.transform,
                new Vector2(-220f, -175f), new Vector2(410f, 62f),
                "< VOLVER A CORREO (PC 1)",
                new Color(0.14f, 0.24f, 0.36f, 0.98f),
                Color.white,
                () => OpenEmailInspection(0)
            );

            CreateInspectionButton(
                "Btn_SwiftToSiem", swiftPanel.transform,
                new Vector2(220f, -175f), new Vector2(410f, 62f),
                "SIGUIENTE: VER SIEM (PC 3) >",
                new Color(0.10f, 0.34f, 0.26f, 0.98f),
                GreenSuccessColor,
                OpenSiemLogInspection
            );

            _activeContentPanel = swiftPanel;
            ShowOverlay();
            AndesSim.Audio.AudioManager.I?.PlayNarration("Voice_14_TerminalSwift", true, 2.0f);
        }

        public void OpenSiemLogInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            SetHeaderTheme(
                GreenSuccessColor,
                "PC 3 · EVIDENCIA #3: CONSOLA SIEM",
                "Alertas de red saliente hacia la IP atacante"
            );

            GameObject siemPanel = new GameObject("SiemContentPanel");
            siemPanel.transform.SetParent(_contentContainer, false);
            RectTransform smRect = siemPanel.AddComponent<RectTransform>();
            smRect.anchorMin = Vector2.zero;
            smRect.anchorMax = Vector2.one;
            smRect.sizeDelta = Vector2.zero;

            GameObject logBox = CreateStyledCard("SiemLogBox", siemPanel.transform, new Vector2(0f, 35f), new Vector2(880f, 300f),
                AndesTypographyTheme.BgCardGlass, GreenSuccessColor);

            CreateLabel("Txt_SiemLogs", logBox.transform, Vector2.zero, new Vector2(820f, 250f),
                "<b><color=#38F28A>REGISTROS FORENSES SIEM (29 ENE 2015)</color></b>\n\n" +
                "· <b>09:18 C2 Saliente:</b> <color=#FF5A4E>192.168.1.101 -> 185.130.44.82:443</color>\n" +
                "· <b>Proceso oculto:</b> svchost_upd.exe (Grupo Lazarus)\n" +
                "· <b>Causa raíz:</b> Descarga .scr ejecutada sin Sandbox",
                20f, TextAlignmentOptions.Center, Color.white);

            Action saveSiem = () =>
            {
                if (GameManager.I != null) GameManager.I.RegisterEvidence("Evidencia_LogAccesos");
                if (AndesSim.Audio.AudioManager.I != null) AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
            };

            CreateInspectionButton(
                "Btn_LogSiemAndBoard", siemPanel.transform,
                new Vector2(-220f, -175f), new Vector2(430f, 62f),
                "GUARDAR LOG E IR AL TABLERO >",
                new Color(0.10f, 0.38f, 0.22f, 0.98f),
                GreenSuccessColor,
                () => { saveSiem(); NavigateToSuspectBoardOrDefense(); }
            );

            CreateInspectionButton(
                "Btn_LogEvidenceSiem", siemPanel.transform,
                new Vector2(230f, -175f), new Vector2(370f, 62f),
                "SOLO GUARDAR Y CERRAR",
                new Color(0.14f, 0.24f, 0.36f, 0.98f),
                Color.white,
                () => { saveSiem(); CloseOverlay(); }
            );

            _activeContentPanel = siemPanel;
            ShowOverlay();
            AndesSim.Audio.AudioManager.I?.PlayNarration("Voice_15_ConsolaSiem", true, 2.0f);
        }

        public void OpenSuspectDossierInspection(int suspectIndex)
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            GameObject dossierPanel = new GameObject("SuspectDossierModalPanel");
            dossierPanel.transform.SetParent(_contentContainer, false);
            RectTransform dr = dossierPanel.AddComponent<RectTransform>();
            dr.sizeDelta = new Vector2(920f, 520f);

            SuspectBoardVR board = SuspectBoardVR.I != null ? SuspectBoardVR.I : FindFirstObjectByType<SuspectBoardVR>();
            int total = (board != null && board.Suspects != null && board.Suspects.Count > 0) ? board.Suspects.Count : 1;
            int safeIndex = ((suspectIndex % total) + total) % total;

            SuspectDossierScenario01 suspect = (board != null && board.Suspects != null && safeIndex < board.Suspects.Count)
                ? board.Suspects[safeIndex]
                : null;

            string name = suspect != null ? suspect.fullName : "Sospechoso";
            string role = suspect != null ? suspect.roleTitle : "Personal Banco Andino";
            string badge = suspect != null ? suspect.threatLevelBadge : "INVESTIGACIÓN";
            string incrim = suspect != null ? suspect.incriminatingPoints : "Sin indicios.";
            string exon = suspect != null ? suspect.exoneratingPoints : "Sin descargos.";

            SetHeaderTheme(
                CyanNeonAccent,
                $"SOSPECHOSO #{safeIndex + 1} DE {total} · {name.ToUpper()}",
                $"{role}  ·  <color=#FFA940>[{badge}]</color>"
            );

            GameObject leftBox = CreateStyledCard("LeftBox", dossierPanel.transform, new Vector2(-225f, 35f), new Vector2(430f, 290f),
                new Color(0.14f, 0.05f, 0.06f, 0.96f), DangerRedColor);

            CreateLabel("Txt_LeftTitle", leftBox.transform, new Vector2(0f, 112f), new Vector2(390f, 36f),
                "<b><color=#FF5A4E>INDICIOS EN CONTRA</color></b>",
                18.5f, TextAlignmentOptions.Center, DangerRedColor);

            CreateLabel("Txt_LeftContent", leftBox.transform, new Vector2(0f, -15f), new Vector2(380f, 210f),
                incrim, 17f, TextAlignmentOptions.Center, new Color(0.96f, 0.92f, 0.92f));

            GameObject rightBox = CreateStyledCard("RightBox", dossierPanel.transform, new Vector2(225f, 35f), new Vector2(430f, 290f),
                new Color(0.05f, 0.14f, 0.09f, 0.96f), GreenSuccessColor);

            CreateLabel("Txt_RightTitle", rightBox.transform, new Vector2(0f, 112f), new Vector2(390f, 36f),
                "<b><color=#38F28A>DESCARGOS A FAVOR</color></b>",
                18.5f, TextAlignmentOptions.Center, GreenSuccessColor);

            CreateLabel("Txt_RightContent", rightBox.transform, new Vector2(0f, -15f), new Vector2(380f, 210f),
                exon, 17f, TextAlignmentOptions.Center, new Color(0.90f, 0.98f, 0.92f));

            CreateInspectionButton(
                "Btn_PrevSuspect", dossierPanel.transform,
                new Vector2(-345f, -175f), new Vector2(180f, 60f),
                "< ANTERIOR",
                new Color(0.14f, 0.22f, 0.35f, 0.96f),
                Color.white,
                () => OpenSuspectDossierInspection((safeIndex + total - 1) % total)
            );

            CreateInspectionButton(
                "Btn_SelectHypothesis", dossierPanel.transform,
                new Vector2(0f, -175f), new Vector2(470f, 60f),
                $"ELEGIR: {name.ToUpper()}",
                new Color(0.10f, 0.38f, 0.22f, 0.98f),
                GreenSuccessColor,
                () =>
                {
                    if (board != null)
                    {
                        board.DisplaySuspect(safeIndex);
                        board.OnSelectHypothesisClicked();
                    }
                    if (AndesSim.Audio.AudioManager.I != null)
                    {
                        AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
                    }
                    CloseOverlay();
                }
            );

            CreateInspectionButton(
                "Btn_NextSuspect", dossierPanel.transform,
                new Vector2(345f, -175f), new Vector2(180f, 60f),
                "SIGUIENTE >",
                new Color(0.14f, 0.22f, 0.35f, 0.96f),
                Color.white,
                () => OpenSuspectDossierInspection((safeIndex + 1) % total)
            );

            _activeContentPanel = dossierPanel;
            ShowOverlay();
            AndesSim.Audio.AudioManager.I?.PlayNarration("Voice_19_TableroSospechosos", false, 12.0f);
        }

        private void NavigateToSuspectBoardOrDefense()
        {
            GameObject tpObj = GameObject.Find("Teleport_TableroSospechosos");
            TeleportPoint tp = tpObj != null ? tpObj.GetComponent<TeleportPoint>() : null;
            if (PlayerTeleportController.I != null && tp != null)
            {
                CloseOverlay();
                PlayerTeleportController.I.TeleportToPoint(tp);
                return;
            }
            OpenSuspectDossierInspection(0);
        }

        private void NavigateToDefenseConsole()
        {
            GameObject tpObj = GameObject.Find("Teleport_ConsolaDefensa");
            TeleportPoint tp = tpObj != null ? tpObj.GetComponent<TeleportPoint>() : null;
            if (PlayerTeleportController.I != null && tp != null)
            {
                CloseOverlay();
                PlayerTeleportController.I.TeleportToPoint(tp);
                return;
            }
            NavigateToSuspectBoardOrDefense();
        }

        private void NavigateToMainMenu()
        {
            CloseOverlay();
            if (VrStartMenuController.I != null)
            {
                VrStartMenuController.I.ShowMenu();
                GameObject lobbyTpObj = GameObject.Find("Teleport_PuertaEntrada");
                TeleportPoint lobbyTp = lobbyTpObj != null ? lobbyTpObj.GetComponent<TeleportPoint>() : null;
                if (PlayerTeleportController.I != null && lobbyTp != null)
                {
                    PlayerTeleportController.I.TeleportToPoint(lobbyTp);
                }
            }
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
                    spawnPos.y = _targetCamera.transform.position.y - 0.04f;

                    _overlayRoot.transform.position = spawnPos;
                    _overlayRoot.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }
                _overlayRoot.SetActive(true);
                _panelEntrance?.PlayEntrance();
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
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
            if (_activeContentPanel != null)
            {
                _activeContentPanel.SetActive(false);
                _activeContentPanel.transform.SetParent(null);
                if (Application.isPlaying) Destroy(_activeContentPanel);
                else DestroyImmediate(_activeContentPanel);
                _activeContentPanel = null;
            }
        }

        private GameObject CreateStyledCard(string name, Transform parent, Vector2 pos, Vector2 size, Color bgColor, Color topAccentColor)
        {
            GameObject cardGO = new GameObject(name);
            cardGO.transform.SetParent(parent, false);
            RectTransform rect = cardGO.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Image bg = cardGO.AddComponent<Image>();
            bg.color = bgColor;

            GameObject bar = new GameObject("TopAccent");
            bar.transform.SetParent(cardGO.transform, false);
            RectTransform barR = bar.AddComponent<RectTransform>();
            barR.anchorMin = new Vector2(0f, 1f);
            barR.anchorMax = new Vector2(1f, 1f);
            barR.anchoredPosition = new Vector2(0f, -3f);
            barR.sizeDelta = new Vector2(0f, 5f);
            Image barImg = bar.AddComponent<Image>();
            barImg.color = topAccentColor;

            return cardGO;
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
            int lastInvokedFrame = -1;
            Action invokeOncePerFrame = () =>
            {
                if (onClick == null || lastInvokedFrame == Time.frameCount) return;
                lastInvokedFrame = Time.frameCount;
                onClick.Invoke();
            };
            if (onClick != null) btn.onClick.AddListener(() => invokeOncePerFrame());

            BoxCollider boxCol = btnGO.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(size.x, size.y, 45f);
            boxCol.center = new Vector3(0f, 0f, -15f);
            boxCol.isTrigger = true;

            GazeInteractable gaze = btnGO.AddComponent<GazeInteractable>();
            gaze.interactableLabel = text.Replace("<b>", "").Replace("</b>", "").Split('\n')[0];
            gaze.dwellTime = 1.1f;
            if (onClick != null) gaze.onClick.AddListener(() => invokeOncePerFrame());

            HolographicCardHover hover = btnGO.AddComponent<HolographicCardHover>();
            hover.Configure(1.045f, 14f);

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            RectTransform tRect = txtGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-14f, -6f);

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            AndesTypographyTheme.StyleButtonLabel(tmp, textColor, 16.5f);

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
            bg.color = new Color(0.16f, 0.13f, 0.08f, 0.96f);

            Button btn = btnGO.AddComponent<Button>();

            BoxCollider boxCol = btnGO.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(size.x, size.y, 25f);
            boxCol.center = new Vector3(0f, 0f, -5f);
            boxCol.isTrigger = true;

            GazeInteractable gaze = btnGO.AddComponent<GazeInteractable>();
            gaze.interactableLabel = $"Inspeccionar {signalId}";
            gaze.dwellTime = 1.0f;

            HolographicCardHover hover = btnGO.AddComponent<HolographicCardHover>();
            hover.Configure(1.055f, 14f);

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            RectTransform tRect = txtGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-14f, -8f);

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            AndesTypographyTheme.StyleButtonLabel(tmp, WarningAmberColor, 17.5f);

            int lastMarkedFrame = -1;
            Action markAction = () =>
            {
                if (lastMarkedFrame == Time.frameCount) return;
                lastMarkedFrame = Time.frameCount;

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
                bg.color = new Color(0.08f, 0.35f, 0.19f, 0.96f);
                tmp.text = "<b>[OK] PISTA LISTA</b>";
                AndesTypographyTheme.StyleBadge(tmp, GreenSuccessColor, 18f);
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
            AndesTypographyTheme.StyleBodyReadable(tmp, alignment, color, fontSize, 10f);
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
