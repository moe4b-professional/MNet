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

            ScrollToLast();
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
            Populate(NetworkAPI.Server.Game.Collection);

            Visible = NetworkAPI.Server.Game.Selection == null;
        }

        void Populate(ICollection<GameServerInfo> collection)
        {
            Clear();

            var entries = GameServerUITemplate.CreateAll(template, collection, InitTemplate);
            templates.AddRange(entries);

            ScrollToLast();
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

        public async UniTask Refresh()
        {
            Popup.Show("Retrieving Servers");

            MasterServerInfoResponse info;

            try
            {
                info = await NetworkAPI.Server.Master.GetInfo();
            }
            catch (UnityWebRequestException ex)
            {
                Debug.LogError(ex);
                Popup.Show("Failed To Retrieve Servers", "Okay").Forget();
                return;
            }

            if (info.Servers.Length == 0)
                Popup.Show("No Game Servers Found on Master", "Okay").Forget();
            else
                Popup.Hide();

            Populate(info.Servers);

            if (NetworkAPI.Server.Game.Selection == null) Show();
        }

        void ScrollToLast()
        {
            StartCoroutine(Procedure());
            IEnumerator Procedure()
            {
                for (int i = 0; i < 2; i++)
                {
                    scroll.verticalNormalizedPosition = 1f;

                    yield return new WaitForEndOfFrame();
                }
            }
        }

        void Clear()
        {
            templates.ForEach(GameServerUITemplate.Destroy);

            templates.Clear();
        }
    }
}