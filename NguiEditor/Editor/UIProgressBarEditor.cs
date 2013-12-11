//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(UIProgressBar))]
public class UIProgressBarEditor : UIWidgetContainerEditor
{
	public override void OnInspectorGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);

		serializedObject.Update();

		GUILayout.Space(3f);

		DrawLegacyFields();

		GUILayout.BeginHorizontal();
		SerializedProperty sp = NGUIEditorTools.DrawProperty("Steps", serializedObject, "numberOfSteps", GUILayout.Width(110f));
		if (sp.intValue == 0) GUILayout.Label("= unlimited");
		GUILayout.EndHorizontal();

		OnDrawExtraFields();

		if (NGUIEditorTools.DrawHeader("Appearance"))
		{
			NGUIEditorTools.BeginContents();
			NGUIEditorTools.DrawProperty("Foreground", serializedObject, "mFG");
			NGUIEditorTools.DrawProperty("Background", serializedObject, "mBG");
			NGUIEditorTools.DrawProperty("Thumb", serializedObject, "thumb");

			GUILayout.BeginHorizontal();
			NGUIEditorTools.DrawProperty("Direction", serializedObject, "mFill");
			GUILayout.Space(18f);
			GUILayout.EndHorizontal();

			OnDrawAppearance();
			NGUIEditorTools.EndContents();
		}

		UIProgressBar sb = target as UIProgressBar;
		NGUIEditorTools.DrawEvents("On Value Change", sb, sb.onChange);
		serializedObject.ApplyModifiedProperties();
	}

	protected virtual void DrawLegacyFields()
	{
		UIProgressBar sb = target as UIProgressBar;
		float val = EditorGUILayout.Slider("Value", sb.value, 0f, 1f);
		float alpha = EditorGUILayout.Slider("Alpha", sb.alpha, 0f, 1f);

		if (sb.value != val ||
			sb.alpha != alpha)
		{
			NGUIEditorTools.RegisterUndo("Scroll Bar Change", sb);
			sb.value = val;
			sb.alpha = alpha;
			UnityEditor.EditorUtility.SetDirty(sb);
		}
	}

	protected virtual void OnDrawExtraFields () { }
	protected virtual void OnDrawAppearance () { }
}
