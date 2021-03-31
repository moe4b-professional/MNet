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
	public class TextInputPanel : UIPanel
	{
		[SerializeField]
		Text label = default;

		[SerializeField]
		InputField input = default;

		public InputField.ContentType ContentType
        {
			get => input.contentType;
			set => input.contentType = value;
        }

		[SerializeField]
		Button ok = default;

		[SerializeField]
		Button cancel = default;

		public delegate void CallbackDelegate(bool confirmed, string text);
		public CallbackDelegate callback;

		public override void Configure()
		{
			base.Configure();

			ok.onClick.AddListener(Confirm);
			cancel.onClick.AddListener(Deny);
		}

		public virtual void Show(string instructions, CallbackDelegate callback)
		{
			Show();

			label.text = instructions;
			this.callback = callback;

			Selection = input.gameObject;
		}

		public override void Show()
        {
            base.Show();

			ContentType = InputField.ContentType.Standard;
		}

        void Confirm() => Action(true);
		void Deny() => Action(false);

		void Action(bool confirmed)
		{
			Hide();

			callback?.Invoke(confirmed, input.text);

			input.text = string.Empty;
			callback = null;
		}
	}
}