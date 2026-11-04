using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Data;
using AndesSim.VR;

namespace AndesSim.UI
{
    /// <summary>
    /// Pantalla final de evaluación técnica y balance comparativo del incidente (Paso 6 del flujo).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Evalúa estrictamente el Criterio de Aceptación: "El jugador encuentra las 3 señales de phishing en menos de 3 minutos (180s)".
    /// - Muestra el balance comparativo de los dos desenlaces:
    ///   • Modo Ataque: 3 equipos infectados, backdoor svchost_upd.exe hacia C2 185.130.44.82, credenciales SWIFT comprometidas.
    ///   • Modo Defensa: 0 infectados, correo en cuarentena, sandbox perimetral y MFA activo.
    /// - Permite alternar de modo directamente para experimentar la contraparte sin reiniciar la app.
    /// </summary>
    public class FinalEvaluationScreen : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - Final Screen]";

        [Header("--- Contenedor de la Pantalla Final ---")]
        [Tooltip("Panel principal que se activa al terminar el escenario.")]
        [SerializeField] private GameObject screenRootPanel;

        [Header("--- Criterio de Aceptación (< 3 minutos) ---")]
        [SerializeField] private TextMeshProUGUI acceptanceCriterionBadgeText;
        [SerializeField] private Image acceptanceCriterionBadgeBackground;
        [SerializeField] private TextMeshProUGUI timeDetailText;

        [Header("--- Evidencias Recolectadas ---")]
        [SerializeField] private TextMeshProUGUI evidencesSummaryText;

        [Header("--- Desenlace Técnico según Modo ---")]
        [SerializeField] private TextMeshProUGUI outcomeTitleText;
        [SerializeField] private TextMeshProUGUI outcomeTechnicalSummaryText;
        [SerializeField] private Image outcomeBannerBackground;

        [Header("--- Hipótesis de Investigación ---")]
        [SerializeField] private TextMeshProUGUI suspectHypothesisText;

        [Header("--- Botones Interactivos de Cierre ---")]
        [Tooltip("Permite cambiar al modo opuesto para cumplir el requisito de experimentar ambos modos.")]
        [SerializeField] private Button switchModeButton;
        [SerializeField] private TextMeshProUGUI switchModeButtonText;

        [Tooltip("Reinicia el escenario desde el portal.")]
        [SerializeField] private Button restartScenarioButton;

        [Header("--- Audio y Celebración ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successEndingClip;
        [SerializeField] private AudioClip failEndingClip;

        private void Start()
        {
            if (screenRootPanel != null)
            {
                screenRootPanel.SetActive(false);
            }

            SetupButtons();

            if (GameManager.I != null)
            {
                GameManager.I.OnScenarioEnded += HandleScenarioEnded;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnScenarioEnded -= HandleScenarioEnded;
            }
        }

        private void SetupButtons()
        {
            if (switchModeButton != null)
            {
                switchModeButton.onClick.AddListener(OnSwitchModeClicked);
            }

            if (restartScenarioButton != null)
            {
                restartScenarioButton.onClick.AddListener(OnRestartScenarioClicked);
            }
        }

        private void HandleScenarioEnded(string summary, bool timeCriterionMet)
        {
            ShowEvaluationScreen();
        }

        /// <summary>
        /// Muestra y calcula toda la evaluación técnica del escenario finalizado.
        /// </summary>
        public void ShowEvaluationScreen()
        {
            if (screenRootPanel != null)
            {
                screenRootPanel.SetActive(true);
            }

            SimMode mode = (GameManager.I != null) ? GameManager.I.mode : SimMode.Ataque;

            // 1. Criterio de Aceptación: < 180 segundos
            float recordTime = (ProgressTracker.I != null) ? ProgressTracker.I.BestPhishingDetectionTime : -1f;
            SimulationClock clock = FindFirstObjectByType<SimulationClock>();

            float elapsedTime = (clock != null && clock.PhishingCompletionRecordedSeconds > 0f)
                ? clock.PhishingCompletionRecordedSeconds
                : (recordTime > 0f ? recordTime : (clock != null ? clock.ElapsedTime : 180f));

            bool criterionMet = elapsedTime <= 180f && elapsedTime > 0f;

            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            string timeStr = $"{minutes:00}:{seconds:00}";

            if (acceptanceCriterionBadgeText != null)
            {
                acceptanceCriterionBadgeText.text = criterionMet
                    ? $"[OK] CRITERIO DE ACEPTACION: SUPERADO EN {timeStr} (< 3 MIN) [OK]"
                    : $"TIEMPO EXCEDIDO: {timeStr} (> 3 MINUTOS REQUERIDOS)";
            }

            if (acceptanceCriterionBadgeBackground != null)
            {
                acceptanceCriterionBadgeBackground.color = criterionMet ? new Color(0.15f, 0.7f, 0.25f, 0.9f) : new Color(0.8f, 0.2f, 0.15f, 0.9f);
            }

            if (timeDetailText != null)
            {
                timeDetailText.text = $"Tiempo exacto de detección: <b>{timeStr}</b> / Límite: <b>03:00</b>";
            }

            // 2. Resumen de Evidencias
            int evidenceCount = (GameManager.I != null && GameManager.I.DiscoveredEvidences != null) ? GameManager.I.DiscoveredEvidences.Count : 0;
            if (evidencesSummaryText != null)
            {
                evidencesSummaryText.text = $"Evidencias identificadas: <b>{evidenceCount} / 3</b>\n" +
                                            $"• Correo con enlace .scr.zip: {(HasEvidence("Evidencia_Correo") ? "<color=#38F28A>OK</color>" : "<color=#888>No</color>")}\n" +
                                            $"• Credenciales en Post-It: {(HasEvidence("Evidencia_PostIt") ? "<color=#38F28A>OK</color>" : "<color=#888>No</color>")}\n" +
                                            $"• Registros de Eventos SIEM: {(HasEvidence("Evidencia_LogAccesos") ? "<color=#38F28A>OK</color>" : "<color=#888>No</color>")}";
            }

            // 3. Desenlace Técnico por Modo
            if (outcomeTitleText != null)
            {
                outcomeTitleText.text = (mode == SimMode.Ataque)
                    ? "DESENLACE: MODO ATAQUE (HISTÓRICO 2015)"
                    : "DESENLACE: MODO DEFENSA (CONTENCIÓN EXITOSA)";
            }

            if (outcomeTechnicalSummaryText != null)
            {
                if (mode == SimMode.Ataque)
                {
                    outcomeTechnicalSummaryText.text =
                        "• <b>3 equipos infectados</b> en la red de pagos (PC-PAGOS-01, PC-PAGOS-02, PC-PAGOS-09).\n" +
                        "• <b>Puerta trasera activa:</b> Dropper ejecutó `svchost_upd.exe` con conexión C2 hacia `185.130.44.82:443`.\n" +
                        "• <b>Fuga de credenciales:</b> Contraseña en texto claro de Carla Mendoza expuesta físicamente.";
                }
                else
                {
                    outcomeTechnicalSummaryText.text =
                        "• <b>0 equipos infectados:</b> Correo neutralizado antes de que los operadores detonaran el adjunto.\n" +
                        "• <b>Protección perimetral:</b> Sandbox detectó el dropper .scr.zip y bloqueó la IP hostil en firewall.\n" +
                        "• <b>MFA activo:</b> Autenticación multifactor impidió el uso de credenciales robadas.";
                }
            }

            if (outcomeBannerBackground != null)
            {
                outcomeBannerBackground.color = (mode == SimMode.Ataque)
                    ? new Color(0.85f, 0.15f, 0.15f, 0.85f)
                    : new Color(0.12f, 0.65f, 0.28f, 0.85f);
            }

            // 4. Hipótesis de sospechoso
            string suspectId = (ProgressTracker.I != null) ? ProgressTracker.I.SelectedSuspectHypothesis : string.Empty;
            if (suspectHypothesisText != null)
            {
                suspectHypothesisText.text = string.IsNullOrEmpty(suspectId)
                    ? "Hipótesis de cómplice interno: <i>No seleccionada</i>"
                    : $"Hipótesis del equipo: <b><color=#9CDCFE>{suspectId}</color></b>";
            }

            // 5. Botón de cambio de modo
            if (switchModeButtonText != null)
            {
                switchModeButtonText.text = (mode == SimMode.Ataque)
                    ? "PROBAR MODO DEFENSA (APLICAR CONTRAMEDIDAS)"
                    : "PROBAR MODO ATAQUE (EXPERIMENTAR VULNERABILIDAD)";
            }

            // Reproducir sonido de fin
            if (audioSource != null)
            {
                audioSource.PlayOneShot(criterionMet ? successEndingClip : failEndingClip);
            }
        }

        private bool HasEvidence(string evidenceId)
        {
            if (GameManager.I == null || GameManager.I.DiscoveredEvidences == null) return false;
            foreach (string e in GameManager.I.DiscoveredEvidences)
            {
                if (string.Equals(e, evidenceId, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private void OnSwitchModeClicked()
        {
            if (GameManager.I == null) return;

            SimMode targetMode = (GameManager.I.mode == SimMode.Ataque) ? SimMode.Defensa : SimMode.Ataque;
            GameManager.I.mode = targetMode;

            Debug.Log($"{LogPrefix} Alternando a {targetMode} y reiniciando simulación...");

            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }

        private void OnRestartScenarioClicked()
        {
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }
    }
}
