using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "WeaponStatProfile", menuName = "Game Data/Combat/Weapon Stat Profile")]
    public class WeaponStatProfileSO : ScriptableObject
    {
        [Header("伤害")]
        [InspectorName("伤害倍率")]
        [Min(0f)] public float damageMultiplier = 1f;
        [InspectorName("削韧")]
        [Min(0f)] public float poiseDamage = 0f;
        [InspectorName("范围倍率")]
        [Min(0f)] public float rangeMultiplier = 1f;
        [InspectorName("攻速倍率")]
        [Min(0f)] public float attackSpeedMultiplier = 1f;

        [Header("时间")]
        [InspectorName("前摇倍率")]
        [Min(0f)] public float startupMultiplier = 1f;
        [InspectorName("后摇倍率")]
        [Min(0f)] public float recoveryMultiplier = 1f;
        [InspectorName("连段窗口加成")]
        [Min(0f)] public float comboWindowBonus = 0f;
        [InspectorName("命中停顿倍率")]
        [Min(0f)] public float hitPauseMultiplier = 1f;

        [Header("手感")]
        [InspectorName("击退倍率")]
        [Min(0f)] public float knockbackMultiplier = 1f;
        [InspectorName("镜头震动倍率")]
        [Min(0f)] public float cameraShakeMultiplier = 1f;
        [InspectorName("位移承诺度")]
        [Min(0f)] public float movementCommitment = 1f;
        [InspectorName("空地节奏")]
        public WeaponAirGroundRhythmProfile rhythm = new WeaponAirGroundRhythmProfile();
    }
}
