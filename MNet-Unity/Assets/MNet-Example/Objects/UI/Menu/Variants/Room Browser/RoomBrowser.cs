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
        GameObject template = default;

        [SerializeField]
        ScrollRect scroll = default;

        List<RoomBasicUITemplate> templates;

        Core Core => Core.Instance;
        PopupPanel Popup => Core.UI.Popup;
        TextInputPanel TextInput => Core.UI.TextInput;

        void Awake()
        {
            templates = new List<RoomBasicUITemplate>();
        }

        void Start()
        {
            NetworkAPI.Lobby.OnClear += Clear;
        }

        public void Refresh()
        {
            Popup.Show("Retrieving Rooms");

            NetworkAPI.Lobby.GetInfo(Callback);

            void Callback(LobbyInfo lobby, RestError error)
            {
                if (error == null)
                {
                    if (lobby.Size == 0)
                        Popup.Show($"No Rooms Found", "Okay");
                    else
                        Popup.Hide();

                    Populate(lobby.Rooms);
                }
                else
                {
                    Popup.Show("Failed To Retrieve Rooms", "Okay");

                    Clear();
                }
            }
        }

        void Populate(IList<RoomInfo> list)
        {
            Clear();

            if (list == null) return;

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
            if (template.Data.Locked)
            {
                TextInput.Show("Please Enter Password", Callback);

                TextInput.ContentType = InputField.ContentType.Password;

                void Callback(bool confirmed, string value)
                {
                    if (confirmed)
                        Join(template.Data, value);
                }
            }
            else
            {
                Join(template.Data);
            }
        }

        void Join(RoomInfo room, string password = null)
        {
            Popup.Show("Joining Room");

            NetworkAPI.Room.Join(room, password);
        }

        void Clear()
        {
            templates.ForEach(RoomBasicUITemplate.Destroy);

            templates.Clear();
        }

        void OnDestroy()
        {
            NetworkAPI.Lobby.OnClear -= Clear;
        }
    }
}