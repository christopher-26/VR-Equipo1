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

        private struct OverlayButtonControl
        {
            public Button Button;
            public GazeInteractable Gaze;
            public CanvasGroup CanvasGroup;
        }

        private int _emailSubPart = 0;
        private bool _isEnlarged = false;
        private bool _isSequenceBusy = false;
        private Coroutine _activeSequenceRoutine;

        private readonly HashSet<string> _markedPhishingSignals = new HashSet<string>();
        private readonly List<OverlayButtonControl> _frameButtons = new List<OverlayButtonControl>();
        private readonly List<OverlayButtonControl> _contentButtons = new List<OverlayButtonControl>();
        private TextMeshProUGUI _activeEmailHintText;
        private GameObject _navPc1Btn;
        private GameObject _navPc2Btn;
        private GameObject _navPc3Btn;
        private int _activeNavStationIndex = 0;

        // Raíz del Canvas de Inspección
        private GameObject _overlayRoot;
        private Canvas _overlayCanvas;
        private CanvasGroup _overlayCanvasGroup;
        private CanvasGroup _contentCanvasGroup;
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
        public bool IsTransitionBusy => _isSequenceBusy;

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

            if (GameManager.I != null)
            {
                GameManager.I.OnSimulationReset -= HandleSimulationReset;
                GameManager.I.OnSimulationReset += HandleSimulationReset;
                GameManager.I.OnModeChanged -= HandleModeChanged;
                GameManager.I.OnModeChanged += HandleModeChanged;
            }
        }

        private void OnDisable()
        {
            ForceCloseOverlayImmediate();
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnSimulationReset -= HandleSimulationReset;
                GameManager.I.OnModeChanged -= HandleModeChanged;
            }
            ForceCloseOverlayImmediate();
        }

        private void HandleSimulationReset()
        {
            _markedPhishingSignals.Clear();
            ForceCloseOverlayImmediate();
            RefreshBottomNavState(0);
        }

        private void HandleModeChanged(SimMode _)
        {
            RefreshBottomNavState(_activeNavStationIndex);
        }

        private void RunSequentialOverlayAction(IEnumerator sequenceRoutine)
        {
            if (sequenceRoutine == null || _isSequenceBusy) return;
            EnsureOverlayCanvas();
            _activeSequenceRoutine = StartCoroutine(ExecuteSequentialOverlayRoutine(sequenceRoutine));
        }

        private IEnumerator ExecuteSequentialOverlayRoutine(IEnumerator sequenceRoutine)
        {
            _isSequenceBusy = true;
            SetOverlayControlsInteractable(false);

            yield return StartCoroutine(sequenceRoutine);

            _isSequenceBusy = false;
            _activeSequenceRoutine = null;
            if (_isOpen)
            {
                SetOverlayControlsInteractable(true);
            }
        }

        private static IEnumerator FadeCanvasGroupRoutine(CanvasGroup group, float fromAlpha, float toAlpha, float duration)
        {
            if (group == null) yield break;

            group.alpha = fromAlpha;
            float safeDuration = Mathf.Max(0.05f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / safeDuration);
                float smooth = toAlpha >= fromAlpha
                    ? (1f - Mathf.Pow(1f - u, 3f))
                    : (u * u * (3f - 2f * u));
                group.alpha = Mathf.Lerp(fromAlpha, toAlpha, smooth);
                yield return null;
            }

            group.alpha = toAlpha;
        }

        private void SetOverlayControlsInteractable(bool interactable)
        {
            ApplyInteractableStateToList(_frameButtons, interactable);
            ApplyInteractableStateToList(_contentButtons, interactable);
        }

        private static void ApplyInteractableStateToList(List<OverlayButtonControl> list, bool interactable)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                OverlayButtonControl ctrl = list[i];
                if (ctrl.Button == null && ctrl.Gaze == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (ctrl.CanvasGroup != null)
                {
                    ctrl.CanvasGroup.alpha = interactable ? 1f : 0.46f;
                    ctrl.CanvasGroup.interactable = interactable;
                }
                if (ctrl.Button != null)
                {
                    ctrl.Button.interactable = interactable;
                }
                if (ctrl.Gaze != null)
                {
                    ctrl.Gaze.SetInteractable(interactable);
                }
            }
        }

        private void Update()
        {
            if (_isSequenceBusy) return;

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
                return;
            }

            if (!_isOpen || _overlayRoot == null) return;

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
                if (_targetCamera == null) return;
            }

            // Si el usuario gira la cabeza completamente fuera del panel, cerrarlo suavemente en secuencia.
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
            _panelEntrance.SetPlayOnEnable(false);
            _panelEntrance.Configure(0.18f, 1f, 0f, _overlayCanvasGroup);

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

            // Contenedor Dinámico Central con CanvasGroup propio para transiciones ordenadas entre pestañas
            GameObject contentGO = new GameObject("DynamicContentContainer");
            contentGO.transform.SetParent(panelGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchoredPosition = new Vector2(0f, -8f);
            contentRect.sizeDelta = new Vector2(930f, 520f);
            _contentCanvasGroup = contentGO.AddComponent<CanvasGroup>();
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

            // Barra Persistente Guiada por Estaciones (Recorrido PC 1 -> PC 2 -> PC 3 -> Tablero)
            _navPc1Btn = CreateInspectionButton(
                "Btn_NavCorreo", panelGO.transform,
                new Vector2(-374f, -328f), new Vector2(174f, 52f),
                "1. PC 1 CORREO",
                new Color(0.08f, 0.22f, 0.36f, 0.96f),
                Color.white,
                () => TeleportAndOpenStation("Teleport_Puesto1_Carla", () =>
                    (GameManager.I != null && GameManager.I.mode == SimMode.Defensa)
                        ? CreateQuarantineViewRoutine(0)
                        : CreateEmailViewRoutine(0))
            );
            _navPc2Btn = CreateInspectionButton(
                "Btn_NavPostIt", panelGO.transform,
                new Vector2(-187f, -328f), new Vector2(174f, 52f),
                "2. PC 2 SWIFT",
                new Color(0.08f, 0.22f, 0.36f, 0.96f),
                Color.white,
                () => TeleportAndOpenStation("Teleport_Puesto2_Analista", () =>
                    (GameManager.I != null && GameManager.I.mode == SimMode.Defensa)
                        ? CreateQuarantineViewRoutine(1)
                        : CreateSwiftViewRoutine())
            );
            _navPc3Btn = CreateInspectionButton(
                "Btn_NavSiem", panelGO.transform,
                new Vector2(0f, -328f), new Vector2(174f, 52f),
                "3. PC 3 SIEM",
                new Color(0.08f, 0.22f, 0.36f, 0.96f),
                Color.white,
                () => TeleportAndOpenStation("Teleport_Puesto3_LogsYDefensa", () =>
                    (GameManager.I != null && GameManager.I.mode == SimMode.Defensa)
                        ? CreateQuarantineViewRoutine(2)
                        : CreateSiemViewRoutine())
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

            RefreshBottomNavState(0);
            _overlayRoot.SetActive(false);
            Debug.Log($"{LogPrefix} Canvas de Inspección VR inicializado con éxito.");
        }

        private void RefreshBottomNavState(int activeStationIndex)
        {
            _activeNavStationIndex = activeStationIndex;
            bool isDefense = GameManager.I != null && GameManager.I.mode == SimMode.Defensa;

            ApplyNavButtonVisual(_navPc1Btn, isDefense ? "1. PC 1 SANDBOX" : "1. PC 1 CORREO", activeStationIndex == 1, isDefense);
            ApplyNavButtonVisual(_navPc2Btn, isDefense ? "2. PC 2 MFA/VLAN" : "2. PC 2 SWIFT", activeStationIndex == 2, isDefense);
            ApplyNavButtonVisual(_navPc3Btn, isDefense ? "3. PC 3 FIREWALL" : "3. PC 3 SIEM", activeStationIndex == 3, isDefense);
        }

        private void ApplyNavButtonVisual(GameObject btnGO, string label, bool isActive, bool isDefense)
        {
            if (btnGO == null) return;

            Color bgColor = isActive
                ? (isDefense ? new Color(0.08f, 0.38f, 0.22f, 0.98f) : new Color(0.10f, 0.36f, 0.50f, 0.98f))
                : new Color(0.08f, 0.16f, 0.24f, 0.96f);
            Color textColor = isActive
                ? (isDefense ? GreenSuccessColor : CyanNeonAccent)
                : Color.white;

            Image bg = btnGO.GetComponent<Image>();
            if (bg != null) bg.color = bgColor;

            Button btn = btnGO.GetComponent<Button>();
            if (btn != null)
            {
                ColorBlock cb = btn.colors;
                cb.normalColor = bgColor;
                cb.highlightedColor = bgColor * 1.25f;
                cb.pressedColor = bgColor * 0.8f;
                btn.colors = cb;
            }

            TextMeshProUGUI tmp = btnGO.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = isActive ? $"<b>{label}</b>" : label;
                tmp.color = textColor;
            }

            GazeInteractable gaze = btnGO.GetComponent<GazeInteractable>();
            if (gaze != null)
            {
                gaze.interactableLabel = label;
                gaze.SetBaseGraphicColor(bgColor);
            }
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

        private IEnumerator TransitionToViewRoutine(
            int navStationIndex,
            Action buildContentAction,
            Func<IEnumerator> postEntranceStepAnimation,
            string narrationClip,
            float narrationDelay = 2.0f)
        {
            EnsureOverlayCanvas();
            RefreshBottomNavState(navStationIndex);

            bool wasAlreadyOpen = _isOpen && _overlayRoot != null && _overlayRoot.activeInHierarchy;

            if (wasAlreadyOpen)
            {
                yield return FadeCanvasGroupRoutine(_contentCanvasGroup, _contentCanvasGroup != null ? _contentCanvasGroup.alpha : 1f, 0f, 0.12f);
                ClearActiveContent();
                buildContentAction?.Invoke();
                SetOverlayControlsInteractable(false);
                SuppressNearbyObstructionsInViewCorridor();
                yield return FadeCanvasGroupRoutine(_contentCanvasGroup, 0f, 1f, 0.15f);
            }
            else
            {
                if (_contentCanvasGroup != null) _contentCanvasGroup.alpha = 1f;
                ClearActiveContent();
                buildContentAction?.Invoke();
                SetOverlayControlsInteractable(false);
                PresentOverlayRootForEntrance();
                yield return FadeCanvasGroupRoutine(_overlayCanvasGroup, 0f, 1f, 0.18f);
            }

            if (!string.IsNullOrEmpty(narrationClip))
            {
                AndesSim.Audio.AudioManager.I?.PlayNarration(narrationClip, true, narrationDelay);
            }

            if (postEntranceStepAnimation != null)
            {
                yield return StartCoroutine(postEntranceStepAnimation());
            }
        }

        private void PresentOverlayRootForEntrance()
        {
            _isOpen = true;
            if (_overlayRoot == null)
            {
                SuppressSceneObstructionsForOverlay();
                return;
            }

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

            CaptureErgonomicAnchorFromCamera();
            RepositionOverlay();
            SuppressSceneObstructionsForOverlay();
            UpdateZoomButtonLabel();

            if (_overlayCanvasGroup != null) _overlayCanvasGroup.alpha = 0f;
            _overlayRoot.SetActive(true);

            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayLaserScan(transform.position);
            }
        }

        public void OpenEmailInspection()
        {
            OpenEmailInspection(0);
        }

        public void OpenEmailInspection(int subPart)
        {
            RunSequentialOverlayAction(CreateEmailViewRoutine(subPart));
        }

        private IEnumerator CreateEmailViewRoutine(int subPart)
        {
            int part = Mathf.Clamp(subPart, 0, 1);
            string narration = part == 0 ? "Voice_06_CorreoParte1" : "Voice_07_CorreoParte2";
            return TransitionToViewRoutine(1, () => BuildEmailView(part), null, narration);
        }

        private void BuildEmailView(int subPart)
        {
            _emailSubPart = Mathf.Clamp(subPart, 0, 1);

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.NotifyWorkstationInteracted("Monitor_01");
                ScenarioFlowController.I.NotifyEmailOpened();
                if (ScenarioFlowController.I.CurrentStep < ScenarioStep.Step4_InspeccionarCorreo)
                {
                    ScenarioFlowController.I.SetStep(ScenarioStep.Step4_InspeccionarCorreo);
                }
            }

            SetHeaderTheme(
                _emailSubPart == 0 ? CyanNeonAccent : WarningAmberColor,
                "PASO 1 DE 3 · PC 1 (CORREO DE CARLA)",
                _emailSubPart == 0
                    ? "Lee el correo sospechoso recibido en la estación de Carla Mendoza"
                    : "Haz clic en las 3 pistas del fraude y continúa el recorrido"
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
                "2. MARCAR LAS 3 PISTAS",
                part2Bg, _emailSubPart == 1 ? WarningAmberColor : Color.white,
                () => OpenEmailInspection(1)
            );

            if (_emailSubPart == 0)
            {
                _activeEmailHintText = null;
                GameObject bodyBox = CreateStyledCard("EmailReadCard", emailPanel.transform, new Vector2(0f, 35f), new Vector2(880f, 260f), AndesTypographyTheme.BgCardGlass, CyanNeonAccent);

                CreateLabel("Txt_MailLines", bodyBox.transform, Vector2.zero, new Vector2(820f, 220f),
                    "<b>De:</b> rasel.ahlam@<color=#FFA940>career-applicants-bd.com</color>\n" +
                    "<b>Asunto:</b> <color=#FF5A4E>URGENTE · CV Especialista SWIFT</color>\n\n" +
                    "<i>\"Carla: adjunto mi CV urgente para el área de pagos.\"</i>\n\n" +
                    "<b>Archivo adjunto:</b> <color=#00E5FF>185.130.44.82/CV_2015.scr.zip</color>",
                    20f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

                CreateInspectionButton(
                    "Btn_NextToPart2", emailPanel.transform,
                    new Vector2(-225f, -165f), new Vector2(430f, 68f),
                    "SIGUIENTE: MARCAR 3 PISTAS >",
                    new Color(0.10f, 0.34f, 0.44f, 0.98f),
                    CyanNeonAccent,
                    () => OpenEmailInspection(1)
                );

                CreateInspectionButton(
                    "Btn_QuickOpenLink", emailPanel.transform,
                    new Vector2(225f, -165f), new Vector2(430f, 68f),
                    "EJECUTAR ADJUNTO (ATAQUE) >",
                    new Color(0.48f, 0.12f, 0.14f, 0.98f),
                    Color.white,
                    TriggerAttackInfectionSequence
                );
            }
            else
            {
                _activeEmailHintText = CreateLabel("Lbl_StepHint", emailPanel.transform, new Vector2(0f, 155f), new Vector2(880f, 38f),
                    string.Empty,
                    19f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);
                UpdateEmailSignalsHint();

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
                    new Vector2(-225f, -55f), new Vector2(430f, 74f),
                    "<color=#FF5A4E><b>SIMULAR ATAQUE (IR A PC 2) ></b></color>",
                    new Color(0.48f, 0.12f, 0.14f, 0.98f),
                    Color.white,
                    TriggerAttackInfectionSequence
                );

                CreateInspectionButton(
                    "Btn_DefenseReport", emailPanel.transform,
                    new Vector2(225f, -55f), new Vector2(430f, 74f),
                    "<color=#38F28A><b>MODO DEFENSA (PROTEGER RED) ></b></color>",
                    new Color(0.08f, 0.34f, 0.20f, 0.98f),
                    Color.white,
                    () => RunSequentialOverlayAction(SwitchModeAndOpenQuarantineRoutine(0))
                );

                CreateInspectionButton(
                    "Btn_NextStepPostIt", emailPanel.transform,
                    new Vector2(0f, -165f), new Vector2(540f, 58f),
                    "VER POST-IT DE CLAVES EN PC 1 >",
                    new Color(0.13f, 0.25f, 0.40f, 0.96f),
                    CyanNeonAccent,
                    OpenCredentialsInspection
                );
            }

            _activeContentPanel = emailPanel;
        }

        private IEnumerator SwitchModeAndOpenQuarantineRoutine(int step)
        {
            if (GameManager.I != null) GameManager.I.SetMode(SimMode.Defensa);
            if (MailQuarantine.I != null) MailQuarantine.I.TriggerQuarantineDefense();
            else if (PhishingInspector.I != null) PhishingInspector.I.ExecuteReportPhishingDefenseSequence();
            yield return StartCoroutine(CreateQuarantineViewRoutine(step));
        }

        public void OpenQuarantineInspection()
        {
            OpenQuarantineInspection(0);
        }

        public void OpenQuarantineInspection(int defenseStep)
        {
            RunSequentialOverlayAction(CreateQuarantineViewRoutine(defenseStep));
        }

        private IEnumerator CreateQuarantineViewRoutine(int defenseStep)
        {
            int step = Mathf.Clamp(defenseStep, 0, 2);
            TextMeshProUGUI statusTmp = null;
            TextMeshProUGUI defHintTmp = null;
            RectTransform fillRect = null;
            string progressDoneText = string.Empty;
            string completedHintText = string.Empty;

            yield return StartCoroutine(TransitionToViewRoutine(
                step + 1,
                () => BuildDefenseView(step, out statusTmp, out defHintTmp, out fillRect, out progressDoneText, out completedHintText),
                () => PlayDefenseStepVerifyRoutine(step, statusTmp, defHintTmp, fillRect, progressDoneText, completedHintText),
                "Voice_12_CuarentenaSandbox"
            ));
        }

        private void BuildDefenseView(
            int step,
            out TextMeshProUGUI statusTmp,
            out TextMeshProUGUI defHintTmp,
            out RectTransform fillRect,
            out string progressDoneText,
            out string completedHintText)
        {
            if (GameManager.I != null && GameManager.I.mode != SimMode.Defensa)
            {
                GameManager.I.SetMode(SimMode.Defensa);
            }

            if (InfectionSpreadVisualizer.I != null)
            {
                InfectionSpreadVisualizer.I.ShowDefenseContainmentSign();
            }

            string headerTitle;
            string headerSub;
            string boxTitle;
            string opt1Title, opt1Desc, opt2Title, opt2Desc, opt3Title, opt3Desc;

            if (step == 0)
            {
                if (GameManager.I != null)
                {
                    GameManager.I.RegisterEvidence("Evidencia_Correo");
                    GameManager.I.ApplyDefense("Defensa_Sandbox");
                    GameManager.I.ApplyDefense("Defensa_BotonReportar");
                }

                headerTitle = "DEFENSA 1 DE 3 · PC 1 (SANDBOX Y REPORTE SOC)";
                headerSub = "El correo malicioso se analiza en entorno aislado sin infectar la PC 1";
                boxTitle = "<b><color=#38F28A>PROTEGIENDO PC 1 · SANDBOX DE CORREO ACTIVO</color></b>";
                progressDoneText =
                    "<b><color=#38F28A>✓ PC 1 PROTEGIDA · ARCHIVO .SCR BLOQUEADO EN SANDBOX</color></b>\n" +
                    "• El adjunto <i>CV_2015.scr.zip</i> fue detonado en una máquina virtual aislada.\n" +
                    "• Alerta automática enviada al Anexo SOC 4040 (16 buzones a salvo).";

                opt1Title = "1. SANDBOX AISLADO";
                opt1Desc = "Detona .scr fuera de la red";
                opt2Title = "2. REPORTE AL SOC";
                opt2Desc = "Purga los 16 buzones";
                opt3Title = "3. PC 1 SIN EJECUCIÓN";
                opt3Desc = "Cero troyanos instalados";
            }
            else if (step == 1)
            {
                if (GameManager.I != null)
                {
                    GameManager.I.RegisterEvidence("Evidencia_PostIt");
                    GameManager.I.ApplyDefense("Defensa_MFA");
                }

                headerTitle = "DEFENSA 2 DE 3 · PC 2 (TOKEN MFA Y RED VLAN)";
                headerSub = "La terminal de pagos exige Token físico rotativo y red separada";
                boxTitle = "<b><color=#38F28A>PROTEGIENDO PC 2 · TERMINAL SWIFT BLINDADA</color></b>";
                progressDoneText =
                    "<b><color=#38F28A>✓ PC 2 PROTEGIDA · TRANSFERENCIA DE US$ 81M RECHAZADA</color></b>\n" +
                    "• El Post-It fue reemplazado por un Token físico MFA de 6 dígitos cada 30s.\n" +
                    "• La segmentación VLAN impide saltos desde la red de correo hacia SWIFT.";

                opt1Title = "1. TOKEN FÍSICO MFA";
                opt1Desc = "Código dinámico cada 30s";
                opt2Title = "2. RED VLAN SEPARADA";
                opt2Desc = "Aísla servidor de pagos";
                opt3Title = "3. GIRO BLOQUEADO";
                opt3Desc = "US$ 81M quedan en bóveda";
            }
            else
            {
                if (GameManager.I != null)
                {
                    GameManager.I.RegisterEvidence("Evidencia_LogAccesos");
                    GameManager.I.ApplyDefense("Defensa_Sandbox");
                    GameManager.I.ApplyDefense("Defensa_BotonReportar");
                    GameManager.I.ApplyDefense("Defensa_MFA");
                }

                headerTitle = "DEFENSA 3 DE 3 · PC 3 (CONSOLA SIEM Y FIREWALL)";
                headerSub = "El SOC bloquea la IP atacante y confirma el 100% de fondos a salvo";
                boxTitle = "<b><color=#38F28A>PROTEGIENDO PC 3 · FIREWALL Y SIEM EN LÍNEA</color></b>";
                progressDoneText =
                    "<b><color=#38F28A>✓ DEFENSA COMPLETADA EN LAS 3 PCs · US$ 0 ROBADOS</color></b>\n" +
                    "• Firewall perimetral bloqueó la IP hostil C2 <i>185.130.44.82:443</i>.\n" +
                    "• <b>Resultado final:</b> US$ 81,000,000.00 protegidos íntegramente.";

                opt1Title = "1. REGLA FIREWALL";
                opt1Desc = "Bloquea IP 185.130.44.82";
                opt2Title = "2. ALERTA SIEM SOC";
                opt2Desc = "Intrusión neutralizada";
                opt3Title = "3. AUDITORÍA EXITOSA";
                opt3Desc = "3/3 defensas verificadas";
            }

            SetHeaderTheme(GreenSuccessColor, headerTitle, headerSub);

            GameObject qPanel = new GameObject("QuarantineContentPanel");
            qPanel.transform.SetParent(_contentContainer, false);
            RectTransform qRect = qPanel.AddComponent<RectTransform>();
            qRect.anchorMin = Vector2.zero;
            qRect.anchorMax = Vector2.one;
            qRect.sizeDelta = Vector2.zero;

            // Tira superior de progreso en Modo Defensa (PC 1 -> PC 2 -> PC 3)
            string[] defensePillTitles = { "1. PC 1 SANDBOX", "2. PC 2 MFA/VLAN", "3. PC 3 SIEM" };
            for (int i = 0; i < defensePillTitles.Length; i++)
            {
                float pillX = (i - 1) * 295f;
                bool isCurrent = i == step;
                bool isDone = i < step;
                Color pillBg = isCurrent
                    ? new Color(0.06f, 0.24f, 0.16f, 0.98f)
                    : AndesTypographyTheme.BgCardGlass;
                Color pillAccent = (isCurrent || isDone) ? GreenSuccessColor : CyanNeonAccent;
                string statusLine = isDone
                    ? "<color=#38F28A>✓ PROTEGIDA</color>"
                    : (isCurrent ? "<color=#38F28A>● REVISANDO</color>" : "<color=#94A3B8>SIGUIENTE ></color>");

                GameObject pillGO = CreateStyledCard($"Pill_DefPC{i + 1}", qPanel.transform, new Vector2(pillX, 212f), new Vector2(280f, 48f),
                    pillBg, pillAccent);
                CreateLabel($"Txt_DefPill{i + 1}", pillGO.transform, Vector2.zero, new Vector2(260f, 40f),
                    $"<b>{defensePillTitles[i]}</b>\n{statusLine}",
                    14.5f, TextAlignmentOptions.Center, (isCurrent || isDone) ? Color.white : AndesTypographyTheme.TextMuted);
            }

            GameObject boxGO = CreateStyledCard("SandboxLogBox", qPanel.transform, new Vector2(0f, 108f), new Vector2(880f, 138f),
                new Color(0.015f, 0.055f, 0.055f, 0.995f), GreenSuccessColor);

            statusTmp = CreateLabel("Txt_QuarantineLog", boxGO.transform, new Vector2(0f, 12f), new Vector2(840f, 94f),
                boxTitle + "\n<size=17.5>Verificando controles de seguridad en esta estación...</size>",
                18f, TextAlignmentOptions.Center, Color.white);

            // Barra animada de verificación de defensa
            GameObject barBgObj = new GameObject("DefenseProgressBg");
            barBgObj.transform.SetParent(boxGO.transform, false);
            RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchoredPosition = new Vector2(0f, -48f);
            barBgRect.sizeDelta = new Vector2(780f, 14f);
            Image barBgImg = barBgObj.AddComponent<Image>();
            barBgImg.color = new Color(0.02f, 0.12f, 0.08f, 0.96f);

            GameObject barFillObj = new GameObject("DefenseProgressFill");
            barFillObj.transform.SetParent(barBgObj.transform, false);
            fillRect = barFillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(0f, 0f);
            Image fillImg = barFillObj.AddComponent<Image>();
            fillImg.color = GreenSuccessColor;

            // 3 tarjetas interactivas para verificar cada capa de protección
            defHintTmp = CreateLabel("Lbl_DefCheckHint", qPanel.transform, new Vector2(0f, 18f), new Vector2(880f, 26f),
                string.Empty, 17f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

            string defCompletedMsg = step < 2
                ? "<color=#38F28A><b>✓ 3/3 CONTROLES VERIFICADOS · Haz clic abajo para continuar ></b></color>"
                : "<color=#38F28A><b>✓ 3/3 CONTROLES VERIFICADOS · Haz clic en COMPLETAR DEFENSA EXITOSA ></b></color>";

            completedHintText = "Haz clic en los 3 controles para verificar cómo protegen esta PC (0/3 verificadas):";

            BindOptionChecklistCounter(
                defHintTmp,
                "Haz clic en los 3 controles para verificar cómo protegen esta PC",
                defCompletedMsg,
                (qPanel.transform, "Btn_DefCheck1", new Vector2(-295f, -40f), new Vector2(280f, 80f), opt1Title, opt1Desc),
                (qPanel.transform, "Btn_DefCheck2", new Vector2(0f, -40f), new Vector2(280f, 80f), opt2Title, opt2Desc),
                (qPanel.transform, "Btn_DefCheck3", new Vector2(295f, -40f), new Vector2(280f, 80f), opt3Title, opt3Desc)
            );
            defHintTmp.text = "<color=#94A3B8>Verificando controles automáticos...</color>";

            // Botones de recorrido paso a paso en Modo Defensa
            if (step == 0)
            {
                CreateInspectionButton(
                    "Btn_DefStep1ToPc2", qPanel.transform,
                    new Vector2(0f, -146f), new Vector2(640f, 66f),
                    "<color=#38F28A><b>CONTINUAR A PC 2: VER PROTECCIÓN SWIFT Y MFA ></b></color>",
                    new Color(0.08f, 0.36f, 0.22f, 0.98f),
                    Color.white,
                    () => TeleportAndOpenStation("Teleport_Puesto2_Analista", () => CreateQuarantineViewRoutine(1))
                );
            }
            else if (step == 1)
            {
                CreateInspectionButton(
                    "Btn_DefStep2BackPc1", qPanel.transform,
                    new Vector2(-260f, -146f), new Vector2(320f, 66f),
                    "< VOLVER A PC 1 (SANDBOX)",
                    new Color(0.12f, 0.24f, 0.34f, 0.98f),
                    Color.white,
                    () => TeleportAndOpenStation("Teleport_Puesto1_Carla", () => CreateQuarantineViewRoutine(0))
                );

                CreateInspectionButton(
                    "Btn_DefStep2ToPc3", qPanel.transform,
                    new Vector2(175f, -146f), new Vector2(510f, 66f),
                    "<color=#38F28A><b>CONTINUAR A PC 3: VER SIEM Y FIREWALL ></b></color>",
                    new Color(0.08f, 0.36f, 0.22f, 0.98f),
                    Color.white,
                    () => TeleportAndOpenStation("Teleport_Puesto3_LogsYDefensa", () => CreateQuarantineViewRoutine(2))
                );
            }
            else
            {
                CreateInspectionButton(
                    "Btn_DefGoBoard", qPanel.transform,
                    new Vector2(-230f, -146f), new Vector2(420f, 66f),
                    "IR A PIZARRA DE SOSPECHOSOS >",
                    new Color(0.08f, 0.28f, 0.38f, 0.98f),
                    CyanNeonAccent,
                    NavigateToSuspectBoardOrDefense
                );

                CreateInspectionButton(
                    "Btn_ConfirmDefense", qPanel.transform,
                    new Vector2(230f, -146f), new Vector2(420f, 66f),
                    "<color=#38F28A><b>COMPLETAR DEFENSA EXITOSA ></b></color>",
                    new Color(0.08f, 0.36f, 0.20f, 0.98f),
                    Color.white,
                    () => CloseOverlayWithCallback(() =>
                    {
                        ScenarioFlowController.I?.CompleteScenarioFlow();
                        if (MailQuarantine.I != null)
                        {
                            MailQuarantine.I.ConfirmContainmentAndEndScenario();
                        }
                        else if (GameManager.I != null)
                        {
                            GameManager.I.EndScenario();
                        }
                    })
                );
            }

            _activeContentPanel = qPanel;
        }

        private IEnumerator PlayDefenseStepVerifyRoutine(
            int step,
            TextMeshProUGUI statusTmp,
            TextMeshProUGUI defHintTmp,
            RectTransform fillRect,
            string progressDoneText,
            string unlockedHintText)
        {
            const float verifyDuration = 0.95f;
            float elapsed = 0f;
            while (elapsed < verifyDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / verifyDuration);
                if (fillRect != null) fillRect.sizeDelta = new Vector2(780f * u, 0f);
                yield return null;
            }

            if (fillRect != null) fillRect.sizeDelta = new Vector2(780f, 0f);
            if (statusTmp != null) statusTmp.text = progressDoneText;
            if (defHintTmp != null) defHintTmp.text = unlockedHintText;
        }

        public void TriggerAttackInfectionSequence()
        {
            RunSequentialOverlayAction(CreateAttackInfectionViewRoutine());
        }

        private IEnumerator CreateAttackInfectionViewRoutine()
        {
            if (GameManager.I != null && GameManager.I.mode != SimMode.Ataque)
            {
                GameManager.I.SetMode(SimMode.Ataque);
            }

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

            if (GameManager.I != null)
            {
                GameManager.I.RegisterEvidence("Evidencia_Correo");
                GameManager.I.RegisterEvidence("Evidencia_PostIt");
            }

            TextMeshProUGUI traceTmp = null;
            RectTransform fillRect = null;
            Image fillImg = null;
            TextMeshProUGUI p1Tmp = null;
            TextMeshProUGUI p2Tmp = null;

            yield return StartCoroutine(TransitionToViewRoutine(
                1,
                () => BuildInfectionView(out traceTmp, out fillRect, out fillImg, out p1Tmp, out p2Tmp),
                () => PlayInfectionStepAnimationRoutine(traceTmp, fillRect, fillImg, p1Tmp, p2Tmp),
                "Voice_10_AtaqueDetonado"
            ));
        }

        private void BuildInfectionView(
            out TextMeshProUGUI traceTmp,
            out RectTransform fillRect,
            out Image fillImg,
            out TextMeshProUGUI p1Tmp,
            out TextMeshProUGUI p2Tmp)
        {
            SetHeaderTheme(
                DangerRedColor,
                "ATAQUE EN PC 1 · TROYANO ACTIVO Y CLAVE ROBADA",
                "Observa cómo se infecta la PC 1 antes de pasar a la PC 2 (SWIFT)"
            );

            GameObject infectPanel = new GameObject("InfectionContentPanel");
            infectPanel.transform.SetParent(_contentContainer, false);
            RectTransform iRect = infectPanel.AddComponent<RectTransform>();
            iRect.anchorMin = Vector2.zero;
            iRect.anchorMax = Vector2.one;
            iRect.sizeDelta = Vector2.zero;

            GameObject terminalBox = CreateStyledCard("InfectionTerminalBox", infectPanel.transform, new Vector2(0f, 68f), new Vector2(880f, 235f),
                new Color(0.14f, 0.03f, 0.04f, 0.99f), DangerRedColor);

            traceTmp = CreateLabel("Txt_InfectionTrace", terminalBox.transform, new Vector2(0f, 22f), new Vector2(820f, 155f),
                "<color=#FF5A4E><b>FASE 1 EN PC 1 · EJECUTANDO CV_2015.scr.zip</b></color>\n\n" +
                "<size=18>Instalando backdoor oculto <i>svchost_upd.exe</i> en el equipo de Carla...</size>",
                20f, TextAlignmentOptions.Center, Color.white);

            // Barra animada de infección en PC 1
            GameObject barBgObj = new GameObject("InfectionProgressBg");
            barBgObj.transform.SetParent(terminalBox.transform, false);
            RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchoredPosition = new Vector2(0f, -78f);
            barBgRect.sizeDelta = new Vector2(760f, 22f);
            Image barBgImg = barBgObj.AddComponent<Image>();
            barBgImg.color = new Color(0.06f, 0.02f, 0.02f, 0.96f);

            GameObject barFillObj = new GameObject("InfectionProgressFill");
            barFillObj.transform.SetParent(barBgObj.transform, false);
            fillRect = barFillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(0f, 0f);
            fillImg = barFillObj.AddComponent<Image>();
            fillImg.color = DangerRedColor;

            // 3 Pills de ruta del ataque (PC 1 -> PC 2 -> PC 3)
            GameObject pill1 = CreateStyledCard("Pill_PC1", infectPanel.transform, new Vector2(-290f, -82f), new Vector2(270f, 58f), AndesTypographyTheme.BgCardGlass, DangerRedColor);
            p1Tmp = CreateLabel("Txt_Pill1", pill1.transform, Vector2.zero, new Vector2(250f, 48f), "<b>1. PC 1 CARLA</b>\n<color=#FF5A4E>Infectando...</color>", 15f, TextAlignmentOptions.Center, Color.white);

            GameObject pill2 = CreateStyledCard("Pill_PC2", infectPanel.transform, new Vector2(0f, -82f), new Vector2(270f, 58f), AndesTypographyTheme.BgCardGlass, WarningAmberColor);
            p2Tmp = CreateLabel("Txt_Pill2", pill2.transform, Vector2.zero, new Vector2(250f, 48f), "<b>2. PC 2 SWIFT</b>\nSiguiente estación", 15f, TextAlignmentOptions.Center, AndesTypographyTheme.TextMuted);

            GameObject pill3 = CreateStyledCard("Pill_PC3", infectPanel.transform, new Vector2(290f, -82f), new Vector2(270f, 58f), AndesTypographyTheme.BgCardGlass, AndesTypographyTheme.TextMuted);
            CreateLabel("Txt_Pill3", pill3.transform, Vector2.zero, new Vector2(250f, 48f), "<b>3. PC 3 SIEM</b>\nPaso final", 15f, TextAlignmentOptions.Center, AndesTypographyTheme.TextMuted);

            // Botones inferiores para que el usuario avance con clic a PC 2 cuando concluya la animación
            CreateInspectionButton(
                "Btn_ReviewPostItFromAttack", infectPanel.transform,
                new Vector2(-260f, -168f), new Vector2(320f, 64f),
                "VER POST-IT DE CLAVES",
                new Color(0.14f, 0.24f, 0.36f, 0.98f),
                WarningAmberColor,
                OpenCredentialsInspection
            );

            CreateInspectionButton(
                "Btn_ContinueToPc2Swift", infectPanel.transform,
                new Vector2(175f, -168f), new Vector2(510f, 64f),
                "<color=#00E5FF><b>CONTINUAR A PC 2: TERMINAL SWIFT ></b></color>",
                new Color(0.08f, 0.34f, 0.44f, 0.98f),
                Color.white,
                () => TeleportAndOpenStation("Teleport_Puesto2_Analista", CreateSwiftViewRoutine)
            );

            _activeContentPanel = infectPanel;
        }

        private IEnumerator PlayInfectionStepAnimationRoutine(
            TextMeshProUGUI traceTmp,
            RectTransform fillRect,
            Image fillImg,
            TextMeshProUGUI p1Tmp,
            TextMeshProUGUI p2Tmp)
        {
            const float animDuration = 1.75f;
            float elapsed = 0f;
            int stage = 0;

            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / animDuration);
                if (fillRect != null) fillRect.sizeDelta = new Vector2(760f * u, 0f);
                if (fillImg != null) fillImg.color = Color.Lerp(WarningAmberColor, DangerRedColor, u);

                if (stage == 0 && u >= 0.45f)
                {
                    stage = 1;
                    if (traceTmp != null)
                    {
                        traceTmp.text =
                            "<color=#38F28A>✓ Backdoor svchost_upd.exe activo en PC 1</color>\n\n" +
                            "<color=#FFA940><b>Capturando clave del Post-It: Usuario 'c.mendoza' | Clave 'Andino15$!'</b></color>";
                    }
                    if (p1Tmp != null) p1Tmp.text = "<b>1. PC 1 CARLA</b>\n<color=#FF3834><b>INFECTADA</b></color>";
                }

                yield return null;
            }

            if (fillRect != null) fillRect.sizeDelta = new Vector2(760f, 0f);
            if (traceTmp != null)
            {
                traceTmp.text =
                    "<color=#FF5A4E><b>✓ PC 1 COMPROMETIDA Y CREDENCIALES ROBADAS</b></color>\n" +
                    "<size=18>• Con la clave del Post-It y la red plana sin VLAN, el atacante salta a la <b>PC 2 (SWIFT)</b>.\n" +
                    "• Haz clic en <b>CONTINUAR A PC 2</b> para seguir el recorrido del ataque.</size>";
            }
            if (p2Tmp != null)
            {
                p2Tmp.color = Color.white;
                p2Tmp.text = "<b>2. PC 2 SWIFT</b>\n<color=#00E5FF><b>HAZ CLIC PARA IR ></b></color>";
            }
        }

        public void OpenAttackResultSummary()
        {
            RunSequentialOverlayAction(CreateAttackResultViewRoutine());
        }

        private IEnumerator CreateAttackResultViewRoutine()
        {
            return TransitionToViewRoutine(0, BuildAttackResultView, null, "Voice_11_RoboConsumado");
        }

        private void BuildAttackResultView()
        {
            SetHeaderTheme(
                DangerRedColor,
                "RESUMEN DEL ATAQUE · ROBO SWIFT US$ 81M",
                "Recorrido completado en PC 1, PC 2 y PC 3 · Ahora compara con Modo Defensa"
            );

            GameObject robPanel = new GameObject("RobberyResultPanel");
            robPanel.transform.SetParent(_contentContainer, false);
            RectTransform rRect = robPanel.AddComponent<RectTransform>();
            rRect.anchorMin = Vector2.zero;
            rRect.anchorMax = Vector2.one;
            rRect.sizeDelta = Vector2.zero;

            GameObject rBox = CreateStyledCard("RobberyBox", robPanel.transform, new Vector2(0f, 38f), new Vector2(880f, 285f),
                new Color(0.015f, 0.035f, 0.055f, 0.995f), DangerRedColor);

            StringBuilder rsb = new StringBuilder();
            rsb.AppendLine("<size=23><color=#FF3834><b>ROBO CONSUMADO: US$ 81,000,000.00 DESVIADOS</b></color></size>\n");
            rsb.AppendLine("<size=19>• <b>1. PC 1 (Correo):</b> Se ejecutó <i>CV_2015.scr.zip</i> y se robó la clave del Post-It.</size>");
            rsb.AppendLine("<size=19>• <b>2. PC 2 (SWIFT):</b> Salto por red plana sin MFA para enviar US$ 81M a Filipinas.</size>");
            rsb.AppendLine("<size=19>• <b>3. PC 3 (SIEM):</b> Alertas silenciadas y salida activa hacia C2 185.130.44.82.</size>\n");
            rsb.AppendLine("<size=20><color=#38F28A><b>Siguiente paso recomendado:</b> Haz el recorrido en <b>MODO DEFENSA</b> para ver cómo se protege cada PC.</color></size>");

            CreateLabel("Txt_RobberySummary", rBox.transform, Vector2.zero, new Vector2(840f, 250f),
                rsb.ToString(), 19.5f, TextAlignmentOptions.Center, Color.white);

            CreateInspectionButton(
                "Btn_StartDefenseTourFromAttack", robPanel.transform,
                new Vector2(-280f, -168f), new Vector2(320f, 64f),
                "<color=#38F28A><b>VER MODO DEFENSA (PC 1) ></b></color>",
                new Color(0.08f, 0.35f, 0.20f, 0.98f),
                Color.white,
                () => RunSequentialOverlayAction(SwitchModeAndTeleportRoutine(SimMode.Defensa, "Teleport_Puesto1_Carla", () => CreateQuarantineViewRoutine(0)))
            );

            CreateInspectionButton(
                "Btn_GoBoardAfterAttack", robPanel.transform,
                new Vector2(45f, -168f), new Vector2(280f, 64f),
                "IR AL TABLERO >",
                new Color(0.10f, 0.30f, 0.42f, 0.98f),
                CyanNeonAccent,
                NavigateToSuspectBoardOrDefense
            );

            CreateInspectionButton(
                "Btn_FinalizeAttack", robPanel.transform,
                new Vector2(325f, -168f), new Vector2(230f, 64f),
                "FINALIZAR",
                new Color(0.48f, 0.12f, 0.14f, 0.98f),
                Color.white,
                () => CloseOverlayWithCallback(() =>
                {
                    ScenarioFlowController.I?.CompleteScenarioFlow();
                    if (GameManager.I != null)
                    {
                        GameManager.I.EndScenario();
                    }
                })
            );

            _activeContentPanel = robPanel;
        }

        public void OpenCredentialsInspection()
        {
            RunSequentialOverlayAction(CreateCredentialsViewRoutine());
        }

        private IEnumerator CreateCredentialsViewRoutine()
        {
            return TransitionToViewRoutine(1, BuildCredentialsView, null, "Voice_13_PostItEvidencia");
        }

        private void BuildCredentialsView()
        {
            if (GameManager.I != null)
            {
                GameManager.I.RegisterEvidence("Evidencia_PostIt");
            }

            SetHeaderTheme(
                WarningAmberColor,
                "PC 1 · EVIDENCIA DE CLAVES EN POST-IT",
                "Credenciales SWIFT anotadas a la vista junto al monitor de Carla"
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
                "<b>FALLA DE SEGURIDAD</b>", 20f, TextAlignmentOptions.Center, WarningAmberColor);

            CreateLabel("Txt_DiagBody", diagBox.transform, new Vector2(0f, -20f), new Vector2(370f, 190f),
                "• Clave anotada en papel sin Token MFA.\n\n" +
                "• Permite autorizar giros en la <b>PC 2 (SWIFT)</b>.",
                19f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

            CreateInspectionButton(
                "Btn_BackToEmailFromPostIt", credPanel.transform,
                new Vector2(-235f, -170f), new Vector2(380f, 62f),
                "< VOLVER AL CORREO (PC 1)",
                new Color(0.14f, 0.24f, 0.36f, 0.98f),
                Color.white,
                () => OpenEmailInspection(1)
            );

            CreateInspectionButton(
                "Btn_SaveAndNextPc2", credPanel.transform,
                new Vector2(205f, -170f), new Vector2(450f, 62f),
                "CONTINUAR A PC 2 (TERMINAL SWIFT) >",
                new Color(0.10f, 0.38f, 0.22f, 0.98f),
                GreenSuccessColor,
                () => TeleportAndOpenStation("Teleport_Puesto2_Analista", CreateSwiftViewRoutine)
            );

            _activeContentPanel = credPanel;
        }

        public void OpenSwiftInspection()
        {
            RunSequentialOverlayAction(CreateSwiftViewRoutine());
        }

        private IEnumerator CreateSwiftViewRoutine()
        {
            return TransitionToViewRoutine(2, BuildSwiftView, null, "Voice_14_TerminalSwift");
        }

        private void BuildSwiftView()
        {
            SetHeaderTheme(
                CyanNeonAccent,
                "PASO 2 DE 3 · PC 2 (TERMINAL DE PAGOS SWIFT)",
                "El malware saltó desde la PC 1 e inyectó una transferencia de US$ 81M"
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

            TextMeshProUGUI swiftTableTmp = CreateLabel("Txt_SwiftTable", tableBox.transform, Vector2.zero, new Vector2(840f, 130f),
                "<b><color=#00E5FF>OPERACIONES SWIFT INTERNACIONALES (MT103)</color></b>\n" +
                "OP #9481 | RESERVA FEDERAL NY   US$ 1,250,000   <color=#38F28A><b>AUTORIZADO [OK]</b></color>\n" +
                "OP #9482 | BARCLAYS LONDRES     US$   480,000   <color=#38F28A><b>AUTORIZADO [OK]</b></color>\n" +
                "<color=#FF5A4E><b>OP #9483 | RCBC MAKATI MANILA   US$ 81,000,000   [ORDEN INYECTADA POR TROYANO]</b></color>",
                18.5f, TextAlignmentOptions.Center, Color.white);

            // Las 3 opciones interactivas para marcar
            TextMeshProUGUI swiftHintTmp = CreateLabel("Lbl_SwiftHint", swiftPanel.transform, new Vector2(0f, 32f), new Vector2(880f, 30f),
                string.Empty,
                17f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

            BindOptionChecklistCounter(
                swiftHintTmp,
                "Haz clic en las 3 fallas que permitieron el salto a esta PC",
                "<color=#38F28A><b>✓ 3/3 FALLAS VERIFICADAS · Haz clic abajo para continuar a PC 3 ></b></color>",
                (swiftPanel.transform, "Btn_OptSwift1", new Vector2(-295f, -30f), new Vector2(280f, 82f), "1. DESTINO ANÓMALO", "RCBC Makati Filipinas"),
                (swiftPanel.transform, "Btn_OptSwift2", new Vector2(0f, -30f), new Vector2(280f, 82f), "2. RED PLANA EXPUESTA", "Sin VLAN con PC 1"),
                (swiftPanel.transform, "Btn_OptSwift3", new Vector2(295f, -30f), new Vector2(280f, 82f), "3. SIN TOKEN MFA", "Solo usó clave del Post-It")
            );

            // Botones de acción inferior
            CreateInspectionButton(
                "Btn_SimulateTheftAndGoPc3", swiftPanel.transform,
                new Vector2(-215f, -142f), new Vector2(450f, 66f),
                "<color=#FF5A4E><b>EJECUTAR GIRO Y PASAR A PC 3 (SIEM) ></b></color>",
                new Color(0.48f, 0.12f, 0.14f, 0.98f),
                Color.white,
                () => RunSequentialOverlayAction(SwiftTransferAndAdvanceToPc3Routine(swiftTableTmp))
            );

            CreateInspectionButton(
                "Btn_BlockSwiftDefense", swiftPanel.transform,
                new Vector2(235f, -142f), new Vector2(410f, 66f),
                "<color=#38F28A><b>VER DEFENSA EN PC 2 (TOKEN MFA) ></b></color>",
                new Color(0.08f, 0.34f, 0.20f, 0.98f),
                GreenSuccessColor,
                () => RunSequentialOverlayAction(SwitchModeAndOpenQuarantineRoutine(1))
            );

            _activeContentPanel = swiftPanel;
        }

        private IEnumerator SwiftTransferAndAdvanceToPc3Routine(TextMeshProUGUI swiftTableTmp)
        {
            if (InfectionSpreadVisualizer.I != null && !InfectionSpreadVisualizer.I.HasPlayed)
            {
                InfectionSpreadVisualizer.I.PlayInfectionSequence();
            }

            const float duration = 1.35f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float amount = Mathf.Lerp(0f, 81000000f, u);
                if (swiftTableTmp != null)
                {
                    swiftTableTmp.text =
                        "<b><color=#FF5A4E>EJECUTANDO TRANSFERENCIA FRAUDULENTA MT103 #9483...</color></b>\n\n" +
                        $"<size=22><color=#FFA940><b>DESVIANDO FONDOS A RCBC FILIPINAS: US$ {amount:N0}</b></color></size>\n" +
                        "<size=16>Autorizado con credencial robada 'c.mendoza' · Pasando a PC 3 (Consola SIEM)...</size>";
                }
                yield return null;
            }

            yield return StartCoroutine(TeleportAndOpenStationSequenceRoutine("Teleport_Puesto3_LogsYDefensa", CreateSiemViewRoutine));
        }

        public void OpenSiemLogInspection()
        {
            RunSequentialOverlayAction(CreateSiemViewRoutine());
        }

        private IEnumerator CreateSiemViewRoutine()
        {
            return TransitionToViewRoutine(3, BuildSiemView, null, "Voice_15_ConsolaSiem");
        }

        private void BuildSiemView()
        {
            if (GameManager.I != null)
            {
                GameManager.I.RegisterEvidence("Evidencia_LogAccesos");
            }

            SetHeaderTheme(
                GreenSuccessColor,
                "PASO 3 DE 3 · PC 3 (CONSOLA SIEM DE SEGURIDAD)",
                "Examina las 3 huellas de red del ataque y pasa al resultado o al modo defensa"
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
                "<b><color=#38F28A>REGISTROS FORENSES SIEM SOC · EVIDENCIA GUARDADA [OK]</color></b>\n" +
                "[09:14] PC 1 (Correo): Ejecución de <color=#FFA940>CV_2015.scr.zip</color> e inyección de <color=#FF5A4E>svchost_upd.exe</color>\n" +
                "[09:18] PC 2 (SWIFT): Salto por red plana y giro MT103 por <color=#FF5A4E>US$ 81,000,000</color>\n" +
                "[09:21] PC 3 (Firewall): <color=#FF5A4E>Canal C2 activo hacia IP 185.130.44.82 (Puerto 443)</color>",
                18f, TextAlignmentOptions.Center, Color.white);

            // Las 3 opciones interactivas para marcar
            TextMeshProUGUI siemHintTmp = CreateLabel("Lbl_SiemHint", siemPanel.transform, new Vector2(0f, 32f), new Vector2(880f, 30f),
                string.Empty,
                17f, TextAlignmentOptions.Center, AndesTypographyTheme.TextPrimary);

            BindOptionChecklistCounter(
                siemHintTmp,
                "Haz clic en los 3 indicadores de compromiso registrados en el SIEM",
                "<color=#38F28A><b>✓ 3/3 INDICADORES VERIFICADOS · Haz clic abajo en Resumen o Modo Defensa ></b></color>",
                (siemPanel.transform, "Btn_OptSiem1", new Vector2(-295f, -30f), new Vector2(280f, 82f), "1. IP HOSTIL C2", "185.130.44.82"),
                (siemPanel.transform, "Btn_OptSiem2", new Vector2(0f, -30f), new Vector2(280f, 82f), "2. PROCESO INYECTADO", "svchost_upd.exe (PID 4820)"),
                (siemPanel.transform, "Btn_OptSiem3", new Vector2(295f, -30f), new Vector2(280f, 82f), "3. CANAL SSL/443", "Túnel cifrado malicioso")
            );

            // Botones inferiores bien espaciados sin superposición
            CreateInspectionButton(
                "Btn_ViewAttackSummaryFromSiem", siemPanel.transform,
                new Vector2(-225f, -142f), new Vector2(430f, 66f),
                "<color=#FF5A4E><b>VER RESUMEN DEL ATAQUE (US$ 81M) ></b></color>",
                new Color(0.46f, 0.12f, 0.14f, 0.98f),
                Color.white,
                OpenAttackResultSummary
            );

            CreateInspectionButton(
                "Btn_StartDefenseFromSiem", siemPanel.transform,
                new Vector2(225f, -142f), new Vector2(430f, 66f),
                "<color=#38F28A><b>INICIAR RECORRIDO MODO DEFENSA ></b></color>",
                new Color(0.08f, 0.35f, 0.20f, 0.98f),
                GreenSuccessColor,
                () => RunSequentialOverlayAction(SwitchModeAndTeleportRoutine(SimMode.Defensa, "Teleport_Puesto1_Carla", () => CreateQuarantineViewRoutine(0)))
            );

            _activeContentPanel = siemPanel;
        }

        private IEnumerator SwitchModeAndTeleportRoutine(SimMode newMode, string teleportName, Func<IEnumerator> viewRoutineFactory)
        {
            if (GameManager.I != null) GameManager.I.SetMode(newMode);
            yield return StartCoroutine(TeleportAndOpenStationSequenceRoutine(teleportName, viewRoutineFactory));
        }

        public void OpenSuspectDossierInspection(int suspectIndex)
        {
            SuspectBoardVR board = SuspectBoardVR.I != null ? SuspectBoardVR.I : FindFirstObjectByType<SuspectBoardVR>(FindObjectsInactive.Include);
            if (board != null)
            {
                int boardTotal = (board.Suspects != null && board.Suspects.Count > 0) ? board.Suspects.Count : 1;
                int clampedIndex = ((suspectIndex % boardTotal) + boardTotal) % boardTotal;

                // Mantener la interacción dentro de la misma pizarra (Tablero_Sospechosos_VR),
                // cerrando suavemente el HUD antes de actualizar el tablero.
                if (_isOpen)
                {
                    CloseOverlayWithCallback(() =>
                    {
                        board.DisplaySuspect(clampedIndex);
                        AndesSim.Audio.AudioManager.I?.PlayGazeClick();
                    });
                    return;
                }

                board.DisplaySuspect(clampedIndex);
                AndesSim.Audio.AudioManager.I?.PlayGazeClick();
                return;
            }

            RunSequentialOverlayAction(TransitionToViewRoutine(0, () => BuildSuspectDossierFallbackView(suspectIndex), null, "Voice_19_TableroSospechosos", 12.0f));
        }

        private void BuildSuspectDossierFallbackView(int suspectIndex)
        {
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
        }

        private void TeleportAndOpenStation(string teleportObjectName, Func<IEnumerator> viewRoutineFactory)
        {
            RunSequentialOverlayAction(TeleportAndOpenStationSequenceRoutine(teleportObjectName, viewRoutineFactory));
        }

        private IEnumerator TeleportAndOpenStationSequenceRoutine(string teleportObjectName, Func<IEnumerator> viewRoutineFactory)
        {
            GameObject tpObj = !string.IsNullOrEmpty(teleportObjectName) ? GameObject.Find(teleportObjectName) : null;
            TeleportPoint tp = tpObj != null ? tpObj.GetComponent<TeleportPoint>() : null;

            bool needsTeleport = false;
            if (PlayerTeleportController.I != null && tp != null)
            {
                if (_targetCamera == null) _targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
                float distToTarget = _targetCamera != null
                    ? Vector2.Distance(
                        new Vector2(_targetCamera.transform.position.x, _targetCamera.transform.position.z),
                        new Vector2(tp.StandPosition.position.x, tp.StandPosition.position.z))
                    : 99f;
                needsTeleport = distToTarget > 0.65f;
            }

            if (needsTeleport)
            {
                // 1. Cerrar suavemente el HUD actual antes de teletransportar y restaurar geometría previa
                if (_isOpen && _overlayRoot != null && _overlayRoot.activeInHierarchy)
                {
                    yield return FadeCanvasGroupRoutine(_overlayCanvasGroup, _overlayCanvasGroup != null ? _overlayCanvasGroup.alpha : 1f, 0f, 0.14f);
                    _overlayRoot.SetActive(false);
                }

                ClearActiveContent();
                RestoreSceneObstructionsAfterOverlay();
                _isOpen = false;
                _hasAnchorPose = false;

                // 2. Teletransportar y esperar a que concluya el parpadeo por completo
                PlayerTeleportController.I.TeleportToPoint(tp);

                float timeout = 1.2f;
                while (PlayerTeleportController.I.IsTeleporting && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
                yield return null;
            }

            _hasAnchorPose = false;

            // 3. Desplegar el HUD del nuevo puesto en secuencia limpia
            if (viewRoutineFactory != null)
            {
                yield return StartCoroutine(viewRoutineFactory());
            }
        }

        private void NavigateToSuspectBoardOrDefense()
        {
            GameObject tpObj = GameObject.Find("Teleport_TableroSospechosos");
            TeleportPoint tp = tpObj != null ? tpObj.GetComponent<TeleportPoint>() : null;
            if (PlayerTeleportController.I != null && tp != null)
            {
                CloseOverlayWithCallback(() => PlayerTeleportController.I.TeleportToPoint(tp));
                return;
            }
            OpenSuspectDossierInspection(0);
        }

        private void NavigateToMainMenu()
        {
            CloseOverlayWithCallback(() =>
            {
                if (VrStartMenuController.I != null)
                {
                    GameObject lobbyTpObj = GameObject.Find("Teleport_PuertaEntrada");
                    TeleportPoint lobbyTp = lobbyTpObj != null ? lobbyTpObj.GetComponent<TeleportPoint>() : null;
                    if (PlayerTeleportController.I != null && lobbyTp != null)
                    {
                        StartCoroutine(TeleportAndShowMainMenuRoutine(lobbyTp));
                    }
                    else
                    {
                        VrStartMenuController.I.ShowMenu();
                    }
                }
            });
        }

        private IEnumerator TeleportAndShowMainMenuRoutine(TeleportPoint lobbyTp)
        {
            if (PlayerTeleportController.I != null && lobbyTp != null)
            {
                PlayerTeleportController.I.TeleportToPoint(lobbyTp);
                float timeout = 1.2f;
                while (PlayerTeleportController.I.IsTeleporting && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
                yield return null;
            }
            VrStartMenuController.I?.ShowMenu();
        }

        #endregion

        #region Control de Ventana y Ayudantes de UI

        public void CloseOverlay()
        {
            CloseOverlayWithCallback(null);
        }

        private void CloseOverlayWithCallback(Action onClosed)
        {
            if (!_isOpen || _isSequenceBusy) return;
            RunSequentialOverlayAction(CloseOverlaySequenceRoutine(onClosed));
        }

        private IEnumerator CloseOverlaySequenceRoutine(Action onClosed)
        {
            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayGazeClick();
            }

            if (_overlayRoot != null && _overlayRoot.activeInHierarchy && _overlayCanvasGroup != null)
            {
                yield return FadeCanvasGroupRoutine(_overlayCanvasGroup, _overlayCanvasGroup.alpha, 0f, 0.15f);
            }

            _activeEmailHintText = null;
            _isOpen = false;
            _isEnlarged = false;
            _hasAnchorPose = false;
            distanceFromCamera = NormalDistance;

            if (_overlayRoot != null)
            {
                RectTransform canvasRect = _overlayRoot.GetComponent<RectTransform>();
                if (canvasRect != null) canvasRect.localScale = Vector3.one * NormalScale;
                if (_overlayCanvasGroup != null) _overlayCanvasGroup.alpha = 1f;
                _overlayRoot.SetActive(false);
            }

            ClearActiveContent();
            // Restaurar los objetos 3D solo después de que el HUD haya desaparecido por completo
            RestoreSceneObstructionsAfterOverlay();

            onClosed?.Invoke();
        }

        public void ForceCloseOverlayImmediate()
        {
            if (_activeSequenceRoutine != null)
            {
                StopCoroutine(_activeSequenceRoutine);
                _activeSequenceRoutine = null;
            }
            _isSequenceBusy = false;
            _activeEmailHintText = null;
            _isOpen = false;
            _isEnlarged = false;
            _hasAnchorPose = false;
            distanceFromCamera = NormalDistance;

            RestoreSceneObstructionsAfterOverlay();

            if (_overlayRoot != null)
            {
                RectTransform canvasRect = _overlayRoot.GetComponent<RectTransform>();
                if (canvasRect != null) canvasRect.localScale = Vector3.one * NormalScale;
                if (_overlayCanvasGroup != null) _overlayCanvasGroup.alpha = 1f;
                _overlayRoot.SetActive(false);
            }
            ClearActiveContent();
        }

        public void ToggleEnlargeZoom()
        {
            if (_isSequenceBusy || !_isOpen) return;
            SetEnlargeZoom(!_isEnlarged);
        }

        public void SetEnlargeZoom(bool enlarged)
        {
            if (_isSequenceBusy) return;
            _isEnlarged = enlarged;
            UpdateZoomButtonLabel();

            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayGazeClick();
            }

            if (gameObject.activeInHierarchy && _isOpen)
            {
                RunSequentialOverlayAction(AnimateZoomRoutine(_isEnlarged));
            }
            else
            {
                ApplyZoomImmediate(_isEnlarged);
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
            if (_isOpen) SuppressNearbyObstructionsInViewCorridor();
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
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

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
            SuppressNearbyObstructionsInViewCorridor();
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

                // Ocultar todos los monitores físicos, pantallas World Space, palancas, balizas y paneles externos
                SuppressAllRootsOfType<InteractiveMonitorController>();
                SuppressAllRootsOfType<PhysicalDefenseSwitchLever>();
                SuppressAllRootsOfType<AccessLogTerminal>();
                SuppressAllRootsOfType<DefenseControl>();
                SuppressAllRootsOfType<EvidenceItem>();
                SuppressAllRootsOfType<PhishingSignalHotspot>();
                SuppressAllRootsOfType<HolographicDataBeacon>();
                SuppressAllRootsOfType<PostItCredenciales>();
                SuppressAllRootsOfType<TokenMfaBoveda>();
                SuppressAllRootsOfType<TelefonoAnexoSoc>();
                SuppressAllRootsOfType<CredencialesDeskDrawer>();
                SuppressAllRootsOfType<DefenseTerminalController>();
                SuppressAllRootsOfType<TeleportPoint>();
                SuppressAllRootsOfType<SuspectBoardVR>();
                SuppressAllRootsOfType<EndScenarioPanelUI>();
                SuppressAllRootsOfType<CreditsAndEthicsModalUI>();
            }

            // Barrido espacial ampliado en el corredor cámara -> HUD para ocultar mobiliario/mamparas que invadan la vista
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
            Vector3 corridorEnd = panelCenter + (forward * 1.25f);

            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled || ShouldSkipSuppressionTransform(r.transform)) continue;
                if (IsBoundsObstructingOverlay(r.bounds, r.transform, camPos, panelCenter, forward, corridorEnd))
                {
                    SuppressComponent(r, _suppressedRenderers, c => c.enabled, (c, v) => c.enabled = v);
                }
            }

            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || !col.enabled || ShouldSkipSuppressionTransform(col.transform)) continue;
                if (IsBoundsObstructingOverlay(col.bounds, col.transform, camPos, panelCenter, forward, corridorEnd))
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

        private static bool IsBoundsObstructingOverlay(Bounds bounds, Transform objTransform, Vector3 camPos, Vector3 panelCenter, Vector3 forward, Vector3 corridorEnd)
        {
            bool isFloorOrCeilingLike = (bounds.max.y < 0.25f || bounds.min.y > 2.35f) && bounds.size.x > 2.5f && bounds.size.z > 2.5f;
            if (isFloorOrCeilingLike) return false;

            Vector3 closestToPanel = bounds.ClosestPoint(panelCenter);
            if (bounds.Contains(panelCenter) || Vector3.Distance(closestToPanel, panelCenter) <= 0.50f)
            {
                return true;
            }

            bool isNamedProp = IsWorkstationOrFurniturePropName(objTransform);
            if (bounds.size.magnitude > 7.5f || (bounds.size.magnitude > 4.2f && !isNamedProp))
            {
                return false;
            }

            if (Vector3.Distance(closestToPanel, panelCenter) <= 0.85f) return true;

            if (isNamedProp)
            {
                Vector3 toProp = (bounds.center - camPos);
                float distToCam = toProp.magnitude;
                if (distToCam <= 2.20f && distToCam > 0.01f && Vector3.Dot(forward, toProp / distToCam) > 0.15f)
                {
                    return true;
                }
            }

            Vector3 seg = corridorEnd - camPos;
            float segLenSq = seg.sqrMagnitude;
            if (segLenSq <= 0.0001f) return false;

            Vector3 closestToCam = bounds.ClosestPoint(camPos);
            float tCam = Mathf.Clamp01(Vector3.Dot(closestToCam - camPos, seg) / segLenSq);
            if (Vector3.Distance(closestToCam, camPos + seg * tCam) <= 0.82f) return true;

            float tPanel = Mathf.Clamp01(Vector3.Dot(closestToPanel - camPos, seg) / segLenSq);
            return Vector3.Distance(closestToPanel, camPos + seg * tPanel) <= 0.82f;
        }

        private static readonly string[] DeskPropKeywords =
        {
            "monitor", "screen", "pantalla", "desk", "escritorio", "chair", "silla",
            "keyboard", "teclado", "mouse", "phone", "telefono", "postit", "token",
            "lever", "palanca", "cajon", "drawer", "consola", "partition", "mampara",
            "cubicle", "lamp", "mug", "cup", "pc_", "cpu"
        };

        private static bool IsWorkstationOrFurniturePropName(Transform t)
        {
            Transform cur = t;
            for (int depth = 0; depth < 3 && cur != null; depth++)
            {
                string n = cur.name;
                for (int k = 0; k < DeskPropKeywords.Length; k++)
                {
                    if (n.IndexOf(DeskPropKeywords[k], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
                cur = cur.parent;
            }
            return false;
        }

        private void ClearActiveContent()
        {
            _contentButtons.Clear();
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

        private void RegisterOverlayButton(GameObject btnGO, Button btn, GazeInteractable gaze, Transform parent)
        {
            if (btnGO == null) return;
            CanvasGroup cg = btnGO.GetComponent<CanvasGroup>();
            if (cg == null) cg = btnGO.AddComponent<CanvasGroup>();
            OverlayButtonControl entry = new OverlayButtonControl
            {
                Button = btn,
                Gaze = gaze,
                CanvasGroup = cg
            };

            bool isFrameButton = _mainPanelRect != null && parent == _mainPanelRect.transform;
            if (isFrameButton) _frameButtons.Add(entry);
            else _contentButtons.Add(entry);
        }

        private static string StripRichTextTags(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            int lineBreak = raw.IndexOf('\n');
            string singleLine = lineBreak >= 0 ? raw.Substring(0, lineBreak) : raw;
            if (singleLine.IndexOf('<') < 0) return singleLine.Trim();

            StringBuilder sb = new StringBuilder(singleLine.Length);
            bool insideTag = false;
            for (int i = 0; i < singleLine.Length; i++)
            {
                char ch = singleLine[i];
                if (ch == '<') { insideTag = true; continue; }
                if (ch == '>') { insideTag = false; continue; }
                if (!insideTag) sb.Append(ch);
            }
            return sb.ToString().Trim();
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
            float lastInvokedTime = -10f;
            Action invokeOncePerFrame = () =>
            {
                if (_isSequenceBusy || onClick == null || lastInvokedFrame == Time.frameCount || (Time.unscaledTime - lastInvokedTime) < 0.22f) return;
                lastInvokedFrame = Time.frameCount;
                lastInvokedTime = Time.unscaledTime;
                onClick.Invoke();
            };
            if (onClick != null) btn.onClick.AddListener(() => invokeOncePerFrame());

            BoxCollider boxCol = btnGO.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(size.x, size.y, 45f);
            boxCol.center = new Vector3(0f, 0f, -15f);
            boxCol.isTrigger = true;

            GazeInteractable gaze = btnGO.AddComponent<GazeInteractable>();
            gaze.interactableLabel = StripRichTextTags(text);
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

            RegisterOverlayButton(btnGO, btn, gaze, parent);
            return btnGO;
        }

        private void BindOptionChecklistCounter(
            TextMeshProUGUI hintLabel,
            string pendingPrefix,
            string completedMessage,
            params (Transform parent, string name, Vector2 pos, Vector2 size, string title, string detail)[] items)
        {
            int total = items.Length;
            int checkedCount = 0;

            void RefreshHint()
            {
                if (hintLabel == null) return;
                hintLabel.text = checkedCount >= total
                    ? completedMessage
                    : $"{pendingPrefix} ({checkedCount}/{total} verificadas):";
            }

            RefreshHint();

            for (int i = 0; i < total; i++)
            {
                var item = items[i];
                CreateOptionCheckButton(item.name, item.parent, item.pos, item.size, item.title, item.detail, isChecked =>
                {
                    checkedCount = Mathf.Clamp(checkedCount + (isChecked ? 1 : -1), 0, total);
                    RefreshHint();
                });
            }
        }

        private void CreateOptionCheckButton(string name, Transform parent, Vector2 pos, Vector2 size, string title, string detail, Action<bool> onCheckedChanged = null)
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
            int lastToggledFrame = -1;
            float lastToggledTime = -10f;
            Action toggleAction = () =>
            {
                if (_isSequenceBusy || lastToggledFrame == Time.frameCount || (Time.unscaledTime - lastToggledTime) < 0.22f) return;
                lastToggledFrame = Time.frameCount;
                lastToggledTime = Time.unscaledTime;

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

                gaze.SetBaseGraphicColor(bg.color);
                onCheckedChanged?.Invoke(isChecked);
            };

            btn.onClick.AddListener(() => toggleAction.Invoke());
            gaze.onClick.AddListener(() => toggleAction.Invoke());
            RegisterOverlayButton(btnGO, btn, gaze, parent);
        }

        private void UpdateEmailSignalsHint()
        {
            if (_activeEmailHintText == null) return;
            int count = _markedPhishingSignals.Count;
            if (count >= 3)
            {
                _activeEmailHintText.text = "<color=#38F28A><b>✓ 3/3 PISTAS DETECTADAS · Haz clic en SIMULAR ATAQUE o MODO DEFENSA ></b></color>";
            }
            else if (count > 0)
            {
                _activeEmailHintText.text = $"Pistas detectadas: <b>{count}/3</b> · Marca las restantes o elige una acción:";
            }
            else
            {
                _activeEmailHintText.text = "Haz clic en las 3 señales sospechosas del correo (0/3 marcadas):";
            }
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
            bool alreadyMarked = _markedPhishingSignals.Contains(signalId);
            if (alreadyMarked)
            {
                bg.color = new Color(0.08f, 0.35f, 0.19f, 0.96f);
                tmp.text = "<b>[OK] PISTA LISTA</b>";
                AndesTypographyTheme.StyleBadge(tmp, GreenSuccessColor, 18f);
            }
            else
            {
                tmp.text = text;
                AndesTypographyTheme.StyleButtonLabel(tmp, WarningAmberColor, 17.5f);
            }

            int lastMarkedFrame = -1;
            float lastMarkedTime = -10f;
            Action markAction = () =>
            {
                if (_isSequenceBusy || lastMarkedFrame == Time.frameCount || (Time.unscaledTime - lastMarkedTime) < 0.22f) return;
                lastMarkedFrame = Time.frameCount;
                lastMarkedTime = Time.unscaledTime;

                if (ScenarioFlowController.I != null)
                {
                    if (ScenarioFlowController.I.CurrentStep < ScenarioStep.Step4_InspeccionarCorreo)
                    {
                        ScenarioFlowController.I.NotifyWorkstationInteracted("Monitor_01");
                        ScenarioFlowController.I.NotifyEmailOpened();
                    }
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
                _markedPhishingSignals.Add(signalId);
                bg.color = new Color(0.08f, 0.35f, 0.19f, 0.96f);
                tmp.text = "<b>[OK] PISTA LISTA</b>";
                AndesTypographyTheme.StyleBadge(tmp, GreenSuccessColor, 18f);
                gaze.SetBaseGraphicColor(bg.color);
                UpdateEmailSignalsHint();
            };

            btn.onClick.AddListener(() => markAction.Invoke());
            gaze.onClick.AddListener(() => markAction.Invoke());
            RegisterOverlayButton(btnGO, btn, gaze, parent);
        }

        private TextMeshProUGUI CreateLabel(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            AndesTypographyTheme.StyleBodyReadable(tmp, alignment, color, fontSize, 10f);
            return tmp;
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