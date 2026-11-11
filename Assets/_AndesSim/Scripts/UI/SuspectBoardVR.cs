using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
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
                    roleTitle = "Jefe de Seguridad TI e Infraestructura Bancaria",
                    threatLevelBadge = "SOSPECHOSO DE NEGLIGENCIA / SABOTAJE ARQUITECTÓNICO",
                    threatColorHex = ColorAlertRed,
                    careerAndPsychology =
                        "• <b>Trayectoria:</b> 12 años en banca comercial e infraestructura de redes. Ingeniero de Sistemas colegiado, especialista certificado en CISCO y CheckPoint.\n" +
                        "• <b>Perfil Psicológico:</b> Meticuloso, tecnócrata y amargado por la burocracia directiva. Siente desdén por gerentes no técnicos que recortaron su presupuesto. Adopta una actitud de \"cumplimiento pasivo estricto\": si la dirección no aprueba fondos, no implementa salvaguardas compensatorias.",
                    incriminatingPoints =
                        "• Poseía privilegios de Administrador Supremo (Domain Admin) en todos los switches, servidores de correo Exchange y enrutadores perimetrales.\n" +
                        "• Omitió configurar reglas elementales de filtrado MIME en Exchange para bloquear ejecutables sospechosos (.scr, .exe, .scr.zip).\n" +
                        "• No aplicó segmentación VLAN entre las computadoras de la oficina de pagos y el cuarto de servidores SWIFT, manteniendo la red completamente plana.",
                    exoneratingPoints =
                        "• Presentó 3 memorandos formales por escrito (MEMO-TI-2014-089 y 2014-112) advirtiendo sobre la vulnerabilidad crítica de la red bancaria y solicitando un firewall de última generación con sandbox.\n" +
                        "• Sus credenciales personales no fueron empleadas en la emisión de transferencias fraudulentas de madrugada.",
                    reason16Recipients =
                        "Mantuvo abierta la lista de distribución global 'pagos-personal@bancoandino.pe' sin autenticación SPF/DKIM para remitentes externos, permitiendo que un correo spoofing desde career-applicants-bd.com ingresara sin filtro a los 16 buzones del área.",
                    reasonCarlaPostIt =
                        "Estableció una política de seguridad draconiana obligando a cambiar contraseñas de 16 caracteres alfanuméricos cada 30 días, pero denegó la adquisición de un gestor de contraseñas corporativo y no proveyó llaves MFA, forzando a los operadores a anotar sus claves.",
                    reasonNoFirewallSandbox =
                        "Su solicitud formal de presupuesto para un Appliance Sandbox de detonación de malware fue rechazada por gerencia por considerarla 'costosa'; en represalia pasiva, no implementó reglas locales de software ni scripts perimetrales de contingencia.",
                    teamHypothesisArgument =
                        "<b>HIPÓTESIS DE NEGLIGENCIA ARQUITECTÓNICA Y RESENTIMIENTO INSTITUCIONAL:</b> Samuel no programó el malware de Lazarus, pero facilitó deliberadamente el terreno por omisión pasiva y despecho corporativo, dejando la red desprotegida tras la negativa presupuestaria de la gerencia."
                },

                // 2. Lic. Hugo Benavides
                new SuspectDossierScenario01
                {
                    id = "SUSPECT_HUGO_BENAVIDES",
                    fullName = "Lic. Hugo Benavides",
                    roleTitle = "Gerente de Operaciones y Director Adjunto de Pagos",
                    threatLevelBadge = "SOSPECHOSO DE ENCUBRIMIENTO Y DESIDIA OPERATIVA",
                    threatColorHex = ColorAmberWarning,
                    careerAndPsychology =
                        "• <b>Trayectoria:</b> 18 años en el Banco Andino escalando puestos comerciales y administrativos. Economista con MBA en Finanzas Internacionales. Supervisa la mesa de dinero y las transferencias de alto valor.\n" +
                        "• <b>Perfil Psicológico:</b> Orientado obsesivamente al cumplimiento de metas comerciales y volumen de facturación. Considera la seguridad como 'un obstáculo burocrático que frena el negocio'. Autoritario con sus subordinados y reacio a reportar anomalías que puedan manchar sus auditorías.",
                    incriminatingPoints =
                        "• El viernes previo al incidente encontró la impresora de auditoría SWIFT apagada y ordenó ignorarla para no frenar la jornada de pagos.\n" +
                        "• Presionó activamente para desestimar los filtros de seguridad del correo que ralentizaban la recepción de comprobantes de clientes.\n" +
                        "• Mantuvo un patrón de ocultamiento de incidentes menores en auditorías previas de la SBS.",
                    exoneratingPoints =
                        "• Cero competencias informáticas avanzadas; incapaz de interactuar con consolas Linux, compilar scripts o establecer túneles de comando y control (C2).\n" +
                        "• Registró presencia en un seminario internacional en Lima durante la ejecución de las transferencias.",
                    reason16Recipients =
                        "Publicó en portales de empleo y redes corporativas los nombres y correos del personal de pagos para una convocatoria de pasantías, entregando involuntariamente la lista de objetivos a la célula de ingeniería social de Lazarus.",
                    reasonCarlaPostIt =
                        "Exigía jornadas con sobretiempo no planificado y relevos acelerados de operadores. Alentaba informalmente que los supervisores conocieran las claves de sus subordinados para 'destrabar pagos urgentes', normalizando el Post-It a la vista pública.",
                    reasonNoFirewallSandbox =
                        "En el comité de presupuesto votó activamente en contra del proyecto de Sandbox de TI, argumentando que 'un banco no puede gastar decenas de miles de dólares en analizar correos cuando necesitamos liquidez operativa'.",
                    teamHypothesisArgument =
                        "<b>HIPÓTESIS DE DESIDIA GERENCIAL Y SABOTAJE PROCEDIMENTAL:</b> Su hostilidad hacia los controles de seguridad y la presión despiadada sobre el personal anularon los procedimientos básicos de custodia, creando el caldo de cultivo que los atacantes explotaron."
                },

                // 3. Carla Mendoza
                new SuspectDossierScenario01
                {
                    id = "SUSPECT_CARLA_MENDOZA",
                    fullName = "Carla Mendoza",
                    roleTitle = "Operadora Senior de Pagos Internacionales (SWIFT)",
                    threatLevelBadge = "PACIENTE CERO / VECTOR INVOLUNTARIO VULNERABLE",
                    threatColorHex = ColorGreenCode,
                    careerAndPsychology =
                        "• <b>Trayectoria:</b> 7 años en la institución, con excelente récord de puntualidad y precisión transaccional. Certificada como Operadora Autorizada de Mensajería Financiera SWIFT Alliance Access.\n" +
                        "• <b>Perfil Psicológico:</b> Empática, servicial, sumamente perfeccionista pero con altos niveles de estrés y fatiga acumulada. Confiada ante pedidos de ayuda o postulaciones laborales externas. Miedo a cometer errores que deriven en sanciones disciplinarias.",
                    incriminatingPoints =
                        "• Su usuario de operador SWIFT ('C_MENDOZA_OP1') fue el utilizado para autorizar las 36 órdenes de transferencia nocturnas por US$ 951 millones.\n" +
                        "• Registró conexiones y horas extra no autorizadas en su puesto de trabajo días antes del ciberataque.",
                    exoneratingPoints =
                        "• Fue el objetivo inicial de phishing ('Patient Zero') junto con otros 15 empleados bancarios.\n" +
                        "• Peritaje forense comprobó que durante las transferencias nocturnas ella se encontraba en su domicilio; el malware operó su sesión de manera remota mediante la puerta trasera instalada.",
                    reason16Recipients =
                        "Su cuenta corporativa estaba expuesta en intercambios con bancos corresponsales extranjeros, figurando en la lista de correo de reclutamiento como contacto de referencia para recepción de currículums.",
                    reasonCarlaPostIt =
                        "Debido a la imposición de contraseñas de 16 caracteres alfanuméricos cambiadas mensualmente y a la prohibición de gestores digitales, anotaba la clave bajo el teclado para no bloquear la cuenta durante los cierres de pagos a presión.",
                    reasonNoFirewallSandbox =
                        "Al carecer de capacitación en ciberseguridad y no contar con un sandbox que examinara el archivo 'Rasel_Ahlam_CV_2015.scr.zip', confió ciegamente en que la red del banco filtraba cualquier archivo peligroso.",
                    teamHypothesisArgument =
                        "<b>HIPÓTESIS DE VÍCTIMA DE INGENIERÍA SOCIAL (PACIENTE CERO):</b> Carla no actuó con dolo criminal; fue el eslabón humano más débil explotado por Lazarus debido a la total ausencia de capacitación en phishing y a una higiene de credenciales comprometida por políticas inadecuadas."
                },

                // 4. Renzo Aliaga
                new SuspectDossierScenario01
                {
                    id = "SUSPECT_RENZO_ALIAGA",
                    fullName = "Renzo Aliaga",
                    roleTitle = "Técnico Residente de Soporte y Cableado de Redes (NetSolutions)",
                    threatLevelBadge = "SOSPECHOSO DE COMPLICIDAD INTERNA / INSIDER THREAT",
                    threatColorHex = ColorAlertRed,
                    careerAndPsychology =
                        "• <b>Trayectoria:</b> Técnico informático contratado por la empresa proveedora de soporte y mantenimiento de cableado estructurado. 4 años realizando servicios periódicos en la sede del Banco Andino.\n" +
                        "• <b>Perfil Psicológico:</b> Reservado, callado, pasa desapercibido. Reportes financieros indican deudas bancarias considerables en cobranza judicial. Interés documentado en comunidades underground de ciberseguridad ofensiva.",
                    incriminatingPoints =
                        "• Permaneció en la oficina de pagos durante el mantenimiento físico de las terminales 15 días antes del ataque, trabajando directamente debajo de los escritorios de los operadores.\n" +
                        "• Sus registros de tarjeta de acceso muestran ingresos a la sala de servidores en horarios de cambio de guardia sin supervisor presente.",
                    exoneratingPoints =
                        "• Todas las órdenes de trabajo estaban firmadas y avaladas por la jefatura de TI.\n" +
                        "• Las cámaras de vigilancia no muestran inserción de memorias USB ni extracción física de componentes de las terminales.",
                    reason16Recipients =
                        "Durante el mantenimiento del cableado de red en las computadoras de pagos tuvo acceso visual y físico a las libretas de direcciones locales de Outlook, pudiendo fotografiar o extraer la lista de los 16 destinatarios.",
                    reasonCarlaPostIt =
                        "Realizó labores de recableado y fijación de ductos debajo del escritorio de Carla Mendoza; estuvo en posición directa de observar el Post-It con las credenciales anotadas pegado debajo de la bandeja deslizable del teclado.",
                    reasonNoFirewallSandbox =
                        "Conocía a la perfección la arquitectura física de los switches del piso y sabía que las terminales de pagos compartían el mismo segmento broadcast sin inspección interna, información de oro para planificar el salto lateral hacia el nodo SWIFT.",
                    teamHypothesisArgument =
                        "<b>HIPÓTESIS DE CÓMPLICE INTERNO / RECOLECTOR FÍSICO (INSIDER THREAT):</b> Renzo actuó como facilitador pagado o informante dentro del banco, vendiendo la topología interna, la lista de correos de pagos y las credenciales observadas en el escritorio de Carla al grupo Lazarus."
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
            rootRect.localScale = new Vector3(0.0022f, 0.0022f, 0.0022f);

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

            headerTitleText = CreateTmpText("Title", headerObj.transform, new Vector2(-150f, 8f), new Vector2(920f, 32f),
                "<b><color=#38F28A>TABLERO TÁCTICO DE INVESTIGACIÓN</color> · CASO BANCO ANDINO 2016</b>", 20f, TextAlignmentOptions.Left, HexToColor(ColorTextLight));

            headerSubtitleText = CreateTmpText("Subtitle", headerObj.transform, new Vector2(-150f, -16f), new Vector2(920f, 22f),
                "<i>Expedientes de los 4 Sospechosos e Hipótesis de Complicidad Interna (Oficina de Pagos - RF-02)</i>", 13f, TextAlignmentOptions.Left, HexToColor(ColorMutedText));

            // Sello superior de hipótesis activa
            GameObject stampObj = CreateUiPanel("Hypothesis_Global_Stamp", headerObj.transform, new Vector2(480f, 0f), new Vector2(260f, 44f), HexToColor(ColorCardBg, 0.98f));
            AddUiBorder(stampObj, HexToColor(ColorGreenCode, 0.9f), 2f);
            hypothesisStampBadge = stampObj;

            hypothesisStampText = CreateTmpText("StampText", stampObj.transform, Vector2.zero, new Vector2(250f, 40f),
                "<b>[*] HIPÓTESIS REGISTRADA [*]</b>", 12f, TextAlignmentOptions.Center, HexToColor(ColorGreenCode));

            // Contenedor de Botones de Pestañas (Izquierda: 280px de ancho)
            GameObject tabsObj = CreateUiPanel("Container_Tabs", bgObj.transform, new Vector2(-490f, -40f), new Vector2(280f, 740f), HexToColor(ColorCardBg, 0.85f));
            AddUiBorder(tabsObj, HexToColor(ColorGreenCode, 0.3f), 1.5f);
            tabsContainer = tabsObj.transform;

            CreateTmpText("TabsHeader", tabsObj.transform, new Vector2(0f, 345f), new Vector2(260f, 30f),
                "<b>SOSPECHOSOS (4)</b>", 15f, TextAlignmentOptions.Center, HexToColor(ColorGreenCode));

            // Contenedor del Expediente Principal (Derecha: 950px de ancho)
            GameObject dossierObj = CreateUiPanel("Container_Dossier_Body", bgObj.transform, new Vector2(145f, -40f), new Vector2(960f, 740f), HexToColor(ColorCardBg, 0.90f));
            AddUiBorder(dossierObj, HexToColor(ColorGreenCode, 0.4f), 1.5f);

            // 1. Cabecera del Sospechoso Activo
            GameObject suspectHeaderObj = CreateUiPanel("Suspect_Profile_Header", dossierObj.transform, new Vector2(0f, 325f), new Vector2(920f, 70f), HexToColor(ColorDarkBg, 0.95f));
            AddUiBorder(suspectHeaderObj, HexToColor(ColorGreenCode, 0.35f), 1f);

            suspectNameText = CreateTmpText("SuspectName", suspectHeaderObj.transform, new Vector2(-170f, 12f), new Vector2(540f, 34f),
                "<b>Ing. Samuel Torres</b>", 22f, TextAlignmentOptions.Left, HexToColor(ColorTextLight));

            suspectRoleText = CreateTmpText("SuspectRole", suspectHeaderObj.transform, new Vector2(-170f, -14f), new Vector2(540f, 24f),
                "Cargo: <b>Jefe de Seguridad TI e Infraestructura</b>", 13f, TextAlignmentOptions.Left, HexToColor(ColorGreenCode));

            threatBadgeText = CreateTmpText("ThreatBadge", suspectHeaderObj.transform, new Vector2(300f, 0f), new Vector2(280f, 40f),
                "<b>[SOSPECHOSO DE NEGLIGENCIA]</b>", 12f, TextAlignmentOptions.Center, HexToColor(ColorAlertRed));

            // 2. Columna Izquierda: Carrera / Psicología / Indicios
            GameObject colLeftObj = CreateUiPanel("Col_Trayectoria", dossierObj.transform, new Vector2(-235f, 90f), new Vector2(440f, 360f), HexToColor(ColorDarkBg, 0.7f));
            AddUiBorder(colLeftObj, HexToColor(ColorGreenCode, 0.25f), 1f);

            CreateTmpText("ColLeftTitle", colLeftObj.transform, new Vector2(0f, 160f), new Vector2(410f, 26f),
                "<b>1. TRAYECTORIA & PERFIL PSICOLÓGICO</b>", 13f, TextAlignmentOptions.Left, HexToColor(ColorGreenCode));

            careerPsychologyText = CreateTmpText("ColLeftBody", colLeftObj.transform, new Vector2(0f, 15f), new Vector2(410f, 250f),
                "Cargando antecedentes...", 11f, TextAlignmentOptions.TopLeft, HexToColor(ColorTextLight));

            // 3. Columna Derecha: Indicios vs Coartada
            GameObject colRightObj = CreateUiPanel("Col_Indicios", dossierObj.transform, new Vector2(235f, 90f), new Vector2(440f, 360f), HexToColor(ColorDarkBg, 0.7f));
            AddUiBorder(colRightObj, HexToColor(ColorGreenCode, 0.25f), 1f);

            CreateTmpText("ColRightTitle", colRightObj.transform, new Vector2(0f, 160f), new Vector2(410f, 26f),
                "<b>2. ANÁLISIS FORENSE (INCULPACIÓN / COARTADA)</b>", 13f, TextAlignmentOptions.Left, HexToColor(ColorAmberWarning));

            incriminatingFactsText = CreateTmpText("ColRightIncrim", colRightObj.transform, new Vector2(0f, 70f), new Vector2(410f, 140f),
                "<b>Indicios en contra:</b>\n...", 10.5f, TextAlignmentOptions.TopLeft, HexToColor(ColorAlertRed));

            exoneratingFactsText = CreateTmpText("ColRightExon", colRightObj.transform, new Vector2(0f, -75f), new Vector2(410f, 130f),
                "<b>Puntos a favor / Coartada:</b>\n...", 10.5f, TextAlignmentOptions.TopLeft, HexToColor(ColorGreenCode));

            // 4. Panel Inferior: CLAVES DEL ESCENARIO 1 (16 correos, Post-It de Carla, Red sin sandbox)
            GameObject scenarioKeysObj = CreateUiPanel("Panel_Claves_Escenario1", dossierObj.transform, new Vector2(0f, -145f), new Vector2(920f, 140f), HexToColor(ColorDarkBg, 0.85f));
            AddUiBorder(scenarioKeysObj, HexToColor(ColorGreenCode, 0.3f), 1f);

            CreateTmpText("ScenarioKeysTitle", scenarioKeysObj.transform, new Vector2(0f, 52f), new Vector2(890f, 24f),
                "<b>3. CLAVES DEL ESCENARIO 1 (OFICINA DE PAGOS - BANCO ANDINO)</b>", 13f, TextAlignmentOptions.Left, HexToColor(ColorGreenCode));

            scenario01AnalysisText = CreateTmpText("ScenarioKeysBody", scenarioKeysObj.transform, new Vector2(0f, -12f), new Vector2(890f, 95f),
                "• <b>16 Destinatarios:</b> ...\n• <b>Post-It de Carla:</b> ...\n• <b>Red sin Sandbox:</b> ...", 10.5f, TextAlignmentOptions.TopLeft, HexToColor(ColorTextLight));

            // 5. Barra Inferior: Hipótesis Fundamentada y Botón de Marcado
            GameObject hypothesisBarObj = CreateUiPanel("Panel_Hipotesis_Accion", dossierObj.transform, new Vector2(0f, -285f), new Vector2(920f, 115f), HexToColor(ColorDarkBg, 0.95f));
            AddUiBorder(hypothesisBarObj, HexToColor(ColorGreenCode, 0.5f), 1.5f);

            hypothesisArgumentText = CreateTmpText("HypothesisSummary", hypothesisBarObj.transform, new Vector2(-155f, 0f), new Vector2(570f, 95f),
                "<b>HIPÓTESIS DEL EQUIPO:</b> Cargando fundamentación...", 11.5f, TextAlignmentOptions.Left, HexToColor(ColorTextLight));

            // Botón interactivo de selección de hipótesis
            GameObject btnSelectObj = CreateUiPanel("Btn_MarcarHipotesis", hypothesisBarObj.transform, new Vector2(300f, 0f), new Vector2(260f, 65f), HexToColor(ColorGreenCode, 0.9f));
            AddUiBorder(btnSelectObj, HexToColor(ColorDarkBg, 1f), 2f);

            selectHypothesisButton = btnSelectObj.AddComponent<Button>();
            selectHypothesisBtnText = CreateTmpText("BtnLabel", btnSelectObj.transform, Vector2.zero, new Vector2(250f, 55f),
                "<b>[*] FIJAR COMO HIPÓTESIS\nOFICIAL DEL EQUIPO [*]</b>", 12f, TextAlignmentOptions.Center, HexToColor(ColorDarkBg));

            // Conectar el adaptador de retícula VR
            WorldSpaceUiGazeButton gazeBtn = btnSelectObj.AddComponent<WorldSpaceUiGazeButton>();
            gazeBtn.ConfigureAudio(audioSource, stampHypothesisSound, null);
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

            float startY = 280f;
            float spacingY = 155f;

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

                // Etiquetas internas del botón
                TextMeshProUGUI label = CreateTmpText("Label", tabObj.transform, new Vector2(0f, 25f), new Vector2(230f, 32f),
                    $"<b>{suspect.fullName}</b>", 14f, TextAlignmentOptions.Center, HexToColor(ColorTextLight));
                _tabButtonTexts.Add(label);

                CreateTmpText("Role", tabObj.transform, new Vector2(0f, -5f), new Vector2(230f, 24f),
                    suspect.roleTitle, 10f, TextAlignmentOptions.Center, HexToColor(ColorGreenCode));

                GameObject miniStamp = CreateUiPanel("MiniStamp", tabObj.transform, new Vector2(0f, -42f), new Vector2(220f, 24f), HexToColor(ColorCardBg, 0.95f));
                AddUiBorder(miniStamp, HexToColor(ColorGreenCode, 0.5f), 1f);
                CreateTmpText("StampText", miniStamp.transform, Vector2.zero, new Vector2(210f, 20f),
                    $"[EXPEDIENTE #{index + 1}]", 9.5f, TextAlignmentOptions.Center, HexToColor(ColorMutedText));
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
            if (suspectRoleText != null) suspectRoleText.text = $"Cargo: <b>{suspect.roleTitle}</b>";

            // 2. Distintivo de Amenaza
            if (threatBadgeText != null)
            {
                threatBadgeText.text = $"<b>[{suspect.threatLevelBadge}]</b>";
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
                incriminatingFactsText.text = $"<b><color={ColorAlertRed}>Indicios en contra / Negligencia:</color></b>\n{suspect.incriminatingPoints}";
            }

            // 5. Puntos a Favor / Coartada
            if (exoneratingFactsText != null)
            {
                exoneratingFactsText.text = $"<b><color={ColorGreenCode}>Puntos a favor / Coartada:</color></b>\n{suspect.exoneratingPoints}";
            }

            // 6. Análisis Clave del Escenario 1
            if (scenario01AnalysisText != null)
            {
                scenario01AnalysisText.text =
                    $"• <b>¿Por qué el correo llegó a 16 personas?:</b> {suspect.reason16Recipients}\n" +
                    $"• <b>¿Por qué la clave de Carla estaba en un Post-It?:</b> {suspect.reasonCarlaPostIt}\n" +
                    $"• <b>¿Por qué la red carecía de firewall/sandbox?:</b> {suspect.reasonNoFirewallSandbox}";
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
                    ? "<b>[*] HIPÓTESIS OFICIAL DEL EQUIPO\n(REGISTRADA PARA EXPOSICIÓN) [*]</b>"
                    : "<b>[*] FIJAR COMO HIPÓTESIS OFICIAL\nDEL EQUIPO (PULSAR CON RETÍCULA) [*]</b>";
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
