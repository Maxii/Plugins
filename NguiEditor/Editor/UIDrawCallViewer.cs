//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Draw Call Viewer shows a list of draw calls created by NGUI and lets you hide them selectively.
/// </summary>

public class UIDrawCallViewer : EditorWindow
{
	static public UIDrawCallViewer instance;

	enum Visibility
	{
		Visible,
		Hidden,
	}

	enum ShowFilter
	{
		AllPanels,
		SelectedPanel,
	}

	Vector2 mScroll = Vector2.zero;

	void OnEnable () { instance = this; }
	void OnDisable () { instance = null; }
	void OnSelectionChange () { Repaint(); }

	/// <summary>
	/// Draw the custom wizard.
	/// </summary>

	void OnGUI ()
	{
		BetterList<UIDrawCall> dcs = UIDrawCall.activeList;

		if (dcs.size == 0)
		{
			EditorGUILayout.HelpBox("No NGUI draw calls present in the scene", MessageType.Info);
			return;
		}

		GUILayout.Space(12f);

		NGUIEditorTools.SetLabelWidth(100f);
		ShowFilter show = (NGUISettings.showAllDCs ? ShowFilter.AllPanels : ShowFilter.SelectedPanel);

		if ((ShowFilter)EditorGUILayout.EnumPopup("Draw Call Filter", show) != show)
			NGUISettings.showAllDCs = !NGUISettings.showAllDCs;

		GUILayout.Space(6f);

		UIPanel selectedPanel = NGUITools.FindInParents<UIPanel>(Selection.activeGameObject);

		if (selectedPanel == null)
		{
			EditorGUILayout.HelpBox("Select any object within the UI hierarchy", MessageType.Info);
			return;
		}

		NGUIEditorTools.SetLabelWidth(80f);
		mScroll = GUILayout.BeginScrollView(mScroll);

		int dcCount = 0;

		for (int i = 0; i < dcs.size; ++i)
		{
			UIDrawCall dc = dcs[i];

			if (dc.manager != selectedPanel)
			{
				if (!NGUISettings.showAllDCs) continue;
				if (dc.showDetails) GUI.color = new Color(0.85f, 0.85f, 0.85f);
				else GUI.contentColor = new Color(0.85f, 0.85f, 0.85f);
			}
			else GUI.contentColor = Color.white;

			++dcCount;
			string key = dc.keyName;
			string name = key + " of " + dcs.size;
			if (!dc.isActive) name = name + " (HIDDEN)";
			else if (dc.manager != selectedPanel) name = name + " (" + dc.manager.name + ")";

			if (NGUIEditorTools.DrawHeader(name, key))
			{
				GUI.color = (dc.manager == selectedPanel) ? Color.white : new Color(0.8f, 0.8f, 0.8f);

				NGUIEditorTools.BeginContents();
				EditorGUILayout.ObjectField("Material", dc.baseMaterial, typeof(Material), false);

				int count = 0;

				for (int b = 0; b < UIWidget.list.size; ++b)
				{
					UIWidget w = UIWidget.list[b];
					if (w.drawCall == dc)
						++count;
				}

				string myPath = NGUITools.GetHierarchy(dc.manager.cachedGameObject);
				string remove = myPath + "\\";
				string[] list = new string[count + 1];
				list[0] = count.ToString();
				count = 0;

				for (int b = 0; b < UIWidget.list.size; ++b)
				{
					UIWidget w = UIWidget.list[b];

					if (w.drawCall == dc)
					{
						string path = NGUITools.GetHierarchy(w.cachedGameObject);
						list[++count] = count + ". " + (string.Equals(path, myPath) ? w.name : path.Replace(remove, ""));
					}
				}

				GUILayout.BeginHorizontal();
				int sel = EditorGUILayout.Popup("Widgets", 0, list);
				GUILayout.Space(18f);
				GUILayout.EndHorizontal();

				if (sel != 0)
				{
					count = 0;

					for (int b = 0; b < UIWidget.list.size; ++b)
					{
						UIWidget w = UIWidget.list[b];

						if (w.drawCall == dc && ++count == sel)
						{
							Selection.activeGameObject = w.gameObject;
							break;
						}
					}
				}

				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Render Q", dc.finalRenderQueue.ToString(), GUILayout.Width(120f));
				bool draw = (Visibility)EditorGUILayout.EnumPopup(dc.isActive ? Visibility.Visible : Visibility.Hidden) == Visibility.Visible;
				GUILayout.Space(18f);
				GUILayout.EndHorizontal();

				if (dc.isActive != draw)
				{
					dc.isActive = draw;
					UnityEditor.EditorUtility.SetDirty(dc.manager);
				}

				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Triangles", dc.triangles.ToString(), GUILayout.Width(120f));

				if (dc.manager != selectedPanel)
				{
					if (GUILayout.Button("Select the Panel"))
					{
						Selection.activeGameObject = dc.manager.gameObject;
					}
					GUILayout.Space(18f);
				}
				GUILayout.EndHorizontal();

				if (dc.manager.clipping != UIDrawCall.Clipping.None && !dc.isClipped)
				{
					EditorGUILayout.HelpBox("You must switch this material's shader to Unlit/Transparent Colored or Unlit/Premultiplied Colored in order for clipping to work.",
						MessageType.Warning);
				}

				NGUIEditorTools.EndContents();
				GUI.color = Color.white;
			}
		}

		if (dcCount == 0)
		{
			EditorGUILayout.HelpBox("No draw calls found", MessageType.Info);
		}
		GUILayout.EndScrollView();
	}
}
