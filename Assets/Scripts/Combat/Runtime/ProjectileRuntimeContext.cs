using UnityEngine;

namespace FenShen.Combat
{
    public readonly struct ProjectileRuntimeContext
    {
        public readonly CombatCoordinator Owner;
        public readonly ProjectileSkillClip Clip;
        public readonly Vector3 Direction;

        public ProjectileRuntimeContext(CombatCoordinator owner, ProjectileSkillClip clip, Vector3 direction)
        {
            Owner = owner;
            Clip = clip;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        }
    }
}
