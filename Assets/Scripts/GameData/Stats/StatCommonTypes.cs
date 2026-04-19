using System;
using System.Collections.Generic;
using UnityEngine;

namespace FenShen.GameData
{
    public enum DamageType
    {
        Physical = 0,
        Fire = 1,
        Ice = 2,
        Lightning = 3,
        Poison = 4,
        Dark = 5,
        Holy = 6
    }

    public enum EnemyArchetype
    {
        Normal = 0,
        Elite = 1,
        Boss = 2
    }

    public enum BuffCategory
    {
        Buff = 0,
        Debuff = 1
    }

    public enum BuffStackRule
    {
        Refresh = 0,
        Stack = 1,
        Replace = 2,
        Ignore = 3
    }

    [Flags]
    public enum BuffControlFlag
    {
        None = 0,
        DisableMove = 1 << 0,
        DisableJump = 1 << 1,
        DisableAttack = 1 << 2,
        DisableSkill = 1 << 3,
        DisableInput = 1 << 4,
        Invulnerable = 1 << 5,
        Silence = 1 << 6,
        Root = 1 << 7,
        Stun = 1 << 8,
    }

    public enum ModifierOperation
    {
        Add = 0,
        Multiply = 1
    }

    public enum WeaponAttackType
    {
        Melee = 0,
        Ranged = 1,
        Magic = 2
    }

    [Serializable]
    public class ResistanceEntry
    {
        public DamageType damageType = DamageType.Physical;
        [Range(-1f, 1f)]
        public float resistance;
    }

    [Serializable]
    public class BaseVitalStats
    {
        [Min(1f)] public float maxHP = 100f;
        [Min(0f)] public float maxEnergy = 0f;
        [Min(0f)] public float defense = 0f;
        [Min(0f)] public float healMultiplier = 1f;
        public List<ResistanceEntry> resistances = new List<ResistanceEntry>();
    }

    [Serializable]
    public class CombatStats
    {
        [Min(0f)] public float attack = 10f;
        [Range(0f, 1f)] public float critRate = 0.05f;
        [Min(1f)] public float critDamage = 1.5f;
        [Min(0.1f)] public float attackSpeed = 1f;
        [Min(0f)] public float knockbackPower = 0f;
        [Min(0f)] public float comboWindow = 0.15f;
    }

    [Serializable]
    public class MovementStats
    {
        [Min(0f)] public float moveSpeed = 5f;
        [Min(0f)] public float jumpForce = 8f;
        [Min(0f)] public float dashDistance = 3f;
        [Min(0f)] public float dashCooldown = 0.5f;
        [Min(0)] public int airJumpCount = 0;
        [Min(0f)] public float gravityScale = 1f;
    }

    [Serializable]
    public class CombatFeelStats
    {
        [Min(0f)] public float invincibleTime = 0.2f;
        [Min(0f)] public float hitPause = 0.05f;
        [Min(0f)] public float inputBuffer = 0.1f;
        [Min(0f)] public float coyoteTime = 0.1f;
        [Min(0f)] public float turnSpeed = 720f;
    }

    [Serializable]
    public class GrowthStats
    {
        [Min(1)] public int level = 1;
        [Min(0)] public int expToNextLevel = 100;
        [Min(0)] public int skillPointReward = 1;
        [Min(0f)] public float luck = 0f;
        [Min(0f)] public float discovery = 1f;
    }

    [Serializable]
    public class EnemyAiStats
    {
        [Min(0f)] public float detectRange = 6f;
        [Min(0f)] public float chaseRange = 10f;
        [Min(0f)] public float attackRange = 1.5f;
        [Min(0f)] public float patrolRadius = 4f;
        [Min(0f)] public float reactionTime = 0.2f;
        [Min(0f)] public float aggroDuration = 2f;
    }

    [Serializable]
    public class EnemyCombatTuning
    {
        [Min(0f)] public float attackCooldown = 1f;
        [Min(0f)] public float windupTime = 0.15f;
        [Min(0f)] public float recoveryTime = 0.2f;
        [Min(0f)] public float hitStunResist = 0f;
        [Min(0f)] public float superArmor = 0f;
        [Min(0f)] public float weight = 1f;
    }

    [Serializable]
    public class RewardStats
    {
        [Min(0)] public int expDrop = 10;
        [Min(0)] public int goldDrop = 0;
        [Min(0f)] public float respawnTime = 0f;
    }

    [Serializable]
    public class StatModifierEntry
    {
        public string statKey;
        public ModifierOperation operation = ModifierOperation.Add;
        public float value = 0f;
    }

    [Serializable]
    public class LootEntry
    {
        public string itemId;
        [Min(1)] public int minAmount = 1;
        [Min(1)] public int maxAmount = 1;
        [Min(0f)] public float dropWeight = 1f;
    }
}
