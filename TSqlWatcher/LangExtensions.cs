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

		public static bool ContainsCall(this string @in, string what)
		{
			return enCulture.CompareInfo.IndexOf(@in, what.Trim() + " ", CompareOptions.IgnoreCase) != NotFound
				|| enCulture.CompareInfo.IndexOf(@in, what.Trim() + "\r", CompareOptions.IgnoreCase) != NotFound
				|| enCulture.CompareInfo.IndexOf(@in, what.Trim() + "\n", CompareOptions.IgnoreCase) != NotFound
				|| enCulture.CompareInfo.IndexOf(@in, what.Trim() + "\t", CompareOptions.IgnoreCase) != NotFound
				|| enCulture.CompareInfo.IndexOf(@in, what.Trim() + "]", CompareOptions.IgnoreCase) != NotFound;
		}

		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var item in collection) action(item);
		}

		public static T TryGet<K, T>(this IDictionary<K, T> dictionary, K key, T defaultValue = default(T))
		{
			if (key == null)
				return defaultValue;

			if (dictionary.ContainsKey(key))
			{
				return dictionary[key];
			}

			return defaultValue;
		}

		public static T? TryGetNullable<K, T>(this IDictionary<K, T> dictionary, K key) where T : struct
		{
			if (key == null)
				return null;

			if (dictionary.ContainsKey(key))
			{
				return dictionary[key];
			}

			return null;
		}

		/// <summary>
		/// Syntaxic sugar similar to C#6 null-proganation operator.
		/// </summary>
		public static TResult Maybe<TArg, TResult>(this TArg obj, Func<TArg, TResult> func, TResult defaultValue = default(TResult)) where TArg : class
		{
			if (obj == null)
			{
				return defaultValue;
			}
			else
			{
				return func(obj);
			}
		}

		/// <summary>
		/// Syntaxic sugar similar to C#6 null-proganation operator.
		/// </summary>
		public static TResult Maybe<TArg, TResult>(this TArg? obj, Func<TArg?, TResult> func, TResult defaultValue = default(TResult)) where TArg : struct
		{
			if (obj == null)
			{
				return defaultValue;
			}

			return func(obj);
		}

		/// <summary>
		/// Syntaxic sugar similar to C#6 null-proganation operator.
		/// </summary>
		public static void MaybeDo<TArg>(this TArg? obj, Action<TArg> func) where TArg : struct
		{
			if (obj == null)
			{
				return;
			}

			func(obj.Value);
		}

		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> list)
		{
			return (list == null)
				? Enumerable.Empty<T>()
				: list;
		}

		public static IEnumerable<T> DistinctSameOrder<T>(this IEnumerable<T> collection)
		{
			var hash = new HashSet<T>();
			foreach (var item in collection)
			{
				if (!hash.Contains(item))
				{
					hash.Add(item);
					yield return item;
				} 
			}
		}
	}
}