using FenShen.GameData;
using UnityEngine;

namespace FenShen.Combat
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly DamageType DamageType;
        public readonly GameObject Source;
        public readonly GameObject Instigator;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitDirection;
        public readonly float PoiseDamage;

        public DamageInfo(
            float amount,
            DamageType damageType = DamageType.Physical,
            GameObject source = null,
            GameObject instigator = null,
            Vector3 hitPoint = default,
            Vector3 hitDirection = default,
            float poiseDamage = 0f)
        {
            Amount = amount;
            DamageType = damageType;
            Source = source;
            Instigator = instigator;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            PoiseDamage = poiseDamage;
        }
    }

    public readonly struct DamageResult
    {
        public readonly bool Applied;
        public readonly float FinalDamage;
        public readonly bool Killed;

        public DamageResult(bool applied, float finalDamage, bool killed)
        {
            Applied = applied;
            FinalDamage = finalDamage;
            Killed = killed;
        }
    }

    public interface IDamageable
    {
        bool CanReceiveDamage(DamageInfo damageInfo);
        DamageResult ReceiveDamage(DamageInfo damageInfo);
        float ReceiveHeal(float amount, GameObject source = null);
    }
}
