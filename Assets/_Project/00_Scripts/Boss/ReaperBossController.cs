using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrandedRoguelike
{
    public enum ReaperAttackType
    {
        None,
        LeftHandSlam, RightHandSlam,
        BothHandSlam,
        Laser,
        BlackSpell,
        LeftSwipe, RightSwipe,
        Rage
    }

    [RequireComponent(typeof(EnemyHealth))]
    public sealed class ReaperBossController : MonoBehaviour
    {
        public ReaperAttackType CurrentAttackType {  get; private set; }
        [Header("Core")]
        [SerializeField] private ReaperReferenceSettings references;
        [SerializeField] private ReaperAnimatorSettings animators;
        [SerializeField] private ReaperRoomSettings room;
        [SerializeField] private ReaperWarningSettings warning;

        [Header("Flow")]
        [SerializeField] private ReaperBossFlowSettings flow;
        [SerializeField] private ReaperPhaseRoarSettings phaseRoar;

        [Header("Patterns")]
        [SerializeField] private ReaperShockwaveSettings shockwave;
        [SerializeField] private ReaperLaserSettings laser;
        [SerializeField] private ReaperSweepSettings sweep;
        [SerializeField] private ReaperBlackSpellSettings blackSpell;
        [SerializeField] private ReaperFinalRageSettings finalRage;

        private EnemyHealth health;
        private Coroutine patternRoutine;
        private bool blackSpellActive;
        private bool finalRageStarted;

        private Coroutine laserRoutine;
        private bool isLaserPatternRunning;

        private Coroutine blackSpellRoutine;
        private bool isBlackSpellCasting;
        private bool deathStarted;

        public void ConfigureSurvivorEncounter(Transform newTarget)
        {
            references.target = newTarget;
            room.roomCenter = null;
            deathStarted = false;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();

            if (animators.bodyAnim == null)
            {
                animators.bodyAnim = GetComponentInChildren<Animator>();
            }
        }

        private void OnEnable()
        {
            FindTargetIfNeeded();

            if (flow.autoStartPatterns)
            {
                patternRoutine = StartCoroutine(BossPatternLoop());
            }
        }

        private void OnDisable()
        {
            if (patternRoutine != null)
            {
                StopCoroutine(patternRoutine);
                patternRoutine = null;
            }
        }

        private void Update()
        {
            if (health != null && health.isDead)
            {
                Death();
                return;
            }

            CheckPhaseTransitions();
        }


        private void Death()
        {
            if (deathStarted) return;
            deathStarted = true;
            CurrentAttackType = ReaperAttackType.None;

            StopAllCoroutines();

            animators.bodyAnim.SetTrigger("Die");
            animators.leftHandAnim.SetTrigger("Die");
            animators.rightHandAnim.SetTrigger("Die");
        }

        [ContextMenu("Boss Attack/Left Hand Line Shockwave")]
        public void Anim_LeftHandSlamShockwave()
        {
            Vector2 startPosition = HandPosition(references.leftHandSlamPoint);
            StartCoroutine(WarningThenLineShockwaveRoutine(startPosition, DirectionToTargetFrom(startPosition)));
        }

        [ContextMenu("Boss Attack/Right Hand Line Shockwave")]
        public void Anim_RightHandSlamShockwave()
        {
            Vector2 startPosition = HandPosition(references.rightHandSlamPoint);
            StartCoroutine(WarningThenLineShockwaveRoutine(startPosition, DirectionToTargetFrom(startPosition)));
        }

        [ContextMenu("Boss Attack/Double Hand Wide Shockwave")]
        public void Anim_DoubleHandSlamShockwave()
        {
            StartCoroutine(WarningThenWideShockwaveRoutine(transform.position));
        }

        [ContextMenu("Boss Attack/Laser Ground Pattern")]
        public void Anim_DoubleHandLaserGroundPattern()
        {
            if (isLaserPatternRunning) return;

            laserRoutine = StartCoroutine(LaserGroundPatternRoutine());
        }

        [ContextMenu("Boss Attack/Sweep Lower Left To Right")]
        public void Anim_SweepLowerLeftToRight()
        {
            StartCoroutine(WarningThenSweepRowsRoutine(true, true));
        }

        [ContextMenu("Boss Attack/Sweep Upper Right To Left")]
        public void Anim_SweepUpperRightToLeft()
        {
            StartCoroutine(WarningThenSweepRowsRoutine(false, false));
        }

        [ContextMenu("Boss Attack/Black Spell")]
        public void Anim_BlackSpell()
        {
            ActivateBlackSpell();
        }

        [ContextMenu("Boss Attack/Final Rage")]
        public void Anim_FinalRage()
        {
            if (!finalRageStarted)
            {
                StartCoroutine(FinalRageRoutine());
            }
        }

        private void LeftHandSlam_Animation_Control()
        {
            CurrentAttackType = ReaperAttackType.LeftHandSlam;

            animators.bodyAnim.SetTrigger("HandSlam");
            animators.leftHandAnim.SetTrigger("HandSlam");
        }

        private void RightHandSlam_Animation_Control()
        {
            CurrentAttackType = ReaperAttackType.RightHandSlam;

            animators.bodyAnim.SetTrigger("HandSlam");
            animators.rightHandAnim.SetTrigger("HandSlam");
        }

        private void BothHandSlam_Animation_Control()
        {
            CurrentAttackType = ReaperAttackType.BothHandSlam;

            animators.bodyAnim.SetTrigger("HandSlam");
            animators.leftHandAnim.SetTrigger("HandSlam");
            animators.rightHandAnim.SetTrigger("HandSlam");
        }

        private void DoubleHandLaser_Animation_Control()
        {
            CurrentAttackType = ReaperAttackType.Laser;

            animators.bodyAnim.SetBool("IsLaser", true);
            animators.leftHandAnim.SetBool("IsLaser", true);
            animators.rightHandAnim.SetBool("IsLaser", true);

            animators.bodyAnim.SetTrigger("Laser");
            animators.leftHandAnim.SetTrigger("Laser");
            animators.rightHandAnim.SetTrigger("Laser");

            if(!isLaserPatternRunning)
            {
                laserRoutine = StartCoroutine(LaserGroundPatternRoutine());
            }
        }

        private void LeftHandSwipe_Animation_Control()
        {
            CurrentAttackType = ReaperAttackType.LeftSwipe;
            animators.bodyAnim.SetTrigger("HandSlam");
            animators.leftHandAnim.SetTrigger("SlamSwipe");
        }

        private void RightHandSwipe_Animation_Control()
        {
            CurrentAttackType = ReaperAttackType.RightSwipe;
            animators.bodyAnim.SetTrigger("HandSlam");
            animators.rightHandAnim.SetTrigger("SlamSwipe");
        }

        private void Rage_Animation_Control()
        {
            CurrentAttackType = ReaperAttackType.Rage;
            animators.bodyAnim.SetTrigger("Rage");
        }


        private IEnumerator BossPatternLoop()
        {
            yield return new WaitForSeconds(flow.patternDelay);

            while (enabled && !finalRageStarted)
            {
                LeftHandSlam_Animation_Control();
                yield return new WaitForSeconds(flow.patternDelay);

                RightHandSlam_Animation_Control();
                yield return new WaitForSeconds(flow.patternDelay);

                BothHandSlam_Animation_Control();
                yield return new WaitForSeconds(flow.patternDelay);

                DoubleHandLaser_Animation_Control();
                yield return new WaitForSeconds(laser.laserDuration + flow.patternDelay);

                LeftHandSwipe_Animation_Control();
                yield return new WaitForSeconds(flow.patternDelay);

                RightHandSwipe_Animation_Control();
                yield return new WaitForSeconds(flow.patternDelay);
            }

            patternRoutine = null;
        }

        private IEnumerator WarningThenLineShockwaveRoutine(Vector2 startPosition, Vector2 direction)
        {
            direction = direction == Vector2.zero ? Vector2.down : direction.normalized;

            ShowLineAttackWarning(
                startPosition,
                direction,
                shockwave.shockwaveDistance,
                shockwave.lineShockwaveWarningWidth,
                shockwave.handSlamAttackWarningTime);

            if (shockwave.handSlamAttackWarningTime > 0f)
            {
                yield return new WaitForSeconds(shockwave.handSlamAttackWarningTime);
            }

            yield return LineShockwaveRoutine(startPosition, direction, 0f);
        }

        private IEnumerator WarningThenWideShockwaveRoutine(Vector2 center)
        {
            float warningRadius = shockwave.wideShockwaveRings * shockwave.wideShockwaveRingSpacing + shockwave.shockwaveRadius;
            EnemyAttackWarning.ShowCircle(center, warningRadius, shockwave.handSlamAttackWarningTime, warning.circleWarningSprite);

            if (shockwave.handSlamAttackWarningTime > 0f)
            {
                yield return new WaitForSeconds(shockwave.handSlamAttackWarningTime);
            }

            yield return WideShockwaveRoutine(center, 0f);
        }

        private IEnumerator LineShockwaveRoutine(Vector2 startPosition, Vector2 direction, float burstWarningTime)
        {
            direction = direction == Vector2.zero ? Vector2.down : direction.normalized;
            int count = Mathf.Max(1, Mathf.CeilToInt(shockwave.shockwaveDistance / shockwave.shockwaveSpacing));

            for (int i = 0; i < count; i++)
            {
                Vector2 position = startPosition + direction * (shockwave.shockwaveSpacing * i);
                SpawnShockwaveBurst(position, shockwave.shockwaveRadius, shockwave.shockwaveDamage, burstWarningTime, startPosition);

                if (shockwave.shockwaveStepDelay > 0f)
                {
                    yield return new WaitForSeconds(shockwave.shockwaveStepDelay);
                }
            }
        }

        private IEnumerator WideShockwaveRoutine(Vector2 center, float burstWarningTime)
        {
            for (int ring = 1; ring <= shockwave.wideShockwaveRings; ring++)
            {
                float radius = shockwave.wideShockwaveRingSpacing * ring;
                int burstCount = Mathf.Max(4, shockwave.wideShockwaveBurstsPerRing + ring * 2);

                for (int i = 0; i < burstCount; i++)
                {
                    float angle = 360f / burstCount * i;
                    Vector2 position = center + Rotate(Vector2.right, angle) * radius;
                    SpawnShockwaveBurst(position, shockwave.shockwaveRadius, shockwave.shockwaveDamage, burstWarningTime, center);
                }

                if (shockwave.shockwaveStepDelay > 0f)
                {
                    yield return new WaitForSeconds(shockwave.shockwaveStepDelay * 2f);
                }
            }
        }

        private IEnumerator LaserGroundPatternRoutine()
        {
            isLaserPatternRunning = true;

            float elapsed = 0f;
            float angle = 0f;
            float leftTimer = 0f;
            float rightTimer = 0f;

            while (elapsed < laser.laserDuration)
            {
                float dt = Time.deltaTime;
                leftTimer += dt;
                rightTimer += dt;

                if (leftTimer >= laser.left.fireInterval)
                {
                    leftTimer = 0f;

                    Vector2 leftSpawnPoint = HandPosition(references.leftHandSlamPoint);

                    for (int i = 0; i < laser.left.spiralBranches; i++)
                    {
                        float branchAngle = angle + 360f / laser.left.spiralBranches * i;

                        FireBullet(
                            leftSpawnPoint, 
                            Rotate(Vector2.right, branchAngle),
                            laser.left.bulletSpeed,
                            laser.left.bulletDamage,
                            laser.left.bulletLifeTime);
                    }

                    angle += laser.left.spiralAngularSpeed * laser.left.fireInterval;

                }
                if (rightTimer >= laser.right.aimInterval)
                {
                    rightTimer = 0f;

                    Vector2 rightSpawnPoint = HandPosition(references.rightHandSlamPoint);
                    Vector2 aimDirection = DirectionToTargetFrom(rightSpawnPoint);

                    FireBullet(
                        rightSpawnPoint,
                        aimDirection,
                        laser.right.bulletSpeed,
                        laser.right.bulletDamage,
                        laser.right.bulletLifeTime);                    

                }
                elapsed += dt;
                yield return null;
            }

            animators.bodyAnim.SetBool("IsLaser", false);
            animators.leftHandAnim.SetBool("IsLaser", false);
            animators.rightHandAnim.SetBool("IsLaser", false);

            isLaserPatternRunning = false;
            laserRoutine = null;
        }

        private IEnumerator WarningThenSweepRowsRoutine(bool lowerHalf, bool leftToRight)
        {
            ShowSweepAttackWarning(lowerHalf, sweep.handSweepAttackWarningTime);

            if (sweep.handSweepAttackWarningTime > 0f)
            {
                yield return new WaitForSeconds(sweep.handSweepAttackWarningTime);
            }

            yield return SweepRowsRoutine(lowerHalf, leftToRight);
        }

        private IEnumerator SweepRowsRoutine(bool lowerHalf, bool leftToRight)
        {
            Rect room = RoomRect();

            float yMin = lowerHalf ? room.yMin : room.center.y;
            float yMax = lowerHalf ? room.center.y : room.yMax;

            float startX = leftToRight ? room.xMin : room.xMax;
            Vector2 direction = leftToRight ? Vector2.right : Vector2.left;

            int columnCount = Mathf.Max(
                1, 
                Mathf.CeilToInt(room.width / Mathf.Max(0.01f, sweep.sweepBulletSpeed * sweep.sweepStepInterval))
                );

            for (int column = 0; column < columnCount; column++)
            {
                for (int row = 0; row < sweep.sweepRows; row++)
                {
                    float t = sweep.sweepRows == 1 ? 0.5f : row / (float)(sweep.sweepRows - 1);
                    float y = Mathf.Lerp(yMin, yMax, t);

                    FireBullet(
                        new Vector2(startX, y),
                        direction, 
                        sweep.sweepBulletSpeed, 
                        sweep.sweepBulletDamage, 
                        sweep.sweepBulletLifeTime);
                }

                yield return new WaitForSeconds(sweep.sweepStepInterval);
            }
        }

        private IEnumerator FinalRageRoutine()
        {
            finalRageStarted = true;

            CurrentAttackType = ReaperAttackType.Rage;

            Rage_Animation_Control();
            PlayPhaseRoarWave();

            yield return new WaitForSeconds(flow.patternDelay);

            if (flow.blackSpellEffect != null)
            {
                flow.blackSpellEffect.SetActive(false);
            }

            yield return MoveToRoomCenterRoutine();

            float elapsed = 0f;
            float angle = 0f;

            while (elapsed < finalRage.finalSpiralDuration && enabled)
            {
                for (int i = 0; i < 4; i++)
                {
                    float branchAngle = angle + 90f * i;
                    FireBullet(
                        transform.position,
                        Rotate(Vector2.right, branchAngle),
                        finalRage.finalSpiralBulletSpeed,
                        finalRage.finalSpiralBulletDamage,
                        finalRage.finalSpiralBulletLifeTime);
                }

                yield return new WaitForSeconds(finalRage.finalSpiralFireInterval);
                elapsed += finalRage.finalSpiralFireInterval;
                angle += finalRage.finalSpiralAngularSpeed * finalRage.finalSpiralFireInterval;
            }

            yield return new WaitForSeconds(flow.patternDelay);
            while (elapsed < finalRage.finalSpiralDuration && enabled)
            {
                for (int i = 0; i < 4; i++)
                {
                    float branchAngle = angle + 90f * i;
                    FireBullet(
                        transform.position,
                        Rotate(Vector2.left, branchAngle),
                        finalRage.finalSpiralBulletSpeed,
                        finalRage.finalSpiralBulletDamage,
                        finalRage.finalSpiralBulletLifeTime);
                }

                yield return new WaitForSeconds(finalRage.finalSpiralFireInterval);
                elapsed += finalRage.finalSpiralFireInterval;
                angle += finalRage.finalSpiralAngularSpeed * finalRage.finalSpiralFireInterval;
            }
        }

        private IEnumerator MoveToRoomCenterRoutine()
        {
            Vector2 center = RoomCenter();

            while (Vector2.Distance(transform.position, center) > 0.05f)
            {
                transform.position = Vector2.MoveTowards(transform.position, center, finalRage.centerMoveSpeed * Time.deltaTime);
                yield return null;
            }
        }

        private void CheckPhaseTransitions()
        {
            if (health == null || health.CurrentHealth <= 0)
            {
                return;
            }

            if (!isBlackSpellCasting && !finalRageStarted && health.HealthRatio <= flow.finalRageHealthRatio)
            {
                ActivateFinalRage();
            }

            if (!blackSpellActive  && !isBlackSpellCasting && health.HealthRatio <= flow.blackSpellHealthRatio)
            {
                blackSpellRoutine = StartCoroutine(BlackSpellRoutine());
            }
        }
        private void ActivateFinalRage()
        {
            if (finalRageStarted)
            {
                return;
            }

            StopCurrentAttacksForPhaseChange();

            StartCoroutine(FinalRageRoutine());
        }

        private IEnumerator BlackSpellRoutine()
        {
            if (blackSpellActive || isBlackSpellCasting) 
                yield break;

            blackSpellActive = true;
            isBlackSpellCasting = true;
            CurrentAttackType = ReaperAttackType.BlackSpell;

            StopCurrentAttacksForPhaseChange(false);
            PlayPhaseRoarWave();

            if(flow.blackSpellEffect != null)
            {
                flow.blackSpellEffect.SetActive(true);
            }

            animators.bodyAnim.SetTrigger(flow.blackSpellTrigger);

            // 손이 공격 중인 상태로 남아있으면 어색하니까 손은 Idle/Cancel로 돌리는 게 좋음
            animators.leftHandAnim.Play("Idle", 0, 0f);
            animators.rightHandAnim.Play("Idle", 0, 0f);

            yield return new WaitForSeconds(flow.blackSpellDuration);

            ApplyBlackSpellBuff();

            isBlackSpellCasting = false;
            blackSpellRoutine = null;

            if (!finalRageStarted && patternRoutine == null)
            {
                patternRoutine = StartCoroutine(BossPatternLoop());
            }
        }
        private void StopCurrentAttacksForPhaseChange(bool stopBlackSpellRoutine = true)
        {
            if (patternRoutine != null)
            {
                StopCoroutine(patternRoutine);
                patternRoutine = null;
            }

            if (laserRoutine != null)
            {
                StopCoroutine(laserRoutine);
                laserRoutine = null;
            }

            if (stopBlackSpellRoutine && blackSpellRoutine != null)
            {
                StopCoroutine(blackSpellRoutine);
                blackSpellRoutine = null;
            }

            isLaserPatternRunning = false;

            animators.bodyAnim.SetBool("IsLaser", false);
            animators.leftHandAnim.SetBool("IsLaser", false);
            animators.rightHandAnim.SetBool("IsLaser", false);

            animators.bodyAnim.ResetTrigger("HandSlam");
            animators.bodyAnim.ResetTrigger("Laser");
            animators.bodyAnim.ResetTrigger("BlackSpell");
            animators.bodyAnim.ResetTrigger("Rage");

            animators.leftHandAnim.ResetTrigger("HandSlam");
            animators.leftHandAnim.ResetTrigger("SlamSwipe");
            animators.leftHandAnim.ResetTrigger("Laser");

            animators.rightHandAnim.ResetTrigger("HandSlam");
            animators.rightHandAnim.ResetTrigger("SlamSwipe");
            animators.rightHandAnim.ResetTrigger("Laser");

            CurrentAttackType = ReaperAttackType.None;
        }
        private void ApplyBlackSpellBuff()
        {
            laser.left.spiralBranches += 1;
            laser.right.aimInterval = Mathf.Max(0.25f, laser.right.aimInterval - 0.35f);
        }

        private void ActivateBlackSpell()
        {
            if (blackSpellActive || isBlackSpellCasting)
            {
                return;
            }

            StopAllCoroutines();

            patternRoutine = null;
            laserRoutine = null;
            blackSpellRoutine = null;
            isLaserPatternRunning = false;

            blackSpellRoutine = StartCoroutine(BlackSpellRoutine());
        }

        private void PlayPhaseRoarWave()
        {
            Vector2 center = transform.position;
            ApplyPhaseRoarKnockback(center);
            StartCoroutine(PhaseRoarWaveVisualRoutine(center));
        }

        private void ApplyPhaseRoarKnockback(Vector2 center)
        {
            if (phaseRoar.knockbackDistance <= 0f || phaseRoar.knockbackDuration <= 0f)
            {
                return;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, phaseRoar.radius);
            HashSet<PlayerMovement> knockedPlayers = new HashSet<PlayerMovement>();

            for (int i = 0; i < hits.Length; i++)
            {
                PlayerMovement playerMovement = hits[i].GetComponentInParent<PlayerMovement>();

                if (playerMovement == null || knockedPlayers.Contains(playerMovement))
                {
                    continue;
                }

                Vector2 direction = (Vector2)playerMovement.transform.position - center;

                if (direction == Vector2.zero)
                {
                    direction = Vector2.down;
                }

                playerMovement.ApplyKnockback(
                    direction.normalized,
                    phaseRoar.knockbackDistance,
                    phaseRoar.knockbackDuration);

                knockedPlayers.Add(playerMovement);
            }
        }

        private IEnumerator PhaseRoarWaveVisualRoutine(Vector2 center)
        {
            GameObject waveObject = new GameObject("Reaper Phase Roar Wave");
            LineRenderer waveRenderer = waveObject.AddComponent<LineRenderer>();

            waveRenderer.useWorldSpace = true;
            waveRenderer.loop = true;
            waveRenderer.positionCount = 72;
            waveRenderer.sortingOrder = 80;
            waveRenderer.startWidth = phaseRoar.lineWidth;
            waveRenderer.endWidth = phaseRoar.lineWidth;
            waveRenderer.startColor = phaseRoar.color;
            waveRenderer.endColor = phaseRoar.color;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                waveRenderer.material = new Material(shader);
            }

            float elapsed = 0f;

            while (elapsed < phaseRoar.visualDuration)
            {
                float t = elapsed / phaseRoar.visualDuration;
                float radius = Mathf.Lerp(0.1f, phaseRoar.radius, t);
                Color color = phaseRoar.color;
                color.a *= 1f - t;

                waveRenderer.startColor = color;
                waveRenderer.endColor = color;
                waveRenderer.startWidth = Mathf.Lerp(phaseRoar.lineWidth, 0.01f, t);
                waveRenderer.endWidth = waveRenderer.startWidth;
                DrawRoarWaveCircle(waveRenderer, center, radius);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(waveObject);
        }

        private static void DrawRoarWaveCircle(LineRenderer lineRenderer, Vector2 center, float radius)
        {
            int count = lineRenderer.positionCount;

            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                Vector3 position = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius,
                    0f);

                lineRenderer.SetPosition(i, position);
            }
        }

        private void SpawnShockwaveBurst(Vector2 position, float radius, int damage, float warningTime, Vector2 origin)
        {
            GameObject burstObject = references.orbBurstPrefab != null
                ? Instantiate(references.orbBurstPrefab, position, Quaternion.identity)
                : new GameObject("Reaper Shockwave Burst");

            burstObject.transform.position = position;

            if (!burstObject.TryGetComponent(out ReaperShockwaveBurst burst))
            {
                burst = burstObject.AddComponent<ReaperShockwaveBurst>();
            }

            burst.Setup(radius, damage, warningTime, origin);
        }

        private void ShowLineAttackWarning(Vector2 startPosition, Vector2 direction, float length, float width, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            direction = direction == Vector2.zero ? Vector2.down : direction.normalized;

            Vector2 center = startPosition + direction * (length * 0.5f);
            Vector2 size = new Vector2(length, width);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            EnemyAttackWarning.ShowBox(center, size, angle, duration, warning.lineWarningSprite);
        }

        private void ShowSweepAttackWarning(bool lowerHalf, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            Rect room = RoomRect();
            float yMin = lowerHalf ? room.yMin : room.center.y;
            float yMax = lowerHalf ? room.center.y : room.yMax;
            Vector2 center = new Vector2(room.center.x, (yMin + yMax) * 0.5f);
            Vector2 size = new Vector2(room.width, Mathf.Abs(yMax - yMin));

            EnemyAttackWarning.ShowBox(center, size, 0f, duration, warning.sweepWarningSprite);
        }
        
        private void FireBullet(Vector2 spawnPosition, Vector2 direction, float speed, int damage, float lifeTime)
        {
            if (references.bulletPrefab == null)
            {
                return;
            }

            EnemyProjectile bullet = Instantiate(references.bulletPrefab, spawnPosition, Quaternion.identity);
            bullet.Launch(direction, speed, damage, lifeTime);
        }

        private Vector2 HandPosition(Transform hand)
        {
            return hand != null ? hand.position : transform.position;
        }

        private Vector2 DirectionToTargetFrom(Vector2 origin)
        {
            FindTargetIfNeeded();

            if (references.target == null)
            {
                return Vector2.down;
            }

            Vector2 direction = (Vector2)references.target.position - origin;
            return direction == Vector2.zero ? Vector2.down : direction.normalized;
        }

        private void FindTargetIfNeeded()
        {
            if (references.target != null)
            {
                return;
            }

            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                references.target = playerHealth.transform;
                return;
            }

            PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                references.target = playerMovement.transform;
            }
        }

        private Rect RoomRect()
        {
            Vector2 center = RoomCenter();
            Vector2 half = room.roomSize * 0.5f;    
            return new Rect(center - half, room.roomSize);
        }

        private Vector2 RoomCenter()
        {
            return room.roomCenter != null ? room.roomCenter.position : transform.position;
        }

        private static Vector2 Rotate(Vector2 direction, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            return new Vector2(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos
            ).normalized;
        }
    }
}
