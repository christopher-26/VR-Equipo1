using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Environment;
using AndesSim.UI;
using AndesSim.VR;

namespace AndesSim.Editor
{
    /// <summary>
    /// Constructor modular del Centro Holográfico Flotante de 5 Pestañas, balizas flotantes sobre estaciones
    /// y aros holográficos en puntos de teletransporte.
    /// </summary>
    public static class OficinaPagosMenuAndFxBuilder
    {
        public static GameObject BuildFloatingHoloCommandHub(Transform parent)
        {
            GameObject menuRoot = new GameObject("MenuInicio_Simple_VR");
            menuRoot.transform.SetParent(parent, false);
            menuRoot.transform.position = new Vector3(-2.60f, 1.48f, 2.35f);
            menuRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            menuRoot.layer = LayerMask.NameToLayer("Interactive");

            // =========================================================================
            // 1. Emisor Sutil en el Piso (Sin aros giratorios ni elementos voladores)
            // =========================================================================
            Material matHoloMatBase = CreateUnlitGlowMaterial("Mat_MenuHolo_Emitter", new Color(0.05f, 0.14f, 0.22f), new Color(0f, 0.45f, 0.65f));

            GameObject emitterPad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            emitterPad.name = "Holo_Emisor_Piso";
            emitterPad.transform.SetParent(menuRoot.transform, false);
            emitterPad.transform.localPosition = new Vector3(0f, -1.44f, 0f);
            emitterPad.transform.localScale = new Vector3(0.72f, 0.015f, 0.72f);
            RemoveCollider(emitterPad);
            emitterPad.GetComponent<Renderer>().sharedMaterial = matHoloMatBase;

            // =========================================================================
            // 2. Contenedor Estable del Menú (Sin levitación)
            // =========================================================================
            GameObject floatWrapper = new GameObject("Holo_MenuFloatFrame");
            floatWrapper.transform.SetParent(menuRoot.transform, false);
            floatWrapper.transform.localPosition = Vector3.zero;

            GameObject canvasObj = new GameObject("Canvas_MenuInicio_Simple");
            canvasObj.transform.SetParent(floatWrapper.transform, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform cRect = canvasObj.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(1020f, 650f);
            cRect.localScale = new Vector3(0.00182f, 0.00182f, 0.00182f);
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasGroup canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            HolographicPanelEntrance rootEntrance = canvasObj.AddComponent<HolographicPanelEntrance>();
            rootEntrance.Configure(0.14f, 1f, 0f, canvasGroup);

            // Fondo Glass Profundo
            GameObject bgObj = new GameObject("Panel_Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = AndesTypographyTheme.BgVoidNavy;

            // Marco Neon
            AddUiEdgeBar("Border_Top", bgObj.transform, new Vector2(0f, 322f), new Vector2(1020f, 6f), AndesTypographyTheme.AccentCyan);
            AddUiEdgeBar("Border_Bottom", bgObj.transform, new Vector2(0f, -322f), new Vector2(1020f, 4f), AndesTypographyTheme.AccentEmerald);
            AddUiEdgeBar("Border_Left", bgObj.transform, new Vector2(-508f, 0f), new Vector2(4f, 650f), AndesTypographyTheme.AccentCyan * 0.75f);
            AddUiEdgeBar("Border_Right", bgObj.transform, new Vector2(508f, 0f), new Vector2(4f, 650f), AndesTypographyTheme.AccentCyan * 0.75f);

            // Título y Subtítulo Centrados
            TextMeshProUGUI titleTmp = CreateCenteredText("Title", canvasObj.transform, new Vector2(0f, 286f), new Vector2(960f, 42f),
                "<color=#00E5FF>BANCO ANDINO</color> · SOC VR", 27f, Color.white);
            AndesTypographyTheme.StyleDisplayTitle(titleTmp, Color.white, 27f);

            TextMeshProUGUI subTmp = CreateCenteredText("SubTitle", canvasObj.transform, new Vector2(0f, 250f), new Vector2(960f, 28f),
                "Caso Lazarus 2015", 16f, AndesTypographyTheme.TextMuted);
            AndesTypographyTheme.StyleBodyCentered(subTmp, AndesTypographyTheme.TextMuted, 16f, 6f);

            // =========================================================================
            // 3. Barra Superior de 5 Pestañas Holográficas
            // =========================================================================
            string[] tabTitles = { "1. INICIO", "2. MAPA", "3. SOSPECHOSOS", "4. GUIA", "5. AJUSTES" };
            Button[] tabButtons = new Button[5];
            Image[] tabBgs = new Image[5];
            TextMeshProUGUI[] tabLabels = new TextMeshProUGUI[5];
            GameObject[] tabPanels = new GameObject[5];

            for (int i = 0; i < 5; i++)
            {
                float xPos = (i - 2) * 188f;
                GameObject tabBtnObj = CreateMenuButton($"Btn_MenuTab_{i}", canvasObj.transform,
                    new Vector2(xPos, 204f), new Vector2(176f, 42f),
                    tabTitles[i],
                    i == 0 ? AndesTypographyTheme.AccentCyan : new Color(0.08f, 0.16f, 0.27f, 0.94f),
                    i == 0 ? new Color(0.03f, 0.08f, 0.14f, 1f) : AndesTypographyTheme.TextPrimary,
                    14.5f);

                tabButtons[i] = tabBtnObj.GetComponent<Button>();
                tabBgs[i] = tabBtnObj.GetComponent<Image>();
                tabLabels[i] = tabBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            }

            // =========================================================================
            // TAB 0: INICIO & MODOS
            // =========================================================================
            GameObject tab0 = CreateTabContainer("Tab_0_Inicio", canvasObj.transform);
            tabPanels[0] = tab0;

            GameObject panelModos = new GameObject("Panel_VistaModos");
            panelModos.transform.SetParent(tab0.transform, false);
            RectTransform pmR = panelModos.AddComponent<RectTransform>();
            pmR.anchorMin = Vector2.zero; pmR.anchorMax = Vector2.one; pmR.sizeDelta = Vector2.zero;

            // Marco Modo Ataque
            GameObject frameAtk = new GameObject("Frame_ModoAtaque");
            frameAtk.transform.SetParent(panelModos.transform, false);
            RectTransform faRect = frameAtk.AddComponent<RectTransform>();
            faRect.anchoredPosition = new Vector2(-224f, 70f);
            faRect.sizeDelta = new Vector2(420f, 166f);
            Image imgAtkBorder = frameAtk.AddComponent<Image>();
            imgAtkBorder.color = AndesTypographyTheme.AccentRed;

            GameObject btnAtkObj = CreateMenuButton("Btn_SelectAtaque", frameAtk.transform,
                Vector2.zero, new Vector2(408f, 154f),
                "<size=22><color=#FF5A4E>MODO ATAQUE</color></size>\n<size=15><color=#E8F1FC>Simula el robo ($81M)</color></size>",
                new Color(0.15f, 0.05f, 0.07f, 0.98f), Color.white, 16f);
            Button btnAtk = btnAtkObj.GetComponent<Button>();

            // Marco Modo Defensa
            GameObject frameDef = new GameObject("Frame_ModoDefensa");
            frameDef.transform.SetParent(panelModos.transform, false);
            RectTransform fdRect = frameDef.AddComponent<RectTransform>();
            fdRect.anchoredPosition = new Vector2(224f, 70f);
            fdRect.sizeDelta = new Vector2(420f, 166f);
            Image imgDefBorder = frameDef.AddComponent<Image>();
            imgDefBorder.color = new Color(0.16f, 0.28f, 0.42f, 0.75f);

            GameObject btnDefObj = CreateMenuButton("Btn_SelectDefensa", frameDef.transform,
                Vector2.zero, new Vector2(408f, 154f),
                "<size=22><color=#38F28A>MODO DEFENSA</color></size>\n<size=15><color=#E8F1FC>Bloquea el phishing SOC</color></size>",
                new Color(0.04f, 0.15f, 0.10f, 0.98f), Color.white, 16f);
            Button btnDef = btnDefObj.GetComponent<Button>();

            TextMeshProUGUI badgeTmp = CreateCenteredText("Txt_ModoBadge", panelModos.transform, new Vector2(0f, -38f), new Vector2(880f, 34f),
                "ACTIVO · MODO ATAQUE", 19.5f, AndesTypographyTheme.AccentRed);
            AndesTypographyTheme.StyleDisplayTitle(badgeTmp, AndesTypographyTheme.AccentRed, 19.5f);

            TextMeshProUGUI descTmp = CreateCenteredText("Txt_ModoDescripcion", panelModos.transform, new Vector2(0f, -84f), new Vector2(880f, 44f),
                "PC 1 (Correo)  ->  PC 2 / PC 3  ->  Tablero",
                16.5f, AndesTypographyTheme.TextPrimary);

            // Subvista rápida de pasos (Alternada con Btn_TogglePasos)
            GameObject panelPasos = new GameObject("Panel_VistaPasos");
            panelPasos.transform.SetParent(tab0.transform, false);
            RectTransform ppR = panelPasos.AddComponent<RectTransform>();
            ppR.anchorMin = Vector2.zero; ppR.anchorMax = Vector2.one; ppR.sizeDelta = Vector2.zero;

            GameObject pasosCard = CreateGlassCard("Card_GuiaPasos", panelPasos.transform, new Vector2(0f, 18f), new Vector2(860f, 224f),
                AndesTypographyTheme.BgCardGlass, AndesTypographyTheme.AccentCyan);
            CreateCenteredText("Txt_GuiaPasos", pasosCard.transform, Vector2.zero, new Vector2(810f, 194f),
                "<color=#00E5FF><b>COMO JUGAR</b></color>\n\n" +
                "<b>1.</b> Revisa PC 1, Post-It y SIEM (PC 3)\n" +
                "<b>2.</b> Abre enlace o aisla en Sandbox\n" +
                "<b>3.</b> Elige al responsable en el Tablero",
                19f, Color.white);
            panelPasos.SetActive(false);

            // CTA Principal + Botones Inferiores Tab 0
            GameObject btnStartObj = CreateMenuButton("Btn_IniciarSimulacion", tab0.transform,
                new Vector2(0f, -158f), new Vector2(560f, 64f),
                "INICIAR SIMULACION",
                new Color(0.06f, 0.44f, 0.56f, 0.98f), AndesTypographyTheme.AccentCyan, 20f);
            Button btnStart = btnStartObj.GetComponent<Button>();

            GameObject btnPasosObj = CreateMenuButton("Btn_TogglePasos", tab0.transform,
                new Vector2(-215f, -238f), new Vector2(400f, 46f),
                "? GUIA RAPIDA",
                new Color(0.09f, 0.19f, 0.33f, 0.96f), AndesTypographyTheme.AccentCyan, 15.5f);
            Button btnPasos = btnPasosObj.GetComponent<Button>();
            TextMeshProUGUI txtPasosLabel = btnPasosObj.GetComponentInChildren<TextMeshProUGUI>();

            GameObject btnCreditosObj = CreateMenuButton("Btn_AbrirCreditos", tab0.transform,
                new Vector2(215f, -238f), new Vector2(400f, 46f),
                "CREDITOS Y ETICA",
                new Color(0.13f, 0.16f, 0.29f, 0.96f), AndesTypographyTheme.AccentSky, 15.5f);
            Button btnCreditos = btnCreditosObj.GetComponent<Button>();

            // =========================================================================
            // TAB 1: MAPA & ACCESO DIRECTO A PUESTOS
            // =========================================================================
            GameObject tab1 = CreateTabContainer("Tab_1_MapaYPuestos", canvasObj.transform);
            tabPanels[1] = tab1;

            CreateCenteredText("Lbl_MapaHeader", tab1.transform, new Vector2(0f, 134f), new Vector2(880f, 34f),
                "<color=#00E5FF><b>IR A UN PUESTO</b></color>", 18f, Color.white);

            string[] tpNames =
            {
                "Teleport_Puesto1_Carla",
                "Teleport_Puesto2_Analista",
                "Teleport_Puesto3_LogsYDefensa",
                "Teleport_TableroSospechosos"
            };
            string[] tpLabels =
            {
                "<color=#00E5FF>PC 1 · CARLA</color>\n<size=14><color=#ADC4DD>Correo y Post-It</color></size>",
                "<color=#FFA940>PC 2 · SWIFT</color>\n<size=14><color=#ADC4DD>Giros MT103 ($81M)</color></size>",
                "<color=#38F28A>PC 3 · SIEM</color>\n<size=14><color=#ADC4DD>Logs y Defensas</color></size>",
                "<color=#A78BFA>TABLERO</color>\n<size=14><color=#ADC4DD>4 Sospechosos</color></size>"
            };
            Vector2[] tpCoords =
            {
                new Vector2(-228f, 38f),
                new Vector2(228f, 38f),
                new Vector2(-228f, -108f),
                new Vector2(228f, -108f)
            };

            Button[] quickTpBtns = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject tpBtnObj = CreateMenuButton($"Btn_QuickTp_{i}", tab1.transform,
                    tpCoords[i], new Vector2(432f, 126f),
                    tpLabels[i], AndesTypographyTheme.BgCardGlass, Color.white, 17f);
                quickTpBtns[i] = tpBtnObj.GetComponent<Button>();
            }

            CreateCenteredText("Lbl_MapaHint", tab1.transform, new Vector2(0f, -220f), new Vector2(880f, 32f),
                "Al elegir un puesto inicia el reloj.",
                15f, AndesTypographyTheme.TextMuted);

            // =========================================================================
            // TAB 2: SOSPECHOSOS (4 FICHAS INTERACTIVAS EN VIVO)
            // =========================================================================
            GameObject tab2 = CreateTabContainer("Tab_2_Sospechosos", canvasObj.transform);
            tabPanels[2] = tab2;

            string[] suspShortNames = { "S. TORRES", "H. BENAVIDES", "C. MENDOZA", "R. ALIAGA" };
            Button[] suspButtons = new Button[4];
            Image[] suspBgs = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                float sx = -342f + i * 228f;
                GameObject sBtnObj = CreateMenuButton($"Btn_SuspectPill_{i}", tab2.transform,
                    new Vector2(sx, 126f), new Vector2(212f, 48f),
                    suspShortNames[i], new Color(0.08f, 0.14f, 0.24f, 0.96f), Color.white, 15f);
                suspButtons[i] = sBtnObj.GetComponent<Button>();
                suspBgs[i] = sBtnObj.GetComponent<Image>();
            }

            GameObject dossierPreviewCard = CreateGlassCard("Card_SuspectPreview", tab2.transform, new Vector2(0f, -14f), new Vector2(896f, 212f),
                AndesTypographyTheme.BgCardGlass, AndesTypographyTheme.AccentAmber);

            TextMeshProUGUI sPrevTitle = CreateCenteredText("Txt_SuspectPreviewTitle", dossierPreviewCard.transform,
                new Vector2(0f, 70f), new Vector2(840f, 36f), "ING. SAMUEL TORRES", 22f, Color.white);
            AndesTypographyTheme.StyleDisplayTitle(sPrevTitle, Color.white, 22f);

            TextMeshProUGUI sPrevRole = CreateCenteredText("Txt_SuspectPreviewRole", dossierPreviewCard.transform,
                new Vector2(0f, 34f), new Vector2(840f, 26f), "Jefe de Seguridad TI", 16f, AndesTypographyTheme.TextMuted);

            TextMeshProUGUI sPrevBadge = CreateCenteredText("Txt_SuspectPreviewBadge", dossierPreviewCard.transform,
                new Vector2(0f, -2f), new Vector2(840f, 26f), "NEGLIGENCIA", 15.5f, AndesTypographyTheme.AccentRed);
            AndesTypographyTheme.StyleBadge(sPrevBadge, AndesTypographyTheme.AccentRed, 15.5f);

            TextMeshProUGUI sPrevClues = CreateCenteredText("Txt_SuspectPreviewClues", dossierPreviewCard.transform,
                new Vector2(0f, -52f), new Vector2(830f, 76f),
                "· Administra red sin VLANs ni bloqueo .exe.", 18f, AndesTypographyTheme.TextPrimary);

            GameObject btnGoBoardMenuObj = CreateMenuButton("Btn_GoTableroTab2", tab2.transform,
                new Vector2(0f, -176f), new Vector2(480f, 56f),
                "IR AL TABLERO >",
                new Color(0.09f, 0.34f, 0.24f, 0.98f), AndesTypographyTheme.AccentEmerald, 17f);

            // =========================================================================
            // TAB 3: GUIA VR VISUAL (PASO A PASO + CONTROLES)
            // =========================================================================
            GameObject tab3 = CreateTabContainer("Tab_3_GuiaVR", canvasObj.transform);
            tabPanels[3] = tab3;

            CreateCenteredText("Lbl_GuiaTitle", tab3.transform, new Vector2(0f, 134f), new Vector2(880f, 34f),
                "<color=#00E5FF><b>GUIA EN 4 PASOS</b></color>", 18f, Color.white);

            string[] guideTitles = { "1 · PC 1", "2 · EVIDENCIA", "3 · ACCION", "4 · TABLERO" };
            string[] guideBodies =
            {
                "Abre el correo y marca 3 pistas.",
                "Guarda Post-It y Log SIEM.",
                "Detona enlace o aisla en Sandbox.",
                "Elige al responsable."
            };
            Color[] guideColors =
            {
                AndesTypographyTheme.AccentCyan,
                AndesTypographyTheme.AccentAmber,
                AndesTypographyTheme.AccentSky,
                AndesTypographyTheme.AccentEmerald
            };

            for (int i = 0; i < 4; i++)
            {
                float gx = -342f + i * 228f;
                GameObject gCard = CreateGlassCard($"Card_GuiaPaso_{i}", tab3.transform, new Vector2(gx, -8f), new Vector2(214f, 214f),
                    AndesTypographyTheme.BgCardGlass, guideColors[i]);
                TextMeshProUGUI gt = CreateCenteredText($"Txt_GuiaTitulo_{i}", gCard.transform, new Vector2(0f, 64f), new Vector2(194f, 38f),
                    guideTitles[i], 15.5f, guideColors[i]);
                AndesTypographyTheme.StyleBadge(gt, guideColors[i], 15.5f);

                CreateCenteredText($"Txt_GuiaBody_{i}", gCard.transform, new Vector2(0f, -18f), new Vector2(190f, 128f),
                    guideBodies[i], 16.5f, AndesTypographyTheme.TextPrimary);
            }

            GameObject ctrlBar = CreateGlassCard("Card_ControlesVR", tab3.transform, new Vector2(0f, -176f), new Vector2(896f, 66f),
                AndesTypographyTheme.BgCardElevated, AndesTypographyTheme.AccentCyan);
            CreateCenteredText("Txt_ControlesVR", ctrlBar.transform, Vector2.zero, new Vector2(860f, 54f),
                "<b>CLIC MANUAL:</b> Selecciona todo a tu ritmo  ·  <b>AROS VERDES:</b> Teletransporte",
                16f, Color.white);

            // =========================================================================
            // TAB 4: AJUSTES, ILUMINACIÓN & FX 3D
            // =========================================================================
            GameObject tab4 = CreateTabContainer("Tab_4_AjustesYFx", canvasObj.transform);
            tabPanels[4] = tab4;

            // Fila 1: Control manual por clic
            CreateCenteredText("Lbl_DwellHeader", tab4.transform, new Vector2(0f, 146f), new Vector2(880f, 26f),
                "CONTROL DE INTERACCION", 14.5f, AndesTypographyTheme.TextMuted);

            GameObject btnDwell1 = CreateMenuButton("Btn_Dwell_Rapido", tab4.transform, new Vector2(-235f, 106f), new Vector2(220f, 44f),
                "1.0s · RAPIDO", AndesTypographyTheme.BgCardGlass, Color.white, 15f);
            GameObject btnDwell2 = CreateMenuButton("Btn_Dwell_Normal", tab4.transform, new Vector2(0f, 106f), new Vector2(220f, 44f),
                "1.6s · NORMAL", new Color(0.08f, 0.28f, 0.38f, 0.98f), AndesTypographyTheme.AccentCyan, 15f);
            GameObject btnDwell3 = CreateMenuButton("Btn_Dwell_Comodo", tab4.transform, new Vector2(235f, 106f), new Vector2(220f, 44f),
                "2.4s · COMODO", AndesTypographyTheme.BgCardGlass, Color.white, 15f);

            TextMeshProUGUI txtDwellBadge = CreateCenteredText("Txt_DwellActual", tab4.transform, new Vector2(0f, 66f), new Vector2(880f, 24f),
                "<b>SOLO CLIC MANUAL</b>", 14.5f, AndesTypographyTheme.AccentCyan);

            // Fila 2: Iluminación Dinámica de Oficina
            CreateCenteredText("Lbl_LightHeader", tab4.transform, new Vector2(0f, 28f), new Vector2(880f, 26f),
                "ILUMINACION", 14.5f, AndesTypographyTheme.TextMuted);

            GameObject btnL1 = CreateMenuButton("Btn_Light_Clara", tab4.transform, new Vector2(-296f, -12f), new Vector2(280f, 46f),
                "OFICINA CLARA", new Color(0.10f, 0.32f, 0.24f, 0.98f), AndesTypographyTheme.AccentEmerald, 15f);
            GameObject btnL2 = CreateMenuButton("Btn_Light_Cyan", tab4.transform, new Vector2(0f, -12f), new Vector2(280f, 46f),
                "SOC CYAN NEON", new Color(0.07f, 0.24f, 0.36f, 0.98f), AndesTypographyTheme.AccentCyan, 15f);
            GameObject btnL3 = CreateMenuButton("Btn_Light_Nocturna", tab4.transform, new Vector2(296f, -12f), new Vector2(280f, 46f),
                "CYBER NOCTURNO", new Color(0.16f, 0.14f, 0.32f, 0.98f), AndesTypographyTheme.AccentViolet, 15f);

            TextMeshProUGUI txtLightBadge = CreateCenteredText("Txt_LightingActual", tab4.transform, new Vector2(0f, -52f), new Vector2(880f, 24f),
                "AMBIENTE: <b>OFICINA CLARA</b>", 14.5f, AndesTypographyTheme.AccentEmerald);

            // Fila 3: Audio, Voz y Demo 3D Virus
            GameObject btnAudioObj = CreateMenuButton("Btn_ToggleAudio", tab4.transform, new Vector2(-296f, -112f), new Vector2(280f, 54f),
                "AUDIO: ON", new Color(0.10f, 0.20f, 0.32f, 0.96f), Color.white, 15f);
            TextMeshProUGUI txtAudioStat = btnAudioObj.GetComponentInChildren<TextMeshProUGUI>();

            GameObject btnVoiceObj = CreateMenuButton("Btn_TestVoice", tab4.transform, new Vector2(0f, -112f), new Vector2(280f, 54f),
                "PROBAR VOZ", new Color(0.08f, 0.26f, 0.38f, 0.96f), AndesTypographyTheme.AccentCyan, 15f);

            GameObject btnDemoVirusObj = CreateMenuButton("Btn_PreviewVirusFx", tab4.transform, new Vector2(296f, -112f), new Vector2(280f, 54f),
                "DEMO VIRUS 3D", new Color(0.42f, 0.10f, 0.13f, 0.98f), AndesTypographyTheme.AccentRed, 15f);

            // Fila 4: Créditos y Ética
            GameObject btnCreditsTab4 = CreateMenuButton("Btn_CreditsTab4", tab4.transform, new Vector2(0f, -196f), new Vector2(520f, 48f),
                "CREDITOS Y ETICA", new Color(0.14f, 0.18f, 0.32f, 0.96f), AndesTypographyTheme.AccentSky, 15.5f);

            // =========================================================================
            // 4. Enlazar todo en VrStartMenuController
            // =========================================================================
            VrStartMenuController startCtrl = menuRoot.AddComponent<VrStartMenuController>();
            startCtrl.TabPanels = tabPanels;
            startCtrl.TabButtons = tabButtons;
            startCtrl.TabButtonBgs = tabBgs;
            startCtrl.TabButtonLabels = tabLabels;

            startCtrl.BtnModoAtaque = btnAtk;
            startCtrl.BtnModoDefensa = btnDef;
            startCtrl.ImgModoAtaqueBorder = imgAtkBorder;
            startCtrl.ImgModoDefensaBorder = imgDefBorder;
            startCtrl.TxtModoSeleccionadoBadge = badgeTmp;
            startCtrl.TxtModoDescripcion = descTmp;
            startCtrl.BtnIniciarSimulacion = btnStart;
            startCtrl.BtnTogglePasos = btnPasos;
            startCtrl.TxtBtnTogglePasosLabel = txtPasosLabel;
            startCtrl.PanelVistaModos = panelModos;
            startCtrl.PanelVistaPasos = panelPasos;
            startCtrl.BtnAbrirCreditos = btnCreditos;

            // Conectar también botón de créditos del Tab 4 al mismo método vía listener persistente o en Setup?
            // Mejor aún: asignarlo limpiamente con UnityEventTools o delegando al mismo patrón usando BtnCreditsTab4
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                btnCreditsTab4.GetComponent<Button>().onClick, startCtrl.OpenCreditsModal);

            startCtrl.QuickTeleportButtons = quickTpBtns;
            startCtrl.QuickTeleportPointNames = tpNames;

            startCtrl.SuspectCardsButtons = suspButtons;
            startCtrl.SuspectCardBgs = suspBgs;
            startCtrl.SuspectPreviewTitle = sPrevTitle;
            startCtrl.SuspectPreviewRole = sPrevRole;
            startCtrl.SuspectPreviewBadge = sPrevBadge;
            startCtrl.SuspectPreviewClues = sPrevClues;
            startCtrl.BtnGoTableroFromMenu = btnGoBoardMenuObj.GetComponent<Button>();

            startCtrl.BtnDwellRapido = btnDwell1.GetComponent<Button>();
            startCtrl.BtnDwellEstandar = btnDwell2.GetComponent<Button>();
            startCtrl.BtnDwellAccesible = btnDwell3.GetComponent<Button>();
            startCtrl.TxtDwellActualBadge = txtDwellBadge;
            startCtrl.BtnToggleAudio = btnAudioObj.GetComponent<Button>();
            startCtrl.TxtAudioStatus = txtAudioStat;
            startCtrl.BtnTestVoice = btnVoiceObj.GetComponent<Button>();
            startCtrl.BtnLightClara = btnL1.GetComponent<Button>();
            startCtrl.BtnLightCyan = btnL2.GetComponent<Button>();
            startCtrl.BtnLightNocturna = btnL3.GetComponent<Button>();
            startCtrl.TxtLightingBadge = txtLightBadge;
            startCtrl.BtnPreviewVirusFx = btnDemoVirusObj.GetComponent<Button>();

            startCtrl.MenuCanvasGroup = canvasGroup;
            startCtrl.HoloFloatingRoot = floatWrapper.transform;

            GameObject tp1Obj = GameObject.Find("Teleport_Puesto1_Carla");
            if (tp1Obj != null)
            {
                startCtrl.TeleportDestination = tp1Obj.GetComponent<TeleportPoint>();
            }

            AndesTypographyTheme.ApplyTypographyToTree(menuRoot.transform, true, true);
            for (int t = 1; t < tabPanels.Length; t++)
            {
                if (tabPanels[t] != null) tabPanels[t].SetActive(false);
            }
            Debug.Log("[ANDES-SIM - SceneAssembler] Centro Holográfico Flotante de 5 Pestañas construido exitosamente.");
            return menuRoot;
        }

        /// <summary>
        /// Agrega una baliza holográfica 3D flotante sobre cada estación de trabajo o consola de defensa.
        /// </summary>
        public static GameObject AttachWorkstationHoloBeacon(Transform parent, string name, Vector3 worldPos, Color initialColor, string tagLine)
        {
            // Se omiten las balizas/banners flotantes sobre escritorios para evitar obstrucciones y distracciones visuales
            return null;
        }

        /// <summary>
        /// Mantiene el aro plano limpio en el suelo sin gemas ni aros orbitales volando encima.
        /// </summary>
        public static void EnhanceTeleportPointHalo(GameObject tpObj, Color ringColor)
        {
        }

        private static GameObject CreateTabContainer(string name, Transform parent)
        {
            GameObject tab = new GameObject(name);
            tab.transform.SetParent(parent, false);
            RectTransform rt = tab.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            CanvasGroup cg = tab.AddComponent<CanvasGroup>();
            HolographicPanelEntrance ent = tab.AddComponent<HolographicPanelEntrance>();
            ent.Configure(0.14f, 1f, 0f, cg);

            return tab;
        }

        private static GameObject CreateGlassCard(string name, Transform parent, Vector2 pos, Vector2 size, Color bgColor, Color topAccentColor)
        {
            GameObject card = new GameObject(name);
            card.transform.SetParent(parent, false);
            RectTransform r = card.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            Image bg = card.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = false;

            AddUiEdgeBar("TopAccent", card.transform, new Vector2(0f, size.y * 0.5f - 2.5f), new Vector2(size.x, 5f), topAccentColor);
            return card;
        }

        private static GameObject CreateMenuButton(string name, Transform parent, Vector2 pos, Vector2 size,
            string text, Color bgColor, Color textColor, float fontSize)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.layer = LayerMask.NameToLayer("Interactive");

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.colorMultiplier = 1.18f;
            cb.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.highlightedColor = Color.white;
            cb.selectedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.pressedColor = new Color(0.68f, 0.68f, 0.68f, 1f);
            btn.colors = cb;

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = new Vector2(-16f, -8f);

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            AndesTypographyTheme.StyleButtonLabel(tmp, textColor, fontSize);

            BoxCollider col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 45f);
            col.center = new Vector3(0f, 0f, -15f);
            col.isTrigger = true;

            GazeInteractable gaze = btnObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = StripRichText(text);
            gaze.dwellTime = 1.2f;

            btnObj.AddComponent<WorldSpaceUiGazeButton>();
            btnObj.AddComponent<HolographicCardHover>().Configure(1.045f, 14f);

            return btnObj;
        }

        private static TextMeshProUGUI CreateCenteredText(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            AndesTypographyTheme.StyleBodyCentered(tmp, color, fontSize, 10f);
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void AddUiEdgeBar(string name, Transform parent, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(parent, false);
            RectTransform r = bar.AddComponent<RectTransform>();
            r.anchoredPosition = anchoredPos;
            r.sizeDelta = sizeDelta;
            Image img = bar.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static Material CreateUnlitGlowMaterial(string name, Color baseColor, Color emissionColor)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material m = new Material(s);
            m.name = name;
            m.color = baseColor;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emissionColor);
            return m;
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        private static string StripRichText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Botón VR";
            string firstLine = raw.Split('\n')[0];
            System.Text.StringBuilder sb = new System.Text.StringBuilder(firstLine.Length);
            bool inTag = false;
            for (int i = 0; i < firstLine.Length; i++)
            {
                char ch = firstLine[i];
                if (ch == '<') { inTag = true; continue; }
                if (ch == '>') { inTag = false; continue; }
                if (!inTag) sb.Append(ch);
            }
            return sb.ToString().Trim();
        }
    }
}
