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

namespace MNet.Example
{
	public class RoomLogPanel : NetworkBehaviour
	{
        [SerializeField]
        GameObject template = default;

        [SerializeField]
        ScrollRect scroll = default;

        Queue<RoomLogUITemplate> stack = new Queue<RoomLogUITemplate>();

        [SerializeField]
        int max = 40;

        void Start()
        {
            NetworkAPI.Room.OnClientConnected += ClientConnectedCallback;
            NetworkAPI.Room.OnClientDisconnected += ClientDisconnectedCallback;

            NetworkAPI.Room.OnChangeMaster += ChangeMasterCallback;
        }

        #region Callbacks
        void ClientConnectedCallback(NetworkClient client)
        {
            var log = new RoomLog($"<b>{client.Name}</b> Connected");

            Add(log);
        }

        void ClientDisconnectedCallback(NetworkClientID id, NetworkClientProfile profile)
        {
            if (profile == null) return;

            var log = new RoomLog($"<b>{profile.Name}</b> Disconnected");

            Add(log);
        }

        void ChangeMasterCallback(NetworkClient client)
        {
            var log = new RoomLog($"<b>{client?.Name}</b> Set as Room Master");

            Add(log);
        }
        #endregion

        #region Chat
        public void SendChat(string text) => RPC(Chat, text);

        [NetworkRPC]
        void Chat(string text, RpcInfo info)
        {
            if (info.Sender == null) return;

            var log = new RoomLog($"<b>{info.Sender.Name}:</b> {text}");

            Add(log);
        }
        #endregion

        void Add(RoomLog log)
		{
            var instance = RoomLogUITemplate.Create(template, log);
            instance.SetParent(scroll.content);
            stack.Enqueue(instance);

            while (stack.Count > max) Release();

            ScrollToLast();
        }

        void Release()
        {
            var element = stack.Dequeue();
            scroll.content.localPosition -= Vector3.up * (element.Rect.sizeDelta.y + 10);
            Destroy(element.gameObject);
        }

        Coroutine coroutine;
        void ScrollToLast()
        {
            if (coroutine != null) StopCoroutine(coroutine);

            coroutine = StartCoroutine(Procedure());

            IEnumerator Procedure()
            {
                yield return new WaitForEndOfFrame();

                var initial = scroll.verticalNormalizedPosition;
                var final = 0f;

                var duration = 0.2f;
                var timer = duration;

                while (timer > 0f)
                {
                    timer = Mathf.MoveTowards(timer, 0f, Time.deltaTime);

                    scroll.verticalNormalizedPosition = Mathf.Lerp(final, initial, timer / duration);

                    yield return new WaitForEndOfFrame();
                }

                coroutine = null;
            }
        }

		void OnDestroy()
        {
            NetworkAPI.Room.OnClientConnected -= ClientConnectedCallback;
            NetworkAPI.Room.OnClientDisconnected -= ClientDisconnectedCallback;
            NetworkAPI.Room.OnChangeMaster -= ChangeMasterCallback;
        }
	}
}