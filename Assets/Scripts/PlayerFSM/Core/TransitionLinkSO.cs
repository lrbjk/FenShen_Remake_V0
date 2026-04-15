using System.Collections.Generic;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    public enum ConditionLogic { All, Any }

    [CreateAssetMenu(fileName = "TransitionLink", menuName = "PlayerFSM/TransitionLink")]
    public class TransitionLinkSO : ScriptableObject
    {
        public StateSO from;
        public StateSO to;
        public ConditionLogic logic = ConditionLogic.All;
        public List<ConditionSO> conditions = new List<ConditionSO>();

        public string DisplayName
        {
            get
            {
                string fromName = from != null ? from.DisplayName : "None";
                string toName = to != null ? to.DisplayName : "None";
                return fromName + " -> " + toName;
            }
        }

        public string ConditionSummary
        {
            get
            {
                if (conditions == null || conditions.Count == 0)
                {
                    return "Always";
                }

                string separator = logic == ConditionLogic.All ? " && " : " || ";
                List<string> parts = new List<string>();
                for (int i = 0; i < conditions.Count; i++)
                {
                    var condition = conditions[i];
                    if (condition == null)
                    {
                        continue;
                    }

                    parts.Add(condition.name);
                }

                return parts.Count == 0 ? "Always" : string.Join(separator, parts);
            }
        }

        public bool Evaluate(PlayerFsm fsm, float dt)
        {
            for (int i = 0; i < conditions.Count; i++) { var c = conditions[i]; if (c != null) c.Tick(fsm, dt); }
            bool any = false;
            for (int i = 0; i < conditions.Count; i++)
            {
                var c = conditions[i];
                if (c == null) continue;
                bool v = c.Evaluate(fsm);
                any = any || v;
                if (logic == ConditionLogic.All && !v) return false;
            }
            return logic == ConditionLogic.All ? true : any;
        }
        public void OnEnter(PlayerFsm fsm) { for (int i = 0; i < conditions.Count; i++) { var c = conditions[i]; if (c != null) c.OnEnter(fsm); } }
        public void OnExit(PlayerFsm fsm) { for (int i = 0; i < conditions.Count; i++) { var c = conditions[i]; if (c != null) c.OnExit(fsm); } }
    }
}
