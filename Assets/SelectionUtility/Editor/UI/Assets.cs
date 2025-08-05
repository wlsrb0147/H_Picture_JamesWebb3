namespace Xarbrough.SelectionUtility
{
	using UnityEditor;
	using UnityEngine;
using Debug = DebugEx;

	internal static class Assets
	{
		public static T LoadFromGuid<T>(string guid) where T : Object
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			return AssetDatabase.LoadAssetAtPath<T>(path);
		}
	}
}
