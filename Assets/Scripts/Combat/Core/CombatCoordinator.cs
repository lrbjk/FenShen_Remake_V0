using System;
using System.Collections;
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

        [Header("引用")]
        [InspectorName("玩家FSM")]
        [SerializeField] private PlayerFsm playerFsm;
        [InspectorName("武器运行时")]
        [SerializeField] private WeaponRuntimeController weaponRuntime;
        [InspectorName("核心运行时")]
        [SerializeField] private CoreRuntimeController coreRuntime;

        [Header("默认攻击")]
        [InspectorName("自动处理攻击输入")]
        [SerializeField] private bool handleAttackInputAutomatically = true;
        [InspectorName("默认技能ID")]
        [SerializeField] private string defaultSkillId = "Primary";
        [InspectorName("默认攻击持续时间")]
        [SerializeField] private float defaultAttackDuration = 0.6f;
        [InspectorName("备用基础攻击力")]
        [SerializeField] private float fallbackBaseAttack = 10f;
        [InspectorName("默认动画状态名")]
        [SerializeField] private string defaultAnimationStateName;
        [InspectorName("默认动画层")]
        [SerializeField] private int defaultAnimationLayer;
        [InspectorName("默认使用过渡")]
        [SerializeField] private bool defaultCrossFade = true;
        [InspectorName("默认过渡时间")]
        [SerializeField] private float defaultTransitionDuration = 0.05f;

        [Header("恢复状态")]
        [InspectorName("地面恢复状态")]
        [SerializeField] private StateSO groundedRecoveryState;
        [InspectorName("空中恢复状态")]
        [SerializeField] private StateSO airborneRecoveryState;

        [Header("输入缓冲")]
        [InspectorName("启用派生输入缓冲")]
        [SerializeField] private bool enableDerivationInputBuffer = true;
        [InspectorName("派生输入缓冲时长")]
        [SerializeField] private float derivationInputBufferDuration = 0.12f;

        [Header("核心技能输入")]
        [InspectorName("方向组合阈值")]
        [SerializeField, Range(0.1f, 1f)] private float directionalInputThreshold = 0.45f;
        [InspectorName("RT按住时禁止普通Y技能")]
        [SerializeField] private bool suppressFaceYWhenModifierHeld = true;
        [InspectorName("武器特殊攻击方向阈值")]
        [SerializeField, Range(0.1f, 1f)] private float weaponSpecialDirectionThreshold = 0.45f;
        [InspectorName("冲刺攻击需要按住冲刺")]
        [SerializeField] private bool dashAttackRequiresSprintHeld = true;

        [Header("运行时生成")]
        [InspectorName("生成根节点")]
        [SerializeField] private Transform spawnRoot;
        [InspectorName("音频源")]
        [SerializeField] private AudioSource audioSource;

        [Header("调试")]
        [InspectorName("绘制当前打击框")]
        [SerializeField] private bool drawActiveHitGizmos = true;
        [InspectorName("未选中时也绘制Gizmos")]
        [SerializeField] private bool drawGizmosWhenNotSelected = true;
        [InspectorName("打击框填充颜色")]
        [SerializeField] private Color activeHitGizmoColor = new Color(1f, 0.25f, 0.15f, 0.35f);
        [InspectorName("打击框线框颜色")]
        [SerializeField] private Color activeHitWireColor = new Color(1f, 0.45f, 0.2f, 1f);
        [InspectorName("位移调试颜色")]
        [SerializeField] private Color activeMovementGizmoColor = new Color(0.2f, 0.7f, 1f, 0.85f);
        [InspectorName("打印命中诊断日志")]
        [SerializeField] private bool logHitDiagnostics;

        private readonly CombatStateMachine _stateMachine = new CombatStateMachine();
        private readonly SkillExecutor _skillExecutor = new SkillExecutor();
        private QueuedCombatSkill _queuedSkill;
        private CombatSkillDefinitionSO _lastCompletedSkill;
        private bool _lastCompletedHadHitConfirm;
        private float _bufferedDerivationInputUntil = float.NegativeInfinity;
        private bool _aerialAttackLockedUntilGrounded;
        private Coroutine _hitStopRoutine;
        private float _hitStopRestoreTimeScale = 1f;

        public bool IsActive
        {
            get { return _stateMachine.IsActive; }
        }

        public WeaponCancelPermission CurrentCancelPermission
        {
            get
            {
                WeaponCancelPermission permission = _skillExecutor.GetCurrentCancelPermission();
                return coreRuntime != null ? coreRuntime.ModifyCancelPermission(permission) : permission;
            }
        }

        public CoreRuntimeController CoreController
        {
            get { return coreRuntime; }
        }

        public event Action<CombatSkillDefinitionSO> SkillStarted;

        public void ClearAerialAttackLock()
        {
            _aerialAttackLockedUntilGrounded = false;
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

            if (coreRuntime == null)
            {
                coreRuntime = GetComponent<CoreRuntimeController>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        void OnDisable()
        {
            RestoreHitStopTimeScale();
        }

        public void ManualUpdate(float deltaTime)
        {
            RefreshAerialAttackLock();
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

        public bool TryHandleCoreSkillInput()
        {
            if (playerFsm == null || coreRuntime == null)
            {
                return false;
            }

            int selectedSlotIndex = -1;
            int selectedPriority = -1;
            int slotCount = coreRuntime.SkillSlotCount;
            for (int i = 0; i < slotCount; i++)
            {
                if (!coreRuntime.TryGetSkillSlotInput(i, out CoreSkillSlotInput input))
                {
                    continue;
                }

                if (!IsCoreSkillInputPressed(input, i))
                {
                    continue;
                }

                int priority = GetCoreSkillInputPriority(input);
                if (priority > selectedPriority)
                {
                    selectedPriority = priority;
                    selectedSlotIndex = i;
                }
            }

            return selectedSlotIndex >= 0 && TryStartCoreSkill(selectedSlotIndex);
        }

        private bool IsCoreSkillInputPressed(CoreSkillSlotInput input, int slotIndex)
        {
            if (playerFsm == null)
            {
                return false;
            }

            switch (input)
            {
                case CoreSkillSlotInput.Skill1:
                    return playerFsm.CoreSkillPressedThisFrame(0);
                case CoreSkillSlotInput.Skill2:
                    return playerFsm.CoreSkillPressedThisFrame(1);
                case CoreSkillSlotInput.Skill3:
                    return playerFsm.CoreSkillPressedThisFrame(2);
                case CoreSkillSlotInput.Skill4:
                    return playerFsm.CoreSkillPressedThisFrame(3);
                case CoreSkillSlotInput.FaceY:
                    return IsFaceYSkillPressed(false);
                case CoreSkillSlotInput.FaceYNeutral:
                    return IsFaceYSkillPressed(false) && IsNeutralDirectionHeld();
                case CoreSkillSlotInput.FaceYUp:
                    return IsFaceYSkillPressed(false) && IsUpDirectionHeld();
                case CoreSkillSlotInput.FaceYDown:
                    return IsFaceYSkillPressed(false) && IsDownDirectionHeld();
                case CoreSkillSlotInput.Finisher:
                    return playerFsm.FinisherPressedThisFrame() || playerFsm.CoreSkillPressedThisFrame(slotIndex);
                case CoreSkillSlotInput.Utility:
                    return playerFsm.CoreSkillPressedThisFrame(slotIndex);
                case CoreSkillSlotInput.ModifierRightShoulder:
                    return playerFsm.CoreModifierIsHeld() && playerFsm.RightShoulderPressedThisFrame();
                case CoreSkillSlotInput.ModifierFaceX:
                    return playerFsm.CoreModifierIsHeld() && playerFsm.FaceXPressedThisFrame();
                case CoreSkillSlotInput.ModifierFaceY:
                    return playerFsm.CoreModifierIsHeld() && playerFsm.FaceYPressedThisFrame();
                default:
                    return playerFsm.CoreSkillPressedThisFrame(slotIndex);
            }
        }

        private bool IsFaceYSkillPressed(bool allowModifier)
        {
            if (playerFsm == null || !playerFsm.FaceYPressedThisFrame())
            {
                return false;
            }

            return allowModifier || !suppressFaceYWhenModifierHeld || !playerFsm.CoreModifierIsHeld();
        }

        private bool IsUpDirectionHeld()
        {
            return playerFsm != null && playerFsm.CurrentMoveInput.y >= directionalInputThreshold;
        }

        private bool IsDownDirectionHeld()
        {
            return playerFsm != null && playerFsm.CurrentMoveInput.y <= -directionalInputThreshold;
        }

        private bool IsNeutralDirectionHeld()
        {
            return playerFsm != null && Mathf.Abs(playerFsm.CurrentMoveInput.y) < directionalInputThreshold;
        }

        private int GetCoreSkillInputPriority(CoreSkillSlotInput input)
        {
            switch (input)
            {
                case CoreSkillSlotInput.ModifierRightShoulder:
                case CoreSkillSlotInput.ModifierFaceX:
                case CoreSkillSlotInput.ModifierFaceY:
                    return 40;
                case CoreSkillSlotInput.FaceYUp:
                case CoreSkillSlotInput.FaceYDown:
                    return 30;
                case CoreSkillSlotInput.FaceYNeutral:
                    return 20;
                case CoreSkillSlotInput.FaceY:
                    return 10;
                default:
                    return 0;
            }
        }

        public bool TryStartAttack(string skillId)
        {
            if (playerFsm == null || IsActive)
            {
                return false;
            }

            bool grounded = playerFsm.CheckGround();
            if (!grounded && _aerialAttackLockedUntilGrounded && !AllowsRepeatedAerialAttackBeforeLanding())
            {
                return false;
            }

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

            if (coreRuntime != null && !coreRuntime.CanPaySkillCost(resolvedSkill.SkillAsset))
            {
                return false;
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
            if (coreRuntime != null)
            {
                coreRuntime.TryPaySkillCost(resolvedSkill.SkillAsset);
                coreRuntime.NotifySkillStarted(resolvedSkill.SkillAsset, grounded);
            }

            SkillStarted?.Invoke(resolvedSkill.SkillAsset);
            bool locksAerialAttack = resolvedSkill.SkillAsset == null || resolvedSkill.SkillAsset.skillKind == CombatSkillKind.Attack;
            if (locksAerialAttack && !grounded && !AllowsRepeatedAerialAttackBeforeLanding())
            {
                _aerialAttackLockedUntilGrounded = true;
            }
            ClearCompletedSkillContext();
            ClearQueuedSkill();
            return true;
        }

        public bool TryStartCoreSkill(int slotIndex)
        {
            if (coreRuntime == null || !coreRuntime.TryGetEquippedSkill(slotIndex, out CombatSkillDefinitionSO skill))
            {
                return false;
            }

            return TryStartSkill(skill);
        }

        public bool TryStartSkill(CombatSkillDefinitionSO skill)
        {
            if (playerFsm == null || skill == null)
            {
                return false;
            }

            bool grounded = playerFsm.CheckGround();
            if (IsActive)
            {
                if (!CanQueueSkillCancel())
                {
                    return false;
                }

                return TryQueueSkill(skill, grounded);
            }

            if (!grounded && skill.skillKind == CombatSkillKind.Attack && _aerialAttackLockedUntilGrounded && !AllowsRepeatedAerialAttackBeforeLanding())
            {
                return false;
            }

            CombatSkillDefinitionSO previousSkill = weaponRuntime != null ? weaponRuntime.LastResolvedSkill : null;
            if (!SkillGateEvaluator.CanEnterSkill(playerFsm, weaponRuntime, skill, previousSkill, out _))
            {
                return false;
            }

            return StartResolvedSkill(skill, skill.ResolveSkillId(), grounded);
        }

        public void ForceExitCombat()
        {
            _stateMachine.Exit(this);
            ClearBufferedDerivationInput();
            ClearCompletedSkillContext();
            RefreshAerialAttackLock();
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
            if (_queuedSkill.IsValid)
            {
                return true;
            }

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
            if (coreRuntime != null)
            {
                bool grounded = playerFsm == null || playerFsm.CheckGround();
                coreRuntime.NotifySkillFinished(_skillExecutor.Skill, grounded);
            }
            _skillExecutor.End();
        }

        public void NotifySkillHitConfirmed(CombatSkillDefinitionSO skill)
        {
            _skillExecutor.NotifyHitConfirmed(skill, skill != null ? skill.ResolveSkillId() : string.Empty);
            if (coreRuntime != null)
            {
                bool grounded = playerFsm == null || playerFsm.CheckGround();
                coreRuntime.NotifyHitConfirmed(skill, grounded);
            }
        }

        public void NotifySkillHitConfirmed(string skillId)
        {
            _skillExecutor.NotifyHitConfirmed(null, skillId);
            if (coreRuntime != null)
            {
                bool grounded = playerFsm == null || playerFsm.CheckGround();
                coreRuntime.NotifyHitConfirmed(null, grounded);
            }
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

            Collider2D[] hits = QueryHitTargets(clip);
            if (hits == null || hits.Length == 0)
            {
                LogHitDiagnostic($"HitClip '{ResolveClipLabel(clip)}' did not overlap any collider. Center={ResolveHitCenter(clip)}, Size={clip.size}, LayerMask={clip.targetLayers.value}");
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
                    LogHitDiagnostic($"Collider '{hit.name}' was overlapped but has no CombatHurtbox in parent.");
                    continue;
                }

                int hurtboxId = hurtbox.GetInstanceID();
                if (!clip.allowRepeatHitsOnSameTarget && hitTargets.Contains(hurtboxId))
                {
                    continue;
                }

                if (!hurtbox.CanBeHitBy(ResolveAttackerTeam(), playerFsm.Character))
                {
                    LogHitDiagnostic($"Hurtbox '{hurtbox.name}' rejected team/root check. Team={hurtbox.Team}, AttackerTeam={ResolveAttackerTeam()}");
                    continue;
                }

                float damage = ResolveDamage(skill, clip);
                DamageType damageType = ResolveDamageType();
                DamageInfo damageInfo = new DamageInfo(
                    damage,
                    damageType,
                    gameObject,
                    playerFsm.Character.gameObject,
                    hit.ClosestPoint(playerFsm.Character.position),
                    hurtbox.RootTransform.position - playerFsm.Character.position,
                    clip.poiseDamage);
                DamageResult damageResult = hurtbox.ReceiveDamage(damageInfo);
                if (!damageResult.Applied)
                {
                    LogHitDiagnostic($"Hurtbox '{hurtbox.name}' received hit but damage was not applied. Raw={damage:0.###}, Type={damageType}, HP={hurtbox.RuntimeStats?.CurrentHP:0.###}");
                    continue;
                }

                ApplyHitKnockback(clip, hurtbox, damageInfo.HitDirection);
                hitTargets.Add(hurtboxId);
                if (!confirmed)
                {
                    firstHitPoint = damageInfo.HitPoint;
                }

                confirmed = true;
                LogHitDiagnostic($"Hit '{hurtbox.name}' for {damageResult.FinalDamage:0.###}. HP={hurtbox.RuntimeStats?.CurrentHP:0.###}/{hurtbox.RuntimeStats?.MaxHP:0.###}");
            }

            if (confirmed)
            {
                TriggerHitFeedback(clip, firstHitPoint);
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
            if (clip == null)
            {
                return;
            }

            SendMessage("OnCombatCameraShake", clip, SendMessageOptions.DontRequireReceiver);
        }

        private void TriggerHitFeedback(HitSkillClip clip, Vector3 hitPoint)
        {
            if (clip == null)
            {
                return;
            }

            StartHitStop(clip.hitStopDuration, clip.hitStopScale);
            TriggerHitCameraShake(clip);
            PlayHitSfx(clip, hitPoint);
            SpawnHitVfx(clip, hitPoint);
        }

        private void StartHitStop(float duration, float scale)
        {
            if (duration <= 0f)
            {
                return;
            }

            if (_hitStopRoutine != null)
            {
                StopCoroutine(_hitStopRoutine);
                Time.timeScale = _hitStopRestoreTimeScale;
            }

            _hitStopRestoreTimeScale = Time.timeScale;
            _hitStopRoutine = StartCoroutine(HitStopRoutine(duration, Mathf.Clamp01(scale)));
        }

        private void RestoreHitStopTimeScale()
        {
            if (_hitStopRoutine == null)
            {
                return;
            }

            StopCoroutine(_hitStopRoutine);
            Time.timeScale = _hitStopRestoreTimeScale;
            _hitStopRoutine = null;
        }

        private IEnumerator HitStopRoutine(float duration, float scale)
        {
            Time.timeScale = Mathf.Max(0.0001f, scale);
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = _hitStopRestoreTimeScale;
            _hitStopRoutine = null;
        }

        private void TriggerHitCameraShake(HitSkillClip clip)
        {
            if (clip.cameraShakeAmplitude <= 0f)
            {
                return;
            }

            TriggerCameraShake(new CameraShakeSkillClip
            {
                clipId = "hit-feedback-camera-shake",
                displayName = "Hit Feedback Camera Shake",
                amplitude = clip.cameraShakeAmplitude,
                frequency = 20f,
            });
        }

        private void PlayHitSfx(HitSkillClip clip, Vector3 hitPoint)
        {
            if (clip.hitSfx == null)
            {
                return;
            }

            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip.hitSfx);
                return;
            }

            AudioSource.PlayClipAtPoint(clip.hitSfx, hitPoint);
        }

        private void SpawnHitVfx(HitSkillClip clip, Vector3 hitPoint)
        {
            if (clip.hitVfxPrefab == null)
            {
                return;
            }

            PooledObjectSpawner.Spawn(
                clip.hitVfxPrefab,
                hitPoint,
                Quaternion.identity,
                0f,
                spawnRoot);
        }

        private void ApplyHitKnockback(HitSkillClip clip, CombatHurtbox hurtbox, Vector3 hitDirection)
        {
            if (clip == null || hurtbox == null)
            {
                return;
            }

            float horizontalDistance = Mathf.Max(0f, clip.knockbackDistance);
            float upwardDistance = Mathf.Max(0f, clip.knockbackUpwardDistance);
            if (horizontalDistance <= 0f && upwardDistance <= 0f)
            {
                return;
            }

            Vector3 horizontalDirection = hitDirection;
            horizontalDirection.z = 0f;
            if (horizontalDirection.sqrMagnitude < 0.0001f)
            {
                horizontalDirection = IsFacingLeft() ? Vector3.left : Vector3.right;
            }

            horizontalDirection.Normalize();
            Vector3 delta = horizontalDirection * horizontalDistance + Vector3.up * upwardDistance;
            Transform targetRoot = hurtbox.RootTransform;
            if (targetRoot == null)
            {
                return;
            }

            Rigidbody rigidbody = targetRoot.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = hurtbox.GetComponentInParent<Rigidbody>();
            }

            if (rigidbody != null)
            {
                rigidbody.MovePosition(rigidbody.position + delta);
                return;
            }

            Rigidbody2D rigidbody2D = targetRoot.GetComponent<Rigidbody2D>();
            if (rigidbody2D == null)
            {
                rigidbody2D = hurtbox.GetComponentInParent<Rigidbody2D>();
            }

            if (rigidbody2D != null)
            {
                rigidbody2D.MovePosition(rigidbody2D.position + new Vector2(delta.x, delta.y));
                return;
            }

            CharacterController characterController = targetRoot.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = hurtbox.GetComponentInParent<CharacterController>();
            }

            if (characterController != null)
            {
                characterController.Move(delta);
                return;
            }

            targetRoot.position += delta;
        }

        public void ApplySelfBuffClip(SelfBuffSkillClip clip)
        {
            if (clip == null || clip.buff == null || playerFsm == null)
            {
                return;
            }

            BuffController buffController = playerFsm.GetComponent<BuffController>();
            if (buffController == null && playerFsm.Character != null)
            {
                buffController = playerFsm.Character.GetComponentInParent<BuffController>();
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
            if (clip == null || coreRuntime == null)
            {
                return;
            }

            coreRuntime.TryModifyResource(clip.amount, clip.requireEnoughResource);
        }

        public List<CombatSkillDefinitionSO> GetCurrentDerivations()
        {
            bool grounded = playerFsm != null && playerFsm.CheckGround();
            List<CombatSkillDefinitionSO> derivations = _skillExecutor.GetCurrentDerivations(grounded);
            if (coreRuntime == null)
            {
                return derivations;
            }

            List<CombatSkillDefinitionSO> coreDerivations = coreRuntime.GetSpecialDerivations(
                _skillExecutor.Skill,
                grounded,
                _skillExecutor.HasHitConfirm);
            for (int i = 0; i < coreDerivations.Count; i++)
            {
                CombatSkillDefinitionSO skill = coreDerivations[i];
                if (skill != null && !derivations.Contains(skill))
                {
                    derivations.Add(skill);
                }
            }

            return derivations;
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

            RefreshAerialAttackLock();
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
                WeaponAttackSlot attackSlot = ResolveWeaponAttackSlot(grounded);
                ResolvedCombatSkill weaponSkill = ResolveWeaponSkillOrPrimary(attackSlot, grounded);
                if (weaponSkill.SkillAsset != null || !string.IsNullOrWhiteSpace(weaponSkill.SkillId))
                {
                    return weaponSkill;
                }
            }

            return new ResolvedCombatSkill
            {
                SkillAsset = null,
                SkillId = defaultSkillId
            };
        }

        private ResolvedCombatSkill ResolveWeaponSkillOrPrimary(WeaponAttackSlot attackSlot, bool grounded)
        {
            ResolvedCombatSkill resolvedSkill = ResolveWeaponSkillFromSlot(attackSlot, grounded);
            if (resolvedSkill.SkillAsset != null || !string.IsNullOrWhiteSpace(resolvedSkill.SkillId))
            {
                return resolvedSkill;
            }

            WeaponAttackSlot primarySlot = grounded ? WeaponAttackSlot.PrimaryGround : WeaponAttackSlot.PrimaryAir;
            if (attackSlot != primarySlot)
            {
                return ResolveWeaponSkillFromSlot(primarySlot, grounded);
            }

            return resolvedSkill;
        }

        private ResolvedCombatSkill ResolveWeaponSkillFromSlot(WeaponAttackSlot attackSlot, bool grounded)
        {
            if (weaponRuntime == null)
            {
                return default;
            }

            CombatSkillDefinitionSO weaponSkill = weaponRuntime.ResolveSkillAssetForSlot(attackSlot, grounded);
            if (weaponSkill != null)
            {
                return new ResolvedCombatSkill
                {
                    SkillAsset = weaponSkill,
                    SkillId = weaponSkill.ResolveSkillId()
                };
            }

            string weaponSkillId = weaponRuntime.ResolveSkillIdForSlot(attackSlot, grounded);
            if (!string.IsNullOrWhiteSpace(weaponSkillId))
            {
                return new ResolvedCombatSkill
                {
                    SkillAsset = null,
                    SkillId = weaponSkillId
                };
            }

            return default;
        }

        private WeaponAttackSlot ResolveWeaponAttackSlot(bool grounded)
        {
            if (weaponRuntime == null)
            {
                return grounded ? WeaponAttackSlot.PrimaryGround : WeaponAttackSlot.PrimaryAir;
            }

            if (ShouldUseDashAttack(grounded) && HasWeaponEntry(WeaponAttackSlot.DashAttack, grounded))
            {
                return WeaponAttackSlot.DashAttack;
            }

            if (IsSpecialUpInputHeld())
            {
                if (HasWeaponEntry(WeaponAttackSlot.SpecialUp, grounded))
                {
                    return WeaponAttackSlot.SpecialUp;
                }

                if (grounded && HasWeaponEntry(WeaponAttackSlot.Launcher, grounded))
                {
                    return WeaponAttackSlot.Launcher;
                }
            }

            if (IsSpecialDownInputHeld())
            {
                if (HasWeaponEntry(WeaponAttackSlot.SpecialDown, grounded))
                {
                    return WeaponAttackSlot.SpecialDown;
                }

                if (!grounded && HasWeaponEntry(WeaponAttackSlot.Slam, grounded))
                {
                    return WeaponAttackSlot.Slam;
                }
            }

            if (IsNeutralSpecialInputHeld() && HasWeaponEntry(WeaponAttackSlot.SpecialNeutral, grounded))
            {
                return WeaponAttackSlot.SpecialNeutral;
            }

            return grounded ? WeaponAttackSlot.PrimaryGround : WeaponAttackSlot.PrimaryAir;
        }

        private bool HasWeaponEntry(WeaponAttackSlot slot, bool grounded)
        {
            if (weaponRuntime == null)
            {
                return false;
            }

            return weaponRuntime.ResolveSkillAssetForSlot(slot, grounded) != null
                || !string.IsNullOrWhiteSpace(weaponRuntime.ResolveSkillIdForSlot(slot, grounded));
        }

        private bool ShouldUseDashAttack(bool grounded)
        {
            if (!grounded || playerFsm == null)
            {
                return false;
            }

            return dashAttackRequiresSprintHeld ? playerFsm.IsSprinting : playerFsm.SprintHeldFor(0.01f);
        }

        private bool IsSpecialUpInputHeld()
        {
            return playerFsm != null && playerFsm.CurrentMoveInput.y >= weaponSpecialDirectionThreshold;
        }

        private bool IsSpecialDownInputHeld()
        {
            return playerFsm != null && playerFsm.CurrentMoveInput.y <= -weaponSpecialDirectionThreshold;
        }

        private bool IsNeutralSpecialInputHeld()
        {
            return playerFsm != null
                && playerFsm.CurrentMoveInput.sqrMagnitude >= weaponSpecialDirectionThreshold * weaponSpecialDirectionThreshold
                && Mathf.Abs(playerFsm.CurrentMoveInput.y) < weaponSpecialDirectionThreshold;
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

            if (!grounded && requestedSkill == _skillExecutor.Skill)
            {
                return false;
            }

            CombatSkillDefinitionSO previousSkill = weaponRuntime != null ? weaponRuntime.LastResolvedSkill : null;
            if (!SkillGateEvaluator.CanEnterSkill(playerFsm, weaponRuntime, requestedSkill, previousSkill, out _))
            {
                return false;
            }

            if (coreRuntime != null)
            {
                if (!coreRuntime.CanPaySkillCost(requestedSkill))
                {
                    return false;
                }

                bool hasHitConfirm = _skillExecutor.HasHitConfirm;
                if (!coreRuntime.TryConsumeSpecialDerivationCost(_skillExecutor.Skill, requestedSkill, grounded, hasHitConfirm))
                {
                    return false;
                }
            }

            _queuedSkill = new QueuedCombatSkill
            {
                SkillAsset = requestedSkill,
                SkillId = requestedSkill.ResolveSkillId()
            };
            return true;
        }

        private bool TryQueueSkill(CombatSkillDefinitionSO requestedSkill, bool grounded)
        {
            if (requestedSkill == null || _queuedSkill.IsValid)
            {
                return false;
            }

            if (!grounded && requestedSkill.skillKind == CombatSkillKind.Attack && requestedSkill == _skillExecutor.Skill)
            {
                return false;
            }

            CombatSkillDefinitionSO previousSkill = weaponRuntime != null ? weaponRuntime.LastResolvedSkill : null;
            if (!SkillGateEvaluator.CanEnterSkill(playerFsm, weaponRuntime, requestedSkill, previousSkill, out _))
            {
                return false;
            }

            if (coreRuntime != null && !coreRuntime.CanPaySkillCost(requestedSkill))
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

        private bool CanQueueSkillCancel()
        {
            WeaponCancelPermission permission = CurrentCancelPermission;
            return permission == WeaponCancelPermission.SkillOnly
                || permission == WeaponCancelPermission.DodgeAndSkill
                || permission == WeaponCancelPermission.DodgeGuardAndSkill
                || permission == WeaponCancelPermission.Free;
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

            if (coreRuntime != null && !coreRuntime.CanPaySkillCost(skill))
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

            if (coreRuntime != null)
            {
                coreRuntime.TryPaySkillCost(skill);
                coreRuntime.NotifySkillStarted(skill, grounded);
            }

            SkillStarted?.Invoke(skill);

            if (skill.skillKind == CombatSkillKind.Attack && !grounded && !AllowsRepeatedAerialAttackBeforeLanding())
            {
                _aerialAttackLockedUntilGrounded = true;
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

        private void RefreshAerialAttackLock()
        {
            if (!_aerialAttackLockedUntilGrounded || playerFsm == null)
            {
                return;
            }

            if (playerFsm.CheckGround())
            {
                _aerialAttackLockedUntilGrounded = false;
            }
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

        private Collider2D[] QueryHitTargets(HitSkillClip clip)
        {
            Vector3 center = ResolveHitCenter(clip);
            switch (clip.hitShape)
            {
                //case SkillHitShape.Sphere:
                //    return Physics2D.OverlapSphere(center, Mathf.Max(0.01f, clip.size.x * 0.5f), clip.targetLayers);
                //case SkillHitShape.Capsule:
                //    float radius = Mathf.Max(0.01f, clip.size.x * 0.5f);
                //    float halfHeight = Mathf.Max(radius, clip.size.y * 0.5f);
                //    Vector3 point1 = center + Vector3.up * (halfHeight - radius);
                //    Vector3 point2 = center + Vector3.down * (halfHeight - radius);
                //    return Physics2D.OverlapCapsule(point1, point2, radius, clip.targetLayers);
                default:
                    Vector3 halfExtents = new Vector3(
                        Mathf.Max(0.01f, clip.size.x * 0.5f),
                        Mathf.Max(0.01f, clip.size.y * 0.5f),
                        Mathf.Max(0.01f, clip.size.z * 0.5f));
                    return Physics2D.OverlapBoxAll(center, halfExtents, 0f, clip.targetLayers);
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

        private Vector3 ResolveSpawnPosition(Vector3 localOffset)
        {
            if (playerFsm == null || playerFsm.Character == null)
            {
                return localOffset;
            }

            Vector3 offset = localOffset;
            if (IsFacingLeft())
            {
                offset.x = -offset.x;
            }

            return playerFsm.Character.position + offset;
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
            if (clip == null || playerFsm == null || playerFsm.Character == null)
            {
                return spawnRoot;
            }

            if (clip.spawnSpace == SkillVfxSpawnSpace.FollowCaster)
            {
                return playerFsm.Character;
            }

            if (clip.spawnSpace == SkillVfxSpawnSpace.Bone)
            {
                Transform socket = ResolveSocketTransform(clip.socketName);
                return socket != null ? socket : playerFsm.Character;
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
            if (playerFsm == null || playerFsm.Character == null || string.IsNullOrWhiteSpace(socketName))
            {
                return null;
            }

            Transform[] children = playerFsm.Character.GetComponentsInChildren<Transform>(true);
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
            float baseAttack = playerFsm != null ? playerFsm.GetStat(StatKeys.Attack, fallbackBaseAttack) : fallbackBaseAttack;
            if (baseAttack <= 0f)
            {
                baseAttack = fallbackBaseAttack;
            }

            float skillMultiplier = skill != null ? Mathf.Max(0f, skill.damageMultiplier) : 1f;
            float clipMultiplier = clip != null ? Mathf.Max(0f, clip.damageMultiplier) : 1f;
            float damage = baseAttack * skillMultiplier * clipMultiplier;
            return coreRuntime != null ? coreRuntime.ModifyDamageDealt(damage) : damage;
        }

        private void LogHitDiagnostic(string message)
        {
            if (!logHitDiagnostics)
            {
                return;
            }

            Debug.Log("[Combat Hit] " + message, this);
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

        private bool AllowsRepeatedAerialAttackBeforeLanding()
        {
            return coreRuntime != null && coreRuntime.AllowsRepeatedAerialAttackBeforeLanding();
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
