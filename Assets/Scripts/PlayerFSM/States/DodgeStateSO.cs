using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "DodgeState", menuName = "PlayerFSM / States / Dodge")]
    public class DodgeStateSO : StateSO
    {
        [Header("Dodge")]
        public float dodgeDistance = 2.2f;
        public bool useAnimatorCurve = true;
        public string displacementCurveParameter = "DodgeDisplacement";
        public AnimationCurve fallbackDisplacementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private float _directionSign = 1f;
        private float _lastDisplacement01;

        public override void OnEnter(PlayerFsm fsm)
        {
            base.OnEnter(fsm);

            _directionSign = ResolveDirectionSign(fsm);
            _lastDisplacement01 = 0f;

            if (fsm != null && fsm.Character != null)
            {
                Vector3 scale = fsm.Character.localScale;
                scale.x = Mathf.Abs(scale.x) * _directionSign;
                fsm.Character.localScale = scale;
            }
        }

        public override void OnUpdate(PlayerFsm fsm, float deltaTime)
        {
            if (fsm == null || fsm.Character == null)
            {
                return;
            }

            float displacement01 = SampleDisplacement01(fsm);
            float delta01 = Mathf.Max(0f, displacement01 - _lastDisplacement01);
            _lastDisplacement01 = displacement01;

            if (delta01 <= 0f)
            {
                return;
            }

            Vector3 delta = Vector3.right * (_directionSign * dodgeDistance * delta01);
            fsm.Character.Translate(delta, Space.World);
        }

        private float ResolveDirectionSign(PlayerFsm fsm)
        {
            if (fsm == null)
            {
                return 1f;
            }

            if (Mathf.Abs(fsm.CurrentMoveInput.x) > 0.01f)
            {
                return Mathf.Sign(fsm.CurrentMoveInput.x);
            }

            if (fsm.Character != null)
            {
                return Mathf.Sign(Mathf.Approximately(fsm.Character.localScale.x, 0f) ? 1f : fsm.Character.localScale.x);
            }

            return 1f;
        }

        private float SampleDisplacement01(PlayerFsm fsm)
        {
            if (fsm != null && fsm.Animator != null && useAnimatorCurve && !string.IsNullOrWhiteSpace(displacementCurveParameter))
            {
                var parameters = fsm.Animator.parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == displacementCurveParameter)
                    {
                        return Mathf.Clamp01(fsm.Animator.GetFloat(displacementCurveParameter));
                    }
                }
            }

            float normalizedTime = GetNormalizedTime(fsm);
            float time01 = normalizedTime - Mathf.Floor(normalizedTime);
            return fallbackDisplacementCurve != null ? Mathf.Clamp01(fallbackDisplacementCurve.Evaluate(time01)) : time01;
        }
    }
}
