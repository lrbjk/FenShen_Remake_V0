using FenShen.GameData;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "JumpState", menuName = "PlayerFSM / States / Jump")]
    public class JumpStateSO : StateSO
    {
        private enum JumpPhase
        {
            Rising,
            Falling,
            Landing
        }

        [Header("Animation")]
        public string jumpAnimStateName = "jump";
        public string fallAnimStateName = "jumptofall";
        public string landingAnimStateName = "landing";

        [Header("Jump Motion")]
        public float jumpForce = 8f;
        public float gravity = 25f;
        public float maxFallSpeed = 18f;
        public float earlyReleaseGravityMultiplier = 1.8f;
        public float fallGravityMultiplier = 1.15f;
        public float fallAnimationThreshold = -0.1f;

        [Header("Horizontal Feel")]
        public float moveSpeed = 4.5f;
        public float groundedTakeoffMomentumMultiplier = 1f;
        public float stationaryTakeoffMomentumMultiplier = 0f;
        public float airAcceleration = 24f;
        public float airDeceleration = 16f;
        public float maxAirSpeedMultiplier = 1f;

        [Header("Landing")]
        public float landingDuration = 0.12f;
        public float landingMoveDamping = 10f;
        public StateSO groundedIdleState;
        public StateSO groundedMoveState;

        private JumpPhase _phase;
        private float _verticalVelocity;
        private float _horizontalVelocity;
        private float _landingTimer;

        public override void OnEnter(PlayerFsm fsm)
        {
            base.OnEnter(fsm);

            if (fsm != null)
            {
                fsm.ConsumeBufferedJump();
                fsm.ClearGroundedHistory();
            }

            _phase = JumpPhase.Rising;
            _landingTimer = 0f;
            _verticalVelocity = Mathf.Max(0f, fsm != null ? fsm.GetStat(StatKeys.JumpForce, jumpForce) : jumpForce);
            _horizontalVelocity = ResolveTakeoffHorizontalVelocity(fsm);
            PlayAnimation(fsm, ResolveAnimationName(jumpAnimStateName, animStateName), true);
        }

        public override void OnUpdate(PlayerFsm fsm, float deltaTime)
        {
            if (fsm == null || fsm.Character == null)
            {
                return;
            }

            switch (_phase)
            {
                case JumpPhase.Rising:
                case JumpPhase.Falling:
                    UpdateAirborne(fsm, deltaTime);
                    break;

                case JumpPhase.Landing:
                    UpdateLanding(fsm, deltaTime);
                    break;
            }
        }

        private void UpdateAirborne(PlayerFsm fsm, float deltaTime)
        {
            float gravityScale = Mathf.Max(0.01f, fsm.GetStat(StatKeys.GravityScale, 1f));
            float resolvedGravity = gravity * gravityScale;

            if (_verticalVelocity > 0f && !fsm.JumpIsHeld())
            {
                resolvedGravity *= earlyReleaseGravityMultiplier;
            }
            else if (_verticalVelocity <= 0f)
            {
                resolvedGravity *= fallGravityMultiplier;
            }

            _verticalVelocity = Mathf.Max(-maxFallSpeed, _verticalVelocity - resolvedGravity * deltaTime);
            UpdateHorizontalVelocity(fsm, deltaTime);

            Vector3 delta = new Vector3(_horizontalVelocity * deltaTime, _verticalVelocity * deltaTime, 0f);
            fsm.Character.Translate(delta, Space.World);
            UpdateFacing(fsm, _horizontalVelocity);

            if (_phase == JumpPhase.Rising && _verticalVelocity <= fallAnimationThreshold)
            {
                _phase = JumpPhase.Falling;
                PlayAnimation(fsm, ResolveAnimationName(fallAnimStateName, jumpAnimStateName), true);
            }

            if (_verticalVelocity <= 0f && fsm.CheckGround())
            {
                StartLanding(fsm);
            }
        }

        private void UpdateLanding(PlayerFsm fsm, float deltaTime)
        {
            _landingTimer += deltaTime;
            _horizontalVelocity = Mathf.MoveTowards(_horizontalVelocity, 0f, landingMoveDamping * deltaTime);

            if (Mathf.Abs(_horizontalVelocity) > 0.001f)
            {
                fsm.Character.Translate(new Vector3(_horizontalVelocity * deltaTime, 0f, 0f), Space.World);
                UpdateFacing(fsm, _horizontalVelocity);
            }

            if (_landingTimer >= landingDuration)
            {
                ExitToGroundedState(fsm);
            }
        }

        private void UpdateHorizontalVelocity(PlayerFsm fsm, float deltaTime)
        {
            float inputX = fsm.CurrentMoveInput.x;
            float maxAirSpeed = fsm.GetStat(StatKeys.MoveSpeed, moveSpeed) * maxAirSpeedMultiplier;
            float targetVelocity = inputX * maxAirSpeed;
            float acceleration = Mathf.Abs(targetVelocity) > 0.01f ? airAcceleration : airDeceleration;
            _horizontalVelocity = Mathf.MoveTowards(_horizontalVelocity, targetVelocity, acceleration * deltaTime);
        }

        private float ResolveTakeoffHorizontalVelocity(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return 0f;
            }

            float inputX = fsm.CurrentMoveInput.x;
            float resolvedMoveSpeed = fsm.GetStat(StatKeys.MoveSpeed, moveSpeed);
            if (Mathf.Abs(inputX) <= 0.01f)
            {
                return Mathf.Sign(inputX) * resolvedMoveSpeed * stationaryTakeoffMomentumMultiplier;
            }

            return inputX * resolvedMoveSpeed * groundedTakeoffMomentumMultiplier;
        }

        private void StartLanding(PlayerFsm fsm)
        {
            _phase = JumpPhase.Landing;
            _verticalVelocity = 0f;
            _landingTimer = 0f;
            PlayAnimation(fsm, ResolveAnimationName(landingAnimStateName, fallAnimStateName), true);
        }

        private void ExitToGroundedState(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return;
            }

            StateSO next = fsm.MoveIsHeld()
                ? (groundedMoveState != null ? groundedMoveState : groundedIdleState)
                : (groundedIdleState != null ? groundedIdleState : groundedMoveState);

            if (next != null)
            {
                fsm.SetState(next);
            }
        }

        private void UpdateFacing(PlayerFsm fsm, float velocityX)
        {
            if (fsm == null || fsm.Character == null || Mathf.Abs(velocityX) <= 0.001f)
            {
                return;
            }

            Vector3 scale = fsm.Character.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(velocityX);
            fsm.Character.localScale = scale;
        }

        private string ResolveAnimationName(string preferredName, string fallbackName)
        {
            if (!string.IsNullOrWhiteSpace(preferredName))
            {
                return preferredName;
            }

            return fallbackName;
        }
    }
}
