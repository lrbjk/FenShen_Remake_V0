using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "DifficultyCurve", menuName = "Game Data/Balance/Difficulty Curve")]
    public class DifficultyCurveSO : ScriptableObject
    {
        public int version = 1;

        [Header("Global Multipliers")]
        public AnimationCurve areaIndexToEnemyHpMultiplier = AnimationCurve.Linear(1f, 1f, 8f, 4f);
        public AnimationCurve areaIndexToEnemyAttackMultiplier = AnimationCurve.Linear(1f, 1f, 8f, 2.5f);
        public AnimationCurve areaIndexToEnemyDefenseMultiplier = AnimationCurve.Linear(1f, 1f, 8f, 1.8f);
        public AnimationCurve areaIndexToRewardMultiplier = AnimationCurve.Linear(1f, 1f, 8f, 2.5f);

        [Header("Player Safety")]
        public AnimationCurve levelDeltaToDamageTakenMultiplier = AnimationCurve.Linear(-5f, 1.5f, 5f, 0.8f);
        public AnimationCurve levelDeltaToDamageDealtMultiplier = AnimationCurve.Linear(-5f, 0.8f, 5f, 1.3f);
    }
}
