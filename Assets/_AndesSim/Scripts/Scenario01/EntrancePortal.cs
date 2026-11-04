using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.VR;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Controlador del Portal de Entrada y Vestíbulo del Escenario 1 (Paso 1 del flujo).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Presenta el briefing histórico del incidente (29 ENE 2015, 09:14 AM).
    /// - Permite seleccionar con la retícula el modo inicial: Modo Ataque vs Modo Defensa.
    /// - Al pulsar "Cruzar Portal", teletransporta al jugador dentro de la oficina 3D
    ///   y avanza automáticamente al Paso 2: Oficina 3D.
    /// </summary>
    public class EntrancePortal : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - Portal]";

        [Header("--- Botones de Selección de Modo (Paso 1) ---")]
        [SerializeField] private Button selectAttackModeButton;
        [SerializeField] private Image attackModeHighlightBorder;

        [SerializeField] private Button selectDefenseModeButton;
        [SerializeField] private Image defenseModeHighlightBorder;

        [Header("--- Botón de Activación del Portal ---")]
        [Tooltip("Botón interactivo en el portal para cruzar a la oficina 3D.")]
        [SerializeField] private Button enterOfficePortalButton;

        [Header("--- Textos del Briefing ---")]
        [SerializeField] private TextMeshProUGUI briefingTitleText;
        [SerializeField] private TextMeshProUGUI briefingDescriptionText;
        [SerializeField] private TextMeshProUGUI selectedModeBadgeText;

        [Header("--- Destino del Teletransporte al Entrar ---")]
        [Tooltip("Punto de inicio en el piso de la oficina donde aparecerá el jugador tras cruzar.")]
        [SerializeField] private Transform officeEntranceSpawnPoint;

        [Header("--- Efectos Audiovisuales ---")]
        [SerializeField] private ParticleSystem portalVfxParticles;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip modeSelectClip;
        [SerializeField] private AudioClip portalWarpClip;

        private void Start()
        {
            SetupButtons();
            RefreshBriefingUi();

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
            }
        }

        private void SetupButtons()
        {
            if (selectAttackModeButton != null)
            {
                selectAttackModeButton.onClick.AddListener(() => SetSimulationMode(SimMode.Ataque));
            }

            if (selectDefenseModeButton != null)
            {
                selectDefenseModeButton.onClick.AddListener(() => SetSimulationMode(SimMode.Defensa));
            }

            if (enterOfficePortalButton != null)
            {
                enterOfficePortalButton.onClick.AddListener(EnterOfficeSimulation);
            }
        }

        /// <summary>
        /// Cambia el modo de la simulación y actualiza la interfaz.
        /// </summary>
        public void SetSimulationMode(SimMode mode)
        {
            if (GameManager.I != null)
            {
                GameManager.I.mode = mode;
            }

            PlaySound(modeSelectClip);
            RefreshBriefingUi();

            Debug.Log($"{LogPrefix} Modo seleccionado en portal: <b>{mode}</b>");
        }

        private void HandleModeChanged(SimMode mode)
        {
            RefreshBriefingUi();
        }

        private void RefreshBriefingUi()
        {
            SimMode currentMode = GameManager.I != null ? GameManager.I.mode : SimMode.Ataque;

            if (selectedModeBadgeText != null)
            {
                selectedModeBadgeText.text = (currentMode == SimMode.Ataque)
                    ? "MODO SELECCIONADO: <color=#FF6B6B>ATAQUE (EXPERIMENTAR VULNERABILIDAD)</color>"
                    : "MODO SELECCIONADO: <color=#38F28A>DEFENSA (APLICAR CONTRAMEDIDAS)</color>";
            }

            if (attackModeHighlightBorder != null)
            {
                attackModeHighlightBorder.enabled = (currentMode == SimMode.Ataque);
            }

            if (defenseModeHighlightBorder != null)
            {
                defenseModeHighlightBorder.enabled = (currentMode == SimMode.Defensa);
            }

            if (briefingDescriptionText != null)
            {
                if (currentMode == SimMode.Ataque)
                {
                    briefingDescriptionText.text =
                        "<b>OBJETIVO DE MODO:</b>\n" +
                        "Observa cómo un correo con dropper .scr.zip infectó 3 terminales y abrió una puerta trasera C2 en la red SWIFT del BANCO ANDINO por falta de filtros y credenciales expuestas.\n\n" +
                        "<i>Criterio: Encuentra las 3 señales en menos de 180 segundos.</i>";
                }
                else
                {
                    briefingDescriptionText.text =
                        "<b>OBJETIVO DE MODO:</b>\n" +
                        "Aplica las 3 contramedidas perimetrales: Detonación en Sandbox, Botón corporativo 'Reportar Phishing' y Autenticación MFA para contener el ataque con 0 infectados.\n\n" +
                        "<i>Criterio: Reporta la amenaza y neutraliza las vías de intrusión.</i>";
                }
            }
        }

        /// <summary>
        /// Ejecuta la transición de cruce del portal hacia el interior de la oficina.
        /// </summary>
        public void EnterOfficeSimulation()
        {
            PlaySound(portalWarpClip);

            if (portalVfxParticles != null)
            {
                portalVfxParticles.Play();
            }

            Debug.Log($"{LogPrefix} Jugador cruzando el portal hacia la oficina 3D...");

            // Teletransportar al jugador al punto de entrada
            if (PlayerTeleportController.I != null && officeEntranceSpawnPoint != null)
            {
                PlayerTeleportController.I.TeleportTo(officeEntranceSpawnPoint.position, officeEntranceSpawnPoint.rotation);
            }

            // Avanzar flujo al Paso 2: Oficina 3D
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.SetStep(ScenarioStep.Step2_Oficina3D);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Soporte físico si el jugador cruza el volumen del portal
            if (other.CompareTag("PlayerRig") || other.GetComponentInParent<PlayerTeleportController>() != null)
            {
                EnterOfficeSimulation();
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
