using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Game Data/Combat/Weapon Definition")]
    public class WeaponDefinitionSO : ScriptableObject
    {
        [InspectorName("版本")]
        public int version = 2;
        [InspectorName("武器ID")]
        public string weaponId;
        [InspectorName("显示名称")]
        public string displayName;
        [InspectorName("描述")]
        [TextArea] public string description;
        [InspectorName("攻击类型")]
        public WeaponAttackType attackType = WeaponAttackType.Melee;
        [InspectorName("伤害类型")]
        public DamageType damageType = DamageType.Physical;
        [InspectorName("武器标签")]
        public WeaponTraitTag traitTags = WeaponTraitTag.Melee;

        [Header("基础数值")]
        [InspectorName("攻击力")]
        [Min(0f)] public float attackPower = 10f;
        [InspectorName("攻击倍率")]
        [Min(0f)] public float attackMultiplier = 1f;
        [InspectorName("暴击率加成")]
        [Range(0f, 1f)] public float critRateBonus = 0f;
        [InspectorName("暴击伤害加成")]
        [Min(0f)] public float critDamageBonus = 0f;
        [InspectorName("攻速倍率")]
        [Min(0f)] public float attackSpeedMultiplier = 1f;
        [InspectorName("体力/能量消耗")]
        [Min(0f)] public float staminaOrEnergyCost = 0f;
        [InspectorName("削韧")]
        [Min(0f)] public float poiseDamage = 0f;

        [Header("范围")]
        [InspectorName("攻击范围")]
        [Min(0f)] public float range = 1.5f;
        [InspectorName("击退力")]
        [Min(0f)] public float knockbackPower = 0f;

        [Header("战斗节奏")]
        [InspectorName("默认取消权限")]
        public WeaponCancelPermission defaultCancelPermission = WeaponCancelPermission.DodgeAndSkill;
        [InspectorName("连段资源获取倍率")]
        [Min(0f)] public float comboResourceGainMultiplier = 1f;
        [InspectorName("命中资源获取倍率")]
        [Min(0f)] public float hitResourceGainMultiplier = 1f;

        [Header("资产")]
        [InspectorName("招式表")]
        public WeaponMoveSetSO moveSet;
        [InspectorName("数值配置")]
        public WeaponStatProfileSO statProfile;

        [Header("入口技能")]
        [InspectorName("地面普攻入口技能")]
        public FenShen.Combat.CombatSkillDefinitionSO primaryAttackSkill;
        [InspectorName("地面普攻入口技能ID")]
        public string primaryAttackSkillId = "Primary";
        [InspectorName("空中普攻入口技能")]
        public FenShen.Combat.CombatSkillDefinitionSO aerialPrimaryAttackSkill;
        [InspectorName("空中普攻入口技能ID")]
        public string aerialPrimaryAttackSkillId;
        [InspectorName("冲刺攻击技能")]
        public FenShen.Combat.CombatSkillDefinitionSO dashAttackSkill;
        [InspectorName("冲刺攻击技能ID")]
        public string dashAttackSkillId;
        [InspectorName("闪避追击技能")]
        public FenShen.Combat.CombatSkillDefinitionSO dodgeFollowUpSkill;
        [InspectorName("闪避追击技能ID")]
        public string dodgeFollowUpSkillId;
        [InspectorName("格挡反击技能")]
        public FenShen.Combat.CombatSkillDefinitionSO guardCounterSkill;
        [InspectorName("格挡反击技能ID")]
        public string guardCounterSkillId;
        [InspectorName("蓄力攻击技能")]
        public FenShen.Combat.CombatSkillDefinitionSO chargedAttackSkill;
        [InspectorName("蓄力攻击技能ID")]
        public string chargedAttackSkillId;

        [Header("资源")]
        [InspectorName("资源交互")]
        public WeaponResourceInteraction resourceInteraction = new WeaponResourceInteraction();

        [Header("被动")]
        [InspectorName("被动规则")]
        public List<WeaponPassiveRuleEntry> passiveRules = new List<WeaponPassiveRuleEntry>();

        [Header("成长")]
        [InspectorName("等级成长曲线")]
        public AnimationCurve levelScaling = AnimationCurve.Linear(1f, 1f, 50f, 2f);
        [InspectorName("额外属性修正")]
        public List<StatModifierEntry> extraModifiers = new List<StatModifierEntry>();

        public FenShen.Combat.CombatSkillDefinitionSO GetEntrySkillAsset(WeaponAttackSlot slot)
        {
            if (moveSet != null)
            {
                FenShen.Combat.CombatSkillDefinitionSO moveSetSkill = moveSet.GetEntrySkillAsset(slot);
                if (moveSetSkill != null)
                {
                    return moveSetSkill;
                }

                if (slot == WeaponAttackSlot.PrimaryGround)
                {
                    FenShen.Combat.CombatSkillDefinitionSO groundStarter = moveSet.GetPrimaryComboSkillAsset(0, true);
                    if (groundStarter != null)
                    {
                        return groundStarter;
                    }
                }

                if (slot == WeaponAttackSlot.PrimaryAir)
                {
                    FenShen.Combat.CombatSkillDefinitionSO airStarter = moveSet.GetPrimaryComboSkillAsset(0, false);
                    if (airStarter != null)
                    {
                        return airStarter;
                    }
                }
            }

            switch (slot)
            {
                case WeaponAttackSlot.PrimaryGround:
                    return primaryAttackSkill;
                case WeaponAttackSlot.PrimaryAir:
                    return aerialPrimaryAttackSkill;
                case WeaponAttackSlot.DashAttack:
                    return dashAttackSkill;
                case WeaponAttackSlot.DodgeFollowUp:
                    return dodgeFollowUpSkill;
                case WeaponAttackSlot.GuardCounter:
                    return guardCounterSkill;
                case WeaponAttackSlot.ChargedAttack:
                    return chargedAttackSkill;
                default:
                    return primaryAttackSkill;
            }
        }

        public string GetEntrySkillId(WeaponAttackSlot slot)
        {
            if (moveSet != null)
            {
                string moveSetSkill = moveSet.GetEntrySkillId(slot);
                if (!string.IsNullOrWhiteSpace(moveSetSkill))
                {
                    return moveSetSkill;
                }

                if (slot == WeaponAttackSlot.PrimaryGround)
                {
                    string groundStarter = moveSet.GetPrimaryComboSkillId(0, true);
                    if (!string.IsNullOrWhiteSpace(groundStarter))
                    {
                        return groundStarter;
                    }
                }

                if (slot == WeaponAttackSlot.PrimaryAir)
                {
                    string airStarter = moveSet.GetPrimaryComboSkillId(0, false);
                    if (!string.IsNullOrWhiteSpace(airStarter))
                    {
                        return airStarter;
                    }
                }
            }

            switch (slot)
            {
                case WeaponAttackSlot.PrimaryGround:
                    return primaryAttackSkillId;
                case WeaponAttackSlot.PrimaryAir:
                    return aerialPrimaryAttackSkillId;
                case WeaponAttackSlot.DashAttack:
                    return dashAttackSkillId;
                case WeaponAttackSlot.DodgeFollowUp:
                    return dodgeFollowUpSkillId;
                case WeaponAttackSlot.GuardCounter:
                    return guardCounterSkillId;
                case WeaponAttackSlot.ChargedAttack:
                    return chargedAttackSkillId;
                default:
                    return primaryAttackSkillId;
            }
        }

        public bool HasTrait(WeaponTraitTag tag)
        {
            return (traitTags & tag) == tag;
        }
    }
}
