using UnityEditor;

namespace FenShen.PlayerFSM
{
    public static class PlayerFsmToolsMenu
    {
        [MenuItem("Tools / Player FSM / Auto Wire & Bind")]
        public static void AutoWireFromTools()
        {
            PlayerFsmAutoWire.AutoWire();
        }
    }
}
