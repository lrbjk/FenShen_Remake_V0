namespace FenShen.Combat
{
    public class CombatStateMachine
    {
        private CombatState _currentState;

        public bool IsActive
        {
            get { return _currentState != null; }
        }

        public CombatState CurrentState
        {
            get { return _currentState; }
        }

        public void Enter(CombatCoordinator coordinator, CombatState nextState)
        {
            Exit(coordinator);
            _currentState = nextState;
            if (_currentState != null)
            {
                _currentState.OnEnter(coordinator);
            }
        }

        public bool Tick(CombatCoordinator coordinator, float deltaTime)
        {
            if (_currentState == null)
            {
                return false;
            }

            if (_currentState.Tick(coordinator, deltaTime))
            {
                Exit(coordinator);
                return true;
            }

            return false;
        }

        public void Exit(CombatCoordinator coordinator)
        {
            if (_currentState == null)
            {
                return;
            }

            _currentState.OnExit(coordinator);
            _currentState = null;
        }
    }
}
