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
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(1);
#else
            return false;
#endif
        }

        private bool IsAltKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
#else
            return false;
#endif
        }

        private bool IsCtrlKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
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
            return Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
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
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.M)) mPressed = true;
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
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Configura el giroscopio del smartphone en caso de que no haya subsistema XR activo.
        /// </summary>
        private void InitializeAndroidGyroscope()
        {
            if (enableGyroFallback && SystemInfo.supportsGyroscope)
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                try
                {
                    Input.gyro.enabled = true;
                    _gyroInitialized = true;
                    Debug.Log($"{LogPrefix} Giroscopio nativo de Android inicializado correctamente.");
                }
                catch { }
#endif
            }
        }

        /// <summary>
        /// Aplica la rotación del giroscopio si Cardboard XR no está tomando el control.
        /// </summary>
        private void HandleAndroidGyroFallback()
        {
            if (!_gyroInitialized || !enableGyroFallback) return;

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                // Si UnityEngine.XR.XRSettings no tiene un dispositivo activo (modo no-XR / ventana previa)
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
