using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace AndesSim.Core
{
    /// <summary>
    /// Reloj de simulación y temporizador de misión para ANDES-SIM (BANCO ANDINO).
    /// Escenario 1: "Oficina de pagos" (Equipo 1).
    /// - Controla el conteo regresivo de 180 segundos (3 minutos) del criterio de aceptación.
    /// - Mantiene el reloj ambiental histórico del incidente ("29 ENE 2015 - 09:14 AM").
    /// - Emite alertas sonoras a los 60s y 30s.
    /// - Registra con precisión milimétrica el momento en que se detectaron las 3 señales de phishing.
    /// </summary>
    public class SimulationClock : MonoBehaviour
    {
        public static SimulationClock I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - Clock]";

        [Header("--- Parámetros del Cronómetro (Criterio de Aceptación) ---")]
        [Tooltip("Duración máxima en segundos para el Escenario 1 (3 minutos).")]
        [SerializeField] private float totalDurationSeconds = 180f;

        [Tooltip("Indica si el cronómetro inicia inmediatamente al cargar la escena.")]
        [SerializeField] private bool autoStartOnStart = true;

        [Header("--- Vinculación Visual (World Space VR HUD) ---")]
        [Tooltip("Texto para el conteo regresivo formateado en MM:SS.")]
        [SerializeField] private TMP_Text countdownDisplay;

        [Tooltip("Texto para el reloj ambiental histórico de la oficina.")]
        [SerializeField] private TMP_Text ambientClockDisplay;

        [Header("--- Alertas Sonoras de Presión Temporal ---")]
        [Tooltip("AudioSource para reproducir los beeps de alerta.")]
        [SerializeField] private AudioSource alertAudioSource;

        [Tooltip("Clip emitido cuando restan 60 segundos.")]
        [SerializeField] private AudioClip alert60SecondsClip;

        [Tooltip("Clip emitido cuando restan 30 segundos (ritmo urgente).")]
        [SerializeField] private AudioClip alert30SecondsClip;

        [Tooltip("Clip emitido cuando expira el tiempo límite.")]
        [SerializeField] private AudioClip timeExpiredClip;

        // Estado del cronómetro
        private float _timeRemaining;
        private float _elapsedTime;
        private bool _isTimerRunning;
        private bool _timeExpired;

        // Flags de alertas únicas
        private bool _alert60Triggered;
        private bool _alert30Triggered;

        // Registro del Criterio de Aceptación (3 señales de phishing)
        private bool _hasRecordedPhishingCompletion;
        private float _phishingCompletionRecordedSeconds = -1f;
        private string _phishingCompletionFormattedStamp = string.Empty;

        // Fecha y hora base del incidente histórico
        private readonly DateTime _incidentBaseTime = new DateTime(2015, 1, 29, 9, 14, 0);

        #region Eventos Públicos Desacoplados

        /// <summary>
        /// Se dispara cada frame que el tiempo cambia, enviando el tiempo formateado en MM:SS.
        /// </summary>
        public event Action<string> OnCountdownUpdatedFormatted;
        public event Action<float, string> OnTimerTick;

        /// <summary>
        /// Se dispara con la fecha/hora ambiental actualizada (ej: "29 ENE 2015 - 09:14 AM").
        /// </summary>
        public event Action<string> OnAmbientClockUpdated;
        public event Action<string> OnAmbientTimeTick;

        /// <summary>
        /// Se dispara a los 60s y 30s con el número de segundos restantes.
        /// </summary>
        public event Action<int> OnTimeAlertTriggered;
        public event Action<float> OnTimerCriticalWarning;

        /// <summary>
        /// Se dispara al agotarse el tiempo reglamentario de 180s.
        /// </summary>
        public event Action OnTimeExpired;

        /// <summary>
        /// Se dispara cuando el jugador completa la detección de las 3 señales de phishing.
        /// Argumentos: (segundos empleados, tiempo restante formateado).
        /// </summary>
        public event Action<float, string> OnPhishingDetectionCompleted;

        #endregion

        #region Propiedades Públicas

        public float TimeRemaining => Mathf.Max(0f, _timeRemaining);
        public float ElapsedTime => _elapsedTime;
        public bool IsTimerRunning => _isTimerRunning;
        public bool HasRecordedPhishingCompletion => _hasRecordedPhishingCompletion;
        public float PhishingCompletionRecordedSeconds => _phishingCompletionRecordedSeconds;
        public string PhishingCompletionFormattedStamp => _phishingCompletionFormattedStamp;

        #endregion

        private void Awake()
        {
            if (I == null) I = this;

            if (alertAudioSource == null)
            {
                alertAudioSource = GetComponent<AudioSource>();
            }

            _timeRemaining = totalDurationSeconds;
            _elapsedTime = 0f;
        }

        private void Start()
        {
            // Sincronizar duración con GameManager.data si está presente
            if (GameManager.I != null && GameManager.I.data != null)
            {
                totalDurationSeconds = GameManager.I.data.TimeLimitSeconds;
                _timeRemaining = totalDurationSeconds;
            }

            UpdateDisplays(forceImmediate: true);

            if (autoStartOnStart)
            {
                StartClock();
            }
        }

        private void Update()
        {
            if (!_isTimerRunning || _timeExpired) return;

            _elapsedTime += Time.deltaTime;
            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                HandleTimeExpired();
            }
            else
            {
                CheckTimeAlerts();
            }

            UpdateDisplays(forceImmediate: false);
        }

        /// <summary>
        /// Inicia o reanuda el cronómetro.
        /// </summary>
        public void StartClock()
        {
            _isTimerRunning = true;
            Debug.Log($"{LogPrefix} Cronómetro de 180s iniciado. Presión temporal activa.");
        }

        /// <summary>
        /// Pausa el conteo de tiempo (ej. durante menús de pausa o tutorial).
        /// </summary>
        public void PauseClock()
        {
            _isTimerRunning = false;
            Debug.Log($"{LogPrefix} Cronómetro en pausa.");
        }

        /// <summary>
        /// Reinicia el cronómetro al estado inicial de 180s.
        /// </summary>
        public void ResetClock()
        {
            _timeRemaining = totalDurationSeconds;
            _elapsedTime = 0f;
            _timeExpired = false;
            _alert60Triggered = false;
            _alert30Triggered = false;
            _hasRecordedPhishingCompletion = false;
            _phishingCompletionRecordedSeconds = -1f;
            _phishingCompletionFormattedStamp = string.Empty;

            UpdateDisplays(forceImmediate: true);
            Debug.Log($"{LogPrefix} Cronómetro reiniciado a {totalDurationSeconds}s.");
        }

        /// <summary>
        /// Registra el tiempo exacto en que el jugador completó el hallazgo de las 3 señales de phishing (Criterio de Aceptación).
        /// </summary>
        public void RecordPhishingDetectionCompleted()
        {
            if (_hasRecordedPhishingCompletion) return;

            _hasRecordedPhishingCompletion = true;
            _phishingCompletionRecordedSeconds = _elapsedTime;

            string formattedElapsed = FormatTimeMMSS(_elapsedTime);
            string formattedRemaining = FormatTimeMMSS(_timeRemaining);
            _phishingCompletionFormattedStamp = formattedElapsed;

            bool criteriaPassed = _elapsedTime <= totalDurationSeconds;

            Debug.Log($"{LogPrefix} <color=#4EC9B0><b>CRITERIO DE ACEPTACIÓN ALCANZADO:</b></color> " +
                      $"3 señales de phishing identificadas en <b>{formattedElapsed}</b> " +
                      $"(Tiempo restante: {formattedRemaining} | Estado: {(criteriaPassed ? "<color=green>APROBADO (< 3 min)</color>" : "<color=red>DESAPROBADO (> 3 min)</color>")})");

            OnPhishingDetectionCompleted?.Invoke(_elapsedTime, formattedElapsed);
        }

        /// <summary>
        /// Evalúa y detona las alertas sonoras y visuales cuando el tiempo baja de 60s y 30s.
        /// </summary>
        private void CheckTimeAlerts()
        {
            if (_timeRemaining <= 60f && !_alert60Triggered)
            {
                _alert60Triggered = true;
                Debug.LogWarning($"{LogPrefix} <color=#FFC66D><b>ALERTA DE TIEMPO: Quedan 60 segundos para finalizar la simulación.</b></color>");
                PlayAlertSound(alert60SecondsClip);
                OnTimeAlertTriggered?.Invoke(60);
                OnTimerCriticalWarning?.Invoke(60f);
            }

            if (_timeRemaining <= 30f && !_alert30Triggered)
            {
                _alert30Triggered = true;
                Debug.LogWarning($"{LogPrefix} <color=#FF6B68><b>ALERTA CRÍTICA: ¡Quedan sólo 30 segundos!</b></color>");
                PlayAlertSound(alert30SecondsClip);
                OnTimeAlertTriggered?.Invoke(30);
                OnTimerCriticalWarning?.Invoke(30f);
            }
        }

        /// <summary>
        /// Maneja el momento en que el tiempo llega a 00:00.
        /// </summary>
        private void HandleTimeExpired()
        {
            _timeExpired = true;
            _isTimerRunning = false;

            Debug.LogError($"{LogPrefix} <color=#FF4444><b>TIEMPO EXPIRADO: Se agotaron los 180 segundos reglamentarios.</b></color>");
            PlayAlertSound(timeExpiredClip);

            OnTimeExpired?.Invoke();

            if (GameManager.I != null && !GameManager.I.IsScenarioEnded)
            {
                GameManager.I.EndScenario();
            }
        }

        /// <summary>
        /// Actualiza los textos de World Space en formato MM:SS y la fecha ambiental de la oficina.
        /// </summary>
        private void UpdateDisplays(bool forceImmediate)
        {
            // 1. Formato Countdown MM:SS
            string countdownFormatted = FormatTimeMMSS(_timeRemaining);
            if (countdownDisplay != null)
            {
                countdownDisplay.text = countdownFormatted;

                // Modulación de color por urgencia
                if (_timeRemaining <= 30f)
                {
                    countdownDisplay.color = Color.red;
                }
                else if (_timeRemaining <= 60f)
                {
                    countdownDisplay.color = new Color(1f, 0.65f, 0f); // Naranja
                }
                else
                {
                    countdownDisplay.color = Color.white;
                }
            }
            OnCountdownUpdatedFormatted?.Invoke(countdownFormatted);
            OnTimerTick?.Invoke(_timeRemaining, countdownFormatted);

            // 2. Reloj Ambiental del Banco Andino ("29 ENE 2015 - 09:14 AM")
            DateTime simDateTime = _incidentBaseTime.AddSeconds(_elapsedTime);
            string ambientFormatted = $"{simDateTime.Day:00} ENE {simDateTime.Year} - {simDateTime:hh:mm tt}";

            if (ambientClockDisplay != null)
            {
                ambientClockDisplay.text = ambientFormatted;
            }
            OnAmbientClockUpdated?.Invoke(ambientFormatted);
            OnAmbientTimeTick?.Invoke(ambientFormatted);
        }

        private void PlayAlertSound(AudioClip clip)
        {
            if (clip != null && alertAudioSource != null)
            {
                alertAudioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Utilidad para formatear segundos en formato "MM:SS".
        /// </summary>
        public static string FormatTimeMMSS(float secondsToFormat)
        {
            int totalSec = Mathf.Max(0, Mathf.FloorToInt(secondsToFormat));
            int minutes = totalSec / 60;
            int seconds = totalSec % 60;
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
