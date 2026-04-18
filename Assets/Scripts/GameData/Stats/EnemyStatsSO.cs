using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "Game Data/Stats/Enemy Stats")]
    public class EnemyStatsSO : ScriptableObject
    {
        public int version = 1;
        public EnemyArchetype archetype = EnemyArchetype.Normal;

        [Header("Vitals")]
        public BaseVitalStats vital = new BaseVitalStats();

        [Header("Combat")]
        public CombatStats combat = new CombatStats();
        public EnemyCombatTuning combatTuning = new EnemyCombatTuning();

        [Header("Movement")]
        public MovementStats movement = new MovementStats();

        [Header("AI")]
        public EnemyAiStats ai = new EnemyAiStats();

        [Header("Rewards")]
        public RewardStats rewards = new RewardStats();
        public LootTableSO lootTable;

        [Header("Boss")]
        public bool isBoss;
        public List<BossPhaseSO> phases = new List<BossPhaseSO>();
    }
}
