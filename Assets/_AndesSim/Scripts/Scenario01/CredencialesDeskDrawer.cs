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
    /// Controlador interactivo del cajón de credenciales en el escritorio de Carla Mendoza.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Opera en la capa "Interactive" (Layer 8) con GazeInteractable y retícula VR.
    /// 
    /// Comportamiento:
    /// • Al mirarlo y hacer clic con la retícula:
    ///   - Anima suavemente la apertura o cierre del cajón (slide frontal hacia el jugador).
    ///   - Reproduce sonido procedimental de deslizamiento de rieles de oficina.
    ///   - Revela el contenido interno:
    ///     • En SimMode.Ataque: Libreta de procedimientos SWIFT con contraseñas de contingencia en texto claro.
    ///       Despliega panel forense de mala práctica de seguridad física y registra evidencia.
    ///     • En SimMode.Defensa: Cajón asegurado con cerrojo digital y custodia estricta de credenciales.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CredencialesDeskDrawer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string LogPrefix = "[ANDES-SIM - DeskDrawer]";
        private static readonly Color HighlightBorderColor = new Color(0.22f, 0.95f, 0.54f, 1f); // #38f28a
        private static readonly Color WarningAmberColor = new Color(1.0f, 0.62f, 0.26f, 1f);     // #ff9f43

        [Header("--- Animación y Cinemática del Cajón ---")]
        [Tooltip("Transform que se desplaza físicamente al abrir/cerrar.")]
        [SerializeField] private Transform drawerMovingRoot;

        [Tooltip("Desplazamiento local relativo al abrirse (en metros hacia el operador).")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 0f, -0.32f);

        [Tooltip("Duración de la animación de deslizamiento en segundos.")]
        [SerializeField] private float slideDuration = 0.4f;

        [Header("--- Contenido Interno del Cajón ---")]
        [Tooltip("Modelo 3D del cuaderno de contingencia con credenciales manuscritas.")]
        [SerializeField] private GameObject contingencyNotebookRoot;

        [Tooltip("Texto 3D visible sobre la libreta abierta.")]
        [SerializeField] private TextMeshPro notebookCredentials3DText;

        [Tooltip("Texto manuscrito en la libreta.")]
        [TextArea(2, 4)]
        [SerializeField] private string notebookCredentialsContent =
            "OPERACIONES SWIFT - CONTINGENCIA\n" +
            "Usuario: cmendoza_op\n" +
            "Clave Operativa: Andino2015*Swift\n" +
            "PIN Autorización: 9481\n" +
            "(No retirar del escritorio)";

        [Tooltip("Indicador visual de cerrojo de seguridad en Modo Defensa.")]
        [SerializeField] private GameObject defenseLockBadgeRoot;

        [Header("--- Panel Flotante Forense 3D ---")]
        [SerializeField] private GameObject forensicPopupPanel;
        [SerializeField] private TextMeshProUGUI popupTitleText;
        [SerializeField] private TextMeshProUGUI popupDetailText;
        [SerializeField] private float popupAutoHideDuration = 4.5f;

        [Header("--- Retroalimentación Sonora ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip drawerOpenClip;
        [SerializeField] private AudioClip drawerCloseClip;
        [SerializeField] private AudioClip hoverClip;

        // Estado interno
        private GazeInteractable _gazeInteractable;
        private Collider _collider;
        private Vector3 _closedLocalPosition;
        private Vector3 _openLocalPosition;
        private bool _isOpen = false;
        private bool _isSliding = false;
        private bool _isEvidenceLogged = false;
        private Coroutine _slideCoroutine;
        private Coroutine _popupCoroutine;

        #region Propiedades Públicas

        public bool IsOpen => _isOpen;
        public bool IsSliding => _isSliding;

        #endregion

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _gazeInteractable = GetComponent<GazeInteractable>();

            gameObject.layer = LayerMask.NameToLayer("Interactive");

            if (drawerMovingRoot == null)
            {
                drawerMovingRoot = transform;
            }

            _closedLocalPosition = drawerMovingRoot.localPosition;
            _openLocalPosition = _closedLocalPosition + openOffset;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1f; // Audio espacial 3D
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
                _gazeInteractable.interactableLabel = "Abrir Cajón del Escritorio (Credenciales)";
            }

            if (notebookCredentials3DText != null)
            {
                notebookCredentials3DText.text = notebookCredentialsContent;
            }

            if (forensicPopupPanel != null)
            {
                forensicPopupPanel.SetActive(false);
            }

            // Suscribirse a eventos de GameManager
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
            }

            UpdateDefenseLockVisuals();
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
            }
        }

        private void HandleModeChanged(SimMode newMode)
        {
            UpdateDefenseLockVisuals();
        }

        private void HandleDefenseApplied(string defenseId)
        {
            if (defenseId.Equals("Defensa_MFA", StringComparison.OrdinalIgnoreCase))
            {
                UpdateDefenseLockVisuals();
            }
        }

        private void UpdateDefenseLockVisuals()
        {
            bool isDefense = GameManager.I != null && GameManager.I.mode == SimMode.Defensa;

            if (defenseLockBadgeRoot != null)
            {
                defenseLockBadgeRoot.SetActive(isDefense);
            }

            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = isDefense
                    ? "Cajón Asegurado (Control de Acceso Físico)"
                    : (_isOpen ? "Cerrar Cajón del Escritorio" : "Abrir Cajón de Credenciales");
            }
        }

        #region Eventos de Mirada y Clic VR

        public void OnPointerClick(PointerEventData eventData)
        {
            ToggleDrawer();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlaySound(hoverClip, GenerateHoverTone());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        #endregion

        #region Cinemática de Apertura y Cierre

        /// <summary>
        /// Alterna la apertura o cierre suave del cajón.
        /// </summary>
        public void ToggleDrawer()
        {
            if (_isSliding) return;

            _isOpen = !_isOpen;

            if (_slideCoroutine != null)
            {
                StopCoroutine(_slideCoroutine);
            }

            _slideCoroutine = StartCoroutine(SlideDrawerCoroutine(_isOpen));

            // Actualizar etiqueta del interactable
            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = _isOpen ? "Cerrar Cajón del Escritorio" : "Abrir Cajón de Credenciales";
            }

            // Al abrir el cajón:
            if (_isOpen)
            {
                PlaySound(drawerOpenClip, GenerateDrawerSlideTone(true));
                ShowForensicInspection();
            }
            else
            {
                PlaySound(drawerCloseClip, GenerateDrawerSlideTone(false));
                if (forensicPopupPanel != null)
                {
                    forensicPopupPanel.SetActive(false);
                }
            }
        }

        private IEnumerator SlideDrawerCoroutine(bool opening)
        {
            _isSliding = true;
            Vector3 startPos = drawerMovingRoot.localPosition;
            Vector3 targetPos = opening ? _openLocalPosition : _closedLocalPosition;

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                // Curva suave SmoothStep
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                drawerMovingRoot.localPosition = Vector3.Lerp(startPos, targetPos, smoothT);
                yield return null;
            }

            drawerMovingRoot.localPosition = targetPos;
            _isSliding = false;
        }

        #endregion

        #region Explicación Forense y Evidencia

        private void ShowForensicInspection()
        {
            bool isDefense = GameManager.I != null && GameManager.I.mode == SimMode.Defensa;

            // Registrar evidencia de credenciales si aún no se registró el Post-It
            if (!isDefense && !_isEvidenceLogged && GameManager.I != null)
            {
                _isEvidenceLogged = true;
                GameManager.I.RegisterEvidence("Evidencia_PostIt");
            }

            if (forensicPopupPanel != null)
            {
                forensicPopupPanel.SetActive(true);

                if (popupTitleText != null)
                {
                    popupTitleText.text = isDefense
                        ? "<b><color=#38F28A>CAJÓN ASEGURADO · POLÍTICA DE ESCRITORIO LIMPIO</color></b>"
                        : "<b><color=#FF9F43>VULNERABILIDAD FÍSICA: LIBRETA DE CREDENCIALES</color></b>";
                }

                if (popupDetailText != null)
                {
                    popupDetailText.text = isDefense
                        ? "<b>Mitigación de Seguridad Física:</b>\n" +
                          "• Las claves operativas ya no se escriben en papel ni se guardan en cajones sin llave.\n" +
                          "• Implementación de política de escritorio limpio y bóveda de credenciales cifrada.\n" +
                          "• Autenticación protegida mediante hardware Token MFA."
                        : "<b>Mala Práctica Grave de Seguridad Física:</b>\n" +
                          "• Cuaderno de contingencia con credenciales de operador SWIFT ('cmendoza_op') en texto claro.\n" +
                          "• Cajón sin cerradura accesible para cualquier persona en la oficina.\n" +
                          "• Facilitó la extracción de credenciales legítimas tras la infección inicial por el dropper.";
                }

                if (_popupCoroutine != null) StopCoroutine(_popupCoroutine);
                _popupCoroutine = StartCoroutine(AutoHidePopupRoutine());
            }

            Debug.Log($"{LogPrefix} Cajón abierto en Modo {(isDefense ? "Defensa" : "Ataque")}. Inspección física desplegada.");
        }

        private IEnumerator AutoHidePopupRoutine()
        {
            yield return new WaitForSeconds(popupAutoHideDuration);
            if (forensicPopupPanel != null)
            {
                forensicPopupPanel.SetActive(false);
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

        private static AudioClip GenerateDrawerSlideTone(bool opening)
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(t / duration * Mathf.PI);
                float freq = opening ? Mathf.Lerp(180f, 320f, t / duration) : Mathf.Lerp(320f, 150f, t / duration);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.15f;
                samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.25f + noise) * env;
            }

            AudioClip clip = AudioClip.Create("Drawer_Slide", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateHoverTone()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.12f * env;
            }

            AudioClip clip = AudioClip.Create("Drawer_Hover", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
