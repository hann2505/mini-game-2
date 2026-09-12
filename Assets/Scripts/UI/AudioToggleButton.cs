using UnityEngine;
using UnityEngine.UI;
using KinematicsGame.Audio;

namespace KinematicsGame.UI
{
    public enum AudioToggleType
    {
        Sound,
        Music
    }

    /// <summary>
    /// In-place sprite-swapping audio toggle button with strict fixed dimensions (64x64)
    /// to prevent layout drift and canvas rebuild jitter.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class AudioToggleButton : MonoBehaviour
    {
        [Header("Toggle Configuration")]
        [SerializeField] private AudioToggleType toggleType = AudioToggleType.Sound;
        [SerializeField] private Sprite activeSprite;    // Displayed when audio is playing (e.g. sound_off / music_TurnOff)
        [SerializeField] private Sprite inactiveSprite;  // Displayed when audio is muted (e.g. sound_on / music_TurnOn)
        [SerializeField] private Vector2 fixedDimensions = new Vector2(64f, 64f);

        [Header("Components")]
        [SerializeField] private Button button;
        [SerializeField] private Image targetImage;
        [SerializeField] private RectTransform rectTransform;

        public AudioToggleType ToggleType
        {
            get => toggleType;
            set
            {
                toggleType = value;
                UpdateVisuals();
            }
        }

        public Sprite ActiveSprite
        {
            get => activeSprite;
            set
            {
                activeSprite = value;
                UpdateVisuals();
            }
        }

        public Sprite InactiveSprite
        {
            get => inactiveSprite;
            set
            {
                inactiveSprite = value;
                UpdateVisuals();
            }
        }

        public Vector2 FixedDimensions
        {
            get => fixedDimensions;
            set
            {
                fixedDimensions = value;
                EnforceDimensions();
            }
        }

        public Image TargetImage
        {
            get => targetImage;
            set => targetImage = value;
        }

        public Button ButtonComponent
        {
            get => button;
            set => button = value;
        }

        private void Awake()
        {
            EnsureComponents();
            EnforceDimensions();
        }

        private void OnEnable()
        {
            EnsureComponents();
            button.onClick.AddListener(OnClick);

            AudioManager.OnSfxMuteChanged += HandleSfxMuteChanged;
            AudioManager.OnMusicMuteChanged += HandleMusicMuteChanged;

            UpdateVisuals();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }

            AudioManager.OnSfxMuteChanged -= HandleSfxMuteChanged;
            AudioManager.OnMusicMuteChanged -= HandleMusicMuteChanged;
        }

        public void EnsureComponents()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        public void EnforceDimensions()
        {
            EnsureComponents();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = fixedDimensions;
            }
        }

        public void OnClick()
        {
            AudioManager mgr = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();
            if (mgr == null) return;

            if (toggleType == AudioToggleType.Sound)
            {
                mgr.ToggleSfx();
            }
            else
            {
                mgr.ToggleMusic();
            }
        }

        public void TriggerClick()
        {
            OnClick();
        }

        private void HandleSfxMuteChanged(bool isMuted)
        {
            if (toggleType == AudioToggleType.Sound)
            {
                UpdateVisuals();
            }
        }

        private void HandleMusicMuteChanged(bool isMuted)
        {
            if (toggleType == AudioToggleType.Music)
            {
                UpdateVisuals();
            }
        }

        public void UpdateVisuals()
        {
            EnsureComponents();
            if (targetImage == null) return;

            bool isMuted = false;
            if (AudioManager.Instance != null)
            {
                isMuted = (toggleType == AudioToggleType.Sound)
                    ? AudioManager.Instance.IsSfxMuted
                    : AudioManager.Instance.IsMusicMuted;
            }

            // When muted (inactive audio), show inactiveSprite (prompts user to unmute)
            // When unmuted (active audio), show activeSprite (prompts user to mute)
            Sprite desiredSprite = isMuted ? inactiveSprite : activeSprite;
            if (desiredSprite != null)
            {
                targetImage.sprite = desiredSprite;
            }

            EnforceDimensions();
        }
    }
}
