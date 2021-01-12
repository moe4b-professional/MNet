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

using MNet;

namespace MNet.Example
{
    [RequireComponent(typeof(Button))]
	public class GameServerUITemplate : UITemplate<GameServerUITemplate, GameServerInfo>
	{
        [SerializeField]
        Text info = null;

        Button button;

        public override void Configure()
        {
            base.Configure();

            button = GetComponent<Button>();
        }

        public override void Init()
        {
            base.Init();

            button.onClick.AddListener(ClickAction);
        }

        public delegate void ClickDelegate(GameServerUITemplate template);
        public event ClickDelegate OnClick;
        void ClickAction()
        {
            OnClick?.Invoke(this);
        }

        public override void SetData(GameServerInfo data)
        {
            base.SetData(data);

            Rename($"{data.ID} | {data.Region}");
        }

        public override void UpdateState()
        {
            base.UpdateState();

            info.text = $"{Data.ID} @ {Data.Region} | {Data.Occupancy} Players";
        }
    }
}