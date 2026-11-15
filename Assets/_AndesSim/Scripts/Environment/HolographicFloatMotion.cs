using UnityEngine;

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
}
