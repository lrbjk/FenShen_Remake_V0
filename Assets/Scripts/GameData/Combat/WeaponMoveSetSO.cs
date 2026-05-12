using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "WeaponMoveSet", menuName = "Game Data/Combat/Weapon Move Set")]
    public class WeaponMoveSetSO : ScriptableObject
    {
        [Header("地面连段")]
        [InspectorName("地面普攻1技能")]
        public FenShen.Combat.CombatSkillDefinitionSO primaryAttackA1Skill;
        [InspectorName("地面普攻1技能ID")]
        public string primaryAttackA1;
        [InspectorName("地面普攻2技能")]
        public FenShen.Combat.CombatSkillDefinitionSO primaryAttackA2Skill;
        [InspectorName("地面普攻2技能ID")]
        public string primaryAttackA2;
        [InspectorName("地面普攻3技能")]
        public FenShen.Combat.CombatSkillDefinitionSO primaryAttackA3Skill;
        [InspectorName("地面普攻3技能ID")]
        public string primaryAttackA3;
        [InspectorName("地面普攻4技能")]
        public FenShen.Combat.CombatSkillDefinitionSO primaryAttackA4Skill;
        [InspectorName("地面普攻4技能ID")]
        public string primaryAttackA4;

        [Header("空中连段")]
        [InspectorName("空中入口技能")]
        public FenShen.Combat.CombatSkillDefinitionSO aerialEntrySkillAsset;
        [InspectorName("空中入口技能ID")]
        public string aerialEntrySkill;
        [InspectorName("空中循环技能")]
        public FenShen.Combat.CombatSkillDefinitionSO aerialLoopSkillAsset;
        [InspectorName("空中循环技能ID")]
        public string aerialLoopSkill;
        [InspectorName("空中终结技能")]
        public FenShen.Combat.CombatSkillDefinitionSO aerialFinisherSkillAsset;
        [InspectorName("空中终结技能ID")]
        public string aerialFinisherSkill;

        [Header("场景技")]
        [InspectorName("挑飞技能")]
        public FenShen.Combat.CombatSkillDefinitionSO launcherSkillAsset;
        [InspectorName("挑飞技能ID")]
        public string launcherSkill;
        [InspectorName("下砸技能")]
        public FenShen.Combat.CombatSkillDefinitionSO slamSkillAsset;
        [InspectorName("下砸技能ID")]
        public string slamSkill;
        [InspectorName("落地追击技能")]
        public FenShen.Combat.CombatSkillDefinitionSO landingChaseSkillAsset;
        [InspectorName("落地追击技能ID")]
        public string landingChaseSkill;
        [InspectorName("特殊中立技能")]
        public FenShen.Combat.CombatSkillDefinitionSO specialNeutralSkillAsset;
        [InspectorName("特殊中立技能ID")]
        public string specialNeutralSkill;
        [InspectorName("特殊上方向技能")]
        public FenShen.Combat.CombatSkillDefinitionSO specialUpSkillAsset;
        [InspectorName("特殊上方向技能ID")]
        public string specialUpSkill;
        [InspectorName("特殊下方向技能")]
        public FenShen.Combat.CombatSkillDefinitionSO specialDownSkillAsset;
        [InspectorName("特殊下方向技能ID")]
        public string specialDownSkill;

        [Header("入口技能")]
        [InspectorName("入口技能列表")]
        public List<WeaponSkillEntry> entrySkills = new List<WeaponSkillEntry>();

        public FenShen.Combat.CombatSkillDefinitionSO GetEntrySkillAsset(WeaponAttackSlot slot)
        {
            FenShen.Combat.CombatSkillDefinitionSO builtInSkill = GetBuiltInEntrySkillAsset(slot);
            if (builtInSkill != null)
            {
                return builtInSkill;
            }

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
            string builtInSkill = GetBuiltInEntrySkillId(slot);
            if (!string.IsNullOrWhiteSpace(builtInSkill))
            {
                return builtInSkill;
            }

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

        private FenShen.Combat.CombatSkillDefinitionSO GetBuiltInEntrySkillAsset(WeaponAttackSlot slot)
        {
            switch (slot)
            {
                case WeaponAttackSlot.Launcher:
                    return launcherSkillAsset;
                case WeaponAttackSlot.Slam:
                    return slamSkillAsset;
                case WeaponAttackSlot.SpecialNeutral:
                    return specialNeutralSkillAsset;
                case WeaponAttackSlot.SpecialUp:
                    return specialUpSkillAsset;
                case WeaponAttackSlot.SpecialDown:
                    return specialDownSkillAsset;
                default:
                    return null;
            }
        }

        private string GetBuiltInEntrySkillId(WeaponAttackSlot slot)
        {
            switch (slot)
            {
                case WeaponAttackSlot.Launcher:
                    return launcherSkill;
                case WeaponAttackSlot.Slam:
                    return slamSkill;
                case WeaponAttackSlot.SpecialNeutral:
                    return specialNeutralSkill;
                case WeaponAttackSlot.SpecialUp:
                    return specialUpSkill;
                case WeaponAttackSlot.SpecialDown:
                    return specialDownSkill;
                default:
                    return string.Empty;
            }
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
