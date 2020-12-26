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
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    sealed class SyncIntervalAttribute : PropertyAttribute
    {
        public (int min, int max)? Range { get; private set; }

        public SyncIntervalAttribute()
        {
            this.Range = null;
        }
        public SyncIntervalAttribute(int min, int max)
        {
            this.Range = (min, max);
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SyncIntervalAttribute))]
        public class Drawer : PropertyDrawer
        {
            public const float Spacing = 80;

            new SyncIntervalAttribute attribute;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
            {
                if (property.propertyType != SerializedPropertyType.Integer)
                {
                    var message = $"{nameof(SyncIntervalAttribute)} is Invalid on {property.propertyPath} Field Because It's not an Integer";
                    EditorGUI.HelpBox(rect, message, MessageType.Error);
                    return;
                }

                if (attribute == null) attribute = base.attribute as SyncIntervalAttribute;

                DrawValue(ref rect, label, property);
                DrawCycles(ref rect, property.intValue);
            }

            void DrawValue(ref Rect rect, GUIContent label, SerializedProperty value)
            {
                var area = rect;

                area.width -= Spacing;

                if (attribute.Range.HasValue)
                {
                    var range = attribute.Range.Value;

                    value.intValue = EditorGUI.IntSlider(area, label, value.intValue, range.min, range.max);
                }
                else
                {
                    value.intValue = EditorGUI.IntField(area, label, value.intValue);
                }

                rect.x += area.width;
                rect.width -= area.width;
            }

            void DrawCycles(ref Rect rect, int value)
            {
                var area = rect;

                string symbol = value == 0 ? "∞" : $"~{1000 / value}";
                var text = $"{symbol}Hz";

                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                EditorGUI.LabelField(area, text, style);
            }
        }
#endif
    }
}