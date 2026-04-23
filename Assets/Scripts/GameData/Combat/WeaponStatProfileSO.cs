using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "WeaponStatProfile", menuName = "Game Data/Combat/Weapon Stat Profile")]
    public class WeaponStatProfileSO : ScriptableObject
    {
        [Header("Damage")]
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0f)] public float poiseDamage = 0f;
        [Min(0f)] public float rangeMultiplier = 1f;
        [Min(0f)] public float attackSpeedMultiplier = 1f;

        [Header("Timing")]
        [Min(0f)] public float startupMultiplier = 1f;
        [Min(0f)] public float recoveryMultiplier = 1f;
        [Min(0f)] public float comboWindowBonus = 0f;
        [Min(0f)] public float hitPauseMultiplier = 1f;

        [Header("Feel")]
        [Min(0f)] public float knockbackMultiplier = 1f;
        [Min(0f)] public float cameraShakeMultiplier = 1f;
        [Min(0f)] public float movementCommitment = 1f;
        public WeaponAirGroundRhythmProfile rhythm = new WeaponAirGroundRhythmProfile();
    }
}
