using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Audio;
using AndesSim.Core;
using AndesSim.Data;
using AndesSim.UI;

namespace AndesSim.VR
{
    /// <summary>
    /// Controlador central del Menú de Inicio VR Holográfico 3D de "ANDES-SIM: BANCO ANDINO - OPERACIÓN LAZARUS 2015".
    /// Ubicado en el vestíbulo de entrada (Lobby) frente al punto de spawn del jugador.
    /// 
    /// Características Principales:
    /// 1. Terminal 3D Holográfico interactivo en World Space.
    /// 2. Navegación fluida por 5 Pestañas:
    ///    - Tab 1: Misión & Modos (Ataque 2015 vs Defensa SOC + Botón directo 'INICIAR SIMULACIÓN').
    ///    - Tab 2: Inteligencia & 4 Sospechosos (Samuel Torres, Hugo Benavides, Carla Mendoza, Renzo Aliaga).
    ///    - Tab 3: Guía de Controles & Tutorial VR (Editor PC y Visor Android Cardboard).
    ///    - Tab 4: Marco Ético-Legal & Créditos (Leyes peruanas 30096 y 29733 + Equipo 1).
    ///    - Tab 5: Ajustes & Accesibilidad (Dwell time 1.2s / 1.8s / 2.5s y volumen de audio).
    /// 3. Interacción 100% VR mediante Gaze Dwell (permanencia de mirada) y clic directo con la retícula central.
    /// 4. Transición inmersiva: teletransporta al jugador al Puesto 01 (Carla Mendoza) al iniciar.
    /// </summary>
    public class VrStartMenuController : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - VrStartMenu]";

        public static VrStartMenuController I { get; private set; }

        public enum MenuTab
        {
            MisionYModos = 0,
            Sospechosos = 1,
            TutorialVR = 2,
            EticaYLeyes = 3,
            Ajustes = 4
        }

        [Header("--- Paneles de Pestañas ---")]
        [SerializeField] private GameObject[] tabPanels; // Longitud 5
        [SerializeField] private Button[] tabButtons;     // Longitud 5
        [SerializeField] private Image[] tabButtonBgs;   // Fondos de botones para iluminar la pestaña activa
        [SerializeField] private TextMeshProUGUI[] tabButtonLabels;

        [Header("--- Tab 1: Misión & Modos ---")]
        [SerializeField] private Button btnModoAtaque;
        [SerializeField] private Button btnModoDefensa;
        [SerializeField] private Image imgModoAtaqueBorder;
        [SerializeField] private Image imgModoDefensaBorder;
        [SerializeField] private TextMeshProUGUI txtModoSeleccionadoBadge;
        [SerializeField] private TextMeshProUGUI txtModoDescripcion;
        [SerializeField] private Button btnIniciarSimulacion;

        [Header("--- Flujo Rápido y Accesos Directos (Cardboard UX) ---")]
        [SerializeField] private Button btnTogglePasos;
        [SerializeField] private TextMeshProUGUI txtBtnTogglePasosLabel;
        [SerializeField] private GameObject panelVistaModos;
        [SerializeField] private GameObject panelVistaPasos;
        [SerializeField] private Button btnAbrirCreditos;

        [Header("--- Tab 2: Sospechosos ---")]
        [SerializeField] private Button[] suspectCardsButtons;
        [SerializeField] private GameObject[] suspectDetailPanels;

        [Header("--- Tab 5: Ajustes & Accesibilidad ---")]
        [SerializeField] private Button btnDwellRapido;      // 1.2s
        [SerializeField] private Button btnDwellEstandar;    // 1.8s
        [SerializeField] private Button btnDwellAccesible;   // 2.5s
        [SerializeField] private TextMeshProUGUI txtDwellActualBadge;
        [SerializeField] private Button btnToggleAudio;
        [SerializeField] private TextMeshProUGUI txtAudioStatus;

        [Header("--- Audio y Efectos ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip tabSwitchSound;
        [SerializeField] private AudioClip modeSwitchSound;
        [SerializeField] private AudioClip startSimWarpSound;

        [Header("--- Teletransporte y Destino ---")]
        [SerializeField] private TeleportPoint teleportDestination; // Carla Mendoza (TeleportPoint_01)
        [SerializeField] private CanvasGroup menuCanvasGroup;

        // Propiedades públicas para inyección desde el Scene Assembler
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

        public Button BtnDwellRapido { get => btnDwellRapido; set => btnDwellRapido = value; }
        public Button BtnDwellEstandar { get => btnDwellEstandar; set => btnDwellEstandar = value; }
        public Button BtnDwellAccesible { get => btnDwellAccesible; set => btnDwellAccesible = value; }
        public TextMeshProUGUI TxtDwellActualBadge { get => txtDwellActualBadge; set => txtDwellActualBadge = value; }
        public Button BtnToggleAudio { get => btnToggleAudio; set => btnToggleAudio = value; }
        public TextMeshProUGUI TxtAudioStatus { get => txtAudioStatus; set => txtAudioStatus = value; }
        public CanvasGroup MenuCanvasGroup { get => menuCanvasGroup; set => menuCanvasGroup = value; }
        public TeleportPoint TeleportDestination { get => teleportDestination; set => teleportDestination = value; }

        // Colores temáticos
        private static readonly Color TabActiveColor = new Color(0f, 0.95f, 1f, 0.95f);       // Cyan neón #00f2fe
        private static readonly Color TabInactiveColor = new Color(0.10f, 0.16f, 0.25f, 0.85f); // Azul oscuro
        private static readonly Color TabActiveText = new Color(0.04f, 0.08f, 0.14f, 1f);
        private static readonly Color TabInactiveText = new Color(0.75f, 0.88f, 0.98f, 1f);

        private static readonly Color AttackRed = new Color(1f, 0.35f, 0.31f, 1f);   // #ff5a4e
        private static readonly Color DefenseGreen = new Color(0.22f, 0.95f, 0.54f, 1f); // #38f28a

        private MenuTab _currentTab = MenuTab.MisionYModos;
        private float _currentDwellTime = 1.8f;
        private bool _audioMuted = false;
        private bool _isStarting = false;

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
            SetupButtonListeners();
            UpdateTabVisuals(MenuTab.MisionYModos);
            UpdateModeDisplay();
            UpdateDwellDisplay();

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += OnExternalModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= OnExternalModeChanged;
            }
        }

        private void SetupButtonListeners()
        {
            // Pestañas
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

            // Selección de Modo
            if (btnModoAtaque != null) btnModoAtaque.onClick.AddListener(() => SelectMode(SimMode.Ataque));
            if (btnModoDefensa != null) btnModoDefensa.onClick.AddListener(() => SelectMode(SimMode.Defensa));

            // Iniciar Simulación
            if (btnIniciarSimulacion != null) btnIniciarSimulacion.onClick.AddListener(IniciarSimulacion);

            // Ajustes Dwell Time
            if (btnDwellRapido != null) btnDwellRapido.onClick.AddListener(() => SetDwellTime(1.2f));
            if (btnDwellEstandar != null) btnDwellEstandar.onClick.AddListener(() => SetDwellTime(1.8f));
            if (btnDwellAccesible != null) btnDwellAccesible.onClick.AddListener(() => SetDwellTime(2.5f));

            // Audio Toggle
            if (btnToggleAudio != null) btnToggleAudio.onClick.AddListener(ToggleAudio);

            if (btnTogglePasos != null) btnTogglePasos.onClick.AddListener(TogglePasosView);
            if (btnAbrirCreditos != null) btnAbrirCreditos.onClick.AddListener(OpenCreditsModal);

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
                if (tabPanels[i] != null) tabPanels[i].SetActive(isActive);

                if (tabButtonBgs != null && i < tabButtonBgs.Length && tabButtonBgs[i] != null)
                {
                    tabButtonBgs[i].color = isActive ? TabActiveColor : TabInactiveColor;
                }

                if (tabButtonLabels != null && i < tabButtonLabels.Length && tabButtonLabels[i] != null)
                {
                    tabButtonLabels[i].color = isActive ? TabActiveText : TabInactiveText;
                    tabButtonLabels[i].fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
                }
            }
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

        private bool _showingPasosView;

        private void UpdateModeDisplay()
        {
            SimMode currentMode = (GameManager.I != null) ? GameManager.I.mode : SimMode.Ataque;

            if (currentMode == SimMode.Ataque)
            {
                if (txtModoSeleccionadoBadge != null)
                {
                    txtModoSeleccionadoBadge.text = "MODO ACTIVO: ATAQUE";
                    txtModoSeleccionadoBadge.color = AttackRed;
                }
                if (txtModoDescripcion != null)
                {
                    txtModoDescripcion.text =
                        "1. Revisa el correo en PC 1\n" +
                        "2. Abre el enlace y mira la infeccion\n" +
                        "3. Ubica al responsable en el tablero";
                }
                if (imgModoAtaqueBorder != null) imgModoAtaqueBorder.color = AttackRed;
                if (imgModoDefensaBorder != null) imgModoDefensaBorder.color = new Color(0.18f, 0.30f, 0.44f, 0.70f);
            }
            else
            {
                if (txtModoSeleccionadoBadge != null)
                {
                    txtModoSeleccionadoBadge.text = "MODO ACTIVO: DEFENSA";
                    txtModoSeleccionadoBadge.color = DefenseGreen;
                }
                if (txtModoDescripcion != null)
                {
                    txtModoDescripcion.text =
                        "1. Marca las 3 pistas del correo (PC 1)\n" +
                        "2. Reporta, revisa SIEM (PC 3) y Post-It\n" +
                        "3. Bloquea, activa MFA y evalua en tablero";
                }
                if (imgModoAtaqueBorder != null) imgModoAtaqueBorder.color = new Color(0.18f, 0.30f, 0.44f, 0.70f);
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
                txtBtnTogglePasosLabel.text = showPasos ? "[ VER MODOS ]" : "[ ? COMO FUNCIONA ]";
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

            // Propagar a todos los GazeInteractables en la escena
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
                txtDwellActualBadge.text = $"TIEMPO DE MIRADA ACTUAL: <b>{_currentDwellTime:F1}s</b>";
            }
        }

        public void ToggleAudio()
        {
            _audioMuted = !_audioMuted;
            AudioListener.volume = _audioMuted ? 0f : 1f;

            if (txtAudioStatus != null)
            {
                txtAudioStatus.text = _audioMuted ? "AUDIO: [SILENCIADO]" : "AUDIO: [ACTIVO 100%]";
            }

            PlayUiSound(tabSwitchSound, _audioMuted ? 300f : 800f);
        }

        public void IniciarSimulacion()
        {
            if (_isStarting) return;
            StartCoroutine(IniciarSimulacionRoutine());
        }

        private IEnumerator IniciarSimulacionRoutine()
        {
            _isStarting = true;
            Debug.Log($"{LogPrefix} <color=#00F2FE><b>Iniciando simulacion forense...</b></color>");

            // 1. Sonido de inmersión y warp
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

            // Conmutar banda sonora a Investigación Forense
            if (AudioManager.I != null)
            {
                AudioManager.I.PlayMusicTrack(MusicTheme.CyberInvestigation, 1.5f);
            }

            // 2. Efecto visual de desvanecimiento suave de la terminal
            if (menuCanvasGroup != null)
            {
                float t = 0f;
                while (t < 0.35f)
                {
                    t += Time.deltaTime;
                    menuCanvasGroup.alpha = Mathf.Lerp(1f, 0.15f, t / 0.35f);
                    yield return null;
                }
            }

            // 3. Iniciar cronómetro de 3 minutos en GameManager
            if (GameManager.I != null)
            {
                GameManager.I.StartSimulationTimer();
            }

            // 4. Teletransportar al jugador a TeleportPoint_01 (Puesto de Carla Mendoza)
            ResolveTeleportDestination();
            if (PlayerTeleportController.I != null)
            {
                if (teleportDestination != null)
                {
                    PlayerTeleportController.I.TeleportToPoint(teleportDestination);
                }
                else
                {
                    PlayerTeleportController.I.TeleportTo(new Vector3(1.40f, 0f, -2.50f), Quaternion.identity);
                }
            }

            // 5. Avanzar flujo del escenario a Oficina 3D y narrar primer puesto
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.SetStep(ScenarioStep.Step2_Oficina3D);
            }
            if (AudioManager.I != null)
            {
                AudioManager.I.PlayNarration("Voice_05_InicioOficina", true, 2.0f);
            }

            yield return new WaitForSeconds(0.3f);

            // Ocultar terminal completamente al entrar a la oficina para no obstruir la vista ni verse desde atrás
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
            gameObject.SetActive(true);
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 1f;
                menuCanvasGroup.interactable = true;
                menuCanvasGroup.blocksRaycasts = true;
            }
            SetPasosViewVisible(false);
            UpdateModeDisplay();
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
                // Tono procedural sutil para retroalimentación auditiva
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
