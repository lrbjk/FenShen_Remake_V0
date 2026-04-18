using UnityEngine;

namespace FenShen.GameData
{
    public class EnemyRuntimeStatsComponent : RuntimeStatsComponent
    {
        [Header("Config")]
        public EnemyStatsSO enemyStats;
        public DifficultyCurveSO difficultyCurve;
        [Min(1)]
        public int areaIndex = 1;
        public int playerLevelDelta;
        public bool initializeOnAwake = true;
        public bool applyDifficultyOnInitialize = true;

        void Awake()
        {
            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            runtimeStats.InitializeFromEnemy(enemyStats);

            if (applyDifficultyOnInitialize && difficultyCurve != null)
            {
                runtimeStats.ApplyDifficulty(difficultyCurve, areaIndex, playerLevelDelta);
            }
        }

        public void EnterBossPhase(BossPhaseSO phase)
        {
            runtimeStats.ApplyBossPhase(phase);
        }
    }
}
