using UnityEngine;
using UnityEditor;
using System;
using System.IO; // for File

public static class GFMenuItems {
	/// <summary>URL of online documentation.</summary>
	private const string onlineHelpURL = "http://hiphish.github.io/grid-framework/documentation/";

	/// <summary>URL of offline documentation.</summary>
	private const string offlineHelpURL = "/WebPlayerTemplates/Grid Framework Documentation/html/index.html";

	#region Grid Creation
	[MenuItem("GameObject/3D Object/Grid/Rectangular Grid", false)]
	public static void CreateRectGrid(){
		CreateGrid<GFRectGrid>("Rectangular");
	}

	[MenuItem("GameObject/3D Object/Grid/Hexagonal Grid", false)]
	public static void CreateHexGrid(){
		CreateGrid<GFHexGrid>("Hexagonal");
	}

	[MenuItem("GameObject/3D Object/Grid/Polar Grid", false)]
	public static void CreatePolarGrid(){
		CreateGrid<GFPolarGrid>("Polar");
	}
	#endregion

	/// <summary>Validation for adding a component.</summary> 
	[MenuItem("Component/Grid Framework/GFRectGrid"                    , true)]
	[MenuItem("Component/Grid Framework/GFHexGrid"                     , true)]
	[MenuItem("Component/Grid Framework/GFPolarGrid"                   , true)]
	[MenuItem("Component/Grid Framework/Camera/GFGridRenderCamera"     , true)]
	[MenuItem("Component/Grid Framework/Debug/GridDebugger"            , true)]
	[MenuItem("Component/Grid Framework/Debug/HexConversionDebugger"   , true)]
	[MenuItem("Component/Grid Framework/Debug/PolarConversionDebugger" , true)]
	public static bool ValidateAddGrid(){
		return Selection.gameObjects.Length != 0;
	}

	#region Grid Component
	[MenuItem("Component/Grid Framework/GFRectGrid")]
	public static void AddRectGrid(){
		AddGrid<GFRectGrid>();
	}

	[MenuItem("Component/Grid Framework/GFHexGrid")]
	public static void AddHexGrid(){
		AddGrid<GFHexGrid>();
	}

	[MenuItem("Component/Grid Framework/GFPolarGrid")]
	public static void AddPolarGrid(){
		AddGrid<GFPolarGrid>();
	}
	#endregion

	#region Camera Scripts
	[MenuItem("Component/Grid Framework/Camera/GFGridRenderCamera")]
	public static void AddGridRenderCamera(){
		foreach(GameObject go in Selection.gameObjects){
			CheckComponent<GFGridRenderCamera> (go, go.GetComponent<Camera> ());
		}
	}
	#endregion

	#region Debug Scripts
	[MenuItem("Component/Grid Framework/Debug/GridDebugger")]
	public static void AddGridDebugger(){
		foreach(GameObject go in Selection.gameObjects){
			CheckComponent<GridDebugger> (go);
		}
	}

	[MenuItem("Component/Grid Framework/Debug/HexConversionDebugger")]
	public static void AddHexConversionDebugger(){
		foreach(GameObject go in Selection.gameObjects){
			CheckComponent<HexConversionDebugger> (go);
		}
	}

	[MenuItem("Component/Grid Framework/Debug/PolarConversionDebugger")]
	public static void AddPolarConversionDebugger(){
		foreach(GameObject go in Selection.gameObjects){
			CheckComponent<PolarConversionDebugger>(go);
		}
	}
	#endregion

	#region Help Menu
	[MenuItem("Help/Grid Framework Documentation", false, -1)]
	public static void BrowseGridFrameworkDocs () {
		if (File.Exists (Application.dataPath + offlineHelpURL)) {
			Help.ShowHelpPage("file://" + Application.dataPath + offlineHelpURL);
		} else {
			Help.BrowseURL(onlineHelpURL);
		}
	}
	#endregion

	#region Helper Functions
	/// <summary>Creates the specified type of grid.</summary>
	/// <param name="name">Name of our grid (the word "Grid" will be appended).</param>
	/// <typeparam name="T">The type of grid.</typeparam>
	///
	/// This method instantiates a new GameObject and attaches the specified
	/// type of grid as its component. Then it positions it at the pivot point
	/// of the scene view.
	private static void CreateGrid<T>(String name) where T : GFGrid{
		var go = new GameObject(name + " Grid");
		go.AddComponent<T>();
		//set go's position to the scene view's pivot point, the "centre" of the scene editor.
		go.transform.position = SceneView.lastActiveSceneView.pivot;
		//The SceneView class is undocumented, so this could break in the future.
	}

	/// <summary>Adds the specified type of grid to all selected `GameObject`s.</summary>
	/// <typeparam name="T">The type of grid to attach.</typeparam>
	private static void AddGrid<T>() where T : GFGrid{
		foreach(GameObject go in Selection.gameObjects){
			CheckComponent<T> (go);
		}
	}

	/// <summary>
	///   Checks whether a component is present, if not then it attaches it
	///   before returning it.
	/// </summary>
	/// <returns>
	///   The specified component; it always returns something if <c>condition
	///   is</c> _true_, otherwise it returns _null_ if nothing is found.
	/// </returns>
	/// <param name="go">GameObject to check.</param>
	/// <param name="condition">
	///   If set to <c>true</c> the the component will be added, otherwise the
	///   method just returns whatever it finds.
	/// </param>
	/// <typeparam name="T">The type of component.</typeparam>
	private static T CheckComponent<T> (GameObject go, bool condition = true) where T: Component {
		T comp = go.GetComponent<T> ();
		if (!condition)
			return comp;
		if (comp == null)
			comp = go.AddComponent<T> ();
		return comp;
	}

	#endregion
}
