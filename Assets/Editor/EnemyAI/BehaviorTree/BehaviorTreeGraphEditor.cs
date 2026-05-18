using System;
using System.Collections.Generic;
using System.Linq;
using FenShen.EnemyAI.BehaviorTree;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FenShen.EnemyAI.BehaviorTree.Editor
{
    public class BehaviorTreeGraphEditor : EditorWindow
    {
        private BehaviorTreeGraphView _graphView;
        private ObjectField _graphField;
        private IMGUIContainer _inspector;
        private BehaviorTreeGraphSO _currentGraph;
        private UnityEditor.Editor _cachedEditor;
        private UnityEngine.Object _inspectedObject;

        [MenuItem("Tools/Enemy AI/Behavior Tree Editor")]
        public static void ShowWindow()
        {
            GetWindow<BehaviorTreeGraphEditor>("Enemy Behavior Tree");
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
            _graphField = new ObjectField("Tree")
            {
                objectType = typeof(BehaviorTreeGraphSO),
                allowSceneObjects = false,
                value = _currentGraph
            };
            _graphField.RegisterValueChangedCallback(evt => LoadGraph(evt.newValue as BehaviorTreeGraphSO));
            toolbar.Add(_graphField);

            toolbar.Add(new ToolbarButton(CreateGraphAsset) { text = "New Tree" });
            toolbar.Add(new ToolbarButton(() => _graphView?.OpenCreateNodeMenu(new Vector2(260f, 180f))) { text = "New Node" });
            toolbar.Add(new ToolbarButton(() => _graphView?.FrameAll()) { text = "Frame All" });
            rootVisualElement.Add(toolbar);

            var splitView = new TwoPaneSplitView(1, 360, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.flexGrow = 1f;
            rootVisualElement.Add(splitView);

            _graphView = new BehaviorTreeGraphView(this);
            _graphView.StretchToParentSize();
            _graphView.style.flexGrow = 1f;
            _graphView.style.minWidth = 520f;
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

        public BehaviorTreeGraphSO CurrentGraph
        {
            get { return _currentGraph; }
        }

        public void LoadGraph(BehaviorTreeGraphSO graph)
        {
            _currentGraph = graph;
            if (_graphField != null && _graphField.value != graph)
            {
                _graphField.SetValueWithoutNotify(graph);
            }

            _graphView?.Populate(graph);
            InspectObject(graph);
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
            if (_currentGraph != null)
            {
                EditorUtility.SetDirty(_currentGraph);
            }

            AssetDatabase.SaveAssets();
            _graphView?.RefreshLabels();
            _inspector?.MarkDirtyRepaint();
        }

        private void DrawInspector()
        {
            if (_currentGraph == null)
            {
                EditorGUILayout.HelpBox("Create or assign a Behavior Tree asset to start editing.", MessageType.Info);
                if (GUILayout.Button("Create Behavior Tree"))
                {
                    CreateGraphAsset();
                }

                return;
            }

            if (_inspectedObject == null)
            {
                _inspectedObject = _currentGraph;
            }

            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField(_inspectedObject, typeof(UnityEngine.Object), false);
            EditorGUILayout.Space(6f);

            if (_inspectedObject == _currentGraph)
            {
                DrawGraphInspector();
                return;
            }

            if (_cachedEditor == null || _cachedEditor.target != _inspectedObject)
            {
                DestroyImmediate(_cachedEditor);
                UnityEditor.Editor.CreateCachedEditor(_inspectedObject, null, ref _cachedEditor);
            }

            if (_cachedEditor == null)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            _cachedEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                PersistAsset(_inspectedObject);
            }

            if (_inspectedObject is BehaviorTreeNodeSO node)
            {
                EditorGUILayout.Space(8f);
                using (new EditorGUI.DisabledScope(_currentGraph.rootNode == node))
                {
                    if (GUILayout.Button("Set As Root"))
                    {
                        _currentGraph.rootNode = node;
                        PersistAsset(_currentGraph);
                    }
                }
            }
        }

        private void DrawGraphInspector()
        {
            EditorGUI.BeginChangeCheck();
            _currentGraph.rootNode = (BehaviorTreeNodeSO)EditorGUILayout.ObjectField("Root", _currentGraph.rootNode, typeof(BehaviorTreeNodeSO), false);
            if (EditorGUI.EndChangeCheck())
            {
                PersistAsset(_currentGraph);
                _graphView?.RefreshLabels();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Nodes", _currentGraph.nodes.Count.ToString());
            EditorGUILayout.HelpBox("Right-click the canvas to create nodes. Drag from a parent output to a child input to build the tree. Composite children execute left to right by their X position.", MessageType.Info);

            if (GUILayout.Button("Create Node"))
            {
                _graphView?.OpenCreateNodeMenu(new Vector2(260f, 180f));
            }

            if (GUILayout.Button("Frame Tree"))
            {
                _graphView?.FrameAll();
            }
        }

        private void CreateGraphAsset()
        {
            BehaviorTreeAssetUtility.EnsureRootFolder("Assets/Settings/EnemyAI");
            string path = EditorUtility.SaveFilePanelInProject("Create Behavior Tree", "EnemyBehaviorTree", "asset", "Choose where to save the behavior tree.", "Assets/Settings/EnemyAI");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            BehaviorTreeGraphSO graph = CreateInstance<BehaviorTreeGraphSO>();
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            LoadGraph(graph);
        }
    }

    internal sealed class BehaviorTreeGraphView : GraphView
    {
        private readonly BehaviorTreeGraphEditor _owner;
        private readonly Dictionary<BehaviorTreeNodeSO, BehaviorTreeGraphNode> _nodes = new Dictionary<BehaviorTreeNodeSO, BehaviorTreeGraphNode>();
        private bool _isRefreshing;

        public BehaviorTreeGraphView(BehaviorTreeGraphEditor owner)
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

        public void Populate(BehaviorTreeGraphSO graph)
        {
            _isRefreshing = true;
            try
            {
                DeleteElements(graphElements.ToList());
                _nodes.Clear();

                if (graph == null)
                {
                    return;
                }

                graph.nodes.RemoveAll(n => n == null);
                foreach (BehaviorTreeNodeSO node in graph.nodes)
                {
                    AddNodeElement(node);
                }

                foreach (BehaviorTreeNodeSO parent in graph.nodes)
                {
                    IReadOnlyList<BehaviorTreeNodeSO> children = parent.GetChildren();
                    for (int i = 0; i < children.Count; i++)
                    {
                        AddTreeEdge(parent, children[i]);
                    }
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
            foreach (BehaviorTreeGraphNode node in _nodes.Values)
            {
                node.RefreshTitle(_owner.CurrentGraph);
            }
        }

        public void OpenCreateNodeMenu(Vector2 graphPosition)
        {
            if (_owner.CurrentGraph == null)
            {
                return;
            }

            GenericMenu menu = new GenericMenu();
            foreach (Type nodeType in TypeCache.GetTypesDerivedFrom<BehaviorTreeNodeSO>().OrderBy(t => t.Name))
            {
                if (nodeType.IsAbstract)
                {
                    continue;
                }

                string category = GetNodeCategory(nodeType);
                string label = ObjectNames.NicifyVariableName(nodeType.Name.Replace("NodeSO", string.Empty));
                menu.AddItem(new GUIContent(category + "/" + label), false, () => CreateNode(nodeType, graphPosition));
            }

            menu.ShowAsContext();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (_owner.CurrentGraph == null)
            {
                evt.menu.AppendAction("Create Behavior Tree First", _ => { }, DropdownMenuAction.Status.Disabled);
                return;
            }

            if (evt.target is Edge)
            {
                return;
            }

            Vector2 graphPosition = contentViewContainer.WorldToLocal(evt.mousePosition);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Create Node", _ => OpenCreateNodeMenu(graphPosition));
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.Where(port => port.direction != startPort.direction && port.node != startPort.node).ToList();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_isRefreshing || _owner.CurrentGraph == null)
            {
                return change;
            }

            if (change.edgesToCreate != null)
            {
                for (int i = 0; i < change.edgesToCreate.Count; i++)
                {
                    Edge rawEdge = change.edgesToCreate[i];
                    var parentNode = rawEdge.output?.node as BehaviorTreeGraphNode;
                    var childNode = rawEdge.input?.node as BehaviorTreeGraphNode;
                    if (parentNode == null || childNode == null || parentNode.Node == null || childNode.Node == null)
                    {
                        continue;
                    }

                    if (CreatesCycle(parentNode.Node, childNode.Node))
                    {
                        Debug.LogWarning("Behavior Tree: rejected connection because it would create a cycle.");
                        continue;
                    }

                    if (parentNode.Node is DecoratorNodeSO)
                    {
                        RemoveExistingDecoratorEdge(parentNode);
                    }

                    parentNode.Node.AddChild(childNode.Node);
                    SortChildren(parentNode.Node);
                    Edge edge = CreateEdge(parentNode, childNode);
                    change.edgesToCreate[i] = edge;
                    _owner.PersistAsset(parentNode.Node);
                    _owner.PersistAsset(_owner.CurrentGraph);
                }
            }

            if (change.movedElements != null)
            {
                HashSet<BehaviorTreeNodeSO> movedParents = new HashSet<BehaviorTreeNodeSO>();
                foreach (GraphElement element in change.movedElements)
                {
                    var movedNode = element as BehaviorTreeGraphNode;
                    if (movedNode == null)
                    {
                        continue;
                    }

                    _owner.PersistAsset(movedNode.Node);
                    foreach (BehaviorTreeNodeSO parent in FindParents(movedNode.Node))
                    {
                        movedParents.Add(parent);
                    }
                }

                foreach (BehaviorTreeNodeSO parent in movedParents)
                {
                    SortChildren(parent);
                    _owner.PersistAsset(parent);
                }
            }

            if (change.elementsToRemove != null)
            {
                foreach (GraphElement element in change.elementsToRemove)
                {
                    if (element is Edge edge)
                    {
                        RemoveEdgeRelationship(edge);
                    }
                    else if (element is BehaviorTreeGraphNode node)
                    {
                        RemoveNode(node.Node);
                    }
                }
            }

            return change;
        }

        private void DeleteSelectionInternal(string operationName, AskUser askUser)
        {
            DeleteElements(selection.OfType<GraphElement>().ToList());
        }

        private void CreateNode(Type nodeType, Vector2 graphPosition)
        {
            BehaviorTreeNodeSO node = BehaviorTreeAssetUtility.CreateNodeAsset(_owner.CurrentGraph, nodeType, graphPosition);
            _owner.CurrentGraph.nodes.Add(node);
            if (_owner.CurrentGraph.rootNode == null)
            {
                _owner.CurrentGraph.rootNode = node;
            }

            AddNodeElement(node);
            _owner.PersistAsset(node);
            _owner.PersistAsset(_owner.CurrentGraph);
            _owner.InspectObject(node);
        }

        private void AddNodeElement(BehaviorTreeNodeSO node)
        {
            var graphNode = new BehaviorTreeGraphNode(node);
            graphNode.OnNodeMoved = movedNode => _owner.PersistAsset(movedNode);
            graphNode.OnNodeSelected = selectedNode => _owner.InspectObject(selectedNode);
            AddElement(graphNode);
            _nodes[node] = graphNode;
            graphNode.RefreshTitle(_owner.CurrentGraph);
        }

        private void AddTreeEdge(BehaviorTreeNodeSO parent, BehaviorTreeNodeSO child)
        {
            BehaviorTreeGraphNode parentNode;
            BehaviorTreeGraphNode childNode;
            if (!_nodes.TryGetValue(parent, out parentNode) || !_nodes.TryGetValue(child, out childNode))
            {
                return;
            }

            AddElement(CreateEdge(parentNode, childNode));
        }

        private Edge CreateEdge(BehaviorTreeGraphNode parentNode, BehaviorTreeGraphNode childNode)
        {
            Edge edge = parentNode.OutputPort.ConnectTo(childNode.InputPort);
            edge.userData = new BehaviorTreeEdgeData(parentNode.Node, childNode.Node);
            return edge;
        }

        private void RemoveExistingDecoratorEdge(BehaviorTreeGraphNode parentNode)
        {
            List<Edge> existing = edges
                .Where(e => e.output == parentNode.OutputPort)
                .ToList();
            foreach (Edge edge in existing)
            {
                DeleteElements(new[] { edge });
            }
        }

        private void RemoveEdgeRelationship(Edge edge)
        {
            var data = edge.userData as BehaviorTreeEdgeData;
            if (data == null)
            {
                var parentNode = edge.output?.node as BehaviorTreeGraphNode;
                var childNode = edge.input?.node as BehaviorTreeGraphNode;
                if (parentNode == null || childNode == null)
                {
                    return;
                }

                data = new BehaviorTreeEdgeData(parentNode.Node, childNode.Node);
            }

            if (data.Parent != null && data.Child != null && data.Parent.RemoveChild(data.Child))
            {
                _owner.PersistAsset(data.Parent);
            }
        }

        private void RemoveNode(BehaviorTreeNodeSO node)
        {
            if (node == null)
            {
                return;
            }

            foreach (BehaviorTreeNodeSO parent in FindParents(node).ToList())
            {
                parent.RemoveChild(node);
                _owner.PersistAsset(parent);
            }

            IReadOnlyList<BehaviorTreeNodeSO> children = node.GetChildren();
            for (int i = children.Count - 1; i >= 0; i--)
            {
                node.RemoveChild(children[i]);
            }

            _owner.CurrentGraph.nodes.Remove(node);
            _nodes.Remove(node);
            if (_owner.CurrentGraph.rootNode == node)
            {
                _owner.CurrentGraph.rootNode = _owner.CurrentGraph.nodes.FirstOrDefault(n => n != null);
            }

            string assetPath = AssetDatabase.GetAssetPath(node);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            _owner.PersistAsset(_owner.CurrentGraph);
            _owner.InspectObject(_owner.CurrentGraph);
        }

        private IEnumerable<BehaviorTreeNodeSO> FindParents(BehaviorTreeNodeSO child)
        {
            for (int i = 0; i < _owner.CurrentGraph.nodes.Count; i++)
            {
                BehaviorTreeNodeSO candidate = _owner.CurrentGraph.nodes[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.GetChildren().Contains(child))
                {
                    yield return candidate;
                }
            }
        }

        private bool CreatesCycle(BehaviorTreeNodeSO parent, BehaviorTreeNodeSO child)
        {
            if (parent == child)
            {
                return true;
            }

            return HasDescendant(child, parent);
        }

        private bool HasDescendant(BehaviorTreeNodeSO node, BehaviorTreeNodeSO expectedDescendant)
        {
            IReadOnlyList<BehaviorTreeNodeSO> children = node.GetChildren();
            for (int i = 0; i < children.Count; i++)
            {
                BehaviorTreeNodeSO child = children[i];
                if (child == null)
                {
                    continue;
                }

                if (child == expectedDescendant || HasDescendant(child, expectedDescendant))
                {
                    return true;
                }
            }

            return false;
        }

        private void SortChildren(BehaviorTreeNodeSO parent)
        {
            CompositeNodeSO composite = parent as CompositeNodeSO;
            if (composite != null)
            {
                composite.SortChildrenByPosition();
            }
        }

        private static string GetNodeCategory(Type nodeType)
        {
            if (typeof(CompositeNodeSO).IsAssignableFrom(nodeType))
            {
                return "Composite";
            }

            if (typeof(DecoratorNodeSO).IsAssignableFrom(nodeType))
            {
                return "Decorator";
            }

            if (nodeType.Name.Contains("Condition"))
            {
                return "Condition";
            }

            return "Action";
        }
    }

    internal sealed class BehaviorTreeGraphNode : Node
    {
        private readonly Label _typeLabel;
        private readonly Label _rootLabel;

        public BehaviorTreeGraphNode(BehaviorTreeNodeSO node)
        {
            Node = node;
            viewDataKey = node.name;

            _rootLabel = new Label();
            _rootLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _rootLabel.style.color = new Color(0.9f, 0.74f, 0.28f, 1f);
            titleContainer.Add(_rootLabel);

            _typeLabel = new Label();
            _typeLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            _typeLabel.style.fontSize = 11f;
            extensionContainer.Add(_typeLabel);

            InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "Parent";
            inputContainer.Add(InputPort);

            Port.Capacity outputCapacity = node is DecoratorNodeSO ? Port.Capacity.Single : Port.Capacity.Multi;
            OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, outputCapacity, typeof(bool));
            OutputPort.portName = "Child";
            outputContainer.Add(OutputPort);

            SetPosition(new Rect(node.editorPosition, new Vector2(230f, 120f)));
            RefreshExpandedState();
            RefreshPorts();
        }

        public BehaviorTreeNodeSO Node { get; }
        public Port InputPort { get; }
        public Port OutputPort { get; }
        public Action<BehaviorTreeNodeSO> OnNodeMoved { get; set; }
        public Action<BehaviorTreeNodeSO> OnNodeSelected { get; set; }

        public void RefreshTitle(BehaviorTreeGraphSO graph)
        {
            title = Node.DisplayName;
            _rootLabel.text = graph != null && graph.rootNode == Node ? "ROOT" : string.Empty;
            _typeLabel.text = ObjectNames.NicifyVariableName(Node.GetType().Name.Replace("NodeSO", string.Empty));
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Node.editorPosition = newPos.position;
            OnNodeMoved?.Invoke(Node);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(Node);
        }
    }

    internal sealed class BehaviorTreeEdgeData
    {
        public BehaviorTreeEdgeData(BehaviorTreeNodeSO parent, BehaviorTreeNodeSO child)
        {
            Parent = parent;
            Child = child;
        }

        public BehaviorTreeNodeSO Parent { get; private set; }
        public BehaviorTreeNodeSO Child { get; private set; }
    }

    internal static class BehaviorTreeAssetUtility
    {
        public static void EnsureRootFolder(string path)
        {
            EnsureFolder(path);
        }

        public static BehaviorTreeNodeSO CreateNodeAsset(BehaviorTreeGraphSO graph, Type nodeType, Vector2 editorPosition)
        {
            EnsureFolders(graph);
            BehaviorTreeNodeSO node = ScriptableObject.CreateInstance(nodeType) as BehaviorTreeNodeSO;
            node.displayName = ObjectNames.NicifyVariableName(nodeType.Name.Replace("NodeSO", string.Empty));
            node.editorPosition = editorPosition;
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(GetNodesFolder(graph) + "/" + node.displayName + ".asset");
            AssetDatabase.CreateAsset(node, assetPath);
            return node;
        }

        private static void EnsureFolders(BehaviorTreeGraphSO graph)
        {
            EnsureFolder(GetRootFolder(graph));
            EnsureFolder(GetNodesFolder(graph));
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

        private static string GetRootFolder(BehaviorTreeGraphSO graph)
        {
            string graphPath = AssetDatabase.GetAssetPath(graph);
            string rootFolder = System.IO.Path.GetDirectoryName(graphPath)?.Replace("\\", "/");
            return string.IsNullOrEmpty(rootFolder) ? "Assets/Settings/EnemyAI" : rootFolder;
        }

        private static string GetNodesFolder(BehaviorTreeGraphSO graph)
        {
            return GetRootFolder(graph) + "/Nodes";
        }
    }
}
