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
    /// Componente de zona interactiva (Hotspot) para las 3 señales de phishing en Panel_Correo (RF-03).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// 
    /// Las 3 regiones oficiales son:
    /// 1. "Signal_Remitente": Cabecera 'De: Rasel Ahlam <rasel.ahlam@career-applicants-bd.com>'
    ///    (Dominio externo no verificado suplantando a un postulante).
    /// 2. "Signal_Urgencia": Párrafo del cuerpo 'Les ruego revisar mi CV hoy mismo con carácter urgente...'
    ///    (Ingeniería social apelando a la amabilidad y urgencia artificial).
    /// 3. "Signal_Enlace": Hipervínculo 'Descargar Currículum: http://bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip'
    ///    (Enlace que apunta a un archivo comprimido con doble extensión ejecutable .scr.zip).
    /// 
    /// Al mirar y hacer clic en un hotspot:
    /// • Resalta el recuadro en color ámbar/rojo con la etiqueta [SEÑAL DE PHISHING DETECTADA].
    /// • Muestra la explicación técnica de por qué es un indicador de phishing.
    /// • Notifica a PhishingInspector y ScenarioFlowController para sumar al contador (1/3, 2/3, 3/3).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PhishingSignalHotspot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string LogPrefix = "[ANDES-SIM - Phishing Hotspot]";

        private static readonly Color AmberDetectedColor = new Color(1.0f, 0.62f, 0.26f, 1.0f); // Ámbar #ff9f43
        private static readonly Color RedAlertColor = new Color(1.0f, 0.32f, 0.32f, 1.0f);     // Rojo #ff5252
        private static readonly Color DefaultIdleColor = new Color(1.0f, 1.0f, 1.0f, 0.08f);

        [Header("--- Identificación de la Señal (RF-03) ---")]
        [Tooltip("Identificador exacto: 'Signal_Remitente', 'Signal_Urgencia' o 'Signal_Enlace'.")]
        [SerializeField] private string signalId = "Signal_Remitente";

        [Tooltip("Título descriptivo de la señal.")]
        [SerializeField] private string signalTitle = "Remitente Externo No Institucional";

        [TextArea(2, 5)]
        [Tooltip("Explicación forense y técnica de por qué este elemento constituye un indicador de phishing.")]
        [SerializeField] private string technicalExplanation =
            "El dominio del correo es 'career-applicants-bd.com', el cual no corresponde a ningún " +
            "sistema institucional del Banco Andino y carece de firmas de autenticación SPF/DKIM válidas.";

        [Header("--- Estado de Inspección ---")]
        [SerializeField] private bool isDetected = false;

        [Header("--- Resaltado Visual y Etiquetas ---")]
        [Tooltip("Imagen de borde o fondo del recuadro interactivo.")]
        [SerializeField] private Image hotspotBorderImage;

        [Tooltip("Etiqueta flotante que indica '[SEÑAL DE PHISHING DETECTADA]'.")]
        [SerializeField] private TextMeshProUGUI statusBadgeText;

        [Tooltip("Icono o marca de verificación.")]
        [SerializeField] private GameObject checkmarkBadge;

        [Header("--- Panel Flotante de Explicación Forense ---")]
        [SerializeField] private GameObject explanationTooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipTitleText;
        [SerializeField] private TextMeshProUGUI tooltipDetailText;
        [SerializeField] private float autoHideSeconds = 6.0f;

        [Header("--- Retroalimentación Sonora ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip signalDetectedClip;
        [SerializeField] private AudioClip hoverClip;

        // Componentes cacheados
        private GazeInteractable _gazeInteractable;
        private Collider _collider;
        private Coroutine _hideTooltipCoroutine;

        #region Propiedades Públicas

        public string SignalId => signalId;
        public string SignalTitle => signalTitle;
        public string TechnicalExplanation => technicalExplanation;
        public bool IsDetected => isDetected;

        #endregion

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _gazeInteractable = GetComponent<GazeInteractable>();

            // Configurar capa interactiva
            gameObject.layer = LayerMask.NameToLayer("Interactive");

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = $"Inspeccionar {signalTitle}";
                _gazeInteractable.onClick.AddListener(InspectSignal);
            }
        }

        private void Start()
        {
            AutoConfigureDefaults();

            if (statusBadgeText != null)
            {
                statusBadgeText.gameObject.SetActive(false);
            }

            if (checkmarkBadge != null)
            {
                checkmarkBadge.SetActive(false);
            }

            if (explanationTooltipPanel != null)
            {
                explanationTooltipPanel.SetActive(false);
            }

            UpdateVisualAppearance();
        }

        private void OnDestroy()
        {
            if (_gazeInteractable != null)
            {
                _gazeInteractable.onClick.RemoveListener(InspectSignal);
            }
        }

        /// <summary>
        /// Asigna automáticamente los textos técnicos oficiales según el ID de la señal.
        /// </summary>
        private void AutoConfigureDefaults()
        {
            switch (signalId)
            {
                case "Signal_Remitente":
                    signalTitle = "Remitente sospechoso y dominio no institucional";
                    technicalExplanation =
                        "De: Rasel Ahlam <rasel.ahlam@career-applicants-bd.com>\n" +
                        "Dominio público no verificado por SPF/DKIM institucional de Banco Andino. " +
                        "Suplanta la identidad de un postulante a vacante bancaria para generar confianza inicial.";
                    break;

                case "Signal_Urgencia":
                    signalTitle = "Sentido artificial de urgencia y presión psicológica";
                    technicalExplanation =
                        "Texto: '...revisar mi CV hoy mismo con carácter urgente antes del cierre de selección...'\n" +
                        "Ingeniería social clásica diseñada para forzar a la víctima a omitir los protocolos " +
                        "de seguridad y actuar apresuradamente por amabilidad o miedo a perder una oportunidad.";
                    break;

                case "Signal_Enlace":
                    signalTitle = "Enlace con doble extensión ejecutable (.scr.zip)";
                    technicalExplanation =
                        "URL: http://bit.ly-cv-storage.org/Rasel_Ahlam_CV_2015.scr.zip\n" +
                        "Apunta a un servidor externo desconocido descargando un ejecutable Screensaver (.scr) " +
                        "camuflado dentro de un archivo .zip para burlar filtros básicos de antivirus.";
                    break;
            }
        }

        /// <summary>
        /// Acción ejecutada al mirar y hacer clic en el hotspot:
        /// 1. Resalta el recuadro en ámbar/rojo con [SEÑAL DE PHISHING DETECTADA].
        /// 2. Despliega la explicación técnica forense.
        /// 3. Notifica a PhishingEmailInspector y ScenarioFlowController.
        /// </summary>
        public void InspectSignal()
        {
            if (isDetected)
            {
                // Si ya fue detectada, alternar tooltip explicativo
                ToggleTooltip();
                return;
            }

            isDetected = true;
            Debug.Log($"{LogPrefix} <color=#FF9F43><b>¡Señal de Phishing Identificada!</b></color> [{signalId}]: {signalTitle}");

            PlaySound(signalDetectedClip);

            // 1. Resaltar visualmente en color ámbar/rojo
            UpdateVisualAppearance();

            // 2. Mostrar tooltip con la explicación técnica
            ShowExplanationTooltip();

            // 3. Notificar a PhishingInspector oficial y PhishingEmailInspector
            if (PhishingInspector.I != null)
            {
                PhishingInspector.I.MarkPhishingSignal(signalId);
            }
            else
            {
                PhishingInspector inspector = FindFirstObjectByType<PhishingInspector>();
                if (inspector != null)
                {
                    inspector.MarkPhishingSignal(signalId);
                }
            }

            if (PhishingEmailInspector.I != null)
            {
                PhishingEmailInspector.I.OnSignalInspected(signalId);
            }
            else
            {
                PhishingEmailInspector inspectorLegacy = FindFirstObjectByType<PhishingEmailInspector>();
                if (inspectorLegacy != null)
                {
                    inspectorLegacy.OnSignalInspected(signalId);
                }
            }

            // 4. Notificar a ScenarioFlowController para la máquina de estados de 6 pasos
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.NotifyPhishingSignalInspected(signalId);
            }
        }

        private void UpdateVisualAppearance()
        {
            if (!isDetected)
            {
                if (hotspotBorderImage != null)
                {
                    hotspotBorderImage.color = DefaultIdleColor;
                }
                return;
            }

            // Resaltar en ámbar/rojo
            if (hotspotBorderImage != null)
            {
                hotspotBorderImage.color = AmberDetectedColor;
            }

            if (statusBadgeText != null)
            {
                statusBadgeText.gameObject.SetActive(true);
                statusBadgeText.text = "<color=#FF9F43><b>[SEÑAL DE PHISHING DETECTADA]</b></color>";
            }

            if (checkmarkBadge != null)
            {
                checkmarkBadge.SetActive(true);
            }

            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = $"[DETECTADA] {signalTitle}";
            }
        }

        private void ShowExplanationTooltip()
        {
            if (explanationTooltipPanel == null) return;

            explanationTooltipPanel.SetActive(true);

            if (tooltipTitleText != null)
            {
                tooltipTitleText.text = $"<color=#FF9F43>SEÑAL IDENTIFICADA:</color>\n<b>{signalTitle}</b>";
            }

            if (tooltipDetailText != null)
            {
                tooltipDetailText.text = technicalExplanation;
            }

            if (autoHideSeconds > 0f)
            {
                if (_hideTooltipCoroutine != null) StopCoroutine(_hideTooltipCoroutine);
                _hideTooltipCoroutine = StartCoroutine(AutoHideTooltip(autoHideSeconds));
            }
        }

        private void ToggleTooltip()
        {
            if (explanationTooltipPanel == null) return;
            bool active = !explanationTooltipPanel.activeSelf;
            explanationTooltipPanel.SetActive(active);

            if (active && autoHideSeconds > 0f)
            {
                if (_hideTooltipCoroutine != null) StopCoroutine(_hideTooltipCoroutine);
                _hideTooltipCoroutine = StartCoroutine(AutoHideTooltip(autoHideSeconds));
            }
        }

        private IEnumerator AutoHideTooltip(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (explanationTooltipPanel != null)
            {
                explanationTooltipPanel.SetActive(false);
            }
        }

        #region EventSystems Handlers

        public void OnPointerClick(PointerEventData eventData)
        {
            InspectSignal();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlaySound(hoverClip);

            if (!isDetected && hotspotBorderImage != null)
            {
                hotspotBorderImage.color = new Color(1f, 0.8f, 0.3f, 0.25f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isDetected && hotspotBorderImage != null)
            {
                hotspotBorderImage.color = DefaultIdleColor;
            }
        }

        #endregion

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
            else if (clip == null && audioSource != null && isDetected)
            {
                AudioClip proceduralBeep = GenerateProceduralDiscoveryTone();
                audioSource.PlayOneShot(proceduralBeep);
            }
        }

        private AudioClip GenerateProceduralDiscoveryTone()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Clamp01(1f - (t / duration));
                samples[i] = Mathf.Sin(2f * Mathf.PI * 784f * t) * 0.3f * env; // G5 note
            }

            AudioClip clip = AudioClip.Create("Procedural_PhishingSignalChime", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
