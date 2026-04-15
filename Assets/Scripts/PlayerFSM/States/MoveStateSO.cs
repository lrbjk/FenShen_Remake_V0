using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "MoveState", menuName = "PlayerFSM / States / Move")]
    public class MoveStateSO : StateSO
    {
        [Header("Movement")]
        public float baseSpeed = 3.5f;
        public float sprintMultiplier = 1.5f;
        public bool useAnimatorCurve = true;
        public string speedCurveParameter = "MoveSpeedCurve";
        public AnimationCurve fallbackSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);
        public float rotationSpeed = 720f;
        [Header("Animation Names")]
        public bool useSeparateAnimations = true;
        public string walkAnim = "Walk";
        public string runAnim = "Run";
        private bool _lastSprint;

        public override void OnEnter(PlayerFsm fsm)
        {
            base.OnEnter(fsm);
            if (useSeparateAnimations && fsm != null && fsm.Animator != null)
            {
                _lastSprint = fsm.IsSprinting;
                var anim = _lastSprint ? runAnim : walkAnim;
                if (!string.IsNullOrEmpty(anim)) fsm.Animator.CrossFadeInFixedTime(anim, transitionDuration, animLayer);
            }
        }
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
                float spMul = fsm.IsSprinting ? sprintMultiplier : 1f;
                float speed = baseSpeed * curveMul * spMul;

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
            if (useSeparateAnimations && fsm.Animator != null)
            {
                bool s = fsm.IsSprinting;
                if (s != _lastSprint)
                {
                    _lastSprint = s;
                    var anim = s ? runAnim : walkAnim;
                    if (!string.IsNullOrEmpty(anim)) fsm.Animator.CrossFadeInFixedTime(anim, transitionDuration, animLayer);
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
