using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    public static class GeneralUtility
    {
		public static string PrettifyName<T>(T value)
		{
			var text = value.ToString();

			var builder = new StringBuilder();

			for (int i = 0; i < text.Length; i++)
			{
				var current = text[i];

				if (char.IsUpper(current))
				{
					if (i + 1 < text.Length && i > 0)
					{
						var next = text[i + 1];
						var previous = text[i - 1];

						if (char.IsLower(previous))
							builder.Append(" ");
					}
				}

				builder.Append(text[i]);
			}

			return builder.ToString();
		}
	}
}