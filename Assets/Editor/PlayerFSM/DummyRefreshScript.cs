using UnityEditor;

public class DummyRefreshScript : EditorWindow
{
    [MenuItem("Window/DummyRefreshScript")]
    public static void ShowWindow()
    {
        GetWindow<DummyRefreshScript>("DummyRefreshScript");
    }
}

