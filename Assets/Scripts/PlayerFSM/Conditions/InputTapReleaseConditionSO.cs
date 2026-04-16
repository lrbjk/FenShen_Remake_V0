using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "InputTapReleaseCondition", menuName = "PlayerFSM / Conditions / InputTapRelease")]
    public class InputTapReleaseConditionSO : ConditionSO
    {
        public InputBindingType binding = InputBindingType.Sprint;
        public float maxHoldDuration = 0.2f;

        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return false;
            }

            switch (binding)
            {
                case InputBindingType.Sprint:
                    return fsm.SprintTapReleasedThisFrame(maxHoldDuration);
                default:
                    return false;
            }
        }
    }
}
