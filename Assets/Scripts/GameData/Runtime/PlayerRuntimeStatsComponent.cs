using UnityEngine;

namespace FenShen.GameData
{
    public class PlayerRuntimeStatsComponent : RuntimeStatsComponent
    {
        [Header("Config")]
        public PlayerBaseStatsSO baseStats;
        public PlayerGrowthSO growthStats;
        [Min(1)]
        public int startLevel = 1;
        public bool initializeOnAwake = true;

        void Awake()
        {
            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            runtimeStats.InitializeFromPlayer(baseStats, growthStats, startLevel);
        }

        public void SetLevel(int level)
        {
            startLevel = Mathf.Max(1, level);
            Initialize();
        }
    }
}
