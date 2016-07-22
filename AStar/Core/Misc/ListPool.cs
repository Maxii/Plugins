#if !UNITY_EDITOR
// Extra optimizations when not running in the editor, but less error checking
#define ASTAR_OPTIMIZE_POOLING
#endif

using System;
using System.Collections.Generic;

namespace Pathfinding.Util {
	/** Lightweight List Pool.
	 * Handy class for pooling lists of type T.
	 *
	 * Usage:
	 * - Claim a new list using \code List<SomeClass> foo = ListPool<SomeClass>.Claim (); \endcode
	 * - Use it and do stuff with it
	 * - Release it with \code ListPool<SomeClass>.Release (foo); \endcode
	 *
	 * You do not need to clear the list before releasing it.
	 * After you have released a list, you should never use it again, if you do use it, you will
	 * mess things up quite badly in the worst case.
	 *
	 * \since Version 3.2
	 * \see Pathfinding.Util.StackPool
	 */
	public static class ListPool<T>{
		/** Internal pool */
		static readonly List<List<T> > pool = new List<List<T> >();

#if !ASTAR_NO_POOLING
		static readonly HashSet<List<T> > inPool = new HashSet<List<T> >();
#endif

		/** When requesting a list with a specified capacity, search max this many lists in the pool before giving up.
		 * Must be greater or equal to one.
		 */
		const int MaxCapacitySearchLength = 8;

		/** Claim a list.
		 * Returns a pooled list if any are in the pool.
		 * Otherwise it creates a new one.
		 * After usage, this list should be released using the Release function (though not strictly necessary).
		 */
		public static List<T> Claim () {
#if ASTAR_NO_POOLING
			return new List<T>();
#else
			lock (pool) {
				if (pool.Count > 0) {
					List<T> ls = pool[pool.Count-1];
					pool.RemoveAt(pool.Count-1);
					inPool.Remove(ls);
					return ls;
				}

				return new List<T>();
			}
#endif
		}

		/** Claim a list with minimum capacity
		 * Returns a pooled list if any are in the pool.
		 * Otherwise it creates a new one.
		 * After usage, this list should be released using the Release function (though not strictly necessary).
		 * This list returned will have at least the capacity specified.
		 */
		public static List<T> Claim (int capacity) {
#if ASTAR_NO_POOLING
			return new List<T>(capacity);
#else
			lock (pool) {
				// Loop through the last MaxCapacitySearchLength items
				// and check if any item has a capacity greater or equal to the one that
				// is desired. If so return it.
				// Otherwise take the largest one and expand the capacity
				// to the desired capacity or if there are no lists in the pool
				// then allocate a new one with the desired capacity
				List<T> list = null;
				int listIndex = -1;
				for (int i = 0; i < pool.Count && i < MaxCapacitySearchLength; i++) {
					// ith last item
					var candidate = pool[pool.Count-1-i];

					if (candidate.Capacity >= capacity) {
						pool.RemoveAt(pool.Count-1-i);
						inPool.Remove(candidate);
						return candidate;
					} else if (list == null || candidate.Capacity > list.Capacity) {
						list = candidate;
						listIndex = pool.Count-1-i;
					}
				}

				if (list == null) {
					list = new List<T>(capacity);
				} else {
					list.Capacity = capacity;
					// Swap current item and last item to enable a more efficient removal
					pool[listIndex] = pool[pool.Count-1];
					pool.RemoveAt(pool.Count-1);
					inPool.Remove(list);
				}
				return list;
			}
#endif
		}

		/** Makes sure the pool contains at least \a count pooled items with capacity \a size.
		 * This is good if you want to do all allocations at start.
		 */
		public static void Warmup (int count, int size) {
			lock (pool) {
				var tmp = new List<T>[count];
				for (int i = 0; i < count; i++) tmp[i] = Claim(size);
				for (int i = 0; i < count; i++) Release(tmp[i]);
			}
		}

		/** Releases a list.
		 * After the list has been released it should not be used anymore.
		 *
		 * \throws System.InvalidOperationException
		 * Releasing a list when it has already been released will cause an exception to be thrown.
		 *
		 * \see Claim
		 */
		public static void Release (List<T> list) {
#if !ASTAR_NO_POOLING
			list.Clear();

			lock (pool) {
#if !ASTAR_OPTIMIZE_POOLING
				if (!inPool.Add(list)) {
					throw new InvalidOperationException("You are trying to pool a list twice. Please make sure that you only pool it once.");
				}
#endif
				pool.Add(list);
			}
#endif
		}

		/** Clears the pool for lists of this type.
		 * This is an O(n) operation, where n is the number of pooled lists.
		 */
		public static void Clear () {
			lock (pool) {
#if !ASTAR_OPTIMIZE_POOLING
				inPool.Clear();
#endif
				pool.Clear();
			}
		}

		/** Number of lists of this type in the pool */
		public static int GetSize () {
			// No lock required since int writes are atomic
			return pool.Count;
		}
	}
}
