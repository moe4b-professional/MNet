#if UNITY_EDITOR || UNITY_STANDALONE
#define UNITY
#endif

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

#if UNITY
using UnityEngine;
#endif

namespace MNet
{
	public static class RichTextMarker
	{
		public static string Bold(object target) => $"<b>{target}</b>";

		public static string Italic(object target) => $"<i>{target}</i>";

		public static string Size(object target, int value) => $"<size={value}>{target}</size>";

		public static string Colorize(object target, ColorSurrogate color) => $"<color=#{color}>{target}</color>";

		public static string Style(object target, bool bold = false, bool italic = false, int? size = null, ColorSurrogate? color = null)
		{
			var text = target.ToString();

			if (bold)
				text = Bold(text);

			if (italic)
				text = Italic(text);

			if (size != null)
				text = Size(text, size.Value);

			if (color != null)
				text = Colorize(text, color.Value);

			return text;
		}

		public struct ColorSurrogate
		{
			string hex;
			public string Hex => hex;

			public override string ToString() => hex;

			public ColorSurrogate(string hex)
			{
				this.hex = hex;
			}

			public static implicit operator ColorSurrogate (string text)
            {
				text = text.TrimStart('#');

				return new ColorSurrogate(text);
			}

#if UNITY
			public static implicit operator ColorSurrogate (Color color)
            {
				var text = ColorUtility.ToHtmlStringRGBA(color);

				return new ColorSurrogate(text);
			}
#endif
		}
	}
}