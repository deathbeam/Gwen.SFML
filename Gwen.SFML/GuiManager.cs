using System.Collections.Generic;

namespace Gwen
{
	/// <summary>
	/// 
	/// </summary>
	public static class GuiManager
	{
		private static readonly Dictionary<string, object> Assets = new Dictionary<string, object>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
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
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="obj"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T Set<T>(string name, object obj) where T : class
		{
			if (!Assets.ContainsKey(name))
				Assets.Add(name, obj);
			return (T)obj;
		}

		/// <summary>
		/// 
		/// </summary>
		public static void Clear()
		{
			Assets.Clear();
		}
	}
}
