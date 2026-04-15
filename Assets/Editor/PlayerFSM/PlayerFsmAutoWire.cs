using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FenShen.PlayerFSM
{
    public static class PlayerFsmAutoWire
    {
        [MenuItem("Tools / Player FSM / Auto Wire & Bind(Fallback)")]
        public static void AutoWire()
        {
            try
            {
                string root = "Assets/Settings/PlayerFSM/";
                var idle = AssetDatabase.LoadAssetAtPath<StateSO>(root + "States/Idle.asset");
                var move = AssetDatabase.LoadAssetAtPath<StateSO>(root + "States/Move.asset");
                var attack = AssetDatabase.LoadAssetAtPath<StateSO>(root + "States/Attack.asset");
                var tIdleMove = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/IdleToMove.asset");
                var tMoveIdle = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/MoveToIdle.asset");
                var tIdleAtk = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/IdleToAttack.asset");
                var tMoveAtk = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/MoveToAttack.asset");
                var tAtkIdle = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/AttackToIdle.asset");

                var condMoveOn = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/MoveOn.asset");
                var condMoveBelow = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/MoveBelow.asset");
                var condAtkPress = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/AttackPress.asset");
                var condAtkEnd = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/AttackEnd.asset");

                var graph = AssetDatabase.LoadAssetAtPath<FsmGraphSO>(root + "PlayerFsmGraph.asset");

                if (idle == null || move == null || attack == null || tIdleMove == null || tMoveIdle == null || tIdleAtk == null || tMoveAtk == null || tAtkIdle == null || graph == null)
                {
                    EditorUtility.DisplayDialog("Player FSM", "Some assets are missing. Ensure States, Conditions, Transitions, and Graph exist.", "OK");
                    return;
                }

                tIdleMove.from = idle;
                tIdleMove.to = move;
                tIdleMove.conditions = new List<ConditionSO>();
                if (condMoveOn != null) tIdleMove.conditions.Add(condMoveOn);

                tMoveIdle.from = move;
                tMoveIdle.to = idle;
                tMoveIdle.conditions = new List<ConditionSO>();
                if (condMoveBelow != null) tMoveIdle.conditions.Add(condMoveBelow);

                tIdleAtk.from = idle;
                tIdleAtk.to = attack;
                tIdleAtk.conditions = new List<ConditionSO>();
                if (condAtkPress != null) tIdleAtk.conditions.Add(condAtkPress);

                tMoveAtk.from = move;
                tMoveAtk.to = attack;
                tMoveAtk.conditions = new List<ConditionSO>();
                if (condAtkPress != null) tMoveAtk.conditions.Add(condAtkPress);

                tAtkIdle.from = attack;
                tAtkIdle.to = idle;
                tAtkIdle.conditions = new List<ConditionSO>();
                if (condAtkEnd != null) tAtkIdle.conditions.Add(condAtkEnd);

                graph.initialState = idle;
                graph.states = new List<StateSO> { idle, move, attack };
                graph.transitions = new List<TransitionLinkSO> { tIdleMove, tMoveIdle, tIdleAtk, tMoveAtk, tAtkIdle };

                EditorUtility.SetDirty(tIdleMove);
                EditorUtility.SetDirty(tMoveIdle);
                EditorUtility.SetDirty(tIdleAtk);
                EditorUtility.SetDirty(tMoveAtk);
                EditorUtility.SetDirty(tAtkIdle);
                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();

                var go = GameObject.Find("Player");
                if (go == null)
                {
                    EditorUtility.DisplayDialog("Player FSM", "GameObject 'Player' not found in scene.", "OK");
                    return;
                }

                var fsm = go.GetComponent<PlayerFsm>();
                if (fsm == null)
                {
                    fsm = go.AddComponent<PlayerFsm>();
                }

                fsm.graph = graph;
                fsm.Animator = go.GetComponent<Animator>();
                fsm.Character = go.transform;

                var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/Input/Player.inputactions");
                fsm.inputActions = inputActions;
                fsm.moveActionName = "Player/Move";
                fsm.attackActionName = "Player/Attack";
                fsm.sprintActionName = "Player/Sprint";

                var combat = go.GetComponent<SimpleCombatSkillSystem>();
                if (combat == null)
                {
                    combat = go.AddComponent<SimpleCombatSkillSystem>();
                }

                fsm.CombatSystemBehaviour = combat;
                EditorUtility.SetDirty(fsm);
                EditorUtility.DisplayDialog("Player FSM", "Auto wiring complete.", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Player FSM", "AutoWire failed:\n" + ex.Message, "OK");
            }
        }
    }
}
