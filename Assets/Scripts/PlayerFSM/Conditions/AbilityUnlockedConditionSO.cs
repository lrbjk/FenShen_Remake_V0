using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "AbilityUnlockedCondition", menuName = "PlayerFSM / Conditions / AbilityUnlocked")]
    public class AbilityUnlockedConditionSO : ConditionSO
    {
        public AbilityId requiredAbility = AbilityId.None;
        [Min(1)]
        public int minimumLevel = 1;
        public bool invertResult;

        public override bool Evaluate(PlayerFsm fsm)
        {
            bool result = fsm != null && fsm.HasAbility(requiredAbility, minimumLevel);
            return invertResult ? !result : result;
        }
    }
}
