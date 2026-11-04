using System;
using UnityEngine;

namespace AndesSim.Core
{
    /// <summary>
    /// Administrador de persistencia y seguimiento de progreso para ANDES-SIM (BANCO ANDINO).
    /// Escenario 1: "Oficina de pagos" (Equipo 1).
    /// Garantiza el cumplimiento del requisito: "Modo ataque y modo defensa en la misma app, con un resultado distinto en cada uno".
    /// Registra en memoria y en PlayerPrefs:
    /// 1. Finalización del Modo Ataque (Puerta trasera instalada en la red bancaria).
    /// 2. Finalización del Modo Defensa (Correo en cuarentena, MFA activo, 0 equipos infectados).
    /// 3. Récord de tiempo en detectar las 3 señales de phishing.
    /// 4. Hipótesis de sospechoso seleccionado por el equipo como cómplice interno.
    /// </summary>
    public class ProgressTracker : MonoBehaviour
    {
        public static ProgressTracker I;

        private const string LogPrefix = "[ANDES-SIM - Progress]";

        // Claves para persistencia en PlayerPrefs
        private const string PrefKeyAttackCompleted = "ANDES_SIM_E1_ATTACK_COMPLETED";
        private const string PrefKeyDefenseCompleted = "ANDES_SIM_E1_DEFENSE_COMPLETED";
        private const string PrefKeyBestPhishingTime = "ANDES_SIM_E1_BEST_PHISHING_TIME";
        private const string PrefKeySelectedSuspect = "ANDES_SIM_E1_SELECTED_SUSPECT";
        private const string PrefKeyAttackDate = "ANDES_SIM_E1_ATTACK_TIMESTAMP";
        private const string PrefKeyDefenseDate = "ANDES_SIM_E1_DEFENSE_TIMESTAMP";

        [Header("--- Estado en Memoria (Sesión Actual) ---")]
        [SerializeField] private bool attackModeCompleted = false;
        [SerializeField] private bool defenseModeCompleted = false;
        [SerializeField] private float bestPhishingDetectionTime = -1f; // -1 indica sin récord aún
        [SerializeField] private string selectedSuspectHypothesis = string.Empty;

        #region Eventos Públicos Desacoplados

        /// <summary>
        /// Se dispara al completar un modo por primera vez o re-completarlo. Argumento: SimMode.
        /// </summary>
        public event Action<SimMode> OnModeCompleted;

        /// <summary>
        /// Se dispara cuando se establece un nuevo récord de tiempo en las 3 señales de phishing.
        /// </summary>
        public event Action<float, string> OnNewPhishingRecordTime;

        /// <summary>
        /// Se dispara cuando el jugador selecciona la hipótesis del sospechoso interno.
        /// </summary>
        public event Action<string> OnSuspectHypothesisChanged;

        #endregion

        #region Propiedades Públicas

        public bool HasCompletedAttackMode => attackModeCompleted;
        public bool HasCompletedDefenseMode => defenseModeCompleted;
        public bool HasCompletedBothModes => attackModeCompleted && defenseModeCompleted;
        public float BestPhishingDetectionTime => bestPhishingDetectionTime;
        public string SelectedSuspectHypothesis => selectedSuspectHypothesis;

        #endregion

        private void Awake()
        {
            if (I == null)
            {
                I = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (I != this)
            {
                Destroy(gameObject);
                return;
            }

            LoadFromPlayerPrefs();
        }

        private void Start()
        {
            // Suscripción automática a los eventos de GameManager si está presente
            if (GameManager.I != null)
            {
                GameManager.I.OnScenarioEnded += HandleScenarioEnded;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnScenarioEnded -= HandleScenarioEnded;
            }
        }

        /// <summary>
        /// Hook que escucha la finalización del escenario desde GameManager.
        /// </summary>
        private void HandleScenarioEnded(string resultMessage, bool timeCriteriaMet)
        {
            if (GameManager.I == null) return;

            if (GameManager.I.mode == SimMode.Ataque)
            {
                RecordAttackModeCompleted();
            }
            else
            {
                RecordDefenseModeCompleted();
            }
        }

        #region Métodos de Registro y Actualización

        /// <summary>
        /// Registra la culminación del Modo Ataque (Puerta trasera instalada por Lazarus).
        /// </summary>
        public void RecordAttackModeCompleted()
        {
            attackModeCompleted = true;
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            PlayerPrefs.SetInt(PrefKeyAttackCompleted, 1);
            PlayerPrefs.SetString(PrefKeyAttackDate, timeStamp);
            PlayerPrefs.Save();

            Debug.Log($"{LogPrefix} <color=#FF6B68><b>RESULTADO REGISTRADO [MODO ATAQUE]:</b></color> " +
                      "El troyano .scr.zip se ejecutó en la red bancaria y la puerta trasera (C2 Lazarus) quedó activa.");

            OnModeCompleted?.Invoke(SimMode.Ataque);
            CheckBothModesCompletion();
        }

        /// <summary>
        /// Registra la culminación del Modo Defensa (Sandbox perimetral, correo en cuarentena y MFA activo).
        /// </summary>
        public void RecordDefenseModeCompleted()
        {
            defenseModeCompleted = true;
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            PlayerPrefs.SetInt(PrefKeyDefenseCompleted, 1);
            PlayerPrefs.SetString(PrefKeyDefenseDate, timeStamp);
            PlayerPrefs.Save();

            Debug.Log($"{LogPrefix} <color=#4EC9B0><b>RESULTADO REGISTRADO [MODO DEFENSA]:</b></color> " +
                      "Correo en cuarentena, detonación en sandbox exitosa, MFA activado y 0 terminales infectadas.");

            OnModeCompleted?.Invoke(SimMode.Defensa);
            CheckBothModesCompletion();
        }

        /// <summary>
        /// Comprueba y registra si el tiempo empleado en encontrar las 3 señales de phishing es un nuevo récord.
        /// </summary>
        /// <param name="timeInSeconds">Tiempo en segundos.</param>
        public bool RecordPhishingDetectionTime(float timeInSeconds)
        {
            if (timeInSeconds <= 0f) return false;

            bool isNewRecord = false;

            if (bestPhishingDetectionTime < 0f || timeInSeconds < bestPhishingDetectionTime)
            {
                bestPhishingDetectionTime = timeInSeconds;
                isNewRecord = true;

                PlayerPrefs.SetFloat(PrefKeyBestPhishingTime, bestPhishingDetectionTime);
                PlayerPrefs.Save();

                string formatted = SimulationClock.FormatTimeMMSS(bestPhishingDetectionTime);
                Debug.Log($"{LogPrefix} <color=#FCE762><b>¡NUEVO RÉCORD DE DETECCIÓN!</b></color> " +
                          $"3 señales de phishing descubiertas en <b>{formatted}</b> ({bestPhishingDetectionTime:F1}s).");

                OnNewPhishingRecordTime?.Invoke(bestPhishingDetectionTime, formatted);
            }

            return isNewRecord;
        }

        /// <summary>
        /// Registra la hipótesis del sospechoso interno seleccionado por el equipo de investigación.
        /// </summary>
        /// <param name="suspectIdOrName">Identificador o nombre del sospechoso (ej: 'Ing. Samuel Torres', 'Carla Mendoza').</param>
        public void SetSelectedSuspectHypothesis(string suspectIdOrName)
        {
            if (string.IsNullOrEmpty(suspectIdOrName)) return;

            selectedSuspectHypothesis = suspectIdOrName;
            PlayerPrefs.SetString(PrefKeySelectedSuspect, selectedSuspectHypothesis);
            PlayerPrefs.Save();

            Debug.Log($"{LogPrefix} <color=#9CDCFE>Hipótesis del cómplice interno seleccionada:</color> <b>{selectedSuspectHypothesis}</b>");
            OnSuspectHypothesisChanged?.Invoke(selectedSuspectHypothesis);
        }

        #endregion

        #region Persistencia y Consultas

        /// <summary>
        /// Carga los datos almacenados desde PlayerPrefs hacia la memoria del componente.
        /// </summary>
        public void LoadFromPlayerPrefs()
        {
            attackModeCompleted = PlayerPrefs.GetInt(PrefKeyAttackCompleted, 0) == 1;
            defenseModeCompleted = PlayerPrefs.GetInt(PrefKeyDefenseCompleted, 0) == 1;
            bestPhishingDetectionTime = PlayerPrefs.GetFloat(PrefKeyBestPhishingTime, -1f);
            selectedSuspectHypothesis = PlayerPrefs.GetString(PrefKeySelectedSuspect, string.Empty);

            Debug.Log($"{LogPrefix} Progreso cargado de PlayerPrefs:\n" +
                      $"• Modo Ataque completado: {attackModeCompleted}\n" +
                      $"• Modo Defensa completado: {defenseModeCompleted}\n" +
                      $"• Récord Phishing: {(bestPhishingDetectionTime > 0 ? SimulationClock.FormatTimeMMSS(bestPhishingDetectionTime) : "Sin registro")}\n" +
                      $"• Sospechoso Seleccionado: {(string.IsNullOrEmpty(selectedSuspectHypothesis) ? "Ninguno" : selectedSuspectHypothesis)}");
        }

        /// <summary>
        /// Guarda manualmente el estado actual en PlayerPrefs.
        /// </summary>
        public void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetInt(PrefKeyAttackCompleted, attackModeCompleted ? 1 : 0);
            PlayerPrefs.SetInt(PrefKeyDefenseCompleted, defenseModeCompleted ? 1 : 0);
            PlayerPrefs.SetFloat(PrefKeyBestPhishingTime, bestPhishingDetectionTime);
            PlayerPrefs.SetString(PrefKeySelectedSuspect, selectedSuspectHypothesis);
            PlayerPrefs.Save();

            Debug.Log($"{LogPrefix} Progreso guardado manualmente en PlayerPrefs.");
        }

        /// <summary>
        /// Borra todos los registros guardados (útil para pruebas y reinicio de evaluación).
        /// </summary>
        public void ClearProgress()
        {
            PlayerPrefs.DeleteKey(PrefKeyAttackCompleted);
            PlayerPrefs.DeleteKey(PrefKeyDefenseCompleted);
            PlayerPrefs.DeleteKey(PrefKeyBestPhishingTime);
            PlayerPrefs.DeleteKey(PrefKeySelectedSuspect);
            PlayerPrefs.DeleteKey(PrefKeyAttackDate);
            PlayerPrefs.DeleteKey(PrefKeyDefenseDate);
            PlayerPrefs.Save();

            attackModeCompleted = false;
            defenseModeCompleted = false;
            bestPhishingDetectionTime = -1f;
            selectedSuspectHypothesis = string.Empty;

            Debug.Log($"{LogPrefix} Progreso reiniciado por completo.");
        }

        /// <summary>
        /// Retorna un resumen comparativo formateado para el HUD o la pantalla de resultados del aula/docente.
        /// </summary>
        public string GetComparativeReport()
        {
            string recordStr = bestPhishingDetectionTime > 0 
                ? SimulationClock.FormatTimeMMSS(bestPhishingDetectionTime) 
                : "Sin registro";

            string suspectStr = string.IsNullOrEmpty(selectedSuspectHypothesis) 
                ? "Pendiente de deliberación" 
                : selectedSuspectHypothesis;

            return $"=== ESTADO DEL SIMULADOR ANDES-SIM (BANCO ANDINO) ===\n" +
                   $"• MODO ATAQUE: {(attackModeCompleted ? "<color=green>EXPERIMENTADO (Compromiso total)</color>" : "<color=yellow>PENDIENTE</color>")}\n" +
                   $"• MODO DEFENSA: {(defenseModeCompleted ? "<color=green>EXPERIMENTADO (Amenaza mitigada)</color>" : "<color=yellow>PENDIENTE</color>")}\n" +
                   $"• AMBOS MODOS COMPLETADOS: {(HasCompletedBothModes ? "<color=green>SÍ (Requisito 100% Cubierto)</color>" : "<color=orange>NO (Falta explorar un modo)</color>")}\n" +
                   $"• MEJOR TIEMPO EN PHISHING: {recordStr} {(bestPhishingDetectionTime <= 180f && bestPhishingDetectionTime > 0 ? "[< 3 MIN]" : "")}\n" +
                   $"• HIPÓTESIS DEL EQUIPO: {suspectStr}";
        }

        private void CheckBothModesCompletion()
        {
            if (HasCompletedBothModes)
            {
                Debug.Log($"{LogPrefix} <color=#4EC9B0><b>¡REQUISITO GENERAL CUMPLIDO!</b> El jugador ha experimentado tanto el Modo Ataque como el Modo Defensa con resultados contrastados.</color>");
            }
        }

        #endregion
    }
}
// Force reimport timestamp: 2026-11-04
