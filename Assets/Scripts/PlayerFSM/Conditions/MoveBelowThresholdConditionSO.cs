using UnityEngine;
namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "MoveBelowThresholdCondition", menuName = "PlayerFSM / Conditions / MoveBelowThreshold")]
    public class MoveBelowThresholdConditionSO : ConditionSO
    {
        public float moveThreshold = 0.1f;
        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null) return false;
            return fsm.CurrentMoveInput.sqrMagnitude < (moveThreshold * moveThreshold);
        }
    }
}
