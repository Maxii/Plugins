#define TUPLE
#pragma warning disable 162
#pragma warning disable 429

namespace Pathfinding {
	/** Binary heap implementation.
	 * Binary heaps are really fast for ordering nodes in a way that
	 * makes it possible to get the node with the lowest F score.
	 * Also known as a priority queue.
	 *
	 * This has actually been rewritten as a d-ary heap (by default a 4-ary heap)
	 * for performance, but it's the same principle.
	 *
	 * \see http://en.wikipedia.org/wiki/Binary_heap
	 * \see https://en.wikipedia.org/wiki/D-ary_heap
	 */
	public class BinaryHeapM {
		/** Number of items in the tree */
		public int numberOfItems;

		/** The tree will grow by at least this factor every time it is expanded */
		public float growthFactor = 2;

		/**
		 * Number of children of each node in the tree.
		 * Different values have been tested and 4 has been empirically found to perform the best.
		 * \see https://en.wikipedia.org/wiki/D-ary_heap
		 */
		const int D = 4;

		/** Sort nodes by G score if there is a tie when comparing the F score */
		const bool SortGScores = true;

		/** Internal backing array for the tree */
		private Tuple[] binaryHeap;

		private struct Tuple {
			public uint F;
			public PathNode node;

			public Tuple (uint f, PathNode node) {
				this.F = f;
				this.node = node;
			}
		}

		public BinaryHeapM (int numberOfElements) {
			binaryHeap = new Tuple[numberOfElements];
			numberOfItems = 0;
		}

		public void Clear () {
			numberOfItems = 0;
		}

		internal PathNode GetNode (int i) {
			return binaryHeap[i].node;
		}

		internal void SetF (int i, uint f) {
			binaryHeap[i].F = f;
		}

		/** Adds a node to the heap */
		public void Add (PathNode node) {
			if (node == null) throw new System.ArgumentNullException("node");

			if (numberOfItems == binaryHeap.Length) {
				int newSize = System.Math.Max(binaryHeap.Length+4, (int)System.Math.Round(binaryHeap.Length*growthFactor));
				if (newSize > 1<<18) {
					throw new System.Exception("Binary Heap Size really large (2^18). A heap size this large is probably the cause of pathfinding running in an infinite loop. " +
						"\nRemove this check (in BinaryHeap.cs) if you are sure that it is not caused by a bug");
				}

				var tmp = new Tuple[newSize];

				for (int i = 0; i < binaryHeap.Length; i++) {
					tmp[i] = binaryHeap[i];
				}
#if ASTARDEBUG
				UnityEngine.Debug.Log("Resizing binary heap to "+newSize);
#endif
				binaryHeap = tmp;
			}

			var obj = new Tuple(node.F, node);
			binaryHeap[numberOfItems] = obj;

			int bubbleIndex = numberOfItems;
			uint nodeF = node.F;
			uint nodeG = node.G;

			while (bubbleIndex != 0) {
				int parentIndex = (bubbleIndex-1) / D;

				if (nodeF < binaryHeap[parentIndex].F || (nodeF == binaryHeap[parentIndex].F && nodeG > binaryHeap[parentIndex].node.G)) {
					binaryHeap[bubbleIndex] = binaryHeap[parentIndex];
					binaryHeap[parentIndex] = obj;
					bubbleIndex = parentIndex;
				} else {
					break;
				}
			}

			numberOfItems++;
		}

		/** Returns the node with the lowest F score from the heap */
		public PathNode Remove () {
			numberOfItems--;
			PathNode returnItem = binaryHeap[0].node;

			binaryHeap[0] = binaryHeap[numberOfItems];

			int swapItem = 0, parent;

			do {
				if (D == 0) {
					parent = swapItem;
					int p2 = parent * D;
					if (p2 + 1 <= numberOfItems) {
						// Both children exist
						if (binaryHeap[parent].F > binaryHeap[p2].F) {
							swapItem = p2;//2 * parent;
						}
						if (binaryHeap[swapItem].F > binaryHeap[p2 + 1].F) {
							swapItem = p2 + 1;
						}
					} else if ((p2) <= numberOfItems) {
						// Only one child exists
						if (binaryHeap[parent].F > binaryHeap[p2].F) {
							swapItem = p2;
						}
					}
				} else {
					parent = swapItem;
					uint swapF = binaryHeap[swapItem].F;
					int pd = parent * D + 1;

					if (D >= 1 && pd+0 <= numberOfItems && (binaryHeap[pd+0].F < swapF || (SortGScores && binaryHeap[pd+0].F == swapF && binaryHeap[pd+0].node.G < binaryHeap[swapItem].node.G))) {
						swapF = binaryHeap[pd+0].F;
						swapItem = pd+0;
					}

					if (D >= 2 && pd+1 <= numberOfItems && (binaryHeap[pd+1].F < swapF || (SortGScores && binaryHeap[pd+1].F == swapF && binaryHeap[pd+1].node.G < binaryHeap[swapItem].node.G))) {
						swapF = binaryHeap[pd+1].F;
						swapItem = pd+1;
					}

					if (D >= 3 && pd+2 <= numberOfItems && (binaryHeap[pd+2].F < swapF || (SortGScores && binaryHeap[pd+2].F == swapF && binaryHeap[pd+2].node.G < binaryHeap[swapItem].node.G))) {
						swapF = binaryHeap[pd+2].F;
						swapItem = pd+2;
					}

					if (D >= 4 && pd+3 <= numberOfItems && (binaryHeap[pd+3].F < swapF || (SortGScores && binaryHeap[pd+3].F == swapF && binaryHeap[pd+3].node.G < binaryHeap[swapItem].node.G))) {
						swapF = binaryHeap[pd+3].F;
						swapItem = pd+3;
					}

					if (D >= 5 && pd+4 <= numberOfItems && binaryHeap[pd+4].F < swapF) {
						swapF = binaryHeap[pd+4].F;
						swapItem = pd+4;
					}

					if (D >= 6 && pd+5 <= numberOfItems && binaryHeap[pd+5].F < swapF) {
						swapF = binaryHeap[pd+5].F;
						swapItem = pd+5;
					}

					if (D >= 7 && pd+6 <= numberOfItems && binaryHeap[pd+6].F < swapF) {
						swapF = binaryHeap[pd+6].F;
						swapItem = pd+6;
					}

					if (D >= 8 && pd+7 <= numberOfItems && binaryHeap[pd+7].F < swapF) {
						swapF = binaryHeap[pd+7].F;
						swapItem = pd+7;
					}

					if (D >= 9 && pd+8 <= numberOfItems && binaryHeap[pd+8].F < swapF) {
						swapF = binaryHeap[pd+8].F;
						swapItem = pd+8;
					}
				}

				// One if the parent's children are smaller or equal, swap them
				if (parent != swapItem) {
					var tmpIndex = binaryHeap[parent];
					binaryHeap[parent] = binaryHeap[swapItem];
					binaryHeap[swapItem] = tmpIndex;
				} else {
					break;
				}
			} while (true);

			//Validate ();

			return returnItem;
		}

		void Validate () {
			for (int i = 1; i < numberOfItems; i++) {
				int parentIndex = (i-1)/D;
				if (binaryHeap[parentIndex].F > binaryHeap[i].F) {
					throw new System.Exception("Invalid state at " + i + ":" +  parentIndex + " ( " + binaryHeap[parentIndex].F + " > " + binaryHeap[i].F + " ) ");
				}
			}
		}

		/** Rebuilds the heap by trickeling down all items.
		 * Usually called after the hTarget on a path has been changed */
		public void Rebuild () {
#if ASTARDEBUG
			int changes = 0;
#endif

			for (int i = 2; i < numberOfItems; i++) {
				int bubbleIndex = i;
				var node = binaryHeap[i];
				uint nodeF = node.F;
				while (bubbleIndex != 1) {
					int parentIndex = bubbleIndex / D;

					if (nodeF < binaryHeap[parentIndex].F) {
						binaryHeap[bubbleIndex] = binaryHeap[parentIndex];
						binaryHeap[parentIndex] = node;
						bubbleIndex = parentIndex;
#if ASTARDEBUG
						changes++;
#endif
					} else {
						break;
					}
				}
			}

#if ASTARDEBUG
			UnityEngine.Debug.Log("+++ Rebuilt Heap - "+changes+" changes +++");
#endif
		}
	}
}
