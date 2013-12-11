//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// EditorGUILayout.ObjectField doesn't support custom components, so a custom wizard saves the day.
/// Unfortunately this tool only shows components that are being used by the scene, so it's a "recently used" selection tool.
/// </summary>

public class ComponentSelector : ScriptableWizard
{
	public delegate void OnSelectionCallback (Object obj);

	System.Type mType;
	OnSelectionCallback mCallback;
	Object[] mObjects;

	static string GetName (System.Type t)
	{
		string s = t.ToString();
		s = s.Replace("UnityEngine.", "");
		if (s.StartsWith("UI")) s = s.Substring(2);
		return s;
	}

	/// <summary>
	/// Draw a button + object selection combo filtering specified types.
	/// </summary>

	static public void Draw<T> (string buttonName, T obj, OnSelectionCallback cb, bool editButton, params GUILayoutOption[] options) where T : Object
	{
		GUILayout.BeginHorizontal();
		bool show = NGUIEditorTools.DrawPrefixButton(buttonName);
		T o = EditorGUILayout.ObjectField(obj, typeof(T), false, options) as T;

		if (editButton && o != null && o is MonoBehaviour)
		{
			Component mb = o as Component;
			if (Selection.activeObject != mb.gameObject && GUILayout.Button("Edit", GUILayout.Width(40f)))
				Selection.activeObject = mb.gameObject;
		}
		GUILayout.EndHorizontal();
		if (show) Show<T>(cb);
		else if (o != obj) cb(o);
	}

	/// <summary>
	/// Draw a button + object selection combo filtering specified types.
	/// </summary>

	static public void Draw<T> (T obj, OnSelectionCallback cb, bool editButton, params GUILayoutOption[] options) where T : Object
	{
		Draw<T>(NGUITools.GetTypeName<T>(), obj, cb, editButton, options);
	}

	/// <summary>
	/// Show the selection wizard.
	/// </summary>

	static public void Show<T> (OnSelectionCallback cb) where T : Object
	{
		System.Type type = typeof(T);
		ComponentSelector comp = ScriptableWizard.DisplayWizard<ComponentSelector>("Select a " + GetName(type));
		comp.mType = type;
		comp.mCallback = cb;

		if (type == typeof(UIAtlas) || type == typeof(UIFont))
		{
			BetterList<T> list = new BetterList<T>();
			string[] paths = AssetDatabase.GetAllAssetPaths();

			for (int i = 0; i < paths.Length; ++i)
			{
				string path = paths[i];
				
				if (path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
				{
					GameObject obj = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;

					if (obj != null && PrefabUtility.GetPrefabType(obj) == PrefabType.Prefab)
					{
						T t = obj.GetComponent(typeof(T)) as T;
						if (t != null) list.Add(t);
					}
				}
			}
			comp.mObjects = list.ToArray();
		}
		else comp.mObjects = Resources.FindObjectsOfTypeAll(typeof(T));
	}

	/// <summary>
	/// Draw the custom wizard.
	/// </summary>

	void OnGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);
		GUILayout.Label("Select a " + GetName(mType), "LODLevelNotifyText");
		NGUIEditorTools.DrawSeparator();

		if (mObjects.Length == 0)
		{
			EditorGUILayout.HelpBox("No " + GetName(mType) + " components found.\nTry creating a new one.", MessageType.Info);

			bool isDone = false;

			EditorGUILayout.Space();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (mType == typeof(UIFont))
			{
				if (GUILayout.Button("Open the Font Maker", GUILayout.Width(150f)))
				{
					EditorWindow.GetWindow<UIFontMaker>(false, "Font Maker", true);
					isDone = true;
				}
			}
			else if (mType == typeof(UIAtlas))
			{
				if (GUILayout.Button("Open the Atlas Maker", GUILayout.Width(150f)))
				{
					EditorWindow.GetWindow<UIAtlasMaker>(false, "Atlas Maker", true);
					isDone = true;
				}
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			if (isDone) Close();
		}
		else
		{
			Object sel = null;

			foreach (Object o in mObjects)
			{
				if (DrawObject(o))
				{
					sel = o;
				}
			}

			if (sel != null)
			{
				mCallback(sel);
				Close();
			}
		}
	}

	/// <summary>
	/// Draw details about the specified object in column format.
	/// </summary>

	bool DrawObject (Object ob)
	{
		bool retVal = false;
		Component comp = ob as Component;

		GUILayout.BeginHorizontal();
		{
			if (comp != null && EditorUtility.IsPersistent(comp.gameObject))
				GUI.contentColor = new Color(0.6f, 0.8f, 1f);
			
			GUILayout.Label(NGUITools.GetTypeName(ob), "AS TextArea", GUILayout.Width(80f), GUILayout.Height(20f));

			if (comp != null)
			{
				GUILayout.Label(NGUITools.GetHierarchy(comp.gameObject), "AS TextArea", GUILayout.Height(20f));
			}
			else if (ob is Font)
			{
				Font fnt = ob as Font;
				GUILayout.Label(fnt.name, "AS TextArea", GUILayout.Height(20f));
			}
			GUI.contentColor = Color.white;

			retVal = GUILayout.Button("Select", "ButtonLeft", GUILayout.Width(60f), GUILayout.Height(16f));
		}
		GUILayout.EndHorizontal();
		return retVal;
	}
}
