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

        protected override void Apply(bool data) => Component.isOn = data;
    }
}