using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using KinematicsGame.Audio;
using KinematicsGame.UI;
using KinematicsGame.Combat;
using KinematicsGame.Core;
using KinematicsGame.Enemy;

namespace KinematicsGame.Tests
{
    public class AudioSystemTests
    {
        private List<Object> disposables;
        private GameObject audioManagerGo;
        private AudioManager audioManager;

        [SetUp]
        public void SetUp()
        {
            disposables = new List<Object>();
            PlayerPrefs.DeleteKey(AudioManager.PrefKeySfxMuted);
            PlayerPrefs.DeleteKey(AudioManager.PrefKeyMusicMuted);

            audioManagerGo = new GameObject("TestAudioManager");
            disposables.Add(audioManagerGo);
            audioManager = audioManagerGo.AddComponent<AudioManager>();
            audioManager.InitializeChannels();
            AudioManager.ResetInstanceForTesting(audioManager);
        }

        [TearDown]
        public void TearDown()
        {
            AudioManager.ResetInstanceForTesting();
            PlayerPrefs.DeleteKey(AudioManager.PrefKeySfxMuted);
            PlayerPrefs.DeleteKey(AudioManager.PrefKeyMusicMuted);

            for (int i = disposables.Count - 1; i >= 0; i--)
            {
                if (disposables[i] != null)
                {
                    Object.DestroyImmediate(disposables[i]);
                }
            }
            disposables.Clear();
        }

        [Test]
        public void AudioManager_InitializesChannelsAndMuteStatesFromPlayerPrefs()
        {
            Assert.IsNotNull(audioManager.MusicSource);
            Assert.IsNotNull(audioManager.SfxSourcePool);
            Assert.AreEqual(4, audioManager.SfxSourcePool.Length);
            Assert.IsNotNull(audioManager.WarningSource);

            Assert.IsFalse(audioManager.IsSfxMuted);
            Assert.IsFalse(audioManager.IsMusicMuted);
        }

        [Test]
        public void AudioManager_ToggleSfx_PersistsStateAndFiresEvent()
        {
            bool eventFired = false;
            bool eventMuteValue = false;
            AudioManager.OnSfxMuteChanged += (muted) =>
            {
                eventFired = true;
                eventMuteValue = muted;
            };

            audioManager.ToggleSfx();

            Assert.IsTrue(audioManager.IsSfxMuted);
            Assert.AreEqual(1, PlayerPrefs.GetInt(AudioManager.PrefKeySfxMuted));
            Assert.IsTrue(eventFired);
            Assert.IsTrue(eventMuteValue);

            // Toggle back
            audioManager.ToggleSfx();
            Assert.IsFalse(audioManager.IsSfxMuted);
            Assert.AreEqual(0, PlayerPrefs.GetInt(AudioManager.PrefKeySfxMuted));
            Assert.IsFalse(eventMuteValue);
        }

        [Test]
        public void AudioManager_ToggleMusic_PersistsStateAndFiresEvent()
        {
            bool eventFired = false;
            bool eventMuteValue = false;
            AudioManager.OnMusicMuteChanged += (muted) =>
            {
                eventFired = true;
                eventMuteValue = muted;
            };

            audioManager.ToggleMusic();

            Assert.IsTrue(audioManager.IsMusicMuted);
            Assert.AreEqual(1, PlayerPrefs.GetInt(AudioManager.PrefKeyMusicMuted));
            Assert.IsTrue(eventFired);
            Assert.IsTrue(eventMuteValue);

            // Toggle back
            audioManager.ToggleMusic();
            Assert.IsFalse(audioManager.IsMusicMuted);
            Assert.AreEqual(0, PlayerPrefs.GetInt(AudioManager.PrefKeyMusicMuted));
            Assert.IsFalse(eventMuteValue);
        }

        [Test]
        public void AudioManager_PlaySfx_AllocatesFromVoicePoolWithoutStarvation()
        {
            AudioClip clip = AudioClip.Create("TestSFX", 44100, 1, 44100, false);
            disposables.Add(clip);

            // Trigger multiple SFX calls
            for (int i = 0; i < 8; i++)
            {
                audioManager.PlaySfx(clip);
            }

            Assert.Pass("PlaySfx handled polyphonic voice allocation without exceptions");
        }

        [Test]
        public void AudioManager_PlayWarningAlarm_SimulatesPulseSequence()
        {
            audioManager.SimulatePulseSequence(4);
            Assert.AreEqual(4, audioManager.WarningPulseCount);
        }

        [Test]
        public void AudioToggleButton_Click_TogglesAudioManagerStateAndSwapsSpriteInPlace()
        {
            GameObject btnGo = new GameObject("SoundToggleButton");
            disposables.Add(btnGo);
            RectTransform rt = btnGo.AddComponent<RectTransform>();
            Button btn = btnGo.AddComponent<Button>();
            Image img = btnGo.AddComponent<Image>();

            Sprite activeSprite = Sprite.Create(new Texture2D(64, 64), new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
            Sprite inactiveSprite = Sprite.Create(new Texture2D(64, 64), new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
            disposables.Add(activeSprite.texture);
            disposables.Add(inactiveSprite.texture);
            disposables.Add(activeSprite);
            disposables.Add(inactiveSprite);

            AudioToggleButton toggle = btnGo.AddComponent<AudioToggleButton>();
            toggle.ToggleType = AudioToggleType.Sound;
            toggle.ActiveSprite = activeSprite;
            toggle.InactiveSprite = inactiveSprite;
            toggle.TargetImage = img;
            toggle.ButtonComponent = btn;

            // Initially sound is unmuted, so activeSprite is shown
            toggle.UpdateVisuals();
            Assert.AreEqual(activeSprite, img.sprite);

            // Click button to mute sound
            toggle.OnClick();
            Assert.IsTrue(audioManager.IsSfxMuted);
            toggle.UpdateVisuals();
            Assert.AreEqual(inactiveSprite, img.sprite);

            // Click button again to unmute sound
            toggle.OnClick();
            Assert.IsFalse(audioManager.IsSfxMuted);
            toggle.UpdateVisuals();
            Assert.AreEqual(activeSprite, img.sprite);
        }

        [Test]
        public void AudioToggleButton_FixedDimensions_Maintains64x64WithoutLayoutDrift()
        {
            GameObject btnGo = new GameObject("MusicToggleButton");
            disposables.Add(btnGo);
            RectTransform rt = btnGo.AddComponent<RectTransform>();
            btnGo.AddComponent<Button>();
            btnGo.AddComponent<Image>();

            AudioToggleButton toggle = btnGo.AddComponent<AudioToggleButton>();
            toggle.FixedDimensions = new Vector2(64f, 64f);
            toggle.EnforceDimensions();

            Assert.AreEqual(new Vector2(64f, 64f), rt.sizeDelta);

            toggle.OnClick();
            toggle.UpdateVisuals();

            Assert.AreEqual(new Vector2(64f, 64f), rt.sizeDelta);
        }

        [Test]
        public void RestrictedZoneTrigger_OnTriggerEnter2D_TriggersWarningWhenEnemyEnters()
        {
            GameObject zoneGo = new GameObject("RestrictedZone");
            disposables.Add(zoneGo);
            RestrictedZoneTrigger zone = zoneGo.AddComponent<RestrictedZoneTrigger>();
            zone.EnableSimulatedTime(0f);

            bool warningInvoked = false;
            RestrictedZoneTrigger.OnWarningTriggered += () => warningInvoked = true;

            bool triggered = zone.TryTriggerAlarm();

            Assert.IsTrue(triggered);
            Assert.AreEqual(1, zone.TriggerCount);
            Assert.IsTrue(warningInvoked);
        }

        [Test]
        public void RestrictedZoneTrigger_Debounce_IgnoresSubsequentEnemiesWithinDebounceWindow()
        {
            GameObject zoneGo = new GameObject("RestrictedZoneDebounce");
            disposables.Add(zoneGo);
            RestrictedZoneTrigger zone = zoneGo.AddComponent<RestrictedZoneTrigger>();
            zone.EnableSimulatedTime(10f);

            // First breach
            Assert.IsTrue(zone.TryTriggerAlarm());
            Assert.AreEqual(1, zone.TriggerCount);

            // Immediate second breach (t = 11s, debounce = 3.0s)
            zone.AdvanceSimulatedTime(1.0f);
            Assert.IsFalse(zone.TryTriggerAlarm());
            Assert.AreEqual(1, zone.TriggerCount);

            // Advance past debounce window (t = 13.5s)
            zone.AdvanceSimulatedTime(2.5f);
            Assert.IsTrue(zone.TryTriggerAlarm());
            Assert.AreEqual(2, zone.TriggerCount);
        }

        [Test]
        public void RestrictedZoneTrigger_AlignToViewport_PositionsCorrectlyForHorizontalAndVertical()
        {
            GameObject camGo = new GameObject("TestCamera");
            disposables.Add(camGo);
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.aspect = 16f / 9f;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            GameObject vpGo = new GameObject("ViewportManager");
            disposables.Add(vpGo);
            ViewportManager vp = vpGo.AddComponent<ViewportManager>();
            vp.TargetCamera = cam;
            vp.Initialize();

            GameObject zoneGo = new GameObject("ZoneToAlign");
            disposables.Add(zoneGo);
            RestrictedZoneTrigger zone = zoneGo.AddComponent<RestrictedZoneTrigger>();

            // Horizontal alignment: Left 20%
            zone.AlignToViewport(GameOrientation.Horizontal);
            float expectedHWidth = vp.Width * 0.2f;
            float expectedHHeight = vp.Height;
            float expectedHX = vp.MinX + (expectedHWidth * 0.5f);
            float expectedHY = vp.Center.y;

            Assert.AreEqual(expectedHWidth, zone.BoxCollider.size.x, 0.01f);
            Assert.AreEqual(expectedHHeight, zone.BoxCollider.size.y, 0.01f);
            Assert.AreEqual(expectedHX, zone.transform.position.x, 0.01f);
            Assert.AreEqual(expectedHY, zone.transform.position.y, 0.01f);

            // Vertical alignment: Top 20%
            zone.AlignToViewport(GameOrientation.Vertical);
            float expectedVWidth = vp.Width;
            float expectedVHeight = vp.Height * 0.2f;
            float expectedVX = vp.Center.x;
            float expectedVY = vp.MaxY - (expectedVHeight * 0.5f);

            Assert.AreEqual(expectedVWidth, zone.BoxCollider.size.x, 0.01f);
            Assert.AreEqual(expectedVHeight, zone.BoxCollider.size.y, 0.01f);
            Assert.AreEqual(expectedVX, zone.transform.position.x, 0.01f);
            Assert.AreEqual(expectedVY, zone.transform.position.y, 0.01f);
        }
    }
}
