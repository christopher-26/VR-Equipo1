using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.VR;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Controlador del objeto obligatorio 'PostIt_Credenciales' en el puesto de Carla Mendoza (Monitor_01).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Hereda de EvidenceItem con evidenceId = "Evidencia_PostIt".
    /// 
    /// Comportamiento Dual:
    /// • En SimMode.Ataque:
    ///   Muestra la nota adhesiva amarilla con texto manuscrito:
    ///   "Usuario SWIFT: cmendoza_op | Clave: Andino2015*Swift (No olvidar)".
    ///   Al mirar y hacer clic, registra la evidencia y explica cómo facilitó el robo de credenciales.
    /// • En SimMode.Defensa:
    ///   Al activarse la contramedida "Defensa_MFA", el Post-It desaparece simbólicamente
    ///   y es reemplazado por un dispositivo Token MFA de hardware sobre el escritorio
    ///   con un código numérico rotativo cada 30 segundos.
    /// </summary>
    public class PostItCredenciales : EvidenceItem
    {
        private const string SpecificLogPrefix = "[ANDES-SIM - PostIt Credenciales]";

        [Header("--- Representación Física: Nota Adhesiva (Modo Ataque) ---")]
        [Tooltip("Contenedor raíz del modelo 3D del Post-It físico.")]
        [SerializeField] private GameObject postItVisualRoot;

        [Tooltip("Texto sobre la nota adhesiva en 3D.")]
        [SerializeField] private TextMeshPro postItNote3DText;

        [Tooltip("Texto manuscrito exacto según especificación.")]
        [TextArea(2, 4)]
        [SerializeField] private string handwrittenNoteText =
            "Usuario SWIFT: cmendoza_op | Clave: Andino2015*Swift (No olvidar)";

        [Header("--- Representación de Defensa: Token MFA Hardware (Modo Defensa) ---")]
        [Tooltip("Contenedor del dispositivo físico de Token MFA sobre el escritorio.")]
        [SerializeField] private GameObject hardwareMfaTokenRoot;

        [Tooltip("Pantalla LCD del Token MFA que muestra el código de 6 dígitos rotativo.")]
        [SerializeField] private TextMeshPro mfaTokenCodeText;

        [Tooltip("Barra de progreso radial o lineal del intervalo de 30 segundos.")]
        [SerializeField] private Image mfaTimerFillImage;

        [Tooltip("Etiqueta de estado del dispositivo MFA.")]
        [SerializeField] private TextMeshPro mfaStatusLabel;

        [Header("--- Configuración del Token Rotativo (30 Segundos) ---")]
        [SerializeField] private float tokenRotationIntervalSeconds = 30f;
        [SerializeField] private AudioClip tokenCodeRotatedClip;
        [SerializeField] private AudioClip postItTransitionClip;

        // Estado interno del Token
        private float _tokenTimer = 0f;
        private int _currentOtpCode = 682914;
        private bool _isMfaActive = false;

        protected override void Awake()
        {
            // Inicializar metadatos de EvidenceItem
            evidenceId = "Evidencia_PostIt";
            evidenceTitle = "Credenciales Expuestas en Post-It (Carla Mendoza)";
            forensicDescription =
                "Mala práctica grave de seguridad física: Contraseña de operador SWIFT ('cmendoza_op' / 'Andino2015*Swift') " +
                "escrita en papel adhesivo junto al monitor. Esta negligencia permitió a los ciberdelincuentes apropiarse " +
                "de credenciales legítimas de autorización tras la infección inicial por el dropper, posibilitando transferencias fraudulentas.";

            base.Awake();

            if (postItNote3DText != null)
            {
                postItNote3DText.text = handwrittenNoteText;
            }
        }

        protected override void Start()
        {
            base.Start();

            // Suscribirse a eventos de GameManager
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
            }

            EvaluateMfaState();
            GenerateNewOtpCode();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
            }
        }

        private void Update()
        {
            // Si el token MFA está activo en Modo Defensa, actualizar el ciclo de 30 segundos
            if (_isMfaActive && hardwareMfaTokenRoot != null && hardwareMfaTokenRoot.activeSelf)
            {
                UpdateMfaTokenCountdown();
            }
        }

        private void HandleModeChanged(SimMode newMode)
        {
            EvaluateMfaState();
        }

        private void HandleDefenseApplied(string defenseId)
        {
            if (string.Equals(defenseId, "Defensa_MFA", System.StringComparison.OrdinalIgnoreCase))
            {
                EvaluateMfaState();
            }
        }

        /// <summary>
        /// Evalúa si el token MFA debe estar activo y si el Post-It físico debe ser ocultado.
        /// </summary>
        public void EvaluateMfaState()
        {
            SimMode currentMode = (GameManager.I != null) ? GameManager.I.mode : SimMode.Ataque;
            bool hasMfaDefense = false;

            if (GameManager.I != null && GameManager.I.AppliedDefenses != null)
            {
                foreach (string def in GameManager.I.AppliedDefenses)
                {
                    if (string.Equals(def, "Defensa_MFA", System.StringComparison.OrdinalIgnoreCase))
                    {
                        hasMfaDefense = true;
                        break;
                    }
                }
            }

            // En Modo Defensa con Defensa_MFA activa, el Post-It desaparece y emerge el Token
            bool showToken = (currentMode == SimMode.Defensa && hasMfaDefense);

            if (showToken != _isMfaActive)
            {
                _isMfaActive = showToken;
                PlayTransitionFeedback(_isMfaActive);
            }

            if (postItVisualRoot != null)
            {
                postItVisualRoot.SetActive(!_isMfaActive);
            }

            if (hardwareMfaTokenRoot != null)
            {
                hardwareMfaTokenRoot.SetActive(_isMfaActive);
            }

            // Actualizar etiqueta del interactable
            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = _isMfaActive
                    ? "Examinar Token MFA Hardware (OTP Dinámico)"
                    : "Examinar Post-It con Contraseñas Expuestas";
            }
        }

        /// <summary>
        /// Cuenta regresiva y rotación del código OTP cada 30 segundos.
        /// </summary>
        private void UpdateMfaTokenCountdown()
        {
            _tokenTimer += Time.deltaTime;

            float remaining = Mathf.Max(0f, tokenRotationIntervalSeconds - _tokenTimer);
            float progress = Mathf.Clamp01(remaining / tokenRotationIntervalSeconds);

            if (mfaTimerFillImage != null)
            {
                mfaTimerFillImage.fillAmount = progress;
            }

            if (mfaStatusLabel != null)
            {
                mfaStatusLabel.text = $"OTP EXPIRA EN: {Mathf.CeilToInt(remaining):00}s";
            }

            // Al completar los 30 segundos, generar nuevo código
            if (_tokenTimer >= tokenRotationIntervalSeconds)
            {
                _tokenTimer = 0f;
                GenerateNewOtpCode();
            }
        }

        /// <summary>
        /// Genera un nuevo código numérico de 6 dígitos simulando un algoritmo TOTP criptográfico.
        /// </summary>
        private void GenerateNewOtpCode()
        {
            _currentOtpCode = Random.Range(100000, 999999);
            string formattedCode = $"{_currentOtpCode / 1000:000} {_currentOtpCode % 1000:000}";

            if (mfaTokenCodeText != null)
            {
                mfaTokenCodeText.text = formattedCode;
            }

            if (_isMfaActive)
            {
                PlaySound(tokenCodeRotatedClip);
                Debug.Log($"{SpecificLogPrefix} Código MFA rotado a: <color=#38F28A><b>{formattedCode}</b></color> (Válido por 30s).");
            }
        }

        private void PlayTransitionFeedback(bool activatingMfa)
        {
            PlaySound(postItTransitionClip);

            if (activatingMfa)
            {
                Debug.Log($"{SpecificLogPrefix} <color=#38F28A><b>¡Defensa_MFA implementada!</b></color> El Post-It con contraseñas ha sido retirado. Token MFA activo en escritorio.");
            }
            else
            {
                Debug.Log($"{SpecificLogPrefix} <color=#FF6B6B>Modo Ataque activo:</color> Post-It físico con credenciales expuestas restaurado en monitor.");
            }
        }

        public override void CollectEvidence()
        {
            if (_isMfaActive)
            {
                // Si el MFA está activo, mostrar panel pedagógico del token seguro
                ShowMfaSecurityPopup();
                return;
            }

            // Si está en Modo Ataque, ejecutar recolección forense normal de EvidenceItem
            base.CollectEvidence();
        }

        private void ShowMfaSecurityPopup()
        {
            if (forensicPopupPanel == null) return;

            forensicPopupPanel.SetActive(true);

            if (popupTitleText != null)
            {
                popupTitleText.text = "<color=#38F28A>[OK] CONTROL DEFENSA: TOKEN MFA ACTIVO</color>\n<b>Doble Factor Criptográfico</b>";
            }

            if (popupDescriptionText != null)
            {
                popupDescriptionText.text =
                    "El Banco Andino desplegó autenticación multifactor (MFA/TOTP) con token físico.\n" +
                    "• El código de 6 dígitos rota automáticamente cada 30 segundos.\n" +
                    "• Cualquier contraseña robada de Carla Mendoza es completamente inútil sin este dispositivo físico.\n" +
                    "• Las autorizaciones de pago en la terminal SWIFT requieren este segundo factor dinámico.";
            }

            if (autoHideDuration > 0f)
            {
                if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
                _hideCoroutine = StartCoroutine(AutoHideRoutine(autoHideDuration));
            }
        }

        private IEnumerator AutoHideRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (forensicPopupPanel != null)
            {
                forensicPopupPanel.SetActive(false);
            }
        }
    }
}
