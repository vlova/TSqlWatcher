using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TSqlWatcher
{
	static class LangExtensions
	{
		public static IEnumerable<IList<T>> IntoGroups<T>(this IEnumerable<T> collection, int count)
		{
			var buffer = new List<T>();
			int index = 0;
			foreach (var item in collection)
			{
				buffer.Add(item);
				index++;
				if (index == count)
				{
					yield return buffer;
					buffer = new List<T>();
				}
			}

			if (buffer.Any())
			{
				yield return buffer;
			}
		}

		private static CultureInfo enCulture = new CultureInfo("en-US");
		private const int NotFound = -1;

		public static bool ContainsInsensetive(this string @in, string what)
		{
			return enCulture.CompareInfo.IndexOf(@in, what, CompareOptions.IgnoreCase) != NotFound;
		}

		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var item in collection) action(item);
		}

		public static T TryGet<K, T>(this IDictionary<K, T> dictionary, K key, T defaultValue = default(T))
		{
			if (dictionary.ContainsKey(key))
			{
				return dictionary[key];
			}

			return defaultValue;
		}

		public static T? TryGetNullable<K, T>(this IDictionary<K, T> dictionary, K key) where T : struct
		{
			if (dictionary.ContainsKey(key))
			{
				return dictionary[key];
			}

			return null;
		}
	}
}
