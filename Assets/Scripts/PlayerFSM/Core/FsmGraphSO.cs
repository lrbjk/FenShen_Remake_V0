using System.Collections.Generic;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CreateAssetMenu(fileName = "FsmGraph", menuName = "PlayerFSM / FSM Graph")]
    public class FsmGraphSO : ScriptableObject
    {
        public StateSO initialState;
        public List<StateSO> states = new List<StateSO>();
        public List<TransitionLinkSO> transitions = new List<TransitionLinkSO>();
        public IEnumerable<TransitionLinkSO> GetOutgoing(StateSO s)
        {
            for (int i = 0; i < transitions.Count; i++) { var t = transitions[i]; if (t != null && t.from == s) yield return t; }
        }

        public bool ContainsState(StateSO state)
        {
            return state != null && states.Contains(state);
        }
    }
}
