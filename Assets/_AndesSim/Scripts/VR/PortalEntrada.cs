using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Data;

namespace AndesSim.VR
{
    /// <summary>
    /// Controlador del Portal Holográfico de Entrada del simulador "ANDES-SIM · SOC Banco Andino".
    /// Componente oficial para el objeto obligatorio 'Portal_Entrada'.
    /// - Muestra el resumen pedagógico de la misión (29 de enero de 2015: 16 empleados, 3 señales, 3 minutos).
    /// - Al mirar y tocar (Gaze + Click / Dwell):
    ///   1. Reproduce el sonido de inmersión en la simulación histórica.
    ///   2. Teletransporta al jugador al TeleportPoint_01 (Puesto de Carla Mendoza).
    ///   3. Inicia el cronómetro de 3 minutos en GameManager.I.StartSimulationTimer().
    ///   4. Avanza el ScenarioFlowController al Paso 2: Oficina 3D.
    /// - Incluye el botón interactivo de "Regresar al Vestíbulo / Cambiar Modo" accesible desde la oficina.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PortalEntrada : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - PortalEntrada]";

        [Header("--- Título y Resumen de Misión ---")]
        [SerializeField] private string portalHeaderTitle = "ANDES-SIM · SOC Banco Andino";

        [TextArea(3, 6)]
        [SerializeField] private string missionBriefingText =
            "29 de enero de 2015: 16 empleados reciben el correo de Rasel Ahlam.\n" +
            "Objetivo: detectar las 3 señales de phishing en menos de 3 minutos, " +
            "recolectar las 3 evidencias y comparar Modo Ataque vs Modo Defensa.";

        [Header("--- Destino del Teletransporte (Paso 2) ---")]
        [Tooltip("TeleportPoint_01 en la estación de Carla Mendoza hacia donde se teletransporta al cruzar.")]
        [SerializeField] private TeleportPoint destinationTeleportPoint;

        [Tooltip("Transform de respaldo si no se asignó TeleportPoint (Puesto 1).")]
        [SerializeField] private Transform fallbackSpawnPoint;

        [Header("--- Retorno al Vestíbulo (Lobby) ---")]
        [Tooltip("Punto de inicio en el vestíbulo frente al portal para volver.")]
        [SerializeField] private Transform lobbySpawnPoint;

        [Tooltip("Botón interactivo dentro de la oficina para regresar al vestíbulo.")]
        [SerializeField] private Button returnToLobbyButton;

        [Header("--- Elementos de UI en Canvas World Space ---")]
        [SerializeField] private TextMeshProUGUI portalTitleDisplay;
        [SerializeField] private TextMeshProUGUI briefingDisplay;
        [SerializeField] private TextMeshProUGUI currentModeBadge;

        [SerializeField] private Button enterPortalButton;
        [SerializeField] private Button selectAttackModeButton;
        [SerializeField] private Button selectDefenseModeButton;
        [SerializeField] private Button viewCreditsButton;

        [Header("--- Efectos Holográficos y Animación ---")]
        [SerializeField] private Renderer hologramPortalRenderer;
        [SerializeField] private Color portalHologramColor = new Color(0f, 0.9f, 1f, 0.85f); // Cyan SOC
        [SerializeField] private ParticleSystem immersionParticles;

        [Header("--- Audio Espacial ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip immersionWarpClip;
        [SerializeField] private AudioClip returnLobbyClip;
        [SerializeField] private AudioClip buttonClickClip;

        // Estado interno
        private GazeInteractable _gazeInteractable;
        private Collider _portalCollider;
        private bool _isTransitioning = false;

        private void Awake()
        {
            _portalCollider = GetComponent<Collider>();
            _gazeInteractable = GetComponent<GazeInteractable>();

            // Asignar capa interactiva (Layer 8)
            gameObject.layer = LayerMask.NameToLayer("Interactive");

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Si tiene GazeInteractable, configurar label y evento de clic
            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = "Cruzar Portal: Entrar a la Oficina 2015";
                _gazeInteractable.onClick.AddListener(CrossPortal);
            }
        }

        private void Start()
        {
            ResolveTargetPoints();
            SetupUserInterface();
            SetupHologramVisuals();

            // Sincronizar eventos con GameManager
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

            if (_gazeInteractable != null)
            {
                _gazeInteractable.onClick.RemoveListener(CrossPortal);
            }
        }

        /// <summary>
        /// Localiza automáticamente TeleportPoint_01 y puntos de spawn si no fueron cableados en el Inspector.
        /// </summary>
        private void ResolveTargetPoints()
        {
            if (destinationTeleportPoint == null)
            {
                TeleportPoint[] points = FindObjectsByType<TeleportPoint>(FindObjectsSortMode.None);
                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i].gameObject.name.IndexOf("01", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        points[i].PointName.IndexOf("Carla", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        destinationTeleportPoint = points[i];
                        break;
                    }
                }
            }

            if (lobbySpawnPoint == null)
            {
                GameObject lobbyObj = GameObject.Find("SpawnPoint_Lobby") ?? GameObject.Find("Player");
                if (lobbyObj != null) lobbySpawnPoint = lobbyObj.transform;
            }
        }

        private void SetupUserInterface()
        {
            if (portalTitleDisplay != null)
            {
                portalTitleDisplay.text = portalHeaderTitle;
            }

            if (briefingDisplay != null)
            {
                briefingDisplay.text = missionBriefingText;
            }

            if (enterPortalButton != null)
            {
                enterPortalButton.onClick.AddListener(CrossPortal);
            }

            if (selectAttackModeButton != null)
            {
                selectAttackModeButton.onClick.AddListener(() => SetMode(SimMode.Ataque));
            }

            if (selectDefenseModeButton != null)
            {
                selectDefenseModeButton.onClick.AddListener(() => SetMode(SimMode.Defensa));
            }

            if (returnToLobbyButton != null)
            {
                returnToLobbyButton.onClick.AddListener(ReturnToLobby);
            }

            if (viewCreditsButton != null)
            {
                viewCreditsButton.onClick.AddListener(OnViewCreditsClicked);
            }

            UpdateModeBadge();
        }

        /// <summary>
        /// Despliega el modal interactivo de créditos, atribución de IA y marco legal en World Space.
        /// </summary>
        public void OnViewCreditsClicked()
        {
            PlaySound(buttonClickClip);
            if (AndesSim.UI.CreditsAndEthicsModalUI.I != null)
            {
                AndesSim.UI.CreditsAndEthicsModalUI.I.OpenModal();
            }
        }

        private void SetupHologramVisuals()
        {
            if (hologramPortalRenderer != null && hologramPortalRenderer.material != null)
            {
                Material mat = hologramPortalRenderer.material;
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", portalHologramColor);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", portalHologramColor * 0.7f);
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }

        private void SetMode(SimMode newMode)
        {
            PlaySound(buttonClickClip);

            if (GameManager.I != null)
            {
                GameManager.I.SetMode(newMode);
            }

            UpdateModeBadge();
        }

        private void HandleModeChanged(SimMode newMode)
        {
            UpdateModeBadge();
        }

        private void UpdateModeBadge()
        {
            if (currentModeBadge == null) return;

            SimMode current = (GameManager.I != null) ? GameManager.I.mode : SimMode.Ataque;
            if (current == SimMode.Ataque)
            {
                currentModeBadge.text = "MODO ACTIVO: <color=#FF6B6B><b>ATAQUE (HISTÓRICO 2015)</b></color>";
            }
            else
            {
                currentModeBadge.text = "MODO ACTIVO: <color=#38F28A><b>DEFENSA (CONTENCIÓN SOC)</b></color>";
            }
        }

        #region Acciones Oficiales de Cruce y Retorno

        /// <summary>
        /// Acción principal al mirar y tocar el portal:
        /// 1. Reproduce sonido de inmersión en la simulación.
        /// 2. Teletransporta al jugador al TeleportPoint_01 en la Oficina de Pagos.
        /// 3. Inicia el cronómetro de 3 minutos en GameManager.I.StartSimulationTimer().
        /// 4. Avanza el ScenarioFlowController al Paso 2: Oficina 3D.
        /// </summary>
        public void CrossPortal()
        {
            if (_isTransitioning) return;
            StartCoroutine(CrossPortalRoutine());
        }

        private IEnumerator CrossPortalRoutine()
        {
            _isTransitioning = true;
            Debug.Log($"{LogPrefix} <b>Cruzar Portal detonado. Iniciando inmersión en la simulación...</b>");

            // 1. Reproducir sonido y partículas de inmersión
            PlayImmersionAudio();

            if (immersionParticles != null)
            {
                immersionParticles.Play();
            }

            // 2. Iniciar temporizador de 3 minutos en GameManager
            if (GameManager.I != null)
            {
                GameManager.I.StartSimulationTimer();
            }

            // 3. Teletransportar al jugador a TeleportPoint_01
            if (PlayerTeleportController.I != null)
            {
                if (destinationTeleportPoint != null)
                {
                    PlayerTeleportController.I.TeleportToPoint(destinationTeleportPoint);
                }
                else if (fallbackSpawnPoint != null)
                {
                    PlayerTeleportController.I.TeleportTo(fallbackSpawnPoint.position, fallbackSpawnPoint.rotation);
                }
                else
                {
                    // Posición por defecto frente a Monitor 01 en Puesto 1
                    PlayerTeleportController.I.TeleportTo(new Vector3(-3.0f, 0f, 0.8f), Quaternion.identity);
                }
            }

            yield return new WaitForSeconds(0.2f);

            // 4. Avanzar el ScenarioFlowController al Paso 2: Oficina 3D
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.SetStep(ScenarioStep.Step2_Oficina3D);
            }

            _isTransitioning = false;
        }

        /// <summary>
        /// Botón de "Regresar al Vestíbulo / Cambiar Modo" dentro de la oficina.
        /// Permite al jugador salir al vestíbulo para reiniciar o experimentar el modo opuesto.
        /// </summary>
        public void ReturnToLobby()
        {
            if (_isTransitioning) return;
            StartCoroutine(ReturnToLobbyRoutine());
        }

        private IEnumerator ReturnToLobbyRoutine()
        {
            _isTransitioning = true;
            Debug.Log($"{LogPrefix} Regresando al vestíbulo para cambio de modo o reinicio...");

            PlaySound(returnLobbyClip);

            // Teletransportar al vestíbulo frente al portal
            if (PlayerTeleportController.I != null)
            {
                Vector3 returnPos = (lobbySpawnPoint != null) ? lobbySpawnPoint.position : new Vector3(0f, 0f, -4.0f);
                Quaternion returnRot = (lobbySpawnPoint != null) ? lobbySpawnPoint.rotation : Quaternion.identity;
                PlayerTeleportController.I.TeleportTo(returnPos, returnRot);
            }

            yield return new WaitForSeconds(0.2f);

            // Regresar el flujo al Paso 1: Portal de Entrada
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.SetStep(ScenarioStep.Step1_PortalEntrada);
            }

            _isTransitioning = false;
        }

        #endregion

        private void OnTriggerEnter(Collider other)
        {
            // Soporte físico si el jugador cruza el volumen del marco
            if (other.CompareTag("PlayerRig") || other.GetComponentInParent<PlayerTeleportController>() != null)
            {
                if (ScenarioFlowController.I != null && ScenarioFlowController.I.CurrentStep == ScenarioStep.Step1_PortalEntrada)
                {
                    CrossPortal();
                }
            }
        }

        private void PlayImmersionAudio()
        {
            if (immersionWarpClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(immersionWarpClip);
            }
            else
            {
                // Fallback procedural: barrido de frecuencias de ciencia ficción (inmersión en simulación)
                AudioClip warp = GenerateProceduralWarpClip();
                if (audioSource != null && warp != null)
                {
                    audioSource.PlayOneShot(warp);
                }
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private AudioClip GenerateProceduralWarpClip()
        {
            int sampleRate = 44100;
            float duration = 0.8f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;
                // Frecuencia que sube de 200Hz a 1200Hz con modulación de anillo
                float freq = Mathf.Lerp(200f, 1200f, progress);
                float envelope = Mathf.Sin(progress * Mathf.PI);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.35f * envelope;
            }

            AudioClip clip = AudioClip.Create("Procedural_PortalImmersion", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
