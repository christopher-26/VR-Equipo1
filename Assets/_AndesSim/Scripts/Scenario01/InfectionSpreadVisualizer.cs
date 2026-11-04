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
    /// Visualizador cinemático en tiempo real de la propagación del malware e infección en red (~8 segundos).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// 
    /// Línea de tiempo de la cinemática en tiempo real:
    /// • Segundo 0–2: Monitor_01 (Carla Mendoza) descarga 'Rasel_Ahlam_CV_2015.scr.zip' y la pantalla se tiñe de alerta roja
    ///   mostrando la instalación silenciosa del backdoor de Lazarus (svchost_upd.exe).
    /// • Segundo 2–5: Líneas de energía roja (LineRenderer animados con pulsos entre los escritorios) viajan desde Monitor_01
    ///   hacia Monitor_02 y Monitor_03, representando que al menos 3 computadoras intentan abrir el archivo y que la red plana
    ///   permite abrir una puerta trasera hacia el resto del BANCO ANDINO.
    /// • Segundo 5–8: Un letrero holográfico sobre la oficina muestra:
    ///   "29 ENE 2015: PUERTA TRASERA ABIERTA EN LA RED DEL BANCO ANDINO. El grupo Lazarus inicia 1 año de espionaje silencioso."
    /// • Segundo 8: Invoca GameManager.I.EndScenario() para desplegar el balance forense del Modo Ataque.
    /// </summary>
    public class InfectionSpreadVisualizer : MonoBehaviour
    {
        public static InfectionSpreadVisualizer I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - InfectionVisualizer]";

        public const string HologramMessage = "29 ENE 2015: PUERTA TRASERA ABIERTA EN LA RED DEL BANCO ANDINO. El grupo Lazarus inicia 1 año de espionaje silencioso.";

        private static readonly Color InfectionRedColor = new Color(1.0f, 0.22f, 0.20f, 1.0f); // #ff3834
        private static readonly Color BeamPulseGlow = new Color(1.0f, 0.35f, 0.31f, 0.95f);

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
        [SerializeField] private int curveResolution = 16;

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
        private bool _isPlaying = false;
        private bool _hasPlayed = false;
        private Coroutine _cinematicCoroutine;

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
        }

        private void Start()
        {
            AutoResolveReferences();
            InitializeLineRenderers();
            InitializeHolographicSign();

            // Suscribirse al cambio de paso del escenario
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged += HandleStepChanged;
            }
        }

        private void OnDestroy()
        {
            if (I == this) I = null;

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged -= HandleStepChanged;
            }
        }

        /// <summary>
        /// Escucha la transición de pasos desde ScenarioFlowController.
        /// </summary>
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

        /// <summary>
        /// Localiza automáticamente los monitores y puntos de los escritorios si no fueron cableados en el Inspector.
        /// </summary>
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

            // Fallback de posiciones si no se encuentran en la escena
            if (monitor01Point == null)
            {
                GameObject dummy1 = new GameObject("Anchor_Monitor01_Fallback");
                dummy1.transform.position = new Vector3(-3.0f, 1.1f, 2.1f);
                monitor01Point = dummy1.transform;
            }
            if (monitor02Point == null)
            {
                GameObject dummy2 = new GameObject("Anchor_Monitor02_Fallback");
                dummy2.transform.position = new Vector3(0.0f, 1.1f, 2.1f);
                monitor02Point = dummy2.transform;
            }
            if (monitor03Point == null)
            {
                GameObject dummy3 = new GameObject("Anchor_Monitor03_Fallback");
                dummy3.transform.position = new Vector3(3.0f, 1.1f, 2.1f);
                monitor03Point = dummy3.transform;
            }
        }

        /// <summary>
        /// Inicializa o construye proceduralmente los LineRenderer de red plana.
        /// </summary>
        private void InitializeLineRenderers()
        {
            if (energyLineMaterial == null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
                if (unlitShader == null) unlitShader = Shader.Find("Standard");

                energyLineMaterial = new Material(unlitShader);
                energyLineMaterial.name = "Mat_Procedural_InfectionBeam";
                energyLineMaterial.color = InfectionRedColor;
            }

            if (lineDesk1ToDesk2 == null)
            {
                GameObject lineObj1 = new GameObject("Line_RedPlana_Desk1_Desk2");
                lineObj1.transform.SetParent(transform);
                lineDesk1ToDesk2 = lineObj1.AddComponent<LineRenderer>();
                ConfigureLineRenderer(lineDesk1ToDesk2);
            }
            else
            {
                ConfigureLineRenderer(lineDesk1ToDesk2);
            }

            if (lineDesk1ToDesk3 == null)
            {
                GameObject lineObj2 = new GameObject("Line_RedPlana_Desk1_Desk3");
                lineObj2.transform.SetParent(transform);
                lineDesk1ToDesk3 = lineObj2.AddComponent<LineRenderer>();
                ConfigureLineRenderer(lineDesk1ToDesk3);
            }
            else
            {
                ConfigureLineRenderer(lineDesk1ToDesk3);
            }

            lineDesk1ToDesk2.enabled = false;
            lineDesk1ToDesk3.enabled = false;
        }

        private void ConfigureLineRenderer(LineRenderer lr)
        {
            lr.material = energyLineMaterial;
            lr.startWidth = defaultLineWidth;
            lr.endWidth = defaultLineWidth;
            lr.useWorldSpace = true;
            lr.startColor = BeamPulseGlow;
            lr.endColor = InfectionRedColor;
            lr.positionCount = curveResolution;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }

        /// <summary>
        /// Inicializa o construye proceduralmente el letrero holográfico superior en World Space.
        /// </summary>
        private void InitializeHolographicSign()
        {
            if (holographicSignPanel == null)
            {
                // Construir Canvas flotante sobre la oficina central
                GameObject canvasObj = new GameObject("Canvas_LetreroHolografico_Infeccion");
                canvasObj.transform.SetParent(transform);
                canvasObj.transform.position = new Vector3(0f, 2.3f, 1.8f);
                canvasObj.transform.rotation = Quaternion.Euler(15f, 0f, 0f); // Inclinación ergonómica hacia el jugador

                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(500f, 140f);
                canvasRect.localScale = new Vector3(0.005f, 0.005f, 0.005f);

                canvasObj.AddComponent<CanvasScaler>();
                holographicSignCanvasGroup = canvasObj.AddComponent<CanvasGroup>();

                // Fondo traslúcido
                GameObject bgObj = new GameObject("Panel_Fondo");
                bgObj.transform.SetParent(canvasObj.transform, false);
                RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                Image bgImage = bgObj.AddComponent<Image>();
                bgImage.color = new Color(0.07f, 0.02f, 0.02f, 0.94f);
                holographicSignBorder = bgImage;

                // Título de Alerta
                GameObject titleObj = new GameObject("Txt_AlertaTitulo");
                titleObj.transform.SetParent(bgObj.transform, false);
                RectTransform titleRect = titleObj.AddComponent<RectTransform>();
                titleRect.anchoredPosition = new Vector2(0f, 42f);
                titleRect.sizeDelta = new Vector2(470f, 32f);
                holographicSignTitle = titleObj.AddComponent<TextMeshProUGUI>();
                holographicSignTitle.text = "<color=#FF5A4E><b>[ALERTA DE SEGURIDAD CRITICA - RED PLANA COMPROMETIDA]</b></color>";
                holographicSignTitle.fontSize = 17;
                holographicSignTitle.alignment = TextAlignmentOptions.Center;

                // Cuerpo oficial requerido por la especificación
                GameObject bodyObj = new GameObject("Txt_MensajeOficial");
                bodyObj.transform.SetParent(bgObj.transform, false);
                RectTransform bodyRect = bodyObj.AddComponent<RectTransform>();
                bodyRect.anchoredPosition = new Vector2(0f, -8f);
                bodyRect.sizeDelta = new Vector2(460f, 60f);
                holographicSignBody = bodyObj.AddComponent<TextMeshProUGUI>();
                holographicSignBody.text =
                    $"<b><color=#FFFFFF>{HologramMessage}</color></b>\n" +
                    $"<size=75%><color=#FFC66D>Topología plana sin segmentación interna facilitó la intrusión masiva.</color></size>";
                holographicSignBody.fontSize = 15;
                holographicSignBody.alignment = TextAlignmentOptions.Center;

                holographicSignPanel = canvasObj;
            }

            if (holographicSignCanvasGroup == null && holographicSignPanel != null)
            {
                holographicSignCanvasGroup = holographicSignPanel.GetComponent<CanvasGroup>();
                if (holographicSignCanvasGroup == null)
                {
                    holographicSignCanvasGroup = holographicSignPanel.AddComponent<CanvasGroup>();
                }
            }

            if (holographicSignPanel != null)
            {
                holographicSignPanel.SetActive(false);
            }
        }

        #region Cinemática en Tiempo Real (~8 Segundos)

        /// <summary>
        /// Inicia la secuencia cinemática en tiempo real de propagación del ataque de 2016.
        /// </summary>
        [ContextMenu("Test Infection Sequence (Play Mode)")]
        public void PlayInfectionSequence()
        {
            if (_isPlaying || _hasPlayed) return;
            _isPlaying = true;
            _hasPlayed = true;

            Debug.LogWarning($"{LogPrefix} <b>INICIANDO CINEMÁTICA EN TIEMPO REAL (~8s) DE PROPAGACIÓN DE MALWARE LAZARUS.</b>");

            if (_cinematicCoroutine != null)
            {
                StopCoroutine(_cinematicCoroutine);
            }
            _cinematicCoroutine = StartCoroutine(RealTimeInfectionTimelineCoroutine());
        }

        /// <summary>
        /// Corrutina orquestadora de los 8 segundos de la cinemática oficial.
        /// </summary>
        private IEnumerator RealTimeInfectionTimelineCoroutine()
        {
            Vector3 pos1 = monitor01Point.position + Vector3.up * 0.15f;
            Vector3 pos2 = monitor02Point.position + Vector3.up * 0.15f;
            Vector3 pos3 = monitor03Point.position + Vector3.up * 0.15f;

            // =========================================================================
            // SEGUNDO 0–2: Monitor_01 (Carla Mendoza) descarga el CV y tiñe la pantalla en rojo
            // =========================================================================
            Debug.Log($"{LogPrefix} <color=#FF5A4E>[Seg 0–2]</color> Monitor_01 descarga 'Rasel_Ahlam_CV_2015.scr.zip'. Backdoor svchost_upd.exe en ejecución silenciosa.");
            PlaySound(downloadExploitClip, GenerateDownloadExploitTone());

            if (monitor01Controller != null)
            {
                monitor01Controller.SetState(InteractiveMonitorController.MonitorState.InfeccionActiva);
            }

            // Activar pulso de emergencia en la iluminación cibernética ambiental
            if (AndesSim.Environment.AmbientCyberGridEffect.I != null)
            {
                AndesSim.Environment.AmbientCyberGridEffect.I.TriggerEmergencyAttackPulse();
            }

            float timer = 0f;
            while (timer < 2.0f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // =========================================================================
            // SEGUNDO 2–5: Líneas de energía roja viajan desde Monitor_01 hacia Monitor_02 y Monitor_03
            // =========================================================================
            Debug.Log($"{LogPrefix} <color=#FF5A4E>[Seg 2–5]</color> Red plana comprometida: haces de energía roja propagándose hacia Monitor_02 y Monitor_03.");
            PlaySound(networkLaserPulseClip, GenerateLaserPulseTone());

            lineDesk1ToDesk2.enabled = true;
            lineDesk1ToDesk3.enabled = true;

            bool monitor02Infected = false;
            bool monitor03Infected = false;

            timer = 0f;
            float durationPhase2 = 3.0f; // 2s a 5s

            while (timer < durationPhase2)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / durationPhase2);

                // 1. Progresión del rayo 1: Monitor_01 -> Monitor_02 (más rápido: se completa al 50% de la fase, ~1.5s)
                float t1 = Mathf.Clamp01(progress / 0.5f);
                UpdateCurvedBeam(lineDesk1ToDesk2, pos1, Vector3.Lerp(pos1, pos2, t1), 0.25f * Mathf.Sin(t1 * Mathf.PI));

                if (t1 >= 1.0f && !monitor02Infected)
                {
                    monitor02Infected = true;
                    Debug.Log($"{LogPrefix} <color=#FF5A4E>[09:21:10]</color> HTTP GET: PC-PAGOS-02 intenta abrir Rasel_Ahlam_CV_2015.scr.zip (Infección lateral).");
                    PlaySound(networkLaserPulseClip, GenerateLaserPulseTone());
                    if (monitor02Controller != null)
                    {
                        monitor02Controller.SetState(InteractiveMonitorController.MonitorState.InfeccionActiva);
                    }
                }

                // 2. Progresión del rayo 2: Monitor_01 -> Monitor_03 (recorre 6 metros, se completa al 90% de la fase)
                float t2 = Mathf.Clamp01(progress / 0.9f);
                UpdateCurvedBeam(lineDesk1ToDesk3, pos1, Vector3.Lerp(pos1, pos3, t2), 0.40f * Mathf.Sin(t2 * Mathf.PI));

                if (t2 >= 1.0f && !monitor03Infected)
                {
                    monitor03Infected = true;
                    Debug.LogWarning($"{LogPrefix} <color=#FF5A4E>[09:25:03]</color> HTTP GET: PC-PAGOS-09 abre Rasel_Ahlam_CV_2015.scr.zip");
                    Debug.LogWarning($"{LogPrefix} <color=#FF5A4E>[09:25:09] ALERTA SILENCIADA:</color> Puerta trasera activa hacia nodo SWIFT.");
                    PlaySound(networkLaserPulseClip, GenerateLaserPulseTone());
                    if (monitor03Controller != null)
                    {
                        monitor03Controller.SetState(InteractiveMonitorController.MonitorState.InfeccionActiva);
                    }
                }

                // Pulsación visual de grosor en las líneas de energía activa
                float pulse = defaultLineWidth * (1.0f + 0.35f * Mathf.Sin(Time.time * 15f));
                lineDesk1ToDesk2.startWidth = pulse;
                lineDesk1ToDesk2.endWidth = pulse;
                lineDesk1ToDesk3.startWidth = pulse * 1.15f;
                lineDesk1ToDesk3.endWidth = pulse * 1.15f;

                yield return null;
            }

            // =========================================================================
            // SEGUNDO 5–8: Letrero holográfico superior con el texto oficial del incidente
            // =========================================================================
            Debug.Log($"{LogPrefix} <color=#FF5A4E>[Seg 5–8]</color> Desplegando letrero holográfico: '{HologramMessage}'");
            PlaySound(backdoorBreachStingClip, GenerateDramaticBreachSting());

            if (holographicSignPanel != null)
            {
                holographicSignPanel.SetActive(true);
            }

            timer = 0f;
            float durationPhase3 = 3.0f; // 5s a 8s

            while (timer < durationPhase3)
            {
                timer += Time.deltaTime;
                float alphaProgress = Mathf.Clamp01(timer / 0.6f);

                if (holographicSignCanvasGroup != null)
                {
                    holographicSignCanvasGroup.alpha = alphaProgress;
                }

                // Efecto de pulso en el contorno del letrero
                if (holographicSignBorder != null)
                {
                    float glow = 0.85f + 0.15f * Mathf.Sin(Time.time * 8f);
                    holographicSignBorder.color = new Color(0.08f * glow, 0.02f, 0.02f, 0.95f);
                }

                // Continuar manteniendo las líneas activas pulsando
                float pulse = defaultLineWidth * (1.0f + 0.25f * Mathf.Sin(Time.time * 12f));
                lineDesk1ToDesk2.startWidth = pulse;
                lineDesk1ToDesk2.endWidth = pulse;
                lineDesk1ToDesk3.startWidth = pulse;
                lineDesk1ToDesk3.endWidth = pulse;

                yield return null;
            }

            // =========================================================================
            // SEGUNDO 8: Concluir la cinemática e invocar GameManager.I.EndScenario()
            // =========================================================================
            Debug.Log($"{LogPrefix} <color=#38F28A><b>Cinemática de 8s concluida. Invocando GameManager.I.EndScenario().</b></color>");

            _isPlaying = false;

            // Desplegar el resumen del Modo Ataque
            if (GameManager.I != null)
            {
                GameManager.I.EndScenario();
            }

            // Actualizar paso de flujo
            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.SetStep(ScenarioStep.Step6_Resultado);
            }
        }

        /// <summary>
        /// Traza una curva parabólica en el LineRenderer entre dos puntos espaciales.
        /// </summary>
        private void UpdateCurvedBeam(LineRenderer line, Vector3 start, Vector3 end, float arcHeight)
        {
            if (line == null) return;

            line.positionCount = curveResolution;
            for (int i = 0; i < curveResolution; i++)
            {
                float t = (float)i / (curveResolution - 1);
                Vector3 pos = Vector3.Lerp(start, end, t);
                // Arco parabólico en Y
                pos.y += 4f * arcHeight * t * (1f - t);
                line.SetPosition(i, pos);
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
                // Chirp descendente con glitch digital
                float freq = Mathf.Lerp(1200f, 320f, t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                if ((int)(t * 40f) % 2 == 0) wave *= 0.5f; // Ruido modulado
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
                // Acorde menor disonante y bajo profundo (C2=65.4Hz, Eb2=77.8Hz, Gb2=92.5Hz)
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
