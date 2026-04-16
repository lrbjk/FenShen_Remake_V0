using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "InputHoldDurationCondition", menuName = "PlayerFSM / Conditions / InputHoldDuration")]
    public class InputHoldDurationConditionSO : ConditionSO
    {
        public InputBindingType binding = InputBindingType.Sprint;
        public float holdDuration = 0.2f;

        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return false;
            }

            switch (binding)
            {
                case InputBindingType.Sprint:
                    return fsm.SprintHeldFor(holdDuration);
                default:
                    return false;
            }
        }
    }
}
