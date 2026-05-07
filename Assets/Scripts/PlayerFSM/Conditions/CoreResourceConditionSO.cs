using FenShen.Combat;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "CoreResourceCondition", menuName = "PlayerFSM / Conditions / CoreResource")]
    public class CoreResourceConditionSO : ConditionSO
    {
        [InspectorName("最低核心资源")]
        [Min(0f)] public float minResource;
        [InspectorName("要求资源已满")]
        public bool requireFull;
        [InspectorName("反转结果")]
        public bool invertResult;

        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null || fsm.CombatController == null)
            {
                return false;
            }

            CoreRuntimeController core = fsm.CombatController.CoreController;
            if (core == null || core.CurrentCore == null)
            {
                return false;
            }

            bool result = requireFull
                ? core.MaxResource > 0f && core.CurrentResource >= core.MaxResource
                : core.CurrentResource >= minResource;
            return invertResult ? !result : result;
        }
    }
}
