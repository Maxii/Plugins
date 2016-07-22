using Math = System.Math;

using  System.IO;
using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization.JsonFx;
using Pathfinding.Voxels;
using Pathfinding.Serialization;

namespace Pathfinding {
	[System.Serializable]
	[JsonOptIn]
	/** Automatically generates navmesh graphs based on world geometry.
	 * The recast graph is based on Recast (http://code.google.com/p/recastnavigation/).\n
	 * I have translated a good portion of it to C# to run it natively in Unity. The Recast process is described as follows:
	 * - The voxel mold is build from the input triangle mesh by rasterizing the triangles into a multi-layer heightfield.
	 * Some simple filters are then applied to the mold to prune out locations where the character would not be able to move.
	 * - The walkable areas described by the mold are divided into simple overlayed 2D regions.
	 * The resulting regions have only one non-overlapping contour, which simplifies the final step of the process tremendously.
	 * - The navigation polygons are peeled off from the regions by first tracing the boundaries and then simplifying them.
	 * The resulting polygons are finally converted to convex polygons which makes them perfect for pathfinding and spatial reasoning about the level.
	 *
	 * It works exactly like that in the C# version as well, except that everything is triangulated to triangles instead of n-gons.
	 * The recast generation process usually works directly on the visiable geometry in the world, this is usually a good thing, because world geometry is usually more detailed than the colliders.
	 * You can however specify that colliders should be rasterized, if you have very detailed world geometry, this can speed up the scan.
	 *
	 * Check out the second part of the Get Started Tutorial which discusses recast graphs.
	 *
	 * \section export Exporting for manual editing
	 * In the editor there is a button for exporting the generated graph to a .obj file.
	 * Usually the generation process is good enough for the game directly, but in some cases you might want to edit some minor details.
	 * So you can export the graph to a .obj file, open it in your favourite 3D application, edit it, and export it to a mesh which Unity can import.
	 * You can then use that mesh in a navmesh graph.
	 *
	 * Since many 3D modelling programs use different axis systems (unity uses X=right, Y=up, Z=forward), it can be a bit tricky to get the rotation and scaling right.
	 * For blender for example, what you have to do is to first import the mesh using the .obj importer. Don't change anything related to axes in the settings.
	 * Then select the mesh, open the transform tab (usually the thin toolbar to the right of the 3D view) and set Scale -> Z to -1.
	 * If you transform it using the S (scale) hotkey, it seems to set both Z and Y to -1 for some reason.
	 * Then make the edits you need and export it as an .obj file to somewhere in the Unity project.
	 * But this time, edit the setting named "Forward" to "Z forward" (not -Z as it is per default).
	 *
	 * \shadowimage{recastgraph_graph.png}
	 * \shadowimage{recastgraph_inspector.png}
	 *
	 *
	 * \ingroup graphs
	 *
	 * \astarpro
	 */
	public class RecastGraph : NavGraph, INavmesh, IRaycastableGraph, IUpdatableGraph, INavmeshHolder {
		/** Enables graph updating.
		 * Uses more memory if enabled.
		 */
		public bool dynamic = true;


		[JsonMember]
		/** Radius of the agent which will traverse the navmesh.
		 * The navmesh will be eroded with this radius.
		 * \shadowimage{recast/character_radius.gif}
		 */
		public float characterRadius = 0.5F;

		/** Max distance from simplified edge to real edge.
		 * \shadowimage{recast/max_edge_error.gif}
		 */
		[JsonMember]
		public float contourMaxError = 2F;

		/** Voxel sample size (x,z).
		 * Lower values will yield higher quality navmeshes, however the graph will be slower to scan.
		 *
		 * \shadowimage{recast/cell_size.gif}
		 */
		[JsonMember]
		public float cellSize = 0.5F;

		/** Voxel sample size (y) */
		[JsonMember]
		public float cellHeight = 0.01F;

		/** Character height.
		 * \shadowimage{recast/walkable_height.gif}
		 */
		[JsonMember]
		public float walkableHeight = 2F;

		/** Height the character can climb.
		 * \shadowimage{recast/walkable_climb.gif}
		 */
		[JsonMember]
		public float walkableClimb = 0.5F;

		/** Max slope in degrees the character can traverse.
		 * \shadowimage{recast/max_slope.gif}
		 */
		[JsonMember]
		public float maxSlope = 30;

		/** Longer edges will be subdivided.
		 * Reducing this value can improve path quality since similarly sized polygons
		 * yield better paths than really large and really small next to each other
		 */
		[JsonMember]
		public float maxEdgeLength = 20;

		/** Minumum region size.
		 * Small regions will be removed from the navmesh.
		 * Measured in square world units (square meters in most games).
		 *
		 * \shadowimage{recast/min_region_size.gif}
		 *
		 * If a region is adjacent to a tile border, it will not be removed
		 * even though it is small since the adjacent tile might join it
		 * to form a larger region.
		 *
		 * \shadowimage{recast_minRegionSize_1.png}
		 * \shadowimage{recast_minRegionSize_2.png}
		 */
		[JsonMember]
		public float minRegionSize = 3;

		/** Size in voxels of a single tile.
		 * This is the width of the tile.
		 *
		 * A large tile size can be faster to initially scan (but beware of out of memory issues if you try with a too large tile size in a large world)
		 * smaller tile sizes are (much) faster to update.
		 *
		 * Different tile sizes can affect the quality of paths. It is often good to split up huge open areas into several tiles for
		 * better quality paths, but too small tiles can lead to effects looking like invisible obstacles.
		 */
		[JsonMember]
		public int editorTileSize = 128;

		/** Size of a tile along the X axis in voxels.
		 * \warning Do not modify, it is set from #editorTileSize at Scan
		 */
		[JsonMember]
		public int tileSizeX = 128;

		/** Size of a tile along the Z axis in voxels.
		 * \warning Do not modify, it is set from #editorTileSize at Scan
		 */
		[JsonMember]
		public int tileSizeZ = 128;

		/** Perform nearest node searches in XZ space only.
		 */
		[JsonMember]
		public bool nearestSearchOnlyXZ;


		/** If true, divide the graph into tiles, otherwise use a single tile covering the whole graph */
		[JsonMember]
		public bool useTiles;

		/** If true, scanning the graph will yield a completely empty graph.
		 * Useful if you want to replace the graph with a custom navmesh for example
		 */
		public bool scanEmptyGraph;

		public enum RelevantGraphSurfaceMode {
			DoNotRequire,
			OnlyForCompletelyInsideTile,
			RequireForAll
		}

		/** Require every region to have a RelevantGraphSurface component inside it.
		 * A RelevantGraphSurface component placed in the scene specifies that
		 * the navmesh region it is inside should be included in the navmesh.
		 *
		 * If this is set to OnlyForCompletelyInsideTile
		 * a navmesh region is included in the navmesh if it
		 * has a RelevantGraphSurface inside it, or if it
		 * is adjacent to a tile border. This can leave some small regions
		 * which you didn't want to have included because they are adjacent
		 * to tile borders, but it removes the need to place a component
		 * in every single tile, which can be tedious (see below).
		 *
		 * If this is set to RequireForAll
		 * a navmesh region is included only if it has a RelevantGraphSurface
		 * inside it. Note that even though the navmesh
		 * looks continous between tiles, the tiles are computed individually
		 * and therefore you need a RelevantGraphSurface component for each
		 * region and for each tile.
		 *
		 *
		 *
		 * \shadowimage{relevantgraphsurface/dontreq.png}
		 * In the above image, the mode OnlyForCompletelyInsideTile was used. Tile borders
		 * are highlighted in black. Note that since all regions are adjacent to a tile border,
		 * this mode didn't remove anything in this case and would give the same result as DoNotRequire.
		 * The RelevantGraphSurface component is shown using the green gizmo in the top-right of the blue plane.
		 *
		 * \shadowimage{relevantgraphsurface/no_tiles.png}
		 * In the above image, the mode RequireForAll was used. No tiles were used.
		 * Note that the small region at the top of the orange cube is now gone, since it was not the in the same
		 * region as the relevant graph surface component.
		 * The result would have been identical with OnlyForCompletelyInsideTile since there are no tiles (or a single tile, depending on how you look at it).
		 *
		 * \shadowimage{relevantgraphsurface/req_all.png}
		 * The mode RequireForAll was used here. Since there is only a single RelevantGraphSurface component, only the region
		 * it was in, in the tile it is placed in, will be enabled. If there would have been several RelevantGraphSurface in other tiles,
		 * those regions could have been enabled as well.
		 *
		 * \shadowimage{relevantgraphsurface/tiled_uneven.png}
		 * Here another tile size was used along with the OnlyForCompletelyInsideTile.
		 * Note that the region on top of the orange cube is gone now since the region borders do not intersect that region (and there is no
		 * RelevantGraphSurface component inside it).
		 *
		 * \note When not using tiles. OnlyForCompletelyInsideTile is equivalent to RequireForAll.
		 */
		[JsonMember]
		public RelevantGraphSurfaceMode relevantGraphSurfaceMode = RelevantGraphSurfaceMode.DoNotRequire;

		[JsonMember]
		/** Use colliders to calculate the navmesh */
		public bool rasterizeColliders;

		[JsonMember]
		/** Use scene meshes to calculate the navmesh */
		public bool rasterizeMeshes = true;

		/** Include the Terrain in the scene. */
		[JsonMember]
		public bool rasterizeTerrain = true;

		/** Rasterize tree colliders on terrains.
		 *
		 * If the tree prefab has a collider, that collider will be rasterized.
		 * Otherwise a simple box collider will be used and the script will
		 * try to adjust it to the tree's scale, it might not do a very good job though so
		 * an attached collider is preferable.
		 *
		 * \see rasterizeTerrain
		 * \see colliderRasterizeDetail
		 */
		[JsonMember]
		public bool rasterizeTrees = true;

		/** Controls detail on rasterization of sphere and capsule colliders.
		 * This controls the number of rows and columns on the generated meshes.
		 * A higher value does not necessarily increase quality of the mesh, but a lower
		 * value will often speed it up.
		 *
		 * You should try to keep this value as low as possible without affecting the mesh quality since
		 * that will yield the fastest scan times.
		 *
		 * \see rasterizeColliders
		 */
		[JsonMember]
		public float colliderRasterizeDetail = 10;

		/** Center of the bounding box.
		 * Scanning will only be done inside the bounding box */
		[JsonMember]
		public Vector3 forcedBoundsCenter;

		/** Size of the bounding box. */
		[JsonMember]
		public Vector3 forcedBoundsSize = new Vector3(100, 40, 100);

		/** Layer mask which filters which objects to include.
		 * \see tagMask
		 */
		[JsonMember]
		public LayerMask mask = -1;

		/** Objects tagged with any of these tags will be rasterized.
		 * Note that this extends the layer mask, so if you only want to use tags, set #mask to 'Nothing'.
		 *
		 * \see mask
		 */
		[JsonMember]
		public List<string> tagMask = new List<string>();

		/** Show an outline of the polygons in the Unity Editor */
		[JsonMember]
		public bool showMeshOutline = true;

		/** Show the connections between the polygons in the Unity Editor */
		[JsonMember]
		public bool showNodeConnections;

		/** Show the surface of the navmesh */
		[JsonMember]
		public bool showMeshSurface;

		/** Controls how large the sample size for the terrain is.
		 * A higher value is faster to scan but less accurate
		 */
		[JsonMember]
		public int terrainSampleSize = 3;

		private Voxelize globalVox;

		/** World bounds for the graph.
		 * Defined as a bounds object with size #forcedBoundsSize and centered at #forcedBoundsCenter
		 */
		public Bounds forcedBounds {
			get {
				return new Bounds(forcedBoundsCenter, forcedBoundsSize);
			}
		}

		/** Number of tiles along the X-axis */
		public int tileXCount;
		/** Number of tiles along the Z-axis */
		public int tileZCount;

		/** All tiles.
		 * A tile can be got from a tile coordinate as tiles[x + z*tileXCount]
		 */
		NavmeshTile[] tiles;

		/** Currently updating tiles in a batch */
		bool batchTileUpdate;

		/** List of tiles updating during batch */
		List<int> batchUpdatedTiles = new List<int>();

#if ASTAR_RECAST_LARGER_TILES
		// Larger tiles
		public const int VertexIndexMask = 0xFFFFF;

		public const int TileIndexMask = 0x7FF;
		public const int TileIndexOffset = 20;
#else
		// Larger worlds
		public const int VertexIndexMask = 0xFFF;

		public const int TileIndexMask = 0x7FFFF;
		public const int TileIndexOffset = 12;
#endif

		public const int BorderVertexMask = 1;
		public const int BorderVertexOffset = 31;

		public class NavmeshTile : INavmeshHolder, INavmesh {
			/** Tile triangles */
			public int[] tris;

			/** Tile vertices */
			public Int3[] verts;

			/** Tile X Coordinate */
			public int x;

			/** Tile Z Coordinate */
			public int z;

			/** Width, in tile coordinates.
			 * Usually 1.
			 */
			public int w;

			/** Depth, in tile coordinates.
			 * Usually 1.
			 */
			public int d;

			/** All nodes in the tile */
			public TriangleMeshNode[] nodes;

			/** Bounding Box Tree for node lookups */
			public BBTree bbTree;

			/** Temporary flag used for batching */
			public bool flag;

			public void GetTileCoordinates (int tileIndex, out int x, out int z) {
				x = this.x;
				z = this.z;
			}

			public int GetVertexArrayIndex (int index) {
				return index & VertexIndexMask;
			}

			/** Get a specific vertex in the tile */
			public Int3 GetVertex (int index) {
				int idx = index & VertexIndexMask;

				return verts[idx];
			}

			public void GetNodes (GraphNodeDelegateCancelable del) {
				if (nodes == null) return;
				for (int i = 0; i < nodes.Length && del(nodes[i]); i++) {}
			}
		}

		/** Gets the vertex coordinate for the specified index.
		 *
		 * \throws IndexOutOfRangeException if the vertex index is invalid.
		 * \throws NullReferenceException if the tile the vertex is in is not calculated.
		 *
		 * \see NavmeshTile.GetVertex
		 */
		public Int3 GetVertex (int index) {
			int tileIndex = (index >> TileIndexOffset) & TileIndexMask;

			return tiles[tileIndex].GetVertex(index);
		}

		/** Returns a tile index from a vertex index */
		public int GetTileIndex (int index) {
			return (index >> TileIndexOffset) & TileIndexMask;
		}

		public int GetVertexArrayIndex (int index) {
			return index & VertexIndexMask;
		}

		/** Returns tile coordinates from a tile index */
		public void GetTileCoordinates (int tileIndex, out int x, out int z) {
			//z = System.Math.DivRem (tileIndex, tileXCount, out x);
			z = tileIndex/tileXCount;
			x = tileIndex - z*tileXCount;
		}

		/** Get all tiles.
		 * \warning Do not modify this array
		 */
		public NavmeshTile[] GetTiles () {
			return tiles;
		}

		/** Returns an XZ bounds object with the bounds of a group of tiles.
		 * The bounds object is defined in world units.
		 */
		public Bounds GetTileBounds (IntRect rect) {
			return GetTileBounds(rect.xmin, rect.ymin, rect.Width, rect.Height);
		}

		/** Returns an XZ bounds object with the bounds of a group of tiles.
		 * The bounds object is defined in world units.
		 */
		public Bounds GetTileBounds (int x, int z, int width = 1, int depth = 1) {
			var b = new Bounds();

			b.SetMinMax(
				new Vector3(x*tileSizeX*cellSize, 0, z*tileSizeZ*cellSize) + forcedBounds.min,
				new Vector3((x+width)*tileSizeX*cellSize, forcedBounds.size.y, (z+depth)*tileSizeZ*cellSize) + forcedBounds.min
				);
			return b;
		}

		/** Returns the tile coordinate which contains the point \a p.
		 * Is not necessarily a valid tile (i.e, it could be out of bounds).
		 */
		public Int2 GetTileCoordinates (Vector3 p) {
			p -= forcedBounds.min;
			p.x /= cellSize*tileSizeX;
			p.z /= cellSize*tileSizeZ;
			return new Int2((int)p.x, (int)p.z);
		}

		public override void OnDestroy () {
			base.OnDestroy();

			// Cleanup
			TriangleMeshNode.SetNavmeshHolder(active.astarData.GetGraphIndex(this), null);
		}

		/** Relocates the nodes in this graph.
		 * Assumes the nodes are already transformed using the "oldMatrix", then transforms them
		 * such that it will look like they have only been transformed using the "newMatrix".
		 *
		 * The matrix the graph is transformed with is typically stored in the #matrix field, so the typical usage for this method is
		 * \code
		 * var myNewMatrix = Matrix4x4.TRS (...);
		 * myGraph.RelocateNodes (myGraph.matrix, myNewMatrix);
		 * \endcode
		 *
		 * So for example if you want to move all your nodes in e.g a recast graph 10 units along the X axis from the initial position
		 * \code
		 * var graph = AstarPath.astarData.recastGraph;
		 * var m = Matrix4x4.TRS (new Vector3(10,0,0), Quaternion.identity, Vector3.one);
		 * graph.RelocateNodes (graph.matrix, m);
		 * \endcode
		 *
		 * \warning Cannot be used on tiled recast graphs.
		 *
		 * \warning This method is lossy, so calling it many times may cause node positions to lose precision.
		 * For example if you set the scale to 0 in one call, and then to 1 in the next call, it will not be able to
		 * recover the correct positions since when the scale was 0, all nodes were scaled/moved to the same point.
		 * The same thing happens for other - less extreme - values as well, but to a lesser degree.
		 *
		 * \version Prior to version 3.6.1 the oldMatrix and newMatrix parameters were reversed by mistake.
		 */
		public override void RelocateNodes (Matrix4x4 oldMatrix, Matrix4x4 newMatrix) {
			// Move all the vertices in each tile
			if (tiles != null) {
				Matrix4x4 inv = oldMatrix.inverse;
				Matrix4x4 m = newMatrix * inv;

				if (tiles.Length > 1) {
					throw new System.Exception("RelocateNodes cannot be used on tiled recast graphs");
				}

				for (int tileIndex = 0; tileIndex < tiles.Length; tileIndex++) {
					var tile = tiles[tileIndex];
					if (tile != null) {
						var tileVerts = tile.verts;
						for (int vertexIndex = 0; vertexIndex < tileVerts.Length; vertexIndex++) {
							tileVerts[vertexIndex] = ((Int3)m.MultiplyPoint((Vector3)tileVerts[vertexIndex]));
						}

						for (int nodeIndex = 0; nodeIndex < tile.nodes.Length; nodeIndex++) {
							var node = tile.nodes[nodeIndex];
							node.UpdatePositionFromVertices();
						}
						tile.bbTree.RebuildFrom(tile.nodes);
					}
				}
			}

			SetMatrix(newMatrix);
		}

		/** Creates a single new empty tile */
		static NavmeshTile NewEmptyTile (int x, int z) {
			var tile = new NavmeshTile();

			tile.x = x;
			tile.z = z;
			tile.w = 1;
			tile.d = 1;
			tile.verts = new Int3[0];
			tile.tris = new int[0];
			tile.nodes = new TriangleMeshNode[0];
			tile.bbTree = new BBTree();
			return tile;
		}

		public override void GetNodes (GraphNodeDelegateCancelable del) {
			/*if (nodes == null) return;
			 * for (int i=0;i<nodes.Length && del (nodes[i]);i++) {}*/
			if (tiles == null) return;
			//
			for (int i = 0; i < tiles.Length; i++) {
				if (tiles[i] == null || tiles[i].x+tiles[i].z*tileXCount != i) continue;
				TriangleMeshNode[] nodes = tiles[i].nodes;

				if (nodes == null) continue;

				for (int j = 0; j < nodes.Length && del(nodes[j]); j++) {}
			}
		}

		/** Returns the closest point of the node */
		public Vector3 ClosestPointOnNode (TriangleMeshNode node, Vector3 pos) {
			return Polygon.ClosestPointOnTriangle((Vector3)GetVertex(node.v0), (Vector3)GetVertex(node.v1), (Vector3)GetVertex(node.v2), pos);
		}

		/** Returns if the point is inside the node in XZ space */
		public bool ContainsPoint (TriangleMeshNode node, Vector3 pos) {
			if (VectorMath.IsClockwiseXZ((Vector3)GetVertex(node.v0), (Vector3)GetVertex(node.v1), pos)
				&& VectorMath.IsClockwiseXZ((Vector3)GetVertex(node.v1), (Vector3)GetVertex(node.v2), pos)
				&& VectorMath.IsClockwiseXZ((Vector3)GetVertex(node.v2), (Vector3)GetVertex(node.v0), pos)) {
				return true;
			}
			return false;
		}

		public void SnapForceBoundsToScene () {
			List<ExtraMesh> meshes;

			CollectMeshes(out meshes, new Bounds(Vector3.zero, new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)));

			if (meshes.Count == 0) {
				return;
			}

			var bounds = meshes[0].bounds;

			for (int i = 1; i < meshes.Count; i++) {
				bounds.Encapsulate(meshes[i].bounds);
			}

			forcedBoundsCenter = bounds.center;
			forcedBoundsSize = bounds.size;
		}

		/** Find all relevant RecastMeshObj components and create ExtraMeshes for them */
		public void GetRecastMeshObjs (Bounds bounds, List<ExtraMesh> buffer) {
			List<RecastMeshObj> buffer2 = Util.ListPool<RecastMeshObj>.Claim();

			// Get all recast mesh objects inside the bounds
			RecastMeshObj.GetAllInBounds(buffer2, bounds);

			var cachedVertices = new Dictionary<Mesh, Vector3[]>();
			var cachedTris = new Dictionary<Mesh, int[]>();

			// Create an ExtraMesh object
			// for each RecastMeshObj
			for (int i = 0; i < buffer2.Count; i++) {
				MeshFilter filter = buffer2[i].GetMeshFilter();
				Renderer rend = filter != null ? filter.GetComponent<Renderer>() : null;

				if (filter != null && rend != null) {
					Mesh mesh = filter.sharedMesh;

					var smesh = new ExtraMesh();
					smesh.matrix = rend.localToWorldMatrix;
					smesh.original = filter;
					smesh.area = buffer2[i].area;

					// Don't read the vertices and triangles from the
					// mesh if we have seen the same mesh previously
					if (cachedVertices.ContainsKey(mesh)) {
						smesh.vertices = cachedVertices[mesh];
						smesh.triangles = cachedTris[mesh];
					} else {
						smesh.vertices = mesh.vertices;
						smesh.triangles = mesh.triangles;
						cachedVertices[mesh] = smesh.vertices;
						cachedTris[mesh] = smesh.triangles;
					}

					smesh.bounds = rend.bounds;

					buffer.Add(smesh);
				} else {
					Collider coll = buffer2[i].GetCollider();

					if (coll == null) {
						Debug.LogError("RecastMeshObject ("+buffer2[i].gameObject.name +") didn't have a collider or MeshFilter+Renderer attached");
						continue;
					}

					ExtraMesh smesh = RasterizeCollider(coll);
					smesh.area = buffer2[i].area;

					//Make sure a valid ExtraMesh was returned
					if (smesh.vertices != null) buffer.Add(smesh);
				}
			}

			//Clear cache to avoid memory leak
			capsuleCache.Clear();

			Util.ListPool<RecastMeshObj>.Release(buffer2);
		}

		static void GetSceneMeshes (Bounds bounds, List<string> tagMask, LayerMask layerMask, List<ExtraMesh> meshes) {
			if ((tagMask != null && tagMask.Count > 0) || layerMask != 0) {
				var filters = GameObject.FindObjectsOfType(typeof(MeshFilter)) as MeshFilter[];

				var filteredFilters = new List<MeshFilter>(filters.Length/3);

				for (int i = 0; i < filters.Length; i++) {
					MeshFilter filter = filters[i];
					Renderer rend = filter.GetComponent<Renderer>();

					if (rend != null && filter.sharedMesh != null && rend.enabled && (((1 << filter.gameObject.layer) & layerMask) != 0 || tagMask.Contains(filter.tag))) {
						if (filter.GetComponent<RecastMeshObj>() == null) {
							filteredFilters.Add(filter);
						}
					}
				}

				var cachedVertices = new Dictionary<Mesh, Vector3[]>();
				var cachedTris = new Dictionary<Mesh, int[]>();

				bool containedStatic = false;

				for (int i = 0; i < filteredFilters.Count; i++) {
					MeshFilter filter = filteredFilters[i];

					// Note, guaranteed to have a renderer
					Renderer rend = filter.GetComponent<Renderer>();

					//Workaround for statically batched meshes
					if (rend.isPartOfStaticBatch) {
						containedStatic = true;
					} else {
						//Only include it if it intersects with the graph
						if (rend.bounds.Intersects(bounds)) {
							Mesh mesh = filter.sharedMesh;
							var smesh = new ExtraMesh();
							smesh.matrix = rend.localToWorldMatrix;
							smesh.original = filter;
							if (cachedVertices.ContainsKey(mesh)) {
								smesh.vertices = cachedVertices[mesh];
								smesh.triangles = cachedTris[mesh];
							} else {
								smesh.vertices = mesh.vertices;
								smesh.triangles = mesh.triangles;
								cachedVertices[mesh] = smesh.vertices;
								cachedTris[mesh] = smesh.triangles;
							}

							smesh.bounds = rend.bounds;

							meshes.Add(smesh);
						}
					}

					if (containedStatic)
						Debug.LogWarning("Some meshes were statically batched. These meshes can not be used for navmesh calculation" +
							" due to technical constraints.\nDuring runtime scripts cannot access the data of meshes which have been statically batched.\n" +
							"One way to solve this problem is to use cached startup (Save & Load tab in the inspector) to only calculate the graph when the game is not playing.");
				}

	#if ASTARDEBUG
				int y = 0;
				foreach (ExtraMesh smesh in meshes) {
					y++;
					Vector3[] vecs = smesh.vertices;
					int[] tris = smesh.triangles;

					for (int i = 0; i < tris.Length; i += 3) {
						Vector3 p1 = smesh.matrix.MultiplyPoint3x4(vecs[tris[i+0]]);
						Vector3 p2 = smesh.matrix.MultiplyPoint3x4(vecs[tris[i+1]]);
						Vector3 p3 = smesh.matrix.MultiplyPoint3x4(vecs[tris[i+2]]);

						Debug.DrawLine(p1, p2, Color.red, 1);
						Debug.DrawLine(p2, p3, Color.red, 1);
						Debug.DrawLine(p3, p1, Color.red, 1);
					}
				}
	#endif
			}
		}

		/** Returns a rect containing the indices of all tiles touching the specified bounds */
		public IntRect GetTouchingTiles (Bounds b) {
			b.center -= forcedBounds.min;

			//Calculate world bounds of all affected tiles
			var r = new IntRect(Mathf.FloorToInt(b.min.x / (tileSizeX*cellSize)), Mathf.FloorToInt(b.min.z / (tileSizeZ*cellSize)), Mathf.FloorToInt(b.max.x / (tileSizeX*cellSize)), Mathf.FloorToInt(b.max.z / (tileSizeZ*cellSize)));
			//Clamp to bounds
			r = IntRect.Intersection(r, new IntRect(0, 0, tileXCount-1, tileZCount-1));
			return r;
		}

		/** Returns a rect containing the indices of all tiles by rounding the specified bounds to tile borders */
		public IntRect GetTouchingTilesRound (Bounds b) {
			b.center -= forcedBounds.min;

			//Calculate world bounds of all affected tiles
			var r = new IntRect(Mathf.RoundToInt(b.min.x / (tileSizeX*cellSize)), Mathf.RoundToInt(b.min.z / (tileSizeZ*cellSize)), Mathf.RoundToInt(b.max.x / (tileSizeX*cellSize))-1, Mathf.RoundToInt(b.max.z / (tileSizeZ*cellSize))-1);
			//Clamp to bounds
			r = IntRect.Intersection(r, new IntRect(0, 0, tileXCount-1, tileZCount-1));
			return r;
		}

		public GraphUpdateThreading CanUpdateAsync (GraphUpdateObject o) {
			return o.updatePhysics ? GraphUpdateThreading.SeparateAndUnityInit : GraphUpdateThreading.SeparateThread;
		}

		public void UpdateAreaInit (GraphUpdateObject o) {
			if (!o.updatePhysics) {
				return;
			}

			if (!dynamic) {
				throw new System.Exception("Recast graph must be marked as dynamic to enable graph updates");
			}

			AstarProfiler.Reset();
			AstarProfiler.StartProfile("UpdateAreaInit");
			AstarProfiler.StartProfile("CollectMeshes");

			RelevantGraphSurface.UpdateAllPositions();

			//Calculate world bounds of all affected tiles
			IntRect touchingTiles = GetTouchingTiles(o.bounds);
			Bounds tileBounds = GetTileBounds(touchingTiles);

			int voxelCharacterRadius = Mathf.CeilToInt(characterRadius/cellSize);
			int borderSize = voxelCharacterRadius + 3;

			//Expand borderSize voxels on each side
			tileBounds.Expand(new Vector3(borderSize, 0, borderSize)*cellSize*2);

			List<ExtraMesh> extraMeshes;

			CollectMeshes(out extraMeshes, tileBounds);

			Voxelize vox = globalVox;

			if (vox == null) {
				//Create the voxelizer and set all settings
				vox = new Voxelize(cellHeight, cellSize, walkableClimb, walkableHeight, maxSlope);

				vox.maxEdgeLength = maxEdgeLength;

				if (dynamic) globalVox = vox;
			}

			vox.inputExtraMeshes = extraMeshes;

			AstarProfiler.EndProfile("CollectMeshes");
			AstarProfiler.EndProfile("UpdateAreaInit");
		}

		public void UpdateArea (GraphUpdateObject guo) {
			// Figure out which tiles are affected
			var r = GetTouchingTiles(guo.bounds);

			if (!guo.updatePhysics) {
				for (int z = r.ymin; z <= r.ymax; z++) {
					for (int x = r.xmin; x <= r.xmax; x++) {
						NavmeshTile tile = tiles[z*tileXCount + x];
						NavMeshGraph.UpdateArea(guo, tile);
					}
				}

				return;
			}

			if (!dynamic) {
				throw new System.Exception("Recast graph must be marked as dynamic to enable graph updates with updatePhysics = true");
			}

			Voxelize vox = globalVox;

			if (vox == null) {
				throw new System.InvalidOperationException("No Voxelizer object. UpdateAreaInit should have been called before this function.");
			}



			AstarProfiler.StartProfile("Init");
			AstarProfiler.StartProfile("RemoveConnections");



			for (int x = r.xmin; x <= r.xmax; x++) {
				for (int z = r.ymin; z <= r.ymax; z++) {
					RemoveConnectionsFromTile(tiles[x + z*tileXCount]);
				}
			}



			AstarProfiler.EndProfile("RemoveConnections");

			AstarProfiler.StartProfile("Build Tiles");

			for (int x = r.xmin; x <= r.xmax; x++) {
				for (int z = r.ymin; z <= r.ymax; z++) {
					BuildTileMesh(vox, x, z);
				}
			}



			AstarProfiler.EndProfile("Build Tiles");


			AstarProfiler.StartProfile("ConnectTiles");
			uint graphIndex = (uint)AstarPath.active.astarData.GetGraphIndex(this);

			for (int x = r.xmin; x <= r.xmax; x++) {
				for (int z = r.ymin; z <= r.ymax; z++) {
					NavmeshTile tile = tiles[x + z*tileXCount];
					GraphNode[] nodes = tile.nodes;

					for (int i = 0; i < nodes.Length; i++) nodes[i].GraphIndex = graphIndex;
				}
			}



			//Connect the newly create tiles with the old tiles and with each other
			r = r.Expand(1);
			//Clamp to bounds
			r = IntRect.Intersection(r, new IntRect(0, 0, tileXCount-1, tileZCount-1));

			for (int x = r.xmin; x <= r.xmax; x++) {
				for (int z = r.ymin; z <= r.ymax; z++) {
					if (x < tileXCount-1 && r.Contains(x+1, z)) {
						ConnectTiles(tiles[x + z*tileXCount], tiles[x+1 + z*tileXCount]);
					}
					if (z < tileZCount-1 && r.Contains(x, z+1)) {
						ConnectTiles(tiles[x + z*tileXCount], tiles[x + (z+1)*tileXCount]);
					}
				}
			}

			AstarProfiler.EndProfile("ConnectTiles");
			AstarProfiler.PrintResults();
		}

		public void ConnectTileWithNeighbours (NavmeshTile tile) {
			if (tile.x > 0) {
				int x = tile.x-1;
				for (int z = tile.z; z < tile.z+tile.d; z++) ConnectTiles(tiles[x + z*tileXCount], tile);
			}
			if (tile.x+tile.w < tileXCount) {
				int x = tile.x+tile.w;
				for (int z = tile.z; z < tile.z+tile.d; z++) ConnectTiles(tiles[x + z*tileXCount], tile);
			}
			if (tile.z > 0) {
				int z = tile.z-1;
				for (int x = tile.x; x < tile.x+tile.w; x++) ConnectTiles(tiles[x + z*tileXCount], tile);
			}
			if (tile.z+tile.d < tileZCount) {
				int z = tile.z+tile.d;
				for (int x = tile.x; x < tile.x+tile.w; x++) ConnectTiles(tiles[x + z*tileXCount], tile);
			}
		}

		public void RemoveConnectionsFromTile (NavmeshTile tile) {
			if (tile.x > 0) {
				int x = tile.x-1;
				for (int z = tile.z; z < tile.z+tile.d; z++) RemoveConnectionsFromTo(tiles[x + z*tileXCount], tile);
			}
			if (tile.x+tile.w < tileXCount) {
				int x = tile.x+tile.w;
				for (int z = tile.z; z < tile.z+tile.d; z++) RemoveConnectionsFromTo(tiles[x + z*tileXCount], tile);
			}
			if (tile.z > 0) {
				int z = tile.z-1;
				for (int x = tile.x; x < tile.x+tile.w; x++) RemoveConnectionsFromTo(tiles[x + z*tileXCount], tile);
			}
			if (tile.z+tile.d < tileZCount) {
				int z = tile.z+tile.d;
				for (int x = tile.x; x < tile.x+tile.w; x++) RemoveConnectionsFromTo(tiles[x + z*tileXCount], tile);
			}
		}

		public void RemoveConnectionsFromTo (NavmeshTile a, NavmeshTile b) {
			if (a == null || b == null) return;
			//Same tile, possibly from a large tile (one spanning several x,z tile coordinates)
			if (a == b) return;

			int tileIdx = b.x + b.z*tileXCount;

			for (int i = 0; i < a.nodes.Length; i++) {
				TriangleMeshNode node = a.nodes[i];
				if (node.connections == null) continue;
				for (int j = 0;; j++) {
					//Length will not be constant if connections are removed
					if (j >= node.connections.Length) break;

					var other = node.connections[j] as TriangleMeshNode;

					//Only evaluate TriangleMeshNodes
					if (other == null) continue;

					int tileIdx2 = other.GetVertexIndex(0);
					tileIdx2 = (tileIdx2 >> TileIndexOffset) & TileIndexMask;

					if (tileIdx2 == tileIdx) {
						node.RemoveConnection(node.connections[j]);
						j--;
					}
				}
			}
		}

		public override NNInfo GetNearest (Vector3 position, NNConstraint constraint, GraphNode hint) {
			return GetNearestForce(position, null);
		}

		public override NNInfo GetNearestForce (Vector3 position, NNConstraint constraint) {
			if (tiles == null) return new NNInfo();

			Vector3 localPosition = position - forcedBounds.min;
			int tx = Mathf.FloorToInt(localPosition.x / (cellSize*tileSizeX));
			int tz = Mathf.FloorToInt(localPosition.z / (cellSize*tileSizeZ));

			// Clamp to graph borders
			tx = Mathf.Clamp(tx, 0, tileXCount-1);
			tz = Mathf.Clamp(tz, 0, tileZCount-1);

			int wmax = Math.Max(tileXCount, tileZCount);

			var best = new NNInfo();
			float bestDistance = float.PositiveInfinity;

			bool xzSearch = nearestSearchOnlyXZ || (constraint != null && constraint.distanceXZ);

			// Search outwards in a diamond pattern from the closest tile
			for (int w = 0; w < wmax; w++) {
				if (!xzSearch && bestDistance < (w-1)*cellSize*Math.Max(tileSizeX, tileSizeZ)) break;

				int zmax = Math.Min(w+tz +1, tileZCount);
				for (int z = Math.Max(-w+tz, 0); z < zmax; z++) {
					// Solve for z such that abs(x-tx) + abs(z-tx) == w
					// Delta X coordinate
					int dx = Math.Abs(w - Math.Abs(z-tz));
					// Solution is dx + tx and -dx + tx

					// First solution negative delta x
					if (-dx + tx >= 0) {
						// Absolute x coordinate
						int x = -dx + tx;
						NavmeshTile tile = tiles[x + z*tileXCount];

						if (tile != null) {
							if (xzSearch) {
								best = tile.bbTree.QueryClosestXZ(position, constraint, ref bestDistance, best);
								if (bestDistance < float.PositiveInfinity) break;
							} else {
								best = tile.bbTree.QueryClosest(position, constraint, ref bestDistance, best);
							}
						}
					}

					// Other solution, make sure it is not the same solution by checking x != 0
					if (dx != 0 && dx + tx < tileXCount) {
						// Absolute x coordinate
						int x = dx + tx;
						NavmeshTile tile = tiles[x + z*tileXCount];
						if (tile != null) {
							if (xzSearch) {
								best = tile.bbTree.QueryClosestXZ(position, constraint, ref bestDistance, best);
								if (bestDistance < float.PositiveInfinity) break;
							} else {
								best = tile.bbTree.QueryClosest(position, constraint, ref bestDistance, best);
							}
						}
					}
				}
			}

			best.node = best.constrainedNode;
			best.constrainedNode = null;
			best.clampedPosition = best.constClampedPosition;

			return best;
		}

		/** Finds the first node which contains \a position.
		 * "Contains" is defined as \a position is inside the triangle node when seen from above. So only XZ space matters.
		 * In case of a multilayered environment, which node of the possibly several nodes
		 * containing the point is undefined.
		 *
		 * Returns null if there was no node containing the point. This serves as a quick
		 * check for "is this point on the navmesh or not".
		 *
		 * Note that the behaviour of this method is distinct from the GetNearest method.
		 * The GetNearest method will return the closest node to a point,
		 * which is not necessarily the one which contains it in XZ space.
		 *
		 * \see GetNearest
		 */
		public GraphNode PointOnNavmesh (Vector3 position, NNConstraint constraint) {
			if (tiles == null) return null;

			Vector3 localPosition = position - forcedBounds.min;
			int tx = Mathf.FloorToInt(localPosition.x / (cellSize*tileSizeX));
			int tz = Mathf.FloorToInt(localPosition.z / (cellSize*tileSizeZ));

			// Graph borders
			if (tx < 0 || tz < 0 || tx >= tileXCount || tz >= tileZCount) return null;

			NavmeshTile tile = tiles[tx + tz*tileXCount];

			if (tile != null) {
				GraphNode node = tile.bbTree.QueryInside(position, constraint);
				return node;
			}

			return null;
		}

		/** Represents a unity mesh to be used in the recast graph rasterization.
		 *
		 * \see ExtraMesh
		 */
		public struct SceneMesh {
			public Mesh mesh;
			public Matrix4x4 matrix;
			public Bounds bounds;
		}

		public override void ScanInternal (OnScanStatus statusCallback) {
			AstarProfiler.Reset();
			AstarProfiler.StartProfile("Base Scan");
			//AstarProfiler.InitializeFastProfile (new string[] {"Rasterize", "Rasterize Inner 1", "Rasterize Inner 2", "Rasterize Inner 3"});

			TriangleMeshNode.SetNavmeshHolder(AstarPath.active.astarData.GetGraphIndex(this), this);


			ScanTiledNavmesh(statusCallback);


#if DEBUG_REPLAY
			DebugReplay.WriteToFile();
#endif
			AstarProfiler.PrintFastResults();
		}

		protected void ScanTiledNavmesh (OnScanStatus statusCallback) {
			ScanAllTiles(statusCallback);
		}

		protected void ScanAllTiles (OnScanStatus statusCallback) {
#if ASTARDEBUG
			System.Console.WriteLine("Recast Graph -- Collecting Meshes");
#endif

			//----

			//Voxel grid size
			int gw = (int)(forcedBounds.size.x/cellSize + 0.5f);
			int gd = (int)(forcedBounds.size.z/cellSize + 0.5f);

			if (!useTiles) {
				tileSizeX = gw;
				tileSizeZ = gd;
			} else {
				tileSizeX = editorTileSize;
				tileSizeZ = editorTileSize;
			}

			//Number of tiles
			int tw = (gw + tileSizeX-1) / tileSizeX;
			int td = (gd + tileSizeZ-1) / tileSizeZ;

			tileXCount = tw;
			tileZCount = td;

			if (tileXCount * tileZCount > TileIndexMask+1) {
				throw new System.Exception("Too many tiles ("+(tileXCount * tileZCount)+") maximum is "+(TileIndexMask+1)+
					"\nTry disabling ASTAR_RECAST_LARGER_TILES under the 'Optimizations' tab in the A* inspector.");
			}

			tiles = new NavmeshTile[tileXCount*tileZCount];

#if ASTARDEBUG
			System.Console.WriteLine("Recast Graph -- Creating Voxel Base");
#endif

			// If this is true, just fill the graph with empty tiles
			if (scanEmptyGraph) {
				for (int z = 0; z < td; z++) {
					for (int x = 0; x < tw; x++) {
						tiles[z*tileXCount + x] = NewEmptyTile(x, z);
					}
				}
				return;
			}

			AstarProfiler.StartProfile("Finding Meshes");
			List<ExtraMesh> extraMeshes;

#if !NETFX_CORE || UNITY_EDITOR
			System.Console.WriteLine("Collecting Meshes");
#endif
			CollectMeshes(out extraMeshes, forcedBounds);

			AstarProfiler.EndProfile("Finding Meshes");

			// A walkableClimb higher than walkableHeight can cause issues when generating the navmesh since then it can in some cases
			// Both be valid for a character to walk under an obstacle and climb up on top of it (and that cannot be handled with navmesh without links)
			// The editor scripts also enforce this but we enforce it here too just to be sure
			walkableClimb = Mathf.Min(walkableClimb, walkableHeight);

			//Create the voxelizer and set all settings
			var vox = new Voxelize(cellHeight, cellSize, walkableClimb, walkableHeight, maxSlope);
			vox.inputExtraMeshes = extraMeshes;

			vox.maxEdgeLength = maxEdgeLength;

			int lastInfoCallback = -1;
			var watch = System.Diagnostics.Stopwatch.StartNew();

			//Generate all tiles
			for (int z = 0; z < td; z++) {
				for (int x = 0; x < tw; x++) {
					int tileNum = z*tileXCount + x;
#if !NETFX_CORE || UNITY_EDITOR
					System.Console.WriteLine("Generating Tile #"+(tileNum) + " of " + td*tw);
#endif

					//Call statusCallback only 10 times since it is very slow in the editor
					if (statusCallback != null && (tileNum*10/tiles.Length > lastInfoCallback || watch.ElapsedMilliseconds > 2000)) {
						lastInfoCallback = tileNum*10/tiles.Length;
						watch.Reset();
						watch.Start();

						statusCallback(new Progress(Mathf.Lerp(0.1f, 0.9f, tileNum/(float)tiles.Length), "Building Tile " + tileNum + "/" + tiles.Length));
					}

					BuildTileMesh(vox, x, z);
				}
			}

#if !NETFX_CORE
			System.Console.WriteLine("Assigning Graph Indices");
#endif

			if (statusCallback != null) statusCallback(new Progress(0.9f, "Connecting tiles"));

			//Assign graph index to nodes
			uint graphIndex = (uint)AstarPath.active.astarData.GetGraphIndex(this);

			GraphNodeDelegateCancelable del = delegate(GraphNode n) {
				n.GraphIndex = graphIndex;
				return true;
			};
			GetNodes(del);

			for (int z = 0; z < td; z++) {
				for (int x = 0; x < tw; x++) {
#if !NETFX_CORE
					System.Console.WriteLine("Connecing Tile #"+(z*tileXCount + x) + " of " + td*tw);
#endif
					if (x < tw-1) ConnectTiles(tiles[x + z*tileXCount], tiles[x+1 + z*tileXCount]);
					if (z < td-1) ConnectTiles(tiles[x + z*tileXCount], tiles[x + (z+1)*tileXCount]);
				}
			}

			AstarProfiler.PrintResults();
		}

		protected void BuildTileMesh (Voxelize vox, int x, int z) {
			AstarProfiler.StartProfile("Build Tile");

			AstarProfiler.StartProfile("Init");

			//World size of tile
			float tcsx = tileSizeX*cellSize;
			float tcsz = tileSizeZ*cellSize;

			int voxelCharacterRadius = Mathf.CeilToInt(characterRadius/cellSize);

			Vector3 forcedBoundsMin = forcedBounds.min;
			Vector3 forcedBoundsMax = forcedBounds.max;

			var bounds = new Bounds();
			bounds.SetMinMax(new Vector3(x*tcsx, 0, z*tcsz) + forcedBoundsMin,
				new Vector3((x+1)*tcsx + forcedBoundsMin.x, forcedBoundsMax.y, (z+1)*tcsz + forcedBoundsMin.z)
				);
			vox.borderSize = voxelCharacterRadius + 3;

			//Expand borderSize voxels on each side
			bounds.Expand(new Vector3(vox.borderSize, 0, vox.borderSize)*cellSize*2);

			vox.forcedBounds = bounds;
			vox.width = tileSizeX + vox.borderSize*2;
			vox.depth = tileSizeZ + vox.borderSize*2;

			if (!useTiles && relevantGraphSurfaceMode == RelevantGraphSurfaceMode.OnlyForCompletelyInsideTile) {
				// This best reflects what the user would actually want
				vox.relevantGraphSurfaceMode = RelevantGraphSurfaceMode.RequireForAll;
			} else {
				vox.relevantGraphSurfaceMode = relevantGraphSurfaceMode;
			}

			vox.minRegionSize = Mathf.RoundToInt(minRegionSize / (cellSize*cellSize));

 #if ASTARDEBUG
			Debug.Log("Building Tile " + x+","+z);
			System.Console.WriteLine("Recast Graph -- Voxelizing");
#endif
			AstarProfiler.EndProfile("Init");


			//Init voxelizer
			vox.Init();

			vox.CollectMeshes();

			vox.VoxelizeInput();

			AstarProfiler.StartProfile("Filter Ledges");


			vox.FilterLedges(vox.voxelWalkableHeight, vox.voxelWalkableClimb, vox.cellSize, vox.cellHeight, vox.forcedBounds.min);

			AstarProfiler.EndProfile("Filter Ledges");

			AstarProfiler.StartProfile("Filter Low Height Spans");
			vox.FilterLowHeightSpans(vox.voxelWalkableHeight, vox.cellSize, vox.cellHeight, vox.forcedBounds.min);
			AstarProfiler.EndProfile("Filter Low Height Spans");

			vox.BuildCompactField();

			vox.BuildVoxelConnections();

#if ASTARDEBUG
			System.Console.WriteLine("Recast Graph -- Eroding");
#endif

			vox.ErodeWalkableArea(voxelCharacterRadius);

#if ASTARDEBUG
			System.Console.WriteLine("Recast Graph -- Building Distance Field");
#endif

			vox.BuildDistanceField();

#if ASTARDEBUG
			System.Console.WriteLine("Recast Graph -- Building Regions");
#endif

			vox.BuildRegions();

#if ASTARDEBUG
			System.Console.WriteLine("Recast Graph -- Building Contours");
#endif

			var cset = new VoxelContourSet();

			vox.BuildContours(contourMaxError, 1, cset, Voxelize.RC_CONTOUR_TESS_WALL_EDGES);

#if ASTARDEBUG
			System.Console.WriteLine("Recast Graph -- Building Poly Mesh");
#endif

			VoxelMesh mesh;

			vox.BuildPolyMesh(cset, 3, out mesh);

#if ASTARDEBUG
			System.Console.WriteLine("Recast Graph -- Building Nodes");
#endif

			//Vector3[] vertices = new Vector3[mesh.verts.Length];

			AstarProfiler.StartProfile("Build Nodes");

			// Debug code
			//matrix = Matrix4x4.TRS (vox.voxelOffset,Quaternion.identity,Int3.Precision*vox.cellScale);

			//Position the vertices correctly in the world
			for (int i = 0; i < mesh.verts.Length; i++) {
				//Note the multiplication is Scalar multiplication of vectors
				mesh.verts[i] = ((mesh.verts[i]*Int3.Precision) * vox.cellScale) + (Int3)vox.voxelOffset;

				// Debug code
				//Debug.DrawRay (matrix.MultiplyPoint3x4(vertices[i]),Vector3.up,Color.red);
			}


#if ASTARDEBUG
			System.Console.WriteLine("Recast Graph -- Generating Nodes");
#endif

			NavmeshTile tile = CreateTile(vox, mesh, x, z);
			tiles[tile.x + tile.z*tileXCount] = tile;

			AstarProfiler.EndProfile("Build Nodes");

#if ASTARDEBUG
			System.Console.WriteLine("Recast Graph -- Done");
#endif

			AstarProfiler.EndProfile("Build Tile");
		}

		private Dictionary<Int2, int> cachedInt2_int_dict = new Dictionary<Int2, int>();
		private Dictionary<Int3, int> cachedInt3_int_dict = new Dictionary<Int3, int>();

		/** Create a tile at tile index \a x , \a z from the mesh.
		 * \warning This implementation is not thread safe. It uses cached variables to improve performance
		 */
		NavmeshTile CreateTile (Voxelize vox, VoxelMesh mesh, int x, int z) {
			if (mesh.tris == null) throw new System.ArgumentNullException("mesh.tris");
			if (mesh.verts == null) throw new System.ArgumentNullException("mesh.verts");

			//Create a new navmesh tile and assign its settings
			var tile = new NavmeshTile();

			tile.x = x;
			tile.z = z;
			tile.w = 1;
			tile.d = 1;
			tile.tris = mesh.tris;
			tile.verts = mesh.verts;
			tile.bbTree = new BBTree();

			if (tile.tris.Length % 3 != 0) throw new System.ArgumentException("Indices array's length must be a multiple of 3 (mesh.tris)");

			if (tile.verts.Length >= VertexIndexMask) {
				if (tileXCount*tileZCount == 1) {
					throw new System.ArgumentException("Too many vertices per tile (more than " + VertexIndexMask + ")." +
						"\n<b>Try enabling tiling in the recast graph settings.</b>\n");
				} else {
					throw new System.ArgumentException("Too many vertices per tile (more than " + VertexIndexMask + ")." +
						"\n<b>Try reducing tile size or enabling ASTAR_RECAST_LARGER_TILES under the 'Optimizations' tab in the A* Inspector</b>");
				}
			}

			//Dictionary<Int3, int> firstVerts = new Dictionary<Int3, int> ();
			Dictionary<Int3, int> firstVerts = cachedInt3_int_dict;
			firstVerts.Clear();

			var compressedPointers = new int[tile.verts.Length];

			int count = 0;
			for (int i = 0; i < tile.verts.Length; i++) {
				if (!firstVerts.ContainsKey(tile.verts[i])) {
					firstVerts.Add(tile.verts[i], count);
					compressedPointers[i] = count;
					tile.verts[count] = tile.verts[i];
					count++;
				} else {
					// There are some cases, rare but still there, that vertices are identical
					compressedPointers[i] = firstVerts[tile.verts[i]];
				}
			}

			for (int i = 0; i < tile.tris.Length; i++) {
				tile.tris[i] = compressedPointers[tile.tris[i]];
			}

			var compressed = new Int3[count];
			for (int i = 0; i < count; i++) compressed[i] = tile.verts[i];

			tile.verts = compressed;

			var nodes = new TriangleMeshNode[tile.tris.Length/3];
			tile.nodes = nodes;

			//Here we are faking a new graph
			//The tile is not added to any graphs yet, but to get the position querys from the nodes
			//to work correctly (not throw exceptions because the tile is not calculated) we fake a new graph
			//and direct the position queries directly to the tile
			int graphIndex = AstarPath.active.astarData.graphs.Length;

			TriangleMeshNode.SetNavmeshHolder(graphIndex, tile);

			//This index will be ORed to the triangle indices
			int tileIndex = x + z*tileXCount;
			tileIndex <<= TileIndexOffset;

			//Create nodes and assign triangle indices
			for (int i = 0; i < nodes.Length; i++) {
				var node = new TriangleMeshNode(active);
				nodes[i] = node;
				node.GraphIndex = (uint)graphIndex;
				node.v0 = tile.tris[i*3+0] | tileIndex;
				node.v1 = tile.tris[i*3+1] | tileIndex;
				node.v2 = tile.tris[i*3+2] | tileIndex;

				//Degenerate triangles might occur, but they will not cause any large troubles anymore
				//if (Polygon.IsColinear (node.GetVertex(0), node.GetVertex(1), node.GetVertex(2))) {
				//	Debug.Log ("COLINEAR!!!!!!");
				//}

				//Make sure the triangle is clockwise
				if (!VectorMath.IsClockwiseXZ(node.GetVertex(0), node.GetVertex(1), node.GetVertex(2))) {
					int tmp = node.v0;
					node.v0 = node.v2;
					node.v2 = tmp;
				}

				node.Walkable = true;
				node.Penalty = initialPenalty;
				node.UpdatePositionFromVertices();
			}

			tile.bbTree.RebuildFrom(nodes);
			CreateNodeConnections(tile.nodes);

			//Remove the fake graph
			TriangleMeshNode.SetNavmeshHolder(graphIndex, null);

			return tile;
		}

		/** Create connections between all nodes.
		 * \warning This implementation is not thread safe. It uses cached variables to improve performance
		 */
		void CreateNodeConnections (TriangleMeshNode[] nodes) {
			List<MeshNode> connections = Pathfinding.Util.ListPool<MeshNode>.Claim();
			List<uint> connectionCosts = Pathfinding.Util.ListPool<uint>.Claim();

			Dictionary<Int2, int> nodeRefs = cachedInt2_int_dict;
			nodeRefs.Clear();

			// Build node neighbours
			for (int i = 0; i < nodes.Length; i++) {
				TriangleMeshNode node = nodes[i];

				int av = node.GetVertexCount();

				for (int a = 0; a < av; a++) {
					// Recast can in some very special cases generate degenerate triangles which are simply lines
					// In that case, duplicate keys might be added and thus an exception will be thrown
					// It is safe to ignore the second edge though... I think (only found one case where this happens)
					var key = new Int2(node.GetVertexIndex(a), node.GetVertexIndex((a+1) % av));
					if (!nodeRefs.ContainsKey(key)) {
						nodeRefs.Add(key, i);
					}
				}
			}


			for (int i = 0; i < nodes.Length; i++) {
				TriangleMeshNode node = nodes[i];

				connections.Clear();
				connectionCosts.Clear();

				int av = node.GetVertexCount();

				for (int a = 0; a < av; a++) {
					int first = node.GetVertexIndex(a);
					int second = node.GetVertexIndex((a+1) % av);
					int connNode;

					if (nodeRefs.TryGetValue(new Int2(second, first), out connNode)) {
						TriangleMeshNode other = nodes[connNode];

						int bv = other.GetVertexCount();

						for (int b = 0; b < bv; b++) {
							/** \todo This will fail on edges which are only partially shared */
							if (other.GetVertexIndex(b) == second && other.GetVertexIndex((b+1) % bv) == first) {
								uint cost = (uint)(node.position - other.position).costMagnitude;
								connections.Add(other);
								connectionCosts.Add(cost);
								break;
							}
						}
					}
				}

				node.connections = connections.ToArray();
				node.connectionCosts = connectionCosts.ToArray();
			}

			Pathfinding.Util.ListPool<MeshNode>.Release(connections);
			Pathfinding.Util.ListPool<uint>.Release(connectionCosts);
		}

		/** Generate connections between the two tiles.
		 * The tiles must be adjacent.
		 */
		void ConnectTiles (NavmeshTile tile1, NavmeshTile tile2) {
			if (tile1 == null) return;//throw new System.ArgumentNullException ("tile1");
			if (tile2 == null) return;//throw new System.ArgumentNullException ("tile2");

			if (tile1.nodes == null) throw new System.ArgumentException("tile1 does not contain any nodes");
			if (tile2.nodes == null) throw new System.ArgumentException("tile2 does not contain any nodes");

			int t1x = Mathf.Clamp(tile2.x, tile1.x, tile1.x+tile1.w-1);
			int t2x = Mathf.Clamp(tile1.x, tile2.x, tile2.x+tile2.w-1);
			int t1z = Mathf.Clamp(tile2.z, tile1.z, tile1.z+tile1.d-1);
			int t2z = Mathf.Clamp(tile1.z, tile2.z, tile2.z+tile2.d-1);

			int coord, altcoord;
			int t1coord, t2coord;

			float tcs;

			if (t1x == t2x) {
				coord = 2;
				altcoord = 0;
				t1coord = t1z;
				t2coord = t2z;
				tcs = tileSizeZ*cellSize;
			} else if (t1z == t2z) {
				coord = 0;
				altcoord = 2;
				t1coord = t1x;
				t2coord = t2x;
				tcs = tileSizeX*cellSize;
			} else {
				throw new System.ArgumentException("Tiles are not adjacent (neither x or z coordinates match)");
			}

			if (Math.Abs(t1coord-t2coord) != 1) {
				Debug.Log(tile1.x + " " + tile1.z + " " + tile1.w + " " + tile1.d + "\n"+
					tile2.x + " " + tile2.z + " " + tile2.w + " " + tile2.d+"\n"+
					t1x + " " + t1z + " " + t2x + " " + t2z);
				throw new System.ArgumentException("Tiles are not adjacent (tile coordinates must differ by exactly 1. Got '" + t1coord + "' and '" + t2coord + "')");
			}

			//Midpoint between the two tiles
			int midpoint = (int)Math.Round((Math.Max(t1coord, t2coord) * tcs + forcedBounds.min[coord]) * Int3.Precision);

#if ASTARDEBUG
			Vector3 v1 = new Vector3(-100, 0, -100);
			Vector3 v2 = new Vector3(100, 0, 100);
			v1[coord] = midpoint*Int3.PrecisionFactor;
			v2[coord] = midpoint*Int3.PrecisionFactor;

			Debug.DrawLine(v1, v2, Color.magenta);
#endif

			TriangleMeshNode[] nodes1 = tile1.nodes;
			TriangleMeshNode[] nodes2 = tile2.nodes;

			//Find adjacent nodes on the border between the tiles
			for (int i = 0; i < nodes1.Length; i++) {
				TriangleMeshNode node = nodes1[i];
				int av = node.GetVertexCount();

				for (int a = 0; a < av; a++) {
					Int3 ap1 = node.GetVertex(a);
					Int3 ap2 = node.GetVertex((a+1) % av);
					if (Math.Abs(ap1[coord] - midpoint) < 2 && Math.Abs(ap2[coord] - midpoint) < 2) {
#if ASTARDEBUG
						Debug.DrawLine((Vector3)ap1, (Vector3)ap2, Color.red);
#endif

						int minalt = Math.Min(ap1[altcoord], ap2[altcoord]);
						int maxalt = Math.Max(ap1[altcoord], ap2[altcoord]);

						//Degenerate edge
						if (minalt == maxalt) continue;

						for (int j = 0; j < nodes2.Length; j++) {
							TriangleMeshNode other = nodes2[j];
							int bv = other.GetVertexCount();
							for (int b = 0; b < bv; b++) {
								Int3 bp1 = other.GetVertex(b);
								Int3 bp2 = other.GetVertex((b+1) % av);
								if (Math.Abs(bp1[coord] - midpoint) < 2 && Math.Abs(bp2[coord] - midpoint) < 2) {
									int minalt2 = Math.Min(bp1[altcoord], bp2[altcoord]);
									int maxalt2 = Math.Max(bp1[altcoord], bp2[altcoord]);

									//Degenerate edge
									if (minalt2 == maxalt2) continue;

									if (maxalt > minalt2 && minalt < maxalt2) {
										//Adjacent

										//Test shortest distance between the segments (first test if they are equal since that is much faster)
										if ((ap1 == bp1 && ap2 == bp2) || (ap1 == bp2 && ap2 == bp1) ||
											VectorMath.SqrDistanceSegmentSegment((Vector3)ap1, (Vector3)ap2, (Vector3)bp1, (Vector3)bp2) < walkableClimb*walkableClimb) {
											uint cost = (uint)(node.position - other.position).costMagnitude;

											node.AddConnection(other, cost);
											other.AddConnection(node, cost);
										}
									}
								}
							}
						}
					}
				}
			}
		}

		/** Start batch updating of tiles.
		 * During batch updating, tiles will not be connected if they are updating with ReplaceTile.
		 * When ending batching, all affected tiles will be connected.
		 * This is faster than not using batching.
		 */
		public void StartBatchTileUpdate () {
			if (batchTileUpdate) throw new System.InvalidOperationException("Calling StartBatchLoad when batching is already enabled");
			batchTileUpdate = true;
		}

		/** End batch updating of tiles.
		 * During batch updating, tiles will not be connected if they are updating with ReplaceTile.
		 * When ending batching, all affected tiles will be connected.
		 * This is faster than not using batching.
		 */
		public void EndBatchTileUpdate () {
			if (!batchTileUpdate) throw new System.InvalidOperationException("Calling EndBatchLoad when batching not enabled");

			batchTileUpdate = false;

			int tw = tileXCount;
			int td = tileZCount;

			//Clear all flags
			for (int z = 0; z < td; z++) {
				for (int x = 0; x < tw; x++) {
					tiles[x + z*tileXCount].flag = false;
				}
			}

			for (int i = 0; i < batchUpdatedTiles.Count; i++) tiles[batchUpdatedTiles[i]].flag = true;

			for (int z = 0; z < td; z++) {
				for (int x = 0; x < tw; x++) {
					if (x < tw-1
						&& (tiles[x + z*tileXCount].flag || tiles[x+1 + z*tileXCount].flag)
						&& tiles[x + z*tileXCount] != tiles[x+1 + z*tileXCount]) {
						ConnectTiles(tiles[x + z*tileXCount], tiles[x+1 + z*tileXCount]);
					}

					if (z < td-1
						&& (tiles[x + z*tileXCount].flag || tiles[x + (z+1)*tileXCount].flag)
						&& tiles[x + z*tileXCount] != tiles[x + (z+1)*tileXCount]) {
						ConnectTiles(tiles[x + z*tileXCount], tiles[x + (z+1)*tileXCount]);
					}
				}
			}

			batchUpdatedTiles.Clear();
		}

		/** Replace tile at index with nodes created from specified navmesh.
		 * \see StartBatchTileUpdating
		 */
		public void ReplaceTile (int x, int z, Int3[] verts, int[] tris, bool worldSpace) {
			ReplaceTile(x, z, 1, 1, verts, tris, worldSpace);
		}

		public void ReplaceTile (int x, int z, int w, int d, Int3[] verts, int[] tris, bool worldSpace) {
			if (x + w > tileXCount || z+d > tileZCount || x < 0 || z < 0) {
				throw new System.ArgumentException("Tile is placed at an out of bounds position or extends out of the graph bounds ("+x+", " + z + " [" + w + ", " + d+ "] " + tileXCount + " " + tileZCount + ")");
			}

			if (w < 1 || d < 1) throw new System.ArgumentException("width and depth must be greater or equal to 1. Was " + w + ", " + d);

			//Remove previous tiles
			for (int cz = z; cz < z+d; cz++) {
				for (int cx = x; cx < x+w; cx++) {
					NavmeshTile otile = tiles[cx + cz*tileXCount];
					if (otile == null) continue;

					//Remove old tile connections
					RemoveConnectionsFromTile(otile);

					for (int i = 0; i < otile.nodes.Length; i++) {
						otile.nodes[i].Destroy();
					}

					for (int qz = otile.z; qz < otile.z+otile.d; qz++) {
						for (int qx = otile.x; qx < otile.x+otile.w; qx++) {
							NavmeshTile qtile = tiles[qx + qz*tileXCount];
							if (qtile == null || qtile != otile) throw new System.Exception("This should not happen");

							if (qz < z || qz >= z+d || qx < x || qx >= x+w) {
								//if out of this tile's bounds, replace with empty tile
								tiles[qx + qz*tileXCount] = NewEmptyTile(qx, qz);

								if (batchTileUpdate) {
									batchUpdatedTiles.Add(qx + qz*tileXCount);
								}
							} else {
								//Will be replaced by the new tile
								tiles[qx + qz*tileXCount] = null;
							}
						}
					}
				}
			}

			//Create a new navmesh tile and assign its settings
			var tile = new NavmeshTile();

			tile.x = x;
			tile.z = z;
			tile.w = w;
			tile.d = d;
			tile.tris = tris;
			tile.verts = verts;
			tile.bbTree = new BBTree();

			if (tile.tris.Length % 3 != 0) throw new System.ArgumentException("Triangle array's length must be a multiple of 3 (tris)");

			if (tile.verts.Length > 0xFFFF) throw new System.ArgumentException("Too many vertices per tile (more than 65535)");

			if (!worldSpace) {
				if (!Mathf.Approximately(x*tileSizeX*cellSize*Int3.FloatPrecision, (float)Math.Round(x*tileSizeX*cellSize*Int3.FloatPrecision))) Debug.LogWarning("Possible numerical imprecision. Consider adjusting tileSize and/or cellSize");
				if (!Mathf.Approximately(z*tileSizeZ*cellSize*Int3.FloatPrecision, (float)Math.Round(z*tileSizeZ*cellSize*Int3.FloatPrecision))) Debug.LogWarning("Possible numerical imprecision. Consider adjusting tileSize and/or cellSize");

				var offset = (Int3)(new Vector3((x * tileSizeX * cellSize), 0, (z * tileSizeZ * cellSize)) + forcedBounds.min);

				for (int i = 0; i < verts.Length; i++) {
					verts[i] += offset;
				}
			}

			var nodes = new TriangleMeshNode[tile.tris.Length/3];
			tile.nodes = nodes;

			//Here we are faking a new graph
			//The tile is not added to any graphs yet, but to get the position querys from the nodes
			//to work correctly (not throw exceptions because the tile is not calculated) we fake a new graph
			//and direct the position queries directly to the tile
			int graphIndex = AstarPath.active.astarData.graphs.Length;

			TriangleMeshNode.SetNavmeshHolder(graphIndex, tile);

			//This index will be ORed to the triangle indices
			int tileIndex = x + z*tileXCount;
			tileIndex <<= TileIndexOffset;

			//Create nodes and assign triangle indices
			for (int i = 0; i < nodes.Length; i++) {
				var node = new TriangleMeshNode(active);
				nodes[i] = node;
				node.GraphIndex = (uint)graphIndex;
				node.v0 = tile.tris[i*3+0] | tileIndex;
				node.v1 = tile.tris[i*3+1] | tileIndex;
				node.v2 = tile.tris[i*3+2] | tileIndex;

				//Degenerate triangles might occur, but they will not cause any large troubles anymore
				//if (Polygon.IsColinear (node.GetVertex(0), node.GetVertex(1), node.GetVertex(2))) {
				//	Debug.Log ("COLINEAR!!!!!!");
				//}

				//Make sure the triangle is clockwise
				if (!VectorMath.IsClockwiseXZ(node.GetVertex(0), node.GetVertex(1), node.GetVertex(2))) {
					int tmp = node.v0;
					node.v0 = node.v2;
					node.v2 = tmp;
				}

				node.Walkable = true;
				node.Penalty = initialPenalty;
				node.UpdatePositionFromVertices();
			}

			tile.bbTree.RebuildFrom(nodes);

			CreateNodeConnections(tile.nodes);

			//Set tile
			for (int cz = z; cz < z+d; cz++) {
				for (int cx = x; cx < x+w; cx++) {
					tiles[cx + cz*tileXCount] = tile;
				}
			}

			if (batchTileUpdate) {
				batchUpdatedTiles.Add(x + z*tileXCount);
			} else {
				ConnectTileWithNeighbours(tile);
				/*if (x > 0) ConnectTiles (tiles[(x-1) + z*tileXCount], tile);
				 * if (z > 0) ConnectTiles (tiles[x + (z-1)*tileXCount], tile);
				 * if (x < tileXCount-1) ConnectTiles (tiles[(x+1) + z*tileXCount], tile);
				 * if (z < tileZCount-1) ConnectTiles (tiles[x + (z+1)*tileXCount], tile);*/
			}

			//Remove the fake graph
			TriangleMeshNode.SetNavmeshHolder(graphIndex, null);

			//Real graph index
			//TODO, could this step be changed for this function, is a fake index required?
			graphIndex = AstarPath.active.astarData.GetGraphIndex(this);

			for (int i = 0; i < nodes.Length; i++) nodes[i].GraphIndex = (uint)graphIndex;
		}

		void CollectTreeMeshes (Terrain terrain, List<ExtraMesh> extraMeshes) {
			TerrainData data = terrain.terrainData;

			for (int i = 0; i < data.treeInstances.Length; i++) {
				TreeInstance instance = data.treeInstances[i];
				TreePrototype prot = data.treePrototypes[instance.prototypeIndex];

				// Make sure that the tree prefab exists
				if (prot.prefab == null) {
					continue;
				}

				var collider = prot.prefab.GetComponent<Collider>();

				if (collider == null) {
					var b = new Bounds(terrain.transform.position + Vector3.Scale(instance.position, data.size), new Vector3(instance.widthScale, instance.heightScale, instance.widthScale));

					Matrix4x4 matrix = Matrix4x4.TRS(terrain.transform.position +  Vector3.Scale(instance.position, data.size), Quaternion.identity, new Vector3(instance.widthScale, instance.heightScale, instance.widthScale)*0.5f);


					var m = new ExtraMesh(BoxColliderVerts, BoxColliderTris, b, matrix);

#if ASTARDEBUG
					Debug.DrawRay(instance.position, Vector3.up, Color.red, 1);
#endif
					extraMeshes.Add(m);
				} else {
					//The prefab has a collider, use that instead
					Vector3 pos = terrain.transform.position + Vector3.Scale(instance.position, data.size);
					var scale = new Vector3(instance.widthScale, instance.heightScale, instance.widthScale);

					//Generate a mesh from the collider
					ExtraMesh m = RasterizeCollider(collider, Matrix4x4.TRS(pos, Quaternion.identity, scale));

					//Make sure a valid mesh was generated
					if (m.vertices != null) {
#if ASTARDEBUG
						Debug.DrawRay(pos, Vector3.up, Color.yellow, 1);
#endif
						//The bounds are incorrectly based on collider.bounds
						m.RecalculateBounds();
						extraMeshes.Add(m);
					}
				}
			}
		}

		void CollectTerrainMeshes (Bounds bounds, bool rasterizeTrees, List<ExtraMesh> extraMeshes) {
			// Find all terrains in the scene
			var terrains = MonoBehaviour.FindObjectsOfType(typeof(Terrain)) as Terrain[];

			if (terrains.Length > 0) {
				// Loop through all terrains in the scene
				for (int j = 0; j < terrains.Length; j++) {
					TerrainData terrainData = terrains[j].terrainData;

					if (terrainData == null) continue;

					Vector3 offset = terrains[j].GetPosition();
					Vector3 center = offset + terrainData.size * 0.5F;

					// Figure out the bounds of the terrain in world space
					var b = new Bounds(center, terrainData.size);

					// Only include terrains which intersects the graph
					if (!b.Intersects(bounds)) continue;

					// Sample the terrain heightmap
					float[, ] heights = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

					// Clamp to at least 1 since that's the resolution of the heightmap
					terrainSampleSize = Math.Max(terrainSampleSize, 1);

					int rwidth = terrainData.heightmapWidth;
					int rheight = terrainData.heightmapHeight;

					int hWidth = (terrainData.heightmapWidth+terrainSampleSize-1) / terrainSampleSize + 1;
					int hHeight = (terrainData.heightmapHeight+terrainSampleSize-1) / terrainSampleSize + 1;

					// Create a mesh from the heightmap
					var terrainVertices = new Vector3[hWidth*hHeight];

					Vector3 hSampleSize = terrainData.heightmapScale;
					float heightScale = terrainData.size.y;

					// Create lots of vertices
					for (int z = 0, nz = 0; nz < hHeight; z += terrainSampleSize, nz++) {
						for (int x = 0, nx = 0; nx < hWidth; x += terrainSampleSize, nx++) {
							int rx = Math.Min(x, rwidth-1);
							int rz = Math.Min(z, rheight-1);

							terrainVertices[nz*hWidth + nx] = new Vector3(rz * hSampleSize.x, heights[rx, rz]*heightScale, rx * hSampleSize.z) + offset;
						}
					}

					// Create the mesh by creating triangles in a grid like pattern
					var tris = new int[(hWidth-1)*(hHeight-1)*2*3];
					int triangleIndex = 0;
					for (int z = 0; z < hHeight-1; z++) {
						for (int x = 0; x < hWidth-1; x++) {
							tris[triangleIndex]   = z*hWidth + x;
							tris[triangleIndex+1] = z*hWidth + x+1;
							tris[triangleIndex+2] = (z+1)*hWidth + x+1;
							triangleIndex += 3;
							tris[triangleIndex]   = z*hWidth + x;
							tris[triangleIndex+1] = (z+1)*hWidth + x+1;
							tris[triangleIndex+2] = (z+1)*hWidth + x;
							triangleIndex += 3;
						}
					}

					#if ASTARDEBUG
					for (int i = 0; i < tris.Length; i += 3) {
						Debug.DrawLine(terrainVertices[tris[i]], terrainVertices[tris[i+1]], Color.red);
						Debug.DrawLine(terrainVertices[tris[i+1]], terrainVertices[tris[i+2]], Color.red);
						Debug.DrawLine(terrainVertices[tris[i+2]], terrainVertices[tris[i]], Color.red);
					}
					#endif


					extraMeshes.Add(new ExtraMesh(terrainVertices, tris, b));

					if (rasterizeTrees) {
						// Rasterize all tree colliders on this terrain object
						CollectTreeMeshes(terrains[j], extraMeshes);
					}
				}
			}
		}

		void CollectColliderMeshes (Bounds bounds, List<ExtraMesh> extraMeshes) {
			var colls = MonoBehaviour.FindObjectsOfType(typeof(Collider)) as Collider[];

			if ((tagMask != null && tagMask.Count > 0) || mask != 0) {
				for (int i = 0; i < colls.Length; i++) {
					Collider col = colls[i];

					if ((((1 << col.gameObject.layer) & mask) != 0 || tagMask.Contains(col.tag)) && col.enabled && !col.isTrigger && col.bounds.Intersects(bounds)) {
						ExtraMesh emesh = RasterizeCollider(col);
						//Make sure a valid ExtraMesh was returned
						if (emesh.vertices != null)
							extraMeshes.Add(emesh);
					}
				}
			}

			//Clear cache to avoid memory leak
			capsuleCache.Clear();
		}

		bool CollectMeshes (out List<ExtraMesh> extraMeshes, Bounds bounds) {
			extraMeshes = new List<ExtraMesh>();

			if (rasterizeMeshes) {
				GetSceneMeshes(bounds, tagMask, mask, extraMeshes);
			}

			GetRecastMeshObjs(bounds, extraMeshes);

			if (rasterizeTerrain) {
				CollectTerrainMeshes(bounds, rasterizeTrees, extraMeshes);
			}

			if (rasterizeColliders) {
				CollectColliderMeshes(bounds, extraMeshes);
			}

			if (extraMeshes.Count == 0) {
				Debug.LogWarning("No MeshFilters were found contained in the layers specified by the 'mask' variables");
				return false;
			}

			return true;
		}

		/** Box Collider triangle indices can be reused for multiple instances.
		 * \warning This array should never be changed
		 */
		private readonly int[] BoxColliderTris = {
			0, 1, 2,
			0, 2, 3,

			6, 5, 4,
			7, 6, 4,

			0, 5, 1,
			0, 4, 5,

			1, 6, 2,
			1, 5, 6,

			2, 7, 3,
			2, 6, 7,

			3, 4, 0,
			3, 7, 4
		};

		/** Box Collider vertices can be reused for multiple instances.
		 * \warning This array should never be changed
		 */
		private readonly Vector3[] BoxColliderVerts = {
			new Vector3(-1, -1, -1),
			new Vector3(1, -1, -1),
			new Vector3(1, -1, 1),
			new Vector3(-1, -1, 1),

			new Vector3(-1, 1, -1),
			new Vector3(1, 1, -1),
			new Vector3(1, 1, 1),
			new Vector3(-1, 1, 1),
		};

		private List<CapsuleCache> capsuleCache = new List<CapsuleCache>();

		class CapsuleCache {
			public int rows;
			public float height;
			public Vector3[] verts;
			public int[] tris;
		}

		/** Rasterizes a collider to a mesh.
		 * This will pass the col.transform.localToWorldMatrix to the other overload of this function.
		 */
		ExtraMesh RasterizeCollider (Collider col) {
			return RasterizeCollider(col, col.transform.localToWorldMatrix);
		}

		/** Rasterizes a collider to a mesh assuming it's vertices should be multiplied with the matrix.
		 * Note that the bounds of the returned ExtraMesh is based on collider.bounds. So you might want to
		 * call myExtraMesh.RecalculateBounds on the returned mesh to recalculate it if the collider.bounds would
		 * not give the correct value.
		 * */
		ExtraMesh RasterizeCollider (Collider col, Matrix4x4 localToWorldMatrix) {
			if (col is BoxCollider) {
				var collider = col as BoxCollider;

				Matrix4x4 matrix = Matrix4x4.TRS(collider.center, Quaternion.identity, collider.size*0.5f);
				matrix = localToWorldMatrix * matrix;

				Bounds b = collider.bounds;

				var m = new ExtraMesh(BoxColliderVerts, BoxColliderTris, b, matrix);

#if ASTARDEBUG
				Vector3[] verts = BoxColliderVerts;
				int[] tris = BoxColliderTris;

				for (int i = 0; i < tris.Length; i += 3) {
					Debug.DrawLine(matrix.MultiplyPoint3x4(verts[tris[i]]), matrix.MultiplyPoint3x4(verts[tris[i+1]]), Color.yellow);
					Debug.DrawLine(matrix.MultiplyPoint3x4(verts[tris[i+2]]), matrix.MultiplyPoint3x4(verts[tris[i+1]]), Color.yellow);
					Debug.DrawLine(matrix.MultiplyPoint3x4(verts[tris[i]]), matrix.MultiplyPoint3x4(verts[tris[i+2]]), Color.yellow);

					//Normal debug
					/*Vector3 va = matrix.MultiplyPoint3x4(verts[tris[i]]);
					 * Vector3 vb = matrix.MultiplyPoint3x4(verts[tris[i+1]]);
					 * Vector3 vc = matrix.MultiplyPoint3x4(verts[tris[i+2]]);
					 *
					 * Debug.DrawRay ((va+vb+vc)/3, Vector3.Cross(vb-va,vc-va).normalized,Color.blue);*/
				}
#endif
				return m;
			} else if (col is SphereCollider || col is CapsuleCollider) {
				var scollider = col as SphereCollider;
				var ccollider = col as CapsuleCollider;

				float radius = (scollider != null ? scollider.radius : ccollider.radius);
				float height = scollider != null ? 0 : (ccollider.height*0.5f/radius) - 1;

				Matrix4x4 matrix = Matrix4x4.TRS(scollider != null ? scollider.center : ccollider.center, Quaternion.identity, Vector3.one*radius);
				matrix = localToWorldMatrix * matrix;

				//Calculate the number of rows to use
				//grows as sqrt(x) to the radius of the sphere/capsule which I have found works quite good
				int rows = Mathf.Max(4, Mathf.RoundToInt(colliderRasterizeDetail*Mathf.Sqrt(matrix.MultiplyVector(Vector3.one).magnitude)));

				if (rows > 100) {
					Debug.LogWarning("Very large detail for some collider meshes. Consider decreasing Collider Rasterize Detail (RecastGraph)");
				}

				int cols = rows;

				Vector3[] verts;
				int[] trisArr;


				//Check if we have already calculated a similar capsule
				CapsuleCache cached = null;
				for (int i = 0; i < capsuleCache.Count; i++) {
					CapsuleCache c = capsuleCache[i];
					if (c.rows == rows && Mathf.Approximately(c.height, height)) {
						cached = c;
					}
				}

				if (cached == null) {
					//Generate a sphere/capsule mesh

					verts = new Vector3[(rows)*cols + 2];

					var tris = new List<int>();
					verts[verts.Length-1] = Vector3.up;

					for (int r = 0; r < rows; r++) {
						for (int c = 0; c < cols; c++) {
							verts[c + r*cols] = new Vector3(Mathf.Cos(c*Mathf.PI*2/cols)*Mathf.Sin((r*Mathf.PI/(rows-1))), Mathf.Cos((r*Mathf.PI/(rows-1))) + (r < rows/2 ? height : -height), Mathf.Sin(c*Mathf.PI*2/cols)*Mathf.Sin((r*Mathf.PI/(rows-1))));
						}
					}

					verts[verts.Length-2] = Vector3.down;

					for (int i = 0, j = cols-1; i < cols; j = i++) {
						tris.Add(verts.Length-1);
						tris.Add(0*cols + j);
						tris.Add(0*cols + i);
					}

					for (int r = 1; r < rows; r++) {
						for (int i = 0, j = cols-1; i < cols; j = i++) {
							tris.Add(r*cols + i);
							tris.Add(r*cols + j);
							tris.Add((r-1)*cols + i);

							tris.Add((r-1)*cols + j);
							tris.Add((r-1)*cols + i);
							tris.Add(r*cols + j);
						}
					}

					for (int i = 0, j = cols-1; i < cols; j = i++) {
						tris.Add(verts.Length-2);
						tris.Add((rows-1)*cols + j);
						tris.Add((rows-1)*cols + i);
					}

					//Add calculated mesh to the cache
					cached = new CapsuleCache();
					cached.rows = rows;
					cached.height = height;
					cached.verts = verts;
					cached.tris = tris.ToArray();
					capsuleCache.Add(cached);
				}

				//Read from cache
				verts = cached.verts;
				trisArr = cached.tris;

				Bounds b = col.bounds;

				var m = new ExtraMesh(verts, trisArr, b, matrix);

#if ASTARDEBUG
				for (int i = 0; i < trisArr.Length; i += 3) {
					Debug.DrawLine(matrix.MultiplyPoint3x4(verts[trisArr[i]]), matrix.MultiplyPoint3x4(verts[trisArr[i+1]]), Color.yellow);
					Debug.DrawLine(matrix.MultiplyPoint3x4(verts[trisArr[i+2]]), matrix.MultiplyPoint3x4(verts[trisArr[i+1]]), Color.yellow);
					Debug.DrawLine(matrix.MultiplyPoint3x4(verts[trisArr[i]]), matrix.MultiplyPoint3x4(verts[trisArr[i+2]]), Color.yellow);
				}
#endif
				return m;
			} else if (col is MeshCollider) {
				var collider = col as MeshCollider;

				if (collider.sharedMesh != null) {
					var m = new ExtraMesh(collider.sharedMesh.vertices, collider.sharedMesh.triangles, collider.bounds, localToWorldMatrix);
					return m;
				}
			}

			return new ExtraMesh();
		}


		public bool Linecast (Vector3 origin, Vector3 end) {
			return Linecast(origin, end, GetNearest(origin, NNConstraint.None).node);
		}

		public bool Linecast (Vector3 origin, Vector3 end, GraphNode hint, out GraphHitInfo hit) {
			return NavMeshGraph.Linecast(this as INavmesh, origin, end, hint, out hit, null);
		}

		public bool Linecast (Vector3 origin, Vector3 end, GraphNode hint) {
			GraphHitInfo hit;

			return NavMeshGraph.Linecast(this as INavmesh, origin, end, hint, out hit, null);
		}

		/** Returns if there is an obstacle between \a origin and \a end on the graph.
		 * \param [in] tmp_origin Point to start from
		 * \param [in] tmp_end Point to linecast to
		 * \param [out] hit Contains info on what was hit, see GraphHitInfo
		 * \param [in] hint You need to pass the node closest to the start point, if null, a search for the closest node will be done
		 * \param trace If a list is passed, then it will be filled with all nodes the linecast traverses
		 * This is not the same as Physics.Linecast, this function traverses the \b graph and looks for collisions instead of checking for collider intersection.
		 * \astarpro */
		public bool Linecast (Vector3 tmp_origin, Vector3 tmp_end, GraphNode hint, out GraphHitInfo hit, List<GraphNode> trace) {
			return NavMeshGraph.Linecast(this, tmp_origin, tmp_end, hint, out hit, trace);
		}

		public override void OnDrawGizmos (bool drawNodes) {
			if (!drawNodes) {
				return;
			}

			Gizmos.color = Color.white;
			Gizmos.DrawWireCube(forcedBounds.center, forcedBounds.size);

			PathHandler debugData = AstarPath.active.debugPathData;

			GraphNodeDelegateCancelable del = delegate(GraphNode _node) {
				var node = _node as TriangleMeshNode;

				if (AstarPath.active.showSearchTree && debugData != null) {
					bool v = InSearchTree(node, AstarPath.active.debugPath);
					//debugData.GetPathNode(node).parent != null && debugData.GetPathNode(node).parent.node != null;
					if (v && showNodeConnections) {
						//Gizmos.color = new Color (0,1,0,0.7F);
						var pnode = debugData.GetPathNode(node);
						if (pnode.parent != null) {
							Gizmos.color = NodeColor(node, debugData);
							Gizmos.DrawLine((Vector3)node.position, (Vector3)debugData.GetPathNode(node).parent.node.position);
						}
					}

					if (showMeshOutline) {
						Gizmos.color = node.Walkable ? NodeColor(node, debugData) : AstarColor.UnwalkableNode;
						if (!v) Gizmos.color = Gizmos.color * new Color(1, 1, 1, 0.1f);

						Gizmos.DrawLine((Vector3)node.GetVertex(0), (Vector3)node.GetVertex(1));
						Gizmos.DrawLine((Vector3)node.GetVertex(1), (Vector3)node.GetVertex(2));
						Gizmos.DrawLine((Vector3)node.GetVertex(2), (Vector3)node.GetVertex(0));
					}
				} else {
					if (showNodeConnections) {
						Gizmos.color = NodeColor(node, null);

						for (int q = 0; q < node.connections.Length; q++) {
							//Gizmos.color = Color.Lerp (Color.green,Color.red,node.connectionCosts[q]/8000F);
							Gizmos.DrawLine((Vector3)node.position, Vector3.Lerp((Vector3)node.connections[q].position, (Vector3)node.position, 0.4f));
						}
					}

					if (showMeshOutline) {
						Gizmos.color = node.Walkable ? NodeColor(node, debugData) : AstarColor.UnwalkableNode;


						Gizmos.DrawLine((Vector3)node.GetVertex(0), (Vector3)node.GetVertex(1));
						Gizmos.DrawLine((Vector3)node.GetVertex(1), (Vector3)node.GetVertex(2));
						Gizmos.DrawLine((Vector3)node.GetVertex(2), (Vector3)node.GetVertex(0));
					}
				}

				//Gizmos.color.a = 0.2F;

				return true;
			};

			GetNodes(del);
		}

#if ASTAR_NO_JSON
		public override void SerializeSettings (GraphSerializationContext ctx) {
			base.SerializeSettings(ctx);
			ctx.writer.Write(characterRadius);
			ctx.writer.Write(contourMaxError);
			ctx.writer.Write(cellSize);
			ctx.writer.Write(cellHeight);
			ctx.writer.Write(walkableHeight);
			ctx.writer.Write(maxSlope);
			ctx.writer.Write(maxEdgeLength);
			ctx.writer.Write(editorTileSize);
			ctx.writer.Write(tileSizeX);
			ctx.writer.Write(nearestSearchOnlyXZ);
			ctx.writer.Write(useTiles);
			ctx.writer.Write((int)relevantGraphSurfaceMode);
			ctx.writer.Write(rasterizeColliders);
			ctx.writer.Write(rasterizeMeshes);
			ctx.writer.Write(rasterizeTerrain);
			ctx.writer.Write(rasterizeTrees);
			ctx.writer.Write(colliderRasterizeDetail);
			ctx.SerializeVector3(forcedBoundsCenter);
			ctx.SerializeVector3(forcedBoundsSize);
			ctx.writer.Write(mask);
			ctx.writer.Write(tagMask.Count);
			for (int i = 0; i < tagMask.Count; i++) {
				ctx.writer.Write(tagMask[i]);
			}
			ctx.writer.Write(showMeshOutline);
			ctx.writer.Write(showNodeConnections);
			ctx.writer.Write(terrainSampleSize);

			ctx.writer.Write(walkableClimb);
			ctx.writer.Write(minRegionSize);
			ctx.writer.Write(tileSizeZ);
			ctx.writer.Write(showMeshSurface);
		}

		public override void DeserializeSettings (GraphSerializationContext ctx) {
			base.DeserializeSettings(ctx);

			characterRadius = ctx.reader.ReadSingle();
			contourMaxError = ctx.reader.ReadSingle();
			cellSize = ctx.reader.ReadSingle();
			cellHeight = ctx.reader.ReadSingle();
			walkableHeight = ctx.reader.ReadSingle();
			maxSlope = ctx.reader.ReadSingle();
			maxEdgeLength = ctx.reader.ReadSingle();
			editorTileSize = ctx.reader.ReadInt32();
			tileSizeX = ctx.reader.ReadInt32();
			nearestSearchOnlyXZ = ctx.reader.ReadBoolean();
			useTiles = ctx.reader.ReadBoolean();
			relevantGraphSurfaceMode = (RelevantGraphSurfaceMode)ctx.reader.ReadInt32();
			rasterizeColliders = ctx.reader.ReadBoolean();
			rasterizeMeshes = ctx.reader.ReadBoolean();
			rasterizeTerrain = ctx.reader.ReadBoolean();
			rasterizeTrees = ctx.reader.ReadBoolean();
			colliderRasterizeDetail = ctx.reader.ReadSingle();
			forcedBoundsCenter = ctx.DeserializeVector3();
			forcedBoundsSize = ctx.DeserializeVector3();
			mask = ctx.reader.ReadInt32();

			int count = ctx.reader.ReadInt32();
			tagMask = new List<string>(count);
			for (int i = 0; i < count; i++) {
				tagMask.Add(ctx.reader.ReadString());
			}

			showMeshOutline = ctx.reader.ReadBoolean();
			showNodeConnections = ctx.reader.ReadBoolean();
			terrainSampleSize = ctx.reader.ReadInt32();

			// These were originally forgotten but added in an upgrade
			// To keep backwards compatibility, they are only deserialized
			// If they exist in the streamed data
			walkableClimb = ctx.DeserializeFloat(walkableClimb);
			minRegionSize = ctx.DeserializeFloat(minRegionSize);

			// Make the world square if this value is not in the stream
			tileSizeZ = ctx.DeserializeInt(tileSizeX);

			showMeshSurface = ctx.reader.ReadBoolean();
		}
#endif

		/** Serializes Node Info.
		 * Should serialize:
		 * - Base
		 *    - Node Flags
		 *    - Node Penalties
		 *    - Node
		 * - Node Positions (if applicable)
		 * - Any other information necessary to load the graph in-game
		 * All settings marked with json attributes (e.g JsonMember) have already been
		 * saved as graph settings and do not need to be handled here.
		 *
		 * It is not necessary for this implementation to be forward or backwards compatible.
		 *
		 * \see
		 */
		public override void SerializeExtraInfo (GraphSerializationContext ctx) {
			BinaryWriter writer = ctx.writer;

			if (tiles == null) {
				writer.Write(-1);
				return;
			}
			writer.Write(tileXCount);
			writer.Write(tileZCount);

			for (int z = 0; z < tileZCount; z++) {
				for (int x = 0; x < tileXCount; x++) {
					NavmeshTile tile = tiles[x + z*tileXCount];

					if (tile == null) {
						throw new System.Exception("NULL Tile");
						//writer.Write (-1);
						//continue;
					}

					writer.Write(tile.x);
					writer.Write(tile.z);

					if (tile.x != x || tile.z != z) continue;

					writer.Write(tile.w);
					writer.Write(tile.d);

					writer.Write(tile.tris.Length);

					for (int i = 0; i < tile.tris.Length; i++) writer.Write(tile.tris[i]);

					writer.Write(tile.verts.Length);
					for (int i = 0; i < tile.verts.Length; i++) {
						writer.Write(tile.verts[i].x);
						writer.Write(tile.verts[i].y);
						writer.Write(tile.verts[i].z);
					}

					writer.Write(tile.nodes.Length);
					for (int i = 0; i < tile.nodes.Length; i++) {
						tile.nodes[i].SerializeNode(ctx);
					}
				}
			}



			//return NavMeshGraph.SerializeMeshNodes (this,nodes);
		}

		public override void DeserializeExtraInfo (GraphSerializationContext ctx) {
			//NavMeshGraph.DeserializeMeshNodes (this,nodes,bytes);

			BinaryReader reader = ctx.reader;

			tileXCount = reader.ReadInt32();

			if (tileXCount < 0) return;

			tileZCount = reader.ReadInt32();

			tiles = new NavmeshTile[tileXCount * tileZCount];

			//Make sure mesh nodes can reference this graph
			TriangleMeshNode.SetNavmeshHolder((int)ctx.graphIndex, this);

			for (int z = 0; z < tileZCount; z++) {
				for (int x = 0; x < tileXCount; x++) {
					int tileIndex = x + z*tileXCount;
					int tx = reader.ReadInt32();
					if (tx < 0) throw new System.Exception("Invalid tile coordinates (x < 0)");

					int tz = reader.ReadInt32();
					if (tz < 0) throw new System.Exception("Invalid tile coordinates (z < 0)");

					// This is not the origin of a large tile. Refer back to that tile.
					if (tx != x || tz != z) {
						tiles[tileIndex] = tiles[tz*tileXCount + tx];
						continue;
					}

					var tile = new NavmeshTile();

					tile.x = tx;
					tile.z = tz;
					tile.w = reader.ReadInt32();
					tile.d = reader.ReadInt32();
					tile.bbTree = new BBTree();

					tiles[tileIndex] = tile;

					int trisCount = reader.ReadInt32();

					if (trisCount % 3 != 0) throw new System.Exception("Corrupt data. Triangle indices count must be divisable by 3. Got " + trisCount);

					tile.tris = new int[trisCount];
					for (int i = 0; i < tile.tris.Length; i++) tile.tris[i] = reader.ReadInt32();

					tile.verts = new Int3[reader.ReadInt32()];
					for (int i = 0; i < tile.verts.Length; i++) {
						tile.verts[i] = new Int3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
					}

					int nodeCount = reader.ReadInt32();
					tile.nodes = new TriangleMeshNode[nodeCount];

					//Prepare for storing in vertex indices
					tileIndex <<= TileIndexOffset;

					for (int i = 0; i < tile.nodes.Length; i++) {
						var node = new TriangleMeshNode(active);
						tile.nodes[i] = node;

						node.DeserializeNode(ctx);

						node.v0 = tile.tris[i*3+0] | tileIndex;
						node.v1 = tile.tris[i*3+1] | tileIndex;
						node.v2 = tile.tris[i*3+2] | tileIndex;
						node.UpdatePositionFromVertices();
					}

					tile.bbTree.RebuildFrom(tile.nodes);
				}
			}
		}
	}
}
