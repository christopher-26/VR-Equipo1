using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.VR;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Controlador del objeto interactivo 'Token_MFA_Boveda' en el puesto de Carla Mendoza.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Opera en la capa "Interactive" (Layer 8) con GazeInteractable y retícula VR.
    /// 
    /// Especificación técnica:
    /// • Dispositivo generador de códigos de doble factor (MFA) y bóveda de contraseñas.
    /// • En Modo Defensa reemplaza al Post-It físico con credenciales expuestas.
    /// • Genera códigos numéricos rotativos de 6 dígitos cada 30 segundos (TOTP - RFC 6238)
    ///   con cuenta regresiva visual en pantalla LCD.
    /// • Al mirarlo y hacer clic con la retícula:
    ///   - Aplica la contramedida oficial 'Defensa_MFA' en GameManager.
    ///   - Muestra un panel flotante 3D con la explicación técnica de por qué el doble factor
    ///     neutraliza el robo de claves en texto plano.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TokenMfaBoveda : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string LogPrefix = "[ANDES-SIM - Token MFA]";
        private static readonly Color ActiveGreenColor = new Color(0.22f, 0.95f, 0.54f, 1f); // #38f28a
        private static readonly Color InactiveRedColor = new Color(1.0f, 0.35f, 0.31f, 1f);  // #ff5a4e
        private static readonly Color CyberCyanColor = new Color(0.3f, 0.85f, 1.0f, 1f);     // #4ec9b0

        [Header("--- Elementos Visuales del Dispositivo ---")]
        [Tooltip("Contenedor raíz del dispositivo físico sobre el escritorio.")]
        [SerializeField] private GameObject tokenHardwareMeshRoot;

        [Tooltip("Pantalla LCD que muestra el código numérico de 6 dígitos.")]
        [SerializeField] private TextMeshPro otpCodeDisplayText;

        [Tooltip("Etiqueta LCD del servicio o token.")]
        [SerializeField] private TextMeshPro serviceLabelText;

        [Tooltip("Barra de progreso del intervalo de 30 segundos (0 a 1).")]
        [SerializeField] private Image countdownProgressBar;

        [Tooltip("Luz LED de sincronización criptográfica.")]
        [SerializeField] private Renderer ledIndicatorRenderer;
        [SerializeField] private Light syncStatusLight;

        [Header("--- Configuración Criptográfica TOTP (30s) ---")]
        [SerializeField] private float tokenIntervalSeconds = 30f;
        [SerializeField] private int currentOtpCode = 739204;

        [Header("--- Panel Flotante 3D de Explicación Forense ---")]
        [SerializeField] private GameObject explanationPopupPanel;
        [SerializeField] private TextMeshProUGUI popupTitleText;
        [SerializeField] private TextMeshProUGUI popupDetailText;
        [SerializeField] private float popupAutoHideDuration = 4.5f;

        [Header("--- Referencia a la Nota Adhesiva (Post-It) ---")]
        [Tooltip("Post-It físico que es reemplazado en Modo Defensa.")]
        [SerializeField] private GameObject postItPhysicalToReplace;

        [Header("--- Efectos Sonoros ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip codeRotatedBeepClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip mfaActivatedChimeClip;

        // Estado interno
        private GazeInteractable _gazeInteractable;
        private Collider _collider;
        private float _cycleTimer = 0f;
        private bool _isMfaActive = false;
        private Coroutine _popupCoroutine;

        #region Propiedades Públicas

        public bool IsMfaActive => _isMfaActive;
        public int CurrentOtpCode => currentOtpCode;

        #endregion

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _gazeInteractable = GetComponent<GazeInteractable>();

            gameObject.layer = LayerMask.NameToLayer("Interactive");

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1f; // Audio espacial 3D VR
                    audioSource.minDistance = 0.3f;
                    audioSource.maxDistance = 5f;
                }
            }
        }

        private void Start()
        {
            if (_gazeInteractable == null)
            {
                _gazeInteractable = gameObject.AddComponent<GazeInteractable>();
            }

            if (serviceLabelText != null)
            {
                serviceLabelText.text = "BANCO ANDINO · SWIFT MFA";
            }

            if (explanationPopupPanel != null)
            {
                explanationPopupPanel.SetActive(false);
            }

            // Suscribirse a eventos de GameManager
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
            }

            EvaluateState();
            GenerateNewOtpCode();
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
            }
        }

        private void Update()
        {
            // Actualizar cuenta regresiva de 30 segundos
            _cycleTimer += Time.deltaTime;
            float remainingProgress = Mathf.Clamp01(1f - (_cycleTimer / tokenIntervalSeconds));

            if (countdownProgressBar != null)
            {
                countdownProgressBar.fillAmount = remainingProgress;
                countdownProgressBar.color = remainingProgress > 0.25f ? ActiveGreenColor : InactiveRedColor;
            }

            if (_cycleTimer >= tokenIntervalSeconds)
            {
                _cycleTimer = 0f;
                GenerateNewOtpCode();
            }
        }

        private void HandleModeChanged(SimMode newMode)
        {
            EvaluateState();
        }

        private void HandleDefenseApplied(string defenseId)
        {
            if (defenseId.Equals("Defensa_MFA", StringComparison.OrdinalIgnoreCase))
            {
                EvaluateState();
            }
        }

        /// <summary>
        /// Evalúa la activación del token según el modo del simulador.
        /// </summary>
        public void EvaluateState()
        {
            bool isDefense = GameManager.I != null && GameManager.I.mode == SimMode.Defensa;
            bool hasMfaApplied = false;

            if (GameManager.I != null && GameManager.I.AppliedDefenses != null)
            {
                foreach (var d in GameManager.I.AppliedDefenses)
                {
                    if (d.Equals("Defensa_MFA", StringComparison.OrdinalIgnoreCase))
                    {
                        hasMfaApplied = true;
                        break;
                    }
                }
            }

            _isMfaActive = isDefense || hasMfaApplied;

            // En Modo Defensa, el token está activo y visible, y oculta al Post-It
            if (postItPhysicalToReplace != null)
            {
                postItPhysicalToReplace.SetActive(!_isMfaActive);
            }

            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = _isMfaActive
                    ? "Token MFA Hardware (Código: " + currentOtpCode.ToString("D6") + ")"
                    : "Dispositivo Token MFA [Inactivo en 2015]";
            }

            SetLedVisuals(_isMfaActive ? ActiveGreenColor : InactiveRedColor);
        }

        private void GenerateNewOtpCode()
        {
            currentOtpCode = UnityEngine.Random.Range(100000, 999999);

            if (otpCodeDisplayText != null)
            {
                otpCodeDisplayText.text = currentOtpCode.ToString("D6");
            }

            if (_isMfaActive)
            {
                PlaySound(codeRotatedBeepClip, GenerateOtpBeepTone());
            }

            if (_gazeInteractable != null && _isMfaActive)
            {
                _gazeInteractable.interactableLabel = "Token MFA Hardware (Código: " + currentOtpCode.ToString("D6") + ")";
            }
        }

        #region Eventos de Retícula VR (Gaze & Touch)

        public void OnPointerClick(PointerEventData eventData)
        {
            InspectTokenDevice();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlaySound(null, GenerateHoverTone());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        #endregion

        #region Inspección Forense y Activación

        /// <summary>
        /// Inspecciona el dispositivo y aplica 'Defensa_MFA' si no estaba activa.
        /// </summary>
        public void InspectTokenDevice()
        {
            PlaySound(buttonClickClip, GenerateClickTone());

            // 1. Aplicar la contramedida oficial en GameManager
            if (GameManager.I != null)
            {
                GameManager.I.ApplyDefense("Defensa_MFA");
            }

            EvaluateState();

            // 2. Mostrar panel 3D explicativo
            if (explanationPopupPanel != null)
            {
                explanationPopupPanel.SetActive(true);

                if (popupTitleText != null)
                {
                    popupTitleText.text = "<b><color=#38F28A>BÓVEDA DE CREDENCIALES & TOKEN MFA (RFC 6238)</color></b>";
                }

                if (popupDetailText != null)
                {
                    popupDetailText.text =
                        "<b>Mecanismo de Mitigación en Profundidad:</b>\n" +
                        "• <b>Código Dinámico:</b> Algoritmo TOTP genera un código de 6 dígitos cada 30 segundos.\n" +
                        "• <b>Neutralización de Robo Físico:</b> El Post-It con contraseñas estáticas queda obsoleto.\n" +
                        "• <b>Protección SWIFT:</b> Aunque un atacante obtenga la clave 'Andino2015*Swift', no puede " +
                        "iniciar transferencias sin poseer físicamente este token criptográfico.";
                }

                if (_popupCoroutine != null) StopCoroutine(_popupCoroutine);
                _popupCoroutine = StartCoroutine(AutoHidePopupRoutine());
            }

            Debug.Log($"{LogPrefix} <color=#38F28A><b>Token MFA inspeccionado.</b></color> Código activo: {currentOtpCode:D6}. Contramedida 'Defensa_MFA' validada.");
        }

        private IEnumerator AutoHidePopupRoutine()
        {
            yield return new WaitForSeconds(popupAutoHideDuration);
            if (explanationPopupPanel != null)
            {
                explanationPopupPanel.SetActive(false);
            }
        }

        private void SetLedVisuals(Color color)
        {
            if (ledIndicatorRenderer != null)
            {
                Material mat = Application.isPlaying ? ledIndicatorRenderer.material : ledIndicatorRenderer.sharedMaterial;
                if (mat != null)
                {
                    mat.color = color;
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", color * 1.3f);
                    }
                }
            }

            if (syncStatusLight != null)
            {
                syncStatusLight.color = color;
            }
        }

        #endregion

        #region Generación Procedural de Audio

        private void PlaySound(AudioClip clip, AudioClip fallback = null)
        {
            if (audioSource == null) return;
            AudioClip toPlay = clip != null ? clip : fallback;
            if (toPlay != null)
            {
                audioSource.PlayOneShot(toPlay, 0.85f);
            }
        }

        private static AudioClip GenerateOtpBeepTone()
        {
            int sampleRate = 44100;
            float duration = 0.06f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 1480f * t) * 0.15f * env;
            }

            AudioClip clip = AudioClip.Create("Token_Beep", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateClickTone()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-5f * t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.22f * env;
            }

            AudioClip clip = AudioClip.Create("Token_Click", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateHoverTone()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.08f;
            }

            AudioClip clip = AudioClip.Create("Token_Hover", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
