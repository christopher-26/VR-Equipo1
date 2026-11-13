using UnityEngine;
using UnityEngine.EventSystems;
using AndesSim.VR;

namespace AndesSim.Environment
{
    /// <summary>
    /// Levitación holográfica suave sin asignaciones por cuadro (0 GC).
    /// Ideal para menús flotantes, carteles 3D, íconos de estación, tokens e insignias VR.
    /// </summary>
    public class HolographicFloatMotion : MonoBehaviour
    {
        [Header("Levitación Vertical y Balanceo")]
        [SerializeField] private float amplitude = 0.036f;
        [SerializeField] private float frequency = 0.95f;
        [SerializeField] private Vector3 swayAngles = new Vector3(0.8f, 1.4f, 0.6f);
        [SerializeField] private float swayFrequency = 0.60f;
        [SerializeField] private float spinYDegreesPerSec = 0f;

        [Header("Respiración de Escala (Opcional)")]
        [SerializeField] private float scalePulseAmount = 0f;
        [SerializeField] private float scalePulseSpeed = 1.4f;

        [Header("Tiempo y Fase")]
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool autoSeedPhaseFromPosition = true;
        [SerializeField] private float phaseOffset = 0f;

        private Vector3 _baseLocalPos;
        private Quaternion _baseLocalRot;
        private Vector3 _baseLocalScale;
        private bool _poseCaptured;
        private float _effectivePhase;
        private HolographicPanelEntrance _coLocatedEntrance;

        public void Configure(
            float floatAmplitude,
            float floatFrequency,
            Vector3 tiltAngles,
            float spinYDegPerSec = 0f,
            float pulseScale = 0f,
            bool unscaled = true)
        {
            amplitude = floatAmplitude;
            frequency = floatFrequency;
            swayAngles = tiltAngles;
            spinYDegreesPerSec = spinYDegPerSec;
            scalePulseAmount = pulseScale;
            useUnscaledTime = unscaled;
            CaptureCurrentRestPose();
        }

        public void CaptureCurrentRestPose()
        {
            _baseLocalPos = transform.localPosition;
            _baseLocalRot = transform.localRotation;
            _baseLocalScale = transform.localScale;
            _poseCaptured = true;
            _effectivePhase = autoSeedPhaseFromPosition
                ? (_baseLocalPos.x * 1.13f + _baseLocalPos.z * 0.77f + phaseOffset)
                : phaseOffset;
        }

        private void Awake()
        {
            if (!_poseCaptured) CaptureCurrentRestPose();
            _coLocatedEntrance = GetComponent<HolographicPanelEntrance>();
        }

        private void OnDisable()
        {
            if (_poseCaptured)
            {
                transform.localPosition = _baseLocalPos;
                transform.localRotation = _baseLocalRot;
                transform.localScale = _baseLocalScale;
            }
        }

        private void Update()
        {
            if (!_poseCaptured) CaptureCurrentRestPose();
            if (_coLocatedEntrance != null && _coLocatedEntrance.IsAnimating) return;

            float t = (useUnscaledTime ? Time.unscaledTime : Time.time) + _effectivePhase;

            float yOffset = Mathf.Sin(t * frequency * Mathf.PI * 2f) * amplitude;
            transform.localPosition = new Vector3(_baseLocalPos.x, _baseLocalPos.y + yOffset, _baseLocalPos.z);

            float swayOmega = t * swayFrequency * Mathf.PI * 2f;
            float rx = Mathf.Sin(swayOmega) * swayAngles.x;
            float ry = Mathf.Cos(swayOmega * 0.78f) * swayAngles.y + (spinYDegreesPerSec * t);
            float rz = Mathf.Sin(swayOmega * 0.62f + 1.1f) * swayAngles.z;
            transform.localRotation = _baseLocalRot * Quaternion.Euler(rx, ry, rz);

            if (scalePulseAmount > 0.0001f)
            {
                float s = 1f + Mathf.Sin(t * scalePulseSpeed * Mathf.PI * 2f) * scalePulseAmount;
                transform.localScale = _baseLocalScale * s;
            }
        }
    }

    /// <summary>
    /// Anillo orbital holográfico con rotación continua y pulso emisivo vía MaterialPropertyBlock (sin clonar materiales).
    /// </summary>
    public class HolographicOrbitRing : MonoBehaviour
    {
        [SerializeField] private Vector3 spinEulerPerSec = new Vector3(0f, 36f, 0f);
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Pulso Emisivo (Opcional)")]
        [SerializeField] private Renderer pulseRenderer;
        [SerializeField] private Color baseColor = new Color(0f, 0.90f, 1f, 0.85f);
        [SerializeField] private float minEmission = 0.8f;
        [SerializeField] private float maxEmission = 2.4f;
        [SerializeField] private float pulseSpeed = 2.1f;

        private MaterialPropertyBlock _mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public void Configure(Vector3 eulerSpeed, Renderer targetRenderer, Color ringColor, float minGlow = 1.0f, float maxGlow = 2.6f, float glowSpeed = 2.2f)
        {
            spinEulerPerSec = eulerSpeed;
            pulseRenderer = targetRenderer;
            baseColor = ringColor;
            minEmission = minGlow;
            maxEmission = maxGlow;
            pulseSpeed = glowSpeed;
            ApplyGlow(maxGlow);
        }

        public void SetColorAndSpeed(Color ringColor, Vector3 eulerSpeed)
        {
            baseColor = ringColor;
            spinEulerPerSec = eulerSpeed;
        }

        private void Awake()
        {
            if (pulseRenderer == null) pulseRenderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (spinEulerPerSec.sqrMagnitude > 0.001f)
            {
                transform.Rotate(spinEulerPerSec * dt, Space.Self);
            }

            if (pulseRenderer != null)
            {
                float t = useUnscaledTime ? Time.unscaledTime : Time.time;
                float wave = (Mathf.Sin(t * pulseSpeed * Mathf.PI) + 1f) * 0.5f;
                ApplyGlow(Mathf.Lerp(minEmission, maxEmission, wave));
            }
        }

        private void ApplyGlow(float mult)
        {
            if (pulseRenderer == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            pulseRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, baseColor);
            _mpb.SetColor(ColorId, baseColor);
            _mpb.SetColor(EmissionColorId, baseColor * mult);
            pulseRenderer.SetPropertyBlock(_mpb);
        }
    }

    /// <summary>
    /// Animación elegante de entrada EaseOutBack + elevación vertical + fade CanvasGroup al mostrarse el panel VR.
    /// </summary>
    public class HolographicPanelEntrance : MonoBehaviour
    {
        [SerializeField] private float duration = 0.28f;
        [SerializeField] private float startScaleFactor = 0.86f;
        [SerializeField] private float riseOffsetY = 0.045f;
        [SerializeField] private CanvasGroup canvasGroup;

        private Vector3 _restLocalPos;
        private Vector3 _restLocalScale;
        private bool _poseCaptured;
        private float _elapsed;
        private bool _animating;

        public bool IsAnimating => _animating;

        public void Configure(float animDuration = 0.28f, float initialScale = 0.86f, float startRiseY = 0.045f, CanvasGroup group = null)
        {
            duration = Mathf.Max(0.08f, animDuration);
            startScaleFactor = initialScale;
            riseOffsetY = startRiseY;
            if (group != null) canvasGroup = group;
            CaptureRestPose();
        }

        public void CaptureRestPose()
        {
            _restLocalPos = transform.localPosition;
            _restLocalScale = transform.localScale;
            _poseCaptured = true;
        }

        public void PlayEntrance()
        {
            if (!_poseCaptured) CaptureRestPose();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            _elapsed = 0f;
            _animating = true;
            ApplyFrame(0f);
        }

        private void Awake()
        {
            if (!_poseCaptured) CaptureRestPose();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            PlayEntrance();
        }

        private void OnDisable()
        {
            _animating = false;
            if (_poseCaptured)
            {
                transform.localPosition = _restLocalPos;
                transform.localScale = _restLocalScale;
            }
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            if (!_animating) return;
            _elapsed += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(_elapsed / duration);
            ApplyFrame(u);
            if (u >= 1f) _animating = false;
        }

        private void ApplyFrame(float u)
        {
            // EaseOutBack suave para sensación holográfica reactiva
            const float c1 = 1.40f;
            const float c3 = c1 + 1f;
            float um1 = u - 1f;
            float easeBack = 1f + c3 * um1 * um1 * um1 + c1 * um1 * um1;
            float easeCubic = 1f - Mathf.Pow(1f - u, 3f);

            float s = Mathf.LerpUnclamped(startScaleFactor, 1f, easeBack);
            transform.localScale = _restLocalScale * s;
            transform.localPosition = _restLocalPos + new Vector3(0f, -riseOffsetY * (1f - easeCubic), 0f);
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(easeCubic * 1.15f);
        }
    }

    /// <summary>
    /// Reacción táctil de tarjetas UI en Gaze y Pointer: elevación y escala suave al mirar.
    /// </summary>
    public class HolographicCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float hoverScale = 1.045f;
        [SerializeField] private float smoothSpeed = 12f;

        private Vector3 _restScale;
        private bool _poseCaptured;
        private bool _isHovered;
        private GazeInteractable _gazeInteractable;

        public void Configure(float targetHoverScale = 1.045f, float lerpSpeed = 12f)
        {
            hoverScale = targetHoverScale;
            smoothSpeed = lerpSpeed;
            CaptureRestScale();
        }

        public void CaptureRestScale()
        {
            _restScale = transform.localScale;
            _poseCaptured = true;
        }

        private void Awake()
        {
            if (!_poseCaptured) CaptureRestScale();
            _gazeInteractable = GetComponent<GazeInteractable>();
        }

        private void OnEnable()
        {
            if (_gazeInteractable == null) _gazeInteractable = GetComponent<GazeInteractable>();
            if (_gazeInteractable != null)
            {
                _gazeInteractable.onEnter.RemoveListener(HandleGazeEnter);
                _gazeInteractable.onEnter.AddListener(HandleGazeEnter);
                _gazeInteractable.onExit.RemoveListener(HandleGazeExit);
                _gazeInteractable.onExit.AddListener(HandleGazeExit);
            }
        }

        private void OnDisable()
        {
            _isHovered = false;
            if (_gazeInteractable != null)
            {
                _gazeInteractable.onEnter.RemoveListener(HandleGazeEnter);
                _gazeInteractable.onExit.RemoveListener(HandleGazeExit);
            }
            if (_poseCaptured) transform.localScale = _restScale;
        }

        private void HandleGazeEnter() => _isHovered = true;
        private void HandleGazeExit() => _isHovered = false;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        private void Update()
        {
            if (!_poseCaptured) return;
            Vector3 target = _isHovered ? _restScale * hoverScale : _restScale;
            if ((transform.localScale - target).sqrMagnitude > 0.000004f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * smoothSpeed);
            }
            else if (transform.localScale != target)
            {
                transform.localScale = target;
            }
        }
    }

    public enum HoloBeaconState
    {
        NormalCyan,
        AlertRed,
        DefenseEmerald,
        AmberWarning
    }

    /// <summary>
    /// Baliza holográfica 3D flotante sobre estaciones y consolas con estado cromático dinámico (0 GC).
    /// </summary>
    public class HolographicDataBeacon : MonoBehaviour
    {
        [SerializeField] private Renderer[] coloredRenderers;
        [SerializeField] private HolographicOrbitRing[] rings;
        [SerializeField] private HoloBeaconState currentState = HoloBeaconState.NormalCyan;

        private MaterialPropertyBlock _mpb;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public void Bind(Renderer[] targetRenderers, HolographicOrbitRing[] targetRings, HoloBeaconState initialState = HoloBeaconState.NormalCyan)
        {
            coloredRenderers = targetRenderers;
            rings = targetRings;
            SetVisualState(initialState);
        }

        public void SetVisualState(HoloBeaconState state)
        {
            currentState = state;
            Color targetColor;
            float spinMult;

            switch (state)
            {
                case HoloBeaconState.AlertRed:
                    targetColor = new Color(1.0f, 0.353f, 0.306f, 0.92f);
                    spinMult = 2.4f;
                    break;
                case HoloBeaconState.DefenseEmerald:
                    targetColor = new Color(0.220f, 0.949f, 0.541f, 0.92f);
                    spinMult = 1.4f;
                    break;
                case HoloBeaconState.AmberWarning:
                    targetColor = new Color(1.0f, 0.663f, 0.251f, 0.92f);
                    spinMult = 1.8f;
                    break;
                default:
                    targetColor = new Color(0.0f, 0.898f, 1.0f, 0.88f);
                    spinMult = 1.0f;
                    break;
            }

            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            if (coloredRenderers != null)
            {
                for (int i = 0; i < coloredRenderers.Length; i++)
                {
                    Renderer r = coloredRenderers[i];
                    if (r == null) continue;
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetColor(BaseColorId, targetColor);
                    _mpb.SetColor(ColorId, targetColor);
                    _mpb.SetColor(EmissionColorId, targetColor * (state == HoloBeaconState.AlertRed ? 2.6f : 1.9f));
                    r.SetPropertyBlock(_mpb);
                }
            }

            if (rings != null)
            {
                for (int i = 0; i < rings.Length; i++)
                {
                    if (rings[i] == null) continue;
                    float dir = (i % 2 == 0) ? 1f : -1f;
                    rings[i].SetColorAndSpeed(targetColor, new Vector3(14f * dir * spinMult, 42f * dir * spinMult, 9f * spinMult));
                }
            }
        }
    }
}
