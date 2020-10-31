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

using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MNet.Example
{
    class ScriptableObjectInitializerBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildTarget target, string path)
        {

        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (ScriptableObjectInitializer.Instance) ScriptableObjectInitializer.Instance.Refresh();
        }
    }
}
#endif