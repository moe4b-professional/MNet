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

using MB;

namespace MNet.Example
{
    [Serializable]
	public class LevelData
	{
		[SerializeField]
        string name = default;
        public string Name => name;

        [SerializeField]
        MSceneAsset scene = default;
        public MSceneAsset Scene => scene;

        public override string ToString() => name;

        public static string GetName(LevelData data) => data.name;
    }
}