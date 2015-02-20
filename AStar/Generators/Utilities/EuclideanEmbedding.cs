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

	[System.Serializable]
	/** Implements heuristic optimizations.
	 * 
	 * \see heuristic-opt
	 * \see Game AI Pro - Pathfinding Architecture Optimizations by Steve Rabin and Nathan R. Sturtevant
	 * 
	 * \astarpro
	 */
	public class EuclideanEmbedding {

		public HeuristicOptimizationMode mode;

		public int seed;

		/** All children of this transform will be used as pivot points */
		public Transform pivotPointRoot;

		public int spreadOutCount = 1;

		/**
		 * Costs laid out as n*[int],n*[int],n*[int] where n is the number of pivot points.
		 * Each node has n integers which is the cost from that node to the pivot node.
		 * They are at around the same place in the array for simplicity and for cache locality.
		 * 
		 * cost(nodeIndex, pivotIndex) = costs[nodeIndex*pivotCount+pivotIndex]
		 */
		uint[] costs = new uint[8];
		int maxNodeIndex = 0;


		int pivotCount = 0;

		[System.NonSerialized]
		public bool dirty = false;

		GraphNode[] pivots = null;

		uint ra = 12820163;    /* must not be zero */
		uint rc = 1140671485;    /* must not be zero */
		uint rval = 0;

		System.Object lockObj = new object();

		/** Simple linear congruential generator.
		 * \see http://en.wikipedia.org/wiki/Linear_congruential_generator
		 */
		public uint GetRandom()
		{
			rval = (ra*rval + rc);
			return rval;
		}

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