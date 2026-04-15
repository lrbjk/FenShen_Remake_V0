using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "IdleState", menuName = "PlayerFSM / States / Idle")]
    public class IdleStateSO : StateSO
    {
        public override void OnUpdate(PlayerFsm fsm, float dt) { }
    }
}
