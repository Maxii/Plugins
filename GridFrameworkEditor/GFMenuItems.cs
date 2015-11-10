#define PLAYMAKER_PRESENT
using UnityEngine;
using UnityEditor;
using System;
using System.IO; // for StreamReader
using System.Text.RegularExpressions; // for Regex

public static class GFMenuItems {
    /// <summary>URL of online documentation.</summary>
    private const string onlineHelpURL = "http://hiphish.github.io/grid-framework/documentation/";

    /// <summary>URL of offline documentation.</summary>
    private const string offlineHelpURL = "/WebPlayerTemplates/Grid Framework Documentation/html/index.html";

#if PLAYMAKER_PRESENT
    /// <summary>Path to Playmaker action scripts</summary>
    private const string playMakerScriptsPath = "/Plugins/Grid Framework/PlayMaker Actions/";

    /// <summary>List of files using Playmaker.</summary>
    private static readonly string[] playMakerScripts = {
		"FsmGFStateAction"        ,
		"FsmGFStateActionGetSet"  ,
		"FsmGFStateActionMethods" ,
	};
#endif // PLAYMAKER_PRESENT

    #region Grid Creation
    [MenuItem("GameObject/3D Object/Grid/Rectangular Grid", false)]
    public static void CreateRectGrid() {
        CreateGrid<GFRectGrid>("Rectangular");
    }

    [MenuItem("GameObject/3D Object/Grid/Hexagonal Grid", false)]
    public static void CreateHexGrid() {
        CreateGrid<GFHexGrid>("Hexagonal");
    }

    [MenuItem("GameObject/3D Object/Grid/Polar Grid", false)]
    public static void CreatePolarGrid() {
        CreateGrid<GFPolarGrid>("Polar");
    }
    #endregion

    /// <summary>Validation for adding a component.</summary> 
    [MenuItem("Component/Grid Framework/GFRectGrid", true)]
    [MenuItem("Component/Grid Framework/GFHexGrid", true)]
    [MenuItem("Component/Grid Framework/GFPolarGrid", true)]
    [MenuItem("Component/Grid Framework/Camera/GFGridRenderCamera", true)]
    [MenuItem("Component/Grid Framework/Debug/GridDebugger", true)]
    [MenuItem("Component/Grid Framework/Debug/HexConversionDebugger", true)]
    [MenuItem("Component/Grid Framework/Debug/PolarConversionDebugger", true)]
    public static bool ValidateAddGrid() {
        return Selection.gameObjects.Length != 0;
    }

    #region Grid Component
    [MenuItem("Component/Grid Framework/GFRectGrid")]
    public static void AddRectGrid() {
        AddGrid<GFRectGrid>();
    }

    [MenuItem("Component/Grid Framework/GFHexGrid")]
    public static void AddHexGrid() {
        AddGrid<GFHexGrid>();
    }

    [MenuItem("Component/Grid Framework/GFPolarGrid")]
    public static void AddPolarGrid() {
        AddGrid<GFPolarGrid>();
    }
    #endregion

    #region Camera Scripts
    [MenuItem("Component/Grid Framework/Camera/GFGridRenderCamera")]
    public static void AddGridRenderCamera() {
        foreach (GameObject go in Selection.gameObjects) {
            CheckComponent<GFGridRenderCamera>(go, go.GetComponent<Camera>());
        }
    }
    #endregion

    #region Debug Scripts
    [MenuItem("Component/Grid Framework/Debug/GridDebugger")]
    public static void AddGridDebugger() {
        foreach (GameObject go in Selection.gameObjects) {
            CheckComponent<GridDebugger>(go);
        }
    }

    [MenuItem("Component/Grid Framework/Debug/HexConversionDebugger")]
    public static void AddHexConversionDebugger() {
        foreach (GameObject go in Selection.gameObjects) {
            CheckComponent<HexConversionDebugger>(go);
        }
    }

    [MenuItem("Component/Grid Framework/Debug/PolarConversionDebugger")]
    public static void AddPolarConversionDebugger() {
        foreach (GameObject go in Selection.gameObjects) {
            CheckComponent<PolarConversionDebugger>(go);
        }
    }
    #endregion

    #region Help Menu
    [MenuItem("Help/Grid Framework Documentation", false, -1)]
    public static void BrowseGridFrameworkDocs() {
        if (File.Exists(Application.dataPath + offlineHelpURL)) {
            Help.ShowHelpPage("file://" + Application.dataPath + offlineHelpURL);
        }
        else {
            Help.BrowseURL(onlineHelpURL);
        }
    }
    #endregion

    #region Playmaker
#if PLAYMAKER_PRESENT
    [MenuItem("Component/Grid Framework/Toggle Playmaker actions", true, 0)]
    public static bool ValidatePlaymakerActions() {
        bool exists = false;
        /* Unity versions before 5 */
        exists |= File.Exists(Application.dataPath + "/PlayMaker/PlayMaker.dll");
        /* Unity version 5 */
        exists |= File.Exists(Application.dataPath + "/Plugins/PlayMaker/PlayMaker.dll");
        return exists;
    }

    [MenuItem("Component/Grid Framework/Toggle Playmaker actions", false, 0)]
    public static void EnablePlaymakerActions() {
        bool enabled = false, success = true;

        foreach (string script in playMakerScripts) {
            try {
                enabled = SwapInFile(
                    Application.dataPath + playMakerScriptsPath + script + ".cs",
                    "//#define PLAYMAKER_PRESENT", "#define PLAYMAKER_PRESENT"
                );
            }
            catch {
                Debug.LogError(
                    "Failed to toggle the script " + script + ". " +
                    "Please try re-importing Grid Framework and PlayMaker. " +
                    "If that doesn't help report the error"
                );
                success = false;
            }
        }
        if (success) {
            Debug.Log((enabled ? "Enabled " : "Disabled ") + "PlayMaker actions for Grid Framework.");
        }
        else {
            Debug.Log("Failed to toggle PlayMaker actions for Grid Framework.");
        }
        AssetDatabase.Refresh();
    }
#endif // PLAYMAKER_PRESENT
    #endregion

    #region Helper Functions
    /// <summary>Creates the specified type of grid.</summary>
    /// <param name="name">Name of our grid (the word "Grid" will be appended).</param>
    /// <typeparam name="T">The type of grid.</typeparam>
    ///
    /// This method instantiates a new GameObject and attaches the specified
    /// type of grid as its component. Then it positions it at the pivot point
    /// of the scene view.
    private static void CreateGrid<T>(String name) where T : GFGrid {
        var go = new GameObject(name + " Grid");
        go.AddComponent<T>();
        //set go's position to the scene view's pivot point, the "centre" of the scene editor.
        go.transform.position = SceneView.lastActiveSceneView.pivot;
        //The SceneView class is undocumented, so this could break in the future.
    }

    /// <summary>Adds the specified type of grid to all selected `GameObject`s.</summary>
    /// <typeparam name="T">The type of grid to attach.</typeparam>
    private static void AddGrid<T>() where T : GFGrid {
        foreach (GameObject go in Selection.gameObjects) {
            CheckComponent<T>(go);
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
    private static T CheckComponent<T>(GameObject go, bool condition = true) where T : Component {
        T comp = go.GetComponent<T>();
        if (!condition)
            return comp;
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }

    /// <summary>Swaps a given text in a file the in file.</summary>
    /// <returns>
    ///   <c>true</c>, if <paramref name="searchText"/> was replaced by
    ///   <paramref name="swapText"/>, <c>false</c> otherwise if the other
    ///   way around.
    /// </returns>
    /// <param name="filePath">Path to the file we want to operate on.</param>
    /// <param name="searchText">Text to be replaced.</param>
    /// <param name="swapText">Text to replace with.</param>
    ///
    /// <para>
    ///   This function will search a given file for instances of <paramref
    ///   name="searchText"/> and replace them with <paramref
    ///   name="swapText"/>.  If no instance can be found the application
    ///   will do the reverse, therefore calling the function twice will revert
    ///   back to normal.
    /// </para>
    /// <para>This function was taken from the open source code of GameAnalytics.</para>
    public static bool SwapInFile(string filePath, string searchText, string swapText) {
        bool enabled; // false

        var reader = new StreamReader(filePath);
        string content = reader.ReadToEnd();
        reader.Close();

        if (content.Contains(searchText)) {
            enabled = true;
            content = Regex.Replace(content, searchText, swapText);
        }
        else {
            enabled = false;
            content = Regex.Replace(content, swapText, searchText);
        }

        var writer = new StreamWriter(filePath);
        writer.Write(content);
        writer.Close();

        return enabled;
    }

    #endregion
}
