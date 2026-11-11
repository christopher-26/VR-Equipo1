using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Data;
using AndesSim.VR;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Controlador principal del ciclo de vida del Panel_Correo en Monitor_01 (Puesto 1: Carla Mendoza).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// 
    /// Especificación técnica:
    /// 1. Lleva el control de las 3 señales ('Signal_Remitente', 'Signal_Urgencia', 'Signal_Enlace').
    /// 2. Al marcar cada señal, llama a GameManager.I.RegisterPhishingSignal(id).
    /// 3. Cuando las 3 señales han sido encontradas:
    ///    - Registra automáticamente la evidencia del correo: GameManager.I.RegisterEvidence("Evidencia_Correo").
    ///    - Verifica si se logró en menos de 3 minutos (<= 180s) y muestra un banner de logro del Criterio de Aceptación 1.
    ///    - Habilita los botones de acción del correo según el modo:
    ///      - Botón "Abrir enlace del CV (Recrear Ataque 2016)": En SimMode.Ataque, inicia la secuencia de infección de las 3 computadoras.
    ///      - Botón "Reportar Phishing a Seguridad TI (Modo Defensa)": Activa el reporte del incidente, aplica "Defensa_BotonReportar" y coordina con MailQuarantine el aislamiento del correo.
    /// </summary>
    public class PhishingInspector : MonoBehaviour
    {
        public static PhishingInspector I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - PhishingInspector]";

        // Constantes oficiales del escenario
        public const string SignalRemitenteId = "Signal_Remitente";
        public const string SignalUrgenciaId = "Signal_Urgencia";
        public const string SignalEnlaceId = "Signal_Enlace";
        public const string EvidenceCorreoId = "Evidencia_Correo";
        public const string DefenseReportarId = "Defensa_BotonReportar";
        public const float TimeLimitSeconds = 180f; // 3 minutos oficiales

        private static readonly Color GreenSuccessColor = new Color(0.22f, 0.95f, 0.54f, 1f); // #38f28a
        private static readonly Color AmberWarningColor = new Color(1.0f, 0.62f, 0.26f, 1f);  // #ff9f43
        private static readonly Color RedInfectionColor = new Color(1.0f, 0.35f, 0.31f, 1f);  // #ff5a4e

        [Header("--- Hotspots de Señales de Phishing (RF-03) ---")]
        [Tooltip("Hotspot para la cabecera 'De: Rasel Ahlam <rasel.ahlam@career-applicants-bd.com>'.")]
        [SerializeField] private PhishingSignalHotspot senderHotspot;

        [Tooltip("Hotspot para el texto con urgencia psicológica e ingeniería social.")]
        [SerializeField] private PhishingSignalHotspot urgencyHotspot;

        [Tooltip("Hotspot para el hipervínculo malicioso con doble extensión .scr.zip.")]
        [SerializeField] private PhishingSignalHotspot linkHotspot;

        [Header("--- Botones de Decisión (Paso 5) ---")]
        [Tooltip("Botón para abrir el enlace del CV y recrear la infección de 2016 en Modo Ataque.")]
        [SerializeField] private Button openLinkCvButton;
        [SerializeField] private TextMeshProUGUI openLinkButtonText;
        [SerializeField] private Image openLinkButtonGlow;

        [Tooltip("Botón para reportar el correo a Seguridad TI en Modo Defensa.")]
        [SerializeField] private Button reportPhishingButton;
        [SerializeField] private TextMeshProUGUI reportButtonText;
        [SerializeField] private Image reportButtonGlow;

        [Header("--- Panel de Logro Criterio de Aceptación 1 ---")]
        [Tooltip("Banner visual de logro al completar las 3 señales en <= 180s.")]
        [SerializeField] private GameObject criterionBannerPanel;
        [SerializeField] private TextMeshProUGUI criterionBannerTitle;
        [SerializeField] private TextMeshProUGUI criterionBannerDetails;
        [SerializeField] private Image criterionBannerBackground;

        [Header("--- Contador de Progreso de Señales ---")]
        [SerializeField] private TextMeshProUGUI signalsCounterText;
        [SerializeField] private Image signalsProgressBar;

        [Header("--- Banners de Estado de Decisión ---")]
        [SerializeField] private GameObject infectionAlertBanner;
        [SerializeField] private TextMeshProUGUI infectionAlertText;
        [SerializeField] private GameObject quarantineNoticeBanner;
        [SerializeField] private TextMeshProUGUI quarantineNoticeText;

        [Header("--- Referencias de Escena ---")]
        [Tooltip("Controlador de cuarentena de correo.")]
        [SerializeField] private MailQuarantine mailQuarantine;

        [Tooltip("Controlador interactivo de Monitor_01 (PC-PAGOS-01).")]
        [SerializeField] private InteractiveMonitorController monitor01Controller;

        [Tooltip("Controlador interactivo de Monitor_02 (PC-PAGOS-02).")]
        [SerializeField] private InteractiveMonitorController monitor02Controller;

        [Tooltip("Controlador interactivo de Monitor_03 (PC-PAGOS-09 / SIEM).")]
        [SerializeField] private InteractiveMonitorController monitor03Controller;

        [Header("--- Efectos Sonoros ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip signalFoundClip;
        [SerializeField] private AudioClip allSignalsCompletedClip;
        [SerializeField] private AudioClip infectionSpreadClip;
        [SerializeField] private AudioClip reportDefenseClip;

        // Estado interno
        private readonly HashSet<string> _discoveredSignals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _allSignalsFound = false;
        private bool _criterion1Passed = false;
        private float _completionTime = 0f;
        private bool _decisionExecuted = false;
        private Coroutine _infectionSequenceCoroutine;

        #region Propiedades Públicas

        public int DiscoveredCount => _discoveredSignals.Count;
        public bool AllSignalsFound => _allSignalsFound;
        public bool Criterion1Passed => _criterion1Passed;
        public float CompletionTime => _completionTime;
        public bool DecisionExecuted => _decisionExecuted;

        #endregion

        private void Awake()
        {
            if (I == null)
            {
                I = this;
            }
            else if (I != this)
            {
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
                    audioSource.spatialBlend = 0.5f;
                }
            }
        }

        private void Start()
        {
            ResolveSceneReferences();
            SetupActionButtons();
            UpdateCounterUi();

            // Ocultar banners iniciales
            if (criterionBannerPanel != null) criterionBannerPanel.SetActive(false);
            if (infectionAlertBanner != null) infectionAlertBanner.SetActive(false);
            if (quarantineNoticeBanner != null) quarantineNoticeBanner.SetActive(false);

            // Suscripciones globales
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
                GameManager.I.OnSimulationReset += HandleSimulationReset;
            }

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged += HandleStepChanged;
            }

            UpdateButtonVisibility();
        }

        private void OnDestroy()
        {
            if (I == this) I = null;

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
                GameManager.I.OnSimulationReset -= HandleSimulationReset;
            }

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged -= HandleStepChanged;
            }
        }

        /// <summary>
        /// Localiza automáticamente los componentes y monitores de la oficina si no fueron asignados manualmente.
        /// </summary>
        private void ResolveSceneReferences()
        {
            if (mailQuarantine == null)
            {
                mailQuarantine = GetComponentInChildren<MailQuarantine>();
                if (mailQuarantine == null)
                {
                    mailQuarantine = FindFirstObjectByType<MailQuarantine>();
                }
            }

            if (monitor01Controller == null || monitor02Controller == null || monitor03Controller == null)
            {
                InteractiveMonitorController[] monitors = FindObjectsByType<InteractiveMonitorController>(FindObjectsSortMode.None);
                foreach (InteractiveMonitorController mon in monitors)
                {
                    string id = (mon.MonitorId + " " + mon.name).ToLowerInvariant();
                    if (id.Contains("01") || id.Contains("carla") || id.Contains("puesto 1"))
                    {
                        if (monitor01Controller == null) monitor01Controller = mon;
                    }
                    else if (id.Contains("02") || id.Contains("analista") || id.Contains("puesto 2"))
                    {
                        if (monitor02Controller == null) monitor02Controller = mon;
                    }
                    else if (id.Contains("03") || id.Contains("siem") || id.Contains("registros") || id.Contains("puesto 3"))
                    {
                        if (monitor03Controller == null) monitor03Controller = mon;
                    }
                }
            }
        }

        /// <summary>
        /// Configura los listeners y adaptadores VR de los botones de decisión del correo.
        /// </summary>
        private void SetupActionButtons()
        {
            if (openLinkCvButton != null)
            {
                openLinkCvButton.onClick.RemoveAllListeners();
                openLinkCvButton.onClick.AddListener(ExecuteOpenLinkAttackSequence);

                // Asegurar soporte de retícula VR
                GazeInteractable gaze = openLinkCvButton.GetComponent<GazeInteractable>();
                if (gaze != null)
                {
                    gaze.interactableLabel = "Abrir enlace del CV (Recrear Ataque 2016)";
                    gaze.onClick.RemoveAllListeners();
                    gaze.onClick.AddListener(ExecuteOpenLinkAttackSequence);
                }
            }

            if (reportPhishingButton != null)
            {
                reportPhishingButton.onClick.RemoveAllListeners();
                reportPhishingButton.onClick.AddListener(ExecuteReportPhishingDefenseSequence);

                GazeInteractable gaze = reportPhishingButton.GetComponent<GazeInteractable>();
                if (gaze != null)
                {
                    gaze.interactableLabel = "Reportar Phishing a Seguridad TI (Modo Defensa)";
                    gaze.onClick.RemoveAllListeners();
                    gaze.onClick.AddListener(ExecuteReportPhishingDefenseSequence);
                }
            }
        }

        #region Registro de Señales de Phishing (Puntos 1, 2 y 3)

        /// <summary>
        /// Registra una señal identificada por el jugador mediante la retícula.
        /// </summary>
        /// <param name="signalId">'Signal_Remitente', 'Signal_Urgencia' o 'Signal_Enlace'.</param>
        public void MarkPhishingSignal(string signalId)
        {
            if (string.IsNullOrEmpty(signalId)) return;

            if (_discoveredSignals.Contains(signalId))
            {
                Debug.Log($"{LogPrefix} Señal '{signalId}' ya registrada previamente.");
                return;
            }

            _discoveredSignals.Add(signalId);
            Debug.Log($"{LogPrefix} <color=#FF9F43><b>¡Señal Detectada!</b></color> [{signalId}] - Progreso: {_discoveredSignals.Count}/3");

            // 1. Notificar a GameManager oficial
            if (GameManager.I != null)
            {
                GameManager.I.RegisterPhishingSignal(signalId);
            }

            // Notificar al flujo del escenario
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.NotifyPhishingSignalInspected(signalId);
            }

            PlaySound(signalFoundClip, GenerateSignalFoundChime());
            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayLaserScan(transform.position);
            }
            UpdateCounterUi();

            // 2. Si se han completado las 3 señales oficiales
            if (_discoveredSignals.Count >= 3 && !_allSignalsFound)
            {
                _allSignalsFound = true;
                HandleAllSignalsDiscovered();
            }
        }

        /// <summary>
        /// Sobrecarga compatible para PhishingSignalHotspot.
        /// </summary>
        public void OnSignalInspected(string signalId)
        {
            MarkPhishingSignal(signalId);
        }

        /// <summary>
        /// Ejecuta las acciones al completar las 3 señales:
        /// - Registra evidencia del correo: GameManager.I.RegisterEvidence("Evidencia_Correo").
        /// - Valida el Criterio de Aceptación 1 (<= 180s) y muestra el banner.
        /// - Habilita los botones de decisión según el modo.
        /// </summary>
        private void HandleAllSignalsDiscovered()
        {
            Debug.Log($"{LogPrefix} <color=#38F28A><b>¡Las 3 señales de phishing han sido recolectadas con éxito!</b></color>");

            // Registrar evidencia obligatoria
            if (GameManager.I != null)
            {
                GameManager.I.RegisterEvidence(EvidenceCorreoId);
            }

            // Obtener tiempo transcurrido
            SimulationClock clock = SimulationClock.I != null ? SimulationClock.I : FindFirstObjectByType<SimulationClock>();
            if (clock != null)
            {
                clock.RecordPhishingDetectionCompleted();
                _completionTime = clock.ElapsedTime;
            }
            else if (GameManager.I != null)
            {
                _completionTime = GameManager.I.ElapsedTime;
            }
            else
            {
                _completionTime = Time.timeSinceLevelLoad;
            }

            _criterion1Passed = (_completionTime <= TimeLimitSeconds);

            PlaySound(allSignalsCompletedClip, GenerateFanfareClip());

            // Mostrar banner de Criterio de Aceptación 1
            ShowCriterionAchievementBanner();

            // Habilitar botones de acción
            UpdateButtonVisibility();
        }

        private void ShowCriterionAchievementBanner()
        {
            if (criterionBannerPanel == null) return;

            criterionBannerPanel.SetActive(true);

            string formattedTime = FormatTime(_completionTime);

            if (_criterion1Passed)
            {
                if (criterionBannerTitle != null)
                {
                    criterionBannerTitle.text = "<color=#38F28A><b>[OK] CRITERIO DE ACEPTACION 1 CUMPLIDO [OK]</b></color>";
                }

                if (criterionBannerDetails != null)
                {
                    criterionBannerDetails.text =
                        $"Las 3 señales de phishing fueron identificadas en <b>{formattedTime}</b> " +
                        $"(Límite: <b>{FormatTime(TimeLimitSeconds)}</b>).\n" +
                        $"<color=#38F28A>[OK] Evidencia Forense Registrada:</color> <b>\"{EvidenceCorreoId}\"</b>\n" +
                        "Selecciona la acción a tomar en el correo para continuar.";
                }

                if (criterionBannerBackground != null)
                {
                    criterionBannerBackground.color = new Color(0.12f, 0.55f, 0.28f, 0.85f);
                }
            }
            else
            {
                if (criterionBannerTitle != null)
                {
                    criterionBannerTitle.text = "<color=#FF9F43><b>3 SEÑALES DETECTADAS (TIEMPO EXCEDIDO)</b></color>";
                }

                if (criterionBannerDetails != null)
                {
                    criterionBannerDetails.text =
                        $"Señales completadas en <b>{formattedTime}</b> " +
                        $"(Excedió el objetivo de {FormatTime(TimeLimitSeconds)}).\n" +
                        $"<color=#38F28A>[OK] Evidencia Forense Registrada:</color> <b>\"{EvidenceCorreoId}\"</b>\n" +
                        "Selecciona la acción a tomar en el correo para continuar.";
                }

                if (criterionBannerBackground != null)
                {
                    criterionBannerBackground.color = new Color(0.55f, 0.35f, 0.12f, 0.85f);
                }
            }
        }

        private void UpdateCounterUi()
        {
            int count = _discoveredSignals.Count;

            if (signalsCounterText != null)
            {
                signalsCounterText.text = $"Señales Detectadas: <b>{count}/3</b>";
                signalsCounterText.color = (count >= 3) ? GreenSuccessColor : (count > 0 ? AmberWarningColor : Color.white);
            }

            if (signalsProgressBar != null)
            {
                signalsProgressBar.fillAmount = Mathf.Clamp01((float)count / 3f);
                signalsProgressBar.color = (count >= 3) ? GreenSuccessColor : AmberWarningColor;
            }
        }

        #endregion

        #region Botones de Decisión y Modos (Ataque vs Defensa)

        /// <summary>
        /// Actualiza la visibilidad e interactividad de los botones según las señales descubiertas y el modo actual.
        /// </summary>
        private void UpdateButtonVisibility()
        {
            if (_decisionExecuted)
            {
                if (openLinkCvButton != null) openLinkCvButton.gameObject.SetActive(false);
                if (reportPhishingButton != null) reportPhishingButton.gameObject.SetActive(false);
                return;
            }

            bool readyForDecision = _allSignalsFound ||
                (ScenarioFlowController.I != null && ScenarioFlowController.I.CurrentStep >= ScenarioStep.Step5_ElegirAccion);

            SimMode mode = GameManager.I != null ? GameManager.I.mode : SimMode.Ataque;

            if (openLinkCvButton != null)
            {
                openLinkCvButton.gameObject.SetActive(readyForDecision);
                openLinkCvButton.interactable = readyForDecision;

                if (openLinkButtonText != null)
                {
                    openLinkButtonText.text = mode == SimMode.Ataque
                        ? "<b>Abrir enlace del CV</b>\n<size=80%>(Recrear Ataque 2016)</size>"
                        : "<b>Abrir enlace del CV</b>\n<size=80%>(Observar Consecuencia)</size>";
                }

                if (openLinkButtonGlow != null)
                {
                    openLinkButtonGlow.color = (mode == SimMode.Ataque)
                        ? new Color(1f, 0.35f, 0.31f, 0.4f)
                        : new Color(1f, 1f, 1f, 0.15f);
                }
            }

            if (reportPhishingButton != null)
            {
                reportPhishingButton.gameObject.SetActive(readyForDecision);
                reportPhishingButton.interactable = readyForDecision;

                if (reportButtonText != null)
                {
                    reportButtonText.text = mode == SimMode.Defensa
                        ? "<b>Reportar Phishing a Seguridad TI</b>\n<size=80%>(Modo Defensa Oficial)</size>"
                        : "<b>Reportar Phishing a Seguridad TI</b>\n<size=80%>(Activar Contramedidas)</size>";
                }

                if (reportButtonGlow != null)
                {
                    reportButtonGlow.color = (mode == SimMode.Defensa)
                        ? new Color(0.22f, 0.95f, 0.54f, 0.4f)
                        : new Color(1f, 1f, 1f, 0.15f);
                }
            }
        }

        private void HandleModeChanged(SimMode newMode)
        {
            UpdateButtonVisibility();
        }

        // Al reiniciar la simulación, las señales deben volver a 0/3.
        private void HandleSimulationReset()
        {
            if (_infectionSequenceCoroutine != null)
            {
                StopCoroutine(_infectionSequenceCoroutine);
                _infectionSequenceCoroutine = null;
            }

            _discoveredSignals.Clear();
            _allSignalsFound = false;
            _criterion1Passed = false;
            _completionTime = 0f;
            _decisionExecuted = false;

            foreach (PhishingSignalHotspot hotspot in FindObjectsByType<PhishingSignalHotspot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                hotspot.ResetSignal();
            }

            if (criterionBannerPanel != null) criterionBannerPanel.SetActive(false);
            if (infectionAlertBanner != null) infectionAlertBanner.SetActive(false);
            if (quarantineNoticeBanner != null) quarantineNoticeBanner.SetActive(false);

            UpdateCounterUi();
            UpdateButtonVisibility();
        }

        private void HandleStepChanged(ScenarioStep newStep, StepGuideInfo info)
        {
            UpdateButtonVisibility();
        }

        #endregion

        #region Secuencia de Ataque (Punto 3: Abrir Enlace)

        /// <summary>
        /// Acción 1: Recrea el incidente de 2016 detonando el dropper e infectando las 3 computadoras.
        /// </summary>
        public void ExecuteOpenLinkAttackSequence()
        {
            if (_decisionExecuted) return;
            _decisionExecuted = true;

            Debug.LogWarning($"{LogPrefix} <b>¡ENLACE DE CV EJECUTADO!</b> Iniciando recreación del ataque de 2016.");

            // Desactivar botones de interacción
            if (openLinkCvButton != null) openLinkCvButton.interactable = false;
            if (reportPhishingButton != null) reportPhishingButton.interactable = false;

            if (infectionAlertBanner != null)
            {
                infectionAlertBanner.SetActive(true);
            }

            if (infectionAlertText != null)
            {
                infectionAlertText.text =
                    "<color=#FF5A4E><b>[ALERTA DE INFECCION EN RED]:</b></color>\n" +
                    "Dropper 'Rasel_Ahlam_CV_2015.scr.zip' ejecutado. Malware Lazarus activo.";
            }

            // Notificar al flujo del escenario
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.NotifyPlayerDecisionExecuted("AbrirEnlace");
            }

            // Lanzar secuencia cinemática oficial de propagación de infección
            if (InfectionSpreadVisualizer.I != null)
            {
                InfectionSpreadVisualizer.I.PlayInfectionSequence();
            }
            else
            {
                InfectionSpreadVisualizer visualizer = FindFirstObjectByType<InfectionSpreadVisualizer>();
                if (visualizer != null)
                {
                    visualizer.PlayInfectionSequence();
                }
                else
                {
                    if (_infectionSequenceCoroutine != null)
                    {
                        StopCoroutine(_infectionSequenceCoroutine);
                    }
                    _infectionSequenceCoroutine = StartCoroutine(InfectionSpreadCoroutine());
                }
            }
        }

        /// <summary>
        /// Corrutina que simula la propagación lateral del malware a través de las 3 estaciones de trabajo.
        /// </summary>
        private IEnumerator InfectionSpreadCoroutine()
        {
            PlaySound(infectionSpreadClip, GenerateInfectionAlertTone());

            // 1. Infección del puesto 1: Carla Mendoza (PC-PAGOS-01)
            Debug.Log($"{LogPrefix} <color=#FF5A4E>[09:18:44]</color> HTTP GET: PC-PAGOS-01 (C. Mendoza) abre 'Rasel_Ahlam_CV_2015.scr.zip'");
            if (monitor01Controller != null)
            {
                monitor01Controller.SetState(InteractiveMonitorController.MonitorState.InfeccionActiva);
            }

            yield return new WaitForSeconds(1.3f);

            // 2. Movimiento lateral hacia puesto 2: Analista de Pagos (PC-PAGOS-02)
            Debug.Log($"{LogPrefix} <color=#FF5A4E>[09:21:10]</color> HTTP GET: PC-PAGOS-02 intenta abrir 'Rasel_Ahlam_CV_2015.scr.zip'");
            PlaySound(infectionSpreadClip, GenerateInfectionAlertTone());
            if (monitor02Controller != null)
            {
                monitor02Controller.SetState(InteractiveMonitorController.MonitorState.InfeccionActiva);
            }

            yield return new WaitForSeconds(1.3f);

            // 3. Propagación hacia puesto 3: Terminal de Registros / SIEM (PC-PAGOS-09)
            Debug.Log($"{LogPrefix} <color=#FF5A4E>[09:25:03]</color> HTTP GET: PC-PAGOS-09 intenta abrir 'Rasel_Ahlam_CV_2015.scr.zip'");
            Debug.LogWarning($"{LogPrefix} <color=#FF5A4E>[09:25:09] ALERTA SILENCIADA:</color> Proceso backdoor iniciado en red plana hacia nodo SWIFT");
            PlaySound(infectionSpreadClip, GenerateInfectionAlertTone());
            if (monitor03Controller != null)
            {
                monitor03Controller.SetState(InteractiveMonitorController.MonitorState.InfeccionActiva);
            }

            Debug.Log($"{LogPrefix} <color=#FF5A4E><b>Secuencia de infección de 3 puestos completada.</b></color>");
        }

        #endregion

        #region Secuencia de Defensa (Punto 3: Reportar Phishing)

        /// <summary>
        /// Acción 2: Activa el protocolo de defensa corporativa:
        /// - Aplica 'Defensa_BotonReportar' en GameManager.
        /// - Coordina con MailQuarantine el aislamiento del correo y purga de 16 buzones.
        /// - Protege las 3 computadoras de la red.
        /// </summary>
        public void ExecuteReportPhishingDefenseSequence()
        {
            if (_decisionExecuted) return;
            _decisionExecuted = true;

            Debug.Log($"{LogPrefix} <color=#38F28A><b>¡CORREO REPORTADO A SEGURIDAD TI!</b></color> Activando defensa.");

            // Desactivar botones de interacción
            if (openLinkCvButton != null) openLinkCvButton.interactable = false;
            if (reportPhishingButton != null) reportPhishingButton.interactable = false;

            // 1. Aplicar contramedida oficial en GameManager
            if (GameManager.I != null)
            {
                GameManager.I.ApplyDefense(DefenseReportarId);
            }

            // 2. Coordinar aislamiento perimetral con MailQuarantine
            if (mailQuarantine != null)
            {
                mailQuarantine.QuarantineEmail();
            }

            // 3. Proteger los 3 monitores interactivos
            if (monitor01Controller != null)
            {
                monitor01Controller.SetState(InteractiveMonitorController.MonitorState.ProtegidoCuarentena);
            }
            if (monitor02Controller != null)
            {
                monitor02Controller.SetState(InteractiveMonitorController.MonitorState.ProtegidoCuarentena);
            }
            if (monitor03Controller != null)
            {
                monitor03Controller.SetState(InteractiveMonitorController.MonitorState.ProtegidoCuarentena);
            }

            // 4. Mostrar banner de contención
            if (quarantineNoticeBanner != null)
            {
                quarantineNoticeBanner.SetActive(true);
            }

            if (quarantineNoticeText != null)
            {
                quarantineNoticeText.text =
                    "<color=#38F28A><b>[OK] PROTOCOLO DE DEFENSA EXITOSO:</b></color>\n" +
                    "• Phishing reportado al SOC en tiempo real.\n" +
                    "• Remitente bloqueado y buzones de los 16 empleados purgados.\n" +
                    "• 0 computadoras infectadas en la red de pagos.";
            }

            // 5. Notificar al flujo del escenario
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.NotifyPlayerDecisionExecuted("ReportarPhishing");
            }

            PlaySound(reportDefenseClip, GenerateDefenseConfirmationChime());
        }

        #endregion

        #region Utilidades y Síntesis de Audio

        private static string FormatTime(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{mins:00}:{secs:00}";
        }

        private void PlaySound(AudioClip clip, AudioClip fallbackProcedural)
        {
            AudioClip target = clip != null ? clip : fallbackProcedural;
            if (target != null && audioSource != null)
            {
                audioSource.PlayOneShot(target);
            }
            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayKeyboardTyping(transform.position);
            }
        }

        private AudioClip GenerateSignalFoundChime()
        {
            int sampleRate = 44100;
            float duration = 0.3f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Clamp01(1f - (t / duration));
                samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.25f * env; // A5
            }

            AudioClip clip = AudioClip.Create("Procedural_SignalFound", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateFanfareClip()
        {
            int sampleRate = 44100;
            float duration = 0.65f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = t < 0.15f ? 523.25f : (t < 0.30f ? 659.25f : (t < 0.45f ? 783.99f : 1046.50f));
                float env = Mathf.Clamp01(1f - ((t % 0.15f) / 0.15f));
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.3f * env;
            }

            AudioClip clip = AudioClip.Create("Procedural_AllSignalsFanfare", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateInfectionAlertTone()
        {
            int sampleRate = 44100;
            float duration = 0.45f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Clamp01(1f - (t / duration));
                // Tono bajo disonante con modulación
                float wave = Mathf.Sin(2f * Mathf.PI * 180f * t) + Mathf.Sin(2f * Mathf.PI * 195f * t);
                samples[i] = Mathf.Clamp(wave * 0.35f * env, -0.6f, 0.6f);
            }

            AudioClip clip = AudioClip.Create("Procedural_InfectionAlert", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateDefenseConfirmationChime()
        {
            int sampleRate = 44100;
            float duration = 0.5f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Clamp01(1f - (t / duration));
                float wave = (Mathf.Sin(2f * Mathf.PI * 659.25f * t) + Mathf.Sin(2f * Mathf.PI * 987.77f * t)) * 0.2f;
                samples[i] = wave * env;
            }

            AudioClip clip = AudioClip.Create("Procedural_DefenseConfirm", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
