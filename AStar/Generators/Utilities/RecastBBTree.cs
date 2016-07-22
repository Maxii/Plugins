using System;
using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	/** Axis Aligned Bounding Box Tree.
	 * Holds a bounding box tree of RecastMeshObj components.\n
	 * Note that it assumes that once an object has been added, it stays at the same
	 * world position. If it is moved, then it might not be able to be found.
	 *
	 * \astarpro
	 */
	public class RecastBBTree {
		public RecastBBTreeBox root;

		/** Queries the tree for all RecastMeshObjs inside the specified bounds.
		 *
		 * \param bounds World space bounds to search within
		 * \param buffer The results will be added to the buffer
		 *
		 */
		public void QueryInBounds (Rect bounds, List<RecastMeshObj> buffer) {
			RecastBBTreeBox c = root;

			if (c == null) return;

			QueryBoxInBounds(c, bounds, buffer);
		}

		void QueryBoxInBounds (RecastBBTreeBox box, Rect bounds, List<RecastMeshObj> boxes) {
			if (box.mesh != null) {
				//Leaf node
				if (RectIntersectsRect(box.rect, bounds)) {
					// Found a RecastMeshObj, add it to the result
					boxes.Add(box.mesh);
				}
			} else {
	#if ASTARDEBUG
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMin), new Vector3(box.rect.xMax, 0, box.rect.yMin), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMax), new Vector3(box.rect.xMax, 0, box.rect.yMax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMin, 0, box.rect.yMin), new Vector3(box.rect.xMin, 0, box.rect.yMax), Color.white);
				Debug.DrawLine(new Vector3(box.rect.xMax, 0, box.rect.yMin), new Vector3(box.rect.xMax, 0, box.rect.yMax), Color.white);
	#endif

				//Search children
				if (RectIntersectsRect(box.c1.rect, bounds)) {
					QueryBoxInBounds(box.c1, bounds, boxes);
				}

				if (RectIntersectsRect(box.c2.rect, bounds)) {
					QueryBoxInBounds(box.c2, bounds, boxes);
				}
			}
		}

		/** Removes the specified mesh from the tree.
		 * Assumes that it has the correct bounds information.
		 *
		 * \returns True if the mesh was removed from the tree, false otherwise.
		 */
		public bool Remove (RecastMeshObj mesh) {
			if (mesh == null) throw new ArgumentNullException("mesh");

			if (root == null) {
				return false;
			}

			bool found = false;
			Bounds b = mesh.GetBounds();
			//Convert to top down rect
			Rect r = Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);

			root = RemoveBox(root, mesh, r, ref found);

			return found;
		}

		RecastBBTreeBox RemoveBox (RecastBBTreeBox c, RecastMeshObj mesh, Rect bounds, ref bool found) {
			if (!RectIntersectsRect(c.rect, bounds)) {
				return c;
			}

			if (c.mesh == mesh) {
				found = true;
				return null;
			}

			if (c.mesh == null && !found) {
				c.c1 = RemoveBox(c.c1, mesh, bounds, ref found);
				if (c.c1 == null) {
					return c.c2;
				}

				if (!found) {
					c.c2 = RemoveBox(c.c2, mesh, bounds, ref found);
					if (c.c2 == null) {
						return c.c1;
					}
				}

				if (found) {
					c.rect = ExpandToContain(c.c1.rect, c.c2.rect);
				}
			}
			return c;
		}

		/** Inserts a RecastMeshObj in the tree at its current position */
		public void Insert (RecastMeshObj mesh) {
			var box = new RecastBBTreeBox(mesh);

			if (root == null) {
				root = box;
				return;
			}

			RecastBBTreeBox c = root;
			while (true) {
				c.rect = ExpandToContain(c.rect, box.rect);
				if (c.mesh != null) {
					//Is Leaf
					c.c1 = box;
					var box2 = new RecastBBTreeBox(c.mesh);
					c.c2 = box2;


					c.mesh = null;
					return;
				} else {
					float e1 = ExpansionRequired(c.c1.rect, box.rect);
					float e2 = ExpansionRequired(c.c2.rect, box.rect);

					// Choose the rect requiring the least expansion to contain box.rect
					if (e1 < e2) {
						c = c.c1;
					} else if (e2 < e1) {
						c = c.c2;
					} else {
						// Equal, Choose the one with the smallest area
						c = RectArea(c.c1.rect) < RectArea(c.c2.rect) ? c.c1 : c.c2;
					}
				}
			}
		}

		public void OnDrawGizmos () {
			// Uncomment to draw nice gizmos
			//Gizmos.color = new Color (1,1,1,0.01F);
			//OnDrawGizmos (root);
		}

		public void OnDrawGizmos (RecastBBTreeBox box) {
			if (box == null) {
				return;
			}

			var min = new Vector3(box.rect.xMin, 0, box.rect.yMin);
			var max = new Vector3(box.rect.xMax, 0, box.rect.yMax);

			Vector3 center = (min+max)*0.5F;
			Vector3 size = (max-center)*2;

			Gizmos.DrawCube(center, size);

			OnDrawGizmos(box.c1);
			OnDrawGizmos(box.c2);
		}

		static bool RectIntersectsRect (Rect r, Rect r2) {
			return (r.xMax > r2.xMin && r.yMax > r2.yMin && r2.xMax > r.xMin && r2.yMax > r.yMin);
		}

		static bool RectIntersectsCircle (Rect r, Vector3 p, float radius) {
			if (float.IsPositiveInfinity(radius)) return true;

			if (RectContains(r, p)) {
				return true;
			}

			return XIntersectsCircle(r.xMin, r.xMax, r.yMin, p, radius) ||
				   XIntersectsCircle(r.xMin, r.xMax, r.yMax, p, radius) ||
				   ZIntersectsCircle(r.yMin, r.yMax, r.xMin, p, radius) ||
				   ZIntersectsCircle(r.yMin, r.yMax, r.xMax, p, radius);
		}

		/** Returns if a rect contains the 3D point in XZ space */
		static bool RectContains (Rect r, Vector3 p) {
			return p.x >= r.xMin && p.x <= r.xMax && p.z >= r.yMin && p.z <= r.yMax;
		}

		static bool ZIntersectsCircle (float z1, float z2, float xpos, Vector3 circle, float radius) {
			double f = Math.Abs(xpos-circle.x)/radius;

			if (f > 1.0 || f < -1.0) {
				return false;
			}

			float s1 = (float)Math.Sqrt(1.0 - f*f)*radius;

			float s2 = circle.z - s1;
			s1 += circle.z;

			float min = Math.Min(s1, s2);
			float max = Math.Max(s1, s2);

			min = Mathf.Max(z1, min);
			max = Mathf.Min(z2, max);

			return max > min;
		}

		static bool XIntersectsCircle (float x1, float x2, float zpos, Vector3 circle, float radius) {
			double f = Math.Abs(zpos-circle.z)/radius;

			if (f > 1.0 || f < -1.0) {
				return false;
			}

			float s1 = (float)Math.Sqrt(1.0 - f*f)*radius;

			float s2 = circle.x - s1;
			s1 += circle.x;

			float min = Math.Min(s1, s2);
			float max = Math.Max(s1, s2);

			min = Mathf.Max(x1, min);
			max = Mathf.Min(x2, max);

			return max > min;
		}

		/** Returns the difference in area between \a r and \a r expanded to contain \a r2 */
		static float ExpansionRequired (Rect r, Rect r2) {
			float xMin = Mathf.Min(r.xMin, r2.xMin);
			float xMax = Mathf.Max(r.xMax, r2.xMax);
			float yMin = Mathf.Min(r.yMin, r2.yMin);
			float yMax = Mathf.Max(r.yMax, r2.yMax);

			return (xMax-xMin)*(yMax-yMin)-RectArea(r);
		}

		/** Returns a new rect which contains both \a r and \a r2 */
		static Rect ExpandToContain (Rect r, Rect r2) {
			float xMin = Mathf.Min(r.xMin, r2.xMin);
			float xMax = Mathf.Max(r.xMax, r2.xMax);
			float yMin = Mathf.Min(r.yMin, r2.yMin);
			float yMax = Mathf.Max(r.yMax, r2.yMax);

			return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
		}

		/** Returns the area of a rect */
		static float RectArea (Rect r) {
			return r.width*r.height;
		}

		public new void ToString () {
			//Console.WriteLine ("Root "+(root.node != null ? root.node.ToString () : ""));

			RecastBBTreeBox c = root;

			var stack = new Stack<RecastBBTreeBox>();

			stack.Push(c);

			c.WriteChildren(0);
		}
	}

	public class RecastBBTreeBox {
		public Rect rect;
		public RecastMeshObj mesh;

		public RecastBBTreeBox c1;
		public RecastBBTreeBox c2;

		public RecastBBTreeBox (RecastMeshObj mesh) {
			this.mesh = mesh;

			Vector3 min = mesh.bounds.min;
			Vector3 max = mesh.bounds.max;
			rect = Rect.MinMaxRect(min.x, min.z, max.x, max.z);
		}

		public bool Contains (Vector3 p) {
			return rect.Contains(p);
		}

		public void WriteChildren (int level) {
			for (int i = 0; i < level; i++) {
#if !NETFX_CORE || UNITY_EDITOR
				Console.Write("  ");
#endif
			}
			if (mesh != null) {
#if !NETFX_CORE || UNITY_EDITOR
				Console.WriteLine("Leaf "); //+triangle.ToString ());
#endif
			} else {
#if !NETFX_CORE || UNITY_EDITOR
				Console.WriteLine("Box "); //+rect.ToString ());
#endif
				c1.WriteChildren(level+1);
				c2.WriteChildren(level+1);
			}
		}
	}
}
