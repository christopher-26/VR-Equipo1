using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Data;
using AndesSim.VR;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Controlador de pantalla y visualización interactiva para los monitores de la oficina (RF-01).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Gestiona Monitor_01 (Carla Mendoza), Monitor_02 (Comercio Exterior) y Monitor_03 (SIEM / Registro).
    /// - Actualiza dinámicamente el contenido de la pantalla según el modo actual: SimMode.Ataque vs SimMode.Defensa.
    /// - Integra colisionador e interacción por mirada VR (GazeInteractable) en la capa "Interactive".
    /// - Permite registrar evidencias (Evidencia_LogAccesos en Monitor 03) y avanzar el flujo pedagógico.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(GazeInteractable))]
    public class OfficeMonitorDisplay : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - Monitor Display]";

        [Header("--- Identificación del Puesto ---")]
        [Tooltip("Identificador único del monitor: 'Monitor_01', 'Monitor_02' o 'Monitor_03'.")]
        [SerializeField] private string workstationId = "Monitor_01";

        [Tooltip("Dataset de puestos de trabajo. Si se deja nulo, se cargará desde Resources/Data/Workstations_Data.")]
        [SerializeField] private WorkstationData workstationDatabase;

        [Header("--- Elementos de UI en Canvas World Space ---")]
        [SerializeField] private Canvas screenCanvas;
        [SerializeField] private TextMeshProUGUI appTitleText;
        [SerializeField] private TextMeshProUGUI operatorInfoText;
        [SerializeField] private TextMeshProUGUI statusBannerText;
        [SerializeField] private Image statusBannerBackground;
        [SerializeField] private TextMeshProUGUI bodyContentText;
        [SerializeField] private TextMeshProUGUI footerNoticeText;

        [Header("--- Efectos y Señalización Visual ---")]
        [Tooltip("Luz emitida o pantalla CRT para tintar según el estado (rojo/verde).")]
        [SerializeField] private Light screenGlowLight;
        [SerializeField] private Renderer monitorScreenRenderer;
        [SerializeField] private int screenMaterialIndex = 0;

        [Header("--- Retroalimentación Sonora ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip inspectSoundClip;
        [SerializeField] private AudioClip alertWarningSoundClip;
        [SerializeField] private AudioClip mitigationSuccessSoundClip;

        // Estado interno
        private WorkstationConfig _config;
        private GazeInteractable _gazeInteractable;
        private BoxCollider _boxCollider;
        private Material _screenMaterialInstance;

        #region Propiedades Públicas

        public string WorkstationId => workstationId;
        public WorkstationConfig CurrentConfig => _config;

        #endregion

        private void Awake()
        {
            _gazeInteractable = GetComponent<GazeInteractable>();
            _boxCollider = GetComponent<BoxCollider>();

            // Configurar capa interactiva
            gameObject.layer = LayerMask.NameToLayer("Interactive");

            // Instanciar material si hay renderer asignado
            if (monitorScreenRenderer != null && screenMaterialIndex < monitorScreenRenderer.materials.Length)
            {
                _screenMaterialInstance = monitorScreenRenderer.materials[screenMaterialIndex];
            }

            // Enlazar eventos de GazeInteractable
            if (_gazeInteractable != null)
            {
                _gazeInteractable.onClick.AddListener(OnScreenGazeClicked);
                _gazeInteractable.onEnter.AddListener(OnScreenGazeEntered);
            }
        }

        private void Start()
        {
            LoadWorkstationData();
            RefreshDisplayContent();

            // Suscribirse a eventos de GameManager si existe
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
                _gazeInteractable.onClick.RemoveListener(OnScreenGazeClicked);
                _gazeInteractable.onEnter.RemoveListener(OnScreenGazeEntered);
            }
        }

        /// <summary>
        /// Carga los datos de la estación desde el ScriptableObject.
        /// </summary>
        private void LoadWorkstationData()
        {
            if (workstationDatabase == null)
            {
                workstationDatabase = Resources.Load<WorkstationData>("Data/Workstations_Data");
            }

            if (workstationDatabase != null)
            {
                _config = workstationDatabase.GetWorkstationById(workstationId);
            }

            if (_config == null)
            {
                Debug.LogWarning($"{LogPrefix} No se encontró configuración para {workstationId}. Usando valores predeterminados.");
            }
            else if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = $"Inspeccionar {_config.workstationName}";
            }
        }

        /// <summary>
        /// Actualiza todos los textos y colores del monitor de acuerdo al modo actual (Ataque vs Defensa).
        /// </summary>
        public void RefreshDisplayContent()
        {
            if (_config == null) return;

            SimMode currentMode = GameManager.I != null ? GameManager.I.mode : SimMode.Ataque;
            ScreenContent content = (currentMode == SimMode.Ataque) ? _config.attackModeScreen : _config.defenseModeScreen;

            // Información del operador y equipo
            if (operatorInfoText != null)
            {
                operatorInfoText.text = $"<b>{_config.operatorName}</b> | {_config.hostname} ({_config.ipAddress})";
            }

            if (content != null)
            {
                if (appTitleText != null)
                {
                    appTitleText.text = content.applicationTitle;
                }

                if (statusBannerText != null)
                {
                    statusBannerText.text = content.screenStatus;
                }

                if (statusBannerBackground != null)
                {
                    statusBannerBackground.color = content.statusColor;
                }

                if (bodyContentText != null)
                {
                    bodyContentText.text = $"{content.headerText}\n\n{content.bodyText}";
                }

                if (footerNoticeText != null)
                {
                    footerNoticeText.text = content.footerNotice;
                }

                // Ajustar luz de brillo en la oficina
                if (screenGlowLight != null)
                {
                    screenGlowLight.color = content.statusColor;
                    screenGlowLight.intensity = (currentMode == SimMode.Ataque) ? 1.8f : 1.2f;
                }

                // Color emisivo en el material de pantalla
                if (_screenMaterialInstance != null && _screenMaterialInstance.HasProperty("_EmissionColor"))
                {
                    Color emissionColor = content.statusColor * 0.4f;
                    _screenMaterialInstance.SetColor("_EmissionColor", emissionColor);
                    _screenMaterialInstance.EnableKeyword("_EMISSION");
                }
            }
        }

        private void HandleModeChanged(SimMode newMode)
        {
            RefreshDisplayContent();
        }

        /// <summary>
        /// Invocado cuando el jugador apunta la retícula hacia la pantalla del monitor.
        /// </summary>
        private void OnScreenGazeEntered()
        {
            // Notificar interacción al orquestador de flujo
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.NotifyWorkstationInteracted(workstationId);
            }
        }

        /// <summary>
        /// Invocado cuando el jugador hace clic o completa el dwell time en la pantalla.
        /// </summary>
        private void OnScreenGazeClicked()
        {
            PlaySound(inspectSoundClip);

            Debug.Log($"{LogPrefix} Clic interactivo en pantalla de: <b>{workstationId}</b>");

            // Si es Monitor_03 (Consola SIEM), registrar la evidencia física/lógica del Log de Accesos
            if (string.Equals(workstationId, "Monitor_03", StringComparison.OrdinalIgnoreCase))
            {
                if (GameManager.I != null)
                {
                    GameManager.I.RegisterEvidence("Evidencia_LogAccesos");
                }
            }

            // Si es Monitor_01 (Carla Mendoza) y estamos en Paso 3, notificar apertura de correo
            if (string.Equals(workstationId, "Monitor_01", StringComparison.OrdinalIgnoreCase))
            {
                if (ScenarioFlowController.I != null)
                {
                    ScenarioFlowController.I.NotifyEmailOpened();
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
    }
}
