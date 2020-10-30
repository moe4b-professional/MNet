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

namespace Game
{
	[DefaultExecutionOrder(-200)]
	public class SampleBehaviour : MonoBehaviour
	{
		void Awake()
        {
			Debug.Log("Awake");
        }

		void Start()
        {
			Debug.Log("Start");
		}

		bool updateLogged = false;
		void Update()
        {
			if (updateLogged) return;

			Debug.Log("Update");
			updateLogged = true;
		}
	}
}