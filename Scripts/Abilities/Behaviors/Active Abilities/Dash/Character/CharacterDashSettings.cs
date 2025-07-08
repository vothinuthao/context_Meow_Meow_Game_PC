using UnityEngine;

namespace OctoberStudio.Abilities
{
    [CreateAssetMenu(fileName = "Character Dash Settings", menuName = "October/Characters/Dash Settings")]
    public class CharacterDashSettings : ScriptableObject
    {
        [Header("Dash Modifiers")]
        [SerializeField, Min(0.1f)] float distanceMultiplier = 1f;
        [SerializeField, Min(0.1f)] float durationMultiplier = 1f;
        [SerializeField, Min(0.1f)] float cooldownMultiplier = 1f;
        
        [Header("Character-Specific Effects")]
        [SerializeField] ParticleSystem customDashParticles;
        [SerializeField] GameObject customGhostPrefab;
        [SerializeField] AudioClip customDashSound;
        [SerializeField] Color characterDashColor = Color.cyan;
        
        [Header("Special Abilities")]
        [SerializeField] bool canDashThroughWalls = false;
        [SerializeField] bool leavesFireTrail = false;
        [SerializeField] bool healsOnDash = false;
        [SerializeField, Min(0f)] float healAmount = 5f;

        // Properties
        public float DistanceMultiplier => distanceMultiplier;
        public float DurationMultiplier => durationMultiplier;
        public float CooldownMultiplier => cooldownMultiplier;
        public ParticleSystem CustomDashParticles => customDashParticles;
        public GameObject CustomGhostPrefab => customGhostPrefab;
        public AudioClip CustomDashSound => customDashSound;
        public Color CharacterDashColor => characterDashColor;
        public bool CanDashThroughWalls => canDashThroughWalls;
        public bool LeavesFireTrail => leavesFireTrail;
        public bool HealsOnDash => healsOnDash;
        public float HealAmount => healAmount;
    }
}