using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "WallCheckCondition", menuName = "PlayerFSM / Conditions / WallCheck")]
    public class WallCheckConditionSO : ConditionSO
    {
        public bool expectedHasWall = true;

        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return false;
            }

            return fsm.CheckWall() == expectedHasWall;
        }
    }
}
