using UnityEngine;

namespace FenShen.PlayerFSM
{
    public enum HeavyAttackTriggerMode
    {
        HeldForDuration = 0,
        ReleaseAfterHold = 1,
    }

    [CreateAssetMenu(fileName = "HeavyAttackCondition", menuName = "PlayerFSM / Conditions / HeavyAttack")]
    public class HeavyAttackConditionSO : ConditionSO
    {
        public HeavyAttackTriggerMode triggerMode = HeavyAttackTriggerMode.ReleaseAfterHold;
        [Min(0.01f)] public float minHoldDuration = 0.35f;

        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return false;
            }

            switch (triggerMode)
            {
                case HeavyAttackTriggerMode.HeldForDuration:
                    return fsm.AttackHeldFor(minHoldDuration);
                default:
                    return fsm.AttackReleaseHeldFor(minHoldDuration);
            }
        }
    }
}
