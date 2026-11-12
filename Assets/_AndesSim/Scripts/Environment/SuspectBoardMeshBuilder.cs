using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AndesSim.Audio;
using AndesSim.Core;
using AndesSim.Scenario01;
using AndesSim.UI;
using AndesSim.VR;

namespace AndesSim.Environment
{
    /// <summary>
    /// Generador procedural especializado del atrezzo táctico interactivo de la oficina (RF-02 y RF-04):
    /// 1. Tablero táctico de corcho montado en la pared lateral con las 4 fichas impresas en 3D de los
    ///    sospechosos (Samuel Torres, Hugo Benavides, Carla Mendoza, Renzo Aliaga), chinchetas, notas de
    ///    evidencias forenses y las icónicas líneas rojas conectando indicios policiales.
    /// 2. Consola física de contramedidas perimetrales con los 3 interruptores tridimensionales
    ///    (DefenseControl), con palancas mecánicas 3D y LEDs reactivos que cambian dinámicamente de rojo
    ///    intenso (vulnerabilidad 2015) a verde brillante #38f28a al activarse en SimMode.Defensa.
    /// Simulación VR ANDES-SIM (BANCO ANDINO - Escenario 1).
    /// </summary>
    public static class SuspectBoardMeshBuilder
    {
        private const string LogPrefix = "[ANDES-SIM - SuspectBoardMeshBuilder]";

        #region Paleta Cromática y Constantes Visuales

        // Colores del Tablero Táctico
        public static readonly Color CorkColor = new Color(0.58f, 0.44f, 0.31f, 1.0f);        // Corcho natural #94704f
        public static readonly Color FrameWoodColor = new Color(0.12f, 0.10f, 0.08f, 1.0f);    // Madera oscura caoba #1f1914
        public static readonly Color MetalTrimColor = new Color(0.24f, 0.28f, 0.34f, 1.0f);    // Titanio cepillado #3d4757
        public static readonly Color CardPaperColor = new Color(0.95f, 0.93f, 0.88f, 1.0f);    // Papel marfil #f2ede0
        public static readonly Color RedYarnColor = new Color(0.95f, 0.12f, 0.12f, 1.0f);      // Hilo rojo forense #f21f1f
        public static readonly Color RedYarnEmission = new Color(0.55f, 0.05f, 0.05f, 1.0f);   // Brillo sutil hilo
        public static readonly Color PushpinRedColor = new Color(0.85f, 0.10f, 0.10f, 1.0f);   // Chincheta roja
        public static readonly Color PushpinGoldColor = new Color(0.88f, 0.72f, 0.20f, 1.0f);  // Chincheta dorada central
        public static readonly Color PinNeedleColor = new Color(0.85f, 0.87f, 0.90f, 1.0f);    // Metal cromado aguja

        // Colores de la Consola de Defensas
        public static readonly Color ConsoleChassisColor = new Color(0.10f, 0.12f, 0.16f, 1.0f); // Grafito militar #1a1f29
        public static readonly Color ConsoleFaceplateColor = new Color(0.18f, 0.22f, 0.28f, 1.0f);// Aluminio anodizado #2e3847
        public static readonly Color SwitchPocketColor = new Color(0.25f, 0.28f, 0.33f, 1.0f);   // Pozo de interruptor #404754
        public static readonly Color SwitchLeverColor = new Color(0.78f, 0.80f, 0.84f, 1.0f);    // Palanca cromo/acero
        public static readonly Color CyberGreenColor = new Color(0.22f, 0.95f, 0.54f, 1.0f);     // Verde neón #38f28a
        public static readonly Color LedInactiveRed = new Color(0.95f, 0.15f, 0.15f, 1.0f);      // LED inactivo rojo
        public static readonly Color LedActiveGreen = new Color(0.22f, 0.95f, 0.54f, 1.0f);      // LED activo verde #38f28a

        #endregion

        #region Cache de Materiales Compartidos

        private static Dictionary<string, Material> _materialCache;

        public static Dictionary<string, Material> GetMaterials()
        {
            if (_materialCache != null && _materialCache.Count > 0)
            {
                bool allValid = true;
                foreach (var kvp in _materialCache)
                {
                    if (kvp.Value == null) { allValid = false; break; }
                }
                if (allValid) return _materialCache;
            }

            _materialCache = new Dictionary<string, Material>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard") ??
                            Shader.Find("Mobile/Diffuse");

            _materialCache["Cork"] = CreateMat(shader, "Mat_TableroCorcho", CorkColor, 0.08f, 0.0f);
            _materialCache["FrameWood"] = CreateMat(shader, "Mat_MarcoMaderaOscura", FrameWoodColor, 0.35f, 0.1f);
            _materialCache["MetalTrim"] = CreateMat(shader, "Mat_MetalTitanio", MetalTrimColor, 0.65f, 0.6f);
            _materialCache["CardPaper"] = CreateMat(shader, "Mat_PapelMarfilFicha", CardPaperColor, 0.15f, 0.0f);
            _materialCache["CardBorder"] = CreateMat(shader, "Mat_BordeFicha", new Color(0.2f, 0.2f, 0.2f), 0.2f, 0.0f);
            _materialCache["PhotoBox"] = CreateMat(shader, "Mat_FotoRetrato", new Color(0.14f, 0.16f, 0.20f), 0.3f, 0.0f);
            _materialCache["RedYarn"] = CreateMat(shader, "Mat_HiloRojoForense", RedYarnColor, 0.2f, 0.0f, RedYarnEmission);
            _materialCache["PushpinRed"] = CreateMat(shader, "Mat_ChinchetaRoja", PushpinRedColor, 0.85f, 0.2f);
            _materialCache["PushpinGold"] = CreateMat(shader, "Mat_ChinchetaDorada", PushpinGoldColor, 0.88f, 0.7f);
            _materialCache["PinNeedle"] = CreateMat(shader, "Mat_ClavilloMetal", PinNeedleColor, 0.9f, 0.85f);
            _materialCache["PostItYellow"] = CreateMat(shader, "Mat_PostItAmarillo", new Color(1.0f, 0.88f, 0.28f), 0.1f, 0.0f, new Color(0.25f, 0.22f, 0.05f));
            _materialCache["EvidenceWhite"] = CreateMat(shader, "Mat_TarjetaEvidencia", new Color(0.96f, 0.96f, 0.96f), 0.15f, 0.0f);
            _materialCache["IncidentFolder"] = CreateMat(shader, "Mat_ExpedienteIncidente", new Color(0.68f, 0.16f, 0.14f), 0.25f, 0.0f);

            // Materiales de Consola
            _materialCache["ConsoleChassis"] = CreateMat(shader, "Mat_ChasisConsolaDefensas", ConsoleChassisColor, 0.4f, 0.3f);
            _materialCache["ConsoleFaceplate"] = CreateMat(shader, "Mat_PlacaMandosConsola", ConsoleFaceplateColor, 0.55f, 0.45f);
            _materialCache["SwitchPocket"] = CreateMat(shader, "Mat_PozoInterruptor", SwitchPocketColor, 0.5f, 0.2f);
            _materialCache["SwitchLever"] = CreateMat(shader, "Mat_PalancaInterruptor3D", SwitchLeverColor, 0.85f, 0.8f);
            _materialCache["NeonCyber"] = CreateMat(shader, "Mat_BordeNeonVerde", CyberGreenColor, 0.5f, 0.0f, CyberGreenColor * 1.5f);
            _materialCache["LedRedInactive"] = CreateMat(shader, "Mat_LedRojoInactivo", LedInactiveRed, 0.8f, 0.1f, LedInactiveRed * 0.8f);
            _materialCache["LedGreenActive"] = CreateMat(shader, "Mat_LedVerdeActivo", LedActiveGreen, 0.8f, 0.1f, LedActiveGreen * 1.8f);

            return _materialCache;
        }

        private static Material CreateMat(Shader s, string name, Color color, float smoothness, float metallic, Color? emission = null)
        {
            Material m = new Material(s) { name = name };
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);

            if (emission.HasValue && emission.Value != Color.black)
            {
                if (m.HasProperty("_EmissionColor"))
                {
                    m.SetColor("_EmissionColor", emission.Value);
                    m.EnableKeyword("_EMISSION");
                }
            }
            return m;
        }

        #endregion

        #region 1. Tablero Táctico de Corcho, Fichas 3D y Líneas Rojas

        /// <summary>
        /// Construye el panel táctico de corcho completo con las 4 fichas 3D de los sospechosos
        /// y la red de hilos rojos conectando evidencias policiales en la pared lateral (RF-02).
        /// </summary>
        public static GameObject BuildTacticalCorkBoard(Transform parent, Vector3 localPos, Quaternion localRot)
        {
            Dictionary<string, Material> mats = GetMaterials();

            GameObject boardRoot = new GameObject("Tablero_Sospechosos_Tactico_3D");
            boardRoot.transform.SetParent(parent, false);
            boardRoot.transform.localPosition = localPos;
            boardRoot.transform.localRotation = localRot;

            // -------------------------------------------------------------
            // 1.1 Estructura del Tablero de Corcho y Marco de Caoba
            // -------------------------------------------------------------
            const float boardW = 3.20f;
            const float boardH = 2.00f;
            const float boardD = 0.06f;

            // Superficie de corcho
            GameObject corkPanel = CreateBox("Panel_Corcho_Fondo", boardRoot.transform,
                new Vector3(0f, 0f, 0f), new Vector3(boardW - 0.10f, boardH - 0.10f, 0.03f), mats["Cork"]);

            // Marco superior e inferior
            CreateBox("Marco_Superior", boardRoot.transform,
                new Vector3(0f, boardH * 0.5f, 0f), new Vector3(boardW, 0.08f, boardD), mats["FrameWood"]);
            CreateBox("Marco_Inferior", boardRoot.transform,
                new Vector3(0f, -boardH * 0.5f, 0f), new Vector3(boardW, 0.08f, boardD), mats["FrameWood"]);

            // Marco lateral izquierdo y derecho
            CreateBox("Marco_Izquierdo", boardRoot.transform,
                new Vector3(-boardW * 0.5f, 0f, 0f), new Vector3(0.08f, boardH, boardD), mats["FrameWood"]);
            CreateBox("Marco_Derecho", boardRoot.transform,
                new Vector3(boardW * 0.5f, 0f, 0f), new Vector3(0.08f, boardH, boardD), mats["FrameWood"]);

            // Moldura interior de titanio
            CreateBox("Moldura_Interior", boardRoot.transform,
                new Vector3(0f, 0f, -0.012f), new Vector3(boardW - 0.06f, boardH - 0.06f, 0.005f), mats["MetalTrim"]);

            // -------------------------------------------------------------
            // Patas de soporte vertical y anclaje al suelo (Evitar que el tablero flote en el aire)
            // -------------------------------------------------------------
            float legHeight = 0.56f; // Longitud de la pata desde el marco inferior hasta el piso (y=0)
            float legY = -boardH * 0.5f - (legHeight * 0.5f);
            float legSpanX = boardW * 0.38f; // ~1.22m desde el centro

            // Pata izquierda y derecha de madera caoba
            CreateBox("Pata_Soporte_Izq", boardRoot.transform,
                new Vector3(-legSpanX, legY, 0f), new Vector3(0.08f, legHeight, 0.06f), mats["FrameWood"]);
            CreateBox("Pata_Soporte_Der", boardRoot.transform,
                new Vector3(legSpanX, legY, 0f), new Vector3(0.08f, legHeight, 0.06f), mats["FrameWood"]);

            // Travesaño horizontal inferior entre patas
            CreateBox("Travesano_Inferior_Patas", boardRoot.transform,
                new Vector3(0f, -boardH * 0.5f - 0.28f, 0f), new Vector3(boardW * 0.82f, 0.05f, 0.04f), mats["FrameWood"]);

            // Zapatas / bases metálicas fijadas al piso
            float floorFootY = -boardH * 0.5f - legHeight + 0.015f;
            CreateBox("Zapata_Piso_Izq", boardRoot.transform,
                new Vector3(-legSpanX, floorFootY, 0f), new Vector3(0.14f, 0.03f, 0.38f), mats["MetalTrim"]);
            CreateBox("Zapata_Piso_Der", boardRoot.transform,
                new Vector3(legSpanX, floorFootY, 0f), new Vector3(0.14f, 0.03f, 0.38f), mats["MetalTrim"]);

            // Anclajes metálicos posteriores hacia la pared
            CreateBox("Anclaje_Pared_Sup_Izq", boardRoot.transform,
                new Vector3(-legSpanX, boardH * 0.42f, 0.045f), new Vector3(0.06f, 0.08f, 0.07f), mats["MetalTrim"]);
            CreateBox("Anclaje_Pared_Sup_Der", boardRoot.transform,
                new Vector3(legSpanX, boardH * 0.42f, 0.045f), new Vector3(0.06f, 0.08f, 0.07f), mats["MetalTrim"]);

            // -------------------------------------------------------------
            // 1.2 Luminaria Tubular Focal Superior
            // -------------------------------------------------------------
            GameObject lampFixture = new GameObject("Luminaria_Tubular_Focal");
            lampFixture.transform.SetParent(boardRoot.transform, false);
            lampFixture.transform.localPosition = new Vector3(0f, boardH * 0.5f + 0.18f, -0.28f);

            // Tubo de lámpara
            GameObject tube = CreateBox("Tubo_Lampara", lampFixture.transform,
                Vector3.zero, new Vector3(2.40f, 0.05f, 0.05f), mats["MetalTrim"]);

            // Tira fluorescente emisiva
            GameObject bulb = CreateBox("Tira_Emisiva_LED", lampFixture.transform,
                new Vector3(0f, -0.02f, 0.015f), new Vector3(2.30f, 0.015f, 0.02f), mats["NeonCyber"]);

            // Soportes de montaje hacia la pared
            CreateBox("Soporte_Izq", lampFixture.transform,
                new Vector3(-0.9f, -0.08f, 0.14f), new Vector3(0.03f, 0.03f, 0.28f), mats["MetalTrim"]);
            CreateBox("Soporte_Der", lampFixture.transform,
                new Vector3(0.9f, -0.08f, 0.14f), new Vector3(0.03f, 0.03f, 0.28f), mats["MetalTrim"]);

            // Luz suave focalizada sobre el corcho (OpenGLES3 60 FPS)
            GameObject spotObj = new GameObject("Luz_Focal_Tablero");
            spotObj.transform.SetParent(lampFixture.transform, false);
            spotObj.transform.localPosition = new Vector3(0f, 0f, 0f);
            spotObj.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            Light spotLight = spotObj.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.range = 3.6f;
            spotLight.spotAngle = 80f;
            spotLight.intensity = 1.25f;
            spotLight.color = new Color(0.92f, 0.97f, 0.94f);
            spotLight.shadows = LightShadows.None;

            // -------------------------------------------------------------
            // 1.3 Placa Superior Serigrafiada de la Investigación
            // -------------------------------------------------------------
            GameObject titleObj = new GameObject("Placa_Encabezado_Forense");
            titleObj.transform.SetParent(boardRoot.transform, false);
            titleObj.transform.localPosition = new Vector3(0f, 0.88f, -0.022f);

            Canvas titleCanvas = titleObj.AddComponent<Canvas>();
            titleCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(900f, 60f);
            titleRect.localScale = new Vector3(0.003f, 0.003f, 0.003f);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "<color=#38F28A><b>BANCO ANDINO · CASO OPERACIÓN LAZARUS 2015</b></color>\n" +
                            "<size=12><color=#A6C8B4>TABLERO TÁCTICO FORENSE · MATRIZ DE SOSPECHOSOS E INDICIOS CLAVE</size></color>";
            titleTmp.fontSize = 17f;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.textWrappingMode = TextWrappingModes.Normal;

            // -------------------------------------------------------------
            // 1.4 Carpeta / Nodo Central del Caso y Notas de Evidencia
            // -------------------------------------------------------------
            Dictionary<string, Vector3> pinPositions = new Dictionary<string, Vector3>();

            // Nodo Central: Fraude SWIFT 2015
            Vector3 centerPin = BuildCentralIncidentCase(boardRoot.transform, mats);
            pinPositions["Central"] = centerPin;

            // Evidencia 01: Post-it credenciales de Carla Mendoza
            Vector3 evi1Pin = BuildEvidenceCard(boardRoot.transform, "Evidencia_PostIt_Carla",
                new Vector3(-0.35f, -0.28f, -0.025f), Quaternion.Euler(0f, 0f, 2.5f),
                "EVIDENCIA #01 (FÍSICA)",
                "Post-It amarillo fluorescente 'Andino2015*' adherido en monitor SWIFT de Carla Mendoza.",
                mats["PostItYellow"], mats);
            pinPositions["Evi1_PostIt"] = evi1Pin;

            // Evidencia 02: 16 Destinatarios de Pagos en Bcc
            Vector3 evi2Pin = BuildEvidenceCard(boardRoot.transform, "Evidencia_16Destinatarios",
                new Vector3(-0.35f, 0.44f, -0.025f), Quaternion.Euler(0f, 0f, -1.8f),
                "EVIDENCIA #02 (CORREO)",
                "Lista de distribución 'pagos-personal@bancoandino.pe' abierta al exterior sin SPF/DKIM.",
                mats["EvidenceWhite"], mats);
            pinPositions["Evi2_16Buzones"] = evi2Pin;

            // Evidencia 03: Payload ejecutable detonado
            Vector3 evi3Pin = BuildEvidenceCard(boardRoot.transform, "Evidencia_Payload_SCR",
                new Vector3(0f, -0.42f, -0.025f), Quaternion.Euler(0f, 0f, 1.2f),
                "EVIDENCIA #03 (MALWARE)",
                "Archivo 'Rasel_Ahlam_CV_2015.scr.zip' con troyano bancario y puerta trasera C2.",
                mats["IncidentFolder"], mats);
            pinPositions["Evi3_Payload"] = evi3Pin;

            // Evidencia 04: Red plana sin Sandbox ni VLANs
            Vector3 evi4Pin = BuildEvidenceCard(boardRoot.transform, "Evidencia_Sin_Sandbox",
                new Vector3(0.35f, 0.44f, -0.025f), Quaternion.Euler(0f, 0f, 2.0f),
                "EVIDENCIA #04 (ARQUITECTURA)",
                "Red bancaria plana sin firewall perimetral ni Cuckoo Sandbox; presupuesto rechazado.",
                mats["EvidenceWhite"], mats);
            pinPositions["Evi4_Sandbox"] = evi4Pin;

            // Evidencia 05: Registros SIEM nocturnos 23:42 hrs
            Vector3 evi5Pin = BuildEvidenceCard(boardRoot.transform, "Evidencia_Logs_SIEM",
                new Vector3(0.35f, -0.28f, -0.025f), Quaternion.Euler(0f, 0f, -2.2f),
                "EVIDENCIA #05 (TELEMETRÍA)",
                "Conexión física nocturna anómala en switch de pagos a las 23:42 sin orden técnica.",
                mats["EvidenceWhite"], mats);
            pinPositions["Evi5_Logs"] = evi5Pin;

            // -------------------------------------------------------------
            // 1.5 Las 4 Fichas Impresas en 3D de los Sospechosos
            // -------------------------------------------------------------
            // 1. Samuel Torres (Arriba Izquierda)
            Vector3 pinTorres = BuildSuspectCard3D(boardRoot.transform, 0,
                "Samuel_Torres", "Ing. Samuel Torres", "Jefe de Seguridad TI",
                "SOSPECHOSO: NEGLIGENCIA ARQUITECTÓNICA", "#ff5a4e",
                "• Domain Admin en toda la red.\n• Omitió filtro .scr.zip en Exchange.\n• Red plana sin VLANs hacia SWIFT.",
                "• 3 memorandos advirtiendo vulnerabilidad.\n• Credenciales no emitieron fraude.",
                new Vector3(-0.95f, 0.38f, -0.025f), Quaternion.Euler(0f, 0f, 1.8f), mats);
            pinPositions["Torres"] = pinTorres;

            // 2. Hugo Benavides (Arriba Derecha)
            Vector3 pinBenavides = BuildSuspectCard3D(boardRoot.transform, 1,
                "Hugo_Benavides", "Lic. Hugo Benavides", "Gerente de Operaciones",
                "SOSPECHOSO: DESIDIA GERENCIAL Y ENCUBRIMIENTO", "#ffc24a",
                "• Votó en contra de adquirir Sandbox.\n• Ignoró impresora de auditoría apagada.\n• Alentaba uso informal de claves.",
                "• Cero conocimientos técnicos de redes.\n• Fuera de la ciudad durante el ataque.",
                new Vector3(0.95f, 0.38f, -0.025f), Quaternion.Euler(0f, 0f, -1.6f), mats);
            pinPositions["Benavides"] = pinBenavides;

            // 3. Carla Mendoza (Abajo Izquierda)
            Vector3 pinMendoza = BuildSuspectCard3D(boardRoot.transform, 2,
                "Carla_Mendoza", "Carla Mendoza", "Operadora Senior SWIFT",
                "VÍCTIMA / PACIENTE CERO (PHISHING)", "#38f28a",
                "• Su usuario autorizó 36 giros por US$ 951M.\n• Clave pegada en Post-It visible.",
                "• Estaba en su hogar a medianoche.\n• Engañada por spear phishing de Rasel Ahlam.\n• Récord intachable 7 años.",
                new Vector3(-0.95f, -0.38f, -0.025f), Quaternion.Euler(0f, 0f, -1.2f), mats);
            pinPositions["Mendoza"] = pinMendoza;

            // 4. Renzo Aliaga (Abajo Derecha)
            Vector3 pinAliaga = BuildSuspectCard3D(boardRoot.transform, 3,
                "Renzo_Aliaga", "Renzo Aliaga", "Técnico Soporte NetSolutions",
                "SOSPECHOSO: CÓMPLICE INTERNO / INSIDER", "#ff5a4e",
                "• Deudas bancarias en cobranza judicial.\n• Mantenimiento físico bajo escritorios.\n• Ingresos no supervisados a servidores.",
                "• Órdenes de trabajo formalizadas.\n• Sin inserción visible de memorias USB.",
                new Vector3(0.95f, -0.38f, -0.025f), Quaternion.Euler(0f, 0f, 2.2f), mats);
            pinPositions["Aliaga"] = pinAliaga;

            // -------------------------------------------------------------
            // 1.6 Líneas Rojas Conectando Evidencias (Investigative Red Web)
            // -------------------------------------------------------------
            GameObject yarnHolder = new GameObject("Lineas_Rojas_Investigacion");
            yarnHolder.transform.SetParent(boardRoot.transform, false);

            Material yarnMat = mats["RedYarn"];
            CreateRedYarnConnection("Hilo_Torres_Sandbox", yarnHolder.transform, pinPositions["Torres"], pinPositions["Evi4_Sandbox"], yarnMat);
            CreateRedYarnConnection("Hilo_Torres_16Buzones", yarnHolder.transform, pinPositions["Torres"], pinPositions["Evi2_16Buzones"], yarnMat);
            CreateRedYarnConnection("Hilo_Benavides_Sandbox", yarnHolder.transform, pinPositions["Benavides"], pinPositions["Evi4_Sandbox"], yarnMat);
            CreateRedYarnConnection("Hilo_Benavides_Central", yarnHolder.transform, pinPositions["Benavides"], pinPositions["Central"], yarnMat);
            CreateRedYarnConnection("Hilo_Mendoza_PostIt", yarnHolder.transform, pinPositions["Mendoza"], pinPositions["Evi1_PostIt"], yarnMat);
            CreateRedYarnConnection("Hilo_Mendoza_Payload", yarnHolder.transform, pinPositions["Mendoza"], pinPositions["Evi3_Payload"], yarnMat);
            CreateRedYarnConnection("Hilo_Aliaga_Logs", yarnHolder.transform, pinPositions["Aliaga"], pinPositions["Evi5_Logs"], yarnMat);
            CreateRedYarnConnection("Hilo_Aliaga_PostIt", yarnHolder.transform, pinPositions["Aliaga"], pinPositions["Evi1_PostIt"], yarnMat);
            CreateRedYarnConnection("Hilo_Evi1_Central", yarnHolder.transform, pinPositions["Evi1_PostIt"], pinPositions["Central"], yarnMat);
            CreateRedYarnConnection("Hilo_Evi2_Central", yarnHolder.transform, pinPositions["Evi2_16Buzones"], pinPositions["Central"], yarnMat);
            CreateRedYarnConnection("Hilo_Evi3_Central", yarnHolder.transform, pinPositions["Evi3_Payload"], pinPositions["Central"], yarnMat);
            CreateRedYarnConnection("Hilo_Evi5_Central", yarnHolder.transform, pinPositions["Evi5_Logs"], pinPositions["Central"], yarnMat);

            // -------------------------------------------------------------
            // 1.7 Integración con SuspectBoardVR (Rúbrica QA VR-5)
            // -------------------------------------------------------------
            // Instanciar o conectar SuspectBoardVR para garantizar funcionalidad completa
            GameObject suspectVRCanvas = new GameObject("Canvas_Tablero_Sospechosos_VR");
            suspectVRCanvas.transform.SetParent(boardRoot.transform, false);
            suspectVRCanvas.transform.localPosition = new Vector3(0f, 0f, -0.035f);

            SuspectBoardVR boardVR = suspectVRCanvas.AddComponent<SuspectBoardVR>();
            boardVR.EnsureDefaultDossiers();

            Debug.Log($"{LogPrefix} <color=#38F28A>Tablero táctico forense 3D construido con 4 fichas impresas y red de hilos rojos.</color>");
            return boardRoot;
        }

        private static Vector3 BuildSuspectCard3D(Transform parent, int suspectIndex,
            string id, string fullName, string roleTitle, string threatBadge, string threatHex,
            string againstPoints, string favorPoints, Vector3 localPos, Quaternion localRot,
            Dictionary<string, Material> mats)
        {
            GameObject cardRoot = new GameObject($"Ficha_3D_{id}");
            cardRoot.transform.SetParent(parent, false);
            cardRoot.transform.localPosition = localPos;
            cardRoot.transform.localRotation = localRot;
            cardRoot.layer = LayerMask.NameToLayer("Interactive");

            const float cardW = 0.52f;
            const float cardH = 0.62f;

            // Placa base de papel fotográfico / cartulina
            GameObject cardBody = CreateBox("Cuerpo_Tarjeta", cardRoot.transform,
                Vector3.zero, new Vector3(cardW, cardH, 0.008f), mats["CardPaper"]);

            // Marco sutil de la ficha
            CreateBox("Borde_Tarjeta", cardRoot.transform,
                new Vector3(0f, 0f, 0.002f), new Vector3(cardW + 0.015f, cardH + 0.015f, 0.004f), mats["CardBorder"]);

            // Chincheta de fijación superior
            Vector3 pinPos = new Vector3(0f, cardH * 0.5f - 0.02f, -0.016f);
            CreatePushpin(cardRoot.transform, pinPos, mats["PushpinRed"], mats["PinNeedle"]);

            // Marco de foto carnet
            CreateBox("Foto_Marco", cardRoot.transform,
                new Vector3(-0.14f, 0.16f, -0.006f), new Vector3(0.14f, 0.16f, 0.004f), mats["PhotoBox"]);

            // Canvas World Space para texto tipográfico de alta legibilidad
            GameObject canvasObj = new GameObject("Canvas_Ficha");
            canvasObj.transform.SetParent(cardRoot.transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 0f, -0.007f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(480f, 580f);
            rect.localScale = new Vector3(0.00105f, 0.00105f, 0.00105f);

            // Contenedor de Textos
            GameObject textHolder = new GameObject("Contenido_Texto");
            textHolder.transform.SetParent(canvasObj.transform, false);
            RectTransform tRect = textHolder.AddComponent<RectTransform>();
            tRect.sizeDelta = new Vector2(460f, 560f);

            TextMeshProUGUI tmp = textHolder.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 13f;
            tmp.color = new Color(0.12f, 0.14f, 0.16f);
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            string cardContent =
                $"<align=right><size=10><color=#556677><b>EXPEDIENTE 0{suspectIndex + 1}</b></color></size></align>\n" +
                $"<line-height=110%><size=18><b>{fullName}</b></size>\n" +
                $"<size=12><color=#2C3E50>{roleTitle}</color></size></line-height>\n\n" +
                $"<size=10><mark={threatHex}44><color={threatHex}><b> {threatBadge} </b></color></mark></size>\n\n" +
                $"<size=11><color=#A93226><b>[INDICIOS EN CONTRA]:</b></color>\n" +
                $"<line-height=105%>{againstPoints}</line-height>\n\n" +
                $"<color=#196F3D><b>[ELEMENTOS A FAVOR]:</b></color>\n" +
                $"<line-height=105%>{favorPoints}</line-height></size>\n\n" +
                $"<align=center><size=11><color=#1A5276><b>[>] MIRAR CON RETÍCULA PARA ABRIR DOSSIER</b></color></size></align>";

            tmp.text = cardContent;

            // Colisionador interactivo para la mirada en VR
            BoxCollider col = cardRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(cardW + 0.02f, cardH + 0.02f, 0.04f);

            GazeInteractable gaze = cardRoot.AddComponent<GazeInteractable>();
            gaze.interactableLabel = $"Examinar expediente de {fullName}";

            // Trigger interactivo para sincronizar con SuspectBoardVR
            SuspectCard3DTrigger trigger = cardRoot.AddComponent<SuspectCard3DTrigger>();
            trigger.suspectIndex = suspectIndex;
            trigger.suspectName = fullName;
            trigger.cardRenderer = cardBody.GetComponent<Renderer>();

            // Retornar posición absoluta local en el tablero
            return cardRoot.transform.localPosition + cardRoot.transform.localRotation * pinPos;
        }

        private static Vector3 BuildCentralIncidentCase(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject caseObj = new GameObject("Ficha_Caso_Lazarus2015");
            caseObj.transform.SetParent(parent, false);
            caseObj.transform.localPosition = new Vector3(0f, 0.08f, -0.025f);

            const float w = 0.54f;
            const float h = 0.32f;

            CreateBox("Carpeta_Roja", caseObj.transform, Vector3.zero, new Vector3(w, h, 0.01f), mats["IncidentFolder"]);

            Vector3 pinPos = new Vector3(0f, h * 0.5f - 0.02f, -0.016f);
            CreatePushpin(caseObj.transform, pinPos, mats["PushpinGold"], mats["PinNeedle"]);

            GameObject canvasObj = new GameObject("Canvas_Caso");
            canvasObj.transform.SetParent(caseObj.transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 0f, -0.008f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500f, 280f);
            rect.localScale = new Vector3(0.00105f, 0.00105f, 0.00105f);

            TextMeshProUGUI tmp = canvasObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "<color=#FFFFFF><size=18><b>CASO: INCIDENTE SWIFT 2015</b></size>\n" +
                       "<size=12><color=#FFC24A>BANCO ANDINO · OFICINA DE PAGOS</color></size>\n\n" +
                       "<size=11>• Fecha de inicio: <b>29/01/2015 - 09:18 AM</b>\n" +
                       "• Vector inicial: <b>Spear Phishing (Rasel Ahlam)</b>\n" +
                       "• Alcance: <b>16 buzones del área comprometidos</b>\n" +
                       "• Intento de fraude: <b>US$ 951 Millones (36 transferencias)</b>\n" +
                       "• Atribución forense: <b>Lazarus Group (APT38)</b></size>\n\n" +
                       "<align=center><color=#FF5A4E><b>[CLASIFICADO - EXPEDIENTE JUDICIAL]</b></color></align></color>";
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            return caseObj.transform.localPosition + pinPos;
        }

        private static Vector3 BuildEvidenceCard(Transform parent, string name, Vector3 pos, Quaternion rot,
            string title, string description, Material bodyMat, Dictionary<string, Material> mats)
        {
            GameObject eviObj = new GameObject(name);
            eviObj.transform.SetParent(parent, false);
            eviObj.transform.localPosition = pos;
            eviObj.transform.localRotation = rot;

            const float w = 0.38f;
            const float h = 0.22f;

            CreateBox("Tarjeta", eviObj.transform, Vector3.zero, new Vector3(w, h, 0.006f), bodyMat);

            Vector3 pinPos = new Vector3(0f, h * 0.5f - 0.015f, -0.014f);
            CreatePushpin(eviObj.transform, pinPos, mats["PushpinRed"], mats["PinNeedle"]);

            GameObject canvasObj = new GameObject("Canvas_Evidencia");
            canvasObj.transform.SetParent(eviObj.transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 0f, -0.006f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(350f, 190f);
            rect.localScale = new Vector3(0.00105f, 0.00105f, 0.00105f);

            TextMeshProUGUI tmp = canvasObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<color=#050E09><size=13><b>{title}</b></size>\n\n" +
                       $"<size=11><line-height=110%>{description}</line-height></size></color>";
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            return eviObj.transform.localPosition + eviObj.transform.localRotation * pinPos;
        }

        private static void CreatePushpin(Transform parent, Vector3 localPos, Material headMat, Material needleMat)
        {
            GameObject pinObj = new GameObject("Chincheta_3D");
            pinObj.transform.SetParent(parent, false);
            pinObj.transform.localPosition = localPos;

            // Cabeza plástica/esférica de la chincheta
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Cabeza_Chincheta";
            head.transform.SetParent(pinObj.transform, false);
            head.transform.localPosition = Vector3.zero;
            head.transform.localScale = new Vector3(0.022f, 0.022f, 0.016f);
            head.GetComponent<Renderer>().sharedMaterial = headMat;
            GameObject.DestroyImmediate(head.GetComponent<Collider>());

            // Clavillo metálico incrustado
            GameObject needle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            needle.name = "Clavillo_Metal";
            needle.transform.SetParent(pinObj.transform, false);
            needle.transform.localPosition = new Vector3(0f, 0f, 0.008f);
            needle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            needle.transform.localScale = new Vector3(0.004f, 0.008f, 0.004f);
            needle.GetComponent<Renderer>().sharedMaterial = needleMat;
            GameObject.DestroyImmediate(needle.GetComponent<Collider>());
        }

        public static GameObject CreateRedYarnConnection(string name, Transform parent, Vector3 localStart, Vector3 localEnd, Material redMat)
        {
            Vector3 delta = localEnd - localStart;
            float length = delta.magnitude;
            if (length < 0.005f) return null;

            GameObject yarnObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            yarnObj.name = name;
            yarnObj.transform.SetParent(parent, false);

            yarnObj.transform.localPosition = (localStart + localEnd) * 0.5f;
            yarnObj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            // Radio de 2mm (diámetro 4mm), altura de cilindro en Unity es 2
            yarnObj.transform.localScale = new Vector3(0.004f, length * 0.5f, 0.004f);

            Renderer r = yarnObj.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = redMat;

            Collider c = yarnObj.GetComponent<Collider>();
            if (c != null) GameObject.DestroyImmediate(c);

            return yarnObj;
        }

        #endregion

        #region 2. Consola Física de Contramedidas y los 3 Interruptores 3D

        /// <summary>
        /// Construye la consola física de contramedidas con los 3 interruptores tridimensionales (RF-04),
        /// palancas mecánicas 3D y LEDs dinámicos que cambian de rojo a verde brillante en SimMode.Defensa.
        /// </summary>
        public static GameObject BuildDefenseConsole(Transform parent, Vector3 localPos, Quaternion localRot)
        {
            Dictionary<string, Material> mats = GetMaterials();

            GameObject consoleRoot = new GameObject("Consola_Controles_Defensa_3D");
            consoleRoot.transform.SetParent(parent, false);
            consoleRoot.transform.localPosition = localPos;
            consoleRoot.transform.localRotation = localRot;

            const float cW = 0.68f;
            const float cH = 0.44f;
            const float cD = 0.14f;

            // -------------------------------------------------------------
            // 2.1 Chasis Metálico Industrial y Bisel Biselado
            // -------------------------------------------------------------
            // Caja principal del rack
            CreateBox("Caja_Chasis", consoleRoot.transform,
                Vector3.zero, new Vector3(cW, cH, cD), mats["ConsoleChassis"]);

            // Placa frontal de aluminio cepillado
            CreateBox("Placa_Frontal", consoleRoot.transform,
                new Vector3(0f, 0f, -cD * 0.5f - 0.005f), new Vector3(cW - 0.04f, cH - 0.04f, 0.01f), mats["ConsoleFaceplate"]);

            // Borde superior e inferior de acento neón verde #38f28a
            CreateBox("Borde_Neon_Top", consoleRoot.transform,
                new Vector3(0f, cH * 0.5f - 0.01f, -cD * 0.5f - 0.008f), new Vector3(cW - 0.02f, 0.012f, 0.01f), mats["NeonCyber"]);
            CreateBox("Borde_Neon_Bottom", consoleRoot.transform,
                new Vector3(0f, -cH * 0.5f + 0.01f, -cD * 0.5f - 0.008f), new Vector3(cW - 0.02f, 0.012f, 0.01f), mats["NeonCyber"]);

            // -------------------------------------------------------------
            // 2.2 Rótulos y Títulos Serigrafiados de la Consola
            // -------------------------------------------------------------
            GameObject headerObj = new GameObject("Encabezado_Consola");
            headerObj.transform.SetParent(consoleRoot.transform, false);
            headerObj.transform.localPosition = new Vector3(0f, cH * 0.5f - 0.045f, -cD * 0.5f - 0.015f);

            Canvas headerCanvas = headerObj.AddComponent<Canvas>();
            headerCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform hRect = headerObj.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(480f, 55f);
            hRect.localScale = new Vector3(0.0012f, 0.0012f, 0.0012f);

            TextMeshProUGUI headerTmp = headerObj.AddComponent<TextMeshProUGUI>();
            headerTmp.text = "<color=#38F28A><b>BANCO ANDINO · SISTEMA CENTRAL DE CONTRAMEDIDAS</b></color>\n" +
                             "<size=10><color=#A0B6A8>SELECCIONE SALVAGUARDAS PARA CONTENCIÓN DE AMENAZAS (RF-04)</size></color>";
            headerTmp.fontSize = 13f;
            headerTmp.alignment = TextAlignmentOptions.Center;

            // -------------------------------------------------------------
            // 2.3 Construcción de los 3 Interruptores Tridimensionales
            // -------------------------------------------------------------
            string[] defIds = { "Defensa_Sandbox", "Defensa_BotonReportar", "Defensa_MFA" };
            string[] defTitles = {
                "1. FILTRO DE CORREO Y SANDBOX CUCKOO",
                "2. BOTÓN SOC \"REPORTAR PHISHING\"",
                "3. GESTOR CORPORATIVO Y DOBLE FACTOR (MFA)"
            };
            string[] defDescriptions = {
                "Detonación en entorno virtual antes de buzones; neutraliza '.scr.zip'.",
                "Capacitación al personal y purga en <60s de los 16 buzones del área.",
                "Tokens dinámicos TOTP; invalida contraseñas expuestas en papel/Post-It."
            };

            float startY = 0.085f;
            float stepY = -0.095f;

            for (int i = 0; i < 3; i++)
            {
                float currentY = startY + (i * stepY);
                Vector3 switchPos = new Vector3(0f, currentY, -cD * 0.5f - 0.012f);
                BuildIndividual3DDefenseSwitch(consoleRoot.transform, switchPos, defIds[i], defTitles[i], defDescriptions[i], mats);
            }

            Debug.Log($"{LogPrefix} <color=#38F28A>Consola física de contramedidas 3D construida con 3 interruptores mecánicos y LEDs reactivos.</color>");
            return consoleRoot;
        }

        private static GameObject BuildIndividual3DDefenseSwitch(Transform parent, Vector3 localPos,
            string defenseId, string title, string description, Dictionary<string, Material> mats)
        {
            GameObject switchRoot = new GameObject($"Switch_{defenseId}");
            switchRoot.transform.SetParent(parent, false);
            switchRoot.transform.localPosition = localPos;
            switchRoot.layer = LayerMask.NameToLayer("Interactive");
            switchRoot.tag = "DefenseControl";

            const float swW = 0.58f;
            const float swH = 0.082f;

            // Pozo de montaje empotrado
            CreateBox("Bisel_Pozo", switchRoot.transform,
                Vector3.zero, new Vector3(swW, swH, 0.012f), mats["SwitchPocket"]);

            // Marco metálico pulido
            CreateBox("Borde_Metal", switchRoot.transform,
                new Vector3(0f, 0f, -0.003f), new Vector3(swW + 0.01f, swH + 0.008f, 0.006f), mats["MetalTrim"]);

            // -------------------------------------------------------------
            // Palanca Mecánica 3D (Toggle Switch Pivotable)
            // -------------------------------------------------------------
            GameObject leverPivot = new GameObject("Pivote_Palanca_3D");
            leverPivot.transform.SetParent(switchRoot.transform, false);
            leverPivot.transform.localPosition = new Vector3(-swW * 0.5f + 0.06f, 0f, -0.015f);
            leverPivot.transform.localRotation = Quaternion.Euler(25f, 0f, 0f); // Posición inicial OFF (abajo)

            // Eje horizontal de pivote
            GameObject pivotAxle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pivotAxle.name = "Eje_Pivote";
            pivotAxle.transform.SetParent(leverPivot.transform, false);
            pivotAxle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            pivotAxle.transform.localScale = new Vector3(0.014f, 0.018f, 0.014f);
            pivotAxle.GetComponent<Renderer>().sharedMaterial = mats["MetalTrim"];
            GameObject.DestroyImmediate(pivotAxle.GetComponent<Collider>());

            // Vástago de la palanca mecánica
            GameObject leverStem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leverStem.name = "Vastago_Palanca";
            leverStem.transform.SetParent(leverPivot.transform, false);
            leverStem.transform.localPosition = new Vector3(0f, 0.022f, -0.012f);
            leverStem.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            leverStem.transform.localScale = new Vector3(0.008f, 0.022f, 0.008f);
            leverStem.GetComponent<Renderer>().sharedMaterial = mats["SwitchLever"];
            GameObject.DestroyImmediate(leverStem.GetComponent<Collider>());

            // Pomo / Cabezal de la palanca
            GameObject leverKnob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leverKnob.name = "Pomo_Palanca";
            leverKnob.transform.SetParent(leverStem.transform, false);
            leverKnob.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            leverKnob.transform.localScale = new Vector3(2.2f, 0.9f, 2.2f);
            leverKnob.GetComponent<Renderer>().sharedMaterial = mats["SwitchLever"];
            GameObject.DestroyImmediate(leverKnob.GetComponent<Collider>());

            // -------------------------------------------------------------
            // LED Tridimensional de Estado Reactivo
            // -------------------------------------------------------------
            GameObject ledHolder = new GameObject("Domo_LED_3D");
            ledHolder.transform.SetParent(switchRoot.transform, false);
            ledHolder.transform.localPosition = new Vector3(-swW * 0.5f + 0.125f, 0f, -0.012f);

            // Bisel metálico del LED
            GameObject ledBezel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ledBezel.name = "Bisel_LED";
            ledBezel.transform.SetParent(ledHolder.transform, false);
            ledBezel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ledBezel.transform.localScale = new Vector3(0.034f, 0.004f, 0.034f);
            ledBezel.GetComponent<Renderer>().sharedMaterial = mats["MetalTrim"];
            GameObject.DestroyImmediate(ledBezel.GetComponent<Collider>());

            // Cúpula o lente del LED (instancia individual para cambio dinámico de color)
            GameObject ledDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ledDome.name = "Lente_LED";
            ledDome.transform.SetParent(ledHolder.transform, false);
            ledDome.transform.localPosition = new Vector3(0f, 0f, -0.006f);
            ledDome.transform.localScale = new Vector3(0.024f, 0.024f, 0.014f);

            Material instancedLedMat = new Material(mats["LedRedInactive"]);
            Renderer ledRenderer = ledDome.GetComponent<Renderer>();
            ledRenderer.sharedMaterial = instancedLedMat;
            GameObject.DestroyImmediate(ledDome.GetComponent<Collider>());

            // Luz puntual real-time focalizada (OpenGLES3 60 FPS, LightShadows.None)
            GameObject ledLightObj = new GameObject("Luz_Foco_LED");
            ledLightObj.transform.SetParent(ledHolder.transform, false);
            ledLightObj.transform.localPosition = new Vector3(0f, 0f, -0.035f);
            Light ledLight = ledLightObj.AddComponent<Light>();
            ledLight.type = LightType.Point;
            ledLight.range = 0.75f;
            ledLight.intensity = 0.90f;
            ledLight.color = LedInactiveRed;
            ledLight.shadows = LightShadows.None;

            // -------------------------------------------------------------
            // Textos Informativos World Space y Botón Virtual
            // -------------------------------------------------------------
            GameObject textCanvasObj = new GameObject("Canvas_Etiqueta");
            textCanvasObj.transform.SetParent(switchRoot.transform, false);
            textCanvasObj.transform.localPosition = new Vector3(0.07f, 0f, -0.012f);

            Canvas tCanvas = textCanvasObj.AddComponent<Canvas>();
            tCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform tRect = textCanvasObj.GetComponent<RectTransform>();
            tRect.sizeDelta = new Vector2(320f, 65f);
            tRect.localScale = new Vector3(0.00115f, 0.00115f, 0.00115f);

            GameObject textObj = new GameObject("Txt_Detalle");
            textObj.transform.SetParent(textCanvasObj.transform, false);
            RectTransform txtRect = textObj.AddComponent<RectTransform>();
            txtRect.sizeDelta = new Vector2(310f, 60f);

            TextMeshProUGUI labelTmp = textObj.AddComponent<TextMeshProUGUI>();
            labelTmp.fontSize = 11.5f;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.textWrappingMode = TextWrappingModes.Normal;
            labelTmp.text = $"<size=12><b>{title}</b></size>\n" +
                           $"<size=9.5><color=#95A5A6>{description}</color></size>\n" +
                           $"<size=9.5><color=#FF4444><b>[-] INACTIVO EN 2015 (VULNERABLE)</b></color></size>";

            // -------------------------------------------------------------
            // Colisionador Ergonómico e Integración con DefenseControl
            // -------------------------------------------------------------
            BoxCollider col = switchRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(swW + 0.02f, swH + 0.01f, 0.06f);

            GazeInteractable gaze = switchRoot.AddComponent<GazeInteractable>();
            gaze.interactableLabel = $"Activar contramedida: {title}";

            DefenseControl defCtrl = switchRoot.AddComponent<DefenseControl>();
            defCtrl.Configure(defenseId, title, ledRenderer, ledLight, null, labelTmp);

            // Controlador físico reactivo de la palanca mecánica y sincronización
            PhysicalDefenseSwitchLever leverCtrl = switchRoot.AddComponent<PhysicalDefenseSwitchLever>();
            leverCtrl.leverTransform = leverPivot.transform;
            leverCtrl.defenseControl = defCtrl;
            leverCtrl.ledRenderer = ledRenderer;
            leverCtrl.ledLight = ledLight;
            leverCtrl.statusLabel = labelTmp;
            leverCtrl.defenseTitle = title;
            leverCtrl.defenseDescription = description;

            return switchRoot;
        }

        #endregion

        #region Utilitarios de Modelado Procedural

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        #endregion
    }
}
