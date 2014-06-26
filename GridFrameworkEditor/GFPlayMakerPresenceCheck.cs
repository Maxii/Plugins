using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>Class that checks whether PlayMaker is present.</summary>
/*
public class GFPlayMakerPresenceCheck : AssetPostprocessor {
	private static string PlayMakerTypeCheck = "HutongGames.PlayMaker.Actions.ActivateGameObject, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

	static void OnPostprocessAllAssets(string[] importedAssets,string[] deletedAssets,string[] movedAssets,string[] movedFromAssetPaths) {
		//check here if we have access to a PlayMaker class, if we can enable the menu item.
		if (System.Type.GetType(PlayMakerTypeCheck) != null) {
			GFMenuItems.ReplaceInFile (Application.dataPath + "/Editor/Grid Framework/GFMenuItems.cs", "//#define PLAYMAKER_PRESENT", "#define PLAYMAKER_PRESENT");
		} else {
			GFMenuItems.ReplaceInFile (Application.dataPath + "/Editor/Grid Framework/GFMenuItems.cs", "#define PLAYMAKER_PRESENT", "//#define PLAYMAKER_PRESENT");
		}
	}
}
*/