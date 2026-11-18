using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AndesSim.VR
{
    /// <summary>
    /// Simulador de cámara VR para pruebas en el Editor de Unity y fallback de giroscopio en Android.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Se adjunta al objeto 'Player' o directamente a la 'Main Camera'.
    /// 
    /// Comportamiento:
    /// - En UNITY_EDITOR:
    ///   * Clic derecho o tecla 'Alt' + movimiento del mouse: Rota la cabeza en 360 grados (Pitch y Yaw).
    ///   * Tecla 'Ctrl' + movimiento horizontal del mouse: Inclina el cuello (Roll).
    ///   * Tecla 'M': Alterna rápidamente entre SimMode.Ataque y SimMode.Defensa para pruebas ágiles.
    /// - En UNITY_ANDROID (Build real):
    ///   * Cede el control a Google Cardboard XR cuando el tracking de XR está activo.
    ///   * Activa Input.gyro como fallback si la simulación corre en modo ventana antes de ingresar a VR.
    /// </summary>
    public class EditorVrCameraSimulator : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - VR Simulator]";

        [Header("--- Sensibilidad en Editor (Mouse / Alt / Ctrl) ---")]
        [Tooltip("Velocidad de giro horizontal y vertical.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float mouseSensitivity = 2.8f;

        [Tooltip("Sensibilidad para la inclinación del cuello con tecla Ctrl.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float rollSensitivity = 2.0f;

        [Tooltip("Invierte el eje vertical (Pitch).")]
        [SerializeField] private bool invertPitch = false;

        [Header("--- Fallback Android (Giroscopio sin XR) ---")]
        [Tooltip("Habilita Input.gyro si el plugin de Cardboard XR no está activo en el inicio.")]
        [SerializeField] private bool enableGyroFallback = true;

        // Ángulos de rotación en espacio local
        private float _yaw = 0f;
        private float _pitch = 0f;
        private float _roll = 0f;

        // Estado del giroscopio Android
        private bool _gyroInitialized = false;

        public float MouseSensitivity => mouseSensitivity;
        public float RollSensitivity => rollSensitivity;
        public bool InvertPitch => invertPitch;
        public bool EnableGyroFallback => enableGyroFallback;
        public bool IsGyroInitialized => _gyroInitialized;

        private void Start()
        {
            // Inicializar ángulos locales a partir de la orientación actual de la cámara
            Vector3 currentAngles = transform.localEulerAngles;
            _pitch = NormalizeAngle(currentAngles.x);
            _yaw = NormalizeAngle(currentAngles.y);
            _roll = NormalizeAngle(currentAngles.z);

#if UNITY_ANDROID && !UNITY_EDITOR
            InitializeAndroidGyroscope();
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            HandleEditorCameraRotation();
            HandleEditorModeSwitchShortcut();
            HandleEditorZoomShortcut();
            HandleEditorInteractShortcut();
#elif UNITY_ANDROID && !UNITY_EDITOR
            HandleAndroidGyroFallback();
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Control de rotación de cabeza para el Editor de Unity (Mouse Right-Click, Alt, Ctrl).
        /// Compatible tanto con el New Input System como con el Input Manager legacy.
        /// </summary>
        private void HandleEditorCameraRotation()
        {
            bool isRotating = IsRightMouseButtonPressed() || IsAltKeyPressed();
            bool isTiltingRoll = IsCtrlKeyPressed();

            if (isRotating)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                Vector2 delta = GetMouseDelta();
                float mouseX = delta.x;
                float mouseY = delta.y;

                if (isTiltingRoll)
                {
                    // Inclinación lateral de la cabeza (Roll)
                    _roll -= mouseX * rollSensitivity;
                    _roll = Mathf.Clamp(_roll, -45f, 45f);
                }
                else
                {
                    // Giro 360 grados (Yaw) e inclinación vertical (Pitch)
                    _yaw += mouseX * mouseSensitivity;

                    float pitchDelta = mouseY * mouseSensitivity * (invertPitch ? 1f : -1f);
                    _pitch = Mathf.Clamp(_pitch - pitchDelta, -85f, 85f);
                }

                transform.localRotation = Quaternion.Euler(_pitch, _yaw, _roll);
            }
            else
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private bool IsRightMouseButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.rightButton.isPressed) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetMouseButton(1); } catch { return false; }
#else
            return false;
#endif
        }

        private bool IsAltKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt); } catch { return false; }
#else
            return false;
#endif
        }

        private bool IsCtrlKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl); } catch { return false; }
#else
            return false;
#endif
        }

        private Vector2 GetMouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 d = Mouse.current.delta.ReadValue();
                return new Vector2(d.x * 0.1f, d.y * 0.1f);
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); } catch { return Vector2.zero; }
#else
            return Vector2.zero;
#endif
        }

        /// <summary>
        /// Atajo de desarrollo: Tecla 'M' conmuta entre Modo Ataque y Modo Defensa.
        /// </summary>
        private void HandleEditorModeSwitchShortcut()
        {
            bool mPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame) mPressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!mPressed)
            {
                try { if (Input.GetKeyDown(KeyCode.M)) mPressed = true; } catch { }
            }
#endif

            if (mPressed)
            {
                if (GameManager.I != null)
                {
                    SimMode nextMode = (GameManager.I.mode == SimMode.Ataque) ? SimMode.Defensa : SimMode.Ataque;
                    GameManager.I.SetMode(nextMode);
                    Debug.Log($"{LogPrefix} <color=#FCE762>Atajo [M] pulsado: Modo conmutado a <b>{nextMode}</b></color>");
                }
                else
                {
                    Debug.LogWarning($"{LogPrefix} No se encontró instancia de GameManager para alternar el modo.");
                }
            }
        }

        private Camera _cachedCam;
        private float _targetFov = 60f;
        private float _normalFov = 60f;
        private bool _isCameraZoomed = false;

        /// <summary>
        /// Atajo [Q]: Aumenta el tamaño / zoom para leer texto pequeño y detallado.
        /// Si hay un panel de inspección abierto, amplía el panel; si no, aplica zoom óptico a la cámara.
        /// </summary>
        private void HandleEditorZoomShortcut()
        {
            bool qPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) qPressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!qPressed)
            {
                try { if (Input.GetKeyDown(KeyCode.Q)) qPressed = true; } catch { }
            }
#endif

            if (qPressed)
            {
                // 1. Si el overlay de inspección VR está abierto, delegar a la ampliación del canvas
                if (AndesSim.UI.VrInspectionOverlayManager.I != null && AndesSim.UI.VrInspectionOverlayManager.I.IsOpen)
                {
                    AndesSim.UI.VrInspectionOverlayManager.I.ToggleEnlargeZoom();
                    return;
                }

                // 2. Si no hay overlay, aplicar zoom óptico de cámara para leer monitores físicos en el escritorio
                if (_cachedCam == null)
                {
                    _cachedCam = GetComponent<Camera>();
                    if (_cachedCam == null) _cachedCam = GetComponentInChildren<Camera>();
                    if (_cachedCam != null) _normalFov = _cachedCam.fieldOfView;
                }

                if (_cachedCam != null)
                {
                    _isCameraZoomed = !_isCameraZoomed;
                    _targetFov = _isCameraZoomed ? 28f : _normalFov;
                    Debug.Log($"{LogPrefix} <color=#38BDF8>Atajo [Q] pulsado: Zoom óptico {(_isCameraZoomed ? "ACTIVADO (28° FOV)" : "DESACTIVADO")}</color>");
                }
            }

            if (_cachedCam != null && Mathf.Abs(_cachedCam.fieldOfView - _targetFov) > 0.05f)
            {
                _cachedCam.fieldOfView = Mathf.Lerp(_cachedCam.fieldOfView, _targetFov, Time.deltaTime * 12f);
            }
        }

        /// <summary>
        /// Atajo [E] / [Espacio]: Abre la pantalla grande del monitor frontal o la minimiza si ya está abierta.
        /// </summary>
        private void HandleEditorInteractShortcut()
        {
            bool ePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) ePressed = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!ePressed)
            {
                try { if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) ePressed = true; } catch { }
            }
#endif

            if (ePressed)
            {
                // Si el panel de inspección VR ya está abierto, cerrarlo / minimizarlo
                if (AndesSim.UI.VrInspectionOverlayManager.I != null && AndesSim.UI.VrInspectionOverlayManager.I.IsOpen)
                {
                    AndesSim.UI.VrInspectionOverlayManager.I.CloseOverlay();
                    return;
                }

                // Si está cerrado, buscar el monitor al que el jugador está mirando y abrirlo
                if (_cachedCam == null)
                {
                    _cachedCam = GetComponent<Camera>();
                    if (_cachedCam == null) _cachedCam = GetComponentInChildren<Camera>();
                }
                if (_cachedCam != null)
                {
                    var monitors = FindObjectsByType<AndesSim.Scenario01.InteractiveMonitorController>(FindObjectsSortMode.None);
                    AndesSim.Scenario01.InteractiveMonitorController bestMon = null;
                    float bestScore = 0.50f;

                    for (int i = 0; i < monitors.Length; i++)
                    {
                        Vector3 toMon = (monitors[i].transform.position - _cachedCam.transform.position).normalized;
                        float dot = Vector3.Dot(_cachedCam.transform.forward, toMon);
                        float dist = Vector3.Distance(_cachedCam.transform.position, monitors[i].transform.position);

                        if (dist < 3.5f && dot > bestScore)
                        {
                            bestScore = dot;
                            bestMon = monitors[i];
                        }
                    }

                    if (bestMon != null)
                    {
                        bestMon.ToggleInspectionModal();
                    }
                }
            }
        }

#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Configura el giroscopio del smartphone en caso de que no haya subsistema XR activo.
        /// Compatible tanto con New Input System (AttitudeSensor) como con Legacy Input Manager (Input.gyro).
        /// </summary>
        private void InitializeAndroidGyroscope()
        {
            if (!enableGyroFallback) return;

#if ENABLE_INPUT_SYSTEM
            if (AttitudeSensor.current != null)
            {
                InputSystem.EnableDevice(AttitudeSensor.current);
                _gyroInitialized = true;
                Debug.Log($"{LogPrefix} Sensor de orientación (AttitudeSensor - New Input System) inicializado.");
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!_gyroInitialized && SystemInfo.supportsGyroscope)
            {
                try
                {
                    Input.gyro.enabled = true;
                    _gyroInitialized = true;
                    Debug.Log($"{LogPrefix} Giroscopio nativo de Android (Legacy Input) inicializado correctamente.");
                }
                catch { }
            }
#endif
        }

        /// <summary>
        /// Aplica la rotación del giroscopio si Cardboard XR no está tomando el control.
        /// </summary>
        private void HandleAndroidGyroFallback()
        {
            if (!_gyroInitialized || !enableGyroFallback) return;
            // Con Cardboard XR activo, el TrackedPoseDriver ya mueve la cabeza: no pelear con él.
            if (UnityEngine.XR.XRSettings.isDeviceActive) return;

#if ENABLE_INPUT_SYSTEM
            if (AttitudeSensor.current != null && AttitudeSensor.current.enabled)
            {
                Quaternion attitude = AttitudeSensor.current.attitude.ReadValue();
                Quaternion convertedRotation = new Quaternion(attitude.x, attitude.y, -attitude.z, -attitude.w);
                transform.localRotation = Quaternion.Euler(90f, 0f, 0f) * convertedRotation;
                return;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (!UnityEngine.XR.XRSettings.isDeviceActive)
                {
                    Quaternion attitude = Input.gyro.attitude;
                    // Transformación de coordenadas de giroscopio de Android hacia el espacio de Unity
                    Quaternion convertedRotation = new Quaternion(attitude.x, attitude.y, -attitude.z, -attitude.w);
                    transform.localRotation = Quaternion.Euler(90f, 0f, 0f) * convertedRotation;
                }
            }
            catch { }
#endif
        }
#endif

        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }
    }
}
