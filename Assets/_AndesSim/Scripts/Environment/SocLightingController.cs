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
        public static readonly Color MoonlightColor = new Color(0.23f, 0.33f, 0.43f, 1.0f);

        /// <summary>
        /// Color de luminaria de puesto: Mezcla de blanco frío (#d4e6f1) y verde suave corporativo (#38f28a).
        /// </summary>
        public static readonly Color DeskCoolWhiteGreenColor = new Color(0.72f, 0.92f, 0.82f, 1.0f);

        /// <summary>
        /// Color de defensa activa: Verde esmeralda protegido (#38f28a).
        /// </summary>
        public static readonly Color DefenseShieldColor = new Color(0.22f, 0.95f, 0.54f, 1.0f);

        #endregion

        #region Campos Serializados (Inspector)

        [Header("--- 1. Luz Direccional Principal (Luna / Ciudad Nocturna) ---")]
        [Tooltip("Luz direccional simulando luz exterior nocturna a través del ventanal.")]
        [SerializeField] private Light moonlightDirectionalLight;
        [SerializeField] private float moonlightBaseIntensity = 2.80f;

        [Header("--- 2. Luces Puntuales de Escritorios (Verde Suave / Blanco Frío) ---")]
        [Tooltip("Luz sobre Puesto 1 (Carla Mendoza / SWIFT).")]
        [SerializeField] private Light desk1PointLight;

        [Tooltip("Luz sobre Puesto 2 (Analista Comercio Exterior).")]
        [SerializeField] private Light desk2PointLight;

        [Tooltip("Luz sobre Puesto 3 (Consola SIEM / Defensas).")]
        [SerializeField] private Light desk3PointLight;

        [SerializeField] private float deskLightBaseIntensity = 2.50f;
        [SerializeField] private float deskLightRange = 8.5f;

        [Header("--- 3. Luces de Alerta Roja (#ff5a4e - Modo Ataque) ---")]
        [Tooltip("Balizas / luces de emergencia de techo que titilan en Modo Ataque.")]
        [SerializeField] private Light[] emergencyAlertLights;

        [Tooltip("Renderers de las cúpulas de las balizas de emergencia para iluminar su material emisivo.")]
        [SerializeField] private Renderer[] emergencyBeaconRenderers;

        [SerializeField] private float emergencyStrobeFrequency = 1.8f;
        [SerializeField] private float emergencyMaxIntensity = 1.50f;

        [Header("--- 4. Estado de Iluminación ---")]
        [SerializeField] private bool isEmergencyActive = false;
        [SerializeField] private bool isDefenseShieldActive = false;

        public bool IsEmergencyActive => isEmergencyActive;
        public bool IsDefenseShieldActive => isDefenseShieldActive;
        public float DeskLightRange => deskLightRange;

        #endregion

        // Estado interno
        private Coroutine _emergencyStrobeCoroutine;
        private Material _beaconEmissiveMaterialInstance;

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

            // Aplicar configuración de ambiente nocturno bancario optimizado para mobile
            ApplyNocturnalAmbientSettings();

            // Garantizar que la iluminación sea clara en vestíbulo de spawn, centro de sala y escritorios
            EnsureCompleteLightingIntegrity();

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

            if (_beaconEmissiveMaterialInstance != null)
            {
                Destroy(_beaconEmissiveMaterialInstance);
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
                moonlightDirectionalLight.color = new Color(0.20f, 0.45f, 0.35f, 1f);
                moonlightDirectionalLight.intensity = moonlightBaseIntensity * 1.2f;
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

        private void SetDeskLightsColor(Color color, float intensity)
        {
            if (desk1PointLight != null) { desk1PointLight.color = color; desk1PointLight.intensity = intensity; }
            if (desk2PointLight != null) { desk2PointLight.color = color; desk2PointLight.intensity = intensity; }
            if (desk3PointLight != null) { desk3PointLight.color = color; desk3PointLight.intensity = intensity; }
        }

        #endregion

        #region Configuración Ambiental Móvil (OpenGLES3 - 60 FPS)

        /// <summary>
        /// Aplica la configuración de RenderSettings para iluminación Trilight nocturna con visibilidad óptima.
        /// </summary>
        public static void ApplyNocturnalAmbientSettings()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.80f, 0.90f, 1f);     // Cielo nocturno ultra brillante
            RenderSettings.ambientEquatorColor = new Color(0.60f, 0.68f, 0.72f, 1f); // Tono medio claro
            RenderSettings.ambientGroundColor = new Color(0.45f, 0.50f, 0.55f, 1f);  // Rebote muy claro de piso
            RenderSettings.ambientIntensity = 2.0f;
            RenderSettings.reflectionIntensity = 1.0f;
        }

        /// <summary>
        /// Verifica y garantiza que todas las fuentes lumínicas clave (luz direccional, vestíbulo,
        /// centro de oficina y escritorios) estén activas, con alcance y potencia adecuados.
        /// Se ejecuta en Awake() para asegurar que la escena nunca se vea a oscuras al presionar Play.
        /// </summary>
        public void EnsureCompleteLightingIntegrity()
        {
            // 1. Luz Direccional (Luna clara a través del ventanal)
            if (moonlightDirectionalLight == null)
            {
                GameObject existingMoon = GameObject.Find("Luz_Direccional_Luna_Ventanal");
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
                moonlightDirectionalLight.intensity = moonlightBaseIntensity;
                moonlightDirectionalLight.color = new Color(1.0f, 0.98f, 0.95f);
            }

            // 2. Luces de escritorio
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

            if (desk1PointLight != null) { desk1PointLight.intensity = deskLightBaseIntensity; desk1PointLight.range = deskLightRange; }
            if (desk2PointLight != null) { desk2PointLight.intensity = deskLightBaseIntensity; desk2PointLight.range = deskLightRange; }
            if (desk3PointLight != null) { desk3PointLight.intensity = deskLightBaseIntensity; desk3PointLight.range = deskLightRange; }

            // 3. Luz Cenital del Vestíbulo (Spawn / Entrada Z = -4.25m a -5.0m)
            GameObject vestObj = GameObject.Find("Luz_Cenital_Vestibulo_Spawn");
            if (vestObj == null)
            {
                vestObj = new GameObject("Luz_Cenital_Vestibulo_Spawn");
                vestObj.transform.SetParent(transform, false);
                vestObj.transform.position = new Vector3(0f, 2.95f, -4.5f);
                Light vLight = vestObj.AddComponent<Light>();
                vLight.type = LightType.Point;
                vLight.color = new Color(1.0f, 1.0f, 1.0f);
                vLight.intensity = 3.80f;
                vLight.range = 15.0f;
                vLight.shadows = LightShadows.None;
            }
            else
            {
                Light vLight = vestObj.GetComponent<Light>();
                if (vLight != null)
                {
                    vLight.intensity = 3.80f;
                    vLight.range = 15.0f;
                    vLight.color = new Color(1.0f, 1.0f, 1.0f);
                }
            }

            // 4. Luz Cenital Central de la Sala SOC (Z = 0.0m)
            GameObject roomObj = GameObject.Find("Luz_Cenital_Central_Oficina");
            if (roomObj == null)
            {
                roomObj = new GameObject("Luz_Cenital_Central_Oficina");
                roomObj.transform.SetParent(transform, false);
                roomObj.transform.position = new Vector3(0f, 3.10f, 0.0f);
                Light rLight = roomObj.AddComponent<Light>();
                rLight.type = LightType.Point;
                rLight.color = new Color(1.0f, 1.0f, 1.0f);
                rLight.intensity = 3.50f;
                rLight.range = 16.0f;
                rLight.shadows = LightShadows.None;
            }
            else
            {
                Light rLight = roomObj.GetComponent<Light>();
                if (rLight != null)
                {
                    rLight.intensity = 3.50f;
                    rLight.range = 16.0f;
                    rLight.color = new Color(1.0f, 1.0f, 1.0f);
                }
            }
        }

        #endregion

        #region Ensamblador Estático del Sistema de Iluminación (One-Click Setup)

        /// <summary>
        /// Construye y conecta el sistema completo de iluminación SOC en la escena:
        /// 1. Luz direccional de luna clara a través del ventanal norte.
        /// 2. 3 luces puntuales intensas y limpias sobre los puestos de trabajo.
        /// 3. Luz cenital de bienvenida en el vestíbulo (zona de spawn del jugador).
        /// 4. Luz cenital general en el centro de la oficina.
        /// 5. 2 balizas de techo para alerta roja (#ff5a4e).
        /// </summary>
        public static SocLightingController SetupLightingAtmosphere(Transform parent = null)
        {
            ApplyNocturnalAmbientSettings();

            GameObject lightingRoot = new GameObject("--- ILUMINACION SOC & ATMOSFERA ---");
            if (parent != null) lightingRoot.transform.SetParent(parent, false);

            SocLightingController controller = lightingRoot.AddComponent<SocLightingController>();

            // 1. Luz Direccional Principal (Luna clara a través del ventanal norte)
            GameObject moonObj = new GameObject("Luz_Direccional_Luna_Ventanal");
            moonObj.transform.SetParent(lightingRoot.transform, false);
            // Angulada a 45° de elevación, orientada hacia el interior de la oficina
            moonObj.transform.rotation = Quaternion.Euler(45f, 170f, 0f);

            Light moonLight = moonObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(1.0f, 0.98f, 0.95f, 1f);
            moonLight.intensity = 2.80f; // Iluminación muy brillante
            moonLight.shadows = LightShadows.None; // 60 FPS Mobile OpenGLES3

            // 2. Luces Puntuales sobre los 3 Escritorios
            float[] deskX = { -3.0f, 0.0f, 3.0f };
            string[] deskNames = { "Luz_Escritorio_Puesto1_Carla", "Luz_Escritorio_Puesto2_Analista", "Luz_Escritorio_Puesto3_Siem" };
            Light[] deskLights = new Light[3];

            for (int i = 0; i < 3; i++)
            {
                GameObject dObj = new GameObject(deskNames[i]);
                dObj.transform.SetParent(lightingRoot.transform, false);
                // Suspendida a 2.80m de altura sobre el centro del puesto (Z = 1.8m)
                dObj.transform.position = new Vector3(deskX[i], 2.80f, 1.8f);

                Light pl = dObj.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = DeskCoolWhiteGreenColor;
                pl.intensity = 2.50f;
                pl.range = 8.5f;
                pl.shadows = LightShadows.None;

                deskLights[i] = pl;
            }

            // 3. Luz Cenital del Vestíbulo (Entrada / Spawn del Jugador)
            GameObject vestObj = new GameObject("Luz_Cenital_Vestibulo_Spawn");
            vestObj.transform.SetParent(lightingRoot.transform, false);
            vestObj.transform.position = new Vector3(0f, 2.95f, -4.5f);
            Light vestLight = vestObj.AddComponent<Light>();
            vestLight.type = LightType.Point;
            vestLight.color = new Color(1.0f, 1.0f, 1.0f);
            vestLight.intensity = 3.80f;
            vestLight.range = 15.0f;
            vestLight.shadows = LightShadows.None;

            // 4. Luz Cenital Central de la Oficina
            GameObject roomObj = new GameObject("Luz_Cenital_Central_Oficina");
            roomObj.transform.SetParent(lightingRoot.transform, false);
            roomObj.transform.position = new Vector3(0f, 3.10f, 0.0f);
            Light roomLight = roomObj.AddComponent<Light>();
            roomLight.type = LightType.Point;
            roomLight.color = new Color(1.0f, 1.0f, 1.0f);
            roomLight.intensity = 3.50f;
            roomLight.range = 16.0f;
            roomLight.shadows = LightShadows.None;

            // 5. Balizas de Alerta Roja (#ff5a4e) en el Techo
            float[] beaconX = { -1.8f, 1.8f };
            Light[] alertLights = new Light[2];
            Renderer[] beaconRenderers = new Renderer[2];

            Material beaconMat = RoomArchitectureBuilder.CreatePbrMaterial(
                "Mat_EmergencyBeaconRed_ff5a4e", AlertRedColor, 0.90f, 0.1f, AlertRedColor * 3.0f);

            for (int b = 0; b < 2; b++)
            {
                GameObject bRoot = new GameObject($"Baliza_Emergencia_Roja_0{b + 1}");
                bRoot.transform.SetParent(lightingRoot.transform, false);
                bRoot.transform.position = new Vector3(beaconX[b], 3.16f, 1.5f);

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
                alertL.range = 7.5f;
                alertL.shadows = LightShadows.None; // Optimizado OpenGLES3
                alertL.enabled = false;

                alertLights[b] = alertL;
            }

            // 6. Serializar referencias en el componente
#if UNITY_EDITOR
            SerializedObject sCtrl = new SerializedObject(controller);
            sCtrl.FindProperty("moonlightDirectionalLight").objectReferenceValue = moonLight;
            sCtrl.FindProperty("moonlightBaseIntensity").floatValue = 2.80f;
            sCtrl.FindProperty("desk1PointLight").objectReferenceValue = deskLights[0];
            sCtrl.FindProperty("desk2PointLight").objectReferenceValue = deskLights[1];
            sCtrl.FindProperty("desk3PointLight").objectReferenceValue = deskLights[2];
            sCtrl.FindProperty("deskLightBaseIntensity").floatValue = 2.50f;
            sCtrl.FindProperty("deskLightRange").floatValue = 8.5f;

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
