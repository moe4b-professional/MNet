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
	[AddComponentMenu(Constants.Path + "Network Stats Info")]
	public class NetworkStatsInfo : MonoBehaviour
	{
		public float time;

		public PingProperty ping;
		[Serializable]
		public struct PingProperty
		{
			public double average;

			public double min;

			public double max;

			public static PingProperty Read()
			{
				var ping = new PingProperty()
				{
					average = NetworkAPI.Ping.Average,
					min = NetworkAPI.Ping.Min,
					max = NetworkAPI.Ping.Max,
				};

				return ping;
			}
		}

		public bool showGUI = true;

		void Update()
		{
			time = NetworkAPI.Time.Seconds;

			ping = PingProperty.Read();
		}

        void OnGUI()
        {
			if (showGUI == false) return;

			var style = new GUIStyle(GUI.skin.label);

			style.fontSize = 20;
			style.fontStyle = FontStyle.Bold;
			style.normal.textColor = Color.black;

			GUILayout.BeginVertical();

			GUILayout.Label($" Average Ping: {ping.average}", style);
			GUILayout.Label($" Min Ping: {ping.min}", style);
			GUILayout.Label($" Max Ping: {ping.max}", style);

			GUILayout.EndVertical();
		}
    }
}