#pragma warning disable 414
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace Pathfinding {
	public enum HeuristicOptimizationMode {
		None,
		Random,
		RandomSpreadOut,
		Custom
	}

	/** Implements heuristic optimizations.
	 *
	 * \see heuristic-opt
	 * \see Game AI Pro - Pathfinding Architecture Optimizations by Steve Rabin and Nathan R. Sturtevant
	 *
	 * \astarpro
	 */
	[System.Serializable]
	public class EuclideanEmbedding {

		public HeuristicOptimizationMode mode;

		public int seed;

		/** All children of this transform will be used as pivot points */
		public Transform pivotPointRoot;

		public int spreadOutCount = 1;

		[System.NonSerialized]
		public bool dirty;


		void EnsureCapacity ( int index ) {
		}

		public uint GetHeuristic ( int nodeIndex1, int nodeIndex2 ) {
			return 0;
		}

		void GetClosestWalkableNodesToChildrenRecursively ( Transform tr, List<GraphNode> nodes ) {
			foreach (Transform ch in tr ) {

				NNInfo info = AstarPath.active.GetNearest ( ch.position, NNConstraint.Default );
				if ( info.node != null && info.node.Walkable ) {
					nodes.Add ( info.node );
				}

				GetClosestWalkableNodesToChildrenRecursively ( tr, nodes );
			}
		}

		public void RecalculatePivots () {
		}

		public void RecalculateCosts () {
			dirty = false;
		}

		public void OnDrawGizmos () {
		}
	}
}
