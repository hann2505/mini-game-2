using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Player;
using KinematicsGame.Enemy;
using KinematicsGame.Combat;

namespace KinematicsGame.Tests
{
    public class GameControllerTests
    {
        private GameObject cameraGo;
        private Camera testCamera;
        private GameObject managerGo;
        private ViewportManager viewportManager;
        private GameObject gameControllerGo;
        private GameController gameController;
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

            gameControllerGo = new GameObject("TestGameController");
            disposables.Add(gameControllerGo);
            gameController = gameControllerGo.AddComponent<GameController>();
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

        private SpriteRenderer CreateSpriteObject(string name, int width, int height)
        {
            GameObject go = new GameObject(name);
            disposables.Add(go);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(width, height);
            disposables.Add(tex);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            return sr;
        }

        [Test]
        public void GameController_NormalizeEntitySizes_EnforcesExactSizeParity()
        {
            // Object A: 64x64 sprite
            SpriteRenderer srPlayer = CreateSpriteObject("PlayerSprite", 64, 64);
            PlayerController player = srPlayer.gameObject.AddComponent<PlayerController>();
            player.SpriteRenderer = srPlayer;

            // Object B: 256x256 sprite (different resolution)
            SpriteRenderer srTarget = CreateSpriteObject("TargetSprite", 256, 256);
            TargetController target = srTarget.gameObject.AddComponent<TargetController>();
            target.SpriteRenderer = srTarget;

            gameController.PlayerInstance = player;
            gameController.TargetInstance = target;
            gameController.TargetUniformSize = 2f;

            gameController.NormalizeEntitySizes();

            // Object A and Object B must have identical bounds dimensions
            Assert.That(srPlayer.bounds.size.x, Is.EqualTo(2f).Within(0.01f));
            Assert.That(srPlayer.bounds.size.y, Is.EqualTo(2f).Within(0.01f));
            Assert.That(srTarget.bounds.size.x, Is.EqualTo(srPlayer.bounds.size.x).Within(0.01f));
            Assert.That(srTarget.bounds.size.y, Is.EqualTo(srPlayer.bounds.size.y).Within(0.01f));
        }

        [Test]
        public void GameController_ApplyOrientation_Horizontal_PositionsOpposingMidEdges()
        {
            SpriteRenderer srPlayer = CreateSpriteObject("PlayerH", 64, 64);
            PlayerController player = srPlayer.gameObject.AddComponent<PlayerController>();
            player.SpriteRenderer = srPlayer;

            SpriteRenderer srTarget = CreateSpriteObject("TargetH", 64, 64);
            TargetController target = srTarget.gameObject.AddComponent<TargetController>();
            target.SpriteRenderer = srTarget;

            gameController.PlayerInstance = player;
            gameController.TargetInstance = target;

            gameController.ApplyOrientation(GameOrientation.Horizontal);

            // Object A at Mid-Left
            Assert.That(player.transform.position.x, Is.EqualTo(viewportManager.MinX + srPlayer.bounds.extents.x).Within(0.01f));
            Assert.That(player.transform.position.y, Is.EqualTo(viewportManager.Center.y).Within(0.01f));
            Assert.That(player.ProjectileDirection, Is.EqualTo(Vector2.right));

            // Object B at Mid-Right
            Assert.That(target.transform.position.x, Is.EqualTo(viewportManager.MaxX - srTarget.bounds.extents.x).Within(0.01f));
            Assert.That(target.transform.position.y, Is.EqualTo(viewportManager.Center.y).Within(0.01f));
            Assert.That(target.IsHorizontal, Is.True);
        }

        [Test]
        public void GameController_ApplyOrientation_Vertical_PositionsOpposingMidEdges()
        {
            SpriteRenderer srPlayer = CreateSpriteObject("PlayerV", 64, 64);
            PlayerController player = srPlayer.gameObject.AddComponent<PlayerController>();
            player.SpriteRenderer = srPlayer;

            SpriteRenderer srTarget = CreateSpriteObject("TargetV", 64, 64);
            TargetController target = srTarget.gameObject.AddComponent<TargetController>();
            target.SpriteRenderer = srTarget;

            gameController.PlayerInstance = player;
            gameController.TargetInstance = target;

            gameController.ApplyOrientation(GameOrientation.Vertical);

            // Object A at Mid-Top
            Assert.That(player.transform.position.y, Is.EqualTo(viewportManager.MaxY - srPlayer.bounds.extents.y).Within(0.01f));
            Assert.That(player.transform.position.x, Is.EqualTo(viewportManager.Center.x).Within(0.01f));
            Assert.That(player.ProjectileDirection, Is.EqualTo(Vector2.down));

            // Object B at Mid-Bottom
            Assert.That(target.transform.position.y, Is.EqualTo(viewportManager.MinY + srTarget.bounds.extents.y).Within(0.01f));
            Assert.That(target.transform.position.x, Is.EqualTo(viewportManager.Center.x).Within(0.01f));
            Assert.That(target.IsHorizontal, Is.False);
        }

        [Test]
        public void GameController_ScaleBackground_EncompassesCameraView()
        {
            SpriteRenderer srBg = CreateSpriteObject("Background", 1920, 1080);
            gameController.BackgroundRenderer = srBg;

            gameController.ScaleBackground();

            Assert.That(srBg.bounds.size.x, Is.GreaterThanOrEqualTo(viewportManager.Width - 0.01f));
            Assert.That(srBg.bounds.size.y, Is.GreaterThanOrEqualTo(viewportManager.Height - 0.01f));
        }

        [Test]
        public void GameController_OrientationProperty_FiresEvent()
        {
            GameOrientation captured = GameOrientation.Horizontal;
            bool eventFired = false;
            System.Action<GameOrientation> handler = (o) =>
            {
                captured = o;
                eventFired = true;
            };

            GameController.OnOrientationChanged += handler;
            try
            {
                gameController.Orientation = GameOrientation.Vertical;
                Assert.That(eventFired, Is.True);
                Assert.That(captured, Is.EqualTo(GameOrientation.Vertical));
            }
            finally
            {
                GameController.OnOrientationChanged -= handler;
            }
        }

        [Test]
        public void GameController_ApplyOrientation_RectangularSprite_UsesRotatedExtents()
        {
            // Non-square 100x50 sprite (width 1.0f, height 0.5f at 100 PPU)
            SpriteRenderer srPlayer = CreateSpriteObject("PlayerRect", 100, 50);
            PlayerController player = srPlayer.gameObject.AddComponent<PlayerController>();
            player.SpriteRenderer = srPlayer;

            gameController.PlayerInstance = player;

            // In Horizontal mode (rotation 0): bounds.extents.x is 0.5f
            gameController.ApplyOrientation(GameOrientation.Horizontal);
            Assert.That(player.transform.position.x, Is.EqualTo(viewportManager.MinX + srPlayer.bounds.extents.x).Within(0.01f));

            // In Vertical mode (rotation -90): bounds.extents.y becomes rotated extents
            gameController.ApplyOrientation(GameOrientation.Vertical);
            Assert.That(player.transform.position.y, Is.EqualTo(viewportManager.MaxY - srPlayer.bounds.extents.y).Within(0.01f));
        }
    }
}
