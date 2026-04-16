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
                var walk = AssetDatabase.LoadAssetAtPath<StateSO>(root + "States/Walk.asset");
                var run = AssetDatabase.LoadAssetAtPath<StateSO>(root + "States/Run.asset");
                var dodge = AssetDatabase.LoadAssetAtPath<StateSO>(root + "States/Dodge.asset");
                var attack = AssetDatabase.LoadAssetAtPath<StateSO>(root + "States/Attack.asset");
                var tIdleDodge = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/IdleToDodge.asset");
                var tIdleRun = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/IdleToRun.asset");
                var tIdleWalk = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/IdleToWalk.asset");
                var tWalkDodge = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/WalkToDodge.asset");
                var tWalkIdle = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/WalkToIdle.asset");
                var tWalkRun = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/WalkToRun.asset");
                var tRunDodge = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/RunToDodge.asset");
                var tRunWalk = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/RunToWalk.asset");
                var tRunIdle = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/RunToIdle.asset");
                var tDodgeIdle = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/DodgeToIdle.asset");
                var tIdleAtk = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/IdleToAttack.asset");
                var tWalkAtk = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/WalkToAttack.asset");
                var tRunAtk = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/RunToAttack.asset");
                var tAtkIdle = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(root + "Transitions/AttackToIdle.asset");

                var condMoveOn = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/MoveOn.asset");
                if (condMoveOn == null) condMoveOn = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/InputPressed.asset");

                var condMoveBelow = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/MoveBelow.asset");
                if (condMoveBelow == null) condMoveBelow = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/MoveBelowThreshold.asset");
                var condSprintHeld = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/SprintHeld.asset");
                var condSprintReleased = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/SprintReleased.asset");
                var condDodgeTap = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/DodgeTap.asset");
                var condDodgeEnd = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/DodgeEnd.asset");
                var condAtkPress = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/AttackPress.asset");
                var condAtkEnd = AssetDatabase.LoadAssetAtPath<ConditionSO>(root + "Conditions/AttackEnd.asset");

                var graph = AssetDatabase.LoadAssetAtPath<FsmGraphSO>(root + "PlayerFsmGraph.asset");

                if (idle == null || walk == null || run == null || dodge == null || attack == null || tIdleDodge == null || tIdleRun == null || tIdleWalk == null || tWalkDodge == null || tWalkIdle == null || tWalkRun == null || tRunDodge == null || tRunWalk == null || tRunIdle == null || tDodgeIdle == null || tIdleAtk == null || tWalkAtk == null || tRunAtk == null || tAtkIdle == null || graph == null)
                {
                    EditorUtility.DisplayDialog("Player FSM", "Some assets are missing. Ensure States, Conditions, Transitions, and Graph exist.", "OK");
                    return;
                }

                tIdleDodge.from = idle;
                tIdleDodge.to = dodge;
                tIdleDodge.conditions = new List<ConditionSO>();
                if (condDodgeTap != null) tIdleDodge.conditions.Add(condDodgeTap);

                tIdleRun.from = idle;
                tIdleRun.to = run;
                tIdleRun.conditions = new List<ConditionSO>();
                if (condMoveOn != null) tIdleRun.conditions.Add(condMoveOn);
                if (condSprintHeld != null) tIdleRun.conditions.Add(condSprintHeld);

                tIdleWalk.from = idle;
                tIdleWalk.to = walk;
                tIdleWalk.conditions = new List<ConditionSO>();
                if (condMoveOn != null) tIdleWalk.conditions.Add(condMoveOn);

                tWalkDodge.from = walk;
                tWalkDodge.to = dodge;
                tWalkDodge.conditions = new List<ConditionSO>();
                if (condDodgeTap != null) tWalkDodge.conditions.Add(condDodgeTap);

                tWalkIdle.from = walk;
                tWalkIdle.to = idle;
                tWalkIdle.conditions = new List<ConditionSO>();
                if (condMoveBelow != null) tWalkIdle.conditions.Add(condMoveBelow);

                tWalkRun.from = walk;
                tWalkRun.to = run;
                tWalkRun.conditions = new List<ConditionSO>();
                if (condSprintHeld != null) tWalkRun.conditions.Add(condSprintHeld);

                tRunDodge.from = run;
                tRunDodge.to = dodge;
                tRunDodge.conditions = new List<ConditionSO>();
                if (condDodgeTap != null) tRunDodge.conditions.Add(condDodgeTap);

                tRunWalk.from = run;
                tRunWalk.to = walk;
                tRunWalk.conditions = new List<ConditionSO>();
                if (condSprintReleased != null) tRunWalk.conditions.Add(condSprintReleased);

                tRunIdle.from = run;
                tRunIdle.to = idle;
                tRunIdle.conditions = new List<ConditionSO>();
                if (condMoveBelow != null) tRunIdle.conditions.Add(condMoveBelow);

                tDodgeIdle.from = dodge;
                tDodgeIdle.to = idle;
                tDodgeIdle.conditions = new List<ConditionSO>();
                if (condDodgeEnd != null) tDodgeIdle.conditions.Add(condDodgeEnd);

                tIdleAtk.from = idle;
                tIdleAtk.to = attack;
                tIdleAtk.conditions = new List<ConditionSO>();
                if (condAtkPress != null) tIdleAtk.conditions.Add(condAtkPress);

                tWalkAtk.from = walk;
                tWalkAtk.to = attack;
                tWalkAtk.conditions = new List<ConditionSO>();
                if (condAtkPress != null) tWalkAtk.conditions.Add(condAtkPress);

                tRunAtk.from = run;
                tRunAtk.to = attack;
                tRunAtk.conditions = new List<ConditionSO>();
                if (condAtkPress != null) tRunAtk.conditions.Add(condAtkPress);

                tAtkIdle.from = attack;
                tAtkIdle.to = idle;
                tAtkIdle.conditions = new List<ConditionSO>();
                if (condAtkEnd != null) tAtkIdle.conditions.Add(condAtkEnd);

                graph.initialState = idle;
                graph.states = new List<StateSO> { idle, walk, run, dodge, attack };
                graph.transitions = new List<TransitionLinkSO> { tIdleDodge, tIdleRun, tIdleWalk, tWalkDodge, tWalkRun, tWalkIdle, tRunDodge, tRunWalk, tRunIdle, tDodgeIdle, tIdleAtk, tWalkAtk, tRunAtk, tAtkIdle };

                EditorUtility.SetDirty(tIdleDodge);
                EditorUtility.SetDirty(tIdleRun);
                EditorUtility.SetDirty(tIdleWalk);
                EditorUtility.SetDirty(tWalkDodge);
                EditorUtility.SetDirty(tWalkIdle);
                EditorUtility.SetDirty(tWalkRun);
                EditorUtility.SetDirty(tRunDodge);
                EditorUtility.SetDirty(tRunWalk);
                EditorUtility.SetDirty(tRunIdle);
                EditorUtility.SetDirty(tDodgeIdle);
                EditorUtility.SetDirty(tIdleAtk);
                EditorUtility.SetDirty(tWalkAtk);
                EditorUtility.SetDirty(tRunAtk);
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
