using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [CreateAssetMenu(fileName = "Buff", menuName = "Game Data/Combat/Buff")]
    public class BuffSO : ScriptableObject
    {
        public int version = 1;
        public int buffId;
        public string buffName;
        public BuffCategory category = BuffCategory.Buff;

        [Header("Timing")]
        [Min(0f)] public float duration = 5f;
        [Min(0f)] public float tickInterval = 0f;
        public BuffStackRule stackRule = BuffStackRule.Refresh;
        public bool refreshDurationOnReapply = true;
        public bool stackable;
        [Min(1)] public int maxStacks = 1;
        [Tooltip("Optional fully qualified type name for a custom BuffBase implementation.")]
        public string customRuntimeTypeName;

        [Header("Modifiers")]
        public List<StatModifierEntry> modifiers = new List<StatModifierEntry>();

        [Header("Effects")]
        [Min(0f)] public float periodicDamage = 0f;
        [Min(0f)] public float periodicHeal = 0f;
        public DamageType periodicDamageType = DamageType.Physical;

        [Header("Control")]
        public BuffControlFlag controlFlags = BuffControlFlag.None;

        public int RuntimeKey
        {
            get
            {
                return buffId;
            }
        }

        public string DisplayName
        {
            get
            {
                return string.IsNullOrWhiteSpace(buffName) ? RuntimeKey.ToString() : buffName;
            }
        }

        public int MaxStacks
        {
            get
            {
                return Mathf.Max(1, maxStacks);
            }
        }

        public BuffStackRule ResolveStackRule()
        {
            if (stackable || maxStacks > 1)
            {
                return BuffStackRule.Stack;
            }

            return stackRule;
        }
    }
}
