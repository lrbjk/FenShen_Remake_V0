using UnityEngine;

namespace FenShen.PlayerFSM
{
    public abstract class StateSO : ScriptableObject
    {
        [Header("Editor")]
        public string displayName;
        public Vector2 editorPosition = new Vector2(120f, 120f);

        [Header("Animation")]
        public string animStateName;
        public int animLayer = 0;
        public bool crossFade = true;
        public float transitionDuration = 0.1f;

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(displayName) ? name : displayName; }
        }

        public virtual void OnEnter(PlayerFsm fsm)
        {
            PlayAnimation(fsm, animStateName, true);
        }

        public virtual void OnUpdate(PlayerFsm fsm, float deltaTime) { }
        public virtual void OnExit(PlayerFsm fsm) { }

        protected void PlayAnimation(PlayerFsm fsm, string animationName, bool restart)
        {
            if (fsm == null || fsm.Animator == null || string.IsNullOrWhiteSpace(animationName))
            {
                return;
            }

            if (crossFade)
            {
                fsm.Animator.CrossFadeInFixedTime(animationName, transitionDuration, animLayer);
                return;
            }

            fsm.Animator.Play(animationName, animLayer, restart ? 0f : float.NegativeInfinity);
        }

        protected float GetNormalizedTime(PlayerFsm fsm)
        {
            if (fsm != null && fsm.Animator != null)
            {
                var info = fsm.Animator.GetCurrentAnimatorStateInfo(animLayer);
                return info.normalizedTime;
            }
            return 0f;
        }
    }
}
