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
		public static string Bold(string text) => $"<b>{text}</b>";

		public static string Italic(string text) => $"<i>{text}</i>";

		public static string Size(string text, int value) => $"<size={value}>{text}</size>";

		public static string Colorize(string text, ColorSurrogate color) => $"<color=#{color}>{text}</color>";

		public static string Style(string text, bool bold = false, bool italic = false, int? size = null, ColorSurrogate? color = null)
		{
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

			public static implicit operator ColorSurrogate (string text) => new ColorSurrogate(text);

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