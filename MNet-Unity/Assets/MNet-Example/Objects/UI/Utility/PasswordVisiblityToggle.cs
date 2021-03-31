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
	[RequireComponent(typeof(Toggle))]
	public class PasswordVisiblityToggle : MonoBehaviour
	{
        [SerializeField]
        InputField field = default;

		Toggle toggle;

        void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        void Start()
        {
            UpdateState();

            toggle.onValueChanged.AddListener(Change);
        }

        void Change(bool value) => UpdateState();

        void UpdateState()
        {
            field.contentType = toggle.isOn ? InputField.ContentType.Standard : InputField.ContentType.Password;
            field.ForceLabelUpdate();
        }
    }
}