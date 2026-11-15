using UnityEngine;

namespace AndesSim.Environment
{
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
}
