using System;
using UnityEngine;

namespace AndesSim.Environment
{
    /// <summary>
    /// Animación limpia y secuencial de entrada/salida por opacidad (CanvasGroup) sin rebotes ni superposición.
    /// </summary>
    public class HolographicPanelEntrance : MonoBehaviour
    {
        [SerializeField] private float duration = 0.16f;
        [SerializeField] private float startScaleFactor = 1f;
        [SerializeField] private float riseOffsetY = 0f;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool playOnEnable = true;

        private Vector3 _restLocalPos;
        private Vector3 _restLocalScale;
        private bool _poseCaptured;
        private float _elapsed;
        private float _activeDuration = 0.16f;
        private float _fromAlpha = 0f;
        private float _toAlpha = 1f;
        private bool _animating;
        private Action _onComplete;

        public bool IsAnimating => _animating;

        public void SetPlayOnEnable(bool enabled)
        {
            playOnEnable = enabled;
        }

        public void Configure(float animDuration = 0.16f, float initialScale = 1f, float startRiseY = 0f, CanvasGroup group = null)
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
            PlayEntrance(null);
        }

        public void PlayEntrance(Action onComplete)
        {
            StartFade(0f, 1f, duration, onComplete);
        }

        public void PlayExit(float exitDuration, Action onComplete)
        {
            float currentAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
            StartFade(currentAlpha, 0f, Mathf.Max(0.06f, exitDuration), onComplete);
        }

        private void StartFade(float fromAlpha, float toAlpha, float animDuration, Action onComplete)
        {
            if (!_poseCaptured) CaptureRestPose();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            _elapsed = 0f;
            _activeDuration = Mathf.Max(0.06f, animDuration);
            _fromAlpha = fromAlpha;
            _toAlpha = toAlpha;
            _onComplete = onComplete;
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
            if (playOnEnable) PlayEntrance();
        }

        private void OnDisable()
        {
            _animating = false;
            _onComplete = null;
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
            float u = Mathf.Clamp01(_elapsed / _activeDuration);
            ApplyFrame(u);
            if (u >= 1f)
            {
                _animating = false;
                Action cb = _onComplete;
                _onComplete = null;
                cb?.Invoke();
            }
        }

        private void ApplyFrame(float u)
        {
            float easeCubic = _toAlpha >= _fromAlpha
                ? (1f - Mathf.Pow(1f - u, 3f))
                : (u * u * (3f - 2f * u));
            float scale = Mathf.Lerp(startScaleFactor, 1f, easeCubic);
            transform.localScale = _restLocalScale * scale;
            transform.localPosition = _restLocalPos + new Vector3(0f, -riseOffsetY * (1f - easeCubic), 0f);
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(Mathf.Lerp(_fromAlpha, _toAlpha, easeCubic));
        }
    }
}
