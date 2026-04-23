using UnityEngine;

namespace FenShen.Combat
{
    public class SkillCombatState : CombatState
    {
        private readonly CombatSkillDefinitionSO _skill;
        private readonly string _skillId;
        private readonly float _duration;
        private readonly string _animationStateName;
        private readonly int _animationLayer;
        private readonly bool _crossFade;
        private readonly float _transitionDuration;
        private float _elapsed;

        public SkillCombatState(CombatSkillDefinitionSO skill, string skillId, float duration, string animationStateName, int animationLayer, bool crossFade, float transitionDuration)
        {
            _skill = skill;
            _skillId = skill != null ? skill.ResolveSkillId() : (string.IsNullOrWhiteSpace(skillId) ? "Primary" : skillId);
            _duration = skill != null ? Mathf.Max(0.01f, skill.duration) : Mathf.Max(0.01f, duration);
            _animationStateName = skill != null && !string.IsNullOrWhiteSpace(skill.animationStateName) ? skill.animationStateName : animationStateName;
            _animationLayer = skill != null ? skill.animationLayer : animationLayer;
            _crossFade = skill != null ? skill.crossFade : crossFade;
            _transitionDuration = skill != null ? skill.transitionDuration : transitionDuration;
        }

        public override void OnEnter(CombatCoordinator coordinator)
        {
            _elapsed = 0f;
            coordinator.BeginSkillExecution(
                _skill,
                _skillId,
                _duration,
                _animationStateName,
                _animationLayer,
                _crossFade,
                _transitionDuration);
        }

        public override bool Tick(CombatCoordinator coordinator, float deltaTime)
        {
            _elapsed += deltaTime;
            return coordinator.TickSkillExecution(deltaTime);
        }

        public override void OnExit(CombatCoordinator coordinator)
        {
            coordinator.EndSkillExecution();
        }
    }
}
