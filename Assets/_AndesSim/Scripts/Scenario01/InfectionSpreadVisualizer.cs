using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Environment;
using AndesSim.UI;

namespace AndesSim.Scenario01
{
    /// <summary>
    /// Visualizador cinemático en tiempo real de la propagación del malware e infección en red (~8 segundos)
    /// y de la barrera holográfica de contención en Modo Defensa.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// Optimizado para OpenGLES3 móvil (objetos pre-agrupados sin asignaciones por cuadro y uso de MaterialPropertyBlock).
    /// </summary>
    public class InfectionSpreadVisualizer : MonoBehaviour
    {
        public static InfectionSpreadVisualizer I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - InfectionVisualizer]";

        public const string HologramMessage = "29 ENE 2015: PUERTA TRASERA ABIERTA EN LA RED DEL BANCO ANDINO. El grupo Lazarus inicia 1 año de espionaje silencioso.";

        private static readonly Color InfectionRedColor = new Color(1.0f, 0.22f, 0.20f, 1.0f);    // #ff3834
        private static readonly Color BeamPulseGlow = new Color(1.0f, 0.42f, 0.24f, 0.96f);        // Naranja-Rojo neón
        private static readonly Color ExfilGoldRed = new Color(1.0f, 0.72f, 0.22f, 0.98f);         // Exfiltración SWIFT
        private static readonly Color DefenseEmerald = new Color(0.22f, 0.95f, 0.54f, 0.95f);      // #38f28a
        private static readonly Color DefenseCyan = new Color(0.00f, 0.90f, 1.00f, 0.92f);         // #00e5ff

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private const int PacketsPerRoute = 6;

        [Header("--- Monitores de la Oficina (Puntos de Propagación) ---")]
        [Tooltip("Controlador de Monitor_01 (Carla Mendoza - PC-PAGOS-01).")]
        [SerializeField] private InteractiveMonitorController monitor01Controller;

        [Tooltip("Controlador de Monitor_02 (Analista Pagos - PC-PAGOS-02).")]
        [SerializeField] private InteractiveMonitorController monitor02Controller;

        [Tooltip("Controlador de Monitor_03 (Terminal SIEM / PC-PAGOS-09).")]
        [SerializeField] private InteractiveMonitorController monitor03Controller;

        [Tooltip("Puntos de anclaje para los haces de energía roja.")]
        [SerializeField] private Transform monitor01Point;
        [SerializeField] private Transform monitor02Point;
        [SerializeField] private Transform monitor03Point;

        [Header("--- Líneas de Energía de Red Plana (LineRenderer) ---")]
        [Tooltip("Línea de energía roja entre Monitor 01 y Monitor 02.")]
        [SerializeField] private LineRenderer lineDesk1ToDesk2;

        [Tooltip("Línea de energía roja entre Monitor 01 y Monitor 03.")]
        [SerializeField] private LineRenderer lineDesk1ToDesk3;

        [SerializeField] private Material energyLineMaterial;
        [SerializeField] private float defaultLineWidth = 0.045f;
        [SerializeField] private int curveResolution = 18;

        [Header("--- Letrero Holográfico Superior ---")]
        [Tooltip("Contenedor raíz del letrero flotante sobre la oficina.")]
        [SerializeField] private GameObject holographicSignPanel;
        [SerializeField] private CanvasGroup holographicSignCanvasGroup;
        [SerializeField] private TextMeshProUGUI holographicSignTitle;
        [SerializeField] private TextMeshProUGUI holographicSignBody;
        [SerializeField] private Image holographicSignBorder;

        [Header("--- Efectos Sonoros Cinemáticos ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip downloadExploitClip;
        [SerializeField] private AudioClip networkLaserPulseClip;
        [SerializeField] private AudioClip backdoorBreachStingClip;

        // Estado interno
        private bool _isPlaying;
        private bool _hasPlayed;
        private Coroutine _cinematicCoroutine;

        // Rig holográfico pre-construido (0 GC en Update/corrutina)
        private Transform _fxRigRoot;
        private LineRenderer _lineDesk2ToC2;
        private Transform _c2NodeRoot;
        private Transform[] _packetsRoute12;
        private Transform[] _packetsRoute13;
        private Transform[] _packetsRoute2C2;
        private GameObject[] _stationNodes = new GameObject[3];
        private HolographicOrbitRing[] _stationOuterRings = new HolographicOrbitRing[3];
        private HolographicOrbitRing[] _stationInnerRings = new HolographicOrbitRing[3];
        private Renderer[] _stationCoreRenderers = new Renderer[3];
        private GameObject[] _shockwaveRings = new GameObject[3];
        private Renderer[] _shockwaveRenderers = new Renderer[3];
        private GameObject _defenseCanopyRing;
        private Material _sharedHoloMat;
        private MaterialPropertyBlock _mpb;
        private HolographicDataBeacon[] _sceneBeacons;

        public bool IsPlaying => _isPlaying;
        public bool HasPlayed => _hasPlayed;

        private void Awake()
        {
            if (I == null)
            {
                I = this;
            }
            else if (I != this)
            {
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
                    audioSource.spatialBlend = 0.35f;
                }
            }

            _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            AutoResolveReferences();
            InitializeLineRenderers();
            InitializeHolographicSign();
            InitializeHolographicFxRig();
            _sceneBeacons = FindObjectsByType<HolographicDataBeacon>(FindObjectsSortMode.None);

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged += HandleStepChanged;
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
                if (GameManager.I.mode == SimMode.Defensa)
                {
                    ShowDefenseContainmentSign();
                }
            }
        }

        private void OnDestroy()
        {
            if (I == this) I = null;

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged -= HandleStepChanged;
            }

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
            }
        }

        private void HandleModeChanged(SimMode newMode)
        {
            if (newMode == SimMode.Defensa)
            {
                ShowDefenseContainmentSign();
            }
            else if (!_isPlaying)
            {
                ResetToCleanState();
            }
        }

        private void HandleStepChanged(ScenarioStep newStep, StepGuideInfo info)
        {
            if (newStep == ScenarioStep.Step6_Resultado)
            {
                SimMode mode = GameManager.I != null ? GameManager.I.mode : SimMode.Ataque;
                if (mode == SimMode.Ataque)
                {
                    PlayInfectionSequence();
                }
            }
        }

        #region Resolución e Inicialización de Activos 3D

        private void AutoResolveReferences()
        {
            if (monitor01Controller == null || monitor02Controller == null || monitor03Controller == null)
            {
                InteractiveMonitorController[] monitors = FindObjectsByType<InteractiveMonitorController>(FindObjectsSortMode.None);
                foreach (InteractiveMonitorController mon in monitors)
                {
                    string searchKey = (mon.MonitorId + " " + mon.name).ToLowerInvariant();
                    if (searchKey.Contains("01") || searchKey.Contains("carla") || searchKey.Contains("puesto 1"))
                    {
                        if (monitor01Controller == null) monitor01Controller = mon;
                    }
                    else if (searchKey.Contains("02") || searchKey.Contains("analista") || searchKey.Contains("puesto 2") || searchKey.Contains("comercio"))
                    {
                        if (monitor02Controller == null) monitor02Controller = mon;
                    }
                    else if (searchKey.Contains("03") || searchKey.Contains("siem") || searchKey.Contains("registros") || searchKey.Contains("puesto 3"))
                    {
                        if (monitor03Controller == null) monitor03Controller = mon;
                    }
                }
            }

            if (monitor01Point == null && monitor01Controller != null) monitor01Point = monitor01Controller.transform;
            if (monitor02Point == null && monitor02Controller != null) monitor02Point = monitor02Controller.transform;
            if (monitor03Point == null && monitor03Controller != null) monitor03Point = monitor03Controller.transform;

            if (monitor01Point == null) monitor01Point = CreateFallbackAnchor("Anchor_Monitor01_Fallback", new Vector3(1.37f, 1.15f, -2.07f));
            if (monitor02Point == null) monitor02Point = CreateFallbackAnchor("Anchor_Monitor02_Fallback", new Vector3(5.34f, 1.15f, -1.93f));
            if (monitor03Point == null) monitor03Point = CreateFallbackAnchor("Anchor_Monitor03_Fallback", new Vector3(-6.37f, 1.15f, -4.69f));
        }

        private Transform CreateFallbackAnchor(string objName, Vector3 pos)
        {
            GameObject dummy = new GameObject(objName);
            dummy.transform.SetParent(transform, false);
            dummy.transform.position = pos;
            return dummy.transform;
        }

        private void InitializeLineRenderers()
        {
            if (energyLineMaterial == null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                                  ?? Shader.Find("Sprites/Default")
                                  ?? Shader.Find("Standard");
                energyLineMaterial = new Material(unlitShader);
                energyLineMaterial.name = "Mat_Procedural_InfectionBeam";
                energyLineMaterial.color = InfectionRedColor;
            }

            if (lineDesk1ToDesk2 == null)
            {
                GameObject lineObj1 = new GameObject("Line_RedPlana_Desk1_Desk2");
                lineObj1.transform.SetParent(transform, false);
                lineDesk1ToDesk2 = lineObj1.AddComponent<LineRenderer>();
            }
            ConfigureLineRenderer(lineDesk1ToDesk2, BeamPulseGlow, InfectionRedColor);

            if (lineDesk1ToDesk3 == null)
            {
                GameObject lineObj2 = new GameObject("Line_RedPlana_Desk1_Desk3");
                lineObj2.transform.SetParent(transform, false);
                lineDesk1ToDesk3 = lineObj2.AddComponent<LineRenderer>();
            }
            ConfigureLineRenderer(lineDesk1ToDesk3, BeamPulseGlow, InfectionRedColor);

            if (_lineDesk2ToC2 == null)
            {
                GameObject lineObj3 = new GameObject("Line_Exfiltracion_C2_Lazarus");
                lineObj3.transform.SetParent(transform, false);
                _lineDesk2ToC2 = lineObj3.AddComponent<LineRenderer>();
            }
            ConfigureLineRenderer(_lineDesk2ToC2, ExfilGoldRed, InfectionRedColor);

            lineDesk1ToDesk2.enabled = false;
            lineDesk1ToDesk3.enabled = false;
            _lineDesk2ToC2.enabled = false;
        }

        private void ConfigureLineRenderer(LineRenderer lr, Color startCol, Color endCol)
        {
            lr.sharedMaterial = energyLineMaterial;
            lr.startWidth = defaultLineWidth;
            lr.endWidth = defaultLineWidth * 0.85f;
            lr.useWorldSpace = true;
            lr.startColor = startCol;
            lr.endColor = endCol;
            lr.positionCount = curveResolution;
            lr.numCapVertices = 4;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }

        private void InitializeHolographicSign()
        {
            Vector3 centerPos = ComputeOptimalBannerPosition();

            if (holographicSignPanel == null)
            {
                GameObject canvasObj = new GameObject("Canvas_LetreroHolografico_Infeccion");
                canvasObj.transform.SetParent(transform, false);
                canvasObj.transform.position = centerPos;
                canvasObj.transform.rotation = Quaternion.Euler(10f, 180f, 0f);

                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(560f, 154f);
                canvasRect.localScale = new Vector3(0.0052f, 0.0052f, 0.0052f);

                canvasObj.AddComponent<CanvasScaler>();
                holographicSignCanvasGroup = canvasObj.AddComponent<CanvasGroup>();

                GameObject bgObj = new GameObject("Panel_Fondo");
                bgObj.transform.SetParent(canvasObj.transform, false);
                RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                Image bgImage = bgObj.AddComponent<Image>();
                bgImage.color = new Color(0.07f, 0.02f, 0.03f, 0.95f);
                holographicSignBorder = bgImage;

                // Barra decorativa luminosa superior
                GameObject accentObj = new GameObject("TopAccentStrip");
                accentObj.transform.SetParent(bgObj.transform, false);
                RectTransform accentRect = accentObj.AddComponent<RectTransform>();
                accentRect.anchorMin = new Vector2(0f, 1f);
                accentRect.anchorMax = new Vector2(1f, 1f);
                accentRect.anchoredPosition = new Vector2(0f, -3f);
                accentRect.sizeDelta = new Vector2(0f, 6f);
                Image accentImg = accentObj.AddComponent<Image>();
                accentImg.color = InfectionRedColor;

                GameObject titleObj = new GameObject("Txt_AlertaTitulo");
                titleObj.transform.SetParent(bgObj.transform, false);
                RectTransform titleRect = titleObj.AddComponent<RectTransform>();
                titleRect.anchoredPosition = new Vector2(0f, 44f);
                titleRect.sizeDelta = new Vector2(520f, 34f);
                holographicSignTitle = titleObj.AddComponent<TextMeshProUGUI>();
                holographicSignTitle.text = "ALERTA CRITICA · RED PLANA COMPROMETIDA";
                AndesTypographyTheme.StyleDisplayTitle(holographicSignTitle, AndesTypographyTheme.AccentRed, 18f);

                GameObject bodyObj = new GameObject("Txt_MensajeOficial");
                bodyObj.transform.SetParent(bgObj.transform, false);
                RectTransform bodyRect = bodyObj.AddComponent<RectTransform>();
                bodyRect.anchoredPosition = new Vector2(0f, -12f);
                bodyRect.sizeDelta = new Vector2(510f, 76f);
                holographicSignBody = bodyObj.AddComponent<TextMeshProUGUI>();
                holographicSignBody.text =
                    $"<b>{HologramMessage}</b>\n" +
                    "<size=78%><color=#FFC66D>Topología sin segmentación VLAN permitió la intrusión masiva.</color></size>";
                AndesTypographyTheme.StyleBodyCentered(holographicSignBody, Color.white, 15.5f, 8f);

                holographicSignPanel = canvasObj;
            }
            else
            {
                holographicSignPanel.transform.position = centerPos;
                AndesTypographyTheme.ApplyTypographyToTree(holographicSignPanel.transform, true, true);
            }

            if (holographicSignCanvasGroup == null && holographicSignPanel != null)
            {
                holographicSignCanvasGroup = holographicSignPanel.GetComponent<CanvasGroup>();
                if (holographicSignCanvasGroup == null) holographicSignCanvasGroup = holographicSignPanel.AddComponent<CanvasGroup>();
            }

            if (holographicSignPanel != null)
            {
                HolographicPanelEntrance entrance = holographicSignPanel.GetComponent<HolographicPanelEntrance>()
                    ?? holographicSignPanel.AddComponent<HolographicPanelEntrance>();
                entrance.Configure(0.32f, 0.84f, 0.06f, holographicSignCanvasGroup);

                HolographicFloatMotion floatMotion = holographicSignPanel.GetComponent<HolographicFloatMotion>()
                    ?? holographicSignPanel.AddComponent<HolographicFloatMotion>();
                floatMotion.Configure(0.040f, 0.85f, new Vector3(0.9f, 1.5f, 0.5f), 0f, 0f, true);

                holographicSignPanel.SetActive(false);
            }
        }

        private Vector3 ComputeOptimalBannerPosition()
        {
            Vector3 p1 = monitor01Point != null ? monitor01Point.position : new Vector3(1.37f, 1.15f, -2.07f);
            Vector3 p2 = monitor02Point != null ? monitor02Point.position : new Vector3(5.34f, 1.15f, -1.93f);
            Vector3 p3 = monitor03Point != null ? monitor03Point.position : new Vector3(-6.37f, 1.15f, -4.69f);
            Vector3 mid = (p1 + p2 + p3) / 3f;
            // Centrado dentro de la nave principal a altura cómoda de lectura sin tocar el techo (Y <= 2.30m)
            return new Vector3(Mathf.Clamp(mid.x, -2.5f, 2.5f), 2.18f, Mathf.Clamp(mid.z + 0.8f, -2.6f, 0.4f));
        }

        private void InitializeHolographicFxRig()
        {
            if (_fxRigRoot != null) return;

            GameObject rigObj = new GameObject("Infection_Holo_FX_Rig");
            rigObj.transform.SetParent(transform, false);
            _fxRigRoot = rigObj.transform;

            Shader holoShader = Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Sprites/Default")
                             ?? Shader.Find("Standard");
            _sharedHoloMat = new Material(holoShader);
            _sharedHoloMat.name = "Mat_Virus_Holo_Shared";
            _sharedHoloMat.color = InfectionRedColor;

            // 1. Paquetes animados 3D a lo largo de las 3 rutas
            _packetsRoute12 = CreatePacketPool("MalwarePacket_1to2", PacketsPerRoute, 0.055f, BeamPulseGlow);
            _packetsRoute13 = CreatePacketPool("MalwarePacket_1to3", PacketsPerRoute, 0.055f, InfectionRedColor);
            _packetsRoute2C2 = CreatePacketPool("ExfilPacket_2toC2", PacketsPerRoute, 0.050f, ExfilGoldRed);

            // 2. Nodo C2 Flotante de Lazarus (185.130.44.82)
            _c2NodeRoot = BuildC2FloatingNode();

            // 3. Nodos de Virus / Escudo y Ondas de Choque sobre cada estación
            Transform[] anchors = { monitor01Point, monitor02Point, monitor03Point };
            for (int i = 0; i < 3; i++)
            {
                BuildStationCluster(i, anchors[i]);
            }

            // 4. Aro protector superior de contención para Modo Defensa
            _defenseCanopyRing = BuildFlatRing("Canopy_DefenseShieldRing", _fxRigRoot, 1.15f, 0.032f, DefenseEmerald);
            _defenseCanopyRing.transform.position = ComputeOptimalBannerPosition() + new Vector3(0f, -0.38f, 0f);
            HolographicOrbitRing canopySpin = _defenseCanopyRing.AddComponent<HolographicOrbitRing>();
            canopySpin.Configure(new Vector3(0f, 38f, 0f), _defenseCanopyRing.GetComponent<Renderer>(), DefenseEmerald, 1.0f, 2.3f, 1.8f);
            _defenseCanopyRing.SetActive(false);
        }

        private Transform[] CreatePacketPool(string prefix, int count, float size, Color tint)
        {
            Transform[] pool = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                GameObject pod = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pod.name = $"{prefix}_{i}";
                pod.transform.SetParent(_fxRigRoot, false);
                pod.transform.localScale = new Vector3(size, size, size * 1.45f);
                DestroySafeCollider(pod);

                Renderer r = pod.GetComponent<Renderer>();
                r.sharedMaterial = _sharedHoloMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ApplyColorToRenderer(r, tint, 2.4f);

                pod.SetActive(false);
                pool[i] = pod.transform;
            }
            return pool;
        }

        private Transform BuildC2FloatingNode()
        {
            Vector3 c2Pos = ComputeOptimalBannerPosition() + new Vector3(0.4f, 0.36f, -1.1f);
            c2Pos.y = Mathf.Clamp(c2Pos.y, 2.20f, 2.46f);

            GameObject root = new GameObject("Nodo_C2_Lazarus_185_130");
            root.transform.SetParent(_fxRigRoot, false);
            root.transform.position = c2Pos;

            HolographicFloatMotion fl = root.AddComponent<HolographicFloatMotion>();
            fl.Configure(0.035f, 1.15f, new Vector3(1.5f, 2.0f, 1.5f), 22f, 0.04f, true);

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "C2_Core";
            core.transform.SetParent(root.transform, false);
            core.transform.localRotation = Quaternion.Euler(35f, 45f, 20f);
            core.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            DestroySafeCollider(core);
            Renderer coreRend = core.GetComponent<Renderer>();
            coreRend.sharedMaterial = _sharedHoloMat;
            ApplyColorToRenderer(coreRend, InfectionRedColor, 2.6f);

            GameObject r1 = BuildFlatRing("C2_RingA", root.transform, 0.32f, 0.018f, BeamPulseGlow);
            r1.transform.localRotation = Quaternion.Euler(28f, 0f, 18f);
            r1.AddComponent<HolographicOrbitRing>().Configure(new Vector3(45f, 75f, 20f), r1.GetComponent<Renderer>(), BeamPulseGlow, 1.2f, 2.8f, 3.0f);

            GameObject r2 = BuildFlatRing("C2_RingB", root.transform, 0.42f, 0.014f, ExfilGoldRed);
            r2.transform.localRotation = Quaternion.Euler(-35f, 20f, -22f);
            r2.AddComponent<HolographicOrbitRing>().Configure(new Vector3(-35f, -65f, 30f), r2.GetComponent<Renderer>(), ExfilGoldRed, 1.0f, 2.5f, 2.4f);

            // Mini etiqueta Canvas World Space sobre el nodo C2
            GameObject lblCanvasObj = new GameObject("Canvas_C2_Label");
            lblCanvasObj.transform.SetParent(root.transform, false);
            lblCanvasObj.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            lblCanvasObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Canvas c = lblCanvasObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            RectTransform cr = lblCanvasObj.GetComponent<RectTransform>();
            cr.sizeDelta = new Vector2(280f, 54f);
            cr.localScale = Vector3.one * 0.0028f;
            Image bg = lblCanvasObj.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.02f, 0.03f, 0.92f);

            GameObject tObj = new GameObject("Txt_C2");
            tObj.transform.SetParent(lblCanvasObj.transform, false);
            RectTransform tr = tObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmp = tObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "SERVIDOR C2 LAZARUS\n185.130.44.82 : 443";
            AndesTypographyTheme.StyleBadge(tmp, ExfilGoldRed, 14.5f);

            root.SetActive(false);
            return root.transform;
        }

        private void BuildStationCluster(int index, Transform anchor)
        {
            Vector3 pos = (anchor != null ? anchor.position : Vector3.zero) + Vector3.up * 0.56f;

            GameObject cluster = new GameObject($"Holo_StationCluster_0{index + 1}");
            cluster.transform.SetParent(_fxRigRoot, false);
            cluster.transform.position = pos;
            HolographicFloatMotion fl = cluster.AddComponent<HolographicFloatMotion>();
            fl.Configure(0.032f, 1.25f, new Vector3(1.2f, 2.2f, 1.0f), 0f, 0.03f, true);

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "CoreSphere";
            core.transform.SetParent(cluster.transform, false);
            core.transform.localScale = Vector3.one * 0.11f;
            DestroySafeCollider(core);
            Renderer coreR = core.GetComponent<Renderer>();
            coreR.sharedMaterial = _sharedHoloMat;
            _stationCoreRenderers[index] = coreR;

            GameObject outer = BuildFlatRing("OuterOrbitRing", cluster.transform, 0.25f, 0.015f, InfectionRedColor);
            outer.transform.localRotation = Quaternion.Euler(24f, 0f, 15f);
            HolographicOrbitRing outerSpin = outer.AddComponent<HolographicOrbitRing>();
            _stationOuterRings[index] = outerSpin;

            GameObject inner = BuildFlatRing("InnerOrbitRing", cluster.transform, 0.18f, 0.013f, BeamPulseGlow);
            inner.transform.localRotation = Quaternion.Euler(-38f, 30f, -15f);
            HolographicOrbitRing innerSpin = inner.AddComponent<HolographicOrbitRing>();
            _stationInnerRings[index] = innerSpin;

            cluster.SetActive(false);
            _stationNodes[index] = cluster;

            // Onda de choque expansiva sobre la mesa (Y = pos - 0.35m)
            GameObject shock = BuildFlatRing($"Shockwave_0{index + 1}", _fxRigRoot, 0.50f, 0.022f, InfectionRedColor);
            shock.transform.position = (anchor != null ? anchor.position : Vector3.zero) + Vector3.up * 0.08f;
            shock.SetActive(false);
            _shockwaveRings[index] = shock;
            _shockwaveRenderers[index] = shock.GetComponent<Renderer>();
        }

        private GameObject BuildFlatRing(string name, Transform parent, float radius, float thickness, Color color)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = name;
            ring.transform.SetParent(parent, false);
            ring.transform.localScale = new Vector3(radius * 2f, thickness * 0.5f, radius * 2f);
            DestroySafeCollider(ring);
            Renderer r = ring.GetComponent<Renderer>();
            r.sharedMaterial = _sharedHoloMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ApplyColorToRenderer(r, color, 2.2f);
            return ring;
        }

        private static void DestroySafeCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        private void ApplyColorToRenderer(Renderer r, Color c, float glowMult)
        {
            if (r == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            _mpb.SetColor(EmissionColorId, c * glowMult);
            r.SetPropertyBlock(_mpb);
        }

        #endregion

        #region Cinemática en Tiempo Real (~8 Segundos) y Vista Previa

        [ContextMenu("Test Infection Sequence (Play Mode)")]
        public void PlayInfectionSequence()
        {
            if (_isPlaying || _hasPlayed) return;
            _isPlaying = true;
            _hasPlayed = true;

            Debug.Log($"{LogPrefix} <b>INICIANDO CINEMÁTICA EN TIEMPO REAL (~8s) DE PROPAGACIÓN DE MALWARE LAZARUS.</b>");

            if (_cinematicCoroutine != null) StopCoroutine(_cinematicCoroutine);
            _cinematicCoroutine = StartCoroutine(RealTimeInfectionTimelineCoroutine(endScenarioOnComplete: true));
        }

        /// <summary>
        /// Ejecuta una demostración visual segura de propagación y contención desde el Menú sin finalizar el escenario.
        /// </summary>
        public void PlayPreviewDemoSequence()
        {
            if (_cinematicCoroutine != null) StopCoroutine(_cinematicCoroutine);
            ResetToCleanState();
            _isPlaying = true;
            _cinematicCoroutine = StartCoroutine(PreviewDemoCoroutine());
        }

        private IEnumerator PreviewDemoCoroutine()
        {
            yield return StartCoroutine(RealTimeInfectionTimelineCoroutine(endScenarioOnComplete: false));
            yield return new WaitForSecondsRealtime(2.0f);
            ShowDefenseContainmentSign();
            yield return new WaitForSecondsRealtime(3.0f);
            ResetToCleanState();
        }

        private IEnumerator RealTimeInfectionTimelineCoroutine(bool endScenarioOnComplete)
        {
            Vector3 pos1 = (monitor01Point != null ? monitor01Point.position : new Vector3(1.37f, 1.15f, -2.07f)) + Vector3.up * 0.22f;
            Vector3 pos2 = (monitor02Point != null ? monitor02Point.position : new Vector3(5.34f, 1.15f, -1.93f)) + Vector3.up * 0.22f;
            Vector3 pos3 = (monitor03Point != null ? monitor03Point.position : new Vector3(-6.37f, 1.15f, -4.69f)) + Vector3.up * 0.22f;
            Vector3 posC2 = _c2NodeRoot != null ? _c2NodeRoot.position : ComputeOptimalBannerPosition() + Vector3.up * 0.35f;

            // =========================================================================
            // FASE 1 (0 - 2s): Monitor_01 comprometido + anillo viral + onda de choque
            // =========================================================================
            Debug.Log($"{LogPrefix} <color=#FF5A4E>[Seg 0–2]</color> Monitor_01 ejecuta 'Rasel_Ahlam_CV_2015.scr.zip'. Backdoor svchost_upd.exe activo.");
            PlaySound(downloadExploitClip, GenerateDownloadExploitTone());

            if (monitor01Controller != null)
            {
                monitor01Controller.SetState(InteractiveMonitorController.MonitorState.InfeccionActiva);
            }

            SetStationClusterMode(0, isAttack: true);
            StartCoroutine(AnimateShockwaveCoroutine(0));
            SyncSceneBeacons(HoloBeaconState.AlertRed);

            if (AmbientCyberGridEffect.I != null)
            {
                AmbientCyberGridEffect.I.TriggerEmergencyAttackPulse();
            }

            float timer = 0f;
            while (timer < 2.0f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // =========================================================================
            // FASE 2 (2 - 5s): Haces curvos, paquetes 3D en vuelo e infección lateral + C2
            // =========================================================================
            Debug.Log($"{LogPrefix} <color=#FF5A4E>[Seg 2–5]</color> Red plana comprometida: propagación lateral hacia Monitor_02 (SWIFT) y Monitor_03 (SIEM).");
            PlaySound(networkLaserPulseClip, GenerateLaserPulseTone());

            lineDesk1ToDesk2.enabled = true;
            lineDesk1ToDesk3.enabled = true;
            SetPacketPoolActive(_packetsRoute12, true);
            SetPacketPoolActive(_packetsRoute13, true);

            bool monitor02Infected = false;
            bool monitor03Infected = false;
            bool c2UplinkStarted = false;

            timer = 0f;
            const float durationPhase2 = 3.0f;

            while (timer < durationPhase2)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / durationPhase2);

                // Haz 1 -> 2
                float t1 = Mathf.Clamp01(progress / 0.48f);
                Vector3 end12 = Vector3.Lerp(pos1, pos2, t1);
                float arc12 = 0.42f;
                UpdateCurvedBeam(lineDesk1ToDesk2, pos1, end12, arc12 * t1);
                AnimatePacketsAlongArc(_packetsRoute12, pos1, pos2, arc12, t1, 1.35f);

                if (t1 >= 1.0f && !monitor02Infected)
                {
                    monitor02Infected = true;
                    Debug.Log($"{LogPrefix} <color=#FF5A4E>[09:21:10]</color> PC-PAGOS-02 comprometido (Salto lateral hacia módulo SWIFT).");
                    PlaySound(networkLaserPulseClip, GenerateLaserPulseTone());
                    if (monitor02Controller != null) monitor02Controller.SetState(InteractiveMonitorController.MonitorState.InfeccionActiva);
                    SetStationClusterMode(1, isAttack: true);
                    StartCoroutine(AnimateShockwaveCoroutine(1));

                    // Encender nodo C2 flotante y enlace de exfiltración
                    c2UplinkStarted = true;
                    if (_c2NodeRoot != null) _c2NodeRoot.gameObject.SetActive(true);
                    if (_lineDesk2ToC2 != null) _lineDesk2ToC2.enabled = true;
                    SetPacketPoolActive(_packetsRoute2C2, true);
                }

                // Haz 1 -> 3
                float t2 = Mathf.Clamp01(progress / 0.88f);
                Vector3 end13 = Vector3.Lerp(pos1, pos3, t2);
                float arc13 = 0.65f;
                UpdateCurvedBeam(lineDesk1ToDesk3, pos1, end13, arc13 * t2);
                AnimatePacketsAlongArc(_packetsRoute13, pos1, pos3, arc13, t2, 1.10f);

                if (t2 >= 1.0f && !monitor03Infected)
                {
                    monitor03Infected = true;
                    Debug.Log($"{LogPrefix} <color=#FF5A4E>[09:25:03]</color> PC-PAGOS-09 (SIEM) alcanzado · Puerta trasera hacia C2 185.130.44.82.");
                    PlaySound(networkLaserPulseClip, GenerateLaserPulseTone());
                    if (monitor03Controller != null) monitor03Controller.SetState(InteractiveMonitorController.MonitorState.InfeccionActiva);
                    SetStationClusterMode(2, isAttack: true);
                    StartCoroutine(AnimateShockwaveCoroutine(2));
                }

                if (c2UplinkStarted && _lineDesk2ToC2 != null)
                {
                    float tC2 = Mathf.Clamp01((progress - 0.48f) / 0.42f);
                    UpdateCurvedBeam(_lineDesk2ToC2, pos2, Vector3.Lerp(pos2, posC2, tC2), 0.35f * tC2);
                    AnimatePacketsAlongArc(_packetsRoute2C2, pos2, posC2, 0.35f, tC2, 1.65f);
                }

                PulseBeams(15f, 0.35f);
                yield return null;
            }

            // =========================================================================
            // FASE 3 (5 - 8s): Letrero holográfico flotante + flujo continuo hacia C2
            // =========================================================================
            Debug.Log($"{LogPrefix} <color=#FF5A4E>[Seg 5–8]</color> Desplegando letrero holográfico de intrusión: '{HologramMessage}'");
            PlaySound(backdoorBreachStingClip, GenerateDramaticBreachSting());

            ConfigureBannerForAttack();

            if (holographicSignPanel != null)
            {
                holographicSignPanel.transform.position = ComputeOptimalBannerPosition();
                holographicSignPanel.SetActive(true);
            }

            timer = 0f;
            const float durationPhase3 = 3.0f;

            while (timer < durationPhase3)
            {
                timer += Time.deltaTime;
                float alphaProgress = Mathf.Clamp01(timer / 0.55f);

                if (holographicSignCanvasGroup != null)
                {
                    holographicSignCanvasGroup.alpha = alphaProgress;
                }

                if (holographicSignBorder != null)
                {
                    float glow = 0.85f + 0.15f * Mathf.Sin(Time.time * 8f);
                    holographicSignBorder.color = new Color(0.11f * glow, 0.02f, 0.03f, 0.95f);
                }

                AnimatePacketsAlongArc(_packetsRoute12, pos1, pos2, 0.42f, 1f, 1.35f);
                AnimatePacketsAlongArc(_packetsRoute13, pos1, pos3, 0.65f, 1f, 1.10f);
                AnimatePacketsAlongArc(_packetsRoute2C2, pos2, posC2, 0.35f, 1f, 1.65f);
                PulseBeams(12f, 0.25f);

                yield return null;
            }

            _isPlaying = false;

            if (endScenarioOnComplete)
            {
                Debug.Log($"{LogPrefix} <color=#38F28A><b>Cinemática de 8s concluida. Invocando GameManager.I.EndScenario().</b></color>");
                if (GameManager.I != null) GameManager.I.EndScenario();
                if (ScenarioFlowController.I != null) ScenarioFlowController.I.SetStep(ScenarioStep.Step6_Resultado);
            }
        }

        private void PulseBeams(float freq, float amp)
        {
            float pulse = defaultLineWidth * (1.0f + amp * Mathf.Sin(Time.time * freq));
            if (lineDesk1ToDesk2 != null) { lineDesk1ToDesk2.startWidth = pulse; lineDesk1ToDesk2.endWidth = pulse * 0.85f; }
            if (lineDesk1ToDesk3 != null) { lineDesk1ToDesk3.startWidth = pulse * 1.12f; lineDesk1ToDesk3.endWidth = pulse * 0.95f; }
            if (_lineDesk2ToC2 != null) { _lineDesk2ToC2.startWidth = pulse * 0.9f; _lineDesk2ToC2.endWidth = pulse * 0.7f; }
        }

        private void UpdateCurvedBeam(LineRenderer line, Vector3 start, Vector3 end, float arcHeight)
        {
            if (line == null) return;
            line.positionCount = curveResolution;
            for (int i = 0; i < curveResolution; i++)
            {
                float t = (float)i / (curveResolution - 1);
                Vector3 pos = Vector3.Lerp(start, end, t);
                pos.y += 4f * arcHeight * t * (1f - t);
                line.SetPosition(i, pos);
            }
        }

        private void AnimatePacketsAlongArc(Transform[] pool, Vector3 start, Vector3 end, float arcHeight, float beamReach, float speed)
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++)
            {
                Transform pkt = pool[i];
                if (pkt == null) continue;

                float phase = ((Time.time * speed) + ((float)i / pool.Length)) % 1f;
                if (phase > beamReach)
                {
                    pkt.gameObject.SetActive(false);
                    continue;
                }

                pkt.gameObject.SetActive(true);
                Vector3 pos = Vector3.Lerp(start, end, phase);
                pos.y += 4f * arcHeight * phase * (1f - phase);

                float nextT = Mathf.Min(1f, phase + 0.03f);
                Vector3 nextPos = Vector3.Lerp(start, end, nextT);
                nextPos.y += 4f * arcHeight * nextT * (1f - nextT);

                pkt.position = pos;
                Vector3 dir = nextPos - pos;
                if (dir.sqrMagnitude > 0.00001f) pkt.rotation = Quaternion.LookRotation(dir);
            }
        }

        private IEnumerator AnimateShockwaveCoroutine(int index)
        {
            if (index < 0 || index >= _shockwaveRings.Length || _shockwaveRings[index] == null) yield break;

            GameObject ring = _shockwaveRings[index];
            Renderer r = _shockwaveRenderers[index];
            ring.SetActive(true);

            const float duration = 0.85f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - u, 3f);
                float diameter = Mathf.Lerp(0.28f, 1.75f, ease);
                ring.transform.localScale = new Vector3(diameter, 0.01f, diameter);

                Color col = Color.Lerp(BeamPulseGlow, new Color(InfectionRedColor.r, InfectionRedColor.g, InfectionRedColor.b, 0f), u);
                ApplyColorToRenderer(r, col, Mathf.Lerp(3.0f, 0.1f, u));
                yield return null;
            }

            ring.SetActive(false);
        }

        private void SetStationClusterMode(int index, bool isAttack)
        {
            if (index < 0 || index >= _stationNodes.Length || _stationNodes[index] == null) return;
            _stationNodes[index].SetActive(true);

            Color primary = isAttack ? InfectionRedColor : DefenseEmerald;
            Color secondary = isAttack ? BeamPulseGlow : DefenseCyan;
            float mult = isAttack ? 2.4f : 1.15f;

            ApplyColorToRenderer(_stationCoreRenderers[index], primary, isAttack ? 2.8f : 1.9f);
            if (_stationOuterRings[index] != null)
            {
                _stationOuterRings[index].Configure(
                    new Vector3(24f * mult, 68f * mult, 16f * mult),
                    _stationOuterRings[index].GetComponent<Renderer>(),
                    primary, 1.2f, 2.8f, 2.6f * mult);
            }
            if (_stationInnerRings[index] != null)
            {
                _stationInnerRings[index].Configure(
                    new Vector3(-32f * mult, -76f * mult, 22f * mult),
                    _stationInnerRings[index].GetComponent<Renderer>(),
                    secondary, 1.0f, 2.4f, 2.1f * mult);
            }
        }

        private void SetPacketPoolActive(Transform[] pool, bool active)
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] != null) pool[i].gameObject.SetActive(active);
            }
        }

        private void ConfigureBannerForAttack()
        {
            if (holographicSignBorder != null)
            {
                holographicSignBorder.color = new Color(0.08f, 0.02f, 0.03f, 0.95f);
            }
            if (holographicSignTitle != null)
            {
                holographicSignTitle.text = "ALERTA CRITICA · RED PLANA COMPROMETIDA";
                AndesTypographyTheme.StyleDisplayTitle(holographicSignTitle, AndesTypographyTheme.AccentRed, 18f);
            }
            if (holographicSignBody != null)
            {
                holographicSignBody.text =
                    $"<b>{HologramMessage}</b>\n" +
                    "<size=78%><color=#FFC66D>Topología sin segmentación VLAN permitió la intrusión masiva.</color></size>";
                AndesTypographyTheme.StyleBodyCentered(holographicSignBody, Color.white, 15.5f, 8f);
            }
        }

        /// <summary>
        /// RF-04: Transforma el letrero superior y la red al modo de Contención Exitosa / Defensa.
        /// Desactiva los haces rojos y enciende halos de protección verde esmeralda y cian sobre cada puesto.
        /// </summary>
        public void ShowDefenseContainmentSign()
        {
            if (_cinematicCoroutine != null)
            {
                StopCoroutine(_cinematicCoroutine);
                _cinematicCoroutine = null;
            }

            _isPlaying = false;

            if (lineDesk1ToDesk2 != null) lineDesk1ToDesk2.enabled = false;
            if (lineDesk1ToDesk3 != null) lineDesk1ToDesk3.enabled = false;
            if (_lineDesk2ToC2 != null) _lineDesk2ToC2.enabled = false;

            SetPacketPoolActive(_packetsRoute12, false);
            SetPacketPoolActive(_packetsRoute13, false);
            SetPacketPoolActive(_packetsRoute2C2, false);

            if (_c2NodeRoot != null) _c2NodeRoot.gameObject.SetActive(false);
            if (_defenseCanopyRing != null) _defenseCanopyRing.SetActive(true);

            for (int i = 0; i < 3; i++)
            {
                SetStationClusterMode(i, isAttack: false);
                if (_shockwaveRings[i] != null) _shockwaveRings[i].SetActive(false);
            }

            SyncSceneBeacons(HoloBeaconState.DefenseEmerald);

            if (holographicSignPanel != null)
            {
                holographicSignPanel.transform.position = ComputeOptimalBannerPosition();
                holographicSignPanel.SetActive(true);
            }

            if (holographicSignCanvasGroup != null)
            {
                holographicSignCanvasGroup.alpha = 1.0f;
            }

            if (holographicSignBorder != null)
            {
                holographicSignBorder.color = new Color(0.02f, 0.09f, 0.05f, 0.95f);
            }

            if (holographicSignTitle != null)
            {
                holographicSignTitle.text = "CONTENCION EXITOSA · ESCUDOS SOC ACTIVOS";
                AndesTypographyTheme.StyleDisplayTitle(holographicSignTitle, DefenseEmerald, 18f);
            }

            if (holographicSignBody != null)
            {
                holographicSignBody.text =
                    "<b>AMENAZA AISLADA EN SANDBOX CUCKOO: INFECCIÓN EVITADA AL 100%.</b>\n" +
                    "<size=78%><color=#A0F5C4>Dropper neutralizado en perímetro · Fondos SWIFT intactos (US$ 0 robados).</color></size>";
                AndesTypographyTheme.StyleBodyCentered(holographicSignBody, Color.white, 15.5f, 8f);
            }

            if (monitor01Controller != null) monitor01Controller.SetState(InteractiveMonitorController.MonitorState.ProtegidoCuarentena);
            if (monitor02Controller != null) monitor02Controller.SetState(InteractiveMonitorController.MonitorState.ProtegidoCuarentena);
            if (monitor03Controller != null) monitor03Controller.SetState(InteractiveMonitorController.MonitorState.ProtegidoCuarentena);

            Debug.Log($"{LogPrefix} <color=#38F28A><b>Letrero superior actualizado a MODO DEFENSA / CONTENCIÓN EXITOSA.</b></color>");
        }

        /// <summary>
        /// Restablece los visualizadores y efectos 3D a su estado inicial limpio.
        /// </summary>
        public void ResetToCleanState()
        {
            if (_cinematicCoroutine != null)
            {
                StopCoroutine(_cinematicCoroutine);
                _cinematicCoroutine = null;
            }

            _isPlaying = false;
            _hasPlayed = false;

            if (lineDesk1ToDesk2 != null) lineDesk1ToDesk2.enabled = false;
            if (lineDesk1ToDesk3 != null) lineDesk1ToDesk3.enabled = false;
            if (_lineDesk2ToC2 != null) _lineDesk2ToC2.enabled = false;

            SetPacketPoolActive(_packetsRoute12, false);
            SetPacketPoolActive(_packetsRoute13, false);
            SetPacketPoolActive(_packetsRoute2C2, false);

            if (_c2NodeRoot != null) _c2NodeRoot.gameObject.SetActive(false);
            if (_defenseCanopyRing != null) _defenseCanopyRing.SetActive(false);

            for (int i = 0; i < 3; i++)
            {
                if (_stationNodes[i] != null) _stationNodes[i].SetActive(false);
                if (_shockwaveRings[i] != null) _shockwaveRings[i].SetActive(false);
            }

            SyncSceneBeacons(HoloBeaconState.NormalCyan);

            if (holographicSignCanvasGroup != null)
            {
                holographicSignCanvasGroup.alpha = 0f;
            }
            if (holographicSignPanel != null)
            {
                holographicSignPanel.SetActive(false);
            }
        }

        private void SyncSceneBeacons(HoloBeaconState state)
        {
            if (_sceneBeacons == null || _sceneBeacons.Length == 0)
            {
                _sceneBeacons = FindObjectsByType<HolographicDataBeacon>(FindObjectsSortMode.None);
            }
            for (int i = 0; i < _sceneBeacons.Length; i++)
            {
                if (_sceneBeacons[i] != null) _sceneBeacons[i].SetVisualState(state);
            }
        }

        #endregion

        #region Síntesis Sonora Procedural Cinemática

        private void PlaySound(AudioClip clip, AudioClip fallbackProcedural)
        {
            AudioClip target = clip != null ? clip : fallbackProcedural;
            if (target != null && audioSource != null)
            {
                audioSource.PlayOneShot(target);
            }
        }

        private AudioClip GenerateDownloadExploitTone()
        {
            int sampleRate = 44100;
            float duration = 1.4f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Clamp01(1f - (t / duration));
                float freq = Mathf.Lerp(1200f, 320f, t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                if ((int)(t * 40f) % 2 == 0) wave *= 0.5f;
                samples[i] = wave * 0.3f * env;
            }

            AudioClip clip = AudioClip.Create("Procedural_DownloadExploit", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateLaserPulseTone()
        {
            int sampleRate = 44100;
            float duration = 0.85f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-3.5f * t);
                float f1 = 440f + 220f * Mathf.Sin(2f * Mathf.PI * 18f * t);
                float wave = Mathf.Sin(2f * Mathf.PI * f1 * t);
                samples[i] = wave * 0.35f * env;
            }

            AudioClip clip = AudioClip.Create("Procedural_LaserPulse", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateDramaticBreachSting()
        {
            int sampleRate = 44100;
            float duration = 2.8f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Clamp01(1f - (t / duration));
                float wave = Mathf.Sin(2f * Mathf.PI * 65.4f * t) * 0.4f
                           + Mathf.Sin(2f * Mathf.PI * 77.8f * t) * 0.3f
                           + Mathf.Sin(2f * Mathf.PI * 92.5f * t) * 0.25f;
                samples[i] = Mathf.Clamp(wave * env, -0.7f, 0.7f);
            }

            AudioClip clip = AudioClip.Create("Procedural_BackdoorBreach", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        #endregion
    }
}
