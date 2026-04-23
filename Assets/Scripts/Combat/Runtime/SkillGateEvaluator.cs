using System.Collections.Generic;
using FenShen.GameData;
using FenShen.PlayerFSM;
using UnityEngine;

namespace FenShen.Combat
{
    public static class SkillGateEvaluator
    {
        public static bool CanEnterSkill(
            PlayerFsm playerFsm,
            WeaponRuntimeController weaponRuntime,
            CombatSkillDefinitionSO skill,
            CombatSkillDefinitionSO previousSkill,
            out string reason)
        {
            reason = string.Empty;

            if (playerFsm == null)
            {
                reason = "Missing PlayerFsm.";
                return false;
            }

            if (skill == null)
            {
                reason = "Missing skill asset.";
                return false;
            }

            bool grounded = playerFsm.CheckGround();
            if (skill.groundedOnly && !grounded)
            {
                reason = "Skill requires grounded state.";
                return false;
            }

            if (skill.aerialOnly && grounded)
            {
                reason = "Skill requires aerial state.";
                return false;
            }

            if (skill.RequiresPreviousSkill && !HasAllowedPreviousSkill(skill, previousSkill))
            {
                reason = "Previous skill is not allowed.";
                return false;
            }

            if (skill.requiredWeaponTraits != WeaponTraitTag.None)
            {
                WeaponDefinitionSO currentWeapon = weaponRuntime != null ? weaponRuntime.CurrentWeapon : null;
                if (currentWeapon == null || !currentWeapon.HasTrait(skill.requiredWeaponTraits))
                {
                    reason = "Required weapon trait not present.";
                    return false;
                }
            }

            if (!EvaluateConditions(playerFsm, skill.enterConditions))
            {
                reason = "Skill conditions failed.";
                return false;
            }

            return true;
        }

        private static bool HasAllowedPreviousSkill(CombatSkillDefinitionSO skill, CombatSkillDefinitionSO previousSkill)
        {
            if (skill.previousSkills == null || skill.previousSkills.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < skill.previousSkills.Count; i++)
            {
                if (skill.previousSkills[i] == previousSkill)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool EvaluateConditions(PlayerFsm playerFsm, SkillConditionGroup group)
        {
            if (group == null || group.conditions == null || group.conditions.Count == 0)
            {
                return true;
            }

            bool any = false;
            List<ConditionSO> conditions = group.conditions;
            for (int i = 0; i < conditions.Count; i++)
            {
                ConditionSO condition = conditions[i];
                if (condition == null)
                {
                    continue;
                }

                bool value = condition.Evaluate(playerFsm);
                any = any || value;
                if (group.logic == ConditionLogic.All && !value)
                {
                    return false;
                }
            }

            return group.logic == ConditionLogic.All ? true : any;
        }
    }
}
