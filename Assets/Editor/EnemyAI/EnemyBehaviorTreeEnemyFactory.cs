using FenShen.Combat;
using FenShen.EnemyAI;
using FenShen.EnemyAI.BehaviorTree;
using FenShen.EnemyAI.Combat;
using FenShen.GameData;
using UnityEditor;
using UnityEngine;

namespace FenShen.EditorTools.EnemyAI
{
    public static class EnemyBehaviorTreeEnemyFactory
    {
        [MenuItem("GameObject/Enemy AI/Create Behavior Tree Enemy", false, 10)]
        public static void CreateEnemy(MenuCommand menuCommand)
        {
            GameObject enemy = new GameObject("Behavior Tree Enemy");
            enemy.name = "Behavior Tree Enemy";
            enemy.transform.position = ResolveSpawnPosition();
            AssignLayerIfExists(enemy, "Enemy");

            SpriteRenderer spriteRenderer = enemy.AddComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(0.9f, 0.25f, 0.2f, 1f);
            spriteRenderer.sortingOrder = 5;

            CapsuleCollider2D collider2D = enemy.AddComponent<CapsuleCollider2D>();
            collider2D.size = new Vector2(1f, 2f);
            collider2D.direction = CapsuleDirection2D.Vertical;
            collider2D.offset = new Vector2(0f, 1f);

            Rigidbody2D body2D = enemy.GetComponent<Rigidbody2D>();
            if (body2D == null)
            {
                body2D = enemy.AddComponent<Rigidbody2D>();
            }

            body2D.gravityScale = 0f;
            body2D.freezeRotation = true;
            body2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            EnemyRuntimeStatsComponent stats = enemy.GetComponent<EnemyRuntimeStatsComponent>();
            if (stats == null)
            {
                stats = enemy.AddComponent<EnemyRuntimeStatsComponent>();
            }

            BuffController buffController = enemy.GetComponent<BuffController>();
            if (buffController == null)
            {
                buffController = enemy.AddComponent<BuffController>();
            }

            CombatHurtbox hurtbox = enemy.GetComponent<CombatHurtbox>();
            if (hurtbox == null)
            {
                hurtbox = enemy.AddComponent<CombatHurtbox>();
            }

            EnemyCombatSkillController combat = enemy.GetComponent<EnemyCombatSkillController>();
            if (combat == null)
            {
                combat = enemy.AddComponent<EnemyCombatSkillController>();
            }

            EnemyBehaviorTreeAgent agent = enemy.GetComponent<EnemyBehaviorTreeAgent>();
            if (agent == null)
            {
                agent = enemy.AddComponent<EnemyBehaviorTreeAgent>();
            }

            EnemyBehaviorTreeActor actor = enemy.GetComponent<EnemyBehaviorTreeActor>();
            if (actor == null)
            {
                actor = enemy.AddComponent<EnemyBehaviorTreeActor>();
            }

            combat.character = enemy.transform;
            combat.body2D = body2D;
            combat.runtimeStats = stats;
            combat.team = CombatTeam.Enemy;
            agent.body = enemy.transform;
            agent.body2D = body2D;
            agent.animator = enemy.GetComponentInChildren<Animator>();
            int playerLayerMask = LayerMask.GetMask("Player");
            if (playerLayerMask != 0)
            {
                agent.targetLayers = playerLayerMask;
            }

            hurtbox.Configure(CombatTeam.Enemy, stats, enemy.transform);

            GameObjectUtility.SetParentAndAlign(enemy, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(enemy, "Create Behavior Tree Enemy");
            Selection.activeObject = enemy;
        }

        private static void AssignLayerIfExists(GameObject gameObject, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
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
