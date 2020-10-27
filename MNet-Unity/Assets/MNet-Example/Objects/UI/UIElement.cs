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
	public class UIElement : MonoBehaviour, IInitialize
	{
		public virtual GameObject Target => gameObject;

		public virtual bool Visible
		{
			get => Target.activeSelf;
			set
			{
				if (value)
					Show();
				else
					Hide();
			}
		}

		public virtual void Configure()
		{

		}

		public virtual void Init()
		{

		}

		public virtual void Show()
		{
			Target.SetActive(true);
		}
		public virtual void Hide()
		{
			Target.SetActive(false);
		}

		public virtual void Toggle() => Visible = !Visible;
	}
}