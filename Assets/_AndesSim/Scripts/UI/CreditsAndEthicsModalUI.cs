using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.VR;

namespace AndesSim.UI
{
    /// <summary>
    /// Modal en World Space de Créditos, Atribución de Recursos, Herramientas de IA y Marco Ético-Legal.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos" (Equipo 1).
    /// - Accesible tanto desde el vestíbulo del Portal_Entrada como desde la pantalla final (EndScenarioPanelUI).
    /// - Muestra:
    ///   1. Nombre oficial del proyecto: ANDES-SIM · Caso BANCO ANDINO - Escenario 1: Oficina de pagos (VR).
    ///   2. Integrantes y los 4 roles del Equipo 1 (Líder/Narrativa, Dev Unity C#, Artista 3D/Audio, Ciberseguridad/QA).
    ///   3. Atribución de recursos libres (Sketchfab/Poly Pizza bajo CC0/CC-BY) y declaración de IA (Gemini/Antigravity y Kling AI).
    ///   4. Declaración ética y legal (Ley 30096 de Delitos Informáticos y Ley 29733 de Protección de Datos Personales).
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public class CreditsAndEthicsModalUI : MonoBehaviour
    {
        public static CreditsAndEthicsModalUI I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - Credits & Ethics]";

        // Paleta oficial Banco Andino
        private const string ColorDarkBg = "#020a06";
        private const string ColorCardBg = "#07150e";
        private const string ColorTextLight = "#d6f2e2";
        private const string ColorGreenCode = "#38f28a";
        private const string ColorAlertRed = "#ff5a4e";
        private const string ColorAmberWarning = "#ffc24a";
        private const string ColorMutedText = "#8fa397";

        [Header("--- Datos del Proyecto ---")]
        [SerializeField] private string projectName = "ANDES-SIM · Caso BANCO ANDINO - Escenario 1: Oficina de pagos (VR)";
        [SerializeField] private string teamLeaderName = "Líder de Proyecto & Narrativa (Scrum Master · Guionista)";
        [SerializeField] private string unityDevName = "Desarrollador Unity C# (Programación · XR Architecture · Build)";
        [SerializeField] private string artistName = "Artista 3D, UI & Sonido (Entorno 3D · Canvas World Space · Audio)";
        [SerializeField] private string cybersecurityQaName = "Analista de Ciberseguridad & QA (Investigación Forense · QA Testing)";

        [Header("--- Referencias UI ---")]
        [SerializeField] private GameObject modalRootPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI teamRolesText;
        [SerializeField] private TextMeshProUGUI attributionAiText;
        [SerializeField] private TextMeshProUGUI legalEthicsText;

        [Header("--- Posicionamiento Dinámico VR ---")]
        [SerializeField] private Transform targetCamera;
        [SerializeField] private float spawnDistanceInFront = 2.1f;
        [SerializeField] private float fadeDuration = 0.25f;

        [Header("--- Efectos Sonoros ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openModalSound;
        [SerializeField] private AudioClip closeModalSound;

        // Estado interno
        private bool _isOpen = false;
        private Coroutine _fadeCoroutine;

        public bool IsOpen => _isOpen;
        public string ProjectName => projectName;

        private void Awake()
        {
            if (I == null)
            {
                I = this;
            }
            else if (I != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureAudioSource();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            // Auto-generar jerarquía si faltan referencias
            if (modalRootPanel == null || titleText == null || closeButton == null)
            {
                BuildModalHierarchy();
            }

            // Ocultar por defecto al iniciar
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (modalRootPanel != null) modalRootPanel.SetActive(false);
            _isOpen = false;
        }

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseModal);
            }

            PopulateTexts();
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f;
        }

        #region Población de Textos y Contenidos Oficiales

        private void PopulateTexts()
        {
            if (titleText != null)
            {
                titleText.text = $"<b><color={ColorGreenCode}>ANDES-SIM</color> · CASO BANCO ANDINO</b>\n" +
                                 $"<size=13><color={ColorTextLight}>Escenario 1: Oficina de Pagos (Simulación VR Inmersiva)</color></size>";
            }

            if (teamRolesText != null)
            {
                teamRolesText.text =
                    $"<b><color={ColorGreenCode}>EQUIPO 1 · ROLES Y RESPONSABILIDADES:</color></b>\n" +
                    $"• <b>Líder de Proyecto y Narrativa:</b> {teamLeaderName}\n" +
                    $"• <b>Desarrollador Unity (C#):</b> {unityDevName}\n" +
                    $"• <b>Artista 3D, UI y Sonido:</b> {artistName}\n" +
                    $"• <b>Analista de Ciberseguridad y QA:</b> {cybersecurityQaName}";
            }

            if (attributionAiText != null)
            {
                attributionAiText.text =
                    $"<b><color={ColorAmberWarning}>ATRIBUCIÓN DE RECURSOS LIBRES & USO DE IA:</color></b>\n" +
                    $"• <b>Recursos Libres 3D:</b> Modelos base optimizados de <i>Sketchfab</i> y <i>Poly Pizza</i> bajo licencias <b>CC0 (Dominio Público)</b> y <b>CC-BY 4.0</b> (Atribución). Texturas procedurales PBR.\n" +
                    $"• <b>Audio Procedural:</b> Síntesis de ondas sinusoidales en tiempo real para clics, confirmaciones y tonos telefónicos DTMF sin dependencias externas propietarias.\n" +
                    $"• <b>Declaración de Herramientas de IA:</b>\n" +
                    $"  - <b>Google Gemini / Antigravity:</b> Asistencia en arquitectura técnica, desarrollo de scripts C# en Unity 6 y soporte para emulación Cardboard XR.\n" +
                    $"  - <b>Kling AI:</b> Generación y asistencia audiovisual para la producción del tráiler cinemático del proyecto.";
            }

            if (legalEthicsText != null)
            {
                legalEthicsText.text =
                    $"<b><color={ColorAlertRed}>DECLARACIÓN ÉTICA & MARCO LEGAL (LEY 30096 Y LEY 29733):</color></b>\n" +
                    $"• <b>Finalidad Estrictamente Académica:</b> Simulador interactivo desarrollado con fines pedagógicos de concientización y defensa frente a amenazas avanzadas persistentes (APT).\n" +
                    $"• <b>Cumplimiento de la Ley N° 30096 (Ley de Delitos Informáticos):</b> Este software no enseña técnicas ofensivas de intrusión ni promueve el acceso no autorizado a sistemas informáticos. Su enfoque exclusivo es la detección temprana de phishing y la remediación perimetral.\n" +
                    $"• <b>Cumplimiento de la Ley N° 29733 (Protección de Datos Personales):</b> No se utilizan ni exponen datos reales. Nombres, correos, dominios y contraseñas (<i>BANCO ANDINO</i>, <i>Rasel Ahlam</i>, <i>Carla Mendoza</i>, <i>career-applicants-bd.com</i>) son <b>ficticios</b> inspirados didácticamente en el caso histórico del Banco Central de Bangladesh (Lazarus Group).";
            }
        }

        #endregion

        #region Generación Procedural de UI en World Space

        private void BuildModalHierarchy()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rootRect = GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1050f, 720f);
            rootRect.localScale = new Vector3(0.0022f, 0.0022f, 0.0022f);

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f;

            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();

            // Fondo general del modal
            GameObject bgObj = CreateUiPanel("Modal_Fondo", transform, Vector2.zero, new Vector2(1050f, 720f), HexToColor(ColorDarkBg, 0.98f));
            AddUiBorder(bgObj, HexToColor(ColorGreenCode, 0.85f), 2.5f);
            modalRootPanel = bgObj;

            // Barra de Cabecera
            GameObject headerObj = CreateUiPanel("Header_Bar", bgObj.transform, new Vector2(0f, 315f), new Vector2(1010f, 70f), HexToColor(ColorCardBg, 0.95f));
            AddUiBorder(headerObj, HexToColor(ColorGreenCode, 0.4f), 1.5f);

            titleText = CreateTmpText("TitleText", headerObj.transform, new Vector2(-60f, 0f), new Vector2(850f, 60f),
                "Cargando título...", 17f, TextAlignmentOptions.Left, HexToColor(ColorTextLight));

            // Botón de Cerrar [X]
            GameObject btnCloseObj = CreateUiPanel("Btn_Cerrar_Modal", headerObj.transform, new Vector2(450f, 0f), new Vector2(80f, 44f), HexToColor(ColorAlertRed, 0.85f));
            AddUiBorder(btnCloseObj, HexToColor(ColorTextLight, 0.6f), 1.5f);
            closeButton = btnCloseObj.AddComponent<Button>();

            CreateTmpText("CloseLabel", btnCloseObj.transform, Vector2.zero, new Vector2(70f, 40f),
                "<b>CERRAR\n[X]</b>", 10.5f, TextAlignmentOptions.Center, HexToColor(ColorTextLight));

            WorldSpaceUiGazeButton gazeClose = btnCloseObj.AddComponent<WorldSpaceUiGazeButton>();
            gazeClose.ConfigureAudio(audioSource, closeModalSound, null);

            // Bloque 1: Equipo 1 y Roles
            GameObject rolesPanel = CreateUiPanel("Panel_Roles", bgObj.transform, new Vector2(0f, 185f), new Vector2(1010f, 150f), HexToColor(ColorCardBg, 0.85f));
            AddUiBorder(rolesPanel, HexToColor(ColorGreenCode, 0.35f), 1f);

            teamRolesText = CreateTmpText("TeamRolesText", rolesPanel.transform, Vector2.zero, new Vector2(980f, 135f),
                "Cargando roles...", 11f, TextAlignmentOptions.TopLeft, HexToColor(ColorTextLight));

            // Bloque 2: Atribución de Recursos Libres y Herramientas de IA
            GameObject aiPanel = CreateUiPanel("Panel_Recursos_IA", bgObj.transform, new Vector2(0f, 10f), new Vector2(1010f, 160f), HexToColor(ColorCardBg, 0.85f));
            AddUiBorder(aiPanel, HexToColor(ColorAmberWarning, 0.45f), 1f);

            attributionAiText = CreateTmpText("AttributionAiText", aiPanel.transform, Vector2.zero, new Vector2(980f, 145f),
                "Cargando atribución...", 10.5f, TextAlignmentOptions.TopLeft, HexToColor(ColorTextLight));

            // Bloque 3: Marco Ético y Legal (Ley 30096 y 29733)
            GameObject ethicsPanel = CreateUiPanel("Panel_Marco_Legal", bgObj.transform, new Vector2(0f, -180f), new Vector2(1010f, 180f), HexToColor(ColorCardBg, 0.85f));
            AddUiBorder(ethicsPanel, HexToColor(ColorAlertRed, 0.45f), 1f);

            legalEthicsText = CreateTmpText("LegalEthicsText", ethicsPanel.transform, Vector2.zero, new Vector2(980f, 165f),
                "Cargando marco legal...", 10.5f, TextAlignmentOptions.TopLeft, HexToColor(ColorTextLight));

            // Botón Inferior de Retorno
            GameObject btnReturnObj = CreateUiPanel("Btn_Volver_Simulacion", bgObj.transform, new Vector2(0f, -315f), new Vector2(380f, 44f), HexToColor(ColorGreenCode, 0.90f));
            AddUiBorder(btnReturnObj, HexToColor(ColorDarkBg, 1f), 2f);
            Button returnBtn = btnReturnObj.AddComponent<Button>();
            returnBtn.onClick.AddListener(CloseModal);

            CreateTmpText("ReturnLabel", btnReturnObj.transform, Vector2.zero, new Vector2(360f, 38f),
                "<b>[<] VOLVER A LA SIMULACIÓN</b>", 13f, TextAlignmentOptions.Center, HexToColor(ColorDarkBg));

            WorldSpaceUiGazeButton gazeReturn = btnReturnObj.AddComponent<WorldSpaceUiGazeButton>();
            gazeReturn.ConfigureAudio(audioSource, closeModalSound, null);
        }

        #endregion

        #region Control de Apertura y Cierre

        /// <summary>
        /// Abre el modal de créditos orientándolo suavemente frente a la cámara del jugador.
        /// </summary>
        public void OpenModal()
        {
            if (_isOpen) return;
            _isOpen = true;

            PositionModalInFrontOfPlayer();

            if (modalRootPanel != null)
            {
                modalRootPanel.SetActive(true);
            }

            PlaySound(openModalSound, GenerateOpenTone());

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, fadeDuration));

            Debug.Log($"{LogPrefix} <color=#38F28A>Modal de Créditos y Marco Ético abierto en World Space.</color>");
        }

        /// <summary>
        /// Cierra el modal de créditos y desactiva la visualización.
        /// </summary>
        public void CloseModal()
        {
            if (!_isOpen) return;
            _isOpen = false;

            PlaySound(closeModalSound, GenerateCloseTone());

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeRoutine(canvasGroup != null ? canvasGroup.alpha : 1f, 0f, fadeDuration, () =>
            {
                if (modalRootPanel != null)
                {
                    modalRootPanel.SetActive(false);
                }
            }));

            Debug.Log($"{LogPrefix} Modal de Créditos y Marco Ético cerrado.");
        }

        public void ToggleModal()
        {
            if (_isOpen) CloseModal();
            else OpenModal();
        }

        private void PositionModalInFrontOfPlayer()
        {
            Transform cam = targetCamera != null ? targetCamera : (Camera.main != null ? Camera.main.transform : null);
            if (cam == null) return;

            Vector3 forward = cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 targetPos = cam.position + (forward * spawnDistanceInFront) + (Vector3.up * 0.05f);
            transform.position = targetPos;
            transform.rotation = Quaternion.LookRotation((transform.position - cam.position).normalized);
        }

        private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration, Action onComplete = null)
        {
            if (canvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            canvasGroup.alpha = fromAlpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = toAlpha;
            onComplete?.Invoke();
            _fadeCoroutine = null;
        }

        #endregion

        #region Helpers de Sonido y UI

        private void PlaySound(AudioClip clip, AudioClip fallbackClip = null)
        {
            if (audioSource == null) return;
            AudioClip toPlay = clip != null ? clip : fallbackClip;
            if (toPlay != null) audioSource.PlayOneShot(toPlay);
        }

        private AudioClip GenerateOpenTone()
        {
            return ProceduralAudioHelper.GenerateSineTone(0.12f, 620f, 0.35f);
        }

        private AudioClip GenerateCloseTone()
        {
            return ProceduralAudioHelper.GenerateSineTone(0.10f, 420f, 0.30f);
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
}
