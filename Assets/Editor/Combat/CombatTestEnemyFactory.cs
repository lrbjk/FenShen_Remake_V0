using FenShen.Combat;
using FenShen.GameData;
using UnityEditor;
using UnityEngine;

namespace FenShen.EditorTools.Combat
{
    public static class CombatTestEnemyFactory
    {
        [MenuItem("GameObject/Combat/Create Test Enemy", false, 10)]
        public static void CreateTestEnemy(MenuCommand menuCommand)
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Combat Test Enemy";
            enemy.transform.position = ResolveSpawnPosition();

            Rigidbody rigidbody = enemy.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = enemy.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            enemy.AddComponent<TestEnemyRuntimeStatsComponent>();
            enemy.AddComponent<BuffController>();
            enemy.AddComponent<CombatHurtbox>();
            enemy.AddComponent<CombatTestEnemy>();

            GameObjectUtility.SetParentAndAlign(enemy, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(enemy, "Create Combat Test Enemy");
            Selection.activeObject = enemy;
        }

        private static Vector3 ResolveSpawnPosition()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return Vector3.zero;
            }

            Vector3 cameraPosition = sceneView.camera.transform.position;
            Vector3 forward = sceneView.camera.transform.forward;
            return cameraPosition + forward * 5f;
        }
    }
}
