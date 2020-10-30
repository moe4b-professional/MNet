﻿using System;
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
        public const int ExecutionOrder = NetworkEntity.ExecutionOrder - 50;

        [SerializeField]
        protected List<NetworkEntity> list = new List<NetworkEntity>();
        public List<NetworkEntity> List => list;

        public int Count => list.Count;

        public NetworkEntity this[int index] => list[index];

        public bool Find(int index, out NetworkEntity entity)
        {
            if (index < 0 || index >= Count)
            {
                entity = null;
                return false;
            }
            else
            {
                entity = list[index];
                return true;
            }
        }

        public Scene Scene => gameObject.scene;
        bool IsInSameScene(NetworkEntity entity) => entity.Scene == this.Scene;

        void FindAll()
        {
            list.Clear();

            var targets = FindObjectsOfType<NetworkEntity>().Where(IsInSameScene);

            list.AddRange(targets);
        }

        void Reset()
        {
            FindAll();
        }

        void Awake()
        {
            if(Application.isPlaying == false)
            {
                Register(this);
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) return;

                list[i].UpdateReadyState();
            }
        }

        void Start()
        {
            if (Application.isPlaying == false) return;

            if (NetworkAPI.Client.IsConnected && NetworkAPI.Client.IsReady)
                EvaluateSpawn();
            else
                NetworkAPI.Room.OnReady += RoomReadyCallback;
        }

        void RoomReadyCallback(ReadyClientResponse response)
        {
            NetworkAPI.Room.OnReady -= RoomReadyCallback;

            EvaluateSpawn();
        }

        #region Spawn
        void EvaluateSpawn()
        {
            if (NetworkAPI.Client.IsMaster == false) return;

            Spawn();
        }

        public void Spawn()
        {
            for (ushort i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;

                if (list[i].IsReady) continue;

                NetworkAPI.Client.SpawnSceneObject(Scene, i);
            }
        }
        #endregion

        public bool Add(NetworkEntity target)
        {
            int? vacancy = null;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) vacancy = i;

                if (list[i] == target) return false;
            }

            if (vacancy == null)
                list.Add(target);
            else
                list[vacancy.Value] = target;

            return true;
        }
        public bool Remove(NetworkEntity target) => list.Remove(target);

        void OnDestroy()
        {
            NetworkAPI.Room.OnReady -= RoomReadyCallback;

            Unregister(this);
        }

        public NetworkScene()
        {
            list = new List<NetworkEntity>();
        }

        //Static Utility
        static Dictionary<Scene, NetworkScene> collection;

        static void Register(NetworkScene component)
        {
            collection[component.Scene] = component;
        }
        static bool Unregister(NetworkScene component)
        {
            return collection.Remove(component.Scene);
        }

        static NetworkScene Create(Scene scene)
        {
            var gameObject = new GameObject("Network Scene");

            SceneManager.MoveGameObjectToScene(gameObject, scene);

            var component = gameObject.AddComponent<NetworkScene>();

            return component;
        }

        public static NetworkScene Get(int sceneIndex)
        {
            var scene = SceneManager.GetSceneByBuildIndex(sceneIndex);

            return Get(scene);
        }
        public static NetworkScene Get(Scene scene)
        {
            NetworkScene component;

            if (collection.TryGetValue(scene, out component)) return component;

            foreach (var target in FindObjectsOfType<NetworkScene>())
                Register(target);

            if (collection.TryGetValue(scene, out component)) return component;

            return null;
        }

        public static void Register(NetworkEntity entity)
        {
            var component = Get(entity.Scene);

            if (component == null) component = Create(entity.Scene);

            component.Add(entity);
        }
        public static void Unregister(NetworkEntity entity)
        {
            var component = Get(entity.Scene);

            if (entity.Scene.isLoaded == false) return;

            if (component == null)
                component = Create(entity.Scene);
            else
                component.Remove(entity);
        }

        static NetworkScene()
        {
            collection = new Dictionary<Scene, NetworkScene>();
        }

#if UNITY_EDITOR
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