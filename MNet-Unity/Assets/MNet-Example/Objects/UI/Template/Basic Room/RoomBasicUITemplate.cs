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
    [RequireComponent(typeof(Button))]
    public class RoomBasicUITemplate : UITemplate<RoomBasicUITemplate, RoomInfo>
	{
        [SerializeField]
        Text title = default;

        [SerializeField]
        Text level = default;

        [SerializeField]
        Text capacity = default;

        Core Core => Core.Instance;

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

        public delegate void ClickDelegate(RoomBasicUITemplate template);
        public event ClickDelegate OnClick;
        void ClickAction()
        {
            OnClick?.Invoke(this);
        }

        public override void SetData(RoomInfo data)
        {
            base.SetData(data);

            Rename($"{data.Name} | {data.ID}");
        }

        public override void UpdateState()
        {
            base.UpdateState();

            title.text = Data.Name;
            level.text = Core.Levels.ReadAttribute(Data.Attributes).Name;
            capacity.text = $"{Data.Occupancy}/{Data.Capacity}";
        }
    }
}