using FenShen.GameData;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "JumpState", menuName = "PlayerFSM / States / Jump")]
    public class JumpStateSO : StateSO
    {
        [Header("Animation")]
        public string jumpAnimStateName = "jump";
        public StateSO fallState;

        [Header("Air Motion")]
        public float jumpForce = 8f;
        public float gravity = 25f;
        public float maxRiseSpeed = 18f;
        public float earlyReleaseGravityMultiplier = 1.8f;
        public float fallAnimationThreshold = -0.1f;

        [Header("Horizontal Feel")]
        public float moveSpeed = 4.5f;
        public float groundedTakeoffMomentumMultiplier = 1f;
        public float stationaryTakeoffMomentumMultiplier = 0f;
        public float airAcceleration = 24f;
        public float airDeceleration = 16f;
        public float maxAirSpeedMultiplier = 1f;
        [Min(0f)]
        public float facingInputDeadzone = 0.2f;

        public override void OnEnter(PlayerFsm fsm)
        {
            base.OnEnter(fsm);

            if (fsm == null)
            {
                return;
            }

            fsm.ConsumeBufferedJump();
            fsm.ClearGroundedHistory();

            fsm.AirborneVerticalVelocity = Mathf.Max(0f, fsm.GetStat(StatKeys.JumpForce, jumpForce));
            fsm.AirborneHorizontalVelocity = ResolveTakeoffHorizontalVelocity(fsm);
            PlayAnimation(fsm, ResolveAnimationName(jumpAnimStateName, animStateName), true);
        }

        public override void OnUpdate(PlayerFsm fsm, float deltaTime)
        {
            if (fsm == null || fsm.Character == null)
            {
                return;
            }

            UpdateAirborne(fsm, deltaTime);
        }

        private void UpdateAirborne(PlayerFsm fsm, float deltaTime)
        {
            float gravityScale = Mathf.Max(0.01f, fsm.GetStat(StatKeys.GravityScale, 1f));
            float resolvedGravity = gravity * gravityScale;

            if (fsm.AirborneVerticalVelocity > 0f && !fsm.JumpIsHeld())
            {
                resolvedGravity *= earlyReleaseGravityMultiplier;
            }

            fsm.AirborneVerticalVelocity = Mathf.Max(-maxRiseSpeed, fsm.AirborneVerticalVelocity - resolvedGravity * deltaTime);
            UpdateHorizontalVelocity(fsm, deltaTime);

            Vector3 delta = new Vector3(fsm.AirborneHorizontalVelocity * deltaTime, fsm.AirborneVerticalVelocity * deltaTime, 0f);
            fsm.Character.Translate(delta, Space.World);
            UpdateFacing(fsm);

            if (fsm.AirborneVerticalVelocity <= fallAnimationThreshold && fallState != null)
            {
                fsm.SetState(fallState);
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
