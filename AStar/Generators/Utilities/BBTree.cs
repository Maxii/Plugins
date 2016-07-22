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

		int GetBox (MeshNode node) {
			if (count >= arr.Length) EnsureCapacity(count+1);

			arr[count] = new BBTreeBox(node);
			count++;
			return count-1;
		}

		int GetBox (IntRect rect) {
			if (count >= arr.Length) EnsureCapacity(count+1);

			arr[count] = new BBTreeBox(rect);
			count++;
			return count-1;
		}

		/** Rebuilds the tree using the specified nodes.
		 * This is faster and gives better quality results compared to calling Insert with all nodes
		 */
		public void RebuildFrom (MeshNode[] nodes) {
			Clear();

			if (nodes.Length == 0) {
				return;
			}

			if (nodes.Length == 1) {
				GetBox(nodes[0]);
				return;
			}

			// We will use approximately 2N tree nodes
			EnsureCapacity(Mathf.CeilToInt(nodes.Length * 2.1f));

			// Make a copy of the nodes array since we will be modifying it
			var nodeCopies = new MeshNode[nodes.Length];
			for (int i = 0; i < nodes.Length; i++) nodeCopies[i] = nodes[i];

			RebuildFromInternal(nodeCopies, 0, nodes.Length, false);
		}

		static int SplitByX (MeshNode[] nodes, int from, int to, int divider) {
			int mx = to;

			for (int i = from; i < mx; i++) {
				if (nodes[i].position.x > divider) {
					// swap with mx
					mx--;
					var tmp = nodes[mx];
					nodes[mx] = nodes[i];
					nodes[i] = tmp;
					i--;
				}
			}
			return mx;
		}

		static int SplitByZ (MeshNode[] nodes, int from, int to, int divider) {
			int mx = to;

			for (int i = from; i < mx; i++) {
				if (nodes[i].position.z > divider) {
					// swap with mx
					mx--;
					var tmp = nodes[mx];
					nodes[mx] = nodes[i];
					nodes[i] = tmp;
					i--;
				}
			}
			return mx;
		}

		int RebuildFromInternal (MeshNode[] nodes, int from, int to, bool odd) {
			if (to - from <= 0) throw new ArgumentException();

			if (to - from == 1) {
				return GetBox(nodes[from]);
			}

			var rect = NodeBounds(nodes, from, to);
			int box = GetBox(rect);

			// Performance optimization for a common case
			if (to - from == 2) {
				arr[box].left = GetBox(nodes[from]);
				arr[box].right = GetBox(nodes[from+1]);
				return box;
			}

			int mx;
			if (odd) {
				// X
				int divider = (rect.xmin + rect.xmax)/2;
				mx = SplitByX(nodes, from, to, divider);
			} else {
				// Y/Z
				int divider = (rect.ymin + rect.ymax)/2;
				mx = SplitByZ(nodes, from, to, divider);
			}

			if (mx == from || mx == to) {
				// All nodes were on one side of the divider
				// Try to split along the other axis

				if (!odd) {
					// X
					int divider = (rect.xmin + rect.xmax)/2;
					mx = SplitByX(nodes, from, to, divider);
				} else {
					// Y/Z
					int divider = (rect.ymin + rect.ymax)/2;
					mx = SplitByZ(nodes, from, to, divider);
				}

				if (mx == from || mx == to) {
					// All nodes were on one side of the divider
					// Just pick one half
					mx = (from+to)/2;
				}
			}

			arr[box].left = RebuildFromInternal(nodes, from, mx, !odd);
			arr[box].right = RebuildFromInternal(nodes, mx, to, !odd);

			return box;
		}

		/** Calculates the bounding box in XZ space of all nodes between \a from (inclusive) and \a to (exclusive) */
		static IntRect NodeBounds (MeshNode[] nodes, int from, int to) {
			if (to - from <= 0) throw new ArgumentException();

			var first = nodes[from].GetVertex(0);
			var min = new Int2(first.x, first.z);
			Int2 max = min;

			for (int j = from; j < to; j++) {
				var node = nodes[j];
				var nverts = node.GetVertexCount();
				for (int i = 0; i < nverts; i++) {
					var p = node.GetVertex(i);
					min.x = Math.Min(min.x, p.x);
					min.y = Math.Min(min.y, p.z);

					max.x = Math.Max(max.x, p.x);
					max.y = Math.Max(max.y, p.z);
				}
			}

			return new IntRect(min.x, min.y, max.x, max.y);
		}

		/** Inserts a mesh node in the tree */
		public void Insert (MeshNode node) {
			int boxi = GetBox(node);

			// Was set to root
			if (boxi == 0) {
				return;
			}

			BBTreeBox box = arr[boxi];

			//int depth = 0;

			int c = 0;
			while (true) {
				BBTreeBox cb = arr[c];

				cb.rect = ExpandToContain(cb.rect, box.rect);
				if (cb.node != null) {
					//Is Leaf
					cb.left = boxi;

					int box2 = GetBox(cb.node);
					//BBTreeBox box2 = new BBTreeBox (this,c.node);

					//Console.WriteLine ("Inserted "+box.node+", rect "+box.rect.ToString ());
					cb.right = box2;


					cb.node = null;
					//cb.depth++;

					//c.rect = c.rect.
					arr[c] = cb;
					//Debug.Log (depth);
					return;
				} else {
					//depth++;
					//cb.depth++;
					arr[c] = cb;

					int e1 = ExpansionRequired(arr[cb.left].rect, box.rect);// * arr[cb.left].depth;
					int e2 =  ExpansionRequired(arr[cb.right].rect, box.rect);// * arr[cb.left].depth;

					//Choose the rect requiring the least expansion to contain box.rect
					if (e1 < e2) {
						c = cb.left;
					} else if (e2 < e1) {
						c = cb.right;
					} else {
						//Equal, Choose the one with the smallest area
						c = RectArea(arr[cb.left].rect) < RectArea(arr[cb.right].rect) ? cb.left : cb.right;
					}
				}
			}
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
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMin), new Vector3(box.rect.xMax, 0, box.rect.yMin), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMax), new Vector3(box.rect.xMax, 0, box.rect.yMax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMin), new Vector3(box.rect.xMin, 0, box.rect.yMax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMax, 0, box.rect.yMin), new Vector3(box.rect.xMax, 0, box.rect.yMax), Color.white);
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
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMin), new Vector3(box.rect.xMax, 0, box.rect.yMin), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMax), new Vector3(box.rect.xMax, 0, box.rect.yMax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMin), new Vector3(box.rect.xMin, 0, box.rect.yMax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMax, 0, box.rect.yMin), new Vector3(box.rect.xMax, 0, box.rect.yMax), Color.white);
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
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMin), new Vector3(box.rect.xMax, 0, box.rect.yMin), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMax), new Vector3(box.rect.xMax, 0, box.rect.yMax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMin), new Vector3(box.rect.xMin, 0, box.rect.yMax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMax, 0, box.rect.yMin), new Vector3(box.rect.xMax, 0, box.rect.yMax), Color.white);
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
			Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMin), new Vector3(box.rect.xMax, 0, box.rect.yMin), Color.white);
			Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMax), new Vector3(box.rect.xMax, 0, box.rect.yMax), Color.white);
			Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMin), new Vector3(box.rect.xMin, 0, box.rect.yMax), Color.white);
			Debug.DrawLine(new Vector3(box.rect.xMax, 0, box.rect.yMin), new Vector3(box.rect.xMax, 0, box.rect.yMax), Color.white);
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

			public BBTreeBox (MeshNode node) {
				this.node = node;
				var first = node.GetVertex(0);
				var min = new Int2(first.x, first.z);
				Int2 max = min;

				for (int i = 1; i < node.GetVertexCount(); i++) {
					var p = node.GetVertex(i);
					min.x = Math.Min(min.x, p.x);
					min.y = Math.Min(min.y, p.z);

					max.x = Math.Max(max.x, p.x);
					max.y = Math.Max(max.y, p.z);
				}

				rect = new IntRect(min.x, min.y, max.x, max.y);
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

		/** Returns the difference in area between \a r and \a r expanded to contain \a r2 */
		static int ExpansionRequired (IntRect r, IntRect r2) {
			int xMin = Math.Min(r.xmin, r2.xmin);
			int xMax = Math.Max(r.xmax, r2.xmax);
			int yMin = Math.Min(r.ymin, r2.ymin);
			int yMax = Math.Max(r.ymax, r2.ymax);

			return (xMax-xMin)*(yMax-yMin)-RectArea(r);
		}

		/** Returns a new rect which contains both \a r and \a r2 */
		static IntRect ExpandToContain (IntRect r, IntRect r2) {
			return IntRect.Union(r, r2);
		}

		/** Returns the area of a rect */
		static int RectArea (IntRect r) {
			return r.Width*r.Height;
		}
	}
}
