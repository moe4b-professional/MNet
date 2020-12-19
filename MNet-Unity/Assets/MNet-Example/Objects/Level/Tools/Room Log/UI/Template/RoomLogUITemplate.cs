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
	public class RoomLogUITemplate : UITemplate<RoomLogUITemplate, RoomLog.Entry>
	{
        [SerializeField]
        Text label = default;

        [SerializeField]
        Image background = default;

        public RectTransform Rect { get; protected set; }

        void Awake()
        {
            Rect = GetComponent<RectTransform>();
        }

        void Start()
        {
            Rename("Room Log UI Template");
        }

        public override void UpdateState()
        {
            base.UpdateState();

            label.text = Data.Text;
            background.color = Data.Color;
        }
    }
}