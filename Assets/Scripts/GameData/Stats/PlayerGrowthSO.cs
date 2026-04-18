using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "PlayerGrowth", menuName = "Game Data/Stats/Player Growth")]
    public class PlayerGrowthSO : ScriptableObject
    {
        public int version = 1;

        [Header("Level Curves")]
        public AnimationCurve levelToMaxHp = AnimationCurve.Linear(1f, 100f, 50f, 500f);
        public AnimationCurve levelToAttack = AnimationCurve.Linear(1f, 10f, 50f, 100f);
        public AnimationCurve levelToDefense = AnimationCurve.Linear(1f, 0f, 50f, 60f);
        public AnimationCurve levelToMoveSpeed = AnimationCurve.Linear(1f, 5f, 50f, 7f);

        [Header("Progression")]
        [Min(1)] public int maxLevel = 50;
        public AnimationCurve levelToExpRequired = AnimationCurve.Linear(1f, 100f, 50f, 5000f);
        public AnimationCurve levelToSkillPointReward = AnimationCurve.Constant(1f, 50f, 1f);
        public AnimationCurve levelToLuck = AnimationCurve.Linear(1f, 0f, 50f, 0.25f);
        public AnimationCurve levelToDiscovery = AnimationCurve.Linear(1f, 1f, 50f, 1.5f);
    }
}
