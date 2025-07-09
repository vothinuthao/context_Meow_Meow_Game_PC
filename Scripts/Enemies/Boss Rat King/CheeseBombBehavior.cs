using OctoberStudio;
using OctoberStudio.Easing;
using UnityEngine;
using UnityEngine.Events;

namespace  TwoSleepyCatsStudio.Enemy
{
    public class CheeseBombBehavior : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] ParticleSystem explosionParticle;
        [SerializeField] Collider2D bombCollider;
        [SerializeField] SpriteRenderer spriteRenderer;

        private static readonly int EXPLODE_TRIGGER = Animator.StringToHash("Explode");

        public event UnityAction<CheeseBombBehavior> onExploded;

        private float damage;
        private float explosionRadius;
        private bool hasExploded = false;

        public void SetData(float damage, float delay, float explosionRadius)
        {
            this.damage = damage;
            this.explosionRadius = explosionRadius;
            hasExploded = false;
            
            bombCollider.enabled = false;
            
            // Warning effect
            StartCoroutine(WarningEffect(delay));
            
            // Auto explode after delay
            EasingManager.DoAfter(delay, Explode);
        }

        private System.Collections.IEnumerator WarningEffect(float duration)
        {
            float elapsed = 0f;
            Color originalColor = spriteRenderer.color;
            
            while (elapsed < duration && !hasExploded)
            {
                float intensity = Mathf.PingPong(elapsed * 4f, 1f);
                spriteRenderer.color = Color.Lerp(originalColor, Color.red, intensity);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            spriteRenderer.color = originalColor;
        }

        public void Explode()
        {
            if (hasExploded) return;
            
            hasExploded = true;
            animator.SetTrigger(EXPLODE_TRIGGER);
            
            // Damage in radius
            if (Vector2.Distance(transform.position, PlayerBehavior.CenterPosition) <= explosionRadius)
            {
                PlayerBehavior.Player.TakeDamage(damage);
            }
            
            if (explosionParticle != null) explosionParticle.Play();
            
            // Hide visuals
            spriteRenderer.enabled = false;
            bombCollider.enabled = false;
            
            EasingManager.DoAfter(1f, () => {
                onExploded?.Invoke(this);
                gameObject.SetActive(false);
                spriteRenderer.enabled = true;
            });
        }

        public void Destroy()
        {
            hasExploded = true;
            gameObject.SetActive(false);
            spriteRenderer.enabled = true;
        }
    }
}