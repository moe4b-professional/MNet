using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MNet
{
    [ExecuteInEditMode]
    [DefaultExecutionOrder(ExecutionOrder)]
    [AddComponentMenu(Constants.Path + "Network Scene")]
    public class NetworkScene : MonoBehaviour
    {
        public const int ExecutionOrder = NetworkEntity.ExecutionOrder + -50;

        #region Locals
        [SerializeField]
        protected List<NetworkEntity> locals = new List<NetworkEntity>();
        public List<NetworkEntity> Locals => locals;

        public bool FindLocal(ushort index, out NetworkEntity entity)
        {
            if (index < 0 || index >= locals.Count)
            {
                entity = null;
                return false;
            }
            else
            {
                entity = locals[index];
                return true;
            }
        }

        public bool AddLocal(NetworkEntity target)
        {
            int? vacancy = null;

            for (int i = 0; i < locals.Count; i++)
            {
                if (locals[i] == null)
                {
                    vacancy = i;
                    break;
                }

                if (locals[i] == target) return false;
            }

            if (vacancy == null)
                locals.Add(target);
            else
                locals[vacancy.Value] = target;

            return true;
        }

        public bool RemoveLocal(NetworkEntity target)
        {
            return locals.Remove(target);
        }
        #endregion

        #region Dynamics
        public HashSet<NetworkEntity> Dynamics { get; protected set; }

        public void AddDynamic(NetworkEntity target)
        {
            SceneManager.MoveGameObjectToScene(target.gameObject, UnityScene);

            Dynamics.Add(target);

            Entities.Add(target);
        }

        public void RemoveDynamic(NetworkEntity target)
        {
            Dynamics.Remove(target);

            Entities.Remove(target);
        }
        #endregion

        public HashSet<NetworkEntity> Entities { get; protected set; }

        public Scene UnityScene => gameObject.scene;

        public byte Index { get; protected set; }

        bool IsInSameScene(NetworkEntity entity) => entity.UnityScene == this.UnityScene;

        void Reset()
        {
#if UNITY_EDITOR
            FindAll();
#endif
        }

        void Awake()
        {
            Index = (byte)UnityScene.buildIndex;

            Register(this);

            if (Application.isPlaying == false) return;

            Dynamics = new HashSet<NetworkEntity>();

            Entities = new HashSet<NetworkEntity>(locals);
        }

        void Start()
        {
            if (Application.isPlaying == false) return;

            if (NetworkAPI.Client.IsRegistered) EvaluateSpawn();
        }

        #region Spawn
        void EvaluateSpawn()
        {
            if (NetworkAPI.Client.IsMaster == false) return;

            Spawn();
        }

        public void Spawn()
        {
            for (ushort resource = 0; resource < locals.Count; resource++)
            {
                if (locals[resource] == null) continue;

                if (locals[resource].IsReady) continue;

                NetworkAPI.Room.Entities.SpawnSceneObject(resource, UnityScene);
            }
        }
        #endregion

        void OnDestroy()
        {
            Unregister(this);
        }

        public NetworkScene()
        {
            locals = new List<NetworkEntity>();
        }

        //Static Utility

        public static Dictionary<byte, NetworkScene> Dictionary { get; protected set; }
        public static List<NetworkScene> List { get; protected set; }

        public static int Count => Dictionary.Count;

        public static NetworkScene Active { get; protected set; }

        static void SelectActive()
        {
            Active = List.FirstOrDefault();
        }

        static void Register(NetworkScene scene)
        {
            Dictionary.Add(scene.Index, scene);

            List.Add(scene);

            if (Active == null) SelectActive();
        }

        public static bool Unregister(byte index)
        {
            if (Dictionary.TryGetValue(index, out var scene) == false) return false;

            return Unregister(scene);
        }
        public static bool Unregister(NetworkScene scene)
        {
            if (Dictionary.Remove(scene.Index) == false) return false;

            List.Remove(scene);

            if (scene == Active) SelectActive();

            if (NetworkAPI.Client.IsConnected)
                NetworkAPI.Room.Entities.DestroyInScene(scene);

            return true;
        }

        public static void UnregisterAll()
        {
            if (Application.isPlaying == false) throw new InvalidOperationException("Method can Only be Invoked when Application is Playing");

            for (int i = List.Count; i-- > 0;)
                Unregister(List[i]);
        }

        static NetworkScene Create(Scene scene)
        {
            var gameObject = new GameObject("Network Scene");

            SceneManager.MoveGameObjectToScene(gameObject, scene);

            var component = gameObject.AddComponent<NetworkScene>();

            return component;
        }

        public static bool Contains(byte index) => Dictionary.ContainsKey(index);

        public static bool TryGet(Scene uscene, out NetworkScene scene)
        {
            var index = (byte)uscene.buildIndex;

            return TryGet(index, out scene);
        }
        public static bool TryGet(byte index, out NetworkScene scene)
        {
            if (Dictionary.TryGetValue(index, out scene)) return true;

            if (Application.isPlaying) return false;

            foreach (var target in FindObjectsOfType<NetworkScene>())
                if (Contains(target.Index) == false)
                    Register(target);

            if (Dictionary.TryGetValue(index, out scene)) return true;

            return false;
        }

        public static NetworkEntity LocateEntity(byte index, ushort resource)
        {
            if(TryGet(index, out var scene) == false) throw new Exception($"Couldn't Find Scene with Index {index}");

            if (scene.FindLocal(resource, out var entity) == false)
                throw new Exception($"Couldn't Find NetworkBehaviour {resource} In Scene {index}");

            return entity;
        }

        public static void RegisterLocal(NetworkEntity entity)
        {
            if(TryGet(entity.UnityScene, out var scene) == false)
                scene = Create(entity.UnityScene);

            scene.AddLocal(entity);
        }
        public static void UnregisterLocal(NetworkEntity entity)
        {
            if (TryGet(entity.UnityScene, out var scene) == false)
                Create(entity.UnityScene);
            else
                scene.RemoveLocal(entity);
        }

        static NetworkScene()
        {
            Dictionary = new Dictionary<byte, NetworkScene>();
            List = new List<NetworkScene>();
        }

#if UNITY_EDITOR
        void FindAll()
        {
            locals.Clear();

            var targets = FindObjectsOfType<NetworkEntity>().Where(IsInSameScene);

            locals.AddRange(targets);

            EditorUtility.SetDirty(this);
        }

        [CustomEditor(typeof(NetworkScene))]
        class Inspector : UnityEditor.Editor
        {
            new NetworkScene target;

            const string HelpText = "This Component is Used to Register All Networked Objects in the Current Scene, " +
                "You Shouldn't Need to Interact With It Manually, just Add Your Objects Normally";

            void OnEnable()
            {
                target = base.target as NetworkScene;
            }

            public override void OnInspectorGUI()
            {
                EditorGUILayout.HelpBox(HelpText, MessageType.Info);

                EditorGUILayout.Space();

                base.OnInspectorGUI();

                EditorGUILayout.Space();

                if (GUILayout.Button("Find All")) target.FindAll();
            }
        }
#endif
    }
}