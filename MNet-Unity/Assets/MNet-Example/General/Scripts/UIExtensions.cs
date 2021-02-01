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
	public static class UIExtensions
	{
		public static Dropdown.OptionData GetOption(this Dropdown dropdown) => GetOption(dropdown, dropdown.options);
		public static T GetOption<T>(this Dropdown dropdown, IList<T> list) => list[dropdown.value];

		public static void AddOptions<T>(this Dropdown dropdown, IList<T> list)
		{
			AddOptions(dropdown, list, GetName);

			string GetName(T value) => value.ToString();
		}
		public static void AddOptions<T>(this Dropdown dropdown, IList<T> list, Func<T, string> function)
		{
			var options = new List<Dropdown.OptionData>(list.Count);

			for (int i = 0; i < list.Count; i++)
			{
				var text = function(list[i]);

				var option = new Dropdown.OptionData(text);

				options.Add(option);
			}

			dropdown.AddOptions(options);
		}
		public static void AddOptions(this Dropdown dropdown, params string[] names) => dropdown.AddOptions((IList<string>)names);

		public static string GetValueOrDefault(this InputField field, string defaultValue)
		{
			return string.IsNullOrEmpty(field.text) ? defaultValue : field.text;
		}
	}
}