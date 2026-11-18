using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AndesSim.Audio;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AndesSim.VR
{
    /// <summary>
    /// Componente de interacción por mirada (Gaze Interaction) para visores Google Cardboard XR y mobile VR.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Soporta llamadas nativas de CardboardReticlePointer (SendMessage: OnPointerEnter, OnPointerExit, OnPointerClick).
    /// - Implementa interfaces estándar de EventSystem (IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler).
    /// - Autoconfigura la Capa 8 ("Interactive") y asegura la existencia de un Collider activo.
    /// - Admite selección por permanencia de mirada (Dwell Time: 1.8s) y toques de pantalla (Input.GetMouseButtonDown / touch).
    /// - Efectos visuales de resaltado con restauración segura de materiales y emisiones.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GazeInteractable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const string LogPrefix = "[ANDES-SIM - GazeInteractable]";
        private const string InteractiveLayerName = "Interactive";
        private const int InteractiveLayerIndex = 8;

        /// <summary>
        /// Referencia global al objeto actualmente enfocado por la mirada del usuario.
        /// </summary>
        public static GazeInteractable CurrentHovered { get; private set; }
        public static event Action<GazeInteractable> OnGazeTargetChanged;

        [Header("--- Identificación e Interfaz ---")]
        [Tooltip("Texto descriptivo mostrado junto a la retícula VR al mirar el objeto (ej: 'Inspeccionar Monitor 01 - Carla Mendoza').")]
        public string interactableLabel = "Interactuar";

        [Header("--- Selección por Permanencia (Dwell Click) ---")]
        [Tooltip("Desactivado globalmente: todas las interacciones requieren clic explícito del jugador.")]
        [SerializeField] private bool enableDwellClick = false;

        [Tooltip("Tiempo de referencia (desactivado en modo solo-clic).")]
        [Range(0.5f, 5.0f)]
        [SerializeField] public float dwellTime = 1.2f;

        public float DwellTime { get => dwellTime; set => dwellTime = value; }

        [Header("--- Resaltado Visual (Feedback de Mirada) ---")]
        [Tooltip("Renderer 3D a resaltar al enfocar la mirada.")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("Componente gráfico UI (Canvas) opcional si el objeto es un elemento de interfaz.")]
        [SerializeField] private Graphic targetGraphic;

        [Tooltip("Color de tinte o emisión al enfocar la mirada.")]
        [SerializeField] private Color highlightColor = new Color(0.2f, 0.8f, 1.0f, 1.0f);

        [Tooltip("Activa emisión en el material (Shader Standard / URP Lit).")]
        [SerializeField] private bool enableEmissionHighlight = true;

        [SerializeField] private Color emissionHighlightColor = new Color(0.1f, 0.4f, 0.6f);

        [Header("--- Eventos de Interacción (UnityEvents) ---")]
        public UnityEvent onEnter = new UnityEvent();
        public UnityEvent onExit = new UnityEvent();
        public UnityEvent onClick = new UnityEvent();

        [Tooltip("Progreso normalizado del dwell timer (0.0 a 1.0), útil para animar retículas radiales.")]
        public UnityEvent<float> onDwellProgress = new UnityEvent<float>();

        // Estado interno
        private bool _isHovered = false;
        private float _dwellTimer = 0f;
        private float _exitGraceTimer = 0f;
        private const float ExitGracePeriod = 0.15f;

        // Control global anti-doble clic y anti-salto de etapas (1 clic físico = 1 acción)
        private static int _lastGlobalClickFrame = -1;
        private static float _lastGlobalClickUnscaledTime = -10f;
        private const float GlobalClickCooldown = 0.22f;

        // Caché de materiales y colores originales
        private Color _originalGraphicColor;
        private Color _originalMaterialColor;
        private Color _originalEmissionColor;
        private bool _hasEmissionColorKey = false;
        private Material _instancedMaterial;
        private Collider _collider;
        private Button _button;
        private CanvasGroup _canvasGroup;
        private bool _isExternallyInteractable = true;

        public string InteractableLabel { get => interactableLabel; set => interactableLabel = value; }
        public bool IsHovered => _isHovered;
        public bool EnableDwellClick => enableDwellClick;
        public float DwellProgress => enableDwellClick ? Mathf.Clamp01(_dwellTimer / Mathf.Max(0.1f, dwellTime)) : 0f;
        public bool IsInteractable =>
            _isExternallyInteractable &&
            enabled &&
            gameObject.activeInHierarchy &&
            (_collider == null || _collider.enabled) &&
            (_button == null || _button.interactable) &&
            (_canvasGroup == null || _canvasGroup.interactable);

        private void Awake()
        {
            // Forzar interacción exclusivamente por clic manual en todo el videojuego
            enableDwellClick = false;
            _collider = GetComponent<Collider>();
            _button = GetComponent<Button>();
            _canvasGroup = GetComponent<CanvasGroup>();
            EnsureInteractiveLayer();
            EnsureCollider();
            DisableDuplicateUiRaycastTargets();
            CacheOriginalAppearance();
        }

        private void OnEnable()
        {
            enableDwellClick = false;
            _dwellTimer = 0f;
            if (_collider == null) _collider = GetComponent<Collider>();
            if (_collider != null) _collider.enabled = _isExternallyInteractable;
        }

        public void SetInteractable(bool interactable)
        {
            _isExternallyInteractable = interactable;
            if (_collider == null) _collider = GetComponent<Collider>();
            if (_collider != null) _collider.enabled = interactable;

            if (!interactable && _isHovered)
            {
                OnPointerExit();
            }
        }

        public void RefreshOriginalAppearance()
        {
            if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();
            if (targetGraphic != null && !_isHovered)
            {
                _originalGraphicColor = targetGraphic.color;
            }
        }

        public void SetBaseGraphicColor(Color newBaseColor)
        {
            if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();
            _originalGraphicColor = newBaseColor;
            if (targetGraphic != null)
            {
                targetGraphic.color = (_isHovered && IsInteractable)
                    ? Color.Lerp(_originalGraphicColor, highlightColor, 0.5f)
                    : _originalGraphicColor;
            }
        }

        private void Update()
        {
            if (!_isHovered) return;

            if (_exitGraceTimer > 0f)
            {
                _exitGraceTimer -= Time.deltaTime;
                if (_exitGraceTimer <= 0f && !_isHovered)
                {
                    _dwellTimer = 0f;
                    onDwellProgress?.Invoke(0f);
                }
            }
        }

        private void OnDisable()
        {
            if (_isHovered)
            {
                OnPointerExit();
            }
        }

        #region Implementación para Google Cardboard XR (SendMessage nativo)

        /// <summary>
        /// Invocado por CardboardReticlePointer al enfocar el objeto con la retícula central.
        /// </summary>
        public void OnPointerEnter()
        {
            if (!IsInteractable || _isHovered) return;

            RefreshOriginalAppearance();
            _isHovered = true;
            if (_exitGraceTimer <= 0f)
            {
                _dwellTimer = 0f;
            }
            _exitGraceTimer = 0f;

            CurrentHovered = this;
            OnGazeTargetChanged?.Invoke(this);

            ApplyHighlight(true);
            onEnter?.Invoke();

            if (AudioManager.I != null)
            {
                AudioManager.I.PlayGazeHover();
            }

            Debug.Log($"{LogPrefix} Mirada enfocada en: <color=#9CDCFE>'{gameObject.name}'</color> [{interactableLabel}]");
        }

        /// <summary>
        /// Invocado por CardboardReticlePointer al desviar la mirada fuera del objeto.
        /// </summary>
        public void OnPointerExit()
        {
            if (!_isHovered) return;

            _isHovered = false;
            _exitGraceTimer = ExitGracePeriod;

            if (CurrentHovered == this)
            {
                CurrentHovered = null;
                OnGazeTargetChanged?.Invoke(null);
            }
            ApplyHighlight(false);
            onExit?.Invoke();
        }

        /// <summary>
        /// Invocado al presionar el gatillo de Cardboard, tocar la pantalla o expirar el tiempo de dwell.
        /// </summary>
        public void OnPointerClick()
        {
            TriggerClick();
        }

        #endregion

        #region Implementación de EventSystem (IPointerEnterHandler, etc.)

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnPointerClick();
        }

        #endregion

        /// <summary>
        /// Ejecuta el evento de clic con feedback en consola y protección global contra doble disparo o salto de etapa.
        /// </summary>
        private void TriggerClick()
        {
            if (!IsInteractable) return;

            if (_lastGlobalClickFrame == Time.frameCount ||
                (Time.unscaledTime - _lastGlobalClickUnscaledTime) < GlobalClickCooldown)
            {
                return;
            }

            _lastGlobalClickFrame = Time.frameCount;
            _lastGlobalClickUnscaledTime = Time.unscaledTime;

            CardboardReticleAdapter.NotifyClickExecuted();

            Debug.Log($"{LogPrefix} <color=#4EC9B0><b>CLIC EJECUTADO:</b></color> '{gameObject.name}' ({interactableLabel})");

            if (AudioManager.I != null)
            {
                AudioManager.I.PlayGazeClick();
            }

            onClick?.Invoke();
        }

        private void DisableDuplicateUiRaycastTargets()
        {
            // Si este objeto tiene un Button y un Collider 3D para GazeInteractable, desactivar raycastTarget
            // en sus gráficos hijos evita que GraphicRaycaster dispare un segundo clic paralelo al de CardboardReticleAdapter.
            if (GetComponent<Button>() == null || GetComponent<Collider>() == null) return;

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                {
                    graphics[i].raycastTarget = false;
                }
            }
        }

        #region Configuración de Capa, Colisionador y Materiales

        private void EnsureInteractiveLayer()
        {
            int targetLayer = LayerMask.NameToLayer(InteractiveLayerName);
            if (targetLayer != -1)
            {
                gameObject.layer = targetLayer;
            }
            else
            {
                gameObject.layer = InteractiveLayerIndex;
                Debug.LogWarning($"{LogPrefix} La capa '{InteractiveLayerName}' no fue encontrada en TagManager. Asignando índice forzado {InteractiveLayerIndex}.");
            }
        }

        private void EnsureCollider()
        {
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
                Debug.LogWarning($"{LogPrefix} '{gameObject.name}' no tenía Collider. Se agregó BoxCollider automáticamente.");
            }
            col.enabled = true;
        }

        private void CacheOriginalAppearance()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (targetRenderer != null)
            {
                _instancedMaterial = Application.isPlaying ? targetRenderer.material : targetRenderer.sharedMaterial;
                if (_instancedMaterial != null)
                {
                    if (_instancedMaterial.HasProperty("_Color"))
                    {
                        _originalMaterialColor = _instancedMaterial.color;
                    }
                    else if (_instancedMaterial.HasProperty("_BaseColor"))
                    {
                        _originalMaterialColor = _instancedMaterial.GetColor("_BaseColor");
                    }
                }

                if (_instancedMaterial.HasProperty("_EmissionColor"))
                {
                    _hasEmissionColorKey = true;
                    _originalEmissionColor = _instancedMaterial.GetColor("_EmissionColor");
                }
            }

            if (targetGraphic != null)
            {
                _originalGraphicColor = targetGraphic.color;
            }
        }

        private void ApplyHighlight(bool enable)
        {
            // Resaltado de Mesh / Material 3D
            if (_instancedMaterial != null)
            {
                if (enable)
                {
                    if (_instancedMaterial.HasProperty("_Color"))
                        _instancedMaterial.color = Color.Lerp(_originalMaterialColor, highlightColor, 0.45f);
                    else if (_instancedMaterial.HasProperty("_BaseColor"))
                        _instancedMaterial.SetColor("_BaseColor", Color.Lerp(_originalMaterialColor, highlightColor, 0.45f));

                    if (enableEmissionHighlight && _hasEmissionColorKey)
                    {
                        _instancedMaterial.EnableKeyword("_EMISSION");
                        _instancedMaterial.SetColor("_EmissionColor", emissionHighlightColor);
                    }
                }
                else
                {
                    if (_instancedMaterial.HasProperty("_Color"))
                        _instancedMaterial.color = _originalMaterialColor;
                    else if (_instancedMaterial.HasProperty("_BaseColor"))
                        _instancedMaterial.SetColor("_BaseColor", _originalMaterialColor);

                    if (_hasEmissionColorKey)
                    {
                        _instancedMaterial.SetColor("_EmissionColor", _originalEmissionColor);
                    }
                }
            }

            // Resaltado de UI Graphic
            if (targetGraphic != null)
            {
                targetGraphic.color = enable ? Color.Lerp(_originalGraphicColor, highlightColor, 0.5f) : _originalGraphicColor;
            }
        }

        #endregion
    }
}
