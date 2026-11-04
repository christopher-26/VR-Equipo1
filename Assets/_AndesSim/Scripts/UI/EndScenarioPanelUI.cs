using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Data;
using AndesSim.VR;

namespace AndesSim.UI
{
    /// <summary>
    /// Panel holográfico curvo en World Space para el bloque "Final: resultado y resumen" (Paso 6 de la arquitectura).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// 
    /// Se despliega automáticamente frente al jugador cuando GameManager.I.EndScenario() es invocado:
    /// 
    /// 1. Si terminó en Modo Ataque (2016):
    ///    - Título en rojo #ff5a4e: "RESULTADO MODO ATAQUE (2016): RED COMPROMETIDA"
    ///    - Resumen de hechos: 16 empleados recibieron el correo de «Rasel Ahlam» el 29 de enero de 2015;
    ///      al menos 3 computadoras intentaron abrir el archivo; la ausencia de sandbox, el desconocimiento de phishing
    ///      y las credenciales anotadas de Carla Mendoza permitieron a Lazarus abrir una puerta trasera hacia la
    ///      terminal SWIFT (que culminaría en las 36 órdenes por US$ 951 millones).
    ///    - Botón grande interactivo por mirada: "PROBAR AHORA EL MODO DEFENSA" (cambia a SimMode.Defensa sin salir de la app).
    /// 
    /// 2. Si terminó en Modo Defensa:
    ///    - Título en verde #38f28a: "RESULTADO MODO DEFENSA: ATAQUE DETENIDO EN EL ESLABÓN 1"
    ///    - Resumen de contramedidas: El filtro de correo con sandbox aisló el enlace malicioso en cuarentena,
    ///      el botón «Reportar phishing» alertó al SOC y el gestor de contraseñas con MFA protegió las credenciales
    ///      de Carla Mendoza. Infecciones: 0 · Dinero robado: US$ 0.
    ///    - Botones interactivos por mirada para:
    ///      • "Ver Expediente de los 4 Sospechosos"
    ///      • "Ver Créditos y Licencias"
    ///      • "Reiniciar Simulación"
    /// </summary>
    public class EndScenarioPanelUI : MonoBehaviour
    {
        public static EndScenarioPanelUI I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - EndScenarioPanelUI]";

        #region Paleta de Colores Oficial

        private static readonly Color ColorDarkBg        = new Color(0.008f, 0.039f, 0.024f, 0.96f); // #020a06
        private static readonly Color ColorLightText     = new Color(0.839f, 0.949f, 0.886f, 1.00f); // #d6f2e2
        private static readonly Color ColorCodeGreen     = new Color(0.220f, 0.950f, 0.540f, 1.00f); // #38f28a
        private static readonly Color ColorAlertRed      = new Color(1.000f, 0.353f, 0.306f, 1.00f); // #ff5a4e
        private static readonly Color ColorWarningAmber  = new Color(1.000f, 0.761f, 0.290f, 1.00f); // #ffc24a

        #endregion

        [Header("--- Contenedor del Panel Holográfico ---")]
        [Tooltip("Contenedor raíz del Canvas World Space.")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("--- Posicionamiento Dinámico VR ---")]
        [Tooltip("Distancia en metros frente al jugador al abrirse.")]
        [SerializeField] private float spawnDistanceInFront = 2.2f;
        [SerializeField] private Transform targetCamera;

        [Header("--- Elementos de Cabecera y Resumen ---")]
        [SerializeField] private TextMeshProUGUI outcomeHeaderTitleText;
        [SerializeField] private Image outcomeHeaderBorder;
        [SerializeField] private TextMeshProUGUI outcomeSubtitleText;
        [SerializeField] private TextMeshProUGUI technicalSummaryBodyText;
        [SerializeField] private TextMeshProUGUI acceptanceCriterionBadgeText;

        [Header("--- Métricas de Impacto Financiero y Operativo ---")]
        [SerializeField] private TextMeshProUGUI infectedHostsMetricText;
        [SerializeField] private TextMeshProUGUI stolenMoneyMetricText;
        [SerializeField] private TextMeshProUGUI swiftStatusMetricText;

        [Header("--- Panel y Botón de Modo Ataque ---")]
        [SerializeField] private GameObject attackActionContainer;
        [SerializeField] private Button tryDefenseModeButton;
        [SerializeField] private TextMeshProUGUI tryDefenseButtonText;
        [SerializeField] private Image tryDefenseButtonGlow;

        [Header("--- Panel y Botones de Modo Defensa ---")]
        [SerializeField] private GameObject defenseActionContainer;
        [SerializeField] private Button viewSuspectsButton;
        [SerializeField] private Button viewCreditsButton;
        [SerializeField] private Button restartSimulationButton;

        [Header("--- Modal de Créditos y Atribución ---")]
        [SerializeField] private GameObject creditsModalPanel;
        [SerializeField] private TextMeshProUGUI creditsBodyText;
        [SerializeField] private Button closeCreditsButton;

        [Header("--- Audio y Retroalimentación ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip attackDefeatClip;
        [SerializeField] private AudioClip defenseVictoryClip;
        [SerializeField] private AudioClip buttonClickClip;

        // Estado interno
        private bool _isShowing = false;
        private Coroutine _fadeCoroutine;

        private void Awake()
        {
            if (I == null)
            {
                I = this;
            }
            else if (I != this)
            {
                Debug.LogWarning($"{LogPrefix} Instancia duplicada detectada en '{gameObject.name}'. Destruyendo.");
                Destroy(this);
                return;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0.4f;
                    audioSource.minDistance = 0.5f;
                    audioSource.maxDistance = 15f;
                }
            }
        }

        private void Start()
        {
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }

            EnsurePanelStructure();
            SetupButtonListeners();

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (creditsModalPanel != null)
            {
                creditsModalPanel.SetActive(false);
            }

            // Suscribirse al evento oficial de cierre en GameManager
            if (GameManager.I != null)
            {
                GameManager.I.OnScenarioEnded += HandleScenarioEnded;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnScenarioEnded -= HandleScenarioEnded;
            }
        }

        private void SetupButtonListeners()
        {
            if (tryDefenseModeButton != null)
            {
                tryDefenseModeButton.onClick.RemoveAllListeners();
                tryDefenseModeButton.onClick.AddListener(OnTryDefenseModeClicked);
            }

            if (viewSuspectsButton != null)
            {
                viewSuspectsButton.onClick.RemoveAllListeners();
                viewSuspectsButton.onClick.AddListener(OnViewSuspectsClicked);
            }

            if (viewCreditsButton != null)
            {
                viewCreditsButton.onClick.RemoveAllListeners();
                viewCreditsButton.onClick.AddListener(OnViewCreditsClicked);
            }

            if (closeCreditsButton != null)
            {
                closeCreditsButton.onClick.RemoveAllListeners();
                closeCreditsButton.onClick.AddListener(OnCloseCreditsClicked);
            }

            if (restartSimulationButton != null)
            {
                restartSimulationButton.onClick.RemoveAllListeners();
                restartSimulationButton.onClick.AddListener(OnRestartSimulationClicked);
            }
        }

        #region Manejador de Fin de Escenario

        private void HandleScenarioEnded(string summary, bool timeCriterionMet)
        {
            DisplayEndScenarioOutcome(timeCriterionMet);
        }

        /// <summary>
        /// Despliega el panel holográfico curvo orientado frente al jugador.
        /// </summary>
        public void DisplayEndScenarioOutcome(bool timeCriterionMet)
        {
            if (_isShowing) return;
            _isShowing = true;

            // 1. Posicionar frente a la vista del jugador en World Space
            PositionPanelInFrontOfPlayer();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            SimMode currentMode = GameManager.I != null ? GameManager.I.mode : SimMode.Ataque;
            float elapsed = GameManager.I != null ? GameManager.I.ElapsedTime : 0f;

            // 2. Configurar contenidos según el modo oficial
            if (currentMode == SimMode.Ataque)
            {
                ConfigureAttackModeOutcome(timeCriterionMet, elapsed);
            }
            else
            {
                ConfigureDefenseModeOutcome(timeCriterionMet, elapsed);
            }

            // 3. Animación de fade in suave
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeInPanelRoutine());

            Debug.Log($"{LogPrefix} <b>Panel final de escenario desplegado con éxito en Modo: {currentMode}</b>");
        }

        private void ConfigureAttackModeOutcome(bool timeCriterionMet, float elapsed)
        {
            PlaySound(attackDefeatClip, GenerateAlertBeepTone());

            // Título en rojo #ff5a4e
            if (outcomeHeaderTitleText != null)
            {
                outcomeHeaderTitleText.text = "<b><color=#FF5A4E>RESULTADO MODO ATAQUE (2016): RED COMPROMETIDA</color></b>";
            }

            if (outcomeHeaderBorder != null)
            {
                outcomeHeaderBorder.color = ColorAlertRed;
            }

            if (outcomeSubtitleText != null)
            {
                outcomeSubtitleText.text = "INTRUSIÓN PERIMETRAL EXITOSA DEL GRUPO LAZARUS · PERSISTENCIA INICIADA";
                outcomeSubtitleText.color = ColorWarningAmber;
            }

            // Resumen de hechos oficial según especificación
            if (technicalSummaryBodyText != null)
            {
                technicalSummaryBodyText.text =
                    "<b>Resumen de Hechos Históricos:</b>\n" +
                    "16 empleados recibieron el correo de \"Rasel Ahlam\" el 29 de enero de 2015; al menos 3 computadoras " +
                    "intentaron abrir el archivo; la ausencia de sandbox, el desconocimiento de phishing y las credenciales anotadas " +
                    "de Carla Mendoza permitieron a Lazarus abrir una puerta trasera hacia la terminal SWIFT " +
                    "(que culminaría en las 36 órdenes fraudulentas por <b>US$ 951 millones</b>).";
                technicalSummaryBodyText.color = ColorLightText;
            }

            // Métricas
            if (infectedHostsMetricText != null)
            {
                infectedHostsMetricText.text = "<b>COMPUTADORAS INFECTADAS:</b> <color=#FF5A4E>3 / 3 (100%)</color>";
            }

            if (stolenMoneyMetricText != null)
            {
                stolenMoneyMetricText.text = "<b>ROBO POTENCIAL SWIFT:</b> <color=#FF5A4E>US$ 951,000,000</color>";
            }

            if (swiftStatusMetricText != null)
            {
                swiftStatusMetricText.text = "<b>CANAL C2:</b> <color=#FF5A4E>185.130.44.82:443 (Backdoor Activo)</color>";
            }

            // Criterio de tiempo
            if (acceptanceCriterionBadgeText != null)
            {
                string eval = timeCriterionMet ? "<color=#38F28A>[CRITERIO CUMPLIDO]</color>" : "<color=#FF5A4E>[TIEMPO EXCEDIDO]</color>";
                acceptanceCriterionBadgeText.text = $"[TIEMPO] Empleado en la misión: <b>{elapsed:F1}s / 180s</b> {eval}";
            }

            // Contenedores de botones
            if (attackActionContainer != null) attackActionContainer.SetActive(true);
            if (defenseActionContainer != null) defenseActionContainer.SetActive(false);
        }

        private void ConfigureDefenseModeOutcome(bool timeCriterionMet, float elapsed)
        {
            PlaySound(defenseVictoryClip, GenerateVictoryFanfareTone());

            // Título en verde #38f28a
            if (outcomeHeaderTitleText != null)
            {
                outcomeHeaderTitleText.text = "<b><color=#38F28A>RESULTADO MODO DEFENSA: ATAQUE DETENIDO EN EL ESLABÓN 1</color></b>";
            }

            if (outcomeHeaderBorder != null)
            {
                outcomeHeaderBorder.color = ColorCodeGreen;
            }

            if (outcomeSubtitleText != null)
            {
                outcomeSubtitleText.text = "PROTECCIÓN EN PROFUNDIDAD SOC · BANCO ANDINO ASEGURADO";
                outcomeSubtitleText.color = ColorCodeGreen;
            }

            // Resumen de contramedidas oficial según especificación
            if (technicalSummaryBodyText != null)
            {
                technicalSummaryBodyText.text =
                    "<b>Resumen de Mitigación Defensiva:</b>\n" +
                    "El filtro de correo con sandbox aisló el enlace malicioso en cuarentena, el botón \"Reportar phishing\" " +
                    "alertó al SOC y el gestor de contraseñas con MFA protegió las credenciales de Carla Mendoza.\n\n" +
                    "<b><color=#38F28A>• Infecciones: 0</color>   |   <color=#38F28A>• Dinero robado: US$ 0</color>   |   <color=#38F28A>• Red SWIFT: Intacta</color></b>";
                technicalSummaryBodyText.color = ColorLightText;
            }

            // Métricas
            if (infectedHostsMetricText != null)
            {
                infectedHostsMetricText.text = "<b>COMPUTADORAS INFECTADAS:</b> <color=#38F28A>0 / 3 (0%)</color>";
            }

            if (stolenMoneyMetricText != null)
            {
                stolenMoneyMetricText.text = "<b>DINERO SUSTRAÍDO:</b> <color=#38F28A>US$ 0 (Protegido)</color>";
            }

            if (swiftStatusMetricText != null)
            {
                swiftStatusMetricText.text = "<b>INTEGRIDAD DE RED:</b> <color=#38F28A>100% Cifrada y Segura (MFA Activo)</color>";
            }

            // Criterio de tiempo
            if (acceptanceCriterionBadgeText != null)
            {
                string eval = timeCriterionMet ? "<color=#38F28A>[CRITERIO CUMPLIDO]</color>" : "<color=#FF5A4E>[TIEMPO EXCEDIDO]</color>";
                acceptanceCriterionBadgeText.text = $"[TIEMPO] Respuesta del equipo: <b>{elapsed:F1}s / 180s</b> {eval}";
            }

            // Contenedores de botones
            if (attackActionContainer != null) attackActionContainer.SetActive(false);
            if (defenseActionContainer != null) defenseActionContainer.SetActive(true);
        }

        private void PositionPanelInFrontOfPlayer()
        {
            Transform cam = targetCamera != null ? targetCamera : (Camera.main != null ? Camera.main.transform : null);
            if (cam == null) return;

            Vector3 forward = cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 targetPos = cam.position + (forward * spawnDistanceInFront) + (Vector3.up * -0.1f);
            transform.position = targetPos;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
        }

        private IEnumerator FadeInPanelRoutine()
        {
            if (panelCanvasGroup == null) yield break;

            panelCanvasGroup.alpha = 0f;
            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            panelCanvasGroup.alpha = 1f;
        }

        #endregion

        #region Acciones de Botones Interactivos por Mirada

        /// <summary>
        /// Botón 1 (Modo Ataque): "PROBAR AHORA EL MODO DEFENSA"
        /// Cambia a SimMode.Defensa sin salir de la app, resetea contadores y teletransporta a Puesto 1.
        /// </summary>
        public void OnTryDefenseModeClicked()
        {
            PlaySound(buttonClickClip, GenerateClickTone());
            Debug.Log($"{LogPrefix} Jugador eligió: <b>'PROBAR AHORA EL MODO DEFENSA'</b>");

            ClosePanel();

            if (GameManager.I != null)
            {
                GameManager.I.ResetSimulation();
                GameManager.I.SetMode(SimMode.Defensa);
                GameManager.I.StartSimulationTimer();
            }

            // Avanzar el flujo al paso de inspección
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.SetStep(ScenarioStep.Step3_RevisarBandeja);
            }

            // Teletransportar frente a Puesto 1
            if (PlayerTeleportController.I != null)
            {
                TeleportPoint tp1 = null;
                TeleportPoint[] points = FindObjectsByType<TeleportPoint>(FindObjectsSortMode.None);
                foreach (var p in points)
                {
                    if (p.gameObject.name.IndexOf("01", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        tp1 = p;
                        break;
                    }
                }

                if (tp1 != null)
                {
                    PlayerTeleportController.I.TeleportToPoint(tp1);
                }
            }
        }

        /// <summary>
        /// Botón 2 (Modo Defensa): "Ver Expediente de los 4 Sospechosos"
        /// Teletransporta al jugador frente al mural interactivo de sospechosos.
        /// </summary>
        public void OnViewSuspectsClicked()
        {
            PlaySound(buttonClickClip, GenerateClickTone());
            Debug.Log($"{LogPrefix} Jugador eligió: <b>'Ver Expediente de los 4 Sospechosos'</b>");

            ClosePanel();

            // Buscar el tablero de sospechosos VR o el punto de teletransporte
            SuspectBoardVR boardVR = FindAnyObjectByType<SuspectBoardVR>();
            GameObject suspectMural = boardVR != null ? boardVR.gameObject : GameObject.Find("Mural_Sospechosos_Root");
            if (suspectMural == null) suspectMural = GameObject.Find("Tablero_Sospechosos_RF02");

            if (suspectMural != null && PlayerTeleportController.I != null)
            {
                Vector3 viewPos = suspectMural.transform.position + (suspectMural.transform.forward * 1.8f);
                viewPos.y = 0f;
                Vector3 lookDir = (suspectMural.transform.position - new Vector3(viewPos.x, suspectMural.transform.position.y, viewPos.z)).normalized;
                Quaternion lookRot = Quaternion.LookRotation(lookDir);
                PlayerTeleportController.I.TeleportTo(viewPos, lookRot);
            }
        }

        /// <summary>
        /// Botón 3 (Modo Defensa): "Ver Créditos y Licencias"
        /// Despliega el panel modal de autoría institucional y atribución técnica.
        /// </summary>
        public void OnViewCreditsClicked()
        {
            PlaySound(buttonClickClip, GenerateClickTone());

            if (CreditsAndEthicsModalUI.I != null)
            {
                CreditsAndEthicsModalUI.I.OpenModal();
                return;
            }

            if (creditsModalPanel != null)
            {
                creditsModalPanel.SetActive(true);
            }

            if (creditsBodyText != null)
            {
                creditsBodyText.text =
                    "<b><color=#38F28A>ANDES-SIM · SIMULADOR VR DE CIBERSEGURIDAD BANCARIA</color></b>\n" +
                    "<b>Escenario 1:</b> Oficina de Pagos · Caso Banco Andino (Equipo 1)\n\n" +
                    "<b>Desarrollo y Arquitectura Técnica:</b>\n" +
                    "• Motor: Unity 6 (6000.3.24f1) | Android OpenGLES3\n" +
                    "• SDK VR: Google Cardboard XR Plugin (Gaze Reticle + Touch Interaction)\n" +
                    "• Tipografía: TextMeshPro / Roboto Dynamic Font\n\n" +
                    "<b>Contexto Pedagógico e Histórico:</b>\n" +
                    "Recreación académica basada en el ciberataque del 29 de enero de 2015 al 5 de febrero de 2016 " +
                    "(caso histórico Banco Central de Bangladesh / Lazarus Group APT38).\n\n" +
                    "<b>Licencias de Assets y Software:</b>\n" +
                    "• Código: Licencia MIT / Banco Andino Educational CyberLab\n" +
                    "• Audio y Modelado: Procedural Audio Synthesis (Sin dependencias externas)";
            }
        }

        public void OnCloseCreditsClicked()
        {
            PlaySound(buttonClickClip, GenerateClickTone());
            if (creditsModalPanel != null)
            {
                creditsModalPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Botón 4: "Reiniciar Simulación"
        /// Regresa al vestíbulo / portal inicial para reiniciar la experiencia completa.
        /// </summary>
        public void OnRestartSimulationClicked()
        {
            PlaySound(buttonClickClip, GenerateClickTone());
            Debug.Log($"{LogPrefix} Jugador eligió: <b>'Reiniciar Simulación'</b>");

            ClosePanel();

            if (GameManager.I != null)
            {
                GameManager.I.ResetSimulation();
            }

            PortalEntrada portal = FindFirstObjectByType<PortalEntrada>();
            if (portal != null)
            {
                portal.ReturnToLobby();
            }
            else if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.SetStep(ScenarioStep.Step1_PortalEntrada);
            }
        }

        public void ClosePanel()
        {
            _isShowing = false;
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            if (creditsModalPanel != null)
            {
                creditsModalPanel.SetActive(false);
            }
        }

        #endregion

        #region Generación de Interfaz de Respaldo en Tiempo de Ejecución

        private void EnsurePanelStructure()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null)
                {
                    panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
                }
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
                audioSource.PlayOneShot(toPlay, 0.9f);
            }
        }

        private static AudioClip GenerateAlertBeepTone()
        {
            int sampleRate = 44100;
            float duration = 0.4f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration);
                float f = (t < 0.2f) ? 440f : 330f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * f * t) * 0.25f * env;
            }

            AudioClip clip = AudioClip.Create("Outcome_AlertTone", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateVictoryFanfareTone()
        {
            int sampleRate = 44100;
            float duration = 0.65f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-2.5f * t);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * 523.25f * t) * 0.25f +
                              Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.25f +
                              Mathf.Sin(2f * Mathf.PI * 783.99f * t) * 0.25f +
                              Mathf.Sin(2f * Mathf.PI * 1046.50f * t) * 0.20f) * env;
            }

            AudioClip clip = AudioClip.Create("Outcome_VictoryTone", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateClickTone()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 800f * t) * 0.15f * env;
            }

            AudioClip clip = AudioClip.Create("Outcome_ClickTone", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
