using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using Pathfinding;

[CustomEditor (typeof(AstarPath))]
public class AstarPathEditor : Editor {

	/** List of all graph editors available (e.g GridGraphEditor) */
	static Dictionary<string,CustomGraphEditorAttribute> graphEditorTypes = new Dictionary<string,CustomGraphEditorAttribute> ();

	/**
	 * Holds node counts for each graph to avoid calculating it every frame.
	 * Only used for visualization purposes
	 */
	static Dictionary<NavGraph,KeyValuePair<float,KeyValuePair<int,int> > > graphNodeCounts;

	/** List of all graph editors for the graphs */
	GraphEditor[] graphEditors;

	System.Type[] graphTypes {
		get {
			return script.astarData.graphTypes;
		}
	}

	static int lastUndoGroup = -1000;

	/** Used to make sure correct behaviour when handling undos */
	static uint ignoredChecksum;

	/** Path to the editor assets folder for the A* Pathfinding Project. If this path turns out to be incorrect, the script will try to find the correct path
	  * \see LoadStyles */
	public static string editorAssets = "Assets/AstarPathfindingProject/Editor/EditorAssets";

	const string scriptsFolder = "Assets/AstarPathfindingProject";

	public static readonly string AstarProTooltip = "A* Pathfinding Project Pro only feature\nThe Pro version can be bought on the A* Pathfinding Project homepage,";
	public static readonly string AstarProButton  = "A* Pathfinding Project Pro only feature\nThe Pro version can be bought on the A* Pathfinding Project homepage, click here for info";

	/** Toggle to use a darker skin which matches the Unity Pro dark skin */
	static bool useDarkSkin;

	/** True if the user is forcing dark skin to be used */
	static bool hasForcedNoDarkSkin;

	/** Used to show notifications to the user the first time the system is used in a project */
	static bool firstRun = true;

#region SectionFlags

	/** Is the 'Add New Graph' menu open */
	bool showAddGraphMenu;

	static bool showSettings;
	static bool colorSettings;
	static bool editorSettings;
	static bool aboutArea;
	static bool optimizationSettings;
	static bool customAreaColorsOpen;
	static bool editTags;

	static bool showSerializationSettings;

#endregion

	static Pathfinding.Serialization.SerializeSettings serializationSettings = Pathfinding.Serialization.SerializeSettings.All;

	/** AstarPath instance that is being inspected */
	public AstarPath script {get; private set;}
	EditorGUILayoutx guiLayoutx;

#region Styles

	public static bool stylesLoaded {get; private set;}

	public static GUISkin astarSkin {get; private set;}

	static GUIStyle graphBoxStyle;
	static GUIStyle topBoxHeaderStyle;
	static GUIStyle graphDeleteButtonStyle;
	static GUIStyle graphInfoButtonStyle;
	static GUIStyle graphGizmoButtonStyle;

	public static GUIStyle helpBox  {get; private set;}
	public static GUIStyle thinHelpBox  {get; private set;}
	public static GUIStyle upArrow {get; private set;}
	public static GUIStyle downArrow {get; private set;}

#endregion

	//Misc


	//End Misc


	/** Enables editor stuff. Loads graphs, reads settings and sets everything up */
	public void OnEnable () {

		script = target as AstarPath;
		guiLayoutx = new EditorGUILayoutx ();
		EditorGUILayoutx.editor = this;

		//Enables the editor to get a callback on OnDrawGizmos to enable graph editors to draw gizmos
		script.OnDrawGizmosCallback = OnDrawGizmos;

		// Make sure all references are set up to avoid NullReferenceExceptions
		script.SetUpReferences ();

		Undo.undoRedoPerformed += OnUndoRedoPerformed;

		//Search the assembly for graph types and graph editors
		if ( graphEditorTypes == null || graphEditorTypes.Count == 0 )
			FindGraphTypes ();

		try {
			GetAstarEditorSettings ();
		} catch (System.Exception e) {
			Debug.LogException ( e );
		}

		LoadStyles ();

		//Load graphs only when not playing, or in extreme cases, when astarData.graphs is null
		if ((!Application.isPlaying && (script.astarData == null || script.astarData.graphs == null || script.astarData.graphs.Length == 0)) || script.astarData.graphs == null) {
			LoadGraphs ();
		}
	}

	/** Cleans up editor stuff */
	public void OnDisable () {

		Undo.undoRedoPerformed -= OnUndoRedoPerformed;

		if (target == null) {
			return;
		}

		SetAstarEditorSettings ();
		CheckGraphEditors ();

		for (int i=0;i<graphEditors.Length;i++) {
			if (graphEditors[i] != null) graphEditors[i].OnDisable ();
		}

		SaveGraphsAndUndo ();
	}

	public void OnDestroy () {
		if (graphEditors != null) {
			for (int i=0;i<graphEditors.Length;i++) {
				if (graphEditors[i] != null) graphEditors[i].OnDestroy ();
			}
		}
	}

	/** Reads settings frome EditorPrefs */
	void GetAstarEditorSettings () {
		EditorGUILayoutx.fancyEffects = EditorPrefs.GetBool ("EditorGUILayoutx.fancyEffects",true);

		useDarkSkin = EditorPrefs.GetBool ("AstarUseDarkSkin",false);
		hasForcedNoDarkSkin = EditorPrefs.GetBool ("AstarForcedNoDarkSkin",false);
		useDarkSkin = hasForcedNoDarkSkin ? useDarkSkin : EditorGUIUtility.isProSkin;

		editorAssets = EditorPrefs.GetString ("AstarEditorAssets",editorAssets);

		//Check if this is the first run of the A* Pathfinding Project in this project
		string runBeforeProjects = EditorPrefs.GetString ("AstarUsedProjects","");

		string projectName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath));
		firstRun = !runBeforeProjects.Contains (projectName);
	}

	void SetAstarEditorSettings () {
		EditorPrefs.SetBool ("EditorGUILayoutx.fancyEffects",EditorGUILayoutx.fancyEffects);
		EditorPrefs.SetBool ("AstarUseDarkSkin",useDarkSkin);
		EditorPrefs.SetBool ("AstarForcedNoDarkSkin",hasForcedNoDarkSkin);
		EditorPrefs.SetString ("AstarEditorAssets",editorAssets);
	}

	/** Checks if JS support is enabled. This is done by checking if the directory 'Assets/AstarPathfindingEditor/Editor' exists */
	static bool IsJsEnabled () {
		return System.IO.Directory.Exists (Application.dataPath+"/AstarPathfindingEditor/Editor");
	}

	/** Enables JS support. This is done by restructuring folders in the project */
	static void EnableJs () {

		//Path to the project folder (not with /Assets at the end)
		string projectPath = Application.dataPath;
		if (projectPath.EndsWith ("/Assets")) {
			projectPath = projectPath.Remove (projectPath.Length-("Assets".Length));
		}

		if (!System.IO.Directory.Exists (projectPath + scriptsFolder)) {
			string error = "Could not enable Js support. AstarPathfindingProject folder did not exist in the default location.\n" +
				"If you get this message and the AstarPathfindingProject is not at the root of your Assets folder (i.e at Assets/AstarPathfindingProject)" +
				" then you should move it to the root";

			Debug.LogError (error);
			EditorUtility.DisplayDialog ("Could not enable Js support",error,"ok");
			return;
		}

		if (!System.IO.Directory.Exists (Application.dataPath+"/AstarPathfindingEditor")) {
			System.IO.Directory.CreateDirectory (Application.dataPath+"/AstarPathfindingEditor");
			AssetDatabase.Refresh ();
		}
		if (!System.IO.Directory.Exists (Application.dataPath+"/Plugins")) {
			System.IO.Directory.CreateDirectory (Application.dataPath+"/Plugins");
			AssetDatabase.Refresh ();
		}


		AssetDatabase.MoveAsset (scriptsFolder + "/Editor","Assets/AstarPathfindingEditor/Editor");
		AssetDatabase.MoveAsset (scriptsFolder,"Assets/Plugins/AstarPathfindingProject");
		AssetDatabase.Refresh ();
	}

	/** Disables JS support if it was enabled. This is done by restructuring folders in the project */
	static void DisableJs () {

		if (System.IO.Directory.Exists (Application.dataPath+"/Plugins/AstarPathfindingProject")) {
			string error = AssetDatabase.MoveAsset ("Assets/Plugins/AstarPathfindingProject",scriptsFolder);
			if (error != "") {
				Debug.LogError ("Couldn't disable Js - "+error);
			} else {
				try {
					System.IO.Directory.Delete (Application.dataPath+"/Plugins");
				} catch (System.Exception) {}
			}
		} else {
			Debug.LogWarning ("Could not disable JS - Could not find directory '"+Application.dataPath+"/Plugins/AstarPathfindingProject'");
		}

		if (System.IO.Directory.Exists (Application.dataPath+"/AstarPathfindingEditor/Editor")) {
			string error = AssetDatabase.MoveAsset ("Assets/AstarPathfindingEditor/Editor",scriptsFolder + "/Editor");
			if (error != "") {
				Debug.LogError ("Couldn't disable Js - "+error);
			} else {
				try {
					System.IO.Directory.Delete (Application.dataPath+"/AstarPathfindingEditor");
				} catch (System.Exception) {}
			}

		} else {
			Debug.LogWarning ("Could not disable JS - Could not find directory '"+Application.dataPath+"/AstarPathfindingEditor/Editor'");
		}

		AssetDatabase.Refresh ();
	}

	/** Discards the first run window.
	 * It will not be shown for this project again */
	static void DiscardFirstRun () {
		string runBeforeProjects = EditorPrefs.GetString ("AstarUsedProjects","");

		string projectName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath));
		if (!runBeforeProjects.Contains (projectName)) {
			runBeforeProjects += "|"+projectName;
		}
		EditorPrefs.SetString ("AstarUsedProjects",runBeforeProjects);

		firstRun = false;
	}

	/** Repaints Scene View.
	 * \warning Uses Undocumented Unity Calls (should be safe for Unity 3.x though) */
	void RepaintSceneView () {
		if (!Application.isPlaying || EditorApplication.isPaused) SceneView.RepaintAll();
	}

	/** Tell Unity that we want to use the whole inspector width */
	public override bool UseDefaultMargins () {
		return false;
	}

	public override void OnInspectorGUI () {

		//Do some loading and checking
		if (!stylesLoaded) {
			if (!LoadStyles ()) {
				GUILayout.Label ("The GUISkin 'AstarEditorSkin.guiskin' in the folder "+editorAssets+"/ was not found or some custom styles in it does not exist.\n"+
					"This file is required for the A* Pathfinding Project editor.\n\n"+
					"If you are trying to add A* to a new project, please do not copy the files outside Unity, "+
					"export them as a UnityPackage and import them to this project or download the package from the Asset Store"+
					"or the 'scripts only' package from the A* Pathfinding Project website.\n\n\n"+
					"Skin loading is done in AstarPathEditor.cs --> LoadStyles function", "HelpBox");
				return;
			}
		}

		bool preChanged = GUI.changed;
		GUI.changed = false;

		EditorGUILayoutx.editor = this;
		guiLayoutx.ClearFadeAreaStack ();

		Undo.RecordObject (script, "A* inspector");

		CheckGraphEditors ();

		//End loading and checking

		EditorGUI.indentLevel = 1;

		// Apparently these can sometimes get eaten by unity components
		// so I catch them here for later use
		EventType storedEventType = Event.current.type;
		string storedEventCommand = Event.current.commandName;

		DrawMainArea ();

		GUILayout.Space (5);

		if (GUILayout.Button (new GUIContent ("Scan", "Recaculate all graphs. Shortcut cmd+alt+s ( ctrl+alt+s on windows )"))) {
			MenuScan ();
		}



		// Handle undo
		SaveGraphsAndUndo (storedEventType, storedEventCommand);

		GUI.changed = preChanged || GUI.changed;

		if (GUI.changed) {
			RepaintSceneView ();
			EditorUtility.SetDirty ( script );
		}
	}

	/** Locates the editor assets folder in case the user has moved it */
	static bool LocateEditorAssets () {
		string projectPath = Application.dataPath;
		if (projectPath.EndsWith ("/Assets")) {
			projectPath = projectPath.Remove (projectPath.Length-("Assets".Length));
		}

		if (!System.IO.File.Exists (projectPath + editorAssets + "/AstarEditorSkinLight.guiskin") && !System.IO.File.Exists (projectPath + editorAssets + "/AstarEditorSkin.guiskin")) {
			//Initiate search

			var sdir = new System.IO.DirectoryInfo (Application.dataPath);

			var dirQueue = new Queue<System.IO.DirectoryInfo>();
			dirQueue.Enqueue (sdir);

			bool found = false;
			while (dirQueue.Count > 0) {
				System.IO.DirectoryInfo dir = dirQueue.Dequeue ();
				if (System.IO.File.Exists (dir.FullName + "/AstarEditorSkinLight.guiskin") || System.IO.File.Exists (dir.FullName + "/AstarEditorSkin.guiskin")) {
					// Handle windows file paths
					string path = dir.FullName.Replace ('\\','/');
					found = true;
					// Remove data path from string to make it relative
					path = path.Replace (projectPath,"");

					if (path.StartsWith ("/")) {
						path = path.Remove (0,1);
					}

					editorAssets = path;
					break;
				}
				var dirs = dir.GetDirectories ();
				for (int i=0;i<dirs.Length;i++) {
					dirQueue.Enqueue (dirs[i]);
				}
			}

			if (!found) {
				Debug.LogWarning ("Could not locate editor assets folder. Make sure you have imported the package correctly.\nA* Pathfinding Project");
				return false;
			}
		}
		return true;
	}

	/** Loads GUISkin and sets up styles.
	 * \see #editorAssets
	 * \returns True if all styles were found, false if there was an error somewhere
	 */
	public static bool LoadStyles () {

		// Dummy styles in case the loading fails
		var inspectorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
		downArrow = upArrow = inspectorSkin.button;

		if (!LocateEditorAssets ()) {
			return false;
		}

		var skinPath = editorAssets + "/AstarEditorSkin" + (useDarkSkin ? "Dark" : "Light") + ".guiskin";
		astarSkin = AssetDatabase.LoadAssetAtPath (skinPath,typeof(GUISkin)) as GUISkin;

		if (astarSkin != null) {
			astarSkin.button = inspectorSkin.button;
		} else {
			Debug.LogWarning ("Could not load editor skin at '" + skinPath + "'");
			return false;
		}

		EditorGUILayoutx.defaultAreaStyle = astarSkin.FindStyle ("PixelBox");

		// If the first style is null, then the rest are likely corrupted as well
		// Probably due to the user not copying meta files
		if (EditorGUILayoutx.defaultAreaStyle == null) {
			return false;
		}

		EditorGUILayoutx.defaultLabelStyle = astarSkin.FindStyle ("BoxHeader");
		topBoxHeaderStyle = astarSkin.FindStyle ("TopBoxHeader");

		graphBoxStyle = astarSkin.FindStyle ("PixelBox3");
		graphDeleteButtonStyle = astarSkin.FindStyle ("PixelButton");
		graphInfoButtonStyle = astarSkin.FindStyle ("InfoButton");
		graphGizmoButtonStyle = astarSkin.FindStyle ("GizmoButton");

		upArrow = astarSkin.FindStyle ("UpArrow");
		downArrow = astarSkin.FindStyle ("DownArrow");

		helpBox = inspectorSkin.FindStyle ("HelpBox") ?? inspectorSkin.box;

		thinHelpBox = new GUIStyle (helpBox);
		thinHelpBox.contentOffset = new Vector2 (0,-2);
		thinHelpBox.stretchWidth = false;
		thinHelpBox.clipping = TextClipping.Overflow;
		thinHelpBox.overflow.top += 1;

		stylesLoaded = true;
		return true;
	}

	/** Draws the first run dialog.
	 * Asks if the user wants to enable JS support
	 */
	void DrawFirstRun () {
		if (!firstRun) {
			return;
		}

		if (IsJsEnabled ()) {
			DiscardFirstRun ();
			return;
		}

		guiLayoutx.BeginFadeArea (true,"Do you want to enable Javascript support?","enableJs");
		EditorGUILayout.HelpBox ("Folders can be restructured to enable pathfinding calls from Js\n" +
		                 "This setting can be edited later in Settings-->Editor", MessageType.Info);
		GUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Yes")) {
			EnableJs ();
		}
		if (GUILayout.Button ("No")) {
			DiscardFirstRun ();
		}
		GUILayout.EndHorizontal ();
		guiLayoutx.EndFadeArea ();
	}

	/** Draws the main area in the inspector */
	void DrawMainArea () {

		AstarProfiler.StartProfile ("Draw Graphs");

		DrawFirstRun ();

		//Show the graph inspectors
		CheckGraphEditors ();

		EditorGUILayoutx.FadeArea graphsFadeArea = guiLayoutx.BeginFadeArea (script.showGraphs,"Graphs", "showGraphInspectors", EditorGUILayoutx.defaultAreaStyle, topBoxHeaderStyle);
		script.showGraphs = graphsFadeArea.open;

		if ( graphsFadeArea.Show () ) {
			for (int i=0;i<script.graphs.Length;i++) {

				NavGraph graph = script.graphs[i];

				if (graph == null) continue;

				GraphEditor editor = graphEditors[i];

				if (editor == null) continue;

				if (DrawGraph (graph, editor)) {
					return;
				}
			}

			AstarProfiler.EndProfile ("Draw Graphs");

			AstarProfiler.StartProfile ("Draw Add New Graph");
			//Draw the Add Graph buttons
			showAddGraphMenu = guiLayoutx.BeginFadeArea (showAddGraphMenu || script.graphs.Length == 0, "Add New Graph","AddNewGraph",graphBoxStyle);
			if ( graphTypes == null ) script.astarData.FindGraphTypes ();
			for (int i=0;i<graphTypes.Length;i++) {
				if (graphEditorTypes.ContainsKey (graphTypes[i].Name)) {
					if (GUILayout.Button (graphEditorTypes[graphTypes[i].Name].displayName)) {
						showAddGraphMenu = false;
						AddGraph (graphTypes[i]);
						//OnSceneGUI ();
					}
				} else {
					bool preEnabled = GUI.enabled;
					GUI.enabled = false;
					GUILayout.Label (graphTypes[i].Name + " (no editor found)","Button");
					GUI.enabled = preEnabled;
				}
			}
			AstarProfiler.EndProfile ("Draw Add New Graph");
			guiLayoutx.EndFadeArea ();


			if (script.astarData.data_backup != null && script.astarData.data_backup.Length != 0) {
				guiLayoutx.BeginFadeArea (true, "Backup data detected","backupData",graphBoxStyle);
				EditorGUILayout.HelpBox ("Backup data was found, this can have been stored because there was an error during deserialization. Check the log.\n" +
				                 "If you load again and everything goes well, you can discard the backup data\n" +
				                 "When trying to load again, the deserializer will ignore version differences (for example 3.0 would try to load 3.0.1 files)\n" +
				                 "The backup data is stored in AstarData.data_backup", MessageType.Warning);
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Try loading data again")) {
					if (script.astarData.graphs == null || script.astarData.graphs.Length == 0
					    	|| EditorUtility.DisplayDialog ("Do you want to load from backup data?",
					                                           "Are you sure you want to load from backup data?\nThis will delete your current graphs.",
					                                           "Yes",
					                                           "Cancel")) {
						script.astarData.SetData(script.astarData.data_backup);
						LoadGraphs ();
					}
				}
				if (GUILayout.Button ("Discard backup data")) {
					script.astarData.data_backup = null;
				}
				GUILayout.EndHorizontal ();
				guiLayoutx.EndFadeArea ();
			}
		}

		guiLayoutx.EndFadeArea ();

		AstarProfiler.StartProfile ("DrawSettings");
		//Draw the settings area
		DrawSettings ();

		AstarProfiler.EndProfile ("DrawSettings");

		AstarProfiler.StartProfile ("DrawSerializationSettings");
		DrawSerializationSettings ();

		AstarProfiler.EndProfile ("DrawSerializationSettings");

		AstarProfiler.StartProfile ("DrawOptimizationSettings");
		DrawOptimizationSettings ();

		AstarProfiler.EndProfile ("DrawOptimizationSettings");

		AstarProfiler.StartProfile ("DrawAboutArea");
		DrawAboutArea ();

		AstarProfiler.EndProfile ("DrawAboutArea");

		AstarProfiler.StartProfile ("Show Graphs");
		bool showNavGraphs = EditorGUILayout.Toggle ("Show Graphs",script.showNavGraphs);
		if (script.showNavGraphs != showNavGraphs) {
			script.showNavGraphs = showNavGraphs;
			RepaintSceneView ();
		}
		AstarProfiler.EndProfile ("Show Graphs");
	}

	/** Draws optimizations settings.
	 * \astarpro */
	public void DrawOptimizationSettings () {
		EditorGUILayoutx.FadeArea fadeArea = guiLayoutx.BeginFadeArea (optimizationSettings,"Optimization","optimization", EditorGUILayoutx.defaultAreaStyle, topBoxHeaderStyle);
		optimizationSettings = fadeArea.open;

		if ( fadeArea.Show () ) {


			GUIUtilityx.SetColor (Color.Lerp (Color.yellow,Color.white,0.5F));
			if (GUILayout.Button ("Optimizations is an "+AstarProButton,helpBox)) {
				Application.OpenURL (AstarUpdateChecker.GetURL ("astarpro"));
			}
			GUIUtilityx.ResetColor ();
		}

		guiLayoutx.EndFadeArea ();

	}

	/** Returns a version with all fields fully defined.
	 * This is used because by default new Version(3,0,0) > new Version(3,0).
	 * This is not the desired behaviour so we make sure that all fields are defined here
	 */
	public static System.Version FullyDefinedVersion (System.Version v) {
		return new System.Version(Mathf.Max(v.Major, 0), Mathf.Max(v.Minor, 0), Mathf.Max(v.Build, 0), Mathf.Max(v.Revision, 0));
	}

	void DrawAboutArea () {

		Color guiColorOrig = GUI.color;
		EditorGUILayoutx.FadeArea fadeArea = guiLayoutx.BeginFadeArea (aboutArea,"aboutArea", 20,EditorGUILayoutx.defaultAreaStyle);
		Color tmpColor1 = GUI.color;
		GUI.color = guiColorOrig;

		GUILayout.BeginHorizontal ();

		if (GUILayout.Button ("About", topBoxHeaderStyle )) {
			aboutArea = !aboutArea;
			GUI.changed = true;
		}

		System.Version newVersion = AstarUpdateChecker.latestVersion;
		bool beta = false;

		// Check if either the latest release version or the latest beta version is newer than this version
		if (FullyDefinedVersion(AstarUpdateChecker.latestVersion) > FullyDefinedVersion(AstarPath.Version) || FullyDefinedVersion(AstarUpdateChecker.latestBetaVersion) > FullyDefinedVersion(AstarPath.Version)) {
			if (FullyDefinedVersion(AstarUpdateChecker.latestVersion) <= FullyDefinedVersion(AstarPath.Version)) {
				newVersion = AstarUpdateChecker.latestBetaVersion;
				beta = true;
			}
		}

		// Check if the latest version is newer than this version
		if (FullyDefinedVersion(newVersion) > FullyDefinedVersion(AstarPath.Version)) {
			GUI.color = guiColorOrig * Color.green;
			if (GUILayout.Button ((beta ? "Beta" : "New") + " Version Available! "+newVersion, thinHelpBox, GUILayout.Height (15))) {
				Application.OpenURL (AstarUpdateChecker.GetURL ("download"));
			}
			GUILayout.Space (20);
		}

		GUILayout.EndHorizontal ();

		GUI.color = tmpColor1;

		if (fadeArea.Show ()) {
			GUILayout.Label ("The A* Pathfinding Project was made by Aron Granberg\nYour current version is "+AstarPath.Version);

			if (newVersion > AstarPath.Version) {
				EditorGUILayout.HelpBox ("A new "+(beta? "beta " : "")+"version of the A* Pathfinding Project is available, the new version is "+
					newVersion, MessageType.Info);

				if (GUILayout.Button ("What's new?")) {
					Application.OpenURL (AstarUpdateChecker.GetURL (beta ? "beta_changelog" : "changelog"));
				}

				if (GUILayout.Button ("Click here to find out more")) {
					Application.OpenURL (AstarUpdateChecker.GetURL ("findoutmore"));
				}

				Color tmpColor2 = GUI.color;
				tmpColor1 *= new Color (0.3F,0.9F,0.3F);
				GUI.color = tmpColor1;

				if (GUILayout.Button ("Download new version")) {
					Application.OpenURL (AstarUpdateChecker.GetURL ("download"));
				}

				GUI.color = tmpColor2;
			}

			if (GUILayout.Button (new GUIContent ("Documentation","Open the documentation for the A* Pathfinding Project"))) {
				Application.OpenURL (AstarUpdateChecker.GetURL ("documentation"));
			}

			if (GUILayout.Button (new GUIContent ("Project Homepage","Open the homepage for the A* Pathfinding Project"))) {
				Application.OpenURL (AstarUpdateChecker.GetURL ("homepage"));
			}
		}

		guiLayoutx.EndFadeArea ();
	}

	/** Draws the inspector for the given graph with the given graph editor */
	bool DrawGraph (NavGraph graph, GraphEditor graphEditor) {

		// Graph guid, just used to get a unique value
		string graphGUIDString = graph.guid.ToString();

		Color tmp1 = GUI.color;
		EditorGUILayoutx.FadeArea topFadeArea = guiLayoutx.BeginFadeArea (graph.open, "", graphGUIDString, graphBoxStyle);

		Color tmp2 = GUI.color;
		GUI.color = tmp1;

		GUILayout.BeginHorizontal ();

		// Make sure that the graph name is not null
		graph.name = graph.name ?? graphEditorTypes[graph.GetType ().Name].displayName;

		GUI.SetNextControlName (graphGUIDString);
		graph.name = GUILayout.TextField (graph.name, EditorGUILayoutx.defaultLabelStyle, GUILayout.ExpandWidth(false),GUILayout.ExpandHeight(false));

		// If the graph name text field is not focused and the graph name is empty, then fill it in
		if (graph.name == "" && Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != graphGUIDString) {
			graph.name = graphEditorTypes[graph.GetType ().Name].displayName;
		}

		if (GUILayout.Button ("", EditorGUILayoutx.defaultLabelStyle)) {
			graph.open = !graph.open;
			if (!graph.open) {
				graph.infoScreenOpen = false;
			}
			RepaintSceneView ();
			return true;
		}

		if (script.prioritizeGraphs) {
			if (GUILayout.Button (new GUIContent ("Up","Increase the graph priority"),GUILayout.Width (40))) {
				int index = script.astarData.GetGraphIndex (graph);

				// Find the previous non null graph
				int next = index-1;
				for (;next >= 0;next--) if (script.graphs[next] != null) break;

				if (next >= 0) {
					NavGraph tmp = script.graphs[next];
					script.graphs[next] = graph;
					script.graphs[index] = tmp;

					GraphEditor tmpEditor = graphEditors[next];
					graphEditors[next] = graphEditors[index];
					graphEditors[index] = tmpEditor;
				}
				CheckGraphEditors ();
				Repaint ();
			}
			if (GUILayout.Button (new GUIContent ("Down","Decrease the graph priority"),GUILayout.Width (40))) {
				int index = script.astarData.GetGraphIndex (graph);

				// Find the next non null graph
				int next = index+1;
				for (;next<script.graphs.Length;next++) if (script.graphs[next] != null) break;

				if (next < script.graphs.Length) {
					NavGraph tmp = script.graphs[next];
					script.graphs[next] = graph;
					script.graphs[index] = tmp;

					GraphEditor tmpEditor = graphEditors[next];
					graphEditors[next] = graphEditors[index];
					graphEditors[index] = tmpEditor;
				}
				CheckGraphEditors ();
				Repaint ();
			}
		}

		bool drawGizmos = GUILayout.Toggle (graph.drawGizmos, "Draw Gizmos", graphGizmoButtonStyle);
		if (drawGizmos != graph.drawGizmos) {
			graph.drawGizmos = drawGizmos;

			// Make sure that the scene view is repainted when gizmos are toggled on or off
			RepaintSceneView ();
		}

		if (GUILayout.Toggle (graph.infoScreenOpen,"Info",graphInfoButtonStyle)) {
			if (!graph.infoScreenOpen) {
				graph.infoScreenOpen = true;
				graph.open = true;
			}
		} else {
			graph.infoScreenOpen = false;
		}

		if (GUILayout.Button ("Delete",graphDeleteButtonStyle)) {
			RemoveGraph (graph);
			return true;
		}
		GUILayout.EndHorizontal ();

		if (topFadeArea.Show () ) {
			EditorGUILayoutx.FadeArea fadeArea = guiLayoutx.BeginFadeArea (graph.infoScreenOpen,"graph_info_"+graphGUIDString,0);
			if (fadeArea.Show ()) {

				bool nodenull = false;
				int total = 0;
				int numWalkable = 0;

				KeyValuePair<float,KeyValuePair<int,int>> pair;
				graphNodeCounts = graphNodeCounts ?? new Dictionary<NavGraph, KeyValuePair<float, KeyValuePair<int, int>>>();

				if ( !graphNodeCounts.TryGetValue ( graph, out pair ) || (Time.realtimeSinceStartup-pair.Key) > 2 ) {
					GraphNodeDelegateCancelable counter = node => {
						if (node == null) {
							nodenull = true;
						} else {
							total++;
							if (node.Walkable) numWalkable++;
						}
						return true;
					};
					graph.GetNodes (counter);
					pair = new KeyValuePair<float, KeyValuePair<int, int>> (Time.realtimeSinceStartup, new KeyValuePair<int,int>( total, numWalkable ) );
					graphNodeCounts[graph] = pair;
				}

				total = pair.Value.Key;
				numWalkable = pair.Value.Value;


				EditorGUI.indentLevel++;

				if (nodenull) {
					//EditorGUILayout.HelpBox ("Some nodes in the graph are null. Please report this error.", MessageType.Info);
					Debug.LogError ("Some nodes in the graph are null. Please report this error.");
				}

				EditorGUILayout.LabelField ("Nodes",total.ToString());
				EditorGUILayout.LabelField ("Walkable",numWalkable.ToString ());
				EditorGUILayout.LabelField ("Unwalkable",(total-numWalkable).ToString ());
				if (total == 0) EditorGUILayout.HelpBox ("The number of nodes in the graph is zero. The graph might not be scanned",MessageType.Info);

				EditorGUI.indentLevel--;
			}
			guiLayoutx.EndFadeArea ();

			GUI.color = tmp2;

			graphEditor.OnInspectorGUI (graph);
			graphEditor.OnBaseInspectorGUI (graph);
		}

		guiLayoutx.EndFadeArea ();

		return false;
	}

	public void OnSceneGUI () {

		bool preChanged = GUI.changed;
		GUI.changed = false;

		script = target as AstarPath;

		AstarPath.active = script;

		if (!stylesLoaded) {
			LoadStyles ();
			return;
		}

		//Some GUI controls might change this to Used, so we need to grab it here
		EventType et = Event.current.type;

		CheckGraphEditors ();
		for (int i=0;i<script.graphs.Length;i++) {

			NavGraph graph = script.graphs[i];

			if (graph == null || graphEditors.Length <= i) {
				continue;
			}

			graphEditors[i].OnSceneGUI (graph);
		}

		SaveGraphsAndUndo (et);

		if (GUI.changed) {
			EditorUtility.SetDirty (target);
		} else {
			GUI.changed = preChanged;
		}
	}

	TextAsset SaveGraphData ( byte[] bytes, TextAsset target = null ) {
		string projectPath = System.IO.Path.GetDirectoryName (Application.dataPath) + "/";

		string path;
		if ( target != null ) {
			path = AssetDatabase.GetAssetPath (target);
		} else {
			int i = 0;
			do {
				path = "Assets/GraphCaches/GraphCache" + (i == 0 ? "" : i.ToString()) + ".bytes";
				i++;
			} while (System.IO.File.Exists (projectPath+path));
		}

		System.IO.Directory.CreateDirectory (System.IO.Path.GetDirectoryName (projectPath + path));
		System.IO.File.WriteAllBytes (projectPath + path, bytes);
		AssetDatabase.Refresh ();
		return AssetDatabase.LoadAssetAtPath (path, typeof(TextAsset)) as TextAsset;
	}

	void DrawSerializationSettings () {

		AstarProfiler.StartProfile ("Serialization step 1");

		Color tmp1 = GUI.color;
		EditorGUILayoutx.FadeArea fadeArea = guiLayoutx.BeginFadeArea (showSerializationSettings, "serializationSettings", 20, EditorGUILayoutx.defaultAreaStyle);
		showSerializationSettings = fadeArea.open;

		Color tmp2 = GUI.color;
		GUI.color = tmp1;

		GUILayout.BeginHorizontal ();

		if (GUILayout.Button ("Save & Load", topBoxHeaderStyle )) {
			showSerializationSettings = !showSerializationSettings;
			GUI.changed = true;
		}

		if (script.astarData.cacheStartup && script.astarData.file_cachedStartup != null) {
			tmp1 *= Color.yellow;
			GUI.color = tmp1;

			GUILayout.Label ("Startup cached",thinHelpBox,GUILayout.Height (15));

			GUILayout.Space (20);

		}

		GUI.color = tmp2;

		GUILayout.EndHorizontal ();

		if ( fadeArea.Show () ) {

			AstarProfiler.EndProfile ("Serialization step 1");

			AstarProfiler.StartProfile ("SerializationSettings.OnGUI");
			/* This displays the values of the serialization settings */
			serializationSettings.nodes =  EditorGUILayout.Toggle ("Save Node Data", serializationSettings.nodes);
			serializationSettings.prettyPrint = EditorGUILayout.Toggle (new GUIContent ("Pretty Print","Format Json data for readability. Yields slightly smaller files when turned off"),serializationSettings.prettyPrint);

			AstarProfiler.EndProfile ("SerializationSettings.OnGUI");

			AstarProfiler.StartProfile ("Cache Startup");
			GUILayout.Space (5);

			bool preEnabled = GUI.enabled;

			script.astarData.cacheStartup = EditorGUILayout.Toggle (new GUIContent ("Cache startup","If enabled, will cache the graphs so they don't have to be scanned at startup"),script.astarData.cacheStartup);

			script.astarData.file_cachedStartup = EditorGUILayout.ObjectField (script.astarData.file_cachedStartup, typeof(TextAsset), false) as TextAsset;

			GUILayout.BeginHorizontal ();

			if (GUILayout.Button ("Generate cache")) {
				if (EditorUtility.DisplayDialog ("Scan before generating cache?","Do you want to scan the graphs before saving the cache","Scan","Don't scan")) {
					MenuScan ();
				}

				// Save graphs
				var bytes = script.astarData.SerializeGraphs (serializationSettings);

				// Store it in a file
				script.astarData.file_cachedStartup = SaveGraphData ( bytes, script.astarData.file_cachedStartup );
				script.astarData.cacheStartup = true;
			}

			if (GUILayout.Button ("Load from cache")) {
				if (EditorUtility.DisplayDialog ("Are you sure you want to load from cache?","Are you sure you want to load graphs from the cache, this will replace your current graphs?","Yes","Cancel")) {
					script.astarData.LoadFromCache ();
				}
			}

			AstarProfiler.EndProfile ("Cache Startup");

			AstarProfiler.StartProfile ("Clear Cache");
			if (GUILayout.Button ("Clear cache", GUILayout.MaxWidth (120))) {
				script.astarData.data_cachedStartup = null;
				script.astarData.file_cachedStartup = null;
				script.astarData.cacheStartup = false;
			}

			GUILayout.EndHorizontal ();

			GUI.enabled = preEnabled;

			if ( script.astarData.data_cachedStartup != null && script.astarData.data_cachedStartup.Length > 0 ) {
				EditorGUILayout.HelpBox ("Storing the cached starup data on the AstarPath object has been deprecated. It is now stored " +
					"in a separate file.", MessageType.Error );

				if (GUILayout.Button ("Transfer cache data to separate file")) {
					script.astarData.file_cachedStartup = SaveGraphData ( script.astarData.data_cachedStartup );
					script.astarData.data_cachedStartup = null;
				}
			}

			EditorGUILayout.HelpBox ("When using 'cache startup', the 'Nodes' toggle should always be enabled otherwise the graphs' nodes won't be saved and the caching is quite useless", MessageType.Info);

			GUILayout.Space (5);

			AstarProfiler.EndProfile ("Clear Cache");

			AstarProfiler.StartProfile ("SaveToFile");

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Save to file")) {
				string path = EditorUtility.SaveFilePanel ("Save Graphs","","graph.bytes","bytes");

				if (path != "") {
					if (EditorUtility.DisplayDialog ("Scan before saving?","Do you want to scan the graphs before saving" +
						"\nNot scanning can cause node data to be omitted from the file if Save Node Data is enabled","Scan","Don't scan")) {
						MenuScan ();
					}

					uint checksum;
					byte[] bytes = SerializeGraphs (serializationSettings, out checksum);
					Pathfinding.Serialization.AstarSerializer.SaveToFile (path,bytes);

					EditorUtility.DisplayDialog ("Done Saving","Done saving graph data.","Ok");
				}
			}

			AstarProfiler.EndProfile ("SaveToFile");
			AstarProfiler.StartProfile ("LoadFromFile");
			if (GUILayout.Button ("Load from file")) {
				string path = EditorUtility.OpenFilePanel ("Load Graphs","","");

				if (path != "") {
					byte[] bytes;
					try {
						bytes = Pathfinding.Serialization.AstarSerializer.LoadFromFile (path);
					} catch (System.Exception e) {
						Debug.LogError ("Could not load from file at '"+path+"'\n"+e);
						bytes = null;
					}

					if (bytes != null) DeserializeGraphs (bytes);
				}

			}

			GUILayout.EndHorizontal ();

			AstarProfiler.EndProfile ("LoadFromFile");


		}

		AstarProfiler.StartProfile ("SerializationEndFadeArea");
		guiLayoutx.EndFadeArea ();
		AstarProfiler.EndProfile ("SerializationEndFadeArea");
	}

	void DrawSettings () {
		EditorGUILayoutx.FadeArea fadeArea = guiLayoutx.BeginFadeArea (showSettings,"Settings","settings", EditorGUILayoutx.defaultAreaStyle, topBoxHeaderStyle);
		showSettings = fadeArea.open;

		if ( fadeArea.Show () ) {
			guiLayoutx.BeginFadeArea (true,"Pathfinding","alwaysShow",graphBoxStyle);

			EditorGUI.BeginDisabledGroup(Application.isPlaying);

			script.threadCount = (ThreadCount)EditorGUILayout.EnumPopup (new GUIContent ("Thread Count","Number of threads to run the pathfinding in (if any). More threads " +
				"can boost performance on multi core systems. \n" +
				"Use None for debugging or if you dont use pathfinding that much.\n " +
	                                                                "See docs for more info"),script.threadCount);

			EditorGUI.EndDisabledGroup();

			int threads = AstarPath.CalculateThreadCount(script.threadCount);
			if (threads > 0) EditorGUILayout.HelpBox ("Using " + threads +" thread(s)" + (script.threadCount < 0 ? " on your machine" : "") + ".\n" +
				"The free version of the A* Pathfinding Project is limited to at most one thread.", MessageType.None);
			else EditorGUILayout.HelpBox ("Using a single coroutine (no threads)" + (script.threadCount < 0 ? " on your machine" : ""), MessageType.None);

			if (script.threadCount == ThreadCount.None) {
				script.maxFrameTime = EditorGUILayout.FloatField (new GUIContent ("Max Frame Time", "Max number of milliseconds to use for path calculation per frame"),script.maxFrameTime);
			} else {
				script.maxFrameTime = 10;
			}

			script.heuristic = (Heuristic)EditorGUILayout.EnumPopup ("Heuristic",script.heuristic);

			guiLayoutx.BeginFadeArea (script.heuristic == Heuristic.Manhattan || script.heuristic == Heuristic.Euclidean || script.heuristic == Heuristic.DiagonalManhattan,"hScale");
			if (guiLayoutx.DrawID ("hScale")) {
				EditorGUI.indentLevel++;
				script.heuristicScale = EditorGUILayout.FloatField ("Heuristic Scale",script.heuristicScale);
				EditorGUI.indentLevel--;
			}
			guiLayoutx.EndFadeArea ();

			script.maxNearestNodeDistance = EditorGUILayout.FloatField (new GUIContent ("Max Nearest Node Distance",
			                                                                            "Normally, if the nearest node to e.g the start point of a path was not walkable" +
			                                                                            " a search will be done for the nearest node which is walkble. This is the maximum distance (world units) which it will serarch"),
			                                                            script.maxNearestNodeDistance);

			GUILayout.Label (new GUIContent ("Advanced"), EditorStyles.boldLabel);

			script.minAreaSize = EditorGUILayout.IntField (new GUIContent ("Min Area Size",
				"The minimum number of nodes an area must have to be granted an unique area id. 2^17 area ids are available (131072)."+
				"This merges small areas to use the same area id and helps keeping the area count below 131072. Usually this is not required. [default = 0]"),script.minAreaSize);

			DrawHeuristicOptimizationSettings();

			script.limitGraphUpdates = EditorGUILayout.Toggle (new GUIContent ("Limit Graph Updates","Limit graph updates to only run every x seconds. Can have positive impact on performance if many graph updates are done"),script.limitGraphUpdates);

			guiLayoutx.BeginFadeArea (script.limitGraphUpdates,"graphUpdateFreq");
			if (guiLayoutx.DrawID ("graphUpdateFreq")) {
				EditorGUI.indentLevel++;
				script.maxGraphUpdateFreq = EditorGUILayout.FloatField ("Max Update Frequency (s)",script.maxGraphUpdateFreq);
				EditorGUI.indentLevel--;
			}
			guiLayoutx.EndFadeArea ();

			script.prioritizeGraphs = EditorGUILayout.Toggle (new GUIContent ("Prioritize Graphs","Normally, the system will search for the closest node in all graphs and choose the closest one" +
				"but if Prioritize Graphs is enabled, the first graph which has a node closer than Priority Limit will be chosen and additional search (e.g for the closest WALKABLE node) will be carried out on that graph only"),
				                                                       script.prioritizeGraphs);
			guiLayoutx.BeginFadeArea (script.prioritizeGraphs,"prioritizeGraphs");
			if (guiLayoutx.DrawID ("prioritizeGraphs")) {
				EditorGUI.indentLevel++;
				script.prioritizeGraphsLimit = EditorGUILayout.FloatField ("Priority Limit",script.prioritizeGraphsLimit);
				EditorGUI.indentLevel--;
			}
			guiLayoutx.EndFadeArea ();

			script.fullGetNearestSearch = EditorGUILayout.Toggle (new GUIContent ("Full Get Nearest Node Search","Forces more accurate searches on all graphs. " +
				"Normally only the closest graph in the initial fast check will perform additional searches, " +
				"if this is toggled, all graphs will do additional searches. Slower, but more accurate"),script.fullGetNearestSearch);
			script.scanOnStartup = EditorGUILayout.Toggle (new GUIContent ("Scan on Awake","Scan all graphs on Awake. If this is false, you must call AstarPath.active.Scan () yourself. Useful if you want to make changes to the graphs with code."),script.scanOnStartup);

			guiLayoutx.EndFadeArea ();

			DrawDebugSettings ();
			DrawColorSettings ();
			DrawTagSettings ();
			DrawEditorSettings ();
		}


		guiLayoutx.EndFadeArea ();
	}

	void DrawHeuristicOptimizationSettings () {
		// Pro only feature
	}

	/** Opens the A* Inspector and shows the section for editing tags */
	public static void EditTags () {
		AstarPath astar = AstarPath.active ?? GameObject.FindObjectOfType (typeof(AstarPath)) as AstarPath;
		if (astar != null) {
			editTags = true;
			showSettings = true;
			Selection.activeGameObject = astar.gameObject;
		} else {
			Debug.LogWarning ("No AstarPath component in the scene");
		}
	}

	void DrawTagSettings () {
		editTags = guiLayoutx.BeginFadeArea (editTags,"Tag Names","tags",graphBoxStyle);

		if (guiLayoutx.DrawID ("tags")) {

			string[] tagNames = script.GetTagNames ();

			for (int i=0;i<tagNames.Length;i++) {
				tagNames[i] = EditorGUILayout.TextField (new GUIContent ("Tag "+i,"Name for tag "+i),tagNames[i]);
				if (tagNames[i] == "") tagNames[i] = ""+i;
			}
		}

		guiLayoutx.EndFadeArea ();
	}

	void DrawEditorSettings () {

		editorSettings = guiLayoutx.BeginFadeArea (editorSettings,"Editor","editorSettings",graphBoxStyle);

		if (guiLayoutx.DrawID ("editorSettings")) {
			EditorGUILayoutx.fancyEffects = EditorGUILayout.Toggle ("Fancy fading effects",EditorGUILayoutx.fancyEffects);

			bool preVal = useDarkSkin;
			int val = useDarkSkin ? 2 : 1;
			if (!hasForcedNoDarkSkin) val = 0;

			val = EditorGUILayout.Popup ("Use Dark Skin",val,new [] {"Auto","Force Light","Force Dark"});

			if (val == 0) {
				useDarkSkin = EditorGUIUtility.isProSkin;
				hasForcedNoDarkSkin = false;
			} else {
				hasForcedNoDarkSkin = true;
				useDarkSkin = val == 2;
			}

			if (useDarkSkin != preVal) {
				LoadStyles ();
			}

			if (IsJsEnabled ()) {
				if (GUILayout.Button (new GUIContent ("Disable Js Support","Revert to only enable pathfinding calls from C#"))) {
					DisableJs ();
				}
			} else {
				if (GUILayout.Button (new GUIContent ("Enable Js Support","Folders can be restructured to enable pathfinding calls from Js instead of just from C#"))) {
					EnableJs ();
				}
			}
		}

		guiLayoutx.EndFadeArea ();
	}

	static void DrawColorSlider ( ref float left, ref float right, bool editable ) {
		GUILayout.BeginHorizontal ();

		GUILayout.Space (20);

		GUILayout.BeginVertical ();

		GUILayout.Box ("", astarSkin.GetStyle("ColorInterpolationBox"));
		GUILayout.BeginHorizontal ();
		if (editable) {
			left = EditorGUILayout.IntField ((int)left);
		} else {
			GUILayout.Label (left.ToString ("0"));
		}
		GUILayout.FlexibleSpace ();
		if (editable) {
			right = EditorGUILayout.IntField ((int)right);
		} else {
			GUILayout.Label (right.ToString ("0"));
		}
		GUILayout.EndHorizontal ();

		GUILayout.EndVertical ();

		GUILayout.Space (4);

		GUILayout.EndHorizontal ();
	}

	void DrawDebugSettings () {
		guiLayoutx.BeginFadeArea (true,"Debug","debugSettings",graphBoxStyle);

		if (guiLayoutx.DrawID ("debugSettings")) {

			script.logPathResults = (PathLog)EditorGUILayout.EnumPopup ("Path Log Mode",script.logPathResults);
			script.debugMode = (GraphDebugMode)EditorGUILayout.EnumPopup ("Path Debug Mode",script.debugMode);

			bool show = script.debugMode == GraphDebugMode.G || script.debugMode == GraphDebugMode.H || script.debugMode == GraphDebugMode.F || script.debugMode == GraphDebugMode.Penalty;
			guiLayoutx.BeginFadeArea (show,"debugRoof");

			if (guiLayoutx.DrawID ("debugRoof")) {
				script.manualDebugFloorRoof = !EditorGUILayout.Toggle ("Automatic Limits", !script.manualDebugFloorRoof);

				DrawColorSlider (ref script.debugFloor, ref script.debugRoof, script.manualDebugFloorRoof);
			}

			guiLayoutx.EndFadeArea ();

			script.showSearchTree = EditorGUILayout.Toggle ("Show Search Tree",script.showSearchTree);

			script.showUnwalkableNodes = EditorGUILayout.Toggle ("Show Unwalkable Nodes", script.showUnwalkableNodes);

			if (script.showUnwalkableNodes) {
				EditorGUI.indentLevel++;
				script.unwalkableNodeDebugSize = EditorGUILayout.FloatField ("Size", script.unwalkableNodeDebugSize);
				EditorGUI.indentLevel--;
			}

		}

		guiLayoutx.EndFadeArea ();
	}

	void DrawColorSettings () {

		colorSettings = guiLayoutx.BeginFadeArea (colorSettings,"Colors","colorSettings",graphBoxStyle);

		if (guiLayoutx.DrawID ("colorSettings")) {
			if (script.colorSettings == null) {
				script.colorSettings = new AstarColor ();
			}

			AstarColor colors = script.colorSettings;

			colors._NodeConnection = EditorGUILayout.ColorField ("Node Connection", colors._NodeConnection);
			colors._UnwalkableNode = EditorGUILayout.ColorField ("Unwalkable Node", colors._UnwalkableNode);
			colors._BoundsHandles = EditorGUILayout.ColorField ("Bounds Handles", colors._BoundsHandles);

			colors._ConnectionLowLerp = EditorGUILayout.ColorField ("Connection Gradient (low)", colors._ConnectionLowLerp);
			colors._ConnectionHighLerp = EditorGUILayout.ColorField ("Connection Gradient (high)", colors._ConnectionHighLerp);

			colors._MeshEdgeColor = EditorGUILayout.ColorField ("Mesh Edge", colors._MeshEdgeColor);
			colors._MeshColor = EditorGUILayout.ColorField ("Mesh Color", colors._MeshColor);

			if (colors._AreaColors == null) {
				colors._AreaColors = new Color[0];
			}

			//Custom Area Colors
			customAreaColorsOpen = EditorGUILayout.Foldout (customAreaColorsOpen,"Custom Area Colors");
			if (customAreaColorsOpen) {
				EditorGUI.indentLevel+=2;

				for (int i=0;i<colors._AreaColors.Length;i++) {
					GUILayout.BeginHorizontal ();
					colors._AreaColors[i] = EditorGUILayout.ColorField ("Area "+i+(i == 0 ? " (not used usually)":""),colors._AreaColors[i]);
					if (GUILayout.Button (new GUIContent ("","Reset to the default color"),astarSkin.FindStyle ("SmallReset"),GUILayout.Width (20))) {
						colors._AreaColors[i] = AstarMath.IntToColor (i,1F);
					}
					GUILayout.EndHorizontal ();
				}

				GUILayout.BeginHorizontal ();

				if (colors._AreaColors.Length > 255) {
					GUI.enabled = false;
				}

				if (GUILayout.Button ("Add New")) {
					var newcols = new Color[colors._AreaColors.Length+1];
					for (int i=0;i<colors._AreaColors.Length;i++) {
						newcols[i] = colors._AreaColors[i];
					}
					newcols[newcols.Length-1] = AstarMath.IntToColor (newcols.Length-1,1F);
					colors._AreaColors = newcols;
				}

				EditorGUI.BeginDisabledGroup(colors._AreaColors.Length == 0);

				if (GUILayout.Button ("Remove last") && colors._AreaColors.Length > 0) {
					var newcols = new Color[colors._AreaColors.Length-1];
					for (int i=0;i<colors._AreaColors.Length-1;i++) {
						newcols[i] = colors._AreaColors[i];
					}
					colors._AreaColors = newcols;
				}

				EditorGUI.EndDisabledGroup();
				GUILayout.EndHorizontal ();

				EditorGUI.indentLevel-=2;
			}

			if (GUI.changed) {
				colors.OnEnable ();
			}
		}

		guiLayoutx.EndFadeArea ();
	}

	/** Make sure every graph has a graph editor */
	void CheckGraphEditors (bool forceRebuild = false) {
		if (forceRebuild || graphEditors == null || script.graphs == null || script.graphs.Length != graphEditors.Length) {

			if (script.graphs == null) {
				script.graphs = new NavGraph[0];
			}

			if (graphEditors != null) {
				for (int i=0;i<graphEditors.Length;i++) {
					if (graphEditors[i] != null) {
						//graphEditors[i].OnDisableUndo ();
						graphEditors[i].OnDisable ();
						graphEditors[i].OnDestroy ();
					}
				}
			}

			graphEditors = new GraphEditor[script.graphs.Length];

			for (int i=0;i< script.graphs.Length;i++) {

				NavGraph graph = script.graphs[i];

				if (graph == null) continue;

				if (graph.guid == new Pathfinding.Util.Guid ()) {
					Debug.LogWarning ("Zeroed guid detected, creating new randomized guid");
					graph.guid = Pathfinding.Util.Guid.NewGuid();
				}

				GraphEditor graphEditor = CreateGraphEditor (graph.GetType ().Name);
				graphEditor.target = graph;
				graphEditor.OnEnable ();
				graphEditors[i] = graphEditor;


			}

		} else {
			for (int i=0;i< script.graphs.Length;i++) {

				if (script.graphs[i] == null) continue;

				if (graphEditors[i] == null || graphEditorTypes[script.graphs[i].GetType ().Name].editorType != graphEditors[i].GetType ()) {
					CheckGraphEditors (true);
					return;
				}

				if (script.graphs[i].guid == new Pathfinding.Util.Guid ()) {
					Debug.LogWarning ("Zeroed guid detected, creating new randomized guid");
					script.graphs[i].guid = Pathfinding.Util.Guid.NewGuid();
				}

				graphEditors[i].target = script.graphs[i];
			}
		}
	}

	void RemoveGraph (NavGraph graph) {
		guiLayoutx.RemoveID (graph.guid.ToString());
		script.astarData.RemoveGraph (graph);
		CheckGraphEditors ();
		GUI.changed = true;
		Repaint ();
	}

	void AddGraph (System.Type type) {
		script.astarData.AddGraph (type);
		CheckGraphEditors ();

		GUI.changed = true;
	}

	/** Creates a GraphEditor for a graph */
	GraphEditor CreateGraphEditor (string graphType) {

		if (graphEditorTypes.ContainsKey (graphType)) {
			var ge = System.Activator.CreateInstance (graphEditorTypes[graphType].editorType) as GraphEditor;
			ge.editor = this;
			return ge;
		}
		Debug.LogError ("Couldn't find an editor for the graph type '"+graphType+"' There are "+graphEditorTypes.Count+" available graph editors");

		var def = new GraphEditor ();
		def.editor = this;
		return def;
	}

	/** Draw Editor Gizmos in graphs. This is called using a delegate OnDrawGizmosCallback in the AstarPath script.*/
	void OnDrawGizmos () {

		AstarProfiler.StartProfile ("OnDrawGizmosEditor");

		CheckGraphEditors ();

		for (int i=0;i<script.graphs.Length;i++) {

			NavGraph graph = script.graphs[i];


			if (graph == null || graphEditors.Length <= i) {
				continue;
			}

			graphEditors[i].OnDrawGizmos ();
		}

		AstarProfiler.EndProfile ("OnDrawGizmosEditor");
	}

	bool HandleUndo () {
		//The user has tried to undo something, apply that
		if (script.astarData.GetData() == null) {
			script.astarData.SetData (new byte[0]);
		} else {
			LoadGraphs ();
			return true;
		}
		return false;
	}

	static int ByteArrayHash ( byte[] arr ) {
		if ( arr == null ) return -1;
		int h = -1;
		for ( int i=0;i<arr.Length;i++) {
			h ^= (arr[i]^i)*3221;
		}
		return h;
	}

	void SerializeIfDataChanged () {
		uint checksum;
		byte[] bytes = SerializeGraphs (out checksum);

		int byteHash = ByteArrayHash(bytes);
		int dataHash = ByteArrayHash(script.astarData.GetData());
		//Check if the data is different than the previous data, use checksums
		bool isDifferent = checksum != ignoredChecksum && dataHash != byteHash;

		//Only save undo if the data was different from the last saved undo
		if (isDifferent) {
			//Assign the new data
			script.astarData.SetData (bytes);

			EditorUtility.SetDirty (script);
			Undo.IncrementCurrentGroup ();
			Undo.RegisterCompleteObjectUndo (script, "A* Graph Settings");
		}
	}

	/** Called when an undo or redo operation has been performed */
	void OnUndoRedoPerformed () {

		if (!this) return;

		uint checksum;
		byte[] bytes = SerializeGraphs (out checksum);

		//Check if the data is different than the previous data, use checksums
		bool isDifferent = ByteArrayHash(script.astarData.GetData()) != ByteArrayHash(bytes);

		if (isDifferent ) {
			HandleUndo ();
		}

		CheckGraphEditors ();
		// Deserializing a graph does not necessarily yield the same hash as the data loaded from
		// this is (probably) because editor settings are not saved all the time
		// so we explicitly ignore the new hash
		SerializeGraphs (out checksum);
		ignoredChecksum = checksum;
	}

	public void SaveGraphsAndUndo (EventType et = EventType.Used, string eventCommand = "" ) {
		// Serialize the settings of the graphs

		// Dont process undo events in editor, we don't want to reset graphs
		if ( Application.isPlaying ) {
			return;
		}

		if ( (Undo.GetCurrentGroup () != lastUndoGroup || et == EventType.MouseUp) && eventCommand != "UndoRedoPerformed") {
			SerializeIfDataChanged ();

			lastUndoGroup = Undo.GetCurrentGroup ();
		}

		if (Event.current == null || script.astarData.GetData() == null) {
			SerializeIfDataChanged ();
			return;
		}
	}

	public void LoadGraphs () {
		//Load graphs from serialized data
		DeserializeGraphs ();
	}

	public byte[] SerializeGraphs (out uint checksum) {

		var settings = Pathfinding.Serialization.SerializeSettings.Settings;
		settings.editorSettings = true;
		byte[] bytes = SerializeGraphs (settings, out checksum);
		return bytes;
	}

	public byte[] SerializeGraphs (Pathfinding.Serialization.SerializeSettings settings, out uint checksum) {
		byte[] bytes = null;
		uint ch = 0;

		// Add a work item since we cannot be sure that pathfinding (or graph updates)
		// is not running at the same time
		AstarPath.active.AddWorkItem (new AstarPath.AstarWorkItem (force => {
			var sr = new Pathfinding.Serialization.AstarSerializer(script.astarData, settings);
			sr.OpenSerialize();
			script.astarData.SerializeGraphsPart (sr);
			sr.SerializeEditorSettings (graphEditors);
			bytes = sr.CloseSerialize();
			ch = sr.GetChecksum ();
			return true;
		}));

		// Make sure the above work item is executed immediately
		AstarPath.active.FlushWorkItems();
		checksum = ch;
		return bytes;
	}

	void DeserializeGraphs () {

		if (script.astarData.GetData() == null || script.astarData.GetData().Length == 0) {
			script.astarData.graphs = new NavGraph[0];
			return;
		}

		DeserializeGraphs (script.astarData.GetData());
	}

	void DeserializeGraphs (byte[] bytes) {

		AstarPath.active.AddWorkItem (new AstarPath.AstarWorkItem (force => {
			var sr = new Pathfinding.Serialization.AstarSerializer(script.astarData);
			if (sr.OpenDeserialize(bytes)) {
				script.astarData.DeserializeGraphsPart (sr);

				// Make sure every graph has a graph editor
				CheckGraphEditors ();
				sr.DeserializeEditorSettings (graphEditors);

				sr.CloseDeserialize();
			} else {
				Debug.LogWarning ("Invalid data file (cannot read zip).\nThe data is either corrupt or it was saved using a 3.0.x or earlier version of the system");
				// Make sure every graph has a graph editor
				CheckGraphEditors ();
			}
			return true;
		}));

		// Make sure the above work item is run directly
		AstarPath.active.FlushWorkItems();
	}

	[MenuItem ("Edit/Pathfinding/Scan All Graphs %&s")]
	public static void MenuScan () {

		if (AstarPath.active == null) {
			AstarPath.active = FindObjectOfType(typeof(AstarPath)) as AstarPath;
			if (AstarPath.active == null) {
				return;
			}
		}

		if (!Application.isPlaying && (AstarPath.active.astarData.graphs == null || AstarPath.active.astarData.graphTypes == null)) {
			EditorUtility.DisplayProgressBar ("Scanning","Deserializing",0);
			AstarPath.active.astarData.DeserializeGraphs ();
		}

		EditorUtility.DisplayProgressBar ("Scanning","Scanning...",0);

		try {
			OnScanStatus info = progress => EditorUtility.DisplayProgressBar ("Scanning",progress.description,progress.progress);
			AstarPath.active.ScanLoop (info);

		} catch (System.Exception e) {
			Debug.LogError ("There was an error generating the graphs:\n"+e+"\n\nIf you think this is a bug, please contact me on arongranberg.com (post a comment)\n");
			EditorUtility.DisplayDialog ("Error Generating Graphs","There was an error when generating graphs, check the console for more info","Ok");
			throw e;
		} finally {
			EditorUtility.ClearProgressBar();
		}
	}

	/** Searches in the current assembly for GraphEditor and NavGraph types */
	void FindGraphTypes () {
		graphEditorTypes = new Dictionary<string,CustomGraphEditorAttribute> ();

		Assembly asm = Assembly.GetAssembly (typeof(AstarPathEditor));

		System.Type[] types = asm.GetTypes ();

		var graphList = new List<System.Type> ();


		// Iterate through the assembly for classes which inherit from GraphEditor
		foreach (System.Type type in types) {

			System.Type baseType = type.BaseType;
			while (!System.Type.Equals (baseType, null)) {
				if (System.Type.Equals ( baseType, typeof(GraphEditor) )) {

					System.Object[] att = type.GetCustomAttributes (false);

					// Loop through the attributes for the CustomGraphEditorAttribute attribute
					foreach (System.Object attribute in att) {
						var cge = attribute as CustomGraphEditorAttribute;

						if (cge != null && !System.Type.Equals (cge.graphType, null)) {
							cge.editorType = type;
							graphList.Add (cge.graphType);
							graphEditorTypes.Add (cge.graphType.Name,cge);
						}

					}
					break;
				}

				baseType = baseType.BaseType;
			}
		}

		// Make sure graph types (not graph editor types) are also up to date
		script.astarData.FindGraphTypes ();

	}
}

