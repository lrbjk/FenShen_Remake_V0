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
        [InspectorName("片段ID")]
        public string clipId;
        [InspectorName("显示名称")]
        public string displayName;
        [InspectorName("备注")]
        [TextArea] public string note;
        [InspectorName("开始时间")]
        [Min(0f)] public float startTime;
        [InspectorName("持续时间")]
        [Min(0f)] public float duration;
        [InspectorName("启用")]
        public bool enabled = true;
    }

    [Serializable]
    public class AnimationSkillClip : SkillClipBase
    {
        [InspectorName("动画状态名")]
        public string animationStateName;
        [InspectorName("动画层")]
        public int layer;
        [InspectorName("使用过渡")]
        public bool crossFade = true;
        [InspectorName("过渡时间")]
        public float transitionDuration = 0.05f;
    }

    [Serializable]
    public class MovementSkillClip : SkillClipBase
    {
        [InspectorName("位移来源")]
        public SkillMotionSource motionSource = SkillMotionSource.AnimatorCurve;
        [InspectorName("Animator X位移曲线名")]
        public string curveXName = "MoveX";
        [InspectorName("Animator Y位移曲线名")]
        public string curveYName = "MoveY";
        [InspectorName("按角色朝向镜像")]
        public bool mirrorByFacing = true;
        [InspectorName("自定义X位移曲线")]
        public AnimationCurve customCurveX = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        [InspectorName("自定义Y位移曲线")]
        public AnimationCurve customCurveY = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    }

    [Serializable]
    public class HitSkillClip : SkillClipBase
    {
        [InspectorName("打击形状")]
        public SkillHitShape hitShape = SkillHitShape.Box;
        [InspectorName("偏移")]
        public Vector3 offset;
        [InspectorName("尺寸")]
        public Vector3 size = Vector3.one;
        [InspectorName("目标层")]
        public LayerMask targetLayers = ~0;
        [InspectorName("跟随释放者")]
        public bool followsCaster = true;
        [InspectorName("允许重复命中同目标")]
        public bool allowRepeatHitsOnSameTarget;
        [InspectorName("持续攻击")]
        public bool isContinuous;
        [InspectorName("命中间隔")]
        [Min(0f)] public float tickInterval = 0.1f;
        [InspectorName("伤害倍率")]
        [Min(0f)] public float damageMultiplier = 1f;
        [InspectorName("削韧")]
        [Min(0f)] public float poiseDamage = 0f;

        [Header("命中反馈")]
        [InspectorName("命中停顿时长")]
        [Min(0f)] public float hitStopDuration;
        [InspectorName("命中停顿时间倍率")]
        [Range(0f, 1f)] public float hitStopScale = 0.05f;
        [InspectorName("命中镜头震动振幅")]
        [Min(0f)] public float cameraShakeAmplitude;
        [InspectorName("命中音效")]
        public AudioClip hitSfx;
        [InspectorName("命中特效预制体")]
        public GameObject hitVfxPrefab;
        [InspectorName("击退距离")]
        [Min(0f)] public float knockbackDistance;
        [InspectorName("击退上抛距离")]
        [Min(0f)] public float knockbackUpwardDistance;
        [InspectorName("击退持续时间")]
        [Min(0f)] public float knockbackDuration = 0.08f;
    }

    [Serializable]
    public class ProjectileSkillClip : SkillClipBase
    {
        [InspectorName("投射物预制体")]
        public GameObject projectilePrefab;
        [InspectorName("发射方式")]
        public ProjectileReleaseMode releaseMode = ProjectileReleaseMode.Forward;
        [InspectorName("生成偏移")]
        public Vector3 spawnOffset;
        [InspectorName("固定方向")]
        public Vector3 fixedDirection = Vector3.right;
        [InspectorName("速度")]
        [Min(0f)] public float speed = 10f;
        [InspectorName("生命周期")]
        [Min(0f)] public float lifetime = 3f;
    }

    [Serializable]
    public class VfxSkillClip : SkillClipBase
    {
        [InspectorName("特效预制体")]
        public GameObject effectPrefab;
        [InspectorName("生成空间")]
        public SkillVfxSpawnSpace spawnSpace = SkillVfxSpawnSpace.FollowCaster;
        [InspectorName("挂点名称")]
        public string socketName;
        [InspectorName("本地偏移")]
        public Vector3 localOffset;
    }

    [Serializable]
    public class SfxSkillClip : SkillClipBase
    {
        [InspectorName("音效")]
        public AudioClip audioClip;
        [InspectorName("音量")]
        [Range(0f, 1f)] public float volume = 1f;
        [InspectorName("空间音效")]
        public bool spatial = true;
    }

    [Serializable]
    public class CameraShakeSkillClip : SkillClipBase
    {
        [InspectorName("振幅")]
        [Min(0f)] public float amplitude = 1f;
        [InspectorName("频率")]
        [Min(0f)] public float frequency = 20f;
    }

    [Serializable]
    public class SelfBuffSkillClip : SkillClipBase
    {
        [InspectorName("Buff")]
        public BuffSO buff;
        [InspectorName("移除Buff")]
        public bool removeBuff;
    }

    [Serializable]
    public class CoreResourceSkillClip : SkillClipBase
    {
        [InspectorName("资源变化量")]
        public float amount;
        [InspectorName("资源不足时忽略")]
        public bool requireEnoughResource;
    }

    [Serializable]
    public class CancelWindowSkillClip : SkillClipBase
    {
        [InspectorName("取消权限")]
        public WeaponCancelPermission cancelPermission = WeaponCancelPermission.SkillOnly;
    }

    [Serializable]
    public class DerivationWindowSkillClip : SkillClipBase
    {
        [InspectorName("可派生技能")]
        public List<CombatSkillDefinitionSO> nextSkills = new List<CombatSkillDefinitionSO>();
        [InspectorName("需要命中确认")]
        public bool requiresHitConfirm;
        [InspectorName("需要在地面")]
        public bool requiresGrounded;
        [InspectorName("需要在空中")]
        public bool requiresAerial;
    }

    [CreateAssetMenu(fileName = "SkillTimeline", menuName = "Game Data/Combat/Skill Timeline")]
    public class SkillTimelineSO : ScriptableObject
    {
        [InspectorName("总长度")]
        [Min(0.01f)] public float length = 0.6f;

        [Header("轨道")]
        [InspectorName("动画片段")]
        public List<AnimationSkillClip> animationClips = new List<AnimationSkillClip>();
        [InspectorName("位移片段")]
        public List<MovementSkillClip> movementClips = new List<MovementSkillClip>();
        [InspectorName("打击片段")]
        public List<HitSkillClip> hitClips = new List<HitSkillClip>();
        [InspectorName("投射物片段")]
        public List<ProjectileSkillClip> projectileClips = new List<ProjectileSkillClip>();
        [InspectorName("特效片段")]
        public List<VfxSkillClip> vfxClips = new List<VfxSkillClip>();
        [InspectorName("音效片段")]
        public List<SfxSkillClip> sfxClips = new List<SfxSkillClip>();
        [InspectorName("镜头震动片段")]
        public List<CameraShakeSkillClip> cameraClips = new List<CameraShakeSkillClip>();
        [InspectorName("自身Buff片段")]
        public List<SelfBuffSkillClip> selfBuffClips = new List<SelfBuffSkillClip>();
        [InspectorName("核心资源片段")]
        public List<CoreResourceSkillClip> coreResourceClips = new List<CoreResourceSkillClip>();
        [InspectorName("取消窗口片段")]
        public List<CancelWindowSkillClip> cancelWindowClips = new List<CancelWindowSkillClip>();
        [InspectorName("派生窗口片段")]
        public List<DerivationWindowSkillClip> derivationWindowClips = new List<DerivationWindowSkillClip>();
    }
}
