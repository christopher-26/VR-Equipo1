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
                AndesSim.Audio.AudioManager.I?.PlayNarration("Voice_20_FinalAtaque", true, 2.0f);
            }
            else
            {
                ConfigureDefenseModeOutcome(timeCriterionMet, elapsed);
                AndesSim.Audio.AudioManager.I?.PlayNarration("Voice_21_FinalDefensa", true, 2.0f);
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
                outcomeHeaderTitleText.text = "<b><color=#FF5A4E>ATAQUE COMPLETADO · RED COMPROMETIDA</color></b>";
            }

            if (outcomeHeaderBorder != null)
            {
                outcomeHeaderBorder.color = ColorAlertRed;
            }

            if (outcomeSubtitleText != null)
            {
                outcomeSubtitleText.text = "EL VIRUS ENTRO POR PC 1 Y TOMO LA RED SWIFT";
                outcomeSubtitleText.color = ColorWarningAmber;
            }

            // Resumen de hechos ultra-legible en VR Cardboard
            if (technicalSummaryBodyText != null)
            {
                technicalSummaryBodyText.text =
                    "<b>LO QUE OCURRIO:</b>\n" +
                    "• <b>Paso 1:</b> Se abrio el archivo falso de <i>Rasel Ahlam</i> en PC 1.\n" +
                    "• <b>Paso 2:</b> Sin Sandbox ni clave segura (Post-It), el virus salto a 3 PCs.\n" +
                    "• <b>Paso 3:</b> Puerta trasera lista para robo SWIFT de <b>US$ 951M</b>.";
                technicalSummaryBodyText.color = ColorLightText;
            }

            // Métricas
            if (infectedHostsMetricText != null)
            {
                infectedHostsMetricText.text = "<b>PC INFECTADAS</b>\n<size=24><color=#FF5A4E><b>3 / 3 (100%)</b></color></size>";
            }

            if (stolenMoneyMetricText != null)
            {
                stolenMoneyMetricText.text = "<b>RIESGO SWIFT</b>\n<size=24><color=#FF5A4E><b>US$ 951M</b></color></size>";
            }

            if (swiftStatusMetricText != null)
            {
                swiftStatusMetricText.text = "<b>ACCESO C2</b>\n<size=20><color=#FF5A4E><b>185.130.44.82</b></color></size>";
            }

            // Criterio de tiempo
            if (acceptanceCriterionBadgeText != null)
            {
                string eval = timeCriterionMet ? "<color=#38F28A>[A TIEMPO]</color>" : "<color=#FF5A4E>[LENTO]</color>";
                acceptanceCriterionBadgeText.text = $"TIEMPO: <b>{elapsed:F0}s / 180s</b>  {eval}";
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
                outcomeHeaderTitleText.text = "<b><color=#38F28A>DEFENSA EXITOSA · BANCO PROTEGIDO</color></b>";
            }

            if (outcomeHeaderBorder != null)
            {
                outcomeHeaderBorder.color = ColorCodeGreen;
            }

            if (outcomeSubtitleText != null)
            {
                outcomeSubtitleText.text = "AMENAZA DETENIDA A TIEMPO · CERO PERDIDAS";
                outcomeSubtitleText.color = ColorCodeGreen;
            }

            // Resumen de contramedidas ultra-legible en VR Cardboard
            if (technicalSummaryBodyText != null)
            {
                technicalSummaryBodyText.text =
                    "<b>CONTENCION SOC APLICADA:</b>\n" +
                    "• <b>Correo:</b> Aislado en cuarentena y reportado antes de abrirse.\n" +
                    "• <b>Red:</b> Sandbox activo bloqueo el avance entre computadoras.\n" +
                    "• <b>Clave:</b> Token MFA protegio la cuenta SWIFT de Carla.";
                technicalSummaryBodyText.color = ColorLightText;
            }

            // Métricas
            if (infectedHostsMetricText != null)
            {
                infectedHostsMetricText.text = "<b>PC INFECTADAS</b>\n<size=24><color=#38F28A><b>0 / 3 (0%)</b></color></size>";
            }

            if (stolenMoneyMetricText != null)
            {
                stolenMoneyMetricText.text = "<b>DINERO ROBADO</b>\n<size=24><color=#38F28A><b>US$ 0</b></color></size>";
            }

            if (swiftStatusMetricText != null)
            {
                swiftStatusMetricText.text = "<b>RED SWIFT</b>\n<size=22><color=#38F28A><b>100% SEGURA</b></color></size>";
            }

            // Criterio de tiempo
            if (acceptanceCriterionBadgeText != null)
            {
                string eval = timeCriterionMet ? "<color=#38F28A>[CRITERIO CUMPLIDO]</color>" : "<color=#FF5A4E>[TIEMPO EXCEDIDO]</color>";
                acceptanceCriterionBadgeText.text = $"TIEMPO: <b>{elapsed:F0}s / 180s</b>  {eval}";
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

            Vector3 targetPos = cam.position + (forward * Mathf.Max(spawnDistanceInFront, 1.45f)) + (Vector3.up * -0.05f);
            transform.position = targetPos;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
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
            Debug.Log($"{LogPrefix} Jugador eligio: Ver Sospechosos");

            ClosePanel();

            if (PlayerTeleportController.I != null)
            {
                GameObject tpBoardObj = GameObject.Find("Teleport_TableroSospechosos");
                TeleportPoint tpBoard = tpBoardObj != null ? tpBoardObj.GetComponent<TeleportPoint>() : null;
                if (tpBoard != null)
                {
                    PlayerTeleportController.I.TeleportToPoint(tpBoard);
                    return;
                }

                SuspectBoardVR boardVR = FindAnyObjectByType<SuspectBoardVR>();
                GameObject suspectMural = GameObject.Find("Tablero_Sospechosos_VR")
                    ?? (boardVR != null ? boardVR.gameObject : GameObject.Find("Tablero_Sospechosos_Tactico_3D"));

                if (suspectMural != null)
                {
                    Vector3 viewPos = suspectMural.transform.position - (suspectMural.transform.forward * 1.45f);
                    viewPos.y = 0f;
                    Vector3 lookDir = (suspectMural.transform.position - new Vector3(viewPos.x, suspectMural.transform.position.y, viewPos.z)).normalized;
                    PlayerTeleportController.I.TeleportTo(viewPos, Quaternion.LookRotation(lookDir));
                }
            }
        }

        /// <summary>
        /// Botón 3: "Ver Créditos y Licencias"
        /// Despliega el panel modal de autoría institucional y atribución técnica.
        /// </summary>
        public void OnViewCreditsClicked()
        {
            PlaySound(buttonClickClip, GenerateClickTone());
            ClosePanel();

            CreditsAndEthicsModalUI modal = CreditsAndEthicsModalUI.I;
            if (modal == null)
            {
                modal = FindFirstObjectByType<CreditsAndEthicsModalUI>(FindObjectsInactive.Include);
            }
            if (modal != null)
            {
                modal.OpenModal();
                return;
            }

            if (creditsModalPanel != null)
            {
                creditsModalPanel.SetActive(true);
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
        /// Botón 4: "Menú Inicial / Reiniciar"
        /// Regresa a la puerta de entrada y vuelve a mostrar el menú inicial.
        /// </summary>
        public void OnRestartSimulationClicked()
        {
            PlaySound(buttonClickClip, GenerateClickTone());
            Debug.Log($"{LogPrefix} Jugador eligio: Volver al Menu Inicial");

            ClosePanel();

            if (GameManager.I != null)
            {
                GameManager.I.ResetSimulation();
            }

            PortalEntrada portal = FindFirstObjectByType<PortalEntrada>();
            if (portal != null)
            {
                portal.ReturnToLobby();
                return;
            }

            if (PlayerTeleportController.I != null)
            {
                GameObject tpEntryObj = GameObject.Find("Teleport_PuertaEntrada");
                TeleportPoint tpEntry = tpEntryObj != null ? tpEntryObj.GetComponent<TeleportPoint>() : null;
                if (tpEntry != null)
                {
                    PlayerTeleportController.I.TeleportToPoint(tpEntry);
                }
                else
                {
                    PlayerTeleportController.I.TeleportTo(new Vector3(-2.60f, 0.01f, 3.40f), Quaternion.Euler(0f, 180f, 0f));
                }
            }

            VrStartMenuController startMenu = VrStartMenuController.I
                ?? FindFirstObjectByType<VrStartMenuController>(FindObjectsInactive.Include);
            if (startMenu != null)
            {
                startMenu.ShowMenu();
            }

            if (ScenarioFlowController.I != null)
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
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rootRect = GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(960f, 650f);
            rootRect.localScale = new Vector3(0.0019f, 0.0019f, 0.0019f);

            if (GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null) panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Si ya tiene estructura completa asignada en Inspector/Editor, respetar
            if (panelRoot != null && panelRoot != gameObject && outcomeHeaderTitleText != null && tryDefenseModeButton != null && restartSimulationButton != null)
            {
                return;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // Contenedor visual interno (mantener root activo para escuchar OnScenarioEnded)
            panelRoot = CreateEndUiBox("Panel_EndFondo", transform, Vector2.zero, new Vector2(960f, 650f), ColorDarkBg);
            AddEndOutline(panelRoot, ColorCodeGreen, 2.5f);

            // Cabecera
            GameObject headerBox = CreateEndUiBox("Header_Outcome", panelRoot.transform, new Vector2(0f, 255f), new Vector2(910f, 100f), new Color(0.03f, 0.09f, 0.07f, 0.96f));
            AddEndOutline(headerBox, ColorCodeGreen, 2f);
            outcomeHeaderBorder = CreateEndUiBox("Header_AccentBar", headerBox.transform, new Vector2(0f, 48f), new Vector2(910f, 5f), ColorCodeGreen).GetComponent<Image>();

            outcomeHeaderTitleText = CreateEndTmp("Txt_OutcomeTitle", headerBox.transform, new Vector2(0f, 16f), new Vector2(880f, 44f),
                "RESULTADO FINAL", 26f, TextAlignmentOptions.Center, ColorCodeGreen);
            outcomeSubtitleText = CreateEndTmp("Txt_OutcomeSubtitle", headerBox.transform, new Vector2(0f, -22f), new Vector2(880f, 30f),
                "SIMULACION COMPLETADA", 17f, TextAlignmentOptions.Center, ColorLightText);

            // Fila de 3 Métricas grandes
            infectedHostsMetricText = BuildMetricCard(panelRoot.transform, "Metric_Hosts", new Vector2(-305f, 130f), new Vector2(285f, 96f));
            stolenMoneyMetricText   = BuildMetricCard(panelRoot.transform, "Metric_Money", new Vector2(0f, 130f),    new Vector2(285f, 96f));
            swiftStatusMetricText   = BuildMetricCard(panelRoot.transform, "Metric_Swift", new Vector2(305f, 130f),  new Vector2(285f, 96f));

            // Resumen de hechos corto y claro
            GameObject bodyBox = CreateEndUiBox("Body_Summary", panelRoot.transform, new Vector2(0f, -15f), new Vector2(910f, 160f), new Color(0.03f, 0.07f, 0.05f, 0.92f));
            AddEndOutline(bodyBox, new Color(0.22f, 0.95f, 0.54f, 0.35f), 1.5f);
            technicalSummaryBodyText = CreateEndTmp("Txt_TechnicalSummary", bodyBox.transform, Vector2.zero, new Vector2(860f, 138f),
                "Cargando resumen...", 19f, TextAlignmentOptions.Left, ColorLightText);

            // Tiempo
            acceptanceCriterionBadgeText = CreateEndTmp("Txt_TimeBadge", panelRoot.transform, new Vector2(0f, -122f), new Vector2(880f, 34f),
                "TIEMPO: 0s / 180s", 18f, TextAlignmentOptions.Center, ColorLightText);

            // Contenedor de botones Modo Ataque
            attackActionContainer = new GameObject("Attack_Actions");
            attackActionContainer.transform.SetParent(panelRoot.transform, false);
            RectTransform atkRect = attackActionContainer.AddComponent<RectTransform>();
            atkRect.anchoredPosition = new Vector2(0f, -215f);
            atkRect.sizeDelta = new Vector2(910f, 110f);

            GameObject tryDefBtnObj = CreateEndGazeButton("Btn_ProbarDefensa", attackActionContainer.transform,
                new Vector2(0f, 22f), new Vector2(620f, 60f),
                "<b>[ > PROBAR MODO DEFENSA AHORA ]</b>",
                new Color(0.08f, 0.42f, 0.24f, 0.98f), ColorCodeGreen, 21f, Color.white);
            tryDefenseModeButton = tryDefBtnObj.GetComponent<Button>();
            tryDefenseButtonText = tryDefBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            GameObject atkSuspectsObj = CreateEndGazeButton("Btn_Ataque_Sospechosos", attackActionContainer.transform,
                new Vector2(-215f, -44f), new Vector2(360f, 46f),
                "<b>[ VER SOSPECHOSOS ]</b>",
                new Color(0.10f, 0.18f, 0.26f, 0.95f), ColorLightText, 17f, ColorLightText);
            atkSuspectsObj.GetComponent<Button>().onClick.AddListener(OnViewSuspectsClicked);

            GameObject atkMenuObj = CreateEndGazeButton("Btn_Ataque_Menu", attackActionContainer.transform,
                new Vector2(215f, -44f), new Vector2(360f, 46f),
                "<b>[ < MENU INICIAL ]</b>",
                new Color(0.16f, 0.12f, 0.16f, 0.95f), ColorLightText, 17f, ColorLightText);
            atkMenuObj.GetComponent<Button>().onClick.AddListener(OnRestartSimulationClicked);

            // Contenedor de botones Modo Defensa
            defenseActionContainer = new GameObject("Defense_Actions");
            defenseActionContainer.transform.SetParent(panelRoot.transform, false);
            RectTransform defRect = defenseActionContainer.AddComponent<RectTransform>();
            defRect.anchoredPosition = new Vector2(0f, -215f);
            defRect.sizeDelta = new Vector2(910f, 110f);

            GameObject viewSuspObj = CreateEndGazeButton("Btn_Def_Sospechosos", defenseActionContainer.transform,
                new Vector2(-305f, -5f), new Vector2(285f, 64f),
                "<b>[ VER\nSOSPECHOSOS ]</b>",
                new Color(0.08f, 0.34f, 0.22f, 0.96f), ColorCodeGreen, 18f, Color.white);
            viewSuspectsButton = viewSuspObj.GetComponent<Button>();

            GameObject viewCredObj = CreateEndGazeButton("Btn_Def_Creditos", defenseActionContainer.transform,
                new Vector2(0f, -5f), new Vector2(285f, 64f),
                "<b>[ CREDITOS\nY ETICA ]</b>",
                new Color(0.10f, 0.22f, 0.34f, 0.96f), ColorLightText, 18f, Color.white);
            viewCreditsButton = viewCredObj.GetComponent<Button>();

            GameObject restartObj = CreateEndGazeButton("Btn_Def_Menu", defenseActionContainer.transform,
                new Vector2(305f, -5f), new Vector2(285f, 64f),
                "<b>[ < VOLVER AL\nMENU INICIAL ]</b>",
                new Color(0.16f, 0.24f, 0.18f, 0.96f), ColorLightText, 18f, Color.white);
            restartSimulationButton = restartObj.GetComponent<Button>();
        }

        private TextMeshProUGUI BuildMetricCard(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject card = CreateEndUiBox(name, parent, pos, size, new Color(0.04f, 0.10f, 0.08f, 0.95f));
            AddEndOutline(card, new Color(0.22f, 0.95f, 0.54f, 0.45f), 1.5f);
            return CreateEndTmp("Txt_Val", card.transform, Vector2.zero, size - new Vector2(18f, 14f),
                "--", 16f, TextAlignmentOptions.Center, ColorLightText);
        }

        private static GameObject CreateEndUiBox(string name, Transform parent, Vector2 pos, Vector2 size, Color bgColor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.color = bgColor;
            return go;
        }

        private static Outline AddEndOutline(GameObject go, Color color, float width)
        {
            Outline o = go.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(width, -width);
            return o;
        }

        private static TextMeshProUGUI CreateEndTmp(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            return tmp;
        }

        private GameObject CreateEndGazeButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, Color bgCol, Color borderCol, float fontSize, Color textCol)
        {
            GameObject btnObj = CreateEndUiBox(name, parent, pos, size, bgCol);
            AddEndOutline(btnObj, borderCol, 2f);
            btnObj.layer = LayerMask.NameToLayer("Interactive");

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgCol;
            cb.highlightedColor = Color.Lerp(bgCol, borderCol, 0.35f);
            cb.pressedColor = borderCol;
            btn.colors = cb;

            CreateEndTmp("Label", btnObj.transform, Vector2.zero, size - new Vector2(14f, 8f), label, fontSize, TextAlignmentOptions.Center, textCol);

            BoxCollider col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 40f);
            col.center = new Vector3(0f, 0f, -10f);
            col.isTrigger = true;

            GazeInteractable gaze = btnObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = label.Replace("<b>", "").Replace("</b>", "").Replace("\n", " ");
            gaze.dwellTime = 1.1f;

            btnObj.AddComponent<WorldSpaceUiGazeButton>();
            return btnObj;
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
