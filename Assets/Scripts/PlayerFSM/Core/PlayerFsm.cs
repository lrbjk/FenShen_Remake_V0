using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FenShen.PlayerFSM
{
    public class PlayerFsm : MonoBehaviour
    {
        [Header("Config")]
        public FsmGraphSO graph;
        public Animator Animator;
        public Transform Character;
        [Header("Input (New Input System)")]
        public InputActionReference MoveAction;
        public InputActionReference AttackAction;
        public InputActionReference SprintAction;
        [Tooltip("Optional: If not using the Reference fields, assign asset and action names.")]
        public InputActionAsset inputActions;
        public string moveActionName = "Player/Move";
        public string attackActionName = "Player/Attack";
        public string sprintActionName = "Player/Sprint";

        [Header("Combat")]
        public MonoBehaviour CombatSystemBehaviour;
        private ICombatSkillSystem _combat;

        private StateSO _current;
        private readonly List<TransitionLinkSO> _buffer = new List<TransitionLinkSO>();

        private InputAction _move;
        private InputAction _attack;
        private InputAction _sprint;

        public Vector2 CurrentMoveInput { get; private set; }
        public bool IsSprinting { get { return _sprint != null && _sprint.IsPressed(); } }

        void Awake()
        {
            RefreshCombatSystemReference();
        }

        void OnEnable()
        {
            _move = (MoveAction != null) ? MoveAction.action : (inputActions != null ? inputActions.FindAction(moveActionName, true) : null);
            _attack = (AttackAction != null) ? AttackAction.action : (inputActions != null ? inputActions.FindAction(attackActionName, true) : null);
            _sprint = (SprintAction != null) ? SprintAction.action : (inputActions != null ? inputActions.FindAction(sprintActionName, true) : null);
            if (_move != null) _move.Enable();
            if (_attack != null) _attack.Enable();
            if (_sprint != null) _sprint.Enable();
        }
        void OnDisable()
        {
            if (_move != null) _move.Disable();
            if (_attack != null) _attack.Disable();
            if (_sprint != null) _sprint.Disable();
        }
        void Start()
        {
            SetState(graph != null ? graph.initialState : null);
        }

        void Update()
        {
            CurrentMoveInput = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            float dt = Time.deltaTime;
            if (_current != null) _current.OnUpdate(this, dt);
            EvaluateTransitions(dt);
        }

        private void EvaluateTransitions(float dt)
        {
            if (graph == null || _current == null) return;
            _buffer.Clear();
            foreach (var t in graph.GetOutgoing(_current))
            {
                if (t == null || t.to == null) continue;
                if (t.Evaluate(this, dt)) _buffer.Add(t);
            }
            if (_buffer.Count > 0) SetState(_buffer[0].to);
        }

        public void SetState(StateSO next)
        {
            if (next == null || _current == next) return;
            if (_current != null)
            {
                foreach (var t in graph.GetOutgoing(_current)) if (t != null) t.OnExit(this);
                _current.OnExit(this);
            }
            _current = next;
            if (_current != null)
            {
                foreach (var t in graph.GetOutgoing(_current)) if (t != null) t.OnEnter(this);
                _current.OnEnter(this);
            }
        }

        public StateSO CurrentState { get { return _current; } }
        public ICombatSkillSystem CombatSystem { get { return _combat; } }
        public bool AttackPressedThisFrame() { return _attack != null && _attack.WasPressedThisFrame(); }

        private void RefreshCombatSystemReference()
        {
            _combat = CombatSystemBehaviour as ICombatSkillSystem;
        }
    }
}
