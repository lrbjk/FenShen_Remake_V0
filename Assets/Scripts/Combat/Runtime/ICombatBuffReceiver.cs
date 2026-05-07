using FenShen.GameData;
using UnityEngine;

namespace FenShen.Combat
{
    public interface ICombatBuffReceiver
    {
        BuffBase AddBuff(BuffSO buff, GameObject source = null);
        BuffBase AddBuff(int buffId, GameObject source = null);
        bool RemoveBuff(BuffSO buff);
        bool RemoveBuff(int buffId);
        bool HasBuff(int buffId);
        bool HasControlFlag(BuffControlFlag flag);
    }
}
