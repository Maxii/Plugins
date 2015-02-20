
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
#define UNITY_4
#endif

#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_3_5 || UNITY_3_4 || UNITY_3_3
#define UNITY_LE_4_3
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Pathfinding;

namespace Pathfinding {
	/** Simple GUI utility functions */
	public class GUIUtilityx {
		
		private static Color prevCol;

		public static void SetColor (Color col) {
			prevCol = GUI.color;
			GUI.color = col;
		}
		
		public static void ResetColor () {
			GUI.color = prevCol;
		}
	}
	
	/** Handles fading effects and also some custom GUI functions such as LayerMaskField.
	 * \warning The code is not pretty.
	 */
	public class EditorGUILayoutx {
	
		Rect fadeAreaRect;
		Rect lastAreaRect;

		private Dictionary<string, FadeArea> fadeAreas;

		/** Global info about which editor is currently active.
		 * \todo Ugly, rewrite this class at some point...
		 */
		public static Editor editor;
		
		public static GUIStyle defaultAreaStyle;
		public static GUIStyle defaultLabelStyle;
		public static GUIStyle stretchStyle;
		public static GUIStyle stretchStyleThin;
		
		private static float speed = 6;
		private static bool fade = true;
		public static bool fancyEffects = true;
		
		private Stack<FadeArea> fadeAreaStack;
		
		public void RemoveID (string id) {
			if (fadeAreas == null) {
				return;
			}
			
			fadeAreas.Remove (id);
		}
		
		public bool DrawID (string id) {
			if (fadeAreas == null) {
				return false;
			}

			return fadeAreas[id].Show ();
		}

		public class FadeArea {
			public Rect currentRect;
			public Rect lastRect;
			public float value;
			public float lastUpdate;
			
			/** Is this area open.
			 * This is not the same as if any contents are visible, use #Show for that.
			 */
			public bool open;
			
			public Color preFadeColor;
			
			/** Update the visibility in Layout to avoid complications with different events not drawing the same thing */
			private bool visibleInLayout;
			
			public void Switch () {
				lastRect = currentRect;
			}
			
			public FadeArea (bool open) {
				value = open ? 1 : 0;
			}
			
			/** Should anything inside this FadeArea be drawn.
			 * Should be called every frame ( in all events ) for best results.
			  */
			public bool Show () {
				
				bool v =  open || value > 0F;
				if ( Event.current.type == EventType.Layout ) {
					visibleInLayout = v;
				}
				
				return visibleInLayout;
			}
			
			public static implicit operator bool (FadeArea o) {
				return o.open;
			}
		}

		/** Make sure the stack is cleared at the start of a frame */
		public void ClearFadeAreaStack () {
			if ( fadeAreaStack != null ) fadeAreaStack.Clear ();
		}

		public FadeArea BeginFadeArea (bool open,string label, string id) {
			return BeginFadeArea (open,label,id, defaultAreaStyle);
		}
		
		public FadeArea BeginFadeArea (bool open,string label, string id, GUIStyle areaStyle) {
			return BeginFadeArea (open, label, id, areaStyle, defaultLabelStyle);
		}
	
		public FadeArea BeginFadeArea (bool open,string label, string id, GUIStyle areaStyle, GUIStyle labelStyle) {

			Color tmp1 = GUI.color;
			
			FadeArea fadeArea = BeginFadeArea (open,id, 20,areaStyle);
			
			Color tmp2 = GUI.color;
			GUI.color = tmp1;
			
			if (label != "") {
				if (GUILayout.Button (label,labelStyle)) {
					fadeArea.open = !fadeArea.open;
					editor.Repaint ();
				}
			}
			
			GUI.color = tmp2;

			return fadeArea;
		}

		public FadeArea BeginFadeArea (bool open, string id) {
			return BeginFadeArea (open,id,0);
		}

		public FadeArea BeginFadeArea (bool open, string id, float minHeight) {
			return BeginFadeArea (open, id, minHeight, GUIStyle.none);
		}
	
		public FadeArea BeginFadeArea (bool open, string id, float minHeight, GUIStyle areaStyle) {
			
			if (editor == null) {
				Debug.LogError ("You need to set the 'EditorGUIx.editor' variable before calling this function");
				return null;
			}
			
			if (stretchStyle == null) {
				
				stretchStyle = new GUIStyle ();
				stretchStyle.stretchWidth = true;
			}
			
			if (fadeAreaStack == null) {
				fadeAreaStack = new Stack<FadeArea>();
			}
			
			if (fadeAreas == null) {
				fadeAreas = new Dictionary<string, FadeArea> ();
			}
			
			if (!fadeAreas.ContainsKey (id)) {
				fadeAreas.Add (id,new FadeArea (open));
			}
			
			FadeArea fadeArea = fadeAreas[id];
			
			fadeAreaStack.Push (fadeArea);
			
			fadeArea.open = open;
			
			//Make sure the area fills the full width
			areaStyle.stretchWidth = true;
			
			Rect lastRect = fadeArea.lastRect;
			
			if (!fancyEffects) {
				fadeArea.value = open ? 1F : 0F;
				lastRect.height -= minHeight;
				lastRect.height = open ? lastRect.height : 0;
				lastRect.height += minHeight;
			} else {
			
				lastRect.height = lastRect.height < minHeight ? minHeight : lastRect.height;
				lastRect.height -= minHeight;
				float faded = Hermite (0F,1F,fadeArea.value);
				lastRect.height *= faded;
				lastRect.height += minHeight;
				lastRect.height = Mathf.Round (lastRect.height);
			}
			
			Rect gotLastRect = GUILayoutUtility.GetRect (new GUIContent (),areaStyle,GUILayout.Height (lastRect.height));
			
			//The clipping area, also drawing background
			GUILayout.BeginArea (lastRect,areaStyle);
			
			Rect newRect = EditorGUILayout.BeginVertical ();
			
			if (Event.current.type == EventType.Repaint || Event.current.type == EventType.ScrollWheel) {
				newRect.x = gotLastRect.x;
				newRect.y = gotLastRect.y;
				newRect.width = gotLastRect.width;//stretchWidthRect.width;
				newRect.height += areaStyle.padding.top+ areaStyle.padding.bottom;
				fadeArea.currentRect = newRect;
				
				if (fadeArea.lastRect != newRect) {
					//@Fix - duplicate
					//fadeArea.lastUpdate = Time.realtimeSinceStartup;
					editor.Repaint ();
				}
				
				fadeArea.Switch ();
			}
			if (Event.current.type == EventType.Repaint) {
				float value = fadeArea.value;
				float targetValue = open ? 1F : 0F;
				
				float newRectHeight = fadeArea.lastRect.height;
				float deltaHeight = 400F / newRectHeight;
				
				float deltaTime = Mathf.Clamp (Time.realtimeSinceStartup-fadeAreas[id].lastUpdate,0.00001F,0.05F);
				
				deltaTime *= Mathf.Lerp (deltaHeight*deltaHeight*0.01F, 0.8F, 0.9F);
				
				fadeAreas[id].lastUpdate = Time.realtimeSinceStartup;
				
				//Useless, but fun feature
				if (Event.current.shift) {
					deltaTime *= 0.05F;
				}
				
				if (Mathf.Abs(targetValue-value) > 0.001F) {
					
					float time = Mathf.Clamp01 (deltaTime*speed);
					value += time*Mathf.Sign (targetValue-value);
					editor.Repaint ();
				} else {
					value = Mathf.Round (value);
				}
				
				fadeArea.value = Mathf.Clamp01 (value);
			}
			
			if (fade) {
				Color c = GUI.color;
				fadeArea.preFadeColor = c;
				c.a *= fadeArea.value;
				GUI.color = c;
			}
			
			fadeArea.open = open;
				
			return fadeArea;
		}
		
		public void EndFadeArea () {
			
			if (fadeAreaStack.Count <= 0) {
				Debug.LogError ("You are popping more Fade Areas than you are pushing, make sure they are balanced");
				return;
			}
			
			FadeArea fadeArea = fadeAreaStack.Pop ();

			EditorGUILayout.EndVertical ();
			GUILayout.EndArea ();
			
			if (fade) {
				GUI.color = fadeArea.preFadeColor;
			}
			
		}

		/** Returns width of current editor indent.
		 * Unity seems to use 13+6*EditorGUI.indentLevel in U3
		 * and 15*indent - (indent > 1 ? 2 : 0) or something like that in U4
		 */
		public static int IndentWidth () {
			#if UNITY_4
			//Works well for indent levels 0,1,2 at least
			return 15*EditorGUI.indentLevel - (EditorGUI.indentLevel > 1 ? 2 : 0);
			#else
			return 13+6*EditorGUI.indentLevel;
			#endif
		}

		/** Begin horizontal indent for the next control.
		 * Fake "real" indent when using EditorGUIUtility.LookLikeControls.\n
		 * Forumula used is 13+6*EditorGUI.indentLevel
		 */
		public static void BeginIndent () {
			GUILayout.BeginHorizontal ();
			GUILayout.Space (IndentWidth());
		}
		
		/** End indent.
		 * Actually just a EndHorizontal call.
		 * \see BeginIndent
		 */
		public static void EndIndent () {
			GUILayout.EndHorizontal ();
		}
		
		public static int SingleTagField (string label, int value) {
			
			string[] tagNames = AstarPath.FindTagNames ();
			value = value < 0 ? 0 : value;
			value = value >= tagNames.Length ? tagNames.Length-1 : value;

			value = EditorGUILayout.IntPopup (label,value,tagNames,new int[] {0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31});
			
			return value;
		}
		
		public static void SetTagField (GUIContent label, ref Pathfinding.TagMask value) {
			
			GUILayout.BeginHorizontal ();

			EditorGUIUtility.LookLikeControls();
			EditorGUILayout.PrefixLabel (label,EditorStyles.layerMaskField);
			
			string text = "";
			if (value.tagsChange == 0) text = "Nothing";
			else if (value.tagsChange == ~0) text = "Everything";
			else {
				text = System.Convert.ToString (value.tagsChange,2);
			}
			
			string[] tagNames = AstarPath.FindTagNames ();
			
			if (GUILayout.Button (text,EditorStyles.layerMaskField,GUILayout.ExpandWidth (true))) {

				GenericMenu menu = new GenericMenu ();
				
				menu.AddItem (new GUIContent ("Everything"),value.tagsChange == ~0, value.SetValues, new Pathfinding.TagMask (~0,value.tagsSet));
				menu.AddItem (new GUIContent ("Nothing"),value.tagsChange == 0, value.SetValues, new Pathfinding.TagMask (0,value.tagsSet));
				
				for (int i=0;i<tagNames.Length;i++) {
					bool on = (value.tagsChange >> i & 0x1) != 0;
					Pathfinding.TagMask result = new Pathfinding.TagMask (on ? value.tagsChange & ~(1 << i) : value.tagsChange | 1<<i,value.tagsSet);
					menu.AddItem (new GUIContent (tagNames[i]),on,value.SetValues, result);
				}
				
				menu.AddItem (new GUIContent ("Edit Tags..."),false,AstarPathEditor.EditTags);
				menu.ShowAsContext ();
				
				Event.current.Use ();
			}
	
	#if UNITY_LE_4_3
			EditorGUIUtility.LookLikeInspector();
	#endif
			GUILayout.EndHorizontal ();
			
		}
		
		public static void TagsMaskField (GUIContent changeLabel, GUIContent setLabel,ref Pathfinding.TagMask value) {
			
			GUILayout.BeginHorizontal ();

			EditorGUIUtility.LookLikeControls();
			EditorGUILayout.PrefixLabel (changeLabel,EditorStyles.layerMaskField);
			
			string text = "";
			if (value.tagsChange == 0) text = "Nothing";
			else if (value.tagsChange == ~0) text = "Everything";
			else {
				text = System.Convert.ToString (value.tagsChange,2);
			}
			
			if (GUILayout.Button (text,EditorStyles.layerMaskField,GUILayout.ExpandWidth (true))) {

				GenericMenu menu = new GenericMenu ();
				
				menu.AddItem (new GUIContent ("Everything"),value.tagsChange == ~0, value.SetValues, new Pathfinding.TagMask (~0,value.tagsSet));
				menu.AddItem (new GUIContent ("Nothing"),value.tagsChange == 0, value.SetValues, new Pathfinding.TagMask (0,value.tagsSet));
				
				for (int i=0;i<32;i++) {
					bool on = (value.tagsChange >> i & 0x1) != 0;
					Pathfinding.TagMask result = new Pathfinding.TagMask (on ? value.tagsChange & ~(1 << i) : value.tagsChange | 1<<i,value.tagsSet);
					menu.AddItem (new GUIContent (""+i),on,value.SetValues, result);
				}
				
				menu.ShowAsContext ();
				
				Event.current.Use ();
			}
	
	#if UNITY_LE_4_3
			EditorGUIUtility.LookLikeInspector();
	#endif
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();

			EditorGUIUtility.LookLikeControls();
			EditorGUILayout.PrefixLabel (setLabel,EditorStyles.layerMaskField);
			
			text = "";
			if (value.tagsSet == 0) text = "Nothing";
			else if (value.tagsSet == ~0) text = "Everything";
			else {
				text = System.Convert.ToString (value.tagsSet,2);
			}
			
			if (GUILayout.Button (text,EditorStyles.layerMaskField,GUILayout.ExpandWidth (true))) {
				

				GenericMenu menu = new GenericMenu ();
				
				if (value.tagsChange != 0)	menu.AddItem (new GUIContent ("Everything"),value.tagsSet == ~0, value.SetValues, new Pathfinding.TagMask (value.tagsChange,~0));
				else				menu.AddDisabledItem (new GUIContent ("Everything"));
				
				menu.AddItem (new GUIContent ("Nothing"),value.tagsSet == 0, value.SetValues, new Pathfinding.TagMask (value.tagsChange,0));
				
				for (int i=0;i<32;i++) {
					bool enabled = (value.tagsChange >> i & 0x1) != 0;
					bool on = (value.tagsSet >> i & 0x1) != 0;
					
					Pathfinding.TagMask result = new Pathfinding.TagMask (value.tagsChange, on ? value.tagsSet & ~(1 << i) : value.tagsSet | 1<<i);
					
					if (enabled)	menu.AddItem (new GUIContent (""+i),on,value.SetValues, result);
					else	menu.AddDisabledItem (new GUIContent (""+i));
				}
				
				menu.ShowAsContext ();
				
				Event.current.Use ();
			}
			
	#if UNITY_LE_4_3
			EditorGUIUtility.LookLikeInspector();
	#endif
			GUILayout.EndHorizontal ();
		}
		
		
		public static int UpDownArrows (GUIContent label, int value, GUIStyle labelStyle, GUIStyle upArrow, GUIStyle downArrow) {
			
			GUILayout.BeginHorizontal ();
			GUILayout.Space (EditorGUI.indentLevel*10);
			GUILayout.Label (label,labelStyle,GUILayout.Width (170));
			
			if (downArrow == null || upArrow == null) {
				upArrow = GUI.skin.FindStyle ("Button");
				downArrow = upArrow;
			}

			if (GUILayout.Button ("",upArrow,GUILayout.Width (16),GUILayout.Height (12))) {
				value++;
			}
			if (GUILayout.Button ("",downArrow,GUILayout.Width (16),GUILayout.Height (12))) {
				value--;
			}

			GUILayout.Space (100);
			GUILayout.EndHorizontal ();
			return value;
		}
		
		public static bool UnityTagMaskList (GUIContent label, bool foldout, List<string> tagMask) {
			if (tagMask == null) throw new System.ArgumentNullException ("tagMask");
			if (EditorGUILayout.Foldout (foldout, label)) {
				EditorGUI.indentLevel++;
				GUILayout.BeginVertical();
				for (int i=0;i<tagMask.Count;i++) {
					tagMask[i] = EditorGUILayout.TagField (tagMask[i]);
				}
				GUILayout.BeginHorizontal();
				if (GUILayout.Button ("Add Tag")) tagMask.Add ("Untagged");
				
				EditorGUI.BeginDisabledGroup (tagMask.Count == 0);
				if (GUILayout.Button ("Remove Last")) tagMask.RemoveAt (tagMask.Count-1);
				EditorGUI.EndDisabledGroup();
				
				GUILayout.EndHorizontal();
				GUILayout.EndVertical();
				EditorGUI.indentLevel--;
				return true;
			}
			return false;
		}
		
		public static LayerMask LayerMaskField (string label, LayerMask selected) {
			return LayerMaskField (label,selected,true);
		}
		
		public static List<string> layers;
		public static List<int> layerNumbers;
		public static string[] layerNames;
		public static long lastUpdateTick;
		
		/** Displays a LayerMask field.
		 * \param label Label to display
		 * \param showSpecial Use the Nothing and Everything selections
		 * \param selected Current LayerMask
		 * \note Unity 3.5 and up will use the EditorGUILayout.MaskField instead of a custom written one.
		 */
		public static LayerMask LayerMaskField (string label, LayerMask selected, bool showSpecial) {
			
	#if !UNITY_3_4
			//Unity 3.5 and up
			
			if (layers == null || (System.DateTime.UtcNow.Ticks - lastUpdateTick > 10000000L && Event.current.type == EventType.Layout)) {
				lastUpdateTick = System.DateTime.UtcNow.Ticks;
				if (layers == null) {
					layers = new List<string>();
					layerNumbers = new List<int>();
					layerNames = new string[4];
				} else {
					layers.Clear ();
					layerNumbers.Clear ();
				}
				
				int emptyLayers = 0;
				for (int i=0;i<32;i++) {
					string layerName = LayerMask.LayerToName (i);
					
					if (layerName != "") {
						
						for (;emptyLayers>0;emptyLayers--) layers.Add ("Layer "+(i-emptyLayers));
						layerNumbers.Add (i);
						layers.Add (layerName);
					} else {
						emptyLayers++;
					}
				}
				
				if (layerNames.Length != layers.Count) {
					layerNames = new string[layers.Count];
				}
				for (int i=0;i<layerNames.Length;i++) layerNames[i] = layers[i];
			}
			
			selected.value =  EditorGUILayout.MaskField (label,selected.value,layerNames);
			
			return selected;
	#else
			
			if (layers == null) {
				layers = new List<string>();
				layerNumbers = new List<int>();
			} else {
				layers.Clear ();
				layerNumbers.Clear ();
			}
			
			string selectedLayers = "";
			
			for (int i=0;i<32;i++) {
				
				string layerName = LayerMask.LayerToName (i);
				
				if (layerName != "") {
					if (selected == (selected | (1 << i))) {
						
						if (selectedLayers == "") {
							selectedLayers = layerName;
						} else {
							selectedLayers = "Mixed";
						}
					}
				}
			}
			
			if (Event.current.type != EventType.MouseDown && Event.current.type != EventType.ExecuteCommand) {
				if (selected.value == 0) {
					layers.Add ("Nothing");
				} else if (selected.value == -1) {
					layers.Add ("Everything");
				} else {
					layers.Add (selectedLayers);
				}
				layerNumbers.Add (-1);
			}
			
			if (showSpecial) {
				layers.Add ((selected.value == 0 ? "[X] " : "     ") + "Nothing");
				layerNumbers.Add (-2);
				
				layers.Add ((selected.value == -1 ? "[X] " : "     ") + "Everything");
				layerNumbers.Add (-3);
			}
			
			for (int i=0;i<32;i++) {
				
				string layerName = LayerMask.LayerToName (i);
				
				if (layerName != "") {
					if (selected == (selected | (1 << i))) {
						layers.Add ("[X] "+layerName);
					} else {
						layers.Add ("     "+layerName);
					}
					layerNumbers.Add (i);
				}
			}
			
			bool preChange = GUI.changed;
			
			GUI.changed = false;
			
			int newSelected = 0;
			
			if (Event.current.type == EventType.MouseDown) {
				newSelected = -1;
			}
			
			newSelected = EditorGUILayout.Popup (label,newSelected,layers.ToArray(),EditorStyles.layerMaskField);
			
			if (GUI.changed && newSelected >= 0) {
				
				int preSelected = selected;
				
				if (showSpecial && newSelected == 0) {
					selected = 0;
				} else if (showSpecial && newSelected == 1) {
					selected = -1;
				} else {
					
					if (selected == (selected | (1 << layerNumbers[newSelected]))) {
						selected &= ~(1 << layerNumbers[newSelected]);
						//Debug.Log ("Set Layer "+LayerMask.LayerToName (LayerNumbers[newSelected]) + " To False "+selected.value);
					} else {
						//Debug.Log ("Set Layer "+LayerMask.LayerToName (LayerNumbers[newSelected]) + " To True "+selected.value);
						selected = selected | (1 << layerNumbers[newSelected]);
					}
				}
				
				if (selected == preSelected) {
					GUI.changed = false;
				} else {
					//Debug.Log ("Difference made");
				}
			}
			
			GUI.changed = preChange || GUI.changed;
			
			return selected;
	#endif
		}

#region Interpolation functions

		public static float Hermite(float start, float end, float value) {
	        return Mathf.Lerp(start, end, value * value * (3.0f - 2.0f * value));
	    }
	    
	    public static float Sinerp(float start, float end, float value) {
	        return Mathf.Lerp(start, end, Mathf.Sin(value * Mathf.PI * 0.5f));
	    }
	
	    public static float Coserp(float start, float end, float value) {
	        return Mathf.Lerp(start, end, 1.0f - Mathf.Cos(value * Mathf.PI * 0.5f));
	    }
	 
	    public static float Berp(float start, float end, float value) {
	        value = Mathf.Clamp01(value);
	        value = (Mathf.Sin(value * Mathf.PI * (0.2f + 2.5f * value * value * value)) * Mathf.Pow(1f - value, 2.2f) + value) * (1f + (1.2f * (1f - value)));
	        return start + (end - start) * value;
	    }

#endregion
	}
}