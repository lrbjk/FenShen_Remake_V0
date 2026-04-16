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

            var walk = Ensure<MoveStateSO>(states + "Walk.asset");
            walk.displayName = "Walk";
            walk.animStateName = "Walk";
            walk.movementMultiplier = 1f;

            var run = Ensure<MoveStateSO>(states + "Run.asset");
            run.displayName = "Run";
            run.animStateName = "Run";
            run.movementMultiplier = 3f;

            var dodge = Ensure<DodgeStateSO>(states + "Dodge.asset");
            dodge.displayName = "Dodge";
            dodge.animStateName = "Dodge";
            dodge.dodgeDistance = 2.2f;
            dodge.displacementCurveParameter = "DodgeDisplacement";

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

            var sprintHeld = Ensure<InputPressedConditionSO>(conds + "SprintHeld.asset");
            sprintHeld.binding = InputBindingType.Sprint;
            sprintHeld.requirePressedThisFrame = false;
            sprintHeld.invertResult = false;

            var sprintReleased = Ensure<InputPressedConditionSO>(conds + "SprintReleased.asset");
            sprintReleased.binding = InputBindingType.Sprint;
            sprintReleased.requirePressedThisFrame = false;
            sprintReleased.invertResult = true;

            var dodgeTap = Ensure<InputTapReleaseConditionSO>(conds + "DodgeTap.asset");
            dodgeTap.binding = InputBindingType.Sprint;
            dodgeTap.maxHoldDuration = 0.2f;

            var dodgeEnd = Ensure<AnimNormalizedTimeConditionSO>(conds + "DodgeEnd.asset");
            dodgeEnd.threshold = 0.95f;
            dodgeEnd.greaterOrEqual = true;

            var attackEnd = Ensure<AnimNormalizedTimeConditionSO>(conds + "AttackEnd.asset");
            attackEnd.threshold = 0.9f;
            attackEnd.greaterOrEqual = true;

            var tIdleDodge = Ensure<TransitionLinkSO>(trans + "IdleToDodge.asset");
            var tIdleRun = Ensure<TransitionLinkSO>(trans + "IdleToRun.asset");
            var tIdleWalk = Ensure<TransitionLinkSO>(trans + "IdleToWalk.asset");
            var tWalkDodge = Ensure<TransitionLinkSO>(trans + "WalkToDodge.asset");
            var tWalkIdle = Ensure<TransitionLinkSO>(trans + "WalkToIdle.asset");
            var tWalkRun = Ensure<TransitionLinkSO>(trans + "WalkToRun.asset");
            var tRunDodge = Ensure<TransitionLinkSO>(trans + "RunToDodge.asset");
            var tRunWalk = Ensure<TransitionLinkSO>(trans + "RunToWalk.asset");
            var tRunIdle = Ensure<TransitionLinkSO>(trans + "RunToIdle.asset");
            var tDodgeIdle = Ensure<TransitionLinkSO>(trans + "DodgeToIdle.asset");
            var tIdleAtk = Ensure<TransitionLinkSO>(trans + "IdleToAttack.asset");
            var tWalkAtk = Ensure<TransitionLinkSO>(trans + "WalkToAttack.asset");
            var tRunAtk = Ensure<TransitionLinkSO>(trans + "RunToAttack.asset");
            var tAtkIdle = Ensure<TransitionLinkSO>(trans + "AttackToIdle.asset");

            tIdleDodge.from = idle;
            tIdleDodge.to = dodge;
            tIdleDodge.conditions = new List<ConditionSO> { dodgeTap };

            tIdleRun.from = idle;
            tIdleRun.to = run;
            tIdleRun.conditions = new List<ConditionSO> { moveOn, sprintHeld };

            tIdleWalk.from = idle;
            tIdleWalk.to = walk;
            tIdleWalk.conditions = new List<ConditionSO> { moveOn };

            tWalkDodge.from = walk;
            tWalkDodge.to = dodge;
            tWalkDodge.conditions = new List<ConditionSO> { dodgeTap };

            tWalkIdle.from = walk;
            tWalkIdle.to = idle;
            tWalkIdle.conditions = new List<ConditionSO> { moveBelow };

            tWalkRun.from = walk;
            tWalkRun.to = run;
            tWalkRun.conditions = new List<ConditionSO> { sprintHeld };

            tRunWalk.from = run;
            tRunWalk.to = walk;
            tRunWalk.conditions = new List<ConditionSO> { sprintReleased };

            tRunDodge.from = run;
            tRunDodge.to = dodge;
            tRunDodge.conditions = new List<ConditionSO> { dodgeTap };

            tRunIdle.from = run;
            tRunIdle.to = idle;
            tRunIdle.conditions = new List<ConditionSO> { moveBelow };

            tDodgeIdle.from = dodge;
            tDodgeIdle.to = idle;
            tDodgeIdle.conditions = new List<ConditionSO> { dodgeEnd };

            tIdleAtk.from = idle;
            tIdleAtk.to = attack;
            tIdleAtk.conditions = new List<ConditionSO> { attackPress };

            tWalkAtk.from = walk;
            tWalkAtk.to = attack;
            tWalkAtk.conditions = new List<ConditionSO> { attackPress };

            tRunAtk.from = run;
            tRunAtk.to = attack;
            tRunAtk.conditions = new List<ConditionSO> { attackPress };

            tAtkIdle.from = attack;
            tAtkIdle.to = idle;
            tAtkIdle.conditions = new List<ConditionSO> { attackEnd };

            var graph = Ensure<FsmGraphSO>(root + "PlayerFsmGraph.asset");
            graph.initialState = idle;
            graph.states = new List<StateSO> { idle, walk, run, dodge, attack };
            graph.transitions = new List<TransitionLinkSO> { tIdleDodge, tIdleRun, tIdleWalk, tWalkDodge, tWalkRun, tWalkIdle, tRunDodge, tRunWalk, tRunIdle, tDodgeIdle, tIdleAtk, tWalkAtk, tRunAtk, tAtkIdle };

            EditorUtility.SetDirty(idle);
            EditorUtility.SetDirty(walk);
            EditorUtility.SetDirty(run);
            EditorUtility.SetDirty(dodge);
            EditorUtility.SetDirty(attack);
            EditorUtility.SetDirty(moveOn);
            EditorUtility.SetDirty(moveBelow);
            EditorUtility.SetDirty(attackPress);
            EditorUtility.SetDirty(sprintHeld);
            EditorUtility.SetDirty(sprintReleased);
            EditorUtility.SetDirty(dodgeTap);
            EditorUtility.SetDirty(dodgeEnd);
            EditorUtility.SetDirty(attackEnd);
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
