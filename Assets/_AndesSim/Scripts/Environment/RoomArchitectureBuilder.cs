using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AndesSim.Environment
{
    /// <summary>
    /// Generador procedural de la arquitectura interna de la "Oficina de pagos" de BANCO ANDINO.
    /// Simulación VR ANDES-SIM (Unity 6 - URP / Standard / Cardboard XR).
    /// 
    /// Especificaciones requeridas:
    /// 1. Piso de baldosas técnicas oscuras (#050d0a) con sutiles líneas divisorias de cuadrícula modular.
    /// 2. Cuatro paredes perimetrales con paneles acústicos corporativos y zócalos de iluminación LED verde tenue (#38f28a).
    /// 3. Gran ventanal de vidrio nocturno con vista a rascacielos iluminados y lluvia de código SWIFT decorativa en el cielo nocturno.
    /// 4. Letrero corporativo en relieve 3D sobre la pared principal: "BANCO ANDINO · OFICINA DE PAGOS".
    /// </summary>
    public static class RoomArchitectureBuilder
    {
        private const string LogPrefix = "[ANDES-SIM - RoomArchitectureBuilder]";

        #region Paleta Cromática Oficial de Arquitectura

        /// <summary>
        /// Color exacto para el piso de baldosas técnicas: Pizarra arquitectónica clara visible (#3d4752).
        /// </summary>
        public static readonly Color TechnicalTileFloorColor = new Color(0.24f, 0.28f, 0.32f, 1.0f);

        /// <summary>
        /// Líneas divisorias sutiles entre baldosas técnicas (#596b66).
        /// </summary>
        public static readonly Color TileSeamLineColor = new Color(0.35f, 0.42f, 0.40f, 1.0f);

        /// <summary>
        /// Iluminación LED tenue de zócalo y halo corporativo: Verde esmeralda (#38f28a).
        /// </summary>
        public static readonly Color NeonGreenLedColor = new Color(0.22f, 0.95f, 0.54f, 1.0f); // #38f28a

        /// <summary>
        /// Color base de paredes acústicas perimetrales: Pizarra corporativa clara y luminosa (#525c6b).
        /// </summary>
        public static readonly Color AcousticWallColor = new Color(0.32f, 0.36f, 0.42f, 1.0f);

        /// <summary>
        /// Listones de paneles acústicos absorbentes: Grafito medio (#424d59).
        /// </summary>
        public static readonly Color AcousticPanelSlatColor = new Color(0.26f, 0.30f, 0.35f, 1.0f);

        /// <summary>
        /// Aluminio anodizado para marcos de ventanas y molduras (#383e47).
        /// </summary>
        public static readonly Color AnodizedBlackMetalColor = new Color(0.22f, 0.24f, 0.28f, 1.0f);

        /// <summary>
        /// Vidrio tintado oscuro reflectante de ventanal (#060b10 con 80% opacidad).
        /// </summary>
        public static readonly Color WindowGlassTintedColor = new Color(0.024f, 0.043f, 0.063f, 0.80f);

        /// <summary>
        /// Placa metálica de soporte para letrero corporativo (#22272e).
        /// </summary>
        public static readonly Color CorporateSignBaseColor = new Color(0.133f, 0.153f, 0.180f, 1.0f);

        #endregion

        #region Creación de Materiales PBR y Texturas Procedurales

        private static Dictionary<string, Material> _cachedMats;

        public static Dictionary<string, Material> GetArchitectureMaterials()
        {
            if (_cachedMats != null && _cachedMats.ContainsKey("TechnicalFloor") && _cachedMats["TechnicalFloor"] != null)
            {
                return _cachedMats;
            }

            _cachedMats = new Dictionary<string, Material>();

            // Texturas procedurales
            Texture2D tilesTex = GenerateTechnicalTilesTexture(512, 64);
            Texture2D skylineTex = GenerateNightSkylineTexture(512, 256);
            Texture2D swiftCodeTex = GenerateSwiftCodeRainTexture(256, 512);

            // 1. Piso de baldosas técnicas (#050d0a con sutiles líneas divisorias)
            Material floorMat = CreatePbrMaterial("Mat_TechnicalTilesFloor_050d0a", TechnicalTileFloorColor, 0.38f, 0.12f);
            floorMat.mainTexture = tilesTex;
            floorMat.mainTextureScale = new Vector2(10f, 8f);
            _cachedMats["TechnicalFloor"] = floorMat;

            // 2. Paredes acústicas perimetrales
            _cachedMats["AcousticWall"] = CreatePbrMaterial("Mat_AcousticWallDark", AcousticWallColor, 0.18f, 0.02f);

            // 3. Listones y paneles acústicos absorbentes
            _cachedMats["AcousticSlats"] = CreatePbrMaterial("Mat_AcousticPanelSlats", AcousticPanelSlatColor, 0.28f, 0.05f);

            // 4. Zócalo LED verde tenue (#38f28a con emisión suave)
            _cachedMats["NeonLedGreen"] = CreatePbrMaterial("Mat_BaseboardNeonLed_38f28a",
                NeonGreenLedColor, 0.85f, 0.0f, NeonGreenLedColor * 2.2f);

            // 5. Marcos metálicos anodizados negros
            _cachedMats["AnodizedMetal"] = CreatePbrMaterial("Mat_AnodizedBlackMetal", AnodizedBlackMetalColor, 0.65f, 0.75f);

            // 6. Vidrio tintado oscuro para ventanal
            _cachedMats["WindowGlass"] = CreatePbrMaterial("Mat_TintedWindowGlass", WindowGlassTintedColor, 0.95f, 0.15f);

            // 7. Fondo de rascacielos nocturnos iluminados
            Material skylineMat = CreatePbrMaterial("Mat_NightSkylineView", Color.white, 0.10f, 0.0f, new Color(0.12f, 0.16f, 0.22f));
            skylineMat.mainTexture = skylineTex;
            _cachedMats["Skyline"] = skylineMat;

            // 8. Lluvia de código SWIFT decorativa en el cielo nocturno
            Material swiftRainMat = CreatePbrMaterial("Mat_SwiftCodeRain_Matrix",
                new Color(0.22f, 0.95f, 0.54f, 0.85f), 0.50f, 0.0f, NeonGreenLedColor * 2.8f);
            swiftRainMat.mainTexture = swiftCodeTex;
            _cachedMats["SwiftCodeRain"] = swiftRainMat;

            // 9. Letrero corporativo en relieve
            _cachedMats["SignBase"] = CreatePbrMaterial("Mat_CorporateSignBase_Alum", CorporateSignBaseColor, 0.75f, 0.85f);

            // 10. Plafón modular de techo
            _cachedMats["Ceiling"] = CreatePbrMaterial("Mat_CeilingAcousticPlates", new Color(0.08f, 0.09f, 0.11f), 0.12f, 0.0f);

            // 11. Difusor acrílico blanco arquitectónico de luminaria troffer
            _cachedMats["TrofferDiffuser"] = CreatePbrMaterial("Mat_CeilingTrofferDiffuser",
                new Color(0.95f, 0.98f, 1.0f), 0.90f, 0.05f, new Color(0.95f, 0.98f, 1.0f) * 1.6f);

            return _cachedMats;
        }

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

        #endregion

        #region Generación de Texturas Procedurales (Baldosas y Código SWIFT)

        /// <summary>
        /// Genera una textura procedural de baldosas técnicas modulares de 60cm en #050d0a con líneas divisorias sutiles.
        /// </summary>
        public static Texture2D GenerateTechnicalTilesTexture(int size, int tilePixelSize)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color baseTile = TechnicalTileFloorColor; // #050d0a
            Color seamColor = TileSeamLineColor;
            Color bevelColor = new Color(0.035f, 0.085f, 0.065f, 1f);

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int modX = x % tilePixelSize;
                    int modY = y % tilePixelSize;
                    bool isSeam = (modX == 0 || modY == 0);
                    bool isBevel = (modX == 1 || modY == 1 || modX == tilePixelSize - 1 || modY == tilePixelSize - 1);

                    if (isSeam)
                    {
                        pixels[y * size + x] = seamColor;
                    }
                    else if (isBevel)
                    {
                        pixels[y * size + x] = bevelColor;
                    }
                    else
                    {
                        // Sutil variación especular técnica
                        float noise = ((x ^ y) % 7) * 0.0015f;
                        pixels[y * size + x] = new Color(baseTile.r + noise, baseTile.g + noise, baseTile.b + noise, 1f);
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        /// <summary>
        /// Genera la textura del skyline nocturno con ventanas cálidas y cianes.
        /// </summary>
        public static Texture2D GenerateNightSkylineTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color skyCol = new Color(0.015f, 0.025f, 0.045f, 1f);
            Color buildingDark = new Color(0.035f, 0.045f, 0.065f, 1f);
            Color windowWarm = new Color(0.96f, 0.82f, 0.42f, 0.95f);
            Color windowCyan = new Color(0.28f, 0.88f, 0.96f, 0.95f);
            Color beaconRed = new Color(1.0f, 0.20f, 0.20f, 1f);

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = skyCol;

            System.Random rand = new System.Random(777);
            int curX = 6;
            while (curX < width - 16)
            {
                int bWidth = rand.Next(26, 56);
                int bHeight = rand.Next(80, 215);

                for (int y = 0; y < bHeight; y++)
                {
                    for (int x = curX; x < curX + bWidth && x < width; x++)
                    {
                        bool isWindow = (y > 10) && (y % 12 >= 3 && y % 12 <= 8) && ((x - curX) % 8 >= 2 && (x - curX) % 8 <= 5);
                        if (isWindow && rand.NextDouble() < 0.35)
                        {
                            pixels[y * width + x] = (rand.NextDouble() < 0.25) ? windowCyan : windowWarm;
                        }
                        else
                        {
                            pixels[y * width + x] = buildingDark;
                        }
                    }
                }

                // Luz baliza roja en la antena de edificios altos
                if (bHeight > 160 && curX + (bWidth / 2) < width)
                {
                    int ax = curX + (bWidth / 2);
                    for (int ay = bHeight; ay < Mathf.Min(height - 2, bHeight + 14); ay++)
                    {
                        pixels[ay * width + ax] = (ay >= bHeight + 11) ? beaconRed : AnodizedBlackMetalColor;
                    }
                }

                curX += bWidth + rand.Next(4, 12);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        /// <summary>
        /// Genera la textura procedural con columnas de lluvia de código SWIFT (caracteres y telegramas bancarios).
        /// </summary>
        public static Texture2D GenerateSwiftCodeRainTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color clearColor = new Color(0f, 0f, 0f, 0f);
            Color neonHead = new Color(0.85f, 1.0f, 0.90f, 0.95f);
            Color neonGreen = new Color(0.22f, 0.95f, 0.54f, 0.80f);
            Color neonTrail = new Color(0.08f, 0.50f, 0.28f, 0.40f);

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clearColor;

            System.Random rand = new System.Random(103);
            int columnSpacing = 16;

            for (int x = 4; x < width - 4; x += columnSpacing)
            {
                int streamLength = rand.Next(40, 110);
                int headY = rand.Next(0, height);

                for (int s = 0; s < streamLength; s++)
                {
                    int py = (headY - s + height) % height;
                    float t = (float)s / streamLength;

                    Color col = (s == 0) ? neonHead :
                                (s < 6) ? neonGreen :
                                Color.Lerp(neonGreen, neonTrail, t);

                    // Pequeños bloques de caracteres de 2x3 pixels
                    if (s % 4 != 0)
                    {
                        for (int dx = 0; dx < 3; dx++)
                        {
                            if (x + dx < width)
                            {
                                pixels[py * width + (x + dx)] = col;
                            }
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        #endregion

        #region 1. Ensamblador Completo de Arquitectura

        /// <summary>
        /// Construye la arquitectura completa de la sala de la "Oficina de pagos":
        /// - Piso de baldosas técnicas oscuras (#050d0a).
        /// - 4 Paredes perimetrales con paneles acústicos corporativos y zócalos de iluminación LED verde tenue (#38f28a).
        /// - Gran ventanal nocturno panorámico con rascacielos y lluvia de código SWIFT decorativa.
        /// - Letrero corporativo en relieve 3D: "BANCO ANDINO · OFICINA DE PAGOS".
        /// - Techo modular con luminarias y vestíbulo de entrada con portal.
        /// </summary>
        public static GameObject BuildCompleteRoomArchitecture(Transform parent, Dictionary<string, Material> mats = null)
        {
            if (mats == null) mats = GetArchitectureMaterials();

            GameObject archRoot = new GameObject("Arquitectura_Oficina_Pagos_3D");
            archRoot.transform.SetParent(parent, false);

            // 1. Piso de Baldosas Técnicas Oscuras (#050d0a)
            BuildTechnicalTileFloor(archRoot.transform, mats);

            // 2. Cuatro Paredes Perimetrales y Paneles Acústicos Corporativos
            BuildPerimeterWallsAndAcoustics(archRoot.transform, mats);

            // 3. Zócalos Perimetrales de Iluminación LED Verde Tenue (#38f28a)
            BuildBaseboardLedStrips(archRoot.transform, mats);

            // 4. Gran Ventanal de Vidrio Nocturno con Lluvia de Código SWIFT Decorativa
            BuildNightWindowWithSwiftRain(archRoot.transform, mats);

            // 5. Letrero Corporativo en Relieve 3D ("BANCO ANDINO · OFICINA DE PAGOS")
            BuildCorporateSignage(archRoot.transform, mats);

            // 6. Techo Modular con Luminarias de Oficina
            BuildModularCeiling(archRoot.transform, mats);

            // 7. Vestíbulo de Entrada con Marco de Portal
            BuildVestibuleArea(archRoot.transform, mats);

            Debug.Log($"{LogPrefix} <color=#38F28A><b>¡Arquitectura de la Oficina de pagos construida con éxito!</b></color>");
            return archRoot;
        }

        #endregion

        #region 1. Piso de Baldosas Técnicas Oscuras (#050d0a) con Líneas Divisorias

        private static void BuildTechnicalTileFloor(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorObj.name = "Piso_Baldosas_Tecnicas_050d0a";
            floorObj.transform.SetParent(parent, false);
            // Salón de 10m x 8m centrado en Z = 1.5m a altura Y = -0.05m
            floorObj.transform.position = new Vector3(0f, -0.05f, 1.5f);
            floorObj.transform.localScale = new Vector3(10.0f, 0.10f, 8.0f);
            floorObj.GetComponent<Renderer>().sharedMaterial = mats["TechnicalFloor"];

            // Zócalo de encuadre perimetral oscuro
            CreateBox("Bisel_Perimetral_Piso", parent,
                new Vector3(0f, -0.005f, 1.5f), new Vector3(9.96f, 0.015f, 7.96f), mats["AnodizedMetal"]);
        }

        #endregion

        #region 2. Cuatro Paredes Perimetrales con Paneles Acústicos Corporativos

        private static void BuildPerimeterWallsAndAcoustics(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject wallsRoot = new GameObject("Paredes_Perimetrales_PanelesAcusticos");
            wallsRoot.transform.SetParent(parent, false);

            // 2.1 Pared Norte (con vano para ventanal panorámico central)
            CreateBox("Pared_Norte_Lateral_Izq", wallsRoot.transform, new Vector3(-3.8f, 1.6f, 5.5f), new Vector3(2.4f, 3.2f, 0.20f), mats["AcousticWall"]);
            CreateBox("Pared_Norte_Lateral_Der", wallsRoot.transform, new Vector3(3.8f, 1.6f, 5.5f), new Vector3(2.4f, 3.2f, 0.20f), mats["AcousticWall"]);
            CreateBox("Pared_Norte_Dintel_Superior", wallsRoot.transform, new Vector3(0f, 2.85f, 5.5f), new Vector3(5.2f, 0.70f, 0.20f), mats["AcousticWall"]);
            CreateBox("Pared_Norte_Bajo_Ventanal", wallsRoot.transform, new Vector3(0f, 0.45f, 5.5f), new Vector3(5.2f, 0.90f, 0.20f), mats["AcousticWall"]);

            // 2.2 Pared Este (Pared lateral donde se ubica el Tablero de Sospechosos)
            CreateBox("Pared_Este", wallsRoot.transform, new Vector3(5.0f, 1.6f, 1.5f), new Vector3(0.20f, 3.2f, 8.0f), mats["AcousticWall"]);

            // Paneles acústicos corporativos en Pared Este (Slats con textura y profundidad)
            for (int i = -3; i <= 3; i++)
            {
                float z = 1.5f + (i * 1.05f);
                CreateBox($"Panel_Acustico_Este_{i + 4}", wallsRoot.transform,
                    new Vector3(4.88f, 1.6f, z), new Vector3(0.045f, 2.60f, 0.88f), mats["AcousticSlats"]);

                // Ranura vertical divisoria metálica
                CreateBox($"Perfil_Divisor_Este_{i + 4}", wallsRoot.transform,
                    new Vector3(4.86f, 1.6f, z), new Vector3(0.015f, 2.65f, 0.04f), mats["AnodizedMetal"]);
            }

            // 2.3 Pared Oeste (Pared lateral opuesta)
            CreateBox("Pared_Oeste", wallsRoot.transform, new Vector3(-5.0f, 1.6f, 1.5f), new Vector3(0.20f, 3.2f, 8.0f), mats["AcousticWall"]);

            // Paneles acústicos corporativos en Pared Oeste
            for (int i = -3; i <= 3; i++)
            {
                float z = 1.5f + (i * 1.05f);
                CreateBox($"Panel_Acustico_Oeste_{i + 4}", wallsRoot.transform,
                    new Vector3(-4.88f, 1.6f, z), new Vector3(0.045f, 2.60f, 0.88f), mats["AcousticSlats"]);

                CreateBox($"Perfil_Divisor_Oeste_{i + 4}", wallsRoot.transform,
                    new Vector3(-4.86f, 1.6f, z), new Vector3(0.015f, 2.65f, 0.04f), mats["AnodizedMetal"]);
            }

            // 2.4 Pared Sur (Pared divisoria hacia el vestíbulo con arcada central)
            CreateBox("Pared_Sur_Lateral_Izq", wallsRoot.transform, new Vector3(-3.3f, 1.6f, -2.5f), new Vector3(3.4f, 3.2f, 0.20f), mats["AcousticWall"]);
            CreateBox("Pared_Sur_Lateral_Der", wallsRoot.transform, new Vector3(3.3f, 1.6f, -2.5f), new Vector3(3.4f, 3.2f, 0.20f), mats["AcousticWall"]);
            CreateBox("Pared_Sur_Dintel_Paso", wallsRoot.transform, new Vector3(0f, 2.85f, -2.5f), new Vector3(3.2f, 0.70f, 0.20f), mats["AcousticWall"]);
        }

        #endregion

        #region 3. Zócalos Perimetrales de Iluminación LED Verde Tenue (#38f28a)

        private static void BuildBaseboardLedStrips(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject baseboardRoot = new GameObject("Zocalos_Iluminacion_LED_38f28a");
            baseboardRoot.transform.SetParent(parent, false);

            // Altura de zócalo a ras de suelo: Y = 0.045m
            float y = 0.045m == 0 ? 0.045f : 0.045f;

            // 3.1 Zócalos Pared Este (Longitud 8.0m)
            CreateBox("Zocalo_Base_Este", baseboardRoot.transform,
                new Vector3(4.88f, y, 1.5f), new Vector3(0.04f, 0.09f, 7.96f), mats["AnodizedMetal"]);
            CreateBox("Tira_LED_Este", baseboardRoot.transform,
                new Vector3(4.855f, y + 0.02f, 1.5f), new Vector3(0.015f, 0.025f, 7.90f), mats["NeonLedGreen"]);

            // 3.2 Zócalos Pared Oeste (Longitud 8.0m)
            CreateBox("Zocalo_Base_Oeste", baseboardRoot.transform,
                new Vector3(-4.88f, y, 1.5f), new Vector3(0.04f, 0.09f, 7.96f), mats["AnodizedMetal"]);
            CreateBox("Tira_LED_Oeste", baseboardRoot.transform,
                new Vector3(-4.855f, y + 0.02f, 1.5f), new Vector3(0.015f, 0.025f, 7.90f), mats["NeonLedGreen"]);

            // 3.3 Zócalos Pared Norte (Lado izquierdo y derecho del ventanal)
            CreateBox("Zocalo_Base_Norte_Izq", baseboardRoot.transform,
                new Vector3(-3.8f, y, 5.38f), new Vector3(2.36f, 0.09f, 0.04f), mats["AnodizedMetal"]);
            CreateBox("Tira_LED_Norte_Izq", baseboardRoot.transform,
                new Vector3(-3.8f, y + 0.02f, 5.355f), new Vector3(2.30f, 0.025f, 0.015f), mats["NeonLedGreen"]);

            CreateBox("Zocalo_Base_Norte_Der", baseboardRoot.transform,
                new Vector3(3.8f, y, 5.38f), new Vector3(2.36f, 0.09f, 0.04f), mats["AnodizedMetal"]);
            CreateBox("Tira_LED_Norte_Der", baseboardRoot.transform,
                new Vector3(3.8f, y + 0.02f, 5.355f), new Vector3(2.30f, 0.025f, 0.015f), mats["NeonLedGreen"]);

            // Tira LED bajo la peana del ventanal norte
            CreateBox("Tira_LED_Bajo_Ventanal", baseboardRoot.transform,
                new Vector3(0f, y + 0.02f, 5.355f), new Vector3(5.15f, 0.025f, 0.015f), mats["NeonLedGreen"]);

            // 3.4 Zócalos Pared Sur (A los lados de la arcada)
            CreateBox("Zocalo_Base_Sur_Izq", baseboardRoot.transform,
                new Vector3(-3.3f, y, -2.38f), new Vector3(3.36f, 0.09f, 0.04f), mats["AnodizedMetal"]);
            CreateBox("Tira_LED_Sur_Izq", baseboardRoot.transform,
                new Vector3(-3.3f, y + 0.02f, -2.355f), new Vector3(3.30f, 0.025f, 0.015f), mats["NeonLedGreen"]);

            CreateBox("Zocalo_Base_Sur_Der", baseboardRoot.transform,
                new Vector3(3.3f, y, -2.38f), new Vector3(3.36f, 0.09f, 0.04f), mats["AnodizedMetal"]);
            CreateBox("Tira_LED_Sur_Der", baseboardRoot.transform,
                new Vector3(3.3f, y + 0.02f, -2.355f), new Vector3(3.30f, 0.025f, 0.015f), mats["NeonLedGreen"]);
        }

        #endregion

        #region 4. Gran Ventanal de Vidrio Nocturno con Lluvia de Código SWIFT

        private static void BuildNightWindowWithSwiftRain(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject windowRoot = new GameObject("Ventanal_Nocturno_Con_Lluvia_SWIFT");
            windowRoot.transform.SetParent(parent, false);
            windowRoot.transform.position = new Vector3(0f, 1.70f, 5.50f);

            // 4.1 Marco perimetral de aluminio anodizado negro (5.30m ancho x 1.65m alto)
            CreateBox("Marco_Ventanal_Exterior", windowRoot.transform,
                Vector3.zero, new Vector3(5.30f, 1.65f, 0.08f), mats["AnodizedMetal"]);

            // 4.2 Parteluces y montantes verticales de la estructura de la ventana (4 vanos panorámicos)
            float[] parteluzX = { -1.70f, -0.57f, 0.57f, 1.70f };
            for (int p = 0; p < parteluzX.Length; p++)
            {
                CreateBox($"Parteluz_Vertical_{p + 1}", windowRoot.transform,
                    new Vector3(parteluzX[p], 0f, 0f), new Vector3(0.06f, 1.55f, 0.07f), mats["AnodizedMetal"]);
            }

            // Travesaño horizontal medio
            CreateBox("Travesaño_Horizontal_Medio", windowRoot.transform,
                new Vector3(0f, 0.35f, 0f), new Vector3(5.20f, 0.04f, 0.06f), mats["AnodizedMetal"]);

            // 4.3 Vidrio tintado oscuro doble acristalamiento
            CreateBox("Vidrio_Ventana_Tintado", windowRoot.transform,
                new Vector3(0f, 0f, 0.01f), new Vector3(5.15f, 1.52f, 0.025f), mats["WindowGlass"]);

            // 4.4 Capa de Lluvia de Código SWIFT Decorativa en el Cielo Nocturno
            // Ubicada inmediatamente detrás del vidrio tintado
            GameObject swiftRainQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            swiftRainQuad.name = "Plano_Lluvia_Codigo_SWIFT";
            swiftRainQuad.transform.SetParent(windowRoot.transform, false);
            swiftRainQuad.transform.localPosition = new Vector3(0f, 0f, 0.22f);
            swiftRainQuad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            swiftRainQuad.transform.localScale = new Vector3(5.50f, 1.80f, 1.0f);
            swiftRainQuad.GetComponent<Renderer>().sharedMaterial = mats["SwiftCodeRain"];

            // Componente ligero de animación de UVs para que la lluvia de código descienda suavemente
            swiftRainQuad.AddComponent<SwiftCodeRainAnimator>();

            // Letrero holográfico sutil con telegramas bancarios SWIFT superpuestos
            GameObject swiftTextObj = new GameObject("Txt_TelegramasSwiftOverlay");
            swiftTextObj.transform.SetParent(windowRoot.transform, false);
            swiftTextObj.transform.localPosition = new Vector3(0f, 0f, 0.20f);
            swiftTextObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            TextMeshPro swiftTmp = swiftTextObj.AddComponent<TextMeshPro>();
            swiftTmp.fontSize = 1.35f;
            swiftTmp.alignment = TextAlignmentOptions.Center;
            swiftTmp.color = new Color(0.22f, 0.95f, 0.54f, 0.45f);
            swiftTmp.text = "MT103 :20:REF-20150129-9942 // :32A:USD81000000,00 // :50K:/192-8841 BANCO ANDINO\n" +
                            ":59:/RCBC-MAKATI-991 // :71A:OUR // :72:/BNF/RASEL AHLAM // SWIFT-NET FIN MSG // AUTH OK\n" +
                            "MT202 :21:ORDER-36-DEF // :53A:NY-FED-RES // :58A:BANGLADESH-BANK // STATUS: INTERCEPTED";

            // 4.5 Telón de fondo con vista a rascacielos nocturnos iluminados
            GameObject skylineQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skylineQuad.name = "Backdrop_Rascacielos_Nocturnos";
            skylineQuad.transform.SetParent(windowRoot.transform, false);
            skylineQuad.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            skylineQuad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            skylineQuad.transform.localScale = new Vector3(6.20f, 2.30f, 1.0f);
            skylineQuad.GetComponent<Renderer>().sharedMaterial = mats["Skyline"];
        }

        #endregion

        #region 5. Letrero Corporativo en Relieve 3D ("BANCO ANDINO · OFICINA DE PAGOS")

        private static void BuildCorporateSignage(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject signRoot = new GameObject("Letrero_Corporativo_BancoAndino");
            signRoot.transform.SetParent(parent, false);
            // Centrado sobre la pared norte a Y = 2.85m mirando hacia el sur (rotación Y = 180°)
            signRoot.transform.position = new Vector3(0f, 2.85f, 5.42f);
            signRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // 5.1 Placa base de aluminio cepillado / compuesto oscuro
            CreateBox("Placa_Aluminio_Base", signRoot.transform,
                Vector3.zero, new Vector3(4.80f, 0.44f, 0.04f), mats["SignBase"]);

            // Marco perimetral biselado
            CreateBox("Bisel_Marco_Sign", signRoot.transform,
                new Vector3(0f, 0f, 0.015f), new Vector3(4.84f, 0.48f, 0.015f), mats["AnodizedMetal"]);

            // 5.2 Halo / Tira de neón verde tenue en el borde inferior (#38f28a)
            CreateBox("Neon_Halo_Sign_Bottom", signRoot.transform,
                new Vector3(0f, -0.225f, -0.025f), new Vector3(4.80f, 0.022f, 0.02f), mats["NeonLedGreen"]);

            // 5.3 Texto en relieve 3D corporativo exacto
            GameObject textObj = new GameObject("Txt_BancoAndinoOficinaPagos");
            textObj.transform.SetParent(signRoot.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.025f);

            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            // Texto solicitado exacto: "BANCO ANDINO · OFICINA DE PAGOS"
            tmp.text = "<b><color=#38F28A>BANCO ANDINO</color></b> · <color=#E2E8F0>OFICINA DE PAGOS</color>";
            tmp.fontSize = 2.45f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.92f, 0.95f, 0.98f);
            tmp.fontStyle = FontStyles.Bold;
        }

        #endregion

        #region 6. Techo Modular y Luminarias de Oficina

        private static void BuildModularCeiling(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject ceilingObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceilingObj.name = "Techo_Modular_Oficina";
            ceilingObj.transform.SetParent(parent, false);
            ceilingObj.transform.position = new Vector3(0f, 3.25f, 1.5f);
            ceilingObj.transform.localScale = new Vector3(10.0f, 0.10f, 8.0f);
            ceilingObj.GetComponent<Renderer>().sharedMaterial = mats["Ceiling"];

            // 3 Luminarias suspendidas sobre los puestos de trabajo
            float[] lightX = { -2.8f, 0.0f, 2.8f };
            for (int i = 0; i < 3; i++)
            {
                CreateCeilingTroffer(parent, $"Luminaria_Puesto_0{i + 1}", new Vector3(lightX[i], 3.15f, 1.5f), mats);
            }

            // Reloj digital de pared bancario en la pared lateral izquierda
            GameObject wallClockObj = new GameObject("Reloj_Pared_Digital");
            wallClockObj.transform.SetParent(parent, false);
            wallClockObj.transform.position = new Vector3(-4.88f, 2.45f, 1.5f);
            wallClockObj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            CreateBox("Marco_Reloj", wallClockObj.transform, Vector3.zero, new Vector3(0.85f, 0.35f, 0.05f), mats["AnodizedMetal"]);
            CreateBox("Fondo_Display", wallClockObj.transform, new Vector3(0f, 0f, -0.026f), new Vector3(0.80f, 0.30f, 0.01f), mats["TechnicalFloor"]);

            GameObject clockDigits = new GameObject("Txt_DigitosReloj");
            clockDigits.transform.SetParent(wallClockObj.transform, false);
            clockDigits.transform.localPosition = new Vector3(0f, 0f, -0.035f);
            TextMeshPro clockTmp = clockDigits.AddComponent<TextMeshPro>();
            clockTmp.text = "<color=#FF5A4E><b>23:45:12</b></color>\n<size=55%><color=#A0AEC0>29 ENE 2015 · LIMA</color></size>";
            clockTmp.fontSize = 2.0f;
            clockTmp.alignment = TextAlignmentOptions.Center;
        }

        private static void CreateCeilingTroffer(Transform parent, string name, Vector3 pos, Dictionary<string, Material> mats)
        {
            GameObject troffer = new GameObject(name);
            troffer.transform.SetParent(parent, false);
            troffer.transform.position = pos;

            // Marco exterior de aluminio empotrado en el cielo raso
            Material metalMat = mats.ContainsKey("AnodizedMetal") ? mats["AnodizedMetal"] : null;
            Material diffuserMat = mats.ContainsKey("TrofferDiffuser") ? mats["TrofferDiffuser"] : mats["NeonLedGreen"];

            CreateBox("Marco_Aluminio_Troffer", troffer.transform,
                Vector3.zero, new Vector3(1.24f, 0.04f, 0.64f), metalMat);

            // Panel difusor acrílico blanco arquitectónico (luz no cegadora)
            CreateBox("Panel_Difusor_Troffer", troffer.transform,
                new Vector3(0f, -0.015f, 0f), new Vector3(1.18f, 0.02f, 0.58f), diffuserMat);

            // Luz suave puntual de iluminación uniforme
            GameObject lObj = new GameObject(name + "_Light");
            lObj.transform.SetParent(troffer.transform, false);
            lObj.transform.localPosition = new Vector3(0f, -0.4f, 0f);

            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.95f, 0.97f, 1.0f);
            l.intensity = 1.25f;
            l.range = 6.5f;
            l.shadows = LightShadows.None;
        }

        #endregion

        #region 7. Vestíbulo de Entrada (Lobby SOC)

        private static void BuildVestibuleArea(Transform parent, Dictionary<string, Material> mats)
        {
            GameObject vestibuleRoot = new GameObject("Sala_Vestibulo_SOC");
            vestibuleRoot.transform.SetParent(parent, false);

            // Piso del Vestíbulo (6m x 3.5m centrado en Z = -4.25m)
            GameObject vFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vFloor.name = "Piso_Vestibulo";
            vFloor.transform.SetParent(vestibuleRoot.transform, false);
            vFloor.transform.position = new Vector3(0f, -0.05f, -4.25f);
            vFloor.transform.localScale = new Vector3(6.0f, 0.10f, 3.5f);
            vFloor.GetComponent<Renderer>().sharedMaterial = mats["TechnicalFloor"];

            // Paredes del Vestíbulo
            CreateBox("Pared_Fondo_Vestibulo", vestibuleRoot.transform, new Vector3(0f, 1.6f, -6.0f), new Vector3(6.0f, 3.2f, 0.20f), mats["AcousticWall"]);
            CreateBox("Pared_Lat_Izq_Vestibulo", vestibuleRoot.transform, new Vector3(-3.0f, 1.6f, -4.25f), new Vector3(0.20f, 3.2f, 3.5f), mats["AcousticWall"]);
            CreateBox("Pared_Lat_Der_Vestibulo", vestibuleRoot.transform, new Vector3(3.0f, 1.6f, -4.25f), new Vector3(0.20f, 3.2f, 3.5f), mats["AcousticWall"]);

            // Techo del Vestíbulo
            GameObject vCeiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vCeiling.name = "Techo_Vestibulo";
            vCeiling.transform.SetParent(vestibuleRoot.transform, false);
            vCeiling.transform.position = new Vector3(0f, 3.25f, -4.25f);
            vCeiling.transform.localScale = new Vector3(6.0f, 0.10f, 3.5f);
            vCeiling.GetComponent<Renderer>().sharedMaterial = mats["Ceiling"];

            // Luminaria sobre la terminal del menú de inicio
            CreateCeilingTroffer(vestibuleRoot.transform, "Luminaria_Vestibulo_Menu", new Vector3(0f, 3.15f, -4.2f), mats);

            // Letrero luminoso de salida de emergencia sobre el portal (Z = -2.5m)
            GameObject exitSign = new GameObject("Letrero_SalidaEmergencia");
            exitSign.transform.SetParent(vestibuleRoot.transform, false);
            exitSign.transform.position = new Vector3(0f, 2.75f, -2.48f);
            exitSign.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            CreateBox("Carcasa_Sign", exitSign.transform, Vector3.zero, new Vector3(0.70f, 0.22f, 0.04f), mats["AnodizedMetal"]);
            CreateBox("Panel_Luz_Sign", exitSign.transform, new Vector3(0f, 0f, -0.022f), new Vector3(0.66f, 0.18f, 0.01f), mats["NeonLedGreen"]);

            GameObject exitTxtObj = new GameObject("Txt_Exit");
            exitTxtObj.transform.SetParent(exitSign.transform, false);
            exitTxtObj.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            TextMeshPro exitTmp = exitTxtObj.AddComponent<TextMeshPro>();
            exitTmp.text = "<b>SALIDA DE EMERGENCIA</b>";
            exitTmp.fontSize = 1.6f;
            exitTmp.alignment = TextAlignmentOptions.Center;
            exitTmp.color = Color.white;

            // Guías de neón verde en el suelo hacia el portal
            CreateBox("Neon_Piso_Guia_Central", vestibuleRoot.transform,
                new Vector3(0f, 0.005f, -4.25f), new Vector3(0.06f, 0.01f, 3.0f), mats["NeonLedGreen"]);
            CreateBox("Neon_Zocalo_Vestibulo_Izq", vestibuleRoot.transform,
                new Vector3(-2.89f, 0.045f, -4.25f), new Vector3(0.02f, 0.06f, 3.3f), mats["NeonLedGreen"]);
            CreateBox("Neon_Zocalo_Vestibulo_Der", vestibuleRoot.transform,
                new Vector3(2.89f, 0.045f, -4.25f), new Vector3(0.02f, 0.06f, 3.3f), mats["NeonLedGreen"]);
        }

        #endregion

        #region Utilidades de Primitivas

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

        #endregion
    }
}
