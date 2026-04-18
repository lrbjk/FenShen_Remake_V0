using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Game Data/Combat/Weapon")]
    public class WeaponSO : ScriptableObject
    {
        public int version = 1;
        public string weaponId;
        public WeaponAttackType attackType = WeaponAttackType.Melee;
        public DamageType damageType = DamageType.Physical;

        [Header("Base")]
        [Min(0f)] public float attackPower = 10f;
        [Min(0f)] public float attackMultiplier = 1f;
        [Range(0f, 1f)] public float critRateBonus = 0f;
        [Min(0f)] public float critDamageBonus = 0f;
        [Min(0f)] public float attackSpeedMultiplier = 1f;
        [Min(0f)] public float staminaOrEnergyCost = 0f;

        [Header("Range")]
        [Min(0f)] public float range = 1.5f;
        [Min(0f)] public float knockbackPower = 0f;

        [Header("Scaling")]
        public AnimationCurve levelScaling = AnimationCurve.Linear(1f, 1f, 50f, 2f);
        public List<StatModifierEntry> extraModifiers = new List<StatModifierEntry>();
    }
}
