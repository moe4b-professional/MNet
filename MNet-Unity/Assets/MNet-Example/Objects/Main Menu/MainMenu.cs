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

            if (NetworkAPI.Server.Game.HasSelection == false) GetMasterServerInfo();
        }

        void GetMasterServerInfo()
        {
            Popup.Show("Retrieving Servers");

            NetworkAPI.Server.Master.OnInfo += Callback;
            NetworkAPI.Server.Master.GetInfo();

            void Callback(MasterServerInfoResponse info, RestError error)
            {
                NetworkAPI.Server.Master.OnInfo -= Callback;

                if (error == null)
                {
                    if (info.Size > 0)
                        Popup.Hide();
                    else
                        Popup.Show("No Game Servers Found on Master", "Retry", GetMasterServerInfo);
                }
                else
                {
                    Popup.Show("Could not Retrieve Servers", "Retry", GetMasterServerInfo);
                }
            }
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
        }

        void OnDestroy()
        {
            NetworkAPI.Server.Master.OnInfo -= MasterServerInfoCallback;
            NetworkAPI.Client.OnDisconnect -= ClientDisconnectCallback;
        }
    }
}