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
        private readonly Color _lineColor = new Color(0.78f, 0.78f, 0.78f, 0.95f);
        private readonly Color _selectedLineColor = new Color(1f, 0.78f, 0.28f, 1f);

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
                pickingMode = PickingMode.Ignore
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
            Add(_label);

            RegisterCallback<GeometryChangedEvent>(_ => RefreshVisuals());
            schedule.Execute(RefreshVisuals).Every(16);
        }

        public TransitionLinkSO Transition { get; }
        public Action<TransitionLinkSO> OnEdgeSelected { get; set; }

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
            if (edgeControl == null)
            {
                return;
            }

            Vector2 from = edgeControl.from;
            Vector2 to = edgeControl.to;
            Vector2 mid = (from + to) * 0.5f;
            _label.style.left = mid.x + 8f;
            _label.style.top = mid.y - 22f;
        }

        private void OnGenerateStraightLine(MeshGenerationContext context)
        {
            if (edgeControl == null)
            {
                return;
            }

            Vector2 from = edgeControl.from;
            Vector2 to = edgeControl.to;

            var painter = context.painter2D;
            painter.lineWidth = selected ? 3.5f : 2.25f;
            painter.strokeColor = selected ? _selectedLineColor : _lineColor;
            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(to);
            painter.Stroke();
        }
    }
}

