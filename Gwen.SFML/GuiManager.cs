using System.Collections.Generic;

namespace Gwen.Tools
{
	/// <summary>
	/// GUI manager.
	/// </summary>
	public static class GUIManager
	{
		private static readonly Dictionary<string, object> Assets = new Dictionary<string, object>();

		/// <summary>
		/// Get the specified name.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static T Get<T>(string name) where T : class
		{
			object result;
			if (Assets.TryGetValue(name, out result))
			{
				return (T)result;
			}
			return null;
		}

		/// <summary>
		/// Set the specified name and obj.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="obj">Object.</param>
		public static void Set(string name, object obj)
		{
			if (!Assets.ContainsKey(name))
				Assets.Add(name, obj);
		}

		/// <summary>
		/// Clear this instance.
		/// </summary>
		public static void Clear()
		{
			Assets.Clear();
		}
	}
}
