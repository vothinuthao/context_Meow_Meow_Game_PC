using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using UnityEngine;

namespace OctoberStudio.Abilities
{
    public class DashAbility : AbilityBehavior<DashAbilityData, DashAbilityLevel>
    {
        [Header("Character-Specific Settings")]
        [SerializeField] protected CharacterDashSettings characterSettings;
        
        [Header("Effects References")]
        [SerializeField] protected ParticleSystem dashStartParticles;
        [SerializeField] protected ParticleSystem dashTrailParticles;
        [SerializeField] protected TrailRenderer dashTrail;
        [SerializeField] protected GameObject ghostPrefab;
        
        // State
        protected bool isDashing = false;
        protected float lastDashTime = -1f;
        protected Vector2 dashDirection;
        protected IEasingCoroutine dashCoroutine;
        protected PlayerBehavior player;
        
        // Audio
        protected static readonly int DASH_HASH = "Dash".GetHashCode();
        
        // Properties
        public bool CanDash => !isDashing && 
                               Time.time >= lastDashTime + AbilityLevel.DashCooldown && 
                               player.IsMovingAlowed && 
                               !player.Healthbar.IsZero;
        public bool IsDashing => isDashing;
        public float DashCooldownProgress => Mathf.Clamp01((Time.time - lastDashTime) / AbilityLevel.DashCooldown);

        protected virtual void Awake()
        {
            player = PlayerBehavior.Player;
            
            if (characterSettings == null)
            {
                characterSettings = player.GetComponent<CharacterDashSettings>();
            }
        }

        protected virtual void Start()
        {
            // Subscribe to input
            if (GameController.InputManager?.InputAsset?.Gameplay != null)
            {
                GameController.InputManager.InputAsset.Gameplay.Dash.performed += OnDashInput;
            }
        }

        protected virtual void Update()
        {
            // Update trail
            if (dashTrail != null)
            {
                dashTrail.emitting = isDashing;
            }
        }

        protected virtual void OnDashInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (CanDash)
            {
                PerformDash();
            }
        }

        public virtual void PerformDash()
        {
            if (!CanDash) return;

            // Determine dash direction
            Vector2 inputDirection = GameController.InputManager.MovementValue;
            
            if (inputDirection.magnitude < 0.1f)
            {
                dashDirection = player.LookDirection;
            }
            else
            {
                dashDirection = inputDirection.normalized;
            }

            StartDash();
        }

        protected virtual void StartDash()
        {
            isDashing = true;
            lastDashTime = Time.time;
            
            Vector3 startPosition = transform.position;
            float actualDashDistance = GetActualDashDistance();
            Vector3 targetPosition = startPosition + (Vector3)dashDirection * actualDashDistance;
            
            // Validate position against field boundaries
            targetPosition = ValidateDashTarget(startPosition, targetPosition);

            // Start dash movement
            dashCoroutine = transform.DoPosition(targetPosition, AbilityLevel.DashDuration)
                .SetEasing(EasingType.QuartOut)
                .SetOnFinish(OnDashCompleted);

            // Apply effects
            StartDashEffects();
            
            // Apply invincibility if enabled
            if (AbilityLevel.HasInvincibilityFrames)
            {
                player.SetInvincible(true);
            }

            Debug.Log($"Dash started! Distance: {actualDashDistance:F2}, Direction: {dashDirection}");
        }

        protected virtual float GetActualDashDistance()
        {
            float baseDistance = AbilityLevel.DashDistance;
            
            // Apply character-specific multiplier
            if (characterSettings != null)
            {
                baseDistance *= characterSettings.DistanceMultiplier;
            }
            
            return baseDistance;
        }

        protected virtual Vector3 ValidateDashTarget(Vector3 startPos, Vector3 targetPos)
        {
            if (StageController.FieldManager == null) return targetPos;
            
            if (StageController.FieldManager.ValidatePosition(targetPos, player.FenceOffset))
            {
                return targetPos;
            }
            
            // Find maximum valid distance
            float maxDistance = FindMaxValidDistance(startPos, dashDirection);
            return startPos + (Vector3)dashDirection * maxDistance;
        }

        protected virtual float FindMaxValidDistance(Vector3 startPos, Vector2 direction)
        {
            float maxDistance = 0f;
            float totalDistance = GetActualDashDistance();
            float step = totalDistance / 20f;
            
            for (float distance = step; distance <= totalDistance; distance += step)
            {
                Vector3 testPos = startPos + (Vector3)direction * distance;
                if (StageController.FieldManager.ValidatePosition(testPos, player.FenceOffset))
                {
                    maxDistance = distance;
                }
                else
                {
                    break;
                }
            }
            
            return Mathf.Max(maxDistance, 0.5f);
        }

        protected virtual void StartDashEffects()
        {
            // Particles
            PlayDashParticles();
            
            // Ghost trail
            if (AbilityLevel.HasGhostTrail)
            {
                CreateGhostTrail();
            }
            
            // Audio
            GameController.AudioManager?.PlaySound(DASH_HASH);
            
            // Vibration
            GameController.VibrationManager?.MediumVibration();
            
            // Screen shake
            if (AbilityLevel.HasScreenShake)
            {
                // Add screen shake if you have camera shake system
                // CameraShake.Instance?.Shake(0.1f, 0.2f);
            }
            
            // Character effects
            ApplyCharacterEffects();
        }

        protected virtual void PlayDashParticles()
        {
            if (dashStartParticles != null)
            {
                dashStartParticles.transform.position = transform.position;
                SetParticleColor(dashStartParticles, AbilityLevel.DashColor);
                dashStartParticles.Play();
            }
            
            if (dashTrailParticles != null)
            {
                dashTrailParticles.transform.position = transform.position;
                SetParticleColor(dashTrailParticles, AbilityLevel.DashColor);
                dashTrailParticles.Play();
            }
            
            if (dashTrail != null)
            {
                dashTrail.Clear();
                dashTrail.emitting = true;
                dashTrail.startColor = AbilityLevel.DashColor;
                dashTrail.endColor = AbilityLevel.DashColor.SetAlpha(0f);
            }
        }

        protected virtual void SetParticleColor(ParticleSystem particles, Color color)
        {
            var main = particles.main;
            main.startColor = color;
        }

        protected virtual void CreateGhostTrail()
        {
            if (ghostPrefab == null) return;
            
            Vector3 startPos = transform.position;
            float spacing = 0.3f;
            
            for (int i = 0; i < AbilityLevel.GhostTrailCount; i++)
            {
                Vector3 ghostPos = startPos - (Vector3)dashDirection * spacing * (i + 1);
                
                EasingManager.DoAfter(i * 0.05f, () =>
                {
                    CreateGhost(ghostPos);
                });
            }
        }

        protected virtual void CreateGhost(Vector3 position)
        {
            GameObject ghost = Instantiate(ghostPrefab, position, transform.rotation);
            SpriteRenderer ghostSprite = ghost.GetComponent<SpriteRenderer>();
            
            if (ghostSprite != null)
            {
                // ghostSprite.sprite = player.Character.SpriteRenderer.sprite;
                ghostSprite.color = AbilityLevel.DashColor.SetAlpha(0.6f);
                
                // Fade out
                ghostSprite.DoAlpha(0f, 0.4f).SetOnFinish(() =>
                {
                    if (ghost != null) Destroy(ghost);
                });
                
                // Scale down
                ghost.transform.DoLocalScale(Vector3.zero, 0.4f).SetEasing(EasingType.BackIn);
            }
        }

        protected virtual void ApplyCharacterEffects()
        {
            // if (player.Character?.SpriteRenderer != null)
            // {
            //     // Flash effect with dash color
            //     Color originalColor = player.Character.SpriteRenderer.color;
            //     Color flashColor = Color.Lerp(originalColor, AbilityLevel.DashColor, 0.7f);
            //     
            //     player.Character.SpriteRenderer.color = flashColor;
            //     
            //     EasingManager.DoAfter(AbilityLevel.DashDuration * 0.5f, () =>
            //     {
            //         if (player.Character?.SpriteRenderer != null)
            //         {
            //             player.Character.SpriteRenderer.DoColor(originalColor, 0.2f);
            //         }
            //     });
            // }
        }

        protected virtual void OnDashCompleted()
        {
            isDashing = false;
            
            // Remove invincibility after delay
            if (AbilityLevel.HasInvincibilityFrames)
            {
                EasingManager.DoAfter(AbilityLevel.InvincibilityDuration, () =>
                {
                    player.SetInvincible(false);
                });
            }
            
            // Stop effects
            StopDashEffects();
            
            Debug.Log("Dash completed!");
        }

        protected virtual void StopDashEffects()
        {
            if (dashTrailParticles != null)
            {
                dashTrailParticles.Stop();
            }
            
            if (dashTrail != null)
            {
                dashTrail.emitting = false;
            }
        }

        // Public API for UI and other systems
        public float GetDashCooldownTimeLeft()
        {
            return Mathf.Max(0f, (lastDashTime + AbilityLevel.DashCooldown) - Time.time);
        }

        public bool IsDashOnCooldown()
        {
            return Time.time < lastDashTime + AbilityLevel.DashCooldown;
        }

        public void CancelDash()
        {
            if (isDashing)
            {
                dashCoroutine?.Stop();
                OnDashCompleted();
            }
        }

        protected virtual void OnDestroy()
        {
            if (GameController.InputManager?.InputAsset?.Gameplay != null)
            {
                GameController.InputManager.InputAsset.Gameplay.Dash.performed -= OnDashInput;
            }
        }

        public override void Clear()
        {
            CancelDash();
            base.Clear();
        }
    }
}