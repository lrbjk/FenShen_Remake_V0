using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "WeaponMoveSet", menuName = "Game Data/Combat/Weapon Move Set")]
    public class WeaponMoveSetSO : ScriptableObject
    {
        [Header("Ground Combo")]
        public FenShen.Combat.CombatSkillDefinitionSO primaryAttackA1Skill;
        public string primaryAttackA1;
        public FenShen.Combat.CombatSkillDefinitionSO primaryAttackA2Skill;
        public string primaryAttackA2;
        public FenShen.Combat.CombatSkillDefinitionSO primaryAttackA3Skill;
        public string primaryAttackA3;
        public FenShen.Combat.CombatSkillDefinitionSO primaryAttackA4Skill;
        public string primaryAttackA4;

        [Header("Air Combo")]
        public FenShen.Combat.CombatSkillDefinitionSO aerialEntrySkillAsset;
        public string aerialEntrySkill;
        public FenShen.Combat.CombatSkillDefinitionSO aerialLoopSkillAsset;
        public string aerialLoopSkill;
        public FenShen.Combat.CombatSkillDefinitionSO aerialFinisherSkillAsset;
        public string aerialFinisherSkill;

        [Header("Situational")]
        public FenShen.Combat.CombatSkillDefinitionSO launcherSkillAsset;
        public string launcherSkill;
        public FenShen.Combat.CombatSkillDefinitionSO slamSkillAsset;
        public string slamSkill;
        public FenShen.Combat.CombatSkillDefinitionSO landingChaseSkillAsset;
        public string landingChaseSkill;

        [Header("Entry Skills")]
        public List<WeaponSkillEntry> entrySkills = new List<WeaponSkillEntry>();

        [Header("Branches")]
        public List<WeaponComboBranch> comboBranches = new List<WeaponComboBranch>();
        public List<WeaponSpecialDerivationWindow> specialWindows = new List<WeaponSpecialDerivationWindow>();

        public FenShen.Combat.CombatSkillDefinitionSO GetEntrySkillAsset(WeaponAttackSlot slot)
        {
            for (int i = 0; i < entrySkills.Count; i++)
            {
                WeaponSkillEntry entry = entrySkills[i];
                if (entry != null && entry.slot == slot && entry.skill != null)
                {
                    return entry.skill;
                }
            }

            return null;
        }

        public string GetEntrySkillId(WeaponAttackSlot slot)
        {
            for (int i = 0; i < entrySkills.Count; i++)
            {
                WeaponSkillEntry entry = entrySkills[i];
                if (entry != null && entry.slot == slot && !string.IsNullOrWhiteSpace(entry.skillId))
                {
                    return entry.skillId;
                }
            }

            return string.Empty;
        }

        public FenShen.Combat.CombatSkillDefinitionSO GetPrimaryComboSkillAsset(int comboIndex, bool grounded)
        {
            if (!grounded)
            {
                if (comboIndex <= 0 && aerialEntrySkillAsset != null)
                {
                    return aerialEntrySkillAsset;
                }

                if (aerialLoopSkillAsset != null)
                {
                    return aerialLoopSkillAsset;
                }

                return aerialFinisherSkillAsset;
            }

            switch (comboIndex)
            {
                case 0:
                    return primaryAttackA1Skill;
                case 1:
                    return primaryAttackA2Skill;
                case 2:
                    return primaryAttackA3Skill;
                case 3:
                    return primaryAttackA4Skill;
                default:
                    return primaryAttackA4Skill;
            }
        }

        public string GetPrimaryComboSkillId(int comboIndex, bool grounded)
        {
            if (!grounded)
            {
                if (comboIndex <= 0 && !string.IsNullOrWhiteSpace(aerialEntrySkill))
                {
                    return aerialEntrySkill;
                }

                if (!string.IsNullOrWhiteSpace(aerialLoopSkill))
                {
                    return aerialLoopSkill;
                }

                return aerialFinisherSkill;
            }

            switch (comboIndex)
            {
                case 0:
                    return primaryAttackA1;
                case 1:
                    return primaryAttackA2;
                case 2:
                    return primaryAttackA3;
                case 3:
                    return primaryAttackA4;
                default:
                    return primaryAttackA4;
            }
        }
    }
}
