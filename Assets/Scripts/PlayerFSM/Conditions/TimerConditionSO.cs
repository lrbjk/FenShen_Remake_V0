using UnityEngine;
namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "TimerCondition", menuName = "PlayerFSM / Conditions / Timer")]
    public class TimerConditionSO : ConditionSO
    {
        public float duration = 0.5f;
        private float _start;
        public override void OnEnter(PlayerFsm fsm) { _start = Time.time; }
        public override bool Evaluate(PlayerFsm fsm) { return Time.time - _start >= duration; }
    }
}