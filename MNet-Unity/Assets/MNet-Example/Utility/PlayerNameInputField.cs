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
	[RequireComponent(typeof(InputField))]
	public class PlayerNameInputField : MonoBehaviour
	{
		InputField field;

		Core Core => Core.Instance;

		void Awake()
		{
			field = GetComponent<InputField>();
		}

		void Start()
		{
			field.onValueChanged.AddListener(ChangeCallback);
		}

		void OnEnable()
		{
			field.text = Core.Network.PlayerName;
		}

		void ChangeCallback(string value)
		{
			Core.Network.PlayerName = value;
		}
	}
}