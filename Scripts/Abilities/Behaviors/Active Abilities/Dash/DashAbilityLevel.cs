using OctoberStudio.Abilities;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    [System.Serializable]
    public class DashAbilityLevel : AbilityLevel
    {
        [Header("Dash Parameters")]
        [SerializeField, Min(1f)] float dashDistance = 3f;
        [SerializeField, Min(0.1f)] float dashDuration = 0.2f;
        [SerializeField, Min(0.1f)] float dashCooldown = 1f;
        
        [Header("Dash Effects")]
        [SerializeField] bool dashThroughEnemies = false;
        [SerializeField] bool hasInvincibilityFrames = false;
        [SerializeField, Min(0f)] float invincibilityDuration = 0.1f;
        
        [Header("Visual Effects")]
        [SerializeField] bool hasGhostTrail = false;
        [SerializeField] int ghostTrailCount = 3;
        [SerializeField] bool hasScreenShake = false;
        [SerializeField] Color dashColor = Color.cyan;

        // Properties
        public float DashDistance => dashDistance;
        public float DashDuration => dashDuration;
        public float DashCooldown => dashCooldown;
        public bool DashThroughEnemies => dashThroughEnemies;
        public bool HasInvincibilityFrames => hasInvincibilityFrames;
        public float InvincibilityDuration => invincibilityDuration;
        public bool HasGhostTrail => hasGhostTrail;
        public int GhostTrailCount => ghostTrailCount;
        public bool HasScreenShake => hasScreenShake;
        public Color DashColor => dashColor;
    }
}