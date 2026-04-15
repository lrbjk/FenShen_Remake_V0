using UnityEngine;
namespace FenShen.PlayerFSM
{
    public class SimpleCombatSkillSystem : MonoBehaviour, ICombatSkillSystem
    {
        public void ExecuteAttack(string skillId)
        {
            Debug.Log("[Combat] ExecuteAttack: " + skillId);
        }
    }
}