using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
#if UNITY_3_5
[CustomEditor(typeof(UIButtonExtended))]
#else
[CustomEditor(typeof(UIButtonExtended), true)]
#endif
public class UIButtonExtendedInspector : UIButtonEditor
{
	protected override void DrawProperties ()
	{
		base.DrawProperties();

		UIButtonExtended btn = target as UIButtonExtended;

		if (NGUIEditorTools.DrawHeader("Extension"))
		{
			NGUIEditorTools.BeginContents();
			EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
			{
				NGUIEditorTools.DrawProperty("Target", serializedObject, "additionalTarget");

				if (btn.additionalTarget != null)
					this.DrawColorsAdditional();
			}
			EditorGUI.EndDisabledGroup();
			NGUIEditorTools.EndContents();
		}
	}

	protected void DrawColorsAdditional()
	{
		UIButtonExtended btn = target as UIButtonExtended;
		
		if (btn.additionalTarget != null)
		{
			EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
			{
				SerializedObject obj = new SerializedObject(btn.additionalTarget);
				obj.Update();
				NGUIEditorTools.DrawProperty("Normal", obj, "mColor");
				obj.ApplyModifiedProperties();
			}
			EditorGUI.EndDisabledGroup();
		}
		
		NGUIEditorTools.DrawProperty("Hover", serializedObject, "additionalHover");
		NGUIEditorTools.DrawProperty("Pressed", serializedObject, "additionalPressed");
		NGUIEditorTools.DrawProperty("Disabled", serializedObject, "additionalDisabled");
	}
}