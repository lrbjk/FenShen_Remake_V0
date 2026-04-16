using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace FenShen.PlayerFSM
{
    internal sealed class FsmStateNode : Node
    {
        private readonly Label _typeLabel;

        public FsmStateNode(StateSO state)
        {
            State = state;
            viewDataKey = state.name;

            _typeLabel = new Label();
            _typeLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            _typeLabel.style.fontSize = 11f;
            extensionContainer.Add(_typeLabel);

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = string.Empty;
            inputContainer.Add(InputPort);

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            OutputPort.portName = string.Empty;
            outputContainer.Add(OutputPort);

            SetPosition(new Rect(state.editorPosition, new Vector2(220f, 110f)));
            RefreshTitle();
            RefreshExpandedState();
            RefreshPorts();
        }

        public StateSO State { get; }
        public Port InputPort { get; }
        public Port OutputPort { get; }
        public Action<StateSO> OnNodeMoved { get; set; }
        public Action<StateSO> OnNodeSelected { get; set; }

        public void RefreshTitle()
        {
            title = State.DisplayName;
            string stateType = State.GetType().Name.Replace("StateSO", string.Empty);
            _typeLabel.text = string.IsNullOrWhiteSpace(State.animStateName)
                ? stateType
                : stateType + " | " + State.animStateName;
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            State.editorPosition = newPos.position;
            OnNodeMoved?.Invoke(State);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(State);
        }
    }

    internal sealed class FsmTransitionEdge : Edge
    {
        private readonly Label _label;
        private readonly VisualElement _lineLayer;
        private readonly Color _lineColor = new Color(0.72f, 0.72f, 0.72f, 0.95f);
        private readonly Color _selectedLineColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        private readonly Color _arrowColor = new Color(1f, 1f, 1f, 0.98f);

        public FsmTransitionEdge(TransitionLinkSO transition)
        {
            Transition = transition;
            userData = transition;

            if (edgeControl != null)
            {
                edgeControl.style.opacity = 0f;
                edgeControl.drawFromCap = false;
                edgeControl.drawToCap = false;
                edgeControl.pickingMode = PickingMode.Position;
                edgeControl.interceptWidth = 28f;
            }

            _lineLayer = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            _lineLayer.style.position = Position.Absolute;
            _lineLayer.style.left = 0f;
            _lineLayer.style.top = 0f;
            _lineLayer.style.right = 0f;
            _lineLayer.style.bottom = 0f;
            _lineLayer.generateVisualContent += OnGenerateStraightLine;
            Insert(0, _lineLayer);

            _label = new Label
            {
                pickingMode = PickingMode.Position
            };
            _label.style.position = Position.Absolute;
            _label.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            _label.style.color = Color.white;
            _label.style.paddingLeft = 4f;
            _label.style.paddingRight = 4f;
            _label.style.borderBottomLeftRadius = 3f;
            _label.style.borderBottomRightRadius = 3f;
            _label.style.borderTopLeftRadius = 3f;
            _label.style.borderTopRightRadius = 3f;
            _label.RegisterCallback<MouseDownEvent>(_ => SelectThisEdge());
            Add(_label);

            RegisterCallback<GeometryChangedEvent>(_ => RefreshVisuals());
            schedule.Execute(RefreshVisuals).Every(16);
        }

        public TransitionLinkSO Transition { get; }
        public Action<TransitionLinkSO> OnEdgeSelected { get; set; }
        public Func<bool> HasReverseEdge { get; set; }
        public Func<float> GetParallelSign { get; set; }

        public void RefreshLabel()
        {
            _label.text = Transition != null ? Transition.ConditionSummary : string.Empty;
            RefreshVisuals();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            RefreshVisuals();
            OnEdgeSelected?.Invoke(Transition);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            UpdateLabelPosition();
            _lineLayer.MarkDirtyRepaint();
        }

        private void UpdateLabelPosition()
        {
            if (!TryGetEndpoints(out Vector2 from, out Vector2 to))
            {
                return;
            }

            OffsetLine(ref from, ref to);
            Vector2 mid = (from + to) * 0.5f;
            _label.style.left = mid.x + 10f;
            _label.style.top = mid.y - 26f;
        }

        private void OnGenerateStraightLine(MeshGenerationContext context)
        {
            if (!TryGetEndpoints(out Vector2 from, out Vector2 to))
            {
                return;
            }

            OffsetLine(ref from, ref to);

            Vector2 arrowSegment = to - from;
            float length = arrowSegment.magnitude;
            if (length < 0.001f)
            {
                return;
            }

            Vector2 direction = arrowSegment / length;
            float arrowLength = 14f;
            float arrowWidth = 6f;
            Vector2 arrowNormal = new Vector2(-direction.y, direction.x);
            Vector2 arrowTip = to;
            Vector2 arrowBase = arrowTip - direction * arrowLength;
            Vector2 arrowLeft = arrowBase + arrowNormal * arrowWidth;
            Vector2 arrowRight = arrowBase - arrowNormal * arrowWidth;

            var painter = context.painter2D;
            painter.lineWidth = selected ? 2.4f : 1.8f;
            painter.strokeColor = selected ? _selectedLineColor : _lineColor;
            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(to);
            painter.Stroke();

            painter.fillColor = _arrowColor;
            painter.BeginPath();
            painter.MoveTo(arrowTip);
            painter.LineTo(arrowLeft);
            painter.LineTo(arrowRight);
            painter.ClosePath();
            painter.Fill();
        }

        private bool TryGetEndpoints(out Vector2 from, out Vector2 to)
        {
            from = Vector2.zero;
            to = Vector2.zero;

            if (output?.node is Node fromNode && input?.node is Node toNode)
            {
                Rect fromRect = fromNode.worldBound;
                Rect toRect = toNode.worldBound;
                Vector2 fromCenterWorld = fromRect.center;
                Vector2 toCenterWorld = toRect.center;
                from = GetNodeBoundaryPoint(fromRect, fromCenterWorld, toCenterWorld);
                to = GetNodeBoundaryPoint(toRect, toCenterWorld, fromCenterWorld);
                return true;
            }

            if (edgeControl == null)
            {
                return false;
            }

            from = edgeControl.from;
            to = edgeControl.to;
            return true;
        }

        private Vector2 GetNodeBoundaryPoint(Rect rectWorld, Vector2 centerWorld, Vector2 targetWorld)
        {
            Vector2 delta = targetWorld - centerWorld;
            if (delta.sqrMagnitude < 0.0001f)
            {
                return _lineLayer.WorldToLocal(centerWorld);
            }

            float halfWidth = rectWorld.width * 0.5f;
            float halfHeight = rectWorld.height * 0.5f;
            float tx = Mathf.Approximately(delta.x, 0f) ? float.PositiveInfinity : halfWidth / Mathf.Abs(delta.x);
            float ty = Mathf.Approximately(delta.y, 0f) ? float.PositiveInfinity : halfHeight / Mathf.Abs(delta.y);
            float t = Mathf.Min(tx, ty);
            Vector2 boundaryWorld = centerWorld + delta * t;
            return _lineLayer.WorldToLocal(boundaryWorld);
        }

        private void OffsetLine(ref Vector2 from, ref Vector2 to)
        {
            if (HasReverseEdge == null || !HasReverseEdge())
            {
                return;
            }

            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.001f)
            {
                return;
            }

            Vector2 direction = delta / length;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            float sign = GetParallelSign != null ? Mathf.Sign(GetParallelSign()) : 1f;
            float offsetAmount = 10f * sign;
            Vector2 offset = normal * offsetAmount;
            from += offset;
            to += offset;
        }

        private void SelectThisEdge()
        {
            GraphView graphView = GetFirstAncestorOfType<GraphView>();
            if (graphView == null)
            {
                return;
            }

            if (!selected)
            {
                graphView.ClearSelection();
                graphView.AddToSelection(this);
            }

            OnEdgeSelected?.Invoke(Transition);
        }
    }
}

