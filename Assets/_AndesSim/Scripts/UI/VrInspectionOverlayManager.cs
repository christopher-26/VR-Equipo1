using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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

        private static readonly Color DarkGlassBgColor = new Color(0.02f, 0.05f, 0.09f, 1.0f);
        private static readonly Color CyanNeonAccent = new Color(0f, 0.95f, 1f, 1f);
        private static readonly Color GreenSuccessColor = new Color(0.22f, 0.95f, 0.54f, 1f);
        private static readonly Color WarningAmberColor = new Color(1f, 0.76f, 0.29f, 1f);
        private static readonly Color DangerRedColor = new Color(1f, 0.35f, 0.31f, 1f);

        [Header("--- Anclaje VR ---")]
        [SerializeField] private float distanceFromCamera = 1.32f;
        private int _emailSubPart = 0;



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

            // El panel de inspección se mantiene ESTACIONARIO en el espacio 3D para permitir apuntar
            // libremente la retícula VR hacia los botones sin que el panel se desplace con la cabeza.
            // Si el jugador gira la cabeza más de 65 grados alejándose del panel, se cierra automáticamente.
            Vector3 dirToPanel = (_overlayRoot.transform.position - _targetCamera.transform.position).normalized;
            float angle = Vector3.Angle(_targetCamera.transform.forward, dirToPanel);
            if (angle > 82f)
            {
                CloseOverlay();
            }
        }

        /// <summary>
        /// Crea de forma procedural el Canvas World Space de inspección con estética de vidrio holográfico.
        /// </summary>
        private void EnsureOverlayCanvas()
        {
            if (_overlayRoot != null) return;

            _overlayRoot = new GameObject("VrInspectionOverlayCanvas");
            _overlayRoot.transform.SetParent(null); // Canvas independiente en World Space
            _overlayRoot.layer = LayerMask.NameToLayer("Interactive");

            _overlayCanvas = _overlayRoot.AddComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.WorldSpace;
            _overlayCanvas.sortingOrder = 5000;
            if (_targetCamera == null) _targetCamera = Camera.main;
            if (_targetCamera != null) _overlayCanvas.worldCamera = _targetCamera;

            CanvasScaler scaler = _overlayRoot.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 4f;

            _overlayRoot.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = _overlayRoot.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1000f, 750f);
            canvasRect.localScale = Vector3.one * 0.00192f;

            // Panel Fondo Principal
            EnsureEventSystemExists();

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
            _headerTitleText.fontSize = 26f;
            _headerTitleText.alignment = TextAlignmentOptions.Center;
            _headerTitleText.color = CyanNeonAccent;
            _headerTitleText.fontStyle = FontStyles.Bold;

            // Subtítulo
            GameObject subGO = new GameObject("SubTitle");
            subGO.transform.SetParent(panelGO.transform, false);
            RectTransform subRect = subGO.AddComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0f, 280f);
            subRect.sizeDelta = new Vector2(920f, 36f);
            _subTitleText = subGO.AddComponent<TextMeshProUGUI>();
            _subTitleText.fontSize = 18f;
            _subTitleText.alignment = TextAlignmentOptions.Center;
            _subTitleText.color = new Color(0.8f, 0.88f, 0.95f, 0.9f);

            // Contenedor de Contenido
            GameObject contentGO = new GameObject("DynamicContentContainer");
            contentGO.transform.SetParent(panelGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchoredPosition = new Vector2(0f, -10f);
            contentRect.sizeDelta = new Vector2(920f, 520f);
            _contentContainer = contentGO.transform;

            // Botón Superior Derecho: CERRAR INMEDIATO (X)
            CreateInspectionButton(
                "Btn_CerrarTopRight", panelGO.transform,
                new Vector2(430f, 325f), new Vector2(90f, 36f),
                "<b>[ X ]</b>",
                new Color(0.35f, 0.12f, 0.15f, 0.95f),
                Color.white,
                CloseOverlay
            );

            // Barra Persistente Guiada (Paso a Paso en VR)
            CreateInspectionButton(
                "Btn_NavCorreo", panelGO.transform,
                new Vector2(-368f, -325f), new Vector2(174f, 52f),
                "<b>1. CORREO</b>",
                new Color(0.08f, 0.22f, 0.36f, 0.96f),
                Color.white,
                () => OpenEmailInspection(0)
            );
            CreateInspectionButton(
                "Btn_NavPostIt", panelGO.transform,
                new Vector2(-184f, -325f), new Vector2(174f, 52f),
                "<b>2. POST-IT</b>",
                new Color(0.08f, 0.22f, 0.36f, 0.96f),
                Color.white,
                OpenCredentialsInspection
            );
            CreateInspectionButton(
                "Btn_NavSiem", panelGO.transform,
                new Vector2(0f, -325f), new Vector2(174f, 52f),
                "<b>3. SIEM</b>",
                new Color(0.08f, 0.22f, 0.36f, 0.96f),
                Color.white,
                OpenSiemLogInspection
            );
            CreateInspectionButton(
                "Btn_NavTablero", panelGO.transform,
                new Vector2(186f, -325f), new Vector2(186f, 52f),
                "<b>4. TABLERO ></b>",
                new Color(0.08f, 0.32f, 0.24f, 0.96f),
                GreenSuccessColor,
                NavigateToSuspectBoardOrDefense
            );
            CreateInspectionButton(
                "Btn_CerrarInspeccion", panelGO.transform,
                new Vector2(378f, -325f), new Vector2(160f, 52f),
                "<b>[ X ] SALIR</b>",
                new Color(0.35f, 0.14f, 0.16f, 0.96f),
                Color.white,
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
            OpenEmailInspection(0);
        }

        /// <summary>
        /// RF-02 y RF-03: Vista guiada en 2 partes (1ra Parte: Leer correo, 2da Parte: Marcar 3 pistas y decidir).
        /// </summary>
        public void OpenEmailInspection(int subPart)
        {
            _emailSubPart = Mathf.Clamp(subPart, 0, 1);
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b>PC 1 - CORREO DE PHISHING</b>";
            _subTitleText.text = _emailSubPart == 0
                ? "1ra Parte: Lee el correo sospechoso"
                : "2da Parte: Marca las 3 pistas y decide tu accion";

            GameObject emailPanel = new GameObject("EmailContentPanel");
            emailPanel.transform.SetParent(_contentContainer, false);
            RectTransform eRect = emailPanel.AddComponent<RectTransform>();
            eRect.anchorMin = Vector2.zero;
            eRect.anchorMax = Vector2.one;
            eRect.sizeDelta = Vector2.zero;

            // Selector superior: 1ra Parte vs 2da Parte
            Color part1Bg = _emailSubPart == 0 ? new Color(0.08f, 0.34f, 0.44f, 0.98f) : new Color(0.10f, 0.15f, 0.22f, 0.92f);
            Color part2Bg = _emailSubPart == 1 ? new Color(0.38f, 0.26f, 0.08f, 0.98f) : new Color(0.10f, 0.15f, 0.22f, 0.92f);

            CreateInspectionButton(
                "Btn_TabParte1", emailPanel.transform,
                new Vector2(-230f, 215f), new Vector2(435f, 48f),
                "<b>1RA PARTE: LEER CORREO</b>",
                part1Bg, _emailSubPart == 0 ? CyanNeonAccent : Color.white,
                () => OpenEmailInspection(0)
            );

            CreateInspectionButton(
                "Btn_TabParte2", emailPanel.transform,
                new Vector2(230f, 215f), new Vector2(435f, 48f),
                "<b>2DA PARTE: PISTAS Y DECIDIR</b>",
                part2Bg, _emailSubPart == 1 ? WarningAmberColor : Color.white,
                () => OpenEmailInspection(1)
            );

            if (_emailSubPart == 0)
            {
                GameObject bodyBox = new GameObject("EmailReadCard");
                bodyBox.transform.SetParent(emailPanel.transform, false);
                RectTransform bbRect = bodyBox.AddComponent<RectTransform>();
                bbRect.anchoredPosition = new Vector2(0f, 35f);
                bbRect.sizeDelta = new Vector2(880f, 260f);
                Image bodyBg = bodyBox.AddComponent<Image>();
                bodyBg.color = new Color(0.07f, 0.11f, 0.17f, 0.96f);

                CreateLabel("Txt_MailLines", bodyBox.transform, Vector2.zero, new Vector2(830f, 225f),
                    "<b>De:</b> rasel.ahlam@<color=#FFC24A>career-applicants-bd.com</color>\n" +
                    "<b>Asunto:</b> <color=#FF5A4E>URGENTE: CV Especialista SWIFT</color>\n\n" +
                    "<i>'Carla: adjunto mi CV urgente para pagos.'</i>\n\n" +
                    "<b>Adjunto:</b> <color=#00F2FE>http://185.130.44.82/CV_2015.scr.zip</color>",
                    20.5f, TextAlignmentOptions.MidlineLeft, Color.white);

                CreateInspectionButton(
                    "Btn_NextToPart2", emailPanel.transform,
                    new Vector2(-225f, -165f), new Vector2(420f, 68f),
                    "<b>VER 2DA PARTE: 3 PISTAS ></b>",
                    new Color(0.10f, 0.34f, 0.42f, 0.98f),
                    CyanNeonAccent,
                    () => OpenEmailInspection(1)
                );

                CreateInspectionButton(
                    "Btn_QuickOpenLink", emailPanel.transform,
                    new Vector2(225f, -165f), new Vector2(420f, 68f),
                    "<b><color=#FF5A4E>ABRIR ENLACE (ATAQUE)</color></b>",
                    new Color(0.48f, 0.12f, 0.12f, 0.98f),
                    Color.white,
                    TriggerAttackInfectionSequence
                );
            }
            else
            {
                CreateLabel("Lbl_StepHint", emailPanel.transform, new Vector2(0f, 155f), new Vector2(880f, 38f),
                    "Mira las 3 senales para marcarlas y luego elige:",
                    19.5f, TextAlignmentOptions.Center, Color.white);

                CreateSignalButton("Btn_Signal1", emailPanel.transform, new Vector2(-300f, 70f), new Vector2(285f, 108f),
                    "<b>1. DOMINIO FALSO</b>\n<size=15><color=#FFC24A>@career-applicants</color></size>",
                    PhishingInspector.SignalRemitenteId);

                CreateSignalButton("Btn_Signal2", emailPanel.transform, new Vector2(0f, 70f), new Vector2(285f, 108f),
                    "<b>2. URGENCIA</b>\n<size=15><color=#FFC24A>Presion inmediata</color></size>",
                    PhishingInspector.SignalUrgenciaId);

                CreateSignalButton("Btn_Signal3", emailPanel.transform, new Vector2(300f, 70f), new Vector2(285f, 108f),
                    "<b>3. ARCHIVO .SCR</b>\n<size=15><color=#FFC24A>IP 185.130.44.82</color></size>",
                    PhishingInspector.SignalEnlaceId);

                CreateInspectionButton(
                    "Btn_AttackOpenLink", emailPanel.transform,
                    new Vector2(-225f, -55f), new Vector2(420f, 74f),
                    "<b><color=#FF5A4E>[ ! ] ABRIR ENLACE (ATAQUE)</color></b>\n<size=14>Ver propagacion del virus</size>",
                    new Color(0.48f, 0.12f, 0.12f, 0.98f),
                    Color.white,
                    TriggerAttackInfectionSequence
                );

                CreateInspectionButton(
                    "Btn_DefenseReport", emailPanel.transform,
                    new Vector2(225f, -55f), new Vector2(420f, 74f),
                    "<b><color=#38F28A>[ + ] AISLAR SANDBOX (DEFENSA)</color></b>\n<size=14>Bloquear amenaza</size>",
                    new Color(0.08f, 0.34f, 0.18f, 0.98f),
                    Color.white,
                    () =>
                    {
                        if (MailQuarantine.I != null)
                        {
                            MailQuarantine.I.TriggerQuarantineDefense();
                        }
                        else if (PhishingInspector.I != null)
                        {
                            PhishingInspector.I.ExecuteReportPhishingDefenseSequence();
                        }
                        OpenQuarantineInspection();
                    }
                );

                CreateInspectionButton(
                    "Btn_NextStepPostIt", emailPanel.transform,
                    new Vector2(0f, -165f), new Vector2(520f, 58f),
                    "<b>SIGUIENTE PASO: VER POST-IT (PC 1) ></b>",
                    new Color(0.14f, 0.24f, 0.38f, 0.96f),
                    CyanNeonAccent,
                    OpenCredentialsInspection
                );
            }

            _activeContentPanel = emailPanel;
            ShowOverlay();
        }

        /// <summary>
        /// MODO DEFENSA (RF-04): El correo es bloqueado por el filtro Sandbox.
        /// </summary>
        public void OpenQuarantineInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            if (AndesSim.Scenario01.InfectionSpreadVisualizer.I != null)
            {
                AndesSim.Scenario01.InfectionSpreadVisualizer.I.ShowDefenseContainmentSign();
            }

            _headerTitleText.text = "<b><color=#38F28A>CORREO EN CUARENTENA (SANDBOX)</color></b>";
            _subTitleText.text = "Amenaza detenida antes de infectar la red";

            GameObject qPanel = new GameObject("QuarantineContentPanel");
            qPanel.transform.SetParent(_contentContainer, false);
            RectTransform qRect = qPanel.AddComponent<RectTransform>();
            qRect.anchorMin = Vector2.zero;
            qRect.anchorMax = Vector2.one;
            qRect.sizeDelta = Vector2.zero;

            GameObject boxGO = new GameObject("SandboxLogBox");
            boxGO.transform.SetParent(qPanel.transform, false);
            RectTransform boxRect = boxGO.AddComponent<RectTransform>();
            boxRect.anchoredPosition = new Vector2(0f, 35f);
            boxRect.sizeDelta = new Vector2(880f, 300f);
            Image boxBg = boxGO.AddComponent<Image>();
            boxBg.color = new Color(0.04f, 0.09f, 0.12f, 0.98f);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b><color=#38F28A>REPORTE DE CONTENCION:</color></b>\n");
            sb.AppendLine("- Sandbox: Adjunto .scr.zip aislado sin ejecutarse.");
            sb.AppendLine("- Remitente: Dominio externo falso bloqueado.");
            sb.AppendLine("- Firewall: IP atacante 185.130.44.82 cortada.\n");
            sb.AppendLine("<color=#38F28A><b>[OK] 100% CONTENIDO - CERO PERDIDAS</b></color>");

            CreateLabel("Txt_QuarantineLog", boxGO.transform, Vector2.zero, new Vector2(830f, 260f),
                sb.ToString(), 20.5f, TextAlignmentOptions.MidlineLeft, Color.white);

            CreateInspectionButton(
                "Btn_GoDefenseConsole", qPanel.transform,
                new Vector2(-305f, -175f), new Vector2(280f, 62f),
                "<b>IR A CONSOLA DEFENSA ></b>",
                new Color(0.08f, 0.28f, 0.36f, 0.98f),
                CyanNeonAccent,
                NavigateToDefenseConsole
            );

            CreateInspectionButton(
                "Btn_ConfirmDefense", qPanel.transform,
                new Vector2(0f, -175f), new Vector2(290f, 62f),
                "<b><color=#38F28A>FINALIZAR CON EXITO</color></b>",
                new Color(0.08f, 0.35f, 0.18f, 0.98f),
                Color.white,
                () =>
                {
                    CloseOverlay();
                    if (MailQuarantine.I != null)
                    {
                        MailQuarantine.I.ConfirmContainmentAndEndScenario();
                    }
                    else if (GameManager.I != null)
                    {
                        GameManager.I.ApplyDefense("Defensa_Sandbox");
                        GameManager.I.ApplyDefense("Defensa_BotonReportar");
                        GameManager.I.ApplyDefense("Defensa_MFA");
                        GameManager.I.EndScenario();
                    }
                }
            );

            CreateInspectionButton(
                "Btn_NextSiemDefense", qPanel.transform,
                new Vector2(305f, -175f), new Vector2(260f, 62f),
                "<b>VER SIEM (PC 3) ></b>",
                new Color(0.16f, 0.22f, 0.34f, 0.98f),
                Color.white,
                OpenSiemLogInspection
            );

            _activeContentPanel = qPanel;
            ShowOverlay();
        }

        /// <summary>
        /// MODO ATAQUE (RF-04): Propaga la infeccion y muestra el resultado.
        /// </summary>
        public void TriggerAttackInfectionSequence()
        {
            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayInfectionVoiceAlert();
                AndesSim.Audio.AudioManager.I.PlayMusicTrack(AndesSim.Audio.MusicTheme.AttackTension, 0.3f);
            }

            if (PhishingInspector.I != null)
            {
                PhishingInspector.I.ExecuteOpenLinkAttackSequence();
            }
            else if (InfectionSpreadVisualizer.I != null)
            {
                InfectionSpreadVisualizer.I.PlayInfectionSequence();
            }

            StopCoroutine(nameof(InfectionScreenAnimationRoutine));
            StartCoroutine(nameof(InfectionScreenAnimationRoutine));
        }

        private IEnumerator InfectionScreenAnimationRoutine()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b><color=#FF3834>[ ! ] MALWARE EN EJECUCION</color></b>";
            _subTitleText.text = "El dropper .scr se propaga por toda la red";

            GameObject infectPanel = new GameObject("InfectionContentPanel");
            infectPanel.transform.SetParent(_contentContainer, false);
            RectTransform iRect = infectPanel.AddComponent<RectTransform>();
            iRect.anchorMin = Vector2.zero;
            iRect.anchorMax = Vector2.one;
            iRect.sizeDelta = Vector2.zero;

            GameObject terminalBox = new GameObject("InfectionTerminalBox");
            terminalBox.transform.SetParent(infectPanel.transform, false);
            RectTransform tRect = terminalBox.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 20f);
            tRect.sizeDelta = new Vector2(880f, 300f);
            Image tBg = terminalBox.AddComponent<Image>();
            tBg.color = new Color(0.20f, 0.03f, 0.03f, 0.98f);

            CreateLabel("Txt_InfectionTrace", terminalBox.transform, Vector2.zero, new Vector2(830f, 260f),
                "<color=#FF5A4E><b>1/3 Descargando CV_2015.scr...</b></color>",
                21f, TextAlignmentOptions.MidlineLeft, Color.white);

            GameObject textObj = terminalBox.transform.Find("Txt_InfectionTrace")?.gameObject;
            TextMeshProUGUI traceTmp = textObj != null ? textObj.GetComponent<TextMeshProUGUI>() : null;

            _activeContentPanel = infectPanel;
            ShowOverlay();

            yield return new WaitForSeconds(1.1f);
            if (traceTmp != null)
            {
                traceTmp.text =
                    "<color=#FF5A4E><b>1/3 [OK] Descarga completa (.scr)</b></color>\n\n" +
                    "<color=#FF3834><b>2/3 Backdoor conectado a IP 185.130.44.82</b></color>";
            }
            tBg.color = new Color(0.28f, 0.03f, 0.03f, 0.98f);
            yield return new WaitForSeconds(1.2f);

            if (traceTmp != null)
            {
                traceTmp.text =
                    "<color=#FF5A4E><b>1/3 [OK] Descarga completa (.scr)</b></color>\n\n" +
                    "<color=#FF3834><b>2/3 [OK] Backdoor conectado a C2</b></color>\n\n" +
                    "<color=#FFC24A><b>3/3 Salto a PC 2 (SWIFT) y PC 3 (SIEM)...</b></color>";
            }
            yield return new WaitForSeconds(1.4f);

            ClearActiveContent();

            _headerTitleText.text = "<b><color=#FF3834>ROBO CONSUMADO: $81M TRANSFERIDOS</color></b>";
            _subTitleText.text = "Red plana sin VLAN ni MFA permitio el ataque";

            GameObject robPanel = new GameObject("RobberyResultPanel");
            robPanel.transform.SetParent(_contentContainer, false);
            RectTransform rRect = robPanel.AddComponent<RectTransform>();
            rRect.anchorMin = Vector2.zero;
            rRect.anchorMax = Vector2.one;
            rRect.sizeDelta = Vector2.zero;

            GameObject rBox = new GameObject("RobberyBox");
            rBox.transform.SetParent(robPanel.transform, false);
            RectTransform rbRect = rBox.AddComponent<RectTransform>();
            rbRect.anchoredPosition = new Vector2(0f, 35f);
            rbRect.sizeDelta = new Vector2(880f, 300f);
            Image rbBg = rBox.AddComponent<Image>();
            rbBg.color = new Color(0.10f, 0.02f, 0.02f, 0.98f);

            StringBuilder rsb = new StringBuilder();
            rsb.AppendLine("<color=#FF3834><b>- ROBADO:</b> US$ 81,000,000 (lavado en Filipinas)</color>");
            rsb.AppendLine("- RETENIDO: US$ 870,000,000 (frenado por error)");
            rsb.AppendLine("- ENTRADA: Phishing .scr.zip en PC 1");
            rsb.AppendLine("<color=#FFC24A><b>- CAUSA: Sin Sandbox, sin VLAN, sin MFA</b></color>");

            CreateLabel("Txt_RobberySummary", rBox.transform, Vector2.zero, new Vector2(830f, 260f),
                rsb.ToString(), 20.5f, TextAlignmentOptions.MidlineLeft, Color.white);

            CreateInspectionButton(
                "Btn_GoBoardAfterAttack", robPanel.transform,
                new Vector2(-300f, -175f), new Vector2(280f, 62f),
                "<b>IR AL TABLERO ></b>",
                new Color(0.10f, 0.30f, 0.40f, 0.98f),
                CyanNeonAccent,
                NavigateToSuspectBoardOrDefense
            );

            CreateInspectionButton(
                "Btn_FinalizeAttack", robPanel.transform,
                new Vector2(0f, -175f), new Vector2(290f, 62f),
                "<b><color=#FF5A4E>FINALIZAR EVALUACION</color></b>",
                new Color(0.48f, 0.12f, 0.12f, 0.98f),
                Color.white,
                () =>
                {
                    CloseOverlay();
                    if (GameManager.I != null)
                    {
                        GameManager.I.EndScenario();
                    }
                }
            );

            CreateInspectionButton(
                "Btn_CloseRobbery", robPanel.transform,
                new Vector2(300f, -175f), new Vector2(250f, 62f),
                "<b>CERRAR VISTA</b>",
                new Color(0.20f, 0.25f, 0.35f, 0.98f),
                Color.white,
                CloseOverlay
            );

            _activeContentPanel = robPanel;
            ShowOverlay();
        }

        /// <summary>
        /// RF-05: Abre la vista ampliada del Post-It de credenciales en PC 1.
        /// </summary>
        public void OpenCredentialsInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b>EVIDENCIA #2: POST-IT CON CLAVES</b>";
            _subTitleText.text = "Puesto 1 - Contrasenas escritas a la vista";

            GameObject credPanel = new GameObject("CredentialsContentPanel");
            credPanel.transform.SetParent(_contentContainer, false);
            RectTransform cRect = credPanel.AddComponent<RectTransform>();
            cRect.anchorMin = Vector2.zero;
            cRect.anchorMax = Vector2.one;
            cRect.sizeDelta = Vector2.zero;

            GameObject postItBox = new GameObject("PostItVisual");
            postItBox.transform.SetParent(credPanel.transform, false);
            RectTransform pRect = postItBox.AddComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(-210f, 30f);
            pRect.sizeDelta = new Vector2(410f, 290f);
            Image postItBg = postItBox.AddComponent<Image>();
            postItBg.color = new Color(0.98f, 0.88f, 0.35f, 0.98f);

            CreateLabel("Txt_PostItTitle", postItBox.transform, new Vector2(0f, 100f), new Vector2(370f, 40f),
                "<b>CLAVES PC-PAGOS-01</b>", 20f, TextAlignmentOptions.Center, new Color(0.1f, 0.1f, 0.1f));

            CreateLabel("Txt_PostItBody", postItBox.transform, new Vector2(0f, -20f), new Vector2(370f, 190f),
                "<b>User:</b> c.mendoza\n" +
                "<b>Pass:</b> <color=#990000>Swift2015$Admin!</color>\n" +
                "<b>Token:</b> 849201\n" +
                "<b>PIN:</b> 9481",
                19.5f, TextAlignmentOptions.MidlineLeft, new Color(0.12f, 0.12f, 0.12f));

            GameObject diagBox = new GameObject("ForensicDiagBox");
            diagBox.transform.SetParent(credPanel.transform, false);
            RectTransform dRect = diagBox.AddComponent<RectTransform>();
            dRect.anchoredPosition = new Vector2(225f, 30f);
            dRect.sizeDelta = new Vector2(410f, 290f);
            Image diagBg = diagBox.AddComponent<Image>();
            diagBg.color = new Color(0.08f, 0.12f, 0.18f, 0.96f);

            CreateLabel("Txt_DiagTitle", diagBox.transform, new Vector2(0f, 100f), new Vector2(370f, 40f),
                "<b>FALLA ISO 27001</b>", 20f, TextAlignmentOptions.Center, WarningAmberColor);

            CreateLabel("Txt_DiagBody", diagBox.transform, new Vector2(0f, -20f), new Vector2(370f, 190f),
                "- Clave visible en papel.\n\n" +
                "- Cualquiera puede usar la cuenta SWIFT sin descifrar nada.",
                19f, TextAlignmentOptions.MidlineLeft, new Color(0.90f, 0.95f, 1f));

            Action saveEvidence = () =>
            {
                if (GameManager.I != null) GameManager.I.RegisterEvidence("Evidencia_PostIt");
                if (AndesSim.Audio.AudioManager.I != null) AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
            };

            CreateInspectionButton(
                "Btn_SaveAndNextSiem", credPanel.transform,
                new Vector2(-210f, -175f), new Vector2(420f, 62f),
                "<b>GUARDAR Y VER SIEM (PC 3) ></b>",
                new Color(0.12f, 0.42f, 0.24f, 0.98f),
                GreenSuccessColor,
                () => { saveEvidence(); OpenSiemLogInspection(); }
            );

            CreateInspectionButton(
                "Btn_LogEvidencePostIt", credPanel.transform,
                new Vector2(225f, -175f), new Vector2(380f, 62f),
                "<b>SOLO GUARDAR Y CERRAR</b>",
                new Color(0.16f, 0.26f, 0.36f, 0.98f),
                Color.white,
                () => { saveEvidence(); CloseOverlay(); }
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

            _headerTitleText.text = "<b>PC 2 - TERMINAL SWIFT</b>";
            _subTitleText.text = "Transferencias internacionales y red sin proteccion";

            GameObject swiftPanel = new GameObject("SwiftContentPanel");
            swiftPanel.transform.SetParent(_contentContainer, false);
            RectTransform sRect = swiftPanel.AddComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.sizeDelta = Vector2.zero;

            GameObject tableBox = new GameObject("SwiftTableBox");
            tableBox.transform.SetParent(swiftPanel.transform, false);
            RectTransform tbRect = tableBox.AddComponent<RectTransform>();
            tbRect.anchoredPosition = new Vector2(0f, 35f);
            tbRect.sizeDelta = new Vector2(880f, 300f);
            Image tbBg = tableBox.AddComponent<Image>();
            tbBg.color = new Color(0.06f, 0.1f, 0.16f, 0.96f);

            CreateLabel("Txt_SwiftTable", tableBox.transform, Vector2.zero, new Vector2(830f, 260f),
                "<b><color=#00F2FE>OPERACION SWIFT MT103</color></b>\n\n" +
                "- Destino sospechoso: RCBC Filipinas\n" +
                "- En riesgo: <color=#FF5A4E><b>US$ 951,000,000</b></color>\n" +
                "- Falla: <color=#FFC24A>Red plana sin VLAN ni codigo MFA</color>",
                20.5f, TextAlignmentOptions.MidlineLeft, Color.white);

            CreateInspectionButton(
                "Btn_SwiftToMail", swiftPanel.transform,
                new Vector2(-220f, -175f), new Vector2(410f, 62f),
                "<b>< VOLVER A CORREO (PC 1)</b>",
                new Color(0.14f, 0.24f, 0.36f, 0.98f),
                Color.white,
                () => OpenEmailInspection(0)
            );

            CreateInspectionButton(
                "Btn_SwiftToSiem", swiftPanel.transform,
                new Vector2(220f, -175f), new Vector2(410f, 62f),
                "<b>SIGUIENTE: VER SIEM (PC 3) ></b>",
                new Color(0.10f, 0.34f, 0.26f, 0.98f),
                GreenSuccessColor,
                OpenSiemLogInspection
            );

            _activeContentPanel = swiftPanel;
            ShowOverlay();
        }

        /// <summary>
        /// RF-05: Abre la consola SIEM (Puesto 3) con los registros hacia la IP atacante.
        /// </summary>
        public void OpenSiemLogInspection()
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            _headerTitleText.text = "<b>PC 3 - EVIDENCIA #3: CONSOLA SIEM</b>";
            _subTitleText.text = "Alertas de conexion hacia IP atacante";

            GameObject siemPanel = new GameObject("SiemContentPanel");
            siemPanel.transform.SetParent(_contentContainer, false);
            RectTransform smRect = siemPanel.AddComponent<RectTransform>();
            smRect.anchorMin = Vector2.zero;
            smRect.anchorMax = Vector2.one;
            smRect.sizeDelta = Vector2.zero;

            GameObject logBox = new GameObject("SiemLogBox");
            logBox.transform.SetParent(siemPanel.transform, false);
            RectTransform lRect = logBox.AddComponent<RectTransform>();
            lRect.anchoredPosition = new Vector2(0f, 35f);
            lRect.sizeDelta = new Vector2(880f, 300f);
            Image lBg = logBox.AddComponent<Image>();
            lBg.color = new Color(0.04f, 0.08f, 0.12f, 0.96f);

            CreateLabel("Txt_SiemLogs", logBox.transform, Vector2.zero, new Vector2(830f, 260f),
                "<b><color=#38F28A>REGISTROS DE RED (29 ENE 2015)</color></b>\n\n" +
                "- 09:18 C2: <color=#FF5A4E>192.168.1.101 -> 185.130.44.82:443</color>\n" +
                "- Proceso: svchost_upd.exe (Malware Lazarus)\n" +
                "- Alerta: Descarga .scr sin Sandbox",
                20.5f, TextAlignmentOptions.MidlineLeft, Color.white);

            Action saveSiem = () =>
            {
                if (GameManager.I != null) GameManager.I.RegisterEvidence("Evidencia_LogAccesos");
                if (AndesSim.Audio.AudioManager.I != null) AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
            };

            CreateInspectionButton(
                "Btn_LogSiemAndBoard", siemPanel.transform,
                new Vector2(-220f, -175f), new Vector2(430f, 62f),
                "<b>GUARDAR LOG Y IR AL TABLERO ></b>",
                new Color(0.12f, 0.42f, 0.24f, 0.98f),
                GreenSuccessColor,
                () => { saveSiem(); NavigateToSuspectBoardOrDefense(); }
            );

            CreateInspectionButton(
                "Btn_LogEvidenceSiem", siemPanel.transform,
                new Vector2(230f, -175f), new Vector2(370f, 62f),
                "<b>SOLO GUARDAR Y CERRAR</b>",
                new Color(0.16f, 0.26f, 0.36f, 0.98f),
                Color.white,
                () => { saveSiem(); CloseOverlay(); }
            );

            _activeContentPanel = siemPanel;
            ShowOverlay();
        }

        /// <summary>
        /// RF-05: Abre el expediente resumido del sospechoso con navegacion Anterior / Siguiente.
        /// </summary>
        public void OpenSuspectDossierInspection(int suspectIndex)
        {
            EnsureOverlayCanvas();
            ClearActiveContent();

            GameObject dossierPanel = new GameObject("SuspectDossierModalPanel");
            dossierPanel.transform.SetParent(_contentContainer, false);
            RectTransform dr = dossierPanel.AddComponent<RectTransform>();
            dr.sizeDelta = new Vector2(920f, 520f);

            SuspectBoardVR board = SuspectBoardVR.I != null ? SuspectBoardVR.I : FindFirstObjectByType<SuspectBoardVR>();
            int total = (board != null && board.Suspects != null && board.Suspects.Count > 0) ? board.Suspects.Count : 1;
            int safeIndex = ((suspectIndex % total) + total) % total;

            SuspectDossierScenario01 suspect = (board != null && board.Suspects != null && safeIndex < board.Suspects.Count)
                ? board.Suspects[safeIndex]
                : null;

            string name = suspect != null ? suspect.fullName : "Sospechoso";
            string role = suspect != null ? suspect.roleTitle : "Personal Banco Andino";
            string badge = suspect != null ? suspect.threatLevelBadge : "INVESTIGACION";
            string incrim = suspect != null ? suspect.incriminatingPoints : "Sin indicios.";
            string exon = suspect != null ? suspect.exoneratingPoints : "Sin descargos.";

            _headerTitleText.text = $"<b>SOSPECHOSO #{safeIndex + 1} DE {total}</b>";
            _subTitleText.text = $"<b>{name}</b> - {role}  <color=#FF5A4E>[{badge}]</color>";

            GameObject leftBox = new GameObject("LeftBox");
            leftBox.transform.SetParent(dossierPanel.transform, false);
            RectTransform lbRect = leftBox.AddComponent<RectTransform>();
            lbRect.anchoredPosition = new Vector2(-225f, 35f);
            lbRect.sizeDelta = new Vector2(430f, 290f);
            Image lbBg = leftBox.AddComponent<Image>();
            lbBg.color = new Color(0.14f, 0.06f, 0.06f, 0.96f);

            CreateLabel("Txt_LeftTitle", leftBox.transform, new Vector2(0f, 115f), new Vector2(390f, 36f),
                "<b><color=#FF5A4E>EN CONTRA</color></b>",
                19f, TextAlignmentOptions.MidlineLeft, WarningAmberColor);

            CreateLabel("Txt_LeftContent", leftBox.transform, new Vector2(0f, -15f), new Vector2(390f, 210f),
                incrim, 17.5f, TextAlignmentOptions.TopLeft, new Color(0.96f, 0.92f, 0.92f));

            GameObject rightBox = new GameObject("RightBox");
            rightBox.transform.SetParent(dossierPanel.transform, false);
            RectTransform rbRect = rightBox.AddComponent<RectTransform>();
            rbRect.anchoredPosition = new Vector2(225f, 35f);
            rbRect.sizeDelta = new Vector2(430f, 290f);
            Image rbBg = rightBox.AddComponent<Image>();
            rbBg.color = new Color(0.06f, 0.14f, 0.09f, 0.96f);

            CreateLabel("Txt_RightTitle", rightBox.transform, new Vector2(0f, 115f), new Vector2(390f, 36f),
                "<b><color=#38F28A>A FAVOR</color></b>",
                19f, TextAlignmentOptions.MidlineLeft, GreenSuccessColor);

            CreateLabel("Txt_RightContent", rightBox.transform, new Vector2(0f, -15f), new Vector2(390f, 210f),
                exon, 17.5f, TextAlignmentOptions.TopLeft, new Color(0.90f, 0.98f, 0.92f));

            CreateInspectionButton(
                "Btn_PrevSuspect", dossierPanel.transform,
                new Vector2(-345f, -175f), new Vector2(180f, 60f),
                "<b>< ANTERIOR</b>",
                new Color(0.16f, 0.22f, 0.34f, 0.96f),
                Color.white,
                () => OpenSuspectDossierInspection((safeIndex + total - 1) % total)
            );

            CreateInspectionButton(
                "Btn_SelectHypothesis", dossierPanel.transform,
                new Vector2(0f, -175f), new Vector2(470f, 60f),
                $"<b>ELEGIR: {name.ToUpper()}</b>",
                new Color(0.12f, 0.38f, 0.22f, 0.98f),
                GreenSuccessColor,
                () =>
                {
                    if (board != null)
                    {
                        board.DisplaySuspect(safeIndex);
                        board.OnSelectHypothesisClicked();
                    }
                    if (AndesSim.Audio.AudioManager.I != null)
                    {
                        AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
                    }
                    CloseOverlay();
                }
            );

            CreateInspectionButton(
                "Btn_NextSuspect", dossierPanel.transform,
                new Vector2(345f, -175f), new Vector2(180f, 60f),
                "<b>SIGUIENTE ></b>",
                new Color(0.16f, 0.22f, 0.34f, 0.96f),
                Color.white,
                () => OpenSuspectDossierInspection((safeIndex + 1) % total)
            );

            _activeContentPanel = dossierPanel;
            ShowOverlay();
        }

        private void NavigateToSuspectBoardOrDefense()
        {
            GameObject tpObj = GameObject.Find("Teleport_TableroSospechosos");
            TeleportPoint tp = tpObj != null ? tpObj.GetComponent<TeleportPoint>() : null;
            if (PlayerTeleportController.I != null && tp != null)
            {
                CloseOverlay();
                PlayerTeleportController.I.TeleportToPoint(tp);
                return;
            }
            OpenSuspectDossierInspection(0);
        }

        private void NavigateToDefenseConsole()
        {
            GameObject tpObj = GameObject.Find("Teleport_ConsolaDefensa");
            TeleportPoint tp = tpObj != null ? tpObj.GetComponent<TeleportPoint>() : null;
            if (PlayerTeleportController.I != null && tp != null)
            {
                CloseOverlay();
                PlayerTeleportController.I.TeleportToPoint(tp);
                return;
            }
            NavigateToSuspectBoardOrDefense();
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
                    if (_overlayCanvas != null) _overlayCanvas.worldCamera = _targetCamera;

                    Vector3 forward = _targetCamera.transform.forward;
                    forward.y = 0f;
                    if (forward.sqrMagnitude < 0.001f) forward = _targetCamera.transform.forward;
                    forward.Normalize();

                    Vector3 spawnPos = _targetCamera.transform.position + (forward * distanceFromCamera);
                    spawnPos.y = _targetCamera.transform.position.y - 0.05f;

                    _overlayRoot.transform.position = spawnPos;
                    _overlayRoot.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
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
            StopAllCoroutines();
            _isOpen = false;
            if (_overlayRoot != null)
            {
                _overlayRoot.SetActive(false);
            }
            ClearActiveContent();

            if (AndesSim.Audio.AudioManager.I != null)
            {
                AndesSim.Audio.AudioManager.I.PlayGazeClick();
            }
        }

        private void ClearActiveContent()
        {
            if (_contentContainer != null)
            {
                for (int i = _contentContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = _contentContainer.GetChild(i);
                    child.gameObject.SetActive(false);
                    child.SetParent(null);
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
            if (_activeContentPanel != null)
            {
                _activeContentPanel.SetActive(false);
                _activeContentPanel.transform.SetParent(null);
                if (Application.isPlaying)
                {
                    Destroy(_activeContentPanel);
                }
                else
                {
                    DestroyImmediate(_activeContentPanel);
                }
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
            int lastInvokedFrame = -1;
            Action invokeOncePerFrame = () =>
            {
                if (onClick == null || lastInvokedFrame == Time.frameCount) return;
                lastInvokedFrame = Time.frameCount;
                onClick.Invoke();
            };
            if (onClick != null) btn.onClick.AddListener(() => invokeOncePerFrame());

            // Colisionador BoxCollider para detección precisa con retícula VR
            BoxCollider boxCol = btnGO.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(size.x, size.y, 45f);
            boxCol.center = new Vector3(0f, 0f, -15f);
            boxCol.isTrigger = true;

            // Añadir soporte de retícula VR
            GazeInteractable gaze = btnGO.AddComponent<GazeInteractable>();
            gaze.interactableLabel = text.Replace("<b>", "").Replace("</b>", "").Split('\n')[0];
            gaze.dwellTime = 1.1f;
            if (onClick != null) gaze.onClick.AddListener(() => invokeOncePerFrame());

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            RectTransform tRect = txtGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16.5f;
            tmp.fontStyle = FontStyles.Bold;
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

            // Colisionador BoxCollider calibrado para el raycast de retícula VR
            BoxCollider boxCol = btnGO.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(size.x, size.y, 25f);
            boxCol.center = new Vector3(0f, 0f, -5f);
            boxCol.isTrigger = true;

            GazeInteractable gaze = btnGO.AddComponent<GazeInteractable>();
            gaze.interactableLabel = $"Inspeccionar {signalId}";
            gaze.dwellTime = 1.0f;

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            RectTransform tRect = txtGO.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = WarningAmberColor;

            int lastMarkedFrame = -1;
            Action markAction = () =>
            {
                if (lastMarkedFrame == Time.frameCount) return;
                lastMarkedFrame = Time.frameCount;

                if (ScenarioFlowController.I != null)
                {
                    ScenarioFlowController.I.NotifyPhishingSignalInspected(signalId);
                }
                if (PhishingInspector.I != null)
                {
                    PhishingInspector.I.MarkPhishingSignal(signalId);
                }
                if (AndesSim.Audio.AudioManager.I != null)
                {
                    AndesSim.Audio.AudioManager.I.PlayEvidenceFound();
                }
                bg.color = new Color(0.08f, 0.35f, 0.18f, 0.95f);
                tmp.text = "<b>[OK] PISTA LISTA</b>";
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

        private void EnsureEventSystemExists()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null && FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }
        }

        #endregion
    }
}
