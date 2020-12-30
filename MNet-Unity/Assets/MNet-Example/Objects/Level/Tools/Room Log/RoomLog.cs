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
	public class RoomLog : NetworkBehaviour
	{
        void Start()
        {
            if (NetworkAPI.Client.IsConnected)
                Add("You Connected");

            NetworkAPI.Room.OnClientConnected += ClientConnectedCallback;
            NetworkAPI.Room.OnClientDisconnected += ClientDisconnectedCallback;

            NetworkAPI.Room.OnChangeMaster += ChangeMasterCallback;
        }

        #region Callbacks
        void ClientConnectedCallback(NetworkClient client)
        {
            Add($"<b>{client.Name}</b> Connected");
        }

        void ClientDisconnectedCallback(NetworkClient client)
        {
            if (client == null) return;

            Add($"<b>{client.Name}</b> Disconnected");
        }

        void ChangeMasterCallback(NetworkClient client)
        {
            Add($"<b>{client?.Name}</b> Set as Room Master");
        }
        #endregion

        #region Chat
        public void Broadcast(string text) => BroadcastRPC(Recieve, text);

        [NetworkRPC]
        void Recieve(string text, RpcInfo info)
        {
            if (info.Sender == null) return;

            var log = new Entry($"<b>{info.Sender.Name}:</b> {text}");

            Add(log);
        }
        #endregion

        public delegate void AddDelegate(Entry entry);
        public event AddDelegate OnAdd;
        void Add(string text)
        {
            var log = new Entry(text);

            Add(log);
        }
        void Add(Entry log)
        {
            OnAdd?.Invoke(log);
        }

        void OnDestroy()
        {
            NetworkAPI.Room.OnClientConnected -= ClientConnectedCallback;
            NetworkAPI.Room.OnClientDisconnected -= ClientDisconnectedCallback;
            NetworkAPI.Room.OnChangeMaster -= ChangeMasterCallback;
        }

        public struct Entry
        {
            public string Text { get; private set; }

            public Color Color { get; private set; }

            public Entry(string text, Color color)
            {
                this.Text = text;
                this.Color = color;
            }
            public Entry(string text) : this(text, Color.white) { }
        }
    }
}