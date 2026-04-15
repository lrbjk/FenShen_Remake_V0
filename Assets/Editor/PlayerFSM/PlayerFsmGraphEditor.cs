using UnityEditor;

public class PlayerFsmGraphEditor : EditorWindow
{
    [MenuItem("Window/PlayerFsmGraphEditor")]
    public static void ShowWindow()
    {
        GetWindow<PlayerFsmGraphEditor>("PlayerFsmGraphEditor");
    }
}

