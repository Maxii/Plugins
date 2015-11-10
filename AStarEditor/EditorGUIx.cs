using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using Pathfinding;

namespace Pathfinding {
	/** Simple GUI utility functions */
	public static class GUIUtilityx {

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

		Dictionary<string, FadeArea> fadeAreas;

		/** Global info about which editor is currently active.
		 * \todo Ugly, rewrite this class at some point...
		 */
		public static Editor editor;

		public static GUIStyle defaultAreaStyle;
		public static GUIStyle defaultLabelStyle;

		const float speed = 8;
		readonly bool fade = true;
		public static bool fancyEffects = true;

		Stack<FadeArea> fadeAreaStack;

		static List<string> layers;
		static string[] layerNames;
		static long lastUpdateTick;

		/** Tag names and an additional 'Edit Tags...' entry.
		 * Used for SingleTagField
		 */
		static string[] tagNamesAndEditTagsButton;

		/** Last tiem tagNamesAndEditTagsButton was updated.
		 * Uses EditorApplication.timeSinceStartup
		 */
		static double timeLastUpdatedTagNames;

		public void RemoveID (string id) {
			if (fadeAreas == null) {
				return;
			}

			fadeAreas.Remove (id);
		}

		public bool DrawID (string id) {
			return fadeAreas != null && fadeAreas[id].Show ();
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
				if ( Event.current.type == EventType.Layout ) {
					visibleInLayout = open || value > 0F;
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

			if (fadeAreaStack == null) {
				fadeAreaStack = new Stack<FadeArea>();
			}

			if (fadeAreas == null) {
				fadeAreas = new Dictionary<string, FadeArea> ();
			}

			FadeArea fadeArea;
			if (!fadeAreas.TryGetValue (id, out fadeArea)) {
				fadeArea = new FadeArea (open);
				fadeAreas.Add (id, fadeArea);
			}

			fadeAreaStack.Push (fadeArea);

			fadeArea.open = open;

			// Make sure the area fills the full width
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

			Rect gotLastRect = GUILayoutUtility.GetRect (new GUIContent (), areaStyle, GUILayout.Height (lastRect.height));

			//The clipping area, also drawing background
			GUILayout.BeginArea (lastRect,areaStyle);

			Rect newRect = EditorGUILayout.BeginVertical ();

			if (Event.current.type == EventType.Repaint || Event.current.type == EventType.ScrollWheel) {
				newRect.x = gotLastRect.x;
				newRect.y = gotLastRect.y;
				newRect.width = gotLastRect.width;
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
		static int IndentWidth () {
			//Works well for indent levels 0,1,2 at least
			return 15*EditorGUI.indentLevel - (EditorGUI.indentLevel > 1 ? 2 : 0);
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

		public static int TagField (string label, int value) {

			// Make sure the tagNamesAndEditTagsButton is relatively up to date
			if (tagNamesAndEditTagsButton == null || EditorApplication.timeSinceStartup - timeLastUpdatedTagNames > 1) {
				timeLastUpdatedTagNames = EditorApplication.timeSinceStartup;
				var tagNames = AstarPath.FindTagNames ();
				tagNamesAndEditTagsButton = new string[tagNames.Length+1];
				tagNames.CopyTo(tagNamesAndEditTagsButton, 0);
				tagNamesAndEditTagsButton[tagNamesAndEditTagsButton.Length-1] = "Edit Tags...";
			}

			// Tags are between 0 and 31
			value = Mathf.Clamp (value, 0, 31);

			var newValue = EditorGUILayout.IntPopup (label,value,tagNamesAndEditTagsButton,new [] {0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31, -1});

			// Last element corresponds to the 'Edit Tags...' entry. Open the tag editor
			if (newValue == -1) {
				AstarPathEditor.EditTags();
			} else {
				value = newValue;
			}

			return value;
		}

		public static void TagMaskField (GUIContent label, int value, System.Action<int> callback) {

			GUILayout.BeginHorizontal ();

			EditorGUIUtility.LookLikeControls();
			EditorGUILayout.PrefixLabel (label,EditorStyles.layerMaskField);

			string text;
			if (value == 0) text = "Nothing";
			else if (value == ~0) text = "Everything";
			else text = System.Convert.ToString (value,2);

			string[] tagNames = AstarPath.FindTagNames ();

			if (GUILayout.Button (text,EditorStyles.layerMaskField,GUILayout.ExpandWidth (true))) {

				GenericMenu.MenuFunction2 wrappedCallback = obj => callback((int)obj);

				var menu = new GenericMenu ();

				menu.AddItem (new GUIContent ("Everything"), value == ~0, wrappedCallback, ~0);
				menu.AddItem (new GUIContent ("Nothing"), value == 0, wrappedCallback, 0);

				for (int i = 0; i < tagNames.Length; i++) {
					bool on = (value >> i & 1) != 0;
					int result = on ? value & ~(1 << i) : value | 1<<i;
					menu.AddItem (new GUIContent (tagNames[i]), on, wrappedCallback, result);
				}

				// Shortcut to open the tag editor
				menu.AddItem (new GUIContent ("Edit Tags..."),false,AstarPathEditor.EditTags);
				menu.ShowAsContext ();

				Event.current.Use ();
			}

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

		/** Displays a LayerMask field.
		 * \param label Label to display
		 * \param selected Current LayerMask
		 * \note Unity 3.5 and up will use the EditorGUILayout.MaskField instead of a custom written one.
		 */
		public static LayerMask LayerMaskField (string label, LayerMask selected) {
			if (layers == null || (System.DateTime.UtcNow.Ticks - lastUpdateTick > 10000000L && Event.current.type == EventType.Layout)) {
				lastUpdateTick = System.DateTime.UtcNow.Ticks;
				if (layers == null) {
					layers = new List<string>();
					layerNames = new string[4];
				} else {
					layers.Clear ();
				}

				int emptyLayers = 0;
				for (int i=0;i<32;i++) {
					string layerName = LayerMask.LayerToName (i);

					if (layerName != "") {

						for (;emptyLayers>0;emptyLayers--) layers.Add ("Layer "+(i-emptyLayers));
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
		}

		public static float Hermite(float start, float end, float value) {
			return Mathf.Lerp(start, end, value * value * (3.0f - 2.0f * value));
		}
	}
}
