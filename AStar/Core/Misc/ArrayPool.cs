#if !UNITY_EDITOR
// Extra optimizations when not running in the editor, but less error checking
#define ASTAR_OPTIMIZE_POOLING
#endif

using System;
using System.Collections.Generic;

namespace Pathfinding.Util {
	/** Lightweight Array Pool.
	 * Handy class for pooling arrays of type T.
	 *
	 * Usage:
	 * - Claim a new array using \code SomeClass[] foo = ArrayPool<SomeClass>.Claim (capacity); \endcode
	 * - Use it and do stuff with it
	 * - Release it with \code ArrayPool<SomeClass>.Release (foo); \endcode
	 *
	 * \warning Arrays returned from the Claim method may contain arbitrary data.
	 *  You cannot rely on it being zeroed out.
	 *
	 * After you have released a array, you should never use it again, if you do use it
	 * your code may modify it at the same time as some other code is using it which
	 * will likely lead to bad results.
	 *
	 * \since Version 3.8.6
	 * \see Pathfinding.Util.ListPool
	 */
	public static class ArrayPool<T>{
#if !ASTAR_NO_POOLING
		/** Internal pool.
		 * The arrays in each bucket have lengths of 2^i
		 */
		static readonly Stack<T[]>[] pool = new Stack<T[]>[31];

		static readonly HashSet<T[]> inPool = new HashSet<T[]>();
#endif

		/** Returns an array with at least the specified length */
		public static T[] Claim (int minimumLength) {
			int bucketIndex = 0;

			while ((1 << bucketIndex) < minimumLength && bucketIndex < 30) {
				bucketIndex++;
			}

			if (bucketIndex == 30)
				throw new System.ArgumentException("Too high minimum length");

#if !ASTAR_NO_POOLING
			lock (pool) {
				if (pool[bucketIndex] == null) {
					pool[bucketIndex] = new Stack<T[]>();
				}

				if (pool[bucketIndex].Count > 0) {
					var array = pool[bucketIndex].Pop();
					inPool.Remove(array);
					return array;
				}
			}
#endif
			return new T[1 << bucketIndex];
		}

		public static void Release (ref T[] array) {
#if !ASTAR_NO_POOLING
			lock (pool) {
#if !ASTAR_OPTIMIZE_POOLING
				if (!inPool.Add(array)) {
					throw new InvalidOperationException("You are trying to pool an array twice. Please make sure that you only pool it once.");
				}
#endif

				int bucketIndex = 0;
				while ((1 << bucketIndex) < array.Length && bucketIndex < 30) {
					bucketIndex++;
				}

				if (array.Length != (1 << bucketIndex)) {
					throw new ArgumentException("Array length is not a power of 2");
				}

				if (pool[bucketIndex] == null) {
					pool[bucketIndex] = new Stack<T[]>();
				}

				pool[bucketIndex].Push(array);
			}
#endif
			array = null;
		}
	}
}
