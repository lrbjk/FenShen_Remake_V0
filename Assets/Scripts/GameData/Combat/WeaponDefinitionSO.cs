using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Game Data/Combat/Weapon Definition")]
    public class WeaponDefinitionSO : ScriptableObject
    {
        public int version = 2;
        public string weaponId;
        public string displayName;
        [TextArea] public string description;
        public WeaponAttackType attackType = WeaponAttackType.Melee;
        public DamageType damageType = DamageType.Physical;
        public WeaponTraitTag traitTags = WeaponTraitTag.Melee;

        [Header("Base")]
        [Min(0f)] public float attackPower = 10f;
        [Min(0f)] public float attackMultiplier = 1f;
        [Range(0f, 1f)] public float critRateBonus = 0f;
        [Min(0f)] public float critDamageBonus = 0f;
        [Min(0f)] public float attackSpeedMultiplier = 1f;
        [Min(0f)] public float staminaOrEnergyCost = 0f;
        [Min(0f)] public float poiseDamage = 0f;

        [Header("Range")]
        [Min(0f)] public float range = 1.5f;
        [Min(0f)] public float knockbackPower = 0f;

        [Header("Flow")]
        public WeaponCancelPermission defaultCancelPermission = WeaponCancelPermission.DodgeAndSkill;
        [Min(0f)] public float comboResourceGainMultiplier = 1f;
        [Min(0f)] public float hitResourceGainMultiplier = 1f;

        [Header("Assets")]
        public WeaponMoveSetSO moveSet;
        public WeaponStatProfileSO statProfile;

        [Header("Entry Skills")]
        public FenShen.Combat.CombatSkillDefinitionSO primaryAttackSkill;
        public string primaryAttackSkillId = "Primary";
        public FenShen.Combat.CombatSkillDefinitionSO aerialPrimaryAttackSkill;
        public string aerialPrimaryAttackSkillId;
        public FenShen.Combat.CombatSkillDefinitionSO dashAttackSkill;
        public string dashAttackSkillId;
        public FenShen.Combat.CombatSkillDefinitionSO dodgeFollowUpSkill;
        public string dodgeFollowUpSkillId;
        public FenShen.Combat.CombatSkillDefinitionSO guardCounterSkill;
        public string guardCounterSkillId;
        public FenShen.Combat.CombatSkillDefinitionSO chargedAttackSkill;
        public string chargedAttackSkillId;

        [Header("Resource")]
        public WeaponResourceInteraction resourceInteraction = new WeaponResourceInteraction();

        [Header("Passives")]
        public List<WeaponPassiveRuleEntry> passiveRules = new List<WeaponPassiveRuleEntry>();

        [Header("Scaling")]
        public AnimationCurve levelScaling = AnimationCurve.Linear(1f, 1f, 50f, 2f);
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
