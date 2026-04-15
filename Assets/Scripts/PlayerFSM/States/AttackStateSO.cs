using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "AttackState", menuName = "PlayerFSM / States / Attack")]
    public class AttackStateSO : StateSO
    {
        [Header("Attack")]
        public string skillId = "Primary";
        public float triggerAtNormalizedTime = 0.2f;
        private bool _fired;

        public override void OnEnter(PlayerFsm fsm)
        {
            base.OnEnter(fsm);
            _fired = false;
        }

        public override void OnUpdate(PlayerFsm fsm, float dt)
        {
            float nt = GetNormalizedTime(fsm);
            if (!_fired && nt >= triggerAtNormalizedTime)
            {
                _fired = true;
                if (fsm != null && fsm.CombatSystem != null) fsm.CombatSystem.ExecuteAttack(skillId);
            }
        }
    }
}
