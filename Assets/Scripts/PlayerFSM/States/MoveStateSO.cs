using UnityEngine;
using FenShen.GameData;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "MoveState", menuName = "PlayerFSM / States / Move")]
    public class MoveStateSO : StateSO
    {
        [Header("Movement")]
        public float baseSpeed = 3.5f;
        public float movementMultiplier = 1f;
        public bool useAnimatorCurve = true;
        public string speedCurveParameter = "MoveSpeedCurve";
        public AnimationCurve fallbackSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);
        public float rotationSpeed = 720f;

        public override void OnUpdate(PlayerFsm fsm, float dt)
        {
            if (fsm == null || fsm.Character == null) return;
            var input = fsm.CurrentMoveInput;
            var dir = new Vector3(input.x, 0f, 0f);
            float mag = Mathf.Clamp01(dir.magnitude);
            if (mag > 0.001f)
            {
                dir.Normalize();
                float curveMul = ResolveCurveMultiplier(fsm);
                float resolvedBaseSpeed = fsm.GetStat(StatKeys.MoveSpeed, baseSpeed);
                float speed = resolvedBaseSpeed * movementMultiplier * curveMul;
                float signedSpeed = dir.x * speed;
                fsm.GroundedHorizontalVelocity = signedSpeed;
                fsm.AirborneHorizontalVelocity = signedSpeed;

                if (dir.x < 0)
                {
                    fsm.Character.localScale = new Vector3(-1, 1, 1);
                    fsm.Character.Translate(-Vector3.right * speed * dt, Space.Self);
                }
                else
                {
                    fsm.Character.localScale = new Vector3(1, 1, 1);
                    fsm.Character.Translate(Vector3.right * speed * dt, Space.Self);
                }
            }
        }

        private float ResolveCurveMultiplier(PlayerFsm fsm)
        {
            if (fsm != null && fsm.Animator != null && useAnimatorCurve && !string.IsNullOrWhiteSpace(speedCurveParameter))
            {
                var parameters = fsm.Animator.parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == speedCurveParameter)
                    {
                        return fsm.Animator.GetFloat(speedCurveParameter);
                    }
                }
            }

            float nt = GetNormalizedTime(fsm);
            return fallbackSpeedCurve != null ? fallbackSpeedCurve.Evaluate(nt - Mathf.Floor(nt)) : 1f;
        }
    }
}
