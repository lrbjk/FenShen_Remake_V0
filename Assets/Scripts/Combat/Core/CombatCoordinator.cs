using System.Collections.Generic;
using FenShen.GameData;
using FenShen.PlayerFSM;
using UnityEngine;

namespace FenShen.Combat
{
    public class CombatCoordinator : MonoBehaviour
    {
        private struct ResolvedCombatSkill
        {
            public CombatSkillDefinitionSO SkillAsset;
            public string SkillId;
        }

        private struct QueuedCombatSkill
        {
            public CombatSkillDefinitionSO SkillAsset;
            public string SkillId;

            public bool IsValid
            {
                get { return SkillAsset != null || !string.IsNullOrWhiteSpace(SkillId); }
            }
        }

        [Header("References")]
        [SerializeField] private PlayerFsm playerFsm;
        [SerializeField] private WeaponRuntimeController weaponRuntime;

        [Header("Default Attack")]
        [SerializeField] private bool handleAttackInputAutomatically = true;
        [SerializeField] private string defaultSkillId = "Primary";
        [SerializeField] private float defaultAttackDuration = 0.6f;
        [SerializeField] private string defaultAnimationStateName;
        [SerializeField] private int defaultAnimationLayer;
        [SerializeField] private bool defaultCrossFade = true;
        [SerializeField] private float defaultTransitionDuration = 0.05f;

        [Header("Recovery")]
        [SerializeField] private StateSO groundedRecoveryState;
        [SerializeField] private StateSO airborneRecoveryState;

        [Header("Input Buffer")]
        [SerializeField] private bool enableDerivationInputBuffer = true;
        [SerializeField] private float derivationInputBufferDuration = 0.12f;

        [Header("Debug")]
        [SerializeField] private bool drawActiveHitGizmos = true;
        [SerializeField] private bool drawGizmosWhenNotSelected = true;
        [SerializeField] private Color activeHitGizmoColor = new Color(1f, 0.25f, 0.15f, 0.35f);
        [SerializeField] private Color activeHitWireColor = new Color(1f, 0.45f, 0.2f, 1f);
        [SerializeField] private Color activeMovementGizmoColor = new Color(0.2f, 0.7f, 1f, 0.85f);

        private readonly CombatStateMachine _stateMachine = new CombatStateMachine();
        private readonly SkillExecutor _skillExecutor = new SkillExecutor();
        private QueuedCombatSkill _queuedSkill;
        private CombatSkillDefinitionSO _lastCompletedSkill;
        private bool _lastCompletedHadHitConfirm;
        private float _bufferedDerivationInputUntil = float.NegativeInfinity;

        public bool IsActive
        {
            get { return _stateMachine.IsActive; }
        }

        public WeaponCancelPermission CurrentCancelPermission
        {
            get { return _skillExecutor.GetCurrentCancelPermission(); }
        }

        void Awake()
        {
            if (playerFsm == null)
            {
                playerFsm = GetComponent<PlayerFsm>();
            }

            if (weaponRuntime == null)
            {
                weaponRuntime = GetComponent<WeaponRuntimeController>();
            }
        }

        public void ManualUpdate(float deltaTime)
        {
            TryConsumeBufferedDerivationInput();

            if (_stateMachine.Tick(this, deltaTime))
            {
                if (TryConsumeQueuedSkill())
                {
                    return;
                }

                if (TryConsumeRecoveryTransition())
                {
                    return;
                }

                ReleaseToLocomotion();
            }
        }

        public bool TryHandleAttackInput()
        {
            if (!handleAttackInputAutomatically || playerFsm == null)
            {
                return false;
            }

            if (!playerFsm.AttackPressedThisFrame())
            {
                return false;
            }

            if (IsActive)
            {
                if (TryQueueDerivedAttack())
                {
                    ClearBufferedDerivationInput();
                    return true;
                }

                BufferDerivationInput();
                return false;
            }

            return TryStartAttack(string.Empty);
        }

        public bool TryStartAttack(string skillId)
        {
            if (playerFsm == null || IsActive)
            {
                return false;
            }

            bool grounded = playerFsm.CheckGround();
            ResolvedCombatSkill resolvedSkill = ResolveAttackSkill(null, skillId, grounded);
            if (resolvedSkill.SkillAsset == null && string.IsNullOrWhiteSpace(resolvedSkill.SkillId))
            {
                return false;
            }

            if (resolvedSkill.SkillAsset != null)
            {
                CombatSkillDefinitionSO previousSkill = weaponRuntime != null ? weaponRuntime.LastResolvedSkill : null;
                if (!SkillGateEvaluator.CanEnterSkill(playerFsm, weaponRuntime, resolvedSkill.SkillAsset, previousSkill, out _))
                {
                    return false;
                }
            }

            playerFsm.SuspendByCombat();
            _stateMachine.Enter(this, new SkillCombatState(
                resolvedSkill.SkillAsset,
                resolvedSkill.SkillId,
                defaultAttackDuration,
                defaultAnimationStateName,
                defaultAnimationLayer,
                defaultCrossFade,
                defaultTransitionDuration));
            if (weaponRuntime != null)
            {
                weaponRuntime.NotifyAttackStarted(resolvedSkill.SkillAsset, resolvedSkill.SkillId, grounded);
            }
            ClearCompletedSkillContext();
            ClearQueuedSkill();
            return true;
        }

        public void ForceExitCombat()
        {
            _stateMachine.Exit(this);
            ClearBufferedDerivationInput();
            ClearCompletedSkillContext();
            ReleaseToLocomotion();
        }

        public void ExecuteAttack(CombatSkillDefinitionSO skill, string skillId)
        {
            if (playerFsm != null && playerFsm.CombatSystem != null)
            {
                if (skill != null)
                {
                    playerFsm.CombatSystem.ExecuteAttack(skill);
                    return;
                }

                playerFsm.CombatSystem.ExecuteAttack(skillId);
            }
        }

        public void BeginSkillExecution(
            CombatSkillDefinitionSO skill,
            string skillId,
            float fallbackDuration,
            string fallbackAnimationStateName,
            int fallbackAnimationLayer,
            bool fallbackCrossFade,
            float fallbackTransitionDuration)
        {
            _skillExecutor.Begin(
                this,
                skill,
                skillId,
                fallbackDuration,
                fallbackAnimationStateName,
                fallbackAnimationLayer,
                fallbackCrossFade,
                fallbackTransitionDuration);
        }

        public bool TickSkillExecution(float deltaTime)
        {
            bool completed = _skillExecutor.Tick(this, deltaTime);
            if (!completed)
            {
                return false;
            }

            CombatSkillDefinitionSO currentSkill = _skillExecutor.Skill;
            if (HasBlockingRecoveryRules(currentSkill))
            {
                bool grounded = playerFsm != null && playerFsm.CheckGround();
                return CanResolveRecoveryTransition(currentSkill, _skillExecutor.HasHitConfirm, grounded);
            }

            return true;
        }

        public void EndSkillExecution()
        {
            _lastCompletedSkill = _skillExecutor.Skill;
            _lastCompletedHadHitConfirm = _skillExecutor.HasHitConfirm;
            _skillExecutor.End();
        }

        public void NotifySkillHitConfirmed(CombatSkillDefinitionSO skill)
        {
            _skillExecutor.NotifyHitConfirmed(skill, skill != null ? skill.ResolveSkillId() : string.Empty);
        }

        public void NotifySkillHitConfirmed(string skillId)
        {
            _skillExecutor.NotifyHitConfirmed(null, skillId);
        }

        public bool TryApplyHitClip(
            CombatSkillDefinitionSO skill,
            string skillId,
            HitSkillClip clip,
            HashSet<int> hitTargets)
        {
            if (clip == null || playerFsm == null || playerFsm.Character == null)
            {
                return false;
            }

            Collider[] hits = QueryHitTargets(clip);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            bool confirmed = false;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                CombatHurtbox hurtbox = hit.GetComponentInParent<CombatHurtbox>();
                if (hurtbox == null)
                {
                    continue;
                }

                int hurtboxId = hurtbox.GetInstanceID();
                if (!clip.allowRepeatHitsOnSameTarget && hitTargets.Contains(hurtboxId))
                {
                    continue;
                }

                if (!hurtbox.CanBeHitBy(ResolveAttackerTeam(), playerFsm.Character))
                {
                    continue;
                }

                float damage = ResolveDamage(skill, clip);
                DamageType damageType = ResolveDamageType();
                if (!hurtbox.ApplyHit(damage, damageType))
                {
                    continue;
                }

                hitTargets.Add(hurtboxId);
                confirmed = true;
            }

            return confirmed;
        }

        public void PlayCombatAnimation(string animationStateName, int animationLayer, bool crossFade, float transitionDuration)
        {
            if (playerFsm == null || playerFsm.Animator == null || string.IsNullOrWhiteSpace(animationStateName))
            {
                return;
            }

            if (crossFade)
            {
                playerFsm.Animator.CrossFadeInFixedTime(animationStateName, transitionDuration, animationLayer);
                return;
            }

            playerFsm.Animator.Play(animationStateName, animationLayer, 0f);
        }

        public void ApplySkillMotion(Vector2 delta)
        {
            if (playerFsm == null || playerFsm.Character == null)
            {
                return;
            }

            playerFsm.Character.Translate(new Vector3(delta.x, delta.y, 0f), Space.World);
        }

        public Vector2 SampleAnimatorMotion(string curveXName, string curveYName, bool mirrorByFacing)
        {
            if (playerFsm == null || playerFsm.Animator == null)
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

        public bool IsFacingLeft()
        {
            return playerFsm != null
                && playerFsm.Character != null
                && playerFsm.Character.localScale.x < 0f;
        }

        public void NotifyRuntimeEvent(string message)
        {
            Debug.Log("[Combat Runtime] " + message, this);
        }

        public List<CombatSkillDefinitionSO> GetCurrentDerivations()
        {
            bool grounded = playerFsm != null && playerFsm.CheckGround();
            return _skillExecutor.GetCurrentDerivations(grounded);
        }

        public List<HitSkillClip> GetActiveHitClips()
        {
            return _skillExecutor.GetActiveHitClips();
        }

        public List<MovementSkillClip> GetActiveMovementClips()
        {
            return _skillExecutor.GetActiveMovementClips();
        }

        private void ReleaseToLocomotion()
        {
            if (playerFsm == null)
            {
                return;
            }

            if (weaponRuntime != null)
            {
                weaponRuntime.NotifyCombatFinished();
            }

            ClearCompletedSkillContext();

            StateSO recoveryState = playerFsm.CheckGround()
                ? (groundedRecoveryState != null ? groundedRecoveryState : playerFsm.ResolveDefaultLocomotionState())
                : (airborneRecoveryState != null ? airborneRecoveryState : playerFsm.FindStateOfType<FallStateSO>());

            playerFsm.ResumeFromCombat(recoveryState);
        }

        private ResolvedCombatSkill ResolveAttackSkill(CombatSkillDefinitionSO requestedSkill, string requestedSkillId, bool grounded)
        {
            if (requestedSkill != null)
            {
                return new ResolvedCombatSkill
                {
                    SkillAsset = requestedSkill,
                    SkillId = requestedSkill.ResolveSkillId()
                };
            }

            if (!string.IsNullOrWhiteSpace(requestedSkillId))
            {
                return new ResolvedCombatSkill
                {
                    SkillAsset = null,
                    SkillId = requestedSkillId
                };
            }

            if (weaponRuntime != null)
            {
                CombatSkillDefinitionSO weaponSkill = weaponRuntime.ResolvePrimaryAttackSkillAsset(grounded);
                if (weaponSkill != null)
                {
                    return new ResolvedCombatSkill
                    {
                        SkillAsset = weaponSkill,
                        SkillId = weaponSkill.ResolveSkillId()
                    };
                }

                string weaponSkillId = weaponRuntime.ResolvePrimaryAttackSkillId(grounded);
                if (!string.IsNullOrWhiteSpace(weaponSkillId))
                {
                    return new ResolvedCombatSkill
                    {
                        SkillAsset = null,
                        SkillId = weaponSkillId
                    };
                }
            }

            return new ResolvedCombatSkill
            {
                SkillAsset = null,
                SkillId = defaultSkillId
            };
        }

        private bool TryQueueDerivedAttack()
        {
            List<CombatSkillDefinitionSO> derivations = GetCurrentDerivations();
            if (derivations.Count == 0)
            {
                return false;
            }

            bool grounded = playerFsm != null && playerFsm.CheckGround();
            CombatSkillDefinitionSO requestedSkill = ResolveRequestedDerivedSkill(derivations, grounded);
            if (requestedSkill == null)
            {
                return false;
            }

            CombatSkillDefinitionSO previousSkill = weaponRuntime != null ? weaponRuntime.LastResolvedSkill : null;
            if (!SkillGateEvaluator.CanEnterSkill(playerFsm, weaponRuntime, requestedSkill, previousSkill, out _))
            {
                return false;
            }

            _queuedSkill = new QueuedCombatSkill
            {
                SkillAsset = requestedSkill,
                SkillId = requestedSkill.ResolveSkillId()
            };
            return true;
        }

        private CombatSkillDefinitionSO ResolveRequestedDerivedSkill(List<CombatSkillDefinitionSO> derivations, bool grounded)
        {
            if (derivations == null || derivations.Count == 0)
            {
                return null;
            }

            if (derivations.Count == 1)
            {
                return derivations[0];
            }

            if (weaponRuntime != null)
            {
                CombatSkillDefinitionSO weaponEntrySkill = weaponRuntime.ResolvePrimaryAttackSkillAsset(grounded);
                if (weaponEntrySkill != null)
                {
                    for (int i = 0; i < derivations.Count; i++)
                    {
                        if (derivations[i] == weaponEntrySkill)
                        {
                            return weaponEntrySkill;
                        }
                    }
                }
            }

            return derivations[0];
        }

        private bool TryConsumeQueuedSkill()
        {
            if (!_queuedSkill.IsValid)
            {
                return false;
            }

            QueuedCombatSkill queuedSkill = _queuedSkill;
            ClearQueuedSkill();
            bool grounded = playerFsm != null && playerFsm.CheckGround();
            if (queuedSkill.SkillAsset != null)
            {
                return StartResolvedSkill(queuedSkill.SkillAsset, queuedSkill.SkillAsset.ResolveSkillId(), grounded);
            }

            return TryStartAttack(queuedSkill.SkillId);
        }

        private bool TryConsumeRecoveryTransition()
        {
            if (_lastCompletedSkill == null || playerFsm == null)
            {
                return false;
            }

            bool grounded = playerFsm.CheckGround();
            if (TryResolveRecoveryTransitionCandidate(_lastCompletedSkill, _lastCompletedHadHitConfirm, grounded, out CombatSkillDefinitionSO nextSkill))
            {
                ClearCompletedSkillContext();
                return StartResolvedSkill(nextSkill, nextSkill.ResolveSkillId(), grounded);
            }

            ClearCompletedSkillContext();
            return false;
        }

        private bool CanResolveRecoveryTransition(CombatSkillDefinitionSO skill, bool hasHitConfirm, bool grounded)
        {
            return TryResolveRecoveryTransitionCandidate(skill, hasHitConfirm, grounded, out _);
        }

        private bool TryResolveRecoveryTransitionCandidate(
            CombatSkillDefinitionSO skill,
            bool hasHitConfirm,
            bool grounded,
            out CombatSkillDefinitionSO nextSkill)
        {
            nextSkill = null;
            if (skill == null || skill.recoveryRules == null || playerFsm == null)
            {
                return false;
            }

            CombatSkillDefinitionSO previousSkill = skill;
            for (int i = 0; i < skill.recoveryRules.Count; i++)
            {
                SkillRecoveryRule rule = skill.recoveryRules[i];
                if (!IsRecoveryRuleSatisfied(rule, hasHitConfirm, grounded))
                {
                    continue;
                }

                if (rule.nextSkill == null)
                {
                    continue;
                }

                if (!SkillGateEvaluator.CanEnterSkill(playerFsm, weaponRuntime, rule.nextSkill, previousSkill, out _))
                {
                    continue;
                }

                nextSkill = rule.nextSkill;
                return true;
            }

            return false;
        }

        private bool IsRecoveryRuleSatisfied(SkillRecoveryRule rule, bool hasHitConfirm, bool grounded)
        {
            if (rule == null)
            {
                return false;
            }

            if (rule.requiresHitConfirm && !hasHitConfirm)
            {
                return false;
            }

            if (rule.requiresGrounded && !grounded)
            {
                return false;
            }

            if (rule.requiresAerial && grounded)
            {
                return false;
            }

            if (rule.conditions == null || rule.conditions.conditions == null || rule.conditions.conditions.Count == 0)
            {
                return true;
            }

            return EvaluateRecoveryConditions(rule.conditions);
        }

        private bool EvaluateRecoveryConditions(SkillConditionGroup conditionGroup)
        {
            if (playerFsm == null || conditionGroup == null || conditionGroup.conditions == null || conditionGroup.conditions.Count == 0)
            {
                return true;
            }

            bool anyMatched = false;
            for (int i = 0; i < conditionGroup.conditions.Count; i++)
            {
                ConditionSO condition = conditionGroup.conditions[i];
                if (condition == null)
                {
                    continue;
                }

                bool result = condition.Evaluate(playerFsm);
                if (conditionGroup.logic == ConditionLogic.All && !result)
                {
                    return false;
                }

                if (conditionGroup.logic == ConditionLogic.Any && result)
                {
                    return true;
                }

                anyMatched |= result;
            }

            return conditionGroup.logic == ConditionLogic.All || anyMatched;
        }

        private bool StartResolvedSkill(CombatSkillDefinitionSO skill, string skillId, bool grounded)
        {
            if (playerFsm == null || skill == null)
            {
                return false;
            }

            playerFsm.SuspendByCombat();
            _stateMachine.Enter(this, new SkillCombatState(
                skill,
                skillId,
                defaultAttackDuration,
                defaultAnimationStateName,
                defaultAnimationLayer,
                defaultCrossFade,
                defaultTransitionDuration));

            if (weaponRuntime != null)
            {
                weaponRuntime.NotifyAttackStarted(skill, skillId, grounded);
            }

            return true;
        }

        private void ClearQueuedSkill()
        {
            _queuedSkill = default;
        }

        private void ClearCompletedSkillContext()
        {
            _lastCompletedSkill = null;
            _lastCompletedHadHitConfirm = false;
        }

        private bool HasBlockingRecoveryRules(CombatSkillDefinitionSO skill)
        {
            return skill != null
                && skill.recoveryRules != null
                && skill.recoveryRules.Count > 0;
        }

        private void BufferDerivationInput()
        {
            if (!enableDerivationInputBuffer)
            {
                return;
            }

            _bufferedDerivationInputUntil = Time.time + Mathf.Max(0.01f, derivationInputBufferDuration);
        }

        private void ClearBufferedDerivationInput()
        {
            _bufferedDerivationInputUntil = float.NegativeInfinity;
        }

        private bool HasBufferedDerivationInput()
        {
            return enableDerivationInputBuffer && Time.time <= _bufferedDerivationInputUntil;
        }

        private void TryConsumeBufferedDerivationInput()
        {
            if (!IsActive || _queuedSkill.IsValid)
            {
                return;
            }

            if (!HasBufferedDerivationInput())
            {
                return;
            }

            if (TryQueueDerivedAttack())
            {
                ClearBufferedDerivationInput();
                return;
            }

            if (Time.time > _bufferedDerivationInputUntil)
            {
                ClearBufferedDerivationInput();
            }
        }

        private float SafeReadAnimatorFloat(string parameterName)
        {
            if (playerFsm == null || playerFsm.Animator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return 0f;
            }

            AnimatorControllerParameter[] parameters = playerFsm.Animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
                {
                    return playerFsm.Animator.GetFloat(parameterName);
                }
            }

            return 0f;
        }

        private Collider[] QueryHitTargets(HitSkillClip clip)
        {
            Vector3 center = ResolveHitCenter(clip);
            switch (clip.hitShape)
            {
                case SkillHitShape.Sphere:
                    return Physics.OverlapSphere(center, Mathf.Max(0.01f, clip.size.x * 0.5f), clip.targetLayers);
                case SkillHitShape.Capsule:
                    float radius = Mathf.Max(0.01f, clip.size.x * 0.5f);
                    float halfHeight = Mathf.Max(radius, clip.size.y * 0.5f);
                    Vector3 point1 = center + Vector3.up * (halfHeight - radius);
                    Vector3 point2 = center + Vector3.down * (halfHeight - radius);
                    return Physics.OverlapCapsule(point1, point2, radius, clip.targetLayers);
                default:
                    Vector3 halfExtents = new Vector3(
                        Mathf.Max(0.01f, clip.size.x * 0.5f),
                        Mathf.Max(0.01f, clip.size.y * 0.5f),
                        Mathf.Max(0.01f, clip.size.z * 0.5f));
                    return Physics.OverlapBox(center, halfExtents, Quaternion.identity, clip.targetLayers);
            }
        }

        private Vector3 ResolveHitCenter(HitSkillClip clip)
        {
            Vector3 offset = clip.offset;
            if (IsFacingLeft())
            {
                offset.x = -offset.x;
            }

            return playerFsm.Character.position + offset;
        }

        private float ResolveDamage(CombatSkillDefinitionSO skill, HitSkillClip clip)
        {
            float baseAttack = playerFsm != null ? playerFsm.GetStat(StatKeys.Attack, 0f) : 0f;
            float skillMultiplier = skill != null ? Mathf.Max(0f, skill.damageMultiplier) : 1f;
            float clipMultiplier = clip != null ? Mathf.Max(0f, clip.damageMultiplier) : 1f;
            return baseAttack * skillMultiplier * clipMultiplier;
        }

        private DamageType ResolveDamageType()
        {
            if (weaponRuntime != null && weaponRuntime.CurrentWeapon != null)
            {
                return weaponRuntime.CurrentWeapon.damageType;
            }

            return DamageType.Physical;
        }

        private CombatTeam ResolveAttackerTeam()
        {
            return playerFsm != null ? CombatTeam.Player : CombatTeam.Neutral;
        }

        private void OnDrawGizmosSelected()
        {
            DrawRuntimeDebugGizmos();
        }

        private void DrawHitClipGizmo(HitSkillClip clip)
        {
            if (clip == null || playerFsm == null || playerFsm.Character == null)
            {
                return;
            }

            Vector3 center = ResolveHitCenter(clip);
            Gizmos.color = activeHitGizmoColor;
            switch (clip.hitShape)
            {
                case SkillHitShape.Sphere:
                    Gizmos.DrawSphere(center, Mathf.Max(0.01f, clip.size.x * 0.5f));
                    Gizmos.color = activeHitWireColor;
                    Gizmos.DrawWireSphere(center, Mathf.Max(0.01f, clip.size.x * 0.5f));
                    break;
                case SkillHitShape.Capsule:
                    DrawCapsuleGizmo(center, clip.size);
                    break;
                default:
                    Gizmos.DrawCube(center, clip.size);
                    Gizmos.color = activeHitWireColor;
                    Gizmos.DrawWireCube(center, clip.size);
                    break;
            }
        }

        private void DrawCapsuleGizmo(Vector3 center, Vector3 size)
        {
            float radius = Mathf.Max(0.01f, size.x * 0.5f);
            float cylinderHeight = Mathf.Max(0f, size.y - (radius * 2f));
            Vector3 top = center + Vector3.up * (cylinderHeight * 0.5f);
            Vector3 bottom = center + Vector3.down * (cylinderHeight * 0.5f);

            Gizmos.color = activeHitGizmoColor;
            Gizmos.DrawSphere(top, radius);
            Gizmos.DrawSphere(bottom, radius);
            Gizmos.DrawCube(center, new Vector3(radius * 2f, cylinderHeight, radius * 2f));

            Gizmos.color = activeHitWireColor;
            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawWireSphere(bottom, radius);
            Gizmos.DrawWireCube(center, new Vector3(radius * 2f, cylinderHeight, radius * 2f));
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmosWhenNotSelected)
            {
                return;
            }

            DrawRuntimeDebugGizmos();
        }

        private void DrawRuntimeDebugGizmos()
        {
            if (!drawActiveHitGizmos || _skillExecutor == null)
            {
                return;
            }

            List<HitSkillClip> activeHitClips = _skillExecutor.GetActiveHitClips();
            for (int i = 0; i < activeHitClips.Count; i++)
            {
                DrawHitClipGizmo(activeHitClips[i]);
            }

            List<MovementSkillClip> activeMovementClips = _skillExecutor.GetActiveMovementClips();
            for (int i = 0; i < activeMovementClips.Count; i++)
            {
                DrawMovementClipGizmo(activeMovementClips[i]);
            }
        }

        private void DrawMovementClipGizmo(MovementSkillClip clip)
        {
            if (clip == null || playerFsm == null || playerFsm.Character == null)
            {
                return;
            }

            Vector3 origin = playerFsm.Character.position;
            Gizmos.color = activeMovementGizmoColor;
            Gizmos.DrawWireSphere(origin, 0.08f);
            Vector3 forward = IsFacingLeft() ? Vector3.left : Vector3.right;
            Gizmos.DrawLine(origin, origin + (forward * 0.75f));
        }
    }
}
