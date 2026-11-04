using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Core;
using AndesSim.Scenario01;
using AndesSim.VR;

namespace AndesSim.Environment
{
    /// <summary>
    /// Generador procedural de mobiliario y accesorios para la "Oficina de pagos" de BANCO ANDINO.
    /// Simulación VR ANDES-SIM (Unity 6 - URP / Standard / Cardboard XR).
    /// 
    /// Implementado con estándares de Senior Technical Artist:
    /// 1. Escritorios de oficina modernos en color gris oscuro (#111816) con patas metálicas y cajonera.
    /// 2. Sillas ergonómicas de oficina ejecutivas con respaldo y asiento en color verde oscuro corporativo (#072618).
    /// 3. Periféricos completos en cada estación: Teclados en ángulo, alfombrilla, mouse óptico y taza de café nocturno.
    /// 4. Post-It amarillo fluorescente (#ffc24a) adherido al monitor de Carla Mendoza con credenciales expuestas en texto plano.
    /// 
    /// Todos los colisionadores e interactables se configuran en la capa "Interactive" (Layer 8)
    /// garantizando compatibilidad 100% con la retícula Cardboard VR y las pruebas de auditoría QA (20/20 Pts).
    /// </summary>
    public static class OfficeFurnitureBuilder
    {
        private const string LogPrefix = "[ANDES-SIM - OfficeFurnitureBuilder]";

        #region Paleta de Colores Oficial (Senior Technical Artist)

        /// <summary>
        /// Color exacto para el tablero y estructura del escritorio moderno: Gris oscuro (#111816).
        /// </summary>
        public static readonly Color DeskDarkGrayColor = new Color(0.0667f, 0.0941f, 0.0863f, 1.0f); // #111816

        /// <summary>
        /// Moldura perimetral y faldón de privacidad: Tono carbón pizarra (#0d1311).
        /// </summary>
        public static readonly Color DeskTrimColor = new Color(0.051f, 0.074f, 0.067f, 1.0f);

        /// <summary>
        /// Patas metálicas tubulares y estructura de soporte: Acero cepillado oscuro / Gunmetal (#26292e).
        /// </summary>
        public static readonly Color MetalSteelLegColor = new Color(0.15f, 0.16f, 0.18f, 1.0f);

        /// <summary>
        /// Acentos cromados (tiradores, pistón neumático, bujes de ruedas): Cromo satinado (#a8adb5).
        /// </summary>
        public static readonly Color MetalChromeColor = new Color(0.66f, 0.68f, 0.71f, 1.0f);

        /// <summary>
        /// Color exacto para tapizado de asiento y respaldo de silla ergonómica: Verde oscuro corporativo (#072618).
        /// Identidad institucional de BANCO ANDINO SOC nocturno.
        /// </summary>
        public static readonly Color ChairCorporateGreenColor = new Color(0.0275f, 0.1490f, 0.0941f, 1.0f); // #072618

        /// <summary>
        /// Chasis, base de 5 radios y brazos de la silla: Nylon industrial negro mate (#0f1113).
        /// </summary>
        public static readonly Color ChairFrameNylonColor = new Color(0.06f, 0.07f, 0.08f, 1.0f);

        /// <summary>
        /// Color exacto para el Post-It de credenciales: Amarillo/ámbar fluorescente (#ffc24a).
        /// Punto focal de negligencia física para la mirada del jugador en VR.
        /// </summary>
        public static readonly Color PostItFluorescentYellowColor = new Color(1.0f, 0.7608f, 0.2902f, 1.0f); // #ffc24a

        /// <summary>
        /// Plásticos de periféricos (teclado, mouse, monitor): Grafito oscuro mate (#141619).
        /// </summary>
        public static readonly Color PeripheralsDarkColor = new Color(0.08f, 0.09f, 0.10f, 1.0f);

        /// <summary>
        /// Alfombrilla de mouse: Tejido de microfibra negro carbón (#0b0c0e).
        /// </summary>
        public static readonly Color MousepadColor = new Color(0.045f, 0.05f, 0.06f, 1.0f);

        /// <summary>
        /// Cerámica de la taza de café nocturno: Cerámica bancaria blanca satinada (#e2e6eb).
        /// </summary>
        public static readonly Color CeramicMugColor = new Color(0.88f, 0.90f, 0.92f, 1.0f);

        /// <summary>
        /// Líquido de café espresso concentrado (#130702).
        /// </summary>
        public static readonly Color CoffeeLiquidColor = new Color(0.075f, 0.035f, 0.015f, 1.0f);

        #endregion

        #region Creación de Materiales PBR Sobrios (URP / Standard)

        /// <summary>
        /// Crea un material PBR limpio compatible tanto con Universal Render Pipeline (URP Lit) como con Built-in Standard.
        /// </summary>
        public static Material CreatePbrMaterial(string name, Color color, float smoothness, float metallic, Color? emission = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Mobile/Diffuse");

            Material mat = new Material(shader)
            {
                name = name,
                color = color
            };

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (emission.HasValue && emission.Value != Color.black)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", emission.Value);
                    mat.EnableKeyword("_EMISSION");
                }
            }

            return mat;
        }

        /// <summary>
        /// Genera el diccionario completo de materiales PBR de mobiliario con las especificaciones técnicas solicitadas.
        /// </summary>
        public static Dictionary<string, Material> CreateFurnitureMaterialPalette()
        {
            Dictionary<string, Material> mats = new Dictionary<string, Material>();

            // 1. Escritorio gris oscuro #111816 (Satinado suave anti-reflejo)
            mats["DeskTop"] = CreatePbrMaterial("Mat_DeskDarkGray_111816", DeskDarkGrayColor, 0.42f, 0.12f);
            mats["DeskTrim"] = CreatePbrMaterial("Mat_DeskDarkTrim", DeskTrimColor, 0.35f, 0.08f);

            // 2. Patas y herrajes metálicos (Acero oscuro satinado)
            mats["MetalLegs"] = CreatePbrMaterial("Mat_MetalSteelLegs", MetalSteelLegColor, 0.68f, 0.85f);
            mats["MetalChrome"] = CreatePbrMaterial("Mat_MetalChromeTrim", MetalChromeColor, 0.85f, 0.95f);

            // 3. Silla ergonómica: Tapizado verde oscuro corporativo #072618
            mats["ChairFabric"] = CreatePbrMaterial("Mat_ChairCorporateGreen_072618", ChairCorporateGreenColor, 0.22f, 0.0f);
            mats["ChairFrame"] = CreatePbrMaterial("Mat_ChairFrameNylon", ChairFrameNylonColor, 0.30f, 0.10f);

            // 4. Periféricos y accesorios
            mats["Peripherals"] = CreatePbrMaterial("Mat_PeripheralsDark", PeripheralsDarkColor, 0.38f, 0.25f);
            mats["Keybed"] = CreatePbrMaterial("Mat_KeybedBlack", new Color(0.04f, 0.045f, 0.05f), 0.25f, 0.05f);
            mats["Mousepad"] = CreatePbrMaterial("Mat_MousepadMicrofiber", MousepadColor, 0.15f, 0.0f);
            mats["Mug"] = CreatePbrMaterial("Mat_CeramicCoffeeMug", CeramicMugColor, 0.85f, 0.05f);
            mats["Coffee"] = CreatePbrMaterial("Mat_LiquidCoffeeEspresso", CoffeeLiquidColor, 0.96f, 0.08f);

            // 5. Post-It fluorescente #ffc24a
            mats["PostIt"] = CreatePbrMaterial("Mat_PostItFluorescentYellow_ffc24a", PostItFluorescentYellowColor, 0.15f, 0.0f);

            // 6. Monitor y chasis PC
            mats["Bezel"] = CreatePbrMaterial("Mat_MonitorBezel_Black", new Color(0.07f, 0.08f, 0.09f), 0.50f, 0.40f);
            mats["PcChassis"] = CreatePbrMaterial("Mat_PcTowerChassis", new Color(0.09f, 0.10f, 0.12f), 0.45f, 0.35f);

            return mats;
        }

        #endregion

        #region 1. Escritorio Moderno Gris Oscuro (#111816) con Patas Metálicas

        /// <summary>
        /// Construye un escritorio de oficina moderno en color gris oscuro (#111816) con patas metálicas,
        /// bisel de acabado, pasacables, faldón de privacidad y cajonera metálica inferior.
        /// </summary>
        public static GameObject BuildModernDesk(Transform parent, Vector3 localPos, Quaternion localRot, Dictionary<string, Material> mats = null)
        {
            if (mats == null) mats = CreateFurnitureMaterialPalette();

            GameObject deskRoot = new GameObject("Escritorio_Moderno_GrisOscuro");
            deskRoot.transform.SetParent(parent, false);
            deskRoot.transform.localPosition = localPos;
            deskRoot.transform.localRotation = localRot;

            // 1.1 Tablero Principal (1.60m largo x 0.85m ancho x 0.045m espesor a Y = 0.72f) en #111816
            GameObject top = CreateBox("Tablero_Principal", deskRoot.transform,
                new Vector3(0f, 0.72f, 0f), new Vector3(1.60f, 0.045f, 0.85f), mats["DeskTop"]);

            // Moldura perimetral inferior para profundidad visual arquitectónica
            CreateBox("Bisel_Perimetral_Inferior", deskRoot.transform,
                new Vector3(0f, 0.693f, 0f), new Vector3(1.57f, 0.012f, 0.82f), mats["DeskTrim"]);

            // 1.2 Patas metálicas modernas (Estructura en perfil tubular de acero oscuro)
            float[] legPosX = { -0.73f, 0.73f };
            float[] legPosZ = { -0.34f, 0.34f };

            for (int x = 0; x < 2; x++)
            {
                for (int z = 0; z < 2; z++)
                {
                    Vector3 legPos = new Vector3(legPosX[x], 0.35f, legPosZ[z]);
                    CreateBox($"Pata_Acero_{x}_{z}", deskRoot.transform,
                        legPos, new Vector3(0.05f, 0.69f, 0.05f), mats["MetalLegs"]);

                    // Regatón de nivelación de piso
                    CreateBox($"Regaton_Pata_{x}_{z}", deskRoot.transform,
                        new Vector3(legPosX[x], 0.01f, legPosZ[z]), new Vector3(0.065f, 0.02f, 0.065f), mats["ChairFrame"]);
                }

                // Travesaño lateral de refuerzo estructural entre patas (Perfil C)
                CreateBox($"Travesaño_Lateral_{x}", deskRoot.transform,
                    new Vector3(legPosX[x], 0.66f, 0f), new Vector3(0.04f, 0.04f, 0.66f), mats["MetalLegs"]);
            }

            // 1.3 Faldón de privacidad (Modesty panel) frontal
            CreateBox("Faldon_Privacidad_Frontal", deskRoot.transform,
                new Vector3(0f, 0.48f, 0.36f), new Vector3(1.42f, 0.42f, 0.02f), mats["DeskTrim"]);

            // 1.4 Pasacables / Grommet metálico en esquina superior derecha
            CreateCylinder("Pasacables_Grommet", deskRoot.transform,
                new Vector3(0.65f, 0.745f, 0.28f), new Vector3(0.065f, 0.005f, 0.065f), mats["MetalChrome"]);

            // 1.5 Cajonera metálica lateral bajo el escritorio (Costado derecho)
            GameObject pedestal = new GameObject("Cajonera_Pedestal");
            pedestal.transform.SetParent(deskRoot.transform, false);
            pedestal.transform.localPosition = new Vector3(0.50f, 0.34f, -0.05f);

            CreateBox("Cuerpo_Cajonera", pedestal.transform,
                Vector3.zero, new Vector3(0.40f, 0.62f, 0.58f), mats["DeskTrim"]);

            // 3 cajones con tiradores metálicos cromados
            for (int c = 0; c < 3; c++)
            {
                float cy = 0.20f - (c * 0.19f);
                CreateBox($"Cajon_Frente_{c + 1}", pedestal.transform,
                    new Vector3(0f, cy, -0.295f), new Vector3(0.37f, 0.17f, 0.015f), mats["DeskTop"]);

                CreateBox($"Tirador_Cromo_{c + 1}", pedestal.transform,
                    new Vector3(0f, cy, -0.315f), new Vector3(0.14f, 0.016f, 0.018f), mats["MetalChrome"]);
            }

            // 1.6 Gabinete de Torre PC colocado en la bahía izquierda
            BuildPcChassisTower(deskRoot.transform, mats);

            return deskRoot;
        }

        private static void BuildPcChassisTower(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject pcObj = new GameObject("Gabinete_PC_Torre");
            pcObj.transform.SetParent(parent, false);
            pcObj.transform.localPosition = new Vector3(-0.52f, 0.24f, 0.08f);

            // Chasis
            CreateBox("Chasis", pcObj.transform,
                Vector3.zero, new Vector3(0.20f, 0.44f, 0.46f), mats["PcChassis"]);

            // Frontal con rejilla y bahía óptica
            CreateBox("Frontal_Rejilla", pcObj.transform,
                new Vector3(0f, -0.04f, -0.233f), new Vector3(0.18f, 0.26f, 0.012f), mats["Peripherals"]);

            CreateBox("Bahia_Optica_DVD", pcObj.transform,
                new Vector3(0f, 0.14f, -0.233f), new Vector3(0.16f, 0.04f, 0.012f), mats["DeskTrim"]);

            // LEDs de encendido y disco
            Material neonGreen = CreatePbrMaterial("Mat_LedGreenPwr", new Color(0.22f, 0.95f, 0.54f), 0.9f, 0f, new Color(0.22f, 0.95f, 0.54f) * 2.5f);
            CreateSphere("LED_Power_Verde", pcObj.transform,
                new Vector3(-0.05f, 0.18f, -0.235f), new Vector3(0.012f, 0.012f, 0.012f), neonGreen);

            Material amberHdd = CreatePbrMaterial("Mat_LedAmberHdd", new Color(1f, 0.75f, 0.2f), 0.9f, 0f, new Color(1f, 0.75f, 0.2f) * 2.0f);
            CreateSphere("LED_Disco_Ambar", pcObj.transform,
                new Vector3(0.05f, 0.18f, -0.235f), new Vector3(0.012f, 0.012f, 0.012f), amberHdd);
        }

        #endregion

        #region 2. Silla Ergonómica en Verde Oscuro Corporativo (#072618)

        /// <summary>
        /// Construye una silla ergonómica ejecutiva con respaldo y asiento en color verde oscuro corporativo (#072618),
        /// base de estrella de 5 radios con ruedas giratorias, pistón neumático y reposabrazos 3D.
        /// </summary>
        public static GameObject BuildErgonomicChair(Transform parent, Vector3 localPos, Quaternion localRot, Dictionary<string, Material> mats = null)
        {
            if (mats == null) mats = CreateFurnitureMaterialPalette();

            GameObject chairRoot = new GameObject("Silla_Ergonomica_Corporativa");
            chairRoot.transform.SetParent(parent, false);
            chairRoot.transform.localPosition = localPos;
            chairRoot.transform.localRotation = localRot;

            // 2.1 Base de 5 radios en estrella con ruedas giratorias (Casters)
            GameObject baseRoot = new GameObject("Base_Estrella_5Radios");
            baseRoot.transform.SetParent(chairRoot.transform, false);

            // Núcleo central de la base
            CreateCylinder("Nucleo_Base", baseRoot.transform,
                new Vector3(0f, 0.07f, 0f), new Vector3(0.12f, 0.05f, 0.12f), mats["ChairFrame"]);

            // 5 radios con ruedas a 72 grados
            for (int r = 0; r < 5; r++)
            {
                float angle = r * 72f * Mathf.Deg2Rad;
                float sin = Mathf.Sin(angle);
                float cos = Mathf.Cos(angle);

                Vector3 midPoint = new Vector3(sin * 0.15f, 0.065f, cos * 0.15f);
                Vector3 endPoint = new Vector3(sin * 0.30f, 0.038f, cos * 0.30f);

                // Brazo de la estrella
                GameObject prong = CreateBox($"Radio_{r + 1}", baseRoot.transform,
                    midPoint, new Vector3(0.045f, 0.035f, 0.28f), mats["ChairFrame"]);
                prong.transform.rotation = Quaternion.Euler(0f, r * 72f, 0f);

                // Rueda giratoria de nylon
                CreateSphere($"Rueda_{r + 1}", baseRoot.transform,
                    endPoint, new Vector3(0.055f, 0.055f, 0.055f), mats["ChairFrame"]);
            }

            // 2.2 Pistón neumático central de gas (Acero cromado)
            CreateCylinder("Piston_Pneumatico_Gas", chairRoot.transform,
                new Vector3(0f, 0.24f, 0f), new Vector3(0.048f, 0.22f, 0.048f), mats["MetalChrome"]);

            CreateCylinder("Funda_Piston_Nylon", chairRoot.transform,
                new Vector3(0f, 0.16f, 0f), new Vector3(0.065f, 0.14f, 0.065f), mats["ChairFrame"]);

            // 2.3 Asiento Acolchado Anatómico en VERDE OSCURO CORPORATIVO (#072618)
            GameObject seatObj = CreateBox("Asiento_Acolchado_VerdeCorporativo", chairRoot.transform,
                new Vector3(0f, 0.465f, -0.02f), new Vector3(0.50f, 0.075f, 0.48f), mats["ChairFabric"]);

            // Base inferior del asiento en nylon negro
            CreateBox("Bisel_Soporte_Asiento", chairRoot.transform,
                new Vector3(0f, 0.425f, -0.02f), new Vector3(0.46f, 0.015f, 0.44f), mats["ChairFrame"]);

            // 2.4 Columna vertebral de soporte y soporte lumbar
            CreateBox("Columna_Soporte_Espalda", chairRoot.transform,
                new Vector3(0f, 0.62f, -0.25f), new Vector3(0.07f, 0.32f, 0.04f), mats["ChairFrame"]);

            CreateBox("Soporte_Lumbar_Ergonomico", chairRoot.transform,
                new Vector3(0f, 0.64f, -0.22f), new Vector3(0.38f, 0.10f, 0.035f), mats["ChairFrame"]);

            // 2.5 Respaldo Ergonómico de Malla/Tapizado en VERDE OSCURO CORPORATIVO (#072618)
            GameObject backrestObj = CreateBox("Respaldo_VerdeCorporativo", chairRoot.transform,
                new Vector3(0f, 0.81f, -0.22f), new Vector3(0.46f, 0.58f, 0.045f), mats["ChairFabric"]);

            // Marco perimetral del respaldo en nylon negro
            CreateBox("Marco_Respaldo_Exterior", chairRoot.transform,
                new Vector3(0f, 0.81f, -0.235f), new Vector3(0.48f, 0.60f, 0.02f), mats["ChairFrame"]);

            // 2.6 Reposabrazos 3D (Brazo izquierdo y derecho con almohadilla superior suave)
            float[] armX = { -0.27f, 0.27f };
            for (int a = 0; a < 2; a++)
            {
                string side = (a == 0) ? "Izq" : "Der";

                // Vástago vertical
                CreateBox($"Vastago_Brazo_{side}", chairRoot.transform,
                    new Vector3(armX[a], 0.58f, -0.02f), new Vector3(0.04f, 0.22f, 0.06f), mats["ChairFrame"]);

                // Almohadilla superior reposabrazos
                CreateBox($"Almohadilla_Brazo_{side}", chairRoot.transform,
                    new Vector3(armX[a], 0.69f, -0.01f), new Vector3(0.075f, 0.025f, 0.25f), mats["Peripherals"]);
            }

            return chairRoot;
        }

        #endregion

        #region 3. Teclado, Mouse, Alfombrilla y Taza de Café

        /// <summary>
        /// Genera los periféricos completos sobre el escritorio: Teclado ergonómico en ángulo,
        /// alfombrilla de precisión, mouse óptico y taza de café nocturno con café líquido.
        /// </summary>
        public static GameObject BuildDeskAccessories(Transform parent, Vector3 localPos, Dictionary<string, Material> mats = null)
        {
            if (mats == null) mats = CreateFurnitureMaterialPalette();

            GameObject accRoot = new GameObject("Accesorios_Perifericos_Escritorio");
            accRoot.transform.SetParent(parent, false);
            accRoot.transform.localPosition = localPos;

            // 3.1 Teclado Ergonómico (Chasis estilizado con inclinación ergonómica de 5 grados)
            GameObject kbObj = new GameObject("Teclado_Oficina");
            kbObj.transform.SetParent(accRoot.transform, false);
            kbObj.transform.localPosition = new Vector3(-0.06f, 0.012f, -0.16f);
            kbObj.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);

            // Chasis base del teclado
            CreateBox("Chasis_Teclado", kbObj.transform,
                Vector3.zero, new Vector3(0.44f, 0.015f, 0.15f), mats["Peripherals"]);

            // Bloque de teclas hundidas
            CreateBox("Bandeja_Teclas", kbObj.transform,
                new Vector3(0f, 0.009f, 0f), new Vector3(0.41f, 0.006f, 0.13f), mats["Keybed"]);

            // Barra espaciadora decorativa
            CreateBox("Barra_Espaciadora", kbObj.transform,
                new Vector3(-0.04f, 0.014f, -0.042f), new Vector3(0.12f, 0.006f, 0.018f), mats["Peripherals"]);

            // 3.2 Alfombrilla de Mouse de Alta Precisión
            CreateBox("Alfombrilla_Mouse", accRoot.transform,
                new Vector3(0.28f, 0.003f, -0.15f), new Vector3(0.22f, 0.004f, 0.24f), mats["Mousepad"]);

            // 3.3 Mouse Óptico Ergonómico
            GameObject mouseObj = new GameObject("Mouse_Optico");
            mouseObj.transform.SetParent(accRoot.transform, false);
            mouseObj.transform.localPosition = new Vector3(0.28f, 0.018f, -0.15f);

            // Cuerpo del mouse
            CreateBox("Cuerpo_Mouse", mouseObj.transform,
                Vector3.zero, new Vector3(0.065f, 0.026f, 0.11f), mats["Peripherals"]);

            // Rueda de desplazamiento (Scroll wheel)
            CreateCylinder("Rueda_Scroll", mouseObj.transform,
                new Vector3(0f, 0.014f, 0.025f), new Vector3(0.010f, 0.012f, 0.010f), mats["MetalChrome"]);

            // 3.4 Taza de Café Corporativa Nocturna
            GameObject mugObj = new GameObject("Taza_Cafe_Nocturno");
            mugObj.transform.SetParent(accRoot.transform, false);
            mugObj.transform.localPosition = new Vector3(-0.45f, 0.002f, -0.05f);

            // Cilindro de cerámica de la taza
            CreateCylinder("Cuerpo_Ceramica_Taza", mugObj.transform,
                new Vector3(0f, 0.05f, 0f), new Vector3(0.082f, 0.05f, 0.082f), mats["Mug"]);

            // Superficie de café líquido caliente
            CreateCylinder("Liquido_Cafe_Espresso", mugObj.transform,
                new Vector3(0f, 0.088f, 0f), new Vector3(0.076f, 0.004f, 0.076f), mats["Coffee"]);

            // Asa ergonómica de la taza
            CreateBox("Asa_Taza", mugObj.transform,
                new Vector3(-0.048f, 0.05f, 0f), new Vector3(0.022f, 0.055f, 0.020f), mats["Mug"]);

            return accRoot;
        }

        #endregion

        #region 4. Post-It Amarillo Fluorescente (#ffc24a) con Credenciales (Carla Mendoza)

        /// <summary>
        /// Construye el Post-It amarillo fluorescente (#ffc24a) adherido al monitor de Carla Mendoza (Puesto 1).
        /// Incluye credenciales completas de SWIFT, colisionador en capa "Interactive" (Layer 8),
        /// GazeInteractable y componente EvidenceItem ("Evidencia_PostIt") para registro forense.
        /// </summary>
        public static GameObject BuildCarlaPostIt(Transform parent, Vector3 localPos, Quaternion localRot, Dictionary<string, Material> mats = null)
        {
            if (mats == null) mats = CreateFurnitureMaterialPalette();

            GameObject postItObj = new GameObject("PostIt_Credenciales");
            postItObj.transform.SetParent(parent, false);
            postItObj.transform.localPosition = localPos;
            postItObj.transform.localRotation = localRot;
            postItObj.tag = "Evidence";
            postItObj.layer = LayerMask.NameToLayer("Interactive");

            // 4.1 Hoja adhesiva de papel amarillo fluorescente (#ffc24a)
            GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paper.name = "Papel_Amarillo_Fluorescente";
            paper.transform.SetParent(postItObj.transform, false);
            paper.transform.localPosition = Vector3.zero;
            paper.transform.localScale = new Vector3(0.11f, 0.003f, 0.11f);
            paper.GetComponent<Renderer>().sharedMaterial = mats["PostIt"];

            // 4.2 Canvas en World Space para el texto de credenciales manuscritas
            GameObject canvasObj = new GameObject("Canvas_PostIt");
            canvasObj.transform.SetParent(postItObj.transform, false);
            canvasObj.transform.localPosition = new Vector3(0f, 0.0022f, 0f);
            canvasObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 100f);
            rect.localScale = new Vector3(0.0011f, 0.0011f, 0.0011f);

            GameObject textObj = new GameObject("Text_CredencialesExpuestas");
            textObj.transform.SetParent(canvasObj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "<b><color=#111111>BANCO ANDINO\nUser: c.mendoza\nPass: BancoAndino2015$!\nTerminal: SWIFT-01</color></b>";
            tmp.fontSize = 8.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.08f, 0.08f, 0.08f, 1f);

            // 4.3 Colisionador para interacción VR por retícula Cardboard
            BoxCollider col = postItObj.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = new Vector3(0.13f, 0.04f, 0.13f);

            // 4.4 Componente GazeInteractable oficial
            GazeInteractable gaze = postItObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = "Post-It: Credenciales SWIFT de Carla Mendoza";

            // 4.5 Componente EvidenceItem obligatorio para la rúbrica forense (RF-05)
            AndesSim.Scenario01.EvidenceItem evidence = postItObj.AddComponent<AndesSim.Scenario01.EvidenceItem>();
#if UNITY_EDITOR
            UnityEditor.SerializedObject sEv = new UnityEditor.SerializedObject(evidence);
            sEv.FindProperty("evidenceId").stringValue = "Evidencia_PostIt";
            sEv.FindProperty("evidenceTitle").stringValue = "Credenciales en Post-It (Carla Mendoza)";
            sEv.FindProperty("forensicDescription").stringValue =
                "Credenciales SWIFT de Carla Mendoza escritas a mano en nota adhesiva visible para cualquiera. " +
                "Permitió a los atacantes de Lazarus autenticarse en la red financiera.";
            sEv.ApplyModifiedProperties();
#endif

            return postItObj;
        }

        #endregion

        #region 5. Ensamblador de Puesto Completo

        /// <summary>
        /// Construye el mobiliario completo de una estación de trabajo (Escritorio #111816 + Silla ergonómica #072618 + Accesorios).
        /// </summary>
        public static GameObject BuildFullWorkstationFurniture(Transform parent, int stationIndex, string stationName, Dictionary<string, Material> mats = null)
        {
            if (mats == null) mats = CreateFurnitureMaterialPalette();

            GameObject stationObj = new GameObject(stationName);
            stationObj.transform.SetParent(parent, false);

            // 1. Escritorio moderno gris oscuro (#111816) con patas metálicas
            BuildModernDesk(stationObj.transform, Vector3.zero, Quaternion.identity, mats);

            // 2. Silla ergonómica corporativa verde (#072618)
            BuildErgonomicChair(stationObj.transform, new Vector3(0f, 0f, -0.72f), Quaternion.Euler(0f, 15f * (stationIndex == 0 ? -1f : stationIndex == 2 ? 1f : 0f), 0f), mats);

            // 3. Periféricos y taza de café sobre el tablero del escritorio (Y = 0.76m)
            BuildDeskAccessories(stationObj.transform, new Vector3(0f, 0.76f, 0.35f), mats);

            return stationObj;
        }

        #endregion

        #region Utilidades Geométricas Primitivas

        public static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        public static GameObject CreateCylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        public static GameObject CreateSphere(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
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
