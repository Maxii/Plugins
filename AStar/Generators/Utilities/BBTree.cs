//#define ASTARDEBUG   //"BBTree Debug" If enables, some queries to the tree will show debug lines. Turn off multithreading when using this since DrawLine calls cannot be called from a different thread

using System;
using UnityEngine;

namespace Pathfinding {
	using Pathfinding;

	/** Axis Aligned Bounding Box Tree.
	 * Holds a bounding box tree of triangles.
	 *
	 * \astarpro
	 */
	public class BBTree {
		/** Holds an Axis Aligned Bounding Box Tree used for faster node lookups.
		 * \astarpro
		 */
		BBTreeBox[] arr = new BBTreeBox[6];
		int count;

		public Rect Size {
			get {
				if (count == 0) {
					return new Rect(0, 0, 0, 0);
				} else {
					var rect = arr[0].rect;
					return Rect.MinMaxRect(rect.xmin*Int3.PrecisionFactor, rect.ymin*Int3.PrecisionFactor, rect.xmax*Int3.PrecisionFactor, rect.ymax*Int3.PrecisionFactor);
				}
			}
		}

		/** Clear the tree.
		 * Note that references to old nodes will still be intact so the GC cannot immediately collect them.
		 */
		public void Clear () {
			count = 0;
		}

		void EnsureCapacity (int c) {
			if (arr.Length < c) {
				var narr = new BBTreeBox[Math.Max(c, (int)(arr.Length*1.5f))];
				for (int i = 0; i < count; i++) {
					narr[i] = arr[i];
				}
				arr = narr;
			}
		}

		int GetBox (MeshNode node, IntRect bounds) {
			if (count >= arr.Length) EnsureCapacity(count+1);

			arr[count] = new BBTreeBox(node, bounds);
			count++;
			return count-1;
		}

		int GetBox (IntRect rect) {
			if (count >= arr.Length) EnsureCapacity(count+1);

			arr[count] = new BBTreeBox(rect);
			count++;
			return count-1;
		}

		public static System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

		/** Rebuilds the tree using the specified nodes.
		 * This is faster and gives better quality results compared to calling Insert with all nodes
		 */
		public void RebuildFrom (MeshNode[] nodes) {
			Clear();

			if (nodes.Length == 0) {
				return;
			}

			// We will use approximately 2N tree nodes
			EnsureCapacity(Mathf.CeilToInt(nodes.Length * 2.1f));

			// This will store the order of the nodes while the tree is being built
			// It turns out that it is a lot faster to do this than to actually modify
			// the nodes and nodeBounds arrays (presumably since that involves shuffling
			// around 20 bytes of memory (sizeof(pointer) + sizeof(IntRect)) per node
			// instead of 4 bytes (sizeof(int)).
			// It also means we don't have to make a copy of the nodes array since
			// we do not modify it
			var permutation = new int[nodes.Length];
			for (int i = 0; i < nodes.Length; i++) {
				permutation[i] = i;
			}

			// Precalculate the bounds of the nodes in XZ space.
			// It turns out that calculating the bounds is a bottleneck and precalculating
			// the bounds makes it around 3 times faster to build a tree
			var nodeBounds = new IntRect[nodes.Length];
			for (int i = 0; i < nodes.Length; i++) {
				var node = nodes[i];
				var v0 = node.GetVertex(0);
				var v1 = node.GetVertex(1);
				var v2 = node.GetVertex(2);

				var r = new IntRect(v0.x, v0.z, v0.x, v0.z);
				r = r.ExpandToContain(v1.x, v1.z);
				r = r.ExpandToContain(v2.x, v2.z);
				nodeBounds[i] = r;
			}

			RebuildFromInternal(nodes, permutation, nodeBounds, 0, nodes.Length, false);
		}

		static int SplitByX (MeshNode[] nodes, int[] permutation, int from, int to, int divider) {
			int mx = to;

			for (int i = from; i < mx; i++) {
				if (nodes[permutation[i]].position.x > divider) {
					mx--;
					// Swap items i and mx
					var tmp = permutation[mx];
					permutation[mx] = permutation[i];
					permutation[i] = tmp;
					i--;
				}
			}
			return mx;
		}

		static int SplitByZ (MeshNode[] nodes, int[] permutation, int from, int to, int divider) {
			int mx = to;

			for (int i = from; i < mx; i++) {
				if (nodes[permutation[i]].position.z > divider) {
					mx--;
					// Swap items i and mx
					var tmp = permutation[mx];
					permutation[mx] = permutation[i];
					permutation[i] = tmp;
					i--;
				}
			}
			return mx;
		}

		int RebuildFromInternal (MeshNode[] nodes, int[] permutation, IntRect[] nodeBounds, int from, int to, bool odd) {
			if (to - from == 1) {
				return GetBox(nodes[permutation[from]], nodeBounds[permutation[from]]);
			}

			var rect = NodeBounds(permutation, nodeBounds, from, to);
			int box = GetBox(rect);

			// Performance optimization for a common case
			if (to - from == 2) {
				arr[box].left = GetBox(nodes[permutation[from]], nodeBounds[permutation[from]]);
				arr[box].right = GetBox(nodes[permutation[from+1]], nodeBounds[permutation[from+1]]);
				return box;
			}

			int mx;
			if (odd) {
				// X
				int divider = (rect.xmin + rect.xmax)/2;
				mx = SplitByX(nodes, permutation, from, to, divider);
			} else {
				// Y/Z
				int divider = (rect.ymin + rect.ymax)/2;
				mx = SplitByZ(nodes, permutation, from, to, divider);
			}

			if (mx == from || mx == to) {
				// All nodes were on one side of the divider
				// Try to split along the other axis

				if (!odd) {
					// X
					int divider = (rect.xmin + rect.xmax)/2;
					mx = SplitByX(nodes, permutation, from, to, divider);
				} else {
					// Y/Z
					int divider = (rect.ymin + rect.ymax)/2;
					mx = SplitByZ(nodes, permutation, from, to, divider);
				}

				if (mx == from || mx == to) {
					// All nodes were on one side of the divider
					// Just pick one half
					mx = (from+to)/2;
				}
			}

			arr[box].left = RebuildFromInternal(nodes, permutation, nodeBounds, from, mx, !odd);
			arr[box].right = RebuildFromInternal(nodes, permutation, nodeBounds, mx, to, !odd);

			return box;
		}

		/** Calculates the bounding box in XZ space of all nodes between \a from (inclusive) and \a to (exclusive) */
		static IntRect NodeBounds (int[] permutation, IntRect[] nodeBounds, int from, int to) {
			if (to - from <= 0) throw new ArgumentException();

			var r = nodeBounds[permutation[from]];
			for (int j = from + 1; j < to; j++) {
				var r2 = nodeBounds[permutation[j]];

				// Equivalent to r = IntRect.Union(r, r2)
				// but manually inlining is approximately
				// 25% faster when building an entire tree.
				// This code is hot when using navmesh cutting.
				r.xmin = System.Math.Min(r.xmin, r2.xmin);
				r.ymin = System.Math.Min(r.ymin, r2.ymin);
				r.xmax = System.Math.Max(r.xmax, r2.xmax);
				r.ymax = System.Math.Max(r.ymax, r2.ymax);
			}

			return r;
		}

		public NNInfo Query (Vector3 p, NNConstraint constraint) {
			if (count == 0) return new NNInfo(null);

			var nnInfo = new NNInfo();

			SearchBox(0, p, constraint, ref nnInfo);

			nnInfo.UpdateInfo();

			return nnInfo;
		}

		/** Queries the tree for the best node, searching within a circle around \a p with the specified radius.
		 * Will fill in both the constrained node and the not constrained node in the NNInfo.
		 *
		 * \see QueryClosest
		 */
		public NNInfo QueryCircle (Vector3 p, float radius, NNConstraint constraint) {
			if (count == 0) return new NNInfo(null);

			var nnInfo = new NNInfo(null);

			SearchBoxCircle(0, p, radius, constraint, ref nnInfo);

			nnInfo.UpdateInfo();

			return nnInfo;
		}

		/** Queries the tree for the closest node to \a p constrained by the NNConstraint.
		 * Note that this function will, unlike QueryCircle, only fill in the constrained node.
		 * If you want a node not constrained by any NNConstraint, do an additional search with constraint = NNConstraint.None
		 *
		 * \see QueryCircle
		 */
		public NNInfo QueryClosest (Vector3 p, NNConstraint constraint, out float distance) {
			distance = float.PositiveInfinity;
			return QueryClosest(p, constraint, ref distance, new NNInfo(null));
		}

		/** Queries the tree for the closest node to \a p constrained by the NNConstraint trying to improve an existing solution.
		 * Note that this function will, unlike QueryCircle, only fill in the constrained node.
		 * If you want a node not constrained by any NNConstraint, do an additional search with constraint = NNConstraint.None
		 *
		 * This method will completely ignore any Y-axis differences in positions.
		 *
		 * \param p Point to search around
		 * \param constraint Optionally set to constrain which nodes to return
		 * \param distance The best distance for the \a previous solution. Will be updated with the best distance
		 * after this search. Will be positive infinity if no node could be found.
		 * Set to positive infinity if there was no previous solution.
		 * \param previous This search will start from the \a previous NNInfo and improve it if possible.
		 * Even if the search fails on this call, the solution will never be worse than \a previous.
		 *
		 * \see QueryCircle
		 */
		public NNInfo QueryClosestXZ (Vector3 p, NNConstraint constraint, ref float distance, NNInfo previous) {
			if (count == 0) {
				return previous;
			}

			SearchBoxClosestXZ(0, p, ref distance, constraint, ref previous);

			return previous;
		}

		void SearchBoxClosestXZ (int boxi, Vector3 p, ref float closestDist, NNConstraint constraint, ref NNInfo nnInfo) {
			BBTreeBox box = arr[boxi];

			if (box.node != null) {
				//Leaf node

				//Update the NNInfo
				#if ASTARDEBUG
				Debug.DrawLine((Vector3)box.node.GetVertex(1) + Vector3.up*0.2f, (Vector3)box.node.GetVertex(2) + Vector3.up*0.2f, Color.red);
				Debug.DrawLine((Vector3)box.node.GetVertex(0) + Vector3.up*0.2f, (Vector3)box.node.GetVertex(1) + Vector3.up*0.2f, Color.red);
				Debug.DrawLine((Vector3)box.node.GetVertex(2) + Vector3.up*0.2f, (Vector3)box.node.GetVertex(0) + Vector3.up*0.2f, Color.red);
				#endif

				Vector3 closest = box.node.ClosestPointOnNodeXZ(p);

				if (constraint == null || constraint.Suitable(box.node)) {
					// XZ distance
					float dist = (closest.x-p.x)*(closest.x-p.x)+(closest.z-p.z)*(closest.z-p.z);

					if (nnInfo.constrainedNode == null) {
						nnInfo.constrainedNode = box.node;
						nnInfo.constClampedPosition = closest;
						closestDist = (float)Math.Sqrt(dist);
					} else if (dist < closestDist*closestDist) {
						nnInfo.constrainedNode = box.node;
						nnInfo.constClampedPosition = closest;
						closestDist = (float)Math.Sqrt(dist);
					}
				}

				#if ASTARDEBUG
				Debug.DrawLine((Vector3)box.node.GetVertex(0), (Vector3)box.node.GetVertex(1), Color.blue);
				Debug.DrawLine((Vector3)box.node.GetVertex(1), (Vector3)box.node.GetVertex(2), Color.blue);
				Debug.DrawLine((Vector3)box.node.GetVertex(2), (Vector3)box.node.GetVertex(0), Color.blue);
				#endif
			} else {
				#if ASTARDEBUG
				Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymin), new Vector3(box.rect.xmax, 0, box.rect.ymin), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymax), new Vector3(box.rect.xmax, 0, box.rect.ymax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymin), new Vector3(box.rect.xmin, 0, box.rect.ymax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xmax, 0, box.rect.ymin), new Vector3(box.rect.xmax, 0, box.rect.ymax), Color.white);
				#endif

				//Search children
				if (RectIntersectsCircle(arr[box.left].rect, p, closestDist)) {
					SearchBoxClosestXZ(box.left, p, ref closestDist, constraint, ref nnInfo);
				}

				if (RectIntersectsCircle(arr[box.right].rect, p, closestDist)) {
					SearchBoxClosestXZ(box.right, p, ref closestDist, constraint, ref nnInfo);
				}
			}
		}

		/** Queries the tree for the closest node to \a p constrained by the NNConstraint trying to improve an existing solution.
		 * Note that this function will, unlike QueryCircle, only fill in the constrained node.
		 * If you want a node not constrained by any NNConstraint, do an additional search with constraint = NNConstraint.None
		 *
		 * \param p Point to search around
		 * \param constraint Optionally set to constrain which nodes to return
		 * \param distance The best distance for the \a previous solution. Will be updated with the best distance
		 * after this search. Will be positive infinity if no node could be found.
		 * Set to positive infinity if there was no previous solution.
		 * \param previous This search will start from the \a previous NNInfo and improve it if possible.
		 * Even if the search fails on this call, the solution will never be worse than \a previous.
		 *
		 * \see QueryCircle
		 */
		public NNInfo QueryClosest (Vector3 p, NNConstraint constraint, ref float distance, NNInfo previous) {
			if (count == 0) return previous;

			SearchBoxClosest(0, p, ref distance, constraint, ref previous);

			return previous;
		}

		void SearchBoxClosest (int boxi, Vector3 p, ref float closestDist, NNConstraint constraint, ref NNInfo nnInfo) {
			BBTreeBox box = arr[boxi];

			if (box.node != null) {
				//Leaf node
				if (NodeIntersectsCircle(box.node, p, closestDist)) {
					//Update the NNInfo
					#if ASTARDEBUG
					Debug.DrawLine((Vector3)box.node.GetVertex(1) + Vector3.up*0.2f, (Vector3)box.node.GetVertex(2) + Vector3.up*0.2f, Color.red);
					Debug.DrawLine((Vector3)box.node.GetVertex(0) + Vector3.up*0.2f, (Vector3)box.node.GetVertex(1) + Vector3.up*0.2f, Color.red);
					Debug.DrawLine((Vector3)box.node.GetVertex(2) + Vector3.up*0.2f, (Vector3)box.node.GetVertex(0) + Vector3.up*0.2f, Color.red);
					#endif

					Vector3 closest = box.node.ClosestPointOnNode(p);

					if (constraint == null || constraint.Suitable(box.node)) {
						float dist = (closest-p).sqrMagnitude;

						if (nnInfo.constrainedNode == null) {
							nnInfo.constrainedNode = box.node;
							nnInfo.constClampedPosition = closest;
							closestDist = (float)Math.Sqrt(dist);
						} else if (dist < closestDist*closestDist) {
							nnInfo.constrainedNode = box.node;
							nnInfo.constClampedPosition = closest;
							closestDist = (float)Math.Sqrt(dist);
						}
					}
				} else {
					#if ASTARDEBUG
					Debug.DrawLine((Vector3)box.node.GetVertex(0), (Vector3)box.node.GetVertex(1), Color.blue);
					Debug.DrawLine((Vector3)box.node.GetVertex(1), (Vector3)box.node.GetVertex(2), Color.blue);
					Debug.DrawLine((Vector3)box.node.GetVertex(2), (Vector3)box.node.GetVertex(0), Color.blue);
					#endif
				}
			} else {
				#if ASTARDEBUG
				Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymin), new Vector3(box.rect.xmax, 0, box.rect.ymin), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymax), new Vector3(box.rect.xmax, 0, box.rect.ymax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymin), new Vector3(box.rect.xmin, 0, box.rect.ymax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xmax, 0, box.rect.ymin), new Vector3(box.rect.xmax, 0, box.rect.ymax), Color.white);
				#endif

				//Search children
				if (RectIntersectsCircle(arr[box.left].rect, p, closestDist)) {
					SearchBoxClosest(box.left, p, ref closestDist, constraint, ref nnInfo);
				}

				if (RectIntersectsCircle(arr[box.right].rect, p, closestDist)) {
					SearchBoxClosest(box.right, p, ref closestDist, constraint, ref nnInfo);
				}
			}
		}

		public MeshNode QueryInside (Vector3 p, NNConstraint constraint) {
			return count != 0 ? SearchBoxInside(0, p, constraint) : null;
		}

		MeshNode SearchBoxInside (int boxi, Vector3 p, NNConstraint constraint) {
			BBTreeBox box = arr[boxi];

			if (box.node != null) {
				if (box.node.ContainsPoint((Int3)p)) {
					//Update the NNInfo

					#if ASTARDEBUG
					Debug.DrawLine((Vector3)box.node.GetVertex(1) + Vector3.up*0.2f, (Vector3)box.node.GetVertex(2) + Vector3.up*0.2f, Color.red);
					Debug.DrawLine((Vector3)box.node.GetVertex(0) + Vector3.up*0.2f, (Vector3)box.node.GetVertex(1) + Vector3.up*0.2f, Color.red);
					Debug.DrawLine((Vector3)box.node.GetVertex(2) + Vector3.up*0.2f, (Vector3)box.node.GetVertex(0) + Vector3.up*0.2f, Color.red);
					#endif


					if (constraint == null || constraint.Suitable(box.node)) {
						return box.node;
					}
				} else {
					#if ASTARDEBUG
					Debug.DrawLine((Vector3)box.node.GetVertex(0), (Vector3)box.node.GetVertex(1), Color.blue);
					Debug.DrawLine((Vector3)box.node.GetVertex(1), (Vector3)box.node.GetVertex(2), Color.blue);
					Debug.DrawLine((Vector3)box.node.GetVertex(2), (Vector3)box.node.GetVertex(0), Color.blue);
					#endif
				}
			} else {
				#if ASTARDEBUG
				Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymin), new Vector3(box.rect.xmax, 0, box.rect.ymin), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymax), new Vector3(box.rect.xmax, 0, box.rect.ymax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymin), new Vector3(box.rect.xmin, 0, box.rect.ymax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xmax, 0, box.rect.ymin), new Vector3(box.rect.xmax, 0, box.rect.ymax), Color.white);
				#endif

				//Search children
				MeshNode g;
				if (arr[box.left].Contains(p)) {
					g = SearchBoxInside(box.left, p, constraint);
					if (g != null) return g;
				}

				if (arr[box.right].Contains(p)) {
					g = SearchBoxInside(box.right, p, constraint);
					if (g != null) return g;
				}
			}

			return null;
		}

		void SearchBoxCircle (int boxi, Vector3 p, float radius, NNConstraint constraint, ref NNInfo nnInfo) {//, int intendentLevel = 0) {
			BBTreeBox box = arr[boxi];

			if (box.node != null) {
				//Leaf node
				if (NodeIntersectsCircle(box.node, p, radius)) {
					//Update the NNInfo

					#if ASTARDEBUG
					Debug.DrawLine((Vector3)box.node.GetVertex(0), (Vector3)box.node.GetVertex(1), Color.red);
					Debug.DrawLine((Vector3)box.node.GetVertex(1), (Vector3)box.node.GetVertex(2), Color.red);
					Debug.DrawLine((Vector3)box.node.GetVertex(2), (Vector3)box.node.GetVertex(0), Color.red);
					#endif

					Vector3 closest = box.node.ClosestPointOnNode(p); //NavMeshGraph.ClosestPointOnNode (box.node,graph.vertices,p);
					float dist = (closest-p).sqrMagnitude;

					if (nnInfo.node == null) {
						nnInfo.node = box.node;
						nnInfo.clampedPosition = closest;
					} else if (dist < (nnInfo.clampedPosition - p).sqrMagnitude) {
						nnInfo.node = box.node;
						nnInfo.clampedPosition = closest;
					}
					if (constraint == null || constraint.Suitable(box.node)) {
						if (nnInfo.constrainedNode == null) {
							nnInfo.constrainedNode = box.node;
							nnInfo.constClampedPosition = closest;
						} else if (dist < (nnInfo.constClampedPosition - p).sqrMagnitude) {
							nnInfo.constrainedNode = box.node;
							nnInfo.constClampedPosition = closest;
						}
					}
				} else {
					#if ASTARDEBUG
					Debug.DrawLine((Vector3)box.node.GetVertex(0), (Vector3)box.node.GetVertex(1), Color.blue);
					Debug.DrawLine((Vector3)box.node.GetVertex(1), (Vector3)box.node.GetVertex(2), Color.blue);
					Debug.DrawLine((Vector3)box.node.GetVertex(2), (Vector3)box.node.GetVertex(0), Color.blue);
					#endif
				}
				return;
			}

			#if ASTARDEBUG
			Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymin), new Vector3(box.rect.xmax, 0, box.rect.ymin), Color.white);
			Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymax), new Vector3(box.rect.xmax, 0, box.rect.ymax), Color.white);
			Debug.DrawLine(new Vector3(box.rect.xmin, 0, box.rect.ymin), new Vector3(box.rect.xmin, 0, box.rect.ymax), Color.white);
			Debug.DrawLine(new Vector3(box.rect.xmax, 0, box.rect.ymin), new Vector3(box.rect.xmax, 0, box.rect.ymax), Color.white);
			#endif

			//Search children
			if (RectIntersectsCircle(arr[box.left].rect, p, radius)) {
				SearchBoxCircle(box.left, p, radius, constraint, ref nnInfo);
			}

			if (RectIntersectsCircle(arr[box.right].rect, p, radius)) {
				SearchBoxCircle(box.right, p, radius, constraint, ref nnInfo);
			}
		}

		void SearchBox (int boxi, Vector3 p, NNConstraint constraint, ref NNInfo nnInfo) {//, int intendentLevel = 0) {
			BBTreeBox box = arr[boxi];

			if (box.node != null) {
				//Leaf node
				if (box.node.ContainsPoint((Int3)p)) {
					//Update the NNInfo

					if (nnInfo.node == null) {
						nnInfo.node = box.node;
					} else if (Mathf.Abs(((Vector3)box.node.position).y - p.y) < Mathf.Abs(((Vector3)nnInfo.node.position).y - p.y)) {
						nnInfo.node = box.node;
					}
					if (constraint.Suitable(box.node)) {
						if (nnInfo.constrainedNode == null) {
							nnInfo.constrainedNode = box.node;
						} else if (Mathf.Abs(box.node.position.y - p.y) < Mathf.Abs(nnInfo.constrainedNode.position.y - p.y)) {
							nnInfo.constrainedNode = box.node;
						}
					}
				}
				return;
			}

			//Search children
			if (arr[box.left].Contains(p)) {
				SearchBox(box.left, p, constraint, ref nnInfo);
			}

			if (arr[box.right].Contains(p)) {
				SearchBox(box.right, p, constraint, ref nnInfo);
			}
		}

		struct BBTreeBox {
			public IntRect rect;

			public MeshNode node;
			public int left, right;

			public bool IsLeaf {
				get {
					return node != null;
				}
			}

			public BBTreeBox (IntRect rect) {
				node = null;
				this.rect = rect;
				left = right = -1;
			}

			public BBTreeBox (MeshNode node, IntRect rect) {
				this.node = node;
				this.rect = rect;
				left = right = -1;
			}

			public bool Contains (Vector3 p) {
				var pi = (Int3)p;

				return rect.Contains(pi.x, pi.z);
			}
		}

		public void OnDrawGizmos () {
			Gizmos.color = new Color(1, 1, 1, 0.5F);
			if (count == 0) return;
			OnDrawGizmos(0, 0);
		}

		void OnDrawGizmos (int boxi, int depth) {
			BBTreeBox box = arr[boxi];

			var min = (Vector3) new Int3(box.rect.xmin, 0, box.rect.ymin);
			var max = (Vector3) new Int3(box.rect.xmax, 0, box.rect.ymax);

			Vector3 center = (min+max)*0.5F;
			Vector3 size = (max-center)*2;

			size = new Vector3(size.x, 1, size.z);
			center.y += depth * 2;

			Gizmos.color = AstarMath.IntToColor(depth, 1f); //new Color (0,0,0,0.2F);
			Gizmos.DrawCube(center, size);

			if (box.node != null) {
			} else {
				OnDrawGizmos(box.left, depth + 1);
				OnDrawGizmos(box.right, depth + 1);
			}
		}

		static bool NodeIntersectsCircle (MeshNode node, Vector3 p, float radius) {
			if (float.IsPositiveInfinity(radius)) return true;

			/** \bug Is not correct on the Y axis */
			return (p - node.ClosestPointOnNode(p)).sqrMagnitude < radius*radius;
		}

		/** Returns true if \a p is within \a radius from \a r.
		 * Correctly handles cases where \a radius is positive infinity.
		 */
		static bool RectIntersectsCircle (IntRect r, Vector3 p, float radius) {
			if (float.IsPositiveInfinity(radius)) return true;

			Vector3 po = p;
			p.x = Math.Max(p.x, r.xmin*Int3.PrecisionFactor);
			p.x = Math.Min(p.x, r.xmax*Int3.PrecisionFactor);
			p.z = Math.Max(p.z, r.ymin*Int3.PrecisionFactor);
			p.z = Math.Min(p.z, r.ymax*Int3.PrecisionFactor);

			// XZ squared magnitude comparison
			return (p.x-po.x)*(p.x-po.x) + (p.z-po.z)*(p.z-po.z) < radius*radius;
		}

		/** Returns a new rect which contains both \a r and \a r2 */
		static IntRect ExpandToContain (IntRect r, IntRect r2) {
			return IntRect.Union(r, r2);
		}
	}
}
