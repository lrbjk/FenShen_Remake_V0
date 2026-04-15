using UnityEngine;
namespace FenShen.PlayerFSM
{
    public enum InputBindingType { Attack, Move, Sprint }
    [CreateAssetMenu(fileName = "InputPressedCondition", menuName = "PlayerFSM / Conditions / InputPressed")]
    public class InputPressedConditionSO : ConditionSO
    {
        public InputBindingType binding = InputBindingType.Attack;
        public float moveThreshold = 0.1f;
        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null) return false;
            switch (binding)
            {
                case InputBindingType.Attack: return fsm.AttackPressedThisFrame();
                case InputBindingType.Move: return fsm.CurrentMoveInput.sqrMagnitude >= (moveThreshold * moveThreshold);
                case InputBindingType.Sprint: return fsm.IsSprinting;
            }
            return false;
        }
    }
}
