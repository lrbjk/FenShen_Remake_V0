using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "JumpReadyCondition", menuName = "PlayerFSM / Conditions / JumpReady")]
    public class JumpReadyConditionSO : ConditionSO
    {
        public bool requireBufferedJump = true;
        public bool requireGroundOrCoyote = true;

        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return false;
            }

            bool bufferOk = !requireBufferedJump || fsm.HasBufferedJump();
            bool groundedOk = !requireGroundOrCoyote || fsm.CanUseCoyoteJump();
            return bufferOk && groundedOk;
        }
    }
}
