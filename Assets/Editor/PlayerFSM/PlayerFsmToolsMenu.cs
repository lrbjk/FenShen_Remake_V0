using UnityEditor;
using UnityEngine;

namespace FenShen.PlayerFSM
{
    public static class PlayerFsmToolsMenu
    {
        [MenuItem("Tools / Player FSM / Auto Wire & Bind")]
        public static void AutoWireFromTools()
        {
            PlayerFsmAutoWire.AutoWire();
        }

        [MenuItem("Tools / Player FSM / Cleanup Orphan Assets")]
        public static void CleanupOrphanAssets()
        {
            FsmGraphSO graph = AssetDatabase.LoadAssetAtPath<FsmGraphSO>("Assets/Settings/PlayerFSM/PlayerFsmGraph.asset");
            if (graph == null)
            {
                Debug.LogWarning("Player FSM graph not found.");
                return;
            }

            PlayerFsmAssetUtility.CleanupOrphanAssets(graph);
            Debug.Log("Player FSM: orphan transition and condition assets cleaned.");
        }
    }
}
