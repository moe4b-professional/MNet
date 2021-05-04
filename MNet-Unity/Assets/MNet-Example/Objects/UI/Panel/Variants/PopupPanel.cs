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
	public class PopupPanel : UIPanel
	{
		[SerializeField]
		Text label = default;

		[SerializeField]
		Button button = default;

		[SerializeField]
		Text instruction = default;

		Action callback;

		public override void Configure()
		{
			base.Configure();

			button.onClick.AddListener(ClickAction);
		}

		public void Show(string text) => Show(text, null, null);
		public void Show(string text, string instruction) => Show(text, instruction, Hide);
		public void Show(string text, string instruction, Action callback)
		{
			label.text = text;

			this.callback = callback;
			this.instruction.text = instruction;

			button.gameObject.SetActive(instruction != null);

			Show();
		}

        void ClickAction()
		{
			callback?.Invoke();
			callback = null;
		}
	}
}