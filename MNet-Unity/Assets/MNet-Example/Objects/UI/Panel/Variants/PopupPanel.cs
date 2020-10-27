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
using WebSocketSharp;

namespace MNet.Example
{
	public class PopupPanel : UIPanel
	{
		[SerializeField]
		Text label = null;

		[SerializeField]
		Button button = null;

		[SerializeField]
		Text instruction = null;

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

        public override void Hide()
        {
            base.Hide();

			callback = null;
		}

        void ClickAction()
		{
			if (callback == null) return;

			callback();
			callback = null;
		}
	}
}