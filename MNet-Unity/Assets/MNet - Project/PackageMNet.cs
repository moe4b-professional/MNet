#if UNITY_EDITOR
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
	public static class PackageMNet
	{
		[MenuItem("MNet/Package", false, 300)]
		static void Package()
        {
			var options = ExportPackageOptions.Interactive | ExportPackageOptions.Recurse;

			AssetDatabase.ExportPackage("Assets/MNet", "MNet-Unity.unitypackage", options);
		}
	}
}
#endif