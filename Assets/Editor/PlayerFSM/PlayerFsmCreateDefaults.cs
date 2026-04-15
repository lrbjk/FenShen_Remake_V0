using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FenShen.PlayerFSM
{
    public static class PlayerFsmCreateDefaults
    {
        [MenuItem("Tools / Player FSM / Create Defaults + Auto Wire")]
        public static void CreateDefaults()
        {
            string root = "Assets/Settings/PlayerFSM/";
            string states = root + "States/";
            string conds = root + "Conditions/";
            string trans = root + "Transitions/";
            System.IO.Directory.CreateDirectory("Assets/Settings/PlayerFSM");
            System.IO.Directory.CreateDirectory(states);
            System.IO.Directory.CreateDirectory(conds);
            System.IO.Directory.CreateDirectory(trans);

            T Ensure<T>(string path) where T : ScriptableObject
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<T>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                return asset;
            }

            var idle = Ensure<IdleStateSO>(states + "Idle.asset");
            idle.displayName = "Idle";
            idle.animStateName = "Idle";

            var move = Ensure<MoveStateSO>(states + "Move.asset");
            move.displayName = "Move";
            move.useSeparateAnimations = true;
            move.walkAnim = "Walk";
            move.runAnim = "Run";
            move.animStateName = move.walkAnim;

            var attack = Ensure<AttackStateSO>(states + "Attack.asset");
            attack.displayName = "Attack";
            attack.animStateName = "Attack";
            attack.triggerAtNormalizedTime = 0.2f;

            var moveOn = Ensure<InputPressedConditionSO>(conds + "MoveOn.asset");
            moveOn.binding = InputBindingType.Move;
            moveOn.moveThreshold = 0.1f;

            var moveBelow = Ensure<MoveBelowThresholdConditionSO>(conds + "MoveBelow.asset");
            moveBelow.moveThreshold = 0.1f;

            var attackPress = Ensure<InputPressedConditionSO>(conds + "AttackPress.asset");
            attackPress.binding = InputBindingType.Attack;

            var attackEnd = Ensure<AnimNormalizedTimeConditionSO>(conds + "AttackEnd.asset");
            attackEnd.threshold = 0.9f;
            attackEnd.greaterOrEqual = true;

            var tIdleMove = Ensure<TransitionLinkSO>(trans + "IdleToMove.asset");
            var tMoveIdle = Ensure<TransitionLinkSO>(trans + "MoveToIdle.asset");
            var tIdleAtk = Ensure<TransitionLinkSO>(trans + "IdleToAttack.asset");
            var tMoveAtk = Ensure<TransitionLinkSO>(trans + "MoveToAttack.asset");
            var tAtkIdle = Ensure<TransitionLinkSO>(trans + "AttackToIdle.asset");

            tIdleMove.from = idle;
            tIdleMove.to = move;
            tIdleMove.conditions = new List<ConditionSO> { moveOn };

            tMoveIdle.from = move;
            tMoveIdle.to = idle;
            tMoveIdle.conditions = new List<ConditionSO> { moveBelow };

            tIdleAtk.from = idle;
            tIdleAtk.to = attack;
            tIdleAtk.conditions = new List<ConditionSO> { attackPress };

            tMoveAtk.from = move;
            tMoveAtk.to = attack;
            tMoveAtk.conditions = new List<ConditionSO> { attackPress };

            tAtkIdle.from = attack;
            tAtkIdle.to = idle;
            tAtkIdle.conditions = new List<ConditionSO> { attackEnd };

            var graph = Ensure<FsmGraphSO>(root + "PlayerFsmGraph.asset");
            graph.initialState = idle;
            graph.states = new List<StateSO> { idle, move, attack };
            graph.transitions = new List<TransitionLinkSO> { tIdleMove, tMoveIdle, tIdleAtk, tMoveAtk, tAtkIdle };

            EditorUtility.SetDirty(idle);
            EditorUtility.SetDirty(move);
            EditorUtility.SetDirty(attack);
            EditorUtility.SetDirty(moveOn);
            EditorUtility.SetDirty(moveBelow);
            EditorUtility.SetDirty(attackPress);
            EditorUtility.SetDirty(attackEnd);
            EditorUtility.SetDirty(tIdleMove);
            EditorUtility.SetDirty(tMoveIdle);
            EditorUtility.SetDirty(tIdleAtk);
            EditorUtility.SetDirty(tMoveAtk);
            EditorUtility.SetDirty(tAtkIdle);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();

            var go = GameObject.Find("Player");
            if (go != null)
            {
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
            }

            EditorUtility.DisplayDialog("Player FSM", "Defaults created and wired. Press Play to test.", "OK");
        }
    }
}
