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
    /// Componente de control e interruptor de contramedidas (RF-04).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Se integra con GazeInteractable en la capa "Interactive" (Layer 8) y tag "DefenseControl".
    /// 
    /// Gestiona las 3 contramedidas oficiales del Escenario 1:
    /// 1. "Defensa_Sandbox": Filtro de correo con análisis de enlaces y archivos en sandbox.
    /// 2. "Defensa_BotonReportar": Capacitación al personal y botón «Reportar phishing» en el cliente de correo.
    /// 3. "Defensa_MFA": Gestor de contraseñas corporativo y doble factor de autenticación (MFA).
    /// 
    /// Comportamiento:
    /// • Al hacer clic con la retícula:
    ///   - Si está en SimMode.Ataque, cambia automáticamente a SimMode.Defensa e invoca GameManager.I.ApplyDefense(defenseId).
    ///   - Cambia el indicador luminoso de rojo (INACTIVO EN 2016) a verde brillante #38f28a (CONTRAMEDIDA ACTIVA).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DefenseControl : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string LogPrefix = "[ANDES-SIM - DefenseControl]";
        private static readonly Color InactiveRedColor = new Color(0.9f, 0.2f, 0.2f, 1.0f);
        private static readonly Color ActiveGreenColor = new Color(0.22f, 0.95f, 0.54f, 1.0f); // #38f28a

        [Header("--- Identificación de la Contramedida (RF-04) ---")]
        [Tooltip("Identificador exacto: 'Defensa_Sandbox', 'Defensa_BotonReportar' o 'Defensa_MFA'.")]
        [SerializeField] private string defenseId = "Defensa_Sandbox";

        [Tooltip("Nombre técnico oficial de la contramedida.")]
        [SerializeField] private string defenseTitle = "Filtro de correo con análisis en Sandbox";

        [TextArea(2, 5)]
        [Tooltip("Descripción del mecanismo de mitigación.")]
        [SerializeField] private string mitigationMechanism =
            "Inspección automatizada en pasarela (Secure Email Gateway). Los enlaces y adjuntos se ejecutan " +
            "en un entorno virtual aislado antes de llegar a los buzones, detonando el archivo '.scr.zip'.";

        [Header("--- Estado del Interruptor ---")]
        [SerializeField] private bool isApplied = false;

        [Header("--- Indicadores Luminosos y Visuales ---")]
        [Tooltip("Luz 3D o Renderer del foco/LED del interruptor.")]
        [SerializeField] private Light statusLedLight;
        [SerializeField] private Renderer statusIndicatorRenderer;
        [SerializeField] private int materialIndex = 0;

        [Tooltip("Imagen UI de estado en Canvas World Space.")]
        [SerializeField] private Image statusLedImage;
        [SerializeField] private TextMeshProUGUI statusLabelText;
        [SerializeField] private TextMeshProUGUI defenseTitleText;

        [Header("--- Panel Flotante de Explicación Forense ---")]
        [SerializeField] private GameObject mitigationPopupPanel;
        [SerializeField] private TextMeshProUGUI popupTitleText;
        [SerializeField] private TextMeshProUGUI popupDetailText;
        [SerializeField] private float popupAutoHideDuration = 4.0f;

        [Header("--- Retroalimentación Sonora ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip toggleSwitchClip;
        [SerializeField] private AudioClip defenseAppliedClip;
        [SerializeField] private AudioClip hoverClip;

        // Componentes cacheados
        private GazeInteractable _gazeInteractable;
        private Collider _collider;
        private Material _instancedMaterial;
        private Coroutine _popupCoroutine;

        #region Propiedades Públicas

        public string DefenseId => defenseId;
        public string DefenseTitle => defenseTitle;
        public string MitigationMechanism => mitigationMechanism;
        public bool IsApplied => isApplied;

        /// <summary>
        /// Configura programáticamente la contramedida desde builders procedurales (como SuspectBoardMeshBuilder).
        /// </summary>
        public void Configure(string id, string title, Renderer ledRenderer = null, Light ledLight = null, Image ledImage = null, TextMeshProUGUI labelText = null)
        {
            defenseId = id;
            defenseTitle = title;
            if (ledRenderer != null) statusIndicatorRenderer = ledRenderer;
            if (ledLight != null) statusLedLight = ledLight;
            if (ledImage != null) statusLedImage = ledImage;
            if (labelText != null) statusLabelText = labelText;

            AutoConfigureDefaults();

            if (statusIndicatorRenderer != null && materialIndex < statusIndicatorRenderer.sharedMaterials.Length)
            {
                _instancedMaterial = Application.isPlaying ? statusIndicatorRenderer.material : statusIndicatorRenderer.sharedMaterial;
            }

            if (_gazeInteractable == null) _gazeInteractable = GetComponent<GazeInteractable>();
            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = $"Activar {defenseTitle}";
            }

            UpdateVisualIndicators();
        }

        #endregion

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _gazeInteractable = GetComponent<GazeInteractable>();

            // Configurar capa interactiva y tag
            gameObject.layer = LayerMask.NameToLayer("Interactive");
            gameObject.tag = "DefenseControl";

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            // Configurar GazeInteractable si está presente
            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = $"Activar {defenseTitle}";
                _gazeInteractable.onClick.AddListener(TriggerDefense);
            }

            // Cachear material para cambios de color
            if (statusIndicatorRenderer != null && materialIndex < statusIndicatorRenderer.sharedMaterials.Length)
            {
                _instancedMaterial = Application.isPlaying ? statusIndicatorRenderer.materials[materialIndex] : statusIndicatorRenderer.sharedMaterials[materialIndex];
            }
        }

        private void Start()
        {
            AutoConfigureDefaults();

            if (defenseTitleText != null)
            {
                defenseTitleText.text = defenseTitle;
            }

            if (mitigationPopupPanel != null)
            {
                mitigationPopupPanel.SetActive(false);
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnDefenseApplied += HandleGlobalDefenseApplied;
                GameManager.I.OnModeChanged += HandleModeChanged;
            }

            SyncWithGameManager();
            UpdateVisualIndicators();
        }

        private void OnDestroy()
        {
            if (_gazeInteractable != null)
            {
                _gazeInteractable.onClick.RemoveListener(TriggerDefense);
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnDefenseApplied -= HandleGlobalDefenseApplied;
                GameManager.I.OnModeChanged -= HandleModeChanged;
            }
        }

        /// <summary>
        /// Asigna textos predeterminados según el ID oficial si no se configuraron en el Inspector.
        /// </summary>
        private void AutoConfigureDefaults()
        {
            switch (defenseId)
            {
                case "Defensa_Sandbox":
                    defenseTitle = "Filtro de correo con análisis en Sandbox";
                    mitigationMechanism = "Inspección automatizada en pasarela (Secure Email Gateway). Los enlaces y adjuntos se ejecutan en un entorno virtual aislado antes de llegar a los buzones, detonando el archivo '.scr.zip'.";
                    break;
                case "Defensa_BotonReportar":
                    defenseTitle = "Capacitación y botón \"Reportar phishing\"";
                    mitigationMechanism = "Cultura continua en el personal y un botón en el cliente de correo para alertar inmediatamente al SOC, permitiendo purgar los buzones en menos de un minuto.";
                    break;
                case "Defensa_MFA":
                    defenseTitle = "Gestor corporativo y doble factor (MFA)";
                    mitigationMechanism = "Autenticación robusta mediante tokens físicos dinámicos (TOTP), invalidando cualquier contraseña robada o expuesta físicamente en papel.";
                    break;
            }
        }

        /// <summary>
        /// Sincroniza el estado del interruptor con las defensas ya aplicadas en GameManager.
        /// </summary>
        private void SyncWithGameManager()
        {
            if (GameManager.I != null && GameManager.I.AppliedDefenses != null)
            {
                foreach (string def in GameManager.I.AppliedDefenses)
                {
                    if (string.Equals(def, defenseId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        isApplied = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Acción principal al hacer clic con la retícula:
        /// 1. Si está en Modo Ataque, cambia automáticamente a Modo Defensa.
        /// 2. Llama a GameManager.I.ApplyDefense(defenseId).
        /// 3. Cambia el indicador luminoso a verde brillante #38f28a.
        /// </summary>
        public void TriggerDefense()
        {
            if (isApplied)
            {
                ShowMitigationPopup();
                return;
            }

            PlaySound(toggleSwitchClip);

            // 1. Si el simulador está en Modo Ataque, cambiar automáticamente a Modo Defensa
            if (GameManager.I != null && GameManager.I.mode == SimMode.Ataque)
            {
                Debug.LogWarning($"{LogPrefix} Activando contramedida en Modo Ataque -> Cambiando automáticamente a <b>SimMode.Defensa</b>.");
                GameManager.I.SetMode(SimMode.Defensa);
            }

            isApplied = true;
            Debug.Log($"{LogPrefix} <color=#38F28A><b>¡Contramedida activada!</b></color> [{defenseId}]: {defenseTitle}");

            // 2. Notificar al GameManager oficial
            if (GameManager.I != null)
            {
                GameManager.I.ApplyDefense(defenseId);
            }

            PlaySound(defenseAppliedClip);

            // 3. Actualizar luces e indicadores visuales a #38f28a
            UpdateVisualIndicators();

            // 4. Desplegar panel forense explicativo
            ShowMitigationPopup();
        }

        private void HandleGlobalDefenseApplied(string appliedId)
        {
            if (string.Equals(appliedId, defenseId, System.StringComparison.OrdinalIgnoreCase))
            {
                isApplied = true;
                UpdateVisualIndicators();
            }
        }

        private void HandleModeChanged(SimMode newMode)
        {
            if (newMode == SimMode.Ataque)
            {
                // Si el usuario regresa al modo ataque, el interruptor se muestra como inactivo históricamente
                isApplied = false;
            }
            else
            {
                SyncWithGameManager();
            }

            UpdateVisualIndicators();
        }

        /// <summary>
        /// Actualiza luces, emisivos y textos: Rojo (Inactivo en 2016) vs Verde #38f28a (Activo).
        /// </summary>
        public void UpdateVisualIndicators()
        {
            Color targetColor = isApplied ? ActiveGreenColor : InactiveRedColor;
            string statusText = isApplied ? "CONTRAMEDIDA ACTIVA" : "INACTIVO EN 2016";

            // Luz 3D
            if (statusLedLight != null)
            {
                statusLedLight.color = targetColor;
                statusLedLight.intensity = isApplied ? 1.5f : 0.8f;
            }

            // Material emisivo
            if (_instancedMaterial == null && statusIndicatorRenderer != null && materialIndex < statusIndicatorRenderer.sharedMaterials.Length)
            {
                _instancedMaterial = Application.isPlaying ? statusIndicatorRenderer.material : statusIndicatorRenderer.sharedMaterial;
            }

            if (_instancedMaterial != null)
            {
                if (_instancedMaterial.HasProperty("_Color"))
                {
                    _instancedMaterial.SetColor("_Color", targetColor);
                }

                if (_instancedMaterial.HasProperty("_EmissionColor"))
                {
                    _instancedMaterial.SetColor("_EmissionColor", targetColor * (isApplied ? 0.8f : 0.2f));
                    _instancedMaterial.EnableKeyword("_EMISSION");
                }
            }

            // UI
            if (statusLedImage != null)
            {
                statusLedImage.color = targetColor;
            }

            if (statusLabelText != null)
            {
                statusLabelText.text = isApplied ? $"<color=#38F28A><b>[OK] {statusText}</b></color>" : $"<color=#FF4444>[-] {statusText}</color>";
            }

            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = isApplied ? $"[ACTIVA] {defenseTitle}" : $"Activar {defenseTitle}";
            }
        }

        private void ShowMitigationPopup()
        {
            if (mitigationPopupPanel == null) return;

            mitigationPopupPanel.SetActive(true);

            if (popupTitleText != null)
            {
                popupTitleText.text = $"<color=#38F28A>[OK] CONTRAMEDIDA ACTIVA</color>\n<b>{defenseTitle}</b>";
            }

            if (popupDetailText != null)
            {
                popupDetailText.text = mitigationMechanism;
            }

            if (popupAutoHideDuration > 0f)
            {
                if (_popupCoroutine != null) StopCoroutine(_popupCoroutine);
                _popupCoroutine = StartCoroutine(AutoHidePopup(popupAutoHideDuration));
            }
        }

        private IEnumerator AutoHidePopup(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (mitigationPopupPanel != null)
            {
                mitigationPopupPanel.SetActive(false);
            }
        }

        #region EventSystems Handlers

        public void OnPointerClick(PointerEventData eventData)
        {
            TriggerDefense();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlaySound(hoverClip);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        #endregion

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
            else if (clip == null && audioSource != null && isApplied)
            {
                AudioClip proceduralBeep = GenerateProceduralToggleTone();
                audioSource.PlayOneShot(proceduralBeep);
            }
        }

        private AudioClip GenerateProceduralToggleTone()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Clamp01(1f - (t / duration));
                samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.3f * env;
            }

            AudioClip clip = AudioClip.Create("Procedural_DefenseTone", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
