using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KinematicsGame.Core;

namespace KinematicsGame.Tests
{
    public class ViewportManagerTests
    {
        private GameObject cameraGo;
        private Camera testCamera;
        private GameObject managerGo;
        private ViewportManager manager;
        private List<Object> disposables;

        [SetUp]
        public void SetUp()
        {
            disposables = new List<Object>();

            cameraGo = new GameObject("TestCamera");
            disposables.Add(cameraGo);
            testCamera = cameraGo.AddComponent<Camera>();
            testCamera.orthographic = true;
            testCamera.orthographicSize = 5f;
            testCamera.transform.position = new Vector3(0f, 0f, -10f);

            managerGo = new GameObject("TestViewportManager");
            disposables.Add(managerGo);
            manager = managerGo.AddComponent<ViewportManager>();
            manager.TargetCamera = testCamera;
            manager.Initialize();
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

        private SpriteRenderer CreateTestSprite(string name, int width, int height, float ppu = 100f)
        {
            GameObject go = new GameObject(name);
            disposables.Add(go);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(width, height);
            disposables.Add(tex);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);
            return sr;
        }

        [Test]
        public void ViewportManager_CalculatesCorrectCameraBounds()
        {
            Assert.That(manager.Height, Is.EqualTo(10f).Within(0.01f));
            Assert.That(manager.Center.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(manager.Center.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(manager.MaxY, Is.EqualTo(5f).Within(0.01f));
            Assert.That(manager.MinY, Is.EqualTo(-5f).Within(0.01f));
            Assert.That(manager.MaxX, Is.GreaterThan(0f));
            Assert.That(manager.MinX, Is.LessThan(0f));
        }

        [Test]
        public void GetViewportWorldPosition_Center_ReturnsOrigin()
        {
            Vector3 center = manager.GetViewportWorldPosition(0.5f, 0.5f);
            Assert.That(center.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(center.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(center.z, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void GetViewportWorldPosition_Corners_MatchBounds()
        {
            Vector3 bottomLeft = manager.GetViewportWorldPosition(0f, 0f);
            Assert.That(bottomLeft.x, Is.EqualTo(manager.MinX).Within(0.01f));
            Assert.That(bottomLeft.y, Is.EqualTo(manager.MinY).Within(0.01f));

            Vector3 topRight = manager.GetViewportWorldPosition(1f, 1f);
            Assert.That(topRight.x, Is.EqualTo(manager.MaxX).Within(0.01f));
            Assert.That(topRight.y, Is.EqualTo(manager.MaxY).Within(0.01f));
        }

        [TestCase(16f / 9f)]
        [TestCase(18f / 9f)]
        [TestCase(4f / 3f)]
        public void ViewportManager_SupportsArbitraryAspectRatios(float aspect)
        {
            testCamera.aspect = aspect;
            manager.UpdateBounds();

            float expectedHalfWidth = testCamera.orthographicSize * aspect;
            Assert.That(manager.MaxX, Is.EqualTo(expectedHalfWidth).Within(0.02f));
            Assert.That(manager.MinX, Is.EqualTo(-expectedHalfWidth).Within(0.02f));
            Assert.That(manager.Width, Is.EqualTo(expectedHalfWidth * 2f).Within(0.02f));

            Vector3 bl = manager.GetViewportWorldPosition(0f, 0f);
            Vector3 tr = manager.GetViewportWorldPosition(1f, 1f);
            Assert.That(bl.x, Is.EqualTo(manager.MinX).Within(0.01f));
            Assert.That(tr.x, Is.EqualTo(manager.MaxX).Within(0.01f));
        }

        [Test]
        public void ViewportManager_TracksCameraTranslation()
        {
            testCamera.transform.position = new Vector3(5f, -3f, -10f);
            manager.UpdateBounds();

            Assert.That(manager.Center.x, Is.EqualTo(5f).Within(0.01f));
            Assert.That(manager.Center.y, Is.EqualTo(-3f).Within(0.01f));
            Assert.That(manager.MaxY, Is.EqualTo(2f).Within(0.01f));
            Assert.That(manager.MinY, Is.EqualTo(-8f).Within(0.01f));
        }

        [Test]
        public void MatchObjectSize_YieldsIdenticalBoundsForDifferentNativeResolutions()
        {
            SpriteRenderer srLowRes = CreateTestSprite("LowRes", 64, 64);
            SpriteRenderer srHighRes = CreateTestSprite("HighRes", 256, 256);

            Vector2 desiredSize = new Vector2(2f, 2f);
            manager.MatchObjectSize(srLowRes, desiredSize);
            manager.MatchObjectSize(srHighRes, desiredSize);

            Assert.That(srLowRes.bounds.size.x, Is.EqualTo(2f).Within(0.01f));
            Assert.That(srLowRes.bounds.size.y, Is.EqualTo(2f).Within(0.01f));
            Assert.That(srHighRes.bounds.size.x, Is.EqualTo(2f).Within(0.01f));
            Assert.That(srHighRes.bounds.size.y, Is.EqualTo(2f).Within(0.01f));
            Assert.That(srLowRes.bounds.size.x, Is.EqualTo(srHighRes.bounds.size.x).Within(0.01f));
        }

        [Test]
        public void MatchObjectUniformSize_NormalizesDominantDimension()
        {
            SpriteRenderer sr = CreateTestSprite("TestSpriteDominant", 100, 50);

            manager.MatchObjectUniformSize(sr, 2f);

            Assert.That(sr.transform.localScale.x, Is.EqualTo(2f).Within(0.01f));
            Assert.That(sr.transform.localScale.y, Is.EqualTo(2f).Within(0.01f));
        }

        [Test]
        public void MatchObjectBounds_ForcesSizeParityBetweenDifferentSprites()
        {
            SpriteRenderer srA = CreateTestSprite("SpriteA", 64, 64);
            srA.transform.localScale = new Vector3(2f, 2f, 1f);

            SpriteRenderer srB = CreateTestSprite("SpriteB", 128, 128);

            manager.MatchObjectBounds(srB, srA, uniform: true);

            Assert.That(srB.bounds.size.x, Is.EqualTo(srA.bounds.size.x).Within(0.01f));
            Assert.That(srB.bounds.size.y, Is.EqualTo(srA.bounds.size.y).Within(0.01f));
        }

        [Test]
        public void MatchObjectSize_NonUniform_ScalesExactAxes()
        {
            SpriteRenderer sr = CreateTestSprite("TestSpriteNonUniform", 100, 100);

            manager.MatchObjectSize(sr, new Vector2(3f, 1.5f), preserveAspect: false);

            Assert.That(sr.transform.localScale.x, Is.EqualTo(3f).Within(0.01f));
            Assert.That(sr.transform.localScale.y, Is.EqualTo(1.5f).Within(0.01f));
        }

        [Test]
        public void MatchObjectUniformSize_NullSprite_DoesNotThrow()
        {
            GameObject emptyGo = new GameObject("EmptySprite");
            disposables.Add(emptyGo);
            SpriteRenderer sr = emptyGo.AddComponent<SpriteRenderer>();

            Assert.DoesNotThrow(() => manager.MatchObjectUniformSize(sr, 2f));
            Assert.DoesNotThrow(() => manager.MatchObjectBounds(sr, sr));
        }
    }
}
