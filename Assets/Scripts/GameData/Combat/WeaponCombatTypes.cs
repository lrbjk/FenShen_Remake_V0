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
        SkillTrigger = 6,
    }

    public enum WeaponDerivationTrigger
    {
        ManualInput = 0,
        OnHit = 1,
        OnBlock = 2,
        OnPerfectDodge = 3,
        OnGuardSuccess = 4,
        OnResourceFull = 5,
        OnAerial = 6,
        OnGrounded = 7,
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
        public WeaponAttackSlot slot = WeaponAttackSlot.PrimaryGround;
        public FenShen.Combat.CombatSkillDefinitionSO skill;
        public string skillId;
        [TextArea] public string notes;
    }

    [Serializable]
    public class WeaponPassiveRuleEntry
    {
        public string ruleId;
        public string displayName;
        [TextArea] public string description;
        public List<StatModifierEntry> extraModifiers = new List<StatModifierEntry>();
    }

    [Serializable]
    public class WeaponResourceInteraction
    {
        public WeaponResourceType resourceType = WeaponResourceType.None;
        public string customResourceKey;
        [Min(0f)] public float gainMultiplier = 1f;
        [Min(0f)] public float hitGain = 0f;
        [Min(0f)] public float killGain = 0f;
        [Min(0f)] public float dodgeGain = 0f;
        [Min(0f)] public float perfectDodgeGain = 0f;
        [Min(0f)] public float guardGain = 0f;
        [Min(0f)] public float passiveRegenPerSecond = 0f;
    }

    [Serializable]
    public class WeaponComboBranch
    {
        public string branchId;
        public FenShen.Combat.CombatSkillDefinitionSO fromSkill;
        public string fromSkillId;
        public FenShen.Combat.CombatSkillDefinitionSO toSkill;
        public string toSkillId;
        public WeaponDerivationTrigger trigger = WeaponDerivationTrigger.ManualInput;
        public bool requiresHitConfirm;
        public bool requiresGrounded;
        public bool requiresAerial;
        public bool consumeResourceOnEnter;
        [Min(0f)] public float windowStartNormalizedTime = 0.2f;
        [Min(0f)] public float windowEndNormalizedTime = 0.8f;
        public WeaponCancelPermission cancelPermission = WeaponCancelPermission.SkillOnly;
    }

    [Serializable]
    public class WeaponSpecialDerivationWindow
    {
        public string windowId;
        public FenShen.Combat.CombatSkillDefinitionSO sourceSkill;
        public string sourceSkillId;
        [TextArea] public string description;
        public WeaponDerivationTrigger trigger = WeaponDerivationTrigger.ManualInput;
        [Min(0f)] public float windowStartNormalizedTime = 0.1f;
        [Min(0f)] public float windowEndNormalizedTime = 0.3f;
        public List<FenShen.Combat.CombatSkillDefinitionSO> nextSkills = new List<FenShen.Combat.CombatSkillDefinitionSO>();
        public List<string> nextSkillIds = new List<string>();
        public WeaponCancelPermission cancelPermission = WeaponCancelPermission.SkillOnly;
    }

    [Serializable]
    public class WeaponAirGroundRhythmProfile
    {
        [Min(0f)] public float groundedChainSpeedMultiplier = 1f;
        [Min(0f)] public float groundedRecoveryMultiplier = 1f;
        [Min(0f)] public float aerialChainSpeedMultiplier = 1f;
        [Min(0f)] public float aerialRecoveryMultiplier = 1f;
        [Min(0f)] public float landingChaseWindow = 0f;
        [Min(0f)] public float hitPauseMultiplier = 1f;
    }
}
