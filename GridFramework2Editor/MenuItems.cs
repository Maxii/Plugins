using UnityEngine;
using UnityEditor;

using GridFramework.Grids;

using File = System.IO.File;

using   GridRenderer = GridFramework.Renderers.GridRenderer;
using Parallelepiped = GridFramework.Renderers.Rectangular.Parallelepiped;
using         Sphere = GridFramework.Renderers.Spherical.Sphere;
using       Cylinder = GridFramework.Renderers.Polar.Cylinder;
using           Cone = GridFramework.Renderers.Hexagonal.Cone;
using    Herringbone = GridFramework.Renderers.Hexagonal.Herringbone;
using      Rectangle = GridFramework.Renderers.Hexagonal.Rectangle;
using        Rhombus = GridFramework.Renderers.Hexagonal.Rhombus;


namespace GridFramework.Editor {
	/// <summary>
	///   Menu items for Grid Framework.
	/// </summary>
	public static class MenuItems {
#region  Private variables
		private const string helpURL
			= "/Plugins/GridFramework/Documentation/html/index.html";
#endregion  // Private variables

#region  Grid creation
		[MenuItem("GameObject/3D Object/Grid/Rectangular", false)]
		public static void CreateRectGrid(){
			CreateGrid<RectGrid, Parallelepiped>("Rectangular");
		}

		[MenuItem("GameObject/3D Object/Grid/Spheric", false)]
		public static void CreateSphereGrid(){
			CreateGrid<SphereGrid, Sphere>("Spheric");
		}

		[MenuItem("GameObject/3D Object/Grid/Polar", false)]
		public static void CreatePolarGrid(){
			CreateGrid<PolarGrid, Cylinder>("Polar");
		}

		[MenuItem("GameObject/3D Object/Grid/Hexagonal/Cone", false)]
		public static void CreateHexConeGrid(){
			CreateGrid<HexGrid, Cone>("Hexagonal");
		}

		[MenuItem("GameObject/3D Object/Grid/Hexagonal/Herringbone", false)]
		public static void CreateHexHerringboneGrid(){
			CreateGrid<HexGrid, Herringbone>("Hexagonal");
		}

		[MenuItem("GameObject/3D Object/Grid/Hexagonal/Rectangle", false)]
		public static void CreateHexRectangleGrid(){
			CreateGrid<HexGrid, Rectangle>("Hexagonal");
		}

		[MenuItem("GameObject/3D Object/Grid/Hexagonal/Rhombus", false)]
		public static void CreateHexRhombusGrid(){
			CreateGrid<HexGrid, Rhombus>("Hexagonal");
		}
#endregion  // Grid creation

#region  Help Menu
		[MenuItem("Help/Grid Framework Documentation", false, -1)]
		public static void BrowseGridFrameworkDocs() {
			Help.ShowHelpPage("file://" + Application.dataPath + helpURL);
		}
#endregion  // Help Menu

#region  Helper Functions
		/// <summary>
		///   Creates the specified type of grid.
		/// </summary>
		/// <param name="name">
		///   Name of our grid (the string " Grid" will be appended).
		/// </param>
		/// <typeparam name="G">
		///   The type of grid.
		/// </typeparam>
		/// <typeparam name="R">
		///   The type of Renderer.
		/// </typeparam>
		/// <remarks>
		///   <para>
		///     This method instantiates a new GameObject and attaches the
		///     specified type of grid and renderer as its components. Then it
		///     positions it at the pivot point of the scene view and selects
		///     it.
		///   </para>
		/// </remarks>
		private static void CreateGrid<TGrid, TRend>(string name) where TGrid : Grid where TRend : GridRenderer {
			var go = new GameObject(name + " Grid");
			go.AddComponent<TGrid>();
			go.AddComponent<TRend>();
			// Set go's position to the scene view's pivot point, the "centre"
			// of the scene editor. The SceneView class is undocumented, so
			// this could break in the future.
			go.transform.position = SceneView.lastActiveSceneView.pivot;
			Selection.activeGameObject = go;
		}
#endregion
	}
}
