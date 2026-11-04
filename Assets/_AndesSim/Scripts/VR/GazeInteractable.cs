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
        [Tooltip("Activa la selección automática al mantener la mirada fija durante el tiempo de dwell.")]
        [SerializeField] private bool enableDwellClick = true;

        [Tooltip("Tiempo en segundos que el jugador debe mantener la mirada para detonar el clic (1.8 segundos por defecto).")]
        [Range(0.5f, 5.0f)]
        [SerializeField] private float dwellTime = 1.8f;

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
        private bool _hasClickedThisHover = false;
        private float _dwellTimer = 0f;

        // Caché de materiales y colores originales
        private Color _originalGraphicColor;
        private Color _originalMaterialColor;
        private Color _originalEmissionColor;
        private bool _hasEmissionColorKey = false;
        private Material _instancedMaterial;

        public string InteractableLabel { get => interactableLabel; set => interactableLabel = value; }
        public bool IsHovered => _isHovered;
        public float DwellProgress => enableDwellClick ? Mathf.Clamp01(_dwellTimer / dwellTime) : 0f;

        private void Awake()
        {
            EnsureInteractiveLayer();
            EnsureCollider();
            CacheOriginalAppearance();
        }

        private void Update()
        {
            if (!_isHovered) return;

            // 1. Detección de toque directo en pantalla o gatillo magnético/capacitivo de Cardboard
            bool screenTapped = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) screenTapped = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) screenTapped = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!screenTapped)
            {
                try
                {
                    if (Input.GetMouseButtonDown(0)) screenTapped = true;
                    if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began) screenTapped = true;
                }
                catch { }
            }
#endif

            if (screenTapped)
            {
                TriggerClick();
                return;
            }

            // 2. Conteo por permanencia de mirada (Dwell Time: 1.8s)
            if (enableDwellClick && !_hasClickedThisHover)
            {
                _dwellTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_dwellTimer / dwellTime);
                onDwellProgress?.Invoke(progress);

                if (_dwellTimer >= dwellTime)
                {
                    _hasClickedThisHover = true;
                    TriggerClick();
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
            if (_isHovered) return;

            _isHovered = true;
            _hasClickedThisHover = false;
            _dwellTimer = 0f;

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
            _hasClickedThisHover = false;
            _dwellTimer = 0f;

            if (CurrentHovered == this)
            {
                CurrentHovered = null;
                OnGazeTargetChanged?.Invoke(null);
            }

            onDwellProgress?.Invoke(0f);
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
        /// Ejecuta el evento de clic con feedback en consola y reinicio controlado de estado.
        /// </summary>
        private void TriggerClick()
        {
            Debug.Log($"{LogPrefix} <color=#4EC9B0><b>CLIC POR MIRADA EJECUTADO:</b></color> '{gameObject.name}' ({interactableLabel})");

            if (AudioManager.I != null)
            {
                AudioManager.I.PlayGazeClick();
            }

            onClick?.Invoke();
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
