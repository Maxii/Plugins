using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(UIIconButton))]
public class UIIconButtonInspector : Editor
{
	protected UIIconButton mButton;
	
	/// <summary>
	/// Draw the atlas and sprite selection fields.
	/// </summary>
	
	public override void OnInspectorGUI()
	{
		EditorGUIUtility.labelWidth = 80f;
		mButton = target as UIIconButton;
		
		UISprite sprite = EditorGUILayout.ObjectField("Icon Sprite", mButton.sprite, typeof(UISprite), true) as UISprite;
		UIButton button = EditorGUILayout.ObjectField("Button", mButton.button, typeof(UIButton), true) as UIButton;
		UIIconButton.UIIconButtonTypes type = (UIIconButton.UIIconButtonTypes)EditorGUILayout.EnumPopup("Icon", mButton.type);
		
		if (mButton.sprite != sprite || mButton.button != button || mButton.type != type)
		{
			mButton.sprite = sprite;
			mButton.button = button;
			mButton.type = type;
		}
	}
	
	public override bool HasPreviewGUI()	
	{
		return true;
	}
	
	/// <summary>
	/// Draw the sprite preview.
	/// </summary>
	
	public override void OnPreviewGUI(Rect rect, GUIStyle background)
	{
		if (mButton == null || mButton.sprite == null || !mButton.sprite.isValid) return;

		Texture2D tex = mButton.sprite.mainTexture as Texture2D;
		if (tex == null) return;
		
		UISpriteData sd = mButton.sprite.atlas.GetSprite(mButton.sprite.spriteName);
		NGUIEditorTools.DrawSprite(tex, rect, sd, mButton.sprite.color);
	}
}
