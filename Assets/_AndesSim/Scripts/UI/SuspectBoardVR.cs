using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Environment;
using AndesSim.VR;

namespace AndesSim.UI
{
    /// <summary>
    /// Modelo de datos detallado de expediente para el Tablero Táctico de Sospechosos (RF-02).
    /// Contiene el análisis forense y el enfoque específico del Escenario 1 ("Oficina de pagos").
    /// </summary>
    [Serializable]
    public class SuspectDossierScenario01
    {
        [Tooltip("Identificador único de sospechoso.")]
        public string id;

        [Tooltip("Nombre y grado profesional.")]
        public string fullName;

        [Tooltip("Cargo oficial dentro de la organización o empresa proveedora.")]
        public string roleTitle;

        [Tooltip("Nivel de sospecha o categoría de amenaza.")]
        public string threatLevelBadge;

        [Tooltip("Color hexadecimal del distintivo de amenaza.")]
        public string threatColorHex;

        [TextArea(3, 5)]
        [Tooltip("Trayectoria profesional, perfil psicológico y conducta.")]
        public string careerAndPsychology;

        [TextArea(3, 5)]
        [Tooltip("Puntos e indicios incriminatorios (En contra).")]
        public string incriminatingPoints;

        [TextArea(3, 5)]
        [Tooltip("Puntos atenuantes o elementos exculpatorios (A favor).")]
        public string exoneratingPoints;

        // --- ENFOQUE ESPECÍFICO DEL ESCENARIO 1 (BANCO ANDINO) ---
        [TextArea(2, 4)]
        [Tooltip("¿Por qué el correo con malware llegó a 16 personas específicas del área?")]
        public string reason16Recipients;

        [TextArea(2, 4)]
        [Tooltip("¿Por qué la clave de Carla Mendoza estaba en un Post-It visible para cualquiera?")]
        public string reasonCarlaPostIt;

        [TextArea(2, 4)]
        [Tooltip("¿Por qué la red bancaria carecía de firewall perimetral y entorno sandbox?")]
        public string reasonNoFirewallSandbox;

        [TextArea(2, 4)]
        [Tooltip("Hipótesis fundamentada del equipo de investigación para la defensa oral.")]
        public string teamHypothesisArgument;
    }

    /// <summary>
    /// Tablero Táctico de Sospechosos en World Space para el muro de la Oficina de Pagos.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1 (Equipo 1).
    /// - Muestra las fichas interactivas de los 4 sospechosos oficiales:
    ///   1. Ing. Samuel Torres (Jefe de Seguridad TI e Infraestructura)
    ///   2. Lic. Hugo Benavides (Gerente de Operaciones y Pagos Internacionales)
    ///   3. Carla Mendoza (Operadora Senior SWIFT / Pagos Internacionales)
    ///   4. Renzo Aliaga (Técnico Residente de Proveedor Externo de Redes)
    /// - Expone carrera, psicología, puntos en contra y a favor, y las 3 incógnitas clave del Escenario 1.
    /// - Permite al equipo interactuar con la retícula VR y marcar su hipótesis fundamentada oficial,
    ///   persistiéndola en PlayerPrefs y ProgressTracker para la exposición final.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class SuspectBoardVR : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - SuspectBoardVR]";
        public const string PrefKeySelectedSuspect = "ANDES_SIM_E1_SELECTED_SUSPECT";
        public const string PrefKeyHypothesisReason = "ANDES_SIM_E1_HYPOTHESIS_REASON";

        // Paleta oficial Banco Andino
        private const string ColorDarkBg = "#020a06";
        private const string ColorCardBg = "#07150e";
        private const string ColorTextLight = "#d6f2e2";
        private const string ColorGreenCode = "#38f28a";
        private const string ColorAlertRed = "#ff5a4e";
        private const string ColorAmberWarning = "#ffc24a";
        private const string ColorMutedText = "#8fa397";

        [Header("--- Base de Datos de Sospechosos (Escenario 1) ---")]
        [SerializeField] private List<SuspectDossierScenario01> suspects = new List<SuspectDossierScenario01>();

        [Header("--- Contenedores de Interfaz ---")]
        [SerializeField] private Transform tabsContainer;
        [SerializeField] private TextMeshProUGUI headerTitleText;
        [SerializeField] private TextMeshProUGUI headerSubtitleText;

        [Header("--- Campos del Expediente Activo ---")]
        [SerializeField] private TextMeshProUGUI suspectNameText;
        [SerializeField] private TextMeshProUGUI suspectRoleText;
        [SerializeField] private TextMeshProUGUI threatBadgeText;
        [SerializeField] private TextMeshProUGUI careerPsychologyText;
        [SerializeField] private TextMeshProUGUI incriminatingFactsText;
        [SerializeField] private TextMeshProUGUI exoneratingFactsText;
        [SerializeField] private TextMeshProUGUI scenario01AnalysisText;
        [SerializeField] private TextMeshProUGUI hypothesisArgumentText;

        [Header("--- Botón y Sello de Hipótesis ---")]
        [SerializeField] private Button selectHypothesisButton;
        [SerializeField] private TextMeshProUGUI selectHypothesisBtnText;
        [SerializeField] private GameObject hypothesisStampBadge;
        [SerializeField] private TextMeshProUGUI hypothesisStampText;

        [Header("--- Efectos Sonoros Procedurales ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip switchSuspectSound;
        [SerializeField] private AudioClip stampHypothesisSound;

        // Estado interno
        private int _selectedSuspectIndex = 0;
        private List<Button> _tabButtons = new List<Button>();
        private List<Image> _tabButtonImages = new List<Image>();
        private List<TextMeshProUGUI> _tabButtonTexts = new List<TextMeshProUGUI>();
        private bool _isInitialized = false;

        public static SuspectBoardVR I { get; private set; }

        public int CurrentSuspectIndex => _selectedSuspectIndex;
        public IReadOnlyList<SuspectDossierScenario01> Suspects => suspects;

        private void Awake()
        {
            I = this;
            EnsureAudioSource();
            EnsureDefaultDossiers();
        }

        private void Start()
        {
            InitializeBoard();
        }

        #region Inicialización del Tablero

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.85f;
            audioSource.maxDistance = 10f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        /// <summary>
        /// Asegura que la lista contenga los 4 perfiles canónicos con el enfoque forense del Escenario 1.
        /// </summary>
        public void EnsureDefaultDossiers()
        {
            if (suspects != null && suspects.Count >= 4) return;

            suspects = new List<SuspectDossierScenario01>
            {
                // 1. Ing. Samuel Torres
                new SuspectDossierScenario01
                {
                    id = "SUSPECT_SAMUEL_TORRES",
                    fullName = "Ing. Samuel Torres",
                    roleTitle = "Jefe de Seguridad TI",
                    threatLevelBadge = "NEGLIGENCIA",
                    threatColorHex = ColorAlertRed,
                    careerAndPsychology =
                        "• 12 años en redes.\n" +
                        "• Molesto por recortes.",
                    incriminatingPoints =
                        "• Administra correo y red.\n" +
                        "• Sin VLANs ni bloqueo .exe.",
                    exoneratingPoints =
                        "• Pidio Sandbox 3 veces.\n" +
                        "• No uso su clave.",
                    reason16Recipients = "Correo abierto al exterior.",
                    reasonCarlaPostIt = "Claves largas sin gestor.",
                    reasonNoFirewallSandbox = "Sin presupuesto para filtros.",
                    teamHypothesisArgument =
                        "<b>CONCLUSION:</b> Negligencia en seguridad de red."
                },

                // 2. Lic. Hugo Benavides
                new SuspectDossierScenario01
                {
                    id = "SUSPECT_HUGO_BENAVIDES",
                    fullName = "Lic. Hugo Benavides",
                    roleTitle = "Gerente Operaciones",
                    threatLevelBadge = "DESIDIA",
                    threatColorHex = ColorAmberWarning,
                    careerAndPsychology =
                        "• 18 años en metas.\n" +
                        "• Prioriza rapidez.",
                    incriminatingPoints =
                        "• Pidio quitar filtros.\n" +
                        "• Ignoro fallas SWIFT.",
                    exoneratingPoints =
                        "• Sin perfil tecnico.\n" +
                        "• Fuera de sede.",
                    reason16Recipients = "Publico correos del area.",
                    reasonCarlaPostIt = "Acepto claves en Post-It.",
                    reasonNoFirewallSandbox = "Voto contra el Sandbox.",
                    teamHypothesisArgument =
                        "<b>CONCLUSION:</b> Recortes anularon la defensa."
                },

                // 3. Carla Mendoza
                new SuspectDossierScenario01
                {
                    id = "SUSPECT_CARLA_MENDOZA",
                    fullName = "Carla Mendoza",
                    roleTitle = "Operadora SWIFT",
                    threatLevelBadge = "VICTIMA",
                    threatColorHex = ColorGreenCode,
                    careerAndPsychology =
                        "• 7 años puntual.\n" +
                        "• Alta carga laboral.",
                    incriminatingPoints =
                        "• Su usuario aprobo giros.\n" +
                        "• Clave pegada en Post-It.",
                    exoneratingPoints =
                        "• En casa de noche.\n" +
                        "• Engañada por phishing.",
                    reason16Recipients = "Su correo era visible.",
                    reasonCarlaPostIt = "Anoto clave por rapidez.",
                    reasonNoFirewallSandbox = "Confio en el filtro.",
                    teamHypothesisArgument =
                        "<b>CONCLUSION:</b> Victima explotada por phishing."
                },

                // 4. Renzo Aliaga
                new SuspectDossierScenario01
                {
                    id = "SUSPECT_RENZO_ALIAGA",
                    fullName = "Renzo Aliaga",
                    roleTitle = "Soporte Externo",
                    threatLevelBadge = "SOSPECHOSO",
                    threatColorHex = ColorAlertRed,
                    careerAndPsychology =
                        "• 4 años en cableado.\n" +
                        "• Deudas graves.",
                    incriminatingPoints =
                        "• Trabajo bajo PC 1.\n" +
                        "• Entro de noche sin guia.",
                    exoneratingPoints =
                        "• Ordenes firmadas.\n" +
                        "• Sin evidencia de USB.",
                    reason16Recipients = "Vio correos en PC 1.",
                    reasonCarlaPostIt = "Estuvo junto al Post-It.",
                    reasonNoFirewallSandbox = "Conocia la red plana.",
                    teamHypothesisArgument =
                        "<b>CONCLUSION:</b> Posible complice interno."
                }
            };
        }

        /// <summary>
        /// Inicializa el tablero táctico, construye o mapea la interfaz y carga la hipótesis activa.
        /// </summary>
        public void InitializeBoard()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            EnsureAudioSource();
            EnsureDefaultDossiers();

            // Ocultar elementos 3D/hilos/chinchetas de respaldo para que nada atraviese ni tape el tablero digital
            if (transform.parent != null)
            {
                foreach (Transform sibling in transform.parent)
                {
                    if (sibling == transform) continue;
                    string sn = sibling.name;
                    if (sn.StartsWith("Lineas_Rojas") || sn.StartsWith("Ficha_") ||
                        sn.StartsWith("Evidencia_") || sn == "Placa_Encabezado_Forense")
                    {
                        sibling.gameObject.SetActive(false);
                    }
                }
            }

            // Si los campos de texto no están vinculados en inspector, auto-generar la estructura UI World Space
            if (suspectNameText == null || tabsContainer == null || selectHypothesisButton == null)
            {
                BuildTacticalBoardHierarchy();
            }

            // Configurar los botones de las 4 pestañas de sospechosos
            SetupTabButtons();

            // Configurar el botón de selección de hipótesis
            if (selectHypothesisButton != null)
            {
                selectHypothesisButton.onClick.RemoveAllListeners();
                selectHypothesisButton.onClick.AddListener(OnSelectHypothesisClicked);
            }

            // Recuperar la hipótesis guardada previamente en ProgressTracker o PlayerPrefs
            string savedHypothesis = PlayerPrefs.GetString(PrefKeySelectedSuspect, string.Empty);
            if (ProgressTracker.I != null && !string.IsNullOrEmpty(ProgressTracker.I.SelectedSuspectHypothesis))
            {
                savedHypothesis = ProgressTracker.I.SelectedSuspectHypothesis;
            }

            int initialIndex = 0;
            if (!string.IsNullOrEmpty(savedHypothesis))
            {
                for (int i = 0; i < suspects.Count; i++)
                {
                    if (string.Equals(suspects[i].id, savedHypothesis, StringComparison.OrdinalIgnoreCase))
                    {
                        initialIndex = i;
                        break;
                    }
                }
            }

            DisplaySuspect(initialIndex);
            AndesTypographyTheme.ApplyTypographyToTree(transform, true, false);
            Debug.Log($"{LogPrefix} <color=#38F28A><b>Tablero Táctico de Sospechosos inicializado con 4 expedientes oficiales.</b></color>");
        }

        #endregion

        #region Generación Procedural de Jerarquía UI (World Space)

        /// <summary>
        /// Genera de forma limpia y robusta toda la interfaz táctica World Space en caso de crearse en tiempo de ejecución.
        /// </summary>
        private void BuildTacticalBoardHierarchy()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rootRect = GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1300f, 850f);
            rootRect.localScale = new Vector3(0.00185f, 0.00185f, 0.00185f);

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f;

            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();

            // Panel de Fondo Táctico
            GameObject bgObj = CreateUiPanel("Panel_Fondo_Tactico", transform, new Vector2(0f, 0f), new Vector2(1300f, 850f), HexToColor(ColorDarkBg, 0.96f));
            AddUiBorder(bgObj, HexToColor(ColorGreenCode, 0.7f), 3f);

            // Cabecera del Tablero
            GameObject headerObj = CreateUiPanel("Header_Board", bgObj.transform, new Vector2(0f, 385f), new Vector2(1260f, 65f), HexToColor(ColorCardBg, 0.95f));
            AddUiBorder(headerObj, HexToColor(ColorGreenCode, 0.4f), 1.5f);

            headerTitleText = CreateTmpText("Title", headerObj.transform, new Vector2(-150f, 9f), new Vector2(920f, 34f),
                "<b><color=#38F28A>TABLERO DE SOSPECHOSOS</color></b>", 22f, TextAlignmentOptions.Left, HexToColor(ColorTextLight));

            headerSubtitleText = CreateTmpText("Subtitle", headerObj.transform, new Vector2(-150f, -16f), new Vector2(920f, 24f),
                "<i>Caso Banco Andino · Elige al responsable</i>", 14.5f, TextAlignmentOptions.Left, HexToColor(ColorMutedText));

            // Sello superior de hipótesis activa
            GameObject stampObj = CreateUiPanel("Hypothesis_Global_Stamp", headerObj.transform, new Vector2(480f, 0f), new Vector2(260f, 44f), HexToColor(ColorCardBg, 0.98f));
            AddUiBorder(stampObj, HexToColor(ColorGreenCode, 0.9f), 2f);
            hypothesisStampBadge = stampObj;

            hypothesisStampText = CreateTmpText("StampText", stampObj.transform, Vector2.zero, new Vector2(250f, 40f),
                "<b>HIPOTESIS ACTIVA</b>", 14f, TextAlignmentOptions.Center, HexToColor(ColorGreenCode));

            // Contenedor de Botones de Pestañas (Izquierda: 280px de ancho)
            GameObject tabsObj = CreateUiPanel("Container_Tabs", bgObj.transform, new Vector2(-490f, -40f), new Vector2(280f, 740f), HexToColor(ColorCardBg, 0.85f));
            AddUiBorder(tabsObj, HexToColor(ColorGreenCode, 0.3f), 1.5f);
            tabsContainer = tabsObj.transform;

            CreateTmpText("TabsHeader", tabsObj.transform, new Vector2(0f, 345f), new Vector2(260f, 30f),
                "<b>SOSPECHOSOS</b>", 16f, TextAlignmentOptions.Center, HexToColor(ColorGreenCode));

            // Contenedor del Expediente Principal (Derecha: 960px de ancho)
            GameObject dossierObj = CreateUiPanel("Container_Dossier_Body", bgObj.transform, new Vector2(145f, -40f), new Vector2(960f, 740f), HexToColor(ColorCardBg, 0.90f));
            AddUiBorder(dossierObj, HexToColor(ColorGreenCode, 0.4f), 1.5f);

            // 1. Cabecera del Sospechoso Activo
            GameObject suspectHeaderObj = CreateUiPanel("Suspect_Profile_Header", dossierObj.transform, new Vector2(0f, 320f), new Vector2(920f, 76f), HexToColor(ColorDarkBg, 0.95f));
            AddUiBorder(suspectHeaderObj, HexToColor(ColorGreenCode, 0.35f), 1f);

            suspectNameText = CreateTmpText("SuspectName", suspectHeaderObj.transform, new Vector2(-170f, 14f), new Vector2(540f, 36f),
                "<b>Ing. Samuel Torres</b>", 27f, TextAlignmentOptions.Left, HexToColor(ColorTextLight));

            suspectRoleText = CreateTmpText("SuspectRole", suspectHeaderObj.transform, new Vector2(-170f, -15f), new Vector2(540f, 26f),
                "<b>Jefe de Seguridad TI</b>", 16.5f, TextAlignmentOptions.Left, HexToColor(ColorGreenCode));

            threatBadgeText = CreateTmpText("ThreatBadge", suspectHeaderObj.transform, new Vector2(300f, 0f), new Vector2(280f, 44f),
                "<b>NEGLIGENCIA</b>", 16.5f, TextAlignmentOptions.Center, HexToColor(ColorAlertRed));

            // 2. Columna Izquierda: Perfil
            GameObject colLeftObj = CreateUiPanel("Col_Trayectoria", dossierObj.transform, new Vector2(-235f, 128f), new Vector2(440f, 280f), HexToColor(ColorDarkBg, 0.7f));
            AddUiBorder(colLeftObj, HexToColor(ColorGreenCode, 0.25f), 1f);

            CreateTmpText("ColLeftTitle", colLeftObj.transform, new Vector2(0f, 108f), new Vector2(400f, 30f),
                "<b>1. PERFIL</b>", 17.5f, TextAlignmentOptions.Left, HexToColor(ColorGreenCode));

            careerPsychologyText = CreateTmpText("ColLeftBody", colLeftObj.transform, new Vector2(0f, -14f), new Vector2(400f, 185f),
                "...", 19f, TextAlignmentOptions.TopLeft, HexToColor(ColorTextLight));

            // 3. Columna Derecha: Pistas A Favor / En Contra
            GameObject colRightObj = CreateUiPanel("Col_Indicios", dossierObj.transform, new Vector2(235f, 128f), new Vector2(440f, 280f), HexToColor(ColorDarkBg, 0.7f));
            AddUiBorder(colRightObj, HexToColor(ColorGreenCode, 0.25f), 1f);

            CreateTmpText("ColRightTitle", colRightObj.transform, new Vector2(0f, 108f), new Vector2(400f, 30f),
                "<b>2. PISTAS</b>", 17.5f, TextAlignmentOptions.Left, HexToColor(ColorAmberWarning));

            incriminatingFactsText = CreateTmpText("ColRightIncrim", colRightObj.transform, new Vector2(0f, 36f), new Vector2(400f, 96f),
                "<b>EN CONTRA:</b>\n...", 16.5f, TextAlignmentOptions.TopLeft, HexToColor(ColorAlertRed));

            exoneratingFactsText = CreateTmpText("ColRightExon", colRightObj.transform, new Vector2(0f, -68f), new Vector2(400f, 96f),
                "<b>A FAVOR:</b>\n...", 16.5f, TextAlignmentOptions.TopLeft, HexToColor(ColorGreenCode));

            // 4. Panel Inferior: Vínculo con el caso
            GameObject scenarioKeysObj = CreateUiPanel("Panel_Claves_Escenario1", dossierObj.transform, new Vector2(0f, -100f), new Vector2(920f, 148f), HexToColor(ColorDarkBg, 0.85f));
            AddUiBorder(scenarioKeysObj, HexToColor(ColorGreenCode, 0.3f), 1f);

            CreateTmpText("ScenarioKeysTitle", scenarioKeysObj.transform, new Vector2(0f, 48f), new Vector2(880f, 28f),
                "<b>3. VINCULO CON EL CASO</b>", 17f, TextAlignmentOptions.Left, HexToColor(ColorGreenCode));

            scenario01AnalysisText = CreateTmpText("ScenarioKeysBody", scenarioKeysObj.transform, new Vector2(0f, -14f), new Vector2(880f, 92f),
                "...", 17f, TextAlignmentOptions.TopLeft, HexToColor(ColorTextLight));

            // 5. Barra Inferior: Conclusión y Botón
            GameObject hypothesisBarObj = CreateUiPanel("Panel_Hipotesis_Accion", dossierObj.transform, new Vector2(0f, -255f), new Vector2(920f, 130f), HexToColor(ColorDarkBg, 0.95f));
            AddUiBorder(hypothesisBarObj, HexToColor(ColorGreenCode, 0.5f), 1.5f);

            hypothesisArgumentText = CreateTmpText("HypothesisSummary", hypothesisBarObj.transform, new Vector2(-160f, 0f), new Vector2(560f, 100f),
                "<b>CONCLUSION:</b> ...", 17.5f, TextAlignmentOptions.Left, HexToColor(ColorTextLight));

            // Botón interactivo de selección de hipótesis
            GameObject btnSelectObj = CreateUiPanel("Btn_MarcarHipotesis", hypothesisBarObj.transform, new Vector2(290f, 0f), new Vector2(292f, 78f), HexToColor(ColorGreenCode, 0.9f));
            AddUiBorder(btnSelectObj, HexToColor(ColorDarkBg, 1f), 2f);

            selectHypothesisButton = btnSelectObj.AddComponent<Button>();
            selectHypothesisBtnText = CreateTmpText("BtnLabel", btnSelectObj.transform, Vector2.zero, new Vector2(274f, 68f),
                "<b>ELEGIR RESPONSABLE</b>", 15.5f, TextAlignmentOptions.Center, HexToColor(ColorDarkBg));

            // Conectar el adaptador de retícula VR
            WorldSpaceUiGazeButton gazeBtn = btnSelectObj.AddComponent<WorldSpaceUiGazeButton>();
            gazeBtn.ConfigureAudio(audioSource, stampHypothesisSound, null);
            btnSelectObj.AddComponent<HolographicCardHover>().Configure(1.045f, 14f);
        }

        #endregion

        #region Configuración de Pestañas Interactivas

        private void SetupTabButtons()
        {
            if (tabsContainer == null) return;

            // Limpiar si ya existen
            foreach (Transform child in tabsContainer)
            {
                if (child.name.StartsWith("Tab_Suspect_"))
                {
                    Destroy(child.gameObject);
                }
            }

            _tabButtons.Clear();
            _tabButtonImages.Clear();
            _tabButtonTexts.Clear();

            float startY = 248f;
            float spacingY = 162f;

            for (int i = 0; i < suspects.Count; i++)
            {
                int index = i;
                SuspectDossierScenario01 suspect = suspects[i];

                GameObject tabObj = CreateUiPanel($"Tab_Suspect_{index}", tabsContainer, new Vector2(0f, startY - (index * spacingY)), new Vector2(250f, 135f), HexToColor(ColorDarkBg, 0.85f));
                AddUiBorder(tabObj, HexToColor(ColorGreenCode, 0.35f), 1.5f);

                Image tabImg = tabObj.GetComponent<Image>();
                _tabButtonImages.Add(tabImg);

                Button btn = tabObj.AddComponent<Button>();
                btn.onClick.AddListener(() => OnTabClicked(index));
                _tabButtons.Add(btn);

                // Enlazar WorldSpaceUiGazeButton para la retícula de Cardboard
                WorldSpaceUiGazeButton gazeBtn = tabObj.AddComponent<WorldSpaceUiGazeButton>();
                gazeBtn.ConfigureAudio(audioSource, switchSuspectSound, null);
                tabObj.AddComponent<HolographicCardHover>().Configure(1.04f, 14f);

                // Etiquetas internas del botón (Fuente grande y limpia para Cardboard)
                TextMeshProUGUI label = CreateTmpText("Label", tabObj.transform, new Vector2(0f, 12f), new Vector2(236f, 40f),
                    $"<b>{suspect.fullName}</b>", 18f, TextAlignmentOptions.Center, HexToColor(ColorTextLight));
                _tabButtonTexts.Add(label);

                CreateTmpText("Role", tabObj.transform, new Vector2(0f, -14f), new Vector2(236f, 28f),
                    suspect.roleTitle, 14f, TextAlignmentOptions.Center, HexToColor(ColorGreenCode));
            }
        }

        #endregion

        #region Despliegue de Expediente y Cambio de Sospechoso

        /// <summary>
        /// Selecciona y despliega en la interfaz táctica el sospechoso con el índice indicado.
        /// </summary>
        public void DisplaySuspect(int index)
        {
            if (suspects == null || index < 0 || index >= suspects.Count) return;
            _selectedSuspectIndex = index;
            SuspectDossierScenario01 suspect = suspects[index];

            // 1. Textos de Identificación
            if (suspectNameText != null) suspectNameText.text = $"<b>{suspect.fullName}</b>";
            if (suspectRoleText != null) suspectRoleText.text = $"<b>{suspect.roleTitle}</b>";

            // 2. Distintivo de Amenaza
            if (threatBadgeText != null)
            {
                threatBadgeText.text = $"<b>{suspect.threatLevelBadge}</b>";
                threatBadgeText.color = HexToColor(suspect.threatColorHex);
            }

            // 3. Trayectoria y Psicología
            if (careerPsychologyText != null)
            {
                careerPsychologyText.text = suspect.careerAndPsychology;
            }

            // 4. Indicios en Contra
            if (incriminatingFactsText != null)
            {
                incriminatingFactsText.text = $"<b><color={ColorAlertRed}>EN CONTRA:</color></b>\n{suspect.incriminatingPoints}";
            }

            // 5. Puntos a Favor / Coartada
            if (exoneratingFactsText != null)
            {
                exoneratingFactsText.text = $"<b><color={ColorGreenCode}>A FAVOR:</color></b>\n{suspect.exoneratingPoints}";
            }

            // 6. Análisis Clave del Escenario 1
            if (scenario01AnalysisText != null)
            {
                scenario01AnalysisText.text =
                    $"• <b>16 Correos:</b> {suspect.reason16Recipients}\n" +
                    $"• <b>Post-It:</b> {suspect.reasonCarlaPostIt}\n" +
                    $"• <b>Sin Sandbox:</b> {suspect.reasonNoFirewallSandbox}";
            }

            // 7. Argumento para la Hipótesis del Equipo
            if (hypothesisArgumentText != null)
            {
                hypothesisArgumentText.text = suspect.teamHypothesisArgument;
            }

            // 8. Estado de la Hipótesis Seleccionada
            UpdateHypothesisSelectionState(suspect.id);

            // 9. Actualizar resaltado de pestañas
            UpdateTabVisuals();
        }

        private void UpdateTabVisuals()
        {
            string currentHypothesisId = GetCurrentSavedHypothesisId();

            for (int i = 0; i < _tabButtonImages.Count && i < suspects.Count; i++)
            {
                bool isSelected = (i == _selectedSuspectIndex);
                bool isHypothesis = string.Equals(suspects[i].id, currentHypothesisId, StringComparison.OrdinalIgnoreCase);

                if (isSelected)
                {
                    _tabButtonImages[i].color = HexToColor(ColorCardBg, 0.98f);
                    if (_tabButtonTexts[i] != null) _tabButtonTexts[i].color = HexToColor(ColorGreenCode);
                }
                else
                {
                    _tabButtonImages[i].color = HexToColor(ColorDarkBg, 0.75f);
                    if (_tabButtonTexts[i] != null) _tabButtonTexts[i].color = HexToColor(isHypothesis ? ColorGreenCode : ColorTextLight);
                }
            }
        }

        private void UpdateHypothesisSelectionState(string currentSuspectId)
        {
            string savedHypothesisId = GetCurrentSavedHypothesisId();
            bool isCurrentSelected = string.Equals(savedHypothesisId, currentSuspectId, StringComparison.OrdinalIgnoreCase);

            if (hypothesisStampBadge != null)
            {
                hypothesisStampBadge.SetActive(isCurrentSelected);
            }

            if (selectHypothesisButton != null)
            {
                Image btnImg = selectHypothesisButton.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = isCurrentSelected ? HexToColor(ColorGreenCode, 0.95f) : HexToColor(ColorAmberWarning, 0.90f);
                }
            }

            if (selectHypothesisBtnText != null)
            {
                selectHypothesisBtnText.text = isCurrentSelected
                    ? "<b>RESPONSABLE ELEGIDO</b>"
                    : "<b>ELEGIR RESPONSABLE</b>";
                selectHypothesisBtnText.color = HexToColor(ColorDarkBg);
            }
        }

        private string GetCurrentSavedHypothesisId()
        {
            if (ProgressTracker.I != null && !string.IsNullOrEmpty(ProgressTracker.I.SelectedSuspectHypothesis))
            {
                return ProgressTracker.I.SelectedSuspectHypothesis;
            }
            return PlayerPrefs.GetString(PrefKeySelectedSuspect, string.Empty);
        }

        #endregion

        #region Manejo de Interacción del Jugador

        public void OnTabClicked(int index)
        {
            PlaySound(switchSuspectSound, GenerateTabSwitchTone());
            DisplaySuspect(index);
        }

        /// <summary>
        /// Fija al sospechoso actual como la hipótesis fundamentada del equipo de investigación.
        /// </summary>
        public void OnSelectHypothesisClicked()
        {
            if (suspects == null || _selectedSuspectIndex < 0 || _selectedSuspectIndex >= suspects.Count) return;

            SuspectDossierScenario01 suspect = suspects[_selectedSuspectIndex];
            PlaySound(stampHypothesisSound, GenerateHypothesisStampTone());

            // 1. Guardar en PlayerPrefs
            PlayerPrefs.SetString(PrefKeySelectedSuspect, suspect.id);
            PlayerPrefs.SetString(PrefKeyHypothesisReason, suspect.teamHypothesisArgument);
            PlayerPrefs.Save();

            // 2. Notificar a ProgressTracker
            if (ProgressTracker.I != null)
            {
                ProgressTracker.I.SetSelectedSuspectHypothesis(suspect.id);
            }

            // 3. Notificar al sistema de interfaz mediante Toast flotante
            string toastMsg = $"Hipótesis Oficial del Equipo Registrada:\n{suspect.fullName} ({suspect.roleTitle})";
            if (UIManager.I != null)
            {
                UIManager.I.ShowNotification("HIPÓTESIS REGISTRADA", toastMsg, NotificationType.EvidenceFound);
            }

            Debug.Log($"{LogPrefix} <color=#38F28A><b>HIPÓTESIS OFICIAL REGISTRADA:</b></color> {suspect.fullName} ({suspect.id}).\nFundamentación: {suspect.teamHypothesisArgument}");

            // Actualizar vista
            DisplaySuspect(_selectedSuspectIndex);
        }

        #endregion

        #region Helpers de Sonido y UI

        private void PlaySound(AudioClip clip, AudioClip fallbackClip = null)
        {
            if (audioSource == null) return;

            AudioClip toPlay = clip != null ? clip : fallbackClip;
            if (toPlay != null)
            {
                audioSource.PlayOneShot(toPlay);
            }
        }

        private AudioClip GenerateTabSwitchTone()
        {
            if (switchSuspectSound != null) return switchSuspectSound;
            return ProceduralAudioHelper.GenerateSineTone(0.08f, 780f, 0.25f);
        }

        private AudioClip GenerateHypothesisStampTone()
        {
            if (stampHypothesisSound != null) return stampHypothesisSound;
            return ProceduralAudioHelper.GenerateSineTone(0.25f, 440f, 0.40f);
        }

        private static GameObject CreateUiPanel(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static void AddUiBorder(GameObject target, Color borderColor, float width)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(width, -width);
        }

        private static TextMeshProUGUI CreateTmpText(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            return tmp;
        }

        private static Color HexToColor(string hex, float alpha = 1f)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color col))
            {
                col.a = alpha;
                return col;
            }
            return new Color(0.9f, 0.9f, 0.9f, alpha);
        }

        #endregion
    }

    /// <summary>
    /// Generador procedural de tonos sintéticos de audio para retroalimentación VR sin dependencias de clips externos.
    /// </summary>
    public static class ProceduralAudioHelper
    {
        public static AudioClip GenerateSineTone(float duration, float frequency, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (t / duration); // Decaimiento suave
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create("ProceduralTone_" + frequency, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
