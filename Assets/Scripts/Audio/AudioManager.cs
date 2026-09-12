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
        [SerializeField] private float defaultMusicVolume = 0.5f;
        [SerializeField] private float defaultSfxVolume = 1f;

        private int currentSfxIndex = 0;
        private Coroutine warningRoutine;

        public bool IsSfxMuted { get; private set; }
        public bool IsMusicMuted { get; private set; }
        public int WarningPulseCount { get; private set; }

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
                float vol = volume >= 0f ? volume : defaultSfxVolume;
                targetSource.PlayOneShot(clip, vol);
            }
        }

        public void PlayWarningAlarm(AudioClip clip, int pulses = 4, float interval = 0.35f)
        {
            if (clip == null) return;
            if (warningSource == null) InitializeChannels();

            if (warningRoutine != null)
            {
                StopCoroutine(warningRoutine);
            }

            warningRoutine = StartCoroutine(WarningRoutine(clip, pulses, interval));
        }

        private IEnumerator WarningRoutine(AudioClip clip, int pulses, float interval)
        {
            for (int i = 0; i < pulses; i++)
            {
                if (!IsSfxMuted && warningSource != null)
                {
                    warningSource.PlayOneShot(clip);
                }
                WarningPulseCount++;
                yield return new WaitForSeconds(interval);
            }
            warningRoutine = null;
        }

        public void SimulatePulseSequence(int count)
        {
            WarningPulseCount += count;
        }

        public static void ResetInstanceForTesting(AudioManager instance = null)
        {
            Instance = instance;
            OnSfxMuteChanged = null;
            OnMusicMuteChanged = null;
        }
    }
}
