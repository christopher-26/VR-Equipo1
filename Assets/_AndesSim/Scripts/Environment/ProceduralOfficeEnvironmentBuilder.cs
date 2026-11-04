using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AndesSim.Audio;
using AndesSim.Core;
using AndesSim.Scenario01;
using AndesSim.UI;
using AndesSim.VR;

namespace AndesSim.Environment
{
    /// <summary>
    /// Generador procedural de la arquitectura 3D y mobiliario de la "Oficina de pagos" del BANCO ANDINO.
    /// Simulación VR ANDES-SIM - Escenario 1 (Equipo 1).
    /// - Construye con mallas optimizadas y materiales PBR oscuros la atmósfera de una oficina bancaria nocturna:
    ///   1. Sala de Vestíbulo: Antesala oscura estilo consola SOC con portal luminoso y tiras de neón verde #38f28a.
    ///   2. Salón Principal (10m x 8m): Piso con cuadrícula arquitectónica, paneles acústicos, techo con LEDs tenues,
    ///      ventanal nocturno con vista a rascacielos y letrero en relieve "BANCO ANDINO · OFICINA DE PAGOS INTERNACIONALES".
    ///   3. 3 Puestos de Trabajo completos (RF-01): Escritorios en L, sillas ergonómicas, gabinetes PC con LEDs,
    ///      teclados, ratones, tazas de café y monitores (Monitor_01, Monitor_02, Monitor_03).
    ///   4. Puesto 1 (Carla Mendoza): Post-It de credenciales adherido al monitor, teléfono IP de anexo SOC y Token MFA.
    ///   5. Pared Central / Lateral: Consola de contramedidas con los 3 interruptores 3D de DefenseControl y tablero de sospechosos.
    /// </summary>
    public class ProceduralOfficeEnvironmentBuilder : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - OfficeBuilder]";

        // Paleta oficial de materiales
        private const string ColorDarkBg = "#020a06";
        private const string ColorCardBg = "#07150e";
        private const string ColorGreenCode = "#38f28a";
        private const string ColorAlertRed = "#ff5a4e";
        private const string ColorAmberWarning = "#ffc24a";
        private const string ColorTextLight = "#d6f2e2";

        [Header("--- Opciones de Construcción ---")]
        [SerializeField] private bool buildOnStart = false;

        private void Start()
        {
            if (buildOnStart)
            {
                BuildOfficeGeometry(transform);
            }
        }

        /// <summary>
        /// Genera de forma procedural y estructurada toda la geometría 3D de la oficina nocturna bajo el Transform raíz indicado.
        /// </summary>
        public static void BuildOfficeGeometry(Transform root)
        {
            BuildOfficeGeometry(root, true, true);
        }

        /// <summary>
        /// Genera la geometría 3D con control granular de mobiliario y consolas para integración en scene builders.
        /// </summary>
        public static void BuildOfficeGeometry(Transform root, bool includeWorkstations, bool includeConsoles)
        {
            if (root == null)
            {
                Debug.LogError($"{LogPrefix} El transform raíz es nulo. Cancelando construcción.");
                return;
            }

            Debug.Log($"{LogPrefix} <color=#38F28A><b>Iniciando generación procedural de la Oficina Bancaria Nocturna...</b></color>");

            // 1. Diccionario de Materiales PBR
            Dictionary<string, Material> mats = CreateMaterialPalette();

            // 2. Estructura Arquitectónica (RoomArchitectureBuilder): Baldosas técnicas #050d0a, acústica, ventanal con código SWIFT y letrero
            GameObject archRoot = RoomArchitectureBuilder.BuildCompleteRoomArchitecture(root);

            // 3. Mobiliario y Puestos de Trabajo (RF-01)
            if (includeWorkstations)
            {
                GameObject workstationsRoot = new GameObject("Mobiliario_Puestos_Trabajo");
                workstationsRoot.transform.SetParent(root, false);
                BuildThreeWorkstations(workstationsRoot.transform, mats);
            }

            // 4. Consola de Defensas y Tablero de Sospechosos
            if (includeConsoles)
            {
                GameObject wallConsolesRoot = new GameObject("Consolas_Pared_Defensa");
                wallConsolesRoot.transform.SetParent(root, false);
                BuildCentralDefenseConsole(wallConsolesRoot.transform, mats);
            }

            Debug.Log($"{LogPrefix} <color=#38F28A><b>¡Oficina bancaria nocturna generada exitosamente!</b></color>");
        }

        #region Paleta de Materiales PBR y Texturas Procedurales

        private static Dictionary<string, Material> CreateMaterialPalette()
        {
            Dictionary<string, Material> mats = new Dictionary<string, Material>();

            // Texturas procedurales
            Texture2D gridTex = GenerateGridTexture(256, 32);
            Texture2D skylineTex = GenerateNightSkylineTexture(512, 256);

            // 1. Piso con cuadrícula arquitectónica
            Material floorMat = CreatePbrMaterial("Mat_OfficeFloor_DarkGrid", new Color(0.28f, 0.32f, 0.36f), 0.35f, 0.05f);
            floorMat.mainTexture = gridTex;
            floorMat.mainTextureScale = new Vector2(10f, 8f);
            mats["Floor"] = floorMat;

            // 2. Piso del Vestíbulo
            Material vestibuleFloorMat = CreatePbrMaterial("Mat_VestibuleFloor", new Color(0.25f, 0.28f, 0.32f), 0.45f, 0.1f);
            mats["VestibuleFloor"] = vestibuleFloorMat;

            // 3. Paredes con paneles acústicos
            Material acousticWallMat = CreatePbrMaterial("Mat_AcousticWalls", new Color(0.35f, 0.40f, 0.45f), 0.15f, 0.02f);
            mats["Walls"] = acousticWallMat;

            // 4. Molduras y divisores metálicos
            Material metalTrimMat = CreatePbrMaterial("Mat_MetalTrim", new Color(0.24f, 0.26f, 0.30f), 0.70f, 0.85f);
            mats["MetalTrim"] = metalTrimMat;

            // 5. Techo y falso plafón
            Material ceilingMat = CreatePbrMaterial("Mat_CeilingGrid", new Color(0.35f, 0.38f, 0.42f), 0.10f, 0.0f);
            mats["Ceiling"] = ceilingMat;

            // 6. Neón verde Banco Andino #38f28a
            Color neonCol = new Color(0.22f, 0.95f, 0.54f);
            Material neonMat = CreatePbrMaterial("Mat_NeonGreen_Emissive", neonCol, 0.85f, 0.1f, neonCol * 2.8f);
            mats["NeonGreen"] = neonMat;

            // 7. Escritorio moderno en color gris oscuro #111816
            Material deskMat = CreatePbrMaterial("Mat_DeskDarkGray_111816", OfficeFurnitureBuilder.DeskDarkGrayColor, 0.42f, 0.12f);
            mats["DeskLaminate"] = deskMat;

            // 8. Patas y herrajes metálicos (Acero cepillado oscuro / Gunmetal)
            Material blackMetalMat = CreatePbrMaterial("Mat_MetalSteelLegs", OfficeFurnitureBuilder.MetalSteelLegColor, 0.68f, 0.85f);
            mats["BlackMetal"] = blackMetalMat;

            // 9. Tela de silla ergonómica: Verde oscuro corporativo #072618
            Material chairMeshMat = CreatePbrMaterial("Mat_ChairCorporateGreen_072618", OfficeFurnitureBuilder.ChairCorporateGreenColor, 0.22f, 0.0f);
            mats["ChairMesh"] = chairMeshMat;

            // 10. Chasis de Gabinete PC
            Material pcChassisMat = CreatePbrMaterial("Mat_PcChassis", new Color(0.11f, 0.12f, 0.14f), 0.45f, 0.40f);
            mats["PcChassis"] = pcChassisMat;

            // 11. Ventanal: Vidrio tintado oscuro
            Material glassMat = CreatePbrMaterial("Mat_WindowTintedGlass", new Color(0.03f, 0.05f, 0.08f, 0.75f), 0.95f, 0.2f);
            mats["WindowGlass"] = glassMat;

            // 12. Fondo de Skyline nocturno con rascacielos
            Material skylineMat = CreatePbrMaterial("Mat_NightSkylineBackdrop", Color.white, 0.1f, 0f, new Color(0.15f, 0.20f, 0.25f));
            skylineMat.mainTexture = skylineTex;
            mats["Skyline"] = skylineMat;

            // 13. Letrero corporativo de aluminio cepillado
            Material signMat = CreatePbrMaterial("Mat_CorporateSignAlum", new Color(0.65f, 0.70f, 0.75f), 0.80f, 0.90f);
            mats["SignAlum"] = signMat;

            // 14. Cerámica de taza de café
            Material mugMat = CreatePbrMaterial("Mat_CoffeeMugCeramic", new Color(0.85f, 0.88f, 0.90f), 0.80f, 0.05f);
            mats["CoffeeMug"] = mugMat;

            // 15. Café líquido
            Material coffeeMat = CreatePbrMaterial("Mat_LiquidCoffee", new Color(0.12f, 0.07f, 0.03f), 0.90f, 0.1f);
            mats["Coffee"] = coffeeMat;

            return mats;
        }

        private static Material CreatePbrMaterial(string name, Color color, float smoothness, float metallic, Color? emission = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Mobile/Diffuse");

            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

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

        private static Texture2D GenerateGridTexture(int size, int spacing)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color baseCol = new Color(0.08f, 0.09f, 0.11f, 1f);
            Color lineCol = new Color(0.14f, 0.17f, 0.20f, 1f);

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isLine = (x % spacing == 0) || (y % spacing == 0);
                    pixels[y * size + x] = isLine ? lineCol : baseCol;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        private static Texture2D GenerateNightSkylineTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color skyCol = new Color(0.02f, 0.03f, 0.06f, 1f);
            Color buildingDark = new Color(0.05f, 0.06f, 0.09f, 1f);
            Color windowWarm = new Color(0.95f, 0.85f, 0.50f, 0.90f);
            Color windowCyan = new Color(0.35f, 0.85f, 0.95f, 0.90f);

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = skyCol;

            System.Random rand = new System.Random(888);
            int curX = 8;
            while (curX < width - 20)
            {
                int bWidth = rand.Next(28, 60);
                int bHeight = rand.Next(70, 205);
                for (int y = 0; y < bHeight; y++)
                {
                    for (int x = curX; x < curX + bWidth && x < width; x++)
                    {
                        bool isWindow = (y > 12) && (y % 13 >= 3 && y % 13 <= 8) && ((x - curX) % 9 >= 2 && (x - curX) % 9 <= 6);
                        if (isWindow && rand.NextDouble() < 0.32)
                        {
                            pixels[y * width + x] = (rand.NextDouble() < 0.22) ? windowCyan : windowWarm;
                        }
                        else
                        {
                            pixels[y * width + x] = buildingDark;
                        }
                    }
                }
                curX += bWidth + rand.Next(4, 14);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        #endregion

        #region Salón Principal de la Oficina (10m x 8m)

        private static void BuildMainOfficeHall(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject hallObj = new GameObject("Salon_Principal_10x8");
            hallObj.transform.SetParent(parent, false);

            // 1. Piso Principal (10m x 8m, centrado entre z = -2.5m y z = 5.5m -> centro en z = 1.5m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Piso_Principal";
            floor.transform.SetParent(hallObj.transform, false);
            floor.transform.position = new Vector3(0f, -0.05f, 1.5f);
            floor.transform.localScale = new Vector3(10f, 0.1f, 8f);
            floor.GetComponent<Renderer>().sharedMaterial = mats["Floor"];

            // 2. Pared Norte (con espacio para ventanal central)
            // Pared a los lados del ventanal
            CreateBox("Pared_Norte_Izq", hallObj.transform, new Vector3(-3.8f, 1.6f, 5.5f), new Vector3(2.4f, 3.2f, 0.2f), mats["Walls"]);
            CreateBox("Pared_Norte_Der", hallObj.transform, new Vector3(3.8f, 1.6f, 5.5f), new Vector3(2.4f, 3.2f, 0.2f), mats["Walls"]);
            CreateBox("Pared_Norte_Superior", hallObj.transform, new Vector3(0f, 2.85f, 5.5f), new Vector3(5.2f, 0.7f, 0.2f), mats["Walls"]);
            CreateBox("Pared_Norte_Inferior", hallObj.transform, new Vector3(0f, 0.45f, 5.5f), new Vector3(5.2f, 0.9f, 0.2f), mats["Walls"]);

            // 3. Pared Este (donde se ubica el Tablero de Sospechosos)
            CreateBox("Pared_Este", hallObj.transform, new Vector3(5.0f, 1.6f, 1.5f), new Vector3(0.2f, 3.2f, 8.0f), mats["Walls"]);

            // Paneles acústicos decorativos con ranuras verticales en la pared este
            for (int i = -3; i <= 3; i++)
            {
                CreateBox($"Panel_Acustico_Este_{i}", hallObj.transform, new Vector3(4.89f, 1.6f, 1.5f + (i * 1.0f)), new Vector3(0.04f, 2.6f, 0.85f), mats["MetalTrim"]);
            }

            // 4. Pared Oeste
            CreateBox("Pared_Oeste", hallObj.transform, new Vector3(-5.0f, 1.6f, 1.5f), new Vector3(0.2f, 3.2f, 8.0f), mats["Walls"]);
            for (int i = -3; i <= 3; i++)
            {
                CreateBox($"Panel_Acustico_Oeste_{i}", hallObj.transform, new Vector3(-4.89f, 1.6f, 1.5f + (i * 1.0f)), new Vector3(0.04f, 2.6f, 0.85f), mats["MetalTrim"]);
            }

            // 5. Pared Sur divisoria (con arcada hacia el vestíbulo)
            CreateBox("Pared_Sur_Izq", hallObj.transform, new Vector3(-3.2f, 1.6f, -2.5f), new Vector3(3.6f, 3.2f, 0.2f), mats["Walls"]);
            CreateBox("Pared_Sur_Der", hallObj.transform, new Vector3(3.2f, 1.6f, -2.5f), new Vector3(3.6f, 3.2f, 0.2f), mats["Walls"]);
            CreateBox("Pared_Sur_Dintel", hallObj.transform, new Vector3(0f, 2.85f, -2.5f), new Vector3(2.8f, 0.7f, 0.2f), mats["Walls"]);

            // 6. Techo con luminarias modulares
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Techo_Modular_Oficina";
            ceiling.transform.SetParent(hallObj.transform, false);
            ceiling.transform.position = new Vector3(0f, 3.25f, 1.5f);
            ceiling.transform.localScale = new Vector3(10f, 0.1f, 8f);
            ceiling.GetComponent<Renderer>().sharedMaterial = mats["Ceiling"];

            // Tiras de iluminación nocturna tenue en el techo (Point lights y difusores rectangulares)
            CreateCeilingTrofferLight(hallObj.transform, "Luminaria_Puesto1", new Vector3(-2.8f, 3.15f, 1.5f), mats["NeonGreen"]);
            CreateCeilingTrofferLight(hallObj.transform, "Luminaria_Puesto2", new Vector3(0f, 3.15f, 1.5f), mats["NeonGreen"]);
            CreateCeilingTrofferLight(hallObj.transform, "Luminaria_Puesto3", new Vector3(2.8f, 3.15f, 1.5f), mats["NeonGreen"]);
        }

        private static void CreateCeilingTrofferLight(Transform parent, string name, Vector3 pos, Material neonMat)
        {
            GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fixture.name = name;
            fixture.transform.SetParent(parent, false);
            fixture.transform.position = pos;
            fixture.transform.localScale = new Vector3(1.2f, 0.04f, 0.6f);
            fixture.GetComponent<Renderer>().sharedMaterial = neonMat;

            GameObject lightObj = new GameObject(name + "_Light");
            lightObj.transform.SetParent(fixture.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, -0.5f, 0f);

            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.85f, 0.95f, 1.0f);
            l.intensity = 0.75f;
            l.range = 5.0f;
            l.shadows = LightShadows.None;
        }

        #endregion

        #region Sala de Vestíbulo (Zona de Inicio)

        private static void BuildVestibuleRoom(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject vestibuleObj = new GameObject("Sala_Vestibulo_SOC");
            vestibuleObj.transform.SetParent(parent, false);

            // 1. Piso del Vestíbulo (6m x 3.5m, desde z = -2.5m hasta z = -6.0m -> centro en z = -4.25m)
            GameObject vFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vFloor.name = "Piso_Vestibulo";
            vFloor.transform.SetParent(vestibuleObj.transform, false);
            vFloor.transform.position = new Vector3(0f, -0.05f, -4.25f);
            vFloor.transform.localScale = new Vector3(6f, 0.1f, 3.5f);
            vFloor.GetComponent<Renderer>().sharedMaterial = mats["VestibuleFloor"];

            // 2. Pared Sur del Vestíbulo (Fondo de entrada)
            CreateBox("Pared_Fondo_Vestibulo", vestibuleObj.transform, new Vector3(0f, 1.6f, -6.0f), new Vector3(6f, 3.2f, 0.2f), mats["Walls"]);

            // 3. Paredes Laterales del Vestíbulo
            CreateBox("Pared_Lat_Izq_Vestibulo", vestibuleObj.transform, new Vector3(-3.0f, 1.6f, -4.25f), new Vector3(0.2f, 3.2f, 3.5f), mats["Walls"]);
            CreateBox("Pared_Lat_Der_Vestibulo", vestibuleObj.transform, new Vector3(3.0f, 1.6f, -4.25f), new Vector3(0.2f, 3.2f, 3.5f), mats["Walls"]);

            // 4. Techo del Vestíbulo
            GameObject vCeiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vCeiling.name = "Techo_Vestibulo";
            vCeiling.transform.SetParent(vestibuleObj.transform, false);
            vCeiling.transform.position = new Vector3(0f, 3.25f, -4.25f);
            vCeiling.transform.localScale = new Vector3(6f, 0.1f, 3.5f);
            vCeiling.GetComponent<Renderer>().sharedMaterial = mats["Ceiling"];

            // 5. Tiras de Neón Verde #38f28a en el piso y zócalo
            CreateBox("Neon_Piso_Eje_Central", vestibuleObj.transform, new Vector3(0f, 0.005f, -4.25f), new Vector3(0.06f, 0.01f, 3.0f), mats["NeonGreen"]);
            CreateBox("Neon_Zocalo_Izq", vestibuleObj.transform, new Vector3(-2.89f, 0.05f, -4.25f), new Vector3(0.02f, 0.08f, 3.3f), mats["NeonGreen"]);
            CreateBox("Neon_Zocalo_Der", vestibuleObj.transform, new Vector3(2.89f, 0.05f, -4.25f), new Vector3(0.02f, 0.08f, 3.3f), mats["NeonGreen"]);

            // 6. Marco luminoso del Portal_Entrada (Portal de paso a la oficina)
            GameObject portalFrameObj = new GameObject("Marco_Luminoso_PortalEntrada");
            portalFrameObj.transform.SetParent(vestibuleObj.transform, false);
            portalFrameObj.transform.position = new Vector3(0f, 0f, -4.5f);

            CreateBox("Arco_Columna_Izq", portalFrameObj.transform, new Vector3(-1.3f, 1.35f, 0f), new Vector3(0.18f, 2.7f, 0.25f), mats["MetalTrim"]);
            CreateBox("Arco_Columna_Der", portalFrameObj.transform, new Vector3(1.3f, 1.35f, 0f), new Vector3(0.18f, 2.7f, 0.25f), mats["MetalTrim"]);
            CreateBox("Arco_Dintel_Superior", portalFrameObj.transform, new Vector3(0f, 2.7f, 0f), new Vector3(2.78f, 0.18f, 0.25f), mats["MetalTrim"]);

            // Franjas neón emissive en el arco
            CreateBox("Neon_Columna_Izq", portalFrameObj.transform, new Vector3(-1.2f, 1.35f, -0.13f), new Vector3(0.04f, 2.6f, 0.02f), mats["NeonGreen"]);
            CreateBox("Neon_Columna_Der", portalFrameObj.transform, new Vector3(1.2f, 1.35f, -0.13f), new Vector3(0.04f, 2.6f, 0.02f), mats["NeonGreen"]);
            CreateBox("Neon_Dintel", portalFrameObj.transform, new Vector3(0f, 2.6f, -0.13f), new Vector3(2.4f, 0.04f, 0.02f), mats["NeonGreen"]);

            // Luz suave cenital del vestíbulo
            GameObject vLightObj = new GameObject("Luz_Ambiental_Vestibulo");
            vLightObj.transform.SetParent(vestibuleObj.transform, false);
            vLightObj.transform.position = new Vector3(0f, 2.8f, -4.5f);
            Light vl = vLightObj.AddComponent<Light>();
            vl.type = LightType.Point;
            vl.color = new Color(0.22f, 0.95f, 0.54f);
            vl.intensity = 0.55f;
            vl.range = 4.5f;
        }

        #endregion

        #region Ventanal Nocturno y Letrero Corporativo

        private static void BuildNightPanoramicWindow(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject windowRoot = new GameObject("Ventanal_Nocturno_Panoramico");
            windowRoot.transform.SetParent(parent, false);
            windowRoot.transform.position = new Vector3(0f, 1.7f, 5.5f);

            // 1. Marco exterior de aluminio negro (5.2m x 1.6m)
            CreateBox("Marco_Ventanal_Exterior", windowRoot.transform, Vector3.zero, new Vector3(5.2f, 1.6f, 0.08f), mats["BlackMetal"]);

            // 2. Vidrio tintado oscuro
            CreateBox("Vidrio_Ventana", windowRoot.transform, new Vector3(0f, 0f, 0.01f), new Vector3(5.0f, 1.45f, 0.02f), mats["WindowGlass"]);

            // 3. Montantes y parteluces verticales
            CreateBox("Parteluz_1", windowRoot.transform, new Vector3(-1.65f, 0f, -0.01f), new Vector3(0.06f, 1.5f, 0.04f), mats["BlackMetal"]);
            CreateBox("Parteluz_2", windowRoot.transform, new Vector3(0f, 0f, -0.01f), new Vector3(0.06f, 1.5f, 0.04f), mats["BlackMetal"]);
            CreateBox("Parteluz_3", windowRoot.transform, new Vector3(1.65f, 0f, -0.01f), new Vector3(0.06f, 1.5f, 0.04f), mats["BlackMetal"]);

            // 4. Telón de fondo con vista a rascacielos nocturnos iluminados
            GameObject skylineQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skylineQuad.name = "Backdrop_Rascacielos_Nocturnos";
            skylineQuad.transform.SetParent(windowRoot.transform, false);
            skylineQuad.transform.localPosition = new Vector3(0f, 0f, 0.45f);
            skylineQuad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            skylineQuad.transform.localScale = new Vector3(5.8f, 2.2f, 1f);
            skylineQuad.GetComponent<Renderer>().sharedMaterial = mats["Skyline"];
        }

        private static void BuildCorporateSignage(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject signRoot = new GameObject("Letrero_Corporativo_BancoAndino");
            signRoot.transform.SetParent(parent, false);
            signRoot.transform.position = new Vector3(0f, 2.85f, 5.42f);
            signRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Placa metálica base
            CreateBox("Placa_Aluminio_Base", signRoot.transform, Vector3.zero, new Vector3(4.6f, 0.42f, 0.04f), mats["SignAlum"]);

            // Borde inferior con neón verde
            CreateBox("Neon_Halo_Sign", signRoot.transform, new Vector3(0f, -0.21f, -0.025f), new Vector3(4.6f, 0.025f, 0.02f), mats["NeonGreen"]);

            // Texto en relieve 3D con TextMeshPro
            GameObject textObj = new GameObject("Txt_BancoAndino");
            textObj.transform.SetParent(signRoot.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.025f);

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = "<b><color=#0A2B1D>BANCO ANDINO</color></b> · <color=#1A3628>OFICINA DE PAGOS INTERNACIONALES</color>";
            tmp.fontSize = 2.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.08f, 0.18f, 0.12f);
        }

        #endregion

        #region Mobiliario de los 3 Puestos de Trabajo (RF-01)

        private static void BuildThreeWorkstations(Transform parent, Dictionary<string, Material> mats)
        {
            float[] posX = { -2.8f, 0.0f, 2.8f };
            string[] names = { "Puesto_01_CarlaMendoza", "Puesto_02_ComercioExterior", "Puesto_03_ConsolaDefensas" };
            string[] labels = { "Puesto 1: Carla Mendoza (SWIFT)", "Puesto 2: Analista Comercio Exterior", "Puesto 3: Consola SIEM (Defensas)" };

            for (int i = 0; i < 3; i++)
            {
                GameObject wsObj = new GameObject(names[i]);
                wsObj.transform.SetParent(parent, false);
                wsObj.transform.position = new Vector3(posX[i], 0f, 1.6f);

                // 1. Escritorio profesional en L
                BuildLShapedDesk(wsObj.transform, mats);

                // 2. Silla ergonómica de oficina
                BuildErgonomicChair(wsObj.transform, mats);

                // 3. Gabinete PC con LEDs de actividad
                BuildPcTower(wsObj.transform, mats);

                // 4. Periféricos: Teclado, mouse y taza de café
                BuildPeripheralsAndCoffee(wsObj.transform, mats);

                // 5. Monitor con marco 3D y base
                string monitorId = $"Monitor_0{i + 1}";
                BuildWorkstationMonitor(wsObj.transform, monitorId, labels[i], mats);

                // 6. Elementos específicos de Carla Mendoza en Puesto 1
                if (i == 0)
                {
                    BuildCarlaMendozaSpecifics(wsObj.transform, mats);
                }
            }
        }

        /// <summary>
        /// Genera un escritorio modular profesional en L con patas metálicas y cajonera.
        /// </summary>
        private static void BuildLShapedDesk(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject deskObj = new GameObject("Escritorio_En_L");
            deskObj.transform.SetParent(parent, false);

            // Tablero Principal (1.6m x 0.8m x 0.04m a 0.75m de altura)
            CreateBox("Tablero_Principal", deskObj.transform, new Vector3(0f, 0.74f, 0f), new Vector3(1.60f, 0.04f, 0.80f), mats["DeskLaminate"]);

            // Ala de retorno en L (0.8m x 0.5m x 0.04m sobre el costado izquierdo)
            CreateBox("Ala_Retorno_L", deskObj.transform, new Vector3(-0.95f, 0.74f, 0.15f), new Vector3(0.50f, 0.04f, 0.90f), mats["DeskLaminate"]);

            // Patas metálicas tubulares
            CreateCylinder("Pata_1", deskObj.transform, new Vector3(-0.72f, 0.36f, 0.32f), new Vector3(0.045f, 0.36f, 0.045f), mats["BlackMetal"]);
            CreateCylinder("Pata_2", deskObj.transform, new Vector3(0.72f, 0.36f, 0.32f), new Vector3(0.045f, 0.36f, 0.045f), mats["BlackMetal"]);
            CreateCylinder("Pata_3", deskObj.transform, new Vector3(0.72f, 0.36f, -0.32f), new Vector3(0.045f, 0.36f, 0.045f), mats["BlackMetal"]);
            CreateCylinder("Pata_L", deskObj.transform, new Vector3(-1.12f, 0.36f, 0.52f), new Vector3(0.045f, 0.36f, 0.045f), mats["BlackMetal"]);

            // Faldón de privacidad (Modesty panel)
            CreateBox("Faldon_Privacidad", deskObj.transform, new Vector3(0f, 0.48f, 0.34f), new Vector3(1.48f, 0.44f, 0.02f), mats["DeskLaminate"]);

            // Cajonera metálica bajo el ala lateral
            GameObject pedestal = CreateBox("Cajonera_Pedestal", deskObj.transform, new Vector3(-0.95f, 0.36f, 0.15f), new Vector3(0.42f, 0.68f, 0.60f), mats["BlackMetal"]);

            // 3 frentes de cajón con tiradores metálicos
            for (int c = 0; c < 3; c++)
            {
                float y = 0.58f - (c * 0.21f);
                CreateBox($"Cajon_Front_{c}", deskObj.transform, new Vector3(-0.95f, y, -0.16f), new Vector3(0.38f, 0.18f, 0.02f), mats["DeskLaminate"]);
                CreateBox($"Tirador_{c}", deskObj.transform, new Vector3(-0.95f, y, -0.18f), new Vector3(0.14f, 0.02f, 0.02f), mats["MetalTrim"]);
            }
        }

        /// <summary>
        /// Genera una silla ergonómica ejecutiva con base de 5 radios y respaldo de malla.
        /// </summary>
        private static void BuildErgonomicChair(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject chairObj = new GameObject("Silla_Ergonomica");
            chairObj.transform.SetParent(parent, false);
            chairObj.transform.localPosition = new Vector3(0f, 0f, -0.65f);

            // Pistón neumático central
            CreateCylinder("Piston_Central", chairObj.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.05f, 0.22f, 0.05f), mats["BlackMetal"]);

            // Base de estrella con 5 radios y ruedas
            for (int r = 0; r < 5; r++)
            {
                float angle = r * 72f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 0.28f;
                CreateBox($"Radio_Base_{r}", chairObj.transform, new Vector3(dir.x * 0.5f, 0.06f, dir.z * 0.5f), new Vector3(0.04f, 0.03f, 0.26f), mats["BlackMetal"]);
                CreateSphere($"Rueda_{r}", chairObj.transform, new Vector3(dir.x, 0.04f, dir.z), new Vector3(0.05f, 0.05f, 0.05f), mats["BlackMetal"]);
            }

            // Asiento acolchado anatómico
            CreateBox("Asiento_Acolchado", chairObj.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.48f, 0.07f, 0.46f), mats["ChairMesh"]);

            // Respaldo ergonómico con soporte lumbar curvado
            CreateBox("Respaldo_Malla", chairObj.transform, new Vector3(0f, 0.78f, -0.22f), new Vector3(0.44f, 0.56f, 0.04f), mats["ChairMesh"]);
            CreateBox("Soporte_Lumbar", chairObj.transform, new Vector3(0f, 0.62f, -0.24f), new Vector3(0.36f, 0.12f, 0.03f), mats["BlackMetal"]);

            // Reposabrazos izquierdo y derecho
            CreateBox("Brazo_Izq", chairObj.transform, new Vector3(-0.25f, 0.62f, 0f), new Vector3(0.05f, 0.22f, 0.24f), mats["BlackMetal"]);
            CreateBox("Brazo_Der", chairObj.transform, new Vector3(0.25f, 0.62f, 0f), new Vector3(0.05f, 0.22f, 0.24f), mats["BlackMetal"]);
        }

        /// <summary>
        /// Genera la torre PC bancaria con luces LED de encendido y disco.
        /// </summary>
        private static void BuildPcTower(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject pcObj = new GameObject("Gabinete_PC_Torre");
            pcObj.transform.SetParent(parent, false);
            pcObj.transform.localPosition = new Vector3(-0.55f, 0.26f, 0.25f);

            // Chasis principal de la torre
            CreateBox("Chasis", pcObj.transform, Vector3.zero, new Vector3(0.20f, 0.44f, 0.42f), mats["PcChassis"]);

            // Rejilla frontal y bahías ópticas
            CreateBox("Rejilla_Frontal", pcObj.transform, new Vector3(0f, -0.06f, -0.215f), new Vector3(0.18f, 0.22f, 0.015f), mats["BlackMetal"]);
            CreateBox("Bahia_DVD", pcObj.transform, new Vector3(0f, 0.14f, -0.215f), new Vector3(0.16f, 0.04f, 0.015f), mats["BlackMetal"]);

            // LED verde de encendido
            GameObject pwrLed = CreateSphere("LED_Power_Verde", pcObj.transform, new Vector3(-0.05f, 0.18f, -0.216f), new Vector3(0.012f, 0.012f, 0.012f), mats["NeonGreen"]);
            Light pwrLight = pwrLed.AddComponent<Light>();
            pwrLight.type = LightType.Point;
            pwrLight.color = new Color(0.22f, 0.95f, 0.54f);
            pwrLight.range = 0.2f;
            pwrLight.intensity = 0.35f;

            // LED ámbar de actividad de disco HDD
            GameObject hddLed = CreateSphere("LED_Disco_Ambar", pcObj.transform, new Vector3(0.05f, 0.18f, -0.216f), new Vector3(0.012f, 0.012f, 0.012f), mats["NeonGreen"]);
            hddLed.GetComponent<Renderer>().sharedMaterial.color = new Color(1f, 0.75f, 0.2f);
        }

        /// <summary>
        /// Genera teclado, ratón, alfombrilla y taza de café de trabajo nocturno.
        /// </summary>
        private static void BuildPeripheralsAndCoffee(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject periphObj = new GameObject("Perifericos_Escritorio");
            periphObj.transform.SetParent(parent, false);

            // 1. Teclado ergonómico
            CreateBox("Teclado_Oficina", periphObj.transform, new Vector3(0f, 0.77f, -0.15f), new Vector3(0.44f, 0.018f, 0.15f), mats["BlackMetal"]);

            // 2. Alfombrilla y Mouse óptico
            CreateBox("Alfombrilla_Mouse", periphObj.transform, new Vector3(0.32f, 0.763f, -0.15f), new Vector3(0.20f, 0.004f, 0.22f), mats["ChairMesh"]);
            CreateBox("Mouse_Optico", periphObj.transform, new Vector3(0.32f, 0.778f, -0.15f), new Vector3(0.065f, 0.026f, 0.11f), mats["BlackMetal"]);

            // 3. Taza de café corporativa de madrugada
            GameObject mugObj = new GameObject("Taza_Cafe_Nocturno");
            mugObj.transform.SetParent(periphObj.transform, false);
            mugObj.transform.localPosition = new Vector3(-0.36f, 0.76f, -0.05f);

            CreateCylinder("Cuerpo_Taza", mugObj.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.08f, 0.05f, 0.08f), mats["CoffeeMug"]);
            CreateCylinder("Liquido_Cafe", mugObj.transform, new Vector3(0f, 0.088f, 0f), new Vector3(0.075f, 0.005f, 0.075f), mats["Coffee"]);
            CreateBox("Asa_Taza", mugObj.transform, new Vector3(-0.048f, 0.05f, 0f), new Vector3(0.025f, 0.06f, 0.02f), mats["CoffeeMug"]);
        }

        /// <summary>
        /// Genera el monitor con marco delgado, base cilíndrica y soporte.
        /// </summary>
        private static void BuildWorkstationMonitor(Transform parent, string id, string label, Dictionary<string, Material> mats)
        {
            GameObject monRoot = new GameObject(id);
            monRoot.transform.SetParent(parent, false);
            monRoot.transform.localPosition = new Vector3(0f, 0.76f, 0.18f);
            monRoot.tag = "Monitor";
            monRoot.layer = LayerMask.NameToLayer("Interactive");

            // Base de monitor
            CreateBox("Base_Pie", monRoot.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.26f, 0.015f, 0.20f), mats["BlackMetal"]);

            // Mástil soporte
            CreateCylinder("Mastil_Soporte", monRoot.transform, new Vector3(0f, 0.16f, 0.05f), new Vector3(0.04f, 0.16f, 0.04f), mats["BlackMetal"]);

            // Chasis y Marco del monitor (Pantalla 27")
            CreateBox("Marco_Bisel", monRoot.transform, new Vector3(0f, 0.36f, 0.01f), new Vector3(0.92f, 0.54f, 0.045f), mats["BlackMetal"]);

            // Colisionador para interacción VR por retícula
            BoxCollider col = monRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.36f, 0.01f);
            col.size = new Vector3(0.95f, 0.58f, 0.10f);

            GazeInteractable gaze = monRoot.AddComponent<GazeInteractable>();
            gaze.interactableLabel = label;
        }

        /// <summary>
        /// Genera los objetos exclusivos del Puesto 1 (Carla Mendoza): Post-It, Teléfono SOC y base del Token MFA.
        /// </summary>
        private static void BuildCarlaMendozaSpecifics(Transform parent, Dictionary<string, Material> mats)
        {
            // 1. Post-It de Credenciales adherido al monitor
            GameObject postItObj = new GameObject("PostIt_Credenciales");
            postItObj.transform.SetParent(parent, false);
            postItObj.transform.localPosition = new Vector3(0.38f, 0.88f, 0.16f);
            postItObj.transform.localRotation = Quaternion.Euler(0f, -8f, 0f);
            postItObj.tag = "Interactive";
            postItObj.layer = LayerMask.NameToLayer("Interactive");

            Material postItMat = CreatePbrMaterial("Mat_PostItFluorescentYellow_ffc24a", OfficeFurnitureBuilder.PostItFluorescentYellowColor, 0.15f, 0f);
            GameObject noteQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            noteQuad.name = "Nota_Fisica";
            noteQuad.transform.SetParent(postItObj.transform, false);
            noteQuad.transform.localScale = new Vector3(0.12f, 0.12f, 1f);
            noteQuad.GetComponent<Renderer>().sharedMaterial = postItMat;

            // Texto escrito de contraseñas expuestas
            GameObject textObj = new GameObject("Txt_Credenciales");
            textObj.transform.SetParent(noteQuad.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = "<b>SWIFT ALLIANCE</b>\nUser: C_MENDOZA\nPass: Andes$2015*Op1";
            tmp.fontSize = 1.3f;
            tmp.color = new Color(0.12f, 0.12f, 0.12f);
            tmp.alignment = TextAlignmentOptions.Center;

            BoxCollider col = postItObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.14f, 0.14f, 0.04f);
            GazeInteractable gaze = postItObj.AddComponent<GazeInteractable>();
            gaze.interactableLabel = "Examinar Post-It con Credenciales SWIFT";
            postItObj.AddComponent<PostItCredenciales>();

            // 2. Teléfono IP interactivo (Anexo SOC)
            GameObject phoneObj = new GameObject("Telefono_Anexo_SOC");
            phoneObj.transform.SetParent(parent, false);
            phoneObj.transform.localPosition = new Vector3(-0.85f, 0.77f, 0.15f);
            phoneObj.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            phoneObj.tag = "Interactive";
            phoneObj.layer = LayerMask.NameToLayer("Interactive");

            CreateBox("Base_Telefono", phoneObj.transform, new Vector3(0f, 0.035f, 0f), new Vector3(0.24f, 0.07f, 0.26f), mats["BlackMetal"]);
            CreateBox("Auricular", phoneObj.transform, new Vector3(-0.07f, 0.085f, 0f), new Vector3(0.06f, 0.05f, 0.25f), mats["BlackMetal"]);

            BoxCollider phoneCol = phoneObj.AddComponent<BoxCollider>();
            phoneCol.center = new Vector3(0f, 0.06f, 0f);
            phoneCol.size = new Vector3(0.26f, 0.12f, 0.28f);
            GazeInteractable phoneGaze = phoneObj.AddComponent<GazeInteractable>();
            phoneGaze.interactableLabel = "Teléfono IP: Llamar a Seguridad TI (Anexo 404)";
            phoneObj.AddComponent<TelefonoAnexoSoc>();

            // 3. Base del Token MFA / Bóveda
            GameObject tokenBaseObj = new GameObject("Token_MFA_Boveda");
            tokenBaseObj.transform.SetParent(parent, false);
            tokenBaseObj.transform.localPosition = new Vector3(0.18f, 0.77f, -0.05f);
            tokenBaseObj.tag = "Interactive";
            tokenBaseObj.layer = LayerMask.NameToLayer("Interactive");

            CreateBox("Carcasa_Token", tokenBaseObj.transform, new Vector3(0f, 0.012f, 0f), new Vector3(0.12f, 0.024f, 0.065f), mats["BlackMetal"]);
            BoxCollider tokCol = tokenBaseObj.AddComponent<BoxCollider>();
            tokCol.size = new Vector3(0.14f, 0.04f, 0.08f);
            GazeInteractable tokGaze = tokenBaseObj.AddComponent<GazeInteractable>();
            tokGaze.interactableLabel = "Token MFA Hardware (Código Dinámico)";
            tokenBaseObj.AddComponent<TokenMfaBoveda>();
        }

        #endregion

        #region Consola de Defensas y Tablero de Sospechosos

        private static void BuildCentralDefenseConsole(Transform parent, Dictionary<string, Material> mats)
        {
            // 1. Consola de Seguridad con los 3 interruptores 3D de DefenseControl
            SuspectBoardMeshBuilder.BuildDefenseConsole(parent, new Vector3(2.15f, 0.88f, 1.70f), Quaternion.Euler(-15f, 15f, 0f));

            // 2. Tablero táctico de corcho con las 4 fichas 3D de los sospechosos y líneas rojas en la pared Este
            SuspectBoardMeshBuilder.BuildTacticalCorkBoard(parent, new Vector3(4.88f, 1.6f, 1.5f), Quaternion.Euler(0f, -90f, 0f));
        }

        #endregion

        #region Primitivas Auxiliares

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

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        private static GameObject CreateSphere(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
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
