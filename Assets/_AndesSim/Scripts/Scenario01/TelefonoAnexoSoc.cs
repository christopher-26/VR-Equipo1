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
    /// Controlador del objeto interactivo 'Telefono_Anexo_SOC' en el escritorio de Carla Mendoza.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Opera en la capa "Interactive" (Layer 8) con GazeInteractable y retícula VR.
    /// 
    /// Especificación técnica:
    /// • Teléfono IP en el escritorio de Carla Mendoza.
    /// • Al mirarlo y tocarlo, permite llamar al Anexo de Seguridad TI (Ext. 4040) para reportar
    ///   el correo sospechoso de 'Rasel Ahlam'.
    /// • Muestra diálogo con subtítulos en World Space en 3D y reproduce audio sintetizado
    ///   (tonos DTMF de marcado, tono de timbrado y transmisión de radio del SOC).
    /// • Aplica la contramedida oficial 'Defensa_BotonReportar' y coordina la contención con MailQuarantine.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TelefonoAnexoSoc : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string LogPrefix = "[ANDES-SIM - Telefono SOC]";
        private static readonly Color IdleBlueColor = new Color(0.2f, 0.7f, 1f, 1f);     // #33b3ff
        private static readonly Color CallingAmberColor = new Color(1.0f, 0.65f, 0.2f, 1f); // #ffa633
        private static readonly Color ConnectedGreenColor = new Color(0.22f, 0.95f, 0.54f, 1f); // #38f28a

        [Header("--- Identificadores del Anexo Telefónico ---")]
        [SerializeField] private string callerId = "Carla Mendoza (Ext. 2401 - Operaciones SWIFT)";
        [SerializeField] private string destinationAnexo = "Ext. 4040 (SOC · Seguridad TI Banco Andino)";

        [Header("--- Indicadores Luminosos y Pantalla del Teléfono ---")]
        [Tooltip("Renderer del LED de llamada en el chasis del teléfono.")]
        [SerializeField] private Renderer callLedRenderer;
        [SerializeField] private Light callIndicatorLight;

        [Tooltip("Pantalla LCD pequeña sobre el teléfono IP.")]
        [SerializeField] private TextMeshPro lcdScreenText;

        [Header("--- Panel Flotante 3D de Diálogo y Subtítulos (World Space) ---")]
        [Tooltip("Canvas o panel 3D flotante sobre el teléfono.")]
        [SerializeField] private GameObject dialogCanvasPanel;
        [SerializeField] private TextMeshProUGUI dialogHeaderTitleText;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI subtitleContentText;
        [SerializeField] private Image dialogStatusBadge;

        [Header("--- Tiempos del Diálogo ---")]
        [SerializeField] private float dialDuration = 1.0f;
        [SerializeField] private float carlaDialogueDuration = 3.5f;
        [SerializeField] private float socDialogueDuration = 4.0f;

        [Header("--- Efectos Sonoros ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip phonePickUpClip;
        [SerializeField] private AudioClip dtmfDialClip;
        [SerializeField] private AudioClip radioSquelchClip;
        [SerializeField] private AudioClip callEndChimeClip;

        // Estado interno
        private GazeInteractable _gazeInteractable;
        private Collider _collider;
        private bool _isCallInProgress = false;
        private bool _isReportCompleted = false;
        private Coroutine _callRoutine;

        #region Propiedades Públicas

        public bool IsCallInProgress => _isCallInProgress;
        public string CallerId => callerId;
        public string DestinationAnexo => destinationAnexo;
        public bool IsReportCompleted => _isReportCompleted;

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
                    audioSource.spatialBlend = 1f; // Audio 3D VR
                    audioSource.minDistance = 0.3f;
                    audioSource.maxDistance = 6f;
                }
            }
        }

        private void Start()
        {
            if (_gazeInteractable == null)
            {
                _gazeInteractable = gameObject.AddComponent<GazeInteractable>();
            }
            _gazeInteractable.interactableLabel = "Llamar al Anexo SOC (Ext. 4040) · Reportar Amenaza";

            if (dialogCanvasPanel != null)
            {
                dialogCanvasPanel.SetActive(false);
            }

            SetLedState(IdleBlueColor, "ANEXO 2401 · LISTO");
        }

        #region Eventos de Retícula VR (Gaze & Touch)

        public void OnPointerClick(PointerEventData eventData)
        {
            StartSocReportCall();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isCallInProgress)
            {
                PlaySound(null, GenerateDtmfTone(1));
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        #endregion

        #region Secuencia de Llamada y Diálogo con el SOC

        /// <summary>
        /// Inicia la llamada interactiva al SOC con subtítulos y audio sincronizado.
        /// </summary>
        public void StartSocReportCall()
        {
            if (_isCallInProgress) return;

            if (_callRoutine != null)
            {
                StopCoroutine(_callRoutine);
            }
            _callRoutine = StartCoroutine(SocCallDialogueRoutine());
        }

        private IEnumerator SocCallDialogueRoutine()
        {
            _isCallInProgress = true;
            Debug.Log($"{LogPrefix} <color=#4EC9B0><b>Iniciando llamada al Anexo 4040 (SOC Seguridad TI)...</b></color>");

            // 1. Descolgar y marcar DTMF
            SetLedState(CallingAmberColor, "MARCANDO: 4040 [SOC]");
            PlaySound(phonePickUpClip, GenerateDtmfTone(4));

            if (dialogCanvasPanel != null)
            {
                dialogCanvasPanel.SetActive(true);
            }

            if (dialogHeaderTitleText != null)
            {
                dialogHeaderTitleText.text = "<b><color=#FFA633>[SOC] MARCANDO AL ANEXO 4040 · SEGURIDAD TI</color></b>";
            }

            if (speakerNameText != null)
            {
                speakerNameText.text = "Conectando con la Central de Incidentes...";
            }

            if (subtitleContentText != null)
            {
                subtitleContentText.text = "<i>[Tonos de marcado DTMF: 4 · 0 · 4 · 0 ... Estableciendo canal seguro VoIP]</i>";
            }

            yield return new WaitForSeconds(dialDuration);

            // 2. Línea 1: Carla Mendoza habla
            SetLedState(CallingAmberColor, "EN LLAMADA: 2401 -> 4040");
            PlaySound(null, GenerateRadioChirpTone());

            if (dialogHeaderTitleText != null)
            {
                dialogHeaderTitleText.text = "<b><color=#4EC9B0>[SOC] CANAL DE EMERGENCIA TI ACTIVO · ANEXO 4040</color></b>";
            }

            if (speakerNameText != null)
            {
                speakerNameText.text = "<b><color=#4EC9B0>Carla Mendoza (Ext. 2401 · Operaciones SWIFT):</color></b>";
            }

            if (subtitleContentText != null)
            {
                subtitleContentText.text =
                    "\"¡Aló, Seguridad TI! Les habla Carla Mendoza de Operaciones de Pagos.\n" +
                    "Acabo de recibir un correo sospechoso de <b>'Rasel Ahlam'</b> con un currículum adjunto en archivo ZIP urgente. " +
                    "No lo he ejecutado, pero tiene señales claras de suplantación y phishing.\"";
            }

            yield return new WaitForSeconds(carlaDialogueDuration);

            // 3. Línea 2: Operador del SOC responde
            SetLedState(ConnectedGreenColor, "SOC: CONTENCIÓN ACTIVA");
            PlaySound(radioSquelchClip, GenerateRadioTransmissionTone());

            if (speakerNameText != null)
            {
                speakerNameText.text = "<b><color=#38F28A>Javier Morales · Analista Senior SOC (Ext. 4040):</color></b>";
            }

            if (subtitleContentText != null)
            {
                subtitleContentText.text =
                    "\"¡Excelente reporte, Carla! Nuestro Sandbox acaba de interceptar el archivo <b>'Rasel_Ahlam_CV_2015.scr.zip'</b>: " +
                    "contiene un backdoor del grupo hostil Lazarus. ¡No toques ningún enlace!\n" +
                    "Estamos aplicando la regla de bloqueo perimetral y purgando los 16 buzones del banco ahora mismo.\"";
            }

            // Aplicar contramedida oficial en GameManager
            if (GameManager.I != null)
            {
                GameManager.I.ApplyDefense("Defensa_BotonReportar");
            }

            // Activar aislamiento perimetral en MailQuarantine
            if (MailQuarantine.I != null)
            {
                MailQuarantine.I.QuarantineEmail();
            }

            yield return new WaitForSeconds(socDialogueDuration);

            // 4. Conclusión de la llamada
            PlaySound(callEndChimeClip, GenerateCallEndTone());
            _isReportCompleted = true;
            _isCallInProgress = false;

            if (dialogHeaderTitleText != null)
            {
                dialogHeaderTitleText.text = "<b><color=#38F28A>[OK] REPORTE TELEFONICO COMPLETADO - AMENAZA AISLADA</color></b>";
            }

            if (speakerNameText != null)
            {
                speakerNameText.text = "<b><color=#38F28A>ESTADO: PROTOCOLO DE DEFENSA SOC EJECUTADO</color></b>";
            }

            if (subtitleContentText != null)
            {
                subtitleContentText.text =
                    "<color=#38F28A>• Remitente hostil 'career-applicants-bd.com' bloqueado en el firewall.\n" +
                    "• 16 buzones corporativos desinfectados preventivamente.\n" +
                    "• 0 equipos infectados en la red de pagos del Banco Andino.</color>";
            }

            SetLedState(ConnectedGreenColor, "ANEXO 4040 · REPORTE EXITOSO");

            if (_gazeInteractable != null)
            {
                _gazeInteractable.interactableLabel = "Anexo SOC (Ext. 4040) · [Reporte Confirmado]";
            }

            Debug.Log($"{LogPrefix} <color=#38F28A><b>¡Reporte por anexo telefónico finalizado exitosamente!</b></color>");
        }

        private void SetLedState(Color color, string lcdMessage)
        {
            if (callLedRenderer != null)
            {
                Material mat = Application.isPlaying ? callLedRenderer.material : callLedRenderer.sharedMaterial;
                if (mat != null)
                {
                    mat.color = color;
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", color * 1.2f);
                    }
                }
            }

            if (callIndicatorLight != null)
            {
                callIndicatorLight.color = color;
            }

            if (lcdScreenText != null)
            {
                lcdScreenText.text = lcdMessage;
            }
        }

        #endregion

        #region Generación Procedural de Audio (VR Sin Dependencias Externas)

        private void PlaySound(AudioClip clip, AudioClip fallback = null)
        {
            if (audioSource == null) return;
            AudioClip toPlay = clip != null ? clip : fallback;
            if (toPlay != null)
            {
                audioSource.PlayOneShot(toPlay, 0.9f);
            }
        }

        private static AudioClip GenerateDtmfTone(int digit)
        {
            int sampleRate = 44100;
            float duration = 0.18f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            // Frecuencias estándar DTMF (Hz)
            float lowFreq = 770f;
            float highFreq = 1336f;
            if (digit == 4) { lowFreq = 770f; highFreq = 1209f; }
            else if (digit == 0) { lowFreq = 941f; highFreq = 1336f; }

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(t / duration * Mathf.PI);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * lowFreq * t) * 0.25f +
                              Mathf.Sin(2f * Mathf.PI * highFreq * t) * 0.25f) * env;
            }

            AudioClip clip = AudioClip.Create("Phone_DTMF", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateRadioChirpTone()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = Mathf.Lerp(600f, 1200f, t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.2f;
            }

            AudioClip clip = AudioClip.Create("Radio_Chirp", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateRadioTransmissionTone()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration);
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.2f;
                samples[i] = (Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.2f + noise) * env;
            }

            AudioClip clip = AudioClip.Create("Radio_Squelch", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateCallEndTone()
        {
            int sampleRate = 44100;
            float duration = 0.45f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-4f * t);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * 587.33f * t) * 0.2f +
                              Mathf.Sin(2f * Mathf.PI * 880.00f * t) * 0.2f) * env;
            }

            AudioClip clip = AudioClip.Create("Call_End", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
