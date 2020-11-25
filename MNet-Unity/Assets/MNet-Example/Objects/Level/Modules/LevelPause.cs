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
    public class LevelPause : MonoBehaviour
    {
        public LevelPauseMode Mode { get; protected set; } = LevelPauseMode.None;

        public delegate void SetDelegate(LevelPauseMode mode);
        public event SetDelegate OnSet;
        public void Set(LevelPauseMode value)
        {
            Mode = value;

            OnSet?.Invoke(Mode);
        }

        Level Level => Level.Instance;
        LevelUI UI => Level.UI;
        PauseMenu Menu => UI.Pause;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) | Input.GetKeyDown(KeyCode.Home))
            {
                if (Mode == LevelPauseMode.None)
                    Set(LevelPauseMode.Hard);
                else if (Mode == LevelPauseMode.Hard && Menu.Visible)
                    Set(LevelPauseMode.None);
            }
        }
    }

    public enum LevelPauseMode
    {
        None, Soft, Hard
    }
}