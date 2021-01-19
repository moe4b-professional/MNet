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
    public class PrefabAsset
    {
        [SerializeField]
        GameObject gameObject = default;
        public GameObject GameObject => gameObject;

        public static implicit operator GameObject(PrefabAsset prefab) => prefab.gameObject;

        public PrefabAsset() : this(null) { }
        public PrefabAsset(GameObject gameObject)
        {
            this.gameObject = gameObject;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(PrefabAsset))]
        public class Drawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var field = property.FindPropertyRelative(nameof(gameObject));

                field.objectReferenceValue = EditorGUI.ObjectField(position, label, field.objectReferenceValue, typeof(GameObject), false);
            }
        }
#endif
    }
}