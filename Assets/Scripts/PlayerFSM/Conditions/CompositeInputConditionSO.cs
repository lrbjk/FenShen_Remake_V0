using UnityEngine;

namespace FenShen.PlayerFSM
{
    public enum CompositeInputDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    [CreateAssetMenu(fileName = "CompositeInputCondition", menuName = "PlayerFSM / Conditions / CompositeInput")]
    public class CompositeInputConditionSO : ConditionSO
    {
        [Header("Modifier")]
        public CompositeInputDirection requiredDirection = CompositeInputDirection.Down;
        [Range(0.1f, 1f)]
        public float directionThreshold = 0.5f;

        [Header("Trigger")]
        public InputBindingType triggerBinding = InputBindingType.Attack;
        public bool requireTriggerPressedThisFrame = true;
        public float moveThreshold = 0.1f;

        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return false;
            }

            if (!IsDirectionMatched(fsm))
            {
                return false;
            }

            switch (triggerBinding)
            {
                case InputBindingType.Attack:
                    return requireTriggerPressedThisFrame ? fsm.AttackPressedThisFrame() : fsm.AttackIsHeld();
                case InputBindingType.Move:
                    return fsm.MoveIsHeld(moveThreshold);
                case InputBindingType.Sprint:
                    return requireTriggerPressedThisFrame ? fsm.SprintPressedThisFrame() : fsm.IsSprinting;
                default:
                    return false;
            }
        }

        private bool IsDirectionMatched(PlayerFsm fsm)
        {
            switch (requiredDirection)
            {
                case CompositeInputDirection.None:
                    return true;
                case CompositeInputDirection.Up:
                    return fsm.IsMoveDirectionHeld(Vector2.up, directionThreshold);
                case CompositeInputDirection.Down:
                    return fsm.IsMoveDirectionHeld(Vector2.down, directionThreshold);
                case CompositeInputDirection.Left:
                    return fsm.IsMoveDirectionHeld(Vector2.left, directionThreshold);
                case CompositeInputDirection.Right:
                    return fsm.IsMoveDirectionHeld(Vector2.right, directionThreshold);
                default:
                    return false;
            }
        }
    }
}
