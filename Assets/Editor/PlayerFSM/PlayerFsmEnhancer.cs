using UnityEditor;
using UnityEngine;
using FenShen.PlayerFSM;

public static class PlayerFsmEnhancer
{
    [MenuItem("Tools/Player FSM/Hard Clean & Re-Wire")]
    public static void HardCleanAndWire()
    {
        var graph = AssetDatabase.LoadAssetAtPath<FsmGraphSO>("Assets/Settings/PlayerFSM/PlayerFsmGraph.asset");
        if (graph == null) return;

        // 1. 自动清理列表中的 Missing 引用
        graph.states.RemoveAll(s => s == null);
        graph.transitions.RemoveAll(t => t == null);

        // 2. 重新触发 AutoWire
        PlayerFsmAutoWire.AutoWire();

        Debug.Log("FSM: Cleaned missing assets and re-wired.");
    }
}

