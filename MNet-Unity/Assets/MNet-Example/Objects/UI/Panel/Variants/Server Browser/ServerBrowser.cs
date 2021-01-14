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
    public class ServerBrowser : UIPanel
    {
        [SerializeField]
        GameObject template = default;

        [SerializeField]
        GameObject panel = default;

        public override GameObject Target => panel;

        [SerializeField]
        ScrollRect scroll = default;

        List<GameServerUITemplate> templates;

        Core Core => Core.Instance;
        PopupPanel Popup => Core.UI.Popup;

        void Awake()
        {
            templates = new List<GameServerUITemplate>();
        }

        void Start()
        {
            NetworkAPI.Server.Master.OnInfo += MasterInfoCallback;

            Populate(NetworkAPI.Server.Game.Collection.Values);

            Visible = NetworkAPI.Server.Game.Selection == null;
        }

        void MasterInfoCallback(MasterServerInfoResponse info, RestError error)
        {
            if (error != null) return;

            Populate(info.Servers);

            if (NetworkAPI.Server.Game.Selection == null) Show();
        }

        public void Refresh()
        {
            Popup.Show("Retrieving Servers");

            NetworkAPI.Server.Master.GetInfo(Callback);

            void Callback(MasterServerInfoResponse info, RestError error)
            {
                if (error != null)
                {
                    Popup.Show("Failed To Retrieve Servers", "Okay");
                    return;
                }

                if (info.Servers.Length == 0)
                    Popup.Show("No Game Servers Found on Master", "Okay");
                else
                    Popup.Hide();
            }
        }

        void Populate(ICollection<GameServerInfo> collection)
        {
            Clear();

            var entries = GameServerUITemplate.CreateAll(template, collection, InitTemplate);
            templates.AddRange(entries);

            StartCoroutine(ScrollToStart());
            IEnumerator ScrollToStart()
            {
                for (int i = 0; i < 2; i++)
                {
                    scroll.verticalNormalizedPosition = 1f;

                    yield return new WaitForEndOfFrame();
                }
            }
        }

        void InitTemplate(GameServerUITemplate template, int index)
        {
            Initializer.Perform(template);

            template.SetParent(scroll.content);

            template.OnClick += TemplateClickCallback;
        }

        void TemplateClickCallback(GameServerUITemplate template)
        {
            NetworkAPI.Server.Game.Select(template.Data);

            Hide();
        }

        void Clear()
        {
            templates.ForEach(GameServerUITemplate.Destroy);

            templates.Clear();
        }

        void OnDestroy()
        {
            NetworkAPI.Server.Master.OnInfo -= MasterInfoCallback;
        }
    }
}