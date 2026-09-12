using System.Collections.Generic;
using UnityEngine;
using KinematicsGame.Player;
using KinematicsGame.Audio;

namespace KinematicsGame.Combat
{
    /// <summary>
    /// Dispatches and resolves the exact 9 distinct gameplay effects produced by collisions
    /// between Player (Object A) and Objects X (Hazard Mine), Y (Tech Supply), Z (Gem Core).
    /// </summary>
    public static class CollisionEffectDispatcher
    {
        public static HashSet<int> TriggeredEffectIds { get; } = new HashSet<int>();

        public static void ResetTracking()
        {
            TriggeredEffectIds.Clear();
        }

        public static void ResolveCollision(InteractiveEntity entity, PlayerController player)
        {
            if (entity == null || player == null) return;

            switch (entity.Type)
            {
                case EntityType.HazardMine:
                    ResolveHazardMine(entity, player);
                    break;
                case EntityType.SupplyCrate:
                    ResolveSupplyCrate(entity, player);
                    break;
                case EntityType.GemCore:
                    ResolveGemCore(entity, player);
                    break;
                case EntityType.MiniBonus:
                    ResolveMiniBonus(entity, player);
                    break;
            }
        }

        /// <summary>
        /// Object X (Hazard Mine):
        /// - Effect 1: Explosion SFX
        /// - Effect 2: Despawn self
        /// - Effect 3: Deduct 25 Armor/HP (or absorbed by Shield)
        /// - Effect 4: -40% speed debuff (0.6x) for 3.0s
        /// </summary>
        private static void ResolveHazardMine(InteractiveEntity entity, PlayerController player)
        {
            // Effect 1: Explosion SFX
            if (AudioManager.Instance != null && entity.InteractionSfx != null)
            {
                AudioManager.Instance.PlaySfx(entity.InteractionSfx);
            }
            TriggeredEffectIds.Add(1);

            // Effect 3: Damage (25 pts, absorbed by Shield if active)
            bool absorbed = false;
            if (player.DefenseSystem != null && player.DefenseSystem.IsShieldActive)
            {
                absorbed = player.DefenseSystem.TryAbsorbDamage();
            }

            if (!absorbed && player.Stats != null)
            {
                player.Stats.TakeDamage(25);
            }
            TriggeredEffectIds.Add(3);

            // Effect 4: Speed Debuff (-40%, 0.6x for 3.0s)
            if (player.Stats != null)
            {
                player.Stats.ApplySpeedModifier(0.6f, 3.0f);
            }
            TriggeredEffectIds.Add(4);

            // Effect 2: Despawn self
            TriggeredEffectIds.Add(2);
            entity.Despawn();
        }

        /// <summary>
        /// Object Y (Tech Supply Crate):
        /// - Effect 5: Restore +50 Armor & deploy Shield if absent
        /// - Effect 6: +50% haste speed boost (1.5x) for 4.0s
        /// - Effect 7: Switch weapon to Missile mode
        /// </summary>
        private static void ResolveSupplyCrate(InteractiveEntity entity, PlayerController player)
        {
            // Effect 5: Restore +50 Armor & grant Shield Barrier if depleted
            if (player.Stats != null)
            {
                player.Stats.RestoreArmor(50);
            }
            if (player.DefenseSystem != null && !player.DefenseSystem.IsShieldActive)
            {
                player.DefenseSystem.ActivateShield(3, 8.0f);
            }
            TriggeredEffectIds.Add(5);

            // Effect 6: Speed Buff (+50%, 1.5x for 4.0s)
            if (player.Stats != null)
            {
                player.Stats.ApplySpeedModifier(1.5f, 4.0f);
            }
            TriggeredEffectIds.Add(6);

            // Effect 7: Weapon Upgrade (switch to Heavy Missile mode)
            if (player.CombatSystem != null)
            {
                player.CombatSystem.SelectWeapon(WeaponType.Missile);
            }
            TriggeredEffectIds.Add(7);

            if (AudioManager.Instance != null && entity.InteractionSfx != null)
            {
                AudioManager.Instance.PlaySfx(entity.InteractionSfx);
            }

            entity.Despawn();
        }

        /// <summary>
        /// Object Z (Gem Core / Bounty):
        /// - Effect 8: Currency windfall (+50 Gold, +5 Diamonds)
        /// - Effect 9: Spawn 3 mini-bonus collectible pickups nearby
        /// </summary>
        private static void ResolveGemCore(InteractiveEntity entity, PlayerController player)
        {
            // Effect 8: Currency Windfall (+50 Gold, +5 Diamonds)
            if (player.Stats != null)
            {
                player.Stats.AddCurrency(50, 5);
            }
            if (AudioManager.Instance != null && entity.InteractionSfx != null)
            {
                AudioManager.Instance.PlaySfx(entity.InteractionSfx);
            }
            TriggeredEffectIds.Add(8);

            // Effect 9: Subsidiary Rewards (3 mini-bonus pickups scattered nearby)
            Vector3 origin = entity.transform.position;
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(0.5f, 0.7f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0f, -0.8f, 0f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject miniGo = null;
                if (entity.MiniBonusPrefab != null)
                {
                    miniGo = Object.Instantiate(entity.MiniBonusPrefab, origin + offsets[i], Quaternion.identity);
                }
                else
                {
                    miniGo = new GameObject($"MiniBonus_{i}");
                    miniGo.transform.position = origin + offsets[i];
                    SpriteRenderer sr = miniGo.AddComponent<SpriteRenderer>();
                    SpriteRenderer parentSr = entity.GetComponent<SpriteRenderer>();
                    if (parentSr != null)
                    {
                        sr.sprite = parentSr.sprite;
                        sr.sortingOrder = parentSr.sortingOrder;
                    }
                    CircleCollider2D cc = miniGo.AddComponent<CircleCollider2D>();
                    cc.isTrigger = true;
                    InteractiveEntity ie = miniGo.AddComponent<InteractiveEntity>();
                    ie.Type = EntityType.MiniBonus;
                    ie.TargetSize = 0.5f;
                    ie.ApplyTargetSize();
                }
            }
            TriggeredEffectIds.Add(9);

            entity.Despawn();
        }

        private static void ResolveMiniBonus(InteractiveEntity entity, PlayerController player)
        {
            if (player.Stats != null)
            {
                player.Stats.AddCurrency(10, 0);
            }

            if (AudioManager.Instance != null && entity.InteractionSfx != null)
            {
                AudioManager.Instance.PlaySfx(entity.InteractionSfx);
            }

            entity.Despawn();
        }
    }
}
