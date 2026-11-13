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
            // 1. Emisor Holográfico Sutil en el Piso + Doble Aro Orbital (Sin columna gris)
            // =========================================================================
            Material matHoloMatBase = CreateUnlitGlowMaterial("Mat_MenuHolo_Emitter", new Color(0.05f, 0.14f, 0.22f), new Color(0f, 0.45f, 0.65f));
            Material matHoloCyan = CreateUnlitGlowMaterial("Mat_MenuHolo_CyanRing", AndesTypographyTheme.AccentCyan, AndesTypographyTheme.AccentCyan * 2.2f);
            Material matHoloEmerald = CreateUnlitGlowMaterial("Mat_MenuHolo_EmeraldRing", AndesTypographyTheme.AccentEmerald, AndesTypographyTheme.AccentEmerald * 1.9f);

            GameObject emitterPad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            emitterPad.name = "Holo_Emisor_Piso";
            emitterPad.transform.SetParent(menuRoot.transform, false);
            emitterPad.transform.localPosition = new Vector3(0f, -1.44f, 0f);
            emitterPad.transform.localScale = new Vector3(0.72f, 0.015f, 0.72f);
            RemoveCollider(emitterPad);
            emitterPad.GetComponent<Renderer>().sharedMaterial = matHoloMatBase;

            GameObject ringOuter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringOuter.name = "Holo_Anillo_Orbital_Exterior";
            ringOuter.transform.SetParent(menuRoot.transform, false);
            ringOuter.transform.localPosition = new Vector3(0f, -1.36f, 0f);
            ringOuter.transform.localRotation = Quaternion.Euler(4f, 0f, 3f);
            ringOuter.transform.localScale = new Vector3(0.92f, 0.008f, 0.92f);
            RemoveCollider(ringOuter);
            Renderer rOuter = ringOuter.GetComponent<Renderer>();
            rOuter.sharedMaterial = matHoloCyan;
            ringOuter.AddComponent<HolographicOrbitRing>().Configure(
                new Vector3(0f, 34f, 0f), rOuter, AndesTypographyTheme.AccentCyan, 1.1f, 2.4f, 1.8f);

            GameObject ringInner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringInner.name = "Holo_Anillo_Orbital_Interior";
            ringInner.transform.SetParent(menuRoot.transform, false);
            ringInner.transform.localPosition = new Vector3(0f, -1.25f, 0f);
            ringInner.transform.localRotation = Quaternion.Euler(-5f, 25f, -4f);
            ringInner.transform.localScale = new Vector3(0.64f, 0.007f, 0.64f);
            RemoveCollider(ringInner);
            Renderer rInner = ringInner.GetComponent<Renderer>();
            rInner.sharedMaterial = matHoloEmerald;
            ringInner.AddComponent<HolographicOrbitRing>().Configure(
                new Vector3(0f, -46f, 0f), rInner, AndesTypographyTheme.AccentEmerald, 0.9f, 2.1f, 2.1f);

            // =========================================================================
            // 2. Contenedor Flotante con Levitación Suave (HolographicFloatMotion)
            // =========================================================================
            GameObject floatWrapper = new GameObject("Holo_MenuFloatFrame");
            floatWrapper.transform.SetParent(menuRoot.transform, false);
            floatWrapper.transform.localPosition = Vector3.zero;
            HolographicFloatMotion menuFloat = floatWrapper.AddComponent<HolographicFloatMotion>();
            menuFloat.Configure(0.022f, 0.72f, new Vector3(0.55f, 0.85f, 0.35f), 0f, 0f, true);

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
            rootEntrance.Configure(0.32f, 0.88f, 22f, canvasGroup);

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
                "<color=#00E5FF>BANCO ANDINO</color> · CENTRO HOLOGRAFICO VR", 27f, Color.white);
            AndesTypographyTheme.StyleDisplayTitle(titleTmp, Color.white, 27f);

            TextMeshProUGUI subTmp = CreateCenteredText("SubTitle", canvasObj.transform, new Vector2(0f, 250f), new Vector2(960f, 28f),
                "Caso Histórico Lazarus 2015  ·  Simulador Forense y de Defensa SOC", 16f, AndesTypographyTheme.TextMuted);
            AndesTypographyTheme.StyleBodyCentered(subTmp, AndesTypographyTheme.TextMuted, 16f, 6f);

            // =========================================================================
            // 3. Barra Superior de 5 Pestañas Holográficas
            // =========================================================================
            string[] tabTitles = { "1. INICIO", "2. MAPA", "3. SOSPECHOSOS", "4. GUIA VR", "5. AJUSTES & FX" };
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
                "<size=20><color=#FF5A4E>MODO ATAQUE</color></size>\n<size=15><color=#E8F1FC>Observa el phishing y el robo\n($81M + virus 3D en vivo)</color></size>",
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
                "<size=20><color=#38F28A>MODO DEFENSA</color></size>\n<size=15><color=#E8F1FC>Marca las 3 señales, bloquea\ny activa escudos SOC</color></size>",
                new Color(0.04f, 0.15f, 0.10f, 0.98f), Color.white, 16f);
            Button btnDef = btnDefObj.GetComponent<Button>();

            TextMeshProUGUI badgeTmp = CreateCenteredText("Txt_ModoBadge", panelModos.transform, new Vector2(0f, -38f), new Vector2(880f, 34f),
                "MODO ACTIVO · ATAQUE LAZARUS", 19.5f, AndesTypographyTheme.AccentRed);
            AndesTypographyTheme.StyleDisplayTitle(badgeTmp, AndesTypographyTheme.AccentRed, 19.5f);

            TextMeshProUGUI descTmp = CreateCenteredText("Txt_ModoDescripcion", panelModos.transform, new Vector2(0f, -86f), new Vector2(880f, 58f),
                "1. Revisa el correo en PC 1   ·   2. Detona el enlace y observa el virus 3D\n3. Recoge Post-It y Log SIEM   ·   4. Identifica al responsable",
                15.5f, AndesTypographyTheme.TextPrimary);

            // Subvista rápida de pasos (Alternada con Btn_TogglePasos)
            GameObject panelPasos = new GameObject("Panel_VistaPasos");
            panelPasos.transform.SetParent(tab0.transform, false);
            RectTransform ppR = panelPasos.AddComponent<RectTransform>();
            ppR.anchorMin = Vector2.zero; ppR.anchorMax = Vector2.one; ppR.sizeDelta = Vector2.zero;

            GameObject pasosCard = CreateGlassCard("Card_GuiaPasos", panelPasos.transform, new Vector2(0f, 18f), new Vector2(860f, 224f),
                AndesTypographyTheme.BgCardGlass, AndesTypographyTheme.AccentCyan);
            CreateCenteredText("Txt_GuiaPasos", pasosCard.transform, Vector2.zero, new Vector2(810f, 194f),
                "<color=#00E5FF><b>COMO FUNCIONA EN 3 PASOS</b></color>\n\n" +
                "<b>1. INICIA Y MIRA PC 1:</b> Lee el correo de Rasel Ahlam y el Post-It amarillo.\n" +
                "<b>2. DECIDE E INVESTIGA:</b> Abre el enlace o aísla en Sandbox y revisa PC 2 y SIEM.\n" +
                "<b>3. TABLERO Y ESCUDOS:</b> Elige al responsable en el tablero y activa MFA.",
                17.5f, Color.white);
            panelPasos.SetActive(false);

            // CTA Principal + Botones Inferiores Tab 0
            GameObject btnStartObj = CreateMenuButton("Btn_IniciarSimulacion", tab0.transform,
                new Vector2(0f, -158f), new Vector2(560f, 64f),
                "INICIAR SIMULACION VR",
                new Color(0.06f, 0.44f, 0.56f, 0.98f), AndesTypographyTheme.AccentCyan, 20f);
            Button btnStart = btnStartObj.GetComponent<Button>();

            GameObject btnPasosObj = CreateMenuButton("Btn_TogglePasos", tab0.transform,
                new Vector2(-215f, -238f), new Vector2(400f, 46f),
                "[ ? COMO JUGAR ]",
                new Color(0.09f, 0.19f, 0.33f, 0.96f), AndesTypographyTheme.AccentCyan, 15.5f);
            Button btnPasos = btnPasosObj.GetComponent<Button>();
            TextMeshProUGUI txtPasosLabel = btnPasosObj.GetComponentInChildren<TextMeshProUGUI>();

            GameObject btnCreditosObj = CreateMenuButton("Btn_AbrirCreditos", tab0.transform,
                new Vector2(215f, -238f), new Vector2(400f, 46f),
                "CREDITOS Y ETICA LEGAL",
                new Color(0.13f, 0.16f, 0.29f, 0.96f), AndesTypographyTheme.AccentSky, 15.5f);
            Button btnCreditos = btnCreditosObj.GetComponent<Button>();

            // =========================================================================
            // TAB 1: MAPA & ACCESO DIRECTO A PUESTOS
            // =========================================================================
            GameObject tab1 = CreateTabContainer("Tab_1_MapaYPuestos", canvasObj.transform);
            tabPanels[1] = tab1;

            CreateCenteredText("Lbl_MapaHeader", tab1.transform, new Vector2(0f, 134f), new Vector2(880f, 34f),
                "<color=#00E5FF><b>SALTA DIRECTAMENTE A CUALQUIER PUESTO DE LA OFICINA</b></color>", 18f, Color.white);

            string[] tpNames =
            {
                "Teleport_Puesto1_Carla",
                "Teleport_Puesto2_Analista",
                "Teleport_Puesto3_LogsYDefensa",
                "Teleport_TableroSospechosos"
            };
            string[] tpLabels =
            {
                "<color=#00E5FF>PUESTO 1 · CARLA MENDOZA</color>\n<size=14><color=#ADC4DD>Correo Phishing · Post-It · Teléfono SOC</color></size>",
                "<color=#FFA940>PUESTO 2 · TERMINAL SWIFT</color>\n<size=14><color=#ADC4DD>Transferencias US$ 951M · MT103</color></size>",
                "<color=#38F28A>PUESTO 3 · SIEM Y DEFENSAS</color>\n<size=14><color=#ADC4DD>Logs IP 185.130.44.82 · Palancas MFA</color></size>",
                "<color=#A78BFA>TABLERO · 4 SOSPECHOSOS</color>\n<size=14><color=#ADC4DD>Expedientes · Hipótesis Final</color></size>"
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
                "Al seleccionar un puesto se inicia el reloj y llegas directo frente al monitor.",
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
                new Vector2(0f, 38f), new Vector2(840f, 26f), "Jefe de Seguridad TI", 16f, AndesTypographyTheme.TextMuted);

            TextMeshProUGUI sPrevBadge = CreateCenteredText("Txt_SuspectPreviewBadge", dossierPreviewCard.transform,
                new Vector2(0f, 8f), new Vector2(840f, 26f), "NEGLIGENCIA DE RED", 15.5f, AndesTypographyTheme.AccentRed);
            AndesTypographyTheme.StyleBadge(sPrevBadge, AndesTypographyTheme.AccentRed, 15.5f);

            TextMeshProUGUI sPrevClues = CreateCenteredText("Txt_SuspectPreviewClues", dossierPreviewCard.transform,
                new Vector2(0f, -48f), new Vector2(830f, 84f),
                "· Administrador de correo y switches sin VLANs ni bloqueo .exe.", 16f, AndesTypographyTheme.TextPrimary);

            GameObject btnGoBoardMenuObj = CreateMenuButton("Btn_GoTableroTab2", tab2.transform,
                new Vector2(0f, -176f), new Vector2(480f, 56f),
                "IR AL TABLERO EN LA OFICINA >",
                new Color(0.09f, 0.34f, 0.24f, 0.98f), AndesTypographyTheme.AccentEmerald, 17f);

            // =========================================================================
            // TAB 3: GUIA VR VISUAL (PASO A PASO + CONTROLES)
            // =========================================================================
            GameObject tab3 = CreateTabContainer("Tab_3_GuiaVR", canvasObj.transform);
            tabPanels[3] = tab3;

            CreateCenteredText("Lbl_GuiaTitle", tab3.transform, new Vector2(0f, 134f), new Vector2(880f, 34f),
                "<color=#00E5FF><b>RECORRIDO GUIADO · 4 PASOS EN REALIDAD VIRTUAL</b></color>", 18f, Color.white);

            string[] guideTitles = { "1 · PC 1 CORREO", "2 · POST-IT Y TEL", "3 · PC 2 Y SIEM", "4 · TABLERO Y MFA" };
            string[] guideBodies =
            {
                "Mira el monitor de Carla para abrir el correo y marcar 3 señales.",
                "Guarda la clave del Post-It amarillo o llama al Anexo 404.",
                "Revisa el riesgo SWIFT ($951M) y guarda el log en PC 3.",
                "Señala al responsable en el tablero y activa las defensas."
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

                CreateCenteredText($"Txt_GuiaBody_{i}", gCard.transform, new Vector2(0f, -20f), new Vector2(190f, 128f),
                    guideBodies[i], 15f, AndesTypographyTheme.TextPrimary);
            }

            GameObject ctrlBar = CreateGlassCard("Card_ControlesVR", tab3.transform, new Vector2(0f, -176f), new Vector2(896f, 66f),
                AndesTypographyTheme.BgCardElevated, AndesTypographyTheme.AccentCyan);
            CreateCenteredText("Txt_ControlesVR", ctrlBar.transform, Vector2.zero, new Vector2(860f, 54f),
                "<b>CONTROLES:</b> Apunta la retícula central por 1.6s (o toca la pantalla) · Aros verdes en el suelo = Teletransporte",
                15f, Color.white);

            // =========================================================================
            // TAB 4: AJUSTES, ILUMINACIÓN & FX 3D
            // =========================================================================
            GameObject tab4 = CreateTabContainer("Tab_4_AjustesYFx", canvasObj.transform);
            tabPanels[4] = tab4;

            // Fila 1: Dwell Time
            CreateCenteredText("Lbl_DwellHeader", tab4.transform, new Vector2(0f, 146f), new Vector2(880f, 26f),
                "VELOCIDAD DE SELECCION CON LA MIRADA (DWELL)", 14.5f, AndesTypographyTheme.TextMuted);

            GameObject btnDwell1 = CreateMenuButton("Btn_Dwell_Rapido", tab4.transform, new Vector2(-235f, 106f), new Vector2(220f, 44f),
                "1.0s · RAPIDO", AndesTypographyTheme.BgCardGlass, Color.white, 15f);
            GameObject btnDwell2 = CreateMenuButton("Btn_Dwell_Normal", tab4.transform, new Vector2(0f, 106f), new Vector2(220f, 44f),
                "1.6s · NORMAL", new Color(0.08f, 0.28f, 0.38f, 0.98f), AndesTypographyTheme.AccentCyan, 15f);
            GameObject btnDwell3 = CreateMenuButton("Btn_Dwell_Comodo", tab4.transform, new Vector2(235f, 106f), new Vector2(220f, 44f),
                "2.4s · COMODO", AndesTypographyTheme.BgCardGlass, Color.white, 15f);

            TextMeshProUGUI txtDwellBadge = CreateCenteredText("Txt_DwellActual", tab4.transform, new Vector2(0f, 66f), new Vector2(880f, 24f),
                "TIEMPO DE MIRADA: <b>1.6s</b>", 14.5f, AndesTypographyTheme.AccentCyan);

            // Fila 2: Iluminación Dinámica de Oficina
            CreateCenteredText("Lbl_LightHeader", tab4.transform, new Vector2(0f, 28f), new Vector2(880f, 26f),
                "ILUMINACION Y TEMA VISUAL DE LA OFICINA", 14.5f, AndesTypographyTheme.TextMuted);

            GameObject btnL1 = CreateMenuButton("Btn_Light_Clara", tab4.transform, new Vector2(-296f, -12f), new Vector2(280f, 46f),
                "OFICINA CLARA", new Color(0.10f, 0.32f, 0.24f, 0.98f), AndesTypographyTheme.AccentEmerald, 15f);
            GameObject btnL2 = CreateMenuButton("Btn_Light_Cyan", tab4.transform, new Vector2(0f, -12f), new Vector2(280f, 46f),
                "SOC CYAN NEON", new Color(0.07f, 0.24f, 0.36f, 0.98f), AndesTypographyTheme.AccentCyan, 15f);
            GameObject btnL3 = CreateMenuButton("Btn_Light_Nocturna", tab4.transform, new Vector2(296f, -12f), new Vector2(280f, 46f),
                "CYBER NOCTURNO", new Color(0.16f, 0.14f, 0.32f, 0.98f), AndesTypographyTheme.AccentViolet, 15f);

            TextMeshProUGUI txtLightBadge = CreateCenteredText("Txt_LightingActual", tab4.transform, new Vector2(0f, -52f), new Vector2(880f, 24f),
                "AMBIENTE ACTIVO: <b>OFICINA CLARA</b>", 14.5f, AndesTypographyTheme.AccentEmerald);

            // Fila 3: Audio, Voz y Demo 3D Virus
            GameObject btnAudioObj = CreateMenuButton("Btn_ToggleAudio", tab4.transform, new Vector2(-296f, -112f), new Vector2(280f, 54f),
                "AUDIO: ACTIVO 100%", new Color(0.10f, 0.20f, 0.32f, 0.96f), Color.white, 15f);
            TextMeshProUGUI txtAudioStat = btnAudioObj.GetComponentInChildren<TextMeshProUGUI>();

            GameObject btnVoiceObj = CreateMenuButton("Btn_TestVoice", tab4.transform, new Vector2(0f, -112f), new Vector2(280f, 54f),
                "PROBAR VOZ NARRADOR", new Color(0.08f, 0.26f, 0.38f, 0.96f), AndesTypographyTheme.AccentCyan, 15f);

            GameObject btnDemoVirusObj = CreateMenuButton("Btn_PreviewVirusFx", tab4.transform, new Vector2(296f, -112f), new Vector2(280f, 54f),
                "DEMO VIRUS 3D (8s)", new Color(0.42f, 0.10f, 0.13f, 0.98f), AndesTypographyTheme.AccentRed, 15f);

            // Fila 4: Créditos y Ética
            GameObject btnCreditsTab4 = CreateMenuButton("Btn_CreditsTab4", tab4.transform, new Vector2(0f, -196f), new Vector2(520f, 48f),
                "VER CREDITOS Y MARCO ETICO-LEGAL", new Color(0.14f, 0.18f, 0.32f, 0.96f), AndesTypographyTheme.AccentSky, 15.5f);

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
            Debug.Log("[ANDES-SIM - SceneAssembler] Centro Holográfico Flotante de 5 Pestañas construido exitosamente.");
            return menuRoot;
        }

        /// <summary>
        /// Agrega una baliza holográfica 3D flotante sobre cada estación de trabajo o consola de defensa.
        /// </summary>
        public static GameObject AttachWorkstationHoloBeacon(Transform parent, string name, Vector3 worldPos, Color initialColor, string tagLine)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = worldPos;

            HolographicFloatMotion fl = root.AddComponent<HolographicFloatMotion>();
            fl.Configure(0.030f, 1.05f, new Vector3(1.1f, 1.8f, 0.9f), 0f, 0.025f, true);

            Material mat = CreateUnlitGlowMaterial("Mat_" + name, initialColor, initialColor * 2.1f);

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "Holo_Core";
            core.transform.SetParent(root.transform, false);
            core.transform.localRotation = Quaternion.Euler(35f, 45f, 15f);
            core.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
            RemoveCollider(core);
            Renderer coreR = core.GetComponent<Renderer>();
            coreR.sharedMaterial = mat;
            core.AddComponent<HolographicOrbitRing>().Configure(new Vector3(18f, 52f, 14f), coreR, initialColor, 1.1f, 2.4f, 1.9f);

            GameObject ringA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringA.name = "Holo_RingA";
            ringA.transform.SetParent(root.transform, false);
            ringA.transform.localRotation = Quaternion.Euler(22f, 0f, 14f);
            ringA.transform.localScale = new Vector3(0.22f, 0.005f, 0.22f);
            RemoveCollider(ringA);
            Renderer rA = ringA.GetComponent<Renderer>();
            rA.sharedMaterial = mat;
            HolographicOrbitRing spinA = ringA.AddComponent<HolographicOrbitRing>();
            spinA.Configure(new Vector3(16f, 44f, 8f), rA, initialColor, 0.9f, 2.2f, 1.7f);

            GameObject ringB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringB.name = "Holo_RingB";
            ringB.transform.SetParent(root.transform, false);
            ringB.transform.localRotation = Quaternion.Euler(-28f, 35f, -18f);
            ringB.transform.localScale = new Vector3(0.17f, 0.004f, 0.17f);
            RemoveCollider(ringB);
            Renderer rB = ringB.GetComponent<Renderer>();
            rB.sharedMaterial = mat;
            HolographicOrbitRing spinB = ringB.AddComponent<HolographicOrbitRing>();
            spinB.Configure(new Vector3(-18f, -54f, -12f), rB, initialColor, 0.9f, 2.2f, 2.1f);

            // Mini etiqueta flotante
            GameObject lblCanvas = new GameObject("Canvas_Tag");
            lblCanvas.transform.SetParent(root.transform, false);
            lblCanvas.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            lblCanvas.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Canvas c = lblCanvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            RectTransform cr = lblCanvas.GetComponent<RectTransform>();
            cr.sizeDelta = new Vector2(210f, 40f);
            cr.localScale = Vector3.one * 0.0022f;
            Image bg = lblCanvas.AddComponent<Image>();
            bg.color = AndesTypographyTheme.BgVoidNavy;

            TextMeshProUGUI txt = CreateCenteredText("Txt_StationTag", lblCanvas.transform, Vector2.zero, new Vector2(198f, 34f),
                tagLine, 14.5f, initialColor);
            AndesTypographyTheme.StyleBadge(txt, initialColor, 14.5f);

            HolographicDataBeacon beacon = root.AddComponent<HolographicDataBeacon>();
            beacon.Bind(new[] { coreR, rA, rB }, new[] { spinA, spinB }, HoloBeaconState.NormalCyan);
            return root;
        }

        /// <summary>
        /// Añade un aro orbital flotante y gema aérea sobre el disco del TeleportPoint (sin colisionador intrusivo).
        /// </summary>
        public static void EnhanceTeleportPointHalo(GameObject tpObj, Color ringColor)
        {
            if (tpObj == null) return;

            Material mat = CreateUnlitGlowMaterial("Mat_TpHolo_" + tpObj.name, ringColor, ringColor * 1.9f);

            GameObject floatGroup = new GameObject("Holo_TeleportMarker");
            floatGroup.transform.SetParent(tpObj.transform, false);
            floatGroup.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            floatGroup.AddComponent<HolographicFloatMotion>().Configure(0.026f, 1.15f, Vector3.zero, 45f, 0.04f, true);

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Holo_TeleportRing";
            ring.transform.SetParent(floatGroup.transform, false);
            ring.transform.localScale = new Vector3(0.56f, 0.006f, 0.56f);
            RemoveCollider(ring);
            Renderer r = ring.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            ring.AddComponent<HolographicOrbitRing>().Configure(new Vector3(0f, 48f, 0f), r, ringColor, 0.8f, 2.2f, 2.0f);

            GameObject diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diamond.name = "Holo_TeleportGem";
            diamond.transform.SetParent(floatGroup.transform, false);
            diamond.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            diamond.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            diamond.transform.localScale = new Vector3(0.055f, 0.055f, 0.055f);
            RemoveCollider(diamond);
            diamond.GetComponent<Renderer>().sharedMaterial = mat;
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
            ent.Configure(0.24f, 0.90f, 18f, cg);

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
            cb.normalColor = bgColor;
            cb.highlightedColor = bgColor * 1.24f;
            cb.pressedColor = bgColor * 0.80f;
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
