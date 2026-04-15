using UnityEngine;
namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "AnimNormalizedTimeCondition", menuName = "PlayerFSM / Conditions / AnimNormalizedTime")]
    public class AnimNormalizedTimeConditionSO : ConditionSO
    {
        public float threshold = 0.9f;
        public bool greaterOrEqual = true;
        public int layer = 0;
        public override bool Evaluate(PlayerFsm fsm)
        {
            if (fsm == null || fsm.Animator == null) return false;
            var info = fsm.Animator.GetCurrentAnimatorStateInfo(layer);
            var nt = info.normalizedTime;
            return greaterOrEqual ? (nt >= threshold) : (nt <= threshold);
        }
    }
}