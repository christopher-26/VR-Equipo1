using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.UI;
using AndesSim.VR;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Controlador interactivo avanzado para los 3 monitores (Monitor_01, Monitor_02, Monitor_03).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Opera en la capa "Interactive" (Layer 8) con Canvas en World Space.
    /// 
    /// Capacidades:
    /// 1. Interacción a distancia: Al mirar y hacer clic desde lejos, teletransporta al jugador
    ///    al punto de vista óptimo de la estación o despliega una vista en alta legibilidad en World Space.
    /// 2. Estado de "Infección Activa":
    ///    Pantalla parpadeante en rojo #ff5a4e con trazas de consola ejecutando el malware de Lazarus
    ///    (svchost_upd.exe, conexión C2 a 185.130.44.82:443) al abrirse el enlace en Modo Ataque.
    /// 3. Estado de "Protegido / En Cuarentena":
    ///    Interfaz verde #38f28a cuando se activan las defensas en Modo Defensa.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractiveMonitorController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string LogPrefix = "[ANDES-SIM - Monitor Controller]";
        private static readonly Color InfectionRedColor = new Color(1.0f, 0.35f, 0.31f, 1.0f); // #ff5a4e
        private static readonly Color ProtectedGreenColor = new Color(0.22f, 0.95f, 0.54f, 1.0f); // #38f28a

        public enum MonitorState
        {
            Normal,
            InfeccionActiva,
            ProtegidoCuarentena
        }

        [Header("--- Identificación del Monitor ---")]
        [Tooltip("ID de la estación: 'Monitor_01', 'Monitor_02' o 'Monitor_03'.")]
        [SerializeField] private string monitorId = "Monitor_01";

        [Tooltip("Estado visual actual de la pantalla.")]
        [SerializeField] private MonitorState currentState = MonitorState.Normal;

        [Header("--- Posicionamiento y Teletransporte Óptimo ---")]
        [Tooltip("Punto de teletransporte óptimo para ver este monitor de cerca.")]
        [SerializeField] private TeleportPoint optimalTeleportPoint;

        [Tooltip("Transform de respaldo para el punto de vista frontal si no hay TeleportPoint.")]
        [SerializeField] private Transform optimalViewingTransform;

        [Tooltip("Distancia máxima en metros a la que se considera que el jugador está 'lejos' y debe teletransportarse.")]
        [SerializeField] private float distanceTeleportThreshold = 1.8f;

        [Header("--- Canvas en World Space y Vista de Alta Legibilidad ---")]
        [SerializeField] private Canvas worldSpaceScreenCanvas;
        [SerializeField] private GameObject highLegibilityOverlayPanel;
        [SerializeField] private TextMeshProUGUI highLegibilityText;
        [SerializeField] private Button closeHighLegibilityButton;

        [Header("--- Elementos de Visualización del Monitor ---")]
        [SerializeField] private TextMeshProUGUI screenTitleText;
        [SerializeField] private TextMeshProUGUI screenBodyContentText;
        [SerializeField] private TextMeshProUGUI statusBannerText;
        [SerializeField] private Image statusBannerBackground;
        [SerializeField] private Renderer monitorScreenRenderer;
        [SerializeField] private Light screenGlowLight;
        [SerializeField] private int screenMaterialIndex = 0;

        [Header("--- Retroalimentación Sonora y Efectos ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip infectionGlitchClip;
        [SerializeField] private AudioClip quarantinedChimeClip;
        [SerializeField] private AudioClip zoomInspectClip;

        // Estado interno
        private GazeInteractable _gazeInteractable;
        private Collider _collider;
        private Material _screenMaterialInstance;
        private Coroutine _flickerCoroutine;
        private bool _isOverlayActive = false;

        #region Propiedades Públicas

        public string MonitorId => monitorId;
        public MonitorState CurrentState => currentState;

        #endregion

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _gazeInteractable = GetComponent<GazeInteractable>();

            // Asignar capa interactiva (Layer 8)
            gameObject.layer = LayerMask.NameToLayer("Interactive");

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (monitorScreenRenderer != null && screenMaterialIndex < monitorScreenRenderer.materials.Length)
            {
                _screenMaterialInstance = monitorScreenRenderer.materials[screenMaterialIndex];
            }

            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = $"Inspeccionar {monitorId}";
                _gazeInteractable.dwellTime = 1.1f;
                _gazeInteractable.onClick.AddListener(OnMonitorClicked);
            }

            if (_collider is BoxCollider boxCol)
            {
                Vector3 currentSize = boxCol.size;
                boxCol.size = new Vector3(Mathf.Max(currentSize.x, 0.72f), Mathf.Max(currentSize.y, 0.48f), Mathf.Max(currentSize.z, 0.25f));
            }
        }

        private void Start()
        {
            ResolveOptimalViewingPoint();

            if (closeHighLegibilityButton != null)
            {
                closeHighLegibilityButton.onClick.AddListener(CloseHighLegibilityOverlay);
            }

            if (highLegibilityOverlayPanel != null)
            {
                highLegibilityOverlayPanel.SetActive(false);
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
            }

            EvaluateStateBySimulationMode();
        }

        private void OnDestroy()
        {
            if (_gazeInteractable != null)
            {
                _gazeInteractable.onClick.RemoveListener(OnMonitorClicked);
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
            }
        }

        private void ResolveOptimalViewingPoint()
        {
            if (optimalTeleportPoint == null)
            {
                TeleportPoint[] points = FindObjectsByType<TeleportPoint>(FindObjectsSortMode.None);
                for (int i = 0; i < points.Length; i++)
                {
                    string pName = points[i].gameObject.name.ToLowerInvariant();
                    string mId = monitorId.ToLowerInvariant();
                    string assoc = points[i].gameObject.name.ToLowerInvariant();

                    if (assoc == mId || pName.Contains(mId) ||
                        (mId.Contains("01") && (pName.Contains("puesto1") || pName.Contains("carla") || pName.Contains("01"))) ||
                        (mId.Contains("02") && (pName.Contains("puesto2") || pName.Contains("analista") || pName.Contains("comercio") || pName.Contains("02"))) ||
                        (mId.Contains("03") && (pName.Contains("puesto3") || pName.Contains("siem") || pName.Contains("log") || pName.Contains("03"))))
                    {
                        optimalTeleportPoint = points[i];
                        break;
                    }
                }
            }
        }

        private void HandleModeChanged(SimMode mode)
        {
            EvaluateStateBySimulationMode();
        }

        private void HandleDefenseApplied(string defenseId)
        {
            if (GameManager.I != null && GameManager.I.mode == SimMode.Defensa)
            {
                SetState(MonitorState.ProtegidoCuarentena);
            }
        }

        private void EvaluateStateBySimulationMode()
        {
            SimMode mode = (GameManager.I != null) ? GameManager.I.mode : SimMode.Ataque;
            if (mode == SimMode.Defensa)
            {
                SetState(MonitorState.ProtegidoCuarentena);
            }
            else
            {
                // En modo ataque, el monitor se mantiene en estado Normal para leer el correo
                // Solo pasa a InfeccionActiva si el ataque ya fue detonado
                if (PhishingInspector.I != null && PhishingInspector.I.DecisionExecuted)
                {
                    SetState(MonitorState.InfeccionActiva);
                }
                else
                {
                    SetState(MonitorState.Normal);
                }
            }
        }

        /// <summary>
        /// Aplica uno de los estados visuales oficiales: Normal, Infección Activa o Protegido.
        /// </summary>
        public void SetState(MonitorState newState)
        {
            currentState = newState;

            if (_flickerCoroutine != null)
            {
                StopCoroutine(_flickerCoroutine);
                _flickerCoroutine = null;
            }

            switch (currentState)
            {
                case MonitorState.InfeccionActiva:
                    ApplyInfectionActiveState();
                    break;

                case MonitorState.ProtegidoCuarentena:
                    ApplyProtectedQuarantinedState();
                    break;

                default:
                    ApplyNormalState();
                    break;
            }
        }

        #region Estado: Infección Activa (Rojo #ff5a4e)

        private void ApplyInfectionActiveState()
        {
            Debug.Log($"{LogPrefix} <b>[{monitorId}] Estado: Infección Activa (#ff5a4e)</b>. Ejecutando trazas de Lazarus.");

            if (screenTitleText != null)
            {
                screenTitleText.text = "<b><color=#FF5A4E>[ALERTA: PROCESO HOSTIL EN EJECUCION]</color></b>";
            }

            if (statusBannerText != null)
            {
                statusBannerText.text = "ESTADO: <color=#FF5A4E><b>INFECCIÓN ACTIVA · BACKDOOR LAZARUS</b></color>";
            }

            if (statusBannerBackground != null)
            {
                statusBannerBackground.color = InfectionRedColor;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<color=#FF5A4E>[INTRUSION DETECTION]</color> Ejecución anómala detectada:");
            sb.AppendLine("• <b>Dropper detonado:</b> Rasel_Ahlam_CV_2015.scr.zip");
            sb.AppendLine("• <b>Proceso creado:</b> C:\\Users\\AppData\\Local\\svchost_upd.exe (PID 4820)");
            sb.AppendLine("• <b>Canal C2 establecido:</b> Conexión TCP saliente -> 185.130.44.82:443 (SSL/TLS)");
            sb.AppendLine("• <b>Evasión de defensas:</b> Tráfico cifrado simulando actualización del sistema.");
            sb.AppendLine("• <b>Movimiento lateral:</b> Reconocimiento de topología hacia el switch SWIFT.");

            if (screenBodyContentText != null)
            {
                screenBodyContentText.text = sb.ToString();
            }

            // Iniciar parpadeo/flicker en rojo #ff5a4e
            _flickerCoroutine = StartCoroutine(ScreenFlickerRoutine(InfectionRedColor));
            PlaySound(infectionGlitchClip);
        }

        #endregion

        #region Estado: Protegido / En Cuarentena (Verde #38f28a)

        private void ApplyProtectedQuarantinedState()
        {
            Debug.Log($"{LogPrefix} <color=#38F28A><b>[{monitorId}] Estado: Protegido / En Cuarentena (#38f28a)</b></color>");

            if (screenTitleText != null)
            {
                screenTitleText.text = "<b><color=#38F28A>[OK] PROTECCION PERIMETRAL SOC ACTIVA</color></b>";
            }

            if (statusBannerText != null)
            {
                statusBannerText.text = "ESTADO: <color=#38F28A><b>CORREO EN CUARENTENA · AMENAZA CONTENIDA</b></color>";
            }

            if (statusBannerBackground != null)
            {
                statusBannerBackground.color = ProtectedGreenColor;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<color=#38F28A>[SECURITY INCIDENT MITIGATED]</color> Protección en profundidad:");
            sb.AppendLine("• <b>Sandbox perimetral:</b> Archivo 'Rasel_Ahlam_CV_2015.scr.zip' aislado y detonado.");
            sb.AppendLine("• <b>Regla Firewall:</b> IP de comando y control 185.130.44.82 bloqueada en borde.");
            sb.AppendLine("• <b>Buzones asegurados:</b> Purga automática en las 16 bandejas de entrada.");
            sb.AppendLine("• <b>MFA Mandatario:</b> Intentos con claves en texto claro bloqueados por token.");
            sb.AppendLine("• <b>Resultado técnico:</b> 0 terminales infectadas. Red SWIFT intacta.");

            if (screenBodyContentText != null)
            {
                screenBodyContentText.text = sb.ToString();
            }

            ApplySolidColor(ProtectedGreenColor);
            PlaySound(quarantinedChimeClip);
        }

        #endregion

        private void ApplyNormalState()
        {
            ApplySolidColor(new Color(0.2f, 0.4f, 0.7f, 1f));
        }

        private IEnumerator ScreenFlickerRoutine(Color flickerColor)
        {
            while (true)
            {
                float intensity = Random.Range(0.6f, 1.3f);
                Color currentColor = flickerColor * intensity;

                ApplyColorToMaterialsAndLight(currentColor);
                yield return new WaitForSeconds(Random.Range(0.08f, 0.25f));
            }
        }

        private void ApplySolidColor(Color col)
        {
            ApplyColorToMaterialsAndLight(col);
        }

        private void ApplyColorToMaterialsAndLight(Color col)
        {
            if (_screenMaterialInstance != null)
            {
                if (_screenMaterialInstance.HasProperty("_Color")) _screenMaterialInstance.SetColor("_Color", col);
                if (_screenMaterialInstance.HasProperty("_EmissionColor"))
                {
                    _screenMaterialInstance.SetColor("_EmissionColor", col * 0.5f);
                    _screenMaterialInstance.EnableKeyword("_EMISSION");
                }
            }

            if (screenGlowLight != null)
            {
                screenGlowLight.color = col;
                screenGlowLight.intensity = 1.4f;
            }
        }

        #region Interacción a Distancia y Alta Legibilidad

        /// <summary>
        /// Al mirar y hacer clic en el monitor (o tocar la pantalla):
        /// 1. Si está lejos (> distanceTeleportThreshold), lo teletransporta al punto óptimo.
        /// 2. Despliega la pantalla en zoom de alta legibilidad VR (Modal Holográfico en primer plano).
        /// </summary>
        public void OnMonitorClicked()
        {
            Transform camTransform = Camera.main != null ? Camera.main.transform : null;
            float distance = (camTransform != null) ? Vector3.Distance(camTransform.position, transform.position) : 0f;

            Debug.Log($"{LogPrefix} Clic en {monitorId} a distancia: {distance:F2}m (Límite: {distanceTeleportThreshold}m)");

            // Notificar al flujo pedagógico de la simulación (Paso 2 -> Paso 3 -> Paso 4)
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.NotifyWorkstationInteracted(monitorId);
                if (monitorId.Contains("01") || monitorId.ToLowerInvariant().Contains("carla"))
                {
                    ScenarioFlowController.I.NotifyEmailOpened();
                }
            }

            if (distance > distanceTeleportThreshold)
            {
                TeleportToOptimalViewpoint();
                StartCoroutine(OpenInspectionAfterTeleport());
            }
            else
            {
                OpenInspectionZoomModal();
            }
        }

        private IEnumerator OpenInspectionAfterTeleport()
        {
            yield return new WaitForSeconds(0.2f);
            OpenInspectionZoomModal();
        }

        public void OpenInspectionZoomModal()
        {
            PlaySound(zoomInspectClip);

            // Priorizar VrInspectionOverlayManager para visualización inmersiva en primer plano
            if (VrInspectionOverlayManager.I != null)
            {
                string id = (monitorId + " " + gameObject.name).ToLowerInvariant();
                if (id.Contains("01") || id.Contains("carla"))
                {
                    SimMode mode = GameManager.I != null ? GameManager.I.mode : SimMode.Ataque;
                    if (mode == SimMode.Defensa)
                    {
                        // MODO DEFENSA (RF-04): El correo NUNCA llega a abrirse.
                        // La bandeja de entrada es reemplazada por el panel de cuarentena del SOC (Sandbox).
                        if (MailQuarantine.I != null)
                        {
                            MailQuarantine.I.TriggerQuarantineDefense();
                        }
                        VrInspectionOverlayManager.I.OpenQuarantineInspection();
                        return;
                    }
                    else
                    {
                        // MODO ATAQUE: Abre el correo de Rasel Ahlam para inspeccionar señales y detonar
                        VrInspectionOverlayManager.I.OpenEmailInspection();
                        return;
                    }
                }
                else if (id.Contains("02") || id.Contains("analista") || id.Contains("comercio"))
                {
                    VrInspectionOverlayManager.I.OpenSwiftInspection();
                    return;
                }
                else if (id.Contains("03") || id.Contains("siem") || id.Contains("log"))
                {
                    VrInspectionOverlayManager.I.OpenSiemLogInspection();
                    return;
                }
            }

            // Fallback: panel de alta legibilidad en el canvas del monitor
            ToggleHighLegibilityOverlay();
        }

        private void TeleportToOptimalViewpoint()
        {
            PlaySound(zoomInspectClip);

            if (PlayerTeleportController.I != null)
            {
                if (optimalTeleportPoint != null)
                {
                    Debug.Log($"{LogPrefix} Teletransportando al TeleportPoint óptimo de {monitorId}...");
                    PlayerTeleportController.I.TeleportToPoint(optimalTeleportPoint);
                }
                else if (optimalViewingTransform != null)
                {
                    Debug.Log($"{LogPrefix} Teletransportando a la posición óptima de {monitorId}...");
                    PlayerTeleportController.I.TeleportTo(optimalViewingTransform.position, optimalViewingTransform.rotation);
                }
            }
        }

        public void ToggleHighLegibilityOverlay()
        {
            _isOverlayActive = !_isOverlayActive;
            PlaySound(zoomInspectClip);

            if (highLegibilityOverlayPanel != null)
            {
                highLegibilityOverlayPanel.SetActive(_isOverlayActive);

                if (_isOverlayActive && highLegibilityText != null && screenBodyContentText != null)
                {
                    highLegibilityText.text = $"<b>[{monitorId.ToUpper()} - ALTA LEGIBILIDAD VR]</b>\n\n" + screenBodyContentText.text;
                }
            }
        }

        public void CloseHighLegibilityOverlay()
        {
            _isOverlayActive = false;
            if (highLegibilityOverlayPanel != null)
            {
                highLegibilityOverlayPanel.SetActive(false);
            }
        }

        #endregion

        #region EventSystems Handlers

        public void OnPointerClick(PointerEventData eventData)
        {
            OnMonitorClicked();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        #endregion

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
