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
	[RequireComponent(typeof(Button))]
	public class QuitLevelButton : MonoBehaviour
	{
		Button button;

		Level Level => Level.Instance;

		void Start()
        {
			button = GetComponent<Button>();
			button.onClick.AddListener(Action);
        }

		void Action() => Level.Quit().Forget();
	}
}