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
    public class ServerSelectorPanel : UIPanel
    {
        [SerializeField]
        GameObject template = default;

        [SerializeField]
        GameObject panel = default;

        public override GameObject Target => panel;

        [SerializeField]
        RectTransform layout = default;

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

            if (NetworkAPI.Server.Game.Selection == null) GetMasterServerInfo();

            Populate(NetworkAPI.Server.Game.Collection.Values);
        }

        void GetMasterServerInfo()
        {
            Popup.Show("Retrieving Servers");

            NetworkAPI.Server.Master.GetInfo();
        }

        void MasterInfoCallback(MasterServerInfoResponse info, RestError error)
        {
            if (error != null)
            {
                Popup.Show("Could not Retrieve Servers", "Retry", GetMasterServerInfo);
                return;
            }

            Populate(info.Servers);

            if (info.Servers.Length > 0)
                Popup.Hide();
            else
                Popup.Show("No Game Servers Found on Master", "Retry", GetMasterServerInfo);

            if (NetworkAPI.Server.Game.Selection == null) Show();
        }

        void Populate(ICollection<GameServerInfo> collection)
        {
            Clear();

            var entries = GameServerUITemplate.CreateAll(template, collection, InitTemplate);
            templates.AddRange(entries);
        }

        void InitTemplate(GameServerUITemplate template, int index)
        {
            Initializer.Perform(template);

            template.SetParent(layout);

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