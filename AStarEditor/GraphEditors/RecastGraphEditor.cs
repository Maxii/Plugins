using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Pathfinding.Util;

namespace Pathfinding {
	/**
	 * Editor for the RecastGraph.
	 * \astarpro
	 */
	[CustomGraphEditor(typeof(RecastGraph), "RecastGraph")]
	public class RecastGraphEditor : GraphEditor {
		public static bool tagMaskFoldout;

#if !UNITY_5_1 && !UNITY_5_0
		/** Material to use for the navmesh in the editor */
		static Material navmeshMaterial;

		/** Material to use for the navmeshe outline in the editor */
		static Material navmeshOutlineMaterial;

		/**
		 * Meshes for visualizing the navmesh.
		 * Used in OnDrawGizmos.
		 */
		List<GizmoTile> gizmoMeshes = new List<GizmoTile>();

		/** Holds a surface and an outline visualization for a navmesh tile */
		struct GizmoTile {
			public RecastGraph.NavmeshTile tile;
			public int hash;
			public Mesh surfaceMesh;
			public Mesh outlineMesh;
		}
#endif

		public enum UseTiles {
			UseTiles = 0,
			DontUseTiles = 1
		}

#if !UNITY_5_1 && !UNITY_5_0
		public override void OnEnable () {
			navmeshMaterial = AssetDatabase.LoadAssetAtPath(AstarPathEditor.editorAssets + "/Materials/Navmesh.mat", typeof(Material)) as Material;
			navmeshOutlineMaterial = AssetDatabase.LoadAssetAtPath(AstarPathEditor.editorAssets + "/Materials/NavmeshOutline.mat", typeof(Material)) as Material;
		}

		public override void UnloadGizmoMeshes () {
			// Avoid memory leaks
			for (int i = 0; i < gizmoMeshes.Count; i++) {
				Mesh.DestroyImmediate(gizmoMeshes[i].surfaceMesh);
				Mesh.DestroyImmediate(gizmoMeshes[i].outlineMesh);
			}
			gizmoMeshes.Clear();
		}

		/** Updates the meshes used in OnDrawGizmos to visualize the navmesh */
		void UpdateDebugMeshes () {
			var graph = target as RecastGraph;

			var tiles = graph.GetTiles();

			if (tiles == null || tiles.Length != gizmoMeshes.Count) {
				// Destroy all previous meshes
				UnloadGizmoMeshes();
			}

			if (tiles != null) {
				// Update navmesh vizualizations for
				// the tiles that have been changed
				for (int i = 0; i < tiles.Length; i++) {
					bool validTile = i < gizmoMeshes.Count && gizmoMeshes[i].tile == tiles[i];

					// Calculate a hash of the tile
					int hash = 0;
					const int HashPrime = 31;
					var nodes = tiles[i].nodes;
					for (int j = 0; j < nodes.Length; j++) {
						hash = hash*HashPrime;
						var node = nodes[j];
						hash ^= node.position.GetHashCode();
						hash ^= 17 * (node.connections != null ? node.connections.Length : -1);
						hash ^= 19 * (int)node.Penalty;
						hash ^= 41 * (int)node.Tag;
						hash ^= 57 * (int)node.Area;
					}

					hash ^= 67 * (int)AstarPath.active.debugMode;
					hash ^= 73 * AstarPath.active.debugFloor.GetHashCode();
					hash ^= 79 * AstarPath.active.debugRoof.GetHashCode();

					validTile = validTile && hash == gizmoMeshes[i].hash;

					if (!validTile) {
						// Tile needs to be updated
						var newTile = new GizmoTile {
							tile = tiles[i],
							hash = hash,
							surfaceMesh = CreateNavmeshSurfaceVisualization(tiles[i]),
							outlineMesh = CreateNavmeshOutlineVisualization(tiles[i])
						};

						if (i < gizmoMeshes.Count) {
							// Destroy and replace existing mesh
							Mesh.DestroyImmediate(gizmoMeshes[i].surfaceMesh);
							Mesh.DestroyImmediate(gizmoMeshes[i].outlineMesh);
							gizmoMeshes[i] = newTile;
						} else {
							gizmoMeshes.Add(newTile);
						}
					}
				}
			}
		}

		/** Creates a mesh of the surfaces of the navmesh for use in OnDrawGizmos in the editor */
		Mesh CreateNavmeshSurfaceVisualization (RecastGraph.NavmeshTile tile) {
			var mesh = new Mesh();

			mesh.hideFlags = HideFlags.DontSave;

			var vertices = ListPool<Vector3>.Claim(tile.verts.Length);
			var colors = ListPool<Color32>.Claim(tile.verts.Length);

			for (int j = 0; j < tile.verts.Length; j++) {
				vertices.Add((Vector3)tile.verts[j]);
				colors.Add(new Color32());
			}

			// TODO: Uses AstarPath.active
			var debugPathData = AstarPath.active.debugPathData;

			for (int j = 0; j < tile.nodes.Length; j++) {
				var node = tile.nodes[j];
				for (int v = 0; v < 3; v++) {
					var color = target.NodeColor(node, debugPathData);
					colors[node.GetVertexArrayIndex(v)] = (Color32)color;//(Color32)AstarColor.GetAreaColor(node.Area);
				}
			}

			mesh.SetVertices(vertices);
			mesh.SetTriangles(tile.tris, 0);
			mesh.SetColors(colors);

			// Upload all data and mark the mesh as unreadable
			mesh.UploadMeshData(true);

			// Return lists to the pool
			ListPool<Vector3>.Release(vertices);
			ListPool<Color32>.Release(colors);

			return mesh;
		}

		/** Creates an outline of the navmesh for use in OnDrawGizmos in the editor */
		static Mesh CreateNavmeshOutlineVisualization (RecastGraph.NavmeshTile tile) {
			var sharedEdges = new bool[3];

			var mesh = new Mesh();

			mesh.hideFlags = HideFlags.DontSave;

			var colorList = ListPool<Color32>.Claim();
			var edgeList = ListPool<Int3>.Claim();

			for (int j = 0; j < tile.nodes.Length; j++) {
				sharedEdges[0] = sharedEdges[1] = sharedEdges[2] = false;

				var node = tile.nodes[j];
				for (int c = 0; c < node.connections.Length; c++) {
					var other = node.connections[c] as TriangleMeshNode;

					// Loop throgh neighbours to figure
					// out which edges are shared
					if (other != null && other.GraphIndex == node.GraphIndex) {
						for (int v = 0; v < 3; v++) {
							for (int v2 = 0; v2 < 3; v2++) {
								if (node.GetVertexIndex(v) == other.GetVertexIndex((v2+1)%3) && node.GetVertexIndex((v+1)%3) == other.GetVertexIndex(v2)) {
									// Found a shared edge with the other node
									sharedEdges[v] = true;
									v = 3;
									break;
								}
							}
						}
					}
				}

				for (int v = 0; v < 3; v++) {
					if (!sharedEdges[v]) {
						edgeList.Add(node.GetVertex(v));
						edgeList.Add(node.GetVertex((v+1)%3));
						var color = (Color32)AstarColor.GetAreaColor(node.Area);
						colorList.Add(color);
						colorList.Add(color);
					}
				}
			}

			// Use pooled lists to avoid excessive allocations
			var vertices = ListPool<Vector3>.Claim(edgeList.Count*2);
			var colors = ListPool<Color32>.Claim(edgeList.Count*2);
			var normals = ListPool<Vector3>.Claim(edgeList.Count*2);
			var tris = ListPool<int>.Claim(edgeList.Count*3);

			// Loop through each endpoint of the lines
			// and add 2 vertices for each
			for (int j = 0; j < edgeList.Count; j++) {
				var vertex = (Vector3)edgeList[j];
				vertices.Add(vertex);
				vertices.Add(vertex);

				// Encode the side of the line
				// in the alpha component
				var color = colorList[j];
				colors.Add(new Color32(color.r, color.g, color.b, 0));
				colors.Add(new Color32(color.r, color.g, color.b, 255));
			}

			// Loop through each line and add
			// one normal for each vertex
			for (int j = 0; j < edgeList.Count; j += 2) {
				var lineDir = (Vector3)(edgeList[j+1] - edgeList[j]);
				lineDir.Normalize();

				// Store the line direction in the normals
				// A line consists of 4 vertices
				// The line direction will be used to
				// offset the vertices to create a
				// line with a fixed pixel thickness
				normals.Add(lineDir);
				normals.Add(lineDir);
				normals.Add(lineDir);
				normals.Add(lineDir);
			}

			// Setup triangle indices
			// A triangle consists of 3 indices
			// A line (4 vertices) consists of 2 triangles, so 6 triangle indices
			for (int j = 0, v = 0; j < edgeList.Count*3; j += 6, v += 4) {
				// First triangle
				tris.Add(v+0);
				tris.Add(v+1);
				tris.Add(v+2);

				// Second triangle
				tris.Add(v+1);
				tris.Add(v+3);
				tris.Add(v+2);
			}

			// Set all data on the mesh
			mesh.SetVertices(vertices);
			mesh.SetTriangles(tris, 0);
			mesh.SetColors(colors);
			mesh.SetNormals(normals);

			// Upload all data and mark the mesh as unreadable
			mesh.UploadMeshData(true);

			// Release the lists back to the pool
			ListPool<Color32>.Release(colorList);
			ListPool<Int3>.Release(edgeList);

			ListPool<Vector3>.Release(vertices);
			ListPool<Color32>.Release(colors);
			ListPool<Vector3>.Release(normals);
			ListPool<int>.Release(tris);

			return mesh;
		}

		public override void OnDrawGizmos () {
			var graph = target as RecastGraph;

			if (graph.showMeshSurface) {
				UpdateDebugMeshes();

				for (int pass = 0; pass <= 2; pass++) {
					navmeshMaterial.SetPass(pass);
					for (int i = 0; i < gizmoMeshes.Count; i++) {
						Graphics.DrawMeshNow(gizmoMeshes[i].surfaceMesh, Matrix4x4.identity);
					}
				}

				navmeshOutlineMaterial.SetPass(0);
				for (int i = 0; i < gizmoMeshes.Count; i++) {
					Graphics.DrawMeshNow(gizmoMeshes[i].outlineMesh, Matrix4x4.identity);
				}
			}
		}
#endif

		public override void OnInspectorGUI (NavGraph target) {
			var graph = target as RecastGraph;

			bool preEnabled = GUI.enabled;

			System.Int64 estWidth = Mathf.RoundToInt(Mathf.Ceil(graph.forcedBoundsSize.x / graph.cellSize));
			System.Int64 estDepth = Mathf.RoundToInt(Mathf.Ceil(graph.forcedBoundsSize.z / graph.cellSize));

			// Show a warning if the number of voxels is too large
			if (estWidth*estDepth >= 1024*1024 || estDepth >= 1024*1024 || estWidth >= 1024*1024) {
				GUIStyle helpBox = GUI.skin.FindStyle("HelpBox") ?? GUI.skin.FindStyle("Box");

				Color preColor = GUI.color;
				if (estWidth*estDepth >= 2048*2048 || estDepth >= 2048*2048 || estWidth >= 2048*2048) {
					GUI.color = Color.red;
				} else {
					GUI.color = Color.yellow;
				}

				GUILayout.Label("Warning : Might take some time to calculate", helpBox);
				GUI.color = preColor;
			}

			GUI.enabled = false;
			EditorGUILayout.LabelField("Width (voxels)", estWidth.ToString());

			EditorGUILayout.LabelField("Depth (voxels)", estDepth.ToString());
			GUI.enabled = preEnabled;

			graph.cellSize = EditorGUILayout.FloatField(new GUIContent("Cell Size", "Size of one voxel in world units"), graph.cellSize);
			if (graph.cellSize < 0.001F) graph.cellSize = 0.001F;

			graph.cellHeight = EditorGUILayout.FloatField(new GUIContent("Cell Height", "Height of one voxel in world units"), graph.cellHeight);
			if (graph.cellHeight < 0.001F) graph.cellHeight = 0.001F;

			graph.useTiles = (UseTiles)EditorGUILayout.EnumPopup("Use Tiles", graph.useTiles ? UseTiles.UseTiles : UseTiles.DontUseTiles) == UseTiles.UseTiles;

			if (graph.useTiles) {
				EditorGUI.indentLevel++;
				graph.editorTileSize = EditorGUILayout.IntField(new GUIContent("Tile Size", "Size in voxels of a single tile.\n" +
						"This is the width of the tile.\n" +
						"\n" +
						"A large tile size can be faster to initially scan (but beware of out of memory issues if you try with a too large tile size in a large world)\n" +
						"smaller tile sizes are (much) faster to update.\n" +
						"\n" +
						"Different tile sizes can affect the quality of paths. It is often good to split up huge open areas into several tiles for\n" +
						"better quality paths, but too small tiles can lead to effects looking like invisible obstacles."), graph.editorTileSize);
				EditorGUI.indentLevel--;
			}

			graph.minRegionSize = EditorGUILayout.FloatField(new GUIContent("Min Region Size", "Small regions will be removed. In square world units"), graph.minRegionSize);

			graph.walkableHeight = EditorGUILayout.FloatField(new GUIContent("Walkable Height", "Minimum distance to the roof for an area to be walkable"), graph.walkableHeight);
			graph.walkableHeight = Mathf.Max(graph.walkableHeight, 0);

			graph.walkableClimb = EditorGUILayout.FloatField(new GUIContent("Walkable Climb", "How high can the character climb"), graph.walkableClimb);

			// A walkableClimb higher than this can cause issues when generating the navmesh since then it can in some cases
			// Both be valid for a character to walk under an obstacle and climb up on top of it (and that cannot be handled with a navmesh without links)
			if (graph.walkableClimb >= graph.walkableHeight) {
				graph.walkableClimb = graph.walkableHeight;
				EditorGUILayout.HelpBox("Walkable climb should be less than walkable height. Clamping to " + graph.walkableHeight+".", MessageType.Warning);
			} else if (graph.walkableClimb < 0) {
				graph.walkableClimb = 0;
			}

			graph.characterRadius = EditorGUILayout.FloatField(new GUIContent("Character Radius", "Radius of the character. It's good to add some margin.\nIn world units."), graph.characterRadius);
			graph.characterRadius = Mathf.Max(graph.characterRadius, 0);

			if (graph.characterRadius < graph.cellSize * 2) {
				EditorGUILayout.HelpBox("For best navmesh quality, it is recommended to keep the character radius to at least 2 times the cell size. Smaller cell sizes will give you higher quality navmeshes, but it will take more time to scan the graph.", MessageType.Warning);
			}

			graph.maxSlope = EditorGUILayout.Slider(new GUIContent("Max Slope", "Approximate maximum slope"), graph.maxSlope, 0F, 90F);
			graph.maxEdgeLength = EditorGUILayout.FloatField(new GUIContent("Max Edge Length", "Maximum length of one edge in the completed navmesh before it is split. A lower value can often yield better quality graphs"), graph.maxEdgeLength);
			graph.maxEdgeLength = graph.maxEdgeLength < graph.cellSize ? graph.cellSize : graph.maxEdgeLength;

			graph.contourMaxError = EditorGUILayout.FloatField(new GUIContent("Max Edge Error", "Amount of simplification to apply to edges.\nIn world units."), graph.contourMaxError);

			graph.rasterizeTerrain = EditorGUILayout.Toggle(new GUIContent("Rasterize Terrain", "Should a rasterized terrain be included"), graph.rasterizeTerrain);
			if (graph.rasterizeTerrain) {
				EditorGUI.indentLevel++;
				graph.rasterizeTrees = EditorGUILayout.Toggle(new GUIContent("Rasterize Trees", "Rasterize tree colliders on terrains. " +
						"If the tree prefab has a collider, that collider will be rasterized. " +
						"Otherwise a simple box collider will be used and the script will " +
						"try to adjust it to the tree's scale, it might not do a very good job though so " +
						"an attached collider is preferable."), graph.rasterizeTrees);
				if (graph.rasterizeTrees) {
					EditorGUI.indentLevel++;
					graph.colliderRasterizeDetail = EditorGUILayout.FloatField(new GUIContent("Collider Detail", "Controls the detail of the generated collider meshes. "+
							"Increasing does not necessarily yield better navmeshes, but lowering will speed up scan.\n"+
							"Spheres and capsule colliders will be converted to meshes in order to be able to rasterize them, a higher value will increase the number of triangles in those meshes."), graph.colliderRasterizeDetail);
					EditorGUI.indentLevel--;
				}

				graph.terrainSampleSize = EditorGUILayout.IntField(new GUIContent("Terrain Sample Size", "Size of terrain samples. A lower value is better, but slower"), graph.terrainSampleSize);
				graph.terrainSampleSize = graph.terrainSampleSize < 1 ? 1 : graph.terrainSampleSize;//Clamp to at least 1
				EditorGUI.indentLevel--;
			}

			graph.rasterizeMeshes = EditorGUILayout.Toggle(new GUIContent("Rasterize Meshes", "Should meshes be rasterized and used for building the navmesh"), graph.rasterizeMeshes);
			graph.rasterizeColliders = EditorGUILayout.Toggle(new GUIContent("Rasterize Colliders", "Should colliders be rasterized and used for building the navmesh"), graph.rasterizeColliders);
			if (graph.rasterizeColliders) {
				EditorGUI.indentLevel++;
				graph.colliderRasterizeDetail = EditorGUILayout.FloatField(new GUIContent("Collider Detail", "Controls the detail of the generated collider meshes. "+
						"Increasing does not necessarily yield better navmeshes, but lowering will speed up scan.\n"+
						"Spheres and capsule colliders will be converted to meshes in order to be able to rasterize them, a higher value will increase the number of triangles in those meshes."), graph.colliderRasterizeDetail);
				EditorGUI.indentLevel--;
			}

			if (graph.rasterizeMeshes && graph.rasterizeColliders) {
				EditorGUILayout.HelpBox("You are rasterizing both meshes and colliders, this might just be duplicating the work that is done if the colliders and meshes are similar in shape. You can use the RecastMeshObj component" +
					" to always include some specific objects regardless of what the above settings are set to.", MessageType.Info);
			}

			Separator();

			graph.forcedBoundsCenter = EditorGUILayout.Vector3Field("Center", graph.forcedBoundsCenter);
			graph.forcedBoundsSize = EditorGUILayout.Vector3Field("Size", graph.forcedBoundsSize);

			if (GUILayout.Button(new GUIContent("Snap bounds to scene", "Will snap the bounds of the graph to exactly contain all meshes that the bounds currently touches"))) {
				graph.SnapForceBoundsToScene();
				GUI.changed = true;
			}

			Separator();

			EditorGUILayout.HelpBox("Objects contained in any of these masks will be rasterized", MessageType.None);
			graph.mask = EditorGUILayoutx.LayerMaskField("Layer Mask", graph.mask);
			tagMaskFoldout = EditorGUILayoutx.UnityTagMaskList(new GUIContent("Tag Mask"), tagMaskFoldout, graph.tagMask);

			Separator();

			// The surface code is not compatible with Unity 5.0 or 5.1
#if !UNITY_5_1 && !UNITY_5_0
			graph.showMeshSurface = EditorGUILayout.Toggle(new GUIContent("Show mesh surface", "Toggles gizmos for drawing the surface of the mesh"), graph.showMeshSurface);
#endif
			graph.showMeshOutline = EditorGUILayout.Toggle(new GUIContent("Show mesh outline", "Toggles gizmos for drawing an outline of the mesh"), graph.showMeshOutline);
			graph.showNodeConnections = EditorGUILayout.Toggle(new GUIContent("Show node connections", "Toggles gizmos for drawing node connections"), graph.showNodeConnections);


			Separator();
			GUILayout.Label(new GUIContent("Advanced"), EditorStyles.boldLabel);

			if (GUILayout.Button("Export to .obj file")) {
				ExportToFile(graph);
			}

			graph.relevantGraphSurfaceMode = (RecastGraph.RelevantGraphSurfaceMode)EditorGUILayout.EnumPopup(new GUIContent("Relevant Graph Surface Mode",
					"Require every region to have a RelevantGraphSurface component inside it.\n" +
					"A RelevantGraphSurface component placed in the scene specifies that\n" +
					"the navmesh region it is inside should be included in the navmesh.\n\n" +
					"If this is set to OnlyForCompletelyInsideTile\n" +
					"a navmesh region is included in the navmesh if it\n" +
					"has a RelevantGraphSurface inside it, or if it\n" +
					"is adjacent to a tile border. This can leave some small regions\n" +
					"which you didn't want to have included because they are adjacent\n" +
					"to tile borders, but it removes the need to place a component\n" +
					"in every single tile, which can be tedious (see below).\n\n" +
					"If this is set to RequireForAll\n" +
					"a navmesh region is included only if it has a RelevantGraphSurface\n" +
					"inside it. Note that even though the navmesh\n" +
					"looks continous between tiles, the tiles are computed individually\n" +
					"and therefore you need a RelevantGraphSurface component for each\n" +
					"region and for each tile."),
				graph.relevantGraphSurfaceMode);

			graph.nearestSearchOnlyXZ = EditorGUILayout.Toggle(new GUIContent("Nearest node queries in XZ space",
					"Recomended for single-layered environments.\nFaster but can be inacurate esp. in multilayered contexts."), graph.nearestSearchOnlyXZ);
		}

		/** Exports the INavmesh graph to a .obj file */
		public static void ExportToFile (RecastGraph target) {
			//INavmesh graph = (INavmesh)target;
			if (target == null) return;

			RecastGraph.NavmeshTile[] tiles = target.GetTiles();

			if (tiles == null) {
				if (EditorUtility.DisplayDialog("Scan graph before exporting?", "The graph does not contain any mesh data. Do you want to scan it?", "Ok", "Cancel")) {
					AstarPathEditor.MenuScan();
					tiles = target.GetTiles();
					if (tiles == null) return;
				} else {
					return;
				}
			}

			string path = EditorUtility.SaveFilePanel("Export .obj", "", "navmesh.obj", "obj");
			if (path == "") return;

			//Generate .obj
			var sb = new System.Text.StringBuilder();

			string name = System.IO.Path.GetFileNameWithoutExtension(path);

			sb.Append("g ").Append(name).AppendLine();

			//Vertices start from 1
			int vCount = 1;

			//Define single texture coordinate to zero
			sb.Append("vt 0 0\n");

			for (int t = 0; t < tiles.Length; t++) {
				RecastGraph.NavmeshTile tile = tiles[t];

				if (tile == null) continue;

				Int3[] vertices = tile.verts;

				//Write vertices
				for (int i = 0; i < vertices.Length; i++) {
					var v = (Vector3)vertices[i];
					sb.Append(string.Format("v {0} {1} {2}\n", -v.x, v.y, v.z));
				}

				//Write triangles
				TriangleMeshNode[] nodes = tile.nodes;
				for (int i = 0; i < nodes.Length; i++) {
					TriangleMeshNode node = nodes[i];
					if (node == null) {
						Debug.LogError("Node was null or no TriangleMeshNode. Critical error. Graph type " + target.GetType().Name);
						return;
					}
					if (node.GetVertexArrayIndex(0) < 0 || node.GetVertexArrayIndex(0) >= vertices.Length) throw new System.Exception("ERR");

					sb.Append(string.Format("f {0}/1 {1}/1 {2}/1\n", (node.GetVertexArrayIndex(0) + vCount), (node.GetVertexArrayIndex(1) + vCount), (node.GetVertexArrayIndex(2) + vCount)));
				}

				vCount += vertices.Length;
			}

			string obj = sb.ToString();

			using (var sw = new System.IO.StreamWriter(path))
			{
				sw.Write(obj);
			}
		}
	}
}
