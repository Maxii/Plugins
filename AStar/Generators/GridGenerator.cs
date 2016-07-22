using System.Collections.Generic;

using Pathfinding.Serialization;
using Pathfinding.Serialization.JsonFx;
using UnityEngine;
using Math = System.Math;

namespace Pathfinding {
	[JsonOptIn]
	/** Generates a grid of nodes.
	 * The GridGraph does exactly what the name implies, generates nodes in a grid pattern.\n
	 * Grid graphs suit well to when you already have a grid based world.
	 * Features:
	 * - You can update the graph during runtime (good for e.g Tower Defence or RTS games)
	 * - Throw any scene at it, with minimal configurations you can get a good graph from it.
	 * - Supports raycast and the funnel algorithm
	 * - Predictable pattern
	 * - Can apply penalty and walkability values from a supplied image
	 * - Perfect for terrain worlds since it can make areas unwalkable depending on the slope
	 *
	 * \shadowimage{gridgraph_graph.png}
	 * \shadowimage{gridgraph_inspector.png}
	 *
	 * The <b>The Snap Size</b> button snaps the internal size of the graph to exactly contain the current number of nodes, i.e not contain 100.3 nodes but exactly 100 nodes.\n
	 * This will make the "center" coordinate more accurate.\n
	 *
	 * <b>Updating the graph during runtime</b>
	 * Any graph which implements the IUpdatableGraph interface can be updated during runtime.\n
	 * For grid graphs this is a great feature since you can update only a small part of the grid without causing any lag like a complete rescan would.\n
	 * If you for example just have instantiated a sphere obstacle in the scene and you want to update the grid where that sphere was instantiated, you can do this:\n
	 * \code AstarPath.active.UpdateGraphs (ob.collider.bounds); \endcode
	 * Where \a ob is the obstacle you just instantiated (a GameObject).\n
	 * As you can see, the UpdateGraphs function takes a Bounds parameter and it will send an update call to all updateable graphs.\n
	 * A grid graph will update that area and a small margin around it equal to \link Pathfinding.GraphCollision.diameter collision testing diameter/2 \endlink
	 * \see graph-updates for more info about updating graphs during runtime
	 *
	 * <b>Hexagon graphs</b>
	 * The graph can be configured to work like a hexagon graph with some simple settings.
	 * Just set the #neighbours (called 'connections' in the editor) field to 'Six' and then click the 'Configure as Hexagon Graph' button that will show up.
	 * That will set the isometricAngle field to 54.74 (more precisely 90-atan(1/sqrt(2))) and enable uniformEdgeCosts.
	 *
	 * Then the graph will work like a hexagon graph. You might want to rotate it to better match your game however.
	 *
	 * Note however that the snapping to the closest node is not exactly as you would expect in a real hexagon graph,
	 * but it is close enough that you will likely not notice.
	 *
	 * <b>Configure using code</b>
	 * \code
	 * // This holds all graph data
	 * AstarData data = AstarPath.active.astarData;
	 *
	 * // This creates a Grid Graph
	 * GridGraph gg = data.AddGraph(typeof(GridGraph)) as GridGraph;
	 *
	 * // Setup a grid graph with some values
	 * gg.width = 50;
	 * gg.depth = 50;
	 * gg.nodeSize = 1;
	 * gg.center = new Vector3 (10,0,0);
	 *
	 * // Updates internal size from the above values
	 * gg.UpdateSizeFromWidthDepth();
	 *
	 * // Scans all graphs, do not call gg.Scan(), that is an internal method
	 * AstarPath.active.Scan();
	 * \endcode
	 *
	 * \ingroup graphs
	 * \nosubgrouping
	 */
	public class GridGraph : NavGraph, IUpdatableGraph
		, IRaycastableGraph {
		/** This function will be called when this graph is destroyed */
		public override void OnDestroy () {
			base.OnDestroy();

			//Clean up a reference in a static variable which otherwise should point to this graph forever and stop the GC from collecting it
			RemoveGridGraphFromStatic();
		}

		void RemoveGridGraphFromStatic () {
			GridNode.SetGridGraph(AstarPath.active.astarData.GetGraphIndex(this), null);
		}

		/** This is placed here so generators inheriting from this one can override it and set it to false.
		 * If it is true, it means that the nodes array's length will always be equal to width*depth
		 * It is used mainly in the editor to do auto-scanning calls, setting it to false for a non-uniform grid will reduce the number of scans */
		public virtual bool uniformWidthDepthGrid {
			get {
				return true;
			}
		}

		public override int CountNodes () {
			return nodes.Length;
		}

		public override void GetNodes (GraphNodeDelegateCancelable del) {
			if (nodes == null) return;
			for (int i = 0; i < nodes.Length && del(nodes[i]); i++) {}
		}

		/** \name Inspector - Settings
		 * \{ */

		/** Width of the grid in nodes. \see UpdateSizeFromWidthDepth */
		public int width;

		/** Depth (height) of the grid in nodes. \see UpdateSizeFromWidthDepth */
		public int depth;

		/** Scaling of the graph along the X axis.
		 * This should be used if you want different scales on the X and Y axis of the grid
		 */
		[JsonMember]
		public float aspectRatio = 1F;

		/** Angle to use for the isometric projection.
		 * If you are making a 2D isometric game, you may want to use this parameter to adjust the layout of the graph to match your game.
		 * This will essentially scale the graph along one of its diagonals to produce something like this:
		 *
		 * A perspective view of an isometric graph.
		 * \shadowimage{isometric/isometric_perspective.png}
		 *
		 * A top down view of an isometric graph. Note that the graph is entirely 2D, there is no perspective in this image.
		 * \shadowimage{isometric/isometric_top.png}
		 *
		 * Usually the angle that you want to use is either 30 degrees (alternatively 90-30 = 60 degrees) or atan(1/sqrt(2)) which is approximately 35.264 degrees (alternatively 90 - 35.264 = 54.736 degrees).
		 * You might also want to rotate the graph plus or minus 45 degrees around the Y axis to get the oritientation required for your game.
		 *
		 * You can read more about it on the wikipedia page linked below.
		 *
		 * \see http://en.wikipedia.org/wiki/Isometric_projection
		 * \see rotation
		 */
		[JsonMember]
		public float isometricAngle;

		/** If true, all edge costs will be set to the same value.
		 * If false, diagonals will cost more.
		 * This is useful for a hexagon graph where the diagonals are actually the same length as the
		 * normal edges (since the graph has been skewed)
		 */
		[JsonMember]
		public bool uniformEdgeCosts;

		/** Rotation of the grid in degrees */
		[JsonMember]
		public Vector3 rotation;

		public Bounds bounds;

		/** Center point of the grid */
		[JsonMember]
		public Vector3 center;

		/** Size of the grid. Might be negative or smaller than #nodeSize */
		[JsonMember]
		public Vector2 unclampedSize;

		/** Size of one node in world units.
		 * \see UpdateSizeFromWidthDepth
		 */
		[JsonMember]
		public float nodeSize = 1;

		/* Collision and stuff */

		/** Settings on how to check for walkability and height */
		[JsonMember]
		public GraphCollision collision;

		/** The max position difference between two nodes to enable a connection.
		 * Set to 0 to ignore the value.
		 */
		[JsonMember]
		public float maxClimb = 0.4F;

		/** The axis to use for #maxClimb. X = 0, Y = 1, Z = 2. */
		[JsonMember]
		public int maxClimbAxis = 1;

		/** The max slope in degrees for a node to be walkable. */
		[JsonMember]
		public float maxSlope = 90;

		/** Use heigh raycasting normal for max slope calculation.
		 * True if #maxSlope is less than 90 degrees.
		 */
		public bool useRaycastNormal { get { return Math.Abs(90-maxSlope) > float.Epsilon; } }

		/** Erosion of the graph.
		 * The graph can be eroded after calculation.
		 * This means a margin is put around unwalkable nodes or other unwalkable connections.
		 * It is really good if your graph contains ledges where the nodes without erosion are walkable too close to the edge.
		 *
		 * Below is an image showing a graph with erode iterations 0, 1 and 2
		 * \shadowimage{erosion.png}
		 *
		 * \note A high number of erode iterations can seriously slow down graph updates during runtime (GraphUpdateObject)
		 * and should be kept as low as possible.
		 * \see erosionUseTags
		 */
		[JsonMember]
		public int erodeIterations;

		/** Use tags instead of walkability for erosion.
		 * Tags will be used for erosion instead of marking nodes as unwalkable. The nodes will be marked with tags in an increasing order starting with the tag #erosionFirstTag.
		 * Debug with the Tags mode to see the effect. With this enabled you can in effect set how close different AIs are allowed to get to walls using the Valid Tags field on the Seeker component.
		 * \shadowimage{erosionTags.png}
		 * \shadowimage{erosionTags2.png}
		 * \see erosionFirstTag
		 */
		[JsonMember]
		public bool erosionUseTags;

		/** Tag to start from when using tags for erosion.
		 * \see #erosionUseTags
		 * \see #erodeIterations
		 */
		[JsonMember]
		public int erosionFirstTag = 1;

		/**
		 * Auto link the graph's edge nodes together with other GridGraphs in the scene on Scan.
		 * \warning This feature is experimental and it is currently disabled.
		 *
		 * \see #autoLinkDistLimit */
		[JsonMember]
		public bool autoLinkGrids;

		/**
		 * Distance limit for grid graphs to be auto linked.
		 * \warning This feature is experimental and it is currently disabled.
		 *
		 * \see #autoLinkGrids */
		[JsonMember]
		public float autoLinkDistLimit = 10F;

		/** Number of neighbours for each node.
		 * Either four, six, eight connections per node.
		 *
		 * Six connections is primarily for emulating hexagon graphs.
		 */
		[JsonMember]
		public NumNeighbours neighbours = NumNeighbours.Eight;

		/** If disabled, will not cut corners on obstacles.
		 * If \link #neighbours connections \endlink is Eight, obstacle corners might be cut by a connection,
		 * setting this to false disables that. \image html images/cutCorners.png
		 */
		[JsonMember]
		public bool cutCorners = true;

		/** Offset for the position when calculating penalty.
		 * \see penaltyPosition */
		[JsonMember]
		public float penaltyPositionOffset;

		/** Use position (y-coordinate) to calculate penalty */
		[JsonMember]
		public bool penaltyPosition;

		/** Scale factor for penalty when calculating from position.
		 * \see penaltyPosition
		 */
		[JsonMember]
		public float penaltyPositionFactor = 1F;

		[JsonMember]
		public bool penaltyAngle;

		/** How much penalty is applied depending on the slope of the terrain.
		 * At a 90 degree slope (not that exactly 90 degree slopes can occur, but almost 90 degree), this penalty is applied.
		 * At a 45 degree slope, half of this is applied and so on.
		 * Note that you may require very large values, a value of 1000 is equivalent to the cost of moving 1 world unit.
		 */
		[JsonMember]
		public float penaltyAngleFactor = 100F;

		/** How much extra to penalize very steep angles */
		[JsonMember]
		public float penaltyAnglePower = 1;

		[JsonMember]
		public bool useJumpPointSearch;

		/** Holds settings for using a texture as source for a grid graph.
		 * Texure data can be used for fine grained control over how the graph will look.
		 * It can be used for positioning, penalty and walkability control.\n
		 * Below is a screenshot of a grid graph with a penalty map applied.
		 * It has the effect of the AI taking the longer path along the green (low penalty) areas.\n
		 * \shadowimage{penaltymap.png}
		 * Color data is got as 0...255 values.
		 * \astarpro
		 * \warning Can only be used with Unity 3.4 and up */
		[JsonMember]
		public TextureData textureData = new TextureData();

		/** \} */

		/** Size of the grid. Will always be positive and larger than #nodeSize.
		 * \see #GenerateMatrix
		 */
		public Vector2 size { get; protected set; }

		/* End collision and stuff */

		/** Index offset to get neighbour nodes. Added to a node's index to get a neighbour node index.
		 *
		 * \code
		 *         Z
		 *         |
		 *         |
		 *
		 *      6  2  5
		 *       \ | /
		 * --  3 - X - 1  ----- X
		 *       / | \
		 *      7  0  4
		 *
		 *         |
		 *         |
		 * \endcode
		 */
		[System.NonSerialized]
		public readonly int[] neighbourOffsets = new int[8];

		/** Costs to neighbour nodes */
		[System.NonSerialized]
		public readonly uint[] neighbourCosts = new uint[8];

		/** Offsets in the X direction for neighbour nodes. Only 1, 0 or -1 */
		[System.NonSerialized]
		public readonly int[] neighbourXOffsets = new int[8];

		/** Offsets in the Z direction for neighbour nodes. Only 1, 0 or -1 */
		[System.NonSerialized]
		public readonly int[] neighbourZOffsets = new int[8];

		/** Which neighbours are going to be used when #neighbours=6 */
		internal static readonly int[] hexagonNeighbourIndices = { 0, 1, 2, 3, 5, 7 };

		/** In GetNearestForce, determines how far to search after a valid node has been found */
		public const int getNearestForceOverlap = 2;

		public Matrix4x4 boundsMatrix { get; protected set; }

		/** All nodes in this graph.
		 * Nodes are laid out row by row.
		 *
		 * The first node has grid coordinates X=0, Z=0, the second one X=1, Z=0\n
		 * the last one has grid coordinates X=width-1, Z=depth-1.
		 *
		 * \see GetNodes
		 */
		public GridNode[] nodes;

		/** Used for using a texture as a source for a grid graph.
		 * \astarpro
		 */
		public class TextureData {
			public bool enabled;

			public Texture2D source;
			public float[] factors = new float[3];
			public ChannelUse[] channels = new ChannelUse[3];

			Color32[] data;

			/** Reads texture data */
			public void Initialize () {
				if (enabled && source != null) {
					for (int i = 0; i < channels.Length; i++) {
						if (channels[i] != ChannelUse.None) {
							try {
								data = source.GetPixels32();
							} catch (UnityException e) {
								Debug.LogWarning(e.ToString());
								data = null;
							}
							break;
						}
					}
				}
			}

			/** Applies the texture to the node */
			public void Apply (GridNode node, int x, int z) {
				if (enabled && data != null && x < source.width && z < source.height) {
					Color32 col = data[z*source.width+x];

					if (channels[0] != ChannelUse.None) {
						ApplyChannel(node, x, z, col.r, channels[0], factors[0]);
					}

					if (channels[1] != ChannelUse.None) {
						ApplyChannel(node, x, z, col.g, channels[1], factors[1]);
					}

					if (channels[2] != ChannelUse.None) {
						ApplyChannel(node, x, z, col.b, channels[2], factors[2]);
					}
				}
			}

			/** Applies a value to the node using the specified ChannelUse */
			void ApplyChannel (GridNode node, int x, int z, int value, ChannelUse channelUse, float factor) {
				switch (channelUse) {
				case ChannelUse.Penalty:
					node.Penalty += (uint)Mathf.RoundToInt(value*factor);
					break;
				case ChannelUse.Position:
					node.position = GridNode.GetGridGraph(node.GraphIndex).GraphPointToWorld(x, z, value);
					break;
				case ChannelUse.WalkablePenalty:
					if (value == 0) {
						node.Walkable = false;
					} else {
						node.Penalty += (uint)Mathf.RoundToInt((value-1)*factor);
					}
					break;
				}
			}

			public enum ChannelUse {
				None,
				Penalty,
				Position,
				WalkablePenalty,
			}
		}

		public GridGraph () {
			unclampedSize = new Vector2(10, 10);
			nodeSize = 1F;
			collision = new GraphCollision();
		}

		/** Relocate the grid graph using new settings.
		 * This will move all nodes in the graph to new positions which matches the new settings.
		 *
		 * \warning This method is lossy, so calling it many times may cause node positions to lose precision.
		 * For example if you set the nodeSize to 0 in one call, and then to 1 in the next call, it will not be able to
		 * recover the correct positions since when the nodeSize was 0, all nodes were scaled/moved to the same point.
		 * The same thing happens for other - less extreme - values as well, but to a lesser degree.
		 *
		 */
		public void RelocateNodes (Vector3 center, Quaternion rotation, float nodeSize, float aspectRatio = 1, float isometricAngle = 0) {
			var omatrix = matrix;

			this.center = center;
			this.rotation = rotation.eulerAngles;
			this.nodeSize = nodeSize;
			this.aspectRatio = aspectRatio;
			this.isometricAngle = isometricAngle;

			UpdateSizeFromWidthDepth();

			RelocateNodes(omatrix, matrix);
		}

		/** Transform a point in graph space to world space.
		 * This will give you the node position for the node at the given x and z coordinate
		 * if it is at the specified height above the base of the graph.
		 */
		public Int3 GraphPointToWorld (int x, int z, float height) {
			return (Int3)matrix.MultiplyPoint3x4(new Vector3(x+0.5f, height, z+0.5f));
		}

		public int Width {
			get {
				return width;
			}
			set {
				width = value;
			}
		}
		public int Depth {
			get {
				return depth;
			}
			set {
				depth = value;
			}
		}

		public uint GetConnectionCost (int dir) {
			return neighbourCosts[dir];
		}

		public GridNode GetNodeConnection (GridNode node, int dir) {
			if (!node.GetConnectionInternal(dir)) return null;
			if (!node.EdgeNode) {
				return nodes[node.NodeInGridIndex + neighbourOffsets[dir]];
			} else {
				int index = node.NodeInGridIndex;
				//int z = Math.DivRem (index,Width, out x);
				int z = index/Width;
				int x = index - z*Width;

				return GetNodeConnection(index, x, z, dir);
			}
		}

		public bool HasNodeConnection (GridNode node, int dir) {
			if (!node.GetConnectionInternal(dir)) return false;
			if (!node.EdgeNode) {
				return true;
			} else {
				int index = node.NodeInGridIndex;
				int z = index/Width;
				int x = index - z*Width;

				return HasNodeConnection(index, x, z, dir);
			}
		}

		public void SetNodeConnection (GridNode node, int dir, bool value) {
			int index = node.NodeInGridIndex;
			int z = index/Width;
			int x = index - z*Width;

			SetNodeConnection(index, x, z, dir, value);
		}

		/** Get the connecting node from the node at (x,z) in the specified direction.
		 * \returns A GridNode if the node has a connection to that node. Null if no connection in that direction exists
		 *
		 * \see GridNode
		 */
		private GridNode GetNodeConnection (int index, int x, int z, int dir) {
			if (!nodes[index].GetConnectionInternal(dir)) return null;

			/** \todo Mark edge nodes and only do bounds checking for them */
			int nx = x + neighbourXOffsets[dir];
			if (nx < 0 || nx >= Width) return null; /** \todo Modify to get adjacent grid graph here */
			int nz = z + neighbourZOffsets[dir];
			if (nz < 0 || nz >= Depth) return null;
			int nindex = index + neighbourOffsets[dir];

			return nodes[nindex];
		}

		/** Set if connection in the specified direction should be enabled.
		 * Note that bounds checking will still be done when getting the connection value again,
		 * so it is not necessarily true that HasNodeConnection will return true just because you used
		 * SetNodeConnection on a node to set a connection to true.
		 *
		 * \param index Index of the node
		 * \param x X coordinate of the node
		 * \param z Z coordinate of the node
		 * \param value Enable or disable the connection
		 *
		 * \note This is identical to Pathfinding.Node.SetConnectionInternal
		 *
		 * \deprecated
		 */
		public void SetNodeConnection (int index, int x, int z, int dir, bool value) {
			nodes[index].SetConnectionInternal(dir, value);
		}

		public bool HasNodeConnection (int index, int x, int z, int dir) {
			if (!nodes[index].GetConnectionInternal(dir)) return false;

			/** \todo Mark edge nodes and only do bounds checking for them */
			int nx = x + neighbourXOffsets[dir];
			if (nx < 0 || nx >= Width) return false; /** \todo Modify to get adjacent grid graph here */
			int nz = z + neighbourZOffsets[dir];
			if (nz < 0 || nz >= Depth) return false;

			return true;
		}

		/** Updates #size from #width, #depth and #nodeSize values. Also \link GenerateMatrix generates a new matrix \endlink.
		 * \note This does not rescan the graph, that must be done with Scan */
		public void UpdateSizeFromWidthDepth () {
			unclampedSize = new Vector2(width, depth)*nodeSize;
			GenerateMatrix();
		}

		/** Generates the matrix used for translating nodes from grid coordinates to world coordintes. */
		public void GenerateMatrix () {
			var newSize = unclampedSize;

			// Make sure size is positive
			newSize.x *= Mathf.Sign(newSize.x);
			newSize.y *= Mathf.Sign(newSize.y);

			// Clamp the nodeSize so that the graph is never larger than 1024*1024
			nodeSize = Mathf.Clamp(nodeSize, newSize.x/1024F, Mathf.Infinity);
			nodeSize = Mathf.Clamp(nodeSize, newSize.y/1024F, Mathf.Infinity);

			// Prevent the graph to become smaller than a single node
			newSize.x = newSize.x < nodeSize ? nodeSize : newSize.x;
			newSize.y = newSize.y < nodeSize ? nodeSize : newSize.y;

			size = newSize;

			// Generate a matrix which shrinks the graph along one of the diagonals
			// corresponding to the isometricAngle
			var isometricMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 45, 0), Vector3.one);
			isometricMatrix = Matrix4x4.Scale(new Vector3(Mathf.Cos(Mathf.Deg2Rad*isometricAngle), 1, 1)) * isometricMatrix;
			isometricMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, -45, 0), Vector3.one) * isometricMatrix;

			// Generate a matrix for the bounds of the graph
			// This moves a point to the correct offset in the world and the correct rotation and the aspect ratio and isometric angle is taken into account
			// The unit is still world units however
			boundsMatrix = Matrix4x4.TRS(center, Quaternion.Euler(rotation), new Vector3(aspectRatio, 1, 1)) * isometricMatrix;

			// Calculate the number of nodes along each side
			width = Mathf.FloorToInt(size.x / nodeSize);
			depth = Mathf.FloorToInt(size.y / nodeSize);

			// Take care of numerical edge cases
			if (Mathf.Approximately(size.x / nodeSize, Mathf.CeilToInt(size.x / nodeSize))) {
				width = Mathf.CeilToInt(size.x / nodeSize);
			}

			if (Mathf.Approximately(size.y / nodeSize, Mathf.CeilToInt(size.y / nodeSize))) {
				depth = Mathf.CeilToInt(size.y / nodeSize);
			}

			// Generate a matrix where Vector3.zero is the corner of the graph instead of the center
			// The unit is nodes here (so (0.5,0,0.5) is the position of the first node and (1.5,0,0.5) is the position of the second node)
			// 0.5 is added since this is the node center, not its corner. In graph space a node has a size of 1
			var m = Matrix4x4.TRS(boundsMatrix.MultiplyPoint3x4(-new Vector3(size.x, 0, size.y)*0.5F), Quaternion.Euler(rotation), new Vector3(nodeSize*aspectRatio, 1, nodeSize)) * isometricMatrix;

			// Set the matrix of the graph
			// This will also set inverseMatrix
			SetMatrix(m);
		}

		public override NNInfo GetNearest (Vector3 position, NNConstraint constraint, GraphNode hint) {
			if (nodes == null || depth*width != nodes.Length) {
				return new NNInfo();
			}

			// Calculate the closest node and the closest point on that node
			position = inverseMatrix.MultiplyPoint3x4(position);

			float xf = position.x-0.5F;
			float zf = position.z-0.5f;
			int x = Mathf.Clamp(Mathf.RoundToInt(xf), 0, width-1);
			int z = Mathf.Clamp(Mathf.RoundToInt(zf), 0, depth-1);

			var nn = new NNInfo(nodes[z*width+x]);

			float y = inverseMatrix.MultiplyPoint3x4((Vector3)nodes[z*width+x].position).y;
			nn.clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(xf, x-0.5f, x+0.5f)+0.5f, y, Mathf.Clamp(zf, z-0.5f, z+0.5f)+0.5f));

			return nn;
		}

		public override NNInfo GetNearestForce (Vector3 position, NNConstraint constraint) {
			if (nodes == null || depth*width != nodes.Length) {
				return new NNInfo();
			}

			// Position in global space
			Vector3 globalPosition = position;

			// Position in graph space
			position = inverseMatrix.MultiplyPoint3x4(position);

			// Find the coordinates of the closest node
			float xf = position.x-0.5F;
			float zf = position.z-0.5f;
			int x = Mathf.Clamp(Mathf.RoundToInt(xf), 0, width-1);
			int z = Mathf.Clamp(Mathf.RoundToInt(zf), 0, depth-1);

			// Closest node
			GridNode node = nodes[x+z*width];

			GridNode minNode = null;
			float minDist = float.PositiveInfinity;
			int overlap = getNearestForceOverlap;

			Vector3 clampedPosition = Vector3.zero;
			var nn = new NNInfo(null);

			// If the closest node was suitable
			if (constraint.Suitable(node)) {
				minNode = node;
				minDist = ((Vector3)minNode.position-globalPosition).sqrMagnitude;
				float y = inverseMatrix.MultiplyPoint3x4((Vector3)node.position).y;
				clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(xf, x-0.5f, x+0.5f)+0.5f, y, Mathf.Clamp(zf, z-0.5f, z+0.5f)+0.5f));
			}

			if (minNode != null) {
				nn.node = minNode;
				nn.clampedPosition = clampedPosition;

				// We have a node, and we don't need to search more, so just return
				if (overlap == 0) return nn;
				overlap--;
			}

			// Search up to this distance
			float maxDist = constraint.constrainDistance ? AstarPath.active.maxNearestNodeDistance : float.PositiveInfinity;
			float maxDistSqr = maxDist*maxDist;

			// Search a square/spiral pattern around the point
			for (int w = 1;; w++) {
				//Check if the nodes are within distance limit
				if (nodeSize*w > maxDist) {
					nn.node = minNode;
					nn.clampedPosition = clampedPosition;
					return nn;
				}

				bool anyInside = false;

				int nx;
				int nz = z+w;
				int nz2 = nz*width;

				// Side 1 on the square
				for (nx = x-w; nx <= x+w; nx++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					anyInside = true;
					if (constraint.Suitable(nodes[nx+nz2])) {
						float dist = ((Vector3)nodes[nx+nz2].position-globalPosition).sqrMagnitude;
						if (dist < minDist && dist < maxDistSqr) {
							// Minimum distance so far
							minDist = dist;
							minNode = nodes[nx+nz2];

							// Closest point on the node if the node is treated as a square
							clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(xf, nx-0.5f, nx+0.5f)+0.5f, inverseMatrix.MultiplyPoint3x4((Vector3)minNode.position).y, Mathf.Clamp(zf, nz-0.5f, nz+0.5f)+0.5f));
						}
					}
				}

				nz = z-w;
				nz2 = nz*width;

				// Side 2 on the square
				for (nx = x-w; nx <= x+w; nx++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					anyInside = true;
					if (constraint.Suitable(nodes[nx+nz2])) {
						float dist = ((Vector3)nodes[nx+nz2].position-globalPosition).sqrMagnitude;
						if (dist < minDist && dist < maxDistSqr) {
							minDist = dist;
							minNode = nodes[nx+nz2];
							clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(xf, nx-0.5f, nx+0.5f)+0.5f, inverseMatrix.MultiplyPoint3x4((Vector3)minNode.position).y, Mathf.Clamp(zf, nz-0.5f, nz+0.5f)+0.5f));
						}
					}
				}

				nx = x-w;

				// Side 3 on the square
				for (nz = z-w+1; nz <= z+w-1; nz++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					anyInside = true;
					if (constraint.Suitable(nodes[nx+nz*width])) {
						float dist = ((Vector3)nodes[nx+nz*width].position-globalPosition).sqrMagnitude;
						if (dist < minDist && dist < maxDistSqr) {
							minDist = dist;
							minNode = nodes[nx+nz*width];
							clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(xf, nx-0.5f, nx+0.5f)+0.5f, inverseMatrix.MultiplyPoint3x4((Vector3)minNode.position).y, Mathf.Clamp(zf, nz-0.5f, nz+0.5f)+0.5f));
						}
					}
				}

				nx = x+w;

				// Side 4 on the square
				for (nz = z-w+1; nz <= z+w-1; nz++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					anyInside = true;
					if (constraint.Suitable(nodes[nx+nz*width])) {
						float dist = ((Vector3)nodes[nx+nz*width].position-globalPosition).sqrMagnitude;
						if (dist < minDist && dist < maxDistSqr) {
							minDist = dist;
							minNode = nodes[nx+nz*width];
							clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(xf, nx-0.5f, nx+0.5f)+0.5f, inverseMatrix.MultiplyPoint3x4((Vector3)minNode.position).y, Mathf.Clamp(zf, nz-0.5f, nz+0.5f)+0.5f));
						}
					}
				}

				// We found a suitable node
				if (minNode != null) {
					// If we don't need to search more, just return
					// Otherwise search for 'overlap' iterations more
					if (overlap == 0) {
						nn.node = minNode;
						nn.clampedPosition = clampedPosition;
						return nn;
					}
					overlap--;
				}

				// No nodes were inside grid bounds
				// We will not be able to find any more valid nodes
				// so just return
				if (!anyInside) {
					nn.node = minNode;
					nn.clampedPosition = clampedPosition;
					return nn;
				}
			}
		}

		/** Sets up #neighbourOffsets with the current settings. #neighbourOffsets, #neighbourCosts, #neighbourXOffsets and #neighbourZOffsets are set up.\n
		 * The cost for a non-diagonal movement between two adjacent nodes is RoundToInt (#nodeSize * Int3.Precision)\n
		 * The cost for a diagonal movement between two adjacent nodes is RoundToInt (#nodeSize * Sqrt (2) * Int3.Precision)
		 */
		public virtual void SetUpOffsetsAndCosts () {
#if ASTARDEBUG
			Debug.Log("+++ --- GridGraph Setting Up Offsets and Costs");
#endif

			//First 4 are for the four directly adjacent nodes the last 4 are for the diagonals
			neighbourOffsets[0] = -width;
			neighbourOffsets[1] = 1;
			neighbourOffsets[2] = width;
			neighbourOffsets[3] = -1;
			neighbourOffsets[4] = -width+1;
			neighbourOffsets[5] = width+1;
			neighbourOffsets[6] = width-1;
			neighbourOffsets[7] = -width-1;

			uint straightCost = (uint)Mathf.RoundToInt(nodeSize*Int3.Precision);

			// Diagonals normally cost sqrt(2) (approx 1.41) times more
			uint diagonalCost = uniformEdgeCosts ? straightCost : (uint)Mathf.RoundToInt(nodeSize*Mathf.Sqrt(2F)*Int3.Precision);

			neighbourCosts[0] = straightCost;
			neighbourCosts[1] = straightCost;
			neighbourCosts[2] = straightCost;
			neighbourCosts[3] = straightCost;
			neighbourCosts[4] = diagonalCost;
			neighbourCosts[5] = diagonalCost;
			neighbourCosts[6] = diagonalCost;
			neighbourCosts[7] = diagonalCost;

			/*         Z
			 *         |
			 *         |
			 *
			 *      6  2  5
			 *       \ | /
			 * --  3 - X - 1  ----- X
			 *       / | \
			 *      7  0  4
			 *
			 *         |
			 *         |
			 */

			neighbourXOffsets[0] = 0;
			neighbourXOffsets[1] = 1;
			neighbourXOffsets[2] = 0;
			neighbourXOffsets[3] = -1;
			neighbourXOffsets[4] = 1;
			neighbourXOffsets[5] = 1;
			neighbourXOffsets[6] = -1;
			neighbourXOffsets[7] = -1;

			neighbourZOffsets[0] = -1;
			neighbourZOffsets[1] =  0;
			neighbourZOffsets[2] =  1;
			neighbourZOffsets[3] =  0;
			neighbourZOffsets[4] = -1;
			neighbourZOffsets[5] =  1;
			neighbourZOffsets[6] =  1;
			neighbourZOffsets[7] = -1;
		}

		public override void ScanInternal (OnScanStatus statusCallback) {
			AstarPath.OnPostScan += new OnScanDelegate(OnPostScan);

			if (nodeSize <= 0) {
				return;
			}

			// Make sure the matrix is up to date
			GenerateMatrix();

			if (width > 1024 || depth > 1024) {
				Debug.LogError("One of the grid's sides is longer than 1024 nodes");
				return;
			}

#if !ASTAR_JPS
			if (this.useJumpPointSearch) {
				Debug.LogError("Trying to use Jump Point Search, but support for it is not enabled. Please enable it in the inspector (Grid Graph settings).");
			}
#endif

			SetUpOffsetsAndCosts();

			// Get the graph index of this graph
			int graphIndex = AstarPath.active.astarData.GetGraphIndex(this);

			// Set a global reference to this graph so that nodes can find it
			GridNode.SetGridGraph(graphIndex, this);

			// Create all nodes
			nodes = new GridNode[width*depth];
			for (int i = 0; i < nodes.Length; i++) {
				nodes[i] = new GridNode(active);
				nodes[i].GraphIndex = (uint)graphIndex;
			}

			// Create and initialize the collision class
			if (collision == null) {
				collision = new GraphCollision();
			}
			collision.Initialize(matrix, nodeSize);

			textureData.Initialize();

			for (int z = 0; z < depth; z++) {
				for (int x = 0; x < width; x++) {
					var node = nodes[z*width+x];

					node.NodeInGridIndex = z*width+x;

					// Updates the position of the node
					// and a bunch of other things
					UpdateNodePositionCollision(node, x, z);

					// Apply texture data if necessary
					textureData.Apply(node, x, z);
				}
			}

			for (int z = 0; z < depth; z++) {
				for (int x = 0; x < width; x++) {
					var node = nodes[z*width + x];
					// Recalculate connections to other nodes
					CalculateConnections(x, z, node);
				}
			}

			// Apply erosion
			ErodeWalkableArea();
		}

		/** Updates position, walkability and penalty for the node.
		 * Assumes that collision.Initialize (...) has been called before this function */
		public virtual void UpdateNodePositionCollision (GridNode node, int x, int z, bool resetPenalty = true) {
			// Set the node's initial position with a y-offset of zero
			node.position = GraphPointToWorld(x, z, 0);

			RaycastHit hit;

			bool walkable;

			// Calculate the actual position using physics raycasting (if enabled)
			// walkable will be set to false if no ground was found (unless that setting has been disabled)
			Vector3 position = collision.CheckHeight((Vector3)node.position, out hit, out walkable);
			node.position = (Int3)position;

			if (resetPenalty) {
				node.Penalty = initialPenalty;

				// Calculate a penalty based on the y coordinate of the node
				if (penaltyPosition) {
					node.Penalty += (uint)Mathf.RoundToInt((node.position.y-penaltyPositionOffset)*penaltyPositionFactor);
				}
			}

			// Check if the node is on a slope steeper than permitted
			if (walkable && useRaycastNormal && collision.heightCheck) {
				if (hit.normal != Vector3.zero) {
					// Take the dot product to find out the cosinus of the angle it has (faster than Vector3.Angle)
					float angle = Vector3.Dot(hit.normal.normalized, collision.up);

					// Add penalty based on normal
					if (penaltyAngle && resetPenalty) {
						node.Penalty += (uint)Mathf.RoundToInt((1F-Mathf.Pow(angle, penaltyAnglePower))*penaltyAngleFactor);
					}

					// Cosinus of the max slope
					float cosAngle = Mathf.Cos(maxSlope*Mathf.Deg2Rad);

					// Check if the ground is flat enough to stand on
					if (angle < cosAngle) {
						walkable = false;
					}
				}
			}

			// If the walkable flag has already been set to false, there is no point in checking for it again
			// Check for obstacles
			node.Walkable = walkable && collision.Check((Vector3)node.position);

			// Store walkability before erosion is applied
			// Used for graph updating
			node.WalkableErosion = node.Walkable;
		}

		/** Erodes the walkable area.
		 * \see #erodeIterations
		 */
		public virtual void ErodeWalkableArea () {
			ErodeWalkableArea(0, 0, Width, Depth);
		}

		/** True if the node has any blocked connections.
		 * For 4 and 8 neighbours the 4 axis aligned connections will be checked.
		 * For 6 neighbours all 6 neighbours will be checked.
		 */
		bool ErosionAnyFalseConnections (GridNode node) {
			if (neighbours == NumNeighbours.Six) {
				// Check the 6 hexagonal connections
				for (int i = 0; i < 6; i++) {
					if (!HasNodeConnection(node, hexagonNeighbourIndices[i])) {
						return true;
					}
				}
			} else {
				// Check the four axis aligned connections
				for (int i = 0; i < 4; i++) {
					if (!HasNodeConnection(node, i)) {
						return true;
					}
				}
			}

			return false;
		}

		/** Erodes the walkable area.
		 *
		 * xmin, zmin (inclusive)\n
		 * xmax, zmax (exclusive)
		 *
		 * \see #erodeIterations */
		public virtual void ErodeWalkableArea (int xmin, int zmin, int xmax, int zmax) {
			// Clamp values to grid
			xmin = Mathf.Clamp(xmin, 0, Width);
			xmax = Mathf.Clamp(xmax, 0, Width);
			zmin = Mathf.Clamp(zmin, 0, Depth);
			zmax = Mathf.Clamp(zmax, 0, Depth);

			if (!erosionUseTags) {
				for (int it = 0; it < erodeIterations; it++) {
					// Loop through all nodes
					// and mark as unwalkble the nodes which
					// have at least one blocked connection
					// to another node
					for (int z = zmin; z < zmax; z++) {
						for (int x = xmin; x < xmax; x++) {
							var node = nodes[z*Width+x];

							if (node.Walkable) {
								if (ErosionAnyFalseConnections(node)) {
									node.Walkable = false;
								}
							}
						}
					}

					//Recalculate connections
					for (int z = zmin; z < zmax; z++) {
						for (int x = xmin; x < xmax; x++) {
							GridNode node = nodes[z*Width+x];
							CalculateConnections(x, z, node);
						}
					}
				}
			} else {
				if (erodeIterations+erosionFirstTag > 31) {
					Debug.LogError("Too few tags available for "+erodeIterations+" erode iterations and starting with tag " + erosionFirstTag + " (erodeIterations+erosionFirstTag > 31)");
					return;
				}
				if (erosionFirstTag <= 0) {
					Debug.LogError("First erosion tag must be greater or equal to 1");
					return;
				}

				for (int it = 0; it < erodeIterations; it++) {
					for (int z = zmin; z < zmax; z++) {
						for (int x = xmin; x < xmax; x++) {
							var node = nodes[z*width+x];

							if (node.Walkable && node.Tag >= erosionFirstTag && node.Tag < erosionFirstTag + it) {
								if (neighbours == NumNeighbours.Six) {
									// Check the 6 hexagonal connections
									for (int i = 0; i < 6; i++) {
										GridNode other = GetNodeConnection(node, hexagonNeighbourIndices[i]);
										if (other != null) {
											uint tag = other.Tag;
											if (tag > erosionFirstTag + it || tag < erosionFirstTag) {
												other.Tag = (uint)(erosionFirstTag+it);
											}
										}
									}
								} else {
									// Check the four axis aligned connections
									for (int i = 0; i < 4; i++) {
										GridNode other = GetNodeConnection(node, i);
										if (other != null) {
											uint tag = other.Tag;
											if (tag > erosionFirstTag + it || tag < erosionFirstTag) {
												other.Tag = (uint)(erosionFirstTag+it);
											}
										}
									}
								}
							} else if (node.Walkable && it == 0) {
								if (ErosionAnyFalseConnections(node)) {
									node.Tag = (uint)(erosionFirstTag+it);
								}
							}
						}
					}
				}
			}
		}

		/** Returns true if a connection between the adjacent nodes \a n1 and \a n2 is valid.
		 * Also takes into account if the nodes are walkable.
		 *
		 * This method may be overriden if you want to customize what connections are valid.
		 * It must however hold that IsValidConnection(a,b) == IsValidConnection(b,a).
		 *
		 * This is used for calculating the connections when the graph is scanned or updated.
		 *
		 * \see CalculateConnections
		 */
		public virtual bool IsValidConnection (GridNode n1, GridNode n2) {
			if (!n1.Walkable || !n2.Walkable) {
				return false;
			}

			return maxClimb <= 0 || System.Math.Abs(n1.position[maxClimbAxis] - n2.position[maxClimbAxis]) <= maxClimb*Int3.Precision;
		}

		/** Calculates the grid connections for a single node.
		 * Convenience function, it's faster to use CalculateConnections(int,int,GridNode)
		 * but that will only show when calculating for a large number of nodes.
		 * \todo Test this function, should work ok, but you never know
		 */
		public static void CalculateConnections (GridNode node) {
			var gg = AstarData.GetGraph(node) as GridGraph;

			if (gg != null) {
				int index = node.NodeInGridIndex;
				int x = index % gg.width;
				int z = index / gg.width;
				gg.CalculateConnections(x, z, node);
			}
		}

		/** Calculates the grid connections for a single node.
		 * \deprecated CalculateConnections no longer takes a node array, it just uses the one on the graph
		 */
		[System.Obsolete("CalculateConnections no longer takes a node array, it just uses the one on the graph")]
		public virtual void CalculateConnections (GridNode[] nodes, int x, int z, GridNode node) {
			CalculateConnections(x, z, node);
		}

		/** Calculates the grid connections for a single node.
		 * The x and z parameters are assumed to be the grid coordinates of the node.
		 *
		 * \see CalculateConnections(GridNode)
		 */
		public virtual void CalculateConnections (int x, int z, GridNode node) {
			// All connections are disabled if the node is not walkable
			if (!node.Walkable) {
				// Reset all connections
				// This makes the node have NO connections to any neighbour nodes
				node.ResetConnectionsInternal();
				return;
			}

			// Internal index of where in the graph the node is
			int index = node.NodeInGridIndex;

			if (neighbours == NumNeighbours.Four || neighbours == NumNeighbours.Eight) {
				// Bitpacked connections
				// bit 0 is set if connection 0 is enabled
				// bit 1 is set if connection 1 is enabled etc.
				int conns = 0;

				// Loop through axis aligned neighbours (down, right, up, left) or (-Z, +X, +Z, -X)
				for (int i = 0; i < 4; i++) {
					int nx = x + neighbourXOffsets[i];
					int nz = z + neighbourZOffsets[i];

					// Check if the new position is inside the grid
					// Bitwise AND (&) is measurably faster than &&
					// (not much, but this code is hot)
					if (nx >= 0 & nz >= 0 & nx < width & nz < depth) {
						var other = nodes[index+neighbourOffsets[i]];

						if (IsValidConnection(node, other)) {
							// Enable connection i
							conns |= 1 << i;
						}
					}
				}

				// Bitpacked diagonal connections
				int diagConns = 0;

				// Add in the diagonal connections
				if (neighbours == NumNeighbours.Eight) {
					if (cutCorners) {
						for (int i = 0; i < 4; i++) {
							// If at least one axis aligned connection
							// is adjacent to this diagonal, then we can add a connection.
							// Bitshifting is a lot faster than calling node.GetConnectionInternal.
							// We need to check if connection i and i+1 are enabled
							// but i+1 may overflow 4 and in that case need to be wrapped around
							// (so 3+1 = 4 goes to 0). We do that by checking both connection i+1
							// and i+1-4 at the same time. Either i+1 or i+1-4 will be in the range
							// from 0 to 4 (exclusive)
							if (((conns >> i | conns >> (i+1) | conns >> (i+1-4)) & 1) != 0) {
								int directionIndex = i+4;

								int nx = x + neighbourXOffsets[directionIndex];
								int nz = z + neighbourZOffsets[directionIndex];

								if (nx >= 0 & nz >= 0 & nx < width & nz < depth) {
									GridNode other = nodes[index+neighbourOffsets[directionIndex]];

									if (IsValidConnection(node, other)) {
										diagConns |= 1 << directionIndex;
									}
								}
							}
						}
					} else {
						for (int i = 0; i < 4; i++) {
							// If exactly 2 axis aligned connections is adjacent to this connection
							// then we can add the connection
							// We don't need to check if it is out of bounds because if both of
							// the other neighbours are inside the bounds this one must be too
							if ((conns >> i & 1) != 0 && ((conns >> (i+1) | conns >> (i+1-4)) & 1) != 0) {
								GridNode other = nodes[index+neighbourOffsets[i+4]];

								if (IsValidConnection(node, other)) {
									diagConns |= 1 << (i+4);
								}
							}
						}
					}
				}

				// Set all connections at the same time
				node.SetAllConnectionInternal(conns | diagConns);
			} else {
				// Hexagon layout

				// Reset all connections
				// This makes the node have NO connections to any neighbour nodes
				node.ResetConnectionsInternal();

				// Loop through all possible neighbours and try to connect to them
				for (int j = 0; j < hexagonNeighbourIndices.Length; j++) {
					var i = hexagonNeighbourIndices[j];

					int nx = x + neighbourXOffsets[i];
					int nz = z + neighbourZOffsets[i];

					if (nx >= 0 & nz >= 0 & nx < width & nz < depth) {
						var other = nodes[index+neighbourOffsets[i]];
						node.SetConnectionInternal(i, IsValidConnection(node, other));
					}
				}
			}
		}

		/** Auto links grid graphs together. Called after all graphs have been scanned.
		 * \see autoLinkGrids
		 */
		public void OnPostScan (AstarPath script) {
			AstarPath.OnPostScan -= new OnScanDelegate(OnPostScan);

			if (!autoLinkGrids || autoLinkDistLimit <= 0) {
				return;
			}

			//Link to other grids

			throw new System.NotSupportedException();

#if FALSE
			int maxCost = Mathf.RoundToInt(autoLinkDistLimit * Int3.Precision);

			//Loop through all GridGraphs
			foreach (GridGraph gg in script.astarData.FindGraphsOfType(typeof(GridGraph))) {
				if (gg == this || gg.nodes == null || nodes == null) {
					continue;
				}

				//Int3 prevPos = gg.GetNearest (nodes[0]).position;

				//Z = 0
				for (int x = 0; x < width; x++) {
					GraphNode node1 = nodes[x];
					GraphNode node2 = gg.GetNearest((Vector3)node1.Position).node;

					Vector3 pos = inverseMatrix.MultiplyPoint3x4((Vector3)node2.Position);

					if (pos.z > 0) {
						continue;
					}

					int cost = (node1.Position-node2.Position).costMagnitude;

					if (cost > maxCost) {
						continue;
					}

					node1.AddConnection(node2, cost);
					node2.AddConnection(node1, cost);
				}

				//X = 0
				for (int z = 0; z < depth; z++) {
					GraphNode node1 = nodes[z*width];
					GraphNode node2 = gg.GetNearest((Vector3)node1.Position).node;

					Vector3 pos = inverseMatrix.MultiplyPoint3x4((Vector3)node2.Position);

					if (pos.x > 0) {
						continue;
					}

					int cost = (node1.Position-node2.Position).costMagnitude;

					if (cost > maxCost) {
						continue;
					}

					node1.AddConnection(node2, cost);
					node2.AddConnection(node1, cost);
				}

				//Z = max
				for (int x = 0; x < width; x++) {
					GraphNode node1 = nodes[(depth-1)*width+x];
					GraphNode node2 = gg.GetNearest((Vector3)node1.Position).node;

					Vector3 pos = inverseMatrix.MultiplyPoint3x4((Vector3)node2.Position);

					if (pos.z < depth-1) {
						continue;
					}

					//Debug.DrawLine (node1.position,node2.position,Color.red);
					int cost = (node1.Position-node2.Position).costMagnitude;

					if (cost > maxCost) {
						continue;
					}

					node1.AddConnection(node2, cost);
					node2.AddConnection(node1, cost);
				}

				//X = max
				for (int z = 0; z < depth; z++) {
					GraphNode node1 = nodes[z*width+width-1];
					GraphNode node2 = gg.GetNearest((Vector3)node1.Position).node;

					Vector3 pos = inverseMatrix.MultiplyPoint3x4((Vector3)node2.Position);

					if (pos.x < width-1) {
						continue;
					}

					int cost = (node1.Position-node2.Position).costMagnitude;

					if (cost > maxCost) {
						continue;
					}



					node1.AddConnection(node2, cost);
					node2.AddConnection(node1, cost);
				}
			}
#endif
		}

		public override void OnDrawGizmos (bool drawNodes) {
			Gizmos.matrix = boundsMatrix;
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0, size.y));

			Gizmos.matrix = Matrix4x4.identity;

			if (!drawNodes || nodes == null || nodes.Length != width*depth) {
				return;
			}

			var debugData = AstarPath.active.debugPathData;

			var showSearchTree = AstarPath.active.showSearchTree && debugData != null;

			for (int z = 0; z < depth; z++) {
				for (int x = 0; x < width; x++) {
					var node = nodes[z*width+x];

					// Don't bother drawing unwalkable nodes
					if (!node.Walkable) {
						continue;
					}

					// Calculate which color to use for drawing the node
					// based on the settings specified in the editor
					Gizmos.color = NodeColor(node, debugData);

					// Get the node position
					// Cast it here to avoid doing it for every neighbour
					var pos = (Vector3)node.position;

					if (showSearchTree) {
						if (InSearchTree(node, AstarPath.active.debugPath)) {
							PathNode nodeR = debugData.GetPathNode(node);
							if (nodeR != null && nodeR.parent != null) {
								Gizmos.DrawLine(pos, (Vector3)nodeR.parent.node.position);
							}
						}
					} else {
						// Draw all enabled connections
						for (int i = 0; i < 8; i++) {
							if (node.GetConnectionInternal(i)) {
								// We could use GetNodeConnection, but that does bounds checking and all kinds
								// of things that slow it down. We want gizmo drawing to be fast
								// So just assume that the graph is valid (which it should be anyway)
								GridNode other = nodes[node.NodeInGridIndex + neighbourOffsets[i]];
								Gizmos.DrawLine(pos, (Vector3)other.position);
							}
						}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
						// Draw custom connections
						if (node.connections != null) for (int i = 0; i < node.connections.Length; i++) {
								GraphNode other = node.connections[i];
								Gizmos.DrawLine(pos, (Vector3)other.position);
							}
#endif
					}
				}
			}
		}

		/** Calculates minimum and maximum points for bounds \a b when multiplied with the matrix */
		protected static void GetBoundsMinMax (Bounds b, Matrix4x4 matrix, out Vector3 min, out Vector3 max) {
			var p = new Vector3[8];

			p[0] = matrix.MultiplyPoint3x4(b.center + new Vector3(b.extents.x, b.extents.y, b.extents.z));
			p[1] = matrix.MultiplyPoint3x4(b.center + new Vector3(b.extents.x, b.extents.y, -b.extents.z));
			p[2] = matrix.MultiplyPoint3x4(b.center + new Vector3(b.extents.x, -b.extents.y, b.extents.z));
			p[3] = matrix.MultiplyPoint3x4(b.center + new Vector3(b.extents.x, -b.extents.y, -b.extents.z));
			p[4] = matrix.MultiplyPoint3x4(b.center + new Vector3(-b.extents.x, b.extents.y, b.extents.z));
			p[5] = matrix.MultiplyPoint3x4(b.center + new Vector3(-b.extents.x, b.extents.y, -b.extents.z));
			p[6] = matrix.MultiplyPoint3x4(b.center + new Vector3(-b.extents.x, -b.extents.y, b.extents.z));
			p[7] = matrix.MultiplyPoint3x4(b.center + new Vector3(-b.extents.x, -b.extents.y, -b.extents.z));

			min = p[0];
			max = p[0];
			for (int i = 1; i < 8; i++) {
				min = Vector3.Min(min, p[i]);
				max = Vector3.Max(max, p[i]);
			}
		}

		/** All nodes inside the bounding box.
		 * \note Be nice to the garbage collector and release the list when you have used it (optional)
		 * \see Pathfinding.Util.ListPool
		 *
		 * \see GetNodesInArea(GraphUpdateShape)
		 */
		public List<GraphNode> GetNodesInArea (Bounds b) {
			return GetNodesInArea(b, null);
		}

		/** All nodes inside the shape.
		 * \note Be nice to the garbage collector and release the list when you have used it (optional)
		 * \see Pathfinding.Util.ListPool
		 *
		 * \see GetNodesInArea(Bounds)
		 */
		public List<GraphNode> GetNodesInArea (GraphUpdateShape shape) {
			return GetNodesInArea(shape.GetBounds(), shape);
		}

		/** All nodes inside the shape or if null, the bounding box.
		 * If a shape is supplied, it is assumed to be contained inside the bounding box.
		 * \see GraphUpdateShape.GetBounds
		 */
		private List<GraphNode> GetNodesInArea (Bounds b, GraphUpdateShape shape) {
			if (nodes == null || width*depth != nodes.Length) {
				return null;
			}

			// Get a buffer we can use
			List<GraphNode> inArea = Pathfinding.Util.ListPool<GraphNode>.Claim();

			// Take the bounds and transform it using the matrix
			// Then convert that to a rectangle which contains
			// all nodes that might be inside the bounds
			Vector3 min, max;
			GetBoundsMinMax(b, inverseMatrix, out min, out max);

			int minX = Mathf.RoundToInt(min.x-0.5F);
			int maxX = Mathf.RoundToInt(max.x-0.5F);

			int minZ = Mathf.RoundToInt(min.z-0.5F);
			int maxZ = Mathf.RoundToInt(max.z-0.5F);

			var originalRect = new IntRect(minX, minZ, maxX, maxZ);

			// Rect which covers the whole grid
			var gridRect = new IntRect(0, 0, width-1, depth-1);

			// Clamp the rect to the grid
			var rect = IntRect.Intersection(originalRect, gridRect);

			// Loop through all nodes in the rectangle
			for (int x = rect.xmin; x <= rect.xmax; x++) {
				for (int z = rect.ymin; z <= rect.ymax; z++) {
					int index = z*width+x;

					GraphNode node = nodes[index];

					// If it is contained in the bounds (and optionally the shape)
					// then add it to the buffer
					if (b.Contains((Vector3)node.position) && (shape == null || shape.Contains((Vector3)node.position))) {
						inArea.Add(node);
					}
				}
			}

			return inArea;
		}

		public GraphUpdateThreading CanUpdateAsync (GraphUpdateObject o) {
			return GraphUpdateThreading.UnityThread;
		}

		public void UpdateAreaInit (GraphUpdateObject o) {}

		/** Internal function to update an area of the graph */
		public void UpdateArea (GraphUpdateObject o) {
			if (nodes == null || nodes.Length != width*depth) {
				Debug.LogWarning("The Grid Graph is not scanned, cannot update area ");
				//Not scanned
				return;
			}

			//Copy the bounds
			Bounds b = o.bounds;

			// Take the bounds and transform it using the matrix
			// Then convert that to a rectangle which contains
			// all nodes that might be inside the bounds
			Vector3 min, max;
			GetBoundsMinMax(b, inverseMatrix, out min, out max);

			int minX = Mathf.RoundToInt(min.x-0.5F);
			int maxX = Mathf.RoundToInt(max.x-0.5F);

			int minZ = Mathf.RoundToInt(min.z-0.5F);
			int maxZ = Mathf.RoundToInt(max.z-0.5F);

			//We now have coordinates in local space (i.e 1 unit = 1 node)
			var originalRect = new IntRect(minX, minZ, maxX, maxZ);
			var affectRect = originalRect;

			// Rect which covers the whole grid
			var gridRect = new IntRect(0, 0, width-1, depth-1);

			var physicsRect = originalRect;

			int erosion = o.updateErosion ? erodeIterations : 0;

#if ASTARDEBUG
			Matrix4x4 debugMatrix = matrix;
			debugMatrix *= Matrix4x4.TRS(new Vector3(0.5f, 0, 0.5f), Quaternion.identity, Vector3.one);

			originalRect.DebugDraw(debugMatrix, Color.red);
#endif

			bool willChangeWalkability = o.updatePhysics || o.modifyWalkability;

			//Calculate the largest bounding box which might be affected

			if (o.updatePhysics && !o.modifyWalkability) {
				// Add the collision.diameter margin for physics calls
				if (collision.collisionCheck) {
					Vector3 margin = new Vector3(collision.diameter, 0, collision.diameter)*0.5F;

					min -= margin*1.02F;//0.02 safety margin, physics is rarely very accurate
					max += margin*1.02F;

					physicsRect = new IntRect(
						Mathf.RoundToInt(min.x-0.5F),
						Mathf.RoundToInt(min.z-0.5F),
						Mathf.RoundToInt(max.x-0.5F),
						Mathf.RoundToInt(max.z-0.5F)
						);

					affectRect = IntRect.Union(physicsRect, affectRect);
				}
			}

			if (willChangeWalkability || erosion > 0) {
				// Add affect radius for erosion. +1 for updating connectivity info at the border
				affectRect = affectRect.Expand(erosion + 1);
			}

			// Clamp the rect to the grid bounds
			IntRect clampedRect = IntRect.Intersection(affectRect, gridRect);

			// Mark nodes that might be changed
			for (int x = clampedRect.xmin; x <= clampedRect.xmax; x++) {
				for (int z = clampedRect.ymin; z <= clampedRect.ymax; z++) {
					o.WillUpdateNode(nodes[z*width+x]);
				}
			}

			// Update Physics
			if (o.updatePhysics && !o.modifyWalkability) {
				collision.Initialize(matrix, nodeSize);

				clampedRect = IntRect.Intersection(physicsRect, gridRect);

				for (int x = clampedRect.xmin; x <= clampedRect.xmax; x++) {
					for (int z = clampedRect.ymin; z <= clampedRect.ymax; z++) {
						int index = z*width+x;

						GridNode node = nodes[index];

						UpdateNodePositionCollision(node, x, z, o.resetPenaltyOnPhysics);
					}
				}
			}

			//Apply GUO

			clampedRect = IntRect.Intersection(originalRect, gridRect);
			for (int x = clampedRect.xmin; x <= clampedRect.xmax; x++) {
				for (int z = clampedRect.ymin; z <= clampedRect.ymax; z++) {
					int index = z*width+x;

					GridNode node = nodes[index];

					if (willChangeWalkability) {
						node.Walkable = node.WalkableErosion;
						if (o.bounds.Contains((Vector3)node.position)) o.Apply(node);
						node.WalkableErosion = node.Walkable;
					} else {
						if (o.bounds.Contains((Vector3)node.position)) o.Apply(node);
					}
				}
			}

#if ASTARDEBUG
			physicsRect.DebugDraw(debugMatrix, Color.blue);
			affectRect.DebugDraw(debugMatrix, Color.black);
#endif

			// Recalculate connections
			if (willChangeWalkability && erosion == 0) {
				clampedRect = IntRect.Intersection(affectRect, gridRect);
				for (int x = clampedRect.xmin; x <= clampedRect.xmax; x++) {
					for (int z = clampedRect.ymin; z <= clampedRect.ymax; z++) {
						int index = z*width+x;

						GridNode node = nodes[index];

						CalculateConnections(x, z, node);
					}
				}
			} else if (willChangeWalkability && erosion > 0) {
				clampedRect = IntRect.Union(originalRect, physicsRect);

				IntRect erosionRect1 = clampedRect.Expand(erosion);
				IntRect erosionRect2 = erosionRect1.Expand(erosion);

				erosionRect1 = IntRect.Intersection(erosionRect1, gridRect);
				erosionRect2 = IntRect.Intersection(erosionRect2, gridRect);

#if ASTARDEBUG
				erosionRect1.DebugDraw(debugMatrix, Color.magenta);
				erosionRect2.DebugDraw(debugMatrix, Color.cyan);
#endif


				// * all nodes inside clampedRect might have had their walkability changed
				// * all nodes inside erosionRect1 might get affected by erosion from clampedRect and erosionRect2
				// * all nodes inside erosionRect2 (but outside erosionRect1) will be reset to previous walkability
				//     after calculation since their erosion might not be correctly calculated (nodes outside erosionRect2 might have an effect)

				for (int x = erosionRect2.xmin; x <= erosionRect2.xmax; x++) {
					for (int z = erosionRect2.ymin; z <= erosionRect2.ymax; z++) {
						int index = z*width+x;

						GridNode node = nodes[index];

						bool tmp = node.Walkable;
						node.Walkable = node.WalkableErosion;

						if (!erosionRect1.Contains(x, z)) {
							//Save the border's walkabilty data (will be reset later)
							node.TmpWalkable = tmp;
						}
					}
				}

				for (int x = erosionRect2.xmin; x <= erosionRect2.xmax; x++) {
					for (int z = erosionRect2.ymin; z <= erosionRect2.ymax; z++) {
						int index = z*width+x;

						GridNode node = nodes[index];

						CalculateConnections(x, z, node);
					}
				}

				// Erode the walkable area
				ErodeWalkableArea(erosionRect2.xmin, erosionRect2.ymin, erosionRect2.xmax+1, erosionRect2.ymax+1);

				for (int x = erosionRect2.xmin; x <= erosionRect2.xmax; x++) {
					for (int z = erosionRect2.ymin; z <= erosionRect2.ymax; z++) {
						if (erosionRect1.Contains(x, z)) continue;

						int index = z*width+x;

						GridNode node = nodes[index];

						//Restore temporarily stored data
						node.Walkable = node.TmpWalkable;
					}
				}

				// Recalculate connections of all affected nodes
				for (int x = erosionRect2.xmin; x <= erosionRect2.xmax; x++) {
					for (int z = erosionRect2.ymin; z <= erosionRect2.ymax; z++) {
						int index = z*width+x;

						GridNode node = nodes[index];
						CalculateConnections(x, z, node);
					}
				}
			}
		}

		/** Returns if there is an obstacle between \a origin and \a end on the graph.
		 * This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
		 * \astarpro */
		public bool Linecast (Vector3 _a, Vector3 _b) {
			GraphHitInfo hit;

			return Linecast(_a, _b, null, out hit);
		}

		/** Returns if there is an obstacle between \a origin and \a end on the graph.
		 * \param [in] _a Point to linecast from
		 * \param [in] _b Point to linecast to
		 * \param [in] hint If you have some idea of what the start node might be (the one close to \a _a), pass it to hint since it can enable faster lookups
		 * This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
		 * \astarpro */
		public bool Linecast (Vector3 _a, Vector3 _b, GraphNode hint) {
			GraphHitInfo hit;

			return Linecast(_a, _b, hint, out hit);
		}

		/** Returns if there is an obstacle between \a origin and \a end on the graph.
		 * \param [in] _a Point to linecast from
		 * \param [in] _b Point to linecast to
		 * \param [out] hit Contains info on what was hit, see GraphHitInfo
		 * \param [in] hint If you have some idea of what the start node might be (the one close to \a _a), pass it to hint since it can enable faster lookups
		 * This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
		 * \astarpro */
		public bool Linecast (Vector3 _a, Vector3 _b, GraphNode hint, out GraphHitInfo hit) {
			return Linecast(_a, _b, hint, out hit, null);
		}

		/** Magnitude of the cross product a x b */
		protected static float CrossMagnitude (Vector2 a, Vector2 b) {
			return a.x*b.y - b.x*a.y;
		}

		/** Utility method used by Linecast.
		 * Required since LevelGridNode does not inherit from GridNode.
		 * Lots of ugly casting but it was better than massive code duplication.
		 *
		 * Returns null if the node has no connection in that direction.
		 */
		protected virtual GridNodeBase GetNeighbourAlongDirection (GridNodeBase node, int direction) {
			var gridNode = node as GridNode;

			if (gridNode.GetConnectionInternal(direction)) {
				return nodes[gridNode.NodeInGridIndex+neighbourOffsets[direction]];
			}
			return null;
		}

		/** Clips a line segment in graph space to the graph bounds.
		 * That is (0,0,0) is the bottom left corner of the graph and (width,0,depth) is the top right corner.
		 * The first node is placed at (0.5,y,0.5). One unit distance is the same as nodeSize.
		 *
		 * Returns false if the line segment does not intersect the graph at all.
		 */
		protected bool ClipLineSegmentToBounds (Vector3 a, Vector3 b, out Vector3 outA, out Vector3 outB) {
			// If the start or end points are outside
			// the graph then clamping is needed
			if (a.x < 0 || a.z < 0 || a.x > width || a.z > depth ||
				b.x < 0 || b.z < 0 || b.x > width || b.z > depth) {
				// Boundary of the grid
				var p1 = new Vector3(0, 0,  0);
				var p2 = new Vector3(0, 0,  depth);
				var p3 = new Vector3(width, 0,  depth);
				var p4 = new Vector3(width, 0,  0);

				int intersectCount = 0;

				bool intersect;
				Vector3 intersection;

				intersection = VectorMath.SegmentIntersectionPointXZ(a, b, p1, p2, out intersect);

				if (intersect) {
					intersectCount++;
					if (!VectorMath.RightOrColinearXZ(p1, p2, a)) {
						a = intersection;
					} else {
						b = intersection;
					}
				}
				intersection = VectorMath.SegmentIntersectionPointXZ(a, b, p2, p3, out intersect);

				if (intersect) {
					intersectCount++;
					if (!VectorMath.RightOrColinearXZ(p2, p3, a)) {
						a = intersection;
					} else {
						b = intersection;
					}
				}
				intersection = VectorMath.SegmentIntersectionPointXZ(a, b, p3, p4, out intersect);

				if (intersect) {
					intersectCount++;
					if (!VectorMath.RightOrColinearXZ(p3, p4, a)) {
						a = intersection;
					} else {
						b = intersection;
					}
				}
				intersection = VectorMath.SegmentIntersectionPointXZ(a, b, p4, p1, out intersect);

				if (intersect) {
					intersectCount++;
					if (!VectorMath.RightOrColinearXZ(p4, p1, a)) {
						a = intersection;
					} else {
						b = intersection;
					}
				}

				if (intersectCount == 0) {
					// The line does not intersect with the grid
					outA = Vector3.zero;
					outB = Vector3.zero;
					return false;
				}
			}

			outA = a;
			outB = b;
			return true;
		}

		/** Returns if there is an obstacle between \a _a and \a _b on the graph.
		 * \param [in] _a Point to linecast from
		 * \param [in] _b Point to linecast to
		 * \param [out] hit Contains info on what was hit, see GraphHitInfo
		 * \param [in] hint \deprecated
		 * \param trace If a list is passed, then it will be filled with all nodes the linecast traverses
		 *
		 * This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
		 *
		 * It uses a method similar to Bresenham's line algorithm but it has been
		 * extended to allow the start and end points to lie on non-integer coordinates
		 * (which makes the math a bit trickier).
		 *
		 * \see https://en.wikipedia.org/wiki/Bresenham's_line_algorithm
		 *
		 * \version In 3.6.8 this method was rewritten to improve accuracy and performance.
		 * Previously it used a sampling approach which could cut corners of obstacles slightly
		 * and was pretty inefficient.
		 *
		 * \astarpro
		 */
		public bool Linecast (Vector3 _a, Vector3 _b, GraphNode hint, out GraphHitInfo hit, List<GraphNode> trace) {
			hit = new GraphHitInfo();

			hit.origin = _a;

			Vector3 aInGraphSpace = inverseMatrix.MultiplyPoint3x4(_a);
			Vector3 bInGraphSpace = inverseMatrix.MultiplyPoint3x4(_b);

			// Clip the line so that the start and end points are on the graph
			if (!ClipLineSegmentToBounds(aInGraphSpace, bInGraphSpace, out aInGraphSpace, out bInGraphSpace)) {
				// Line does not intersect the graph
				// So there are no obstacles we can hit
				return false;
			}

			// Find the closest nodes to the start and end on the part of the segment which is on the graph
			var n1 = GetNearest(matrix.MultiplyPoint3x4(aInGraphSpace), NNConstraint.None).node as GridNodeBase;
			var n2 = GetNearest(matrix.MultiplyPoint3x4(bInGraphSpace), NNConstraint.None).node as GridNodeBase;

			if (!n1.Walkable) {
				hit.node = n1;
				// Hit point is the point where the segment intersects with the graph boundary
				// or just _a if it starts inside the graph
				hit.point = matrix.MultiplyPoint3x4(aInGraphSpace);
				hit.tangentOrigin = hit.point;
				return true;
			}

			// Throw away components we don't care about (y)
			var a = new Vector2(aInGraphSpace.x, aInGraphSpace.z);
			var b = new Vector2(bInGraphSpace.x, bInGraphSpace.z);

			// Subtract 0.5 because nodes have an offset of 0.5 (first node is at (0.5,0.5) not at (0,0))
			// And it's just more convenient to remove that term here
			a -= Vector2.one*0.5f;
			b -= Vector2.one*0.5f;

			// Couldn't find a valid node
			// This shouldn't really happen unless there are NO nodes in the graph
			if (n1 == null || n2 == null) {
				hit.node = null;
				hit.point = _a;
				return true;
			}

			var dir = b-a;

			// Primary direction that we will move in
			// (e.g up and right or down and left)
			var sign = new Int2((int)Mathf.Sign(dir.x), (int)Mathf.Sign(dir.y));

			// How much further we move away from (or towards) the line when walking along #sign
			// This isn't an actual distance. It is a signed distance so it can be negative (other side of the line)
			// Also it includes an additional factor, but the same factor is used everywhere
			// and we only check for if the signed distance is greater or equal to zero so it is ok
			var primaryDirectionError = CrossMagnitude(dir, new Vector2(sign.x, sign.y))*0.5f;

			/*         Z
			 *         |
			 *         |
			 *
			 *         2
			 *         |
			 * --  3 - X - 1  ----- X
			 *         |
			 *         0
			 *
			 *         |
			 *         |
			 */

			// This is the direction which moves further to the right of the segment (when looking from the start)
			int directionToReduceError;
			// This is the direction which moves further to the left of the segment (when looking from the start)
			int directionToIncreaseError;

			if (dir.y >= 0) {
				if (dir.x >= 0) {
					// First quadrant
					directionToReduceError = 1;
					directionToIncreaseError = 2;
				} else {
					// Second quadrant
					directionToReduceError = 2;
					directionToIncreaseError = 3;
				}
			} else {
				if (dir.x < 0) {
					// Third quadrant
					directionToReduceError = 3;
					directionToIncreaseError = 0;
				} else {
					// Fourth quadrant
					directionToReduceError = 0;
					directionToIncreaseError = 1;
				}
			}


			// Current node. Start at n1
			var current = n1;

			while (current.NodeInGridIndex != n2.NodeInGridIndex) {
				// We visited #current so add it to the trace
				if (trace != null) {
					trace.Add(current);
				}

				// Position of the node in 2D graph/node space
				// Here the first node in the graph is at (0,0)
				var p = new Vector2(current.NodeInGridIndex % width, current.NodeInGridIndex / width);

				// Calculate the error
				// This is proportional to the distance between the line and the node
				var error = CrossMagnitude(dir, p-a);

				// How does the error change we take one step in the primary direction
				var nerror = error + primaryDirectionError;

				// Check if we need to reduce or increase the error (we want to keep it near zero)
				// and pick the appropriate direction to move in
				int ndir = nerror < 0 ? directionToIncreaseError : directionToReduceError;

				// Check we can move in that direction
				var other = GetNeighbourAlongDirection(current, ndir);
				if (other != null) {
					current = other;
				} else {
					// Hit obstacle
					// We know from what direction we moved in
					// so we can calculate the line which we hit

					// Either X offset is 0 or Z offset is zero since we only move in one of the 4 axis aligned directions
					// The line we hit will be right between two nodes (so a distance of 0.5 from the current node in graph space)
					Vector2 lineOrigin = p + new Vector2(neighbourXOffsets[ndir], neighbourZOffsets[ndir]) * 0.5f;
					Vector2 lineDirection;

					if (neighbourXOffsets[ndir] == 0) {
						// We hit a line parallel to the X axis
						lineDirection = new Vector2(1, 0);
					} else {
						// We hit a line parallel to the Z axis
						lineDirection = new Vector2(0, 1);
					}

					// Find the intersection
					var intersection = VectorMath.LineIntersectionPoint(lineOrigin, lineOrigin+lineDirection, a, b);

					var currentNodePositionInGraphSpace = inverseMatrix.MultiplyPoint3x4((Vector3)current.position);

					// The intersection is in graph space (with an offset of 0.5) so we need to transform it to world space
					var intersection3D = new Vector3(intersection.x + 0.5f, currentNodePositionInGraphSpace.y, intersection.y + 0.5f);
					var lineOrigin3D = new Vector3(lineOrigin.x + 0.5f, currentNodePositionInGraphSpace.y, lineOrigin.y + 0.5f);

					hit.point = matrix.MultiplyPoint3x4(intersection3D);
					hit.tangentOrigin = matrix.MultiplyPoint3x4(lineOrigin3D);
					hit.tangent = matrix.MultiplyVector(new Vector3(lineDirection.x, 0, lineDirection.y));
					hit.node = current;

					return true;
				}
			}

			// Add the last node to the trace
			if (trace != null) {
				trace.Add(current);
			}

			// No obstacles detected
			if (current == n2) {
				return false;
			}

			// Reached node right above or right below n2 but we cannot reach it
			hit.point = (Vector3)current.position;
			hit.tangentOrigin = hit.point;
			return true;
		}

		/** Returns if there is an obstacle between \a \a and \a \b on the graph.
		 * This function is different from the other Linecast functions since it 1) snaps the start and end positions directly to the graph.
		 *
		 * \param [in] a Point to linecast from
		 * \param [in] b Point to linecast to
		 * \param [out] hit Contains info on what was hit, see GraphHitInfo
		 * \param [in] hint \deprecated
		 *
		 * This is not the same as Physics.Linecast, this function traverses the graph and looks for collisions.
		 *
		 * \version Since 3.6.8 this method uses the same implementation as the other linecast methods so there is no performance boost to using it.
		 * \version In 3.6.8 this method was rewritten and that fixed a large number of bugs.
		 * Previously it had not always followed the line exactly as it should have
		 * and the hit output was not very accurate
		 * (for example the hit point was just the node position instead of a point on the edge which was hit)
		 */
		public bool SnappedLinecast (Vector3 a, Vector3 b, GraphNode hint, out GraphHitInfo hit) {
			return Linecast(
				(Vector3)GetNearest(a, NNConstraint.None).node.position,
				(Vector3)GetNearest(b, NNConstraint.None).node.position,
				hint,
				out hit
				);
		}

		/** Returns if \a node is connected to it's neighbour in the specified direction.
		 * This will also return true if #neighbours = NumNeighbours.Four, the direction is diagonal and one can move through one of the adjacent nodes
		 * to the targeted node.
		 *
		 * \see neighbourOffsets
		 */
		public bool CheckConnection (GridNode node, int dir) {
			if (neighbours == NumNeighbours.Eight || neighbours == NumNeighbours.Six || dir < 4) {
				return HasNodeConnection(node, dir);
			} else {
				int dir1 = (dir-4-1) & 0x3;
				int dir2 = (dir-4+1) & 0x3;

				if (!HasNodeConnection(node, dir1) || !HasNodeConnection(node, dir2)) {
					return false;
				} else {
					GridNode n1 = nodes[node.NodeInGridIndex+neighbourOffsets[dir1]];
					GridNode n2 = nodes[node.NodeInGridIndex+neighbourOffsets[dir2]];

					if (!n1.Walkable || !n2.Walkable) {
						return false;
					}

					if (!HasNodeConnection(n2, dir1) || !HasNodeConnection(n1, dir2)) {
						return false;
					}
				}
				return true;
			}
		}

		public override void SerializeExtraInfo (GraphSerializationContext ctx) {
			if (nodes == null) {
				ctx.writer.Write(-1);
				return;
			}

			ctx.writer.Write(nodes.Length);

			for (int i = 0; i < nodes.Length; i++) {
				nodes[i].SerializeNode(ctx);
			}
		}

		public override void DeserializeExtraInfo (GraphSerializationContext ctx) {
			int count = ctx.reader.ReadInt32();

			if (count == -1) {
				nodes = null;
				return;
			}

			nodes = new GridNode[count];

			for (int i = 0; i < nodes.Length; i++) {
				nodes[i] = new GridNode(active);
				nodes[i].DeserializeNode(ctx);
			}
		}

#if ASTAR_NO_JSON
		public override void SerializeSettings (GraphSerializationContext ctx) {
			base.SerializeSettings(ctx);
			ctx.writer.Write(aspectRatio);
			ctx.SerializeVector3(rotation);
			ctx.SerializeVector3(center);
			ctx.SerializeVector3((Vector3)unclampedSize);
			ctx.writer.Write(nodeSize);
			// collision
			collision.SerializeSettings(ctx);

			ctx.writer.Write(maxClimb);
			ctx.writer.Write(maxClimbAxis);
			ctx.writer.Write(maxSlope);
			ctx.writer.Write(erodeIterations);
			ctx.writer.Write(erosionUseTags);
			ctx.writer.Write(erosionFirstTag);
			ctx.writer.Write(autoLinkGrids);
			ctx.writer.Write((int)neighbours);
			ctx.writer.Write(cutCorners);
			ctx.writer.Write(penaltyPosition);
			ctx.writer.Write(penaltyPositionFactor);
			ctx.writer.Write(penaltyAngle);
			ctx.writer.Write(penaltyAngleFactor);
			ctx.writer.Write(penaltyAnglePower);
			ctx.writer.Write(isometricAngle);
			ctx.writer.Write(uniformEdgeCosts);
			ctx.writer.Write(useJumpPointSearch);
		}

		public override void DeserializeSettings (GraphSerializationContext ctx) {
			base.DeserializeSettings(ctx);

			aspectRatio = ctx.reader.ReadSingle();
			rotation = ctx.DeserializeVector3();
			center = ctx.DeserializeVector3();
			unclampedSize = (Vector2)ctx.DeserializeVector3();
			nodeSize = ctx.reader.ReadSingle();
			collision.DeserializeSettings(ctx);
			maxClimb = ctx.reader.ReadSingle();
			maxClimbAxis = ctx.reader.ReadInt32();
			maxSlope = ctx.reader.ReadSingle();
			erodeIterations = ctx.reader.ReadInt32();
			erosionUseTags = ctx.reader.ReadBoolean();
			erosionFirstTag = ctx.reader.ReadInt32();
			autoLinkGrids = ctx.reader.ReadBoolean();
			neighbours = (NumNeighbours)ctx.reader.ReadInt32();
			cutCorners = ctx.reader.ReadBoolean();
			penaltyPosition = ctx.reader.ReadBoolean();
			penaltyPositionFactor = ctx.reader.ReadSingle();
			penaltyAngle = ctx.reader.ReadBoolean();
			penaltyAngleFactor = ctx.reader.ReadSingle();
			penaltyAnglePower = ctx.reader.ReadSingle();
			isometricAngle = ctx.reader.ReadSingle();
			uniformEdgeCosts = ctx.reader.ReadBoolean();
			useJumpPointSearch = ctx.reader.ReadBoolean();
		}
#endif

		public override void PostDeserialization () {
#if ASTARDEBUG
			Debug.Log("Grid Graph - Post Deserialize");
#endif

			GenerateMatrix();
			SetUpOffsetsAndCosts();

			if (nodes == null || nodes.Length == 0) return;

			if (width*depth != nodes.Length) {
				Debug.LogError("Node data did not match with bounds data. Probably a change to the bounds/width/depth data was made after scanning the graph just prior to saving it. Nodes will be discarded");
				nodes = new GridNode[0];
				return;
			}

			GridNode.SetGridGraph(AstarPath.active.astarData.GetGraphIndex(this), this);

			for (int z = 0; z < depth; z++) {
				for (int x = 0; x < width; x++) {
					var node = nodes[z*width+x];

					if (node == null) {
						Debug.LogError("Deserialization Error : Couldn't cast the node to the appropriate type - GridGenerator");
						return;
					}

					node.NodeInGridIndex = z*width+x;
				}
			}
		}
	}

	/** Number of neighbours for a single grid node.
	 * \since The 'Six' item was added in 3.6.1
	 */
	public enum NumNeighbours {
		Four,
		Eight,
		Six
	}
}
