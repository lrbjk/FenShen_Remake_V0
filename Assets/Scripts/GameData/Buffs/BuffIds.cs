namespace FenShen.GameData
{
    public static class BuffIds
    {
        public const int None = 0;

        // 1000-1999: Damage over time / sustain
        public const int Poison = 1001;
        public const int Burn = 1002;
        public const int Bleed = 1003;
        public const int Regeneration = 1101;
        public const int Lifesteal = 1102;

        // 2000-2999: Stat buffs
        public const int AttackUp = 2001;
        public const int DefenseUp = 2002;
        public const int MoveSpeedUp = 2003;
        public const int AttackSpeedUp = 2004;
        public const int CritRateUp = 2005;
        public const int CritDamageUp = 2006;
        public const int HealPowerUp = 2007;
        public const int Shield = 2101;
        public const int Rage = 2102;

        // 3000-3999: Debuffs
        public const int AttackDown = 3001;
        public const int DefenseDown = 3002;
        public const int Slow = 3003;
        public const int ArmorBreak = 3004;
        public const int HealingReduction = 3005;
        public const int Curse = 3006;

        // 4000-4999: Control
        public const int Stun = 4001;
        public const int Freeze = 4002;
        public const int Root = 4003;
        public const int Silence = 4004;
        public const int Knockdown = 4005;

        // 5000-5999: Boss / special mechanics
        public const int BossEnrage = 5001;
        public const int PhaseShield = 5002;
        public const int ReflectDamage = 5003;
        public const int Berserk = 5004;
    }
}
