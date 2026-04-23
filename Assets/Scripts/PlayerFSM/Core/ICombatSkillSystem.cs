using UnityEngine;

namespace FenShen.PlayerFSM
{
    public interface ICombatSkillSystem
    {
        void ExecuteAttack(FenShen.Combat.CombatSkillDefinitionSO skill);
        void ExecuteAttack(string skillId);
    }
}
