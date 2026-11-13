using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Environment;
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
                titleText.text = $"<b><color={ColorGreenCode}>ANDES-SIM</color> · CREDITOS Y ETICA</b>";
            }

            if (teamRolesText != null)
            {
                teamRolesText.text =
                    $"<b><color={ColorGreenCode}>EQUIPO 1 · INTEGRANTES:</color></b>\n" +
                    $"• <b>Lider / Guion:</b> {teamLeaderName}\n" +
                    $"• <b>Unity C# / VR:</b> {unityDevName}  |  <b>3D / UI / Audio:</b> {artistName}\n" +
                    $"• <b>Ciberseguridad y QA:</b> {cybersecurityQaName}";
            }

            if (attributionAiText != null)
            {
                attributionAiText.text =
                    $"<b><color={ColorAmberWarning}>RECURSOS Y HERRAMIENTAS:</color></b>\n" +
                    $"• <b>Modelos 3D:</b> Assets CC0 / CC-BY (VNB Office Set, Sketchfab) y texturas propias.\n" +
                    $"• <b>Audio e IA:</b> Sintesis Web/SFX + apoyo de Google Gemini / Antigravity y Kling AI.";
            }

            if (legalEthicsText != null)
            {
                legalEthicsText.text =
                    $"<b><color={ColorAlertRed}>USO EDUCATIVO Y LEYES (30096 / 29733):</color></b>\n" +
                    $"• <b>Fines Academicos:</b> Simulador solo para concientizacion y defensa (nunca ataque).\n" +
                    $"• <b>Datos Ficticios:</b> Nombres y correos (<i>Banco Andino</i>, <i>Carla</i>, <i>Rasel</i>) inspirados en el caso Bangladesh 2016.";
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
            rootRect.sizeDelta = new Vector2(960f, 650f);
            rootRect.localScale = new Vector3(0.0019f, 0.0019f, 0.0019f);

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f;

            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();

            // Limpiar hijos previos si hubiera reconstruccion
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // Fondo general del modal
            GameObject bgObj = CreateUiPanel("Modal_Fondo", transform, Vector2.zero, new Vector2(960f, 650f), AndesTypographyTheme.BgVoidNavy);
            AddUiBorder(bgObj, HexToColor(ColorGreenCode, 0.85f), 2.5f);
            bgObj.AddComponent<HolographicPanelEntrance>().Configure(0.28f, 0.88f, 22f, null);
            bgObj.AddComponent<HolographicFloatMotion>().Configure(9f, 0.78f, new Vector3(0.45f, 0.70f, 0.30f), 0f, 0f, true);
            modalRootPanel = bgObj;

            // Barra de Cabecera
            GameObject headerObj = CreateUiPanel("Header_Bar", bgObj.transform, new Vector2(0f, 270f), new Vector2(910f, 76f), HexToColor(ColorCardBg, 0.95f));
            AddUiBorder(headerObj, HexToColor(ColorGreenCode, 0.45f), 1.5f);

            titleText = CreateTmpText("TitleText", headerObj.transform, new Vector2(-95f, 0f), new Vector2(680f, 64f),
                "Cargando titulo...", 26f, TextAlignmentOptions.Left, HexToColor(ColorTextLight));

            // Botón de Cerrar [X]
            GameObject btnCloseObj = CreateUiPanel("Btn_Cerrar_Modal", headerObj.transform, new Vector2(365f, 0f), new Vector2(140f, 52f), HexToColor(ColorAlertRed, 0.90f));
            AddUiBorder(btnCloseObj, HexToColor(ColorTextLight, 0.6f), 1.5f);
            closeButton = btnCloseObj.AddComponent<Button>();

            CreateTmpText("CloseLabel", btnCloseObj.transform, Vector2.zero, new Vector2(130f, 46f),
                "<b>[ X ] CERRAR</b>", 18f, TextAlignmentOptions.Center, HexToColor(ColorTextLight));

            WorldSpaceUiGazeButton gazeClose = btnCloseObj.AddComponent<WorldSpaceUiGazeButton>();
            gazeClose.ConfigureAudio(audioSource, closeModalSound, null);
            btnCloseObj.AddComponent<HolographicCardHover>().Configure(1.045f, 14f);

            // Bloque 1: Equipo 1 y Roles
            GameObject rolesPanel = CreateUiPanel("Panel_Roles", bgObj.transform, new Vector2(0f, 145f), new Vector2(910f, 140f), HexToColor(ColorCardBg, 0.90f));
            AddUiBorder(rolesPanel, HexToColor(ColorGreenCode, 0.40f), 1.5f);

            teamRolesText = CreateTmpText("TeamRolesText", rolesPanel.transform, Vector2.zero, new Vector2(870f, 124f),
                "Cargando roles...", 18f, TextAlignmentOptions.Left, HexToColor(ColorTextLight));

            // Bloque 2: Recursos y Herramientas
            GameObject aiPanel = CreateUiPanel("Panel_Recursos_IA", bgObj.transform, new Vector2(0f, -5f), new Vector2(910f, 130f), HexToColor(ColorCardBg, 0.90f));
            AddUiBorder(aiPanel, HexToColor(ColorAmberWarning, 0.45f), 1.5f);

            attributionAiText = CreateTmpText("AttributionAiText", aiPanel.transform, Vector2.zero, new Vector2(870f, 114f),
                "Cargando recursos...", 18f, TextAlignmentOptions.Left, HexToColor(ColorTextLight));

            // Bloque 3: Marco Ético y Legal
            GameObject ethicsPanel = CreateUiPanel("Panel_Marco_Legal", bgObj.transform, new Vector2(0f, -155f), new Vector2(910f, 136f), HexToColor(ColorCardBg, 0.90f));
            AddUiBorder(ethicsPanel, HexToColor(ColorAlertRed, 0.45f), 1.5f);

            legalEthicsText = CreateTmpText("LegalEthicsText", ethicsPanel.transform, Vector2.zero, new Vector2(870f, 120f),
                "Cargando marco legal...", 18f, TextAlignmentOptions.Left, HexToColor(ColorTextLight));

            // Botón Inferior de Retorno
            GameObject btnReturnObj = CreateUiPanel("Btn_Volver_Simulacion", bgObj.transform, new Vector2(0f, -268f), new Vector2(460f, 58f), HexToColor(ColorGreenCode, 0.94f));
            AddUiBorder(btnReturnObj, HexToColor(ColorDarkBg, 1f), 2f);
            Button returnBtn = btnReturnObj.AddComponent<Button>();
            returnBtn.onClick.AddListener(CloseModal);

            CreateTmpText("ReturnLabel", btnReturnObj.transform, Vector2.zero, new Vector2(440f, 50f),
                "<b>[ < ] VOLVER A LA SIMULACION</b>", 20f, TextAlignmentOptions.Center, HexToColor(ColorDarkBg));

            WorldSpaceUiGazeButton gazeReturn = btnReturnObj.AddComponent<WorldSpaceUiGazeButton>();
            gazeReturn.ConfigureAudio(audioSource, closeModalSound, null);
            btnReturnObj.AddComponent<HolographicCardHover>().Configure(1.045f, 14f);

            AndesTypographyTheme.ApplyTypographyToTree(transform, true, true);
        }

        #endregion

        #region Control de Apertura y Cierre

        /// <summary>
        /// Abre el modal de créditos orientándolo suavemente frente a la cámara del jugador.
        /// </summary>
        public void OpenModal()
        {
            if (modalRootPanel == null || titleText == null || closeButton == null)
            {
                BuildModalHierarchy();
                if (closeButton != null)
                {
                    closeButton.onClick.RemoveAllListeners();
                    closeButton.onClick.AddListener(CloseModal);
                }
            }
            PopulateTexts();

            _isOpen = true;
            gameObject.SetActive(true);

            PositionModalInFrontOfPlayer();

            AndesTypographyTheme.ApplyTypographyToTree(transform, true, true);

            if (modalRootPanel != null)
            {
                modalRootPanel.SetActive(true);
                HolographicPanelEntrance ent = modalRootPanel.GetComponent<HolographicPanelEntrance>();
                if (ent != null) ent.PlayEntrance();
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

            Vector3 targetPos = cam.position + (forward * Mathf.Max(spawnDistanceInFront, 1.45f)) + (Vector3.up * 0.02f);
            transform.position = targetPos;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
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
