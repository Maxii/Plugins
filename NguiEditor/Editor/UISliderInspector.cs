//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UISlider))]
public class UISliderInspector : UIWidgetContainerEditor
{
	void ValidatePivot (Transform fg, string name, UISlider.Direction dir)
	{
		if (fg != null)
		{
			UISprite sprite = fg.GetComponent<UISprite>();

			if (sprite != null && sprite.type != UISprite.Type.Filled)
			{
				if (dir == UISlider.Direction.Horizontal)
				{
					if (sprite.pivot != UIWidget.Pivot.Left &&
						sprite.pivot != UIWidget.Pivot.TopLeft &&
						sprite.pivot != UIWidget.Pivot.BottomLeft)
					{
						GUI.color = new Color(1f, 0.7f, 0f);
						GUILayout.Label(name + " should use a Left pivot");
						GUI.color = Color.white;
					}
				}
				else if (sprite.pivot != UIWidget.Pivot.BottomLeft &&
						 sprite.pivot != UIWidget.Pivot.Bottom &&
						 sprite.pivot != UIWidget.Pivot.BottomRight)
				{
					GUI.color = new Color(1f, 0.7f, 0f);
					GUILayout.Label(name + " should use a Bottom pivot");
					GUI.color = Color.white;
				}
			}
		}
	}

	public override void OnInspectorGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);
		UISlider slider = target as UISlider;

		GUILayout.Space(3f);

		float sliderValue = EditorGUILayout.Slider("Value", slider.value, 0f, 1f);

		if (slider.value != sliderValue)
		{
			NGUIEditorTools.RegisterUndo("Slider Change", slider);
			slider.value = sliderValue;
			UnityEditor.EditorUtility.SetDirty(slider);
		}

		int steps = EditorGUILayout.IntSlider("Steps", slider.numberOfSteps, 0, 11);

		if (slider.numberOfSteps != steps)
		{
			NGUIEditorTools.RegisterUndo("Slider Change", slider);
			slider.numberOfSteps = steps;
			slider.ForceUpdate();
			UnityEditor.EditorUtility.SetDirty(slider);
		}

		//GUILayout.Space(6f);
		Transform fg = slider.foreground;
		Transform tb = slider.thumb;
		UISlider.Direction dir = slider.direction;

		if (NGUIEditorTools.DrawHeader("Appearance"))
		{
			NGUIEditorTools.BeginContents();

			GUILayout.BeginHorizontal();
			dir = (UISlider.Direction)EditorGUILayout.EnumPopup("Direction", slider.direction);
			GUILayout.Space(18f);
			GUILayout.EndHorizontal();

			fg = EditorGUILayout.ObjectField("Foreground", slider.foreground, typeof(Transform), true) as Transform;
			tb = EditorGUILayout.ObjectField("Thumb", slider.thumb, typeof(Transform), true) as Transform;

			// If we're using a sprite for the foreground, ensure it's using a proper pivot.
			ValidatePivot(fg, "Foreground sprite", dir);
			NGUIEditorTools.EndContents();
		}
		//GUILayout.Space(3f);

		if (slider.foreground != fg ||
			slider.thumb != tb ||
			slider.direction != dir)
		{
			NGUIEditorTools.RegisterUndo("Slider Change", slider);
			slider.foreground = fg;
			slider.thumb = tb;
			slider.direction = dir;

			if (slider.thumb != null)
			{
				slider.thumb.localPosition = Vector3.zero;
				slider.value = -1f;
				slider.value = sliderValue;
			}
			else slider.ForceUpdate();

			UnityEditor.EditorUtility.SetDirty(slider);
		}

		NGUIEditorTools.DrawEvents("On Value Change", slider, slider.onChange);
	}
}
