using System.Collections.Generic;
using Pathfinding.Serialization;
using UnityEngine;

namespace Pathfinding {
	/** Base class for GridNode and LevelGridNode */
	public abstract class GridNodeBase : GraphNode {
		protected GridNodeBase (AstarPath astar) : base(astar) {
		}

		protected int nodeInGridIndex;

		/** The index of the node in the grid.
		 * This is x + z*graph.width
		 * So you can get the X and Z indices using
		 * \code
		 * int index = node.NodeInGridIndex;
		 * int x = index % graph.width;
		 * int z = index / graph.width;
		 * // where graph is GridNode.GetGridGraph (node.graphIndex), i.e the graph the nodes are contained in.
		 * \endcode
		 */
		public int NodeInGridIndex { get { return nodeInGridIndex; } set { nodeInGridIndex = value; } }
	}

	public class GridNode : GridNodeBase {
		public GridNode (AstarPath astar) : base(astar) {
		}

#if !ASTAR_NO_GRID_GRAPH
		private static GridGraph[] _gridGraphs = new GridGraph[0];
		public static GridGraph GetGridGraph (uint graphIndex) { return _gridGraphs[(int)graphIndex]; }

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
		public GraphNode[] connections;
		public uint[] connectionCosts;
#endif

		public static void SetGridGraph (int graphIndex, GridGraph graph) {
			if (_gridGraphs.Length <= graphIndex) {
				var gg = new GridGraph[graphIndex+1];
				for (int i = 0; i < _gridGraphs.Length; i++) gg[i] = _gridGraphs[i];
				_gridGraphs = gg;
			}

			_gridGraphs[graphIndex] = graph;
		}

		protected ushort gridFlags;

		/** Internal use only */
		internal ushort InternalGridFlags {
			get { return gridFlags; }
			set { gridFlags = value; }
		}

		const int GridFlagsConnectionOffset = 0;
		const int GridFlagsConnectionBit0 = 1 << GridFlagsConnectionOffset;
		const int GridFlagsConnectionMask = 0xFF << GridFlagsConnectionOffset;

		const int GridFlagsWalkableErosionOffset = 8;
		const int GridFlagsWalkableErosionMask = 1 << GridFlagsWalkableErosionOffset;

		const int GridFlagsWalkableTmpOffset = 9;
		const int GridFlagsWalkableTmpMask = 1 << GridFlagsWalkableTmpOffset;

		const int GridFlagsEdgeNodeOffset = 10;
		const int GridFlagsEdgeNodeMask = 1 << GridFlagsEdgeNodeOffset;

		/** Returns true if the node has a connection in the specified direction.
		 * The dir parameter corresponds to directions in the grid as:
		 * \code
		 * [0] = -Y
		 * [1] = +X
		 * [2] = +Y
		 * [3] = -X
		 * [4] = -Y+X
		 * [5] = +Y+X
		 * [6] = +Y-X
		 * [7] = -Y-X
		 * \endcode
		 *
		 * \see SetConnectionInternal
		 */
		public bool GetConnectionInternal (int dir) {
			return (gridFlags >> dir & GridFlagsConnectionBit0) != 0;
		}

		/** Enables or disables a connection in a specified direction on the graph.
		 *	\see GetConnectionInternal
		 */
		public void SetConnectionInternal (int dir, bool value) {
			unchecked { gridFlags = (ushort)(gridFlags & ~((ushort)1 << GridFlagsConnectionOffset << dir) | (value ? (ushort)1 : (ushort)0) << GridFlagsConnectionOffset << dir); }
		}

		/** Sets the state of all grid connections.
		 * \param connections a bitmask of the connections (bit 0 is the first connection, etc.).
		 *
		 * \see SetConnectionInternal
		 */
		public void SetAllConnectionInternal (int connections) {
			unchecked { gridFlags = (ushort)((gridFlags & ~GridFlagsConnectionMask) | (connections << GridFlagsConnectionOffset)); }
		}

		/** Disables all grid connections from this node.
		 * \note Other nodes might still be able to get to this node.
		 * Therefore it is recommended to also disable the relevant connections on adjacent nodes.
		 */
		public void ResetConnectionsInternal () {
			unchecked {
				gridFlags = (ushort)(gridFlags & ~GridFlagsConnectionMask);
			}
		}

		public bool EdgeNode {
			get {
				return (gridFlags & GridFlagsEdgeNodeMask) != 0;
			}
			set {
				unchecked { gridFlags = (ushort)(gridFlags & ~GridFlagsEdgeNodeMask | (value ? GridFlagsEdgeNodeMask : 0)); }
			}
		}

		/** Stores walkability before erosion is applied.
		 * Used by graph updating.
		 */
		public bool WalkableErosion {
			get {
				return (gridFlags & GridFlagsWalkableErosionMask) != 0;
			}
			set {
				unchecked { gridFlags = (ushort)(gridFlags & ~GridFlagsWalkableErosionMask | (value ? (ushort)GridFlagsWalkableErosionMask : (ushort)0)); }
			}
		}

		/** Temporary variable used by graph updating */
		public bool TmpWalkable {
			get {
				return (gridFlags & GridFlagsWalkableTmpMask) != 0;
			}
			set {
				unchecked { gridFlags = (ushort)(gridFlags & ~GridFlagsWalkableTmpMask | (value ? (ushort)GridFlagsWalkableTmpMask : (ushort)0)); }
			}
		}

		public override void ClearConnections (bool alsoReverse) {
			if (alsoReverse) {
				GridGraph gg = GetGridGraph(GraphIndex);
				for (int i = 0; i < 8; i++) {
					GridNode other = gg.GetNodeConnection(this, i);
					if (other != null) {
						//Remove reverse connection
						other.SetConnectionInternal(i < 4 ? ((i + 2) % 4) : (((5-2) % 4) + 4), false);
					}
				}
			}

			ResetConnectionsInternal();

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			if (alsoReverse) {
				if (connections != null) for (int i = 0; i < connections.Length; i++) connections[i].RemoveConnection(this);
			}
			connections = null;
			connectionCosts = null;
#endif
		}

		public override void GetConnections (GraphNodeDelegate del) {
			GridGraph gg = GetGridGraph(GraphIndex);

			int[] neighbourOffsets = gg.neighbourOffsets;
			GridNode[] nodes = gg.nodes;

			for (int i = 0; i < 8; i++) {
				if (GetConnectionInternal(i)) {
					GridNode other = nodes[nodeInGridIndex + neighbourOffsets[i]];
					if (other != null) del(other);
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			if (connections != null) for (int i = 0; i < connections.Length; i++) del(connections[i]);
#endif
		}

		public Vector3 ClosestPointOnNode (Vector3 p) {
			var gg = GetGridGraph(GraphIndex);

			// Convert to graph space
			p = gg.inverseMatrix.MultiplyPoint3x4(p);

			// Nodes are offset 0.5 graph space nodes
			float xf = position.x-0.5F;
			float zf = position.z-0.5f;

			// Calculate graph position of this node
			int x = nodeInGridIndex % gg.width;
			int z = nodeInGridIndex / gg.width;

			// Handle the y coordinate separately
			float y = gg.inverseMatrix.MultiplyPoint3x4((Vector3)p).y;

			var closestInGraphSpace = new Vector3(Mathf.Clamp(xf, x-0.5f, x+0.5f)+0.5f, y, Mathf.Clamp(zf, z-0.5f, z+0.5f)+0.5f);

			// Convert to world space
			return gg.matrix.MultiplyPoint3x4(closestInGraphSpace);
		}

		public override bool GetPortal (GraphNode other, List<Vector3> left, List<Vector3> right, bool backwards) {
			if (backwards) return true;

			GridGraph gg = GetGridGraph(GraphIndex);
			int[] neighbourOffsets = gg.neighbourOffsets;
			GridNode[] nodes = gg.nodes;

			for (int i = 0; i < 4; i++) {
				if (GetConnectionInternal(i) && other == nodes[nodeInGridIndex + neighbourOffsets[i]]) {
					Vector3 middle = ((Vector3)(position + other.position))*0.5f;
					Vector3 cross = Vector3.Cross(gg.collision.up, (Vector3)(other.position-position));
					cross.Normalize();
					cross *= gg.nodeSize*0.5f;
					left.Add(middle - cross);
					right.Add(middle + cross);
					return true;
				}
			}

			for (int i = 4; i < 8; i++) {
				if (GetConnectionInternal(i) && other == nodes[nodeInGridIndex + neighbourOffsets[i]]) {
					bool rClear = false;
					bool lClear = false;
					if (GetConnectionInternal(i-4)) {
						GridNode n2 = nodes[nodeInGridIndex + neighbourOffsets[i-4]];
						if (n2.Walkable && n2.GetConnectionInternal((i-4+1)%4)) {
							rClear = true;
						}
					}

					if (GetConnectionInternal((i-4+1)%4)) {
						GridNode n2 = nodes[nodeInGridIndex + neighbourOffsets[(i-4+1)%4]];
						if (n2.Walkable && n2.GetConnectionInternal(i-4)) {
							lClear = true;
						}
					}

					Vector3 middle = ((Vector3)(position + other.position))*0.5f;
					Vector3 cross = Vector3.Cross(gg.collision.up, (Vector3)(other.position-position));
					cross.Normalize();
					cross *= gg.nodeSize*1.4142f;
					left.Add(middle - (lClear ? cross : Vector3.zero));
					right.Add(middle + (rClear ? cross : Vector3.zero));
					return true;
				}
			}

			return false;
		}

		public override void FloodFill (Stack<GraphNode> stack, uint region) {
			GridGraph gg = GetGridGraph(GraphIndex);

			int[] neighbourOffsets = gg.neighbourOffsets;
			GridNode[] nodes = gg.nodes;

			for (int i = 0; i < 8; i++) {
				if (GetConnectionInternal(i)) {
					GridNode other = nodes[nodeInGridIndex + neighbourOffsets[i]];
					if (other != null && other.Area != region) {
						other.Area = region;
						stack.Push(other);
					}
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			if (connections != null) for (int i = 0; i < connections.Length; i++) {
					GraphNode other = connections[i];
					if (other.Area != region) {
						other.Area = region;
						stack.Push(other);
					}
				}
#endif
		}

#if ASTAR_GRID_NO_CUSTOM_CONNECTIONS
		public override void AddConnection (GraphNode node, uint cost) {
			throw new System.NotImplementedException("GridNodes do not have support for adding manual connections with your current settings."+
				"\nPlease disable ASTAR_GRID_NO_CUSTOM_CONNECTIONS in the Optimizations tab in the A* Inspector");
		}

		public override void RemoveConnection (GraphNode node) {
			throw new System.NotImplementedException("GridNodes do not have support for adding manual connections with your current settings."+
				"\nPlease disable ASTAR_GRID_NO_CUSTOM_CONNECTIONS in the Optimizations tab in the A* Inspector");
		}
#else
		/** Add a connection from this node to the specified node.
		 * If the connection already exists, the cost will simply be updated and
		 * no extra connection added.
		 *
		 * \note Only adds a one-way connection. Consider calling the same function on the other node
		 * to get a two-way connection.
		 */
		public override void AddConnection (GraphNode node, uint cost) {
			if (connections != null) {
				for (int i = 0; i < connections.Length; i++) {
					if (connections[i] == node) {
						connectionCosts[i] = cost;
						return;
					}
				}
			}

			int connLength = connections != null ? connections.Length : 0;

			var newconns = new GraphNode[connLength+1];
			var newconncosts = new uint[connLength+1];
			for (int i = 0; i < connLength; i++) {
				newconns[i] = connections[i];
				newconncosts[i] = connectionCosts[i];
			}

			newconns[connLength] = node;
			newconncosts[connLength] = cost;

			connections = newconns;
			connectionCosts = newconncosts;
		}

		/** Removes any connection from this node to the specified node.
		 * If no such connection exists, nothing will be done.
		 *
		 * \note This only removes the connection from this node to the other node.
		 * You may want to call the same function on the other node to remove its eventual connection
		 * to this node.
		 */
		public override void RemoveConnection (GraphNode node) {
			if (connections == null) return;

			for (int i = 0; i < connections.Length; i++) {
				if (connections[i] == node) {
					int connLength = connections.Length;

					var newconns = new GraphNode[connLength-1];
					var newconncosts = new uint[connLength-1];
					for (int j = 0; j < i; j++) {
						newconns[j] = connections[j];
						newconncosts[j] = connectionCosts[j];
					}
					for (int j = i+1; j < connLength; j++) {
						newconns[j-1] = connections[j];
						newconncosts[j-1] = connectionCosts[j];
					}

					connections = newconns;
					connectionCosts = newconncosts;
					return;
				}
			}
		}
#endif

		public override void UpdateRecursiveG (Path path, PathNode pathNode, PathHandler handler) {
			GridGraph gg = GetGridGraph(GraphIndex);

			int[] neighbourOffsets = gg.neighbourOffsets;
			GridNode[] nodes = gg.nodes;

			UpdateG(path, pathNode);
			handler.PushNode(pathNode);

			ushort pid = handler.PathID;

			for (int i = 0; i < 8; i++) {
				if (GetConnectionInternal(i)) {
					GridNode other = nodes[nodeInGridIndex + neighbourOffsets[i]];
					PathNode otherPN = handler.GetPathNode(other);
					if (otherPN.parent == pathNode && otherPN.pathID == pid) other.UpdateRecursiveG(path, otherPN, handler);
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			if (connections != null) for (int i = 0; i < connections.Length; i++) {
					GraphNode other = connections[i];
					PathNode otherPN = handler.GetPathNode(other);
					if (otherPN.parent == pathNode && otherPN.pathID == pid) other.UpdateRecursiveG(path, otherPN, handler);
				}
#endif
		}

#if ASTAR_JPS
/*
 * Non Cyclic
 * [0] = -Y
 * [1] = +X
 * [2] = +Y
 * [3] = -X
 * [4] = -Y+X
 * [5] = +Y+X
 * [6] = +Y-X
 * [7] = -Y-X
 *
 * Cyclic
 * [0] = -X
 * [1] = -X+Y
 * [2] = +Y
 * [3] = +X+Y
 * [4] = +X
 * [5] = +X-Y
 * [6] = -Y
 * [7] = -Y-X
 */
		static byte[] JPSForced = {
			0xFF, // Shouldn't really happen
			0,
			(1<<3),
			0,
			0,
			0,
			(1<<5),
			0
		};

		static byte[] JPSForcedDiagonal = {
			0xFF, // Shouldn't really happen
			(1<<2),
			0,
			0,
			0,
			0,
			0,
			(1<<6)
		};

		/** Permutation of the standard grid node neighbour order to put them on a clockwise cycle around the node.
		 * Enables easier math in some cases
		 */
		static int[] JPSCyclic = {
			6,
			4,
			2,
			0,
			5,
			3,
			1,
			7
		};

		/** Inverse permutation of #JPSCyclic */
		static int[] JPSInverseCyclic = {
			3, // 0
			6, // 1
			2, // 2
			5, // 3
			1, // 4
			4, // 5
			0, // 6
			7  // 7
		};

		const int JPSNaturalStraightNeighbours = 1 << 4;
		const int JPSNaturalDiagonalNeighbours = (1 << 5) | (1 << 4) | (1 << 3);

		/** Memoization of what results to return from the Jump method.
		 */
		GridNode[] JPSCache;

		/** Each byte is a bitfield where each bit indicates if direction number i should return null from the Jump method.
		 * Used as a cache.
		 * I would like to make this a ulong instead, but that could run into data races.
		 */
		byte[] JPSDead;
		ushort[] JPSLastCacheID;

		/** Executes a straight jump search.
		 * \see http://en.wikipedia.org/wiki/Jump_point_search
		 */
		static GridNode JPSJumpStraight (GridNode node, Path path, PathHandler handler, int parentDir, int depth = 0) {
			GridGraph gg = GetGridGraph(node.GraphIndex);

			int[] neighbourOffsets = gg.neighbourOffsets;
			GridNode[] nodes = gg.nodes;

			GridNode origin = node;
			// Indexing into the cache arrays from multiple threads like this should cause
			// a lot of false sharing and cache trashing, but after profiling it seems
			// that this is not a major concern
			int threadID = handler.threadID;
			int threadOffset = 8*handler.threadID;

			int cyclicParentDir = JPSCyclic[parentDir];

			GridNode result = null;

			// Rotate 180 degrees
			const int forwardDir = 4;
			int forwardOffset = neighbourOffsets[JPSInverseCyclic[(forwardDir + cyclicParentDir) % 8]];

			// Move forwards in the same direction
			// until a node is encountered which we either
			// * know the result for (memoization)
			// * is a special node (flag2 set)
			// * has custom connections
			// * the node has a forced neighbour
			// Then break out of the loop
			// and start another loop which goes through the same nodes and sets the
			// memoization caches to avoid expensive calls in the future
			while (true) {
				// This is needed to make sure different threads don't overwrite each others results
				// It doesn't matter if we throw away some caching done by other threads as this will only
				// happen during the first few path requests
				if (node.JPSLastCacheID == null || node.JPSLastCacheID.Length < handler.totalThreadCount) {
					lock (node) {
						// Check again in case another thread has already created the array
						if (node.JPSLastCacheID == null || node.JPSLastCacheID.Length < handler.totalThreadCount) {
							node.JPSCache = new GridNode[8*handler.totalThreadCount];
							node.JPSDead = new byte[handler.totalThreadCount];
							node.JPSLastCacheID = new ushort[handler.totalThreadCount];
						}
					}
				}
				if (node.JPSLastCacheID[threadID] != path.pathID) {
					for (int i = 0; i < 8; i++) node.JPSCache[i + threadOffset] = null;
					node.JPSLastCacheID[threadID] = path.pathID;
					node.JPSDead[threadID] = 0;
				}

				// Cache earlier results, major optimization
				// It is important to read from it once and then return the same result,
				// if we read from it twice, we might get different results due to other threads clearing the array sometimes
				GridNode cachedResult = node.JPSCache[parentDir + threadOffset];
				if (cachedResult != null) {
					result = cachedResult;
					break;
				}

				if (((node.JPSDead[threadID] >> parentDir)&1) != 0) return null;

				// Special node (e.g end node), take care of
				if (handler.GetPathNode(node).flag2) {
					//Debug.Log ("Found end Node!");
					//Debug.DrawRay ((Vector3)position, Vector3.up*2, Color.green);
					result = node;
					break;
				}

				#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
				// Special node which has custom connections, take care of
				if (node.connections != null && node.connections.Length > 0) {
					result = node;
					break;
				}
				#endif


				// These are the nodes this node is connected to, one bit for each of the 8 directions
				int noncyclic = node.gridFlags;//We don't actually need to & with this because we don't use the other bits. & 0xFF;
				int cyclic = 0;
				for (int i = 0; i < 8; i++) cyclic |= ((noncyclic >> i)&0x1) << JPSCyclic[i];


				int forced = 0;
				// Loop around to be able to assume -X is where we came from
				cyclic = ((cyclic >> cyclicParentDir) | ((cyclic << 8) >> cyclicParentDir)) & 0xFF;

				//for ( int i = 0; i < 8; i++ ) if ( ((cyclic >> i)&1) == 0 ) forced |= JPSForced[i];
				if ((cyclic & (1 << 2)) == 0) forced |= (1<<3);
				if ((cyclic & (1 << 6)) == 0) forced |= (1<<5);

				int natural = JPSNaturalStraightNeighbours;

				// Check if there are any forced neighbours which we can reach that are not natural neighbours
				//if ( ((forced & cyclic) & (~(natural & cyclic))) != 0 ) {
				if ((forced & (~natural) & cyclic) != 0) {
					// Some of the neighbour nodes are forced
					result = node;
					break;
				}

				// Make sure we can reach the next node
				if ((cyclic & (1 << forwardDir)) != 0) {
					node = nodes[node.nodeInGridIndex + forwardOffset];

					//Debug.DrawLine ( (Vector3)position + Vector3.up*0.2f*(depth), (Vector3)other.position + Vector3.up*0.2f*(depth+1), Color.magenta);
				} else {
					result = null;
					break;
				}
			}

			if (result == null) {
				while (origin != node) {
					origin.JPSDead[threadID] |= (byte)(1 << parentDir);
					origin = nodes[origin.nodeInGridIndex + forwardOffset];
				}
			} else {
				while (origin != node) {
					origin.JPSCache[parentDir + threadOffset] = result;
					origin = nodes[origin.nodeInGridIndex + forwardOffset];
				}
			}

			return result;
		}

		/** Executes a diagonal jump search.
		 * \see http://en.wikipedia.org/wiki/Jump_point_search
		 */
		GridNode JPSJumpDiagonal (Path path, PathHandler handler, int parentDir, int depth = 0) {
			// Indexing into the cache arrays from multiple threads like this should cause
			// a lot of false sharing and cache trashing, but after profiling it seems
			// that this is not a major concern
			int threadID = handler.threadID;
			int threadOffset = 8*handler.threadID;

			// This is needed to make sure different threads don't overwrite each others results
			// It doesn't matter if we throw away some caching done by other threads as this will only
			// happen during the first few path requests
			if (JPSLastCacheID == null || JPSLastCacheID.Length < handler.totalThreadCount) {
				lock (this) {
					// Check again in case another thread has already created the array
					if (JPSLastCacheID == null || JPSLastCacheID.Length < handler.totalThreadCount) {
						JPSCache = new GridNode[8*handler.totalThreadCount];
						JPSDead = new byte[handler.totalThreadCount];
						JPSLastCacheID = new ushort[handler.totalThreadCount];
					}
				}
			}
			if (JPSLastCacheID[threadID] != path.pathID) {
				for (int i = 0; i < 8; i++) JPSCache[i + threadOffset] = null;
				JPSLastCacheID[threadID] = path.pathID;
				JPSDead[threadID] = 0;
			}

			// Cache earlier results, major optimization
			// It is important to read from it once and then return the same result,
			// if we read from it twice, we might get different results due to other threads clearing the array sometimes
			GridNode cachedResult = JPSCache[parentDir + threadOffset];
			if (cachedResult != null) {
				//return cachedResult;
			}

			//if ( ((JPSDead[threadID] >> parentDir)&1) != 0 ) return null;

			// Special node (e.g end node), take care of
			if (handler.GetPathNode(this).flag2) {
				//Debug.Log ("Found end Node!");
				//Debug.DrawRay ((Vector3)position, Vector3.up*2, Color.green);
				JPSCache[parentDir + threadOffset] = this;
				return this;
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			// Special node which has custom connections, take care of
			if (connections != null && connections.Length > 0) {
				JPSCache[parentDir] = this;
				return this;
			}
#endif

			int noncyclic = gridFlags;//We don't actually need to & with this because we don't use the other bits. & 0xFF;
			int cyclic = 0;
			for (int i = 0; i < 8; i++) cyclic |= ((noncyclic >> i)&0x1) << JPSCyclic[i];


			int forced = 0;
			int cyclicParentDir = JPSCyclic[parentDir];
			// Loop around to be able to assume -X is where we came from
			cyclic = ((cyclic >> cyclicParentDir) | ((cyclic << 8) >> cyclicParentDir)) & 0xFF;

			int natural;

			for (int i = 0; i < 8; i++) if (((cyclic >> i)&1) == 0) forced |= JPSForcedDiagonal[i];

			natural = JPSNaturalDiagonalNeighbours;
			/*
			 * if ( ((Vector3)position - new Vector3(1.5f,0,-1.5f)).magnitude < 0.5f ) {
			 *  Debug.Log (noncyclic + " " + parentDir + " " + cyclicParentDir);
			 *  Debug.Log (System.Convert.ToString (cyclic, 2)+"\n"+System.Convert.ToString (noncyclic, 2)+"\n"+System.Convert.ToString (natural, 2)+"\n"+System.Convert.ToString (forced, 2));
			 * }*/

			// Don't force nodes we cannot reach anyway
			forced &= cyclic;
			natural &= cyclic;

			if ((forced & (~natural)) != 0) {
				// Some of the neighbour nodes are forced
				JPSCache[parentDir+threadOffset] = this;
				return this;
			}

			int forwardDir;

			GridGraph gg = GetGridGraph(GraphIndex);
			int[] neighbourOffsets = gg.neighbourOffsets;
			GridNode[] nodes = gg.nodes;

			{
				// Rotate 180 degrees - 1 node
				forwardDir = 3;
				if (((cyclic >> forwardDir)&1) != 0) {
					int oi = JPSInverseCyclic[(forwardDir + cyclicParentDir) % 8];
					GridNode other = nodes[nodeInGridIndex + neighbourOffsets[oi]];

					//Debug.DrawLine ( (Vector3)position + Vector3.up*0.2f*(depth), (Vector3)other.position + Vector3.up*0.2f*(depth+1), Color.black);
					GridNode v;
					if (oi < 4) {
						v = JPSJumpStraight(other, path, handler, JPSInverseCyclic[(cyclicParentDir-1+8)%8], depth+1);
					} else {
						v = other.JPSJumpDiagonal(path, handler, JPSInverseCyclic[(cyclicParentDir-1+8)%8], depth+1);
					}
					if (v != null) {
						JPSCache[parentDir+threadOffset] = this;
						return this;
					}
				}

				// Rotate 180 degrees + 1 node
				forwardDir = 5;
				if (((cyclic >> forwardDir)&1) != 0) {
					int oi = JPSInverseCyclic[(forwardDir + cyclicParentDir) % 8];
					GridNode other = nodes[nodeInGridIndex + neighbourOffsets[oi]];

					//Debug.DrawLine ( (Vector3)position + Vector3.up*0.2f*(depth), (Vector3)other.position + Vector3.up*0.2f*(depth+1), Color.grey);
					GridNode v;
					if (oi < 4) {
						v = JPSJumpStraight(other, path, handler, JPSInverseCyclic[(cyclicParentDir+1+8)%8], depth+1);
					} else {
						v = other.JPSJumpDiagonal(path, handler, JPSInverseCyclic[(cyclicParentDir+1+8)%8], depth+1);
					}

					if (v != null) {
						JPSCache[parentDir+threadOffset] = this;
						return this;
					}
				}
			}

			// Rotate 180 degrees
			forwardDir = 4;
			if (((cyclic >> forwardDir)&1) != 0) {
				int oi = JPSInverseCyclic[(forwardDir + cyclicParentDir) % 8];
				GridNode other = nodes[nodeInGridIndex + neighbourOffsets[oi]];

				//Debug.DrawLine ( (Vector3)position + Vector3.up*0.2f*(depth), (Vector3)other.position + Vector3.up*0.2f*(depth+1), Color.magenta);

				var v = other.JPSJumpDiagonal(path, handler, parentDir, depth+1);
				if (v != null) {
					JPSCache[parentDir+threadOffset] = v;
					return v;
				}
			}
			JPSDead[threadID] |= (byte)(1 << parentDir);
			return null;
		}

		/** Opens a node using Jump Point Search.
		 * \see http://en.wikipedia.org/wiki/Jump_point_search
		 */
		public void JPSOpen (Path path, PathNode pathNode, PathHandler handler) {
			GridGraph gg = GetGridGraph(GraphIndex);

			int[] neighbourOffsets = gg.neighbourOffsets;
			GridNode[] nodes = gg.nodes;
			ushort pid = handler.PathID;

			int noncyclic = gridFlags & 0xFF;
			int cyclic = 0;
			for (int i = 0; i < 8; i++) cyclic |= ((noncyclic >> i)&0x1) << JPSCyclic[i];

			var parent = pathNode.parent != null ? pathNode.parent.node as GridNode : null;
			int parentDir = -1;

			if (parent != null) {
				int diff = parent != null ? parent.nodeInGridIndex - nodeInGridIndex : 0;

				int x2 = nodeInGridIndex % gg.width;
				int x1 = parent.nodeInGridIndex % gg.width;
				if (diff < 0) {
					if (x1 == x2) {
						parentDir = 0;
					} else if (x1 < x2) {
						parentDir = 7;
					} else {
						parentDir = 4;
					}
				} else {
					if (x1 == x2) {
						parentDir = 1;
					} else if (x1 < x2) {
						parentDir = 6;
					} else {
						parentDir = 5;
					}
				}
			}
			int cyclicParentDir = 0;
			// Check for -1

			int forced = 0;
			if (parentDir != -1) {
				cyclicParentDir = JPSCyclic[parentDir];
				// Loop around to be able to assume -X is where we came from
				cyclic = ((cyclic >> cyclicParentDir) | ((cyclic << 8) >> cyclicParentDir)) & 0xFF;
			} else {
				forced = 0xFF;
				//parentDir = 0;
			}

			bool diagonal = parentDir >= 4;
			int natural;

			if (diagonal) {
				for (int i = 0; i < 8; i++) if (((cyclic >> i)&1) == 0) forced |= JPSForcedDiagonal[i];

				natural = JPSNaturalDiagonalNeighbours;
			} else {
				for (int i = 0; i < 8; i++) if (((cyclic >> i)&1) == 0) forced |= JPSForced[i];

				natural = JPSNaturalStraightNeighbours;
			}

			// Don't force nodes we cannot reach anyway
			forced &= cyclic;
			natural &= cyclic;

			int nb = forced | natural;


			/*if ( ((Vector3)position - new Vector3(0.5f,0,3.5f)).magnitude < 0.5f ) {
			 *  Debug.Log (noncyclic + " " + parentDir + " " + cyclicParentDir);
			 *  Debug.Log (System.Convert.ToString (cyclic, 2)+"\n"+System.Convert.ToString (noncyclic, 2)+"\n"+System.Convert.ToString (natural, 2)+"\n"+System.Convert.ToString (forced, 2));
			 * }*/

			for (int i = 0; i < 8; i++) {
				if (((nb >> i)&1) != 0) {
					int oi = JPSInverseCyclic[(i + cyclicParentDir) % 8];
					GridNode other = nodes[nodeInGridIndex + neighbourOffsets[oi]];

#if ASTARDEBUG
					if (((forced >> i)&1) != 0) {
						Debug.DrawLine((Vector3)position, Vector3.Lerp((Vector3)other.position, (Vector3)position, 0.6f), Color.red);
					}
					if (((natural >> i)&1) != 0) {
						Debug.DrawLine((Vector3)position + Vector3.up*0.2f, Vector3.Lerp((Vector3)other.position, (Vector3)position, 0.6f) + Vector3.up*0.2f, Color.green);
					}
#endif

					if (oi < 4) {
						other = JPSJumpStraight(other, path, handler, JPSInverseCyclic[(i + 4 + cyclicParentDir) % 8]);
					} else {
						other = other.JPSJumpDiagonal(path, handler, JPSInverseCyclic[(i + 4 + cyclicParentDir) % 8]);
					}

					if (other != null) {
						//Debug.DrawLine ( (Vector3)position + Vector3.up*0.0f, (Vector3)other.position + Vector3.up*0.3f, Color.cyan);
						//Debug.DrawRay ( (Vector3)other.position, Vector3.up, Color.cyan);
						//GridNode other = nodes[nodeInGridIndex + neighbourOffsets[i]];
						//if (!path.CanTraverse (other)) continue;

						PathNode otherPN = handler.GetPathNode(other);

						if (otherPN.pathID != pid) {
							otherPN.parent = pathNode;
							otherPN.pathID = pid;

							otherPN.cost = (uint)(other.position - position).costMagnitude;//neighbourCosts[i];

							otherPN.H = path.CalculateHScore(other);
							other.UpdateG(path, otherPN);

							//Debug.Log ("G " + otherPN.G + " F " + otherPN.F);
							handler.PushNode(otherPN);
							//Debug.DrawRay ((Vector3)otherPN.node.Position, Vector3.up,Color.blue);
						} else {
							//If not we can test if the path from the current node to this one is a better one then the one already used
							uint tmpCost = (uint)(other.position - position).costMagnitude;//neighbourCosts[i];

							if (pathNode.G+tmpCost+path.GetTraversalCost(other) < otherPN.G) {
								//Debug.Log ("Path better from " + NodeIndex + " to " + otherPN.node.NodeIndex + " " + (pathNode.G+tmpCost+path.GetTraversalCost(other)) + " < " + otherPN.G);
								otherPN.cost = tmpCost;

								otherPN.parent = pathNode;

								other.UpdateRecursiveG(path, otherPN, handler);

								//Or if the path from this node ("other") to the current ("current") is better
							} else if (otherPN.G+tmpCost+path.GetTraversalCost(this) < pathNode.G) {
								//Debug.Log ("Path better from " + otherPN.node.NodeIndex + " to " + NodeIndex + " " + (otherPN.G+tmpCost+path.GetTraversalCost (this)) + " < " + pathNode.G);
								pathNode.parent = otherPN;
								pathNode.cost = tmpCost;

								UpdateRecursiveG(path, pathNode, handler);
							}
						}
					}
				}

#if ASTARDEBUG
				if (i == 0 && parentDir != -1 && this.nodeInGridIndex > 10) {
					int oi = JPSInverseCyclic[(i + cyclicParentDir) % 8];

					if (nodeInGridIndex + neighbourOffsets[oi] < 0 || nodeInGridIndex + neighbourOffsets[oi] >= nodes.Length) {
						//Debug.LogError ("ERR: " + (nodeInGridIndex + neighbourOffsets[oi]) + " " + cyclicParentDir + " " + parentDir + " Reverted " + oi);
						//Debug.DrawRay ((Vector3)position, Vector3.up, Color.red);
					} else {
						GridNode other = nodes[nodeInGridIndex + neighbourOffsets[oi]];
						Debug.DrawLine((Vector3)position - Vector3.up*0.2f, Vector3.Lerp((Vector3)other.position, (Vector3)position, 0.6f) - Vector3.up*0.2f, Color.blue);
					}
				}
#endif
			}
		}
#endif

		public override void Open (Path path, PathNode pathNode, PathHandler handler) {
			GridGraph gg = GetGridGraph(GraphIndex);

			ushort pid = handler.PathID;

#if ASTAR_JPS
			if (gg.useJumpPointSearch && !path.FloodingPath) {
				JPSOpen(path, pathNode, handler);
			} else
#endif
			{
				int[] neighbourOffsets = gg.neighbourOffsets;
				uint[] neighbourCosts = gg.neighbourCosts;
				GridNode[] nodes = gg.nodes;

				for (int i = 0; i < 8; i++) {
					if (GetConnectionInternal(i)) {
						GridNode other = nodes[nodeInGridIndex + neighbourOffsets[i]];
						if (!path.CanTraverse(other)) continue;

						PathNode otherPN = handler.GetPathNode(other);

						uint tmpCost = neighbourCosts[i];

						if (otherPN.pathID != pid) {
							otherPN.parent = pathNode;
							otherPN.pathID = pid;

							otherPN.cost = tmpCost;

							otherPN.H = path.CalculateHScore(other);
							other.UpdateG(path, otherPN);

							//Debug.Log ("G " + otherPN.G + " F " + otherPN.F);
							handler.PushNode(otherPN);
							//Debug.DrawRay ((Vector3)otherPN.node.Position, Vector3.up,Color.blue);
						} else {
							// Sorry for the huge number of #ifs

							//If not we can test if the path from the current node to this one is a better one then the one already used

#if ASTAR_NO_TRAVERSAL_COST
							if (pathNode.G+tmpCost < otherPN.G)
#else
							if (pathNode.G+tmpCost+path.GetTraversalCost(other) < otherPN.G)
#endif
							{
								//Debug.Log ("Path better from " + NodeIndex + " to " + otherPN.node.NodeIndex + " " + (pathNode.G+tmpCost+path.GetTraversalCost(other)) + " < " + otherPN.G);
								otherPN.cost = tmpCost;

								otherPN.parent = pathNode;

								other.UpdateRecursiveG(path, otherPN, handler);

								//Or if the path from this node ("other") to the current ("current") is better
							}
#if ASTAR_NO_TRAVERSAL_COST
							else if (otherPN.G+tmpCost < pathNode.G)
#else
							else if (otherPN.G+tmpCost+path.GetTraversalCost(this) < pathNode.G)
#endif
							{
								//Debug.Log ("Path better from " + otherPN.node.NodeIndex + " to " + NodeIndex + " " + (otherPN.G+tmpCost+path.GetTraversalCost (this)) + " < " + pathNode.G);
								pathNode.parent = otherPN;
								pathNode.cost = tmpCost;

								UpdateRecursiveG(path, pathNode, handler);
							}
						}
					}
				}
			}

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			if (connections != null) for (int i = 0; i < connections.Length; i++) {
					GraphNode other = connections[i];
					if (!path.CanTraverse(other)) continue;

					PathNode otherPN = handler.GetPathNode(other);

					uint tmpCost = connectionCosts[i];

					if (otherPN.pathID != pid) {
						otherPN.parent = pathNode;
						otherPN.pathID = pid;

						otherPN.cost = tmpCost;

						otherPN.H = path.CalculateHScore(other);
						other.UpdateG(path, otherPN);

						//Debug.Log ("G " + otherPN.G + " F " + otherPN.F);
						handler.PushNode(otherPN);
						//Debug.DrawRay ((Vector3)otherPN.node.Position, Vector3.up,Color.blue);
					} else {
						// Sorry for the huge number of #ifs

						//If not we can test if the path from the current node to this one is a better one then the one already used

#if ASTAR_NO_TRAVERSAL_COST
						if (pathNode.G+tmpCost < otherPN.G)
#else
						if (pathNode.G+tmpCost+path.GetTraversalCost(other) < otherPN.G)
#endif
						{
							//Debug.Log ("Path better from " + NodeIndex + " to " + otherPN.node.NodeIndex + " " + (pathNode.G+tmpCost+path.GetTraversalCost(other)) + " < " + otherPN.G);
							otherPN.cost = tmpCost;

							otherPN.parent = pathNode;

							other.UpdateRecursiveG(path, otherPN, handler);

							//Or if the path from this node ("other") to the current ("current") is better
						}
#if ASTAR_NO_TRAVERSAL_COST
						else if (otherPN.G+tmpCost < pathNode.G && other.ContainsConnection(this))
#else
						else if (otherPN.G+tmpCost+path.GetTraversalCost(this) < pathNode.G && other.ContainsConnection(this))
#endif
						{
							//Debug.Log ("Path better from " + otherPN.node.NodeIndex + " to " + NodeIndex + " " + (otherPN.G+tmpCost+path.GetTraversalCost (this)) + " < " + pathNode.G);
							pathNode.parent = otherPN;
							pathNode.cost = tmpCost;

							UpdateRecursiveG(path, pathNode, handler);
						}
					}
				}
#endif
		}

		public override void SerializeNode (GraphSerializationContext ctx) {
			base.SerializeNode(ctx);
			ctx.writer.Write(position.x);
			ctx.writer.Write(position.y);
			ctx.writer.Write(position.z);
			ctx.writer.Write(gridFlags);
		}

		public override void DeserializeNode (GraphSerializationContext ctx) {
			base.DeserializeNode(ctx);
			position = new Int3(ctx.reader.ReadInt32(), ctx.reader.ReadInt32(), ctx.reader.ReadInt32());
			gridFlags = ctx.reader.ReadUInt16();
		}
#else
		public override void AddConnection (GraphNode node, uint cost) {
			throw new System.NotImplementedException();
		}

		public override void ClearConnections (bool alsoReverse) {
			throw new System.NotImplementedException();
		}

		public override void GetConnections (GraphNodeDelegate del) {
			throw new System.NotImplementedException();
		}

		public override void Open (Path path, PathNode pathNode, PathHandler handler) {
			throw new System.NotImplementedException();
		}

		public override void RemoveConnection (GraphNode node) {
			throw new System.NotImplementedException();
		}
#endif
	}
}
