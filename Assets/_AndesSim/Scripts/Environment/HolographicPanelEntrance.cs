using UnityEngine;

namespace AndesSim.Environment
{
    /// <summary>
    /// Animación elegante de entrada EaseOutBack + elevación vertical + fade CanvasGroup al mostrarse el panel VR.
    /// </summary>
    public class HolographicPanelEntrance : MonoBehaviour
    {
        [SerializeField] private float duration = 0.14f;
        [SerializeField] private float startScaleFactor = 1f;
        [SerializeField] private float riseOffsetY = 0f;
        [SerializeField] private CanvasGroup canvasGroup;

        private Vector3 _restLocalPos;
        private Vector3 _restLocalScale;
        private bool _poseCaptured;
        private float _elapsed;
        private bool _animating;

        public bool IsAnimating => _animating;

        public void Configure(float animDuration = 0.14f, float initialScale = 1f, float startRiseY = 0f, CanvasGroup group = null)
        {
            duration = Mathf.Max(0.06f, animDuration);
            startScaleFactor = 1f;
            riseOffsetY = 0f;
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
            // Transición limpia de opacidad sin rebotes ni desplazamientos verticales
            float easeCubic = 1f - Mathf.Pow(1f - u, 3f);
            float scale = Mathf.Lerp(startScaleFactor, 1f, easeCubic);
            transform.localScale = _restLocalScale * scale;
            transform.localPosition = _restLocalPos + new Vector3(0f, -riseOffsetY * (1f - easeCubic), 0f);
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(easeCubic);
        }
    }
}
