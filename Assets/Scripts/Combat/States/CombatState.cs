namespace FenShen.Combat
{
    public abstract class CombatState
    {
        public abstract void OnEnter(CombatCoordinator coordinator);
        public abstract bool Tick(CombatCoordinator coordinator, float deltaTime);
        public abstract void OnExit(CombatCoordinator coordinator);
    }
}
