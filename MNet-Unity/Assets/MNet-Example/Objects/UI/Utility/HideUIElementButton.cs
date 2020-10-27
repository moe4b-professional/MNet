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
	public class HideUIElementButton : MonoBehaviour
	{
		[SerializeField]
		UIElement target = null;

		Button button;

		void Awake()
        {
			button = GetComponent<Button>();
        }

		void Start()
        {
			button.onClick.AddListener(ClickAction);
        }

		void ClickAction()
		{
			target.Hide();
		}
	}
}