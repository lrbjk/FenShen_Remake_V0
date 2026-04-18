using System;
using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    [Serializable]
    public class RuntimeStatModifier
    {
        public string sourceId;
        public string statKey;
        public ModifierOperation operation = ModifierOperation.Add;
        public float value;
    }

    public class RuntimeStats
    {
        private readonly Dictionary<string, float> _baseStats = new Dictionary<string, float>();
        private readonly Dictionary<DamageType, float> _resistances = new Dictionary<DamageType, float>();
        private readonly List<RuntimeStatModifier> _modifiers = new List<RuntimeStatModifier>();

        public int Level { get; private set; } = 1;
        public float CurrentHP { get; private set; }
        public float CurrentEnergy { get; private set; }

        public event Action StatsChanged;
        public event Action<float> Damaged;
        public event Action<float> Healed;
        public event Action Died;

        public void InitializeFromPlayer(PlayerBaseStatsSO baseStats, PlayerGrowthSO growth, int level)
        {
            Clear();

            if (baseStats == null)
            {
                return;
            }

            Level = Mathf.Max(1, level);
            ApplyBaseVital(baseStats.vital);
            ApplyCombat(baseStats.combat);
            ApplyMovement(baseStats.movement);
            ApplyFeel(baseStats.feel);
            SetBaseStat(StatKeys.Luck, baseStats.growth.luck);
            SetBaseStat(StatKeys.Discovery, baseStats.growth.discovery);

            if (growth != null)
            {
                SetBaseStat(StatKeys.MaxHP, EvaluateCurve(growth.levelToMaxHp, Level, baseStats.vital.maxHP));
                SetBaseStat(StatKeys.Attack, EvaluateCurve(growth.levelToAttack, Level, baseStats.combat.attack));
                SetBaseStat(StatKeys.Defense, EvaluateCurve(growth.levelToDefense, Level, baseStats.vital.defense));
                SetBaseStat(StatKeys.MoveSpeed, EvaluateCurve(growth.levelToMoveSpeed, Level, baseStats.movement.moveSpeed));
                SetBaseStat(StatKeys.Luck, EvaluateCurve(growth.levelToLuck, Level, baseStats.growth.luck));
                SetBaseStat(StatKeys.Discovery, EvaluateCurve(growth.levelToDiscovery, Level, baseStats.growth.discovery));
            }

            CurrentHP = GetStat(StatKeys.MaxHP);
            CurrentEnergy = GetStat(StatKeys.MaxEnergy);
            NotifyStatsChanged();
        }

        public void InitializeFromEnemy(EnemyStatsSO enemyStats)
        {
            Clear();

            if (enemyStats == null)
            {
                return;
            }

            Level = 1;
            ApplyBaseVital(enemyStats.vital);
            ApplyCombat(enemyStats.combat);
            ApplyMovement(enemyStats.movement);
            ApplyEnemyAi(enemyStats.ai);
            ApplyEnemyCombat(enemyStats.combatTuning);
            SetBaseStat(StatKeys.ExpDrop, enemyStats.rewards.expDrop);
            SetBaseStat(StatKeys.GoldDrop, enemyStats.rewards.goldDrop);

            CurrentHP = GetStat(StatKeys.MaxHP);
            CurrentEnergy = GetStat(StatKeys.MaxEnergy);
            NotifyStatsChanged();
        }

        public void ApplyDifficulty(DifficultyCurveSO difficulty, int areaIndex, int playerLevelDelta = 0)
        {
            if (difficulty == null)
            {
                return;
            }

            MultiplyBaseStat(StatKeys.MaxHP, EvaluateCurve(difficulty.areaIndexToEnemyHpMultiplier, areaIndex, 1f));
            MultiplyBaseStat(StatKeys.Attack, EvaluateCurve(difficulty.areaIndexToEnemyAttackMultiplier, areaIndex, 1f));
            MultiplyBaseStat(StatKeys.Defense, EvaluateCurve(difficulty.areaIndexToEnemyDefenseMultiplier, areaIndex, 1f));
            MultiplyBaseStat(StatKeys.ExpDrop, EvaluateCurve(difficulty.areaIndexToRewardMultiplier, areaIndex, 1f));
            MultiplyBaseStat(StatKeys.GoldDrop, EvaluateCurve(difficulty.areaIndexToRewardMultiplier, areaIndex, 1f));

            AddModifier(new RuntimeStatModifier
            {
                sourceId = "difficulty-level-delta-dealt",
                statKey = StatKeys.Attack,
                operation = ModifierOperation.Multiply,
                value = EvaluateCurve(difficulty.levelDeltaToDamageDealtMultiplier, playerLevelDelta, 1f)
            });

            AddModifier(new RuntimeStatModifier
            {
                sourceId = "difficulty-level-delta-taken",
                statKey = StatKeys.Defense,
                operation = ModifierOperation.Multiply,
                value = 2f - EvaluateCurve(difficulty.levelDeltaToDamageTakenMultiplier, playerLevelDelta, 1f)
            });

            CurrentHP = Mathf.Min(CurrentHP, GetStat(StatKeys.MaxHP));
            NotifyStatsChanged();
        }

        public void ApplyBossPhase(BossPhaseSO phase)
        {
            if (phase == null)
            {
                return;
            }

            AddOrReplaceModifier("boss-phase-attack", StatKeys.Attack, ModifierOperation.Multiply, phase.attackMultiplier);
            AddOrReplaceModifier("boss-phase-defense", StatKeys.Defense, ModifierOperation.Multiply, phase.defenseMultiplier);
            AddOrReplaceModifier("boss-phase-speed", StatKeys.MoveSpeed, ModifierOperation.Multiply, phase.speedMultiplier);
            AddOrReplaceModifier("boss-phase-rage", StatKeys.RageMultiplier, ModifierOperation.Multiply, phase.rageMultiplier);
            AddOrReplaceModifier("boss-phase-stagger", StatKeys.StaggerValue, ModifierOperation.Add, phase.staggerValue);

            if (phase.extraModifiers != null)
            {
                for (int i = 0; i < phase.extraModifiers.Count; i++)
                {
                    var modifier = phase.extraModifiers[i];
                    if (modifier == null || string.IsNullOrWhiteSpace(modifier.statKey))
                    {
                        continue;
                    }

                    AddOrReplaceModifier("boss-phase-extra-" + modifier.statKey, modifier.statKey, modifier.operation, modifier.value);
                }
            }

            NotifyStatsChanged();
        }

        public void AddModifier(RuntimeStatModifier modifier)
        {
            if (modifier == null || string.IsNullOrWhiteSpace(modifier.statKey))
            {
                return;
            }

            _modifiers.Add(modifier);
            NotifyStatsChanged();
        }

        public void AddOrReplaceModifier(string sourceId, string statKey, ModifierOperation operation, float value)
        {
            if (string.IsNullOrWhiteSpace(statKey))
            {
                return;
            }

            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = _modifiers[i];
                if (modifier != null && modifier.sourceId == sourceId && modifier.statKey == statKey)
                {
                    _modifiers.RemoveAt(i);
                }
            }

            AddModifier(new RuntimeStatModifier
            {
                sourceId = sourceId,
                statKey = statKey,
                operation = operation,
                value = value
            });
        }

        public void RemoveModifiersBySource(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            bool removed = false;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = _modifiers[i];
                if (modifier != null && modifier.sourceId == sourceId)
                {
                    _modifiers.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                NotifyStatsChanged();
            }
        }

        public float GetStat(string statKey)
        {
            if (string.IsNullOrWhiteSpace(statKey))
            {
                return 0f;
            }

            _baseStats.TryGetValue(statKey, out float baseValue);
            float add = 0f;
            float mul = 1f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                var modifier = _modifiers[i];
                if (modifier == null || modifier.statKey != statKey)
                {
                    continue;
                }

                if (modifier.operation == ModifierOperation.Add)
                {
                    add += modifier.value;
                }
                else
                {
                    mul *= modifier.value;
                }
            }

            return (baseValue + add) * mul;
        }

        public float GetResistance(DamageType damageType)
        {
            return _resistances.TryGetValue(damageType, out float resistance)
                ? Mathf.Clamp(resistance, -1f, 1f)
                : 0f;
        }

        public void SetCurrentHP(float hp)
        {
            CurrentHP = Mathf.Clamp(hp, 0f, GetStat(StatKeys.MaxHP));
            NotifyStatsChanged();
        }

        public void SetCurrentEnergy(float energy)
        {
            CurrentEnergy = Mathf.Clamp(energy, 0f, GetStat(StatKeys.MaxEnergy));
            NotifyStatsChanged();
        }

        public float TakeDamage(float rawDamage, DamageType damageType = DamageType.Physical)
        {
            float defense = Mathf.Max(0f, GetStat(StatKeys.Defense));
            float reducedByDefense = rawDamage * (100f / (100f + defense));
            float reducedByResistance = reducedByDefense * (1f - GetResistance(damageType));
            float finalDamage = Mathf.Max(0f, reducedByResistance);

            if (finalDamage <= 0f)
            {
                return 0f;
            }

            CurrentHP = Mathf.Max(0f, CurrentHP - finalDamage);
            Damaged?.Invoke(finalDamage);
            NotifyStatsChanged();

            if (CurrentHP <= 0f)
            {
                Died?.Invoke();
            }

            return finalDamage;
        }

        public float Heal(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float finalHeal = amount * Mathf.Max(0f, GetStat(StatKeys.HealMultiplier));
            float before = CurrentHP;
            CurrentHP = Mathf.Clamp(CurrentHP + finalHeal, 0f, GetStat(StatKeys.MaxHP));
            float applied = CurrentHP - before;

            if (applied > 0f)
            {
                Healed?.Invoke(applied);
                NotifyStatsChanged();
            }

            return applied;
        }

        public bool ConsumeEnergy(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (CurrentEnergy < amount)
            {
                return false;
            }

            CurrentEnergy -= amount;
            NotifyStatsChanged();
            return true;
        }

        public float RestoreEnergy(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float before = CurrentEnergy;
            CurrentEnergy = Mathf.Clamp(CurrentEnergy + amount, 0f, GetStat(StatKeys.MaxEnergy));
            float applied = CurrentEnergy - before;
            if (applied > 0f)
            {
                NotifyStatsChanged();
            }

            return applied;
        }

        public void SetBaseStat(string statKey, float value)
        {
            if (string.IsNullOrWhiteSpace(statKey))
            {
                return;
            }

            _baseStats[statKey] = value;
        }

        public void MultiplyBaseStat(string statKey, float multiplier)
        {
            SetBaseStat(statKey, GetBaseStat(statKey) * multiplier);
        }

        public float GetBaseStat(string statKey)
        {
            return _baseStats.TryGetValue(statKey, out float value) ? value : 0f;
        }

        public void SetResistance(DamageType damageType, float resistance)
        {
            _resistances[damageType] = Mathf.Clamp(resistance, -1f, 1f);
        }

        private void Clear()
        {
            _baseStats.Clear();
            _resistances.Clear();
            _modifiers.Clear();
            Level = 1;
            CurrentHP = 0f;
            CurrentEnergy = 0f;
        }

        private void ApplyBaseVital(BaseVitalStats vital)
        {
            if (vital == null)
            {
                return;
            }

            SetBaseStat(StatKeys.MaxHP, vital.maxHP);
            SetBaseStat(StatKeys.MaxEnergy, vital.maxEnergy);
            SetBaseStat(StatKeys.Defense, vital.defense);
            SetBaseStat(StatKeys.HealMultiplier, vital.healMultiplier);

            if (vital.resistances == null)
            {
                return;
            }

            for (int i = 0; i < vital.resistances.Count; i++)
            {
                var entry = vital.resistances[i];
                if (entry == null)
                {
                    continue;
                }

                SetResistance(entry.damageType, entry.resistance);
            }
        }

        private void ApplyCombat(CombatStats combat)
        {
            if (combat == null)
            {
                return;
            }

            SetBaseStat(StatKeys.Attack, combat.attack);
            SetBaseStat(StatKeys.CritRate, combat.critRate);
            SetBaseStat(StatKeys.CritDamage, combat.critDamage);
            SetBaseStat(StatKeys.AttackSpeed, combat.attackSpeed);
            SetBaseStat(StatKeys.KnockbackPower, combat.knockbackPower);
            SetBaseStat(StatKeys.ComboWindow, combat.comboWindow);
        }

        private void ApplyMovement(MovementStats movement)
        {
            if (movement == null)
            {
                return;
            }

            SetBaseStat(StatKeys.MoveSpeed, movement.moveSpeed);
            SetBaseStat(StatKeys.JumpForce, movement.jumpForce);
            SetBaseStat(StatKeys.DashDistance, movement.dashDistance);
            SetBaseStat(StatKeys.DashCooldown, movement.dashCooldown);
            SetBaseStat(StatKeys.AirJumpCount, movement.airJumpCount);
            SetBaseStat(StatKeys.GravityScale, movement.gravityScale);
        }

        private void ApplyFeel(CombatFeelStats feel)
        {
            if (feel == null)
            {
                return;
            }

            SetBaseStat(StatKeys.InvincibleTime, feel.invincibleTime);
            SetBaseStat(StatKeys.HitPause, feel.hitPause);
            SetBaseStat(StatKeys.InputBuffer, feel.inputBuffer);
            SetBaseStat(StatKeys.CoyoteTime, feel.coyoteTime);
            SetBaseStat(StatKeys.TurnSpeed, feel.turnSpeed);
        }

        private void ApplyEnemyAi(EnemyAiStats ai)
        {
            if (ai == null)
            {
                return;
            }

            SetBaseStat(StatKeys.DetectRange, ai.detectRange);
            SetBaseStat(StatKeys.ChaseRange, ai.chaseRange);
            SetBaseStat(StatKeys.AttackRange, ai.attackRange);
            SetBaseStat(StatKeys.PatrolRadius, ai.patrolRadius);
            SetBaseStat(StatKeys.ReactionTime, ai.reactionTime);
            SetBaseStat(StatKeys.AggroDuration, ai.aggroDuration);
        }

        private void ApplyEnemyCombat(EnemyCombatTuning tuning)
        {
            if (tuning == null)
            {
                return;
            }

            SetBaseStat(StatKeys.AttackCooldown, tuning.attackCooldown);
            SetBaseStat(StatKeys.WindupTime, tuning.windupTime);
            SetBaseStat(StatKeys.RecoveryTime, tuning.recoveryTime);
            SetBaseStat(StatKeys.HitStunResist, tuning.hitStunResist);
            SetBaseStat(StatKeys.SuperArmor, tuning.superArmor);
            SetBaseStat(StatKeys.Weight, tuning.weight);
        }

        private float EvaluateCurve(AnimationCurve curve, float x, float fallback)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(x) : fallback;
        }

        private void NotifyStatsChanged()
        {
            CurrentHP = Mathf.Clamp(CurrentHP, 0f, GetStat(StatKeys.MaxHP));
            CurrentEnergy = Mathf.Clamp(CurrentEnergy, 0f, GetStat(StatKeys.MaxEnergy));
            StatsChanged?.Invoke();
        }
    }
}
