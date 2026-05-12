using System.Collections.Generic;
using FenShen.GameData;
using UnityEngine;

namespace FenShen.Combat
{
    public sealed class SkillExecutor
    {
        private CombatSkillDefinitionSO _skill;
        private string _fallbackSkillId;
        private float _fallbackDuration;
        private string _fallbackAnimationStateName;
        private int _fallbackAnimationLayer;
        private bool _fallbackCrossFade;
        private float _fallbackTransitionDuration;
        private float _elapsed;
        private bool _fallbackAttackTriggered;
        private bool _hasHitConfirm;
        private readonly HashSet<int> _triggeredAnimationClips = new HashSet<int>();
        private readonly HashSet<int> _triggeredHitClips = new HashSet<int>();
        private readonly Dictionary<int, float> _continuousHitNextTickTimes = new Dictionary<int, float>();
        private readonly HashSet<int> _triggeredProjectileClips = new HashSet<int>();
        private readonly HashSet<int> _triggeredVfxClips = new HashSet<int>();
        private readonly HashSet<int> _triggeredSfxClips = new HashSet<int>();
        private readonly HashSet<int> _triggeredCameraClips = new HashSet<int>();
        private readonly HashSet<int> _triggeredSelfBuffClips = new HashSet<int>();
        private readonly HashSet<int> _triggeredCoreResourceClips = new HashSet<int>();
        private readonly Dictionary<int, Vector2> _movementClipPreviousSamples = new Dictionary<int, Vector2>();
        private readonly Dictionary<int, HashSet<int>> _hitTargetsByClip = new Dictionary<int, HashSet<int>>();

        public CombatSkillDefinitionSO Skill
        {
            get { return _skill; }
        }

        public float Elapsed
        {
            get { return _elapsed; }
        }

        public bool IsActive
        {
            get { return _skill != null || !string.IsNullOrWhiteSpace(_fallbackSkillId); }
        }

        public bool HasHitConfirm
        {
            get { return _hasHitConfirm; }
        }

        public List<HitSkillClip> GetActiveHitClips()
        {
            List<HitSkillClip> results = new List<HitSkillClip>();
            if (_skill == null || _skill.timeline == null || _skill.timeline.hitClips == null)
            {
                return results;
            }

            for (int i = 0; i < _skill.timeline.hitClips.Count; i++)
            {
                HitSkillClip clip = _skill.timeline.hitClips[i];
                if (clip != null && clip.enabled && IsWithinClip(_elapsed, clip))
                {
                    results.Add(clip);
                }
            }

            return results;
        }

        public List<MovementSkillClip> GetActiveMovementClips()
        {
            List<MovementSkillClip> results = new List<MovementSkillClip>();
            if (_skill == null || _skill.timeline == null || _skill.timeline.movementClips == null)
            {
                return results;
            }

            for (int i = 0; i < _skill.timeline.movementClips.Count; i++)
            {
                MovementSkillClip clip = _skill.timeline.movementClips[i];
                if (clip != null && clip.enabled && IsWithinClip(_elapsed, clip))
                {
                    results.Add(clip);
                }
            }

            return results;
        }

        public void Begin(
            CombatCoordinator coordinator,
            CombatSkillDefinitionSO skill,
            string fallbackSkillId,
            float fallbackDuration,
            string fallbackAnimationStateName,
            int fallbackAnimationLayer,
            bool fallbackCrossFade,
            float fallbackTransitionDuration)
        {
            Reset();

            _skill = skill;
            _fallbackSkillId = fallbackSkillId;
            _fallbackDuration = Mathf.Max(0.01f, fallbackDuration);
            _fallbackAnimationStateName = fallbackAnimationStateName;
            _fallbackAnimationLayer = fallbackAnimationLayer;
            _fallbackCrossFade = fallbackCrossFade;
            _fallbackTransitionDuration = fallbackTransitionDuration;

            if (_skill == null || _skill.timeline == null)
            {
                coordinator.PlayCombatAnimation(_fallbackAnimationStateName, _fallbackAnimationLayer, _fallbackCrossFade, _fallbackTransitionDuration);
                coordinator.ExecuteAttack(_skill, ResolveSkillId());
                _fallbackAttackTriggered = true;
                return;
            }

            if (_skill.timeline.animationClips == null || _skill.timeline.animationClips.Count == 0)
            {
                coordinator.PlayCombatAnimation(
                    _skill.animationStateName,
                    _skill.animationLayer,
                    _skill.crossFade,
                    _skill.transitionDuration);
            }
        }

        public bool Tick(CombatCoordinator coordinator, float deltaTime)
        {
            if (!IsActive)
            {
                return true;
            }

            float previousTime = _elapsed;
            _elapsed += deltaTime;

            if (_skill != null && _skill.timeline != null)
            {
                ProcessAnimationClips(coordinator, previousTime);
                ProcessMovementClips(coordinator);
                ProcessHitClips(coordinator, previousTime);
                ProcessProjectileClips(coordinator, previousTime);
                ProcessVfxClips(coordinator, previousTime);
                ProcessSfxClips(coordinator, previousTime);
                ProcessCameraClips(coordinator, previousTime);
                ProcessSelfBuffClips(coordinator, previousTime);
                ProcessCoreResourceClips(coordinator, previousTime);
                return _elapsed >= _skill.ResolveDuration();
            }

            if (!_fallbackAttackTriggered)
            {
                coordinator.ExecuteAttack(null, ResolveSkillId());
                _fallbackAttackTriggered = true;
            }

            return _elapsed >= _fallbackDuration;
        }

        public void NotifyHitConfirmed(CombatSkillDefinitionSO skill, string skillId)
        {
            if (!IsActive)
            {
                return;
            }

            if (_skill != null)
            {
                if (skill == _skill)
                {
                    _hasHitConfirm = true;
                    return;
                }

                if (!string.IsNullOrWhiteSpace(skillId) && skillId == _skill.ResolveSkillId())
                {
                    _hasHitConfirm = true;
                }

                return;
            }

            if (!string.IsNullOrWhiteSpace(skillId) && skillId == _fallbackSkillId)
            {
                _hasHitConfirm = true;
            }
        }

        public void End()
        {
            Reset();
        }

        public WeaponCancelPermission GetCurrentCancelPermission()
        {
            if (_skill == null || _skill.timeline == null || _skill.timeline.cancelWindowClips == null)
            {
                return WeaponCancelPermission.None;
            }

            for (int i = 0; i < _skill.timeline.cancelWindowClips.Count; i++)
            {
                CancelWindowSkillClip clip = _skill.timeline.cancelWindowClips[i];
                if (clip != null && clip.enabled && IsWithinClip(_elapsed, clip))
                {
                    return clip.cancelPermission;
                }
            }

            return WeaponCancelPermission.None;
        }

        public List<CombatSkillDefinitionSO> GetCurrentDerivations(bool grounded)
        {
            List<CombatSkillDefinitionSO> results = new List<CombatSkillDefinitionSO>();
            if (_skill == null || _skill.timeline == null || _skill.timeline.derivationWindowClips == null)
            {
                return results;
            }

            for (int i = 0; i < _skill.timeline.derivationWindowClips.Count; i++)
            {
                DerivationWindowSkillClip clip = _skill.timeline.derivationWindowClips[i];
                if (clip == null || !clip.enabled || !IsWithinClip(_elapsed, clip) || clip.nextSkills == null)
                {
                    continue;
                }

                if (clip.requiresHitConfirm && !_hasHitConfirm)
                {
                    continue;
                }

                if (clip.requiresGrounded && !grounded)
                {
                    continue;
                }

                if (clip.requiresAerial && grounded)
                {
                    continue;
                }

                for (int j = 0; j < clip.nextSkills.Count; j++)
                {
                    CombatSkillDefinitionSO nextSkill = clip.nextSkills[j];
                    if (nextSkill != null && !results.Contains(nextSkill))
                    {
                        results.Add(nextSkill);
                    }
                }
            }

            return results;
        }

        private void ProcessAnimationClips(CombatCoordinator coordinator, float previousTime)
        {
            List<AnimationSkillClip> clips = _skill.timeline.animationClips;
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                AnimationSkillClip clip = clips[i];
                if (clip == null || !clip.enabled || _triggeredAnimationClips.Contains(i))
                {
                    continue;
                }

                if (previousTime <= clip.startTime && _elapsed >= clip.startTime)
                {
                    _triggeredAnimationClips.Add(i);
                    coordinator.PlayCombatAnimation(clip.animationStateName, clip.layer, clip.crossFade, clip.transitionDuration);
                }
            }
        }

        private void ProcessMovementClips(CombatCoordinator coordinator)
        {
            List<MovementSkillClip> clips = _skill.timeline.movementClips;
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                MovementSkillClip clip = clips[i];
                if (clip == null || !clip.enabled || !IsWithinClip(_elapsed, clip))
                {
                    continue;
                }

                float clipTime = _elapsed - clip.startTime;
                float normalizedTime = clip.duration > 0.0001f ? Mathf.Clamp01(clipTime / clip.duration) : 1f;
                Vector2 sample = SampleMovement(clip, normalizedTime, coordinator);
                _movementClipPreviousSamples.TryGetValue(i, out Vector2 previousSample);
                Vector2 delta = sample - previousSample;
                _movementClipPreviousSamples[i] = sample;
                if (delta.sqrMagnitude > 0.000001f)
                {
                    coordinator.ApplySkillMotion(delta);
                }
            }
        }

        private void ProcessHitClips(CombatCoordinator coordinator, float previousTime)
        {
            List<HitSkillClip> clips = _skill.timeline.hitClips;
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                HitSkillClip clip = clips[i];
                if (clip == null || !clip.enabled)
                {
                    continue;
                }

                if (!clip.isContinuous)
                {
                    if (_triggeredHitClips.Contains(i))
                    {
                        continue;
                    }

                    if (previousTime <= clip.startTime && _elapsed >= clip.startTime)
                    {
                        _triggeredHitClips.Add(i);
                        ProcessHitClipImpact(coordinator, clip, i);
                    }

                    continue;
                }

                if (!IsWithinClip(_elapsed, clip))
                {
                    continue;
                }

                if (!_continuousHitNextTickTimes.TryGetValue(i, out float nextTickTime))
                {
                    nextTickTime = clip.startTime;
                }

                while (_elapsed >= nextTickTime && nextTickTime <= clip.startTime + clip.duration + 0.0001f)
                {
                    ProcessHitClipImpact(coordinator, clip, i);
                    float interval = Mathf.Max(0.01f, clip.tickInterval);
                    nextTickTime += interval;
                }

                _continuousHitNextTickTimes[i] = nextTickTime;
            }
        }

        private void ProcessProjectileClips(CombatCoordinator coordinator, float previousTime)
        {
            List<ProjectileSkillClip> clips = _skill.timeline.projectileClips;
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                ProjectileSkillClip clip = clips[i];
                if (clip == null || !clip.enabled || _triggeredProjectileClips.Contains(i))
                {
                    continue;
                }

                if (previousTime <= clip.startTime && _elapsed >= clip.startTime)
                {
                    _triggeredProjectileClips.Add(i);
                    coordinator.SpawnProjectile(clip);
                }
            }
        }

        private void ProcessVfxClips(CombatCoordinator coordinator, float previousTime)
        {
            List<VfxSkillClip> clips = _skill.timeline.vfxClips;
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                VfxSkillClip clip = clips[i];
                if (clip == null || !clip.enabled || _triggeredVfxClips.Contains(i))
                {
                    continue;
                }

                if (previousTime <= clip.startTime && _elapsed >= clip.startTime)
                {
                    _triggeredVfxClips.Add(i);
                    coordinator.SpawnVfx(clip);
                }
            }
        }

        private void ProcessSfxClips(CombatCoordinator coordinator, float previousTime)
        {
            List<SfxSkillClip> clips = _skill.timeline.sfxClips;
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                SfxSkillClip clip = clips[i];
                if (clip == null || !clip.enabled || _triggeredSfxClips.Contains(i))
                {
                    continue;
                }

                if (previousTime <= clip.startTime && _elapsed >= clip.startTime)
                {
                    _triggeredSfxClips.Add(i);
                    coordinator.PlaySfx(clip);
                }
            }
        }

        private void ProcessCameraClips(CombatCoordinator coordinator, float previousTime)
        {
            List<CameraShakeSkillClip> clips = _skill.timeline.cameraClips;
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                CameraShakeSkillClip clip = clips[i];
                if (clip == null || !clip.enabled || _triggeredCameraClips.Contains(i))
                {
                    continue;
                }

                if (previousTime <= clip.startTime && _elapsed >= clip.startTime)
                {
                    _triggeredCameraClips.Add(i);
                    coordinator.TriggerCameraShake(clip);
                }
            }
        }

        private Vector2 SampleMovement(MovementSkillClip clip, float normalizedTime, CombatCoordinator coordinator)
        {
            if (clip.motionSource == SkillMotionSource.AnimatorCurve)
            {
                return coordinator.SampleAnimatorMotion(
                    clip.curveXName,
                    clip.curveYName,
                    clip.mirrorByFacing);
            }

            if (clip.motionSource == SkillMotionSource.CustomCurve)
            {
                float x = clip.customCurveX != null ? clip.customCurveX.Evaluate(normalizedTime) : 0f;
                float y = clip.customCurveY != null ? clip.customCurveY.Evaluate(normalizedTime) : 0f;
                if (clip.mirrorByFacing && coordinator.IsFacingLeft())
                {
                    x = -x;
                }

                return new Vector2(x, y);
            }

            return Vector2.zero;
        }

        private bool IsWithinClip(float time, SkillClipBase clip)
        {
            float endTime = clip.duration > 0f ? clip.startTime + clip.duration : clip.startTime;
            return time >= clip.startTime && time <= endTime;
        }

        private string ResolveSkillId()
        {
            if (_skill != null)
            {
                return _skill.ResolveSkillId();
            }

            return _fallbackSkillId;
        }

        private void Reset()
        {
            _skill = null;
            _fallbackSkillId = string.Empty;
            _fallbackDuration = 0.01f;
            _fallbackAnimationStateName = string.Empty;
            _fallbackAnimationLayer = 0;
            _fallbackCrossFade = true;
            _fallbackTransitionDuration = 0.05f;
            _elapsed = 0f;
            _fallbackAttackTriggered = false;
            _hasHitConfirm = false;
            _triggeredAnimationClips.Clear();
            _triggeredHitClips.Clear();
            _continuousHitNextTickTimes.Clear();
            _triggeredProjectileClips.Clear();
            _triggeredVfxClips.Clear();
            _triggeredSfxClips.Clear();
            _triggeredCameraClips.Clear();
            _triggeredSelfBuffClips.Clear();
            _triggeredCoreResourceClips.Clear();
            _movementClipPreviousSamples.Clear();
            _hitTargetsByClip.Clear();
        }

        private void ProcessSelfBuffClips(CombatCoordinator coordinator, float previousTime)
        {
            List<SelfBuffSkillClip> clips = _skill.timeline.selfBuffClips;
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                SelfBuffSkillClip clip = clips[i];
                if (clip == null || !clip.enabled || _triggeredSelfBuffClips.Contains(i))
                {
                    continue;
                }

                if (previousTime <= clip.startTime && _elapsed >= clip.startTime)
                {
                    _triggeredSelfBuffClips.Add(i);
                    coordinator.ApplySelfBuffClip(clip);
                }
            }
        }

        private void ProcessCoreResourceClips(CombatCoordinator coordinator, float previousTime)
        {
            List<CoreResourceSkillClip> clips = _skill.timeline.coreResourceClips;
            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Count; i++)
            {
                CoreResourceSkillClip clip = clips[i];
                if (clip == null || !clip.enabled || _triggeredCoreResourceClips.Contains(i))
                {
                    continue;
                }

                if (previousTime <= clip.startTime && _elapsed >= clip.startTime)
                {
                    _triggeredCoreResourceClips.Add(i);
                    coordinator.ApplyCoreResourceClip(clip);
                }
            }
        }

        private void ProcessHitClipImpact(CombatCoordinator coordinator, HitSkillClip clip, int clipIndex)
        {
            bool confirmed = coordinator.TryApplyHitClip(_skill, ResolveSkillId(), clip, GetOrCreateHitTargets(clipIndex));
            if (confirmed)
            {
                coordinator.NotifySkillHitConfirmed(_skill);
            }
        }

        private HashSet<int> GetOrCreateHitTargets(int clipIndex)
        {
            if (!_hitTargetsByClip.TryGetValue(clipIndex, out HashSet<int> targets))
            {
                targets = new HashSet<int>();
                _hitTargetsByClip.Add(clipIndex, targets);
            }

            return targets;
        }
    }
}
