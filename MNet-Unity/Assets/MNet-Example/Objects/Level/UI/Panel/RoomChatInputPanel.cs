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
	public class RoomChatInputPanel : UIPanel
	{
		[SerializeField]
		InputField input = default;

		[SerializeField]
		Button send = default;

		[SerializeField]
		RoomLogPanel log = default;

		void Start()
        {
			send.onClick.AddListener(Action);
        }

		void Action()
        {
			if (string.IsNullOrEmpty(input.text)) return;

			log.SendChat(input.text);

			input.text = string.Empty;
        }
	}
}