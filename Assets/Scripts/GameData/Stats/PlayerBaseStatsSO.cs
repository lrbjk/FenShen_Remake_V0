using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Game Data/Stats/Player Base Stats")]
    public class PlayerBaseStatsSO : ScriptableObject
    {
        public int version = 1;

        [Header("Vitals")]
        public BaseVitalStats vital = new BaseVitalStats();

        [Header("Combat")]
        public CombatStats combat = new CombatStats();

        [Header("Movement")]
        public MovementStats movement = new MovementStats();

        [Header("Combat Feel")]
        public CombatFeelStats feel = new CombatFeelStats();

        [Header("Growth Snapshot")]
        public GrowthStats growth = new GrowthStats();
    }
}
