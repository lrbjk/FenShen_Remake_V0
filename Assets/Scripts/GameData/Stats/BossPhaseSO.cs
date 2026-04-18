using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "BossPhase", menuName = "Game Data/Boss/Boss Phase")]
    public class BossPhaseSO : ScriptableObject
    {
        public int version = 1;
        public string phaseId;

        [Header("Trigger")]
        [Range(0f, 1f)] public float phaseHpThreshold = 0.7f;

        [Header("Combat")]
        [Min(0f)] public float rageMultiplier = 1f;
        [Min(0f)] public float attackMultiplier = 1f;
        [Min(0f)] public float defenseMultiplier = 1f;
        [Min(0f)] public float speedMultiplier = 1f;
        [Min(0f)] public float staggerValue = 0f;

        [Header("Pattern")]
        public List<string> skillRotation = new List<string>();
        public List<StatModifierEntry> extraModifiers = new List<StatModifierEntry>();
    }
}
