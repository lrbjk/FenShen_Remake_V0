namespace FenShen.GameData
{
    public class PeriodicBuff : BuffBase
    {
        public override void OnTick()
        {
            var stats = GetRuntimeStats();
            if (stats == null || instance == null || instance.data == null)
            {
                return;
            }

            int stackCount = GetCurrentStack();
            if (instance.data.periodicDamage > 0f)
            {
                stats.TakeDamage(instance.data.periodicDamage * stackCount, instance.data.periodicDamageType);
            }

            if (instance.data.periodicHeal > 0f)
            {
                stats.Heal(instance.data.periodicHeal * stackCount);
            }
        }
    }
}
