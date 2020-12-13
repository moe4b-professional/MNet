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
        GameObject template = null;

        [SerializeField]
        RectTransform layout = null;

        List<GameServerUITemplate> templates = new List<GameServerUITemplate>();

        public override void Configure()
        {
            base.Configure();

            NetworkAPI.Server.Master.OnInfo += MasterInfoCallback;
        }

        public override void Init()
        {
            base.Init();

            Populate(NetworkAPI.Server.Game.Collection.Values);
        }

        void MasterInfoCallback(MasterServerInfoResponse info, RestError error)
        {
            Clear();

            if (error == null) Populate(info.Servers);
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
    }
}