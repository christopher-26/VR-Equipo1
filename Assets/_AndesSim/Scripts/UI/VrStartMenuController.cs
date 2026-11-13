using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Audio;
using AndesSim.Core;
using AndesSim.Data;
using AndesSim.Environment;
using AndesSim.Scenario01;
using AndesSim.UI;

namespace AndesSim.VR
{
    /// <summary>
    /// Controlador central del Centro Holográfico Flotante VR "ANDES-SIM: BANCO ANDINO".
    /// Menú interactivo de 5 pestañas suspendido en el vestíbulo con tipografía moderna,
    /// animaciones EaseOutBack, vista previa 3D del virus, selector de iluminación ambiental,
    /// teletransporte directo por puestos, fichas de sospechosos y accesibilidad Cardboard.
    /// </summary>
    public class VrStartMenuController : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - VrStartMenu]";

        public static VrStartMenuController I { get; private set; }

        public enum MenuTab
        {
            MisionYModos = 0,
            MapaYPuestos = 1,
            Sospechosos = 2,
            GuiaVR = 3,
            AjustesYFx = 4
        }

        [Header("--- Paneles de Pestañas (5) ---")]
        [SerializeField] private GameObject[] tabPanels;
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private Image[] tabButtonBgs;
        [SerializeField] private TextMeshProUGUI[] tabButtonLabels;

        [Header("--- Tab 0: Inicio & Modos ---")]
        [SerializeField] private Button btnModoAtaque;
        [SerializeField] private Button btnModoDefensa;
        [SerializeField] private Image imgModoAtaqueBorder;
        [SerializeField] private Image imgModoDefensaBorder;
        [SerializeField] private TextMeshProUGUI txtModoSeleccionadoBadge;
        [SerializeField] private TextMeshProUGUI txtModoDescripcion;
        [SerializeField] private Button btnIniciarSimulacion;

        [Header("--- Flujo Rápido y Accesos Directos ---")]
        [SerializeField] private Button btnTogglePasos;
        [SerializeField] private TextMeshProUGUI txtBtnTogglePasosLabel;
        [SerializeField] private GameObject panelVistaModos;
        [SerializeField] private GameObject panelVistaPasos;
        [SerializeField] private Button btnAbrirCreditos;

        [Header("--- Tab 1: Teletransporte Directo por Puestos ---")]
        [SerializeField] private Button[] quickTeleportButtons;
        [SerializeField] private string[] quickTeleportPointNames;

        [Header("--- Tab 2: Sospechosos (4 Perfiles) ---")]
        [SerializeField] private Button[] suspectCardsButtons;
        [SerializeField] private Image[] suspectCardBgs;
        [SerializeField] private GameObject[] suspectDetailPanels;
        [SerializeField] private TextMeshProUGUI suspectPreviewTitle;
        [SerializeField] private TextMeshProUGUI suspectPreviewRole;
        [SerializeField] private TextMeshProUGUI suspectPreviewBadge;
        [SerializeField] private TextMeshProUGUI suspectPreviewClues;
        [SerializeField] private Button btnGoTableroFromMenu;

        [Header("--- Tab 4: Ajustes, Iluminación y FX ---")]
        [SerializeField] private Button btnDwellRapido;      // 1.0s
        [SerializeField] private Button btnDwellEstandar;    // 1.6s
        [SerializeField] private Button btnDwellAccesible;   // 2.4s
        [SerializeField] private TextMeshProUGUI txtDwellActualBadge;
        [SerializeField] private Button btnToggleAudio;
        [SerializeField] private TextMeshProUGUI txtAudioStatus;
        [SerializeField] private Button btnTestVoice;
        [SerializeField] private Button btnLightClara;
        [SerializeField] private Button btnLightCyan;
        [SerializeField] private Button btnLightNocturna;
        [SerializeField] private TextMeshProUGUI txtLightingBadge;
        [SerializeField] private Button btnPreviewVirusFx;

        [Header("--- Audio y Efectos ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip tabSwitchSound;
        [SerializeField] private AudioClip modeSwitchSound;
        [SerializeField] private AudioClip startSimWarpSound;

        [Header("--- Teletransporte y Contenedor Flotante ---")]
        [SerializeField] private TeleportPoint teleportDestination;
        [SerializeField] private CanvasGroup menuCanvasGroup;
        [SerializeField] private Transform holoFloatingRoot;

        // Propiedades públicas para inyección desde Scene Assembler
        public GameObject[] TabPanels { get => tabPanels; set => tabPanels = value; }
        public Button[] TabButtons { get => tabButtons; set => tabButtons = value; }
        public Image[] TabButtonBgs { get => tabButtonBgs; set => tabButtonBgs = value; }
        public TextMeshProUGUI[] TabButtonLabels { get => tabButtonLabels; set => tabButtonLabels = value; }

        public Button BtnModoAtaque { get => btnModoAtaque; set => btnModoAtaque = value; }
        public Button BtnModoDefensa { get => btnModoDefensa; set => btnModoDefensa = value; }
        public Image ImgModoAtaqueBorder { get => imgModoAtaqueBorder; set => imgModoAtaqueBorder = value; }
        public Image ImgModoDefensaBorder { get => imgModoDefensaBorder; set => imgModoDefensaBorder = value; }
        public TextMeshProUGUI TxtModoSeleccionadoBadge { get => txtModoSeleccionadoBadge; set => txtModoSeleccionadoBadge = value; }
        public TextMeshProUGUI TxtModoDescripcion { get => txtModoDescripcion; set => txtModoDescripcion = value; }
        public Button BtnIniciarSimulacion { get => btnIniciarSimulacion; set => btnIniciarSimulacion = value; }
        public Button BtnTogglePasos { get => btnTogglePasos; set => btnTogglePasos = value; }
        public TextMeshProUGUI TxtBtnTogglePasosLabel { get => txtBtnTogglePasosLabel; set => txtBtnTogglePasosLabel = value; }
        public GameObject PanelVistaModos { get => panelVistaModos; set => panelVistaModos = value; }
        public GameObject PanelVistaPasos { get => panelVistaPasos; set => panelVistaPasos = value; }
        public Button BtnAbrirCreditos { get => btnAbrirCreditos; set => btnAbrirCreditos = value; }

        public Button[] QuickTeleportButtons { get => quickTeleportButtons; set => quickTeleportButtons = value; }
        public string[] QuickTeleportPointNames { get => quickTeleportPointNames; set => quickTeleportPointNames = value; }

        public Button[] SuspectCardsButtons { get => suspectCardsButtons; set => suspectCardsButtons = value; }
        public Image[] SuspectCardBgs { get => suspectCardBgs; set => suspectCardBgs = value; }
        public TextMeshProUGUI SuspectPreviewTitle { get => suspectPreviewTitle; set => suspectPreviewTitle = value; }
        public TextMeshProUGUI SuspectPreviewRole { get => suspectPreviewRole; set => suspectPreviewRole = value; }
        public TextMeshProUGUI SuspectPreviewBadge { get => suspectPreviewBadge; set => suspectPreviewBadge = value; }
        public TextMeshProUGUI SuspectPreviewClues { get => suspectPreviewClues; set => suspectPreviewClues = value; }
        public Button BtnGoTableroFromMenu { get => btnGoTableroFromMenu; set => btnGoTableroFromMenu = value; }

        public Button BtnDwellRapido { get => btnDwellRapido; set => btnDwellRapido = value; }
        public Button BtnDwellEstandar { get => btnDwellEstandar; set => btnDwellEstandar = value; }
        public Button BtnDwellAccesible { get => btnDwellAccesible; set => btnDwellAccesible = value; }
        public TextMeshProUGUI TxtDwellActualBadge { get => txtDwellActualBadge; set => txtDwellActualBadge = value; }
        public Button BtnToggleAudio { get => btnToggleAudio; set => btnToggleAudio = value; }
        public TextMeshProUGUI TxtAudioStatus { get => txtAudioStatus; set => txtAudioStatus = value; }
        public Button BtnTestVoice { get => btnTestVoice; set => btnTestVoice = value; }
        public Button BtnLightClara { get => btnLightClara; set => btnLightClara = value; }
        public Button BtnLightCyan { get => btnLightCyan; set => btnLightCyan = value; }
        public Button BtnLightNocturna { get => btnLightNocturna; set => btnLightNocturna = value; }
        public TextMeshProUGUI TxtLightingBadge { get => txtLightingBadge; set => txtLightingBadge = value; }
        public Button BtnPreviewVirusFx { get => btnPreviewVirusFx; set => btnPreviewVirusFx = value; }

        public CanvasGroup MenuCanvasGroup { get => menuCanvasGroup; set => menuCanvasGroup = value; }
        public TeleportPoint TeleportDestination { get => teleportDestination; set => teleportDestination = value; }
        public Transform HoloFloatingRoot { get => holoFloatingRoot; set => holoFloatingRoot = value; }

        // Paleta oficial
        private static readonly Color TabActiveColor = AndesTypographyTheme.AccentCyan;
        private static readonly Color TabInactiveColor = new Color(0.08f, 0.16f, 0.27f, 0.92f);
        private static readonly Color TabActiveText = new Color(0.03f, 0.08f, 0.14f, 1f);
        private static readonly Color TabInactiveText = new Color(0.78f, 0.90f, 1.00f, 1f);

        private static readonly Color AttackRed = AndesTypographyTheme.AccentRed;
        private static readonly Color DefenseGreen = AndesTypographyTheme.AccentEmerald;

        private MenuTab _currentTab = MenuTab.MisionYModos;
        private float _currentDwellTime = 1.6f;
        private bool _audioMuted = false;
        private bool _isStarting = false;
        private int _selectedSuspectIdx = 0;
        private bool _showingPasosView;
        private Coroutine _previewFxCoroutine;

        private struct SuspectQuickProfile
        {
            public string Name;
            public string Role;
            public string Badge;
            public Color BadgeColor;
            public string Clues;
        }

        private static readonly SuspectQuickProfile[] DefaultSuspectProfiles =
        {
            new SuspectQuickProfile
            {
                Name = "ING. SAMUEL TORRES",
                Role = "Jefe de Seguridad TI",
                Badge = "NEGLIGENCIA DE RED",
                BadgeColor = new Color(1.0f, 0.35f, 0.31f, 1f),
                Clues = "· Administrador de correo y switches sin VLANs ni bloqueo .exe.\n· Pidió 3 veces presupuesto para Sandbox antes del ataque."
            },
            new SuspectQuickProfile
            {
                Name = "LIC. HUGO BENAVIDES",
                Role = "Gerente de Operaciones",
                Badge = "DESIDIA GERENCIAL",
                BadgeColor = new Color(1.0f, 0.66f, 0.25f, 1f),
                Clues = "· Pidió quitar filtros para acelerar cierres y votó contra el Sandbox.\n· Aceptó el uso de claves en Post-It por metas comerciales."
            },
            new SuspectQuickProfile
            {
                Name = "CARLA MENDOZA",
                Role = "Operadora Senior SWIFT",
                Badge = "VICTIMA PHISHING",
                BadgeColor = new Color(0.22f, 0.95f, 0.54f, 1f),
                Clues = "· Su terminal PC 1 ejecutó el correo falso y dejó su clave en Post-It.\n· Engañada por ingeniería social; estaba en casa durante el robo."
            },
            new SuspectQuickProfile
            {
                Name = "RENZO ALIAGA",
                Role = "Soporte Redes (Externo)",
                Badge = "SOSPECHOSO INTERNO",
                BadgeColor = new Color(0.65f, 0.54f, 0.98f, 1f),
                Clues = "· Cableó debajo del escritorio de Carla cerca del Post-It amarillo.\n· Ingresó de noche al área de servidores sin guía registrada."
            }
        };

        private void Awake()
        {
            if (I == null) I = this;
            else if (I != this) { Destroy(gameObject); return; }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            AndesTypographyTheme.ApplyTypographyToTree(transform, true, true);
            SetupButtonListeners();
            UpdateTabVisuals(MenuTab.MisionYModos);
            UpdateModeDisplay();
            UpdateDwellDisplay();
            UpdateLightingDisplay(SocLightingController.I != null ? SocLightingController.I.ActivePreset : SocLightingPreset.OficinaClara);
            SelectSuspectPreview(0, silent: true);

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += OnExternalModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (I == this) I = null;
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= OnExternalModeChanged;
            }
        }

        private void SetupButtonListeners()
        {
            if (tabButtons != null)
            {
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    int index = i;
                    if (tabButtons[i] != null)
                    {
                        tabButtons[i].onClick.AddListener(() => SwitchTab((MenuTab)index));
                    }
                }
            }

            if (btnModoAtaque != null) btnModoAtaque.onClick.AddListener(() => SelectMode(SimMode.Ataque));
            if (btnModoDefensa != null) btnModoDefensa.onClick.AddListener(() => SelectMode(SimMode.Defensa));
            if (btnIniciarSimulacion != null) btnIniciarSimulacion.onClick.AddListener(IniciarSimulacion);

            if (btnDwellRapido != null) btnDwellRapido.onClick.AddListener(() => SetDwellTime(1.0f));
            if (btnDwellEstandar != null) btnDwellEstandar.onClick.AddListener(() => SetDwellTime(1.6f));
            if (btnDwellAccesible != null) btnDwellAccesible.onClick.AddListener(() => SetDwellTime(2.4f));

            if (btnToggleAudio != null) btnToggleAudio.onClick.AddListener(ToggleAudio);
            if (btnTestVoice != null) btnTestVoice.onClick.AddListener(PlayTestVoiceSample);

            if (btnLightClara != null) btnLightClara.onClick.AddListener(() => ApplyLightingPreset(SocLightingPreset.OficinaClara));
            if (btnLightCyan != null) btnLightCyan.onClick.AddListener(() => ApplyLightingPreset(SocLightingPreset.SocCyan));
            if (btnLightNocturna != null) btnLightNocturna.onClick.AddListener(() => ApplyLightingPreset(SocLightingPreset.CyberNocturno));

            if (btnPreviewVirusFx != null) btnPreviewVirusFx.onClick.AddListener(StartVirusPreviewFromMenu);

            if (btnTogglePasos != null) btnTogglePasos.onClick.AddListener(TogglePasosView);
            if (btnAbrirCreditos != null) btnAbrirCreditos.onClick.AddListener(OpenCreditsModal);

            if (quickTeleportButtons != null && quickTeleportPointNames != null)
            {
                int count = Mathf.Min(quickTeleportButtons.Length, quickTeleportPointNames.Length);
                for (int i = 0; i < count; i++)
                {
                    string tpName = quickTeleportPointNames[i];
                    if (quickTeleportButtons[i] != null && !string.IsNullOrEmpty(tpName))
                    {
                        quickTeleportButtons[i].onClick.AddListener(() => IniciarSimulacionToPoint(tpName));
                    }
                }
            }

            if (suspectCardsButtons != null)
            {
                for (int i = 0; i < suspectCardsButtons.Length; i++)
                {
                    int idx = i;
                    if (suspectCardsButtons[i] != null)
                    {
                        suspectCardsButtons[i].onClick.AddListener(() => SelectSuspectPreview(idx, silent: false));
                    }
                }
            }

            if (btnGoTableroFromMenu != null)
            {
                btnGoTableroFromMenu.onClick.AddListener(() => IniciarSimulacionToPoint("Teleport_TableroSospechosos"));
            }

            SetPasosViewVisible(false);
        }

        public void SwitchTab(MenuTab tab)
        {
            _currentTab = tab;
            PlayUiSound(tabSwitchSound, 880f);
            UpdateTabVisuals(tab);
        }

        private void UpdateTabVisuals(MenuTab activeTab)
        {
            if (tabPanels == null) return;

            for (int i = 0; i < tabPanels.Length; i++)
            {
                bool isActive = (i == (int)activeTab);
                if (tabPanels[i] != null)
                {
                    tabPanels[i].SetActive(isActive);
                    if (isActive)
                    {
                        HolographicPanelEntrance ent = tabPanels[i].GetComponent<HolographicPanelEntrance>();
                        ent?.PlayEntrance();
                    }
                }

                if (tabButtonBgs != null && i < tabButtonBgs.Length && tabButtonBgs[i] != null)
                {
                    tabButtonBgs[i].color = isActive ? TabActiveColor : TabInactiveColor;
                }

                if (tabButtonLabels != null && i < tabButtonLabels.Length && tabButtonLabels[i] != null)
                {
                    tabButtonLabels[i].color = isActive ? TabActiveText : TabInactiveText;
                    tabButtonLabels[i].fontStyle = FontStyles.Bold;
                }
            }
        }

        public void SelectSuspectPreview(int index, bool silent)
        {
            _selectedSuspectIdx = Mathf.Clamp(index, 0, DefaultSuspectProfiles.Length - 1);
            if (!silent) PlayUiSound(tabSwitchSound, 760f);

            SuspectQuickProfile prof = DefaultSuspectProfiles[_selectedSuspectIdx];
            if (suspectPreviewTitle != null) suspectPreviewTitle.text = prof.Name;
            if (suspectPreviewRole != null) suspectPreviewRole.text = prof.Role;
            if (suspectPreviewBadge != null)
            {
                suspectPreviewBadge.text = prof.Badge;
                suspectPreviewBadge.color = prof.BadgeColor;
            }
            if (suspectPreviewClues != null) suspectPreviewClues.text = prof.Clues;

            if (suspectCardBgs != null)
            {
                for (int i = 0; i < suspectCardBgs.Length; i++)
                {
                    if (suspectCardBgs[i] != null)
                    {
                        suspectCardBgs[i].color = (i == _selectedSuspectIdx)
                            ? new Color(0.11f, 0.34f, 0.45f, 0.98f)
                            : new Color(0.08f, 0.14f, 0.24f, 0.94f);
                    }
                }
            }
        }

        public void ApplyLightingPreset(SocLightingPreset preset)
        {
            PlayUiSound(tabSwitchSound, 940f);
            if (SocLightingController.I != null)
            {
                SocLightingController.I.ApplyLightingPreset(preset);
            }
            UpdateLightingDisplay(preset);
        }

        private void UpdateLightingDisplay(SocLightingPreset preset)
        {
            if (txtLightingBadge == null) return;
            switch (preset)
            {
                case SocLightingPreset.SocCyan:
                    txtLightingBadge.text = "AMBIENTE ACTIVO: <b>SOC CYAN NEON</b>";
                    txtLightingBadge.color = AndesTypographyTheme.AccentCyan;
                    break;
                case SocLightingPreset.CyberNocturno:
                    txtLightingBadge.text = "AMBIENTE ACTIVO: <b>CYBER NOCTURNO</b>";
                    txtLightingBadge.color = AndesTypographyTheme.AccentViolet;
                    break;
                default:
                    txtLightingBadge.text = "AMBIENTE ACTIVO: <b>OFICINA CLARA</b>";
                    txtLightingBadge.color = AndesTypographyTheme.AccentEmerald;
                    break;
            }
        }

        public void PlayTestVoiceSample()
        {
            PlayUiSound(tabSwitchSound, 1020f);
            if (AudioManager.I != null)
            {
                AudioManager.I.PlayNarration("Voice_01_Bienvenida", true, 0f);
            }
        }

        public void StartVirusPreviewFromMenu()
        {
            if (_previewFxCoroutine != null) StopCoroutine(_previewFxCoroutine);
            _previewFxCoroutine = StartCoroutine(PreviewVirusPanoramicRoutine());
        }

        private IEnumerator PreviewVirusPanoramicRoutine()
        {
            PlayUiSound(modeSwitchSound, 520f);

            // Mover momentáneamente al centro de observación dentro de la oficina para apreciar toda la animación 3D
            Vector3 savedPos = Vector3.zero;
            Quaternion savedRot = Quaternion.identity;
            Transform playerTf = PlayerTeleportController.I != null ? PlayerTeleportController.I.transform : GameObject.Find("Player")?.transform;
            if (playerTf != null)
            {
                savedPos = playerTf.position;
                savedRot = playerTf.rotation;
            }

            HideMenu();

            if (PlayerTeleportController.I != null)
            {
                PlayerTeleportController.I.TeleportTo(new Vector3(0.0f, 0f, 0.90f), Quaternion.Euler(0f, 172f, 0f));
            }

            if (UIManager.I != null)
            {
                UIManager.I.ShowNotification(
                    "DEMO VISUAL 3D",
                    "Observa cómo viaja el virus entre las 3 PC y luego el escudo protector (8s).",
                    NotificationType.GeneralInfo,
                    AndesTypographyTheme.AccentCyan);
            }

            if (InfectionSpreadVisualizer.I != null)
            {
                InfectionSpreadVisualizer.I.PlayPreviewDemoSequence();
            }

            yield return new WaitForSecondsRealtime(12.8f);

            if (PlayerTeleportController.I != null && playerTf != null)
            {
                PlayerTeleportController.I.TeleportTo(savedPos, savedRot);
            }

            ShowMenu();
            SwitchTab(MenuTab.AjustesYFx);
            _previewFxCoroutine = null;
        }

        public void SelectMode(SimMode mode)
        {
            PlayUiSound(modeSwitchSound, (mode == SimMode.Ataque) ? 440f : 660f);

            if (GameManager.I != null)
            {
                GameManager.I.SetMode(mode);
            }

            if (AudioManager.I != null)
            {
                AudioManager.I.PlayNarration(mode == SimMode.Ataque ? "Voice_02_ModoAtaqueElegido" : "Voice_03_ModoDefensaElegido", true, 1.5f);
            }

            UpdateModeDisplay();
        }

        private void OnExternalModeChanged(SimMode newMode)
        {
            UpdateModeDisplay();
        }

        private void UpdateModeDisplay()
        {
            SimMode currentMode = (GameManager.I != null) ? GameManager.I.mode : SimMode.Ataque;

            if (currentMode == SimMode.Ataque)
            {
                if (txtModoSeleccionadoBadge != null)
                {
                    txtModoSeleccionadoBadge.text = "MODO ACTIVO · ATAQUE LAZARUS";
                    txtModoSeleccionadoBadge.color = AttackRed;
                }
                if (txtModoDescripcion != null)
                {
                    txtModoDescripcion.text =
                        "1. Revisa el correo en PC 1   ·   2. Detona el enlace y observa el virus 3D\n" +
                        "3. Recoge el Post-It y Log SIEM   ·   4. Identifica al responsable";
                }
                if (imgModoAtaqueBorder != null) imgModoAtaqueBorder.color = AttackRed;
                if (imgModoDefensaBorder != null) imgModoDefensaBorder.color = new Color(0.16f, 0.28f, 0.42f, 0.75f);
            }
            else
            {
                if (txtModoSeleccionadoBadge != null)
                {
                    txtModoSeleccionadoBadge.text = "MODO ACTIVO · DEFENSA SOC";
                    txtModoSeleccionadoBadge.color = DefenseGreen;
                }
                if (txtModoDescripcion != null)
                {
                    txtModoDescripcion.text =
                        "1. Marca las 3 señales en PC 1   ·   2. Aísla el correo en Sandbox\n" +
                        "3. Guarda Post-It y SIEM   ·   4. Activa MFA, Red y Escudo SOC";
                }
                if (imgModoAtaqueBorder != null) imgModoAtaqueBorder.color = new Color(0.16f, 0.28f, 0.42f, 0.75f);
                if (imgModoDefensaBorder != null) imgModoDefensaBorder.color = DefenseGreen;
            }
        }

        public void TogglePasosView()
        {
            SetPasosViewVisible(!_showingPasosView);
        }

        public void SetPasosViewVisible(bool showPasos)
        {
            _showingPasosView = showPasos;
            if (panelVistaModos != null) panelVistaModos.SetActive(!showPasos);
            if (panelVistaPasos != null) panelVistaPasos.SetActive(showPasos);
            if (txtBtnTogglePasosLabel != null)
            {
                txtBtnTogglePasosLabel.text = showPasos ? "[ VER MODOS ]" : "[ ? COMO JUGAR ]";
            }
            if (showPasos && AudioManager.I != null)
            {
                AudioManager.I.PlayNarration("Voice_04_GuiaPasosMenu", true, 2.0f);
            }
        }

        public void OpenCreditsModal()
        {
            var modal = CreditsAndEthicsModalUI.I;
            if (modal == null)
            {
                modal = FindFirstObjectByType<CreditsAndEthicsModalUI>(FindObjectsInactive.Include);
            }
            if (modal != null)
            {
                modal.OpenModal();
            }
        }

        public void SetDwellTime(float seconds)
        {
            _currentDwellTime = seconds;
            PlayUiSound(tabSwitchSound, 1000f);

            GazeInteractable[] interactables = FindObjectsByType<GazeInteractable>(FindObjectsSortMode.None);
            for (int i = 0; i < interactables.Length; i++)
            {
                if (interactables[i] != null)
                {
                    interactables[i].DwellTime = _currentDwellTime;
                }
            }

            UpdateDwellDisplay();
            Debug.Log($"{LogPrefix} Dwell Time ajustado a {_currentDwellTime:F1}s para {interactables.Length} interactuables.");
        }

        private void UpdateDwellDisplay()
        {
            if (txtDwellActualBadge != null)
            {
                txtDwellActualBadge.text = $"TIEMPO DE MIRADA: <b>{_currentDwellTime:F1}s</b>";
            }
        }

        public void ToggleAudio()
        {
            _audioMuted = !_audioMuted;
            AudioListener.volume = _audioMuted ? 0f : 1f;

            if (txtAudioStatus != null)
            {
                txtAudioStatus.text = _audioMuted ? "AUDIO: SILENCIADO" : "AUDIO: ACTIVO 100%";
            }

            PlayUiSound(tabSwitchSound, _audioMuted ? 300f : 800f);
        }

        public void IniciarSimulacion()
        {
            if (_isStarting) return;
            if (_previewFxCoroutine != null)
            {
                StopCoroutine(_previewFxCoroutine);
                _previewFxCoroutine = null;
            }
            StartCoroutine(IniciarSimulacionRoutine(null));
        }

        public void IniciarSimulacionToPoint(string teleportObjectName)
        {
            if (_isStarting) return;
            if (_previewFxCoroutine != null)
            {
                StopCoroutine(_previewFxCoroutine);
                _previewFxCoroutine = null;
            }
            StartCoroutine(IniciarSimulacionRoutine(teleportObjectName));
        }

        private IEnumerator IniciarSimulacionRoutine(string overrideTeleportName)
        {
            _isStarting = true;
            Debug.Log($"{LogPrefix} <color=#00F2FE><b>Iniciando simulacion forense...</b></color>");

            if (startSimWarpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(startSimWarpSound);
            }
            else if (AudioManager.I != null)
            {
                AudioManager.I.PlayGazeClick();
            }
            else
            {
                PlayUiSound(null, 1200f);
            }

            if (AudioManager.I != null)
            {
                AudioManager.I.PlayMusicTrack(MusicTheme.CyberInvestigation, 1.5f);
            }

            if (menuCanvasGroup != null)
            {
                float t = 0f;
                while (t < 0.30f)
                {
                    t += Time.deltaTime;
                    menuCanvasGroup.alpha = Mathf.Lerp(1f, 0.10f, t / 0.30f);
                    yield return null;
                }
            }

            if (GameManager.I != null)
            {
                GameManager.I.StartSimulationTimer();
            }

            TeleportPoint targetPoint = null;
            if (!string.IsNullOrEmpty(overrideTeleportName))
            {
                GameObject overrideObj = GameObject.Find(overrideTeleportName);
                if (overrideObj != null) targetPoint = overrideObj.GetComponent<TeleportPoint>();
            }
            if (targetPoint == null)
            {
                ResolveTeleportDestination();
                targetPoint = teleportDestination;
            }

            if (PlayerTeleportController.I != null)
            {
                if (targetPoint != null)
                {
                    PlayerTeleportController.I.TeleportToPoint(targetPoint);
                }
                else
                {
                    PlayerTeleportController.I.TeleportTo(new Vector3(1.40f, 0f, -2.50f), Quaternion.identity);
                }
            }

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.SetStep(ScenarioStep.Step2_Oficina3D);
            }
            if (AudioManager.I != null)
            {
                AudioManager.I.PlayNarration("Voice_05_InicioOficina", true, 2.0f);
            }

            yield return new WaitForSeconds(0.25f);
            HideMenu();
            _isStarting = false;
        }

        public void HideMenu()
        {
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 0f;
                menuCanvasGroup.interactable = false;
                menuCanvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        public void ShowMenu()
        {
            _isStarting = false;
            gameObject.SetActive(true);
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 1f;
                menuCanvasGroup.interactable = true;
                menuCanvasGroup.blocksRaycasts = true;
            }

            HolographicPanelEntrance entrance = GetComponentInChildren<HolographicPanelEntrance>(true);
            entrance?.PlayEntrance();

            SetPasosViewVisible(false);
            UpdateTabVisuals(MenuTab.MisionYModos);
            UpdateModeDisplay();
            UpdateDwellDisplay();
            UpdateLightingDisplay(SocLightingController.I != null ? SocLightingController.I.ActivePreset : SocLightingPreset.OficinaClara);

            if (AudioManager.I != null)
            {
                AudioManager.I.PlayNarration("Voice_01_Bienvenida", true, 3.0f);
            }
        }

        private void ResolveTeleportDestination()
        {
            if (teleportDestination != null) return;

            TeleportPoint[] points = FindObjectsByType<TeleportPoint>(FindObjectsSortMode.None);
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].gameObject.name.IndexOf("01", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    points[i].PointName.IndexOf("Carla", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    teleportDestination = points[i];
                    break;
                }
            }
        }

        private void PlayUiSound(AudioClip clip, float proceduralFreq = 600f)
        {
            if (_audioMuted || audioSource == null) return;

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
            else
            {
                AudioClip synth = CreateToneClip($"UiTone_{proceduralFreq}", proceduralFreq, 0.08f);
                audioSource.PlayOneShot(synth, 0.35f);
            }
        }

        private AudioClip CreateToneClip(string name, float freq, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (float)i / samples;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
