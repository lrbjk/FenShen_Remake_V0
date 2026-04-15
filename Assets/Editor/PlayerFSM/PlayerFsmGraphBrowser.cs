using UnityEditor;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    public class PlayerFsmGraphBrowser : EditorWindow
    {
        private FsmGraphSO _graph;
        private Vector2 _scroll;

        [MenuItem("Tools / Player FSM / Graph Browser")]
        [MenuItem("Window / Player FSM / Graph Browser")]
        public static void ShowWindow()
        {
            GetWindow<PlayerFsmGraphBrowser>("FSM Graph Browser");
        }

        private void OnGUI()
        {
            _graph = (FsmGraphSO)EditorGUILayout.ObjectField("Graph", _graph, typeof(FsmGraphSO), false);
            if (_graph == null)
            {
                EditorGUILayout.HelpBox("Assign a FsmGraph asset to browse.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Auto Wire & Bind Now"))
            {
                PlayerFsmAutoWire.AutoWire();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Initial State", _graph.initialState ? _graph.initialState.DisplayName : "< None >");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("States (" + _graph.states.Count + ")");
            foreach (var state in _graph.states)
            {
                if (state != null)
                {
                    EditorGUILayout.LabelField("- " + state.DisplayName);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transitions (" + _graph.transitions.Count + ")");
            foreach (var transition in _graph.transitions)
            {
                if (transition == null)
                {
                    continue;
                }

                EditorGUILayout.LabelField(transition.DisplayName + " | " + transition.ConditionSummary);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
