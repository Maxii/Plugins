using UnityEngine;

namespace GridFramework.Renderers.Hexagonal {
	/// <summary>
	///   Herringbone pattern along the faces of the hexagons.
	/// </summary>
	/// <remarks>
	///   <para>
	///     This renderer is similar to the <c>Parallelepiped</c> renderer for
	///     rectangular grids. The herringbone lines connect the faces of
	///     hexagons with straight lines and it is possible to draw partial
	///     lines, i.e. lines that don't reach all the way to the face of the
	///     next hexagon.
	///   </para>
	/// </remarks>
	[AddComponentMenu("Grid Framework/Renderers/Hexagonal/Herringbone")]
	public sealed class Herringbone : HexRenderer {
#region  Types
		/// <summary>
		///   Whether odd columns are shifted up or down.
		/// </summary>
		public enum OddColumnShift {
			/// <summary>
			///   Every odd column is shifted upwards.
			/// </summary>
			Up,
			/// <summary>
			///   Every odd column is shifted downwards.
			/// </summary>
			Down
		}
#endregion  // Types

#region  Private variables
		[SerializeField] private OddColumnShift  _shift = OddColumnShift.Up;

		[SerializeField] private Vector3 _from = new Vector3(-2, -2, -2);
		[SerializeField] private Vector3 _to   = new Vector3( 2,  2,  2);

#endregion  // Private variables

#region  Properties
		/// <summary>
		///   The shift of every odd column, up or down.
		/// </summary>
		public OddColumnShift Shift {
			get {
				return _shift;
			} set {
				var previous = _shift;
				_shift = value;
				OnShiftChange(previous);
			}
		}

		/// <summary>
		///   Lower limit of the rendering range.
		/// </summary>
		public Vector3 From {
			get {
				return _from;
			} set {
				var previous = _from;
				_from = Vector3.Min(value, To);
				OnFromChange(previous);
			}
		}

		/// <summary>
		///   Upper limit of the rendering range.
		/// </summary>
		public Vector3 To {
			get {
				return _to;
			} set {
				var previous = _from;
				_to = Vector3.Max(value, From);
				OnToChange(previous);
			}
		}
#endregion  // Properties

#region  Count
		protected override void CountLines() {

			// Vertical lines are the same as for rectangular grids
			//
			// | | | | |  | | | | |  Horizontal lines are same as for rectangular
			// |\|/|\|/|  |/|\|/|\|  grids, times the number of vertical strips,
			// | | | | |  | | | | |  plus two for the outside strips
			// |\|/|\|/|  |/|\|/|\|
			// | | | | |  | | | | |  Layer lines are the same as for rectangular
			// |\|/|\|/|  |/|\|/|\|  grids
			if (FlatSides) {
				Swap<float>(ref _from.x, ref _from.y);
				Swap<float>(ref _to.x, ref _to.y);
			}

			int x = Mathf.FloorToInt(To.x) - Mathf.CeilToInt(From.x) + 1;
			int y = Mathf.FloorToInt(To.y) - Mathf.CeilToInt(From.y) + 1;
			int z = Mathf.FloorToInt(To.z) - Mathf.CeilToInt(From.z) + 1;

			_lineCount[0] = (x+1) * y * z;
			_lineCount[1] =  x    * 1 * z;
			_lineCount[2] = (x+1) * y    ;

			if (FlatSides) {
				Swap<int>(ref _lineCount[0], ref _lineCount[1]);
				Swap<float>(ref _from.x, ref _from.y);
				Swap<float>(ref _to.x, ref _to.y);
			}
		}
#endregion  // Count

#region  Compute
		protected override void ComputeLines() {
			// How it works
			// ============
			//
			// We have three types of lines: zig-zag, straight and layer lines.
			// Straight and layer are similar: Start at the lower-left-back
			// corner and iterate over the colums, and inside those either over
			// the layers (straight) or rows (layer). In each inner step draw
			// one line.
			//
			// Zig-zag lines require looping over colums, rows and layers. In
			// each step add a vector to the current vertex that depends on
			// whether the current column is even or odd.
			//
			//
			// Pointed VS flat sides
			// ---------------------
			//
			// Pointed sides are the default. We want to re-use the algorithm
			// for flat sides, so we will "rotate" the entire grid 90°
			// counter-clockwise and pretend that it's a pointed grid then.
			//
			// To this end the left edge becomes the bottom, the right edge the
			// top, the top the left edge (inverse sign) and the bottom the
			// right edge (inverse sign).
			//
			// We also need different basis vectors: the flat `right` vector
			// acts like the pointy `up` and the two flat `down` act like the
			// pointed `right`.

			var pointed = Grid.Sides == GridFramework.Grids.HexGrid.Orientation.Pointed;
			var shiftUp = Shift == OddColumnShift.Up;

			Vector3 from = From, to = To;

			var up = Vector3.zero;
			var forward = Vector3.zero;
			var right = new Vector3[2];

			if (FlatSides) {
				Swap<Vector3[][]>(ref _lineSets[0], ref _lineSets[1]);
			}

			if (!pointed) {
				// Swap coordinates counter-clockwise
				from.x = -To.y;
				from.y = From.x;
				to.y   = To.x;
				to.x   = -From.y;
			}

			// The limits rounded up or down to integers, used when a hard
			// limit is required
			int Right = Mathf.FloorToInt(to.x), Left   = Mathf.CeilToInt(from.x);
			int Top   = Mathf.FloorToInt(to.y), Bottom = Mathf.CeilToInt(from.y);
			int Front = Mathf.FloorToInt(to.z), Back   = Mathf.CeilToInt(from.z);

			// the local origin transformed to world space
			var origin = DetermineOrigin(pointed, shiftUp, Left, Bottom, Back);

			MakeBasisVectors(pointed, shiftUp, ref right, ref up, ref forward);

			System.Action<Vector3[][], Vector3> zigZagLines = (lines, o) => {
				var i = 0;  // Iterator through the lines array.
				for (var column = Left; column <= Right; ++column) {
					var u = column % 2 == 0 ? 0 : 1;  // index to use for the `right` array
					var r = o;
					for (var row = Bottom; row <= Top; ++row) {
						var l = r;
						for (var layer = Back; layer <= Front; ++layer) {
							if (column != Right) {
								lines[i][0] = l;
								lines[i][1] = lines[i][0] + right[u];
								++i;
							}
							if (column == Left) { // Leftmost zig-zag lines
								var extra = from.x - Left;
								lines[i][0]  = l;
								lines[i][1]  = lines[i][0] + extra * right[u == 0 ? 1 : 0];
								++i;
							}
							if (column == Right) { // Rightmost zig-zag lines
								var extra = to.x - Right;
								lines[i][0] = l;
								lines[i][1] = lines[i][0] + extra * right[u];
								++i;
							}
							l += forward;
						}
						r += up;
					}
					o += right[u];
				}
			};

			System.Action<Vector3[][], Vector3> straightLines = (lines, o) => {
				var i = 0;  // Iterator through the lines array.
				var extra  = from.y - Bottom;
				var length = to.y   - from.y;
				for (var column = Left; column <= Right; ++column) {
					var u = column % 2 == 0 ? 0 : 1;  // index to use for the `right` array
					var l = o;
					for (var layer = Back; layer <= Front; ++layer) {
						lines[i][0] = l + extra * up;
						lines[i][1] = lines[i][0] + length * up;
						++i;
						l += forward;
					}
					o += right[u];
				}
			};

			System.Action<Vector3[][], Vector3> layerLines = (lines, o) => {
				var i = 0;  // Iterator through the lines array.
				var extra  = from.z - Back;
				var length = to.z   - from.z;
				for (var column = Left; column <= Right; ++column) {
					var u = column % 2 == 0 ? 0 : 1;  // index to use for the `right` array
					var r = o;
					for (var row = Bottom; row <= Top; ++row) {
						lines[i][0] = r + extra * forward;
						lines[i][1] = lines[i][0] + length * forward;
						++i;
						r += up;
					}
					o += right[u];
				}
			};

			zigZagLines(_lineSets[0], origin);
			straightLines(_lineSets[1], origin);
			layerLines(_lineSets[2], origin);

			if (FlatSides) {
				Swap<Vector3[][]>(ref _lineSets[0], ref _lineSets[1]);
			}
		}

		Vector3 DetermineOrigin(bool pointed, bool shiftedUp, int left, int bottom, int back) {
			var herring = pointed ? new Vector3(left, bottom, back)
					              : new Vector3(bottom, -left, back);
			return shiftedUp ? Grid.HerringUpToWorld(herring)
					         : Grid.HerringDownToWorld(herring);
		}

		void MakeBasisVectors(bool pointed,
		                      bool shiftedUp,
		                      ref Vector3[] right,
		                      ref Vector3 up,
		                      ref Vector3 forward) {
			if (pointed) {
				up      = Grid.HerringUpToWorld(Vector3.up) - Grid.HerringUpToWorld(Vector3.zero);
				right   = new [] {
					Grid.HerringDownToWorld(Vector3.right) - Grid.HerringDownToWorld(Vector3.zero),
					Grid.HerringUpToWorld(Vector3.right) - Grid.HerringUpToWorld(Vector3.zero),
				};
			} else {
				up    = Grid.HerringUpToWorld(Vector3.right) - Grid.HerringUpToWorld(Vector3.zero);
				right = new [] {
					Grid.HerringDownToWorld(-Vector3.up) - Grid.HerringDownToWorld(Vector3.zero),
					Grid.HerringUpToWorld(-Vector3.up) - Grid.HerringUpToWorld(Vector3.zero),
				};
			}
			forward = Grid.HerringUpToWorld(Vector3.forward) - Grid.HerringUpToWorld(Vector3.zero);

			if (shiftedUp) {
				Swap<Vector3>(ref right[0], ref right[1]);
			}
		}
#endregion  // Compute

#region  Hook methods
		private void OnShiftChange(OddColumnShift previous) {
			if (previous == Shift) {
				return;
			}
			UpdatePoints();
		}

		private void OnFromChange(Vector3 previous) {
			if (From - previous == Vector3.zero) {
				return;
			}
			UpdatePoints();
		}

		private void OnToChange(Vector3 previous) {
			if (To - previous == Vector3.zero) {
				return;
			}
			UpdatePoints();
		}
#endregion  // Hook methods
	}
}
