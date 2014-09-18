using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(UIWindow))]
public class UIWindowInspector : Editor
{
	private UIWindow mWindow;
	
	public override void OnInspectorGUI()
	{
		this.mWindow = target as UIWindow;
		
		serializedObject.Update();
		NGUIEditorTools.DrawProperty("Window ID", serializedObject, "WindowId");
		NGUIEditorTools.DrawProperty("Content Holder", serializedObject, "contentHolder");
		NGUIEditorTools.DrawProperty("Start Hidden", serializedObject, "startHidden");
		NGUIEditorTools.DrawProperty("Use Fading", serializedObject, "fading");
		NGUIEditorTools.DrawProperty("Fading Duration", serializedObject, "fadeDuration");
		serializedObject.ApplyModifiedProperties();
		
		DrawEvents();
	}
	
	public void DrawEvents()
	{
		NGUIEditorTools.DrawEvents("On Show Begin", this.mWindow, this.mWindow.onShowBegin);
		NGUIEditorTools.DrawEvents("On Show Complete", this.mWindow, this.mWindow.onShowComplete);
		
		NGUIEditorTools.DrawEvents("On Hide Begin", this.mWindow, this.mWindow.onHideBegin);
		NGUIEditorTools.DrawEvents("On Hide Complete", this.mWindow, this.mWindow.onHideComplete);
	}
}

