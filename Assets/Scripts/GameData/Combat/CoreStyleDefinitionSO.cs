using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "CoreStyleDefinition", menuName = "Game Data/Combat/Core Style Definition")]
    public class CoreStyleDefinitionSO : ScriptableObject
    {
        [Header("身份信息")]
        [InspectorName("核心ID")]
        public string coreId;
        [InspectorName("显示名称")]
        public string displayName;
        [InspectorName("描述")]
        [TextArea] public string description;
        [InspectorName("图标")]
        public Sprite icon;

        [Header("核心资源")]
        [InspectorName("资源配置")]
        public CoreResourceSettings resource = new CoreResourceSettings();
        [InspectorName("资源获取规则")]
        public List<CoreResourceGainRule> resourceGainRules = new List<CoreResourceGainRule>();
        [InspectorName("技能消耗规则")]
        public CoreSkillCostRule skillCost = new CoreSkillCostRule();

        [Header("战斗规则")]
        [InspectorName("取消规则")]
        public CoreCancelRule cancelRule = new CoreCancelRule();
        [InspectorName("空地规则")]
        public CoreAirGroundRule airGroundRule = new CoreAirGroundRule();
        [InspectorName("闪避规则")]
        public CoreDodgeRule dodgeRule = new CoreDodgeRule();
        [InspectorName("风险收益规则")]
        public CoreRiskRewardRule riskReward = new CoreRiskRewardRule();

        [Header("特殊派生")]
        [InspectorName("特殊派生规则")]
        public List<CoreSpecialDerivationRule> specialDerivations = new List<CoreSpecialDerivationRule>();

        [Header("被动")]
        [InspectorName("被动规则")]
        public List<CorePassiveRule> passiveRules = new List<CorePassiveRule>();

        public string ResolveCoreId()
        {
            return !string.IsNullOrWhiteSpace(coreId) ? coreId : name;
        }
    }
}
