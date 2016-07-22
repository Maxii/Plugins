using UnityEngine;
using System;

namespace GridFramework.Renderers.Hexagonal {
	/// <summary>
	///   Hexagons wrapping around one central hexagon.
	/// </summary>
	/// <remarks>
	///   <para>
	///     One central hex is set as the origin. At each of the six edges a
	///     row of other hexagons is appended, the user can set at which edge
	///     to start and at which one to end. This creates a sort of "area of
	///     effect" cone shape used in board games.
	///   </para>
	///   <para>
	///     The hexagons around the origin do not have to start adjacent to the
	///     origin, you can also start further away.
	///   </para>
	///   <para>
	///     This shape is very similar to the <c>Cylinder</c> renderer of polar
	///     grids and we use similar terms. The difference is that instead of
	///     an angle we use an edge and all values must be integers.
	///   </para>
	/// </remarks>
	[AddComponentMenu("Grid Framework/Renderers/Hexagonal/Cone")]
	public sealed class Cone : HexRenderer {
#region  Private variables
		/// <summary>
		///   Vectors from hex to vertices (world space).
		/// </summary>
		private Vector3[] vertices = new Vector3[6];

		/// <summary>
		///   Vectors from origin to radial hexes (world space).
		/// </summary>
		private Vector3[] edges = new Vector3[6];

		[SerializeField] private int originX;
		[SerializeField] private int originY;

		[SerializeField] private int radiusFrom;
		[SerializeField] private int radiusTo = 3;

		[SerializeField] private int hexFrom;
		[SerializeField] private int hexTo = 6;

		[SerializeField] private float layerFrom;
		[SerializeField] private float layerTo;
#endregion

#region  Properties
		/// <summary>
		///   X-coordinate of the origin hex.
		/// </summary>
		public int OriginX {
			get {
				return originX;
			} set {
				var previous = originX;
				originX = value;
				OnOriginChanged(previous, OriginY);
			}
		}

		/// <summary>
		///   Y-coordinate of the origin hex.
		/// </summary>
		public int OriginY {
			get {
				return originY;
			} set {
				var previous = originY;
				originY = value;
				OnOriginChanged(OriginX ,previous);
			}
		}

		/// <summary>
		///   Distance of the first hexagon from the origin hex.
		/// </summary>
		public int RadiusFrom {
			get {
				return radiusFrom;
			} set {
				var previous = radiusFrom;
				radiusFrom = Mathf.Max(value, 0);
				radiusFrom = Mathf.Min(radiusFrom, RadiusTo);
				OnRadiusChanged(previous, RadiusTo);
			}
		}

		/// <summary>
		///   Distance of the last hexagon from the origin hex.
		/// </summary>
		public int RadiusTo {
			get {
				return radiusTo;
			} set {
				var previous = radiusTo;
				radiusTo = Mathf.Max(value, RadiusFrom);
				OnRadiusChanged(RadiusFrom, previous);
			}
		}

		/// <summary>
		///   Which of the six edges the rendering should start at.
		/// </summary>
		public int HexFrom {
			get {
				return hexFrom;
			} set {
				var previous = hexFrom;
				hexFrom = value % 6;
				if (hexFrom < 0) {
					hexFrom += 7;
				}
				OnHexChanged(previous, HexTo);
			}
		}

		/// <summary>
		///   Which of the six edges the rendering should end at.
		/// </summary>
		public int HexTo {
			get {
				return hexTo;
			} set {
				var previous = hexTo;
				hexTo = value % 7;
				if (hexTo < 0) {
					hexTo += 7;
				}
				OnHexChanged(HexFrom, previous);
			}
		}

		/// <summary>
		///   First layer of the rendering.
		/// </summary>
		public float LayerFrom {
			get {
				return layerFrom;
			}
			set {
				var previous = layerFrom;
				layerFrom = Mathf.Min(LayerTo, value);
				OnLayerChanged(previous, LayerTo);
			}
		}

		/// <summary>
		///   Last layer of the rendering.
		/// </summary>
		public float LayerTo {
			get {
				return layerTo;
			}
			set {
				var previous = layerTo;
				layerTo = Mathf.Max(LayerFrom, value);
				OnLayerChanged(LayerFrom, previous);
			}
		}
#endregion

#region  Count
		protected override void CountLines() {
#region  Explanation comment
			/* Drawing and counting hexagonal cones
			 * ====================================
			 * Draw a cone-shape around a given hexagon, that hexagon is not
			 * necessarily the origin. The rendering range consist of a distance
			 * and angle. The distance is from the central hex, if it is 0 the
			 * central hex is included. The angle is between 0 and 6 and describes
			 * which cones to render.
			 *
			 * Counting the lines in a circle
			 * ------------------------------
			 * The central hex has six vertices and thus six edges, no exception. The
			 * radial hexes always have four vertices and four edges; the other two
			 * vertices and one edge are from the previous inner hex, one edge is
			 * from the neighbour. Hexes in between have three vertices and three
			 * edges.
			 *
			 *                          _
			 *               _          2\_
			 *     _       _ 1\         _/2\
			 *    /0\ --> /0\_/ -->   _/1\_/
			 *    \_/     \_/        /0\_/
			 *                       \_/
			 *
			 *         Distances:              Edges:
			 *             _                      _
			 *           _/4\_                  _/4\_
			 *         _/4\_/4\_              _/3\_/3\_
			 *       _/4\_/3\_/4\           _/3\_/4\_/3\_
			 *     _/4\_/3\_/3\_/4\_      _/3\_/3\_/3\_/3\_
			 *    /4\_/3\ /2\_/3\_/4\    /4\_/3\ /4\_/3\_/4\
			 *    \_/3\_/2\_/2\_/3\_/    \_/4\_/3\_/3\_/4\_/
			 *    /4\_/2\_/1\_/2\_/4\    /3\_/4\_/4\_/4\_/3\
			 *    \_/3\_/1\_/1\_/3\_/    \_/3\_/4\_/4\_/3\_/
			 *    /4\_/2\_/0\_/2\_/4\    /3\_/3\_/6\_/3\_/3\
			 *    \_/3\_/1\_/1\_/3\_/    \_/3\_/4\_/4\_/3\_/
			 *    /4\_/2\_/1\_/2\_/4\    /3\_/4\_/4\_/4\_/3\
			 *    \_/3\_/2\_/2\_/3\_/    \_/4\_/3\_/3\_/4\_/
			 *    /4\_/3\_/2\_/3\_/4\    /4\_/3\_/4\_/3\_/4\
			 *    \_/4\_/3\_/3\_/4\_/    \_/3\_/3\_/3\_/3\_/
			 *      \_/4\_/3\_/4\_/        \_/3\_/4\_/3\_/
			 *        \_/4\_/4\_/            \_/3\_/3\_/
			 *          \_/4\_/                \_/4\_/
			 *            \_/                    \_/
			 *
			 * Radial hexes are hexes directly on a line through the edges of the
			 * central hex.
			 *
			 *
			 * Drawing the cone
			 * ----------------
			 * 1) To draw a cone start out with the six edges around the central hex.
			 * 2) For ever layer do:
			 *    2.1) Do for every radial hex:
			 *         2.1.1) For the radial hex draw four edges: one adjacent to
			 *                the inner hex, two zig-zag sides, one top
			 *         2.1.2) If the current radial index is the last or 6, quit
			 *         2.1.3) For (layer - 1) inner hexes do:
			 *                2.1.3.1) Draw three edges: two sides, one top.
			 *         2.1.4) If the cone is not a closed circle the last radial hex
			 *                needs a closing edge
			 *
			 * Example: cone from 0 to 2
			 * -------------------------
			 * For starters let's look at a complete circle. Start out with the
			 * central hex:
			 *       _
			 *      /0\
			 *      \_/
			 *
			 * Add the four edges of the radial hex:
			 *                _          _          _          _          _
			 *         _     /1\_      _/1\_      _/1\_      _/1\_      _/1\_
			 *       _ 1\     _/1\    /1\_/1\    /1\_/1\    /1\_/1\    /1\_/1\
			 *      /0\_/ -> /0\_/ -> \ /0\_/ -> \_/0\_/ -> \_/0\_/ -> \_/0\_/
			 *      \_/      \_/        \_/      /1\_/      /1\_/      /1\_/1\
			 *                                   \_         \_/1       \_/1\_/
			 *                                                \_/        \_/
			 *
			 * Now start with the next layer, one radial hex and the inner hexes:
			 *                                 _                     _
			 *                      _        _/2\_                 _/2\_
			 *       _   _        _ 2\_     /2\_/2\_             _/2\_/2\_
			 *     _/1\_ 2\     _/1\_/2\     _/1\_/2\           /2\_/1\_/2\
			 *    /1\_/1\_/    /1\_/1\_/    /1\_/1\_/           \_/1\_/1\_/
			 *    \_/0\_/   -> \_/0\_/   -> \_/0\_/   -> ... -> /2\_/0\_/2\
			 *    /1\_/1\      /1\_/1\      /1\_/1\             \_/1\_/1\_/
			 *    \_/1\_/      \_/1\_/      \_/1\_/             /2\_/1\_/2\
			 *      \_/          \_/          \_/               \_/2\_/2\_/
			 *                                                    \_/2\_/
			 *                                                      \_/
			 *
			 * Note that the last radial hex is the same as the first. This is
			 * different from cones where there is an explicit radial hex. Let's
			 * look at the cone now:
			 *
			 *                                      _              _
			 *                                    _/2\_          _/2\_
			 *              _          _        _/2\_/2\_      _/2\_/2\_
			 *            _/1\_      _/1\_     /2\_/1\_/2\    /2\_/1\_/2\
			 *     _     /1\_/1\    /1\_/1\    \ /1\_/1\_/    \_/1\_/1\_/
			 *    /0\ -> \ /0\_/ -> \_/0\_/ ->   \_/0\_/   ->   \_/0\_/
			 *    \_/      \_/        \_/          \_/            \_/
			 *
			 * We start at the radial hex of the first radial index and end at the
			 * radial hex of the last radial index. We only draw the inner hexes
			 * with radial index *between* these two indices. Finally, we draw the
			 * closing line.
			 *
			 * If the last radial index is 6 (or more precisely ``first index +
			 * last index = 0 mod 6``) we don't draw that radial hex, since that
			 * would be a closed circle.
			 *
			 * Drawing not from the centre
			 * ---------------------------
			 * If the first radius index is not 0 we skip over the inner-most hex
			 * and any hexes before that. However, that does leave some undrawn
			 * edges:
			 *                                 _                      _
			 *                               _/4\_                  _/4\_
			 *                             _/4\ /4\_              _/4\_/4\_
			 *             3             _/4\  3  /4\_          _/4\_/3\_/4\_
			 *           3   3         _/4\  3   3  /4\_      _/4\_/3   3\_/4\_
			 *         3   2   3      /4\  3   2   3  /4\    /4\_/3   2   3\_/4\
			 *       3   2   2   3    \_ 3   2   2   3 _/    \_/3   2   2   3\_/
			 *         2   1   2      /4   2   1   2   4\    /4\  2   1   2  /4\
			 *       3   1   1   3    \_ 3   1   1   3 _/    \_/3   1   1   3\_/
			 *         2   0   2   -> /4   2   0   2   4\ -> /4\  2   0   2  /4\
			 *       3   1   1   3    \_ 3   1   1   3 _/    \_/3   1   1   3\_/
			 *         2   1   2      /4   2   1   2   4\    /4\  2   1   2  /4\
			 *       3   2   2   3    \_ 3   2   2   3 _/    \_/3   2   2   3\_/
			 *         3   2   3      /4   3   2   3   4\    /4\_ 3   2   3 _/4\
			 *           3   3        \_/4   3   3   4\_/    \_/4\_ 3   3 _/4\_/
			 *             3            \_/4   3   4\_/        \_/4\_ 3 _/4\_/
			 *                            \_/4   4\_/            \_/4\_/4\_/
			 *                              \_/4\_/                \_/4\_/
			 *                                \_/                    \_/
			 *
			 * As we can see the radial hexes are missing one line, the line shared
			 * with the previous circle, and the inner hexes are missing two lines.
			 *
			 * Layer lines
			 * -----------
			 * Layer lines are needed where the vertices are. To cover all vertices
			 * start out with the six inner vertices. Then, for every other hex add
			 * a vertex at the end point of every line, except for the second side
			 * line.
			 *
			 * If the cone is a ring we also add the end point of every closing
			 * line.
			 *
			 * For a non-closed cone we must add the start and end point of the
			 * closing line of the last hex.
			 */
#endregion  // Explanation comment

			int layers = Mathf.FloorToInt(LayerTo) - Mathf.CeilToInt(LayerFrom) + 1;
			int rMin = RadiusFrom, rMax = RadiusTo;
			int aMin = HexFrom, aMax = HexTo;

			if (aMin > aMax) {
				aMax += 6;
			}

			_lineCount[0] = 0;
			_lineCount[1] = 0;
			_lineCount[2] = 0;

			// If the central hex is drawn add its lines, otherwise add the closing lines
			if (RadiusFrom == 0) {
				_lineCount[0] += 2; _lineCount[1] += 4; _lineCount[2] += 6;
				// if the radius is from 0 to 0 we are done here
				if (radiusTo == 0) {
					goto end;
				}
				// otherwise we move on to the first actual ring
				++rMin;
			} else {
				// for every radial hex add one line, for inner hexes add two lines
				for (int i = aMin; i < aMax; ++i){
					// the closing line is opposite of the first side line
					IncrementCountXY(i + 1, rMin    );
					IncrementCountXY(i + 2, rMin - 1);
					_lineCount[2] += 2 * rMin - 1; // 2 * for inner, + 1 radial
				}
				// add the closing line for the last radial hex
				if ((aMax - aMin) % 6 != 0 || aMax == aMin) {
					IncrementCountXY(aMax + 1, 1);
					_lineCount[2] += 2;
				}
			}

			// Now start the actual loop...

			for (int i = rMin; i <= rMax; ++i) {
				for (int j = aMin; j < aMax; ++j) {
					// connection, botton and two sides
					// the connection is opposite of the 2nd side
					for (int k = 0; k < i; ++k) {
						if (k == 0) {
							IncrementCountXY(j + 2, 1);
							++_lineCount[2];
						}
						// now the inner hexes
						IncrementCountXY(j + 0, 1);
						IncrementCountXY(j + 1, 1);
						IncrementCountXY(j + 2, 1);

						_lineCount[2] += 2;
					}
				}
				// The last radial hex, unless the cone is a circle
				if ((aMax - aMin) % 6 != 0 || aMax == aMin) {
					// close hex, line opposite of bottom
					if ((aMax - aMin) % 6 != 5 || i > 1) {
						IncrementCountXY(aMax+0, 1);
					}

					IncrementCountXY(aMax+0, 1);
					IncrementCountXY(aMax+1, 1);
					IncrementCountXY(aMax+2, 2);

					_lineCount[2] += 4;
				}
			}

	end:
			// finally multiply all this by the number of layers.
			_lineCount[0] *= layers;
			_lineCount[1] *= layers;

			var flat = Grid.Sides == GridFramework.Grids.HexGrid.Orientation.Flat;
			Swap<int>(ref _lineCount[0], ref _lineCount[1], flat);
		}
#endregion  // Count

#region  Compute
		protected override void ComputeLines() {
			int iterator_x = 0, iterator_y = 0, iterator_z = 0;

			int[] radius = {RadiusFrom, RadiusTo}; // inner and outer radius
			int[] angle  = {HexFrom   , HexTo   }; // start and end hex

			if (angle[0] > angle[1]) {
				angle[1] += 6;
			}

			// Current hex.
			Vector3 currentHex;

			// Vectors from one layer to the next (world space).
			Vector3 forward = Grid.Forward;

			Vector3 origin = Grid.HerringUpToWorld(new Vector3(OriginX, OriginY, 0));

			var front = Mathf.FloorToInt(LayerTo);
			var back  = Mathf.CeilToInt(LayerFrom);

			MakeVertices();
			MakeEdges();

			currentHex = origin + back * forward;

			if (radius[0] == 0) {
				for (var i = 0; i < 6; ++i) {
					ContributeLayer(origin, 0, i, forward, ref iterator_z);
				}
				for (var i = back; i <= front; ++i) {
					ContributeCenter(currentHex, ref iterator_x, ref iterator_y);
					currentHex += forward;
				}

				// if the radius is from 0 to 0 we are done here
				if (radius[1] == 0) {
					return;
				}
				// otherwise we move on to the first actual ring
				++radius[0];
			} else {
				// for every radial hex add one line, for inner hexes add two lines
				for (var i = angle[0]; i < angle[1]; ++i) {
					currentHex = origin + radius[0] * edges[i%6];
					// one inner line for the radial hex, opposite of side 1
					currentHex += back * forward;
					for (var j = 0; j < radius[0]; ++j) {
						// two inner lines for the inner hex
						for (var k = back; k <= front; ++k) {
							// every hex gets an inner line
							// every inner hex a connecting line
							if (j != 0) {
								ContributeConnecting(currentHex, i%6, ref iterator_x, ref iterator_y);
							}
							ContributeInner(currentHex, i%6, ref iterator_x, ref iterator_y);
							currentHex += forward;
						}
						currentHex -= (front+1) * forward; // back to the origin layer
						if (j != 0) {
							ContributeLayer(currentHex, i%6, 5, forward, ref iterator_z);
						}
						ContributeLayer(currentHex, i%6, 4, forward, ref iterator_z);
						currentHex += back * forward; // back forward
						currentHex += edges[(i+2)%6];
					}
				}
				// add the last radial hex
				if ((angle[1] - angle[0]) % 6 != 0 || angle[1] == angle[0]) {
					currentHex = origin + radius[0] * edges[angle[1]%6];
					currentHex += back * forward;
					for (var j = back; j <= front; ++j) {
						ContributeInner(currentHex, angle[1]%6, ref iterator_x, ref iterator_y);
						currentHex += forward;
					}
					currentHex -= (front+1) * forward;
					ContributeLayer(currentHex, angle[1]%6, 3, forward, ref iterator_z);
					ContributeLayer(currentHex, angle[1]%6, 4, forward, ref iterator_z);
				}
			}

			// Now start the actual loop...

			for (var i = radius[0]; i <= radius[1]; ++i) {
				for (var j = angle[0]; j < angle[1]; ++j) {
					// connection, bottom and two sides
					currentHex = origin + i * edges[j%6];
					// the connection is opposite of the 2nd side now the inner
					// hexes
					currentHex += back * forward;
					for (var k = 0; k < i; ++k) {
						for (var l = back; l <= front; ++l) {
							if (k == 0) {
								ContributeConnecting(currentHex, j%6, ref iterator_x, ref iterator_y);
							}
							ContributeBottom(currentHex, j%6, ref iterator_x, ref iterator_y);
							ContributeSides(currentHex, j%6, ref iterator_x, ref iterator_y);
							currentHex += forward;
						}
						// now the inner hexes
						currentHex -= (front+1) * forward; // back to origin layer
						if (k == 0) {
							ContributeLayer(currentHex, j%6, 5, forward, ref iterator_z);
						}
						ContributeLayer(currentHex, j%6, 0, forward, ref iterator_z);
						ContributeLayer(currentHex, j%6, 1, forward, ref iterator_z);
						currentHex += back * forward; // back forward
						currentHex += edges[(j+2)%6];
					}
				}
				// The last radial hex, unless the cone is a circle
				if ((angle[1] - angle[0]) % 6 != 0 || angle[1] == angle[0]) {
					currentHex = origin + i * edges[angle[1]%6];
					currentHex += back * forward;
					for (var j = back; j <= front; ++j) {
						ContributeConnecting(currentHex, angle[1]%6, ref iterator_x, ref iterator_y);
						ContributeBottom(currentHex, angle[1]%6, ref iterator_x, ref iterator_y);
						ContributeSides(currentHex, angle[1]%6, ref iterator_x, ref iterator_y);
						// If we don't do this a line will be drawn twice
						if ((angle[1] - angle[0]) % 6 != 5 || i > 1) {
							ContributeClosing(currentHex, angle[1]%6, ref iterator_x, ref iterator_y);
						}
						currentHex += forward;
					}
						currentHex -= (front+1) * forward; // back to origin layer
						ContributeLayer(currentHex, angle[1]%6, 0, forward, ref iterator_z);
						ContributeLayer(currentHex, angle[1]%6, 1, forward, ref iterator_z);
						ContributeLayer(currentHex, angle[1]%6, 2, forward, ref iterator_z);
						ContributeLayer(currentHex, angle[1]%6, 5, forward, ref iterator_z);
				}
			}
		}

#endregion  // Compute

#region  Contribute
		//                        Side 2
		//                     .----------\                        _
		//                    .            \                 _    /1\    _
		//           Closing .              \ Side 1       _ 0\    _/   /2\_
		//                  .      000       \            /O\_/   /O\   \ /O\
		//                 .      0  00       \           \_/     \_/     \_/
		//      /----------.      0 0 0       /            _       _       _
		//     /            .     00  0      /           _/O\     /O\     /O\
		//    /        Inner .     000      / Bottom    /3\_/     \_/     \_/5\
		//   /      OOO       .            /            \_        /4        \_/
		//  /      O   O       .----------/                       \_/
		//  \      O   O       /Connecting
		//   \     O   O      /
		//    \     OOO      /
		//     \            /
		//      \----------/

		private int ContributeEdge(int index, Vector3 hex, int i, int iterator) {
			_lineSets[index][iterator][0] = hex + vertices[(i + 0) % 6];
			_lineSets[index][iterator][1] = hex + vertices[(i + 1) % 6];
			return ++iterator;
		}

		private void ContributeCenter(Vector3 hex, ref int iterator_x, ref int iterator_y) {
			var pointed = Grid.Sides == GridFramework.Grids.HexGrid.Orientation.Pointed;
			if (pointed) {
				iterator_y = ContributeEdge(1, hex, 0, iterator_y);
				iterator_x = ContributeEdge(0, hex, 1, iterator_x);
				iterator_y = ContributeEdge(1, hex, 2, iterator_y);
				iterator_y = ContributeEdge(1, hex, 3, iterator_y);
				iterator_x = ContributeEdge(0, hex, 4, iterator_x);
				iterator_y = ContributeEdge(1, hex, 5, iterator_y);
			} else {
				iterator_y = ContributeEdge(1, hex, 0, iterator_y);
				iterator_x = ContributeEdge(0, hex, 1, iterator_x);
				iterator_x = ContributeEdge(0, hex, 2, iterator_x);
				iterator_y = ContributeEdge(1, hex, 3, iterator_y);
				iterator_x = ContributeEdge(0, hex, 4, iterator_x);
				iterator_x = ContributeEdge(0, hex, 5, iterator_x);
			}
		}
	
		private void ContributeConnecting(Vector3 hex, int a, ref int iterator_x, ref int iterator_y) {
			int ix;
			var pointed = Grid.Sides == GridFramework.Grids.HexGrid.Orientation.Pointed;
			if (pointed) {
				ix = (a == 0 || a == 3) ? 0 : 1;
			} else {
				ix = (a == 2 || a == 5) ? 1 : 0;
			}
			if (ix == 0) {
				iterator_x = ContributeEdge(ix, hex, a + 4, iterator_x);
			} else {
				iterator_y = ContributeEdge(ix, hex, a + 4, iterator_y);
			}
		}

		private void ContributeBottom(Vector3 hex, int a, ref int iterator_x, ref int iterator_y) {
			int ix;
			var pointed = Grid.Sides == GridFramework.Grids.HexGrid.Orientation.Pointed;
			if (pointed) {
				ix = (a == 2 || a == 5) ? 0 : 1;
			} else {
				ix = (a == 1 || a == 4) ? 1 : 0;
			}
			if (ix == 0) {
				iterator_x = ContributeEdge(ix, hex, a + 5, iterator_x);
			} else {
				iterator_y = ContributeEdge(ix, hex, a + 5, iterator_y);
			}
		}

		private void ContributeSides(Vector3 hex, int a, ref int iterator_x, ref int iterator_y) {
			int ix1, ix2;
			var pointed = Grid.Sides == GridFramework.Grids.HexGrid.Orientation.Pointed;
			if (pointed) {
				ix1 = (a == 1 || a == 4) ? 0 : 1;
				ix2 = (a == 0 || a == 3) ? 0 : 1;
			} else {
				ix1 = (a == 0 || a == 3) ? 1 : 0;
				ix2 = (a == 2 || a == 5) ? 1 : 0;
			}
			if (ix1 == 0) {
				iterator_x = ContributeEdge(ix1, hex, a + 0, iterator_x);
			} else {
				iterator_y = ContributeEdge(ix1, hex, a + 0, iterator_y);
			}
			if (ix2 == 0) {
				iterator_x = ContributeEdge(ix2, hex, a + 1, iterator_x);
			} else {
				iterator_y = ContributeEdge(ix2, hex, a + 1, iterator_y);
			}
		}

		private void ContributeClosing(Vector3 hex, int a, ref int iterator_x, ref int iterator_y) {
			int ix;
			var pointed = Grid.Sides == GridFramework.Grids.HexGrid.Orientation.Pointed;
			if (pointed) {
				ix = (a == 2 || a == 5) ? 0 : 1;
			} else {
				ix = (a == 1 || a == 4) ? 1 : 0;
			}
			if (ix == 0) {
				iterator_x = ContributeEdge(ix, hex, a + 2, iterator_x);
			} else {
				iterator_y = ContributeEdge(ix, hex, a + 2, iterator_y);
			}
		}

		private void ContributeInner(Vector3 hex, int a, ref int iterator_x, ref int iterator_y) {
			int ix;
			var pointed = Grid.Sides == GridFramework.Grids.HexGrid.Orientation.Pointed;
			if (pointed) {
				ix = (a == 1 || a == 4) ? 0 : 1;
			} else {
				ix = (a == 0 || a == 3) ? 1 : 0;
			}
			if (ix == 0) {
				iterator_x = ContributeEdge(ix, hex, a + 3, iterator_x);
			} else {
				iterator_y = ContributeEdge(ix, hex, a + 3, iterator_y);
			}
		}

		private void ContributeLayer(Vector3 hex, int a, int indx, Vector3 forward, ref int iterator_z) {
			_lineSets[2][iterator_z][0] = hex + vertices[(indx+a)%6] + LayerFrom * forward;
			_lineSets[2][iterator_z][1] = hex + vertices[(indx+a)%6] + LayerTo   * forward;
			++iterator_z;
		}
#endregion  // Contribute

#region  Helpers
		private void IncrementCountXY(int i, int value) {
			i %= 6;
			switch (i) {
				case 0:
				case 1:
				case 3:
				case 4:
					_lineCount[1] += value;
					break;
				case 2:
				case 5:
					_lineCount[0] += value;
					break;
				default: throw new ArgumentOutOfRangeException();
			}
		}

		///<summary>Maps cardinal direction to direction hex to vertex.</summary>
		Vector3 CardinalToHex(int d) {
			Vector4 result;
			switch (d) {
				case 0: result = new Vector4( 1,  0, -1); break;
				case 1: result = new Vector4( 0,  1, -1); break;
				case 2: result = new Vector4(-1,  1,  0); break;
				case 3: result = new Vector4(-1,  0,  1); break;
				case 4: result = new Vector4( 0, -1,  1); break;
				case 5: result = new Vector4( 1, -1,  0); break;
				default: throw new ArgumentOutOfRangeException();
			}
			var hex = Grid.CubicToWorld(result) - Grid.CubicToWorld(Vector4.zero);
			return hex;
		}

		///<summary>Maps cardinal direction to direction origin hex to hex.</summary>
		Vector3 CardinalToVertex(int d) {
			Vector4 result;
			switch (d) {
				case 0 : result = new Vector4( 2f/3f, -1f/3f, -1f/3f); break;
				case 1 : result = new Vector4( 1f/3f,  1f/3f, -2f/3f); break;
				case 2 : result = new Vector4(-1f/3f,  2f/3f, -1f/3f); break;
				case 3 : result = new Vector4(-2f/3f,  1f/3f,  1f/3f); break;
				case 4 : result = new Vector4(-1f/3f, -1f/3f,  2f/3f); break;
				case 5 : result = new Vector4( 1f/3f, -2f/3f,  1f/3f); break;
				default: throw new ArgumentOutOfRangeException();
			}
			return Grid.CubicToWorld(result) - Grid.CubicToWorld(Vector4.zero);
		}

		void MakeVertices() {                  //        2_____1
			vertices[0] = CardinalToVertex(0); // [-]    /     \
			vertices[1] = CardinalToVertex(1); // [/]   /       \
			vertices[2] = CardinalToVertex(2); // [\]  3    #    0
			vertices[3] = CardinalToVertex(3); // [-]   \       /
			vertices[4] = CardinalToVertex(4); // [/]    \_____/
			vertices[5] = CardinalToVertex(5); // [\]    4     5
		}

		void MakeEdges() {
			edges[0] = CardinalToHex(0); // [/]     _____
			edges[1] = CardinalToHex(1); // [|]    /  1  \
			edges[2] = CardinalToHex(2); // [\]   2       0
			edges[3] = CardinalToHex(3); // [/]  (    #    )
			edges[4] = CardinalToHex(4); // [|]   3       5
			edges[5] = CardinalToHex(5); // [\]    \__4__/
		}
#endregion  // Helpers

#region  Hook methods
		private void OnOriginChanged(int previousX, int previousY) {
			if (previousX == OriginX && previousY == OriginY) {
				return;
			}
			UpdatePoints();
		}

		private void OnRadiusChanged(int previousFrom, int previousTo) {
			if (previousFrom == RadiusFrom && previousTo == RadiusTo) {
				return;
			}
			UpdatePoints();
		}

		private void OnHexChanged(int previousFrom, int previousTo) {
			if (previousFrom == HexFrom && previousTo == HexTo) {
				return;
			}
			UpdatePoints();
		}

		private void OnLayerChanged(float previousFrom, float previousTo) {
			var deltaFrom = Mathf.Abs(previousFrom - LayerFrom);
			var deltaTo   = Mathf.Abs(previousTo   - LayerTo  );
			if (deltaFrom < Mathf.Epsilon && deltaTo < Mathf.Epsilon) {
				return;
			}
			UpdatePoints();
		}
#endregion  // Hook methods
	}
}
