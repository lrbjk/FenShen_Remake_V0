using UnityEngine;
namespace FenShen.PlayerFSM
{
    public abstract class ConditionSO : ScriptableObject
    {
        public virtual void OnEnter(PlayerFsm fsm) { }
        public virtual void OnExit(PlayerFsm fsm) { }
        public virtual void Tick(PlayerFsm fsm, float deltaTime) { }
        public abstract bool Evaluate(PlayerFsm fsm);
    }
}