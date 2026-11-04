using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
#if UNITY_ANDROID || UNITY_STANDALONE
using UnityEngine.XR.Management;
#endif

namespace AndesSim.VR
{
    /// <summary>
    /// Gestor de ciclo de vida, rendimiento y calibración para Google Cardboard XR sobre Android.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// 
    /// Responsabilidades:
    /// 1. Configurar Screen.sleepTimeout = NeverSleep y targetFrameRate = 60 para evitar suspensión del dispositivo.
    /// 2. Validar e iniciar el subsistema XR en tiempo de ejecución en Android a través de XRGeneralSettings.
    /// 3. Ofrecer el método público RecenterView() para reorientar la vista VR de forma accesible por mirada.
    /// </summary>
    public class CardboardXrLifecycleManager : MonoBehaviour
    {
        public static CardboardXrLifecycleManager I;

        private const string LogPrefix = "[ANDES-SIM - XrLifecycle]";

        [Header("--- Rendimiento y Pantalla Móvil ---")]
        [Tooltip("Cuadros por segundo objetivo para asegurar fluidez y confort térmico (60 fps estándar Cardboard).")]
        [SerializeField] private int targetFrameRate = 60;

        [Tooltip("Evita que el celular entre en modo reposo o apague la pantalla dentro del visor.")]
        [SerializeField] private bool preventScreenSleep = true;

        [Header("--- Jerarquía de Cámara y Rig (Recenter) ---")]
        [Tooltip("Transform raíz del rig (PlayerRig) o CameraOffset a rotar para compensar la orientación del usuario.")]
        [SerializeField] private Transform playerRigRoot;

        [Tooltip("Cámara principal VR del jugador.")]
        [SerializeField] private Camera playerCamera;

        [Header("--- Ciclo de Vida XR ---")]
        [Tooltip("Asegura la inicialización síncrona del XR Loader al arrancar si estuviera detenido.")]
        [SerializeField] private bool autoStartSubsystems = true;

        public event Action OnViewRecentered;

        public bool IsScreenNeverSleepActive => Screen.sleepTimeout == SleepTimeout.NeverSleep;
        public int CurrentTargetFrameRate => Application.targetFrameRate;
        public bool AutoStartSubsystems => autoStartSubsystems;

        private void Awake()
        {
            if (I == null)
            {
                I = this;
            }
            else if (I != this)
            {
                Destroy(this);
                return;
            }

            // 1. Configuración de pantalla y tasa de cuadros por segundo
            ConfigureScreenAndFrameRate();

            // 2. Localización de referencias de cámara y rig
            ResolveRigReferences();
        }

        private void Start()
        {
            // 3. Verificación e inicialización de XR Management en Android
            VerifyAndInitializeXrManagement();
        }

        /// <summary>
        /// Aplica la configuración de suspensión de pantalla y targetFrameRate.
        /// </summary>
        private void ConfigureScreenAndFrameRate()
        {
            if (preventScreenSleep)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Debug.Log($"{LogPrefix} Modo anti-suspensión activado (Screen.sleepTimeout = SleepTimeout.NeverSleep).");
            }

            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0; // Desactivar vSync fijo para permitir control exacto de frame rate móvil
            Debug.Log($"{LogPrefix} Tasa de cuadros por segundo configurada a: <b>{targetFrameRate} FPS</b>.");
        }

        /// <summary>
        /// Localiza automáticamente el PlayerRig y la Main Camera si no fueron asignados en el Inspector.
        /// </summary>
        private void ResolveRigReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    playerCamera = FindFirstObjectByType<Camera>();
                }
            }

            if (playerRigRoot == null && playerCamera != null)
            {
                // Si la cámara tiene un padre (ej. CameraOffset o PlayerRig), usamos el padre
                playerRigRoot = playerCamera.transform.parent != null ? playerCamera.transform.parent : playerCamera.transform;
            }
        }

        /// <summary>
        /// Verifica el estado de XRGeneralSettings y asegura que el cargador XR esté activo e iniciado en Android.
        /// </summary>
        private void VerifyAndInitializeXrManagement()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
                {
                    XRManagerSettings manager = XRGeneralSettings.Instance.Manager;

                    if (!manager.isInitializationComplete)
                    {
                        Debug.Log($"{LogPrefix} Inicializando XR Loader síncronamente en Android...");
                        manager.InitializeLoaderSync();
                    }

                    if (manager.activeLoader != null)
                    {
                        Debug.Log($"{LogPrefix} <color=#4EC9B0>XR Loader Activo: '{manager.activeLoader.name}'</color>");

                        if (autoStartSubsystems && !manager.isStarted)
                        {
                            manager.StartSubsystems();
                            Debug.Log($"{LogPrefix} Subsistemas XR iniciados exitosamente.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"{LogPrefix} Alerta: No se encontró ningún XR Loader activo en XRGeneralSettings.");
                    }
                }
                else
                {
                    Debug.LogWarning($"{LogPrefix} XRGeneralSettings.Instance no está disponible en tiempo de ejecución.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} Error al verificar ciclo de vida de XR: {ex.Message}");
            }
#else
            Debug.Log($"{LogPrefix} Entorno Editor / Standalone: Subordinando gestión XR a simulación y emulación local.");
#endif
        }

        /// <summary>
        /// Recalibra el frente de la cámara VR hacia el horizonte frontal del escenario.
        /// Accesible mediante selección por mirada (GazeInteractable) en botones de reorientación.
        /// </summary>
        public void RecenterView()
        {
            Debug.Log($"{LogPrefix} <b>Iniciando recentrado de vista VR...</b>");

            // 1. Invocar Recentrado a través de los subsistemas nativos de Unity XR
            List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(inputSubsystems);

            for (int i = 0; i < inputSubsystems.Count; i++)
            {
                if (inputSubsystems[i].running)
                {
                    inputSubsystems[i].TryRecenter();
                    Debug.Log($"{LogPrefix} XRInputSubsystem.TryRecenter() ejecutado en '{inputSubsystems[i].subsystemDescriptor.id}'.");
                }
            }

            // 2. Compensación geométrica del Rig (Universal para Cardboard y Gyro sin deriva)
            if (playerCamera != null && playerRigRoot != null)
            {
                Vector3 cameraForward = playerCamera.transform.forward;
                cameraForward.y = 0f; // Proyectar sobre el plano horizontal

                if (cameraForward.sqrMagnitude > 0.001f)
                {
                    // Ángulo actual de desviación horizontal respecto al frente del escenario (Vector3.forward)
                    float yawOffset = Vector3.SignedAngle(Vector3.forward, cameraForward.normalized, Vector3.up);

                    // Rotar el PlayerRig sobre la posición de la cabeza para realinear el frente
                    playerRigRoot.RotateAround(playerCamera.transform.position, Vector3.up, -yawOffset);
                    Debug.Log($"{LogPrefix} <color=#4EC9B0>Compensación de orientación aplicada: corrección de {yawOffset:F1}° en Yaw.</color>");
                }
            }

            OnViewRecentered?.Invoke();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                // Restaurar políticas al recuperar foco tras una llamada o bloqueo
                if (preventScreenSleep)
                {
                    Screen.sleepTimeout = SleepTimeout.NeverSleep;
                }
                Application.targetFrameRate = targetFrameRate;
            }
        }
    }
}
