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

    [Serializable]
    public class SkillMotionSettings
    {
        public SkillMotionSource motionSource = SkillMotionSource.AnimatorCurve;
        public bool mirrorByFacing = true;
        public string curveXName = "MoveX";
        public string curveYName = "MoveY";
        public AnimationCurve customCurveX = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        public AnimationCurve customCurveY = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    }

    [Serializable]
    public class SkillConditionGroup
    {
        public ConditionLogic logic = ConditionLogic.All;
        public List<ConditionSO> conditions = new List<ConditionSO>();
    }

    [Serializable]
    public class SkillDerivationRule
    {
        public string ruleId;
        public CombatSkillDefinitionSO nextSkill;
        [TextArea] public string description;
        [Min(0f)] public float windowStartTime = 0f;
        [Min(0f)] public float windowEndTime = 0.2f;
        public bool requiresHitConfirm;
        public bool requiresGrounded;
        public bool requiresAerial;
        public bool requiresPerfectDodge;
        public bool requiresGuardSuccess;
    }

    [Serializable]
    public class SkillRecoveryRule
    {
        public string ruleId;
        public string displayName;
        public CombatSkillDefinitionSO nextSkill;
        [TextArea] public string description;
        public bool requiresHitConfirm;
        public bool requiresGrounded;
        public bool requiresAerial;
        public SkillConditionGroup conditions = new SkillConditionGroup();
    }

    [CreateAssetMenu(fileName = "CombatSkillDefinition", menuName = "Game Data/Combat/Combat Skill Definition")]
    public class CombatSkillDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string skillId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public SkillUsageSide usageSide = SkillUsageSide.Both;
        public List<string> tags = new List<string>();

        [Header("Presentation")]
        public string animationStateName;
        public int animationLayer;
        public bool crossFade = true;
        public float transitionDuration = 0.05f;
        public AnimationClip previewAnimationClip;

        [Header("Timeline")]
        public SkillTimelineSO timeline;
        public SkillMotionSettings motion = new SkillMotionSettings();

        [Header("Timing")]
        [Min(0.01f)] public float duration = 0.6f;
        [Min(0f)] public float startup = 0.1f;
        [Min(0f)] public float active = 0.1f;
        [Min(0f)] public float recovery = 0.2f;
        [Min(0f)] public float cooldown = 0f;

        [Header("Combat")]
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0f)] public float poiseDamage = 0f;
        [Min(0f)] public float resourceCost = 0f;
        public bool groundedOnly;
        public bool aerialOnly;
        public WeaponTraitTag requiredWeaponTraits = WeaponTraitTag.None;

        [Header("Entry Gate")]
        public SkillConditionGroup enterConditions = new SkillConditionGroup();
        public List<CombatSkillDefinitionSO> previousSkills = new List<CombatSkillDefinitionSO>();

        [Header("Derivation")]
        public List<SkillDerivationRule> derivationRules = new List<SkillDerivationRule>();

        [Header("Recovery")]
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
