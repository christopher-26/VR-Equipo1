using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Data;

namespace AndesSim.UI
{
    /// <summary>
    /// HUD en World Space diseñado para visualización ergonómica en Google Cardboard VR (RF-06).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Se orienta suavemente hacia la vista del jugador o se ancla ergonómicamente frente a la cámara.
    /// - Muestra en tiempo real:
    ///   1. Instrucción pedagógica del paso actual y progreso (1/6 a 6/6).
    ///   2. Reloj de conteo regresivo de 180 segundos (< 3 minutos) y fecha histórica (29 ENE 2015 - 09:14 AM).
    ///   3. Checklist de evidencias recolectadas (Correo, Post-It, Log de accesos).
    ///   4. Modo de simulación activo (Ataque vs Defensa).
    /// </summary>
    public class WorldSpaceHudManager : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - HUD]";

        [Header("--- Anclaje y Seguimiento VR Ergonométrico ---")]
        [Tooltip("Cámara del jugador. Si se deja nula, se buscará Camera.main.")]
        [SerializeField] private Transform targetCamera;
        [Tooltip("Distancia en metros frente al jugador.")]
        [SerializeField] private float distanceInFront = 2.2f;
        [Tooltip("Desplazamiento vertical para no obstruir el centro de la retícula.")]
        [SerializeField] private float heightOffset = -0.35f;
        [Tooltip("Velocidad de seguimiento suave para amortiguar movimientos bruscos.")]
        [SerializeField] private float followSmoothSpeed = 5.0f;

        [Header("--- Elementos de UI: Guía y Progreso (1/6 a 6/6) ---")]
        [SerializeField] private TextMeshProUGUI stepTitleText;
        [SerializeField] private TextMeshProUGUI hudInstructionText;
        [SerializeField] private TextMeshProUGUI progressBadgeText;
        [SerializeField] private Image progressBarFill;

        [Header("--- Elementos de UI: Relojes y Temporizador ---")]
        [SerializeField] private TextMeshProUGUI countdownTimerText;
        [SerializeField] private TextMeshProUGUI ambientClockText;
        [SerializeField] private Image countdownWarningGlow;

        [Header("--- Elementos de UI: Checklist de Evidencias (0/3 a 3/3) ---")]
        [SerializeField] private TextMeshProUGUI evidenceCounterText;
        [SerializeField] private Image emailEvidenceIcon;
        [SerializeField] private Image postItEvidenceIcon;
        [SerializeField] private Image logEvidenceIcon;

        [Header("--- Elementos de UI: Modo de Simulación ---")]
        [SerializeField] private TextMeshProUGUI modeBadgeText;
        [SerializeField] private Image modeBadgeBackground;

        // Estado interno
        private readonly HashSet<string> _collectedEvidences = new HashSet<string>();

        private void Start()
        {
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }

            // Suscribirse a eventos de ScenarioFlowController
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnHudInstructionUpdated += HandleHudInstructionUpdated;
                ScenarioFlowController.I.OnStepChanged += HandleStepChanged;

                // Cargar estado inicial
                StepGuideInfo currentInfo = ScenarioFlowController.I.GetStepInfo(ScenarioFlowController.I.CurrentStep);
                HandleHudInstructionUpdated(currentInfo.hudGuidanceInstruction, currentInfo.progressLabel, currentInfo.stepTitle);
            }

            // Suscribirse a eventos de SimulationClock
            SimulationClock clock = FindFirstObjectByType<SimulationClock>();
            if (clock != null)
            {
                clock.OnTimerTick += HandleTimerTick;
                clock.OnAmbientTimeTick += HandleAmbientTimeTick;
                clock.OnTimerCriticalWarning += HandleTimerCriticalWarning;
            }

            // Suscribirse a GameManager
            if (GameManager.I != null)
            {
                GameManager.I.OnEvidenceRegistered += HandleEvidenceRegistered;
                GameManager.I.OnModeChanged += HandleModeChanged;
                UpdateModeBadge(GameManager.I.mode);
            }

            UpdateEvidenceChecklist();
        }

        private void OnDestroy()
        {
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnHudInstructionUpdated -= HandleHudInstructionUpdated;
                ScenarioFlowController.I.OnStepChanged -= HandleStepChanged;
            }

            SimulationClock clock = FindFirstObjectByType<SimulationClock>();
            if (clock != null)
            {
                clock.OnTimerTick -= HandleTimerTick;
                clock.OnAmbientTimeTick -= HandleAmbientTimeTick;
                clock.OnTimerCriticalWarning -= HandleTimerCriticalWarning;
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnEvidenceRegistered -= HandleEvidenceRegistered;
                GameManager.I.OnModeChanged -= HandleModeChanged;
            }
        }

        private void LateUpdate()
        {
            SmoothFollowCamera();
        }

        /// <summary>
        /// Mantiene el HUD flotando suavemente en la vista del jugador sin vibraciones mareantes.
        /// </summary>
        private void SmoothFollowCamera()
        {
            if (targetCamera == null)
            {
                if (Camera.main != null) targetCamera = Camera.main.transform;
                else return;
            }

            Vector3 forwardFlat = targetCamera.forward;
            forwardFlat.y = 0f;
            if (forwardFlat.sqrMagnitude < 0.001f) forwardFlat = Vector3.forward;
            forwardFlat.Normalize();

            Vector3 targetPosition = targetCamera.position + (forwardFlat * distanceInFront) + (Vector3.up * heightOffset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSmoothSpeed);

            Quaternion targetRotation = Quaternion.LookRotation(transform.position - targetCamera.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSmoothSpeed);
        }

        private void HandleHudInstructionUpdated(string instruction, string progress, string title)
        {
            if (hudInstructionText != null) hudInstructionText.text = instruction;
            if (progressBadgeText != null) progressBadgeText.text = $"PASO {progress}";
            if (stepTitleText != null) stepTitleText.text = title;

            if (progressBarFill != null)
            {
                int current = 1;
                if (progress.Contains("/"))
                {
                    int.TryParse(progress.Split('/')[0], out current);
                }
                progressBarFill.fillAmount = Mathf.Clamp01((float)current / 6f);
            }
        }

        private void HandleStepChanged(ScenarioStep step, StepGuideInfo info)
        {
            HandleHudInstructionUpdated(info.hudGuidanceInstruction, info.progressLabel, info.stepTitle);
        }

        private void HandleTimerTick(float remainingSeconds, string formattedTime)
        {
            if (countdownTimerText != null)
            {
                countdownTimerText.text = $"TIEMPO RESTANTE: <b>{formattedTime}</b>";

                if (remainingSeconds <= 30f)
                {
                    countdownTimerText.color = Color.red;
                }
                else if (remainingSeconds <= 60f)
                {
                    countdownTimerText.color = new Color(1f, 0.5f, 0f); // Naranja
                }
                else
                {
                    countdownTimerText.color = Color.white;
                }
            }
        }

        private void HandleAmbientTimeTick(string ambientDateString)
        {
            if (ambientClockText != null)
            {
                ambientClockText.text = ambientDateString;
            }
        }

        private void HandleTimerCriticalWarning(float remainingSeconds)
        {
            if (countdownWarningGlow != null)
            {
                StartCoroutine(FlashWarningGlow());
            }
        }

        private IEnumerator FlashWarningGlow()
        {
            if (countdownWarningGlow == null) yield break;

            countdownWarningGlow.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.4f);
            countdownWarningGlow.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.4f);
            countdownWarningGlow.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.4f);
            countdownWarningGlow.gameObject.SetActive(false);
        }

        private void HandleEvidenceRegistered(string evidenceId)
        {
            _collectedEvidences.Add(evidenceId);
            UpdateEvidenceChecklist();
        }

        private void UpdateEvidenceChecklist()
        {
            bool hasEmail = _collectedEvidences.Contains("Evidencia_Correo");
            bool hasPostIt = _collectedEvidences.Contains("Evidencia_PostIt");
            bool hasLog = _collectedEvidences.Contains("Evidencia_LogAccesos");

            if (evidenceCounterText != null)
            {
                evidenceCounterText.text = $"Evidencias: <b>{_collectedEvidences.Count} / 3</b>";
            }

            if (emailEvidenceIcon != null) emailEvidenceIcon.color = hasEmail ? Color.green : new Color(1f, 1f, 1f, 0.25f);
            if (postItEvidenceIcon != null) postItEvidenceIcon.color = hasPostIt ? Color.green : new Color(1f, 1f, 1f, 0.25f);
            if (logEvidenceIcon != null) logEvidenceIcon.color = hasLog ? Color.green : new Color(1f, 1f, 1f, 0.25f);
        }

        private void HandleModeChanged(SimMode mode)
        {
            UpdateModeBadge(mode);
        }

        private void UpdateModeBadge(SimMode mode)
        {
            if (modeBadgeText != null)
            {
                modeBadgeText.text = (mode == SimMode.Ataque) ? "MODO ATAQUE" : "MODO DEFENSA";
            }

            if (modeBadgeBackground != null)
            {
                modeBadgeBackground.color = (mode == SimMode.Ataque)
                    ? new Color(0.85f, 0.2f, 0.2f, 0.85f)
                    : new Color(0.15f, 0.75f, 0.35f, 0.85f);
            }
        }
    }
}
