using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KinematicsGame.Enemy;

namespace KinematicsGame.Tests
{
    public class TargetProfileTests
    {
        private List<Object> disposables;

        [SetUp]
        public void SetUp()
        {
            disposables = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = disposables.Count - 1; i >= 0; i--)
            {
                if (disposables[i] != null)
                {
                    Object.DestroyImmediate(disposables[i]);
                }
            }
            disposables.Clear();
        }

        private Sprite CreateDummySprite(string name)
        {
            Texture2D tex = new Texture2D(32, 32);
            tex.name = name;
            disposables.Add(tex);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            disposables.Add(sprite);
            return sprite;
        }

        [Test]
        public void TargetProfile_CreateDefaultPresets_ReturnsThreeDistinctArchetypes_ExcludingBird3()
        {
            Sprite[] sprites = new Sprite[]
            {
                CreateDummySprite("Bird1"),
                CreateDummySprite("Bird2"),
                CreateDummySprite("Bird4")
            };

            TargetProfile[] presets = TargetProfile.CreateDefaultPresets(sprites);

            Assert.That(presets, Is.Not.Null);
            Assert.That(presets.Length, Is.EqualTo(3));

            // Bird 1: Pigeon (Common)
            Assert.That(presets[0].ProfileName, Is.EqualTo("Common Pigeon"));
            Assert.That(presets[0].Sprite, Is.EqualTo(sprites[0]));
            Assert.That(presets[0].SpeedMultiplier, Is.EqualTo(1.0f));
            Assert.That(presets[0].WaveFrequencyMultiplier, Is.EqualTo(1.0f));
            Assert.That(presets[0].WaveAmplitudeMultiplier, Is.EqualTo(1.0f));
            Assert.That(presets[0].PointValue, Is.EqualTo(10));
            Assert.That(presets[0].SpawnWeight, Is.EqualTo(60f));

            // Bird 2: Hummingbird (Agile)
            Assert.That(presets[1].ProfileName, Is.EqualTo("Agile Hummingbird"));
            Assert.That(presets[1].Sprite, Is.EqualTo(sprites[1]));
            Assert.That(presets[1].SpeedMultiplier, Is.EqualTo(1.5f));
            Assert.That(presets[1].WaveFrequencyMultiplier, Is.EqualTo(2.5f));
            Assert.That(presets[1].WaveAmplitudeMultiplier, Is.EqualTo(0.8f));
            Assert.That(presets[1].PointValue, Is.EqualTo(30));
            Assert.That(presets[1].SpawnWeight, Is.EqualTo(25f));

            // Bird 4: Golden Eagle (Rare)
            Assert.That(presets[2].ProfileName, Is.EqualTo("Rare Golden Eagle"));
            Assert.That(presets[2].Sprite, Is.EqualTo(sprites[2]));
            Assert.That(presets[2].SpeedMultiplier, Is.EqualTo(2.2f));
            Assert.That(presets[2].WaveFrequencyMultiplier, Is.EqualTo(0.0f));
            Assert.That(presets[2].WaveAmplitudeMultiplier, Is.EqualTo(0.0f));
            Assert.That(presets[2].PointValue, Is.EqualTo(100));
            Assert.That(presets[2].SpawnWeight, Is.EqualTo(15f));
        }

        [Test]
        public void TargetProfile_CustomValues_RetainsAssignedProperties()
        {
            Sprite sprite = CreateDummySprite("CustomBird");
            TargetProfile profile = new TargetProfile("TestBird", sprite, 3.5f, 1.2f, 0.5f, 50, 12f);

            Assert.That(profile.ProfileName, Is.EqualTo("TestBird"));
            Assert.That(profile.Sprite, Is.EqualTo(sprite));
            Assert.That(profile.SpeedMultiplier, Is.EqualTo(3.5f));
            Assert.That(profile.WaveFrequencyMultiplier, Is.EqualTo(1.2f));
            Assert.That(profile.WaveAmplitudeMultiplier, Is.EqualTo(0.5f));
            Assert.That(profile.PointValue, Is.EqualTo(50));
            Assert.That(profile.SpawnWeight, Is.EqualTo(12f));
        }

        [Test]
        public void TargetProfile_WeightedSelectionMath_ProducesValidProbabilities()
        {
            TargetProfile[] presets = TargetProfile.CreateDefaultPresets(null);

            float totalWeight = 0f;
            foreach (var p in presets)
            {
                totalWeight += p.SpawnWeight;
            }

            Assert.That(totalWeight, Is.EqualTo(100f).Within(0.001f));

            // Validate cumulative weight partition covers entire range [0, 100]
            float roll = 65f; // Should map to Agile Hummingbird (50 to 75)
            TargetProfile selected = null;
            float currentSum = 0f;
            foreach (var p in presets)
            {
                currentSum += p.SpawnWeight;
                if (roll <= currentSum)
                {
                    selected = p;
                    break;
                }
            }

            Assert.That(selected, Is.Not.Null);
            Assert.That(selected.ProfileName, Is.EqualTo("Agile Hummingbird"));
        }
    }
}
