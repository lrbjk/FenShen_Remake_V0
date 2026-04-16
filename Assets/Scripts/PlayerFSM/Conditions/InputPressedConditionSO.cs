using UnityEngine;

namespace FenShen.PlayerFSM
{
    public enum InputBindingType { Attack, Move, Sprint }

    [CreateAssetMenu(fileName = "InputPressedCondition", menuName = "PlayerFSM / Conditions / InputPressed")]
    public class InputPressedConditionSO : ConditionSO
    {
        public InputBindingType binding = InputBindingType.Attack;
        public float moveThreshold = 0.1f;
        public bool requirePressedThisFrame = true;
        public bool invertResult = false;
        public float minHoldDuration = 0f;

        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null) return false;
            bool result;
            switch (binding)
            {
                case InputBindingType.Attack:
                    result = requirePressedThisFrame ? fsm.AttackPressedThisFrame() : fsm.AttackIsHeld();
                    break;
                case InputBindingType.Move:
                    result = fsm.MoveIsHeld(moveThreshold);
                    break;
                case InputBindingType.Sprint:
                    result = requirePressedThisFrame
                        ? fsm.SprintPressedThisFrame()
                        : (minHoldDuration > 0f ? fsm.SprintHeldFor(minHoldDuration) : fsm.IsSprinting);
                    break;
                default:
                    result = false;
                    break;
            }

            return invertResult ? !result : result;
        }
    }
}
