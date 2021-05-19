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

namespace MNet.Example
{
	[RequireComponent(typeof(Button))]
	public class TransitionUIElementButton : MonoBehaviour
	{
		[SerializeField]
		UIElement from = null;

		[SerializeField]
		UIElement to = null;

		Button button;

		void Reset()
		{
			from = QueryComponent.InParents<UIElement>(gameObject);
		}

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
			from.Hide();

			to.Show();
		}
	}
}