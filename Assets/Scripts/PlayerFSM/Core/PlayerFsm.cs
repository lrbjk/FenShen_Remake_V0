using System.Collections.Generic;
using FenShen.Combat;
using FenShen.GameData;
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
        public InputActionReference JumpAction;
        [Tooltip("Optional: If not using the Reference fields, assign asset and action names.")]
        public InputActionAsset inputActions;
        public string moveActionName = "Player/Move";
        public string attackActionName = "Player/Attack";
        public string sprintActionName = "Player/Sprint";
        public string jumpActionName = "Player/Jump";

        [Header("Combat")]
        public MonoBehaviour CombatSystemBehaviour;
        public CombatCoordinator CombatCoordinator;
        [Header("Abilities")]
        public PlayerAbilityController AbilityController;
        [Header("Detection")]
        public PlayerDetection PlayerDetection;
        [Header("Runtime Stats")]
        public PlayerRuntimeStatsComponent RuntimeStatsComponent;
        [Header("Debug UI")]
        public bool showDebugOverlay = true;
        public Vector2 debugOverlayPosition = new Vector2(16f, 16f);
        public Vector2 debugOverlaySize = new Vector2(360f, 140f);
        private ICombatSkillSystem _combat;

        private StateSO _current;
        private readonly List<TransitionLinkSO> _buffer = new List<TransitionLinkSO>();
        private TransitionLinkSO _currentTriggeredTransition;
        private string _lastTransitionLabel = "None";
        private bool _isSuspendedByCombat;

        private InputAction _move;
        private InputAction _attack;
        private InputAction _sprint;
        private InputAction _jump;
        private float _sprintHeldTime;
        private float _lastSprintPressDuration;
        private bool _sprintReleasedThisFrame;
        private float _dodgeCooldownUntil;
        private float _lastJumpPressedTime = float.NegativeInfinity;
        private float _lastGroundedTime = float.NegativeInfinity;
        private bool _jumpBufferedConsumed;

        public Vector2 CurrentMoveInput { get; private set; }
        public float AirborneVerticalVelocity { get; set; }
        public float AirborneHorizontalVelocity { get; set; }
        public float GroundedHorizontalVelocity { get; set; }
        public bool IsSprinting { get { return _sprint != null && _sprint.IsPressed(); } }
        public float SprintHeldTime { get { return _sprintHeldTime; } }
        public bool IsSuspendedByCombat { get { return _isSuspendedByCombat; } }

        void Awake()
        {
            RefreshCombatSystemReference();
            RefreshCombatCoordinatorReference();
            RefreshAbilityControllerReference();
            RefreshPlayerDetectionReference();
            RefreshRuntimeStatsReference();
        }

        void OnEnable()
        {
            _move = (MoveAction != null) ? MoveAction.action : FindOptionalAction(moveActionName);
            _attack = (AttackAction != null) ? AttackAction.action : FindOptionalAction(attackActionName);
            _sprint = (SprintAction != null) ? SprintAction.action : FindOptionalAction(sprintActionName);
            _jump = (JumpAction != null) ? JumpAction.action : FindOptionalAction(jumpActionName);
            if (_move != null) _move.Enable();
            if (_attack != null) _attack.Enable();
            if (_sprint != null) _sprint.Enable();
            if (_jump != null) _jump.Enable();
        }
        void OnDisable()
        {
            if (_move != null) _move.Disable();
            if (_attack != null) _attack.Disable();
            if (_sprint != null) _sprint.Disable();
            if (_jump != null) _jump.Disable();
        }
        void Start()
        {
            if (CheckGround())
            {
                _lastGroundedTime = Time.time;
            }

            SetState(graph != null ? graph.initialState : null);
        }

        void Update()
        {
            CurrentMoveInput = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
            float dt = Time.deltaTime;
            UpdateJumpStateTracking();
            UpdateSprintInputState(dt);
            if (CombatCoordinator != null)
            {
                CombatCoordinator.ManualUpdate(dt);
                if (!_isSuspendedByCombat)
                {
                    CombatCoordinator.TryHandleAttackInput();
                }
            }

            if (_isSuspendedByCombat)
            {
                return;
            }

            if (_current != null) _current.OnUpdate(this, dt);
            EvaluateTransitions(dt);
        }

        private void EvaluateTransitions(float dt)
        {
            if (graph == null || _current == null) return;
            _buffer.Clear();
            _currentTriggeredTransition = null;
            foreach (var t in graph.GetOutgoing(_current))
            {
                if (t == null || t.to == null) continue;
                if (t.Evaluate(this, dt))
                {
                    _buffer.Add(t);
                    if (_currentTriggeredTransition == null)
                    {
                        _currentTriggeredTransition = t;
                    }
                }
            }
            if (_buffer.Count > 0)
            {
                TransitionLinkSO chosenTransition = _buffer[0];
                _lastTransitionLabel = BuildTransitionLabel(chosenTransition);
                SetState(chosenTransition.to);
            }
        }

        public void SetState(StateSO next)
        {
            if (next == null || _current == next) return;
            ChangeState(next, false);
        }

        public void ReenterState(StateSO state = null)
        {
            StateSO target = state != null ? state : _current;
            if (target == null)
            {
                return;
            }

            ChangeState(target, true);
        }

        private void ChangeState(StateSO next, bool allowReenter)
        {
            if (next == null)
            {
                return;
            }

            if (!allowReenter && _current == next)
            {
                return;
            }

            DodgeStateSO dodgeState = next as DodgeStateSO;
            if (dodgeState != null && !CanEnterDodge(dodgeState.dodgeCooldown)) return;
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
        public CombatCoordinator CombatController { get { return CombatCoordinator; } }
        public PlayerAbilityController Abilities { get { return AbilityController; } }
        public PlayerDetection Detection { get { return PlayerDetection; } }
        public PlayerRuntimeStatsComponent RuntimeStats { get { return RuntimeStatsComponent; } }
        public bool AttackPressedThisFrame() { return _attack != null && _attack.WasPressedThisFrame(); }
        public bool AttackIsHeld() { return _attack != null && _attack.IsPressed(); }
        public bool MoveIsHeld(float threshold = 0.1f) { return CurrentMoveInput.sqrMagnitude >= (threshold * threshold); }
        public bool CheckGround() { return PlayerDetection != null && PlayerDetection.CheckGround(); }
        public bool CheckWall() { return PlayerDetection != null && PlayerDetection.CheckWall(); }
        public bool JumpPressedThisFrame() { return _jump != null && _jump.WasPressedThisFrame(); }
        public bool JumpReleasedThisFrame() { return _jump != null && _jump.WasReleasedThisFrame(); }
        public bool JumpIsHeld() { return _jump != null && _jump.IsPressed(); }
        public bool IsWithinJumpBuffer
        {
            get
            {
                return !_jumpBufferedConsumed && Time.time - _lastJumpPressedTime <= GetJumpBufferDuration();
            }
        }
        public bool IsWithinCoyoteTime
        {
            get
            {
                if (CheckGround())
                {
                    return true;
                }

                return Time.time - _lastGroundedTime <= GetCoyoteTimeDuration();
            }
        }
        public bool SprintPressedThisFrame() { return _sprint != null && _sprint.WasPressedThisFrame(); }
        public bool SprintReleasedThisFrame() { return _sprintReleasedThisFrame; }
        public bool SprintHeldFor(float duration) { return IsSprinting && _sprintHeldTime >= duration; }
        public bool SprintTapReleasedThisFrame(float maxHoldDuration) { return _sprintReleasedThisFrame && _lastSprintPressDuration <= maxHoldDuration; }
        public bool IsDodgeOnCooldown { get { return Time.time < _dodgeCooldownUntil; } }
        public float DodgeCooldownRemaining { get { return Mathf.Max(0f, _dodgeCooldownUntil - Time.time); } }
        public bool HasAbility(AbilityId abilityId, int minLevel = 1)
        {
            return AbilityController != null && AbilityController.HasAbility(abilityId, minLevel);
        }
        public bool UnlockAbility(AbilityId abilityId, int level = 1)
        {
            return AbilityController != null && AbilityController.UnlockAbility(abilityId, level);
        }
        public bool LockAbility(AbilityId abilityId)
        {
            return AbilityController != null && AbilityController.LockAbility(abilityId);
        }
        public float GetStat(string statKey, float fallbackValue = 0f)
        {
            if (RuntimeStatsComponent == null || RuntimeStatsComponent.Stats == null)
            {
                return fallbackValue;
            }

            return RuntimeStatsComponent.GetStat(statKey);
        }
        public float GetDodgeCooldownDuration(float fallbackValue = 0f)
        {
            return Mathf.Max(0f, GetStat(StatKeys.DashCooldown, fallbackValue));
        }
        public float GetJumpBufferDuration(float fallbackValue = 0.12f)
        {
            return Mathf.Max(0f, GetStat(StatKeys.InputBuffer, fallbackValue));
        }
        public float GetCoyoteTimeDuration(float fallbackValue = 0.1f)
        {
            return Mathf.Max(0f, GetStat(StatKeys.CoyoteTime, fallbackValue));
        }
        public bool HasBufferedJump()
        {
            return IsWithinJumpBuffer;
        }
        public bool CanUseCoyoteJump()
        {
            return IsWithinCoyoteTime;
        }
        public bool CanStartJump()
        {
            return HasBufferedJump() && CanUseCoyoteJump();
        }
        public bool ConsumeBufferedJump()
        {
            if (!HasBufferedJump())
            {
                return false;
            }

            _jumpBufferedConsumed = true;
            return true;
        }
        public void ResetJumpBuffer()
        {
            _jumpBufferedConsumed = true;
        }
        public void ClearGroundedHistory()
        {
            _lastGroundedTime = float.NegativeInfinity;
        }
        public bool CanEnterDodge(float fallbackCooldown = 0f)
        {
            return DodgeCooldownRemaining <= 0f && GetDodgeCooldownDuration(fallbackCooldown) >= 0f;
        }
        public void StartDodgeCooldown(float fallbackCooldown = 0f)
        {
            float duration = GetDodgeCooldownDuration(fallbackCooldown);
            _dodgeCooldownUntil = duration > 0f ? Time.time + duration : 0f;
        }
        public bool IsMoveDirectionHeld(Vector2 direction, float threshold = 0.5f)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            if (CurrentMoveInput.sqrMagnitude < (threshold * threshold))
            {
                return false;
            }

            Vector2 normalizedInput = CurrentMoveInput.normalized;
            Vector2 normalizedDirection = direction.normalized;
            return Vector2.Dot(normalizedInput, normalizedDirection) >= 0.7071f;
        }

        public bool TryStartCombatAttack(string skillId)
        {
            return CombatCoordinator != null && CombatCoordinator.TryStartAttack(skillId);
        }

        public void SuspendByCombat()
        {
            _isSuspendedByCombat = true;
        }

        public void ResumeFromCombat(StateSO recoveryState = null)
        {
            _isSuspendedByCombat = false;

            StateSO next = recoveryState != null ? recoveryState : ResolveDefaultLocomotionState();
            if (next == null)
            {
                return;
            }

            if (_current == next)
            {
                ReenterState(next);
                return;
            }

            SetState(next);
        }

        public StateSO ResolveDefaultLocomotionState()
        {
            if (!CheckGround())
            {
                return FindStateOfType<FallStateSO>();
            }

            if (MoveIsHeld())
            {
                StateSO moveState = FindPreferredGroundMoveState();
                if (moveState != null)
                {
                    return moveState;
                }
            }

            return FindStateOfType<IdleStateSO>();
        }

        public StateSO FindPreferredGroundMoveState()
        {
            StateSO moveState = FindStateOfType<RunStateSO>();
            if (moveState != null)
            {
                return moveState;
            }

            moveState = FindStateOfType<WalkStateSO>();
            if (moveState != null)
            {
                return moveState;
            }

            return FindStateOfType<MoveStateSO>();
        }

        public T FindStateOfType<T>() where T : StateSO
        {
            if (graph == null || graph.states == null)
            {
                return null;
            }

            for (int i = 0; i < graph.states.Count; i++)
            {
                T state = graph.states[i] as T;
                if (state != null)
                {
                    return state;
                }
            }

            return null;
        }

        private void RefreshCombatSystemReference()
        {
            _combat = CombatSystemBehaviour as ICombatSkillSystem;
        }

        private void RefreshCombatCoordinatorReference()
        {
            if (CombatCoordinator == null)
            {
                CombatCoordinator = GetComponent<CombatCoordinator>();
            }
        }

        private void RefreshAbilityControllerReference()
        {
            if (AbilityController == null)
            {
                AbilityController = GetComponent<PlayerAbilityController>();
            }
        }

        private void RefreshPlayerDetectionReference()
        {
            if (PlayerDetection == null)
            {
                PlayerDetection = GetComponent<PlayerDetection>();
            }
        }

        private void RefreshRuntimeStatsReference()
        {
            if (RuntimeStatsComponent == null)
            {
                RuntimeStatsComponent = GetComponent<PlayerRuntimeStatsComponent>();
            }
        }

        private void UpdateSprintInputState(float dt)
        {
            _sprintReleasedThisFrame = false;

            if (_sprint == null)
            {
                _sprintHeldTime = 0f;
                _lastSprintPressDuration = 0f;
                return;
            }

            if (_sprint.WasReleasedThisFrame())
            {
                _sprintReleasedThisFrame = true;
                _lastSprintPressDuration = _sprintHeldTime;
                _sprintHeldTime = 0f;
                return;
            }

            if (_sprint.IsPressed())
            {
                if (_sprint.WasPressedThisFrame())
                {
                    _sprintHeldTime = 0f;
                }

                _sprintHeldTime += dt;
                return;
            }

            _sprintHeldTime = 0f;
        }

        private void UpdateJumpStateTracking()
        {
            if (_jump != null && _jump.WasPressedThisFrame())
            {
                _lastJumpPressedTime = Time.time;
                _jumpBufferedConsumed = false;
            }

            bool isGrounded = CheckGround();
            if (isGrounded)
            {
                _lastGroundedTime = Time.time;
            }
        }

        private InputAction FindOptionalAction(string actionName)
        {
            if (inputActions == null || string.IsNullOrWhiteSpace(actionName))
            {
                return null;
            }

            InputAction action = inputActions.FindAction(actionName, false);
            if (action == null)
            {
                Debug.LogWarning($"PlayerFsm could not find input action '{actionName}' in '{inputActions.name}'.", this);
            }

            return action;
        }

        private void OnGUI()
        {
            if (!showDebugOverlay)
            {
                return;
            }

            Rect rect = new Rect(debugOverlayPosition.x, debugOverlayPosition.y, debugOverlaySize.x, debugOverlaySize.y);
            GUI.Box(rect, "Player FSM Debug");

            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 28f, rect.width - 24f, rect.height - 40f));
            GUILayout.Label($"Current State: {(_current != null ? _current.DisplayName : "None")}");
            GUILayout.Label($"Current Transition: {BuildTransitionLabel(_currentTriggeredTransition)}");
            GUILayout.Label($"Last Transition: {_lastTransitionLabel}");
            GUILayout.Label($"Grounded: {CheckGround()}");
            GUILayout.EndArea();
        }

        private string BuildTransitionLabel(TransitionLinkSO transition)
        {
            if (transition == null)
            {
                return "None";
            }

            return $"{transition.DisplayName} [{transition.ConditionSummary}]";
        }
    }
}
