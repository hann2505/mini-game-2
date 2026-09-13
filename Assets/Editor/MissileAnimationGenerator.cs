using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using KinematicsGame.Combat;

namespace KinematicsGame.Editor
{
    /// <summary>
    /// Generates AnimationClips, AnimatorController, and Explosion prefabs for Missiles
    /// from sprite frame sequences, and wires them into Missile.prefab.
    /// </summary>
    public static class MissileAnimationGenerator
    {
        private const string AnimDir = "Assets/Animations/Missile";
        private const string FlyingAnimPath = "Assets/Animations/Missile/Missile_Flying.anim";
        private const string ExplosionAnimPath = "Assets/Animations/Missile/Missile_Explosion.anim";
        private const string ControllerPath = "Assets/Animations/Missile/Missile.controller";
        private const string ExplosionPrefabPath = "Assets/Prefabs/Missile_Explosion.prefab";
        private const string MissilePrefabPath = "Assets/Prefabs/Missile.prefab";

        [MenuItem("Kinematics Game/Generate Missile Animations", false, 5)]
        [InitializeOnLoadMethod]
        public static void GenerateMissileAnimations()
        {
            EnsureDirectories();

            Sprite[] flyingSprites = LoadSpritesSorted("Assets/Sprites/Weapons/Missiles/Missile_1/Flying");
            Sprite[] explosionSprites = LoadSpritesSorted("Assets/Sprites/Weapons/Missiles/Missile_1/Explosion");

            if (flyingSprites == null || flyingSprites.Length == 0)
            {
                Debug.LogWarning("[MissileAnimationGenerator] No flying sprites found for Missile_1.");
                return;
            }

            // 1. Create Missile_Flying.anim (12 FPS, Looping)
            AnimationClip flyingClip = CreateOrUpdateSpriteClip(FlyingAnimPath, flyingSprites, 12f, true);

            // 2. Create Missile_Explosion.anim (15 FPS, Non-looping)
            AnimationClip explosionClip = null;
            if (explosionSprites != null && explosionSprites.Length > 0)
            {
                explosionClip = CreateOrUpdateSpriteClip(ExplosionAnimPath, explosionSprites, 15f, false);
            }

            // 3. Create Animator Controller
            AnimatorController controller = CreateOrUpdateController(ControllerPath, flyingClip, explosionClip);

            // 4. Create Explosion Prefab
            GameObject explosionPrefab = CreateExplosionPrefab(explosionSprites, explosionClip, controller);

            // 5. Update Missile.prefab with Animator and frame sequences
            UpdateMissilePrefab(controller, flyingSprites, explosionSprites, explosionPrefab);

            AssetDatabase.SaveAssets();
            Debug.Log("[MissileAnimationGenerator] Missile animations and prefab bindings updated successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Animations/Missile"))
            {
                AssetDatabase.CreateFolder("Assets/Animations", "Missile");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
        }

        private static Sprite[] LoadSpritesSorted(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
            List<Sprite> list = new List<Sprite>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null && !list.Contains(sp))
                {
                    list.Add(sp);
                }
            }

            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return list.ToArray();
        }

        private static AnimationClip CreateOrUpdateSpriteClip(string clipPath, Sprite[] sprites, float frameRate, bool loop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.frameRate = frameRate;

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / frameRate,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateOrUpdateController(string path, AnimationClip flyingClip, AnimationClip explosionClip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            if (controller.layers.Length > 0)
            {
                var sm = controller.layers[0].stateMachine;

                // Configure Flying State
                AnimatorState flyingState = null;
                foreach (var s in sm.states)
                {
                    if (s.state.name == "Flying")
                    {
                        flyingState = s.state;
                        break;
                    }
                }
                if (flyingState == null)
                {
                    flyingState = sm.AddState("Flying");
                }
                flyingState.motion = flyingClip;
                sm.defaultState = flyingState;

                // Configure Explosion State
                if (explosionClip != null)
                {
                    AnimatorState explodeState = null;
                    foreach (var s in sm.states)
                    {
                        if (s.state.name == "Explosion")
                        {
                            explodeState = s.state;
                            break;
                        }
                    }
                    if (explodeState == null)
                    {
                        explodeState = sm.AddState("Explosion");
                    }
                    explodeState.motion = explosionClip;

                    // Ensure Explode parameter exists
                    bool hasParam = false;
                    foreach (var p in controller.parameters)
                    {
                        if (p.name == "Explode") { hasParam = true; break; }
                    }
                    if (!hasParam)
                    {
                        controller.AddParameter("Explode", AnimatorControllerParameterType.Trigger);
                    }

                    // Ensure AnyState -> Explosion transition
                    bool hasTransition = false;
                    foreach (var t in sm.anyStateTransitions)
                    {
                        if (t.destinationState == explodeState) { hasTransition = true; break; }
                    }
                    if (!hasTransition)
                    {
                        var t = sm.AddAnyStateTransition(explodeState);
                        t.AddCondition(AnimatorConditionMode.If, 0, "Explode");
                        t.duration = 0f;
                        t.hasExitTime = false;
                    }
                }
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject CreateExplosionPrefab(Sprite[] explosionSprites, AnimationClip explosionClip, AnimatorController controller)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath);
            if (existing != null) return existing;

            GameObject expGo = new GameObject("Missile_Explosion");
            expGo.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            SpriteRenderer sr = expGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 15;
            if (explosionSprites != null && explosionSprites.Length > 0)
            {
                sr.sprite = explosionSprites[0];
            }

            AnimatedSpriteEffect effect = expGo.AddComponent<AnimatedSpriteEffect>();
            effect.Frames = explosionSprites;
            effect.FramesPerSecond = 15f;
            effect.Loop = false;
            effect.AutoDestroy = true;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(expGo, ExplosionPrefabPath);
            Object.DestroyImmediate(expGo);
            return prefab;
        }

        private static void UpdateMissilePrefab(AnimatorController controller, Sprite[] flyingSprites, Sprite[] explosionSprites, GameObject explosionPrefab)
        {
            GameObject missilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MissilePrefabPath);
            if (missilePrefab == null) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(missilePrefab);
            try
            {
                instance.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
                Animator anim = instance.GetComponent<Animator>();
                if (anim == null)
                {
                    anim = instance.AddComponent<Animator>();
                }
                anim.runtimeAnimatorController = controller;

                HomingMissile hm = instance.GetComponent<HomingMissile>();
                if (hm != null)
                {
                    hm.AnimatorComponent = anim;
                    hm.FlyingFrames = flyingSprites;
                    hm.ExplosionFrames = explosionSprites;
                    hm.ExplosionPrefab = explosionPrefab;
                    hm.AnimationFps = 15f;
                    hm.TargetUniformSize = 1.1f;
                }

                PrefabUtility.SaveAsPrefabAsset(instance, MissilePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
