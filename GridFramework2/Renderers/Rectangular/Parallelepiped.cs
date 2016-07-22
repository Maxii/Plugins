using UnityEngine;

namespace GridFramework.Renderers.Rectangular {
	/// <summary>
	///   Parallelepiped shape of a rect grid.
	/// </summary>
	/// <remarks>
	///   <para>
	///     A parallelepiped is similar to a cuboid, except that it can also
	///     have a shearing. The volume to render is given by two vectors, one
	///     for the lower and one for the upper limit.
	///   </para>
	/// </remarks>
	[AddComponentMenu("Grid Framework/Renderers/Rectangular/Parallelepiped")]
	public sealed class Parallelepiped : RectangularRenderer {
#region  Private variables
		[SerializeField] private Vector3 _from = new Vector3(0, 0, 0);
		[SerializeField] private Vector3 _to   = new Vector3(5, 5, 5);
#endregion  // Private variables

#region  Properties
		/// <summary>
		///   Lower limit of the rendering range.
		/// </summary>
		public Vector3 From {
			get {
				return _from;
			} set {
				var difference = _from - value;
				_from = Vector3.Min(value, To);
				if (difference != Vector3.zero) {
					UpdatePoints();
				}
			}
		}

		/// <summary>
		///   Upper limit of the rendering range.
		/// </summary>
		public Vector3 To {
			get {
				return _to;
			} set {
				var difference = _to - value;
				_to = Vector3.Max(value, From);
				if (difference != Vector3.zero) {
					UpdatePoints();
				}
			}
		}
#endregion

#region  Count
		protected override void CountLines() {
			int x = Mathf.FloorToInt(_to.x) - Mathf.CeilToInt(_from.x) + 1;
			int y = Mathf.FloorToInt(_to.y) - Mathf.CeilToInt(_from.y) + 1;
			int z = Mathf.FloorToInt(_to.z) - Mathf.CeilToInt(_from.z) + 1;

			_lineCount[0] = 1 * y * z;
			_lineCount[1] = x * 1 * z;
			_lineCount[2] = x * y * 1;
		}
#endregion  // Count

#region  Compute
		protected override void ComputeLines() {
			var minWidth  = Mathf.CeilToInt(From.x);
			var minHeight = Mathf.CeilToInt(From.y);
			var minDepth  = Mathf.CeilToInt(From.z);

			// Amount of lines perpendicular to each axis
			int[] lines = {
				Mathf.FloorToInt(To.x) - minWidth  + 1,
				Mathf.FloorToInt(To.y) - minHeight + 1,
				Mathf.FloorToInt(To.z) - minDepth  + 1
			};

			// The starting point of the first pair (an iteration vector will
			// be added to this), everything in the left bottom back.
			Vector3[] startPoint = {
				Grid.GridToWorld(new Vector3(From.x  , minHeight, minDepth)),
				Grid.GridToWorld(new Vector3(minWidth, From.y   , minDepth )),
				Grid.GridToWorld(new Vector3(minWidth, minHeight, From.z   ))
			};
			
			// This will be added to each first point in a pair
			Vector3[] endDirection = {
				Grid.GridToWorld(new Vector3(To.x, minHeight, minDepth)) - startPoint[0],
				Grid.GridToWorld(new Vector3(minWidth, To.y, minDepth))  - startPoint[1],
				Grid.GridToWorld(new Vector3(minWidth, minHeight, To.z)) - startPoint[2]
			};

			// A multiple of this will be added to the starting point for
			// iteration
			Vector3[] iterationVector = {Grid.Right, Grid.Up, Grid.Forward};
			
			// Assemble the array
			for (var i = 0; i < 3; ++i) {			
				// When collecting the line sets we need to know the amount of
				// lines, it depends on the other two coordinates that don't
				// affect the line's size. Get them using modulo
				var idx1 = (i + 1) % 3;
				var idx2 = (i + 2) % 3;
				// j+k won't do it if one is larger than zero, use this
				// independent iterator
				var iterator = 0;

				for (var j = 0; j < lines[idx1]; ++j) {
					for (var k = 0; k < lines[idx2]; ++k) {
						var start = startPoint[i];
						var iter1 = j * iterationVector[idx1];
						var iter2 = k * iterationVector[idx2];

						var point1 = start + iter1 + iter2;
						var endDir = endDirection[i];

						_lineSets[i][iterator][0] = point1;
						_lineSets[i][iterator][1] = point1 + endDir;
						++iterator;
					}
				}
			}
		}
#endregion  // Compute
	}
}
