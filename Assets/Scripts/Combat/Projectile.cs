using UnityEngine;

namespace KinematicsGame.Combat
{
    /// <summary>
    /// Represents Object C (Blaster bullet) fired by Player.
    /// Inherits from BaseProjectile with direct linear kinematics.
    /// </summary>
    [DisallowMultipleComponent]
    public class Projectile : BaseProjectile
    {
        // Inherits all movement, initialization, and viewport culling behavior from BaseProjectile
    }
}
