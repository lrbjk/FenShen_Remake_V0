using System.Collections.Generic;
using FenShen.Combat;
using FenShen.GameData;
using UnityEngine;

namespace FenShen.EnemyAI.Combat
{
    public class EnemyCombatSkillController : MonoBehaviour, ICombatSkillRuntime
    {
        [Header("References")]
        public Transform character;
        public Animator animator;
        public Rigidbody2D body2D;
        public Transform spawnRoot;
        public AudioSource audioSource;
        public RuntimeStatsComponent runtimeStats;

        [Header("Skill")]
        public CombatSkillDefinitionSO defaultAttackSkill;
        public string fallbackSkillId = "EnemyAttack";
        public float fallbackDuration = 0.6f;
        public string fallbackAnimationStateName = "Attack";
        public int fallbackAnimationLayer;
        public bool fallbackCrossFade = true;
        public float fallbackTransitionDuration = 0.05f;
        public float fallbackBaseAttack = 10f;
        public DamageType fallbackDamageType = DamageType.Physical;
        public CombatTeam team = CombatTeam.Enemy;

        [Header("Debug")]
        public bool logHitDiagnostics;
        public Color activeHitGizmoColor = new Color(1f, 0.25f, 0.15f, 0.35f);
        public Color activeHitWireColor = new Color(1f, 0.45f, 0.2f, 1f);

        private readonly SkillExecutor _skillExecutor = new SkillExecutor();
        private readonly Dictionary<string, float> _cooldownUntilBySkillId = new Dictionary<string, float>();
        private Transform _target;

        public bool IsActive
        {
            get { return _skillExecutor.IsActive; }
        }

        public CombatSkillDefinitionSO CurrentSkill
        {
            get { return _skillExecutor.Skill; }
        }

        public Transform Target
        {
            get { return _target; }
        }

        private void Reset()
        {
            character = transform;
            animator = GetComponentInChildren<Animator>();
            body2D = GetComponent<Rigidbody2D>();
            audioSource = GetComponent<AudioSource>();
            runtimeStats = GetComponent<RuntimeStatsComponent>();
        }

        private void Awake()
        {
            if (character == null)
            {
                character = transform;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (body2D == null)
            {
                body2D = GetComponent<Rigidbody2D>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (runtimeStats == null)
            {
                runtimeStats = GetComponent<RuntimeStatsComponent>();
            }
        }

        private void Update()
        {
            if (!_skillExecutor.IsActive)
            {
                return;
            }

            if (_skillExecutor.Tick(this, Time.deltaTime))
            {
                EndCurrentSkill();
            }
        }

        public bool CanStartSkill(CombatSkillDefinitionSO skill)
        {
            CombatSkillDefinitionSO resolved = skill != null ? skill : defaultAttackSkill;
            if (_skillExecutor.IsActive)
            {
                return false;
            }

            if (resolved == null)
            {
                return !IsFallbackOnCooldown();
            }

            if (resolved.usageSide == SkillUsageSide.PlayerOnly)
            {
                return false;
            }

            string skillId = resolved.ResolveSkillId();
            return !_cooldownUntilBySkillId.TryGetValue(skillId, out float until) || Time.time >= until;
        }

        public bool TryStartSkill(CombatSkillDefinitionSO skill)
        {
            CombatSkillDefinitionSO resolved = skill != null ? skill : defaultAttackSkill;
            if (!CanStartSkill(resolved))
            {
                return false;
            }

            if (resolved != null)
            {
                _skillExecutor.Begin(
                    this,
                    resolved,
                    resolved.ResolveSkillId(),
                    resolved.ResolveDuration(),
                    resolved.animationStateName,
                    resolved.animationLayer,
                    resolved.crossFade,
                    resolved.transitionDuration);
                return true;
            }

            _skillExecutor.Begin(
                this,
                null,
                fallbackSkillId,
                fallbackDuration,
                fallbackAnimationStateName,
                fallbackAnimationLayer,
                fallbackCrossFade,
                fallbackTransitionDuration);
            return true;
        }

        public bool TryStartDefaultAttack()
        {
            return TryStartSkill(defaultAttackSkill);
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void ForceStopSkill()
        {
            EndCurrentSkill();
        }

        public bool IsFacingLeft()
        {
            Transform visual = character != null ? character : transform;
            return visual.localScale.x < 0f;
        }

        public void PlayCombatAnimation(string animationStateName, int animationLayer, bool crossFade, float transitionDuration)
        {
            if (animator == null || string.IsNullOrWhiteSpace(animationStateName))
            {
                return;
            }

            if (crossFade)
            {
                animator.CrossFadeInFixedTime(animationStateName, transitionDuration, animationLayer);
                return;
            }

            animator.Play(animationStateName, animationLayer, 0f);
        }

        public void ApplySkillMotion(Vector2 delta)
        {
            Transform mover = character != null ? character : transform;
            if (body2D != null)
            {
                body2D.MovePosition(body2D.position + delta);
                return;
            }

            mover.Translate(new Vector3(delta.x, delta.y, 0f), Space.World);
        }

        public Vector2 SampleAnimatorMotion(string curveXName, string curveYName, bool mirrorByFacing)
        {
            if (animator == null)
            {
                return Vector2.zero;
            }

            float x = string.IsNullOrWhiteSpace(curveXName) ? 0f : SafeReadAnimatorFloat(curveXName);
            float y = string.IsNullOrWhiteSpace(curveYName) ? 0f : SafeReadAnimatorFloat(curveYName);
            if (mirrorByFacing && IsFacingLeft())
            {
                x = -x;
            }

            return new Vector2(x, y);
        }

        public bool TryApplyHitClip(CombatSkillDefinitionSO skill, string skillId, HitSkillClip clip, HashSet<int> hitTargets)
        {
            if (clip == null || character == null)
            {
                return false;
            }

            Collider2D[] hits = QueryHitTargets(clip);
            if (hits == null || hits.Length == 0)
            {
                LogHitDiagnostic("HitClip missed: " + ResolveClipLabel(clip));
                return false;
            }

            bool confirmed = false;
            Vector3 firstHitPoint = Vector3.zero;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                CombatHurtbox hurtbox = hit.GetComponentInParent<CombatHurtbox>();
                if (hurtbox == null)
                {
                    continue;
                }

                int targetId = hurtbox.RootTransform != null ? hurtbox.RootTransform.GetInstanceID() : hurtbox.GetInstanceID();
                if (!clip.allowRepeatHitsOnSameTarget && hitTargets != null && hitTargets.Contains(targetId))
                {
                    continue;
                }

                if (!hurtbox.CanBeHitBy(team, character))
                {
                    continue;
                }

                Vector3 hitPoint = hit.ClosestPoint(ResolveHitCenter(clip));
                Vector3 hitDirection = hitPoint - character.position;
                if (hitDirection.sqrMagnitude < 0.0001f)
                {
                    hitDirection = IsFacingLeft() ? Vector3.left : Vector3.right;
                }

                DamageInfo damageInfo = new DamageInfo(
                    ResolveDamage(skill, clip),
                    fallbackDamageType,
                    gameObject,
                    gameObject,
                    hitPoint,
                    hitDirection.normalized,
                    clip.poiseDamage);

                DamageResult result = hurtbox.ReceiveDamage(damageInfo);
                if (!result.Applied)
                {
                    continue;
                }

                hitTargets?.Add(targetId);
                confirmed = true;
                if (firstHitPoint == Vector3.zero)
                {
                    firstHitPoint = hitPoint;
                }
            }

            if (confirmed)
            {
                TriggerHitFeedback(clip, firstHitPoint);
            }

            return confirmed;
        }

        public void NotifySkillHitConfirmed(CombatSkillDefinitionSO skill)
        {
            _skillExecutor.NotifyHitConfirmed(skill, skill != null ? skill.ResolveSkillId() : fallbackSkillId);
        }

        public void ExecuteAttack(CombatSkillDefinitionSO skill, string skillId)
        {
            SendMessage("OnEnemySkillExecuted", skill != null ? skill : null, SendMessageOptions.DontRequireReceiver);
            if (skill == null)
            {
                SendMessage("OnEnemySkillIdExecuted", skillId, SendMessageOptions.DontRequireReceiver);
            }
        }

        public void SpawnProjectile(ProjectileSkillClip clip)
        {
            if (clip == null || clip.projectilePrefab == null)
            {
                return;
            }

            Vector3 position = ResolveSpawnPosition(clip.spawnOffset);
            Vector3 direction = ResolveProjectileDirection(clip);
            Quaternion rotation = Quaternion.identity;
            if (direction.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                rotation = Quaternion.Euler(0f, 0f, angle);
            }

            PooledObject spawned = PooledObjectSpawner.Spawn(
                clip.projectilePrefab,
                position,
                rotation,
                Mathf.Max(0f, clip.lifetime),
                spawnRoot);

            if (spawned != null)
            {
                spawned.SendMessage("InitializeProjectile", new ProjectileRuntimeContext(this, clip, direction), SendMessageOptions.DontRequireReceiver);
            }
        }

        public void SpawnVfx(VfxSkillClip clip)
        {
            if (clip == null || clip.effectPrefab == null)
            {
                return;
            }

            Transform parent = ResolveVfxParent(clip);
            Vector3 position = ResolveVfxPosition(clip, parent);
            PooledObjectSpawner.Spawn(
                clip.effectPrefab,
                position,
                Quaternion.identity,
                Mathf.Max(0f, clip.duration),
                parent);
        }

        public void PlaySfx(SfxSkillClip clip)
        {
            if (clip == null || clip.audioClip == null)
            {
                return;
            }

            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip.audioClip, clip.volume);
                return;
            }

            AudioSource.PlayClipAtPoint(clip.audioClip, ResolveSpawnPosition(Vector3.zero), clip.volume);
        }

        public void TriggerCameraShake(CameraShakeSkillClip clip)
        {
            if (clip != null)
            {
                SendMessage("OnCombatCameraShake", clip, SendMessageOptions.DontRequireReceiver);
            }
        }

        public void ApplySelfBuffClip(SelfBuffSkillClip clip)
        {
            if (clip == null || clip.buff == null)
            {
                return;
            }

            BuffController buffController = GetComponent<BuffController>();
            if (buffController == null && character != null)
            {
                buffController = character.GetComponentInParent<BuffController>();
            }

            if (buffController == null)
            {
                return;
            }

            if (clip.removeBuff)
            {
                buffController.RemoveBuff(clip.buff);
                return;
            }

            buffController.AddBuff(clip.buff, gameObject);
        }

        public void ApplyCoreResourceClip(CoreResourceSkillClip clip)
        {
        }

        private void EndCurrentSkill()
        {
            CombatSkillDefinitionSO skill = _skillExecutor.Skill;
            if (skill != null && skill.cooldown > 0f)
            {
                _cooldownUntilBySkillId[skill.ResolveSkillId()] = Time.time + skill.cooldown;
            }
            else if (skill == null && fallbackDuration > 0f)
            {
                _cooldownUntilBySkillId[fallbackSkillId] = Time.time + fallbackDuration;
            }

            _skillExecutor.End();
        }

        private bool IsFallbackOnCooldown()
        {
            return _cooldownUntilBySkillId.TryGetValue(fallbackSkillId, out float until) && Time.time < until;
        }

        private Collider2D[] QueryHitTargets(HitSkillClip clip)
        {
            Vector3 center = ResolveHitCenter(clip);
            switch (clip.hitShape)
            {
                case SkillHitShape.Sphere:
                    return Physics2D.OverlapCircleAll(center, Mathf.Max(0.01f, clip.size.x * 0.5f), clip.targetLayers);
                case SkillHitShape.Capsule:
                    CapsuleDirection2D direction = clip.size.y >= clip.size.x ? CapsuleDirection2D.Vertical : CapsuleDirection2D.Horizontal;
                    return Physics2D.OverlapCapsuleAll(center, new Vector2(Mathf.Max(0.01f, clip.size.x), Mathf.Max(0.01f, clip.size.y)), direction, 0f, clip.targetLayers);
                default:
                    Vector2 size = new Vector2(Mathf.Max(0.01f, clip.size.x), Mathf.Max(0.01f, clip.size.y));
                    return Physics2D.OverlapBoxAll(center, size, 0f, clip.targetLayers);
            }
        }

        private Vector3 ResolveHitCenter(HitSkillClip clip)
        {
            Vector3 offset = clip.offset;
            if (IsFacingLeft())
            {
                offset.x = -offset.x;
            }

            return character.position + offset;
        }

        private Vector3 ResolveSpawnPosition(Vector3 localOffset)
        {
            Vector3 offset = localOffset;
            if (IsFacingLeft())
            {
                offset.x = -offset.x;
            }

            return character != null ? character.position + offset : transform.position + offset;
        }

        private Vector3 ResolveProjectileDirection(ProjectileSkillClip clip)
        {
            if (clip == null)
            {
                return IsFacingLeft() ? Vector3.left : Vector3.right;
            }

                Vector3 direction;
            switch (clip.releaseMode)
            {
                case ProjectileReleaseMode.ToTarget:
                    direction = _target != null ? _target.position - ResolveSpawnPosition(clip.spawnOffset) : Vector3.zero;
                    break;
                case ProjectileReleaseMode.FixedDirection:
                    direction = clip.fixedDirection;
                    break;
                default:
                    direction = IsFacingLeft() ? Vector3.left : Vector3.right;
                    break;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = IsFacingLeft() ? Vector3.left : Vector3.right;
            }

            return direction.normalized;
        }

        private Transform ResolveVfxParent(VfxSkillClip clip)
        {
            if (clip == null)
            {
                return spawnRoot;
            }

            if (clip.spawnSpace == SkillVfxSpawnSpace.FollowCaster)
            {
                return character;
            }

            if (clip.spawnSpace == SkillVfxSpawnSpace.Bone)
            {
                Transform socket = ResolveSocketTransform(clip.socketName);
                return socket != null ? socket : character;
            }

            return spawnRoot;
        }

        private Vector3 ResolveVfxPosition(VfxSkillClip clip, Transform parent)
        {
            if (clip == null)
            {
                return ResolveSpawnPosition(Vector3.zero);
            }

            if (clip.spawnSpace == SkillVfxSpawnSpace.World)
            {
                return ResolveSpawnPosition(clip.localOffset);
            }

            if (parent != null)
            {
                return parent.position + clip.localOffset;
            }

            return ResolveSpawnPosition(clip.localOffset);
        }

        private Transform ResolveSocketTransform(string socketName)
        {
            if (character == null || string.IsNullOrWhiteSpace(socketName))
            {
                return null;
            }

            Transform[] children = character.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == socketName)
                {
                    return children[i];
                }
            }

            return null;
        }

        private float ResolveDamage(CombatSkillDefinitionSO skill, HitSkillClip clip)
        {
            float baseAttack = runtimeStats != null ? runtimeStats.GetStat(StatKeys.Attack) : fallbackBaseAttack;
            if (baseAttack <= 0f)
            {
                baseAttack = fallbackBaseAttack;
            }

            float skillMultiplier = skill != null ? Mathf.Max(0f, skill.damageMultiplier) : 1f;
            float clipMultiplier = clip != null ? Mathf.Max(0f, clip.damageMultiplier) : 1f;
            return baseAttack * skillMultiplier * clipMultiplier;
        }

        private float SafeReadAnimatorFloat(string parameterName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return 0f;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
                {
                    return animator.GetFloat(parameterName);
                }
            }

            return 0f;
        }

        private void TriggerHitFeedback(HitSkillClip clip, Vector3 hitPoint)
        {
            if (clip == null)
            {
                return;
            }

            if (clip.hitSfx != null)
            {
                AudioSource.PlayClipAtPoint(clip.hitSfx, hitPoint);
            }

            if (clip.hitVfxPrefab != null)
            {
                PooledObjectSpawner.Spawn(clip.hitVfxPrefab, hitPoint, Quaternion.identity, 0f, spawnRoot);
            }

            if (clip.cameraShakeAmplitude > 0f)
            {
                TriggerCameraShake(new CameraShakeSkillClip
                {
                    amplitude = clip.cameraShakeAmplitude
                });
            }
        }

        private string ResolveClipLabel(SkillClipBase clip)
        {
            if (clip == null)
            {
                return "<null>";
            }

            if (!string.IsNullOrWhiteSpace(clip.displayName))
            {
                return clip.displayName;
            }

            return !string.IsNullOrWhiteSpace(clip.clipId) ? clip.clipId : clip.GetType().Name;
        }

        private void LogHitDiagnostic(string message)
        {
            if (logHitDiagnostics)
            {
                Debug.Log("[Enemy Combat] " + message, this);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_skillExecutor.IsActive)
            {
                return;
            }

            List<HitSkillClip> activeHitClips = _skillExecutor.GetActiveHitClips();
            for (int i = 0; i < activeHitClips.Count; i++)
            {
                DrawHitClipGizmo(activeHitClips[i]);
            }
        }

        private void DrawHitClipGizmo(HitSkillClip clip)
        {
            if (clip == null || character == null)
            {
                return;
            }

            Vector3 center = ResolveHitCenter(clip);
            Gizmos.color = activeHitGizmoColor;
            Gizmos.DrawCube(center, clip.size);
            Gizmos.color = activeHitWireColor;
            Gizmos.DrawWireCube(center, clip.size);
        }
    }
}
