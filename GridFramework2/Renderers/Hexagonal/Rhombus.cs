using UnityEngine;
using GridFramework.Grids;

namespace GridFramework.Renderers.Hexagonal {
	/// <summary>
	///   A rhombic arrangement of hexagons with configurable direction.
	/// </summary>
	/// <remarks>
	///   <para>
	///     Hexagons are arranged in the rhombic pattern shifted up or down
	///     (right or left if the sides are pointed), depending on your
	///     settings.
	///   </para>
	/// </remarks>
	[AddComponentMenu("Grid Framework/Renderers/Hexagonal/Rhombus")]
	public sealed class Rhombus : HexRenderer {

#region  Types
		/// <summary>
		///   Direction of the rhombus (up/down or right/left).
		/// </summary>
		public enum RhombDirection {
			/// <summary>
			///   Every column is shifted upwards (pointed sides) or to the
			///   right (flat sides).
			/// </summary>
			Up,
			/// <summary>
			///   Every column is shifted downwards (pointed sides) or to the
			///   left (flat sides).
			/// </summary>
			Down
		}
#endregion  // Types

#region  Private variables
		[SerializeField] private int _bottom = -2;
		[SerializeField] private int _top    =  2;
		[SerializeField] private int _left   = -2;
		[SerializeField] private int _right  =  2;

		[SerializeField] private float _layerFrom = -2;
		[SerializeField] private float _layerTo   =  2;

		[SerializeField] private RhombDirection _direction;
#endregion  // Private variables

#region  Properties
		/// <summary>
		///   The direction of the rhombus, up- or downwards.
		/// </summary>
		public RhombDirection Direction {
			get {
				return _direction;
			} set {
				var previous = _direction;
				_direction= value;
				OnDirectionChanged(previous);
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

			// The rhombic points are easier to count than herring points:
			//      _
			//    _/ \ - The number of horizontal lines is the number of columns
			//  _/ \_/   times (number of rows + 1).
			// / \_/ \ - The number of angled lines is two times the number of rows
			// \_/ \_/   times the number of columns+1 plus two times the number of
			// / \_/ \   columns-1.
			// \_/ \_/ - The number of cylindric lines is two times the number of
			// / \_/     rows times number of +1 plus two times the number of
			// \_/       columns.

			int cs = Right - Left   + 1; // columns
			int rs = Top   - Bottom + 1; // rows

			// swap the role of horizontal and vertical for flat sides
			Swap<int>(ref cs, ref rs, FlatSides);

			var l = Mathf.FloorToInt(LayerTo) - Mathf.CeilToInt(LayerFrom) + 1; //layers

			_lineCount[0] = cs * (rs+1) * l;
			_lineCount[1] = (2 * rs * (cs+1) + cs-1) * l;
			_lineCount[2] =  2 * rs * (cs+1) + 2*cs;

			Swap<int>(ref _lineCount[0], ref _lineCount[1], FlatSides);
		}
#endregion  // Count

#region  Compute
		protected override void ComputeLines() {
			int Front = Mathf.FloorToInt(LayerTo), Back = Mathf.CeilToInt(LayerFrom);

			var pointed   = Grid.Sides == HexGrid.Orientation.Pointed;
			var flat      = Grid.Sides == HexGrid.Orientation.Flat;
			var upwards   = Direction == RhombDirection.Up;
			var downwards = Direction == RhombDirection.Down;

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

			Vector3 right = Vector3.zero, up = Vector3.zero, forward = Vector3.zero;
			MakeBasisVectors(pointed, upwards, ref right, ref up, ref forward);

			int leftEdge = Left, rightEdge = Right, bottomEdge = Bottom, topEdge = Top;

			if (flat) {
				// Swap coordinates counter-clockwise
				leftEdge   = -Top;
				rightEdge  = -Bottom;
				topEdge    =  Right;
				bottomEdge =  Left;
			}

			Vector3 hexColumn;

			// iterator variables
			int iterator_x = 0, iterator_y = 0;

			if (FlatSides) {
				Swap<Vector3[][]>(ref _lineSets[0], ref _lineSets[1]);
			}

			System.Action<Vector3, int, int, int> drawHex = (hex, column, row, layer) => {
				System.Action<Vector3, Vector3> horizontalLine =
					(v1, v2) => ContributeLine(_lineSets[0], hex, v1, v2, ref iterator_x);
				System.Action<Vector3, Vector3> verticalLine =
					(v1, v2) => ContributeLine(_lineSets[1], hex, v1, v2, ref iterator_y);

				var topmost   = row == topEdge;
				var lowest    = row == bottomEdge;
				var rightmost = column == rightEdge;

				horizontalLine(SE, SW);
				verticalLine(SW, WW);
				verticalLine(WW, NW);
				if (topmost) {
					horizontalLine(NW, NE);
				}
				if (rightmost) {
					verticalLine(SE, EE);
					verticalLine(EE, NE);
					return;
				}
				if (lowest && (pointed ? upwards : downwards)) {
					verticalLine(SE, EE);
				} else if (topmost && (pointed ? downwards : upwards)) {
					verticalLine(EE, NE);
				}
			};

			hexColumn = DetermineOrigin(pointed, upwards, leftEdge, bottomEdge, Back);
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
				hexColumn += right;
			}
		}

		Vector3 DetermineOrigin(bool pointed, bool upwards, int left, int bottom, int back) {
			var rhomb = pointed ? new Vector3(left, bottom, back)
			                    : new Vector3(bottom, -left, back);
			return upwards ? Grid.RhombicUpToWorld(rhomb)
			               : Grid.RhombicDownToWorld(rhomb);
		}

		void MakeBasisVectors(bool pointed, bool upwards,
			                  ref Vector3 right, ref Vector3 up, ref Vector3 forward) {
			var zeroHex = upwards
			              ? Grid.RhombicUpToWorld(Vector3.zero)
			              : Grid.RhombicDownToWorld(Vector3.zero);

			var rVector = pointed ? Vector3.right : -Vector3.up;
			var uVector = pointed ? Vector3.up    : Vector3.right;
			right = (upwards
			        ? Grid.RhombicUpToWorld(rVector)
			        : Grid.RhombicDownToWorld(rVector))
			        - zeroHex;

			up = (upwards
			     ? Grid.RhombicUpToWorld(uVector)
			     : Grid.RhombicDownToWorld(uVector))
			     - zeroHex;

			forward = (upwards
			          ? Grid.RhombicUpToWorld( Vector3.forward)
			          : Grid.RhombicDownToWorld(Vector3.forward))
			          - zeroHex;
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
			var changedFrom = Mathf.Abs(previousFrom - LayerFrom) < Mathf.Epsilon;
			var changedTo   = Mathf.Abs(previousTo   - LayerTo  ) < Mathf.Epsilon;
			if (changedFrom && changedTo) {
				return;
			}
			UpdatePoints();
		}

		private void OnDirectionChanged(RhombDirection previous) {
			if (previous == Direction) {
				return;
			}
			UpdatePoints();
		}
#endregion  // Hook methods
	}
}
