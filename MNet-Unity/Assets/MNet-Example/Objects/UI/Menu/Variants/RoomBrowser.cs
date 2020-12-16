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
    public class RoomBrowser : UIMenu
    {
        [SerializeField]
        GameObject template = null;

        [SerializeField]
        ScrollRect scroll = default;

        List<RoomBasicUITemplate> templates = new List<RoomBasicUITemplate>();

        Core Core => Core.Instance;
        PopupPanel Popup => Core.UI.Popup;

        void Start()
        {
            NetworkAPI.Lobby.OnInfo += LobbyInfoCallback;
            NetworkAPI.Lobby.OnClear += Clear;
        }

        void OnEnable()
        {
            NetworkAPI.Lobby.OnGetInfo += GetLobbyInfoCallback;
        }

        void GetLobbyInfoCallback()
        {
            Popup.Show("Retrieving Rooms");
        }

        void LobbyInfoCallback(LobbyInfo lobby, RestError error)
        {
            if (error == null)
            {
                if (Visible && lobby.Size == 0)
                    Popup.Show($"Found {lobby.Size} Rooms", "Okay");
                else
                    Popup.Hide();

                Populate(lobby.Rooms);
            }
            else
            {
                if (Visible) Popup.Show("Failed To Retrieve Rooms", "Okay");

                Clear();
            }
        }

        void Populate(IList<RoomBasicInfo> list)
        {
            Clear();

            var entries = RoomBasicUITemplate.CreateAll(template, list, InitTemplate);
            templates.AddRange(entries);
        }

        void InitTemplate(RoomBasicUITemplate template, int index)
        {
            Initializer.Perform(template);

            template.SetParent(scroll.content);

            template.OnClick += TemplateClickCallback;
        }

        void TemplateClickCallback(RoomBasicUITemplate template)
        {
            Popup.Show("Joining Room");

            var info = template.Data;
            NetworkAPI.Room.Join(info);
        }

        void Clear()
        {
            templates.ForEach(RoomBasicUITemplate.Destroy);

            templates.Clear();
        }

        void OnDisable()
        {
            NetworkAPI.Lobby.OnGetInfo -= GetLobbyInfoCallback;
        }

        void OnDestroy()
        {
            NetworkAPI.Lobby.OnInfo -= LobbyInfoCallback;
            NetworkAPI.Lobby.OnClear -= Clear;
        }
    }
}