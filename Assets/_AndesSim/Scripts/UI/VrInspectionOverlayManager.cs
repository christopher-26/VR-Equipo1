using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Scenario01;
using AndesSim.VR;

namespace AndesSim.UI
{
    /// <summary>
    /// Administrador del Sistema de Inspección y Zoom VR (RF-01 a RF-05).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// 
    /// Permite al jugador ampliar a pantalla completa en World Space:
    /// 1. El correo de Rasel Ahlam con sus 3 señales de phishing interactivas.
    /// 2. El Post-It y libreta de credenciales bancarias de Carla Mendoza.
    /// 3. La terminal de transferencias internacionales SWIFT ($951M).
    /// 4. La consola SIEM con los logs de conexión hacia la IP 185.130.44.82.
    /// </summary>
    public class VrInspectionOverlayManager : MonoBehaviour
    {
        public static VrInspectionOverlayManager I
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<VrInspectionOverlayManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("VrInspectionOverlayManager");
                        _instance = go.AddComponent<VrInspectionOverlayManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private static VrInspectionOverlayManager _instance;

        private const string LogPrefix = "[ANDES-SIM - InspectionOverlay]";

        private static readonly Color DarkGlassBgColor = new Color(0.04f, 0.07f, 0.12f, 0.96f);
        private static readonly Color CyanNeonAccent = new Color(0f, 0.95f, 1f, 1f);
        private static readonly Color GreenSuccessColor = new Color(0.22f, 0.95f, 0.54f, 1f);
        private static readonly Color WarningAmberColor = new Color(1f, 0.76f, 0.29f, 1f);
        private static readonly Color DangerRedColor = new Color(1f, 0.35f, 0.31f, 1f);

        [Header("--- Anclaje VR ---")]
        [SerializeField] private float distanceFromCamera = 1.05f;
        [SerializeField] private float smoothFollowSpeed = 8f;

        // Raíz del Canvas de Inspección
        private GameObject _overlayRoot;
        private Canvas _overlayCanvas;
        private RectTransform _mainPanelRect;
        private Camera _targetCamera;
        private bool _isOpen = false;

        // Contenedores dinámicos
        private TextMeshProUGUI _headerTitleText;
        private TextMeshProUGUI _subTitleText;
        private Transform _contentContainer;
        private GameObject _activeContentPanel;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureOverlayCanvas();
        }

        private void Start()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            }
        }

        private void Update()
        {
            if (!_isOpen || _overlayRoot == null) return;

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
                if (_targetCamera == null) return;
            }

            // Suave orientación y posicionamiento frente a los ojos del jugador
            Vector3 targetPosition = _targetCamera.transform.position + (_targetCamera.transform.forward * distanceFromCamera);
            Quaternion targetRotation = Quaternion.LookRotation(_targetCamera.transform.forward, _targetCamera.transform.up);

            _overlayRoot.transform.position = Vector3.Lerp(_overlayRoot.transform.position, targetPosition, Time.deltaTime * smoothFollowSpeed);
            _overlayRoot.transform.rotation = Quaternion.Slerp(_overlayRoot.transform.rotation, targetRotation, Time.deltaTime * smoothFollowSpeed);
        }

        /// <summary>
        /// Crea de forma procedural el Canvas World Space de inspección con estética de vidrio holográfico.
        /// </summary>
        private void EnsureOverlayCanvas()
        {
            if (_overlayRoot != null) return;

            _overlayRoot = new GameObject("VrInspectionOverlayCanvas");
            _overlayRoot.transform.SetParent(transform, false);

            _overlayCanvas = _overlayRoot.AddComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.WorldSpace;
            _overlayCanvas.sortingOrder = 950;

            CanvasScaler scaler = _overlayRoot.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f;

            _overlayRoot.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = _overlayRoot.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000f, 750f);
            canvasRect.localScale = Vector3.one * 0.0016f;

            // Panel Fondo Principal
            GameObject panelGO = new GameObject("InspectionFrame");
            panelGO.transform.SetParent(_overlayRoot.transform, false);
            _mainPanelRect = panelGO.AddComponent<RectTransform>();
            _mainPanelRect.anchorMin = Vector2.zero;
            _mainPanelRect.anchorMax = Vector2.one;
            _mainPanelRect.sizeDelta = Vector2.zero;

            Image bgImage = panelGO.AddComponent<Image>();
            bgImage.color = DarkGlassBgColor;

            // Borde estético superior
            GameObject topBar = new GameObject("TopGlowBar");
            topBar.transform.SetParent(panelGO.transform, false);
            RectTransform topBarRect = topBar.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.anchoredPosition = new Vector2(0f, -4f);
            topBarRect.sizeDelta = new Vector2(0f, 6f);
            Image topBarImg = topBar.AddComponent<Image>();
            topBarImg.color = CyanNeonAccent;

            // Título Principal
            GameObject titleGO = new GameObject("HeaderTitle");
            titleGO.transform.SetParent(panelGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 320f);
            titleRect.sizeDelta = new Vector2(920f, 45f);
            _headerTitleText = titleGO.AddComponent<TextMeshProUGUI>();
            _headerTitleText.fontSize = 22f;
            _headerTitleText.alignment = TextAlignmentOptions.Center;
            _headerTitleText.color = CyanNeonAccent;
            _headerTitleText.fontStyle = FontStyles.Bold;

            // Subtítulo
            GameObject subGO = new GameObject("SubTitle");
            subGO.transform.SetParent(panelGO.transform, false);
            RectTransform subRect = subGO.AddComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0f, 280f);
            subRect.sizeDelta = new Vector2(920f, 32f);
            _subTitleText = subGO.AddComponent<TextMeshProUGUI>();
            _subTitleText.fontSize = 14f;
            _subTitleText.alignment = TextAlignmentOptions.Center;
            _subTitleText.color = new Color(0.8f, 0.88f, 0.95f, 0.9f);

            // Contenedor de Contenido
            GameObject contentGO = new GameObject("DynamicContentContainer");
            contentGO.transform.SetParent(panelGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchoredPosition = new Vector2(0f, -10f);
            contentRect.sizeDelta = new Vector2(920f, 520f);
            _contentContainer = contentGO.transform;

            // Botón Inferior: CERRAR INSPECCIÓN
            GameObject closeBtnGO = CreateInspectionButton(
                "Btn_CerrarInspeccion", panelGO.transform,
                new Vector2(0f, -325f), new Vector2(360f, 50f),
                "<b>[ X CERRAR VISTA DE DETALLE ]</b>",
                new Color(0.2f, 0.25f, 0.35f, 0.95f),
                new Color(0.9f, 0.95f, 1f, 1f),
                CloseOverlay
            );

            _overlayRoot.SetActive(false);
            Debug.Log($"{LogPrefix} Canvas de Inspección VR inicializado con éxito.");
        }

        #region Vistas de Inspección Oficiales

        /// <summary>
        /// RF-02 y RF-03: Abre el correo de Rasel Ahlam en vista ampliada con las 3 señales resaltadas.
        /// </summary>
        public void OpenEmailInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b>BANCO ANDINO · BANDEJA DE CORREO CORPORATIVO</b>";
            _subTitleText.text = "Inspección Forense de Spear Phishing · Jueves 29 de Enero de 2015 (09:18 AM)";

            GameObject emailPanel = new GameObject("EmailContentPanel");
            emailPanel.transform.SetParent(_contentContainer, false);
            RectTransform eRect = emailPanel.AddComponent<RectTransform>();
            eRect.anchorMin = Vector2.zero;
            eRect.anchorMax = Vector2.one;
            eRect.sizeDelta = Vector2.zero;

            // Encabezado de correo
            CreateLabel("Lbl_From", emailPanel.transform, new Vector2(0f, 210f), new Vector2(900f, 32f),
                "<b>De:</b> Rasel Ahlam &lt;rasel.ahlam@career-applicants-bd.com&gt;", 15f, TextAlignmentOptions.Left, Color.white);

            CreateLabel("Lbl_To", emailPanel.transform, new Vector2(0f, 180f), new Vector2(900f, 28f),
                "<b>Para:</b> Carla Mendoza &lt;c.mendoza@bancoandino.pe&gt; y 15 destinatarios de pagos", 13.5f, TextAlignmentOptions.Left, new Color(0.7f, 0.8f, 0.9f));

            CreateLabel("Lbl_Subject", emailPanel.transform, new Vector2(0f, 145f), new Vector2(900f, 32f),
                "<b>Asunto:</b> <color=#FF5A4E>URGENTE: Contratacion Inmediata</color> - Curriculum Vitae y Anexos", 15.5f, TextAlignmentOptions.Left, Color.white);

            // Señal 1: Botón Remitente
            CreateSignalButton("Btn_Signal1", emailPanel.transform, new Vector2(-290f, 85f), new Vector2(280f, 65f),
                "1. SEÑAL: REMITENTE\n<size=11><color=#FFC24A>@career-applicants-bd.com</color></size>",
                PhishingInspector.SignalRemitenteId);

            // Señal 2: Botón Urgencia
            CreateSignalButton("Btn_Signal2", emailPanel.transform, new Vector2(0f, 85f), new Vector2(280f, 65f),
                "2. SEÑAL: URGENCIA\n<size=11><color=#FFC24A>\"Contratación Inmediata\"</color></size>",
                PhishingInspector.SignalUrgenciaId);

            // Señal 3: Botón Enlace Malicioso
            CreateSignalButton("Btn_Signal3", emailPanel.transform, new Vector2(290f, 85f), new Vector2(280f, 65f),
                "3. SEÑAL: ADJUNTO MALICIOSO\n<size=11><color=#FFC24A>CV_2015.scr.zip (IP externa)</color></size>",
                PhishingInspector.SignalEnlaceId);

            // Cuerpo del correo
            GameObject bodyBox = new GameObject("EmailBodyBox");
            bodyBox.transform.SetParent(emailPanel.transform, false);
            RectTransform bbRect = bodyBox.AddComponent<RectTransform>();
            bbRect.anchoredPosition = new Vector2(0f, -20f);
            bbRect.sizeDelta = new Vector2(900f, 110f);
            Image bodyBg = bodyBox.AddComponent<Image>();
            bodyBg.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);

            CreateLabel("Txt_EmailBody", bodyBox.transform, Vector2.zero, new Vector2(870f, 95f),
                "<i>\"Estimada Carla Mendoza,\n\nLe ruego revisar con carácter de extrema urgencia mi Curriculum Vitae adjunto en el enlace seguro:\n" +
                "<color=#00F2FE><u>http://185.130.44.82/Rasel_Ahlam_CV_2015.scr.zip</u></color>\n" +
                "Poseo amplia experiencia en liquidación de divisas SWIFT y estoy disponible para incorporación inmediata.\"</i>",
                13f, TextAlignmentOptions.TopLeft, new Color(0.9f, 0.95f, 1f));

            // Botones de Decisión Oficiales (RF-04)
            // Botón Rojo: ABRIR ENLACE (MODO ATAQUE)
            CreateInspectionButton(
                "Btn_AttackOpenLink", emailPanel.transform,
                new Vector2(-220f, -145f), new Vector2(400f, 58f),
                "<b>[ ABRIR ENLACE (MODO ATAQUE 2016) ]</b>\n<size=10.5>Detonar infección malware de Lazarus</size>",
                new Color(0.5f, 0.12f, 0.12f, 0.95f),
                new Color(1f, 0.4f, 0.35f, 1f),
                () =>
                {
                    CloseOverlay();
                    if (PhishingInspector.I != null)
                    {
                        PhishingInspector.I.ExecuteOpenLinkAttackSequence();
                    }
                }
            );

            // Botón Verde: REPORTAR PHISHING (MODO DEFENSA)
            CreateInspectionButton(
                "Btn_DefenseReport", emailPanel.transform,
                new Vector2(220f, -145f), new Vector2(400f, 58f),
                "<b>[ REPORTAR PHISHING (MODO DEFENSA) ]</b>\n<size=10.5>Aislar muestra en Sandbox del SOC</size>",
                new Color(0.08f, 0.35f, 0.2f, 0.95f),
                new Color(0.35f, 1f, 0.6f, 1f),
                () =>
                {
                    CloseOverlay();
                    if (MailQuarantine.I != null)
                    {
                        MailQuarantine.I.TriggerQuarantineDefense();
                    }
                    else if (PhishingInspector.I != null)
                    {
                        PhishingInspector.I.ExecuteReportPhishingDefenseSequence();
                    }
                }
            );

            _activeContentPanel = emailPanel;
            ShowOverlay();
        }

        /// <summary>
        /// RF-05: Abre la vista ampliada del Post-It y libreta de credenciales de Carla Mendoza.
        /// </summary>
        public void OpenCredentialsInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b>EVIDENCIA FORENSE #2: CREDENCIALES EXPUESTAS</b>";
            _subTitleText.text = "Post-It y Libreta de Operaciones en el Puesto 1 (Carla Mendoza)";

            GameObject credPanel = new GameObject("CredentialsContentPanel");
            credPanel.transform.SetParent(_contentContainer, false);
            RectTransform cRect = credPanel.AddComponent<RectTransform>();
            cRect.anchorMin = Vector2.zero;
            cRect.anchorMax = Vector2.one;
            cRect.sizeDelta = Vector2.zero;

            // Simulación visual del Post-It Amarillo
            GameObject postItBox = new GameObject("PostItVisual");
            postItBox.transform.SetParent(credPanel.transform, false);
            RectTransform pRect = postItBox.AddComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(-180f, 20f);
            pRect.sizeDelta = new Vector2(400f, 320f);
            Image postItBg = postItBox.AddComponent<Image>();
            postItBg.color = new Color(0.98f, 0.88f, 0.35f, 0.98f); // Amarillo Post-It

            CreateLabel("Txt_PostItTitle", postItBox.transform, new Vector2(0f, 110f), new Vector2(360f, 40f),
                "<b>BANCO ANDINO · SWIFT</b>", 16f, TextAlignmentOptions.Center, new Color(0.1f, 0.1f, 0.1f));

            CreateLabel("Txt_PostItBody", postItBox.transform, new Vector2(0f, -10f), new Vector2(360f, 200f),
                "<b>Terminal:</b> PC-PAGOS-01\n" +
                "<b>Usuario:</b> c.mendoza\n" +
                "<b>Password:</b> <color=#990000>Swift2015$Admin!</color>\n" +
                "<b>Token Bóveda:</b> 849201\n" +
                "<b>PIN Respaldo:</b> 9481\n\n" +
                "<i>(No olvidar actualizar cada 90 días)</i>",
                14f, TextAlignmentOptions.TopLeft, new Color(0.12f, 0.12f, 0.12f));

            // Panel de Diagnóstico Forense
            GameObject diagBox = new GameObject("ForensicDiagBox");
            diagBox.transform.SetParent(credPanel.transform, false);
            RectTransform dRect = diagBox.AddComponent<RectTransform>();
            dRect.anchoredPosition = new Vector2(230f, 20f);
            dRect.sizeDelta = new Vector2(380f, 320f);
            Image diagBg = diagBox.AddComponent<Image>();
            diagBg.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);

            CreateLabel("Txt_DiagTitle", diagBox.transform, new Vector2(0f, 110f), new Vector2(350f, 40f),
                "<b>ANÁLISIS FORENSE ISO 27001</b>", 15f, TextAlignmentOptions.Center, WarningAmberColor);

            CreateLabel("Txt_DiagBody", diagBox.transform, new Vector2(0f, -15f), new Vector2(350f, 220f),
                "<b>Falla Crítica Identificada:</b>\n" +
                "Violación del control A.11.2.9 (Puesto de trabajo despejado).\n\n" +
                "Las credenciales bancarias estaban escritas a mano y a la vista de cualquier visitante.\n\n" +
                "<b>Impacto:</b> Permitió al intruso elevar privilegios de inmediato y operar transferencias SWIFT sin requerir cracking de contraseñas.",
                12.5f, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.92f, 1f));

            // Botón de Registro de Evidencia
            CreateInspectionButton(
                "Btn_LogEvidencePostIt", credPanel.transform,
                new Vector2(0f, -180f), new Vector2(460f, 54f),
                "<b>[ REGISTRAR EVIDENCIA #2 EN EL EXPEDIENTE ]</b>",
                new Color(0.15f, 0.45f, 0.25f, 0.95f),
                GreenSuccessColor,
                () =>
                {
                    if (GameManager.I != null)
                    {
                        GameManager.I.RegisterEvidence("Evidencia_PostIt");
                    }
                    if (AndesSim.Audio.AudioManager.I != null)
                    {
                        AndesSim.Audio.AudioManager.I.PlayEvidenceFound(transform.position);
                    }
                    CloseOverlay();
                }
            );

            _activeContentPanel = credPanel;
            ShowOverlay();
        }

        /// <summary>
        /// RF-01 / RF-04: Abre la vista ampliada de la Terminal SWIFT (Puesto 2).
        /// </summary>
        public void OpenSwiftInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b>TERMINAL SWIFT ALLIANCE ACCESS · PUESTO 2</b>";
            _subTitleText.text = "Operaciones Internacionales y Auditoría de Red Plana (VLANs Ausentes)";

            GameObject swiftPanel = new GameObject("SwiftContentPanel");
            swiftPanel.transform.SetParent(_contentContainer, false);
            RectTransform sRect = swiftPanel.AddComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.sizeDelta = Vector2.zero;

            GameObject tableBox = new GameObject("SwiftTableBox");
            tableBox.transform.SetParent(swiftPanel.transform, false);
            RectTransform tbRect = tableBox.AddComponent<RectTransform>();
            tbRect.anchoredPosition = new Vector2(0f, 30f);
            tbRect.sizeDelta = new Vector2(880f, 310f);
            Image tbBg = tableBox.AddComponent<Image>();
            tbBg.color = new Color(0.06f, 0.1f, 0.16f, 0.95f);

            CreateLabel("Txt_SwiftTable", tableBox.transform, Vector2.zero, new Vector2(850f, 290f),
                "<b>SISTEMA SWIFT ALLIANCE ACCESS - MENSAJERÍA MT103 PENDIENTE:</b>\n" +
                "────────────────────────────────────────────────────────────────────────────\n" +
                "<b>ID Transacción:</b> MT103-20150129-948192\n" +
                "<b>Banco Emisor:</b> BANCO ANDINO (Código SWIFT: ANDIPEPL)\n" +
                "<b>Banco Beneficiario:</b> Federal Reserve Bank of New York -> RCBC Filipinas\n" +
                "<b>Monto Total Comprometido:</b> <color=#FF5A4E><b>US$ 951,000,000.00</b></color>\n" +
                "<b>Estado de Tráfico:</b> Sin filtrado de paquetes ni inspección profunda\n" +
                "<b>Segmentación de Red:</b> <color=#FFC24A>RED PLANA (Switch Cisco no configurado con VLAN 100)</color>\n" +
                "────────────────────────────────────────────────────────────────────────────\n" +
                "<i>Nota Forense: La ausencia de segmentación permitió que la infección de Carla se propagara a este puesto en segundos.</i>",
                13f, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.95f, 1f));

            _activeContentPanel = swiftPanel;
            ShowOverlay();
        }

        /// <summary>
        /// RF-05: Abre la vista ampliada de la Consola SIEM (Puesto 3) con los registros de accesos al C2.
        /// </summary>
        public void OpenSiemLogInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b>CONSOLA SIEM Y REGISTROS DE ACCESO · PUESTO 3</b>";
            _subTitleText.text = "Auditoría de Tráfico Saliente C2 hacia la IP 185.130.44.82";

            GameObject siemPanel = new GameObject("SiemContentPanel");
            siemPanel.transform.SetParent(_contentContainer, false);
            RectTransform smRect = siemPanel.AddComponent<RectTransform>();
            smRect.anchorMin = Vector2.zero;
            smRect.anchorMax = Vector2.one;
            smRect.sizeDelta = Vector2.zero;

            GameObject logBox = new GameObject("SiemLogBox");
            logBox.transform.SetParent(siemPanel.transform, false);
            RectTransform lRect = logBox.AddComponent<RectTransform>();
            lRect.anchoredPosition = new Vector2(0f, 40f);
            lRect.sizeDelta = new Vector2(880f, 290f);
            Image lBg = logBox.AddComponent<Image>();
            lBg.color = new Color(0.04f, 0.08f, 0.12f, 0.95f);

            CreateLabel("Txt_SiemLogs", logBox.transform, Vector2.zero, new Vector2(850f, 270f),
                "<b>SECURITY INFORMATION AND EVENT MANAGEMENT (SIEM) - ACCESS LOG:</b>\n" +
                "────────────────────────────────────────────────────────────────────────────\n" +
                "[09:18:22] <color=#FF5A4E>TCP CONNECT: 192.168.1.101:49152 -> 185.130.44.82:443 [ESTABLISHED]</color>\n" +
                "[09:18:25] PROCESS: svchost_upd.exe (PID 4820) origin: C:\\Users\\c.mendoza\\AppData\\Local\n" +
                "[09:18:29] DNS QUERY: career-applicants-bd.com -> 185.130.44.82 (Dominio malicioso Lazarus)\n" +
                "[09:18:35] ALERT: Descarga de archivo de 4.8MB (.scr.zip) sin escaneo perimetral.\n" +
                "[09:18:40] STATUS: Alerta desestimada por excepción manual en firewall perimetral.\n" +
                "────────────────────────────────────────────────────────────────────────────\n" +
                "<b>Indicio Clave:</b> La IP 185.130.44.82 es el centro de comando del grupo Lazarus.",
                12.5f, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.95f, 1f));

            // Botón de Registro de Evidencia SIEM
            CreateInspectionButton(
                "Btn_LogEvidenceSiem", siemPanel.transform,
                new Vector2(0f, -165f), new Vector2(460f, 54f),
                "<b>[ REGISTRAR EVIDENCIA #3: LOG DE ACCESOS ]</b>",
                new Color(0.15f, 0.45f, 0.25f, 0.95f),
                GreenSuccessColor,
                () =>
                {
                    if (GameManager.I != null)
                    {
                        GameManager.I.RegisterEvidence("Evidencia_LogAccesos");
                    }
                    if (AndesSim.Audio.AudioManager.I != null)
                    {
                        AndesSim.Audio.AudioManager.I.PlayEvidenceFound(transform.position);
                    }
                    CloseOverlay();
                }
            );

            _activeContentPanel = siemPanel;
            ShowOverlay();
        }

        #endregion

        #region Control de Ventana y Ayudantes de UI

        public void ShowOverlay()
        {
            _isOpen = true;
            if (_overlayRoot != null)
            {
                if (_targetCamera == null) _targetCamera = Camera.main;
                if (_targetCamera != null)
                {
                    _overlayRoot.transform.position = _targetCamera.transform.position + (_targetCamera.transform.forward * distanceFromCamera);
                    _overlayRoot.transform.rotation = Quaternion.LookRotation(_targetCamera.transform.forward, _targetCamera.transform.up);
                }
                _overlayRoot.SetActive(true);
            }

            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayLaserScan(transform.position);
            }
        }

        public void CloseOverlay()
        {
            _isOpen = false;
            if (_overlayRoot != null)
            {
                _overlayRoot.SetActive(false);
            }
            ClearActiveContent();

            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayGazeClick(transform.position);
            }
        }

        private void ClearActiveContent()
        {
            if (_activeContentPanel != null)
            {
                Destroy(_activeContentPanel);
                _activeContentPanel = null;
            }
        }

        private GameObject CreateInspectionButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, Color bgColor, Color textColor, Action onClick)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            btnGO.layer = LayerMask.NameToLayer("Interactive");

            RectTransform rect = btnGO.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image bg = btnGO.AddComponent<Image>();
            bg.color = bgColor;

            Button btn = btnGO.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = bgColor * 1.25f;
            cb.pressedColor = bgColor * 0.8f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());

            // Añadir soporte de retícula VR
            GazeInteractable gaze = btnGO.AddComponent<GazeInteractable>();
            gaze.interactableLabel = text.Replace("<b>", "").Replace("</b>", "").Split('\n')[0];
            if (onClick != null) gaze.onClick.AddListener(() => onClick.Invoke());

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            RectTransform tRect = txtGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;

            return btnGO;
        }

        private void CreateSignalButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, string signalId)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            btnGO.layer = LayerMask.NameToLayer("Interactive");

            RectTransform rect = btnGO.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image bg = btnGO.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.14f, 0.08f, 0.95f);

            Button btn = btnGO.AddComponent<Button>();
            GazeInteractable gaze = btnGO.AddComponent<GazeInteractable>();
            gaze.interactableLabel = $"Inspeccionar {signalId}";

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            RectTransform tRect = txtGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 12f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = WarningAmberColor;

            Action markAction = () =>
            {
                if (PhishingInspector.I != null)
                {
                    PhishingInspector.I.MarkPhishingSignal(signalId);
                }
                bg.color = new Color(0.08f, 0.35f, 0.18f, 0.95f);
                tmp.text = $"<b>[✓ IDENTIFICADA]</b>\n<size=10>{signalId}</size>";
                tmp.color = GreenSuccessColor;
            };

            btn.onClick.AddListener(() => markAction.Invoke());
            gaze.onClick.AddListener(() => markAction.Invoke());
        }

        private void CreateLabel(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
        }

        #endregion
    }
}
