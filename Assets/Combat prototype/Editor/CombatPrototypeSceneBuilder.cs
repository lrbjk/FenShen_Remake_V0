using FenShen.CombatPrototype;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FenShen.CombatPrototype.Editor
{
    public static class CombatPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Combat prototype/Scenes/CombatPrototypeScene.unity";
        private const string WhitePixelPath = "Assets/Combat prototype/PrototypeWhitePixel.png";
        private const string PlayerControllerPath = "Assets/Combat prototype/Art/Animation/Cyber Jinyiwei Player.controller";

        [InitializeOnLoadMethod]
        private static void AutoBuildPrototypeSceneOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                {
                    RebuildPrototypeScene();
                }
            };
        }

        [MenuItem("FenShen/Combat Prototype/Rebuild Prototype Scene")]
        public static void RebuildPrototypeScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CombatPrototypeScene";
            EnsurePrototypeAnimatorController();

            CreateCamera();
            CreateBand("Background", new Vector3(9f, 1.2f, 3f), new Vector2(24f, 10f), new Color(0.035f, 0.04f, 0.075f));
            CreateBand("Neon Skyline", new Vector3(9f, 2.4f, 2f), new Vector2(24f, 0.18f), new Color(0.1f, 0.85f, 1f, 0.45f));
            CreateBand("Magenta Data Rail", new Vector3(9f, 0.9f, 1.5f), new Vector2(24f, 0.08f), new Color(1f, 0.12f, 0.85f, 0.55f));

            CreateGround("Safe Test Floor", new Vector3(-5f, -2f, 0f), new Vector2(9f, 0.6f));
            CreateGround("Single Enemy Floor", new Vector3(5f, -2f, 0f), new Vector2(8f, 0.6f));
            CreateGround("Pressure Floor", new Vector3(15f, -2f, 0f), new Vector2(10f, 0.6f));
            CreateGround("Air Combo Platform", new Vector3(5.2f, 0.2f, 0f), new Vector2(3.2f, 0.35f));
            CreateGround("Upper Pressure Platform", new Vector3(15.8f, 0.55f, 0f), new Vector2(3.8f, 0.35f));

            CreateZoneLabel("Safe Test Area", new Vector3(-5f, 1.5f, 0f), new Color(0.2f, 1f, 0.85f));
            CreateZoneLabel("Single Enemy", new Vector3(5f, 1.5f, 0f), new Color(1f, 0.9f, 0.25f));
            CreateZoneLabel("Clone Pressure", new Vector3(15f, 1.5f, 0f), new Color(1f, 0.25f, 0.9f));

            CreatePlayer(new Vector3(-7.5f, -0.8f, 0f));
            CreateEnemy("Training Enemy", new Vector3(5.2f, -0.7f, 0f));
            CreateEnemy("Pressure Enemy A", new Vector3(13.3f, -0.7f, 0f));
            CreateEnemy("Pressure Enemy B", new Vector3(16.1f, -0.7f, 0f));
            CreateEnemy("Pressure Enemy C", new Vector3(18.4f, -0.7f, 0f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Combat prototype scene rebuilt at " + ScenePath);
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Prototype Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.2f;
            camera.backgroundColor = new Color(0.03f, 0.035f, 0.06f);
            cameraObject.transform.position = new Vector3(5f, 0.1f, -10f);
            cameraObject.tag = "MainCamera";
        }

        private static void CreatePlayer(Vector3 position)
        {
            GameObject player = CombatPrototypeFactory.CreateBody("Cyber Jinyiwei Player", position, new Vector2(0.58f, 1.35f), new Color(0.25f, 0.85f, 1f));
            ApplyPersistentSprite(player);
            player.AddComponent<Rigidbody2D>();
            PrototypeDamageable damageable = player.AddComponent<PrototypeDamageable>();
            damageable.team = PrototypeTeam.Player;
            damageable.maxHealth = 120f;
            damageable.currentHealth = 120f;
            damageable.destroyOnDeath = false;
            Animator animator = player.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerControllerPath);
            player.AddComponent<CombatPrototypeRuntime>();
        }

        private static void CreateEnemy(string name, Vector3 position)
        {
            GameObject enemy = CombatPrototypeFactory.CreateBody(name, position, new Vector2(0.66f, 1.28f), new Color(0.95f, 0.24f, 0.26f));
            ApplyPersistentSprite(enemy);
            Rigidbody2D body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            PrototypeDamageable damageable = enemy.AddComponent<PrototypeDamageable>();
            damageable.team = PrototypeTeam.Enemy;
            damageable.maxHealth = 80f;
            damageable.currentHealth = 80f;
            enemy.AddComponent<CombatPrototypeEnemy>();
        }

        private static void CreateGround(string name, Vector3 position, Vector2 size)
        {
            GameObject ground = CombatPrototypeFactory.CreateBody(name, position, size, new Color(0.12f, 0.14f, 0.18f));
            ApplyPersistentSprite(ground);
            Object.DestroyImmediate(ground.GetComponent<BoxCollider2D>());
            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
            renderer.color = new Color(0.14f, 0.16f, 0.2f);
        }

        private static void CreateBand(string name, Vector3 position, Vector2 size, Color color)
        {
            GameObject band = new GameObject(name);
            band.transform.position = position;
            band.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = band.AddComponent<SpriteRenderer>();
            renderer.sprite = GetOrCreateWhiteSpriteAsset();
            renderer.color = color;
            renderer.sortingOrder = -10;
        }

        private static void CreateZoneLabel(string text, Vector3 position, Color color)
        {
            GameObject label = new GameObject(text);
            label.transform.position = position;
            TextMesh mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = 0.22f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
        }

        private static void ApplyPersistentSprite(GameObject target)
        {
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = GetOrCreateWhiteSpriteAsset();
            }
        }

        private static Sprite GetOrCreateWhiteSpriteAsset()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePixelPath);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            File.WriteAllBytes(WhitePixelPath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(WhitePixelPath);

            TextureImporter importer = AssetImporter.GetAtPath(WhitePixelPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 1f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(WhitePixelPath);
        }

        private static void EnsurePrototypeAnimatorController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
            if (controller == null || controller.layers == null || controller.layers.Length == 0)
            {
                if (controller != null)
                {
                    AssetDatabase.DeleteAsset(PlayerControllerPath);
                }

                controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerControllerPath);
            }

            AddOrUpdateState(controller, "Idle", "Assets/Combat prototype/Art/Animation/Idle.anim", true);
            AddOrUpdateState(controller, "Move", "Assets/Combat prototype/Art/Animation/Move.anim", false);
            AddOrUpdateState(controller, "JumpStart", "Assets/Combat prototype/Art/Animation/JumpStart.anim", false);
            AddOrUpdateState(controller, "Fall", "Assets/Combat prototype/Art/Animation/Fall.anim", false);
            AddOrUpdateState(controller, "DoubleJump", "Assets/Combat prototype/Art/Animation/DoubleJump.anim", false);
            AddOrUpdateState(controller, "GroundDash", "Assets/Combat prototype/Art/Animation/GroundDash.anim", false);
            AddOrUpdateState(controller, "AirDash", "Assets/Combat prototype/Art/Animation/AirDash.anim", false);
            AddOrUpdateState(controller, "Landing", "Assets/Combat prototype/Art/Animation/Landing.anim", false);
            AddOrUpdateState(controller, "Attack01", "Assets/Combat prototype/Art/Animation/Attack01.anim", false);
            AddOrUpdateState(controller, "Attack02", "Assets/Combat prototype/Art/Animation/Attack02.anim", false);
            AddOrUpdateState(controller, "Attack03", "Assets/Combat prototype/Art/Animation/Attack03.anim", false);
            AddOrUpdateState(controller, "Attack04", "Assets/Combat prototype/Art/Animation/Attack04.anim", false);
            AddOrUpdateState(controller, "AttackUp", "Assets/Combat prototype/Art/Animation/AttackUp.anim", false);
            AddOrUpdateState(controller, "AirAttack01", "Assets/Combat prototype/Art/Animation/AirAttack01.anim", false);
            AddOrUpdateState(controller, "AirAttack02", "Assets/Combat prototype/Art/Animation/AirAttack02.anim", false);
            AddOrUpdateState(controller, "AirAttack03", "Assets/Combat prototype/Art/Animation/AirAttack03.anim", false);
            AddOrUpdateState(controller, "AirAttack04", "Assets/Combat prototype/Art/Animation/AirAttack04.anim", false);
            AddOrUpdateState(controller, "SpAttack", "Assets/Combat prototype/Art/Animation/SpAttack.anim", false);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static void AddOrUpdateState(AnimatorController controller, string stateName, string clipPath, bool isDefault)
        {
            if (controller == null || controller.layers == null || controller.layers.Length == 0)
            {
                return;
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                Debug.LogWarning("Missing combat prototype animation clip: " + clipPath);
                return;
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = null;
            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                {
                    state = states[i].state;
                    break;
                }
            }

            if (state == null)
            {
                state = stateMachine.AddState(stateName);
            }

            state.motion = clip;
            state.writeDefaultValues = true;

            if (isDefault)
            {
                stateMachine.defaultState = state;
            }
        }
    }
}
