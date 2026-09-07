using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using KinematicsGame.Core;
using KinematicsGame.Player;
using KinematicsGame.Enemy;
using KinematicsGame.Combat;

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
            bgSr.sprite = LoadSprite("Assets/Sprites/Backgrounds/background1.png");
            bgSr.sortingOrder = -100;
            gc.BackgroundRenderer = bgSr;

            // Load and assign Prefabs, Sprites and Audio
            var serializedGc = new SerializedObject(gc);
            serializedGc.FindProperty("playerPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            serializedGc.FindProperty("targetPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Target.prefab");
            serializedGc.FindProperty("projectilePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");

            serializedGc.FindProperty("playerSprite").objectReferenceValue = LoadSprite("Assets/Sprites/Characters/Players/Ships/spaceship1.png");
            serializedGc.FindProperty("targetSprite").objectReferenceValue = LoadSprite("Assets/Sprites/Characters/Enemies/Birds/bird1.png");
            serializedGc.FindProperty("projectileSprite").objectReferenceValue = LoadSprite("Assets/Sprites/Weapons/Bullets/bullet1.png");
            serializedGc.FindProperty("backgroundSprite").objectReferenceValue = bgSr.sprite;

            serializedGc.FindProperty("backgroundMusic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/music.mp3");
            serializedGc.FindProperty("fireSfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/click.ogg");
            serializedGc.FindProperty("explosionSfx").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/explosion.wav");
            serializedGc.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Kinematics Game] Scene and prefabs successfully configured and saved!");
        }
    }
}
