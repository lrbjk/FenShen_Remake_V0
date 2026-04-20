using FenShen.GameData;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "FallState", menuName = "PlayerFSM / States / Fall")]
    public class FallStateSO : StateSO
    {
        [Header("Animation")]
        public string fallAnimStateName = "jumptofall";
        public string landingAnimStateName = "landing";

        [Header("Air Motion")]
        public float gravity = 25f;
        public float maxFallSpeed = 18f;
        public float fallGravityMultiplier = 1.15f;

        [Header("Horizontal Feel")]
        public float moveSpeed = 4.5f;
        public float airAcceleration = 24f;
        public float airDeceleration = 16f;
        public float maxAirSpeedMultiplier = 1f;
        [Min(0f)]
        public float facingInputDeadzone = 0.2f;
        [Min(0f)]
        public float groundedInertiaMultiplier = 1f;
        [Min(0f)]
        public float groundedInertiaAdoptThreshold = 0.01f;

        [Header("Landing")]
        public float landingDuration = 0.12f;
        public float landingMoveDamping = 10f;
        public StateSO groundedIdleState;
        public StateSO groundedMoveState;

        private bool _isLanding;
        private float _landingTimer;

        public override void OnEnter(PlayerFsm fsm)
        {
            base.OnEnter(fsm);

            if (fsm != null && Mathf.Abs(fsm.AirborneHorizontalVelocity) <= groundedInertiaAdoptThreshold)
            {
                fsm.AirborneHorizontalVelocity = fsm.GroundedHorizontalVelocity * groundedInertiaMultiplier;
            }

            _isLanding = false;
            _landingTimer = 0f;
            PlayAnimation(fsm, ResolveAnimationName(fallAnimStateName, animStateName), true);

            if (fsm != null && fsm.CheckGround())
            {
                StartLanding(fsm);
            }
        }

        public override void OnUpdate(PlayerFsm fsm, float deltaTime)
        {
            if (fsm == null || fsm.Character == null)
            {
                return;
            }

            if (_isLanding)
            {
                UpdateLanding(fsm, deltaTime);
                return;
            }

            UpdateAirborne(fsm, deltaTime);
        }

        private void UpdateAirborne(PlayerFsm fsm, float deltaTime)
        {
            float gravityScale = Mathf.Max(0.01f, fsm.GetStat(StatKeys.GravityScale, 1f));
            float resolvedGravity = gravity * gravityScale * fallGravityMultiplier;

            fsm.AirborneVerticalVelocity = Mathf.Max(-maxFallSpeed, fsm.AirborneVerticalVelocity - resolvedGravity * deltaTime);
            UpdateHorizontalVelocity(fsm, deltaTime);

            Vector3 delta = new Vector3(fsm.AirborneHorizontalVelocity * deltaTime, fsm.AirborneVerticalVelocity * deltaTime, 0f);
            fsm.Character.Translate(delta, Space.World);
            UpdateFacing(fsm);

            if (fsm.AirborneVerticalVelocity <= 0f && fsm.CheckGround())
            {
                StartLanding(fsm);
            }
        }

        private void UpdateLanding(PlayerFsm fsm, float deltaTime)
        {
            _landingTimer += deltaTime;
            fsm.AirborneHorizontalVelocity = Mathf.MoveTowards(fsm.AirborneHorizontalVelocity, 0f, landingMoveDamping * deltaTime);

            if (Mathf.Abs(fsm.AirborneHorizontalVelocity) > 0.001f)
            {
                fsm.Character.Translate(new Vector3(fsm.AirborneHorizontalVelocity * deltaTime, 0f, 0f), Space.World);
                UpdateFacing(fsm);
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
            fsm.AirborneHorizontalVelocity = Mathf.MoveTowards(fsm.AirborneHorizontalVelocity, targetVelocity, acceleration * deltaTime);
        }

        private void StartLanding(PlayerFsm fsm)
        {
            _isLanding = true;
            _landingTimer = 0f;
            fsm.AirborneVerticalVelocity = 0f;
            PlayAnimation(fsm, ResolveAnimationName(landingAnimStateName, fallAnimStateName), true);
        }

        private void ExitToGroundedState(PlayerFsm fsm)
        {
            StateSO next = fsm.MoveIsHeld()
                ? (groundedMoveState != null ? groundedMoveState : groundedIdleState)
                : (groundedIdleState != null ? groundedIdleState : groundedMoveState);

            if (next != null)
            {
                fsm.SetState(next);
            }
        }

        private void UpdateFacing(PlayerFsm fsm)
        {
            if (fsm == null || fsm.Character == null)
            {
                return;
            }

            float inputX = fsm.CurrentMoveInput.x;
            if (Mathf.Abs(inputX) < facingInputDeadzone)
            {
                return;
            }

            Vector3 scale = fsm.Character.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(inputX);
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
