using UnityEngine;
using System.Collections;
using Pathfinding.Serialization;

namespace Pathfinding {
	/** Base class for GridNode and LevelGridNode */
	public abstract class GridNodeBase : GraphNode {
		protected GridNodeBase (AstarPath astar) : base(astar) {
		}

		const int GridFlagsWalkableErosionOffset = 8;
		const int GridFlagsWalkableErosionMask = 1 << GridFlagsWalkableErosionOffset;

		const int GridFlagsWalkableTmpOffset = 9;
		const int GridFlagsWalkableTmpMask = 1 << GridFlagsWalkableTmpOffset;

		protected int nodeInGridIndex;
		protected ushort gridFlags;

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
		public GraphNode[] connections;
		public uint[] connectionCosts;
#endif

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

		/** Stores walkability before erosion is applied.
		 * Used internally when updating the graph.
		 */
		public bool WalkableErosion {
			get {
				return (gridFlags & GridFlagsWalkableErosionMask) != 0;
			}
			set {
				unchecked { gridFlags = (ushort)(gridFlags & ~GridFlagsWalkableErosionMask | (value ? (ushort)GridFlagsWalkableErosionMask : (ushort)0)); }
			}
		}

		/** Temporary variable used internally when updating the graph. */
		public bool TmpWalkable {
			get {
				return (gridFlags & GridFlagsWalkableTmpMask) != 0;
			}
			set {
				unchecked { gridFlags = (ushort)(gridFlags & ~GridFlagsWalkableTmpMask | (value ? (ushort)GridFlagsWalkableTmpMask : (ushort)0)); }
			}
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
		public override void FloodFill (System.Collections.Generic.Stack<GraphNode> stack, uint region) {
			if (connections != null) for (int i = 0; i < connections.Length; i++) {
					GraphNode other = connections[i];
					if (other.Area != region) {
						other.Area = region;
						stack.Push(other);
					}
				}
		}

		public override void ClearConnections (bool alsoReverse) {
			if (alsoReverse) {
				if (connections != null) for (int i = 0; i < connections.Length; i++) connections[i].RemoveConnection(this);
			}
			connections = null;
			connectionCosts = null;
		}

		public override bool ContainsConnection (GraphNode node) {
			if (connections != null) {
				for (int i = 0; i < connections.Length; i++) {
					if (connections[i] == node) {
						return true;
					}
				}
			}

			return false;
		}

		public override void GetConnections (GraphNodeDelegate del) {
			if (connections != null) for (int i = 0; i < connections.Length; i++) del(connections[i]);
		}

		public override void UpdateRecursiveG (Path path, PathNode pathNode, PathHandler handler) {
			ushort pid = handler.PathID;

			if (connections != null) for (int i = 0; i < connections.Length; i++) {
					GraphNode other = connections[i];
					PathNode otherPN = handler.GetPathNode(other);
					if (otherPN.parent == pathNode && otherPN.pathID == pid) other.UpdateRecursiveG(path, otherPN, handler);
				}
		}

		public override void Open (Path path, PathNode pathNode, PathHandler handler) {
			ushort pid = handler.PathID;

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
		}

		/** Add a connection from this node to the specified node.
		 * If the connection already exists, the cost will simply be updated and
		 * no extra connection added.
		 *
		 * \note Only adds a one-way connection. Consider calling the same function on the other node
		 * to get a two-way connection.
		 */
		public override void AddConnection (GraphNode node, uint cost) {
			if (node == null) throw new System.ArgumentNullException();

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

		public override void SerializeReferences (GraphSerializationContext ctx) {
			// TODO: Deduplicate code
			if (connections == null) {
				ctx.writer.Write(-1);
			} else {
				ctx.writer.Write(connections.Length);
				for (int i = 0; i < connections.Length; i++) {
					ctx.SerializeNodeReference(connections[i]);
					ctx.writer.Write(connectionCosts[i]);
				}
			}
		}

		/** Cached to avoid allocations */
		static readonly System.Version VERSION_3_8_3 = new System.Version(3, 8, 3);

		public override void DeserializeReferences (GraphSerializationContext ctx) {
			// Grid nodes didn't serialize references before 3.8.3
			if (ctx.meta.version < VERSION_3_8_3)
				return;

			int count = ctx.reader.ReadInt32();

			if (count == -1) {
				connections = null;
				connectionCosts = null;
			} else {
				connections = new GraphNode[count];
				connectionCosts = new uint[count];

				for (int i = 0; i < count; i++) {
					connections[i] = ctx.DeserializeNodeReference();
					connectionCosts[i] = ctx.reader.ReadUInt32();
				}
			}
		}
#endif
	}
}
