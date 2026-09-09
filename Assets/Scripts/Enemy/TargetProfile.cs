using System;
using UnityEngine;

namespace KinematicsGame.Enemy
{
    /// <summary>
    /// Encapsulates visual, kinematic, scoring, and spawn weighting parameters for bird archetypes.
    /// </summary>
    [Serializable]
    public class TargetProfile
    {
        [SerializeField] private string profileName = "Common Pigeon";
        [SerializeField] private Sprite sprite;
        [SerializeField] private float speedMultiplier = 1.0f;
        [SerializeField] private float waveFrequencyMultiplier = 1.0f;
        [SerializeField] private float waveAmplitudeMultiplier = 1.0f;
        [SerializeField] private int pointValue = 10;
        [SerializeField] private float spawnWeight = 50f;

        public string ProfileName
        {
            get => profileName;
            set => profileName = value;
        }

        public Sprite Sprite
        {
            get => sprite;
            set => sprite = value;
        }

        public float SpeedMultiplier
        {
            get => speedMultiplier;
            set => speedMultiplier = value;
        }

        public float WaveFrequencyMultiplier
        {
            get => waveFrequencyMultiplier;
            set => waveFrequencyMultiplier = value;
        }

        public float WaveAmplitudeMultiplier
        {
            get => waveAmplitudeMultiplier;
            set => waveAmplitudeMultiplier = value;
        }

        public int PointValue
        {
            get => pointValue;
            set => pointValue = value;
        }

        public float SpawnWeight
        {
            get => spawnWeight;
            set => spawnWeight = value;
        }

        public TargetProfile()
        {
        }

        public TargetProfile(
            string name,
            Sprite sprite,
            float speedMult,
            float freqMult,
            float ampMult,
            int points,
            float weight)
        {
            profileName = name;
            this.sprite = sprite;
            speedMultiplier = speedMult;
            waveFrequencyMultiplier = freqMult;
            waveAmplitudeMultiplier = ampMult;
            pointValue = points;
            spawnWeight = weight;
        }

        /// <summary>
        /// Creates default archetype presets using provided sprites (or null fallbacks),
        /// excluding bird3 (Sweeper Albatross).
        /// </summary>
        public static TargetProfile[] CreateDefaultPresets(Sprite[] birdSprites = null)
        {
            Sprite s1 = birdSprites != null && birdSprites.Length > 0 ? birdSprites[0] : null;
            Sprite s2 = birdSprites != null && birdSprites.Length > 1 ? birdSprites[1] : null;
            // Support passing either [bird1, bird2, bird4] (length 3) or legacy [bird1, bird2, bird3, bird4] (length 4)
            Sprite sRare = null;
            if (birdSprites != null)
            {
                if (birdSprites.Length >= 4) sRare = birdSprites[3];
                else if (birdSprites.Length >= 3) sRare = birdSprites[2];
            }

            return new TargetProfile[]
            {
                new TargetProfile("Common Pigeon", s1, 1.0f, 1.0f, 1.0f, 10, 60f),
                new TargetProfile("Agile Hummingbird", s2, 1.5f, 2.5f, 0.8f, 30, 25f),
                new TargetProfile("Rare Golden Eagle", sRare, 2.2f, 0.0f, 0.0f, 100, 15f)
            };
        }
    }
}
