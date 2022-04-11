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