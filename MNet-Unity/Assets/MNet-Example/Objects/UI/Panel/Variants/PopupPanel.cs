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

using Cysharp.Threading.Tasks;

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

		public void Show(string text)
        {
			this.label.text = text;

			button.gameObject.SetActive(false);

			Show();
		}

		public async UniTask Show(string text, string instruction)
		{
			this.label.text = text;
			this.instruction.text = instruction;

			button.gameObject.SetActive(true);

			Show();

			await button.OnClickAsync(default);

			Hide();
		}
	}
}