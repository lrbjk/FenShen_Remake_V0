using System;
using UnityEngine;

namespace FenShen.GameData
{
    public abstract class RuntimeStatsComponent : MonoBehaviour
    {
        protected RuntimeStats runtimeStats = new RuntimeStats();

        public RuntimeStats Stats
        {
            get { return runtimeStats; }
        }

        public float CurrentHP
        {
            get { return runtimeStats.CurrentHP; }
        }

        public float CurrentEnergy
        {
            get { return runtimeStats.CurrentEnergy; }
        }

        public float MaxHP
        {
            get { return runtimeStats.GetStat(StatKeys.MaxHP); }
        }

        public float Attack
        {
            get { return runtimeStats.GetStat(StatKeys.Attack); }
        }

        public float Defense
        {
            get { return runtimeStats.GetStat(StatKeys.Defense); }
        }

        public event Action<float> Damaged
        {
            add { runtimeStats.Damaged += value; }
            remove { runtimeStats.Damaged -= value; }
        }

        public event Action<float> Healed
        {
            add { runtimeStats.Healed += value; }
            remove { runtimeStats.Healed -= value; }
        }

        public event Action Died
        {
            add { runtimeStats.Died += value; }
            remove { runtimeStats.Died -= value; }
        }

        public event Action StatsChanged
        {
            add { runtimeStats.StatsChanged += value; }
            remove { runtimeStats.StatsChanged -= value; }
        }

        public float TakeDamage(float rawDamage, DamageType damageType = DamageType.Physical)
        {
            return runtimeStats.TakeDamage(rawDamage, damageType);
        }

        public float Heal(float amount)
        {
            return runtimeStats.Heal(amount);
        }

        public bool ConsumeEnergy(float amount)
        {
            return runtimeStats.ConsumeEnergy(amount);
        }

        public float RestoreEnergy(float amount)
        {
            return runtimeStats.RestoreEnergy(amount);
        }

        public float GetStat(string statKey)
        {
            return runtimeStats.GetStat(statKey);
        }
    }
}
