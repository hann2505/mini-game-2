using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using KinematicsGame.Core;
using KinematicsGame.Player;
using KinematicsGame.Enemy;
using KinematicsGame.Combat;
using KinematicsGame.Audio;
using KinematicsGame.UI;

namespace KinematicsGame.Editor
{
    /// <summary>
    /// Editor utility to construct combat prefabs, configure audio, HUD toggles,
    /// restricted zone, and assemble SampleScene.unity on demand.
    /// Access via menu item: Kinematics Game -> Setup Scene & Prefabs
    /// </summary>
    public static class SceneSetupHelper
    {
        [MenuItem("Kinematics Game/Setup Scene & Prefabs", false, 1)]
        public static void SetupAll()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
            {
                if (System.IO.File.Exists(scenePath))
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
            }

            EnsurePrefabsExist();
            SetupSceneHierarchy();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
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
            Sprite missileSprite = LoadSprite("Assets/Sprites/Weapons/Missiles/Missile_1/Flying/Missile_1_Flying_000.png");
            Sprite bombSprite = LoadSprite("Assets/Sprites/Weapons/Bombs/Bomb_1/Idle/Bomb_1_Idle_000.png");
            Sprite mineSprite = LoadSprite("Assets/Sprites/Items/Bonuses/Enemy_Destroy_Bonus.png");
            Sprite crateSprite = LoadSprite("Assets/Sprites/Items/Bonuses/Armor_Bonus.png");
            Sprite gemSprite = LoadSprite("Assets/Sprites/Items/Collectibles/diamond.png");
            Sprite shieldSprite = LoadSprite("Assets/Sprites/Items/Bonuses/Barrier_Bonus.png");
            Sprite shipSprite = LoadSprite("Assets/Sprites/Characters/Players/Ships/spaceship1.png");
            Sprite birdSprite = LoadSprite("Assets/Sprites/Characters/Enemies/Birds/bird1.png");

            AudioClip explosionClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/explosion.wav");
            AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/click.ogg");
            AudioClip bombClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/bomb.mp3");
            AudioClip eatClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/eat.ogg");

            // 1. Blaster Projectile Prefab
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

            // 2. Missile Prefab
            string missilePath = "Assets/Prefabs/Missile.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(missilePath) == null)
            {
                GameObject mGo = new GameObject("Missile");
                SpriteRenderer sr = mGo.AddComponent<SpriteRenderer>();
                sr.sprite = missileSprite != null ? missileSprite : bulletSprite;
                sr.sortingOrder = 10;
                CircleCollider2D col = mGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                HomingMissile hm = mGo.AddComponent<HomingMissile>();

                PrefabUtility.SaveAsPrefabAsset(mGo, missilePath);
                Object.DestroyImmediate(mGo);
            }

            // 3. Bomb Prefab
            string bombPath = "Assets/Prefabs/Bomb.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(bombPath) == null)
            {
                GameObject bGo = new GameObject("Bomb");
                SpriteRenderer sr = bGo.AddComponent<SpriteRenderer>();
                sr.sprite = bombSprite != null ? bombSprite : bulletSprite;
                sr.sortingOrder = 10;
                CircleCollider2D col = bGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                ClusterBomb cb = bGo.AddComponent<ClusterBomb>();

                PrefabUtility.SaveAsPrefabAsset(bGo, bombPath);
                Object.DestroyImmediate(bGo);
            }

            // 4. Hazard Mine Prefab (Object X)
            string minePath = "Assets/Prefabs/Hazard_Mine.prefab";
            GameObject existingMine = AssetDatabase.LoadAssetAtPath<GameObject>(minePath);
            if (existingMine == null || existingMine.GetComponent<Rigidbody2D>() == null)
            {
                GameObject mineGo = new GameObject("Hazard_Mine");
                SpriteRenderer sr = mineGo.AddComponent<SpriteRenderer>();
                sr.sprite = mineSprite != null ? mineSprite : bulletSprite;
                sr.sortingOrder = 8;
                Rigidbody2D rb = mineGo.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
                CircleCollider2D col = mineGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                InteractiveEntity ie = mineGo.AddComponent<InteractiveEntity>();
                ie.Type = EntityType.HazardMine;
                ie.TargetSize = 1.0f;
                ie.ApplyTargetSize();
                ie.InteractionSfx = explosionClip;

                PrefabUtility.SaveAsPrefabAsset(mineGo, minePath);
                Object.DestroyImmediate(mineGo);
            }

            // 5. Tech Supply Crate Prefab (Object Y)
            string cratePath = "Assets/Prefabs/Supply_Crate.prefab";
            GameObject existingCrate = AssetDatabase.LoadAssetAtPath<GameObject>(cratePath);
            if (existingCrate == null || existingCrate.GetComponent<Rigidbody2D>() == null)
            {
                GameObject crateGo = new GameObject("Supply_Crate");
                SpriteRenderer sr = crateGo.AddComponent<SpriteRenderer>();
                sr.sprite = crateSprite != null ? crateSprite : bulletSprite;
                sr.sortingOrder = 8;
                Rigidbody2D rb = crateGo.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
                CircleCollider2D col = crateGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                InteractiveEntity ie = crateGo.AddComponent<InteractiveEntity>();
                ie.Type = EntityType.SupplyCrate;
                ie.TargetSize = 1.0f;
                ie.ApplyTargetSize();
                ie.InteractionSfx = eatClip;

                PrefabUtility.SaveAsPrefabAsset(crateGo, cratePath);
                Object.DestroyImmediate(crateGo);
            }

            // 6. Gem Core Prefab (Object Z)
            string gemPath = "Assets/Prefabs/Gem_Core.prefab";
            GameObject existingGem = AssetDatabase.LoadAssetAtPath<GameObject>(gemPath);
            if (existingGem == null || existingGem.GetComponent<Rigidbody2D>() == null)
            {
                GameObject gemGo = new GameObject("Gem_Core");
                SpriteRenderer sr = gemGo.AddComponent<SpriteRenderer>();
                sr.sprite = gemSprite != null ? gemSprite : bulletSprite;
                sr.sortingOrder = 8;
                Rigidbody2D rb = gemGo.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
                CircleCollider2D col = gemGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                if (col.radius < 0.6f) col.radius = 0.6f;
                InteractiveEntity ie = gemGo.AddComponent<InteractiveEntity>();
                ie.Type = EntityType.GemCore;
                ie.TargetSize = 1.5f;
                ie.ApplyTargetSize();
                ie.InteractionSfx = eatClip;

                PrefabUtility.SaveAsPrefabAsset(gemGo, gemPath);
                Object.DestroyImmediate(gemGo);
            }

            // 7. Shield Overlay Prefab
            string shieldPath = "Assets/Prefabs/ShieldOverlay.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(shieldPath) == null)
            {
                GameObject shieldGo = new GameObject("ShieldOverlay");
                SpriteRenderer sr = shieldGo.AddComponent<SpriteRenderer>();
                sr.sprite = shieldSprite;
                sr.color = new Color(0.4f, 0.8f, 1f, 0.6f);
                sr.sortingOrder = 12;

                PrefabUtility.SaveAsPrefabAsset(shieldGo, shieldPath);
                Object.DestroyImmediate(shieldGo);
            }

            // 8. Player Prefab
            string playerPath = "Assets/Prefabs/Player.prefab";
            GameObject existingPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
            if (existingPlayerPrefab == null || existingPlayerPrefab.GetComponent<PlayerStats>() == null || existingPlayerPrefab.GetComponent<PlayerCombatSystem>() == null || existingPlayerPrefab.GetComponent<Rigidbody2D>() == null)
            {
                GameObject playerGo = new GameObject("Player");
                SpriteRenderer sr = playerGo.AddComponent<SpriteRenderer>();
                sr.sprite = shipSprite;
                sr.sortingOrder = 5;
                Rigidbody2D rb = playerGo.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
                CircleCollider2D col = playerGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                AudioSource audio = playerGo.AddComponent<AudioSource>();
                audio.playOnAwake = false;

                PlayerStats ps = playerGo.AddComponent<PlayerStats>();
                PlayerCombatSystem pcs = playerGo.AddComponent<PlayerCombatSystem>();
                pcs.BlasterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);
                pcs.MissilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(missilePath);
                pcs.BombPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bombPath);
                pcs.BlasterFireClip = clickClip;
                pcs.MissileFireClip = bombClip;
                pcs.BombDeployClip = clickClip;

                PlayerDefenseSystem pds = playerGo.AddComponent<PlayerDefenseSystem>();
                pds.ShieldActivateClip = eatClip;
                pds.ShieldHitClip = clickClip;
                pds.EmpClip = explosionClip;

                PlayerController pc = playerGo.AddComponent<PlayerController>();
                pc.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);
                pc.Stats = ps;
                pc.CombatSystem = pcs;
                pc.DefenseSystem = pds;

                PrefabUtility.SaveAsPrefabAsset(playerGo, playerPath);
                Object.DestroyImmediate(playerGo);
            }

            // 9. Target Prefab
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
            // ── AudioManager ─────────────────────────────────────────────────────
            AudioManager am = Object.FindFirstObjectByType<AudioManager>();
            if (am == null)
            {
                GameObject amGo = new GameObject("AudioManager");
                am = amGo.AddComponent<AudioManager>();
                am.InitializeChannels();
            }
            if (am != null)
            {
                am.SfxVolume = 0.25f;
                am.MusicVolume = 0.25f;
                if (am.MusicSource != null)
                {
                    AudioClip bgm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/music.mp3");
                    if (bgm != null) am.PlayMusic(bgm, 0.25f, true);
                }
            }

            // ── EventSystem ──────────────────────────────────────────────────────
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            // ── GameController ───────────────────────────────────────────────────
            GameController gc = Object.FindFirstObjectByType<GameController>();
            if (gc == null)
            {
                GameObject gcGo = new GameObject("GameController");
                gc = gcGo.AddComponent<GameController>();
                AudioSource audio = gcGo.AddComponent<AudioSource>();
                audio.playOnAwake = false;
            }

            // ── ViewportManager ──────────────────────────────────────────────────
            ViewportManager vm = Object.FindFirstObjectByType<ViewportManager>();
            if (vm == null)
            {
                GameObject vmGo = new GameObject("ViewportManager");
                vm = vmGo.AddComponent<ViewportManager>();
            }

            // ── Background ───────────────────────────────────────────────────────
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

            // ── Prefabs, Sprites and Audio configuration on GameController ────────
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
            serializedGc.FindProperty("musicVolume").floatValue = 0.25f;
            serializedGc.ApplyModifiedProperties();

            // ── TargetSpawner ────────────────────────────────────────────────────
            TargetSpawner spawner = Object.FindFirstObjectByType<TargetSpawner>();
            if (spawner == null)
            {
                GameObject spawnerGo = new GameObject("TargetSpawner");
                spawner = spawnerGo.AddComponent<TargetSpawner>();
            }
            gc.TargetSpawner = spawner;

            Sprite[] birdSprites = new Sprite[]
            {
                LoadSprite("Assets/Sprites/Characters/Enemies/Birds/bird1.png"),
                LoadSprite("Assets/Sprites/Characters/Enemies/Birds/bird2.png"),
                LoadSprite("Assets/Sprites/Characters/Enemies/Birds/bird4.png"),
            };
            TargetProfile[] profiles = TargetProfile.CreateDefaultPresets(birdSprites);
            spawner.TargetProfiles = profiles;
            spawner.InitialQuota = 1;
            spawner.MaxQuota = 6;
            spawner.ScorePerQuotaIncrease = 200;
            spawner.InitialSpawnInterval = 2.5f;
            spawner.MinSpawnInterval = 1.0f;

            // ── HazardSpawner ────────────────────────────────────────────────────
            HazardSpawner hazardSpawner = Object.FindFirstObjectByType<HazardSpawner>();
            if (hazardSpawner == null)
            {
                GameObject hsGo = new GameObject("HazardSpawner");
                hazardSpawner = hsGo.AddComponent<HazardSpawner>();
            }
            hazardSpawner.HazardMinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hazard_Mine.prefab");
            hazardSpawner.SupplyCratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Supply_Crate.prefab");
            hazardSpawner.GemCorePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gem_Core.prefab");
            hazardSpawner.SpawnInterval = 8.0f;

            // ── RestrictedZoneTrigger ────────────────────────────────────────────
            RestrictedZoneTrigger zoneTrigger = Object.FindFirstObjectByType<RestrictedZoneTrigger>();
            if (zoneTrigger == null)
            {
                GameObject rzGo = new GameObject("RestrictedZone");
                zoneTrigger = rzGo.AddComponent<RestrictedZoneTrigger>();
                zoneTrigger.WarningClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/bomb.mp3");
            }
            zoneTrigger.AlignToViewport(gc.Orientation);

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
            scoreMgr.SfxVolume = 0.25f;

            // ── FloatingTextPool ──────────────────────────────────────────────────
            FloatingTextPool fctPool = Object.FindFirstObjectByType<FloatingTextPool>();
            if (fctPool == null)
            {
                GameObject fctPoolGo = new GameObject("FloatingTextPool");
                fctPool = fctPoolGo.AddComponent<FloatingTextPool>();
            }

            // ── HUD Canvas ────────────────────────────────────────────────────────
            GameObject canvasGo = GameObject.Find("HUD_Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("HUD_Canvas");
                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            GameHUDController hudCtrl = canvasGo.GetComponent<GameHUDController>();
            if (hudCtrl == null) hudCtrl = canvasGo.AddComponent<GameHUDController>();

            // Audio Toggle Buttons (64x64, top-left)
            Sprite soundOffSprite = LoadSprite("Assets/Sprites/UI/Buttons/sound_off.png");
            Sprite soundOnSprite = LoadSprite("Assets/Sprites/UI/Buttons/sound_on.png");
            Sprite musicOffSprite = LoadSprite("Assets/Sprites/UI/Buttons/music_TurnOff.png");
            Sprite musicOnSprite = LoadSprite("Assets/Sprites/UI/Buttons/music_TurnOn.png");

            GameObject soundBtnGo = GameObject.Find("SoundToggleButton");
            if (soundBtnGo == null)
            {
                soundBtnGo = new GameObject("SoundToggleButton");
                soundBtnGo.transform.SetParent(canvasGo.transform, false);
                RectTransform rt = soundBtnGo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(20f, -20f);
                rt.sizeDelta = new Vector2(64f, 64f);

                Image img = soundBtnGo.AddComponent<Image>();
                img.sprite = soundOffSprite;
                soundBtnGo.AddComponent<Button>();

                AudioToggleButton toggle = soundBtnGo.AddComponent<AudioToggleButton>();
                toggle.ToggleType = AudioToggleType.Sound;
                toggle.ActiveSprite = soundOffSprite;
                toggle.InactiveSprite = soundOnSprite;
                toggle.FixedDimensions = new Vector2(64f, 64f);
            }

            GameObject musicBtnGo = GameObject.Find("MusicToggleButton");
            if (musicBtnGo == null)
            {
                musicBtnGo = new GameObject("MusicToggleButton");
                musicBtnGo.transform.SetParent(canvasGo.transform, false);
                RectTransform rt = musicBtnGo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(95f, -20f);
                rt.sizeDelta = new Vector2(64f, 64f);

                Image img = musicBtnGo.AddComponent<Image>();
                img.sprite = musicOffSprite;
                musicBtnGo.AddComponent<Button>();

                AudioToggleButton toggle = musicBtnGo.AddComponent<AudioToggleButton>();
                toggle.ToggleType = AudioToggleType.Music;
                toggle.ActiveSprite = musicOffSprite;
                toggle.InactiveSprite = musicOnSprite;
                toggle.FixedDimensions = new Vector2(64f, 64f);
            }

            // Player vitals & currency labels
            TextMeshProUGUI hpLbl = CreateOrFindLabel(canvasGo, "HpLabel", "HP: 100/100", 22, new Vector2(170f, -22f), new Vector2(180f, 30f));
            TextMeshProUGUI armorLbl = CreateOrFindLabel(canvasGo, "ArmorLabel", "ARMOR: 50/50", 20, new Vector2(170f, -50f), new Vector2(180f, 30f));
            TextMeshProUGUI goldLbl = CreateOrFindLabel(canvasGo, "GoldLabel", "🪙 0", 22, new Vector2(360f, -22f), new Vector2(120f, 30f));
            TextMeshProUGUI diamondLbl = CreateOrFindLabel(canvasGo, "DiamondLabel", "💎 0", 22, new Vector2(360f, -50f), new Vector2(120f, 30f));
            TextMeshProUGUI weaponLbl = CreateOrFindLabel(canvasGo, "WeaponLabel", "WEAPON: Blaster", 20, new Vector2(490f, -22f), new Vector2(200f, 30f));
            TextMeshProUGUI cdLbl = CreateOrFindLabel(canvasGo, "CooldownLabel", "SHIELD: READY | EMP: READY", 16, new Vector2(490f, -50f), new Vector2(250f, 30f));
            TextMeshProUGUI shieldLbl = CreateOrFindLabel(canvasGo, "ShieldLabel", "SHIELD: READY", 16, new Vector2(170f, -74f), new Vector2(180f, 25f));

            // Score & Combo labels
            TextMeshProUGUI scoreLabel = CreateOrFindLabel(canvasGo, "ScoreLabel", "0", 72, new Vector2(-20f, -20f), new Vector2(400f, 90f), TextAlignmentOptions.TopRight, new Vector2(1f, 1f));
            TextMeshProUGUI highScoreLabel = CreateOrFindLabel(canvasGo, "HighScoreLabel", "Best: 0", 28, new Vector2(-20f, -110f), new Vector2(400f, 40f), TextAlignmentOptions.TopRight, new Vector2(1f, 1f));
            TextMeshProUGUI comboLabel = CreateOrFindLabel(canvasGo, "ComboLabel", "x2 COMBO", 54, new Vector2(0f, 50f), new Vector2(500f, 80f), TextAlignmentOptions.Bottom, new Vector2(0.5f, 0f));
            comboLabel.gameObject.SetActive(false);

            hudCtrl.SetLabels(scoreLabel, highScoreLabel, comboLabel);
            hudCtrl.SetStatsLabels(hpLbl, armorLbl, shieldLbl, goldLbl, diamondLbl, weaponLbl, null, cdLbl);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Kinematics Game] Scene and prefabs successfully configured and saved!");
        }

        private static TextMeshProUGUI CreateOrFindLabel(
            GameObject parent,
            string name,
            string defaultText,
            float fontSize,
            Vector2 anchoredPos,
            Vector2 sizeDelta,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left,
            Vector2? anchor = null)
        {
            Transform existing = parent.transform.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();

            Vector2 anchorVal = anchor ?? new Vector2(0f, 1f);
            rt.anchorMin = anchorVal;
            rt.anchorMax = anchorVal;
            rt.pivot = anchorVal;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = defaultText;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;

            return tmp;
        }
    }
}
