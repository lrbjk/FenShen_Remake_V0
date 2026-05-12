using System;
using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [Flags]
    public enum WeaponTraitTag
    {
        None = 0,
        Melee = 1 << 0,
        Ranged = 1 << 1,
        Aerial = 1 << 2,
        Charged = 1 << 3,
        Parry = 1 << 4,
        Guard = 1 << 5,
        Mobility = 1 << 6,
        Heavy = 1 << 7,
    }

    public enum WeaponAttackSlot
    {
        PrimaryGround = 0,
        PrimaryAir = 1,
        DashAttack = 2,
        DodgeFollowUp = 3,
        GuardCounter = 4,
        ChargedAttack = 5,
        Launcher = 7,
        Slam = 8,
        SpecialNeutral = 9,
        SpecialUp = 10,
        SpecialDown = 11,
    }

    public enum WeaponResourceType
    {
        None = 0,
        Stamina = 1,
        Energy = 2,
        Ammo = 3,
        Heat = 4,
        Style = 5,
        Guard = 6,
        Custom = 7,
    }

    public enum WeaponCancelPermission
    {
        None = 0,
        DodgeOnly = 1,
        GuardOnly = 2,
        SkillOnly = 3,
        DodgeAndSkill = 4,
        DodgeGuardAndSkill = 5,
        Free = 6,
    }

    [Serializable]
    public class WeaponSkillEntry
    {
        [InspectorName("入口槽位")]
        public WeaponAttackSlot slot = WeaponAttackSlot.PrimaryGround;
        [InspectorName("技能资产")]
        public FenShen.Combat.CombatSkillDefinitionSO skill;
        [InspectorName("技能ID")]
        public string skillId;
        [InspectorName("备注")]
        [TextArea] public string notes;
    }

    [Serializable]
    public class WeaponPassiveRuleEntry
    {
        [InspectorName("规则ID")]
        public string ruleId;
        [InspectorName("显示名称")]
        public string displayName;
        [InspectorName("描述")]
        [TextArea] public string description;
        [InspectorName("额外属性修正")]
        public List<StatModifierEntry> extraModifiers = new List<StatModifierEntry>();
    }

    [Serializable]
    public class WeaponResourceInteraction
    {
        [InspectorName("资源类型")]
        public WeaponResourceType resourceType = WeaponResourceType.None;
        [InspectorName("自定义资源键")]
        public string customResourceKey;
        [InspectorName("获取倍率")]
        [Min(0f)] public float gainMultiplier = 1f;
        [InspectorName("命中获取")]
        [Min(0f)] public float hitGain = 0f;
        [InspectorName("击杀获取")]
        [Min(0f)] public float killGain = 0f;
        [InspectorName("闪避获取")]
        [Min(0f)] public float dodgeGain = 0f;
        [InspectorName("完美闪避获取")]
        [Min(0f)] public float perfectDodgeGain = 0f;
        [InspectorName("防御获取")]
        [Min(0f)] public float guardGain = 0f;
        [InspectorName("每秒自然恢复")]
        [Min(0f)] public float passiveRegenPerSecond = 0f;
    }

    [Serializable]
    public class WeaponAirGroundRhythmProfile
    {
        [InspectorName("地面连段速度倍率")]
        [Min(0f)] public float groundedChainSpeedMultiplier = 1f;
        [InspectorName("地面后摇倍率")]
        [Min(0f)] public float groundedRecoveryMultiplier = 1f;
        [InspectorName("空中连段速度倍率")]
        [Min(0f)] public float aerialChainSpeedMultiplier = 1f;
        [InspectorName("空中后摇倍率")]
        [Min(0f)] public float aerialRecoveryMultiplier = 1f;
        [InspectorName("落地追击窗口")]
        [Min(0f)] public float landingChaseWindow = 0f;
        [InspectorName("命中停顿倍率")]
        [Min(0f)] public float hitPauseMultiplier = 1f;
    }
}
