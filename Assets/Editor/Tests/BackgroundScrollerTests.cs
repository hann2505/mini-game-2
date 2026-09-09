using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KinematicsGame.Core;

namespace KinematicsGame.Tests
{
    public class BackgroundScrollerTests
    {
        private GameObject cameraGo;
        private Camera testCamera;
        private GameObject managerGo;
        private ViewportManager viewportManager;
        private GameObject scrollerGo;
        private BackgroundScroller scroller;
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
            testCamera.aspect = 16f / 9f;
            testCamera.transform.position = new Vector3(0f, 0f, -10f);

            managerGo = new GameObject("TestViewportManager");
            disposables.Add(managerGo);
            viewportManager = managerGo.AddComponent<ViewportManager>();
            viewportManager.TargetCamera = testCamera;
            viewportManager.Initialize();

            scrollerGo = new GameObject("TestBackgroundScroller");
            disposables.Add(scrollerGo);
            scroller = scrollerGo.AddComponent<BackgroundScroller>();
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

        private Sprite CreateMockSprite(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height);
            disposables.Add(tex);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            disposables.Add(sprite);
            return sprite;
        }

        [Test]
        public void BackgroundScroller_Tick_AdvancesSegmentsByExactKinematicDistance()
        {
            Sprite sprite = CreateMockSprite(192, 76);
            scroller.Initialize(sprite, 1f, viewportManager, GameOrientation.Horizontal);
            scroller.ScrollSpeed = 4f;
            scroller.ScrollDirection = Vector2.left;

            float initialXA = scroller.SegmentA.transform.position.x;
            float initialXB = scroller.SegmentB.transform.position.x;

            scroller.Tick(0.1f);

            // -4f * 0.1f = -0.4f
            Assert.That(scroller.SegmentA.transform.position.x, Is.EqualTo(initialXA - 0.4f).Within(0.001f));
            Assert.That(scroller.SegmentB.transform.position.x, Is.EqualTo(initialXB - 0.4f).Within(0.001f));
        }

        [Test]
        public void BackgroundScroller_Tick_LeapfrogsWhenExitingViewportMinX()
        {
            Sprite sprite = CreateMockSprite(192, 76); // width = 1.92 units
            scroller.Initialize(sprite, 1f, viewportManager, GameOrientation.Horizontal);
            scroller.ScrollSpeed = 10f;
            scroller.ScrollDirection = Vector2.left;

            float minX = viewportManager.MinX;
            float segWidth = scroller.SegmentWidth;

            // Move segmentA until its right edge crosses minX
            float distanceToCross = (scroller.SegmentA.transform.position.x + (segWidth * 0.5f) - minX) + 0.1f;
            float deltaTime = distanceToCross / scroller.ScrollSpeed;

            scroller.Tick(deltaTime);

            // segmentA must have leapfrogged behind segmentB
            Assert.That(scroller.SegmentA.transform.position.x, Is.EqualTo(scroller.SegmentB.transform.position.x + segWidth).Within(0.01f));
        }

        [Test]
        public void BackgroundScroller_SetOrientation_SwitchesScrollAxisAndRealignsSegments()
        {
            Sprite sprite = CreateMockSprite(192, 76);
            scroller.Initialize(sprite, 1f, viewportManager, GameOrientation.Horizontal);

            scroller.SetOrientation(GameOrientation.Vertical);

            Assert.That(scroller.CurrentOrientation, Is.EqualTo(GameOrientation.Vertical));
            Assert.That(scroller.ScrollDirection.y, Is.GreaterThan(0f)); // Upward (+Y)

            // Segment B should be vertically below Segment A
            Assert.That(scroller.SegmentB.transform.position.y, Is.EqualTo(scroller.SegmentA.transform.position.y - scroller.SegmentHeight).Within(0.01f));
            Assert.That(scroller.SegmentB.transform.position.x, Is.EqualTo(scroller.SegmentA.transform.position.x).Within(0.01f));
        }

        [Test]
        public void BackgroundScroller_RefreshScale_PreservesScrollPositionAndContiguity()
        {
            Sprite sprite = CreateMockSprite(192, 76);
            scroller.Initialize(sprite, 1f, viewportManager, GameOrientation.Horizontal);
            scroller.ScrollSpeed = 5f;
            scroller.Tick(0.5f); // advances partially

            float posBeforeRefreshA = scroller.SegmentA.transform.position.x;

            scroller.RefreshScale(2f);

            // Main scroll phase must be preserved
            Assert.That(scroller.SegmentA.transform.position.x, Is.EqualTo(posBeforeRefreshA).Within(0.001f));

            // Contiguity preserved without gap or overlap: separation distance must equal new SegmentWidth
            float distanceBetweenSegments = Mathf.Abs(scroller.SegmentB.transform.position.x - scroller.SegmentA.transform.position.x);
            Assert.That(distanceBetweenSegments, Is.EqualTo(scroller.SegmentWidth).Within(0.001f));

            // Scaled dimensions must update
            Assert.That(scroller.SegmentWidth, Is.EqualTo(1.92f * 2f).Within(0.01f));
            Assert.That(scroller.SegmentHeight, Is.EqualTo(0.76f * 2f).Within(0.01f));
        }

        [Test]
        public void BackgroundScroller_Tick_LeapfrogsWhenExitingViewportMaxY_Vertical()
        {
            Sprite sprite = CreateMockSprite(76, 192); // height = 1.92 units
            scroller.Initialize(sprite, 1f, viewportManager, GameOrientation.Vertical);
            scroller.ScrollSpeed = 10f;
            scroller.ScrollDirection = Vector2.up;

            float maxY = viewportManager.MaxY;
            float segHeight = scroller.SegmentHeight;

            // Advance segmentA until its bottom edge passes maxY
            float distanceToCross = (maxY - (scroller.SegmentA.transform.position.y - (segHeight * 0.5f))) + 0.1f;
            float deltaTime = distanceToCross / scroller.ScrollSpeed;

            scroller.Tick(deltaTime);

            // segmentA must have leapfrogged below segmentB
            Assert.That(scroller.SegmentA.transform.position.y, Is.EqualTo(scroller.SegmentB.transform.position.y - segHeight).Within(0.01f));
        }

        [Test]
        public void BackgroundScroller_ZeroDeltaTime_DoesNotMoveSegments()
        {
            Sprite sprite = CreateMockSprite(192, 76);
            scroller.Initialize(sprite, 1f, viewportManager, GameOrientation.Horizontal);

            Vector3 posA = scroller.SegmentA.transform.position;
            Vector3 posB = scroller.SegmentB.transform.position;

            scroller.Tick(0f);

            Assert.That(scroller.SegmentA.transform.position, Is.EqualTo(posA));
            Assert.That(scroller.SegmentB.transform.position, Is.EqualTo(posB));
        }

        [Test]
        public void BackgroundScroller_ScaleBackground_DisablesParentRendererAndIsolatesScale()
        {
            GameObject gcGo = new GameObject("TestGameController");
            disposables.Add(gcGo);
            GameController gc = gcGo.AddComponent<GameController>();

            GameObject bgGo = new GameObject("Background");
            disposables.Add(bgGo);
            SpriteRenderer parentSr = bgGo.AddComponent<SpriteRenderer>();
            Sprite sprite = CreateMockSprite(1920, 768);
            parentSr.sprite = sprite;

            BackgroundScroller bs = bgGo.AddComponent<BackgroundScroller>();
            gc.BackgroundRenderer = parentSr;
            gc.BackgroundScroller = bs;
            gc.BackgroundSprite = sprite;

            gc.ScaleBackground();

            // Parent SpriteRenderer must be disabled to prevent Z-fighting
            Assert.That(parentSr.enabled, Is.False);

            // Parent scale must remain Vector3.one for scale isolation
            Assert.That(bgGo.transform.localScale, Is.EqualTo(Vector3.one));

            // Child segments must be initialized and enabled
            Assert.That(bs.IsInitialized, Is.True);
            Assert.That(bs.SegmentA.enabled, Is.True);
            Assert.That(bs.SegmentB.enabled, Is.True);
        }
    }
}
