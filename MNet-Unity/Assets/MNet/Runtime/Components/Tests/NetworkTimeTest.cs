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

namespace MNet
{
    [AddComponentMenu(Constants.Path + "Tests/" + "Network Time Test")]
	public class NetworkTimeTest : MonoBehaviour
	{
		public float time;

		public float delta;

		void Update()
        {
			time = NetworkAPI.Time.Seconds;

			delta = NetworkAPI.Time.Delta.Seconds;
        }
	}
}