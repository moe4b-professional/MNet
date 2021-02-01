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
    public class ToggleUIPreference : UIPreference<Toggle, bool>
    {
        protected override void RegisterCallback() => Component.onValueChanged.AddListener(ChangeCallback);

        protected override void ApplyData(bool data) => Component.isOn = data;

        protected override bool Load() => Convert(PlayerPrefs.GetInt(ID, Convert(Default)));

        protected override void Save(bool data) => PlayerPrefs.SetInt(ID, Convert(data));

        static int Convert(bool value) => value ? 1 : 0;
        static bool Convert(int value) => value == 1 ? true : false;
    }
}