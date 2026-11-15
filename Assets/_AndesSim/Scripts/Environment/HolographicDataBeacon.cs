using UnityEngine;

namespace AndesSim.Environment
{
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
        [SerializeField] private Transform labelTransform;
        [SerializeField] private HoloBeaconState currentState = HoloBeaconState.NormalCyan;

        private MaterialPropertyBlock _mpb;
        private Camera _cachedCam;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            if (labelTransform == null)
            {
                labelTransform = transform.Find("Canvas_Tag");
            }
        }

        private void LateUpdate()
        {
            if (labelTransform == null) return;
            if (_cachedCam == null || !_cachedCam.isActiveAndEnabled)
            {
                _cachedCam = Camera.main;
            }
            if (_cachedCam == null) return;

            // Orientar el frente legible (-forward hacia la camara -> forward apuntando desde la camara hacia el tag)
            Vector3 dirFromCam = labelTransform.position - _cachedCam.transform.position;
            dirFromCam.y = 0f;
            if (dirFromCam.sqrMagnitude > 0.0004f)
            {
                labelTransform.rotation = Quaternion.LookRotation(dirFromCam, Vector3.up);
            }
        }

        public void Bind(Renderer[] targetRenderers, HolographicOrbitRing[] targetRings, HoloBeaconState initialState = HoloBeaconState.NormalCyan, Transform targetLabel = null)
        {
            coloredRenderers = targetRenderers;
            rings = targetRings;
            if (targetLabel != null)
            {
                labelTransform = targetLabel;
            }
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
