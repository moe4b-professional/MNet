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
	public class PauseMenu : UIMenu
	{
        Level Level => Level.Instance;

        public override void Initialize()
        {
            base.Initialize();

            Level.Pause.OnSet += LevelPauseCallback;
        }

        void LevelPauseCallback(LevelPauseMode mode)
        {
            Visible = mode == LevelPauseMode.Hard;
        }
    }
}