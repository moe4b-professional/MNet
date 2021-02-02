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

        #region Query
        List<QueryPredicate> queries;

        public delegate bool QueryPredicate(GameServerInfo info);

        public void AddQuery(QueryPredicate predicate)
        {
            queries.Add(predicate);

            UpdateQuery();
        }

        public void UpdateQuery()
        {
            for (int i = 0; i < templates.Count; i++)
                templates[i].Visible = Query(templates[i].Data);
        }

        bool Query(GameServerInfo info)
        {
            for (int i = 0; i < queries.Count; i++)
                if (queries[i](info) == false)
                    return false;

            return true;
        }

        public void RemoveQuery(QueryPredicate predicate)
        {
            queries.Remove(predicate);

            UpdateQuery();
        }
        #endregion

        Core Core => Core.Instance;
        PopupPanel Popup => Core.UI.Popup;

        void Awake()
        {
            templates = new List<GameServerUITemplate>();

            queries = new List<QueryPredicate>();
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