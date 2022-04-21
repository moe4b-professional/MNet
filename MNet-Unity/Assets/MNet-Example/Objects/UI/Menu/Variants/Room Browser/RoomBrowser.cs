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
using Cysharp.Threading.Tasks;

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

        public async UniTask Refresh()
        {
            Popup.Show("Retrieving Rooms");

            LobbyInfo info;

            try
            {
                info = await NetworkAPI.Lobby.GetInfo();
            }
            catch (Exception ex) when (ex is UnityWebRequestException)
            {
                Popup.Show("Failed To Retrieve Rooms", "Okay").Forget();
                Clear();
                return;
            }

            if (info.Size == 0)
                Popup.Show($"No Rooms Found", "Okay").Forget();
            else
                Popup.Hide();

            Populate(info.Rooms);
        }

        void Populate(IList<RoomInfo> list)
        {
            Clear();

            if (list == null) return;

            var entries = RoomBasicUITemplate.CreateAll(template, list, Init);
            templates.AddRange(entries);

            void Init(RoomBasicUITemplate template, int index)
            {
                Initializer.Perform(template);

                template.SetParent(scroll.content);

                template.OnClick += TemplateClickCallback;
            }
        }

        void TemplateClickCallback(RoomBasicUITemplate template)
        {
            if (template.Data.Locked)
            {
                TextInput.Show("Please Enter Password", Callback);

                TextInput.ContentType = InputField.ContentType.Password;
                TextInput.CharacterLimit = FixedString16.MaxSize;

                void Callback(bool confirmed, string value)
                {
                    if (confirmed)
                        Join(template.Data.ID, value);
                }
            }
            else
            {
                Join(template.Data.ID);
            }
        }

        void Clear()
        {
            templates.ForEach(RoomBasicUITemplate.Destroy);

            templates.Clear();
        }

        void Join(RoomID id, string password = null)
        {
            Popup.Show("Joining Room");

            NetworkAPI.Room.Join(id, Core.Network.Profile, new FixedString16(password));
        }

        void OnDestroy()
        {
            NetworkAPI.Lobby.OnClear -= Clear;
        }
    }
}