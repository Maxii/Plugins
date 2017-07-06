using UnityEngine;
using HexGrid = GridFramework.Grids.HexGrid;
using CoordinateSystem = GridFramework.Grids.HexGrid.CoordinateSystem;

namespace GridFramework.Extensions.Nearest {
	/// <summary>
	///   Extension methods for finding the nearest vertex, face or cell in a
	///   hexagonal grid.
	/// </summary>
	public static class Hexgonal {
#region  Nearest vertex
		/// <summary>
		///   Returns the position of the nearest vertex.
		/// </summary>
		/// <returns>
		///   Position of the nearest vertex.
		/// </returns>
		///
		/// <param name="grid">
		///   The rectangular grid instance.
		/// </param>
		/// <param name="point">
		///   Point in world space.
		/// </param>
		/// <param name="system">
		///   Coordinate system to use.
		/// </param>
		/// 
		/// <remarks>
		///   Returns the position of the nearest vertex from a given point in
		///   either grid- or world space.
		/// </remarks>
		public static Vector3 NearestVertex(this HexGrid grid, Vector3 point, CoordinateSystem system) {
			// Vertices in the hex-grid are dual to faces in the triangular
			// tessellation of the grid. Convert the point to cubic coordinates
			var cubicPoint = grid.WorldToCubic(point);
			cubicPoint.x = Mathf.Floor(cubicPoint.x) + .5f;
			cubicPoint.y = Mathf.Floor(cubicPoint.y) + .5f;
			cubicPoint.z = Mathf.Floor(cubicPoint.z) + .5f;
			cubicPoint.w = Mathf.Round(cubicPoint.w);

			switch (system) {
				case CoordinateSystem.Cubic:
					return cubicPoint;
				case CoordinateSystem.HerringboneUp:
					return grid.CubicToHerringU(cubicPoint);
				case CoordinateSystem.HerringboneDown:
					return grid.CubicToHerringD(cubicPoint);
				case CoordinateSystem.RhombicUp:
					return grid.CubicToRhombic(cubicPoint);
				case CoordinateSystem.RhombicDown:
					return grid.CubicToRhombicD(cubicPoint);
				case CoordinateSystem.World:
					return grid.CubicToWorld(cubicPoint);
			}
			throw new System.ComponentModel.InvalidEnumArgumentException();
		}
#endregion  // Nearest vertex

#region  Nearest face
		/// <summary>
		///   Returns the position of the nearest face.
		/// </summary>
		/// <returns>
		///   Position of the nearest face.
		/// </returns>
		/// <param name="grid">
		///   The rectangular grid instance.
		/// </param>
		/// <param name="point">
		///   Point in world space.
		/// </param>
		/// <param name="system">
		///   Coordinate system to use.
		/// </param>
		/// <remarks>
		///   <para>
		///     Returns the coordinates of a face on the grid closest to a
		///     given point. Since the face is enclosed by four vertices, the
		///     returned value is the point in between all four of the
		///     vertices. You also need to specify on which plane the face
		///     lies.
		///   </para>
		/// </remarks>
		public static Vector3 NearestFace(
			this HexGrid grid, Vector3 point, CoordinateSystem system
		) {
			// Faces in the hex-grid are dual to vertices in the triangular
			// tessellation of the grid. Convert the point to cubic coordinates
			var cubic = grid.WorldToCubic(point);
			// We need the original cubic coordinates and the rounded ones, so
			// use a separate variable
			var rounded = new Vector4(
				Mathf.Round(cubic.x),
				Mathf.Round(cubic.y),
				Mathf.Round(cubic.z),
				Mathf.Round(cubic.w)
			);

			// Rounding all three coordinates does not guarantee that their sum
			// is zero. Therefore we will find the coordinate with the largest
			// change and compute its value from the other two instead of its
			// rounded value.
			var deltaX = Mathf.Abs(rounded.x - cubic.x);
			var deltaY = Mathf.Abs(rounded.y - cubic.y);
			var deltaZ = Mathf.Abs(rounded.z - cubic.z);

			if (deltaX > deltaY && deltaX > deltaZ) {
				rounded.x = -rounded.y - rounded.z;
			} else if (deltaY > deltaZ) {
				rounded.y = -rounded.x - rounded.z;
			} else {
				rounded.z = -rounded.x - rounded.y;
			}

			switch (system) {
				case CoordinateSystem.Cubic:
					return rounded;
				case CoordinateSystem.HerringboneUp:
					return grid.CubicToHerringU(rounded);
				case CoordinateSystem.HerringboneDown:
					return grid.CubicToHerringD(rounded);
				case CoordinateSystem.RhombicUp:
					return grid.CubicToRhombic(rounded);
				case CoordinateSystem.RhombicDown:
					return grid.CubicToRhombicD(rounded);
				case CoordinateSystem.World:
					return grid.CubicToWorld(rounded);
			}
			throw new System.ComponentModel.InvalidEnumArgumentException();
		}
#endregion  // Nearest face

#region  Nearest cell
		/// <summary>
		///   Returns the grid position of the nearest box.
		/// </summary>
		/// <returns>
		///   Grid position of the nearest box.
		/// </returns>
		/// <param name="grid">
		///   The rectangular grid instance.
		/// </param>
		/// <param name="point">
		///   Point in world space.
		/// </param>
		/// <param name="system">
		///   Coordinate system to use.
		/// </param>
		/// <remarks>
		///   <para>
		///     returns the coordinates of a cell in the grid. Since cell lies
		///     between vertices all three values will always have +0.5
		///     compared to vertex coordinates.
		///   </para>
		/// </remarks>
		/// <example>
		///   <code>
		///     GFRectGrid myGrid;
		///     Vector3 worldPoint;
		///     // something like (2.5, -1.5, 3.5)
		///     Vector3 box = myGrid.NearestBoxG(worldPoint);
		///   </code>
		/// </example>
		public static Vector3 NearestCell(this HexGrid grid,
			                                    Vector3 point,
			                           CoordinateSystem system) {
			// Faces in the hex-grid are dual to vertices in the triangular
			// tessellation of the grid. Convert the point to cubic coordinates
			var cubicPoint = grid.WorldToCubic(point);
			cubicPoint.x = Mathf.Round(cubicPoint.x);
			cubicPoint.y = Mathf.Round(cubicPoint.y);
			cubicPoint.z = Mathf.Round(cubicPoint.z);
			cubicPoint.w = Mathf.Floor(cubicPoint.w) + .5f;

			switch (system) {
				case CoordinateSystem.Cubic:
					return cubicPoint;
				case CoordinateSystem.HerringboneUp:
					return grid.CubicToHerringU(cubicPoint);
				case CoordinateSystem.HerringboneDown:
					return grid.CubicToHerringD(cubicPoint);
				case CoordinateSystem.RhombicUp:
					return grid.CubicToRhombic(cubicPoint);
				case CoordinateSystem.RhombicDown:
					return grid.CubicToRhombicD(cubicPoint);
				case CoordinateSystem.World:
					return grid.CubicToWorld(cubicPoint);
			}
			throw new System.ComponentModel.InvalidEnumArgumentException();
		}
#endregion  // Nearest cell
	}
}

