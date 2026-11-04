using UnityEngine;

namespace AndesSim.Environment
{
    /// <summary>
    /// Componente de animación ligero para el efecto decorativo de lluvia de código SWIFT en el ventanal.
    /// Optimizado al 100% para móviles (OpenGLES3 - 60 FPS) mediante desplazamiento continuo de UVs.
    /// </summary>
    public class SwiftCodeRainAnimator : MonoBehaviour
    {
        [Tooltip("Velocidad de descenso de la lluvia de código SWIFT.")]
        [SerializeField] private float scrollSpeedY = -0.12f;

        private Renderer _renderer;
        private Material _materialInstance;
        private Vector2 _currentOffset = Vector2.zero;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _materialInstance = Application.isPlaying ? _renderer.material : _renderer.sharedMaterial;
            }
        }

        private void Update()
        {
            if (_materialInstance != null)
            {
                _currentOffset.y += scrollSpeedY * Time.deltaTime;
                _materialInstance.mainTextureOffset = _currentOffset;
            }
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }
    }
}
