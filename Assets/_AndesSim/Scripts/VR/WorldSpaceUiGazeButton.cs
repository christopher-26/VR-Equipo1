using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AndesSim.VR
{
    /// <summary>
    /// Conector de mirada VR para botones de UI (UnityEngine.UI.Button) en Canvas World Space.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Ajusta automáticamente el BoxCollider a las dimensiones exactas del RectTransform del botón.
    /// - Asigna la capa "Interactive" (Layer 8) y enlaza GazeInteractable.
    /// - Al recibir OnPointerClick por retícula o dwell, invoca button.onClick con feedback visual y sonoro.
    /// - Permite operar el correo de Rasel Ahlam, el botón 'Reportar Phishing', interruptores de defensa y selector de modo.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Button))]
    public class WorldSpaceUiGazeButton : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - UI GazeButton]";
        private const string InteractiveLayerName = "Interactive";
        private const int InteractiveLayerIndex = 8;

        [Header("--- Parámetros del Colisionador 3D ---")]
        [Tooltip("Grosor en metros en el eje Z para el BoxCollider de interacción (0.02m por defecto).")]
        [SerializeField] private float colliderThickness = 0.02f;

        [Tooltip("Etiqueta personalizada para la retícula. Si se deja vacía, se extraerá automáticamente del texto del botón.")]
        [SerializeField] private string overrideInteractableLabel = string.Empty;

        [Header("--- Feedback Sonoro ---")]
        [Tooltip("AudioSource para emitir el sonido de clic y hover.")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Efecto de sonido al ejecutar clic con la mirada.")]
        [SerializeField] private AudioClip clickSoundClip;

        [Tooltip("Efecto de sonido al enfocar el botón.")]
        [SerializeField] private AudioClip hoverSoundClip;

        [Header("--- Feedback Visual ---")]
        [Tooltip("Efecto de compresión y rebote al pulsar.")]
        [SerializeField] private bool enablePunchScale = true;

        // Componentes cacheados
        private Button _button;
        private BoxCollider _boxCollider;
        private RectTransform _rectTransform;
        private GazeInteractable _gazeInteractable;
        private Vector3 _originalScale;
        private Coroutine _punchCoroutine;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _boxCollider = GetComponent<BoxCollider>();
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = transform.localScale;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            // 1. Ajustar dimensiones del BoxCollider al RectTransform
            UpdateColliderBounds();

            // 2. Asignar Capa "Interactive" (Layer 8)
            EnsureInteractiveLayer();

            // 3. Configurar GazeInteractable vinculado
            ConfigureGazeInteractable();
        }

        private void OnEnable()
        {
            UpdateColliderBounds();
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateColliderBounds();
        }

        /// <summary>
        /// Configura las referencias de audio y efectos de sonido para la interacción con la mirada.
        /// </summary>
        public void ConfigureAudio(AudioSource src, AudioClip clickClip, AudioClip hoverClip = null)
        {
            if (src != null) audioSource = src;
            if (clickClip != null) clickSoundClip = clickClip;
            if (hoverClip != null) hoverSoundClip = hoverClip;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateColliderBounds();
        }
#endif

        /// <summary>
        /// Sincroniza el tamaño y el centro del BoxCollider con el tamaño real y el pivote del RectTransform.
        /// </summary>
        public void UpdateColliderBounds()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_boxCollider == null) _boxCollider = GetComponent<BoxCollider>();

            if (_rectTransform != null && _boxCollider != null)
            {
                Rect r = _rectTransform.rect;
                _boxCollider.size = new Vector3(Mathf.Abs(r.width), Mathf.Abs(r.height), colliderThickness);

                // Compensar el pivote del RectTransform
                Vector2 pivot = _rectTransform.pivot;
                _boxCollider.center = new Vector3(
                    (0.5f - pivot.x) * r.width,
                    (0.5f - pivot.y) * r.height,
                    0f
                );
            }
        }

        private void EnsureInteractiveLayer()
        {
            int targetLayer = LayerMask.NameToLayer(InteractiveLayerName);
            gameObject.layer = (targetLayer != -1) ? targetLayer : InteractiveLayerIndex;
        }

        private void ConfigureGazeInteractable()
        {
            _gazeInteractable = GetComponent<GazeInteractable>();
            if (_gazeInteractable == null)
            {
                _gazeInteractable = gameObject.AddComponent<GazeInteractable>();
            }

            // Extraer texto descriptivo para la retícula si no fue especificado manualmente
            string label = overrideInteractableLabel;
            if (string.IsNullOrEmpty(label))
            {
                TMP_Text tmp = GetComponentInChildren<TMP_Text>();
                if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                {
                    label = tmp.text.Trim();
                }
                else
                {
                    Text legacyText = GetComponentInChildren<Text>();
                    if (legacyText != null && !string.IsNullOrEmpty(legacyText.text))
                    {
                        label = legacyText.text.Trim();
                    }
                    else
                    {
                        label = gameObject.name.Replace("Button", string.Empty).Replace("Btn", string.Empty).Trim();
                    }
                }
            }

            // Asignar el label directamente en GazeInteractable
            if (_gazeInteractable != null && !string.IsNullOrEmpty(label))
            {
                _gazeInteractable.interactableLabel = label;
            }

            // Suscribir eventos de mirada
            _gazeInteractable.onClick.RemoveListener(HandleGazePointerClick);
            _gazeInteractable.onClick.AddListener(HandleGazePointerClick);

            _gazeInteractable.onEnter.RemoveListener(HandleGazePointerEnter);
            _gazeInteractable.onEnter.AddListener(HandleGazePointerEnter);
        }

        /// <summary>
        /// Ejecutado cuando la retícula detona el clic (por permanencia o toque de pantalla).
        /// </summary>
        private void HandleGazePointerClick()
        {
            if (_button == null || !_button.interactable || !_button.isActiveAndEnabled) return;

            Debug.Log($"{LogPrefix} Clic por retícula VR procesado en botón: <color=#4EC9B0>'{gameObject.name}'</color>");

            // 1. Invocar el evento nativo del botón de Unity UI
            _button.onClick?.Invoke();

            // 2. Feedback sonoro
            PlayAudioFeedback(clickSoundClip);

            // 3. Feedback visual (Punch Scale)
            if (enablePunchScale && gameObject.activeInHierarchy)
            {
                if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
                _punchCoroutine = StartCoroutine(PunchScaleAnimation());
            }
        }

        private void HandleGazePointerEnter()
        {
            if (_button != null && _button.interactable)
            {
                PlayAudioFeedback(hoverSoundClip);
            }
        }

        private void PlayAudioFeedback(AudioClip clip)
        {
            if (clip == null) return;

            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, transform.position);
            }
        }

        private IEnumerator PunchScaleAnimation()
        {
            Vector3 compressedScale = _originalScale * 0.90f;
            float elapsed = 0f;
            float duration = 0.08f;

            // Compresión
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(_originalScale, compressedScale, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            // Rebote a tamaño original
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(compressedScale, _originalScale, elapsed / duration);
                yield return null;
            }

            transform.localScale = _originalScale;
            _punchCoroutine = null;
        }
    }
}
