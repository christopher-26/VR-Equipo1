using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.VR;

namespace AndesSim.UI
{
    /// <summary>
    /// Tipos de notificación forense para el banner flotante NotificationToast.
    /// </summary>
    public enum NotificationType
    {
        PhishingSignal,
        EvidenceFound,
        DefenseActivated,
        ModeChanged,
        GeneralInfo,
        WarningAlert
    }

    /// <summary>
    /// Estructura de datos para encolar notificaciones en el NotificationToast.
    /// </summary>
    public struct ToastData
    {
        public string Title;
        public string Message;
        public NotificationType Type;
        public Color CustomColor;

        public ToastData(string title, string message, NotificationType type, Color customColor)
        {
            Title = title;
            Message = message;
            Type = type;
            CustomColor = customColor;
        }
    }

    /// <summary>
    /// Administrador central de interfaces en World Space para el simulador VR ANDES-SIM (BANCO ANDINO).
    /// Escenario 1: "Oficina de pagos".
    /// 
    /// Implementa la paleta de colores oficial del documento docente:
    /// • Fondo oscuro:      #020a06 (RGB 0.008, 0.039, 0.024)
    /// • Texto principal:   #d6f2e2 (RGB 0.839, 0.949, 0.886)
    /// • Verde código/éxito:#38f28a (RGB 0.220, 0.950, 0.540)
    /// • Alerta roja:       #ff5a4e (RGB 1.000, 0.353, 0.306)
    /// • Ámbar advertencia: #ffc24a (RGB 1.000, 0.761, 0.290)
    /// 
    /// Componentes oficiales:
    /// 1. WristOrDeskHUD:
    ///    - Panel de estado visible en la oficina.
    ///    - Modo activo (ATAQUE 2016 vs CON DEFENSA) con botón interactivo por mirada para alternar.
    ///    - Reloj regresivo 03:00 sincronizado con SimulationClock y GameManager.
    ///    - Paso actual del flujo pedagógico (1 al 6).
    ///    - Checklist en vivo: Señales (0/3), Evidencias (0/3), Defensas Aplicadas (0/3).
    /// 
    /// 2. NotificationToast:
    ///    - Banner flotante suave a 2 metros frente al jugador con orientación hacia la cámara.
    ///    - Se dispara automáticamente al descubrir señales, registrar evidencias o activar contramedidas.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - UIManager]";

        #region Paleta de Colores Oficial

        public static readonly Color ColorDarkBg        = new Color(0.008f, 0.039f, 0.024f, 0.95f); // #020a06
        public static readonly Color ColorLightText     = new Color(0.839f, 0.949f, 0.886f, 1.00f); // #d6f2e2
        public static readonly Color ColorCodeGreen     = new Color(0.220f, 0.950f, 0.540f, 1.00f); // #38f28a
        public static readonly Color ColorAlertRed      = new Color(1.000f, 0.353f, 0.306f, 1.00f); // #ff5a4e
        public static readonly Color ColorWarningAmber  = new Color(1.000f, 0.761f, 0.290f, 1.00f); // #ffc24a

        #endregion

        [Header("--- 1. WristOrDeskHUD (Panel de Estado en World Space) ---")]
        [Tooltip("Contenedor raíz del HUD de estado.")]
        [SerializeField] private GameObject wristOrDeskHudPanel;

        [Tooltip("Texto del modo activo: 'MODO: ATAQUE 2016' o 'MODO: CON DEFENSA'.")]
        [SerializeField] private TextMeshProUGUI modeStatusText;
        [SerializeField] private Image modeStatusBadgeBg;

        [Tooltip("Botón interactuable por mirada para alternar el modo.")]
        [SerializeField] private Button toggleModeButton;
        [SerializeField] private TextMeshProUGUI toggleModeButtonText;
        [SerializeField] private Image toggleModeButtonGlow;

        [Tooltip("Reloj regresivo (formato 03:00 / MM:SS).")]
        [SerializeField] private TextMeshProUGUI countdownClockText;

        [Tooltip("Título e indicador del paso del flujo (1 al 6).")]
        [SerializeField] private TextMeshProUGUI currentStepTitleText;
        [SerializeField] private Image stepProgressBarFill;

        [Header("--- Checklist en Vivo (0/3) ---")]
        [SerializeField] private TextMeshProUGUI phishingSignalsChecklistText;
        [SerializeField] private TextMeshProUGUI evidencesChecklistText;
        [SerializeField] private TextMeshProUGUI defensesChecklistText;
        [SerializeField] private Image phishingSignalsCheckmark;
        [SerializeField] private Image evidencesCheckmark;
        [SerializeField] private Image defensesCheckmark;

        [Header("--- 2. NotificationToast (Banner Flotante a 2m) ---")]
        [Tooltip("Contenedor raíz del banner flotante.")]
        [SerializeField] private GameObject notificationToastPanel;
        [SerializeField] private CanvasGroup toastCanvasGroup;
        [SerializeField] private TextMeshProUGUI toastTitleText;
        [SerializeField] private TextMeshProUGUI toastMessageText;
        [SerializeField] private Image toastBorderGlow;
        [SerializeField] private Image toastBackground;
        [SerializeField] private float toastDistanceInFront = 2.0f;
        [SerializeField] private float toastDisplayDuration = 2.8f;

        [Header("--- Cámara del Jugador ---")]
        [Tooltip("Cámara principal de VR. Si es nula se busca Camera.main.")]
        [SerializeField] private Transform targetCamera;

        [Header("--- Efectos Sonoros ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip toastClip;
        [SerializeField] private AudioClip modeToggleClip;
        [SerializeField] private AudioClip buttonClickClip;

        // Estado interno
        private readonly Queue<ToastData> _toastQueue = new Queue<ToastData>();
        private bool _isToastActive = false;
        private Coroutine _toastCoroutine;

        private void Awake()
        {
            if (I == null)
            {
                I = this;
            }
            else if (I != this)
            {
                Debug.LogWarning($"{LogPrefix} Instancia duplicada detectada en '{gameObject.name}'. Destruyendo.");
                Destroy(this);
                return;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0.5f; // Semiespacial para HUD
                    audioSource.minDistance = 0.5f;
                    audioSource.maxDistance = 15f;
                }
            }
        }

        private void Start()
        {
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }

            // Asegurar que existan los paneles o construirlos dinámicamente
            EnsureUiPanelsExist();

            // Erradicar de raiz cualquier remanente del bloque flotante WristOrDeskHUD en la pared
            if (wristOrDeskHudPanel != null)
            {
                Destroy(wristOrDeskHudPanel);
                wristOrDeskHudPanel = null;
            }
            GameObject lingeringHud = GameObject.Find("WristOrDeskHUD");
            if (lingeringHud != null)
            {
                Destroy(lingeringHud);
            }
            EnsureEventSystemExists();

            if (toggleModeButton != null)
            {
                toggleModeButton.onClick.RemoveAllListeners();
                toggleModeButton.onClick.AddListener(ToggleSimulationMode);
            }

            // Ocultar toast inicialmente
            if (notificationToastPanel != null)
            {
                if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;
                notificationToastPanel.SetActive(false);
            }

            // Suscribirse a eventos del núcleo de la simulación
            SubscribeToEvents();

            // Actualizar vista inicial
            RefreshAllHudDisplays();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // Sincronizar reloj regresivo en tiempo real
            UpdateCountdownClock();
        }

        #region Suscripción a Eventos de Simulación

        private void SubscribeToEvents()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
                GameManager.I.OnPhishingSignalRegistered += HandlePhishingSignalRegistered;
                GameManager.I.OnEvidenceRegistered += HandleEvidenceRegistered;
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
            }

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged += HandleStepChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
                GameManager.I.OnPhishingSignalRegistered -= HandlePhishingSignalRegistered;
                GameManager.I.OnEvidenceRegistered -= HandleEvidenceRegistered;
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
            }

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged -= HandleStepChanged;
            }
        }

        private void HandleModeChanged(SimMode newMode)
        {
            RefreshAllHudDisplays();

            bool isDefensa = newMode == SimMode.Defensa;
            string title = isDefensa ? "MODO DEFENSA ACTIVADO" : "MODO ATAQUE 2016 ACTIVADO";
            string msg = isDefensa
                ? "Estrategia de seguridad en profundidad: Sandbox, botón de reporte y MFA."
                : "Recreando el ataque de 2015: Infección lateral y robo de credenciales en red plana.";
            Color color = isDefensa ? ColorCodeGreen : ColorAlertRed;

            ShowNotification(title, msg, NotificationType.ModeChanged, color);
        }

        private void HandlePhishingSignalRegistered(string signalId)
        {
            RefreshChecklist();

            int count = GameManager.I != null ? GameManager.I.DiscoveredPhishingSignals.Count : 1;
            string name = GetSignalHumanName(signalId);
            string title = $"SEÑAL DE PHISHING DETECTADA [{count}/3]";
            string msg = $"Identificador: {name}\nAnaliza el correo para ubicar las 3 señales antes de 3 minutos.";

            ShowNotification(title, msg, NotificationType.PhishingSignal, ColorWarningAmber);
        }

        private void HandleEvidenceRegistered(string evidenceId)
        {
            RefreshChecklist();

            int count = GameManager.I != null ? GameManager.I.DiscoveredEvidences.Count : 1;
            string name = GetEvidenceHumanName(evidenceId);
            string title = $"EVIDENCIA REGISTRADA [{count}/3]";
            string msg = $"Elemento: {name}\nIncorporado al informe forense oficial del Banco Andino.";

            ShowNotification(title, msg, NotificationType.EvidenceFound, ColorCodeGreen);
        }

        private void HandleDefenseApplied(string defenseId)
        {
            RefreshChecklist();

            int count = GameManager.I != null ? GameManager.I.AppliedDefenses.Count : 1;
            string name = GetDefenseHumanName(defenseId);
            string title = $"CONTRAMEDIDA ACTIVADA [{count}/3]";
            string msg = $"Defensa: {name}\nMitigando el vector de ataque en tiempo real.";

            ShowNotification(title, msg, NotificationType.DefenseActivated, ColorCodeGreen);
        }

        private void HandleStepChanged(ScenarioStep newStep, StepGuideInfo info)
        {
            UpdateStepProgress(newStep, info);
        }

        #endregion

        #region WristOrDeskHUD: Métodos de Actualización Visual

        /// <summary>
        /// Refresca todos los elementos del HUD de estado.
        /// </summary>
        public void RefreshAllHudDisplays()
        {
            UpdateModeDisplay();
            RefreshChecklist();
            UpdateCountdownClock();

            if (ScenarioFlowController.I != null)
            {
                StepGuideInfo info = ScenarioFlowController.I.GetStepInfo(ScenarioFlowController.I.CurrentStep);
                UpdateStepProgress(ScenarioFlowController.I.CurrentStep, info);
            }
        }

        private void UpdateModeDisplay()
        {
            SimMode currentMode = GameManager.I != null ? GameManager.I.mode : SimMode.Ataque;
            bool isDefensa = currentMode == SimMode.Defensa;

            if (modeStatusText != null)
            {
                modeStatusText.text = isDefensa
                    ? "<b>MODO: <color=#38F28A>CON DEFENSA (SOC)</color></b>"
                    : "<b>MODO: <color=#FF5A4E>ATAQUE 2016 (LAZARUS)</color></b>";
            }

            if (modeStatusBadgeBg != null)
            {
                modeStatusBadgeBg.color = isDefensa ? new Color(0.12f, 0.45f, 0.22f, 0.95f) : new Color(0.55f, 0.15f, 0.15f, 0.95f);
            }

            if (toggleModeButtonText != null)
            {
                toggleModeButtonText.text = isDefensa
                    ? "<b>Cambiar a: ATAQUE 2016</b>"
                    : "<b>Cambiar a: CON DEFENSA</b>";
                toggleModeButtonText.color = ColorLightText;
            }

            if (toggleModeButtonGlow != null)
            {
                toggleModeButtonGlow.color = isDefensa
                    ? new Color(0.22f, 0.95f, 0.54f, 0.85f)
                    : new Color(1.0f, 0.35f, 0.31f, 0.85f);
            }
        }

        private void UpdateCountdownClock()
        {
            if (countdownClockText == null) return;

            float remainingSeconds = 180f;
            if (GameManager.I != null)
            {
                remainingSeconds = GameManager.I.RemainingTime;
            }

            int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
            int seconds = Mathf.FloorToInt(remainingSeconds % 60f);

            bool isCritical = remainingSeconds <= 30f;
            string colorHex = isCritical ? "#FF5A4E" : "#38F28A";

            countdownClockText.text = $"TIEMPO: <color={colorHex}><b>{minutes:00}:{seconds:00}</b></color>";
        }

        private void UpdateStepProgress(ScenarioStep step, StepGuideInfo info)
        {
            int stepNum = (int)step;
            string title = !string.IsNullOrEmpty(info.stepTitle) ? info.stepTitle : $"Paso {stepNum}/6";

            if (currentStepTitleText != null)
            {
                currentStepTitleText.text = $"<b>FLUJO:</b> <color=#D6F2E2>{title}</color>";
            }

            if (stepProgressBarFill != null)
            {
                stepProgressBarFill.fillAmount = Mathf.Clamp01((float)stepNum / 6f);
                stepProgressBarFill.color = ColorCodeGreen;
            }
        }

        private void RefreshChecklist()
        {
            int signalsCount = GameManager.I != null ? GameManager.I.DiscoveredPhishingSignals.Count : 0;
            int evidencesCount = GameManager.I != null ? GameManager.I.DiscoveredEvidences.Count : 0;
            int defensesCount = GameManager.I != null ? GameManager.I.AppliedDefenses.Count : 0;

            // 1. Señales de Phishing (0/3)
            if (phishingSignalsChecklistText != null)
            {
                string icon = signalsCount >= 3 ? "<color=#38F28A>[OK]</color>" : "<color=#FFC24A>[-]</color>";
                string countColor = signalsCount >= 3 ? "#38F28A" : (signalsCount > 0 ? "#FFC24A" : "#D6F2E2");
                phishingSignalsChecklistText.text = $"{icon} Señales de Phishing: <color={countColor}><b>{signalsCount}/3</b></color>";
            }
            if (phishingSignalsCheckmark != null)
            {
                phishingSignalsCheckmark.color = signalsCount >= 3 ? ColorCodeGreen : new Color(0.4f, 0.4f, 0.4f, 0.6f);
            }

            // 2. Evidencias Recolectadas (0/3)
            if (evidencesChecklistText != null)
            {
                string icon = evidencesCount >= 3 ? "<color=#38F28A>[OK]</color>" : "<color=#FFC24A>[-]</color>";
                string countColor = evidencesCount >= 3 ? "#38F28A" : (evidencesCount > 0 ? "#FFC24A" : "#D6F2E2");
                evidencesChecklistText.text = $"{icon} Evidencias Forenses: <color={countColor}><b>{evidencesCount}/3</b></color>";
            }
            if (evidencesCheckmark != null)
            {
                evidencesCheckmark.color = evidencesCount >= 3 ? ColorCodeGreen : new Color(0.4f, 0.4f, 0.4f, 0.6f);
            }

            // 3. Defensas Aplicadas (0/3)
            if (defensesChecklistText != null)
            {
                string icon = defensesCount >= 3 ? "<color=#38F28A>[OK]</color>" : "<color=#FFC24A>[-]</color>";
                string countColor = defensesCount >= 3 ? "#38F28A" : (defensesCount > 0 ? "#FFC24A" : "#D6F2E2");
                defensesChecklistText.text = $"{icon} Defensas Aplicadas: <color={countColor}><b>{defensesCount}/3</b></color>";
            }
            if (defensesCheckmark != null)
            {
                defensesCheckmark.color = defensesCount >= 3 ? ColorCodeGreen : new Color(0.4f, 0.4f, 0.4f, 0.6f);
            }
        }

        /// <summary>
        /// Alterna en caliente entre SimMode.Ataque y SimMode.Defensa al hacer clic en el botón por mirada.
        /// </summary>
        public void ToggleSimulationMode()
        {
            if (GameManager.I == null) return;

            SimMode targetMode = GameManager.I.mode == SimMode.Ataque ? SimMode.Defensa : SimMode.Ataque;
            PlaySound(modeToggleClip, GenerateClickTone());

            Debug.Log($"{LogPrefix} <b>Alternando modo de simulación mediante WristOrDeskHUD a: {targetMode}</b>");
            GameManager.I.SetMode(targetMode);
        }

        #endregion

        #region NotificationToast: Banner Flotante Suave (2m)

        /// <summary>
        /// Muestra una notificación emergente flotante frente al jugador con diseño de alta legibilidad.
        /// </summary>
        public void ShowNotification(string title, string message, NotificationType type = NotificationType.GeneralInfo, Color? accentColor = null)
        {
            Color effectiveColor = accentColor ?? GetColorForType(type);
            ToastData data = new ToastData(title, message, type, effectiveColor);

            _toastQueue.Enqueue(data);

            if (!_isToastActive)
            {
                if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
                _toastCoroutine = StartCoroutine(ProcessToastQueueRoutine());
            }
        }

        /// <summary>
        /// Sobrecarga simplificada para mostrar un aviso rápido en el NotificationToast.
        /// </summary>
        public void ShowNotificationToast(string message, float duration = 3f)
        {
            ShowNotification("NOTIFICACIÓN FORENSE", message, NotificationType.GeneralInfo);
        }

        private IEnumerator ProcessToastQueueRoutine()
        {
            _isToastActive = true;

            while (_toastQueue.Count > 0)
            {
                ToastData data = _toastQueue.Dequeue();

                // 1. Posicionar el panel suavemente a 2 metros frente a la cámara del jugador
                PositionToastInFrontOfPlayer();

                // 2. Configurar contenidos y colores de la paleta oficial
                if (toastTitleText != null)
                {
                    string hex = ColorUtility.ToHtmlStringRGB(data.CustomColor);
                    string icon = GetIconForType(data.Type);
                    toastTitleText.text = $"<b><color=#{hex}>{icon} {data.Title.ToUpperInvariant()}</color></b>";
                }

                if (toastMessageText != null)
                {
                    toastMessageText.text = data.Message;
                    toastMessageText.color = ColorLightText;
                }

                if (toastBorderGlow != null)
                {
                    toastBorderGlow.color = data.CustomColor;
                }

                if (toastBackground != null)
                {
                    toastBackground.color = ColorDarkBg;
                }

                if (notificationToastPanel != null)
                {
                    notificationToastPanel.SetActive(true);
                }

                PlaySound(toastClip, GenerateToastTone());

                // 3. Fade In suave (0.2s)
                float elapsed = 0f;
                float fadeInDuration = 0.22f;
                while (elapsed < fadeInDuration)
                {
                    elapsed += Time.deltaTime;
                    if (toastCanvasGroup != null)
                    {
                        toastCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                    }
                    yield return null;
                }

                if (toastCanvasGroup != null) toastCanvasGroup.alpha = 1f;

                // 4. Mantener en pantalla orientándose suavemente hacia el jugador
                float displayTimer = 0f;
                while (displayTimer < toastDisplayDuration)
                {
                    displayTimer += Time.deltaTime;
                    PositionToastInFrontOfPlayer();
                    yield return null;
                }

                // 5. Fade Out suave (0.25s)
                elapsed = 0f;
                float fadeOutDuration = 0.25f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    if (toastCanvasGroup != null)
                    {
                        toastCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeOutDuration));
                    }
                    yield return null;
                }

                if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;
                if (notificationToastPanel != null) notificationToastPanel.SetActive(false);

                yield return new WaitForSeconds(0.1f);
            }

            _isToastActive = false;
        }

        private void PositionToastInFrontOfPlayer()
        {
            if (notificationToastPanel == null) return;

            Transform cam = targetCamera != null ? targetCamera : (Camera.main != null ? Camera.main.transform : null);
            if (cam == null) return;

            // 2.0 metros exactamente frente a la vista del usuario con leve inclinación ergonómica
            Vector3 forwardFlat = cam.forward;
            Vector3 targetPos = cam.position + (forwardFlat * toastDistanceInFront) + (Vector3.up * 0.15f);

            notificationToastPanel.transform.position = Vector3.Lerp(notificationToastPanel.transform.position, targetPos, Time.deltaTime * 10f);
            notificationToastPanel.transform.rotation = Quaternion.LookRotation(notificationToastPanel.transform.position - cam.position);
        }

        #endregion

        #region Construcción Dinámica de Respaldo de Interfaz (Runtime Fallback)

        /// <summary>
        /// Garantiza que si la escena no cuenta con los objetos serializados, se creen dinámicamente
        /// con alta legibilidad en World Space sin errores de referencia nula.
        /// </summary>
        private void EnsureUiPanelsExist()
        {
            // 2. NotificationToast
            if (notificationToastPanel == null)
            {
                notificationToastPanel = new GameObject("NotificationToast");
                notificationToastPanel.transform.SetParent(transform, false);
                notificationToastPanel.transform.position = new Vector3(0f, 1.5f, 2.0f);

                Canvas toastCanvas = notificationToastPanel.AddComponent<Canvas>();
                toastCanvas.renderMode = RenderMode.WorldSpace;
                RectTransform tR = notificationToastPanel.GetComponent<RectTransform>();
                tR.sizeDelta = new Vector2(280f, 85f);
                tR.localScale = new Vector3(0.0035f, 0.0035f, 0.0035f);
                notificationToastPanel.AddComponent<CanvasScaler>();

                toastCanvasGroup = notificationToastPanel.AddComponent<CanvasGroup>();
                toastCanvasGroup.alpha = 0f;

                // Fondo oscuro oficial #020a06
                toastBackground = notificationToastPanel.AddComponent<Image>();
                toastBackground.color = ColorDarkBg;

                // Borde glow
                GameObject borderObj = new GameObject("Border_Glow");
                borderObj.transform.SetParent(notificationToastPanel.transform, false);
                RectTransform bR = borderObj.AddComponent<RectTransform>();
                bR.anchorMin = Vector2.zero;
                bR.anchorMax = Vector2.one;
                bR.sizeDelta = new Vector2(4f, 4f);
                toastBorderGlow = borderObj.AddComponent<Image>();
                toastBorderGlow.color = ColorCodeGreen;

                // Título
                GameObject tTitleObj = CreateTextObject("Txt_ToastTitle", notificationToastPanel.transform, new Vector2(0f, 22f), new Vector2(265f, 20f));
                toastTitleText = tTitleObj.GetComponent<TextMeshProUGUI>();
                toastTitleText.fontSize = 8.5f;

                // Mensaje
                GameObject tMsgObj = CreateTextObject("Txt_ToastMessage", notificationToastPanel.transform, new Vector2(0f, -12f), new Vector2(265f, 44f));
                toastMessageText = tMsgObj.GetComponent<TextMeshProUGUI>();
                toastMessageText.fontSize = 7.2f;

                notificationToastPanel.SetActive(false);
            }
        }

        private static GameObject CreateTextObject(string name, Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.color = ColorLightText;
            tmp.alignment = TextAlignmentOptions.Center;
            return obj;
        }

        #endregion

        #region Helpers de Texto y Paleta

        private static Color GetColorForType(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.PhishingSignal:
                    return ColorWarningAmber;
                case NotificationType.EvidenceFound:
                case NotificationType.DefenseActivated:
                    return ColorCodeGreen;
                case NotificationType.WarningAlert:
                    return ColorAlertRed;
                case NotificationType.ModeChanged:
                    return ColorCodeGreen;
                default:
                    return ColorLightText;
            }
        }

        private static string GetIconForType(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.PhishingSignal:
                    return "[ALERTA]";
                case NotificationType.EvidenceFound:
                    return "[EVIDENCIA]";
                case NotificationType.DefenseActivated:
                    return "[DEFENSA]";
                case NotificationType.WarningAlert:
                    return "[AVISO]";
                case NotificationType.ModeChanged:
                    return "[MODO]";
                default:
                    return "[INFO]";
            }
        }

        private static string GetSignalHumanName(string signalId)
        {
            if (signalId.Equals("Signal_Remitente", StringComparison.OrdinalIgnoreCase)) return "Dominio externo no verificado (rasel.ahlam@...)";
            if (signalId.Equals("Signal_Urgencia", StringComparison.OrdinalIgnoreCase)) return "Ingeniería social con sentido de urgencia psicológica";
            if (signalId.Equals("Signal_Enlace", StringComparison.OrdinalIgnoreCase)) return "Enlace con doble extensión ejecutable (.scr.zip)";
            return signalId;
        }

        private static string GetEvidenceHumanName(string evidenceId)
        {
            if (evidenceId.Equals("Evidencia_Correo", StringComparison.OrdinalIgnoreCase)) return "Correo Malicioso de Phishing (Puesto 1)";
            if (evidenceId.Equals("Evidencia_PostIt", StringComparison.OrdinalIgnoreCase)) return "Credenciales SWIFT Expuestas (Carla Mendoza)";
            if (evidenceId.Equals("Evidencia_LogAccesos", StringComparison.OrdinalIgnoreCase)) return "Eventos de Conexión y Dropper (Puesto 3 - SIEM)";
            return evidenceId;
        }

        private static string GetDefenseHumanName(string defenseId)
        {
            if (defenseId.Equals("Defensa_Sandbox", StringComparison.OrdinalIgnoreCase)) return "Análisis en Sandbox Cuckoo / FireEye";
            if (defenseId.Equals("Defensa_BotonReportar", StringComparison.OrdinalIgnoreCase)) return "Capacitación y Botón de Reporte al SOC";
            if (defenseId.Equals("Defensa_MFA", StringComparison.OrdinalIgnoreCase)) return "Doble Factor (MFA) y Bóveda Criptográfica";
            return defenseId;
        }

        #endregion

        #region Generación Procedural de Audio

        private void PlaySound(AudioClip clip, AudioClip fallback = null)
        {
            if (audioSource == null) return;
            AudioClip toPlay = clip != null ? clip : fallback;
            if (toPlay != null)
            {
                audioSource.PlayOneShot(toPlay, 0.85f);
            }
        }

        private static AudioClip GenerateToastTone()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(t / duration * Mathf.PI);
                float f1 = (t < 0.12f) ? 587.33f : 880.00f; // D5 -> A5
                samples[i] = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.22f * env;
            }

            AudioClip clip = AudioClip.Create("UI_ToastChime", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateClickTone()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 720f * t) * 0.18f * env;
            }

            AudioClip clip = AudioClip.Create("UI_ClickTone", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
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
