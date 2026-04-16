using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FenShen.PlayerFSM
{
    public class FsmGraphViewEditor : EditorWindow
    {
        private FsmGraphView _graphView;
        private ObjectField _graphField;
        private IMGUIContainer _inspector;
        private FsmGraphSO _currentGraph;
        private UnityEditor.Editor _cachedEditor;
        private UnityEngine.Object _inspectedObject;

        [MenuItem("Tools/Player FSM/Graph Editor")]
        public static void ShowWindow()
        {
            GetWindow<FsmGraphViewEditor>("Player FSM Graph");
        }

        private void OnEnable()
        {
            CreateLayout();
        }

        private void OnDisable()
        {
            DestroyImmediate(_cachedEditor);
        }

        private void CreateLayout()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.flexGrow = 1f;

            var toolbar = new Toolbar();
            _graphField = new ObjectField("Graph")
            {
                objectType = typeof(FsmGraphSO),
                allowSceneObjects = false,
                value = _currentGraph
            };
            _graphField.RegisterValueChangedCallback(evt => LoadGraph(evt.newValue as FsmGraphSO));
            toolbar.Add(_graphField);

            var frameButton = new ToolbarButton(() => _graphView?.FrameAll())
            {
                text = "Frame All"
            };
            toolbar.Add(frameButton);

            var createStateButton = new ToolbarButton(() =>
            {
                if (_currentGraph == null)
                {
                    EditorUtility.DisplayDialog("Player FSM", "Assign a GraphSO first.", "OK");
                    return;
                }

                _graphView.OpenCreateStateMenu(new Vector2(240f, 180f));
            })
            {
                text = "New State"
            };
            toolbar.Add(createStateButton);
            rootVisualElement.Add(toolbar);

            var splitView = new TwoPaneSplitView(1, 360, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1f;
            rootVisualElement.Add(splitView);

            _graphView = new FsmGraphView(this);
            _graphView.StretchToParentSize();
            _graphView.style.flexGrow = 1f;
            _graphView.style.minWidth = 480f;
            splitView.Add(_graphView);

            _inspector = new IMGUIContainer(DrawInspector);
            _inspector.style.flexGrow = 1f;
            _inspector.style.minWidth = 320f;
            _inspector.style.paddingLeft = 8f;
            _inspector.style.paddingRight = 8f;
            _inspector.style.paddingTop = 8f;
            splitView.Add(_inspector);

            if (_currentGraph != null)
            {
                LoadGraph(_currentGraph);
            }
        }

        public void LoadGraph(FsmGraphSO graph)
        {
            _currentGraph = graph;
            if (_graphField != null && _graphField.value != graph)
            {
                _graphField.SetValueWithoutNotify(graph);
            }

            _graphView?.Populate(graph);
            InspectObject(graph);
        }

        public FsmGraphSO CurrentGraph
        {
            get { return _currentGraph; }
        }

        public void InspectObject(UnityEngine.Object target)
        {
            _inspectedObject = target;
            _inspector?.MarkDirtyRepaint();
        }

        public void PersistAsset(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            _graphView?.RefreshLabels();
            _inspector?.MarkDirtyRepaint();
        }

        private void DrawInspector()
        {
            if (_currentGraph == null)
            {
                EditorGUILayout.HelpBox("Drag a GraphSO here to start editing.", MessageType.Info);
                return;
            }

            if (_inspectedObject == null)
            {
                _inspectedObject = _currentGraph;
            }

            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField(_inspectedObject, typeof(UnityEngine.Object), false);
            EditorGUILayout.Space(6f);

            if (_inspectedObject is FsmGraphSO graph)
            {
                DrawGraphInspector(graph);
                return;
            }

            if (_inspectedObject is TransitionLinkSO transition)
            {
                DrawTransitionInspector(transition);
                return;
            }

            if (_cachedEditor == null || _cachedEditor.target != _inspectedObject)
            {
                DestroyImmediate(_cachedEditor);
                UnityEditor.Editor.CreateCachedEditor(_inspectedObject, null, ref _cachedEditor);
            }

            if (_cachedEditor != null)
            {
                EditorGUI.BeginChangeCheck();
                _cachedEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    PersistAsset(_inspectedObject);
                }
            }
        }

        private void DrawGraphInspector(FsmGraphSO graph)
        {
            EditorGUILayout.LabelField("Graph Summary", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            graph.initialState = (StateSO)EditorGUILayout.ObjectField("Initial State", graph.initialState, typeof(StateSO), false);
            if (EditorGUI.EndChangeCheck())
            {
                PersistAsset(graph);
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("States", graph.states.Count.ToString());
            EditorGUILayout.LabelField("Transitions", graph.transitions.Count.ToString());
            EditorGUILayout.Space(8f);

            EditorGUILayout.HelpBox("Use the graph canvas to create states and connect transitions. Select a node or edge to edit its details here.", MessageType.Info);

            if (GUILayout.Button("Frame Graph"))
            {
                _graphView?.FrameAll();
            }

            if (GUILayout.Button("Create State"))
            {
                _graphView?.OpenCreateStateMenu(new Vector2(240f, 180f));
            }

            EditorGUILayout.Space(12f);
            if (GUILayout.Button("Auto Wire & Bind Now"))
            {
                PlayerFsmAutoWire.AutoWire();
            }
        }

        private void DrawTransitionInspector(TransitionLinkSO transition)
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.LabelField("Transition", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Path", transition.DisplayName);
                EditorGUILayout.LabelField("Conditions", transition.ConditionSummary, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(6f);

                transition.logic = (ConditionLogic)EditorGUILayout.EnumPopup("Logic", transition.logic);
                EditorGUILayout.Space(4f);

                for (int i = 0; i < transition.conditions.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    transition.conditions[i] = (ConditionSO)EditorGUILayout.ObjectField("Condition " + (i + 1), transition.conditions[i], typeof(ConditionSO), false);
                    if (GUILayout.Button("X", GUILayout.Width(24f)))
                    {
                        transition.conditions.RemoveAt(i);
                        PersistAsset(transition);
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add Condition Slot"))
                {
                    transition.conditions.Add(null);
                    PersistAsset(transition);
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Create Condition Asset"))
                {
                    ShowCreateConditionMenu(transition);
                }

                if (change.changed)
                {
                    PersistAsset(transition);
                }
            }
        }

        private void ShowCreateConditionMenu(TransitionLinkSO transition)
        {
            GenericMenu menu = new GenericMenu();
            foreach (Type conditionType in TypeCache.GetTypesDerivedFrom<ConditionSO>().OrderBy(t => t.Name))
            {
                if (conditionType.IsAbstract)
                {
                    continue;
                }

                string label = conditionType.Name.Replace("ConditionSO", string.Empty);
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    ConditionSO condition = PlayerFsmAssetUtility.CreateConditionAsset(_currentGraph, conditionType);
                    transition.conditions.Add(condition);
                    PersistAsset(condition);
                    PersistAsset(transition);
                });
            }

            menu.ShowAsContext();
        }
    }

    internal sealed class FsmGraphView : GraphView
    {
        private readonly FsmGraphViewEditor _owner;
        private readonly Dictionary<StateSO, FsmStateNode> _nodes = new Dictionary<StateSO, FsmStateNode>();
        private readonly Dictionary<TransitionLinkSO, FsmTransitionEdge> _edges = new Dictionary<TransitionLinkSO, FsmTransitionEdge>();
        private bool _isRefreshing;

        public FsmGraphView(FsmGraphViewEditor owner)
        {
            _owner = owner;
            style.flexGrow = 1f;
            Insert(0, new GridBackground());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            graphViewChanged += OnGraphViewChanged;
            serializeGraphElements = _ => string.Empty;
            canPasteSerializedData = _ => false;
            unserializeAndPaste = (_, __) => { };
            deleteSelection = DeleteSelectionInternal;
        }

        public void Populate(FsmGraphSO graph)
        {
            _isRefreshing = true;
            try
            {
                DeleteElements(graphElements.ToList());
                _nodes.Clear();
                _edges.Clear();

                if (graph == null)
                {
                    return;
                }

                foreach (StateSO state in graph.states.Where(s => s != null))
                {
                    AddStateNode(state);
                }

                foreach (TransitionLinkSO transition in graph.transitions.Where(t => t != null && t.from != null && t.to != null))
                {
                    AddTransitionEdge(transition);
                }

                FrameAll();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        public void RefreshLabels()
        {
            foreach (FsmTransitionEdge edge in _edges.Values)
            {
                edge.RefreshLabel();
            }

            foreach (FsmStateNode node in _nodes.Values)
            {
                node.RefreshTitle();
            }
        }

        public void OpenCreateStateMenu(Vector2 graphPosition)
        {
            if (_owner.CurrentGraph == null)
            {
                return;
            }

            GenericMenu menu = new GenericMenu();
            foreach (Type stateType in TypeCache.GetTypesDerivedFrom<StateSO>().OrderBy(t => t.Name))
            {
                if (stateType.IsAbstract)
                {
                    continue;
                }

                string label = stateType.Name.Replace("StateSO", string.Empty);
                menu.AddItem(new GUIContent(label), false, () => CreateState(stateType, graphPosition));
            }

            menu.ShowAsContext();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (_owner.CurrentGraph == null)
            {
                evt.menu.AppendAction("Create GraphSO First", _ => { }, DropdownMenuAction.Status.Disabled);
                return;
            }

            if (evt.target is Edge)
            {
                return;
            }

            Vector2 graphPosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Create State", _ => OpenCreateStateMenu(graphPosition));
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.Where(port => port.direction != startPort.direction && port.node != startPort.node).ToList();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isRefreshing)
            {
                return change;
            }

            if (_owner.CurrentGraph == null)
            {
                return change;
            }

            if (change.edgesToCreate != null)
            {
                for (int i = 0; i < change.edgesToCreate.Count; i++)
                {
                    Edge rawEdge = change.edgesToCreate[i];
                    if (rawEdge.userData is TransitionLinkSO)
                    {
                        continue;
                    }

                    var fromNode = rawEdge.output?.node as FsmStateNode;
                    var toNode = rawEdge.input?.node as FsmStateNode;
                    if (fromNode == null || toNode == null)
                    {
                        continue;
                    }

                    TransitionLinkSO transition = PlayerFsmAssetUtility.FindTransition(_owner.CurrentGraph, fromNode.State, toNode.State);
                    if (transition == null)
                    {
                        transition = PlayerFsmAssetUtility.CreateTransitionAsset(_owner.CurrentGraph, fromNode.State, toNode.State);
                        transition.from = fromNode.State;
                        transition.to = toNode.State;
                    }

                    if (!_owner.CurrentGraph.transitions.Contains(transition))
                    {
                        _owner.CurrentGraph.transitions.Add(transition);
                    }

                    var edge = new FsmTransitionEdge(transition)
                    {
                        output = fromNode.OutputPort,
                        input = toNode.InputPort
                    };
                    edge.output.Connect(edge);
                    edge.input.Connect(edge);
                    edge.RefreshLabel();
                    edge.OnEdgeSelected = selectedTransition => _owner.InspectObject(selectedTransition);
                    edge.HasReverseEdge = () => HasReverseTransition(transition);
                    edge.GetParallelSign = () => GetParallelSign(transition);
                    _edges[transition] = edge;
                    change.edgesToCreate[i] = edge;

                    _owner.PersistAsset(transition);
                    _owner.PersistAsset(_owner.CurrentGraph);
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (GraphElement element in change.elementsToRemove)
                {
                    if (element is FsmTransitionEdge edge && edge.Transition != null)
                    {
                        RemoveTransition(edge.Transition);
                    }
                    else if (element is FsmStateNode node && node.State != null)
                    {
                        RemoveState(node.State);
                    }
                }
            }

            return change;
        }

        private void DeleteSelectionInternal(string operationName, AskUser askUser)
        {
            DeleteElements(selection.OfType<GraphElement>().ToList());
        }

        private void CreateState(Type stateType, Vector2 graphPosition)
        {
            StateSO state = PlayerFsmAssetUtility.CreateStateAsset(_owner.CurrentGraph, stateType, graphPosition);
            _owner.CurrentGraph.states.Add(state);
            if (_owner.CurrentGraph.initialState == null)
            {
                _owner.CurrentGraph.initialState = state;
            }

            AddStateNode(state);
            _owner.PersistAsset(state);
            _owner.PersistAsset(_owner.CurrentGraph);
            _owner.InspectObject(state);
        }

        private void AddStateNode(StateSO state)
        {
            var node = new FsmStateNode(state);
            node.OnNodeMoved = movedState => _owner.PersistAsset(movedState);
            node.OnNodeSelected = selectedState => _owner.InspectObject(selectedState);
            AddElement(node);
            _nodes[state] = node;
        }

        private void AddTransitionEdge(TransitionLinkSO transition)
        {
            if (!_nodes.TryGetValue(transition.from, out FsmStateNode fromNode) || !_nodes.TryGetValue(transition.to, out FsmStateNode toNode))
            {
                return;
            }

            var edge = new FsmTransitionEdge(transition)
            {
                output = fromNode.OutputPort,
                input = toNode.InputPort
            };
            edge.output.Connect(edge);
            edge.input.Connect(edge);
            edge.RefreshLabel();
            edge.OnEdgeSelected = selectedTransition => _owner.InspectObject(selectedTransition);
            edge.HasReverseEdge = () => HasReverseTransition(transition);
            edge.GetParallelSign = () => GetParallelSign(transition);
            AddElement(edge);
            _edges[transition] = edge;
        }

        private bool HasReverseTransition(TransitionLinkSO transition)
        {
            for (int i = 0; i < _owner.CurrentGraph.transitions.Count; i++)
            {
                TransitionLinkSO other = _owner.CurrentGraph.transitions[i];
                if (other == null || other == transition)
                {
                    continue;
                }

                if (other.from == transition.to && other.to == transition.from)
                {
                    return true;
                }
            }

            return false;
        }

        private float GetParallelSign(TransitionLinkSO transition)
        {
            string fromName = transition.from != null ? transition.from.DisplayName : string.Empty;
            string toName = transition.to != null ? transition.to.DisplayName : string.Empty;
            int compare = string.CompareOrdinal(fromName, toName);
            if (compare == 0)
            {
                int fromId = transition.from != null ? transition.from.GetInstanceID() : 0;
                int toId = transition.to != null ? transition.to.GetInstanceID() : 0;
                compare = fromId.CompareTo(toId);
            }

            return compare <= 0 ? -1f : 1f;
        }

        private void RemoveTransition(TransitionLinkSO transition)
        {
            if (_owner.CurrentGraph.transitions.Remove(transition))
            {
                _edges.Remove(transition);
                PlayerFsmAssetUtility.DeleteUnusedConditions(_owner.CurrentGraph, transition);
                string assetPath = AssetDatabase.GetAssetPath(transition);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }

                _owner.PersistAsset(_owner.CurrentGraph);
            }
        }

        private void RemoveState(StateSO state)
        {
            List<TransitionLinkSO> attachedTransitions = _owner.CurrentGraph.transitions
                .Where(t => t != null && (t.from == state || t.to == state))
                .ToList();

            foreach (TransitionLinkSO transition in attachedTransitions)
            {
                RemoveTransition(transition);
            }

            _owner.CurrentGraph.states.Remove(state);
            _nodes.Remove(state);
            if (_owner.CurrentGraph.initialState == state)
            {
                _owner.CurrentGraph.initialState = _owner.CurrentGraph.states.FirstOrDefault(s => s != null);
            }

            string assetPath = AssetDatabase.GetAssetPath(state);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            _owner.PersistAsset(_owner.CurrentGraph);
        }
    }

    internal static class PlayerFsmAssetUtility
    {
        public static TransitionLinkSO FindTransition(FsmGraphSO graph, StateSO from, StateSO to)
        {
            for (int i = 0; i < graph.transitions.Count; i++)
            {
                TransitionLinkSO transition = graph.transitions[i];
                if (transition != null && transition.from == from && transition.to == to)
                {
                    return transition;
                }
            }

            string[] guids = AssetDatabase.FindAssets("t:TransitionLinkSO", new[] { GetTransitionsFolder(graph) });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TransitionLinkSO transition = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(path);
                if (transition != null && transition.from == from && transition.to == to)
                {
                    return transition;
                }
            }

            return null;
        }

        public static StateSO CreateStateAsset(FsmGraphSO graph, Type stateType, Vector2 editorPosition)
        {
            EnsureFolders(graph);
            StateSO state = ScriptableObject.CreateInstance(stateType) as StateSO;
            state.displayName = stateType.Name.Replace("StateSO", string.Empty);
            state.editorPosition = editorPosition;
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(GetStatesFolder(graph) + "/" + state.displayName + ".asset");
            AssetDatabase.CreateAsset(state, assetPath);
            return state;
        }

        public static TransitionLinkSO CreateTransitionAsset(FsmGraphSO graph, StateSO from, StateSO to)
        {
            EnsureFolders(graph);
            TransitionLinkSO transition = ScriptableObject.CreateInstance<TransitionLinkSO>();
            string fileName = string.Format("{0}To{1}.asset", from != null ? from.DisplayName : "From", to != null ? to.DisplayName : "To");
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(GetTransitionsFolder(graph) + "/" + fileName);
            AssetDatabase.CreateAsset(transition, assetPath);
            return transition;
        }

        public static ConditionSO CreateConditionAsset(FsmGraphSO graph, Type conditionType)
        {
            EnsureFolders(graph);
            ConditionSO condition = ScriptableObject.CreateInstance(conditionType) as ConditionSO;
            string label = conditionType.Name.Replace("ConditionSO", string.Empty);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(GetConditionsFolder(graph) + "/" + label + ".asset");
            AssetDatabase.CreateAsset(condition, assetPath);
            return condition;
        }

        public static void DeleteUnusedConditions(FsmGraphSO graph, TransitionLinkSO removedTransition)
        {
            if (removedTransition == null || removedTransition.conditions == null)
            {
                return;
            }

            for (int i = 0; i < removedTransition.conditions.Count; i++)
            {
                ConditionSO condition = removedTransition.conditions[i];
                if (condition == null || IsConditionReferenced(graph, condition, removedTransition))
                {
                    continue;
                }

                string conditionPath = AssetDatabase.GetAssetPath(condition);
                if (!string.IsNullOrEmpty(conditionPath) && conditionPath.StartsWith(GetConditionsFolder(graph), StringComparison.OrdinalIgnoreCase))
                {
                    AssetDatabase.DeleteAsset(conditionPath);
                }
            }
        }

        public static void CleanupOrphanAssets(FsmGraphSO graph)
        {
            if (graph == null)
            {
                return;
            }

            graph.transitions.RemoveAll(t => t == null);
            graph.states.RemoveAll(s => s == null);

            string[] transitionGuids = AssetDatabase.FindAssets("t:TransitionLinkSO", new[] { GetTransitionsFolder(graph) });
            for (int i = 0; i < transitionGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(transitionGuids[i]);
                TransitionLinkSO transition = AssetDatabase.LoadAssetAtPath<TransitionLinkSO>(path);
                if (transition == null || !graph.transitions.Contains(transition))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            string[] conditionGuids = AssetDatabase.FindAssets("t:ConditionSO", new[] { GetConditionsFolder(graph) });
            for (int i = 0; i < conditionGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(conditionGuids[i]);
                ConditionSO condition = AssetDatabase.LoadAssetAtPath<ConditionSO>(path);
                if (condition == null || !IsConditionReferenced(graph, condition, null))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
        }

        private static bool IsConditionReferenced(FsmGraphSO graph, ConditionSO condition, TransitionLinkSO excludeTransition)
        {
            for (int i = 0; i < graph.transitions.Count; i++)
            {
                TransitionLinkSO transition = graph.transitions[i];
                if (transition == null || transition == excludeTransition || transition.conditions == null)
                {
                    continue;
                }

                if (transition.conditions.Contains(condition))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFolders(FsmGraphSO graph)
        {
            string rootFolder = GetRootFolder(graph);
            EnsureFolder(rootFolder);
            EnsureFolder(GetStatesFolder(graph));
            EnsureFolder(GetTransitionsFolder(graph));
            EnsureFolder(GetConditionsFolder(graph));
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

        private static string GetRootFolder(FsmGraphSO graph)
        {
            string graphPath = AssetDatabase.GetAssetPath(graph);
            string rootFolder = System.IO.Path.GetDirectoryName(graphPath)?.Replace("\\", "/");
            return string.IsNullOrEmpty(rootFolder) ? "Assets/Settings/PlayerFSM" : rootFolder;
        }

        private static string GetStatesFolder(FsmGraphSO graph)
        {
            return GetRootFolder(graph) + "/States";
        }

        private static string GetTransitionsFolder(FsmGraphSO graph)
        {
            return GetRootFolder(graph) + "/Transitions";
        }

        private static string GetConditionsFolder(FsmGraphSO graph)
        {
            return GetRootFolder(graph) + "/Conditions";
        }
    }
}

