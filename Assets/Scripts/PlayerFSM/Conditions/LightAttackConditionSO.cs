using UnityEngine;

namespace FenShen.PlayerFSM
{
    public enum LightAttackTriggerMode
    {
        PressedThisFrame = 0,
        TapReleased = 1,
        Held = 2,
    }

    [CreateAssetMenu(fileName = "LightAttackCondition", menuName = "PlayerFSM / Conditions / LightAttack")]
    public class LightAttackConditionSO : ConditionSO
    {
        [InspectorName("触发模式")]
        public LightAttackTriggerMode triggerMode = LightAttackTriggerMode.PressedThisFrame;
        [InspectorName("最大轻点时长")]
        [Min(0f)] public float maxTapDuration = 0.2f;

        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return false;
            }

            switch (triggerMode)
            {
                case LightAttackTriggerMode.TapReleased:
                    return fsm.AttackTapReleasedThisFrame(maxTapDuration);
                case LightAttackTriggerMode.Held:
                    return fsm.AttackIsHeld() && !fsm.AttackHeldFor(maxTapDuration);
                default:
                    return fsm.AttackPressedThisFrame();
            }
        }
    }
}
