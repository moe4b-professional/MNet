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
    public class InputFieldUIPreference : UIPreference<InputField, string>
    {
        protected override void RegisterCallback() => Component.onValueChanged.AddListener(ChangeCallback);

        protected override void Apply(string data) => Component.text = data;

        protected override string Load() => PlayerPrefs.GetString(ID, Default);

        protected override void Save(string data) => PlayerPrefs.SetString(ID, data);
    }
}