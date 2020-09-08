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

namespace Backend
{
    [DefaultExecutionOrder(ExecutionOrder)]
	public class NetworkScene : MonoBehaviour
	{
        public const int ExecutionOrder = NetworkEntity.ExecutionOrder - 50;

        [SerializeField]
        protected List<NetworkEntity> objects;
        public List<NetworkEntity> Objects => objects;

        public Dictionary<NetworkEntity, int> Indexes { get; protected set; }
        public bool FindIndex(NetworkEntity target, out int index) => Indexes.TryGetValue(target, out index);

        public Dictionary<int, NetworkEntity> Types { get; protected set; }
        public bool FindObject(int index, out NetworkEntity target) => Types.TryGetValue(index, out target);

        public Scene Scene => gameObject.scene;

        void Reset()
        {
            GetAll();
        }

        void Awake()
        {
            collection[Scene.buildIndex] = this;

            Indexes = new Dictionary<NetworkEntity, int>(objects.Count);
            Types = new Dictionary<int, NetworkEntity>(objects.Count);

            for (int i = 0; i < objects.Count; i++)
            {
                var entity = objects[i];

                Indexes[entity] = i;
                Types[i] = entity;
            }
        }

        void Start()
        {
            if (NetworkAPI.Client.IsReady)
                EvaluateSpawn();
            else
                NetworkAPI.Room.OnReady += RoomReadyCallback;
        }

        void RoomReadyCallback(ReadyClientResponse response)
        {
            NetworkAPI.Room.OnReady -= RoomReadyCallback;

            EvaluateSpawn();
        }

        void EvaluateSpawn()
        {
            if (NetworkAPI.Client.IsMaster) SpawnAll();
        }
        public void SpawnAll()
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].IsReady) continue;

                NetworkAPI.Client.RequestSpawnSceneObject(Scene.buildIndex, i);
            }
        }

        public bool Register(NetworkEntity target)
        {
            int? vacancy = null;

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] == null) vacancy = i;

                if (objects[i] == target) return false;
            }

            if (vacancy == null)
                objects.Add(target);
            else
                objects[vacancy.Value] = target;

            return true;
        }
        
        public bool Remove(NetworkEntity target) => objects.Remove(target);

        void GetAll()
        {
            objects.Clear();

            var targets = FindObjectsOfType<NetworkEntity>().Where(x => x.Scene == Scene);

            objects.AddRange(targets);
        }

        void OnDestroy()
        {
            NetworkAPI.Room.OnReady -= RoomReadyCallback;
        }

        public NetworkScene()
        {
            objects = new List<NetworkEntity>();
        }

        //Static Utility
        static Dictionary<int, NetworkScene> collection;

        public static NetworkScene Get(Scene scene) => Get(scene.buildIndex);
        public static NetworkScene Get(int scene)
        {
            NetworkScene result;

            if (collection.TryGetValue(scene, out result)) return result;

            foreach (var target in FindObjectsOfType<NetworkScene>())
                collection[target.Scene.buildIndex] = target;

            if (collection.TryGetValue(scene, out result)) return result;

            return null;
        }

        static NetworkScene()
        {
            collection = new Dictionary<int, NetworkScene>();
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(NetworkScene))]
        class Inspector : Editor
        {
            new NetworkScene target;

            void OnEnable()
            {
                target = base.target as NetworkScene;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (GUILayout.Button("Get All")) target.GetAll();
            }
        }
#endif
    }
}