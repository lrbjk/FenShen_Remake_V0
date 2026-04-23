using FenShen.GameData;
using UnityEngine;

namespace FenShen.Combat
{
    public enum CombatTeam
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2,
    }

    public class CombatHurtbox : MonoBehaviour
    {
        [SerializeField] private CombatTeam team = CombatTeam.Neutral;
        [SerializeField] private RuntimeStatsComponent runtimeStats;
        [SerializeField] private Transform rootTransform;

        public CombatTeam Team
        {
            get { return team; }
        }

        public RuntimeStatsComponent RuntimeStats
        {
            get { return runtimeStats; }
        }

        public Transform RootTransform
        {
            get { return rootTransform != null ? rootTransform : transform.root; }
        }

        void Awake()
        {
            if (runtimeStats == null)
            {
                runtimeStats = GetComponentInParent<RuntimeStatsComponent>();
            }

            if (rootTransform == null)
            {
                rootTransform = transform.root;
            }
        }

        public bool CanBeHitBy(CombatTeam attackerTeam, Transform attackerRoot)
        {
            if (attackerRoot != null && RootTransform == attackerRoot)
            {
                return false;
            }

            if (team != CombatTeam.Neutral && attackerTeam != CombatTeam.Neutral && team == attackerTeam)
            {
                return false;
            }

            return runtimeStats != null;
        }

        public bool ApplyHit(float damage, DamageType damageType)
        {
            if (runtimeStats == null)
            {
                return false;
            }

            float finalDamage = runtimeStats.TakeDamage(damage, damageType);
            return finalDamage > 0f;
        }
    }
}
