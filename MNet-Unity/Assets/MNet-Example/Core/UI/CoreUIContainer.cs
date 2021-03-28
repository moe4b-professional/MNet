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
	public class CoreUIContainer : MonoBehaviour
	{
		[SerializeField]
		PopupPanel popup = default;
		public PopupPanel Popup => popup;

		[SerializeField]
        TextInputPanel textInput = default;
        public TextInputPanel TextInput => textInput;

        [SerializeField]
        FaderUI fader = default;
        public FaderUI Fader => fader;
    }
}