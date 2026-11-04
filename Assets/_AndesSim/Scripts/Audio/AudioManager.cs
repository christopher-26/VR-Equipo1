using System;
using UnityEngine;
using AndesSim.Core;

namespace AndesSim.Audio
{
    /// <summary>
    /// Administrador Central de Audio Procedural y Espacial de ANDES-SIM (BANCO ANDINO).
    /// Escenario 1: "Oficina de pagos" (Equipo 1).
    /// - Sintetiza proceduralmente en Awake() las 7 ondas sonoras matemáticas obligatorias
    ///   para garantizar audio 100% libre de dependencias externas (archivos .wav/.mp3):
    ///   1. ambientNightOfficeClip: Zumbido suave de aire acondicionado y ventiladores (ruido rosa filtrado en loop).
    ///   2. gazeHoverClip: Beep corto de alta tecnología (880 Hz, 0.05s).
    ///   3. gazeClickClip: Doble tono de confirmación (1200 Hz -> 1600 Hz, 0.12s).
    ///   4. evidenceFoundClip: Acorde ascendente de descubrimiento forense.
    ///   5. phishingSignalClip: Tono de advertencia táctica (señales de phishing).
    ///   6. infectionAlarmClip: Sirena pulsante de alerta del SOC (440 Hz / 330 Hz).
    ///   7. quarantineSuccessClip: Fanfarria armónica limpia (cuarentena y sandbox exitosos).
    /// - Permite reemplazar cualquiera de los clips arrastrándolos directamente en el Inspector.
    /// - Se conecta automáticamente con los eventos de GameManager (evidencias, defensas, phishing).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - AudioManager]";

        [Header("--- 1. Clips de Audio (Inspector o Síntesis Procedural) ---")]
        [Tooltip("Zumbido suave de aire acondicionado y ventiladores de computadoras en loop continuo.")]
        [SerializeField] private AudioClip ambientNightOfficeClip;

        [Tooltip("Beep corto de alta tecnología (880 Hz, 0.05s) al posar la mirada en un interactuable.")]
        [SerializeField] private AudioClip gazeHoverClip;

        [Tooltip("Doble tono de confirmación (1200 Hz -> 1600 Hz, 0.12s) al activar con la retícula.")]
        [SerializeField] private AudioClip gazeClickClip;

        [Tooltip("Acorde ascendente arpegiado de descubrimiento de evidencia forense.")]
        [SerializeField] private AudioClip evidenceFoundClip;

        [Tooltip("Tono de advertencia táctica al detectar una señal de phishing.")]
        [SerializeField] private AudioClip phishingSignalClip;

        [Tooltip("Sirena pulsante de alerta del SOC (440 Hz / 330 Hz) en Modo Ataque.")]
        [SerializeField] private AudioClip infectionAlarmClip;

        [Tooltip("Fanfarria armónica limpia cuando el correo es bloqueado en cuarentena.")]
        [SerializeField] private AudioClip quarantineSuccessClip;

        [Header("--- 2. Canales de Audio ---")]
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource alarmSource;

        [Header("--- 3. Controles de Volumen ---")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float ambientVolume = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float alarmVolume = 0.80f;

        [Header("--- 4. Configuración Automática ---")]
        [Tooltip("Si está activo, inicia el loop ambiental de la oficina bancaria nocturna al cargar.")]
        [SerializeField] private bool autoStartAmbientLoop = true;

        [Tooltip("Si está activo, escucha los eventos de GameManager para detonar sonidos de feedback automáticamente.")]
        [SerializeField] private bool listenGameManagerEvents = true;

        #region Propiedades Públicas de Clips

        public AudioClip AmbientNightOfficeClip => ambientNightOfficeClip;
        public AudioClip GazeHoverClip => gazeHoverClip;
        public AudioClip GazeClickClip => gazeClickClip;
        public AudioClip EvidenceFoundClip => evidenceFoundClip;
        public AudioClip PhishingSignalClip => phishingSignalClip;
        public AudioClip InfectionAlarmClip => infectionAlarmClip;
        public AudioClip QuarantineSuccessClip => quarantineSuccessClip;

        #endregion

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

            SetupAudioChannels();
            SynthesizeProceduralClipsIfMissing();
        }

        private void Start()
        {
            if (autoStartAmbientLoop)
            {
                PlayAmbientNightOffice(true);
            }

            if (listenGameManagerEvents && GameManager.I != null)
            {
                GameManager.I.OnEvidenceRegistered += HandleEvidenceRegistered;
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
                GameManager.I.OnPhishingSignalRegistered += HandlePhishingSignalDiscovered;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnEvidenceRegistered -= HandleEvidenceRegistered;
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
                GameManager.I.OnPhishingSignalRegistered -= HandlePhishingSignalDiscovered;
            }
        }

        #region Configuración de Canales de Audio

        private void SetupAudioChannels()
        {
            // 1. Canal SFX (OneShot)
            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
                if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D Directo a auriculares
            sfxSource.volume = sfxVolume * masterVolume;

            // 2. Canal Ambiental (Loop continuo de oficina)
            if (ambientSource == null)
            {
                GameObject ambObj = new GameObject("AudioChannel_Ambient");
                ambObj.transform.SetParent(transform, false);
                ambientSource = ambObj.AddComponent<AudioSource>();
            }
            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            ambientSource.spatialBlend = 0.25f;
            ambientSource.volume = ambientVolume * masterVolume;

            // 3. Canal de Alarma de Infección
            if (alarmSource == null)
            {
                GameObject almObj = new GameObject("AudioChannel_Alarm");
                almObj.transform.SetParent(transform, false);
                alarmSource = almObj.AddComponent<AudioSource>();
            }
            alarmSource.playOnAwake = false;
            alarmSource.loop = true;
            alarmSource.spatialBlend = 0.35f;
            alarmSource.volume = alarmVolume * masterVolume;
        }

        #endregion

        #region Síntesis de Ondas Matemáticas Procedurales (Awake)

        /// <summary>
        /// Genera proceduralmente mediante código y ondas matemáticas cualquiera de los 7 clips requeridos
        /// que no haya sido asignado manualmente en el Inspector.
        /// </summary>
        public void SynthesizeProceduralClipsIfMissing()
        {
            int synthesizedCount = 0;

            // 1. ambientNightOfficeClip: Ruido rosa filtrado + zumbido 60Hz/120Hz
            if (ambientNightOfficeClip == null)
            {
                ambientNightOfficeClip = GenerateAmbientNightOfficeClip();
                synthesizedCount++;
            }

            // 2. gazeHoverClip: Beep corto (880 Hz, 0.05s)
            if (gazeHoverClip == null)
            {
                gazeHoverClip = GenerateGazeHoverClip();
                synthesizedCount++;
            }

            // 3. gazeClickClip: Doble tono de confirmación (1200 Hz -> 1600 Hz, 0.12s)
            if (gazeClickClip == null)
            {
                gazeClickClip = GenerateGazeClickClip();
                synthesizedCount++;
            }

            // 4. evidenceFoundClip: Acorde ascendente forense
            if (evidenceFoundClip == null)
            {
                evidenceFoundClip = GenerateEvidenceFoundClip();
                synthesizedCount++;
            }

            // 5. phishingSignalClip: Tono de advertencia táctica
            if (phishingSignalClip == null)
            {
                phishingSignalClip = GeneratePhishingSignalClip();
                synthesizedCount++;
            }

            // 6. infectionAlarmClip: Sirena pulsante de alerta SOC (440 Hz / 330 Hz)
            if (infectionAlarmClip == null)
            {
                infectionAlarmClip = GenerateInfectionAlarmClip();
                synthesizedCount++;
            }

            // 7. quarantineSuccessClip: Fanfarria armónica limpia de defensa
            if (quarantineSuccessClip == null)
            {
                quarantineSuccessClip = GenerateQuarantineSuccessClip();
                synthesizedCount++;
            }

            Debug.Log($"{LogPrefix} <color=#38F28A><b>Síntesis de Audio Procedural Completa:</b></color> " +
                      $"{synthesizedCount} ondas sintetizadas matemáticamente en runtime.");
        }

        /// <summary>
        /// 1. Zumbido suave de aire acondicionado y ventiladores de computadoras en loop continuo.
        /// Generado con ruido rosa (Kellet filter), atenuación paso bajo a ~280Hz y armónicos de 60Hz/120Hz.
        /// </summary>
        private AudioClip GenerateAmbientNightOfficeClip()
        {
            int sampleRate = 44100;
            float duration = 4.0f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            float b0 = 0f, b1 = 0f, b2 = 0f, b3 = 0f, b4 = 0f, b5 = 0f, b6 = 0f;
            System.Random rand = new System.Random(1337);

            for (int i = 0; i < numSamples; i++)
            {
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                b0 = 0.99886f * b0 + white * 0.0555179f;
                b1 = 0.99332f * b1 + white * 0.0750759f;
                b2 = 0.96900f * b2 + white * 0.1538520f;
                b3 = 0.86650f * b3 + white * 0.3104856f;
                b4 = 0.55000f * b4 + white * 0.5329522f;
                b5 = -0.7616f * b5 - white * 0.0168980f;
                float pink = (b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362f) * 0.045f;
                b6 = white * 0.115926f;

                float t = (float)i / sampleRate;
                float hum60 = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.045f;
                float hum120 = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.022f;

                samples[i] = pink + hum60 + hum120;
            }

            // Filtro paso bajo simple (LPF) a ~280 Hz para amortiguar el flujo de aire
            float lp = 0f;
            float alpha = 0.075f;
            for (int i = 0; i < numSamples; i++)
            {
                lp += alpha * (samples[i] - lp);
                samples[i] = lp;
            }

            // Crossfade suave para inicio y fin de loop continuo sin clics
            int crossfade = (int)(sampleRate * 0.35f);
            for (int i = 0; i < crossfade; i++)
            {
                float w = (float)i / crossfade;
                int tailIndex = numSamples - crossfade + i;
                samples[i] = Mathf.Lerp(samples[tailIndex], samples[i], w);
            }

            AudioClip clip = AudioClip.Create("Procedural_AmbientNightOffice", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 2. Beep corto de alta tecnología (880 Hz, 0.05s) al posar la mirada en un objeto interactivo.
        /// </summary>
        private AudioClip GenerateGazeHoverClip()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (t / duration);
                if (t < 0.005f) envelope *= (t / 0.005f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * envelope * 0.22f;
            }

            AudioClip clip = AudioClip.Create("Procedural_GazeHover", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 3. Doble tono de confirmación (1200 Hz -> 1600 Hz, 0.12s) al activar un botón con la retícula.
        /// </summary>
        private AudioClip GenerateGazeClickClip()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float sample = 0f;

                // Primer pulso (1200 Hz, 0 - 0.05s)
                if (t < 0.05f)
                {
                    float env = 1f - (t / 0.05f);
                    if (t < 0.005f) env *= (t / 0.005f);
                    sample = Mathf.Sin(2f * Mathf.PI * 1200f * t) * env * 0.35f;
                }
                // Segundo pulso (1600 Hz, 0.06 - 0.12s)
                else if (t >= 0.06f)
                {
                    float t2 = t - 0.06f;
                    float env2 = 1f - (t2 / 0.06f);
                    if (t2 < 0.005f) env2 *= (t2 / 0.005f);
                    sample = Mathf.Sin(2f * Mathf.PI * 1600f * t2) * env2 * 0.40f;
                }

                samples[i] = sample;
            }

            AudioClip clip = AudioClip.Create("Procedural_GazeClick", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 4. Acorde ascendente de descubrimiento forense (C5 -> E5 -> G5 -> C6).
        /// </summary>
        private AudioClip GenerateEvidenceFoundClip()
        {
            int sampleRate = 44100;
            float duration = 0.65f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            float[] freqs = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float[] startTimes = { 0.0f, 0.09f, 0.18f, 0.27f };

            for (int n = 0; n < freqs.Length; n++)
            {
                float f = freqs[n];
                float st = startTimes[n];
                int startIdx = (int)(st * sampleRate);

                for (int i = startIdx; i < numSamples; i++)
                {
                    float tNote = (float)(i - startIdx) / sampleRate;
                    float decay = (n == freqs.Length - 1) ? 5.5f : 8f;
                    float env = Mathf.Exp(-tNote * decay);
                    if (tNote < 0.008f) env *= (tNote / 0.008f);

                    float fundamental = Mathf.Sin(2f * Mathf.PI * f * tNote);
                    float harmonic = Mathf.Sin(2f * Mathf.PI * (f * 2f) * tNote) * 0.20f;
                    samples[i] += (fundamental + harmonic) * env * 0.22f;
                }
            }

            AudioClip clip = AudioClip.Create("Procedural_EvidenceFound", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 5. Tono de advertencia táctica al detectar una de las 3 señales de phishing (740 Hz -> 880 Hz).
        /// </summary>
        private AudioClip GeneratePhishingSignalClip()
        {
            int sampleRate = 44100;
            float duration = 0.26f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float sample = 0f;

                if (t < 0.09f)
                {
                    float env = 1f - (t / 0.09f);
                    if (t < 0.005f) env *= (t / 0.005f);
                    sample = (Mathf.Sin(2f * Mathf.PI * 740f * t) + Mathf.Sin(2f * Mathf.PI * 1480f * t) * 0.25f) * env * 0.35f;
                }
                else if (t >= 0.12f)
                {
                    float t2 = t - 0.12f;
                    float env = 1f - (t2 / 0.14f);
                    if (t2 < 0.005f) env *= (t2 / 0.005f);
                    sample = (Mathf.Sin(2f * Mathf.PI * 880f * t2) + Mathf.Sin(2f * Mathf.PI * 1760f * t2) * 0.30f) * env * 0.40f;
                }

                samples[i] = sample;
            }

            AudioClip clip = AudioClip.Create("Procedural_PhishingSignal", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 6. Sirena pulsante de alerta del SOC (440 Hz / 330 Hz) en Modo Ataque.
        /// </summary>
        private AudioClip GenerateInfectionAlarmClip()
        {
            int sampleRate = 44100;
            float duration = 1.20f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float cycle = t % 0.60f;
                float freq = (cycle < 0.30f) ? 440f : 330f;
                float tHalf = (cycle < 0.30f) ? cycle : (cycle - 0.30f);

                float env = 1f - (tHalf / 0.35f);
                if (tHalf < 0.01f) env *= (tHalf / 0.01f);

                float fundamental = Mathf.Sin(2f * Mathf.PI * freq * t);
                float harmonic = Mathf.Sin(2f * Mathf.PI * (freq * 3f) * t) * 0.18f;
                samples[i] = (fundamental + harmonic) * env * 0.38f;
            }

            AudioClip clip = AudioClip.Create("Procedural_InfectionAlarm", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 7. Fanfarria armónica limpia cuando el correo es bloqueado en cuarentena en Modo Defensa.
        /// </summary>
        private AudioClip GenerateQuarantineSuccessClip()
        {
            int sampleRate = 44100;
            float duration = 0.95f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            float[] freqs = { 349.23f, 440f, 523.25f, 698.46f, 1046.50f };
            float[] startTimes = { 0.0f, 0.10f, 0.20f, 0.32f, 0.32f };

            for (int n = 0; n < freqs.Length; n++)
            {
                float f = freqs[n];
                float st = startTimes[n];
                int startIdx = (int)(st * sampleRate);

                for (int i = startIdx; i < numSamples; i++)
                {
                    float tNote = (float)(i - startIdx) / sampleRate;
                    float decay = (n >= 3) ? 2.5f : 4.5f;
                    float env = Mathf.Exp(-tNote * decay);
                    if (tNote < 0.01f) env *= (tNote / 0.01f);
                    samples[i] += Mathf.Sin(2f * Mathf.PI * f * tNote) * env * 0.22f;
                }
            }

            AudioClip clip = AudioClip.Create("Procedural_QuarantineSuccess", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion

        #region API Pública de Reproducción

        public void PlayGazeHover()
        {
            PlaySound(gazeHoverClip, 0.50f);
        }

        public void PlayGazeClick()
        {
            PlaySound(gazeClickClip, 0.85f);
        }

        public void PlayEvidenceFound()
        {
            PlaySound(evidenceFoundClip, 0.90f);
        }

        public void PlayPhishingSignal()
        {
            PlaySound(phishingSignalClip, 0.85f);
        }

        public void PlayInfectionAlarm(bool loop = false)
        {
            if (alarmSource == null || infectionAlarmClip == null) return;
            alarmSource.clip = infectionAlarmClip;
            alarmSource.loop = loop;
            alarmSource.volume = alarmVolume * masterVolume;
            alarmSource.Play();
        }

        public void StopInfectionAlarm()
        {
            if (alarmSource != null && alarmSource.isPlaying)
            {
                alarmSource.Stop();
            }
        }

        public void PlayQuarantineSuccess()
        {
            PlaySound(quarantineSuccessClip, 0.90f);
        }

        public void PlayAmbientNightOffice(bool play = true)
        {
            if (ambientSource == null || ambientNightOfficeClip == null) return;

            if (play)
            {
                ambientSource.clip = ambientNightOfficeClip;
                ambientSource.volume = ambientVolume * masterVolume;
                if (!ambientSource.isPlaying) ambientSource.Play();
            }
            else
            {
                ambientSource.Stop();
            }
        }

        public void PlaySound(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume * masterVolume);
        }

        public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f, float spatialBlend = 1f)
        {
            if (clip == null) return;
            GameObject tempAudio = new GameObject("TempAudioSource_" + clip.name);
            tempAudio.transform.position = position;

            AudioSource src = tempAudio.AddComponent<AudioSource>();
            src.clip = clip;
            src.spatialBlend = spatialBlend;
            src.volume = volumeScale * sfxVolume * masterVolume;
            src.Play();

            Destroy(tempAudio, clip.length + 0.1f);
        }

        #endregion

        #region Handlers de Eventos de GameManager

        private void HandleEvidenceRegistered(string evidenceId)
        {
            PlayEvidenceFound();
        }

        private void HandleDefenseApplied(string defenseId)
        {
            PlayQuarantineSuccess();
        }

        private void HandlePhishingSignalDiscovered(string signalId)
        {
            PlayPhishingSignal();
        }

        #endregion
    }
}
