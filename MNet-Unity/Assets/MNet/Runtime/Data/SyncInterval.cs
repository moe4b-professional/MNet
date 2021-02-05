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

namespace MNet
{
	[Serializable]
	public class SyncInterval
	{
        [SerializeField]
        bool custom = default;
        public bool Custom => custom;

        [SerializeField]
        int value = default;
        public int Value => value;

        public float Milliseconds => value;
        public float Seconds => value / 1000f;

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SyncInterval))]
        public class Drawer : PropertyDrawer
        {
            SerializedProperty property;
            SerializedProperty value;
            SerializedProperty custom;

            void Init(SerializedProperty reference)
            {
                if (property == reference) return;

                property = reference;

                value = property.FindPropertyRelative(nameof(SyncInterval.value));
                custom = property.FindPropertyRelative(nameof(SyncInterval.custom));
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight * 2f;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {

            }

            void DrawCustom(ref Rect rect, GUIContent label)
            {

            }

            void DrawValue(ref Rect rect)
            {

            }
        }
#endif
    }
}