using System;
using System.Collections;
using UnityEngine;
using AndesSim.Core;
using AndesSim.Scenario01;

namespace AndesSim.Environment
{
    /// <summary>
    /// Estados de iluminación y atmósfera cibernética de la oficina bancaria.
    /// </summary>
    public enum CyberAmbientState
    {
        NormalInvestigation,        // Azul / Verde tenue durante la investigación forense
        EmergencyAttackInfection,   // Pulso rojo rítmico de alerta cuando el malware infecta la red
        QuarantineDefenseShield     // Cian / Verde esmeralda estable cuando el sandbox bloquea el correo
    }

    /// <summary>
    /// Controlador de efectos ligeros de partículas y luces dinámicas para la Oficina de Pagos.
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1 (Equipo 1).
    /// - Optimizado estrictamente para móviles Android (60 FPS estables en OpenGLES3 / Google Cardboard XR).
    /// - Aplica micropartículas de datos y modulación de iluminación en el ventanal nocturno y portal:
    ///   1. Azul/Verde tenue durante la investigación normal.
    ///   2. Pulso rojo de emergencia cuando se ejecuta el enlace malicioso en SimMode.Ataque.
    ///   3. Iluminación cian/verde estable cuando el correo entra en cuarentena en SimMode.Defensa.
    /// </summary>
    public class AmbientCyberGridEffect : MonoBehaviour
    {
        public static AmbientCyberGridEffect I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - CyberGrid]";

        [Header("--- 1. Puntos de Anclaje de Efectos ---")]
        [Tooltip("Transform del ventanal panorámico norte.")]
        [SerializeField] private Transform windowAnchor;

        [Tooltip("Transform del marco del Portal_Entrada en el vestíbulo.")]
        [SerializeField] private Transform portalAnchor;

        [Header("--- 2. Fuentes de Luz Dinámicas ---")]
        [SerializeField] private Light windowCyberLight;
        [SerializeField] private Light portalCyberLight;

        [Header("--- 3. Sistemas de Partículas Ligeros (Mobile Optimized) ---")]
        [SerializeField] private ParticleSystem windowParticleSystem;
        [SerializeField] private ParticleSystem portalParticleSystem;

        [Header("--- 4. Paleta de Colores por Estado ---")]
        [ColorUsage(true, true)]
        [SerializeField] private Color normalColor = new Color(0.14f, 0.65f, 0.45f, 1f); // Verde/azul tenue

        [ColorUsage(true, true)]
        [SerializeField] private Color emergencyRedColor = new Color(1.0f, 0.22f, 0.20f, 1f); // Rojo #ff3834

        [ColorUsage(true, true)]
        [SerializeField] private Color defenseCyanColor = new Color(0.22f, 0.95f, 0.65f, 1f); // Cian/verde #38f28a

        [Header("--- 5. Parámetros de Pulsación de Emergencia ---")]
        [SerializeField] private float emergencyPulseFrequency = 3.2f;
        [SerializeField] private float minPulseIntensity = 0.35f;
        [SerializeField] private float maxPulseIntensity = 1.35f;
        [SerializeField] private float transitionSpeed = 3.0f;

        // Estado interno
        private CyberAmbientState _currentState = CyberAmbientState.NormalInvestigation;
        private Color _currentLightColor;
        private Color _targetLightColor;
        private float _currentBaseIntensity = 0.85f;
        private float _targetBaseIntensity = 0.85f;

        public CyberAmbientState CurrentState => _currentState;

        private void Awake()
        {
            if (I == null)
            {
                I = this;
            }
            else if (I != this)
            {
                Destroy(this);
                return;
            }

            _currentLightColor = normalColor;
            _targetLightColor = normalColor;

            EnsureAnchorsAndReferences();
            SetupMobileLights();
            SetupMobileParticleSystems();
        }

        private void Start()
        {
            SetAmbientState(CyberAmbientState.NormalInvestigation, true);

            // Sincronización con eventos de GameManager y Flujo
            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged += HandleModeChanged;
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
            }

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged += HandleStepChanged;
            }
        }

        private void OnDestroy()
        {
            if (I == this) I = null;

            if (GameManager.I != null)
            {
                GameManager.I.OnModeChanged -= HandleModeChanged;
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
            }

            if (ScenarioFlowController.I != null)
            {
                ScenarioFlowController.I.OnStepChanged -= HandleStepChanged;
            }
        }

        private void Update()
        {
            UpdateLightingTransitions();
        }

        #region Inicialización Automática y Optimización Mobile (OpenGLES3)

        private void EnsureAnchorsAndReferences()
        {
            if (windowAnchor == null)
            {
                GameObject winObj = GameObject.Find("Ventanal_Nocturno_Panoramico") ?? GameObject.Find("Backdrop_Rascacielos_Nocturnos");
                if (winObj != null) windowAnchor = winObj.transform;
                else
                {
                    GameObject dummyWin = new GameObject("Anchor_Ventanal_Cyber");
                    dummyWin.transform.SetParent(transform, false);
                    dummyWin.transform.position = new Vector3(0f, 1.8f, 5.3f);
                    windowAnchor = dummyWin.transform;
                }
            }

            if (portalAnchor == null)
            {
                GameObject portalObj = GameObject.Find("Marco_Luminoso_PortalEntrada") ?? GameObject.Find("Portal_Entrada");
                if (portalObj != null) portalAnchor = portalObj.transform;
                else
                {
                    GameObject dummyPortal = new GameObject("Anchor_Portal_Cyber");
                    dummyPortal.transform.SetParent(transform, false);
                    dummyPortal.transform.position = new Vector3(0f, 1.4f, -4.5f);
                    portalAnchor = dummyPortal.transform;
                }
            }
        }

        private void SetupMobileLights()
        {
            // 1. Luz de Ventanal
            if (windowCyberLight == null && windowAnchor != null)
            {
                GameObject wLightObj = new GameObject("Luz_Ventanal_Cyber");
                wLightObj.transform.SetParent(windowAnchor, false);
                wLightObj.transform.localPosition = new Vector3(0f, 0f, -0.3f);

                windowCyberLight = wLightObj.AddComponent<Light>();
                windowCyberLight.type = LightType.Point;
                windowCyberLight.range = 7.0f;
                windowCyberLight.intensity = 0.85f;
                windowCyberLight.color = normalColor;
                windowCyberLight.shadows = LightShadows.None; // 0 costo de sombras para OpenGLES3
            }

            // 2. Luz de Portal
            if (portalCyberLight == null && portalAnchor != null)
            {
                GameObject pLightObj = new GameObject("Luz_Portal_Cyber");
                pLightObj.transform.SetParent(portalAnchor, false);
                pLightObj.transform.localPosition = new Vector3(0f, 1.3f, 0f);

                portalCyberLight = pLightObj.AddComponent<Light>();
                portalCyberLight.type = LightType.Point;
                portalCyberLight.range = 5.0f;
                portalCyberLight.intensity = 0.75f;
                portalCyberLight.color = normalColor;
                portalCyberLight.shadows = LightShadows.None;
            }
        }

        private void SetupMobileParticleSystems()
        {
            Material particleMat = CreateParticleMaterial();

            // 1. Partículas del Ventanal: Máximo 24 partículas flotantes lentas
            if (windowParticleSystem == null && windowAnchor != null)
            {
                GameObject psObj = new GameObject("Particulas_Ventanal_CyberGrid");
                psObj.transform.SetParent(windowAnchor, false);
                psObj.transform.localPosition = new Vector3(0f, 0f, -0.15f);

                windowParticleSystem = ConfigureLightweightParticleSystem(psObj, particleMat, 24, new Vector3(5.0f, 1.4f, 0.4f), normalColor);
            }

            // 2. Partículas del Portal: Máximo 16 partículas ascendentes en el arco
            if (portalParticleSystem == null && portalAnchor != null)
            {
                GameObject psPortalObj = new GameObject("Particulas_Portal_CyberField");
                psPortalObj.transform.SetParent(portalAnchor, false);
                psPortalObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);

                portalParticleSystem = ConfigureLightweightParticleSystem(psPortalObj, particleMat, 16, new Vector3(2.4f, 0.2f, 0.3f), normalColor, true);
            }
        }

        private static ParticleSystem ConfigureLightweightParticleSystem(GameObject host, Material mat, int maxParticles, Vector3 boxSize, Color startColor, bool verticalFlow = false)
        {
            ParticleSystem ps = host.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psRenderer = host.GetComponent<ParticleSystemRenderer>();
            psRenderer.sharedMaterial = mat;
            psRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psRenderer.receiveShadows = false;

            // Módulo Principal
            ParticleSystem.MainModule main = ps.main;
            main.maxParticles = maxParticles;
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5.5f);
            main.startSpeed = verticalFlow ? new ParticleSystem.MinMaxCurve(0.18f, 0.35f) : new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = startColor;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.loop = true;
            main.playOnAwake = true;

            // Emisión constante muy baja (4-6 partículas por segundo)
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = maxParticles / 4f;

            // Forma de emisión (Caja para ventanal / portal)
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            if (verticalFlow)
            {
                shape.rotation = new Vector3(-90f, 0f, 0f); // flujo hacia arriba
            }

            return ps;
        }

        private static Material CreateParticleMaterial()
        {
            Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (pShader == null) pShader = Shader.Find("Mobile/Particles/Additive");
            if (pShader == null) pShader = Shader.Find("Sprites/Default");

            Material mat = new Material(pShader);
            mat.name = "Mat_CyberGrid_SoftParticles";
            mat.mainTexture = GenerateSoftParticleTexture(64);
            return mat;
        }

        private static Texture2D GenerateSoftParticleTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float maxRadius = center;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float t = Mathf.Clamp01(dist / maxRadius);
                    float alpha = Mathf.Pow(1f - t, 2.0f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        #endregion

        #region Transiciones y Máquina de Estados

        /// <summary>
        /// Cambia el estado ambiental cibernético de la oficina.
        /// </summary>
        public void SetAmbientState(CyberAmbientState newState, bool instant = false)
        {
            _currentState = newState;

            switch (newState)
            {
                case CyberAmbientState.NormalInvestigation:
                    _targetLightColor = normalColor;
                    _targetBaseIntensity = 0.85f;
                    SetParticleColor(normalColor);
                    SetRenderSettingsAmbient(new Color(0.12f, 0.22f, 0.18f), new Color(0.08f, 0.14f, 0.12f));
                    break;

                case CyberAmbientState.EmergencyAttackInfection:
                    _targetLightColor = emergencyRedColor;
                    _targetBaseIntensity = 1.10f;
                    SetParticleColor(emergencyRedColor);
                    SetRenderSettingsAmbient(new Color(0.38f, 0.10f, 0.08f), new Color(0.22f, 0.06f, 0.05f));
                    break;

                case CyberAmbientState.QuarantineDefenseShield:
                    _targetLightColor = defenseCyanColor;
                    _targetBaseIntensity = 0.95f;
                    SetParticleColor(defenseCyanColor);
                    SetRenderSettingsAmbient(new Color(0.10f, 0.32f, 0.26f), new Color(0.06f, 0.20f, 0.16f));
                    break;
            }

            if (instant)
            {
                _currentLightColor = _targetLightColor;
                _currentBaseIntensity = _targetBaseIntensity;
                ApplyLightColorAndIntensity(_currentLightColor, _currentBaseIntensity);
            }

            Debug.Log($"{LogPrefix} <color=#{ColorUtility.ToHtmlStringRGB(_targetLightColor)}><b>Estado de iluminación ambiental cambiado a:</b></color> {newState}");
        }

        public void TriggerEmergencyAttackPulse()
        {
            SetAmbientState(CyberAmbientState.EmergencyAttackInfection);
        }

        public void TriggerQuarantineDefenseShield()
        {
            SetAmbientState(CyberAmbientState.QuarantineDefenseShield);
        }

        public void ResetToNormalInvestigation()
        {
            SetAmbientState(CyberAmbientState.NormalInvestigation);
        }

        private void UpdateLightingTransitions()
        {
            // Interpolar color base suavemente
            _currentLightColor = Color.Lerp(_currentLightColor, _targetLightColor, Time.deltaTime * transitionSpeed);
            _currentBaseIntensity = Mathf.Lerp(_currentBaseIntensity, _targetBaseIntensity, Time.deltaTime * transitionSpeed);

            float effectiveIntensity = _currentBaseIntensity;

            // Si está en emergencia (Modo Ataque con malware activo), aplicar pulso de sirena
            if (_currentState == CyberAmbientState.EmergencyAttackInfection)
            {
                float pulseSin = (Mathf.Sin(Time.time * emergencyPulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
                effectiveIntensity = Mathf.Lerp(minPulseIntensity, maxPulseIntensity, pulseSin);
            }
            // En investigación normal, sutil respiración cyber
            else if (_currentState == CyberAmbientState.NormalInvestigation)
            {
                float breathSin = (Mathf.Sin(Time.time * 0.8f * Mathf.PI) + 1f) * 0.5f;
                effectiveIntensity = Mathf.Lerp(_currentBaseIntensity * 0.88f, _currentBaseIntensity * 1.05f, breathSin);
            }

            ApplyLightColorAndIntensity(_currentLightColor, effectiveIntensity);
        }

        private void ApplyLightColorAndIntensity(Color color, float intensity)
        {
            if (windowCyberLight != null)
            {
                windowCyberLight.color = color;
                windowCyberLight.intensity = intensity;
            }

            if (portalCyberLight != null)
            {
                portalCyberLight.color = color;
                portalCyberLight.intensity = intensity * 0.90f;
            }
        }

        private void SetParticleColor(Color color)
        {
            if (windowParticleSystem != null)
            {
                var main = windowParticleSystem.main;
                main.startColor = color;
            }

            if (portalParticleSystem != null)
            {
                var main = portalParticleSystem.main;
                main.startColor = color;
            }
        }

        private static void SetRenderSettingsAmbient(Color sky, Color equator)
        {
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
        }

        #endregion

        #region Handlers de Eventos de Simulación

        private void HandleModeChanged(SimMode newMode)
        {
            if (newMode == SimMode.Defensa)
            {
                // Si la cuarentena ya se activó o estamos en defensa, cambiar a escudo cian/verde
                if (MailQuarantine.I != null && MailQuarantine.I.IsQuarantined)
                {
                    SetAmbientState(CyberAmbientState.QuarantineDefenseShield);
                }
                else
                {
                    SetAmbientState(CyberAmbientState.NormalInvestigation);
                }
            }
            else
            {
                SetAmbientState(CyberAmbientState.NormalInvestigation);
            }
        }

        private void HandleDefenseApplied(string defenseId)
        {
            // Al aplicar contramedidas, cambiar a escudo de protección
            SetAmbientState(CyberAmbientState.QuarantineDefenseShield);
        }

        private void HandleStepChanged(ScenarioStep newStep, StepGuideInfo info)
        {
            if (newStep == ScenarioStep.Step6_Resultado)
            {
                SimMode mode = GameManager.I != null ? GameManager.I.mode : SimMode.Ataque;
                if (mode == SimMode.Ataque)
                {
                    SetAmbientState(CyberAmbientState.EmergencyAttackInfection);
                }
                else
                {
                    SetAmbientState(CyberAmbientState.QuarantineDefenseShield);
                }
            }
            else if (newStep == ScenarioStep.Step1_PortalEntrada || newStep == ScenarioStep.Step2_Oficina3D)
            {
                SetAmbientState(CyberAmbientState.NormalInvestigation);
            }
        }

        #endregion
    }
}
