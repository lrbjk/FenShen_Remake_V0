using FenShen.EnemyAI.BehaviorTree;
using UnityEditor;
using UnityEngine;

namespace FenShen.EnemyAI.BehaviorTree.Editor
{
    public static class BehaviorTreeCreateDefaults
    {
        [MenuItem("Tools/Enemy AI/Create Default Behavior Tree")]
        public static void CreateDefaultTree()
        {
            const string root = "Assets/Settings/EnemyAI";
            const string nodes = root + "/Nodes";
            EnsureFolder(root);
            EnsureFolder(nodes);

            string graphPath = AssetDatabase.GenerateUniqueAssetPath(root + "/EnemyBehaviorTree.asset");
            BehaviorTreeGraphSO graph = ScriptableObject.CreateInstance<BehaviorTreeGraphSO>();
            AssetDatabase.CreateAsset(graph, graphPath);

            SelectorNodeSO rootSelector = CreateNode<SelectorNodeSO>(nodes, "Root Selector", new Vector2(360f, 80f));
            SequenceNodeSO attackSequence = CreateNode<SequenceNodeSO>(nodes, "Attack Sequence", new Vector2(120f, 260f));
            TargetInRangeNodeSO targetInAttackRange = CreateNode<TargetInRangeNodeSO>(nodes, "Target In Attack Range", new Vector2(20f, 440f));
            targetInAttackRange.range = 1.4f;
            AttackReadyNodeSO attackReady = CreateNode<AttackReadyNodeSO>(nodes, "Attack Ready", new Vector2(220f, 440f));
            AttackTargetNodeSO attack = CreateNode<AttackTargetNodeSO>(nodes, "Attack Target", new Vector2(420f, 440f));

            SequenceNodeSO chaseSequence = CreateNode<SequenceNodeSO>(nodes, "Chase Sequence", new Vector2(560f, 260f));
            HasTargetNodeSO hasTarget = CreateNode<HasTargetNodeSO>(nodes, "Has Target", new Vector2(560f, 440f));
            MoveToTargetNodeSO moveToTarget = CreateNode<MoveToTargetNodeSO>(nodes, "Move To Target", new Vector2(760f, 440f));
            moveToTarget.stoppingDistance = 1.4f;

            PatrolNodeSO patrol = CreateNode<PatrolNodeSO>(nodes, "Patrol", new Vector2(980f, 260f));

            graph.rootNode = rootSelector;
            Add(graph, rootSelector);
            Add(graph, attackSequence);
            Add(graph, targetInAttackRange);
            Add(graph, attackReady);
            Add(graph, attack);
            Add(graph, chaseSequence);
            Add(graph, hasTarget);
            Add(graph, moveToTarget);
            Add(graph, patrol);

            rootSelector.AddChild(attackSequence);
            rootSelector.AddChild(chaseSequence);
            rootSelector.AddChild(patrol);
            attackSequence.AddChild(targetInAttackRange);
            attackSequence.AddChild(attackReady);
            attackSequence.AddChild(attack);
            chaseSequence.AddChild(hasTarget);
            chaseSequence.AddChild(moveToTarget);

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            Selection.activeObject = graph;
            BehaviorTreeGraphEditor.ShowWindow();
            EditorUtility.DisplayDialog("Enemy Behavior Tree", "Default behavior tree created.", "OK");
        }

        private static T CreateNode<T>(string folder, string displayName, Vector2 position) where T : BehaviorTreeNodeSO
        {
            T node = ScriptableObject.CreateInstance<T>();
            node.displayName = displayName;
            node.editorPosition = position;
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + displayName + ".asset");
            AssetDatabase.CreateAsset(node, path);
            return node;
        }

        private static void Add(BehaviorTreeGraphSO graph, BehaviorTreeNodeSO node)
        {
            graph.nodes.Add(node);
            EditorUtility.SetDirty(node);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
