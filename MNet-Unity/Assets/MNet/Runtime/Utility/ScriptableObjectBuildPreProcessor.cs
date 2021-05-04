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
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MNet
{
	public class ScriptableObjectBuildPreProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report)
		{
			var guids = AssetDatabase.FindAssets("t:ScriptableObject");

			for (int i = 0; i < guids.Length; i++)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);

				var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

				if (asset is IScriptableObjectBuildPreProcess contract) contract.Process();
			}
		}
	}

	public interface IScriptableObjectBuildPreProcess
	{
		void Process();
	}
}