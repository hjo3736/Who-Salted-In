using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Net;
using Game.Unity;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static class KitchenSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Kitchen.unity";
        const string MaterialFolder = "Assets/Materials";

        [MenuItem("Who Salted In/Build Kitchen Scene")]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build Kitchen Scene",
                    ScenePath + " 를 새로 만듭니다. 기존 파일이 있으면 덮어씁니다.",
                    "만들기", "취소"))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLight();
            var lobbyCamera = CreateLobbyCamera();
            CreateNetworkManager();

            var match = CreateMatchObject();
            var player = CreatePlayer(match);
            var kitchen = CreateKitchen(match);

            WirePresence(match, kitchen, player, lobbyCamera);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();

            StampSceneNetworkIds();

            Debug.Log("[Builder] " + ScenePath + " 생성 완료");
        }

        static void StampSceneNetworkIds()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var match = GameObject.Find("Match");

            if (match == null)
            {
                Debug.LogError("[Builder] Match 오브젝트를 찾지 못했다");
                return;
            }

            var networkObject = match.GetComponent<NetworkObject>();
            var serialized = new SerializedObject(networkObject);
            var hash = serialized.FindProperty("GlobalObjectIdHash");

            if (hash == null || hash.uintValue == 0u)
            {
                Debug.LogError("[Builder] GlobalObjectIdHash 가 비어 있다. 씬을 다시 열고 Match 를 눌러 인스펙터를 갱신할 것");
                return;
            }

            EditorUtility.SetDirty(networkObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("[Builder] in-scene NetworkObject id = " + hash.uintValue);
        }

        static void CreateLight()
        {
            var go = new GameObject("Directional Light");
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
        }

        static Camera CreateLobbyCamera()
        {
            var go = new GameObject("LobbyCamera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 2f, -12f);

            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            go.AddComponent<AudioListener>();

            return camera;
        }

        static void CreateNetworkManager()
        {
            var go = new GameObject("NetworkManager");
            var manager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();

            if (manager.NetworkConfig == null)
            {
                manager.NetworkConfig = new NetworkConfig();
            }

            manager.NetworkConfig.NetworkTransport = transport;
        }

        static GameObject CreateMatchObject()
        {
            var go = new GameObject("Match");
            go.AddComponent<NetworkObject>();

            var runtime = go.AddComponent<MatchRuntime>();
            var kitchenDriver = go.AddComponent<KitchenDriver>();
            var turnDriver = go.AddComponent<TurnDriver>();
            var voice = go.AddComponent<VoiceCheck>();
            var session = go.AddComponent<SessionCheck>();
            go.AddComponent<KitchenPresence>();

            Set(runtime, "kitchen", kitchenDriver);
            Set(kitchenDriver, "runtime", runtime);
            Set(turnDriver, "runtime", runtime);
            Set(turnDriver, "voice", voice);
            Set(session, "voice", voice);

            return go;
        }

        static GameObject CreatePlayer(GameObject match)
        {
            var runtime = match.GetComponent<MatchRuntime>();

            var player = new GameObject("Player");
            player.transform.position = new Vector3(1.2f, 1.1f, -2.4f);

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var head = new GameObject("Head").transform;
            head.SetParent(player.transform, false);
            head.localPosition = new Vector3(0f, 1.6f, 0f);

            var cameraGo = new GameObject("PlayerCamera");
            cameraGo.transform.SetParent(head, false);

            var camera = cameraGo.AddComponent<Camera>();
            camera.fieldOfView = 90f;
            camera.nearClipPlane = 0.05f;

            var hand = new GameObject("Hand").transform;
            hand.SetParent(head, false);
            hand.localPosition = new Vector3(0.32f, -0.28f, 0.55f);

            var rig = player.AddComponent<PlayerRig>();
            Set(rig, "head", head);

            var interactor = player.AddComponent<Interactor>();
            Set(interactor, "runtime", runtime);
            Set(interactor, "head", head);
            Set(interactor, "hand", hand);

            return player;
        }

        static GameObject CreateKitchen(GameObject match)
        {
            var kitchen = new GameObject("Kitchen");
            var t = kitchen.transform;

            Box(t, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(8f, 0.2f, 8f), Grey(0.35f));
            Box(t, "Pantry Floor", new Vector3(-6.8f, -0.1f, 0f), new Vector3(5.5f, 0.2f, 8f), Grey(0.3f));

            Box(t, "Wall North", new Vector3(-1.8f, 1.5f, 4.1f), new Vector3(11.5f, 3f, 0.2f), Grey(0.5f));
            Box(t, "Wall South", new Vector3(-1.8f, 1.5f, -4.1f), new Vector3(11.5f, 3f, 0.2f), Grey(0.5f));
            Box(t, "Wall East", new Vector3(4.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 8f), Grey(0.5f));
            Box(t, "Wall Far West", new Vector3(-9.6f, 1.5f, 0f), new Vector3(0.2f, 3f, 8f), Grey(0.5f));

            Box(t, "Divider North", new Vector3(-4.1f, 1.5f, 2.7f), new Vector3(0.2f, 3f, 2.8f), Grey(0.45f));
            Box(t, "Divider South", new Vector3(-4.1f, 1.5f, -2.7f), new Vector3(0.2f, 3f, 2.8f), Grey(0.45f));

            Box(t, "Counter North", new Vector3(0.4f, 0.45f, 3.4f), new Vector3(6.5f, 0.9f, 0.8f), Grey(0.45f));
            Box(t, "Tool Shelf", new Vector3(3.4f, 0.95f, -0.5f), new Vector3(0.8f, 0.15f, 5f), Grey(0.55f));
            Box(t, "Spice Shelf", new Vector3(-1.5f, 0.95f, -3.6f), new Vector3(3f, 0.15f, 0.7f), Grey(0.55f));
            Box(t, "Pantry Shelf", new Vector3(-8.6f, 0.95f, 0f), new Vector3(1.4f, 0.15f, 5f), Grey(0.5f));

            var counter = Station(t, "조리대", new Vector3(-0.6f, 0.95f, 3.4f), new Vector3(1.6f, 0.05f, 0.7f),
                Grey(0.6f), new[] { 0 }, -1, 1.0f);

            AddSlot(counter.transform, "Board Slot", new Vector3(0f, 0.09f, 0f),
                new[] { KitchenItems.Board });

            var stove = Station(t, "화구", new Vector3(2.4f, 0.95f, 3.4f), new Vector3(1.0f, 0.06f, 0.7f),
                new Color(0.3f, 0.3f, 0.32f), new[] { 1, 3, 4, 5 }, 5, 1.0f);

            AddSlot(stove.transform, "Pot Slot", new Vector3(0f, 0.26f, 0f),
                new[] { KitchenItems.Pot });

            Station(t, "싱크대", new Vector3(-2.8f, 0.95f, 3.4f), new Vector3(1.2f, 0.06f, 0.7f),
                new Color(0.62f, 0.66f, 0.7f), new[] { 2 }, -1, 1.0f);

            var pot = Item(t, "냄비", KitchenItems.Pot, new Vector3(3.4f, 1.2f, 1.4f),
                new Vector3(0.7f, 0.45f, 0.7f), new Color(0.5f, 0.5f, 0.55f));

            var water = Inner(pot.transform, "Water", new Vector3(0f, -0.08f, 0f),
                new Vector3(0.6f, 0.1f, 0.6f), new Color(0.45f, 0.7f, 0.9f));

            var onion = Inner(pot.transform, "Onion", new Vector3(0f, 0.08f, 0f),
                new Vector3(0.35f, 0.06f, 0.35f), new Color(0.6f, 0.85f, 0.4f));

            var contents = pot.AddComponent<PotContents>();
            Set(contents, "runtime", match.GetComponent<MatchRuntime>());
            Set(contents, "water", water.transform);
            Set(contents, "onion", onion.transform);
            Set(contents, "waterStep", 3);
            Set(contents, "onionStep", 1);

            var board = Item(t, "도마", KitchenItems.Board, new Vector3(3.4f, 1.1f, 0.4f),
                new Vector3(0.7f, 0.12f, 0.55f), new Color(0.72f, 0.55f, 0.35f));

            AddSlot(board.transform, "Onion Slot", new Vector3(0f, 0.15f, 0f),
                new[] { KitchenItems.Onion, KitchenItems.ChoppedOnion });

            Item(t, "칼", KitchenItems.Knife, new Vector3(3.4f, 1.08f, -0.4f),
                new Vector3(0.12f, 0.06f, 0.55f), new Color(0.8f, 0.82f, 0.85f));

            Item(t, "국자", KitchenItems.Ladle, new Vector3(3.4f, 1.08f, -1.2f),
                new Vector3(0.14f, 0.08f, 0.6f), new Color(0.75f, 0.7f, 0.5f));

            Item(t, "물병", KitchenItems.WaterJug, new Vector3(3.4f, 1.25f, -2.2f),
                new Vector3(0.28f, 0.45f, 0.28f), new Color(0.4f, 0.65f, 0.88f));

            Item(t, "소금통", KitchenItems.SaltShaker, new Vector3(-0.6f, 1.2f, -3.6f),
                new Vector3(0.22f, 0.3f, 0.22f), new Color(0.95f, 0.95f, 0.92f));

            Item(t, "설탕", KitchenItems.SugarJar, new Vector3(-1.5f, 1.2f, -3.6f),
                new Vector3(0.24f, 0.3f, 0.24f), new Color(0.9f, 0.86f, 0.78f));

            Item(t, "간장", KitchenItems.SoySauce, new Vector3(-2.4f, 1.25f, -3.6f),
                new Vector3(0.2f, 0.4f, 0.2f), new Color(0.3f, 0.2f, 0.15f));

            Item(t, "파", KitchenItems.Onion, new Vector3(-8.6f, 1.15f, 1.4f),
                new Vector3(0.16f, 0.16f, 0.6f), new Color(0.85f, 0.95f, 0.6f));

            Item(t, "양파", KitchenItems.Shallot, new Vector3(-8.6f, 1.2f, 0.2f),
                new Vector3(0.3f, 0.3f, 0.3f), new Color(0.85f, 0.7f, 0.5f));

            Item(t, "쌀", KitchenItems.Rice, new Vector3(-8.6f, 1.2f, -1.0f),
                new Vector3(0.35f, 0.3f, 0.25f), new Color(0.95f, 0.94f, 0.88f));

            var entrance = new GameObject("Entrance").transform;
            entrance.SetParent(t, false);
            entrance.localPosition = new Vector3(1.2f, 1.1f, -2.4f);
            entrance.localRotation = Quaternion.identity;

            return kitchen;
        }

        static void WirePresence(GameObject match, GameObject kitchen, GameObject player, Camera lobbyCamera)
        {
            var presence = match.GetComponent<KitchenPresence>();

            Set(presence, "runtime", match.GetComponent<MatchRuntime>());
            Set(presence, "kitchenRoot", kitchen);
            Set(presence, "playerRoot", player);
            Set(presence, "entrance", kitchen.transform.Find("Entrance"));
            Set(presence, "lobbyCamera", lobbyCamera);
        }

        static GameObject Station(
            Transform parent, string name, Vector3 position, Vector3 size,
            Color color, int[] performSteps, int tasteStep, float nearbyRadius)
        {
            var go = Box(parent, name, position, size, color);

            var interactable = go.AddComponent<Interactable>();
            Set(interactable, "label", name);
            SetInts(interactable, "performSteps", performSteps);
            Set(interactable, "tasteStep", tasteStep);
            SetFloat(interactable, "nearbyRadius", nearbyRadius);

            return go;
        }

        static GameObject Item(Transform parent, string name, string itemId, Vector3 position, Vector3 size, Color color)
        {
            var go = Box(parent, name, position, size, color);

            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.5f;
            body.interpolation = RigidbodyInterpolation.None;

            var holdable = go.AddComponent<Holdable>();
            Set(holdable, "key", itemId);
            Set(holdable, "itemId", itemId);
            Set(holdable, "label", name);

            return go;
        }

        static void SetFloat(Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInts(Object target, string field, int[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);

            property.arraySize = values.Length;

            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).intValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSlot(Transform parent, string name, Vector3 localPosition, string[] accepts)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            var slot = go.AddComponent<Slot>();
            SetStrings(slot, "accepts", accepts);
        }

        static void SetStrings(Object target, string field, string[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);

            property.arraySize = values.Length;

            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject Inner(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.SetActive(false);

            return go;
        }

        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = size;
            visual.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);

            return root;
        }

        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        static Color Grey(float value)
        {
            return new Color(value, value, value);
        }

        static Material MaterialFor(Color color)
        {
            var key = ColorUtility.ToHtmlStringRGB(color);

            if (materials.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            Directory.CreateDirectory(MaterialFolder);

            var path = MaterialFolder + "/Kit_" + key + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");

                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                existing = new Material(shader);
                existing.SetColor("_BaseColor", color);
                existing.color = color;
                AssetDatabase.CreateAsset(existing, path);
            }

            materials[key] = existing;
            return existing;
        }

        static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            if (scenes.Exists(s => s.path == ScenePath))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void Set(Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);

            if (property == null)
            {
                Debug.LogError("[Builder] " + target.GetType().Name + " 에 " + field + " 필드가 없다");
                return;
            }

            if (value is Object reference)
            {
                property.objectReferenceValue = reference;
            }
            else if (value is int number)
            {
                property.intValue = number;
            }
            else if (value is string text)
            {
                property.stringValue = text;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
