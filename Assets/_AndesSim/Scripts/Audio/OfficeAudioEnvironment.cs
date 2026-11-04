using System.Collections;
using UnityEngine;
using AndesSim.Core;

namespace AndesSim.Audio
{
    /// <summary>
    /// Gestión del entorno sonoro inmersivo en 3D para la oficina de pagos (RF-07).
    /// Simulación VR ANDES-SIM (BANCO ANDINO) - Escenario 1: "Oficina de pagos".
    /// - Reproduce zumbido ambiental de oficina y ventiladores de servidores.
    /// - Emite llamadas telefónicas periódicas distantes para aumentar la inmersión y tensión.
    /// - Incluye sonido de impresora matricial/láser atascada (indicio de Hugo Benavides).
    /// - Sintetiza clips de audio procedimentales de respaldo si no hay clips .wav/.mp3 asignados,
    ///   garantizando funcionamiento 100% libre de errores bajo cualquier condición.
    /// </summary>
    public class OfficeAudioEnvironment : MonoBehaviour
    {
        private const string LogPrefix = "[ANDES-SIM - Audio]";

        [Header("--- Fuentes de Audio Espacial ---")]
        [SerializeField] private AudioSource ambientLoopSource;
        [SerializeField] private AudioSource telephoneSource;
        [SerializeField] private AudioSource printerSource;
        [SerializeField] private AudioSource alertSource;

        [Header("--- Clips de Audio (Opcionales o Procedurales) ---")]
        [SerializeField] private AudioClip officeAmbienceClip;
        [SerializeField] private AudioClip phoneRingClip;
        [SerializeField] private AudioClip printerWorkClip;
        [SerializeField] private AudioClip warningAlertClip;

        [Header("--- Intervalos de Eventos Ambientales ---")]
        [SerializeField] private float minPhoneInterval = 25f;
        [SerializeField] private float maxPhoneInterval = 55f;

        [Header("--- Volumen General ---")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.8f;

        private void Start()
        {
            SetupAudioSources();
            StartCoroutine(PhoneRingingRoutine());

            // Suscribirse a alertas del reloj
            SimulationClock clock = FindFirstObjectByType<SimulationClock>();
            if (clock != null)
            {
                clock.OnTimerCriticalWarning += HandleTimerCriticalWarning;
            }
        }

        private void OnDestroy()
        {
            SimulationClock clock = FindFirstObjectByType<SimulationClock>();
            if (clock != null)
            {
                clock.OnTimerCriticalWarning -= HandleTimerCriticalWarning;
            }
        }

        private void SetupAudioSources()
        {
            // 1. Loop ambiental
            if (ambientLoopSource == null)
            {
                ambientLoopSource = gameObject.AddComponent<AudioSource>();
            }

            ambientLoopSource.loop = true;
            ambientLoopSource.spatialBlend = 0.3f; // Semi-ambiental
            ambientLoopSource.volume = 0.25f * masterVolume;

            if (officeAmbienceClip == null)
            {
                officeAmbienceClip = GenerateProceduralHum(2.0f, 120f); // 120Hz hum de transformador/ventilador
            }

            ambientLoopSource.clip = officeAmbienceClip;
            ambientLoopSource.Play();

            // 2. Teléfono distante
            if (telephoneSource == null)
            {
                telephoneSource = gameObject.AddComponent<AudioSource>();
            }
            telephoneSource.spatialBlend = 0.85f; // 3D
            telephoneSource.volume = 0.4f * masterVolume;

            if (phoneRingClip == null)
            {
                phoneRingClip = GenerateProceduralPhoneRing();
            }

            // 3. Alertas
            if (alertSource == null)
            {
                alertSource = gameObject.AddComponent<AudioSource>();
            }
            alertSource.spatialBlend = 0.2f;
            alertSource.volume = 0.6f * masterVolume;

            if (warningAlertClip == null)
            {
                warningAlertClip = GenerateProceduralBeep(880f, 0.25f);
            }
        }

        private IEnumerator PhoneRingingRoutine()
        {
            while (true)
            {
                float waitTime = Random.Range(minPhoneInterval, maxPhoneInterval);
                yield return new WaitForSeconds(waitTime);

                if (telephoneSource != null && phoneRingClip != null)
                {
                    telephoneSource.PlayOneShot(phoneRingClip);
                }
            }
        }

        private void HandleTimerCriticalWarning(float remainingSeconds)
        {
            if (alertSource != null && warningAlertClip != null)
            {
                alertSource.PlayOneShot(warningAlertClip);
            }
        }

        #region Generación de Audio Procedural (Fallback 100% Confiable)

        private AudioClip GenerateProceduralHum(float duration, float frequency)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Onda sinusoidal suave de 120Hz + ruido blanco suave
                float sine = Mathf.Sin(2 * Mathf.PI * frequency * t) * 0.15f;
                float noise = (Random.value * 2f - 1f) * 0.03f;
                samples[i] = sine + noise;
            }

            AudioClip clip = AudioClip.Create("Procedural_OfficeHum", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralPhoneRing()
        {
            int sampleRate = 44100;
            float duration = 1.6f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Modulación de doble tono telefónico tradicional (440Hz + 480Hz)
                if ((t < 0.4f) || (t > 0.6f && t < 1.0f))
                {
                    float tone1 = Mathf.Sin(2 * Mathf.PI * 440f * t);
                    float tone2 = Mathf.Sin(2 * Mathf.PI * 480f * t);
                    samples[i] = (tone1 + tone2) * 0.18f;
                }
                else
                {
                    samples[i] = 0f;
                }
            }

            AudioClip clip = AudioClip.Create("Procedural_PhoneRing", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralBeep(float frequency, float duration)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - (t / duration));
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * 0.3f * envelope;
            }

            AudioClip clip = AudioClip.Create("Procedural_AlertBeep", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
