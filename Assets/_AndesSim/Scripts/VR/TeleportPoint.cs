using System;
using System.Collections;
using AndesSim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AndesSim.VR
{
    /// <summary>
    /// Punto de teletransporte discreto para navegación VR en la oficina ("Zero Cybersickness").
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Diseñado para los 3 puestos de trabajo oficiales (Carla Mendoza, Analista de Pagos, Terminal de Registros).
    /// - Se activa con GazeInteractable en la capa "Interactive" mediante retícula o dwell timer.
    /// - Ejecuta un fundido visual rápido (Blink Comfort de 0.15s) transportando al jugador sin movimiento lineal continuo.
    /// - Anima un aro luminoso color verde esmeralda (#38f28a) en el suelo para guía visual clara.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(GazeInteractable))]
    public class TeleportPoint : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - TeleportPoint]";

        [Header("--- Identificación del Puesto ---")]
        [Tooltip("Nombre descriptivo del punto (ej. 'Puesto 1: Carla Mendoza (Operadora SWIFT)').")]
        [SerializeField] private string pointName = "Puesto 1: Carla Mendoza (Operadora SWIFT)";

        [Tooltip("Identificador de estación asociada para el flujo (ej: 'Monitor_01', 'Monitor_02', 'Monitor_03').")]
        [SerializeField] private string associatedWorkstationId = "Monitor_01";

        [Header("--- Posicionamiento del Jugador ---")]
        [Tooltip("Transform exacto donde se ubicará el PlayerRig (a nivel de piso y mirando hacia el escritorio).")]
        [SerializeField] private Transform standPosition;

        [Header("--- Confort Visual (Blink Teleport) ---")]
        [Tooltip("Duración total en segundos del fundido a negro (0.15 segundos para evitar mareos en VR).")]
        [SerializeField] private float fadeDuration = 0.15f;

        [Tooltip("Efecto de sonido espacial al teletransportarse.")]
        [SerializeField] private AudioClip teleportAudioClip;

        [Header("--- Aro Luminoso en el Suelo (#38f28a) ---")]
        [Tooltip("Renderer del aro o disco luminoso en el suelo.")]
        [SerializeField] private Renderer ringRenderer;

        [Tooltip("Color oficial del aro de teletransporte (#38f28a).")]
        [SerializeField] private Color ringColor = new Color(0.22f, 0.95f, 0.54f, 1.0f); // #38f28a

        [Tooltip("Frecuencia de pulsación del aro luminoso.")]
        [SerializeField] private float pulseSpeed = 2.4f;

        // Componentes cacheados
        private GazeInteractable _gazeInteractable;
        private Material _ringMaterialInstance;
        private Vector3 _originalScale;
        private bool _isHovered = false;

        // Referencia estática al fader de confort para no duplicar overlays
        private static Canvas _sharedFaderCanvas;
        private static Image _sharedFaderImage;
        private static bool _isTeleporting = false;

        public string PointName => pointName;
        public Transform StandPosition => standPosition != null ? standPosition : transform;

        private void Awake()
        {
            _gazeInteractable = GetComponent<GazeInteractable>();
            _originalScale = transform.localScale;

            // Si no se asignó un punto de pie específico, usar la propia posición del transform
            if (standPosition == null)
            {
                standPosition = transform;
            }

            // Configurar etiqueta para la retícula
            ConfigureGazeProperties();

            // Configurar y preparar material del aro luminoso
            SetupRingVisuals();
        }

        private void Start()
        {
            EnsureFaderOverlayExists();
        }

        private void Update()
        {
            AnimateRingPulse();
        }

        private void ConfigureGazeProperties()
        {
            if (_gazeInteractable == null) return;

            // Inyectar etiqueta descriptiva para la retícula
            var labelField = typeof(GazeInteractable).GetField("interactableLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (labelField != null)
            {
                labelField.SetValue(_gazeInteractable, $"Teletransportarse a {pointName}");
            }

            // Suscribir eventos
            _gazeInteractable.onClick.RemoveListener(ExecuteTeleport);
            _gazeInteractable.onClick.AddListener(ExecuteTeleport);

            _gazeInteractable.onEnter.RemoveListener(HandleGazeEnter);
            _gazeInteractable.onEnter.AddListener(HandleGazeEnter);

            _gazeInteractable.onExit.RemoveListener(HandleGazeExit);
            _gazeInteractable.onExit.AddListener(HandleGazeExit);
        }

        private void SetupRingVisuals()
        {
            if (ringRenderer == null)
            {
                ringRenderer = GetComponentInChildren<Renderer>();
            }

            if (ringRenderer != null)
            {
                _ringMaterialInstance = ringRenderer.material;
                _ringMaterialInstance.EnableKeyword("_EMISSION");
                _ringMaterialInstance.SetColor("_Color", ringColor);
                _ringMaterialInstance.SetColor("_EmissionColor", ringColor * 0.8f);
            }
        }

        /// <summary>
        /// Anima la escala y brillo del aro verde (#38f28a) en el suelo para orientar al jugador.
        /// </summary>
        private void AnimateRingPulse()
        {
            float wave = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;

            // Modulación de escala sutil
            float scaleMultiplier = _isHovered ? 1.08f : (1.0f + wave * 0.05f);
            transform.localScale = _originalScale * scaleMultiplier;

            // Modulación de emisión en el material
            if (_ringMaterialInstance != null)
            {
                float emissionIntensity = _isHovered ? 1.8f : (0.7f + wave * 0.6f);
                _ringMaterialInstance.SetColor("_EmissionColor", ringColor * emissionIntensity);
            }
        }

        private void HandleGazeEnter()
        {
            _isHovered = true;
        }

        private void HandleGazeExit()
        {
            _isHovered = false;
        }

        /// <summary>
        /// Inicia el teletransporte con confort vestibular mediante parpadeo (Blink).
        /// </summary>
        public void ExecuteTeleport()
        {
            if (_isTeleporting) return;

            StartCoroutine(ComfortBlinkTeleportRoutine());
        }

        /// <summary>
        /// Corrutina de transición instantánea: Fundido a negro (0.075s), traslación, y apertura de vista (0.075s).
        /// </summary>
        private IEnumerator ComfortBlinkTeleportRoutine()
        {
            _isTeleporting = true;
            Debug.Log($"{LogPrefix} Ejecutando teletransporte instantáneo hacia: <color=#38f28a>'{pointName}'</color>");

            EnsureFaderOverlayExists();

            Transform playerTransform = ResolvePlayerRigTransform();
            float halfDuration = Mathf.Max(0.04f, fadeDuration * 0.5f);

            // 1. Fundido a negro (Entrada rápida)
            if (_sharedFaderImage != null)
            {
                _sharedFaderImage.gameObject.SetActive(true);
                float elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float alpha = Mathf.Clamp01(elapsed / halfDuration);
                    _sharedFaderImage.color = new Color(0f, 0f, 0f, alpha);
                    yield return null;
                }
                _sharedFaderImage.color = Color.black;
            }

            // 2. Reposicionamiento instantáneo del jugador (Cero movimiento continuo)
            if (playerTransform != null)
            {
                Transform target = standPosition != null ? standPosition : transform;

                // Si el PlayerRig tiene CameraOffset, alineamos la posición base
                playerTransform.position = target.position;

                // Orientar hacia el frente del escritorio
                Vector3 targetForward = target.forward;
                targetForward.y = 0f;
                if (targetForward.sqrMagnitude > 0.001f)
                {
                    playerTransform.rotation = Quaternion.LookRotation(targetForward);
                }

                Debug.Log($"{LogPrefix} Posición del Player actualizada a: {target.position}");
            }

            // Reproducir sonido espacial de teletransporte
            if (teleportAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(teleportAudioClip, standPosition.position);
            }

            // Notificar al controlador de flujo de la simulación
            if (ScenarioFlowController.I != null && !string.IsNullOrEmpty(associatedWorkstationId))
            {
                ScenarioFlowController.I.NotifyWorkstationInteracted(associatedWorkstationId);
            }

            // Breve espera en negro absoluto para asentamiento
            yield return new WaitForSecondsRealtime(0.02f);

            // 3. Apertura de vista (Salida del fundido)
            if (_sharedFaderImage != null)
            {
                float elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float alpha = 1f - Mathf.Clamp01(elapsed / halfDuration);
                    _sharedFaderImage.color = new Color(0f, 0f, 0f, alpha);
                    yield return null;
                }
                _sharedFaderImage.color = Color.clear;
                _sharedFaderImage.gameObject.SetActive(false);
            }

            _isTeleporting = false;
        }

        /// <summary>
        /// Localiza la raíz del jugador o la cámara principal para su traslación.
        /// </summary>
        private Transform ResolvePlayerRigTransform()
        {
            Camera cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam == null) return transform;

            // Si la cámara pertenece a un rig jerárquico (ej. Player / CameraOffset)
            if (cam.transform.parent != null)
            {
                // Subir hasta la raíz del rig si es posible
                Transform root = cam.transform.parent;
                while (root.parent != null && !root.CompareTag("Player"))
                {
                    if (root.parent.GetComponentInChildren<Camera>() == cam)
                    {
                        root = root.parent;
                    }
                    else
                    {
                        break;
                    }
                }
                return root;
            }

            return cam.transform;
        }

        /// <summary>
        /// Crea e inyecta dinámicamente un Canvas de confort visual frente a la cámara si no existe.
        /// </summary>
        private static void EnsureFaderOverlayExists()
        {
            if (_sharedFaderCanvas != null && _sharedFaderImage != null) return;

            Camera cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam == null) return;

            GameObject faderGO = new GameObject("VR_ComfortScreenFader");
            faderGO.transform.SetParent(cam.transform, false);

            _sharedFaderCanvas = faderGO.AddComponent<Canvas>();
            _sharedFaderCanvas.renderMode = RenderMode.WorldSpace;
            _sharedFaderCanvas.sortingOrder = 32767;

            RectTransform canvasRect = faderGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(4, 4);
            canvasRect.localPosition = new Vector3(0, 0, 0.12f); // 12 cm frente al lente
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one;

            GameObject imageGO = new GameObject("BlackOverlay");
            imageGO.transform.SetParent(faderGO.transform, false);

            _sharedFaderImage = imageGO.AddComponent<Image>();
            _sharedFaderImage.color = Color.clear;
            _sharedFaderImage.raycastTarget = false;

            RectTransform imgRect = imageGO.GetComponent<RectTransform>();
            imgRect.sizeDelta = new Vector2(6, 6);
            imgRect.localPosition = Vector3.zero;

            imageGO.SetActive(false);
        }
    }
}
