using System;
using System.Collections.Generic;
using AndesSim.Data;
using UnityEngine;

namespace AndesSim.Core
{
    /// <summary>
    /// Enumeración de los 6 pasos oficiales del flujo del jugador en el Escenario 1.
    /// </summary>
    public enum ScenarioStep
    {
        Step1_PortalEntrada = 1,
        Step2_Oficina3D = 2,
        Step3_RevisarBandeja = 3,
        Step4_InspeccionarCorreo = 4,
        Step5_ElegirAccion = 5,
        Step6_Resultado = 6
    }

    /// <summary>
    /// Información y textos pedagógicos de cada paso para el HUD en World Space.
    /// Diseñado para que la experiencia "Se entienda sin ayuda; hay tensión, ritmo y recompensa".
    /// </summary>
    [Serializable]
    public struct StepGuideInfo
    {
        public ScenarioStep step;
        public string stepTitle;
        [TextArea(2, 4)] public string playerObjective;
        [TextArea(2, 5)] public string hudGuidanceInstruction;
        public string progressLabel; // "1/6", "2/6", etc.
    }

    /// <summary>
    /// Máquina de estados oficial que orquesta los 6 pasos del flujo del jugador en la "Oficina de pagos".
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1 (Equipo 1).
    /// </summary>
    public class ScenarioFlowController : MonoBehaviour
    {
        public static ScenarioFlowController I;

        private const string LogPrefix = "[ANDES-SIM - Flow]";

        [Header("--- Estado Actual del Flujo ---")]
        [SerializeField] private ScenarioStep currentStep = ScenarioStep.Step1_PortalEntrada;

        [Header("--- Configuración de los 6 Pasos Oficiales ---")]
        [SerializeField] private List<StepGuideInfo> stepConfigurations = new List<StepGuideInfo>();

        // Seguimiento interno de micro-objetivos por paso
        private readonly HashSet<string> _inspectedPhishingSignals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _visitedStations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        #region Eventos Públicos Desacoplados

        /// <summary>
        /// Se dispara al cambiar de paso. Argumentos: (pasoEnum, infoPaso).
        /// </summary>
        public event Action<ScenarioStep, StepGuideInfo> OnStepChanged;

        /// <summary>
        /// Se dispara para actualizar directamente el HUD. Argumentos: (instruccionHUD, etiquetaProgreso, tituloPaso).
        /// </summary>
        public event Action<string, string, string> OnHudInstructionUpdated;

        /// <summary>
        /// Se dispara cuando se completan las 3 señales de phishing en el Paso 4.
        /// </summary>
        public event Action OnAllPhishingSignalsInspected;

        /// <summary>
        /// Se dispara cuando finaliza el Paso 6 (conclusión del escenario).
        /// </summary>
        public event Action OnScenarioFlowCompleted;

        #endregion

        #region Propiedades Públicas

        public ScenarioStep CurrentStep => currentStep;
        public int CurrentStepNumber => (int)currentStep;
        public int TotalSteps => 6;
        public string ProgressFraction => $"{CurrentStepNumber}/{TotalSteps}";

        #endregion

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

            if (stepConfigurations == null || stepConfigurations.Count == 0)
            {
                InitializeDefaultStepGuides();
            }
        }

        private void Start()
        {
            // Inicializar el flujo en el Paso 1
            SetStep(currentStep, forceNotification: true);
        }

        /// <summary>
        /// Establece y ejecuta la transición hacia un paso específico del flujo.
        /// </summary>
        public void SetStep(ScenarioStep newStep, bool forceNotification = false)
        {
            if (currentStep == newStep && !forceNotification) return;

            currentStep = newStep;
            StepGuideInfo info = GetStepInfo(currentStep);

            Debug.Log($"{LogPrefix} <b>Transición a [{info.progressLabel}]: <color=#4EC9B0>{info.stepTitle}</color></b>\n" +
                      $"<color=#9CDCFE>Objetivo:</color> {info.playerObjective}\n" +
                      $"<color=#FCE762>Guía HUD:</color> {info.hudGuidanceInstruction}");

            // Notificar a observadores y HUD
            OnStepChanged?.Invoke(currentStep, info);
            OnHudInstructionUpdated?.Invoke(info.hudGuidanceInstruction, info.progressLabel, info.stepTitle);

            // Acciones específicas al entrar a cada estado
            HandleStepEntered(currentStep);
        }

        /// <summary>
        /// Avanza automáticamente al siguiente paso del flujo (1 -> 2 -> ... -> 6).
        /// </summary>
        public void AdvanceToNextStep()
        {
            int next = (int)currentStep + 1;
            if (next <= TotalSteps)
            {
                SetStep((ScenarioStep)next);
            }
            else
            {
                Debug.Log($"{LogPrefix} El flujo ya se encuentra en el paso final (6/6).");
            }
        }

        /// <summary>
        /// Lógica de entrada a cada paso de la simulación.
        /// </summary>
        private void HandleStepEntered(ScenarioStep step)
        {
            switch (step)
            {
                case ScenarioStep.Step1_PortalEntrada:
                    // Jugador en vestíbulo leyendo el briefing y eligiendo modo inicial
                    break;

                case ScenarioStep.Step2_Oficina3D:
                    // Jugador cruza el portal hacia la oficina 3D y explora puestos
                    break;

                case ScenarioStep.Step3_RevisarBandeja:
                    // Jugador posicionado frente a Monitor_01 de Carla Mendoza
                    _visitedStations.Add("Monitor_01");
                    break;

                case ScenarioStep.Step4_InspeccionarCorreo:
                    // Comienza la inspección de las 3 señales con la retícula
                    break;

                case ScenarioStep.Step5_ElegirAccion:
                    // Momento decisivo: Abrir enlace malicioso o Reportar + Defensas
                    break;

                case ScenarioStep.Step6_Resultado:
                    // Llamada al GameManager para cerrar la sesión y emitir evaluación
                    if (GameManager.I != null)
                    {
                        GameManager.I.EndScenario();
                    }
                    OnScenarioFlowCompleted?.Invoke();
                    break;
            }
        }

        #region Métodos de Notificación de Micro-Acciones del Jugador

        /// <summary>
        /// Paso 2 -> Paso 3: Notifica que el jugador se posicionó frente a un puesto o interactuó con un monitor.
        /// </summary>
        public void NotifyWorkstationInteracted(string workstationId)
        {
            _visitedStations.Add(workstationId);

            if (currentStep == ScenarioStep.Step2_Oficina3D &&
                string.Equals(workstationId, "Monitor_01", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"{LogPrefix} Jugador ubicado en Puesto 01 (Carla Mendoza). Avanzando a Paso 3.");
                SetStep(ScenarioStep.Step3_RevisarBandeja);
            }
        }

        /// <summary>
        /// Paso 3 -> Paso 4: Notifica que el jugador abrió el correo de Rasel Ahlam en el Monitor 01.
        /// </summary>
        public void NotifyEmailOpened()
        {
            if (currentStep == ScenarioStep.Step3_RevisarBandeja)
            {
                Debug.Log($"{LogPrefix} Correo de Rasel Ahlam abierto en Monitor 01. Avanzando a Paso 4 (Inspección de señales).");
                SetStep(ScenarioStep.Step4_InspeccionarCorreo);
            }
        }

        /// <summary>
        /// Paso 4: Registra el examen de una de las 3 señales de phishing mediante la retícula VR (RF-03).
        /// </summary>
        public void NotifyPhishingSignalInspected(string signalId)
        {
            if (currentStep != ScenarioStep.Step4_InspeccionarCorreo) return;

            if (!_inspectedPhishingSignals.Contains(signalId))
            {
                _inspectedPhishingSignals.Add(signalId);
                Debug.Log($"{LogPrefix} Señal de phishing examinada con retícula: <color=#4EC9B0>{signalId}</color> [{_inspectedPhishingSignals.Count}/3]");

                // Actualizar instrucción dinámica en HUD
                string dynamicInstruction = $"Señales identificadas ({_inspectedPhishingSignals.Count}/3):\n" +
                                             $"• Remitente: {(_inspectedPhishingSignals.Contains("Signal_Remitente") ? "DETECTADO" : "Pendiente")}\n" +
                                             $"• Enlace .scr.zip: {(_inspectedPhishingSignals.Contains("Signal_Enlace") ? "DETECTADO" : "Pendiente")}\n" +
                                             $"• Urgencia: {(_inspectedPhishingSignals.Contains("Signal_Urgencia") ? "DETECTADO" : "Pendiente")}";

                OnHudInstructionUpdated?.Invoke(dynamicInstruction, ProgressFraction, "Paso 4: Inspeccionar correo");

                if (_inspectedPhishingSignals.Count >= 3)
                {
                    Debug.Log($"{LogPrefix} <color=#9CDCFE><b>¡Las 3 señales de phishing han sido detectadas con éxito!</b></color>");

                    // Registrar en SimulationClock si está presente en la escena
                    SimulationClock clock = FindFirstObjectByType<SimulationClock>();
                    if (clock != null)
                    {
                        clock.RecordPhishingDetectionCompleted();
                    }

                    OnAllPhishingSignalsInspected?.Invoke();

                    // Transición automática al Paso 5 (Toma de decisión)
                    SetStep(ScenarioStep.Step5_ElegirAccion);
                }
            }
        }

        /// <summary>
        /// Paso 5 -> Paso 6: Ejecuta la resolución según la acción del usuario.
        /// </summary>
        /// <param name="actionType">"AbrirEnlace" en Modo Ataque o "ReportarPhishing" en Modo Defensa.</param>
        public void NotifyPlayerDecisionExecuted(string actionType)
        {
            if (currentStep != ScenarioStep.Step5_ElegirAccion) return;

            Debug.Log($"{LogPrefix} Decisión ejecutada en Paso 5: <color=#FCE762>{actionType}</color> (Modo: {GameManager.I?.mode})");

            if (GameManager.I != null)
            {
                if (GameManager.I.mode == SimMode.Ataque)
                {
                    GameManager.I.RegisterEvidence("Evidencia_Correo");
                }
                else
                {
                    GameManager.I.ApplyDefense("Defensa_BotonReportar");
                    GameManager.I.ApplyDefense("Defensa_Sandbox");
                    GameManager.I.ApplyDefense("Defensa_MFA");
                }
            }

            // Avanzar al paso final de evaluación
            SetStep(ScenarioStep.Step6_Resultado);
        }

        #endregion

        /// <summary>
        /// Retorna la ficha técnica de un paso específico.
        /// </summary>
        public StepGuideInfo GetStepInfo(ScenarioStep step)
        {
            for (int i = 0; i < stepConfigurations.Count; i++)
            {
                if (stepConfigurations[i].step == step)
                {
                    return stepConfigurations[i];
                }
            }

            // Fallback genérico
            return new StepGuideInfo
            {
                step = step,
                stepTitle = $"Paso {(int)step}",
                playerObjective = "Continúa con la simulación interactiva.",
                hudGuidanceInstruction = "Interactúa con los elementos resaltados en la oficina.",
                progressLabel = $"{(int)step}/6"
            };
        }

        /// <summary>
        /// Inicializa los textos de los 6 pasos oficiales con enfoque en tensión, ritmo y recompensa.
        /// </summary>
        private void InitializeDefaultStepGuides()
        {
            stepConfigurations.Clear();

            stepConfigurations.Add(new StepGuideInfo
            {
                step = ScenarioStep.Step1_PortalEntrada,
                stepTitle = "Paso 1: Portal de entrada",
                playerObjective = "Lectura del briefing y selección del modo de simulación.",
                hudGuidanceInstruction = "Bienvenido a ANDES-SIM. Fija tu retícula en el panel de briefing para seleccionar MODO ATAQUE o MODO DEFENSA y cruzar el portal hacia 2015.",
                progressLabel = "1/6"
            });

            stepConfigurations.Add(new StepGuideInfo
            {
                step = ScenarioStep.Step2_Oficina3D,
                stepTitle = "Paso 2: Oficina 3D",
                playerObjective = "Exploración de la oficina de pagos y recolección de indicios físicos y lógicos.",
                hudGuidanceInstruction = "Usa el teletransporte por mirada en el suelo para desplazarte. Encuentra las credenciales en post-it (Monitor 01) y revisa el monitor de eventos SIEM (Monitor 03).",
                progressLabel = "2/6"
            });

            stepConfigurations.Add(new StepGuideInfo
            {
                step = ScenarioStep.Step3_RevisarBandeja,
                stepTitle = "Paso 3: Revisar bandeja",
                playerObjective = "Posicionamiento en el puesto de Carla Mendoza y apertura de correo.",
                hudGuidanceInstruction = "Acércate a la estación 'PC-PAGOS-01' de Carla Mendoza. Fija tu mirada en la pantalla para abrir la bandeja de entrada del Banco Andino.",
                progressLabel = "3/6"
            });

            stepConfigurations.Add(new StepGuideInfo
            {
                step = ScenarioStep.Step4_InspeccionarCorreo,
                stepTitle = "Paso 4: Inspeccionar correo",
                playerObjective = "Examen con la retícula de las 3 señales: Remitente, Enlace y Urgencia (RF-03).",
                hudGuidanceInstruction = "Examen minucioso: Apunta tu retícula directamente hacia: 1) El correo del remitente, 2) El enlace '.scr.zip', 3) El texto de urgencia temporal.",
                progressLabel = "4/6"
            });

            stepConfigurations.Add(new StepGuideInfo
            {
                step = ScenarioStep.Step5_ElegirAccion,
                stepTitle = "Paso 5: Elegir: abrir o reportar",
                playerObjective = "Respuesta activa ante el vector de ataque.",
                hudGuidanceInstruction = "Modo Ataque: Haz clic en el enlace para presenciar la infección en tiempo real. / Modo Defensa: Haz clic en el botón 'Reportar Phishing' para aislar la amenaza.",
                progressLabel = "5/6"
            });

            stepConfigurations.Add(new StepGuideInfo
            {
                step = ScenarioStep.Step6_Resultado,
                stepTitle = "Paso 6: Resultado",
                playerObjective = "Evaluación técnica final y balance del criterio de aceptación (< 3 min).",
                hudGuidanceInstruction = "Simulación concluida. Revisa en el panel frontal tu desempeño, evidencias recolectadas y el tiempo de respuesta frente al incidente de 2015.",
                progressLabel = "6/6"
            });
        }
    }
}
