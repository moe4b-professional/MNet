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
    public class GameScene
    {
        [SerializeField]
        protected Object asset;
#if UNITY_EDITOR
        public Object Asset { get { return asset; } }

        /// <summary>
        /// EDITOR ONLY
        /// </summary>
        public string Path
        {
            get
            {
                return AssetDatabase.GetAssetPath(asset);
            }
        }
#endif

        [SerializeField]
        protected string _name;
        public string name
        {
            get
            {
#if UNITY_EDITOR
                if (asset == null) return null;

                if(asset.name != this._name)
                {
                    Debug.LogWarning("Scene name mismatch, old name: (" + _name + "), new name: (" + asset.name + "). Please check scene fields");

                    _name = asset.name;
                }
#endif

                return _name;
            }
        }

        public static implicit operator string(GameScene scene)
        {
            return scene._name;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(GameScene))]
        public class Drawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                SerializedProperty asset;
                asset = property.FindPropertyRelative(nameof(asset));

                SerializedProperty name;
                name = property.FindPropertyRelative("_" + nameof(name));

                asset.objectReferenceValue = EditorGUI.ObjectField(position, property.displayName, asset.objectReferenceValue, typeof(SceneAsset), false);
                
                name.stringValue = asset.objectReferenceValue == null ? string.Empty : asset.objectReferenceValue.name;
            }
        }
#endif
    }
}