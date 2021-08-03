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
using MB;
using UnityEngine.PlayerLoop;

namespace MNet
{
    public static class FluentObjectRecord
    {
#if UNITY_EDITOR || DEBUG
        public static Dictionary<IInterface, float> Dictionary { get; private set; }

        public static class Time
        {
            public static float Current => UnityEngine.Time.time;

            //Time in Seconds
            public static float Limit { get; set; } = 2f;
        }

        static List<IInterface> Removals;
        static void Update()
        {
            foreach (var pair in Dictionary)
            {
                var stamp = pair.Value;

                if (Time.Current > stamp + Time.Limit)
                {
                    var item = pair.Key;

                    Debug.LogWarning($"Fluent Object '{item.GetType().Name}->{item}' Not Consumed? " +
                        $"Don't Forget to Send RPCs and SyncVar commands");

                    Removals.Add(item);
                }
            }

            foreach (var item in Removals)
                Remove(item);

            Removals.Clear();
        }
#endif

        public interface IInterface { }

        public static void Add<T>(T item)
            where T : class, IInterface
        {
#if UNITY_EDITOR || DEBUG
            Dictionary.Add(item, Time.Current);
#endif
        }

        public static bool Remove<T>(T item)
            where T : class, IInterface
        {
#if UNITY_EDITOR || DEBUG
            return Dictionary.Remove(item);
#else
            return false;
#endif
        }

#if UNITY_EDITOR || DEBUG
        static FluentObjectRecord()
        {
            Dictionary = new Dictionary<IInterface, float>();
            Removals = new List<IInterface>();

            MUtility.RegisterPlayerLoop<Update>(Update);
        }
#endif
    }

    public interface IDeliveryModeConstructor<TSelf>
    {
        /// <summary>
        /// Set Message Delivery Mode
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        TSelf Delivery(DeliveryMode value);
    }

    public interface IChannelConstructor<TSelf>
    {
        /// <summary>
        /// Set Channel to Send Message On
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        TSelf Channel(byte value);
    }

    public interface INetworkGroupConstructor<TSelf>
    {
        /// <summary>
        /// Set Group to Recieve Message
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        TSelf Group(NetworkGroupID value);
    }

    public interface IRemoteBufferModeConstructor<TSelf>
    {
        /// <summary>
        /// Set Remote Buffering Mode for Message
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        TSelf Buffer(RemoteBufferMode value);
    }

    public interface INetworkClientExceptionConstructor<TSelf>
    {
        /// <summary>
        /// Set a Network Client to not Recieve to this Message
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        TSelf Exception(NetworkClient value);
    }
}