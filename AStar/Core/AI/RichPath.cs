using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Util;

namespace Pathfinding {
	public class RichPath {
		int currentPart;
		readonly List<RichPathPart> parts = new List<RichPathPart>();

		public Seeker seeker;

		/** Use this for initialization.
		 *
		 * \param s Optionally provide in order to take tag penalties into account. May be null if you do not use a Seeker\
		 * \param p Path to follow
		 * \param mergePartEndpoints If true, then adjacent parts that the path is split up in will
		 * try to use the same start/end points. For example when using a link on a navmesh graph
		 * Instead of first following the path to the center of the node where the link is and then
		 * follow the link, the path will be adjusted to go to the exact point where the link starts
		 * which usually makes more sense.
		 * \param simplificationMode The path can optionally be simplified. This can be a bit expensive for long paths.
		 */
		public void Initialize (Seeker s, Path p, bool mergePartEndpoints, RichFunnel.FunnelSimplification simplificationMode) {
			if (p.error) throw new System.ArgumentException("Path has an error");

			List<GraphNode> nodes = p.path;
			if (nodes.Count == 0) throw new System.ArgumentException("Path traverses no nodes");

			seeker = s;
			// Release objects back to object pool
			// Yeah, I know, it's casting... but this won't be called much
			for (int i = 0; i < parts.Count; i++) {
				if (parts[i] is RichFunnel) ObjectPool<RichFunnel>.Release(parts[i] as RichFunnel);
				else if (parts[i] is RichSpecial) ObjectPool<RichSpecial>.Release(parts[i] as RichSpecial);
			}

			parts.Clear();
			currentPart = 0;

			// Initialize new

			//Break path into parts
			for (int i = 0; i < nodes.Count; i++) {
				if (nodes[i] is TriangleMeshNode) {
					var graph = AstarData.GetGraph(nodes[i]);
					RichFunnel f = ObjectPool<RichFunnel>.Claim().Initialize(this, graph);

					f.funnelSimplificationMode = simplificationMode;

					int sIndex = i;
					uint currentGraphIndex = nodes[sIndex].GraphIndex;


					for (; i < nodes.Count; i++) {
						if (nodes[i].GraphIndex != currentGraphIndex && !(nodes[i] is NodeLink3Node)) {
							break;
						}
					}
					i--;

					if (sIndex == 0) {
						f.exactStart = p.vectorPath[0];
					} else {
						f.exactStart = (Vector3)nodes[mergePartEndpoints ? sIndex-1 : sIndex].position;
					}

					if (i == nodes.Count-1) {
						f.exactEnd = p.vectorPath[p.vectorPath.Count-1];
					} else {
						f.exactEnd = (Vector3)nodes[mergePartEndpoints ? i+1 : i].position;
					}

					f.BuildFunnelCorridor(nodes, sIndex, i);

					parts.Add(f);
				} else if (NodeLink2.GetNodeLink(nodes[i]) != null) {
					NodeLink2 nl = NodeLink2.GetNodeLink(nodes[i]);

					int sIndex = i;
					uint currentGraphIndex = nodes[sIndex].GraphIndex;

					for (i++; i < nodes.Count; i++) {
						if (nodes[i].GraphIndex != currentGraphIndex) {
							break;
						}
					}
					i--;

					if (i - sIndex > 1) {
						throw new System.Exception("NodeLink2 path length greater than two (2) nodes. " + (i - sIndex));
					} else if (i - sIndex == 0) {
						//Just continue, it might be the case that a NodeLink was the closest node
						continue;
					}

					RichSpecial rps = ObjectPool<RichSpecial>.Claim().Initialize(nl, nodes[sIndex]);
					parts.Add(rps);
				}
			}
		}

		public bool PartsLeft () {
			return currentPart < parts.Count;
		}

		public void NextPart () {
			currentPart++;
			if (currentPart >= parts.Count) currentPart = parts.Count;
		}

		public RichPathPart GetCurrentPart () {
			return currentPart < parts.Count ? parts[currentPart] : null;
		}
	}

	public abstract class RichPathPart : Pathfinding.Util.IAstarPooledObject {
		public abstract void OnEnterPool ();
	}

	public class RichFunnel : RichPathPart {
		readonly List<Vector3> left;
		readonly List<Vector3> right;
		List<TriangleMeshNode> nodes;
		public Vector3 exactStart;
		public Vector3 exactEnd;
		NavGraph graph;
		int currentNode;
		Vector3 currentPosition;
		int checkForDestroyedNodesCounter;
		RichPath path;
		int[] triBuffer = new int[3];

		/** Different algorithms for simplifying a funnel corridor using linecasting.
		 * This makes the most sense when using tiled navmeshes. A lot of times, the funnel corridor can be improved by using funnel simplification.
		 * You will have to experiment and see which one of these give the best result on your map.
		 * In my own tests, iterative has usually given the best results, closesly followed by RecursiveTrinary which was the fastest one, recursive binary came last with
		 * both the worst quality and slowest execution time. But on other maps, it might be totally different.
		 */
		public enum FunnelSimplification {
			None,
			Iterative,
			RecursiveBinary,
			RecursiveTrinary
		}

		/** How to post process the funnel corridor */
		public FunnelSimplification funnelSimplificationMode = FunnelSimplification.Iterative;

		public RichFunnel () {
			left = Pathfinding.Util.ListPool<Vector3>.Claim();
			right = Pathfinding.Util.ListPool<Vector3>.Claim();
			nodes = new List<TriangleMeshNode>();
			this.graph = null;
		}

		/** Works like a constructor, but can be used even for pooled objects. Returns \a this for easy chaining */
		public RichFunnel Initialize (RichPath path, NavGraph graph) {
			if (graph == null) throw new System.ArgumentNullException("graph");
			if (this.graph != null) throw new System.InvalidOperationException("Trying to initialize an already initialized object. " + graph);

			this.graph = graph;
			this.path = path;
			return this;
		}

		public override void OnEnterPool () {
			left.Clear();
			right.Clear();
			nodes.Clear();
			graph = null;
			currentNode = 0;
			checkForDestroyedNodesCounter = 0;
		}

		/** Build a funnel corridor from a node list slice.
		 * The nodes are assumed to be of type TriangleMeshNode.
		 *
		 * \param nodes Nodes to build the funnel corridor from
		 * \param start Start index in the nodes list
		 * \param end End index in the nodes list, this index is inclusive
		 */
		public void BuildFunnelCorridor (List<GraphNode> nodes, int start, int end) {
			//Make sure start and end points are on the correct nodes
			exactStart = (nodes[start] as MeshNode).ClosestPointOnNode(exactStart);
			exactEnd = (nodes[end] as MeshNode).ClosestPointOnNode(exactEnd);

			left.Clear();
			right.Clear();
			left.Add(exactStart);
			right.Add(exactStart);


			this.nodes.Clear();

			var rcg = graph as IRaycastableGraph;
			if (rcg != null && funnelSimplificationMode != FunnelSimplification.None) {
				List<GraphNode> tmp = Pathfinding.Util.ListPool<GraphNode>.Claim(end-start);

				switch (funnelSimplificationMode) {
				case FunnelSimplification.Iterative:
					SimplifyPath(rcg, nodes, start, end, tmp, exactStart, exactEnd);
					break;
				case FunnelSimplification.RecursiveBinary:
					SimplifyPath2(rcg, nodes, start, end, tmp, exactStart, exactEnd);
					break;
				case FunnelSimplification.RecursiveTrinary:
					SimplifyPath3(rcg, nodes, start, end, tmp, exactStart, exactEnd);
					break;
				}

				if (this.nodes.Capacity < tmp.Count) this.nodes.Capacity = tmp.Count;

				for (int i = 0; i < tmp.Count; i++) {
					//Guaranteed to be TriangleMeshNodes since they are all in the same graph
					var nd = tmp[i] as TriangleMeshNode;
					if (nd != null) this.nodes.Add(nd);
				}

				Pathfinding.Util.ListPool<GraphNode>.Release(tmp);
			} else {
				if (this.nodes.Capacity < end-start) this.nodes.Capacity = (end-start);
				for (int i = start; i <= end; i++) {
					//Guaranteed to be TriangleMeshNodes since they are all in the same graph
					var nd = nodes[i] as TriangleMeshNode;
					if (nd != null) this.nodes.Add(nd);
				}
			}

			for (int i = 0; i < this.nodes.Count-1; i++) {
				/** \todo should use return value in future versions */
				this.nodes[i].GetPortal(this.nodes[i+1], left, right, false);
			}

			left.Add(exactEnd);
			right.Add(exactEnd);
		}

		public static void SimplifyPath3 (IRaycastableGraph rcg, List<GraphNode> nodes, int start, int end, List<GraphNode> result, Vector3 startPoint, Vector3 endPoint, int depth = 0) {
			if (start == end) {
				result.Add(nodes[start]);
				return;
			}
			if (start+1 == end) {
				result.Add(nodes[start]);
				result.Add(nodes[end]);
				return;
			}

			int resCount = result.Count;

			GraphHitInfo hit;
			bool linecast = rcg.Linecast(startPoint, endPoint, nodes[start], out hit, result);
			if (linecast || result[result.Count-1] != nodes[end]) {
				//Debug.DrawLine (startPoint, endPoint, Color.black);
				//Obstacle
				//Refine further
				result.RemoveRange(resCount, result.Count-resCount);

				int maxDistNode = 0;
				float maxDist = 0;
				for (int i = start+1; i < end-1; i++) {
					float dist = VectorMath.SqrDistancePointSegment(startPoint, endPoint, (Vector3)nodes[i].position);
					if (dist > maxDist) {
						maxDistNode = i;
						maxDist = dist;
					}
				}

				int mid1 = (maxDistNode+start)/2;
				int mid2 = (maxDistNode+end)/2;

				if (mid1 == mid2) {
					SimplifyPath3(rcg, nodes, start, mid1, result, startPoint, (Vector3)nodes[mid1].position);
					//Remove start node of next part so that it is not added twice
					result.RemoveAt(result.Count-1);
					SimplifyPath3(rcg, nodes, mid1, end, result, (Vector3)nodes[mid1].position, endPoint, depth+1);
				} else {
					SimplifyPath3(rcg, nodes, start, mid1, result, startPoint, (Vector3)nodes[mid1].position, depth+1);

					//Remove start node of next part so that it is not added twice
					result.RemoveAt(result.Count-1);
					SimplifyPath3(rcg, nodes, mid1, mid2, result, (Vector3)nodes[mid1].position, (Vector3)nodes[mid2].position, depth+1);

					//Remove start node of next part so that it is not added twice
					result.RemoveAt(result.Count-1);
					SimplifyPath3(rcg, nodes, mid2, end, result, (Vector3)nodes[mid2].position, endPoint, depth+1);
				}
			}
		}

		public static void SimplifyPath2 (IRaycastableGraph rcg, List<GraphNode> nodes, int start, int end, List<GraphNode> result, Vector3 startPoint, Vector3 endPoint) {
			int resCount = result.Count;

			if (end <= start+1) {
				result.Add(nodes[start]);
				result.Add(nodes[end]);
				//t--;
				return;
			}

			GraphHitInfo hit;
			if ((rcg.Linecast(startPoint, endPoint, nodes[start], out hit, result) || result[result.Count-1] != nodes[end])) {
				//Obstacle
				//Refine further
				result.RemoveRange(resCount, result.Count-resCount);

				int minDistNode = -1;
				float minDist = float.PositiveInfinity;
				for (int i = start+1; i < end; i++) {
					float dist = VectorMath.SqrDistancePointSegment(startPoint, endPoint, (Vector3)nodes[i].position);
					if (minDistNode == -1 || dist < minDist) {
						minDistNode = i;
						minDist = dist;
					}
				}

				SimplifyPath2(rcg, nodes, start, minDistNode, result, startPoint, (Vector3)nodes[minDistNode].position);
				//Remove start node of next part so that it is not added twice
				result.RemoveAt(result.Count-1);
				SimplifyPath2(rcg, nodes, minDistNode, end, result, (Vector3)nodes[minDistNode].position, endPoint);
			}
		}

		/** Simplifies a funnel path using linecasting.
		 * Running time is roughly O(n^2 log n) in the worst case (where n = end-start)
		 * Actually it depends on how the graph looks, so in theory the actual upper limit on the worst case running time is O(n*m log n) (where n = end-start and m = nodes in the graph)
		 * but O(n^2 log n) is a much more realistic worst case limit.
		 *
		 * Requires #graph to implement IRaycastableGraph
		 */
		public void SimplifyPath (IRaycastableGraph graph, List<GraphNode> nodes, int start, int end, List<GraphNode> result, Vector3 startPoint, Vector3 endPoint) {
			if (graph == null) throw new System.ArgumentNullException("graph");

			if (start > end) {
				throw new System.ArgumentException("start >= end");
			}

			int ostart = start;

			int count = 0;
			while (true) {
				if (count++ > 1000) {
					Debug.LogError("!!!");
					break;
				}

				if (start == end) {
					result.Add(nodes[end]);
					return;
				}

				int resCount = result.Count;

				int mx = end+1;
				int mn = start+1;
				bool anySucceded = false;
				while (mx > mn+1) {
					int mid = (mx+mn)/2;

					GraphHitInfo hit;
					Vector3 sp = start == ostart ? startPoint : (Vector3)nodes[start].position;
					Vector3 ep = mid == end ? endPoint : (Vector3)nodes[mid].position;

					if (graph.Linecast(sp, ep, nodes[start], out hit)) {
						mx = mid;
					} else {
						anySucceded = true;
						mn = mid;
					}
				}

				if (!anySucceded) {
					result.Add(nodes[start]);

					//It is guaranteed that mn = start+1
					start = mn;
				} else {
					//Need to redo the linecast to get the trace
					GraphHitInfo hit;
					Vector3 sp = start == ostart ? startPoint : (Vector3)nodes[start].position;
					Vector3 ep = mn == end ? endPoint : (Vector3)nodes[mn].position;
					graph.Linecast(sp, ep, nodes[start], out hit, result);

					long penaltySum = 0;
					long penaltySum2 = 0;
					for (int i = start; i <= mn; i++) {
						penaltySum += nodes[i].Penalty + (path.seeker != null ? path.seeker.tagPenalties[nodes[i].Tag] : 0);
					}

					for (int i = resCount; i < result.Count; i++) {
						penaltySum2 += result[i].Penalty + (path.seeker != null ? path.seeker.tagPenalties[result[i].Tag] : 0);
					}

					// Allow 40% more penalty on average per node
					if ((penaltySum*1.4*(mn-start+1)) < (penaltySum2*(result.Count-resCount)) || result[result.Count-1] != nodes[mn]) {
						//Debug.Log ((penaltySum*1.4*(mn-start+1)) + " < "+ (penaltySum2*(result.Count-resCount)));
						//Debug.DrawLine ((Vector3)nodes[start].Position, (Vector3)nodes[mn].Position, Color.red);
						//Linecast hit the wrong node
						result.RemoveRange(resCount, result.Count-resCount);

						result.Add(nodes[start]);
						//Debug.Break();
						start = start+1;
					} else {
						//Debug.Log ("!! " + (penaltySum*1.4*(mn-start+1)) + " < "+ (penaltySum2*(result.Count-resCount)));
						//Debug.DrawLine ((Vector3)nodes[start].Position, (Vector3)nodes[mn].Position, Color.green);
						//Debug.Break ();
						//Remove nodes[end]
						result.RemoveAt(result.Count-1);
						start = mn;
					}
				}
			}
		}

		/** Split funnel at node index \a splitIndex and throw the nodes up to that point away and replace with \a prefix.
		 * Used when the AI has happened to get sidetracked and entered a node outside the funnel.
		 */
		public void UpdateFunnelCorridor (int splitIndex, TriangleMeshNode prefix) {
			if (splitIndex > 0) {
				nodes.RemoveRange(0, splitIndex-1);
				//This is a node which should be removed, we replace it with the prefix
				nodes[0] = prefix;
			} else {
				nodes.Insert(0, prefix);
			}

			left.Clear();
			right.Clear();
			left.Add(exactStart);
			right.Add(exactStart);

			for (int i = 0; i < nodes.Count-1; i++) {
				//NOTE should use return value in future versions
				nodes[i].GetPortal(nodes[i+1], left, right, false);
			}

			left.Add(exactEnd);
			right.Add(exactEnd);
		}

		public Vector3 Update (Vector3 position, List<Vector3> buffer, int numCorners, out bool lastCorner, out bool requiresRepath) {
			lastCorner = false;
			requiresRepath = false;
			var i3Pos = (Int3)position;

			if (nodes[currentNode].Destroyed) {
				requiresRepath = true;
				lastCorner = false;
				buffer.Add(position);
				return position;
			}

			// Check if we are in the same node as we were in during the last frame
			if (nodes[currentNode].ContainsPoint(i3Pos)) {
				// Only check for destroyed nodes every 10 frames
				if (checkForDestroyedNodesCounter >= 10) {
					checkForDestroyedNodesCounter = 0;

					// Loop through all nodes and check if they are destroyed
					// If so, we really need a recalculation of our path quickly
					// since there might be an obstacle blocking our path after
					// a graph update or something similar
					for (int i = 0, t = nodes.Count; i < t; i++) {
						if (nodes[i].Destroyed) {
							requiresRepath = true;
							break;
						}
					}
				} else {
					checkForDestroyedNodesCounter++;
				}
			} else {
				// This part of the code is relatively seldom called
				// Most of the time we are still on the same node as during the previous frame

				// Otherwise check the 2 nodes ahead and 2 nodes back
				// If they contain the node in XZ space, then we probably moved into those nodes
				bool found = false;

				// 2 nodes ahead
				for (int i = currentNode+1, t = System.Math.Min(currentNode+3, nodes.Count); i < t && !found; i++) {
					// If the node is destroyed, make sure we recalculate a new path quickly
					if (nodes[i].Destroyed) {
						requiresRepath = true;
						lastCorner = false;
						buffer.Add(position);
						return position;
					}

					// We found a node which contains our current position in XZ space
					if (nodes[i].ContainsPoint(i3Pos)) {
						currentNode = i;
						found = true;
					}
				}

				// 2 nodes behind
				for (int i = currentNode-1, t = System.Math.Max(currentNode-3, 0); i > t && !found; i--) {
					if (nodes[i].Destroyed) {
						requiresRepath = true;
						lastCorner = false;
						buffer.Add(position);
						return position;
					}

					if (nodes[i].ContainsPoint(i3Pos)) {
						currentNode = i;
						found = true;
					}
				}

				if (!found) {
					int closestNodeInPath = 0;
					int closestIsNeighbourOf = 0;
					float closestDist = float.PositiveInfinity;
					bool closestIsInPath = false;
					TriangleMeshNode closestNode = null;

					int containingIndex = nodes.Count-1;

					// If we still couldn't find a good node
					// Check all nodes in the whole path

					// We are checking for if any node is destroyed in the loop
					// So we can reset this counter
					checkForDestroyedNodesCounter = 0;

					for (int i = 0, t = nodes.Count; i < t; i++) {
						if (nodes[i].Destroyed) {
							requiresRepath = true;
							lastCorner = false;
							buffer.Add(position);
							return position;
						}

						Vector3 close = nodes[i].ClosestPointOnNode(position);
						float d = (close-position).sqrMagnitude;
						if (d < closestDist) {
							closestDist = d;
							closestNodeInPath = i;
							closestNode = nodes[i];
							closestIsInPath = true;
						}
					}

					// Loop through all neighbours of all nodes in the path
					// and find the closet point on them
					// We cannot just look on the ones in the path since it is impossible
					// to know if we are outside the navmesh completely or if we have just
					// stepped in to an adjacent node

					// Need to make a copy here, the JIT will move this variable to the heap
					// because it is used inside a delegate, if we didn't make a copy here
					// we would *always* allocate 24 bytes (sizeof(position)) on the heap every time
					// this method was called
					// now we only do it when this IF statement is executed
					var posCopy = position;

					GraphNodeDelegate del = node => {
						// Check so that this neighbour we are processing is neither the node after the current node or the node before the current node in the path
						// This is done for optimization, we have already checked those nodes earlier
						if (!(containingIndex > 0 && node == nodes[containingIndex-1]) && !(containingIndex < nodes.Count-1 && node == nodes[containingIndex+1])) {
							// Check if the neighbour was a mesh node
							var mn = node as TriangleMeshNode;
							if (mn != null) {
								// Find the distance to the closest point on it from our current position
								var close = mn.ClosestPointOnNode(posCopy);
								float d = (close-posCopy).sqrMagnitude;

								// Is that distance better than the best distance seen so far
								if (d < closestDist) {
									closestDist = d;
									closestIsNeighbourOf = containingIndex;
									closestNode = mn;
									closestIsInPath = false;
								}
							}
						}
					};

					// Loop through all the nodes in the path in reverse order
					// The callback needs to know about the index, so we store it
					// in a local variable which it can read
					for (; containingIndex >= 0; containingIndex--) {
						// Loop through all neighbours of the node
						nodes[containingIndex].GetConnections(del);
					}

					// Check if the closest node
					// was on the path already or if we need to adjust it
					if (closestIsInPath) {
						// If we have found a node
						// Snap to the closest point in XZ space (keep the Y coordinate)
						// If we would have snapped to the closest point in 3D space, the agent
						// might slow down when traversing slopes
						currentNode = closestNodeInPath;
						position = nodes[closestNodeInPath].ClosestPointOnNodeXZ(position);
					} else {
						// Snap to the closest point in XZ space on the node
						position = closestNode.ClosestPointOnNodeXZ(position);

						// We have found a node containing the position, but it is outside the funnel
						// Recalculate the funnel to include this node
						exactStart = position;
						UpdateFunnelCorridor(closestIsNeighbourOf, closestNode);

						// Restart from the first node in the updated path
						currentNode = 0;
					}
				}
			}

			currentPosition = position;


			if (!FindNextCorners(position, currentNode, buffer, numCorners, out lastCorner)) {
				Debug.LogError("Oh oh");
				buffer.Add(position);
				return position;
			}

			return position;
		}

		/** Fill wallBuffer with all navmesh wall segments close to the current position.
		 * A wall segment is a node edge which is not shared by any other neighbour node, i.e an outer edge on the navmesh.
		 */
		public void FindWalls (List<Vector3> wallBuffer, float range) {
			FindWalls(currentNode, wallBuffer, currentPosition, range);
		}

		void FindWalls (int nodeIndex, List<Vector3> wallBuffer, Vector3 position, float range) {
			if (range <= 0) return;

			bool negAbort = false;
			bool posAbort = false;

			range *= range;

			position.y = 0;
			//Looping as 0,-1,1,-2,2,-3,3,-4,4 etc. Avoids code duplication by keeping it to one loop instead of two
			for (int i = 0; !negAbort || !posAbort; i = i < 0 ? -i : -i-1) {
				if (i < 0 && negAbort) continue;
				if (i > 0 && posAbort) continue;

				if (i < 0 && nodeIndex+i < 0) {
					negAbort = true;
					continue;
				}

				if (i > 0 && nodeIndex+i >= nodes.Count) {
					posAbort = true;
					continue;
				}

				TriangleMeshNode prev = nodeIndex+i-1 < 0 ? null : nodes[nodeIndex+i-1];
				TriangleMeshNode node = nodes[nodeIndex+i];
				TriangleMeshNode next = nodeIndex+i+1 >= nodes.Count ? null : nodes[nodeIndex+i+1];

				if (node.Destroyed) {
					break;
				}

				if ((node.ClosestPointOnNodeXZ(position)-position).sqrMagnitude > range) {
					if (i < 0) negAbort = true;
					else posAbort = true;
					continue;
				}

				for (int j = 0; j < 3; j++) triBuffer[j] = 0;

				for (int j = 0; j < node.connections.Length; j++) {
					var other = node.connections[j] as TriangleMeshNode;
					if (other == null) continue;

					int va = -1;
					for (int a = 0; a < 3; a++) {
						for (int b = 0; b < 3; b++) {
							if (node.GetVertex(a) == other.GetVertex((b+1) % 3) && node.GetVertex((a+1) % 3) == other.GetVertex(b)) {
								va = a;
								a = 3;
								break;
							}
						}
					}
					if (va == -1) {
						//No direct connection
					} else {
						triBuffer[va] = other == prev || other == next ? 2 : 1;
					}
				}

				for (int j = 0; j < 3; j++) {
					//Tribuffer values
					// 0 : Navmesh border, outer edge
					// 1 : Inner edge, to node inside funnel
					// 2 : Inner edge, to node outside funnel
					if (triBuffer[j] == 0) {
						//Add edge to list of walls
						wallBuffer.Add((Vector3)node.GetVertex(j));
						wallBuffer.Add((Vector3)node.GetVertex((j+1) % 3));
					}
				}
			}
		}

		public bool FindNextCorners (Vector3 origin, int startIndex, List<Vector3> funnelPath, int numCorners, out bool lastCorner) {
			lastCorner = false;

			if (left == null) throw new System.Exception("left list is null");
			if (right == null) throw new System.Exception("right list is null");
			if (funnelPath == null) throw new System.ArgumentNullException("funnelPath");

			if (left.Count != right.Count) throw new System.ArgumentException("left and right lists must have equal length");

			int diagonalCount = left.Count;

			if (diagonalCount == 0) throw new System.ArgumentException("no diagonals");

			if (diagonalCount-startIndex < 3) {
				//Direct path
				funnelPath.Add(left[diagonalCount-1]);
				lastCorner = true;
				return true;
			}

			#if ASTARDEBUG
			for (int i = startIndex; i < left.Count-1; i++) {
				Debug.DrawLine(left[i], left[i+1], Color.red);
				Debug.DrawLine(right[i], right[i+1], Color.magenta);
				Debug.DrawRay(right[i], Vector3.up, Color.magenta);
			}
			for (int i = 0; i < left.Count; i++) {
				Debug.DrawLine(right[i], left[i], Color.cyan);
			}
			#endif

			//Remove identical vertices
			while (left[startIndex+1] == left[startIndex+2] && right[startIndex+1] == right[startIndex+2]) {
				//System.Console.WriteLine ("Removing identical left and right");
				//left.RemoveAt (1);
				//right.RemoveAt (1);
				startIndex++;

				if (diagonalCount-startIndex <= 3) {
					return false;
				}
			}

			Vector3 swPoint = left[startIndex+2];
			if (swPoint == left[startIndex+1]) {
				swPoint = right[startIndex+2];
			}


			//Test
			while (VectorMath.IsColinearXZ(origin, left[startIndex+1], right[startIndex+1]) || VectorMath.RightOrColinearXZ(left[startIndex+1], right[startIndex+1], swPoint) == VectorMath.RightOrColinearXZ(left[startIndex+1], right[startIndex+1], origin)) {
	#if ASTARDEBUG
				Debug.DrawLine(left[startIndex+1], right[startIndex+1], new Color(0, 0, 0, 0.5F));
				Debug.DrawLine(origin, swPoint, new Color(0, 0, 0, 0.5F));
	#endif
				//left.RemoveAt (1);
				//right.RemoveAt (1);
				startIndex++;

				if (diagonalCount-startIndex < 3) {
					//Debug.Log ("#2 " + left.Count + " - " + startIndex + " = " + (left.Count-startIndex));
					//Direct path
					funnelPath.Add(left[diagonalCount-1]);
					lastCorner = true;
					return true;
				}

				swPoint = left[startIndex+2];
				if (swPoint == left[startIndex+1]) {
					swPoint = right[startIndex+2];
				}
			}


			//funnelPath.Add (origin);

			Vector3 portalApex = origin;
			Vector3 portalLeft = left[startIndex+1];
			Vector3 portalRight = right[startIndex+1];

			int apexIndex = startIndex+0;
			int rightIndex = startIndex+1;
			int leftIndex = startIndex+1;

			for (int i = startIndex+2; i < diagonalCount; i++) {
				if (funnelPath.Count >= numCorners) {
					return true;
				}

				if (funnelPath.Count > 2000) {
					Debug.LogWarning("Avoiding infinite loop. Remove this check if you have this long paths.");
					break;
				}

				Vector3 pLeft = left[i];
				Vector3 pRight = right[i];

				/*Debug.DrawLine (portalApex,portalLeft,Color.red);
				 * Debug.DrawLine (portalApex,portalRight,Color.yellow);
				 * Debug.DrawLine (portalApex,left,Color.cyan);
				 * Debug.DrawLine (portalApex,right,Color.cyan);*/

				if (VectorMath.SignedTriangleAreaTimes2XZ(portalApex, portalRight, pRight) >= 0) {
					if (portalApex == portalRight || VectorMath.SignedTriangleAreaTimes2XZ(portalApex, portalLeft, pRight) <= 0) {
						portalRight = pRight;
						rightIndex = i;
					} else {
						funnelPath.Add(portalLeft);
						portalApex = portalLeft;
						apexIndex = leftIndex;

						portalLeft = portalApex;
						portalRight = portalApex;

						leftIndex = apexIndex;
						rightIndex = apexIndex;

						i = apexIndex;

						continue;
					}
				}

				if (VectorMath.SignedTriangleAreaTimes2XZ(portalApex, portalLeft, pLeft) <= 0) {
					if (portalApex == portalLeft || VectorMath.SignedTriangleAreaTimes2XZ(portalApex, portalRight, pLeft) >= 0) {
						portalLeft = pLeft;
						leftIndex = i;
					} else {
						funnelPath.Add(portalRight);
						portalApex = portalRight;
						apexIndex = rightIndex;

						portalLeft = portalApex;
						portalRight = portalApex;

						leftIndex = apexIndex;
						rightIndex = apexIndex;

						i = apexIndex;

						continue;
					}
				}
			}

			lastCorner = true;
			funnelPath.Add(left[diagonalCount-1]);

			return true;
		}
	}

	public class RichSpecial : RichPathPart {
		public NodeLink2 nodeLink;
		public Transform first;
		public Transform second;
		public bool reverse;

		public override void OnEnterPool () {
			nodeLink = null;
		}

		/** Works like a constructor, but can be used even for pooled objects. Returns \a this for easy chaining */
		public RichSpecial Initialize (NodeLink2 nodeLink, GraphNode first) {
			this.nodeLink = nodeLink;
			if (first == nodeLink.StartNode) {
				this.first = nodeLink.StartTransform;
				this.second = nodeLink.EndTransform;
				reverse = false;
			} else {
				this.first = nodeLink.EndTransform;
				this.second = nodeLink.StartTransform;
				reverse = true;
			}
			return this;
		}
	}
}
