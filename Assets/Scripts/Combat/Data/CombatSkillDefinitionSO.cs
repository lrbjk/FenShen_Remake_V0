using System;
using System.Collections.Generic;
using FenShen.GameData;
using FenShen.PlayerFSM;
using UnityEngine;

namespace FenShen.Combat
{
    public enum SkillUsageSide
    {
        Both = 0,
        PlayerOnly = 1,
        EnemyOnly = 2,
    }

    public enum SkillMotionSource
    {
        None = 0,
        AnimatorCurve = 1,
        CustomCurve = 2,
    }

    public enum CombatSkillKind
    {
        Any = -1,
        Attack = 0,
        Movement = 1,
        Defense = 2,
        Control = 3,
        ResourceConvert = 4,
        Finisher = 5,
        CoreTrigger = 6,
    }

    [Flags]
    public enum CombatSkillRoleTag
    {
        None = 0,
        BurstDamage = 1 << 0,
        DashEngage = 1 << 1,
        CrowdControl = 1 << 2,
        InvulnerableSave = 1 << 3,
        ResourceConvert = 1 << 4,
        ComboFinisher = 1 << 5,
        BuildCoreTrigger = 1 << 6,
    }

    [Serializable]
    public class SkillMotionSettings
    {
        [InspectorName("位移来源")]
        public SkillMotionSource motionSource = SkillMotionSource.AnimatorCurve;
        [InspectorName("按角色朝向镜像")]
        public bool mirrorByFacing = true;
        [InspectorName("Animator X位移曲线名")]
        public string curveXName = "MoveX";
        [InspectorName("Animator Y位移曲线名")]
        public string curveYName = "MoveY";
        [InspectorName("自定义X位移曲线")]
        public AnimationCurve customCurveX = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        [InspectorName("自定义Y位移曲线")]
        public AnimationCurve customCurveY = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    }

    [Serializable]
    public class SkillConditionGroup
    {
        [InspectorName("条件逻辑")]
        public ConditionLogic logic = ConditionLogic.All;
        [InspectorName("条件列表")]
        public List<ConditionSO> conditions = new List<ConditionSO>();
    }

    [Serializable]
    public class SkillDerivationRule
    {
        [InspectorName("规则ID")]
        public string ruleId;
        [InspectorName("下一个技能")]
        public CombatSkillDefinitionSO nextSkill;
        [InspectorName("说明")]
        [TextArea] public string description;
        [InspectorName("窗口开始时间")]
        [Min(0f)] public float windowStartTime = 0f;
        [InspectorName("窗口结束时间")]
        [Min(0f)] public float windowEndTime = 0.2f;
        [InspectorName("需要命中确认")]
        public bool requiresHitConfirm;
        [InspectorName("需要在地面")]
        public bool requiresGrounded;
        [InspectorName("需要在空中")]
        public bool requiresAerial;
        [InspectorName("需要完美闪避")]
        public bool requiresPerfectDodge;
        [InspectorName("需要防御成功")]
        public bool requiresGuardSuccess;
    }

    [Serializable]
    public class SkillRecoveryRule
    {
        [InspectorName("规则ID")]
        public string ruleId;
        [InspectorName("显示名称")]
        public string displayName;
        [InspectorName("收招后进入技能")]
        public CombatSkillDefinitionSO nextSkill;
        [InspectorName("说明")]
        [TextArea] public string description;
        [InspectorName("需要命中确认")]
        public bool requiresHitConfirm;
        [InspectorName("需要在地面")]
        public bool requiresGrounded;
        [InspectorName("需要在空中")]
        public bool requiresAerial;
        [InspectorName("额外条件")]
        public SkillConditionGroup conditions = new SkillConditionGroup();
    }

    [CreateAssetMenu(fileName = "CombatSkillDefinition", menuName = "Game Data/Combat/Combat Skill Definition")]
    public class CombatSkillDefinitionSO : ScriptableObject
    {
        [Header("身份信息")]
        [InspectorName("技能ID")]
        public string skillId;
        [InspectorName("显示名称")]
        public string displayName;
        [InspectorName("描述")]
        [TextArea] public string description;
        [InspectorName("图标")]
        public Sprite icon;
        [InspectorName("使用方")]
        public SkillUsageSide usageSide = SkillUsageSide.Both;
        [InspectorName("技能类型")]
        public CombatSkillKind skillKind = CombatSkillKind.Attack;
        [InspectorName("战斗定位")]
        public CombatSkillRoleTag roleTags = CombatSkillRoleTag.None;
        [InspectorName("标签")]
        public List<string> tags = new List<string>();

        [Header("表现")]
        [InspectorName("动画状态名")]
        public string animationStateName;
        [InspectorName("动画层")]
        public int animationLayer;
        [InspectorName("使用过渡")]
        public bool crossFade = true;
        [InspectorName("过渡时间")]
        public float transitionDuration = 0.05f;
        [InspectorName("预览动画")]
        public AnimationClip previewAnimationClip;

        [Header("时间轴")]
        [InspectorName("技能时间轴")]
        public SkillTimelineSO timeline;
        [InspectorName("位移配置")]
        public SkillMotionSettings motion = new SkillMotionSettings();

        [Header("时间参数")]
        [InspectorName("持续时间")]
        [Min(0.01f)] public float duration = 0.6f;
        [InspectorName("前摇")]
        [Min(0f)] public float startup = 0.1f;
        [InspectorName("生效时间")]
        [Min(0f)] public float active = 0.1f;
        [InspectorName("后摇")]
        [Min(0f)] public float recovery = 0.2f;
        [InspectorName("冷却")]
        [Min(0f)] public float cooldown = 0f;

        [Header("战斗参数")]
        [InspectorName("伤害倍率")]
        [Min(0f)] public float damageMultiplier = 1f;
        [InspectorName("削韧")]
        [Min(0f)] public float poiseDamage = 0f;
        [InspectorName("资源消耗")]
        [Min(0f)] public float resourceCost = 0f;
        [InspectorName("仅地面")]
        public bool groundedOnly;
        [InspectorName("仅空中")]
        public bool aerialOnly;
        [InspectorName("需要武器标签")]
        public WeaponTraitTag requiredWeaponTraits = WeaponTraitTag.None;

        [Header("进入条件")]
        [InspectorName("进入条件组")]
        public SkillConditionGroup enterConditions = new SkillConditionGroup();
        [InspectorName("允许前置技能")]
        public List<CombatSkillDefinitionSO> previousSkills = new List<CombatSkillDefinitionSO>();

        [Header("派生")]
        [InspectorName("派生规则")]
        public List<SkillDerivationRule> derivationRules = new List<SkillDerivationRule>();

        [Header("收招")]
        [InspectorName("收招规则")]
        public List<SkillRecoveryRule> recoveryRules = new List<SkillRecoveryRule>();

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    return displayName;
                }

                if (!string.IsNullOrWhiteSpace(skillId))
                {
                    return skillId;
                }

                return name;
            }
        }

        public string ResolveSkillId()
        {
            return !string.IsNullOrWhiteSpace(skillId) ? skillId : name;
        }

        public float ResolveDuration()
        {
            if (timeline != null)
            {
                return Mathf.Max(0.01f, timeline.length);
            }

            return Mathf.Max(0.01f, duration);
        }

        public float ResolvePreviewFrameRate()
        {
            return previewAnimationClip != null ? Mathf.Max(1f, previewAnimationClip.frameRate) : 60f;
        }

        public bool RequiresPreviousSkill
        {
            get { return previousSkills != null && previousSkills.Count > 0; }
        }
    }
}
