using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "GroundCheckCondition", menuName = "PlayerFSM / Conditions / GroundCheck")]
    public class GroundCheckConditionSO : ConditionSO
    {
        public bool expectedIsGrounded = true;

        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return false;
            }

            return fsm.CheckGround() == expectedIsGrounded;
        }
    }
}
