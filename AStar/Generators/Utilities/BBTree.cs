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
		public void OnDrawGizmos () {}
	}

}
