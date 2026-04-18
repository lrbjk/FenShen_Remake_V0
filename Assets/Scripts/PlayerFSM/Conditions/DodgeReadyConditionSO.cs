using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "DodgeReadyCondition", menuName = "PlayerFSM / Conditions / DodgeReady")]
    public class DodgeReadyConditionSO : ConditionSO
    {
        public bool invertResult;

        public override bool Evaluate(PlayerFsm fsm)
        {
            bool result = fsm != null && fsm.CanEnterDodge();
            return invertResult ? !result : result;
        }
    }
}
