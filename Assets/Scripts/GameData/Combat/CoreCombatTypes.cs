using System;
using System.Collections.Generic;
using FenShen.Combat;
using UnityEngine;

namespace FenShen.GameData
{
    public enum CoreResourceGainTrigger
    {
        SkillStart = 0,
        SkillEnd = 1,
        HitConfirm = 2,
        GroundedSkillStart = 3,
        AerialSkillStart = 4,
        Dodge = 5,
        PerfectDodge = 6,
        Guard = 7,
    }

    [Serializable]
    public class CoreResourceSettings
    {
        [InspectorName("资源上限")]
        [Min(0f)] public float maxResource = 100f;
        [InspectorName("初始资源")]
        [Min(0f)] public float initialResource;
        [InspectorName("每秒自然恢复")]
        [Min(0f)] public float passiveRegenPerSecond;
    }

    [Serializable]
    public class CoreResourceGainRule
    {
        [InspectorName("获取触发时机")]
        public CoreResourceGainTrigger trigger = CoreResourceGainTrigger.HitConfirm;
        [InspectorName("获取量")]
        [Min(0f)] public float amount = 5f;
        [InspectorName("武器获取倍率")]
        [Min(0f)] public float weaponGainMultiplier = 1f;
        [InspectorName("需要在地面")]
        public bool requiresGrounded;
        [InspectorName("需要在空中")]
        public bool requiresAerial;
    }

    [Serializable]
    public class CoreCancelRule
    {
        [InspectorName("覆盖取消权限")]
        public bool overrideCancelPermission;
        [InspectorName("取消权限")]
        public WeaponCancelPermission cancelPermission = WeaponCancelPermission.None;
        [InspectorName("需要资源阈值")]
        public bool requireResourceThreshold;
        [InspectorName("最低资源")]
        [Min(0f)] public float minResource;
    }

    [Serializable]
    public class CoreSkillCostRule
    {
        [InspectorName("使用技能自身资源消耗")]
        public bool useSkillResourceCost = true;
        [InspectorName("额外固定资源消耗")]
        [Min(0f)] public float flatResourceCost;
        [InspectorName("资源不足时禁止释放")]
        public bool requireEnoughResource = true;
    }

    [Serializable]
    public class CoreSpecialDerivationRule
    {
        [InspectorName("规则ID")]
        public string ruleId;
        [InspectorName("来源技能")]
        public CombatSkillDefinitionSO fromSkill;
        [InspectorName("派生到技能")]
        public CombatSkillDefinitionSO nextSkill;
        [InspectorName("说明")]
        [TextArea] public string description;
        [InspectorName("需要命中确认")]
        public bool requiresHitConfirm;
        [InspectorName("需要在地面")]
        public bool requiresGrounded;
        [InspectorName("需要在空中")]
        public bool requiresAerial;
        [InspectorName("需要资源阈值")]
        public bool requireResourceThreshold;
        [InspectorName("最低资源")]
        [Min(0f)] public float minResource;
        [InspectorName("进入时消耗资源")]
        public bool consumeResourceOnEnter;
        [InspectorName("资源消耗")]
        [Min(0f)] public float resourceCost;
    }

    [Serializable]
    public class CoreAirGroundRule
    {
        [InspectorName("地面资源获取倍率")]
        [Min(0f)] public float groundedResourceGainMultiplier = 1f;
        [InspectorName("空中资源获取倍率")]
        [Min(0f)] public float aerialResourceGainMultiplier = 1f;
        [InspectorName("落地前允许重复空中攻击")]
        public bool allowRepeatedAerialAttackBeforeLanding;
    }

    [Serializable]
    public class CoreDodgeRule
    {
        [InspectorName("允许空中闪避")]
        public bool allowAerialDodge;
        [InspectorName("落地前最大空中闪避次数")]
        [Min(0)] public int maxAerialDodgesBeforeLanding = 1;
        [InspectorName("闪避后刷新普攻")]
        public bool refreshPrimaryAttackOnDodge;
        [InspectorName("闪避后刷新空中攻击锁")]
        public bool refreshAerialAttackLockOnDodge;
        [InspectorName("闪避后强化下一击")]
        public bool empowerNextAttackOnDodge;
        [InspectorName("强化攻击伤害倍率")]
        [Min(0f)] public float empoweredAttackDamageMultiplier = 1.25f;
        [InspectorName("仅完美闪避触发强化")]
        public bool onlyEmpowerOnPerfectDodge = true;
    }

    [Serializable]
    public class CoreRiskRewardRule
    {
        [InspectorName("造成伤害倍率")]
        [Min(0f)] public float damageDealtMultiplier = 1f;
        [InspectorName("受到伤害倍率")]
        [Min(0f)] public float damageTakenMultiplier = 1f;
        [InspectorName("资源获取倍率")]
        [Min(0f)] public float resourceGainMultiplier = 1f;
    }

    [Serializable]
    public class CorePassiveRule
    {
        [InspectorName("规则ID")]
        public string ruleId;
        [InspectorName("显示名称")]
        public string displayName;
        [InspectorName("说明")]
        [TextArea] public string description;
        [InspectorName("属性修正")]
        public List<StatModifierEntry> statModifiers = new List<StatModifierEntry>();
    }
}
