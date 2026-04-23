using System;
using System.Collections.Generic;
using FenShen.GameData;
using UnityEngine;

namespace FenShen.Combat
{
    public enum SkillHitShape
    {
        Box = 0,
        Sphere = 1,
        Capsule = 2,
    }

    public enum SkillVfxSpawnSpace
    {
        World = 0,
        FollowCaster = 1,
        Bone = 2,
    }

    public enum ProjectileReleaseMode
    {
        Forward = 0,
        ToTarget = 1,
        FixedDirection = 2,
    }

    [Serializable]
    public abstract class SkillClipBase
    {
        public string clipId;
        public string displayName;
        [TextArea] public string note;
        [Min(0f)] public float startTime;
        [Min(0f)] public float duration;
        public bool enabled = true;
    }

    [Serializable]
    public class AnimationSkillClip : SkillClipBase
    {
        public string animationStateName;
        public int layer;
        public bool crossFade = true;
        public float transitionDuration = 0.05f;
    }

    [Serializable]
    public class MovementSkillClip : SkillClipBase
    {
        public SkillMotionSource motionSource = SkillMotionSource.AnimatorCurve;
        public string curveXName = "MoveX";
        public string curveYName = "MoveY";
        public bool mirrorByFacing = true;
        public AnimationCurve customCurveX = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        public AnimationCurve customCurveY = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    }

    [Serializable]
    public class HitSkillClip : SkillClipBase
    {
        public SkillHitShape hitShape = SkillHitShape.Box;
        public Vector3 offset;
        public Vector3 size = Vector3.one;
        public LayerMask targetLayers = ~0;
        public bool followsCaster = true;
        public bool allowRepeatHitsOnSameTarget;
        public bool isContinuous;
        [Min(0f)] public float tickInterval = 0.1f;
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0f)] public float poiseDamage = 0f;
    }

    [Serializable]
    public class ProjectileSkillClip : SkillClipBase
    {
        public GameObject projectilePrefab;
        public ProjectileReleaseMode releaseMode = ProjectileReleaseMode.Forward;
        public Vector3 spawnOffset;
        public Vector3 fixedDirection = Vector3.right;
        [Min(0f)] public float speed = 10f;
        [Min(0f)] public float lifetime = 3f;
    }

    [Serializable]
    public class VfxSkillClip : SkillClipBase
    {
        public GameObject effectPrefab;
        public SkillVfxSpawnSpace spawnSpace = SkillVfxSpawnSpace.FollowCaster;
        public string socketName;
        public Vector3 localOffset;
    }

    [Serializable]
    public class SfxSkillClip : SkillClipBase
    {
        public AudioClip audioClip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool spatial = true;
    }

    [Serializable]
    public class CameraShakeSkillClip : SkillClipBase
    {
        [Min(0f)] public float amplitude = 1f;
        [Min(0f)] public float frequency = 20f;
    }

    [Serializable]
    public class CancelWindowSkillClip : SkillClipBase
    {
        public WeaponCancelPermission cancelPermission = WeaponCancelPermission.SkillOnly;
    }

    [Serializable]
    public class DerivationWindowSkillClip : SkillClipBase
    {
        public List<CombatSkillDefinitionSO> nextSkills = new List<CombatSkillDefinitionSO>();
        public bool requiresHitConfirm;
        public bool requiresGrounded;
        public bool requiresAerial;
    }

    [CreateAssetMenu(fileName = "SkillTimeline", menuName = "Game Data/Combat/Skill Timeline")]
    public class SkillTimelineSO : ScriptableObject
    {
        [Min(0.01f)] public float length = 0.6f;

        [Header("Tracks")]
        public List<AnimationSkillClip> animationClips = new List<AnimationSkillClip>();
        public List<MovementSkillClip> movementClips = new List<MovementSkillClip>();
        public List<HitSkillClip> hitClips = new List<HitSkillClip>();
        public List<ProjectileSkillClip> projectileClips = new List<ProjectileSkillClip>();
        public List<VfxSkillClip> vfxClips = new List<VfxSkillClip>();
        public List<SfxSkillClip> sfxClips = new List<SfxSkillClip>();
        public List<CameraShakeSkillClip> cameraClips = new List<CameraShakeSkillClip>();
        public List<CancelWindowSkillClip> cancelWindowClips = new List<CancelWindowSkillClip>();
        public List<DerivationWindowSkillClip> derivationWindowClips = new List<DerivationWindowSkillClip>();
    }
}
