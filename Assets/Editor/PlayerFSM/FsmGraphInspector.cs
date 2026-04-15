using UnityEditor;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    [CustomEditor(typeof(FsmGraphSO))]
    public class FsmGraphInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (GUILayout.Button("Auto Wire & Bind Now"))
            {
                PlayerFsmAutoWire.AutoWire();
            }

            EditorGUILayout.HelpBox("Select this asset and click to wire transitions and bind PlayerFsm to 'Player'.", MessageType.Info);
        }

        [MenuItem("CONTEXT / FsmGraphSO / Auto Wire & Bind")]
        private static void ContextAutoWire(MenuCommand cmd)
        {
            PlayerFsmAutoWire.AutoWire();
        }
    }
}
