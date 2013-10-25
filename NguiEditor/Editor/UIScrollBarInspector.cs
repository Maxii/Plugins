//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UIScrollBar))]
public class UIScrollBarInspector : UIWidgetContainerEditor
{
	public override void OnInspectorGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);
		UIScrollBar sb = target as UIScrollBar;

		GUILayout.Space(3f);

		float val = EditorGUILayout.Slider("Value", sb.value, 0f, 1f);
		float size = EditorGUILayout.Slider("Size", sb.barSize, 0f, 1f);
		float alpha = EditorGUILayout.Slider("Alpha", sb.alpha, 0f, 1f);

		UISprite bg = sb.background;
		UISprite fg = sb.foreground;
		bool inv = sb.inverted;
		UIScrollBar.Direction dir = sb.direction;

		//GUILayout.Space(6f);

		if (NGUIEditorTools.DrawHeader("Appearance"))
		{
			NGUIEditorTools.BeginContents();
			bg = (UISprite)EditorGUILayout.ObjectField("Background", sb.background, typeof(UISprite), true);
			fg = (UISprite)EditorGUILayout.ObjectField("Foreground", sb.foreground, typeof(UISprite), true);

			GUILayout.BeginHorizontal();
			dir = (UIScrollBar.Direction)EditorGUILayout.EnumPopup("Direction", sb.direction);
			GUILayout.Space(18f);
			GUILayout.EndHorizontal();

			inv = EditorGUILayout.Toggle("Inverted", sb.inverted);

			NGUIEditorTools.EndContents();
		}

		//GUILayout.Space(3f);

		if (sb.value != val ||
			sb.barSize != size ||
			sb.background != bg ||
			sb.foreground != fg ||
			sb.direction != dir ||
			sb.inverted != inv ||
			sb.alpha != alpha)
		{
			NGUIEditorTools.RegisterUndo("Scroll Bar Change", sb);
			sb.value = val;
			sb.barSize = size;
			sb.inverted = inv;
			sb.background = bg;
			sb.foreground = fg;
			sb.direction = dir;
			sb.alpha = alpha;
			UnityEditor.EditorUtility.SetDirty(sb);
		}

		NGUIEditorTools.DrawEvents("On Value Change", sb, sb.onChange);
	}
}
