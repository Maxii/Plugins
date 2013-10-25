//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(UIPanel))]
public class UIPanelInspector : Editor
{
	/// <summary>
	/// Handles & interaction.
	/// </summary>

	public void OnSceneGUI ()
	{
		Event e = Event.current;

		switch (e.type)
		{
			case EventType.KeyDown:
			{
				if (e.keyCode == KeyCode.Escape)
				{
					Tools.current = Tool.Move;
					Selection.activeGameObject = null;
					e.Use();
				}
			}
			break;
		}
	}

	/// <summary>
	/// Draw the inspector widget.
	/// </summary>

	public override void OnInspectorGUI ()
	{
		UIPanel panel = target as UIPanel;
		NGUIEditorTools.SetLabelWidth(80f);
		EditorGUILayout.Space();

		float alpha = EditorGUILayout.Slider("Alpha", panel.alpha, 0f, 1f);

		if (alpha != panel.alpha)
		{
			NGUIEditorTools.RegisterUndo("Panel Alpha", panel);
			panel.alpha = alpha;
		}

		GUILayout.BeginHorizontal();
		{
			EditorGUILayout.PrefixLabel("Depth");

			int depth = panel.depth;
			if (GUILayout.Button("Back", GUILayout.Width(60f))) --depth;
			depth = EditorGUILayout.IntField(depth, GUILayout.MinWidth(20f));
			if (GUILayout.Button("Forward", GUILayout.Width(68f))) ++depth;

			if (panel.depth != depth)
			{
				NGUIEditorTools.RegisterUndo("Panel Depth", panel);
				panel.depth = depth;

				if (UIPanelTool.instance != null)
					UIPanelTool.instance.Repaint();
			}
		}
		GUILayout.EndHorizontal();

		int matchingDepths = 0;

		for (int i = 0; i < UIPanel.list.size; ++i)
		{
			UIPanel p = UIPanel.list[i];
			if (p != null && panel.depth == p.depth)
				++matchingDepths;
		}

		if (matchingDepths > 1)
		{
			EditorGUILayout.HelpBox(matchingDepths + " panels are sharing the depth value of " + panel.depth, MessageType.Info);
		}

		GUILayout.BeginHorizontal();
		bool norms = EditorGUILayout.Toggle("Normals", panel.generateNormals, GUILayout.Width(100f));
		GUILayout.Label("Needed for lit shaders");
		GUILayout.EndHorizontal();

		if (panel.generateNormals != norms)
		{
			panel.generateNormals = norms;
			UIPanel.SetDirty();
			EditorUtility.SetDirty(panel);
		}

		GUILayout.BeginHorizontal();
		bool cull = EditorGUILayout.Toggle("Cull", panel.cullWhileDragging, GUILayout.Width(100f));
		GUILayout.Label("Cull widgets while dragging them");
		GUILayout.EndHorizontal();

		if (panel.cullWhileDragging != cull)
		{
			panel.cullWhileDragging = cull;
			UIPanel.SetDirty();
			EditorUtility.SetDirty(panel);
		}

		GUILayout.BeginHorizontal();
		bool stat = EditorGUILayout.Toggle("Static", panel.widgetsAreStatic, GUILayout.Width(100f));
		GUILayout.Label("Check if widgets won't move");
		GUILayout.EndHorizontal();

		if (panel.widgetsAreStatic != stat)
		{
			panel.widgetsAreStatic = stat;
			UIPanel.SetDirty();
			EditorUtility.SetDirty(panel);
		}

		if (stat)
		{
			EditorGUILayout.HelpBox("Only mark the panel as 'static' if you know FOR CERTAIN that the widgets underneath will not move, rotate, or scale. Doing this improves performance, but moving widgets around will have no effect.", MessageType.Warning);
		}

		GUILayout.BeginHorizontal();
		if (NGUISettings.showAllDCs != EditorGUILayout.Toggle("Show All", NGUISettings.showAllDCs, GUILayout.Width(100f)))
			NGUISettings.showAllDCs = !NGUISettings.showAllDCs;
		GUILayout.Label("Show all draw calls");
		GUILayout.EndHorizontal();

		if (panel.showInPanelTool != EditorGUILayout.Toggle("Panel Tool", panel.showInPanelTool))
		{
			panel.showInPanelTool = !panel.showInPanelTool;
			EditorUtility.SetDirty(panel);
			EditorWindow.FocusWindowIfItsOpen<UIPanelTool>();
		}

		UIDrawCall.Clipping clipping = (UIDrawCall.Clipping)EditorGUILayout.EnumPopup("Clipping", panel.clipping);

		if (panel.clipping != clipping)
		{
			panel.clipping = clipping;
			EditorUtility.SetDirty(panel);
		}

		if (panel.clipping != UIDrawCall.Clipping.None)
		{
			Vector4 range = panel.clipRange;

			GUILayout.BeginHorizontal();
			GUILayout.Space(80f);
			Vector2 pos = EditorGUILayout.Vector2Field("Center", new Vector2(range.x, range.y));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Space(80f);
			Vector2 size = EditorGUILayout.Vector2Field("Size", new Vector2(range.z, range.w));
			GUILayout.EndHorizontal();

			if (size.x < 0f) size.x = 0f;
			if (size.y < 0f) size.y = 0f;

			range.x = pos.x;
			range.y = pos.y;
			range.z = size.x;
			range.w = size.y;

			if (panel.clipRange != range)
			{
				NGUIEditorTools.RegisterUndo("Clipping Change", panel);
				panel.clipRange = range;
				EditorUtility.SetDirty(panel);
			}

			if (panel.clipping == UIDrawCall.Clipping.SoftClip)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(80f);
				Vector2 soft = EditorGUILayout.Vector2Field("Softness", panel.clipSoftness);
				GUILayout.EndHorizontal();

				if (soft.x < 1f) soft.x = 1f;
				if (soft.y < 1f) soft.y = 1f;

				if (panel.clipSoftness != soft)
				{
					NGUIEditorTools.RegisterUndo("Clipping Change", panel);
					panel.clipSoftness = soft;
					EditorUtility.SetDirty(panel);
				}
			}

#if !UNITY_3_5 && !UNITY_4_0 && (UNITY_ANDROID || UNITY_IPHONE || UNITY_WP8 || UNITY_BLACKBERRY)
			if (PlayerSettings.targetGlesGraphics == TargetGlesGraphics.OpenGLES_1_x)
			{
				EditorGUILayout.HelpBox("Clipping requires shader support!\n\nOpen File -> Build Settings -> Player Settings -> Other Settings, then set:\n\n- Graphics Level: OpenGL ES 2.0.", MessageType.Error);
			}
#endif
		}

		if (clipping != UIDrawCall.Clipping.None && !NGUIEditorTools.IsUniform(panel.transform.lossyScale))
		{
			EditorGUILayout.HelpBox("Clipped panels must have a uniform scale, or clipping won't work properly!", MessageType.Error);
			
			if (GUILayout.Button("Auto-fix"))
			{
				NGUIEditorTools.FixUniform(panel.gameObject);
			}
		}

		for (int i = 0; i < UIDrawCall.list.size; ++i)
		{
			UIDrawCall dc = UIDrawCall.list[i];

			if (dc.panel != panel)
			{
				if (!NGUISettings.showAllDCs) continue;
				if (dc.showDetails) GUI.color = new Color(0.85f, 0.85f, 0.85f);
				else GUI.contentColor = new Color(0.85f, 0.85f, 0.85f);
			}
			else GUI.contentColor = Color.white;

			string key = dc.keyName;
			string name = key + " of " + UIDrawCall.list.size;
			if (!dc.isActive) name = name + " (HIDDEN)";
			else if (dc.panel != panel) name = name + " (" + dc.panel.name + ")";

			if (NGUIEditorTools.DrawHeader(name, key))
			{
				GUI.color = (dc.panel == panel) ? Color.white : new Color(0.8f, 0.8f, 0.8f);

				NGUIEditorTools.BeginContents();
				EditorGUILayout.ObjectField("Material", dc.material, typeof(Material), false);

				int count = 0;

				for (int b = 0; b < UIWidget.list.size; ++b)
				{
					UIWidget w = UIWidget.list[b];
					if (w.drawCall == dc)
						++count;
				}

				string myPath = NGUITools.GetHierarchy(dc.panel.cachedGameObject);
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
				EditorGUILayout.LabelField("Triangles", dc.triangles.ToString(), GUILayout.Width(120f));

				if (dc.panel != panel)
				{
					if (GUILayout.Button("Select the Panel"))
					{
						Selection.activeGameObject = dc.panel.gameObject;
					}
					GUILayout.Space(18f);
				}
				GUILayout.EndHorizontal();

				bool draw = !EditorGUILayout.Toggle("Hide", !dc.isActive);

				if (dc.isActive != draw)
				{
					dc.isActive = draw;
					UnityEditor.EditorUtility.SetDirty(dc.panel);
				}

				if (dc.panel.clipping != UIDrawCall.Clipping.None && !dc.isClipped)
				{
					EditorGUILayout.HelpBox("You must switch this material's shader to Unlit/Transparent Colored or Unlit/Premultiplied Colored in order for clipping to work.",
						MessageType.Warning);
				}

				NGUIEditorTools.EndContents();
				GUI.color = Color.white;
			}
		}
	}
}
