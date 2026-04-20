using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "IdleState", menuName = "PlayerFSM / States / Idle")]
    public class IdleStateSO : StateSO
    {
        public override void OnEnter(PlayerFsm fsm)
        {
            base.OnEnter(fsm);

            if (fsm != null)
            {
                fsm.GroundedHorizontalVelocity = 0f;
                fsm.AirborneHorizontalVelocity = 0f;
            }
        }

        public override void OnUpdate(PlayerFsm fsm, float dt) { }
    }
}
