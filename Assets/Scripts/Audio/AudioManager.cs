using System;
using System.Collections;
using UnityEngine;

namespace KinematicsGame.Audio
{
    /// <summary>
    /// Isolated 3-channel audio manager for music (BGM), polyphonic SFX (4-voice pool),
    /// and dedicated warning siren. Handles PlayerPrefs persistence and mute events.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public const string PrefKeySfxMuted = "Audio_SFX_Muted";
        public const string PrefKeyMusicMuted = "Audio_Music_Muted";

        public static AudioManager Instance { get; private set; }

        public static event Action<bool> OnSfxMuteChanged;
        public static event Action<bool> OnMusicMuteChanged;

        [Header("Audio Channels")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource[] sfxSourcePool = new AudioSource[4];
        [SerializeField] private AudioSource warningSource;

        [Header("Settings")]
        [SerializeField] private int sfxPoolSize = 4;
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.25f;
        [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 0.25f;

        [Header("Warning Cooldown")]
        [SerializeField] private float warningCooldown = 2.0f;
        private float lastWarningEndTime = -100f;

        private int currentSfxIndex = 0;
        private Coroutine warningRoutine;

        public bool IsSfxMuted { get; private set; }
        public bool IsMusicMuted { get; private set; }
        public int WarningPulseCount { get; private set; }

        public bool IsWarningPlaying => warningRoutine != null || (warningSource != null && warningSource.isPlaying);
        public bool IsWarningOnCooldown => Time.time < lastWarningEndTime + warningCooldown;
        public float WarningCooldown
        {
            get => warningCooldown;
            set => warningCooldown = Mathf.Max(0f, value);
        }

        public float SfxVolume
        {
            get => defaultSfxVolume;
            set => defaultSfxVolume = Mathf.Clamp01(value);
        }

        public float MusicVolume
        {
            get => defaultMusicVolume;
            set
            {
                defaultMusicVolume = Mathf.Clamp01(value);
                if (musicSource != null)
                {
                    musicSource.volume = defaultMusicVolume;
                }
            }
        }

        public AudioSource MusicSource => musicSource;
        public AudioSource[] SfxSourcePool => sfxSourcePool;
        public AudioSource WarningSource => warningSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeChannels();
            LoadPreferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void InitializeChannels()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicChannel");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            if (sfxSourcePool == null || sfxSourcePool.Length != sfxPoolSize)
            {
                sfxSourcePool = new AudioSource[sfxPoolSize];
            }

            for (int i = 0; i < sfxSourcePool.Length; i++)
            {
                if (sfxSourcePool[i] == null)
                {
                    GameObject sfxObj = new GameObject($"SFXChannel_{i}");
                    sfxObj.transform.SetParent(transform);
                    sfxSourcePool[i] = sfxObj.AddComponent<AudioSource>();
                    sfxSourcePool[i].playOnAwake = false;
                }
            }

            if (warningSource == null)
            {
                GameObject warningObj = new GameObject("WarningChannel");
                warningObj.transform.SetParent(transform);
                warningSource = warningObj.AddComponent<AudioSource>();
                warningSource.playOnAwake = false;
            }
        }

        public void LoadPreferences()
        {
            IsSfxMuted = PlayerPrefs.GetInt(PrefKeySfxMuted, 0) == 1;
            IsMusicMuted = PlayerPrefs.GetInt(PrefKeyMusicMuted, 0) == 1;

            ApplyMuteStates();
        }

        public void ToggleSfx()
        {
            SetSfxMuted(!IsSfxMuted);
        }

        public void SetSfxMuted(bool muted)
        {
            IsSfxMuted = muted;
            PlayerPrefs.SetInt(PrefKeySfxMuted, IsSfxMuted ? 1 : 0);
            PlayerPrefs.Save();

            ApplyMuteStates();
            OnSfxMuteChanged?.Invoke(IsSfxMuted);
        }

        public void ToggleMusic()
        {
            SetMusicMuted(!IsMusicMuted);
        }

        public void SetMusicMuted(bool muted)
        {
            IsMusicMuted = muted;
            PlayerPrefs.SetInt(PrefKeyMusicMuted, IsMusicMuted ? 1 : 0);
            PlayerPrefs.Save();

            ApplyMuteStates();
            OnMusicMuteChanged?.Invoke(IsMusicMuted);
        }

        private void ApplyMuteStates()
        {
            if (musicSource != null)
            {
                musicSource.mute = IsMusicMuted;
            }

            if (sfxSourcePool != null)
            {
                foreach (AudioSource sfx in sfxSourcePool)
                {
                    if (sfx != null)
                    {
                        sfx.mute = IsSfxMuted;
                    }
                }
            }

            if (warningSource != null)
            {
                warningSource.mute = IsSfxMuted;
            }
        }

        public void PlayMusic(AudioClip clip, float volume = -1f, bool loop = true)
        {
            if (clip == null) return;
            if (musicSource == null) InitializeChannels();

            musicSource.clip = clip;
            musicSource.volume = volume >= 0f ? volume : defaultMusicVolume;
            musicSource.loop = loop;
            musicSource.mute = IsMusicMuted;

            if (!IsMusicMuted && !musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public void StopMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }
        }

        public void PlaySfx(AudioClip clip, float volume = -1f)
        {
            if (clip == null || IsSfxMuted) return;
            if (sfxSourcePool == null || sfxSourcePool.Length == 0) InitializeChannels();

            AudioSource targetSource = null;
            for (int i = 0; i < sfxSourcePool.Length; i++)
            {
                if (sfxSourcePool[i] != null && !sfxSourcePool[i].isPlaying)
                {
                    targetSource = sfxSourcePool[i];
                    break;
                }
            }

            if (targetSource == null)
            {
                targetSource = sfxSourcePool[currentSfxIndex];
                currentSfxIndex = (currentSfxIndex + 1) % sfxSourcePool.Length;
            }

            if (targetSource != null)
            {
                float clipVol = volume >= 0f ? volume : 1f;
                float vol = Mathf.Clamp01(clipVol * defaultSfxVolume);
                targetSource.PlayOneShot(clip, vol);
            }
        }

        public bool PlayWarningAlarm(AudioClip clip, int pulses = 4, float interval = 0.35f, bool forceRestart = false)
        {
            if (clip == null) return false;
            if (warningSource == null) InitializeChannels();

            // Anti-spam guard: do not interrupt currently playing alarm unless explicitly forced
            if (!forceRestart && IsWarningPlaying)
            {
                return false;
            }

            // Cooldown guard: do not restart alarm during cooldown window unless explicitly forced
            if (!forceRestart && IsWarningOnCooldown)
            {
                return false;
            }

            if (warningRoutine != null)
            {
                StopCoroutine(warningRoutine);
                warningRoutine = null;
            }

            warningSource.Stop();
            warningRoutine = StartCoroutine(WarningRoutine(clip, pulses, interval));
            return true;
        }

        public void StopWarningAlarm()
        {
            if (warningRoutine != null)
            {
                StopCoroutine(warningRoutine);
                warningRoutine = null;
            }

            if (warningSource != null)
            {
                warningSource.Stop();
            }

            lastWarningEndTime = Time.time;
        }

        private IEnumerator WarningRoutine(AudioClip clip, int pulses, float interval)
        {
            float pulseDelay = CalculateWarningPulseDelay(clip, interval);
            float clipDuration = clip != null ? clip.length : 0f;

            for (int i = 0; i < pulses; i++)
            {
                if (!IsSfxMuted && warningSource != null)
                {
                    warningSource.clip = clip;
                    warningSource.volume = defaultSfxVolume;
                    warningSource.Play();
                }
                WarningPulseCount++;

                if (i < pulses - 1)
                {
                    yield return new WaitForSeconds(pulseDelay);
                }
            }

            // Wait for final pulse clip to complete before resetting routine & starting cooldown
            if (clipDuration > 0f)
            {
                yield return new WaitForSeconds(clipDuration);
            }

            lastWarningEndTime = Time.time;
            warningRoutine = null;
        }

        public static float CalculateWarningPulseDelay(AudioClip clip, float interval)
        {
            float clipDuration = clip != null ? clip.length : 0f;
            return Mathf.Max(clipDuration, Mathf.Max(0f, interval));
        }

        public void SimulatePulseSequence(int count)
        {
            WarningPulseCount += count;
        }

        public void ResetWarningCooldown()
        {
            lastWarningEndTime = -100f;
        }

        public static void ResetInstanceForTesting(AudioManager instance = null)
        {
            Instance = instance;
            OnSfxMuteChanged = null;
            OnMusicMuteChanged = null;
        }
    }
}
