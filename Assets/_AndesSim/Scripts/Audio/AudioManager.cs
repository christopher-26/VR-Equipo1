using System;
using System.Collections;
using UnityEngine;
using AndesSim.Core;

namespace AndesSim.Audio
{
    /// <summary>
    /// Temas musicales adaptativos para la simulación ANDES-SIM.
    /// </summary>
    public enum MusicTheme
    {
        None,
        MenuIntro,
        CyberInvestigation,
        AttackTension,
        DefenseVictory
    }

    /// <summary>
    /// Administrador Central de Audio Procedural, Espacial y Soundtrack Dinámico de ANDES-SIM (BANCO ANDINO).
    /// Escenario 1: "Oficina de pagos" (Equipo 1).
    /// - Gestiona canales independientes: Música de Fondo, Ambiente de Oficina, Efectos SFX y Alarma SOC.
    /// - Incluye 4 pistas musicales cinematográficas adaptativas con crossfade suave:
    ///   1. MenuIntro: Tema atmosférico futurista para el menú de inicio y briefing.
    ///   2. CyberInvestigation: Tema principal de investigación en la oficina bancaria (D minor synthwave).
    ///   3. AttackTension: Tensión de alta urgencia (128 BPM driving bass) ante propagación de malware.
    ///   4. DefenseVictory: Fanfarria de victoria y ciberdefensa restablecida al aplicar cuarentena.
    /// - Carga clips físicos de alta definición desde 'Assets/_AndesSim/Audio/...' y cuenta con sintetizador
    ///   matemático procedural como respaldo a prueba de fallos (0 dependencias externas obligatorias).
    /// - Se conecta automáticamente con los eventos de GameManager (evidencias, defensas, phishing y modos).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        private const string LogPrefix = "[ANDES-SIM - AudioManager]";

        [Header("--- 0. Pistas de Música Dinámica (Soundtrack) ---")]
        [Tooltip("Tema musical del Menú Principal VR (Intro cyberpunk suave y atmosférica).")]
        [SerializeField] private AudioClip menuIntroMusicClip;

        [Tooltip("Tema principal de investigación en la oficina bancaria (Cyber synth Dm arpegiado).")]
        [SerializeField] private AudioClip investigationMusicClip;

        [Tooltip("Tema de tensión y ataque activo (Música Horror / Tensión de infección).")]
        [SerializeField] private AudioClip attackTensionMusicClip;

        [Tooltip("Audio de reacción de alerta de NPC cuando se ejecuta el malware (Diálogo: 'What's That').")]
        [SerializeField] private AudioClip infectionVoiceAlertClip;

        [Tooltip("Tema de victoria y ciberdefensa restablecida (Fanfarria mayor, escudo digital).")]
        [SerializeField] private AudioClip defenseVictoryMusicClip;

        [Header("--- 1. Clips de Efectos Físicos y de Interacción (SFX) ---")]
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

        [Tooltip("Timbre telefónico de conmutador IP de la operadora Carla Mendoza.")]
        [SerializeField] private AudioClip phoneRingClip;

        [Tooltip("Sonido de tipeo rápido en teclado mecánico de oficina bancaria.")]
        [SerializeField] private AudioClip keyboardTypingClip;

        [Tooltip("Sonido de palanca / switch industrial de relé (Defensas SIEM).")]
        [SerializeField] private AudioClip switchToggleClip;

        [Tooltip("Sonido de haz de escaneo láser / inspección forense de correo.")]
        [SerializeField] private AudioClip laserScanClip;

        [Tooltip("Efecto de descompresión y teletransporte VR (Whoosh espacial).")]
        [SerializeField] private AudioClip teleportWhooshClip;

        [Header("--- 2. Canales de Audio ---")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource alarmSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("--- 3. Controles de Volumen ---")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.50f;
        [Range(0f, 1f)] [SerializeField] private float ambientVolume = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float alarmVolume = 0.80f;
        [Range(0f, 1f)] [SerializeField] private float voiceVolume = 1.00f;

        [Header("--- 4. Configuración Automática ---")]
        [Tooltip("Si está activo, inicia el loop musical y ambiental de la oficina al cargar.")]
        [SerializeField] private bool autoStartMusic = true;
        [SerializeField] private bool autoStartAmbientLoop = true;
        [SerializeField] private bool enableVoiceNarration = true;

        [Tooltip("Si está activo, escucha los eventos de GameManager para detonar sonidos y conmutar música automáticamente.")]
        [SerializeField] private bool listenGameManagerEvents = true;

        private Coroutine _musicCrossfadeCoroutine;
        private Coroutine _voiceDuckCoroutine;
        private MusicTheme _currentMusicTheme = MusicTheme.None;
        private readonly System.Collections.Generic.Dictionary<string, AudioClip> _narrationCache =
            new System.Collections.Generic.Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        private string _lastNarrationKey;
        private float _lastNarrationRealtime = -999f;

        #region Propiedades Públicas

        public MusicTheme CurrentMusicTheme => _currentMusicTheme;
        public AudioClip MenuIntroMusicClip => menuIntroMusicClip;
        public AudioClip InvestigationMusicClip => investigationMusicClip;
        public AudioClip AttackTensionMusicClip => attackTensionMusicClip;
        public AudioClip InfectionVoiceAlertClip => infectionVoiceAlertClip;
        public AudioClip DefenseVictoryMusicClip => defenseVictoryMusicClip;

        public AudioClip AmbientNightOfficeClip => ambientNightOfficeClip;
        public AudioClip GazeHoverClip => gazeHoverClip;
        public AudioClip GazeClickClip => gazeClickClip;
        public AudioClip EvidenceFoundClip => evidenceFoundClip;
        public AudioClip PhishingSignalClip => phishingSignalClip;
        public AudioClip InfectionAlarmClip => infectionAlarmClip;
        public AudioClip QuarantineSuccessClip => quarantineSuccessClip;

        public AudioClip PhoneRingClip => phoneRingClip;
        public AudioClip KeyboardTypingClip => keyboardTypingClip;
        public AudioClip SwitchToggleClip => switchToggleClip;
        public AudioClip LaserScanClip => laserScanClip;
        public AudioClip TeleportWhooshClip => teleportWhooshClip;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;

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
            LoadOrSynthesizeAllClips();
        }

        private void Start()
        {
            if (autoStartAmbientLoop)
            {
                PlayAmbientNightOffice(true);
            }

            if (autoStartMusic)
            {
                // Si la simulación ya inició, reproducir tema de investigación; si está en portal o menú, reproducir intro
                bool isPlaying = (GameManager.I != null && GameManager.I.RemainingTime < 180f);
                PlayMusicTrack(isPlaying ? MusicTheme.CyberInvestigation : MusicTheme.MenuIntro, 2.0f);
            }

            if (listenGameManagerEvents && GameManager.I != null)
            {
                GameManager.I.OnEvidenceRegistered += HandleEvidenceRegistered;
                GameManager.I.OnDefenseApplied += HandleDefenseApplied;
                GameManager.I.OnPhishingSignalRegistered += HandlePhishingSignalDiscovered;
                GameManager.I.OnModeChanged += HandleModeChanged;
            }

            if (enableVoiceNarration)
            {
                StartCoroutine(PlayWelcomeNarrationDelayed());
            }
        }

        private IEnumerator PlayWelcomeNarrationDelayed()
        {
            yield return new WaitForSecondsRealtime(0.75f);
            if (!enableVoiceNarration) yield break;
            if (GameManager.I != null && GameManager.I.IsScenarioEnded) yield break;
            if (voiceSource != null && !voiceSource.isPlaying && string.IsNullOrEmpty(_lastNarrationKey))
            {
                PlayNarration("Voice_01_Bienvenida", false, 5.0f);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.I != null)
            {
                GameManager.I.OnEvidenceRegistered -= HandleEvidenceRegistered;
                GameManager.I.OnDefenseApplied -= HandleDefenseApplied;
                GameManager.I.OnPhishingSignalRegistered -= HandlePhishingSignalDiscovered;
                GameManager.I.OnModeChanged -= HandleModeChanged;
            }
        }

        #region Configuración de Canales de Audio

        private void SetupAudioChannels()
        {
            // 0. Canal Música (Stereo, loop continuo con crossfade)
            if (musicSource == null)
            {
                GameObject musObj = new GameObject("AudioChannel_Music");
                musObj.transform.SetParent(transform, false);
                musicSource = musObj.AddComponent<AudioSource>();
            }
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f; // 2D Stereo
            musicSource.volume = musicVolume * masterVolume;

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

            // 4. Canal de Narración por Voz en Español (2D Claro en Auriculares)
            if (voiceSource == null)
            {
                GameObject voiceObj = new GameObject("AudioChannel_Voice");
                voiceObj.transform.SetParent(transform, false);
                voiceSource = voiceObj.AddComponent<AudioSource>();
            }
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0f;
            voiceSource.volume = voiceVolume * masterVolume;
        }

        #endregion

        #region Carga de Clips Físicos y Síntesis de Ondas Matemáticas

        /// <summary>
        /// Carga clips de audio físicos de alta calidad desde 'Assets/_AndesSim/Audio/...'
        /// y sintetiza proceduralmente ondas matemáticas para cualquier clip faltante.
        /// </summary>
        public void LoadOrSynthesizeAllClips()
        {
#if UNITY_EDITOR
            // Intentar enlazar clips de alta resolución generados si están vacíos
            if (menuIntroMusicClip == null)
                menuIntroMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/Music/Music_VrStartMenu_Intro.wav");
            if (investigationMusicClip == null)
                investigationMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/Music/Music_CyberInvestigation_Theme.wav");
            if (attackTensionMusicClip == null)
            {
                attackTensionMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Horror_Music_Free_Starter_Kit/WAV_LIGHT_TENSION_LOOP_The_Urge_to_Kill.wav");
                if (attackTensionMusicClip == null)
                    attackTensionMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Horror_Music_Free_Starter_Kit/WAV_LIGHT_TENSION_LOOP_Run_for_Your_Life.wav");
                if (attackTensionMusicClip == null)
                    attackTensionMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/Music/Music_AttackTension_Theme.wav");
            }
            if (infectionVoiceAlertClip == null)
            {
                infectionVoiceAlertClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/The Alerted NPC/04. What's That.wav");
            }
            if (defenseVictoryMusicClip == null)
                defenseVictoryMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/Music/Music_DefenseVictory_Theme.wav");

            if (ambientNightOfficeClip == null)
                ambientNightOfficeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/Ambience/SFX_Office_Atmosphere_Loop.wav");

            if (phoneRingClip == null)
                phoneRingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/SFX/SFX_Phone_Ring_3D.wav");
            if (keyboardTypingClip == null)
                keyboardTypingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/SFX/SFX_Keyboard_Typing.wav");
            if (switchToggleClip == null)
                switchToggleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/SFX/SFX_Switch_Toggle.wav");
            if (laserScanClip == null)
                laserScanClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/SFX/SFX_Cyber_LaserScan.wav");
            if (teleportWhooshClip == null)
                teleportWhooshClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_AndesSim/Audio/SFX/SFX_Teleport_Whoosh.wav");
#endif

            // Síntesis procedural de respaldo para garantizar cero nulos bajo cualquier circunstancia
            if (ambientNightOfficeClip == null) ambientNightOfficeClip = GenerateAmbientNightOfficeClip();
            if (gazeHoverClip == null) gazeHoverClip = GenerateGazeHoverClip();
            if (gazeClickClip == null) gazeClickClip = GenerateGazeClickClip();
            if (evidenceFoundClip == null) evidenceFoundClip = GenerateEvidenceFoundClip();
            if (phishingSignalClip == null) phishingSignalClip = GeneratePhishingSignalClip();
            if (infectionAlarmClip == null) infectionAlarmClip = GenerateInfectionAlarmClip();
            if (quarantineSuccessClip == null) quarantineSuccessClip = GenerateQuarantineSuccessClip();

            if (phoneRingClip == null) phoneRingClip = GenerateProceduralPhoneRing();
            if (keyboardTypingClip == null) keyboardTypingClip = GenerateProceduralKeyboardTyping();
            if (switchToggleClip == null) switchToggleClip = GenerateProceduralSwitchToggle();
            if (laserScanClip == null) laserScanClip = GenerateProceduralLaserScan();
            if (teleportWhooshClip == null) teleportWhooshClip = GenerateProceduralTeleportWhoosh();

            if (menuIntroMusicClip == null) menuIntroMusicClip = GenerateProceduralMenuMusic();
            if (investigationMusicClip == null) investigationMusicClip = GenerateProceduralInvestigationMusic();
            if (attackTensionMusicClip == null) attackTensionMusicClip = GenerateProceduralAttackMusic();
            if (defenseVictoryMusicClip == null) defenseVictoryMusicClip = GenerateProceduralDefenseMusic();

            Debug.Log($"{LogPrefix} <color=#38F28A><b>Sistema de Audio y Soundtrack Inicializado:</b></color> 14 clips activos (Música, SFX y Ambiente).");
        }

        #endregion

        #region Control Dinámico de Música (Crossfade)

        /// <summary>
        /// Conmuta la pista musical de fondo según el tema requerido con transición suave (crossfade).
        /// </summary>
        public void PlayMusicTrack(MusicTheme theme, float fadeDuration = 1.5f)
        {
            if (_currentMusicTheme == theme && musicSource != null && musicSource.isPlaying) return;
            _currentMusicTheme = theme;

            AudioClip targetClip = null;
            switch (theme)
            {
                case MusicTheme.MenuIntro:
                    targetClip = menuIntroMusicClip;
                    break;
                case MusicTheme.CyberInvestigation:
                    targetClip = investigationMusicClip;
                    break;
                case MusicTheme.AttackTension:
                    targetClip = attackTensionMusicClip;
                    break;
                case MusicTheme.DefenseVictory:
                    targetClip = defenseVictoryMusicClip;
                    break;
                case MusicTheme.None:
                default:
                    targetClip = null;
                    break;
            }

            PlayMusic(targetClip, fadeDuration);
        }

        public void PlayMusic(AudioClip clip, float fadeDuration = 1.5f, bool loop = true)
        {
            if (musicSource == null) return;
            if (_musicCrossfadeCoroutine != null)
            {
                StopCoroutine(_musicCrossfadeCoroutine);
            }
            _musicCrossfadeCoroutine = StartCoroutine(CrossfadeMusicRoutine(clip, fadeDuration, loop));
        }

        private IEnumerator CrossfadeMusicRoutine(AudioClip newClip, float duration, bool loop)
        {
            float targetVol = musicVolume * masterVolume;

            if (musicSource.isPlaying && musicSource.clip != null && duration > 0.05f)
            {
                float startVol = musicSource.volume;
                float elapsed = 0f;
                float halfDuration = duration * 0.5f;

                while (elapsed < halfDuration)
                {
                    elapsed += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfDuration);
                    yield return null;
                }
                musicSource.Stop();
            }

            if (newClip != null)
            {
                musicSource.clip = newClip;
                musicSource.loop = loop;
                musicSource.Play();

                float elapsed = 0f;
                float inDuration = duration > 0.05f ? duration * 0.5f : 0.01f;
                while (elapsed < inDuration)
                {
                    elapsed += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(0f, targetVol, elapsed / inDuration);
                    yield return null;
                }
                musicSource.volume = targetVol;
            }
            else
            {
                musicSource.Stop();
                musicSource.volume = targetVol;
            }

            _musicCrossfadeCoroutine = null;
        }

        public void StopMusic(float fadeDuration = 1.0f)
        {
            _currentMusicTheme = MusicTheme.None;
            PlayMusic(null, fadeDuration);
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            if (musicSource != null) musicSource.volume = musicVolume * masterVolume;
            if (ambientSource != null) ambientSource.volume = ambientVolume * masterVolume;
            if (sfxSource != null) sfxSource.volume = sfxVolume * masterVolume;
            if (alarmSource != null) alarmSource.volume = alarmVolume * masterVolume;
            if (voiceSource != null) voiceSource.volume = voiceVolume * masterVolume;
        }

        public void PlayNarration(string clipKey, bool interruptCurrent = true, float minRepeatCooldown = 3.0f)
        {
            if (!enableVoiceNarration || string.IsNullOrEmpty(clipKey) || voiceSource == null) return;
            if (!interruptCurrent && voiceSource.isPlaying) return;

            if (string.Equals(_lastNarrationKey, clipKey, StringComparison.OrdinalIgnoreCase) &&
                (Time.realtimeSinceStartup - _lastNarrationRealtime) < minRepeatCooldown)
            {
                return;
            }

            if (!_narrationCache.TryGetValue(clipKey, out AudioClip clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("Audio/Narration/" + clipKey);
                _narrationCache[clipKey] = clip;
            }

            if (clip == null) return;

            _lastNarrationKey = clipKey;
            _lastNarrationRealtime = Time.realtimeSinceStartup;

            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.volume = voiceVolume * masterVolume;
            voiceSource.Play();

            if (_voiceDuckCoroutine != null) StopCoroutine(_voiceDuckCoroutine);
            _voiceDuckCoroutine = StartCoroutine(DuckMusicWhileSpeakingRoutine(clip.length));
        }

        public void StopNarration()
        {
            if (_voiceDuckCoroutine != null)
            {
                StopCoroutine(_voiceDuckCoroutine);
                _voiceDuckCoroutine = null;
            }
            if (voiceSource != null && voiceSource.isPlaying)
            {
                voiceSource.Stop();
            }
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }

        private IEnumerator DuckMusicWhileSpeakingRoutine(float clipLength)
        {
            float deadline = Time.realtimeSinceStartup + clipLength + 0.15f;
            while (Time.realtimeSinceStartup < deadline && voiceSource != null && voiceSource.isPlaying)
            {
                if (musicSource != null && musicSource.isPlaying)
                {
                    musicSource.volume = Mathf.Min(musicSource.volume, musicVolume * masterVolume * 0.30f);
                }
                yield return null;
            }

            if (musicSource != null)
            {
                float startVol = musicSource.volume;
                float elapsed = 0f;
                while (elapsed < 0.45f)
                {
                    elapsed += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(startVol, musicVolume * masterVolume, elapsed / 0.45f);
                    yield return null;
                }
                musicSource.volume = musicVolume * masterVolume;
            }
            _voiceDuckCoroutine = null;
        }

        #endregion

        #region API Pública de Reproducción de Efectos

        public void PlayGazeHover()
        {
            PlaySound(gazeHoverClip, 0.45f);
        }

        public void PlayGazeClick()
        {
            PlaySound(gazeClickClip, 0.75f);
        }

        public void PlayEvidenceFound()
        {
            PlaySound(evidenceFoundClip, 0.95f);
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

        /// <summary>
        /// Reproduce la reacción de voz del NPC (04. What's That) cuando el jugador abre el enlace malicioso.
        /// </summary>
        public void PlayInfectionVoiceAlert(Vector3 position = default)
        {
            if (infectionVoiceAlertClip == null) return;
            if (position != default)
                PlaySoundAtPosition(infectionVoiceAlertClip, position, 1.0f, 0.75f);
            else
                PlaySound(infectionVoiceAlertClip, 1.0f);
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

        public void PlayPhoneRing(Vector3 position = default)
        {
            if (position != default)
                PlaySoundAtPosition(phoneRingClip, position, 0.75f, 0.85f);
            else
                PlaySound(phoneRingClip, 0.75f);
        }

        public void PlayKeyboardTyping(Vector3 position = default)
        {
            if (position != default)
                PlaySoundAtPosition(keyboardTypingClip, position, 0.70f, 0.75f);
            else
                PlaySound(keyboardTypingClip, 0.70f);
        }

        public void PlaySwitchToggle(Vector3 position = default)
        {
            if (position != default)
                PlaySoundAtPosition(switchToggleClip, position, 0.90f, 0.75f);
            else
                PlaySound(switchToggleClip, 0.90f);
        }

        public void PlayLaserScan(Vector3 position = default)
        {
            if (position != default)
                PlaySoundAtPosition(laserScanClip, position, 0.85f, 0.70f);
            else
                PlaySound(laserScanClip, 0.85f);
        }

        public void PlayTeleportWhoosh(Vector3 position = default)
        {
            if (position != default)
                PlaySoundAtPosition(teleportWhooshClip, position, 0.80f, 0.50f);
            else
                PlaySound(teleportWhooshClip, 0.80f);
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
            if (GameManager.I != null && GameManager.I.IsScenarioEnded) return;

            int defCount = GameManager.I != null ? GameManager.I.AppliedDefenses.Count : 0;
            if (defCount >= 3)
            {
                PlayNarration("Voice_17_TodasDefensasActivas", true, 4.0f);
            }
            else
            {
                bool isQuarantineVoiceActive = voiceSource != null && voiceSource.isPlaying &&
                    string.Equals(_lastNarrationKey, "Voice_12_CuarentenaSandbox", StringComparison.OrdinalIgnoreCase);
                if (!isQuarantineVoiceActive)
                {
                    PlayNarration("Voice_16_DefensaActivada", true, 2.5f);
                }
            }
        }

        private void HandlePhishingSignalDiscovered(string signalId)
        {
            PlayPhishingSignal();
            int count = GameManager.I != null ? GameManager.I.DiscoveredPhishingSignals.Count : 0;
            PlayNarration(count >= 3 ? "Voice_09_TresSenalesCompletas" : "Voice_08_SenalDetectada", true, 1.5f);
        }

        private void HandleModeChanged(SimMode newMode)
        {
            Debug.Log($"{LogPrefix} Modo cambiado a: <color=#9CDCFE>{newMode}</color>. Conmutando banda sonora...");
            if (newMode == SimMode.Ataque)
            {
                PlayMusicTrack(MusicTheme.AttackTension, 1.2f);
            }
            else if (newMode == SimMode.Defensa)
            {
                PlayMusicTrack(MusicTheme.DefenseVictory, 1.2f);
            }
        }

        #endregion

        #region Generadores Procedurales de Ondas Matemáticas (Fallback)

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
                float hum60 = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.035f;
                float hum120 = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.020f;
                samples[i] = Mathf.Clamp(pink + hum60 + hum120, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Procedural_AmbientOffice", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateGazeHoverClip()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1f - (t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Procedural_GazeHover", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateGazeClickClip()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = (t < 0.06f) ? 1200f : 1600f;
                float subT = (t < 0.06f) ? t : (t - 0.06f);
                float env = 1f - (subT / 0.06f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.50f;
            }

            AudioClip clip = AudioClip.Create("Procedural_GazeClick", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateEvidenceFoundClip()
        {
            int sampleRate = 44100;
            float duration = 0.40f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5, E5, G5, C6
            float noteDuration = duration / notes.Length;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Min(notes.Length - 1, (int)(t / noteDuration));
                float subT = t % noteDuration;
                float env = Mathf.Exp(-subT * 8f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t) * env * 0.60f;
            }

            AudioClip clip = AudioClip.Create("Procedural_EvidenceFound", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GeneratePhishingSignalClip()
        {
            int sampleRate = 44100;
            float duration = 0.28f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 9f);
                float f1 = Mathf.Sin(2f * Mathf.PI * 620f * t);
                float f2 = Mathf.Sin(2f * Mathf.PI * 480f * t);
                samples[i] = (f1 + f2) * 0.5f * env * 0.65f;
            }

            AudioClip clip = AudioClip.Create("Procedural_PhishingSignal", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateInfectionAlarmClip()
        {
            int sampleRate = 44100;
            float duration = 0.60f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = (t < 0.30f) ? 440f : 330f;
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.3f
                           + Mathf.Sin(2f * Mathf.PI * freq * t) * 0.4f;
                samples[i] = wave * 0.55f;
            }

            AudioClip clip = AudioClip.Create("Procedural_InfectionAlarm", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateQuarantineSuccessClip()
        {
            int sampleRate = 44100;
            float duration = 0.65f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];
            float[] notes = { 440f, 554.37f, 659.25f, 880f }; // A4, C#5, E5, A5
            float noteDuration = duration / notes.Length;

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Min(notes.Length - 1, (int)(t / noteDuration));
                float subT = t % noteDuration;
                float env = Mathf.Exp(-subT * 6f);
                float tone = Mathf.Sin(2f * Mathf.PI * notes[noteIndex] * t) * 0.6f
                           + Mathf.Sin(2f * Mathf.PI * (notes[noteIndex] * 2f) * t) * 0.2f;
                samples[i] = tone * env * 0.70f;
            }

            AudioClip clip = AudioClip.Create("Procedural_QuarantineSuccess", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralPhoneRing()
        {
            int sampleRate = 44100;
            float duration = 2.0f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                if (t < 0.8f)
                {
                    float ringMod = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 20f * t);
                    samples[i] = (Mathf.Sin(2f * Mathf.PI * 440f * t) + Mathf.Sin(2f * Mathf.PI * 480f * t)) * 0.35f * ringMod;
                }
                else samples[i] = 0f;
            }

            AudioClip clip = AudioClip.Create("Procedural_PhoneRing", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralKeyboardTyping()
        {
            int sampleRate = 44100;
            float duration = 0.5f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];
            System.Random rand = new System.Random(42);

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float val = 0f;
                float[] times = { 0.05f, 0.15f, 0.28f, 0.40f };
                foreach (var st in times)
                {
                    float dt = t - st;
                    if (dt >= 0f && dt < 0.05f)
                    {
                        val += Mathf.Sin(2f * Mathf.PI * 2600f * dt) * Mathf.Exp(-dt * 120f) * 0.5f;
                    }
                }
                samples[i] = Mathf.Clamp(val, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Procedural_Keyboard", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralSwitchToggle()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float snap = Mathf.Sin(2f * Mathf.PI * 2200f * t) * Mathf.Exp(-t * 90f) * 0.6f;
                float body = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 40f) * 0.4f;
                samples[i] = Mathf.Clamp(snap + body, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Procedural_SwitchToggle", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralLaserScan()
        {
            int sampleRate = 44100;
            float duration = 0.8f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 400f + 1200f * Mathf.Sin(Mathf.PI * (t / duration));
                float env = Mathf.Sin(Mathf.PI * (t / duration));
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.45f;
            }

            AudioClip clip = AudioClip.Create("Procedural_LaserScan", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralTeleportWhoosh()
        {
            int sampleRate = 44100;
            float duration = 0.45f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];
            System.Random rand = new System.Random(99);

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(Mathf.PI * (t / duration));
                float sub = Mathf.Sin(2f * Mathf.PI * (120f - 60f * (t / duration)) * t) * 0.5f;
                float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.2f;
                samples[i] = (sub + noise) * env * 0.6f;
            }

            AudioClip clip = AudioClip.Create("Procedural_TeleportWhoosh", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralMenuMusic()
        {
            int sampleRate = 44100;
            float duration = 6.0f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float drone = Mathf.Sin(2f * Mathf.PI * 73.42f * t) * 0.25f;
                float pad = Mathf.Sin(2f * Mathf.PI * 293.66f * t) * 0.08f + Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.05f;
                float fade = (t < 0.5f) ? (t / 0.5f) : (t > duration - 0.5f ? (duration - t) / 0.5f : 1f);
                samples[i] = (drone + pad) * fade;
            }

            AudioClip clip = AudioClip.Create("Procedural_MenuMusic", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralInvestigationMusic()
        {
            int sampleRate = 44100;
            float duration = 8.0f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float bass = Mathf.Sin(2f * Mathf.PI * 73.42f * t) * 0.28f;
                float arp = Mathf.Sin(2f * Mathf.PI * 587.33f * t) * 0.10f * Mathf.Exp(-(t % 0.25f) * 15f);
                float fade = (t < 0.5f) ? (t / 0.5f) : (t > duration - 0.5f ? (duration - t) / 0.5f : 1f);
                samples[i] = (bass + arp) * fade;
            }

            AudioClip clip = AudioClip.Create("Procedural_InvestigationMusic", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralAttackMusic()
        {
            int sampleRate = 44100;
            float duration = 4.0f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float bass = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 87.31f * t)) * 0.22f;
                float pulse = Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.10f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 4f * t));
                float fade = (t < 0.4f) ? (t / 0.4f) : (t > duration - 0.4f ? (duration - t) / 0.4f : 1f);
                samples[i] = (bass + pulse) * fade;
            }

            AudioClip clip = AudioClip.Create("Procedural_AttackMusic", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralDefenseMusic()
        {
            int sampleRate = 44100;
            float duration = 5.0f;
            int numSamples = (int)(sampleRate * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / sampleRate;
                float brass = Mathf.Sin(2f * Mathf.PI * 261.63f * t) * 0.18f + Mathf.Sin(2f * Mathf.PI * 392f * t) * 0.12f;
                float chime = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.08f * Mathf.Exp(-(t % 0.33f) * 10f);
                float fade = (t < 0.5f) ? (t / 0.5f) : (t > duration - 0.5f ? (duration - t) / 0.5f : 1f);
                samples[i] = (brass + chime) * fade;
            }

            AudioClip clip = AudioClip.Create("Procedural_DefenseMusic", numSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
