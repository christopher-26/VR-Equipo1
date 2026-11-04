using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Data;
using AndesSim.VR;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Inspector de correo de phishing para el Monitor_01 de Carla Mendoza (RF-03, RF-04, RF-05).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Maneja las 3 zonas interactivas de inspección por mirada:
    ///   1. Signal_Remitente ("rasel.ahlam@career-applicants-bd.com")
    ///   2. Signal_Enlace ("http://bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip")
    ///   3. Signal_Urgencia ("Cierre inminente hoy / Descargar urgentemente")
    /// - Resalta y marca con check verde cada señal detectada.
    /// - Al completar las 3 señales en < 3 minutos (criterio de aceptación), activa las opciones del Paso 5.
    /// - En Paso 5 gestiona los botones: "Abrir enlace de CV" (Modo Ataque) y "Reportar Phishing" (Modo Defensa).
    /// </summary>
    public class PhishingEmailInspector : MonoBehaviour
    {
        public static PhishingEmailInspector I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - Email Inspector]";

        private void Awake()
        {
            if (I == null) I = this;
        }

        [Header("--- Zonas de Señales de Phishing (RF-03) ---")]
        [Tooltip("Interactable de la zona del remitente externo no confiable.")]
        [SerializeField] private GazeInteractable senderInteractable;
        [SerializeField] private Image senderHighlightImage;
        [SerializeField] private GameObject senderCheckmarkBadge;

        [Tooltip("Interactable de la zona del enlace malicioso con doble extensión .scr.zip.")]
        [SerializeField] private GazeInteractable linkInteractable;
        [SerializeField] private Image linkHighlightImage;
        [SerializeField] private GameObject linkCheckmarkBadge;

        [Tooltip("Interactable de la zona del texto con ingeniería social y urgencia temporal.")]
        [SerializeField] private GazeInteractable urgencyInteractable;
        [SerializeField] private Image urgencyHighlightImage;
        [SerializeField] private GameObject urgencyCheckmarkBadge;

        [Header("--- Botones de Decisión (Paso 5) ---")]
        [Tooltip("Botón para abrir el enlace malicioso y observar la infección C2 (Modo Ataque).")]
        [SerializeField] private Button openLinkButton;

        [Tooltip("Botón para reportar el phishing al SOC y aislar el buzón (Modo Defensa).")]
        [SerializeField] private Button reportPhishingButton;

        [Header("--- Paneles de Feedback y Cuarentena ---")]
        [SerializeField] private GameObject signalsStatusPanel;
        [SerializeField] private TextMeshProUGUI signalsCountText;
        [SerializeField] private GameObject infectionAlertBanner;
        [SerializeField] private GameObject quarantinedNoticeBanner;

        [Header("--- Efectos Sonoros ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip signalFoundClip;
        [SerializeField] private AudioClip allSignalsCompletedClip;
        [SerializeField] private AudioClip infectionAlertClip;
        [SerializeField] private AudioClip reportSuccessClip;

        // Estado interno
        private readonly HashSet<string> _discoveredSignals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _decisionMade = false;

        private void Start()
        {
            SetupInteractables();
            SetupDecisionButtons();
            UpdateVisualIndicators();

            // Suscribirse a cambios de paso
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged += HandleStepChanged;
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
            }

            // Ocultar banners de resultado iniciales
            if (infectionAlertBanner != null) infectionAlertBanner.SetActive(false);
            if (quarantinedNoticeBanner != null) quarantinedNoticeBanner.SetActive(false);
        }

        private void OnDestroy()
        {
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged -= HandleStepChanged;
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
            }
        }

        private void SetupInteractables()
        {
            if (senderInteractable != null)
            {
                senderInteractable.interactableLabel = "Inspeccionar Remitente: @career-applicants-bd.com";
                senderInteractable.onClick.AddListener(() => OnSignalInspected("Signal_Remitente"));
            }

            if (linkInteractable != null)
            {
                linkInteractable.interactableLabel = "Inspeccionar Enlace: Dropper .scr.zip";
                linkInteractable.onClick.AddListener(() => OnSignalInspected("Signal_Enlace"));
            }

            if (urgencyInteractable != null)
            {
                urgencyInteractable.interactableLabel = "Inspeccionar Urgencia: Presión psicológica";
                urgencyInteractable.onClick.AddListener(() => OnSignalInspected("Signal_Urgencia"));
            }
        }

        private void SetupDecisionButtons()
        {
            if (openLinkButton != null)
            {
                openLinkButton.onClick.AddListener(OnOpenLinkSelected);
            }

            if (reportPhishingButton != null)
            {
                reportPhishingButton.onClick.AddListener(OnReportPhishingSelected);
            }

            UpdateButtonVisibility();
        }

        /// <summary>
        /// Procesa la inspección de una de las 3 señales por retícula o dwell.
        /// </summary>
        public void OnSignalInspected(string signalId)
        {
            if (_discoveredSignals.Contains(signalId))
            {
                Debug.Log($"{LogPrefix} Señal {signalId} ya había sido registrada previamente.");
                return;
            }

            _discoveredSignals.Add(signalId);
            PlaySound(signalFoundClip);

            Debug.Log($"{LogPrefix} <color=#4EC9B0>¡Señal descubierta!</color> <b>{signalId}</b> [{_discoveredSignals.Count}/3]");

            UpdateVisualIndicators();

            // Notificar al controlador de flujo del escenario
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.NotifyPhishingSignalInspected(signalId);
            }

            // Si se encontraron las 3 señales
            if (_discoveredSignals.Count >= 3)
            {
                PlaySound(allSignalsCompletedClip);

                // Registrar evidencia del correo en GameManager
                if (GameManager.I != null)
                {
                    GameManager.I.RegisterEvidence("Evidencia_Correo");
                }

                UpdateButtonVisibility();
            }
        }

        private void UpdateVisualIndicators()
        {
            bool senderFound = _discoveredSignals.Contains("Signal_Remitente");
            if (senderHighlightImage != null)
            {
                senderHighlightImage.color = senderFound ? new Color(0.2f, 1f, 0.4f, 0.35f) : new Color(1f, 1f, 1f, 0.05f);
            }
            if (senderCheckmarkBadge != null) senderCheckmarkBadge.SetActive(senderFound);

            bool linkFound = _discoveredSignals.Contains("Signal_Enlace");
            if (linkHighlightImage != null)
            {
                linkHighlightImage.color = linkFound ? new Color(0.2f, 1f, 0.4f, 0.35f) : new Color(1f, 1f, 1f, 0.05f);
            }
            if (linkCheckmarkBadge != null) linkCheckmarkBadge.SetActive(linkFound);

            bool urgencyFound = _discoveredSignals.Contains("Signal_Urgencia");
            if (urgencyHighlightImage != null)
            {
                urgencyHighlightImage.color = urgencyFound ? new Color(0.2f, 1f, 0.4f, 0.35f) : new Color(1f, 1f, 1f, 0.05f);
            }
            if (urgencyCheckmarkBadge != null) urgencyCheckmarkBadge.SetActive(urgencyFound);

            if (signalsCountText != null)
            {
                signalsCountText.text = $"Señales de Phishing: <b>{_discoveredSignals.Count}/3</b>";
                signalsCountText.color = (_discoveredSignals.Count >= 3) ? Color.green : Color.yellow;
            }
        }

        private void UpdateButtonVisibility()
        {
            SimMode currentMode = GameManager.I != null ? GameManager.I.mode : SimMode.Ataque;
            bool readyForDecision = _discoveredSignals.Count >= 3 || (ScenarioFlowController.I != null && ScenarioFlowController.I.CurrentStep >= ScenarioStep.Step5_ElegirAccion);

            if (openLinkButton != null)
            {
                // En modo ataque se resalta abrir el enlace; en modo defensa también puede estar como opción para aprender la consecuencia
                openLinkButton.gameObject.SetActive(readyForDecision && !_decisionMade);
            }

            if (reportPhishingButton != null)
            {
                // Botón de defensa corporativa
                reportPhishingButton.gameObject.SetActive(readyForDecision && !_decisionMade);
            }
        }

        private void HandleStepChanged(ScenarioStep step, StepGuideInfo info)
        {
            UpdateButtonVisibility();
        }

        private void HandleModeChanged(SimMode newMode)
        {
            UpdateButtonVisibility();
        }

        /// <summary>
        /// Jugador elige: Abrir enlace malicioso (Modo Ataque).
        /// </summary>
        private void OnOpenLinkSelected()
        {
            if (_decisionMade) return;
            _decisionMade = true;

            PlaySound(infectionAlertClip);

            Debug.LogWarning($"{LogPrefix} <b>¡Enlace malicioso ejecutado!</b> Dropper 'Rasel_Ahlam_CV_2015.scr.zip' infectando equipo local.");

            if (openLinkButton != null) openLinkButton.gameObject.SetActive(false);
            if (reportPhishingButton != null) reportPhishingButton.gameObject.SetActive(false);

            if (infectionAlertBanner != null)
            {
                infectionAlertBanner.SetActive(true);
            }

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.NotifyPlayerDecisionExecuted("AbrirEnlace");
            }
        }

        /// <summary>
        /// Jugador elige: Reportar phishing y activar defensas (Modo Defensa).
        /// </summary>
        private void OnReportPhishingSelected()
        {
            if (_decisionMade) return;
            _decisionMade = true;

            PlaySound(reportSuccessClip);

            Debug.Log($"{LogPrefix} <color=#4EC9B0><b>¡Phishing reportado al SOC!</b></color> Contramedidas activadas.");

            if (openLinkButton != null) openLinkButton.gameObject.SetActive(false);
            if (reportPhishingButton != null) reportPhishingButton.gameObject.SetActive(false);

            if (quarantinedNoticeBanner != null)
            {
                quarantinedNoticeBanner.SetActive(true);
            }

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.NotifyPlayerDecisionExecuted("ReportarPhishing");
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
