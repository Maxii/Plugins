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
		///
		/// <remarks>
		///   <para>
		///     Returns the coordinates of a face on the grid closest to a
		///     given point. Since the face is enclosed by four vertices, the
		///     returned value is the point in between all four of the
		///     vertices. You also need to specify on which plane the face
		///     lies.
		///   </para>
		/// </remarks>
		public static Vector3 NearestFace(this HexGrid grid,
			                                    Vector3 point,
			                           CoordinateSystem system) {
			// Faces in the hex-grid are dual to vertices in the triangular
			// tessellation of the grid. Convert the point to cubic coordinates
			var cubicPoint = grid.WorldToCubic(point);
			cubicPoint.x = Mathf.Round(cubicPoint.x);
			cubicPoint.y = Mathf.Round(cubicPoint.y);
			cubicPoint.z = Mathf.Round(cubicPoint.z);
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

