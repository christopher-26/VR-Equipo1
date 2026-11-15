using System;
using System.Collections;
using UnityEngine;
using AndesSim.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AndesSim.Environment
{
    /// <summary>
    /// Presets visuales de iluminación ambiental seleccionables desde el menú holográfico VR.
    /// </summary>
    public enum SocLightingPreset
    {
        OficinaClara = 0,   // Oficina corporativa luminosa y nítida (predeterminado)
        SocCyan = 1,        // Iluminación SOC cian-esmeralda de alto contraste
        CyberNocturno = 2   // Ambiente nocturno táctico equilibrado
    }

    /// <summary>
    /// Controlador central de la atmósfera lumínica de la sala de operaciones bancarias / SOC.
    /// Simulación VR ANDES-SIM (Unity 6 - URP / Standard / Cardboard XR).
    /// 
    /// Especificaciones requeridas:
    /// 1. Luces puntuales (Point Lights) tenues de color verde suave y blanco frío sobre cada escritorio
    ///    (Puesto 1: Carla Mendoza, Puesto 2: Analista Comercio Exterior, Puesto 3: Consola SIEM).
    /// 2. Luz direccional principal con intensidad muy baja (simulando luz de luna/ciudad nocturna a través del ventanal).
    /// 3. Luces de alerta rojas (#ff5a4e) que se encienden intermitentemente solo cuando se activa SimMode.Ataque
    ///    y el malware de Lazarus comienza a propagarse por las 3 PCs.
    /// 4. Optimizado al 100% para móviles Android (OpenGLES3 - 60 FPS estables):
    ///    - LightShadows.None en luces en tiempo real (cero costo de mapas de sombras).
    ///    - Rangos contenidos (4.5m) para evitar sobrepasar el límite de 4 luces por vértice/píxel de GPU móvil.
    ///    - Ambient Trilight nocturno para profundidad volumétrica sin luces adicionales.
    /// </summary>
    public class SocLightingController : MonoBehaviour
    {
        public static SocLightingController I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - SocLighting]";

        #region Paleta Cromática de Iluminación

        /// <summary>
        /// Color exacto de alerta crítica / intrusión: Rojo (#ff5a4e).
        /// </summary>
        public static readonly Color AlertRedColor = new Color(1.0f, 0.35f, 0.31f, 1.0f); // #ff5a4e

        /// <summary>
        /// Color de luz de luna nocturna fría a través del ventanal (#3a546e).
        /// </summary>
        public static readonly Color MoonlightColor = new Color(0.88f, 0.94f, 1.0f, 1.0f);

        /// <summary>
        /// Color de luminaria de puesto: Mezcla de blanco frío (#d4e6f1) y verde suave corporativo (#38f28a).
        /// </summary>
        public static readonly Color DeskCoolWhiteGreenColor = new Color(0.88f, 0.97f, 0.93f, 1.0f);

        /// <summary>
        /// Color de defensa activa: Verde esmeralda protegido (#38f28a).
        /// </summary>
        public static readonly Color DefenseShieldColor = new Color(0.22f, 0.95f, 0.54f, 1.0f);

        #endregion

        #region Campos Serializados (Inspector)

        [Header("--- 1. Luz Direccional Principal (Luna / Ciudad Nocturna) ---")]
        [Tooltip("Luz direccional simulando luz exterior nocturna a través del ventanal.")]
        [SerializeField] private Light moonlightDirectionalLight;
        [SerializeField] private float moonlightBaseIntensity = 0.15f;

        [Header("--- 2. Luces Puntuales de Escritorios (Verde Suave / Blanco Frío) ---")]
        [Tooltip("Luz sobre Puesto 1 (Carla Mendoza / SWIFT).")]
        [SerializeField] private Light desk1PointLight;

        [Tooltip("Luz sobre Puesto 2 (Analista Comercio Exterior).")]
        [SerializeField] private Light desk2PointLight;

        [Tooltip("Luz sobre Puesto 3 (Consola SIEM / Defensas).")]
        [SerializeField] private Light desk3PointLight;

        [SerializeField] private float deskLightBaseIntensity = 0.68f;
        [SerializeField] private float deskLightRange = 2.8f;

        [Header("--- 3. Luces de Alerta Roja (#ff5a4e - Modo Ataque) ---")]
        [Tooltip("Balizas / luces de emergencia de techo que titilan en Modo Ataque.")]
        [SerializeField] private Light[] emergencyAlertLights;

        [Tooltip("Renderers de las cúpulas de las balizas de emergencia para iluminar su material emisivo.")]
        [SerializeField] private Renderer[] emergencyBeaconRenderers;

        [SerializeField] private float emergencyStrobeFrequency = 1.8f;
        [SerializeField] private float emergencyMaxIntensity = 2.00f;

        [Header("--- 4. Estado de Iluminación ---")]
        [SerializeField] private bool isEmergencyActive = false;
        [SerializeField] private bool isDefenseShieldActive = false;
        [SerializeField] private SocLightingPreset activePreset = SocLightingPreset.OficinaClara;

        public bool IsEmergencyActive => isEmergencyActive;
        public bool IsDefenseShieldActive => isDefenseShieldActive;
        public float DeskLightRange => deskLightRange;
        public SocLightingPreset ActivePreset => activePreset;

        private CyberAmbientState _cyberTintState = CyberAmbientState.NormalInvestigation;

        #endregion

        // Estado interno
        private Coroutine _emergencyStrobeCoroutine;

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

            // Aplicar iluminación calibrada y equilibrada acorde al tema activo (sin sobreexposición en LDR)
            EnsureCompleteLightingIntegrity();
            ApplyLightingPreset(activePreset);

            // Asegurar que las luces de emergencia inicien apagadas
            SetEmergencyLightsState(false);
        }

        private void Start()
        {
            // Suscribirse a eventos de GameManager
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
                GameManager.I.OnScenarioEnded += HandleScenarioEnded;
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
                GameManager.I.OnScenarioEnded -= HandleScenarioEnded;
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
            }
        }

        #region Manejadores de Eventos del Juego

        private void HandleModeChanged(SimMode newMode)
        {
            if (newMode == SimMode.Ataque)
            {
                TriggerEmergencyRedAlert();
            }
            else if (newMode == SimMode.Defensa)
            {
                TriggerDefenseShieldLighting();
            }
        }

        private void HandleDefenseApplied(string defenseId)
        {
            TriggerDefenseShieldLighting();
        }

        private void HandleScenarioEnded(string resultMessage, bool timeCriteriaMet)
        {
            if (GameManager.I != null && GameManager.I.mode == SimMode.Defensa)
            {
                TriggerDefenseShieldLighting();
            }
            else
            {
                // Dejar tenue en evaluación final
                RestoreNormalSocAtmosphere();
            }
        }

        #endregion

        #region Control Dinámico de la Atmósfera Lumínica

        /// <summary>
        /// Activa la alerta de intrusión roja (#ff5a4e):
        /// - Enciende las balizas de emergencia con pulsación rítmica estroboscópica.
        /// - Tiñe las luces de los escritorios hacia un matiz ámbar/rojo tenue de crisis.
        /// - Reduce la luz de luna para acentuar el dramatismo en VR.
        /// </summary>
        public void TriggerEmergencyRedAlert()
        {
            isEmergencyActive = true;
            isDefenseShieldActive = false;

            if (_emergencyStrobeCoroutine != null)
            {
                StopCoroutine(_emergencyStrobeCoroutine);
            }
            _emergencyStrobeCoroutine = StartCoroutine(EmergencyStrobeRoutine());

            Debug.Log($"{LogPrefix} <color=#FF5A4E><b>[ALERTA SOC] Luces rojas de emergencia activadas (SimMode.Ataque)</b></color>");
        }

        /// <summary>
        /// Restablece la atmósfera lumínica normal y tranquila de investigación nocturna:
        /// - Apaga las balizas de emergencia.
        /// - Restablece las luces de escritorio a blanco frío / verde suave.
        /// </summary>
        public void RestoreNormalSocAtmosphere()
        {
            isEmergencyActive = false;
            isDefenseShieldActive = false;

            if (_emergencyStrobeCoroutine != null)
            {
                StopCoroutine(_emergencyStrobeCoroutine);
                _emergencyStrobeCoroutine = null;
            }

            SetEmergencyLightsState(false);
            SetDeskLightsColor(DeskCoolWhiteGreenColor, deskLightBaseIntensity);

            if (moonlightDirectionalLight != null)
            {
                moonlightDirectionalLight.intensity = moonlightBaseIntensity;
                moonlightDirectionalLight.color = MoonlightColor;
            }

            Debug.Log($"{LogPrefix} <color=#38F28A>Atmósfera normal del SOC restablecida.</color>");
        }

        /// <summary>
        /// Activa la atmósfera de escudo y contención de Modo Defensa:
        /// - Apaga las luces rojas.
        /// - Tiñe la iluminación con el verde esmeralda institucional (#38f28a).
        /// </summary>
        public void TriggerDefenseShieldLighting()
        {
            isEmergencyActive = false;
            isDefenseShieldActive = true;

            if (_emergencyStrobeCoroutine != null)
            {
                StopCoroutine(_emergencyStrobeCoroutine);
                _emergencyStrobeCoroutine = null;
            }

            SetEmergencyLightsState(false);
            SetDeskLightsColor(DefenseShieldColor, deskLightBaseIntensity * 1.15f);

            if (moonlightDirectionalLight != null)
            {
                moonlightDirectionalLight.color = new Color(0.68f, 0.94f, 0.80f, 1f);
                moonlightDirectionalLight.intensity = moonlightBaseIntensity * 1.15f;
            }

            Debug.Log($"{LogPrefix} <color=#38F28A><b>[DEFENSA ACTIVA] Atmósfera de escudo y contención aplicada.</b></color>");
        }

        #endregion

        #region Corrutina de Pulsación de Emergencia (Sin Asignaciones de Memoria)

        private IEnumerator EmergencyStrobeRoutine()
        {
            float timer = 0f;

            // Matiz de escritorio en emergencia (Rojizo tenue)
            Color deskAlertColor = new Color(0.95f, 0.45f, 0.35f, 1f);

            while (isEmergencyActive)
            {
                timer += Time.deltaTime * emergencyStrobeFrequency * Mathf.PI * 2f;

                // Onda senoidal suave modulada entre 0.15 y emergencyMaxIntensity
                float pulse = (Mathf.Sin(timer) + 1f) * 0.5f;
                float currentIntensity = Mathf.Lerp(0.15f, emergencyMaxIntensity, pulse);

                // Aplicar a las luces de emergencia
                if (emergencyAlertLights != null)
                {
                    for (int i = 0; i < emergencyAlertLights.Length; i++)
                    {
                        if (emergencyAlertLights[i] != null)
                        {
                            emergencyAlertLights[i].enabled = true;
                            emergencyAlertLights[i].intensity = currentIntensity;
                            emergencyAlertLights[i].color = AlertRedColor;
                        }
                    }
                }

                // Modular el color de los escritorios en tensión
                SetDeskLightsColor(Color.Lerp(DeskCoolWhiteGreenColor, deskAlertColor, pulse * 0.7f), deskLightBaseIntensity * 0.85f);

                yield return null;
            }

            SetEmergencyLightsState(false);
        }

        private void SetEmergencyLightsState(bool enabled)
        {
            if (emergencyAlertLights != null)
            {
                for (int i = 0; i < emergencyAlertLights.Length; i++)
                {
                    if (emergencyAlertLights[i] != null)
                    {
                        emergencyAlertLights[i].intensity = 0f;
                        emergencyAlertLights[i].enabled = enabled;
                    }
                }
            }
        }

        /// <summary>
        /// Cambia en tiempo real el estilo cromático y lumínico de la oficina desde el menú VR.
        /// Calibrado para URP móvil sin HDR (evita quemado blanco en paredes y escritorios).
        /// </summary>
        public void ApplyLightingPreset(SocLightingPreset preset)
        {
            activePreset = preset;

            float overheadMult;
            Color overheadTint;

            switch (preset)
            {
                case SocLightingPreset.SocCyan:
                    moonlightBaseIntensity = 0.12f;
                    deskLightBaseIntensity = 0.62f;
                    overheadMult = 0.86f;
                    overheadTint = new Color(0.68f, 0.90f, 1.00f, 1f);
                    if (!isEmergencyActive && !isDefenseShieldActive)
                    {
                        SetDeskLightsColor(new Color(0.55f, 0.88f, 0.98f, 1f), deskLightBaseIntensity);
                    }
                    break;

                case SocLightingPreset.CyberNocturno:
                    moonlightBaseIntensity = 0.08f;
                    deskLightBaseIntensity = 0.52f;
                    overheadMult = 0.68f;
                    overheadTint = new Color(0.62f, 0.76f, 0.96f, 1f);
                    if (!isEmergencyActive && !isDefenseShieldActive)
                    {
                        SetDeskLightsColor(new Color(0.66f, 0.82f, 0.98f, 1f), deskLightBaseIntensity);
                    }
                    break;

                default: // OficinaClara
                    moonlightBaseIntensity = 0.15f;
                    deskLightBaseIntensity = 0.68f;
                    overheadMult = 1.00f;
                    overheadTint = new Color(0.95f, 0.97f, 1.00f, 1f);
                    if (!isEmergencyActive && !isDefenseShieldActive)
                    {
                        SetDeskLightsColor(DeskCoolWhiteGreenColor, deskLightBaseIntensity);
                    }
                    break;
            }

            if (moonlightDirectionalLight != null && !isEmergencyActive)
            {
                moonlightDirectionalLight.intensity = moonlightBaseIntensity;
            }

            ApplyOverheadLightTuning(overheadMult, overheadTint);
            ApplyCyberAmbientTint(_cyberTintState);
        }

        private void ApplyOverheadLightTuning(float multiplier, Color tint)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Light pt = child.GetComponent<Light>();
                if (pt == null || pt.type != LightType.Point) continue;
                if (child.name == "Luz_Escritorio_Puesto1_Carla" ||
                    child.name == "Luz_Escritorio_Puesto2_Analista" ||
                    child.name == "Luz_Escritorio_Puesto3_Siem")
                {
                    continue;
                }

                bool isSconce = child.name.StartsWith("Luz_Aplique_", StringComparison.Ordinal);
                float baseInt = isSconce ? 0.28f : 0.74f;
                pt.intensity = baseInt * multiplier;
                pt.color = Color.Lerp(new Color(0.94f, 0.97f, 1.0f, 1f), tint, isSconce ? 0.25f : 0.45f);
            }
        }

        /// <summary>
        /// Sincroniza el matiz ambiental de RenderSettings con rango luminoso equilibrado sin quemado blanco.
        /// </summary>
        public void ApplyCyberAmbientTint(CyberAmbientState state)
        {
            _cyberTintState = state;
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;

            Color baseSky;
            Color baseEq;
            Color baseGround;
            float baseAmbientInt;

            switch (activePreset)
            {
                case SocLightingPreset.SocCyan:
                    baseSky = new Color(0.24f, 0.36f, 0.46f, 1f);
                    baseEq = new Color(0.18f, 0.27f, 0.35f, 1f);
                    baseGround = new Color(0.11f, 0.16f, 0.21f, 1f);
                    baseAmbientInt = 0.36f;
                    break;

                case SocLightingPreset.CyberNocturno:
                    baseSky = new Color(0.18f, 0.24f, 0.34f, 1f);
                    baseEq = new Color(0.14f, 0.18f, 0.26f, 1f);
                    baseGround = new Color(0.09f, 0.12f, 0.17f, 1f);
                    baseAmbientInt = 0.31f;
                    break;

                default: // OficinaClara
                    baseSky = new Color(0.30f, 0.36f, 0.44f, 1f);
                    baseEq = new Color(0.22f, 0.27f, 0.34f, 1f);
                    baseGround = new Color(0.14f, 0.17f, 0.22f, 1f);
                    baseAmbientInt = 0.42f;
                    break;
            }

            if (state == CyberAmbientState.EmergencyAttackInfection)
            {
                RenderSettings.ambientSkyColor = Color.Lerp(baseSky, new Color(0.48f, 0.16f, 0.14f, 1f), 0.55f);
                RenderSettings.ambientEquatorColor = Color.Lerp(baseEq, new Color(0.35f, 0.12f, 0.10f, 1f), 0.50f);
                RenderSettings.ambientGroundColor = baseGround;
                RenderSettings.ambientIntensity = Mathf.Max(0.34f, baseAmbientInt * 0.90f);
            }
            else if (state == CyberAmbientState.QuarantineDefenseShield)
            {
                RenderSettings.ambientSkyColor = Color.Lerp(baseSky, new Color(0.20f, 0.48f, 0.38f, 1f), 0.40f);
                RenderSettings.ambientEquatorColor = Color.Lerp(baseEq, new Color(0.15f, 0.38f, 0.29f, 1f), 0.36f);
                RenderSettings.ambientGroundColor = baseGround;
                RenderSettings.ambientIntensity = baseAmbientInt;
            }
            else
            {
                RenderSettings.ambientSkyColor = baseSky;
                RenderSettings.ambientEquatorColor = baseEq;
                RenderSettings.ambientGroundColor = baseGround;
                RenderSettings.ambientIntensity = baseAmbientInt;
            }
            RenderSettings.reflectionIntensity = 0.24f;
        }

        private void SetDeskLightsColor(Color color, float intensity)
        {
            if (desk1PointLight != null) { desk1PointLight.color = color; desk1PointLight.intensity = intensity; }
            if (desk2PointLight != null) { desk2PointLight.color = color; desk2PointLight.intensity = intensity; }
            if (desk3PointLight != null) { desk3PointLight.color = color; desk3PointLight.intensity = intensity; }
        }

        #endregion

        #region Configuración Ambiental Móvil (OpenGLES3 - 60 FPS)

        /// <summary>
        /// Aplica la configuración de RenderSettings para iluminación Trilight equilibrada sin sobreexposición LDR.
        /// </summary>
        public static void ApplyNocturnalAmbientSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.30f, 0.36f, 0.44f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.27f, 0.34f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.17f, 0.22f, 1f);
            RenderSettings.ambientIntensity = 0.42f;
            RenderSettings.reflectionIntensity = 0.24f;
        }

        private static Light EnsureOrUpdatePointLight(Transform parent, string lightName, Vector3 defaultPos, Color color, float intensity, float range, bool updatePositionIfExists = false)
        {
            GameObject obj = GameObject.Find(lightName);
            if (obj == null)
            {
                obj = new GameObject(lightName);
                obj.transform.SetParent(parent, false);
                obj.transform.position = defaultPos;
            }
            else if (updatePositionIfExists)
            {
                obj.transform.position = defaultPos;
            }

            Light pt = obj.GetComponent<Light>();
            if (pt == null) pt = obj.AddComponent<Light>();
            pt.type = LightType.Point;
            pt.color = color;
            pt.intensity = intensity;
            pt.range = range;
            pt.shadows = LightShadows.None; // 60 FPS Mobile OpenGLES3
            pt.enabled = true;
            return pt;
        }

        private static Vector3 ResolveDeskLightPos(string monitorName, Vector3 fallbackPos)
        {
            GameObject mon = GameObject.Find(monitorName);
            if (mon != null)
            {
                return mon.transform.position + new Vector3(0f, 1.45f, -0.35f);
            }
            return fallbackPos;
        }

        private static void EnsureOverheadZoneLights(Transform parent, bool isVnbOffice, bool updatePositions)
        {
            if (!isVnbOffice)
            {
                EnsureOrUpdatePointLight(parent, "Luz_Cenital_Vestibulo_Spawn", new Vector3(0f, 2.58f, -4.5f), new Color(0.98f, 0.96f, 0.92f), 2.10f, 8.5f, updatePositions);
                EnsureOrUpdatePointLight(parent, "Luz_Cenital_Central_Oficina", new Vector3(0f, 2.58f, 0.0f), new Color(0.98f, 0.99f, 1.0f), 2.10f, 8.5f, updatePositions);
                return;
            }

            // Cuadrícula 3x4 a Y = 2.50m (luz cómoda, bien visible y sin quemado blanco)
            // Fila Norte
            EnsureOrUpdatePointLight(parent, "Luz_Techo_Lounge_Oeste",      new Vector3(-5.80f, 2.50f,  2.20f), new Color(0.98f, 0.95f, 0.92f), 0.72f, 4.2f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Cenital_Vestibulo_Spawn", new Vector3(-2.40f, 2.50f,  2.60f), new Color(0.98f, 0.96f, 0.93f), 0.78f, 4.4f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Cenital_Central_Oficina", new Vector3( 1.80f, 2.50f,  2.25f), new Color(0.95f, 0.97f, 1.00f), 0.76f, 4.4f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Cocina_Pasillo_Este",     new Vector3( 5.80f, 2.50f,  2.20f), new Color(0.98f, 0.95f, 0.92f), 0.70f, 4.2f, updatePositions);

            // Fila Central
            EnsureOrUpdatePointLight(parent, "Luz_Techo_Centro_Oeste",      new Vector3(-5.90f, 2.50f, -1.25f), new Color(0.94f, 0.97f, 1.00f), 0.70f, 4.2f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Techo_Nave_CentroOeste",  new Vector3(-1.90f, 2.50f, -1.00f), new Color(0.95f, 0.98f, 1.00f), 0.74f, 4.2f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Techo_Nave_CentroEste",   new Vector3( 1.65f, 2.50f, -1.35f), new Color(0.95f, 0.98f, 1.00f), 0.74f, 4.2f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Techo_Oficina_Este",      new Vector3( 5.75f, 2.50f, -1.35f), new Color(0.98f, 0.96f, 0.94f), 0.72f, 4.2f, updatePositions);

            // Fila Sur
            EnsureOrUpdatePointLight(parent, "Luz_Sala_Descanso_Oeste",     new Vector3(-5.95f, 2.50f, -4.15f), new Color(0.94f, 0.97f, 1.00f), 0.72f, 4.2f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Techo_Pasillo_SurOeste",  new Vector3(-1.90f, 2.50f, -4.30f), new Color(0.95f, 0.98f, 1.00f), 0.74f, 4.2f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Pasillo_Tablero_Sur",     new Vector3( 2.20f, 2.50f, -4.35f), new Color(0.96f, 0.98f, 1.00f), 0.80f, 4.4f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Techo_Esquina_Sureste",   new Vector3( 5.95f, 2.50f, -4.35f), new Color(0.98f, 0.95f, 0.92f), 0.74f, 4.2f, updatePositions);

            // Apliques murales suaves de perímetro (bajo contraste sin focos quemados en pared)
            EnsureOrUpdatePointLight(parent, "Luz_Aplique_Puerta_Izq",      new Vector3(-3.45f, 2.08f,  3.15f), new Color(0.96f, 0.92f, 0.86f), 0.28f, 2.6f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Aplique_Puerta_Der",      new Vector3(-1.75f, 2.08f,  3.15f), new Color(0.96f, 0.92f, 0.86f), 0.28f, 2.6f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Aplique_Muro_Este_Norte", new Vector3( 7.12f, 2.08f,  1.20f), new Color(0.96f, 0.92f, 0.86f), 0.28f, 2.6f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Aplique_Muro_Este_Sur",   new Vector3( 7.12f, 2.08f, -3.40f), new Color(0.96f, 0.92f, 0.86f), 0.28f, 2.6f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Aplique_Muro_Sur_Este",   new Vector3( 5.80f, 2.08f, -5.12f), new Color(0.96f, 0.92f, 0.86f), 0.28f, 2.6f, updatePositions);
            EnsureOrUpdatePointLight(parent, "Luz_Aplique_Muro_Oeste",      new Vector3(-7.12f, 2.08f,  0.40f), new Color(0.92f, 0.96f, 1.00f), 0.28f, 2.6f, updatePositions);

            BoostVnbOfficeLamps();
        }

        private static void BoostVnbOfficeLamps()
        {
            GameObject floorRoot = GameObject.Find("Floor 0");
            if (floorRoot == null) return;

            Light[] embeddedLights = floorRoot.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < embeddedLights.Length; i++)
            {
                Light l = embeddedLights[i];
                if (l == null) continue;
                if (l.type == LightType.Point)
                {
                    l.enabled = true;
                    l.color = new Color(1.0f, 0.93f, 0.82f, 1f);
                    l.intensity = Mathf.Clamp(l.intensity, 0.22f, 0.40f);
                    l.range = Mathf.Clamp(l.range, 2.0f, 2.4f);
                    l.shadows = LightShadows.None;
                }
            }
        }

        /// <summary>
        /// Verifica y garantiza que todas las fuentes lumínicas clave estén activas dentro de un rango balanceado.
        /// </summary>
        public void EnsureCompleteLightingIntegrity()
        {
            bool isVnbOffice = GameObject.Find("Floor 0") != null;

            ApplyNocturnalAmbientSettings();

            if (moonlightDirectionalLight == null)
            {
                GameObject existingMoon = GameObject.Find("Luz_Direccional_Luna_Ventanal") ?? GameObject.Find("Directional Light");
                if (existingMoon != null)
                {
                    moonlightDirectionalLight = existingMoon.GetComponent<Light>();
                }
                else
                {
                    Light[] dirLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                    for (int i = 0; i < dirLights.Length; i++)
                    {
                        if (dirLights[i].type == LightType.Directional)
                        {
                            moonlightDirectionalLight = dirLights[i];
                            break;
                        }
                    }
                }
            }

            if (moonlightDirectionalLight != null)
            {
                moonlightDirectionalLight.intensity = Mathf.Clamp(moonlightBaseIntensity, 0.08f, 0.20f);
                moonlightDirectionalLight.color = new Color(0.94f, 0.96f, 1.0f);
                moonlightDirectionalLight.shadows = LightShadows.None;
            }

            if (desk1PointLight == null)
            {
                GameObject d1 = GameObject.Find("Luz_Escritorio_Puesto1_Carla");
                if (d1 != null) desk1PointLight = d1.GetComponent<Light>();
            }
            if (desk2PointLight == null)
            {
                GameObject d2 = GameObject.Find("Luz_Escritorio_Puesto2_Analista");
                if (d2 != null) desk2PointLight = d2.GetComponent<Light>();
            }
            if (desk3PointLight == null)
            {
                GameObject d3 = GameObject.Find("Luz_Escritorio_Puesto3_Siem");
                if (d3 != null) desk3PointLight = d3.GetComponent<Light>();
            }

            float clampedDeskInt = Mathf.Clamp(deskLightBaseIntensity, 0.45f, 0.85f);
            float clampedDeskRange = Mathf.Clamp(deskLightRange, 2.4f, 3.1f);
            if (desk1PointLight != null) { desk1PointLight.intensity = clampedDeskInt; desk1PointLight.range = clampedDeskRange; }
            if (desk2PointLight != null) { desk2PointLight.intensity = clampedDeskInt; desk2PointLight.range = clampedDeskRange; }
            if (desk3PointLight != null) { desk3PointLight.intensity = clampedDeskInt * 1.08f; desk3PointLight.range = clampedDeskRange + 0.15f; }

            EnsureOverheadZoneLights(transform, isVnbOffice, false);
        }

        #endregion

        #region Ensamblador Estático del Sistema de Iluminación (One-Click Setup)

        /// <summary>
        /// Construye y conecta el sistema completo de iluminación SOC en la escena:
        /// 1. Luz direccional suave balanceada sin bloqueo de techo.
        /// 2. 3 luces puntuales sobre los puestos de trabajo (Monitor 1, 2 y 3).
        /// 3. Luces cenitales bajo cada plafón LED del techo (vestíbulo, lounge, cocina, SOC y tablero sur).
        /// 4. Refuerzo a lámparas decorativas de Floor 0.
        /// 5. 2 balizas de techo para alerta roja (#ff5a4e).
        /// </summary>
        public static SocLightingController SetupLightingAtmosphere(Transform parent = null)
        {
            ApplyNocturnalAmbientSettings();

            bool isVnbOffice = GameObject.Find("Floor 0") != null;

            GameObject existingRoot = GameObject.Find("--- ILUMINACION SOC & ATMOSFERA ---");
            if (existingRoot != null)
            {
                if (Application.isPlaying) Destroy(existingRoot);
                else DestroyImmediate(existingRoot);
            }

            GameObject lightingRoot = new GameObject("--- ILUMINACION SOC & ATMOSFERA ---");
            if (parent != null) lightingRoot.transform.SetParent(parent, false);

            SocLightingController controller = lightingRoot.AddComponent<SocLightingController>();

            // 1. Luz Direccional Principal (sin sombras de techo cerrado para mantener claridad interior 60 FPS)
            Light moonLight = null;
            GameObject existingDir = GameObject.Find("Directional Light");
            if (existingDir != null)
            {
                moonLight = existingDir.GetComponent<Light>();
            }
            if (moonLight == null)
            {
                GameObject moonObj = new GameObject("Luz_Direccional_Luna_Ventanal");
                moonObj.transform.SetParent(lightingRoot.transform, false);
                moonObj.transform.rotation = Quaternion.Euler(45f, 170f, 0f);
                moonLight = moonObj.AddComponent<Light>();
                moonLight.type = LightType.Directional;
            }
            moonLight.color = new Color(0.94f, 0.96f, 1.0f, 1f);
            moonLight.intensity = isVnbOffice ? 0.15f : 0.22f;
            moonLight.shadows = LightShadows.None;

            // 2. Luces Puntuales sobre los 3 Escritorios (luz suave sin quemar monitores ni cubículos)
            Vector3 p1Pos = isVnbOffice ? ResolveDeskLightPos("Monitor_01", new Vector3(1.37f, 2.40f, -1.97f)) : new Vector3(-3.0f, 2.50f, 1.8f);
            Vector3 p2Pos = isVnbOffice ? ResolveDeskLightPos("Monitor_02", new Vector3(5.34f, 2.40f, -1.83f)) : new Vector3(0.0f, 2.50f, 1.8f);
            Vector3 p3Pos = isVnbOffice ? ResolveDeskLightPos("Monitor_03", new Vector3(-6.37f, 2.40f, -4.59f)) : new Vector3(3.0f, 2.50f, 1.8f);

            Light[] deskLights = new Light[3];
            deskLights[0] = EnsureOrUpdatePointLight(lightingRoot.transform, "Luz_Escritorio_Puesto1_Carla", p1Pos, DeskCoolWhiteGreenColor, 0.68f, 2.8f, true);
            deskLights[1] = EnsureOrUpdatePointLight(lightingRoot.transform, "Luz_Escritorio_Puesto2_Analista", p2Pos, DeskCoolWhiteGreenColor, 0.68f, 2.8f, true);
            deskLights[2] = EnsureOrUpdatePointLight(lightingRoot.transform, "Luz_Escritorio_Puesto3_Siem", p3Pos, DeskCoolWhiteGreenColor, isVnbOffice ? 0.74f : 0.85f, isVnbOffice ? 2.9f : 3.4f, true);

            // 3. Luces Cenitales de Entrada, Centro y Zonas de Oficina (alineadas al techo)
            EnsureOverheadZoneLights(lightingRoot.transform, isVnbOffice, true);

            // 4. Balizas de Alerta Roja (#ff5a4e) fijadas al techo (Y = 2.98m)
            Vector3[] beaconPositions = isVnbOffice
                ? new[] { new Vector3(1.40f, 2.98f, -0.55f), new Vector3(-6.10f, 2.98f, -4.55f) }
                : new[] { new Vector3(-1.8f, 3.16f, 1.5f), new Vector3(1.8f, 3.16f, 1.5f) };

            Light[] alertLights = new Light[2];
            Renderer[] beaconRenderers = new Renderer[2];

            Material beaconMat = RoomArchitectureBuilder.CreatePbrMaterial(
                "Mat_EmergencyBeaconRed_ff5a4e", AlertRedColor, 0.80f, 0.1f, AlertRedColor * 1.8f);

            for (int b = 0; b < 2; b++)
            {
                GameObject bRoot = new GameObject($"Baliza_Emergencia_Roja_0{b + 1}");
                bRoot.transform.SetParent(lightingRoot.transform, false);
                bRoot.transform.position = beaconPositions[b];

                // Base cilíndrica oscura en el techo
                GameObject bBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bBase.name = "Base_Fijacion";
                bBase.transform.SetParent(bRoot.transform, false);
                bBase.transform.localScale = new Vector3(0.14f, 0.02f, 0.14f);
                bBase.GetComponent<Renderer>().sharedMaterial = beaconMat;

                // Cúpula semiesférica roja
                GameObject bDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bDome.name = "Cupula_Baliza";
                bDome.transform.SetParent(bRoot.transform, false);
                bDome.transform.localPosition = new Vector3(0f, -0.04f, 0f);
                bDome.transform.localScale = new Vector3(0.12f, 0.10f, 0.12f);
                Renderer domeRend = bDome.GetComponent<Renderer>();
                domeRend.sharedMaterial = beaconMat;
                beaconRenderers[b] = domeRend;

                // Fuente de luz roja puntual de emergencia
                GameObject lObj = new GameObject("Luz_Alerta_Roja");
                lObj.transform.SetParent(bRoot.transform, false);
                lObj.transform.localPosition = new Vector3(0f, -0.08f, 0f);

                Light alertL = lObj.AddComponent<Light>();
                alertL.type = LightType.Point;
                alertL.color = AlertRedColor;
                alertL.intensity = 0f; // Inicia apagada
                alertL.range = 7.0f;
                alertL.shadows = LightShadows.None; // Optimizado OpenGLES3
                alertL.enabled = false;

                alertLights[b] = alertL;
            }

            // 5. Serializar referencias en el componente
#if UNITY_EDITOR
            SerializedObject sCtrl = new SerializedObject(controller);
            sCtrl.FindProperty("moonlightDirectionalLight").objectReferenceValue = moonLight;
            sCtrl.FindProperty("moonlightBaseIntensity").floatValue = 0.15f;
            sCtrl.FindProperty("desk1PointLight").objectReferenceValue = deskLights[0];
            sCtrl.FindProperty("desk2PointLight").objectReferenceValue = deskLights[1];
            sCtrl.FindProperty("desk3PointLight").objectReferenceValue = deskLights[2];
            sCtrl.FindProperty("deskLightBaseIntensity").floatValue = 0.68f;
            sCtrl.FindProperty("deskLightRange").floatValue = 2.8f;
            sCtrl.FindProperty("emergencyMaxIntensity").floatValue = 2.00f;

            SerializedProperty alertLightsProp = sCtrl.FindProperty("emergencyAlertLights");
            alertLightsProp.arraySize = 2;
            alertLightsProp.GetArrayElementAtIndex(0).objectReferenceValue = alertLights[0];
            alertLightsProp.GetArrayElementAtIndex(1).objectReferenceValue = alertLights[1];

            SerializedProperty beaconRendProp = sCtrl.FindProperty("emergencyBeaconRenderers");
            beaconRendProp.arraySize = 2;
            beaconRendProp.GetArrayElementAtIndex(0).objectReferenceValue = beaconRenderers[0];
            beaconRendProp.GetArrayElementAtIndex(1).objectReferenceValue = beaconRenderers[1];

            sCtrl.ApplyModifiedProperties();
#endif

            return controller;
        }

        #endregion
    }
}
