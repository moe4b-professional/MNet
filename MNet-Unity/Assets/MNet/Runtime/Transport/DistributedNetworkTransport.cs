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

using System.Threading;
using System.Collections.Concurrent;

using Utility = MNet.NetworkTransportUtility;

namespace MNet
{
    public abstract class DistributedNetworkTransport : NetworkTransport
    {
        public bool IsRegistered { get; protected set; }

        public RoomID Room { get; protected set; }

        public override void Connect(GameServerID server, RoomID room)
        {
            this.Room = room;

            IsRegistered = false;
        }

        protected virtual void RequestRegisteration()
        {
            var raw = BitConverter.GetBytes(Room.Value);

            Send(raw, DeliveryMode.Reliable);
        }

        protected virtual void ProcessRegistration(byte[] raw) => ProcessRegistration(raw[0]);
        protected virtual void ProcessRegistration(byte code)
        {
            IsRegistered = code == Utility.Registeration.Success;

            if (IsRegistered)
                QueueConnect();
            else
                Debug.LogError($"Registeration Failed, Recieved Code: {code} Instead of Code {Utility.Registeration.Success}");
        }

        protected virtual void ProcessMessage(byte[] raw, DeliveryMode mode)
        {
            if (IsRegistered)
            {
                var message = NetworkMessage.Read(raw);

                QueueMessage(message, mode);
            }
            else
            {
                ProcessRegistration(raw);
            }
        }

        protected override void QueueDisconnect(DisconnectCode code)
        {
            IsRegistered = false;

            base.QueueDisconnect(code);
        }

        public DistributedNetworkTransport() : base()
        {

        }
    }
}