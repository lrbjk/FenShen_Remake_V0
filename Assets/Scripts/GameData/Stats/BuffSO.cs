using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "Buff", menuName = "Game Data/Combat/Buff")]
    public class BuffSO : ScriptableObject
    {
        public int version = 1;
        public string buffId;
        public BuffCategory category = BuffCategory.Buff;

        [Header("Timing")]
        [Min(0f)] public float duration = 5f;
        [Min(0f)] public float tickInterval = 0f;
        public bool refreshDurationOnReapply = true;
        public bool stackable;
        [Min(1)] public int maxStacks = 1;

        [Header("Modifiers")]
        public List<StatModifierEntry> modifiers = new List<StatModifierEntry>();

        [Header("Effects")]
        [Min(0f)] public float periodicDamage = 0f;
        [Min(0f)] public float periodicHeal = 0f;
        public DamageType periodicDamageType = DamageType.Physical;
    }
}
