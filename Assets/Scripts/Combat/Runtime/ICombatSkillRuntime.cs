using System.Collections.Generic;
using UnityEngine;

namespace FenShen.Combat
{
    public interface ICombatSkillRuntime
    {
        bool IsFacingLeft();
        void PlayCombatAnimation(string animationStateName, int animationLayer, bool crossFade, float transitionDuration);
        void ApplySkillMotion(Vector2 delta);
        Vector2 SampleAnimatorMotion(string curveXName, string curveYName, bool mirrorByFacing);
        bool TryApplyHitClip(CombatSkillDefinitionSO skill, string skillId, HitSkillClip clip, HashSet<int> hitTargets);
        void NotifySkillHitConfirmed(CombatSkillDefinitionSO skill);
        void ExecuteAttack(CombatSkillDefinitionSO skill, string skillId);
        void SpawnProjectile(ProjectileSkillClip clip);
        void SpawnVfx(VfxSkillClip clip);
        void PlaySfx(SfxSkillClip clip);
        void TriggerCameraShake(CameraShakeSkillClip clip);
        void ApplySelfBuffClip(SelfBuffSkillClip clip);
        void ApplyCoreResourceClip(CoreResourceSkillClip clip);
    }
}
