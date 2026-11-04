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
    /// Controlador del panel de contramedidas y consola de defensa perimetral (RF-04).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Permite activar interactivamente las 3 defensas con la retícula:
    ///   1. Defensa_Sandbox: Detonación perimetral y análisis dinámico de URLs / .scr.zip.
    ///   2. Defensa_BotonReportar: Despliegue de botón corporativo en clientes de correo.
    ///   3. Defensa_MFA: Autenticación multifactor obligatoria en terminales de transferencias SWIFT.
    /// - Actualiza en tiempo real los indicadores de seguridad y el impacto en equipos infectados (3 -> 0).
    /// </summary>
    public class DefenseTerminalController : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - Defense Terminal]";

        [Header("--- Botones Interactivos de Contramedida (RF-04) ---")]
        [Tooltip("Botón para activar Sandbox Perimetral.")]
        [SerializeField] private Button sandboxDefenseButton;
        [SerializeField] private Image sandboxStatusLight;
        [SerializeField] private TextMeshProUGUI sandboxStatusText;

        [Tooltip("Botón para activar Botón Corporativo de Reporte.")]
        [SerializeField] private Button reportButtonDefenseButton;
        [SerializeField] private Image reportButtonStatusLight;
        [SerializeField] private TextMeshProUGUI reportButtonStatusText;

        [Tooltip("Botón para activar Autenticación Multifactor (MFA).")]
        [SerializeField] private Button mfaDefenseButton;
        [SerializeField] private Image mfaStatusLight;
        [SerializeField] private TextMeshProUGUI mfaStatusText;

        [Header("--- Indicadores Generales de Red y Estado ---")]
        [SerializeField] private TextMeshProUGUI infectedCountText;
        [SerializeField] private TextMeshProUGUI networkStatusText;
        [SerializeField] private Image networkStatusBanner;
        [SerializeField] private TextMeshProUGUI defenseProgressText;

        [Header("--- Retroalimentación Sonora ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip defenseAppliedClip;
        [SerializeField] private AudioClip allDefensesActiveClip;

        // Estado interno
        private readonly HashSet<string> _activeDefenses = new HashSet<string>();

        private void Start()
        {
            SetupButtons();
            RefreshAllIndicators();

            if (GameManager.I != null)
            {
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
                GameManager.I.OnModeChanged += HandleModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
                GameManager.I.OnModeChanged -= HandleModeChanged;
            }
        }

        private void SetupButtons()
        {
            if (sandboxDefenseButton != null)
            {
                sandboxDefenseButton.onClick.AddListener(() => ToggleDefense("Defensa_Sandbox"));
            }

            if (reportButtonDefenseButton != null)
            {
                reportButtonDefenseButton.onClick.AddListener(() => ToggleDefense("Defensa_BotonReportar"));
            }

            if (mfaDefenseButton != null)
            {
                mfaDefenseButton.onClick.AddListener(() => ToggleDefense("Defensa_MFA"));
            }
        }

        /// <summary>
        /// Aplica una contramedida específica y la registra en el GameManager.
        /// </summary>
        public void ToggleDefense(string defenseId)
        {
            if (_activeDefenses.Contains(defenseId))
            {
                Debug.Log($"{LogPrefix} La defensa {defenseId} ya está activa.");
                return;
            }

            _activeDefenses.Add(defenseId);
            PlaySound(defenseAppliedClip);

            Debug.Log($"{LogPrefix} <color=#4EC9B0><b>Contramedida implementada:</b></color> {defenseId} [{_activeDefenses.Count}/3]");

            // Notificar al GameManager oficial
            if (GameManager.I != null)
            {
                GameManager.I.ApplyDefense(defenseId);
            }

            RefreshAllIndicators();

            if (_activeDefenses.Count >= 3)
            {
                PlaySound(allDefensesActiveClip);
                Debug.Log($"{LogPrefix} <color=#9CDCFE><b>¡Todas las contramedidas activas! Red del Banco Andino blindada.</b></color>");
            }
        }

        private void HandleDefenseApplied(string defenseId)
        {
            if (!_activeDefenses.Contains(defenseId))
            {
                _activeDefenses.Add(defenseId);
                RefreshAllIndicators();
            }
        }

        private void HandleModeChanged(SimMode mode)
        {
            RefreshAllIndicators();
        }

        /// <summary>
        /// Actualiza los textos, luces y métricas visuales del panel de control de defensas.
        /// </summary>
        public void RefreshAllIndicators()
        {
            bool hasSandbox = _activeDefenses.Contains("Defensa_Sandbox");
            bool hasReport = _activeDefenses.Contains("Defensa_BotonReportar");
            bool hasMfa = _activeDefenses.Contains("Defensa_MFA");

            // Sandbox
            if (sandboxStatusLight != null) sandboxStatusLight.color = hasSandbox ? Color.green : Color.red;
            if (sandboxStatusText != null) sandboxStatusText.text = hasSandbox ? "ACTIVO (Detonación perimetral)" : "INACTIVO (Tráfico sin filtrar)";

            // Botón de Reporte
            if (reportButtonStatusLight != null) reportButtonStatusLight.color = hasReport ? Color.green : Color.red;
            if (reportButtonStatusText != null) reportButtonStatusText.text = hasReport ? "DESPLEGADO (Buzones sincronizados)" : "INEXISTENTE (Sin canal rápido)";

            // MFA
            if (mfaStatusLight != null) mfaStatusLight.color = hasMfa ? Color.green : Color.red;
            if (mfaStatusText != null) mfaStatusText.text = hasMfa ? "OBLIGATORIO (Token criptográfico)" : "DESHABILITADO (Solo clave local)";

            // Contador de equipos infectados
            int infectedCount = 3 - _activeDefenses.Count;
            if (infectedCount < 0) infectedCount = 0;

            if (infectedCountText != null)
            {
                infectedCountText.text = $"Equipos infectados: <size=120%><b>{infectedCount} / 3</b></size>";
                infectedCountText.color = (infectedCount == 0) ? Color.green : ((infectedCount <= 1) ? Color.yellow : Color.red);
            }

            // Estado de red
            if (networkStatusText != null)
            {
                if (infectedCount == 0)
                {
                    networkStatusText.text = "ESTADO DE RED: SEGURA Y MITIGADA";
                }
                else if (infectedCount == 1)
                {
                    networkStatusText.text = "ESTADO DE RED: CONTENCIÓN PARCIAL";
                }
                else
                {
                    networkStatusText.text = "ESTADO DE RED: COMPROMETIDA (C2 ACTIVO)";
                }
            }

            if (networkStatusBanner != null)
            {
                networkStatusBanner.color = (infectedCount == 0) ? new Color(0.1f, 0.7f, 0.2f, 0.8f) : (infectedCount == 1 ? new Color(0.8f, 0.6f, 0.1f, 0.8f) : new Color(0.8f, 0.1f, 0.1f, 0.8f));
            }

            if (defenseProgressText != null)
            {
                defenseProgressText.text = $"Contramedidas implementadas: <b>{_activeDefenses.Count} / 3</b>";
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
