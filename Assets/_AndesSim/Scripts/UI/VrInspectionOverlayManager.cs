using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
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

        [Header("--- Anclaje VR y Zoom ---")]
        [SerializeField] private float distanceFromCamera = 0.68f;
        public const float NormalScale = 0.00072f;
        public const float EnlargedScale = 0.00084f; // Escala ampliada ergonómica sin recortar bordes ni chocar con escritorio
        public const float NormalDistance = 0.68f;
        public const float EnlargedDistance = 0.58f;

        private int _emailSubPart = 0;
        private bool _isEnlarged = false;
        private Coroutine _zoomRoutine;

        // Raíz del Canvas de Inspección
        private GameObject _overlayRoot;
        private Canvas _overlayCanvas;
        private CanvasGroup _overlayCanvasGroup;
        private RectTransform _mainPanelRect;
        private HolographicPanelEntrance _panelEntrance;
        private BoxCollider _overlayBackplateCollider;
        private Image _topGlowBarImg;
        private Camera _targetCamera;
        private bool _isOpen = false;
        private bool _hasAnchorPose = false;
        private Vector3 _anchorForward = Vector3.forward;

        // Registro de objetos 3D temporalmente ocultos/deshabilitados mientras se revisa una PC
        private bool _sceneObstructionsSuppressed = false;
        private readonly List<Renderer> _suppressedRenderers = new List<Renderer>();
        private readonly List<Canvas> _suppressedCanvases = new List<Canvas>();
        private readonly List<Collider> _suppressedColliders = new List<Collider>();
        private readonly List<Collider2D> _suppressedColliders2D = new List<Collider2D>();
        private readonly List<Light> _suppressedLights = new List<Light>();
        private readonly HashSet<int> _suppressedComponentIds = new HashSet<int>();

        public bool IsOpen => _isOpen;
        public bool IsEnlarged => _isEnlarged;

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

        private void OnDisable()
        {
            RestoreSceneObstructionsAfterOverlay();
        }

        private void OnDestroy()
        {
            RestoreSceneObstructionsAfterOverlay();
        }

        private void Update()
        {
            // Atajos de teclado: 'Q' para Zoom; 'E' o 'Escape' para Cerrar / Minimizar
            bool qPressed = false;
            bool closePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame) qPressed = true;
                if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame) closePressed = true;
            }
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.Q)) qPressed = true;
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)) closePressed = true;
            }
            catch { }

            if (closePressed && _isOpen)
            {
                CloseOverlay();
                return;
            }

            if (qPressed && _isOpen)
            {
                ToggleEnlargeZoom();
            }

            if (!_isOpen || _overlayRoot == null) return;

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
                if (_targetCamera == null) return;
            }

            // Si el usuario gira la cabeza completamente fuera del panel, cerrarlo automáticamente.
            Vector3 dirToPanel = (_overlayRoot.transform.position - _targetCamera.transform.position).normalized;
            float angle = Vector3.Angle(_targetCamera.transform.forward, dirToPanel);
            if (angle > 85f)
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
            canvasRect.localScale = Vector3.one * NormalScale;

            EnsureEventSystemExists();

            GameObject panelGO = new GameObject("InspectionFrame");
            panelGO.transform.SetParent(_overlayRoot.transform, false);
            panelGO.layer = _overlayRoot.layer;
            _mainPanelRect = panelGO.AddComponent<RectTransform>();
            _mainPanelRect.anchorMin = Vector2.zero;
            _mainPanelRect.anchorMax = Vector2.one;
            _mainPanelRect.sizeDelta = Vector2.zero;

            _overlayCanvasGroup = panelGO.AddComponent<CanvasGroup>();
            _panelEntrance = panelGO.AddComponent<HolographicPanelEntrance>();
            _panelEntrance.Configure(0.24f, 0.90f, 16f, _overlayCanvasGroup);

            // Colisionador trasero no interactivo: estabiliza la retícula frente al HUD sin bloquear botones hijos
            _overlayBackplateCollider = panelGO.AddComponent<BoxCollider>();
            _overlayBackplateCollider.size = new Vector3(1020f, 760f, 12f);
            _overlayBackplateCollider.center = new Vector3(0f, 0f, 16f);
            _overlayBackplateCollider.isTrigger = true;

            Image bgImage = panelGO.AddComponent<Image>();
            bgImage.color = new Color(0.012f, 0.030f, 0.055f, 1.0f); // 100% opaco para eliminar transparencia trasera

            Outline panelOutline = panelGO.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.000f, 0.898f, 1.000f, 0.50f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

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

            // Botón de Zoom / Agrandar con atajo [Q]
            CreateInspectionButton(
                "Btn_ToggleZoom", panelGO.transform,
                new Vector2(285f, 326f), new Vector2(170f, 40f),
                "[Q] AGRANDAR",
                new Color(0.08f, 0.35f, 0.42f, 0.98f),
                CyanNeonAccent,
                ToggleEnlargeZoom
            );

            // Botón Superior Derecho: CERRAR INMEDIATO
            CreateInspectionButton(
                "Btn_CerrarTopRight", panelGO.transform,
                new Vector2(436f, 326f), new Vector2(80f, 40f),
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
                    ? "Analiza el mensaje recibido por Carla Mendoza"
                    : "Marca las 3 pistas y decide tu accion"
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
                "1. LEER CORREO",
                part1Bg, _emailSubPart == 0 ? CyanNeonAccent : Color.white,
                () => OpenEmailInspection(0)
            );

            CreateInspectionButton(
                "Btn_TabParte2", emailPanel.transform,
                new Vector2(230f, 215f), new Vector2(435f, 48f),
                "2. PISTAS Y ACCION",
                part2Bg, _emailSubPart == 1 ? WarningAmberColor : Color.white,
                () => OpenEmailInspection(1)
            );

            if (_emailSubPart == 0)
            {
                GameObject bodyBox = CreateStyledCard("EmailReadCard", emailPanel.transform, new Vector2(0f, 35f), new Vector2(880f, 260f), AndesTypographyTheme.BgCardGlass, CyanNeonAccent);

                CreateLabel("Txt_MailLines", bodyBox.transform, Vector2.zero, new Vector2(820f, 220f),
                    "<b>De:</b> rasel.ahlam@<color=#FFA940>career-applicants-bd.com</color>\n" +
                    "<b>Asunto:</b> <color=#FF5A4E>URGENTE · CV Especialista SWIFT</color>\n\n" +
                    "<i>\"Carla: adjunto mi CV urgente para pagos.\"</i>\n\n" +
                    "<b>Adjunto:</b> <color=#00E5FF>185.130.44.82/CV_2015.scr.zip</color>",
                    20f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

                CreateInspectionButton(
                    "Btn_NextToPart2", emailPanel.transform,
                    new Vector2(-225f, -165f), new Vector2(420f, 68f),
                    "VER PISTAS >",
                    new Color(0.10f, 0.34f, 0.44f, 0.98f),
                    CyanNeonAccent,
                    () => OpenEmailInspection(1)
                );

                CreateInspectionButton(
                    "Btn_QuickOpenLink", emailPanel.transform,
                    new Vector2(225f, -165f), new Vector2(420f, 68f),
                    "ABRIR ENLACE",
                    new Color(0.48f, 0.12f, 0.14f, 0.98f),
                    Color.white,
                    TriggerAttackInfectionSequence
                );
            }
            else
            {
                CreateLabel("Lbl_StepHint", emailPanel.transform, new Vector2(0f, 155f), new Vector2(880f, 38f),
                    "Marca las 3 pistas y elige una accion:",
                    19f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

                CreateSignalButton("Btn_Signal1", emailPanel.transform, new Vector2(-300f, 70f), new Vector2(285f, 108f),
                    "<b>1. DOMINIO FALSO</b>\n<size=15><color=#FFA940>@career-applicants</color></size>",
                    PhishingInspector.SignalRemitenteId);

                CreateSignalButton("Btn_Signal2", emailPanel.transform, new Vector2(0f, 70f), new Vector2(285f, 108f),
                    "<b>2. URGENCIA</b>\n<size=15><color=#FFA940>Presion inmediata</color></size>",
                    PhishingInspector.SignalUrgenciaId);

                CreateSignalButton("Btn_Signal3", emailPanel.transform, new Vector2(300f, 70f), new Vector2(285f, 108f),
                    "<b>3. ARCHIVO .SCR</b>\n<size=15><color=#FFA940>IP 185.130.44.82</color></size>",
                    PhishingInspector.SignalEnlaceId);

                CreateInspectionButton(
                    "Btn_AttackOpenLink", emailPanel.transform,
                    new Vector2(-225f, -55f), new Vector2(420f, 74f),
                    "<color=#FF5A4E><b>ABRIR ENLACE (ATAQUE)</b></color>",
                    new Color(0.48f, 0.12f, 0.14f, 0.98f),
                    Color.white,
                    TriggerAttackInfectionSequence
                );

                CreateInspectionButton(
                    "Btn_DefenseReport", emailPanel.transform,
                    new Vector2(225f, -55f), new Vector2(420f, 74f),
                    "<color=#38F28A><b>AISLAR EN SANDBOX</b></color>",
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
                    "VER POST-IT (PC 1) >",
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
                "CORREO EN CUARENTENA",
                "Amenaza aislada antes de entrar a la red"
            );

            GameObject qPanel = new GameObject("QuarantineContentPanel");
            qPanel.transform.SetParent(_contentContainer, false);
            RectTransform qRect = qPanel.AddComponent<RectTransform>();
            qRect.anchorMin = Vector2.zero;
            qRect.anchorMax = Vector2.one;
            qRect.sizeDelta = Vector2.zero;

            GameObject boxGO = CreateStyledCard("SandboxLogBox", qPanel.transform, new Vector2(0f, 35f), new Vector2(880f, 290f),
                new Color(0.015f, 0.055f, 0.055f, 0.995f), GreenSuccessColor);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<size=24><b><color=#38F28A>✓ CONTENCIÓN EXITOSA EN SANDBOX</color></b></size>\n");
            sb.AppendLine("<size=21>• <b>Sandbox:</b> Adjunto <i>.scr.zip</i> bloqueado y detonado en aislamiento.</size>");
            sb.AppendLine("<size=21>• <b>Firewall:</b> Salida hacia <i>185.130.44.82</i> clausurada preventivamente.</size>\n");
            sb.AppendLine("<size=22><color=#38F28A><b>[OK] 16 BUZONES PROTEGIDOS · US$ 0 ROBADOS</b></color></size>");

            CreateLabel("Txt_QuarantineLog", boxGO.transform, Vector2.zero, new Vector2(840f, 250f),
                sb.ToString(), 21f, TextAlignmentOptions.Center, Color.white);

            CreateInspectionButton(
                "Btn_GoDefenseConsole", qPanel.transform,
                new Vector2(-305f, -175f), new Vector2(280f, 62f),
                "CONSOLA DEFENSA >",
                new Color(0.08f, 0.28f, 0.36f, 0.98f),
                CyanNeonAccent,
                NavigateToDefenseConsole
            );

            CreateInspectionButton(
                "Btn_ConfirmDefense", qPanel.transform,
                new Vector2(0f, -175f), new Vector2(290f, 62f),
                "FINALIZAR",
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
                "MALWARE EN EJECUCION",
                "Propagacion en la red del banco"
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
                "<color=#FF5A4E><b>1/3 · DESCARGA EN CURSO</b></color>\n<size=17>Ejecutando CV_2015.scr en PC 1...</size>",
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
                        float stolenLive = Mathf.Lerp(12000000f, 81000000f, (u - 0.65f) / 0.35f);
                        traceTmp.text =
                            "<color=#38F28A>✓ Dropper detonado</color> | <color=#38F28A>✓ C2 185.130.44.82 activo</color>\n\n" +
                            $"<color=#FF3834><b>💸 ROBO SWIFT EN VIVO: US$ {stolenLive:N0} ROBADOS</b></color>";
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
                "ROBO SWIFT · US$ 81M",
                "Red plana sin MFA permitio el ataque"
            );

            GameObject robPanel = new GameObject("RobberyResultPanel");
            robPanel.transform.SetParent(_contentContainer, false);
            RectTransform rRect = robPanel.AddComponent<RectTransform>();
            rRect.anchorMin = Vector2.zero;
            rRect.anchorMax = Vector2.one;
            rRect.sizeDelta = Vector2.zero;

            GameObject rBox = CreateStyledCard("RobberyBox", robPanel.transform, new Vector2(0f, 35f), new Vector2(880f, 300f),
                new Color(0.015f, 0.035f, 0.055f, 0.995f), DangerRedColor);

            StringBuilder rsb = new StringBuilder();
            rsb.AppendLine("<size=24><color=#FF3834><b>💸 ROBO SWIFT CONSUMADO: US$ 81,000,000.00</b></color></size>\n");
            rsb.AppendLine("<size=20>• <b>Exfiltración:</b> Fondos desviados a RCBC Makati (Filipinas) vía C2 185.130.44.82.</size>");
            rsb.AppendLine("<size=20>• <b>Causa raíz:</b> Red plana sin VLAN · Sin Sandbox perimetral · Sin token MFA.</size>\n");
            rsb.AppendLine("<size=21><color=#38F28A>• <b>Lección:</b> En Modo Defensa, aislar el adjunto salva el 100% de los fondos.</color></size>");

            CreateLabel("Txt_RobberySummary", rBox.transform, Vector2.zero, new Vector2(840f, 260f),
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
                "FINALIZAR",
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
                "CERRAR",
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
                "EVIDENCIA 2 · POST-IT",
                "Claves SWIFT expuestas a simple vista"
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
                "<b>SWIFT-01</b>", 20f, TextAlignmentOptions.Center, new Color(0.10f, 0.10f, 0.10f));

            CreateLabel("Txt_PostItBody", postItBox.transform, new Vector2(0f, -20f), new Vector2(370f, 190f),
                "<b>Usuario:</b> c.mendoza\n" +
                "<b>Clave:</b> <color=#990000>Andino15$!</color>\n" +
                "<b>PIN:</b> 9481",
                20f, TextAlignmentOptions.Center, new Color(0.12f, 0.12f, 0.12f));

            GameObject diagBox = CreateStyledCard("ForensicDiagBox", credPanel.transform, new Vector2(225f, 30f), new Vector2(415f, 290f),
                AndesTypographyTheme.BgCardGlass, WarningAmberColor);

            CreateLabel("Txt_DiagTitle", diagBox.transform, new Vector2(0f, 96f), new Vector2(370f, 40f),
                "<b>SEGURIDAD FISICA</b>", 20f, TextAlignmentOptions.Center, WarningAmberColor);

            CreateLabel("Txt_DiagBody", diagBox.transform, new Vector2(0f, -20f), new Vector2(370f, 190f),
                "· Clave anotada en papel.\n\n" +
                "· Cualquiera puede usar la terminal SWIFT.",
                19f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

            Action saveEvidence = () =>
            {
                if (GameManager.I != null) GameManager.I.RegisterEvidence("Evidencia_PostIt");
                if (AndesSim.Audio.AudioManager.I != null) AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
            };

            CreateInspectionButton(
                "Btn_SaveAndNextSiem", credPanel.transform,
                new Vector2(-215f, -175f), new Vector2(420f, 62f),
                "GUARDAR Y VER SIEM >",
                new Color(0.10f, 0.38f, 0.22f, 0.98f),
                GreenSuccessColor,
                () => { saveEvidence(); OpenSiemLogInspection(); }
            );

            CreateInspectionButton(
                "Btn_LogEvidencePostIt", credPanel.transform,
                new Vector2(225f, -175f), new Vector2(390f, 62f),
                "GUARDAR Y CERRAR",
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
                "PC 2 · TERMINAL DE PAGOS SWIFT",
                "Monitoreo de transferencias y detección de exfiltración"
            );

            GameObject swiftPanel = new GameObject("SwiftContentPanel");
            swiftPanel.transform.SetParent(_contentContainer, false);
            RectTransform sRect = swiftPanel.AddComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.sizeDelta = Vector2.zero;

            // Tabla visual superior de transferencias MT103
            GameObject tableBox = CreateStyledCard("SwiftTableBox", swiftPanel.transform, new Vector2(0f, 130f), new Vector2(880f, 150f),
                new Color(0.015f, 0.045f, 0.075f, 0.995f), CyanNeonAccent);

            CreateLabel("Txt_SwiftTable", tableBox.transform, Vector2.zero, new Vector2(840f, 130f),
                "<b><color=#00E5FF>OPERACIONES SWIFT INTERNACIONALES (MT103)</color></b>\n" +
                "OP #9481 | RESERVA FEDERAL NY   US$ 1,250,000   <color=#38F28A><b>AUTORIZADO [OK]</b></color>\n" +
                "OP #9482 | BARCLAYS LONDRES     US$   480,000   <color=#38F28A><b>AUTORIZADO [OK]</b></color>\n" +
                "<color=#FF5A4E><b>OP #9483 | RCBC MAKATI MANILA   US$ 81,000,000   [ANOMALÍA LAZARUS]</b></color>",
                18.5f, TextAlignmentOptions.Center, Color.white);

            // Las 3 opciones interactivas para marcar
            CreateLabel("Lbl_SwiftHint", swiftPanel.transform, new Vector2(0f, 32f), new Vector2(880f, 30f),
                "Selecciona las opciones a auditar:", 17f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

            CreateOptionCheckButton("Btn_OptSwift1", swiftPanel.transform, new Vector2(-295f, -30f), new Vector2(280f, 82f),
                "1. DESTINO ANÓMALO", "RCBC Makati Filipinas");

            CreateOptionCheckButton("Btn_OptSwift2", swiftPanel.transform, new Vector2(0f, -30f), new Vector2(280f, 82f),
                "2. RED PLANA EXPUESTA", "Sin VLAN con PC 1");

            CreateOptionCheckButton("Btn_OptSwift3", swiftPanel.transform, new Vector2(295f, -30f), new Vector2(280f, 82f),
                "3. SIN TOKEN MFA", "Falta doble factor físico");

            // Botones de acción inferior
            CreateInspectionButton(
                "Btn_SimulateTheft", swiftPanel.transform,
                new Vector2(-225f, -135f), new Vector2(420f, 66f),
                "<color=#FF5A4E><b>SIMULAR ROBO SWIFT</b></color>",
                new Color(0.48f, 0.12f, 0.14f, 0.98f),
                Color.white,
                TriggerAttackInfectionSequence
            );

            CreateInspectionButton(
                "Btn_BlockSwiftDefense", swiftPanel.transform,
                new Vector2(225f, -135f), new Vector2(420f, 66f),
                "<color=#38F28A><b>BLOQUEAR Y PROTEGER FONDOS</b></color>",
                new Color(0.08f, 0.34f, 0.20f, 0.98f),
                GreenSuccessColor,
                () =>
                {
                    if (MailQuarantine.I != null) MailQuarantine.I.TriggerQuarantineDefense();
                    OpenQuarantineInspection();
                }
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
                "PC 3 · CONSOLA SIEM DE SEGURIDAD (SOC)",
                "Registro de intrusión y correlación de alertas de red"
            );

            GameObject siemPanel = new GameObject("SiemContentPanel");
            siemPanel.transform.SetParent(_contentContainer, false);
            RectTransform smRect = siemPanel.AddComponent<RectTransform>();
            smRect.anchorMin = Vector2.zero;
            smRect.anchorMax = Vector2.one;
            smRect.sizeDelta = Vector2.zero;

            // Tabla visual superior de logs SIEM
            GameObject logBox = CreateStyledCard("SiemLogBox", siemPanel.transform, new Vector2(0f, 130f), new Vector2(880f, 150f),
                new Color(0.015f, 0.045f, 0.055f, 0.995f), GreenSuccessColor);

            CreateLabel("Txt_SiemLogs", logBox.transform, Vector2.zero, new Vector2(840f, 130f),
                "<b><color=#38F28A>REGISTROS DE SEGURIDAD SOC · 29 ENE 2015</color></b>\n" +
                "[09:14] SMTP: Correo malicioso recibido en PC 1 (CV_2015.scr.zip)\n" +
                "[09:18] HOST: Dropper inyecta proceso <color=#FF5A4E>svchost_upd.exe (PID 4820)</color>\n" +
                "[09:21] FIREWALL: <color=#FF5A4E>Salida TCP 443 hacia C2 185.130.44.82 activa</color>",
                18f, TextAlignmentOptions.Center, Color.white);

            // Las 3 opciones interactivas para marcar
            CreateLabel("Lbl_SiemHint", siemPanel.transform, new Vector2(0f, 32f), new Vector2(880f, 30f),
                "Selecciona los indicadores de compromiso:", 17f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

            CreateOptionCheckButton("Btn_OptSiem1", siemPanel.transform, new Vector2(-295f, -30f), new Vector2(280f, 82f),
                "1. IP HOSTIL C2", "185.130.44.82");

            CreateOptionCheckButton("Btn_OptSiem2", siemPanel.transform, new Vector2(0f, -30f), new Vector2(280f, 82f),
                "2. PROCESO INYECTADO", "svchost_upd.exe (PID 4820)");

            CreateOptionCheckButton("Btn_OptSiem3", siemPanel.transform, new Vector2(295f, -30f), new Vector2(280f, 82f),
                "3. CANAL SSL/443", "Túnel cifrado malicioso");

            Action saveSiem = () =>
            {
                if (GameManager.I != null) GameManager.I.RegisterEvidence("Evidencia_LogAccesos");
                if (AndesSim.Audio.AudioManager.I != null) AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
            };

            // Botones de acción inferior
            CreateInspectionButton(
                "Btn_BlockIpDefense", siemPanel.transform,
                new Vector2(-225f, -135f), new Vector2(420f, 66f),
                "<color=#38F28A><b>BLOQUEAR IP EN FIREWALL</b></color>",
                new Color(0.08f, 0.34f, 0.20f, 0.98f),
                GreenSuccessColor,
                () => { saveSiem(); OpenQuarantineInspection(); }
            );

            CreateInspectionButton(
                "Btn_LogSiemAndBoard", siemPanel.transform,
                new Vector2(225f, -135f), new Vector2(420f, 66f),
                "GUARDAR EN TABLERO >",
                new Color(0.10f, 0.32f, 0.44f, 0.98f),
                CyanNeonAccent,
                () => { saveSiem(); NavigateToSuspectBoardOrDefense(); }
            );

            CreateInspectionButton(
                "Btn_LogEvidenceSiem", siemPanel.transform,
                new Vector2(230f, -175f), new Vector2(370f, 62f),
                "GUARDAR Y CERRAR",
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
            SuspectBoardVR board = SuspectBoardVR.I != null ? SuspectBoardVR.I : FindFirstObjectByType<SuspectBoardVR>(FindObjectsInactive.Include);
            if (board != null)
            {
                int boardTotal = (board.Suspects != null && board.Suspects.Count > 0) ? board.Suspects.Count : 1;
                int clampedIndex = ((suspectIndex % boardTotal) + boardTotal) % boardTotal;

                // Mantener la interacción dentro de la misma pizarra (Tablero_Sospechosos_VR),
                // sin abrir un panel flotante externo sobre ella.
                if (_isOpen)
                {
                    CloseOverlay();
                }

                board.DisplaySuspect(clampedIndex);
                AndesSim.Audio.AudioManager.I?.PlayGazeClick();
                return;
            }

            EnsureOverlayCanvas();
            ClearActiveContent();

            GameObject dossierPanel = new GameObject("SuspectDossierModalPanel");
            dossierPanel.transform.SetParent(_contentContainer, false);
            RectTransform dr = dossierPanel.AddComponent<RectTransform>();
            dr.sizeDelta = new Vector2(920f, 520f);

            int total = 1;
            int safeIndex = ((suspectIndex % total) + total) % total;

            SuspectDossierScenario01 suspect = null;

            string name = suspect != null ? suspect.fullName : "Sospechoso";
            string role = suspect != null ? suspect.roleTitle : "Personal Banco Andino";
            string badge = suspect != null ? suspect.threatLevelBadge : "INVESTIGACIÓN";
            string incrim = suspect != null ? suspect.incriminatingPoints : "Sin indicios.";
            string exon = suspect != null ? suspect.exoneratingPoints : "Sin descargos.";

            SetHeaderTheme(
                CyanNeonAccent,
                $"SOSPECHOSO {safeIndex + 1}/{total} · {name.ToUpper()}",
                $"{role}  ·  <color=#FFA940>{badge}</color>"
            );

            GameObject leftBox = CreateStyledCard("LeftBox", dossierPanel.transform, new Vector2(-225f, 35f), new Vector2(430f, 290f),
                new Color(0.14f, 0.05f, 0.06f, 0.96f), DangerRedColor);

            CreateLabel("Txt_LeftTitle", leftBox.transform, new Vector2(0f, 112f), new Vector2(390f, 36f),
                "<b><color=#FF5A4E>EN CONTRA</color></b>",
                18.5f, TextAlignmentOptions.Center, DangerRedColor);

            CreateLabel("Txt_LeftContent", leftBox.transform, new Vector2(0f, -15f), new Vector2(380f, 210f),
                incrim, 17f, TextAlignmentOptions.Center, new Color(0.96f, 0.92f, 0.92f));

            GameObject rightBox = CreateStyledCard("RightBox", dossierPanel.transform, new Vector2(225f, 35f), new Vector2(430f, 290f),
                new Color(0.05f, 0.14f, 0.09f, 0.96f), GreenSuccessColor);

            CreateLabel("Txt_RightTitle", rightBox.transform, new Vector2(0f, 112f), new Vector2(390f, 36f),
                "<b><color=#38F28A>A FAVOR</color></b>",
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
            bool wasAlreadyOpen = _isOpen;
            _isOpen = true;

            if (_overlayRoot != null)
            {
                EnsureEventSystemExists();
                if (_targetCamera == null) _targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
                if (_targetCamera != null && _overlayCanvas != null)
                {
                    _overlayCanvas.worldCamera = _targetCamera;
                }

                distanceFromCamera = _isEnlarged ? EnlargedDistance : NormalDistance;
                RectTransform canvasRect = _overlayRoot.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    canvasRect.localScale = Vector3.one * (_isEnlarged ? EnlargedScale : NormalScale);
                }

                // Capturar anclaje ergonómico cómodo al abrir; mantener estable al cambiar de pestaña interna
                if (!wasAlreadyOpen || !_hasAnchorPose)
                {
                    CaptureErgonomicAnchorFromCamera();
                }

                RepositionOverlay();
                SuppressSceneObstructionsForOverlay();
                UpdateZoomButtonLabel();

                _overlayRoot.SetActive(true);
                if (!wasAlreadyOpen)
                {
                    _panelEntrance?.PlayEntrance();
                }
            }
            else
            {
                SuppressSceneObstructionsForOverlay();
            }

            if (!wasAlreadyOpen && AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayLaserScan(transform.position);
            }
        }

        public void CloseOverlay()
        {
            if (_zoomRoutine != null)
            {
                StopCoroutine(_zoomRoutine);
                _zoomRoutine = null;
            }
            _isOpen = false;
            _isEnlarged = false;
            _hasAnchorPose = false;
            distanceFromCamera = NormalDistance;

            // Restaurar 100% de monitores físicos, balizas, mobiliario, colisionadores y visuales 3D
            RestoreSceneObstructionsAfterOverlay();

            if (_overlayRoot != null)
            {
                RectTransform canvasRect = _overlayRoot.GetComponent<RectTransform>();
                if (canvasRect != null) canvasRect.localScale = Vector3.one * NormalScale;
                _overlayRoot.SetActive(false);
            }
            ClearActiveContent();

            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayGazeClick();
            }
        }

        public void ToggleEnlargeZoom()
        {
            SetEnlargeZoom(!_isEnlarged);
        }

        public void SetEnlargeZoom(bool enlarged)
        {
            _isEnlarged = enlarged;
            if (_zoomRoutine != null) StopCoroutine(_zoomRoutine);
            if (gameObject.activeInHierarchy)
            {
                _zoomRoutine = StartCoroutine(AnimateZoomRoutine(_isEnlarged));
            }
            else
            {
                ApplyZoomImmediate(_isEnlarged);
            }

            UpdateZoomButtonLabel();

            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayGazeClick();
            }
        }

        private void UpdateZoomButtonLabel()
        {
            if (_overlayRoot == null) return;
            Transform btnTr = _overlayRoot.transform.Find("InspectionFrame/Btn_ToggleZoom");
            if (btnTr != null)
            {
                var tmp = btnTr.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.text = _isEnlarged ? "<b>[Q] REDUCIR [-]</b>" : "<b>[Q] AGRANDAR [+]</b>";
                }
            }
        }

        private void ApplyZoomImmediate(bool enlarged)
        {
            if (_overlayRoot == null) return;
            RectTransform canvasRect = _overlayRoot.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.localScale = Vector3.one * (enlarged ? EnlargedScale : NormalScale);
            }
            distanceFromCamera = enlarged ? EnlargedDistance : NormalDistance;
            RepositionOverlay();
        }

        private System.Collections.IEnumerator AnimateZoomRoutine(bool enlarged)
        {
            if (_overlayRoot == null) yield break;

            RectTransform canvasRect = _overlayRoot.GetComponent<RectTransform>();
            float startScale = canvasRect != null ? canvasRect.localScale.x : NormalScale;
            float targetScale = enlarged ? EnlargedScale : NormalScale;

            float startDist = distanceFromCamera;
            float targetDist = enlarged ? EnlargedDistance : NormalDistance;

            float elapsed = 0f;
            float duration = 0.20f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (canvasRect != null)
                {
                    canvasRect.localScale = Vector3.one * Mathf.Lerp(startScale, targetScale, t);
                }

                distanceFromCamera = Mathf.Lerp(startDist, targetDist, t);
                RepositionOverlay();
                yield return null;
            }

            if (canvasRect != null) canvasRect.localScale = Vector3.one * targetScale;
            distanceFromCamera = targetDist;
            RepositionOverlay();
            _zoomRoutine = null;
        }

        public bool IsOverlayTransform(Transform t)
        {
            return t != null && _overlayRoot != null && (t == _overlayRoot.transform || t.IsChildOf(_overlayRoot.transform));
        }

        public bool TryRaycastOverlayPlane(Ray ray, out float distance)
        {
            distance = 0f;
            if (!_isOpen || _overlayRoot == null || !_overlayRoot.activeInHierarchy) return false;

            if (_overlayBackplateCollider != null && _overlayBackplateCollider.enabled &&
                _overlayBackplateCollider.Raycast(ray, out RaycastHit hit, 5f))
            {
                distance = hit.distance;
                return true;
            }

            Plane panelPlane = new Plane(-_overlayRoot.transform.forward, _overlayRoot.transform.position);
            if (panelPlane.Raycast(ray, out float planeDist) && planeDist > 0.08f && planeDist < 5f)
            {
                distance = planeDist;
                return true;
            }

            return false;
        }

        private void CaptureErgonomicAnchorFromCamera()
        {
            if (_targetCamera == null) _targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (_targetCamera == null) return;

            Vector3 camForward = _targetCamera.transform.forward;
            Vector3 flatForward = new Vector3(camForward.x, 0f, camForward.z);
            if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
            flatForward.Normalize();

            // Inclinación ergonómica limitada (-4° arriba, +10° abajo) para coincidir con la mirada al PC
            // sin inclinar el HUD dentro del escritorio y manteniendo los 4 bordes dentro del FOV VR.
            float downPitchDeg = -Mathf.Asin(Mathf.Clamp(camForward.y, -0.99f, 0.99f)) * Mathf.Rad2Deg;
            float clampedDownPitchDeg = Mathf.Clamp(downPitchDeg, -4f, 10f);

            Quaternion yawRot = Quaternion.LookRotation(flatForward, Vector3.up);
            _anchorForward = (yawRot * Quaternion.Euler(clampedDownPitchDeg, 0f, 0f) * Vector3.forward).normalized;
            _hasAnchorPose = true;
        }

        private void RepositionOverlay()
        {
            if (_overlayRoot == null) return;
            if (_targetCamera == null) _targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (_targetCamera == null) return;

            if (!_hasAnchorPose)
            {
                CaptureErgonomicAnchorFromCamera();
            }

            Vector3 forward = _anchorForward.sqrMagnitude > 0.001f ? _anchorForward.normalized : _targetCamera.transform.forward;
            Vector3 newPos = _targetCamera.transform.position + (forward * distanceFromCamera);

            _overlayRoot.transform.position = newPos;
            _overlayRoot.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        private void SuppressSceneObstructionsForOverlay()
        {
            InfectionSpreadVisualizer.I?.SetVisualsSuppressedForInspection(true);

            if (!_sceneObstructionsSuppressed)
            {
                _sceneObstructionsSuppressed = true;

                // Ocultar todos los monitores físicos, pantallas World Space, balizas y accesorios de escritorio
                SuppressAllRootsOfType<InteractiveMonitorController>();
                SuppressAllRootsOfType<HolographicDataBeacon>();
                SuppressAllRootsOfType<PostItCredenciales>();
                SuppressAllRootsOfType<TokenMfaBoveda>();
                SuppressAllRootsOfType<TelefonoAnexoSoc>();
                SuppressAllRootsOfType<CredencialesDeskDrawer>();
                SuppressAllRootsOfType<DefenseTerminalController>();
                SuppressAllRootsOfType<TeleportPoint>();
            }

            // Barrido espacial en el corredor cámara -> HUD para ocultar mobiliario/mamparas que invadan la vista
            SuppressNearbyObstructionsInViewCorridor();
        }

        private void SuppressAllRootsOfType<T>() where T : Component
        {
            T[] items = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                {
                    SuppressHierarchyComponents(items[i].gameObject);
                }
            }
        }

        private void SuppressNearbyObstructionsInViewCorridor()
        {
            if (_overlayRoot == null || _targetCamera == null) return;

            Vector3 camPos = _targetCamera.transform.position;
            Vector3 panelCenter = _overlayRoot.transform.position;
            Vector3 forward = _anchorForward.sqrMagnitude > 0.001f ? _anchorForward.normalized : _targetCamera.transform.forward;
            Vector3 corridorEnd = panelCenter + (forward * 0.45f);

            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled || ShouldSkipSuppressionTransform(r.transform)) continue;
                if (IsBoundsObstructingOverlay(r.bounds, camPos, panelCenter, corridorEnd))
                {
                    SuppressComponent(r, _suppressedRenderers, c => c.enabled, (c, v) => c.enabled = v);
                }
            }

            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || !col.enabled || ShouldSkipSuppressionTransform(col.transform)) continue;
                if (IsBoundsObstructingOverlay(col.bounds, camPos, panelCenter, corridorEnd))
                {
                    SuppressComponent(col, _suppressedColliders, c => c.enabled, (c, v) => c.enabled = v);
                }
            }
        }

        private void SuppressHierarchyComponents(GameObject root)
        {
            if (root == null || ShouldSkipSuppressionTransform(root.transform)) return;

            SuppressComponentsInChildren(root, _suppressedRenderers, c => c.enabled, (c, v) => c.enabled = v);
            SuppressComponentsInChildren(root, _suppressedCanvases, c => c.enabled, (c, v) => c.enabled = v);
            SuppressComponentsInChildren(root, _suppressedColliders, c => c.enabled, (c, v) => c.enabled = v);
            SuppressComponentsInChildren(root, _suppressedColliders2D, c => c.enabled, (c, v) => c.enabled = v);
            SuppressComponentsInChildren(root, _suppressedLights, c => c.enabled, (c, v) => c.enabled = v);
        }

        private void SuppressComponent<T>(T component, List<T> restoreList, Func<T, bool> isEnabled, Action<T, bool> setEnabled) where T : Component
        {
            if (component == null || !isEnabled(component) || ShouldSkipSuppressionTransform(component.transform))
            {
                return;
            }

            if (!_suppressedComponentIds.Add(component.GetInstanceID()))
            {
                return;
            }

            setEnabled(component, false);
            restoreList.Add(component);
        }

        private void SuppressComponentsInChildren<T>(GameObject root, List<T> restoreList, Func<T, bool> isEnabled, Action<T, bool> setEnabled) where T : Component
        {
            if (root == null) return;
            T[] items = root.GetComponentsInChildren<T>(false);
            for (int i = 0; i < items.Length; i++)
            {
                SuppressComponent(items[i], restoreList, isEnabled, setEnabled);
            }
        }

        private void RestoreSceneObstructionsAfterOverlay()
        {
            InfectionSpreadVisualizer.I?.SetVisualsSuppressedForInspection(false);

            RestoreComponents(_suppressedRenderers, (c, v) => c.enabled = v);
            RestoreComponents(_suppressedCanvases, (c, v) => c.enabled = v);
            RestoreComponents(_suppressedColliders, (c, v) => c.enabled = v);
            RestoreComponents(_suppressedColliders2D, (c, v) => c.enabled = v);
            RestoreComponents(_suppressedLights, (c, v) => c.enabled = v);

            _suppressedComponentIds.Clear();
            _sceneObstructionsSuppressed = false;
        }

        private static void RestoreComponents<T>(List<T> restoreList, Action<T, bool> setEnabled) where T : Component
        {
            for (int i = 0; i < restoreList.Count; i++)
            {
                T item = restoreList[i];
                if (item != null)
                {
                    setEnabled(item, true);
                }
            }
            restoreList.Clear();
        }

        private bool ShouldSkipSuppressionTransform(Transform t)
        {
            if (t == null) return true;
            if (_overlayRoot != null && (t == _overlayRoot.transform || t.IsChildOf(_overlayRoot.transform))) return true;
            if (PlayerTeleportController.I != null && (t == PlayerTeleportController.I.transform || t.IsChildOf(PlayerTeleportController.I.transform))) return true;
            if (_targetCamera != null && (t == _targetCamera.transform || t.IsChildOf(_targetCamera.transform))) return true;
            if (t.GetComponentInParent<CardboardReticleAdapter>() != null) return true;

            string n = t.name;
            return n.IndexOf("CardboardReticle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   n.IndexOf("PlayerBlinkFader", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsBoundsObstructingOverlay(Bounds bounds, Vector3 camPos, Vector3 panelCenter, Vector3 corridorEnd)
        {
            bool isFloorOrCeilingLike = (bounds.max.y < 0.25f || bounds.min.y > 2.35f) && bounds.size.x > 2.5f && bounds.size.z > 2.5f;
            if (isFloorOrCeilingLike) return false;

            Vector3 closestToPanel = bounds.ClosestPoint(panelCenter);
            if (bounds.Contains(panelCenter) || Vector3.Distance(closestToPanel, panelCenter) <= 0.42f)
            {
                return true;
            }

            if (bounds.size.magnitude > 7.5f) return false;
            if (Vector3.Distance(closestToPanel, panelCenter) <= 0.72f) return true;

            Vector3 seg = corridorEnd - camPos;
            float segLenSq = seg.sqrMagnitude;
            if (segLenSq <= 0.0001f) return false;

            Vector3 closestToCam = bounds.ClosestPoint(camPos);
            float tCam = Mathf.Clamp01(Vector3.Dot(closestToCam - camPos, seg) / segLenSq);
            if (Vector3.Distance(closestToCam, camPos + seg * tCam) <= 0.65f) return true;

            float tPanel = Mathf.Clamp01(Vector3.Dot(closestToPanel - camPos, seg) / segLenSq);
            return Vector3.Distance(closestToPanel, camPos + seg * tPanel) <= 0.65f;
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
            bg.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.995f); // Fondo opaco para evitar transparencias

            var outline = cardGO.AddComponent<Outline>();
            outline.effectColor = new Color(topAccentColor.r, topAccentColor.g, topAccentColor.b, 0.50f);
            outline.effectDistance = new Vector2(2f, -2f);

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

        private void CreateOptionCheckButton(string name, Transform parent, Vector2 pos, Vector2 size, string title, string detail)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            btnGO.layer = LayerMask.NameToLayer("Interactive");

            RectTransform rect = btnGO.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image bg = btnGO.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.12f, 0.18f, 0.98f);

            Outline ol = btnGO.AddComponent<Outline>();
            ol.effectColor = new Color(0.18f, 0.65f, 0.85f, 0.45f);
            ol.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = btnGO.AddComponent<Button>();

            BoxCollider boxCol = btnGO.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(size.x, size.y, 25f);
            boxCol.center = new Vector3(0f, 0f, -5f);
            boxCol.isTrigger = true;

            GazeInteractable gaze = btnGO.AddComponent<GazeInteractable>();
            gaze.interactableLabel = title;
            gaze.dwellTime = 1.0f;

            btnGO.AddComponent<HolographicCardHover>().Configure(1.04f, 14f);

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            RectTransform tRect = txtGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-16f, -8f);

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = $"[ ] <b>{title}</b>\n<size=15><color=#94A3B8>{detail}</color></size>";
            AndesTypographyTheme.StyleButtonLabel(tmp, Color.white, 16.5f);

            bool isChecked = false;
            Action toggleAction = () =>
            {
                isChecked = !isChecked;
                if (isChecked)
                {
                    bg.color = new Color(0.08f, 0.35f, 0.20f, 0.98f);
                    ol.effectColor = GreenSuccessColor;
                    tmp.text = $"[✓] <b><color=#38F28A>{title}</color></b>\n<size=15><color=#E2E8F0>{detail}</color></size>";
                    if (AndesSim.Audio.AudioManager.I != null) AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
                }
                else
                {
                    bg.color = new Color(0.06f, 0.12f, 0.18f, 0.98f);
                    ol.effectColor = new Color(0.18f, 0.65f, 0.85f, 0.45f);
                    tmp.text = $"[ ] <b>{title}</b>\n<size=15><color=#94A3B8>{detail}</color></size>";
                    if (AndesSim.Audio.AudioManager.I != null) AndesSim.Audio.AudioManager.I.PlayGazeClick();
                }
            };

            btn.onClick.AddListener(() => toggleAction.Invoke());
            gaze.onClick.AddListener(() => toggleAction.Invoke());
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