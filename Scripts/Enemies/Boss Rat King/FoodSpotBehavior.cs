using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.Enemy
{
    public class FoodSpotBehavior : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] ParticleSystem glowParticle;
        [SerializeField] Collider2D spotCollider;

        public event UnityAction<FoodSpotBehavior> onConsumed;

        private float healPercent;
        private int hitsToDestroy = 2;
        private int currentHits = 0;

        public void SetData(float healPercent)
        {
            this.healPercent = healPercent;
            currentHits = 0;
            
            if (glowParticle != null) glowParticle.Play();
            spriteRenderer.enabled = true;
            spotCollider.enabled = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Player can destroy food spots
            if (other.GetComponent<ProjectileBehavior>() != null)
            {
                TakeHit();
            }
        }

        private void TakeHit()
        {
            currentHits++;
            
            // Flash effect
            spriteRenderer.color = Color.red;
            
            if (currentHits >= hitsToDestroy)
            {
                Destroy();
            }
        }

        public void Consume()
        {
            if (glowParticle != null) glowParticle.Stop();
            
            onConsumed?.Invoke(this);
            gameObject.SetActive(false);
        }

        public void Destroy()
        {
            if (glowParticle != null) glowParticle.Stop();
            
            spriteRenderer.enabled = false;
            spotCollider.enabled = false;
            gameObject.SetActive(false);
        }
    }
}