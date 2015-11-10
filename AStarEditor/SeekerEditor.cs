using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Pathfinding;

[CustomEditor(typeof(Seeker))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Seeker))]
public class SeekerEditor : Editor {

	static bool modifiersOpen;
	static bool tagPenaltiesOpen;

	List<IPathModifier> mods;

	public override void OnInspectorGUI () {
		DrawDefaultInspector ();

		var script = target as Seeker;

		Undo.RecordObject ( script, "modify settings on Seeker");

		// Show a dropdown selector for the tags that this seeker can traverse
		// A callback is necessary because Unity's GenericMenu uses callbacks
		EditorGUILayoutx.TagMaskField (new GUIContent ("Valid Tags"), script.traversableTags, result => script.traversableTags = result);

		EditorGUI.indentLevel=0;
		tagPenaltiesOpen = EditorGUILayout.Foldout (tagPenaltiesOpen,new GUIContent ("Tag Penalties","Penalties for each tag"));
		if (tagPenaltiesOpen) {
			EditorGUI.indentLevel=2;
			string[] tagNames = AstarPath.FindTagNames ();
			for (int i=0;i<script.tagPenalties.Length;i++) {
				int tmp = EditorGUILayout.IntField ((i < tagNames.Length ? tagNames[i] : "Tag "+i),(int)script.tagPenalties[i]);
				if (tmp < 0) tmp = 0;

				// If the new value is different than the old one
				// Update the value and mark the script as dirty
				if (script.tagPenalties[i] != tmp) {
					script.tagPenalties[i] = tmp;
					EditorUtility.SetDirty (target);
				}
			}
			if (GUILayout.Button ("Edit Tag Names...")) {
				AstarPathEditor.EditTags ();
			}
		}
		EditorGUI.indentLevel=1;

		//Do some loading and checking
		if (!AstarPathEditor.stylesLoaded) {
			AstarPathEditor.LoadStyles ();
		}

		GUIStyle helpBox = GUI.skin.GetStyle ("helpBox");

		if (mods == null) {
			mods = new List<IPathModifier>(script.GetComponents<MonoModifier>() as IPathModifier[]);
		} else {
			mods.Clear ();
			mods.AddRange (script.GetComponents<MonoModifier>() as IPathModifier[]);
		}

		mods.Add (script.startEndModifier);

		bool changed = true;
		while (changed) {
			changed = false;
			for (int i=0;i<mods.Count-1;i++) {
				if (mods[i].Priority < mods[i+1].Priority) {
					IPathModifier tmp = mods[i+1];
					mods[i+1] = mods[i];
					mods[i] = tmp;
					changed = true;
				}
			}
		}

		for (int i=0;i<mods.Count;i++) {
			if (mods.Count-i != mods[i].Priority) {
				mods[i].Priority = mods.Count-i;
				GUI.changed = true;
				EditorUtility.SetDirty (target);

				SetModifierDirty (mods[i]);
			}
		}

		bool modifierErrors = false;

		IPathModifier prevMod = mods[0];

		//Loops through all modifiers and checks if there are any errors in converting output between modifiers
		for (int i=1;i<mods.Count;i++) {
			var monoMod = mods[i] as MonoModifier;
			if ((prevMod as MonoModifier) != null && !(prevMod as MonoModifier).enabled) {
				if (monoMod == null || monoMod.enabled) prevMod = mods[i];
				continue;
			}

			if ((monoMod == null || monoMod.enabled) && prevMod != mods[i] && !ModifierConverter.CanConvert (prevMod.output, mods[i].input)) {
				modifierErrors = true;
			}

			if (monoMod == null || monoMod.enabled) {
				prevMod = mods[i];
			}
		}

		EditorGUI.indentLevel = 0;

		modifiersOpen = EditorGUILayout.Foldout (modifiersOpen, "Modifiers Priorities"+(modifierErrors ? " - Errors in modifiers!" : ""));

		if (modifiersOpen) {
			if (GUILayout.Button ("Modifiers attached to this gameObject are listed here.\nModifiers with a higher priority (higher up in the list) will be executed first.\nClick here for more info",helpBox)) {
				Application.OpenURL (AstarUpdateChecker.GetURL ("modifiers"));
			}

			EditorGUILayout.HelpBox ("Original or All can be converted to anything\n" +
			    "NodePath can be converted to VectorPath\n"+
				"VectorPath can only be used as VectorPath\n"+
			    "Vector takes both VectorPath and StrictVectorPath\n"+
			    "Strict... can be converted to the non-strict variant", MessageType.None );

			prevMod = mods[0];

			for (int i=0;i<mods.Count;i++) {

				var monoMod = mods[i] as MonoModifier;

				Color prevCol = GUI.color;
				if (monoMod != null && !monoMod.enabled) {
					GUI.color *= new Color (1,1,1,0.5F);
				}

				GUILayout.BeginVertical (GUI.skin.box);

				if (i > 0) {
					if ((prevMod as MonoModifier) != null && !(prevMod as MonoModifier).enabled) {
						prevMod = mods[i];
					} else {

						if ((monoMod == null || monoMod.enabled) && !ModifierConverter.CanConvert (prevMod.output, mods[i].input)) {
							//GUILayout.BeginHorizontal ();
							//GUILayout.Space (28);
							GUIUtilityx.SetColor (new Color (0.8F,0,0));
							GUILayout.Label ("Cannot convert "+prevMod.GetType ().Name+"'s output to "+mods[i].GetType ().Name+"'s input\nRearranging the modifiers might help",EditorStyles.whiteMiniLabel);
							GUIUtilityx.ResetColor ();
							//GUILayout.EndHorizontal ();
						}

						if (monoMod == null || monoMod.enabled) {
							prevMod = mods[i];
						}
					}
				}

				GUILayout.Label ("Input: "+mods[i].input,EditorStyles.wordWrappedMiniLabel);
				int newPrio = EditorGUILayoutx.UpDownArrows (new GUIContent (ObjectNames.NicifyVariableName (mods[i].GetType ().ToString ())),mods[i].Priority, EditorStyles.label, AstarPathEditor.upArrow,AstarPathEditor.downArrow);

				GUILayout.Label ("Output: "+mods[i].output,EditorStyles.wordWrappedMiniLabel);

				GUILayout.EndVertical ();

				int diff = newPrio - mods[i].Priority;

				if (i > 0 && diff > 0) {
					mods[i-1].Priority = mods[i].Priority;
					SetModifierDirty (mods[i-1]);
				} else if (i < mods.Count-1 && diff < 0) {
					mods[i+1].Priority = mods[i].Priority;
					SetModifierDirty (mods[i+1]);
				}

				if (mods[i].Priority != newPrio) {
					mods[i].Priority = newPrio;
					SetModifierDirty (mods[i]);
				}


				GUI.color = prevCol;
			}

			EditorGUI.indentLevel-= 2;

		}

		if (GUI.changed) {
			EditorUtility.SetDirty (target);
		}
	}

	/** Calls EditorUtility.SetDirty on the modifier */
	static void SetModifierDirty (IPathModifier modifier) {
		var unityObj = modifier as UnityEngine.Object;

		// Try to mark the modifier as dirty
		// Not all modifiers are unity objects
		if (unityObj != null) {
			EditorUtility.SetDirty (unityObj);
		}
	}
}
