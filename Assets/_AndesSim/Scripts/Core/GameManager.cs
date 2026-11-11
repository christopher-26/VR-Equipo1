using System;
using System.Collections.Generic;
using AndesSim.Core;
using AndesSim.Data;
using UnityEngine;

public enum SimMode
{
    Ataque,
    Defensa
}

/// <summary>
/// Controlador central de simulación para el simulador VR "ANDES-SIM" (BANCO ANDINO).
/// Escenario 1: "Oficina de pagos" (Equipo 1).
/// Administra el modo de simulación (Ataque/Defensa), registro de evidencias (RF-05),
/// activación de contramedidas y evaluación de criterios de aceptación (180 segundos).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager I;            // instancia única
    public ScenarioData data;               // textos, fechas, cifras del escenario
    public SimMode mode = SimMode.Ataque;

    private const string LogPrefix = "[ANDES-SIM - GameManager]";

    // Colecciones de seguimiento
    private readonly HashSet<string> _discoveredEvidences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _appliedDefenses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _discoveredPhishingSignals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // Cronómetro de misión (Criterio de aceptación: 180 segundos)
    private float _elapsedTime = 0f;
    private bool _isTimerRunning = true;
    private bool _scenarioEnded = false;

    // Eventos desacoplados para la UI, sonido y retroalimentación VR
    public event Action<string> OnEvidenceRegistered;
    public event Action<string> OnDefenseApplied;
    public event Action<string> OnPhishingSignalRegistered;
    public event Action<SimMode> OnModeChanged;
    public event Action<string, bool> OnScenarioEnded; // (resumen, cumplió criterio de tiempo)
    public event Action OnSimulationReset;

    public IReadOnlyCollection<string> DiscoveredEvidences => _discoveredEvidences;
    public IReadOnlyCollection<string> AppliedDefenses => _appliedDefenses;
    public IReadOnlyCollection<string> DiscoveredPhishingSignals => _discoveredPhishingSignals;
    public float ElapsedTime => _elapsedTime;
    public bool IsScenarioEnded => _scenarioEnded;

    public float RemainingTime
    {
        get
        {
            if (data == null) return 0f;
            return Mathf.Max(0f, data.TimeLimitSeconds - _elapsedTime);
        }
    }

    private void Awake()
    {
        // Implementación del patrón Singleton
        if (I == null)
        {
            I = this;
        }
        else if (I != this)
        {
            Debug.LogWarning($"{LogPrefix} Instancia duplicada detectada en '{gameObject.name}'. Destruyendo componente.");
            Destroy(this);
            return;
        }

        // Carga automática de respaldo desde Resources si no fue asignado en el Inspector
        if (data == null)
        {
            data = Resources.Load<ScenarioData>("Data/Scenario01_Data");
            if (data != null)
            {
                Debug.Log($"{LogPrefix} <color=#4EC9B0>ScenarioData cargado automáticamente desde Resources/Data/Scenario01_Data.</color>");
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} No se encontró 'Scenario01_Data' en Resources/Data/. Asígnalo manualmente en el Inspector.");
            }
        }
    }

    private void Start()
    {
        Debug.Log($"{LogPrefix} <b>Simulador iniciado en Modo: <color=#9CDCFE>{mode}</color></b>");
        if (data != null)
        {
            Debug.Log($"{LogPrefix} Escenario #{data.ScenarioNumber}: '{data.ScenarioTitle}' | Fecha: {data.ScenarioDate} | Límite: {data.TimeLimitSeconds}s");
        }
    }

    private void Update()
    {
        if (_isTimerRunning && !_scenarioEnded)
        {
            _elapsedTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// Inicia el conteo regresivo / cronómetro de la simulación (180 segundos).
    /// Invocado al cruzar el Portal_Entrada hacia la oficina 3D.
    /// </summary>
    public void StartSimulationTimer()
    {
        _elapsedTime = 0f;
        _isTimerRunning = true;
        _scenarioEnded = false;
        Debug.Log($"{LogPrefix} <b>Cronómetro de simulación iniciado (180s / 3 minutos).</b>");

        // Sincronizar con SimulationClock si está presente en la escena
        SimulationClock clock = FindFirstObjectByType<SimulationClock>();
        if (clock != null)
        {
            clock.StartClock();
        }
    }

    /// <summary>
    /// Registra el hallazgo de una de las 3 señales de phishing (Signal_Remitente, Signal_Urgencia, Signal_Enlace).
    /// </summary>
    public void RegisterPhishingSignal(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!_discoveredPhishingSignals.Contains(id))
        {
            _discoveredPhishingSignals.Add(id);
            Debug.Log($"{LogPrefix} <color=#4EC9B0>Señal de Phishing registrada en GameManager:</color> <b>{id}</b> [{_discoveredPhishingSignals.Count}/3]");
            OnPhishingSignalRegistered?.Invoke(id);
        }
    }

    /// <summary>
    /// Cambia el modo activo de simulación (Ataque o Defensa).
    /// </summary>
    public void SetMode(SimMode newMode)
    {
        if (mode == newMode) return;

        mode = newMode;
        Debug.Log($"{LogPrefix} <b>Modo de simulación cambiado a: <color=#FCE762>{mode}</color></b>");
        OnModeChanged?.Invoke(mode);
    }

    /// <summary>
    /// Registra el hallazgo de una evidencia en la oficina (ej. "Evidencia_Correo", "Evidencia_PostIt", "Evidencia_LogAccesos").
    /// </summary>
    /// <param name="id">Identificador único de la evidencia.</param>
    public void RegisterEvidence(string id) // al encontrar una pista
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"{LogPrefix} Intento de registrar evidencia con ID nulo o vacío.");
            return;
        }

        if (_discoveredEvidences.Contains(id))
        {
            Debug.Log($"{LogPrefix} La evidencia '{id}' ya se encontraba registrada previamente.");
            return;
        }

        _discoveredEvidences.Add(id);

        string detailInfo = id;
        if (data != null)
        {
            EvidenceItem item = data.GetEvidenceById(id);
            if (!string.IsNullOrEmpty(item.evidenceName))
            {
                detailInfo = $"{item.evidenceName} ({item.locationDescription})";
            }
        }

        Debug.Log($"{LogPrefix} <color=#4EC9B0>EVIDENCIA REGISTRADA:</color> <b>{detailInfo}</b> [{_discoveredEvidences.Count}/3]");

        OnEvidenceRegistered?.Invoke(id);

        // Si se descubrieron las 3 evidencias requeridas en Modo Ataque
        if (_discoveredEvidences.Count >= 3 && mode == SimMode.Ataque)
        {
            Debug.Log($"{LogPrefix} <color=#9CDCFE><b>¡Todas las evidencias de compromiso (3/3) han sido descubiertas!</b></color>");
        }
    }

    /// <summary>
    /// Aplica una contramedida técnica o humana (ej. "Defensa_Sandbox", "Defensa_BotonReportar", "Defensa_MFA").
    /// </summary>
    /// <param name="id">Identificador único de la defensa.</param>
    public void ApplyDefense(string id) // al activar una contramedida
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"{LogPrefix} Intento de aplicar defensa con ID nulo o vacío.");
            return;
        }

        if (_appliedDefenses.Contains(id))
        {
            Debug.Log($"{LogPrefix} La defensa '{id}' ya fue aplicada anteriormente.");
            return;
        }

        _appliedDefenses.Add(id);

        string defenseTitle = id;
        if (data != null)
        {
            DefenseItem item = data.GetDefenseById(id);
            if (!string.IsNullOrEmpty(item.defenseName))
            {
                defenseTitle = item.defenseName;
            }
        }

        Debug.Log($"{LogPrefix} <color=#4EC9B0>DEFENSA ACTIVADA:</color> <b>{defenseTitle}</b> [{_appliedDefenses.Count}/3]");

        OnDefenseApplied?.Invoke(id);

        // Si se aplicaron las 3 contramedidas en Modo Defensa
        if (_appliedDefenses.Count >= 3 && mode == SimMode.Defensa)
        {
            Debug.Log($"{LogPrefix} <color=#9CDCFE><b>¡Todas las defensas (3/3) han sido desplegadas exitosamente!</b></color>");
        }
    }

    /// <summary>
    /// Finaliza la simulación y genera el informe de resultados según el modo actual.
    /// </summary>
    public void EndScenario() // muestra el resultado del modo actual
    {
        if (_scenarioEnded)
        {
            Debug.Log($"{LogPrefix} El escenario ya ha finalizado previamente.");
            return;
        }

        _scenarioEnded = true;
        _isTimerRunning = false;

        float limit = data != null ? data.TimeLimitSeconds : 180f;
        bool timeCriteriaMet = _elapsedTime <= limit;

        int totalEvidences = _discoveredEvidences.Count;
        int totalDefenses = _appliedDefenses.Count;

        string modeSummary = string.Empty;
        if (data != null)
        {
            modeSummary = (mode == SimMode.Ataque) ? data.AttackModeSummary : data.DefenseModeSummary;
        }

        string resultMessage = 
            $"===================================================\n" +
            $"   RESULTADO DE SIMULACIÓN - BANCO ANDINO (VR)\n" +
            $"   Escenario 1: Oficina de pagos (Equipo 1)\n" +
            $"===================================================\n" +
            $"• Modo Evaluado: {mode}\n" +
            $"• Tiempo Empleado: {_elapsedTime:F1}s / {limit:F0}s " + (timeCriteriaMet ? "<color=green>[CRITERIO CUMPLIDO]</color>" : "<color=red>[TIEMPO EXCEDIDO]</color>") + "\n" +
            $"• Evidencias Descubiertas: {totalEvidences}/3\n" +
            $"• Defensas Aplicadas: {totalDefenses}/3\n\n" +
            $"--- CONCLUSIÓN TÉCNICA ---\n" +
            $"{modeSummary}\n" +
            $"===================================================";

        Debug.Log($"{LogPrefix} <color=#4EC9B0>{resultMessage}</color>");

        OnScenarioEnded?.Invoke(resultMessage, timeCriteriaMet);
    }

    /// <summary>
    /// Reinicia los contadores y colecciones para una nueva sesión.
    /// </summary>
    public void ResetSimulation()
    {
        _discoveredEvidences.Clear();
        _appliedDefenses.Clear();
        _discoveredPhishingSignals.Clear();
        _elapsedTime = 0f;
        _isTimerRunning = true;
        _scenarioEnded = false;

        Debug.Log($"{LogPrefix} Simulación reiniciada.");
        OnSimulationReset?.Invoke();
    }
}
