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

    public class CombatHurtbox : MonoBehaviour, IDamageable
    {
        [InspectorName("阵营")]
        [SerializeField] private CombatTeam team = CombatTeam.Neutral;
        [InspectorName("运行时属性")]
        [SerializeField] private RuntimeStatsComponent runtimeStats;
        [InspectorName("根节点")]
        [SerializeField] private Transform rootTransform;

        private BuffController _buffController;

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

            _buffController = GetComponentInParent<BuffController>();
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

        public bool CanReceiveDamage(DamageInfo damageInfo)
        {
            if (runtimeStats == null)
            {
                return false;
            }

            if (damageInfo.Instigator != null && RootTransform == damageInfo.Instigator.transform.root)
            {
                return false;
            }

            if (_buffController != null && _buffController.HasControlFlag(BuffControlFlag.Invulnerable))
            {
                return false;
            }

            return true;
        }

        public DamageResult ReceiveDamage(DamageInfo damageInfo)
        {
            if (!CanReceiveDamage(damageInfo))
            {
                return new DamageResult(false, 0f, false);
            }

            float beforeHp = runtimeStats.CurrentHP;
            float finalDamage = runtimeStats.TakeDamage(damageInfo.Amount, damageInfo.DamageType);
            bool killed = beforeHp > 0f && runtimeStats.CurrentHP <= 0f;
            return new DamageResult(finalDamage > 0f, finalDamage, killed);
        }

        public float ReceiveHeal(float amount, GameObject source = null)
        {
            return runtimeStats != null ? runtimeStats.Heal(amount) : 0f;
        }

        public bool ApplyHit(float damage, DamageType damageType)
        {
            DamageResult result = ReceiveDamage(new DamageInfo(damage, damageType));
            return result.Applied;
        }
    }
}
