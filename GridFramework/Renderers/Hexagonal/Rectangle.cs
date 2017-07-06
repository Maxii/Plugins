using UnityEngine;
using GridFramework.Grids;

namespace GridFramework.Renderers.Hexagonal {
	/// <summary>
	///   A rectangular arrangement of hexagons with alternating columns or
	///   rows.
	/// </summary>
	/// <remarks>
	///   <para>
	///     Hexagons are arranged in the rectangular pattern with every odd
	///     column (row if the sides are pointed) shifted up or down (right or
	///     left if the sides are pointed), depending on your settings.
	///   </para>
	/// </remarks>
	[AddComponentMenu("Grid Framework/Renderers/Hexagonal/Rectangle")]
	public sealed class Rectangle : HexRenderer {
#region  Types
		/// <summary>
		///   Whether odd columns are shifted up, down or clipped.
		/// </summary>
		public enum OddColumnShift {
			/// <summary>
			///   Every odd column is shifted upwards.
			/// </summary>
			Up,
			/// <summary>
			///   Every odd column is shifted downwards.
			/// </summary>
			Down,
			/// <summary>
			///   Every odd column is clipped.
			/// </summary>
			Clipped
		}
#endregion  // Types

#region  Private variables
		[SerializeField] private int _bottom = -2;
		[SerializeField] private int _top    =  2;
		[SerializeField] private int _left   = -2;
		[SerializeField] private int _right  =  2;
	
		[SerializeField] private float _layerFrom = -2;
		[SerializeField] private float _layerTo   =  2;
	
		[SerializeField] private OddColumnShift _mode;
#endregion  // Private variables
	
#region  Properties
		/// <summary>
		///   The shift of every odd column, up or down.
		/// </summary>
		public OddColumnShift Shift {
			get {
				return _mode;
			} set {
				var previous = _mode;
				_mode = value;
				OnShiftChanged(previous);
			}
		}

		/// <summary>
		///   Index of the bottom row.
		/// </summary>
		public int Bottom {
			get {
				return _bottom;
			} set {
				var previous = _bottom;
				_bottom = Mathf.Min(value, Top);
				OnHorizontalChanged(previous, Top);
			}
		}

		/// <summary>
		///   Index of the top row.
		/// </summary>
		public int Top {
			get {
				return _top;
			} set {
				var previous = _top;
				_top = Mathf.Max(value, Bottom);
				OnHorizontalChanged(Bottom, previous);
			}
		}

		/// <summary>
		///   Index of the left column.
		/// </summary>
		public int Left {
			get {
				return _left;
			} set {
				var previous = _left;
				_left = Mathf.Min(value, Right);
				OnVerticalChanged(previous, Right);
			}
		}

		/// <summary>
		///   Index of the right column.
		/// </summary>
		public int Right {
			get {
				return _right;
			} set {
				var previous = _right;
				_right = Mathf.Max(value, Left);
				OnVerticalChanged(Left, previous);
			}
		}

		/// <summary>
		///   First layer of the rendering.
		/// </summary>
		public float LayerFrom {
			get {
				return _layerFrom;
			}
			set {
				var previous = _layerFrom;
				_layerFrom = Mathf.Min(LayerTo, value);
				OnLayerChanged(previous, LayerTo);
			}
		}

		/// <summary>
		///   Last layer of the rendering.
		/// </summary>
		public float LayerTo {
			get {
				return _layerTo;
			}
			set {
				var previous = _layerTo;
				_layerTo = Mathf.Max(LayerFrom, value);
				OnLayerChanged(LayerFrom, previous);
			}
		}
#endregion
	
#region  Count
		protected override void CountLines() {
			// First we need to count the amount of hexes. Starting in the lower
			// left corner each hex always adds one  horizontal and two vertical
			// lines: S, SW, NW. On top of this there are edge cases:
			//    _   _
			//  _/ \_/ \_  (1) Every top hex needs a N line.
			// / \_/ \_/ \ (2) Every right hex needs a NE and SE line.
			// \_/o\_/o\_/ (3) Every even bottom hex needs a SE line, unless it is
			// /e\_/e\_/e\     in the rightmost column, in which case see point 2.
			// \_/ \_/ \_/ (4) Every odd top hex needs a NE line, unless it is in
			// / \_/ \_/ \     the rightmost column, in which case see point 2.
			// \_/ \_/ \_/
			//
			// Since the even bottom hexes and odd top hexes add one diagonal line
			// each we can simply add one diagonal line total per column, then
			// subtract the one line that satisfies two conditions (point 2 and
			// either point 3 or point 4). Since only one such line can exist we
			// always subtract one.
			//
			// As for the straight lines, each column always adds one 1 line, so
			// nothing special there.
			//
			// Downwards grids have the same number of lines, but slightly
			// different rules:
			//  _   _   _
			// / \_/ \_/ \  (a) Every top hex needs a N line.
			// \_/ \_/ \_/  (b) Every right hex needs a NE and SE line.
			// /e\_/e\_/e\  (c) Every even top hex needs a NE line, unless it is in
			// \_/o\_/o\_/      the rightmost column, in which case see point (b).
			// / \_/ \_/ \  (d) Every odd bottom hex needs a SE line, unless it is
			// \_/ \_/ \_/      in the rightmost column, in which case see point (b).
			//   \_/ \_/
			//
			// These are exactly the same rules, except the roles of (3) and (4)
			// are swapped.
			//  _   _   _
			// / \_/ \_/ \  Clipped rectangles are the same both up- and downwards. We'll
			// \_/o\_/o\_/  use an upwards pattern and then remove certain lines from it:
			// /e\_/e\_/e\
			// \_/ \_/ \_/  (  i)  From every odd column remove one horizontal (N) and two
			// / \_/ \_/ \         vertical lines (NW, NE).                       _
			// \_/ \_/ \_/  ( ii)  If the left-most column is odd remove one    _/e\_
			//                     vertical line (top SW).                     /o\_/o\
			//              (iii)  If the right-most column is odd remove one  \_/e\_/
			//                     vertical line (top SE).                       \_/

			var hexesH = Right - Left   + 1; // columns
			var hexesV = Top   - Bottom + 1; // rows

			var layers = Mathf.FloorToInt(LayerTo) - Mathf.CeilToInt(LayerFrom) + 1;

			// swap the role of horizontal and vertical for flat sides
			Swap<int>(ref hexesH, ref hexesV, FlatSides);

			// regular cases + 1 top line per column
			_lineCount[0] = layers * (1 * hexesH * hexesV + 1 * hexesH);
			// regular cases + 2 diagonal lines per row + 1 diagonal line per
			// column - 1 line that's either even-bottom-right-SE or odd-top-right
			// NE
			_lineCount[1] = layers * (2 * hexesH * hexesV + 2 * hexesV + 1 * hexesH - 1);

			// The blue Z-lines depend on the amount of hexes as well. Each hex
			// adds two lines, a SW and a W line. There is no W line for the
			// top-most left-most hex if it is in the odd column of a clipped grid.
			// The edge cases are as follows:
			//
			//  (1') Every top hex needs a NW line            (a') Every top hex needs a NW line
			//  (2') Every right hex needs an E line          (b') Every right hex needs an E line
			//  (3') Every even bottom hex needs a SE line    (c') Every odd bottom hex needs a SE line
			//  (4') Every odd top hex needs a NE line        (d') Every even top hex needs a NE line
			//  (5') Every right hex needs, if it is even,    (e') Every right hex needs, if it is even,
			//       a NE line, or, if it is odd, a SE line        a SE line, or, if it is odd, a NE line
			//
			// Point one adds on line per column, point two adds one line per row,
			// point three and four add together one line per column and point four
			// adds one line per row.
	
			// regular case + 1 NW top per column + 1 E right per row + 1 SE (even
			// bottom) or NE (odd top) per column + 1 NE (even) or SE (odd) right
			// per row
			_lineCount[2] = 2 * hexesH * hexesV + 1 * hexesH + 1 * hexesV + 1 * hexesH + 1 * hexesV;

			// If the rectangle is compact we have to subtract some lines.
			if (Shift == OddColumnShift.Clipped) {
				// First count the number of odd columns. There are two cases:
				// either the total number of columns is even, or it is odd. If
				// it's even then exactly half of the columns are odd.
				//
				// If the total is odd, then either half of (total - 1) columns are
				// odd or the same amount is even. If the first column is odd it's
				// the latter, otherwise the former.
				//   _   _        _   _      _   _   _       _   _
				//  / \_/ \_    _/ \_/ \    / \_/ \_/ \    _/ \_/ \_
				//  \_/o\_/o\  /o\_/o\_/    \_/o\_/o\_/   /o\_/o\_/o\
				//  /e\_/e\_/  \_/e\_/e\    /e\_/e\_/e\   \_/e\_/e\_/
				//  \_/ \_/ \  / \_/ \_/    \_/ \_/ \_/   / \_/ \_/ \
				//  / \_/ \_/  \_/ \_/ \    / \_/ \_/ \   \_/ \_/ \_/
				//  \_/ \_/      \_/ \_/    \_/ \_/ \_/     \_/ \_/
				//                          
				//   Case 1     Case 1       Case 2.1     Case 2.2
				
				var left  = FlatSides ? -Top    : Left;
				var right = FlatSides ? -Bottom : Right;
				int oddColumns;
				if (IsEven(hexesH)) {
					oddColumns = hexesH / 2;
				} else {
					oddColumns = (hexesH - 1) / 2 + (IsEven(left) ? 0 : 1);
				}
				//Debug.Log(oddColumns);
				// Every odd top north lines needs to be removed. Every odd top NE
				// and NW line needs to be removed. The first odd top SW and last
				// top SE line need to be removed.
				_lineCount[0] -= (1 * oddColumns                                                   ) * layers;
				_lineCount[1] -= (2 * oddColumns + (IsEven(left) ? 0 : 1) + (IsEven(right) ? 0 : 1)) * layers;
				_lineCount[2] -=  2 * oddColumns + (IsEven(left) ? 0 : 1) + (IsEven(right) ? 0 : 1)          ;
			}
	
			if (FlatSides) {
				Swap<int>(ref _lineCount[0], ref _lineCount[1]);
			}
		}
#endregion  // Count
	
#region  Compute
		protected override void ComputeLines() {
			// How to draw the hexes
			// ---------------------           _   _          _   _
			//   /   /         /   / \       _/  _/ \       _/ \_/ \
			// / \_/ \_      / \_/ \_/      / \_/ \_/      / \_/ \_/
			// \_/ \_/       \_/ \_/ \      \_/ \_/ \      \_/ \_/ \
			// / \_/ \_  ->  / \_/ \_/  ->  / \_/ \_/  ->  / \_/ \_/
			// \_/ \_/       \_/ \_/ \      \_/ \_/ \      \_/ \_/ \
			// / \_/ \_      / \_/ \_/      / \_/ \_/      / \_/ \_/
			// \_  \_        \_  \_         \_  \_         \_/ \_/
			// S, SW, NW      SE, NE           N            SE, NE
			var pointed   = Grid.Sides == HexGrid.Orientation.Pointed;
			var downShift = Shift == OddColumnShift.Down;
	
			int Front = Mathf.FloorToInt(LayerTo), Back = Mathf.CeilToInt(LayerFrom);
	
			// iterator variables
			int iterator_x = 0, iterator_y = 0, iterator_z = 0;
	
			var right = new Vector3[2];
			var up = Vector3.zero;
			var forward = Vector3.zero;
			
			int leftEdge = Left, rightEdge = Right, bottomEdge = Bottom, topEdge = Top;

			if (!pointed) {
				// Swap coordinates counter-clockwise
				leftEdge   = -Top;
				rightEdge  = -Bottom;
				topEdge    =  Right;
				bottomEdge =  Left;

				Swap<Vector3[][]>(ref _lineSets[0], ref _lineSets[1]);
			}
	
			/* We loop in the following order: column -> row -> layer. First we
	 		 * take a column, then a row and then go through the layers of that
	 		 * hex. The tree layers of the loop do the following:
	 		 *
			 *  - INNER:
			 *      Every hex on every layer contributes a S, SW and NW line. The
			 *      rightmost hexes contribute also a NE and SE line.
			 *
			 *  - MIDDLE:
			 *      Before the first layer is set the original from layer value is
			 *      used to contribute the first points of the SW and W layer-line.
			 *      If the column is rightmost the first point of the E layer-line
			 *      is contributed. Also, if it is an even column a NE point is
			 *      contributed, otherwise a SE point is contributed.  Then the
			 *      layer is set and the inner loop starts. After exiting the loop
			 *      the variable `k` is set to the original `t` value and the above
			 *      rules are applied again to contribute the second point of each
			 *      layer line.
			 *
			 *  - OUTER: 
			 *      If the column is even and not rightmost a bottom SE line is
			 *      contributed.  Afterwards we contribute a complete SE bottom
			 *      layer line if the column is even.  Then the middle loop starts.
			 *      After exiting the loop we are in the top row, so we contribute
			 *      a N line. If the column is odd and not rightmost we contribute
			 *      a top NE line. Finally, if the column is odd we contribute a
			 *      complete top NE layer line.
			 */
	
			/* In order to keep performance high we need to stay in world-space as
	 		 * much as possible, ideally the amount of conversions from herring
	 		 * space to world space should be constant. To achieve this we will
	 		 * only convert the position of the very first hex from herring- to
	 		 * world space and from there on add world-space direction vectors.
	 		 * These direction vectors will be created from herring space as well
	 		 * and prepared before the loop starts. While in the loop the
	 		 * methodology will be the same as if we were using herring
	 		 * coordinates, except there will be no conversions taking place.
			 *
			 * We need three hex coordinates. The first will be be column hex, it
			 * will be initialized once entering the outer loop and incremented
			 * every time we move to another column. The row hex will be
			 * initialized before entering the middle loop from the current column
			 * hex and incremented on every row iteration. The layer hex is
			 * initialized before entering the inner loop and incremented on every
			 * layer iteration. It is the hex that's actually used for contributing
			 * lines.
			 *
			 * We need incrementation vectors: right, up and down. Right is an
			 * array, so we can use `i % 2` to pick either the even or odd value
			 * for each column.
			 *
			 * Finally we need six vertex vectors, one for each vertex possibility.
			 * There are always two variants of each vector, one for pointy sides
			 * and one for flat sides.  Since the rules are the same for both types
			 * we don't need any other adjustments. We also need two layer vectors
			 * for front and back layer lines
			 */
	
			Vector3 EE, NE, NW, WW, SW, SE;

			if (pointed) {
				EE = CardinalToVertex(HexGrid.HexDirection.E );
				NE = CardinalToVertex(HexGrid.HexDirection.NE);
				NW = CardinalToVertex(HexGrid.HexDirection.NW);
				WW = CardinalToVertex(HexGrid.HexDirection.W );
				SW = CardinalToVertex(HexGrid.HexDirection.SW);
				SE = CardinalToVertex(HexGrid.HexDirection.SE);
			} else {
				EE = CardinalToVertex(HexGrid.HexDirection.SE);
				NE = CardinalToVertex(HexGrid.HexDirection.E );
				NW = CardinalToVertex(HexGrid.HexDirection.NE);
				WW = CardinalToVertex(HexGrid.HexDirection.NW);
				SW = CardinalToVertex(HexGrid.HexDirection.W );
				SE = CardinalToVertex(HexGrid.HexDirection.SW);
			}

			MakeBasisVectors(pointed, downShift, ref right, ref up, ref forward);

			System.Action<Vector3, int, int, int> drawHex = (hex, c, r, l) => {
				var even = c % 2 == 0;
				var odd  = !even;
				var clipped = Shift == OddColumnShift.Clipped;
				var topmost = r == topEdge;
				var leftmost = c == leftEdge;

				System.Action<Vector3, Vector3> horizontalLine =
					(v1, v2) => ContributeLine(_lineSets[0], hex, v1, v2, ref iterator_x);
				System.Action<Vector3, Vector3> verticalLine =
					(v1, v2) => ContributeLine(_lineSets[1], hex, v1, v2, ref iterator_y);

				horizontalLine(SE, SW);
				if (!(clipped && odd && topmost)) {
					verticalLine(WW, NW);
				}
				if (!(clipped && odd && leftmost && topmost)) {
					verticalLine(SW, WW);
				}
				if (topmost && !(clipped && odd)) {
					horizontalLine(NE, NW);
				}
				if (c == rightEdge) {
					if (!(clipped && odd && topmost)) {
						verticalLine(EE, NE);
					}
					if (!(clipped && odd && topmost)) {
						verticalLine(SE, EE);
					}
					return;
				}
				if (r == bottomEdge && (even && !downShift || odd && downShift)) {
					verticalLine(SE, EE);
				}
				if (topmost && (odd && !downShift || even && downShift)) {
					if (!clipped) {
						verticalLine(EE, NE);
					}
				}
			};

			Vector3 back = LayerFrom * forward, front = LayerTo * forward;

			System.Action<Vector3, int, int> drawLayer = (hex, column, row) => {
				var even = column % 2 == 0;
				var odd  = !even;

				var clipped = Shift == OddColumnShift.Clipped;
				var upShift = clipped || Shift == OddColumnShift.Up;

				var topmost   =    row == topEdge;
				var leftmost  = column == leftEdge;
				var rightmost = column == rightEdge;

				System.Action<Vector3> layerLine =
					vertex => ContributeLine(_lineSets[2], hex, vertex, back, front, ref iterator_z);
				
				layerLine(SW);
				if (!(clipped && leftmost && topmost && odd)) {
					layerLine(WW);
				}
				if (topmost && !(clipped && odd)) {
					layerLine(NW);
				}
				if (row == bottomEdge && (upShift && even || !upShift && !even)) {
					layerLine(SE);
				}
				if (topmost && (upShift && !even || !upShift && even)) {
					if (!clipped) {
						layerLine(NE);
					}
				}
				if (rightmost) {
					if (!clipped) {
						layerLine(EE);
					}
					if (even) {
						layerLine(upShift ? NE : SE);
					} else {
						layerLine(upShift ? SE : NE);
					}
				}
			};

			Vector3 hexColumn = DetermineOrigin(pointed, downShift, leftEdge, bottomEdge, Back);
			for (var column = leftEdge; column <= rightEdge; ++column) {
				var hexRow = hexColumn;
				for (var row = bottomEdge; row <= topEdge; ++row) {
					var hexLayer = hexRow;
					for (var layer = Back; layer <= Front; ++layer) {
						drawHex(hexLayer, column, row, layer);
						hexLayer += forward;
					}
					hexRow += up;
				}
				hexColumn += right[column % 2 == 0 ? 1 : 0];
			}

			hexColumn = DetermineOrigin(pointed, downShift, leftEdge, bottomEdge, 0);
			for (var column = leftEdge; column <= rightEdge; ++column) {
				var hexRow = hexColumn;
				for (var row = bottomEdge; row <= topEdge; ++row) {
					drawLayer(hexRow, column, row);
					hexRow += up;
				}
				hexColumn += right[column % 2 == 0 ? 1 : 0];
			}
	
			// Undo our little swapping trick
			if (FlatSides) {
				Swap<Vector3[][]>(ref _lineSets[0], ref _lineSets[1]);
			}
		}

		Vector3 DetermineOrigin(bool pointed, bool shiftedDown, int left, int bottom, int back) {
			var herring = pointed ? new Vector3(left, bottom, back)
					              : new Vector3(bottom, -left, back);
			return shiftedDown ? Grid.HerringDownToWorld(herring)
					           : Grid.HerringUpToWorld(herring);
		}

		void MakeBasisVectors(bool pointed,
		                      bool shiftedDown,
		                      ref Vector3[] right,
		                      ref Vector3 up,
		                      ref Vector3 forward) {
			var rVector = pointed ? Vector3.right : -Vector3.up;
			var uVector = pointed ? Vector3.up    : Vector3.right;

			right   = new [] {
				Grid.HerringDownToWorld(rVector) - Grid.HerringDownToWorld(Vector3.zero),
				Grid.HerringUpToWorld(rVector) - Grid.HerringUpToWorld(Vector3.zero),
			};
			up      = Grid.HerringUpToWorld(uVector        ) - Grid.HerringUpToWorld(Vector3.zero);
			forward = Grid.HerringUpToWorld(Vector3.forward) - Grid.HerringUpToWorld(Vector3.zero);

			if (shiftedDown) {
				Swap<Vector3>(ref right[0], ref right[1]);
			}
		}
#endregion  // Compute

#region  Hook methods
		private void OnHorizontalChanged(int previousLeft, int previousRight) {
			if (previousLeft == Left && previousRight == Right) {
				return;
			}
			UpdatePoints();
		}

		private void OnVerticalChanged(int previousBotton, int previousTop) {
			if (previousBotton == Left && previousTop == Right) {
				return;
			}
			UpdatePoints();
		}

		private void OnLayerChanged(float previousFrom, float previousTo) {
			var unchangedFrom = Mathf.Abs(previousFrom - LayerFrom) < Mathf.Epsilon;
			var unchangedTo   = Mathf.Abs(previousTo   - LayerTo  ) < Mathf.Epsilon;
			if (unchangedFrom && unchangedTo) {
				return;
			}
			UpdatePoints();
		}

		private void OnShiftChanged(OddColumnShift previous) {
			if (previous == Shift) {
				return;
			}
			UpdatePoints();
		}
#endregion  // Hook methods
	}
}
