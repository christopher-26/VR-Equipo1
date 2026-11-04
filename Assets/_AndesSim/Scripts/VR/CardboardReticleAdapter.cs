using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AndesSim.VR
{
    /// <summary>
    /// Adaptador y controlador de retícula central para visores VR (Google Cardboard XR / Mobile VR).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Se adjunta a la 'Main Camera' del Player.
    /// - Emite un Raycast central continuo (Viewport 0.5, 0.5) hasta 15m sobre la capa "Interactive".
    /// - Detecta componentes GazeInteractable y gestiona OnPointerEnter / OnPointerExit / OnPointerClick.
    /// - Soporta pulsación por toque de pantalla (TouchPhase.Began / Clic) o por permanencia (Dwell Time).
    /// - Proyecta una retícula flotante con anillo de progreso radial y etiqueta descriptiva del objeto enfocado.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CardboardReticleAdapter : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - ReticleAdapter]";
        private const string InteractiveLayerName = "Interactive";
        private const int InteractiveLayerIndex = 8;

        [Header("--- Parámetros del Raycast de Mirada ---")]
        [Tooltip("Distancia máxima de interacción por mirada (15 metros según especificación).")]
        [Range(1f, 30f)]
        [SerializeField] private float maxDistance = 15f;

        [Tooltip("Distancia default de la retícula en reposo cuando no colisiona con ningún objeto interactivo.")]
        [SerializeField] private float defaultDistance = 2.5f;

        [Tooltip("Máscara de capas para la interacción (capa 'Interactive').")]
        [SerializeField] private LayerMask interactiveMask;

        [Header("--- Elementos Visuales de la Retícula (World Space) ---")]
        [Tooltip("Transform raíz del canvas flotante de la retícula.")]
        [SerializeField] private Transform reticleRoot;

        [Tooltip("Punto o círculo estático central de la retícula.")]
        [SerializeField] private Image reticleCenterDot;

        [Tooltip("Anillo circular de progreso de mirada (Image Type: Filled - Radial 360).")]
        [SerializeField] private Image reticleProgressRing;

        [Tooltip("Etiqueta flotante para mostrar el nombre del interactable enfocado.")]
        [SerializeField] private TMP_Text reticleLabelText;

        [Header("--- Estilo y Colores ---")]
        [SerializeField] private Color defaultReticleColor = new Color(1f, 1f, 1f, 0.75f);
        [SerializeField] private Color activeHoverColor = new Color(0.2f, 0.85f, 1.0f, 0.95f);
        [SerializeField] private Color progressFillColor = new Color(0.3f, 1.0f, 0.5f, 1.0f);

        // Componentes y estado interno
        private Camera _playerCamera;
        private GazeInteractable _currentInteractable;
        private RaycastHit _currentHit;
        private bool _isHittingInteractable;

        public GazeInteractable CurrentTarget => _currentInteractable;
        public bool IsHittingInteractable => _isHittingInteractable;

        private void Awake()
        {
            _playerCamera = GetComponent<Camera>();

            // Configurar máscara de capa "Interactive"
            int layerId = LayerMask.NameToLayer(InteractiveLayerName);
            if (layerId != -1)
            {
                interactiveMask = 1 << layerId;
            }
            else
            {
                interactiveMask = 1 << InteractiveLayerIndex;
            }

            // Construir o validar la UI flotante de la retícula
            EnsureReticleVisuals();
        }

        private void Update()
        {
            PerformCentralGazeRaycast();
            HandleInputTrigger();
            UpdateReticleVisuals();
        }

        /// <summary>
        /// Ejecuta el raycast continuo desde el centro de la cámara hacia la capa Interactive.
        /// </summary>
        private void PerformCentralGazeRaycast()
        {
            Ray centerRay = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(centerRay, out RaycastHit hit, maxDistance, interactiveMask))
            {
                _currentHit = hit;
                _isHittingInteractable = true;

                GazeInteractable interactable = hit.collider.GetComponentInParent<GazeInteractable>();

                if (interactable != _currentInteractable)
                {
                    // Salir del objetivo anterior
                    if (_currentInteractable != null)
                    {
                        _currentInteractable.OnPointerExit();
                    }

                    // Entrar en el nuevo objetivo
                    _currentInteractable = interactable;
                    if (_currentInteractable != null)
                    {
                        _currentInteractable.OnPointerEnter();
                    }
                }
            }
            else
            {
                _isHittingInteractable = false;

                if (_currentInteractable != null)
                {
                    _currentInteractable.OnPointerExit();
                    _currentInteractable = null;
                }
            }
        }

        /// <summary>
        /// Detecta si el jugador toca la pantalla o pulsa el disparador de Cardboard.
        /// Compatible con New Input System e Input Manager legacy.
        /// </summary>
        private void HandleInputTrigger()
        {
            if (_currentInteractable == null) return;

            bool isTriggered = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) isTriggered = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) isTriggered = true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0)) isTriggered = true;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began) isTriggered = true;
#endif

            if (isTriggered)
            {
                Debug.Log($"{LogPrefix} Input manual detectado sobre '{_currentInteractable.InteractableLabel}'.");
                _currentInteractable.OnPointerClick();
            }
        }

        /// <summary>
        /// Posiciona la retícula en el espacio 3D y actualiza el anillo de progreso y la etiqueta.
        /// </summary>
        private void UpdateReticleVisuals()
        {
            if (reticleRoot == null) return;

            Ray centerRay = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (_isHittingInteractable)
            {
                // Posicionar ligeramente separada de la superficie impactada
                reticleRoot.position = _currentHit.point - (centerRay.direction * 0.04f);
                reticleRoot.rotation = Quaternion.LookRotation(centerRay.direction);

                // Escalar con la distancia para mantener tamaño angular constante
                float distance = Vector3.Distance(_playerCamera.transform.position, _currentHit.point);
                float scaleFactor = Mathf.Clamp(distance * 0.015f, 0.01f, 0.4f);
                reticleRoot.localScale = Vector3.one * scaleFactor;

                // Actualizar textos y colores
                if (_currentInteractable != null)
                {
                    if (reticleLabelText != null)
                    {
                        reticleLabelText.text = _currentInteractable.InteractableLabel;
                        reticleLabelText.gameObject.SetActive(!string.IsNullOrEmpty(_currentInteractable.InteractableLabel));
                    }

                    if (reticleProgressRing != null)
                    {
                        reticleProgressRing.fillAmount = _currentInteractable.DwellProgress;
                        reticleProgressRing.color = progressFillColor;
                    }

                    if (reticleCenterDot != null)
                    {
                        reticleCenterDot.color = activeHoverColor;
                    }
                }
            }
            else
            {
                // Posición de reposo en la distancia por defecto
                reticleRoot.position = centerRay.origin + (centerRay.direction * defaultDistance);
                reticleRoot.rotation = Quaternion.LookRotation(centerRay.direction);

                float scaleFactor = defaultDistance * 0.015f;
                reticleRoot.localScale = Vector3.one * scaleFactor;

                if (reticleLabelText != null)
                {
                    reticleLabelText.text = string.Empty;
                    reticleLabelText.gameObject.SetActive(false);
                }

                if (reticleProgressRing != null)
                {
                    reticleProgressRing.fillAmount = 0f;
                }

                if (reticleCenterDot != null)
                {
                    reticleCenterDot.color = defaultReticleColor;
                }
            }
        }

        #region Generación Procedural y Resiliente de la Retícula

        /// <summary>
        /// Asegura que existan los objetos y componentes de la retícula en la escena; los crea automáticamente si no existen.
        /// </summary>
        private void EnsureReticleVisuals()
        {
            if (reticleRoot != null) return;

            // 1. Crear Canvas en World Space
            GameObject canvasGO = new GameObject("CardboardReticleCanvas");
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 999;

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200, 200);
            reticleRoot = canvasGO.transform;

            // 2. Sprite procedural de anillo y punto
            Sprite circleSprite = GenerateCircleSprite(64, isRing: false);
            Sprite ringSprite = GenerateCircleSprite(64, isRing: true);

            // 3. Crear Punto Central
            GameObject dotGO = new GameObject("ReticleCenterDot");
            dotGO.transform.SetParent(reticleRoot, false);
            reticleCenterDot = dotGO.AddComponent<Image>();
            reticleCenterDot.sprite = circleSprite;
            reticleCenterDot.color = defaultReticleColor;
            RectTransform dotRect = dotGO.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(8, 8);

            // 4. Crear Anillo de Progreso (Radial 360)
            GameObject ringGO = new GameObject("ReticleProgressRing");
            ringGO.transform.SetParent(reticleRoot, false);
            reticleProgressRing = ringGO.AddComponent<Image>();
            reticleProgressRing.sprite = ringSprite;
            reticleProgressRing.type = Image.Type.Filled;
            reticleProgressRing.fillMethod = Image.FillMethod.Radial360;
            reticleProgressRing.fillOrigin = (int)Image.Origin360.Top;
            reticleProgressRing.fillClockwise = true;
            reticleProgressRing.fillAmount = 0f;
            reticleProgressRing.color = progressFillColor;
            RectTransform ringRect = ringGO.GetComponent<RectTransform>();
            ringRect.sizeDelta = new Vector2(28, 28);

            // 5. Crear Etiqueta TextMeshPro
            GameObject textGO = new GameObject("ReticleLabel");
            textGO.transform.SetParent(reticleRoot, false);
            reticleLabelText = textGO.AddComponent<TextMeshProUGUI>();
            reticleLabelText.fontSize = 18;
            reticleLabelText.alignment = TextAlignmentOptions.Center;
            reticleLabelText.color = Color.white;
            reticleLabelText.outlineWidth = 0.2f;
            reticleLabelText.outlineColor = Color.black;
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(250, 40);
            textRect.anchoredPosition = new Vector2(0, -28);

            reticleLabelText.text = string.Empty;
            reticleLabelText.gameObject.SetActive(false);

            Debug.Log($"{LogPrefix} Canvas de retícula World Space generado proceduralmente con éxito.");
        }

        /// <summary>
        /// Genera en memoria un Sprite circular suave y anti-aliasing sin requerir archivos externos de texturas.
        /// </summary>
        private Sprite GenerateCircleSprite(int size, bool isRing)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            float radius = size * 0.5f;
            float innerRadius = isRing ? radius * 0.70f : 0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));

                    if (dist <= radius && dist >= innerRadius)
                    {
                        float alpha = Mathf.Clamp01((radius - dist) * 1.5f);
                        if (isRing)
                        {
                            alpha *= Mathf.Clamp01((dist - innerRadius) * 1.5f);
                        }
                        colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(colors);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        #endregion
    }
}
