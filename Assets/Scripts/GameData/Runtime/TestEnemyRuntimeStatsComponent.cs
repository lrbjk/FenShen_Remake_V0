using UnityEngine;

namespace FenShen.GameData
{
    public class TestEnemyRuntimeStatsComponent : RuntimeStatsComponent
    {
        [Header("测试敌人数值")]
        [InspectorName("启动时初始化")]
        [SerializeField] private bool initializeOnAwake = true;
        [InspectorName("最大生命")]
        [SerializeField, Min(1f)] private float maxHP = 120f;
        [InspectorName("攻击力")]
        [SerializeField, Min(0f)] private float attack = 8f;
        [InspectorName("防御力")]
        [SerializeField, Min(0f)] private float defense = 0f;
        [InspectorName("削韧值")]
        [SerializeField, Min(0f)] private float staggerValue = 10f;

        void Awake()
        {
            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            runtimeStats.SetBaseStat(StatKeys.MaxHP, maxHP);
            runtimeStats.SetBaseStat(StatKeys.Attack, attack);
            runtimeStats.SetBaseStat(StatKeys.Defense, defense);
            runtimeStats.SetBaseStat(StatKeys.StaggerValue, staggerValue);
            runtimeStats.SetBaseStat(StatKeys.HealMultiplier, 1f);
            runtimeStats.SetCurrentHP(maxHP);
        }
    }
}
