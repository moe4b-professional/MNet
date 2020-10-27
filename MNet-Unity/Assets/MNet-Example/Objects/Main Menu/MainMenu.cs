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
    public class MainMenu : MonoBehaviour
    {
        [SerializeField]
        ServerSelectorPanel serverSelector = default;

        Core Core => Core.Instance;
        PopupPanel Popup => Core.UI.Popup;

        void Start()
        {
            NetworkAPI.Server.Master.OnInfo += MasterServerInfoCallback;
            NetworkAPI.Client.OnDisconnect += ClientDisconnectCallback;
        }

        void ClientDisconnectCallback(DisconnectCode code)
        {
            Popup.Show($"Client Disconnected{Environment.NewLine}Reason: {code}", "Okay");
        }

        void MasterServerInfoCallback(MasterServerInfoResponse info, RestError error)
        {
            if (error == null)
            {
                if (NetworkAPI.Server.Game.HasSelection == false)
                    serverSelector.Show();
            }
            else
            {

            }
        }

        void OnDestroy()
        {
            Debug.Log("Destory");

            NetworkAPI.Server.Master.OnInfo -= MasterServerInfoCallback;
            NetworkAPI.Client.OnDisconnect -= ClientDisconnectCallback;
        }
    }
}