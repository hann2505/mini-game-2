using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KinematicsGame.Core;
using KinematicsGame.Player;
using KinematicsGame.Enemy;
using KinematicsGame.Combat;
using KinematicsGame.UI;

namespace KinematicsGame.Editor
{
    /// <summary>
    /// Editor utility to construct prefabs and configure SampleScene.unity on demand.
    /// Access via menu item: Kinematics Game -> Setup Scene & Prefabs
    /// </summary>
    public static class SceneSetupHelper
    {
        [MenuItem("Kinematics Game/Setup Scene & Prefabs", false, 1)]
        public static void SetupAll()
        {
            EnsurePrefabsExist();
            SetupSceneHierarchy();
        }

        public static Sprite LoadSprite(string path)
        {
            Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (direct != null) return direct;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null)
            {
                foreach (Object obj in assets)
                {
                    if (obj is Sprite s) return s;
                }
            }
            return null;
        }

        public static void EnsurePrefabsExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            Sprite bulletSprite = LoadSprite("Assets/Sprites/Weapons/Bullets/bullet1.png");
            Sprite shipSprite = LoadSprite("Assets/Sprites/Characters/Players/Ships/spaceship1.png");
            Sprite birdSprite = LoadSprite("Assets/Sprites/Characters/Enemies/Birds/bird1.png");

            // 1. Projectile Prefab
            string projPath = "Assets/Prefabs/Projectile.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(projPath) == null)
            {
                GameObject pGo = new GameObject("Projectile");
                SpriteRenderer sr = pGo.AddComponent<SpriteRenderer>();
                sr.sprite = bulletSprite;
                sr.sortingOrder = 10;
                CircleCollider2D col = pGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                pGo.AddComponent<Projectile>();

                PrefabUtility.SaveAsPrefabAsset(pGo, projPath);
                Object.DestroyImmediate(pGo);
            }

            // 2. Player Prefab
            string playerPath = "Assets/Prefabs/Player.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(playerPath) == null)
            {
                GameObject playerGo = new GameObject("Player");
                SpriteRenderer sr = playerGo.AddComponent<SpriteRenderer>();
                sr.sprite = shipSprite;
                sr.sortingOrder = 5;
                CircleCollider2D col = playerGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                AudioSource audio = playerGo.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                PlayerController pc = playerGo.AddComponent<PlayerController>();
                pc.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);

                PrefabUtility.SaveAsPrefabAsset(playerGo, playerPath);
                Object.DestroyImmediate(playerGo);
            }

            // 3. Target Prefab
            string targetPath = "Assets/Prefabs/Target.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) == null)
            {
                GameObject targetGo = new GameObject("Target");
                SpriteRenderer sr = targetGo.AddComponent<SpriteRenderer>();
                sr.sprite = birdSprite;
                sr.sortingOrder = 5;
                CircleCollider2D col = targetGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                AudioSource audio = targetGo.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                targetGo.AddComponent<TargetController>();

                PrefabUtility.SaveAsPrefabAsset(targetGo, targetPath);
                Object.DestroyImmediate(targetGo);
            }

            AssetDatabase.SaveAssets();
        }

        public static void SetupSceneHierarchy()
        {
            // Find or create GameController
            GameController gc = Object.FindFirstObjectByType<GameController>();
            if (gc == null)
            {
                GameObject gcGo = new GameObject("GameController");
                gc = gcGo.AddComponent<GameController>();
                AudioSource audio = gcGo.AddComponent<AudioSource>();
                audio.playOnAwake = false;
            }

            // Find or create ViewportManager
            ViewportManager vm = Object.FindFirstObjectByType<ViewportManager>();
            if (vm == null)
            {
                GameObject vmGo = new GameObject("ViewportManager");
                vm = vmGo.AddComponent<ViewportManager>();
            }

            // Find or create Background
            GameObject bgGo = GameObject.Find("Background");
            if (bgGo == null)
            {
                bgGo = new GameObject("Background");
            }
            SpriteRenderer bgSr = bgGo.GetComponent<SpriteRenderer>();
            if (bgSr == null) bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = LoadSprite("Assets/Sprites/Backgrounds/background12.jpg");
            bgSr.sortingOrder = -100;
            gc.BackgroundRenderer = bgSr;

            BackgroundScroller scroller = bgGo.GetComponent<BackgroundScroller>();
            if (scroller == null) scroller = bgGo.AddComponent<BackgroundScroller>();
            gc.BackgroundScroller = scroller;

            // Load and assign Prefabs, Sprites and Audio
            var serializedGc = new SerializedObject(gc);
            serializedGc.FindProperty("playerPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            serializedGc.FindProperty("targetPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Target.prefab");
            serializedGc.FindProperty("projectilePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");

            serializedGc.FindProperty("playerSprite").objectReferenceValue = LoadSprite("Assets/Sprites/Characters/Players/Ships/spaceship1.png");
            serializedGc.FindProperty("targetSprite").objectReferenceValue = LoadSprite("Assets/Sprites/Characters/Enemies/Birds/bird1.png");
            serializedGc.FindProperty("projectileSprite").objectReferenceValue = LoadSprite("Assets/Sprites/Weapons/Bullets/bullet1.png");
            serializedGc.FindProperty("backgroundRenderer").objectReferenceValue = bgSr;
            serializedGc.FindProperty("backgroundSprite").objectReferenceValue = bgSr.sprite;
            serializedGc.FindProperty("backgroundScroller").objectReferenceValue = scroller;

            serializedGc.FindProperty("backgroundMusic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/music.mp3");
            serializedGc.FindProperty("fireSfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/click.ogg");
            serializedGc.FindProperty("explosionSfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/explosion.wav");
            serializedGc.ApplyModifiedProperties();

            // ── TargetSpawner ────────────────────────────────────────────────────
            TargetSpawner spawner = Object.FindFirstObjectByType<TargetSpawner>();
            if (spawner == null)
            {
                GameObject spawnerGo = new GameObject("TargetSpawner");
                spawner = spawnerGo.AddComponent<TargetSpawner>();
            }
            gc.TargetSpawner = spawner;

            // Assign bird sprites to TargetSpawner profiles (excluding bird3)
            Sprite[] birdSprites = new Sprite[]
            {
                LoadSprite("Assets/Sprites/Characters/Enemies/Birds/bird1.png"),
                LoadSprite("Assets/Sprites/Characters/Enemies/Birds/bird2.png"),
                LoadSprite("Assets/Sprites/Characters/Enemies/Birds/bird4.png"),
            };
            TargetProfile[] profiles = TargetProfile.CreateDefaultPresets(birdSprites);
            spawner.TargetProfiles = profiles;
            spawner.PlayerInstance = gc.PlayerInstance;

            var serializedSpawner = new SerializedObject(spawner);
            var profilesProp = serializedSpawner.FindProperty("targetProfiles");
            profilesProp.ClearArray();
            profilesProp.arraySize = profiles.Length;
            for (int i = 0; i < profiles.Length; i++)
            {
                var elem = profilesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("profileName").stringValue = profiles[i].ProfileName;
                elem.FindPropertyRelative("sprite").objectReferenceValue = profiles[i].Sprite;
                elem.FindPropertyRelative("speedMultiplier").floatValue = profiles[i].SpeedMultiplier;
                elem.FindPropertyRelative("waveFrequencyMultiplier").floatValue = profiles[i].WaveFrequencyMultiplier;
                elem.FindPropertyRelative("waveAmplitudeMultiplier").floatValue = profiles[i].WaveAmplitudeMultiplier;
                elem.FindPropertyRelative("pointValue").intValue = profiles[i].PointValue;
                elem.FindPropertyRelative("spawnWeight").floatValue = profiles[i].SpawnWeight;
            }
            serializedSpawner.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);

            // ── ScoreManager ─────────────────────────────────────────────────────
            ScoreManager scoreMgr = Object.FindFirstObjectByType<ScoreManager>();
            if (scoreMgr == null)
            {
                GameObject scoreMgrGo = new GameObject("ScoreManager");
                scoreMgr = scoreMgrGo.AddComponent<ScoreManager>();
                AudioSource scoreAudio = scoreMgrGo.AddComponent<AudioSource>();
                scoreAudio.playOnAwake = false;
            }
            gc.ScoreManager = scoreMgr;
            scoreMgr.ExplosionVolume = 0.25f;
            var serializedSm = new SerializedObject(scoreMgr);
            serializedSm.FindProperty("hitClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/explosion.wav");
            serializedSm.FindProperty("comboClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/eat.ogg");
            serializedSm.FindProperty("highScoreClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/congratulation.wav");
            var expVolProp = serializedSm.FindProperty("explosionVolume");
            if (expVolProp != null) expVolProp.floatValue = 0.25f;
            serializedSm.ApplyModifiedProperties();

            // ── FloatingTextPool ──────────────────────────────────────────────────
            FloatingTextPool fctPool = Object.FindFirstObjectByType<FloatingTextPool>();
            if (fctPool == null)
            {
                GameObject fctPoolGo = new GameObject("FloatingTextPool");
                fctPool = fctPoolGo.AddComponent<FloatingTextPool>();
            }

            // ── HUD Canvas ────────────────────────────────────────────────────────
            GameHUDController hudCtrl = Object.FindFirstObjectByType<GameHUDController>();
            if (hudCtrl == null)
            {
                GameObject canvasGo = new GameObject("HUD_Canvas");
                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
                hudCtrl = canvasGo.AddComponent<GameHUDController>();

                // Score label (increased size)
                GameObject scoreLblGo = new GameObject("ScoreLabel");
                scoreLblGo.transform.SetParent(canvasGo.transform, false);
                TextMeshProUGUI scoreLabel = scoreLblGo.AddComponent<TextMeshProUGUI>();
                scoreLabel.text = "0";
                scoreLabel.fontSize = 72;
                scoreLabel.alignment = TextAlignmentOptions.TopRight;
                RectTransform scoreRect = scoreLblGo.GetComponent<RectTransform>();
                scoreRect.anchorMin = new Vector2(1f, 1f);
                scoreRect.anchorMax = new Vector2(1f, 1f);
                scoreRect.pivot = new Vector2(1f, 1f);
                scoreRect.anchoredPosition = new Vector2(-20f, -20f);
                scoreRect.sizeDelta = new Vector2(400f, 90f);

                // High score label
                GameObject hslGo = new GameObject("HighScoreLabel");
                hslGo.transform.SetParent(canvasGo.transform, false);
                TextMeshProUGUI highScoreLabel = hslGo.AddComponent<TextMeshProUGUI>();
                highScoreLabel.text = "Best: 0";
                highScoreLabel.fontSize = 28;
                highScoreLabel.color = new Color(1f, 0.9f, 0.2f);
                highScoreLabel.alignment = TextAlignmentOptions.TopRight;
                RectTransform hsRect = hslGo.GetComponent<RectTransform>();
                hsRect.anchorMin = new Vector2(1f, 1f);
                hsRect.anchorMax = new Vector2(1f, 1f);
                hsRect.pivot = new Vector2(1f, 1f);
                hsRect.anchoredPosition = new Vector2(-20f, -110f);
                hsRect.sizeDelta = new Vector2(400f, 40f);

                // Combo label (increased size)
                GameObject comboGo = new GameObject("ComboLabel");
                comboGo.transform.SetParent(canvasGo.transform, false);
                TextMeshProUGUI comboLabel = comboGo.AddComponent<TextMeshProUGUI>();
                comboLabel.text = "x2 COMBO";
                comboLabel.fontSize = 54;
                comboLabel.color = new Color(1f, 0.9f, 0.2f);
                comboLabel.alignment = TextAlignmentOptions.Bottom;
                comboGo.SetActive(false);
                RectTransform comboRect = comboGo.GetComponent<RectTransform>();
                comboRect.anchorMin = new Vector2(0.5f, 0f);
                comboRect.anchorMax = new Vector2(0.5f, 0f);
                comboRect.pivot = new Vector2(0.5f, 0f);
                comboRect.anchoredPosition = new Vector2(0f, 50f);
                comboRect.sizeDelta = new Vector2(500f, 80f);

                hudCtrl.SetLabels(scoreLabel, highScoreLabel, comboLabel);
            }

            if (hudCtrl != null)
            {
                hudCtrl.ScoreFontSize = 72f;
                hudCtrl.HighScoreFontSize = 28f;
                hudCtrl.ComboFontSize = 54f;
                hudCtrl.ApplyFontSizes();

                if (hudCtrl.ScoreLabel != null)
                {
                    RectTransform sr = hudCtrl.ScoreLabel.GetComponent<RectTransform>();
                    if (sr != null) sr.sizeDelta = new Vector2(400f, 90f);
                }
                if (hudCtrl.HighScoreLabel != null)
                {
                    RectTransform hsr = hudCtrl.HighScoreLabel.GetComponent<RectTransform>();
                    if (hsr != null)
                    {
                        hsr.anchoredPosition = new Vector2(-20f, -110f);
                        hsr.sizeDelta = new Vector2(400f, 40f);
                    }
                }
                if (hudCtrl.ComboLabel != null)
                {
                    RectTransform cr = hudCtrl.ComboLabel.GetComponent<RectTransform>();
                    if (cr != null)
                    {
                        cr.anchoredPosition = new Vector2(0f, 50f);
                        cr.sizeDelta = new Vector2(500f, 80f);
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Kinematics Game] Scene and prefabs successfully configured and saved!");
        }
    }
}
