using OctoberStudio.Easing;
using OctoberStudio.Pool;
using System.Collections;
using System.Collections.Generic;
using OctoberStudio;
using OctoberStudio.Enemy;
using OctoberStudio.Extensions;
using UnityEngine;

namespace TwoSleepyCatsStudio.Enemy
{
    public class RatKingBehavior : EnemyBehavior
    {
        private static readonly int IS_BURROWED_BOOL = Animator.StringToHash("Is Burrowed");
        private static readonly int IS_CHARGING_BOOL = Animator.StringToHash("Is Charging");
        private static readonly int DASH_TRIGGER = Animator.StringToHash("Dash");
        private static readonly int SQUEAK_TRIGGER = Animator.StringToHash("Squeak");

        [SerializeField] Animator animator;

        [Header("Crown System")]
        [SerializeField] GameObject crownObject;
        [SerializeField] GameObject crownProjectilePrefab;
        [SerializeField] float crownVulnerabilityMultiplier = 1.25f;
        [SerializeField] float crownReturnTime = 5f;

        [Header("Particles")]
        [SerializeField] ParticleSystem burrowingParticle;
        [SerializeField] ParticleSystem burrowingTrail;
        [SerializeField] ParticleSystem squeakParticle;
        [SerializeField] ParticleSystem crownSparkle;

        [Header("Phase 1: Aggressive Raider")]
        [SerializeField] GameObject cheeseBombPrefab;
        [SerializeField] float cheeseBombDamage = 8f;
        [SerializeField] int cheeseBombCount = 4;
        [SerializeField] float dashSpeed = 15f;
        [SerializeField] float dashDamage = 12f;

        [Header("Phase 2: Defensive Burrow")]
        [SerializeField] float burrowedDuration = 3f;
        [SerializeField] float burrowMoveSpeed = 2f;
        [SerializeField] GameObject foodSpotPrefab;
        [SerializeField] float foodHealPercent = 5f;
        [SerializeField] float tailWhipRadius = 2.5f;
        [SerializeField] float tailWhipDamage = 10f;

        [Header("Phase 3: Desperate King")]
        [SerializeField] EnemyType ratMinionType = EnemyType.Slime;
        [SerializeField] int ratArmyCount = 12;
        [SerializeField] GameObject squeakWavePrefab;
        [SerializeField] float squeakDamage = 15f;
        [SerializeField] int squeakWaveCount = 3;

        // Pools
        private PoolComponent<CheeseBombBehavior> cheeseBombPool;
        private PoolComponent<FoodSpotBehavior> foodSpotPool;
        private PoolComponent<SimpleEnemyProjectileBehavior> crownPool;
        private PoolComponent<SimpleEnemyProjectileBehavior> squeakWavePool;

        // State
        private List<CheeseBombBehavior> activeBombs = new List<CheeseBombBehavior>();
        private List<FoodSpotBehavior> activeFoodSpots = new List<FoodSpotBehavior>();
        private List<EnemyBehavior> ratMinions = new List<EnemyBehavior>();
        
        private bool hasCrown = true;
        private bool isUnderground = false;
        private int currentPhase = 1;
        private Coroutine behaviorCoroutine;
        private Coroutine crownReturnCoroutine;

        // Phase thresholds
        private float phase2Threshold = 0.7f;
        private float phase3Threshold = 0.3f;

        protected override void Awake()
        {
            base.Awake();

            cheeseBombPool = new PoolComponent<CheeseBombBehavior>(cheeseBombPrefab, 10);
            foodSpotPool = new PoolComponent<FoodSpotBehavior>(foodSpotPrefab, 3);
            crownPool = new PoolComponent<SimpleEnemyProjectileBehavior>(crownProjectilePrefab, 1);
            squeakWavePool = new PoolComponent<SimpleEnemyProjectileBehavior>(squeakWavePrefab, 5);
        }

        public override void Play()
        {
            base.Play();
            
            currentPhase = 1;
            hasCrown = true;
            isUnderground = false;
            
            crownObject.SetActive(true);
            if (crownSparkle != null) crownSparkle.Play();

            behaviorCoroutine = StartCoroutine(BehaviorCoroutine());
        }

        public override void TakeDamage(float damage)
        {
            if (!hasCrown)
            {
                damage *= crownVulnerabilityMultiplier;
            }

            base.TakeDamage(damage);
            float hpPercent = HP / MaxHP;
            
            if (currentPhase == 1 && hpPercent <= phase2Threshold)
            {
                currentPhase = 2;
                StartCoroutine(TransitionToPhase2());
            }
            else if (currentPhase == 2 && hpPercent <= phase3Threshold)
            {
                currentPhase = 3;
                StartCoroutine(TransitionToPhase3());
            }
        }

        private IEnumerator BehaviorCoroutine()
        {
            while (IsAlive)
            {
                switch (currentPhase)
                {
                    case 1:
                        yield return Phase1Behavior();
                        break;
                    case 2:
                        yield return Phase2Behavior();
                        break;
                    case 3:
                        yield return Phase3Behavior();
                        break;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        #region Phase 1: Aggressive Raider
        private IEnumerator Phase1Behavior()
        {
            // Cheese Bomb Barrage
            yield return CheeseBombBarrage();
            yield return new WaitForSeconds(2f);

            // Royal Dash
            yield return RoyalDash();
            yield return new WaitForSeconds(2f);

            // Minion Summon
            yield return SummonRatMinions(4);
            yield return new WaitForSeconds(3f);
        }

        private IEnumerator CheeseBombBarrage()
        {
            animator.SetBool(IS_CHARGING_BOOL, true);
            
            for (int i = 0; i < cheeseBombCount; i++)
            {
                var bomb = cheeseBombPool.GetEntity();
                
                // Random position around player
                Vector2 playerPos = PlayerBehavior.CenterPosition;
                Vector2 randomOffset = Random.insideUnitCircle * 3f;
                Vector2 bombPos = playerPos + randomOffset;
                
                bomb.transform.position = bombPos;
                bomb.SetData(cheeseBombDamage * StageController.Stage.EnemyDamage, 2f, 1.5f);
                bomb.onExploded += OnBombExploded;
                activeBombs.Add(bomb);

                yield return new WaitForSeconds(0.3f);
            }
            
            animator.SetBool(IS_CHARGING_BOOL, false);
        }

        private IEnumerator RoyalDash()
        {
            animator.SetTrigger(DASH_TRIGGER);
            
            Vector2 dashDirection = (PlayerBehavior.CenterPosition - transform.position.XY()).normalized;
            Vector2 targetPosition = (Vector2)transform.position + dashDirection * 8f;
            if (!StageController.FieldManager.ValidatePosition(targetPosition, Vector2.one))
            {
                targetPosition = StageController.FieldManager.Fence.GetRandomPointInside(1f);
            }

            IsMoving = false;
            var dashCoroutine = transform.DoPosition(targetPosition, 0.8f).SetEasing(EasingType.QuartOut);
            
            yield return new WaitForSeconds(0.8f);
            IsMoving = true;
        }

        private IEnumerator SummonRatMinions(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 spawnPos = StageController.FieldManager.Fence.GetRandomPointInside(0.5f);
                var minion = StageController.EnemiesSpawner.Spawn(ratMinionType, spawnPos, OnRatMinionDied);
                
                if (minion != null)
                {
                    ratMinions.Add(minion);
                }
                
                yield return new WaitForSeconds(0.5f);
            }
        }
        #endregion

        #region Phase 2: Defensive Burrow
        private IEnumerator TransitionToPhase2()
        {
            // Spawn food spots
            SpawnFoodSpots();
            yield return null;
        }

        private IEnumerator Phase2Behavior()
        {
            yield return UndergroundNetwork();
            yield return new WaitForSeconds(1f);
            if (activeFoodSpots.Count > 0 && HP < MaxHP * 0.5f)
            {
                yield return FoodTheftHealing();
                yield return new WaitForSeconds(1f);
            }

            // Tail Whip
            yield return TailWhipSpin();
            yield return new WaitForSeconds(2f);
        }

        private IEnumerator UndergroundNetwork()
        {
            animator.SetBool(IS_BURROWED_BOOL, true);
            IsMoving = false;
            isUnderground = true;
            
            if (burrowingParticle != null) burrowingParticle.Play();
            
            yield return new WaitForSeconds(0.7f);
            
            // Move underground
            if (burrowingTrail != null) burrowingTrail.Play();
            
            for (int i = 0; i < 3; i++)
            {
                var warningCircle = StageController.PoolsManager.GetEntity<WarningCircleBehavior>("Warning Circle");
                warningCircle.transform.position = PlayerBehavior.CenterPosition;
                warningCircle.Play(1f, 0.3f, 1f, null);
                
                yield return new WaitForSeconds(1f);
                transform.position = warningCircle.transform.position;
                yield return new WaitForSeconds(0.5f);
            }
            
            // Surface
            animator.SetBool(IS_BURROWED_BOOL, false);
            isUnderground = false;
            IsMoving = true;
            
            if (burrowingTrail != null) burrowingTrail.Stop();
        }

        private void SpawnFoodSpots()
        {
            Vector2[] positions = {
                new Vector2(-5f, 3f),
                new Vector2(5f, 3f),
                new Vector2(0f, -4f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var foodSpot = foodSpotPool.GetEntity();
                foodSpot.transform.position = positions[i];
                foodSpot.SetData(foodHealPercent);
                foodSpot.onConsumed += OnFoodConsumed;
                activeFoodSpots.Add(foodSpot);
            }
        }

        private IEnumerator FoodTheftHealing()
        {
            if (activeFoodSpots.Count == 0) yield break;
            
            var nearestFood = GetNearestFoodSpot();
            if (nearestFood == null) yield break;

            IsMovingToCustomPoint = true;
            CustomPoint = nearestFood.transform.position;
            
            yield return new WaitUntil(() => 
                Vector2.Distance(transform.position, nearestFood.transform.position) < 0.5f
            );
            
            // Consume food
            nearestFood.Consume();
            float healAmount = MaxHP * (foodHealPercent / 100f);
            HP = Mathf.Min(HP + healAmount, MaxHP);
            
            IsMovingToCustomPoint = false;
        }

        private IEnumerator TailWhipSpin()
        {
            animator.SetBool(IS_CHARGING_BOOL, true);
            IsMoving = false;
            
            yield return new WaitForSeconds(1f);
            
            // Damage in radius
            var enemies = StageController.EnemiesSpawner.GetEnemiesInRadius(transform.position, tailWhipRadius);
            if (Vector2.Distance(transform.position, PlayerBehavior.CenterPosition) <= tailWhipRadius)
            {
                PlayerBehavior.Player.TakeDamage(tailWhipDamage * StageController.Stage.EnemyDamage);
            }
            
            yield return new WaitForSeconds(2f);
            
            animator.SetBool(IS_CHARGING_BOOL, false);
            IsMoving = true;
        }
        #endregion

        #region Phase 3: Desperate King
        private IEnumerator TransitionToPhase3()
        {
            // Clear remaining food spots
            foreach (var food in activeFoodSpots)
            {
                food.Destroy();
            }
            activeFoodSpots.Clear();
            yield return null;
        }

        private IEnumerator Phase3Behavior()
        {
            yield return RatArmyInvasion();
            yield return new WaitForSeconds(2f);
            yield return SupersonicSqueak();
            yield return new WaitForSeconds(1f);

            if (hasCrown)
            {
                yield return CrownBoomerang();
                yield return new WaitForSeconds(2f);
            }
        }

        private IEnumerator RatArmyInvasion()
        {
            // Dramatic effect
            if (squeakParticle != null) squeakParticle.Play();
            
            for (int i = 0; i < ratArmyCount; i++)
            {
                Vector2 spawnPos = CameraManager.GetRandomPointOutsideCamera(0.5f);
                var minion = StageController.EnemiesSpawner.Spawn(ratMinionType, spawnPos, OnRatMinionDied);
                
                if (minion != null)
                {
                    ratMinions.Add(minion);
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator SupersonicSqueak()
        {
            animator.SetTrigger(SQUEAK_TRIGGER);
            IsMoving = false;
            
            yield return new WaitForSeconds(1.5f); // Charge up
            
            Vector2 playerDirection = (PlayerBehavior.CenterPosition - transform.position.XY()).normalized;
            
            for (int i = 0; i < squeakWaveCount; i++)
            {
                var wave = squeakWavePool.GetEntity();
                float angleOffset = (i - 1) * 15f; // -15, 0, 15 degrees
                Vector2 waveDirection = Quaternion.Euler(0, 0, angleOffset) * playerDirection;
                
                wave.Init(transform.position, waveDirection);
                wave.Damage = squeakDamage * StageController.Stage.EnemyDamage;
                wave.Speed = 10f;
                
                yield return new WaitForSeconds(0.3f);
            }
            
            IsMoving = true;
        }

        private IEnumerator CrownBoomerang()
        {
            if (!hasCrown) yield break;
            
            hasCrown = false;
            crownObject.SetActive(false);
            if (crownSparkle != null) crownSparkle.Stop();
            
            var crown = crownPool.GetEntity();
            crown.transform.position = transform.position;
            crown.Damage = 8f * StageController.Stage.EnemyDamage;
            
            // Crown follows player
            StartCoroutine(CrownFollowPlayer(crown));
            
            // Return crown after time
            if (crownReturnCoroutine != null) StopCoroutine(crownReturnCoroutine);
            crownReturnCoroutine = StartCoroutine(ReturnCrown(crownReturnTime));
            
            yield return null;
        }

        private IEnumerator CrownFollowPlayer(SimpleEnemyProjectileBehavior crown)
        {
            float followTime = 3f;
            float elapsed = 0f;
            
            while (elapsed < followTime && crown.gameObject.activeInHierarchy)
            {
                Vector2 direction = (PlayerBehavior.CenterPosition - crown.transform.position.XY()).normalized;
                crown.transform.position += (Vector3)direction * 5f * Time.deltaTime;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Return to boss
            while (crown.gameObject.activeInHierarchy)
            {
                Vector2 direction = (transform.position - crown.transform.position).normalized;
                crown.transform.position += (Vector3)direction * 8f * Time.deltaTime;
                
                if (Vector2.Distance(crown.transform.position, transform.position) < 0.5f)
                {
                    crown.gameObject.SetActive(false);
                    ReturnCrownToBoss();
                    break;
                }
                
                yield return null;
            }
        }

        private IEnumerator ReturnCrown(float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnCrownToBoss();
        }

        private void ReturnCrownToBoss()
        {
            hasCrown = true;
            crownObject.SetActive(true);
            if (crownSparkle != null) crownSparkle.Play();
            
            if (crownReturnCoroutine != null)
            {
                StopCoroutine(crownReturnCoroutine);
                crownReturnCoroutine = null;
            }
        }
        #endregion

        #region Event Handlers
        private void OnBombExploded(CheeseBombBehavior bomb)
        {
            bomb.onExploded -= OnBombExploded;
            activeBombs.Remove(bomb);
        }

        private void OnFoodConsumed(FoodSpotBehavior food)
        {
            food.onConsumed -= OnFoodConsumed;
            activeFoodSpots.Remove(food);
        }

        private void OnRatMinionDied(EnemyBehavior rat)
        {
            rat.onEnemyDied -= OnRatMinionDied;
            ratMinions.Remove(rat);
        }

        private FoodSpotBehavior GetNearestFoodSpot()
        {
            FoodSpotBehavior nearest = null;
            float minDistance = float.MaxValue;
            
            foreach (var food in activeFoodSpots)
            {
                float distance = Vector2.Distance(transform.position, food.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = food;
                }
            }
            
            return nearest;
        }
        #endregion

        #region Animation Events
        public void OnDashHit()
        {
            // Called by animation event during dash
            if (Vector2.Distance(transform.position, PlayerBehavior.CenterPosition) < 2f)
            {
                PlayerBehavior.Player.TakeDamage(dashDamage * StageController.Stage.EnemyDamage);
            }
        }

        public void OnSqueakWave()
        {
            // Called by animation event for squeak attack
            if (squeakParticle != null) squeakParticle.Play();
        }
        #endregion

        protected override void Die(bool flash)
        {
            if (behaviorCoroutine != null) StopCoroutine(behaviorCoroutine);
            if (crownReturnCoroutine != null) StopCoroutine(crownReturnCoroutine);

            // Clean up active objects
            foreach (var bomb in activeBombs)
            {
                bomb.onExploded -= OnBombExploded;
                bomb.Destroy();
            }
            activeBombs.Clear();

            foreach (var food in activeFoodSpots)
            {
                food.onConsumed -= OnFoodConsumed;
                food.Destroy();
            }
            activeFoodSpots.Clear();

            foreach (var rat in ratMinions)
            {
                rat.onEnemyDied -= OnRatMinionDied;
                rat.Kill();
            }
            ratMinions.Clear();

            base.Die(flash);
        }
    }
}