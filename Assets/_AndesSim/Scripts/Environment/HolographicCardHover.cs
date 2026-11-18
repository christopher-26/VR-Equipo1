using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AndesSim.VR;

namespace AndesSim.Environment
{
    /// <summary>
    /// Reacción táctil de tarjetas UI en Gaze y Pointer: escala suave al apuntar cuando el botón está disponible.
    /// </summary>
    public class HolographicCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float hoverScale = 1.045f;
        [SerializeField] private float smoothSpeed = 12f;

        private Vector3 _restScale;
        private bool _poseCaptured;
        private bool _isHovered;
        private GazeInteractable _gazeInteractable;
        private Button _button;

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
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_gazeInteractable == null) _gazeInteractable = GetComponent<GazeInteractable>();
            if (_button == null) _button = GetComponent<Button>();
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

        private bool CanHover()
        {
            if (_button != null && !_button.interactable) return false;
            if (_gazeInteractable != null && !_gazeInteractable.IsInteractable) return false;
            return true;
        }

        private void HandleGazeEnter() => _isHovered = CanHover();
        private void HandleGazeExit() => _isHovered = false;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = CanHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        private void Update()
        {
            if (!_poseCaptured) return;
            if (_isHovered && !CanHover()) _isHovered = false;

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
}
