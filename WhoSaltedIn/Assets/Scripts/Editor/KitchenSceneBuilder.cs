using System.Collections.Generic;
using System.IO;
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
            var kitchen = CreateKitchen();

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
            player.transform.position = new Vector3(0f, 1.1f, -3.2f);

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

            var rig = player.AddComponent<PlayerRig>();
            Set(rig, "head", head);

            var interactor = player.AddComponent<Interactor>();
            Set(interactor, "runtime", runtime);
            Set(interactor, "head", head);

            return player;
        }

        static GameObject CreateKitchen()
        {
            var kitchen = new GameObject("Kitchen");

            Box(kitchen.transform, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(8f, 0.2f, 8f), Grey(0.35f));

            Box(kitchen.transform, "Wall North", new Vector3(0f, 1.5f, 4.1f), new Vector3(8f, 3f, 0.2f), Grey(0.5f));
            Box(kitchen.transform, "Wall South", new Vector3(0f, 1.5f, -4.1f), new Vector3(8f, 3f, 0.2f), Grey(0.5f));
            Box(kitchen.transform, "Wall East", new Vector3(4.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 8f), Grey(0.5f));
            Box(kitchen.transform, "Wall West", new Vector3(-4.1f, 1.5f, 0f), new Vector3(0.2f, 3f, 8f), Grey(0.5f));

            Box(kitchen.transform, "Counter North", new Vector3(0f, 0.45f, 3.4f), new Vector3(7f, 0.9f, 0.8f), Grey(0.45f));
            Box(kitchen.transform, "Counter East", new Vector3(3.4f, 0.45f, -1f), new Vector3(0.8f, 0.9f, 4f), Grey(0.45f));

            Station(kitchen.transform, "도마", new Vector3(-2.4f, 1.0f, 3.4f), new Vector3(0.9f, 0.15f, 0.7f),
                new Color(0.72f, 0.55f, 0.35f), 0, -1);

            Station(kitchen.transform, "물통", new Vector3(-0.6f, 1.15f, 3.4f), new Vector3(0.45f, 0.6f, 0.45f),
                new Color(0.35f, 0.6f, 0.85f), 1, -1);

            Station(kitchen.transform, "불 스위치", new Vector3(1.2f, 1.1f, 3.4f), new Vector3(0.35f, 0.35f, 0.35f),
                new Color(0.85f, 0.4f, 0.25f), 2, -1);

            Station(kitchen.transform, "냄비", new Vector3(2.6f, 1.2f, 3.4f), new Vector3(0.8f, 0.6f, 0.8f),
                new Color(0.55f, 0.55f, 0.6f), -1, 3);

            Station(kitchen.transform, "소금통", new Vector3(3.4f, 1.15f, -2.6f), new Vector3(0.4f, 0.5f, 0.4f),
                new Color(0.95f, 0.95f, 0.9f), 3, -1);

            var entrance = new GameObject("Entrance").transform;
            entrance.SetParent(kitchen.transform, false);
            entrance.localPosition = new Vector3(0f, 1.1f, -3.2f);
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
            Color color, int performStep, int tasteStep)
        {
            var go = Box(parent, name, position, size, color);

            var interactable = go.AddComponent<Interactable>();
            Set(interactable, "label", name);
            Set(interactable, "performStep", performStep);
            Set(interactable, "tasteStep", tasteStep);

            return go;
        }

        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);

            return go;
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
