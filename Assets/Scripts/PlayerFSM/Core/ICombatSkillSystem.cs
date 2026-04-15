using UnityEngine;

namespace FenShen.PlayerFSM
{
    public interface ICombatSkillSystem
    {
        void ExecuteAttack(string skillId);
    }
}
