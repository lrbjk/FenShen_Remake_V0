using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    public class StatBuff : BuffBase
    {
        public override void OnApply()
        {
            ApplyModifiers();
        }

        public override void OnRemove()
        {
            var stats = GetRuntimeStats();
            if (stats == null)
            {
                return;
            }

            stats.RemoveModifiersBySource(instance.sourceId);
        }

        public override void OnRefresh()
        {
            ApplyModifiers();
        }

        public override void OnStackChanged(int previousStack, int newStack)
        {
            ApplyModifiers();
        }

        protected virtual void ApplyModifiers()
        {
            var stats = GetRuntimeStats();
            if (stats == null || instance == null || instance.data == null)
            {
                return;
            }

            stats.RemoveModifiersBySource(instance.sourceId);

            List<StatModifierEntry> modifiers = instance.data.modifiers;
            if (modifiers == null)
            {
                return;
            }

            int stackCount = GetCurrentStack();
            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier == null || string.IsNullOrWhiteSpace(modifier.statKey))
                {
                    continue;
                }

                stats.AddModifier(new RuntimeStatModifier
                {
                    sourceId = instance.sourceId,
                    statKey = modifier.statKey,
                    operation = modifier.operation,
                    value = ScaleModifier(modifier, stackCount)
                });
            }
        }

        private float ScaleModifier(StatModifierEntry modifier, int stackCount)
        {
            if (stackCount <= 1)
            {
                return modifier.value;
            }

            if (modifier.operation == ModifierOperation.Add)
            {
                return modifier.value * stackCount;
            }

            return Mathf.Pow(modifier.value, stackCount);
        }
    }
}
