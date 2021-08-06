#if UNITY_EDITOR || UNITY_STANDALONE
#define UNITY
#endif

#if UNITY
using UnityEngine;

using MB;
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    [Preserve]
    [Serializable]
    public struct Version : INetworkSerializable
    {
#if UNITY
        [SerializeField]
#endif
        byte major;
        public byte Major => major;

#if UNITY
        [SerializeField]
#endif
        byte minor;
        public byte Minor => minor;

#if UNITY
        [SerializeField]
#endif
        byte patch;
        public byte Patch => patch;

        /// <summary>
        /// Numerical Value to Describe the Version, used for Comparison & Hashing
        /// </summary>
        public int Value => (patch) + (minor * 1_000) + (major * 1_000_000);

        public const char Splitter = '.';

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref major);
            context.Select(ref minor);
            context.Select(ref patch);
        }

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => $"{major}.{minor}.{patch}";

        public override bool Equals(object obj)
        {
            if (obj is Version instance) return Equals(instance);

            return false;
        }
        public bool Equals(Version instance)
        {
            if (this.major != instance.major) return false;
            if (this.minor != instance.minor) return false;
            if (this.patch != instance.patch) return false;

            return true;
        }

        public Version(byte major, byte minor, byte patch)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
        }
        public Version(byte major, byte minor) : this(major, minor, 0) { }
        public Version(byte major) : this(major, 0, 0) { }

        public static bool operator ==(Version left, Version right) => left.Equals(right);
        public static bool operator !=(Version left, Version right) => !left.Equals(right);

        public static bool operator >(Version left, Version right) => left.Value > right.Value;
        public static bool operator <(Version left, Version right) => left.Value < right.Value;

        public static bool operator >=(Version left, Version right) => left == right || left > right;
        public static bool operator <=(Version left, Version right) => left == right || left < right;

        public static explicit operator Version(string text) => Parse(text);

        //Static Utility
        public static Version Zero { get; private set; } = new Version(0, 0, 0);

        public static Version Parse(string text)
        {
            if (TryParse(text, out var result)) return result;

            throw new ArgumentException($"{text} Cannot be Parsed as a Version");
        }

        public static bool TryParse(string text, out Version version)
        {
            if (string.IsNullOrEmpty(text))
            {
                version = default;
                return false;
            }

            var words = text.Split(Splitter);

            if (words.Length > 3)
            {
                version = default;
                return false;
            }

            var numbers = new byte[words.Length];

            for (int i = 0; i < words.Length; i++)
            {
                if (byte.TryParse(words[i], out var number) == false)
                {
                    version = default;
                    return false;
                }

                numbers[i] = number;
            }

            version = Create(numbers);
            return true;
        }

        public static Version Create(params byte[] numbers)
        {
            if (numbers == null) throw new ArgumentNullException();

            switch (numbers.Length)
            {
                case 1:
                    return new Version(numbers[0]);

                case 2:
                    return new Version(numbers[0], numbers[1]);

                case 3:
                    return new Version(numbers[0], numbers[1], numbers[2]);

                default:
                    throw new ArgumentException($"Cannot Create Version from an Array of {numbers.Length} Numbers");
            }
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(Version))]
        public class Drawer : PersistantPropertyDrawer
        {
            SerializedProperty major;
            SerializedProperty minor;
            SerializedProperty patch;

            public const int FieldWidth = 30;
            public const int SeperatorWidth = 8;

            public static GUIStyle SeperatorStyle;

            static Drawer()
            {
                SeperatorStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                };
            }

            protected override void Init()
            {
                base.Init();

                major = Property.FindPropertyRelative(nameof(major));
                minor = Property.FindPropertyRelative(nameof(minor));
                patch = Property.FindPropertyRelative(nameof(patch));
            }

            public static float LineHeight => EditorGUIUtility.singleLineHeight;

            public override float CalculateHeight()
            {
                return LineHeight;
            }

            public override void Draw(Rect rect)
            {
                MUtility.GUICoordinates.ClearIndent();

                DrawLabel(ref rect, Label);

                DrawField(ref rect, major);
                DrawSeperator(ref rect); // .
                DrawField(ref rect, minor);
                DrawSeperator(ref rect); // .
                DrawField(ref rect, patch);

                MUtility.GUICoordinates.RestoreIndent();
            }

            static void DrawLabel(ref Rect rect, GUIContent content)
            {
                var width = EditorGUIUtility.labelWidth;

                var area = new Rect(rect.x, rect.y, width, LineHeight);

                EditorGUI.LabelField(area, content);

                rect.width -= width;
                rect.x += width;
            }

            static void DrawField(ref Rect rect, SerializedProperty property)
            {
                var area = new Rect(rect.x, rect.y, FieldWidth, LineHeight);

                property.intValue = EditorGUI.IntField(area, property.intValue);

                rect.x += FieldWidth;
                rect.width -= FieldWidth;
            }
            static void DrawField(ref Rect rect, int value)
            {
                var area = new Rect(rect.x, rect.y, FieldWidth, LineHeight);

                EditorGUI.IntField(area, value);

                rect.x += FieldWidth;
                rect.width -= FieldWidth;
            }

            static void DrawSeperator(ref Rect rect)
            {
                var area = new Rect(rect.x, rect.y, SeperatorWidth, LineHeight);

                EditorGUI.LabelField(area, ".", SeperatorStyle);

                rect.x += SeperatorWidth;
                rect.width -= SeperatorWidth;
            }

            public static void DrawReadOnly(Rect rect, GUIContent label, Version version)
            {
                MUtility.GUICoordinates.ClearIndent();

                DrawLabel(ref rect, label);

                DrawField(ref rect, version.major);
                DrawSeperator(ref rect); // .
                DrawField(ref rect, version.minor);
                DrawSeperator(ref rect); // .
                DrawField(ref rect, version.patch);

                MUtility.GUICoordinates.RestoreIndent();
            }
        }
#endif
    }
}