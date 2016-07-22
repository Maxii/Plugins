using UnityEngine;
using System.Collections.Generic;
using GridRenderer = GridFramework.Renderers.GridRenderer;

namespace GridFramework.Extensions.Vectrosity {
	/// <summary>
	///   Extension methods for getting Vectrosity points.
	/// </summary>
	public static class Points {
		/// <summary>
		///   Get a list of renderer points ready for use with Vectrosity.
		/// </summary>
		/// <returns>
		///   List of all points for all three axes.
		/// </returns>
		/// <param name="renderer">
		///   The instance to extend.
		/// </param>
		/// <remarks>
		///   <para>
		///     The axes are sorted from <c>x</c> to <c>z</c>.
		///   </para>
		/// </remarks>
		public static List<Vector3> GetVectrosityPoints(this GridRenderer renderer) {
			var lineSets = renderer.LineSets;

			// Make lines into points
			var points = new List<Vector3>();
			foreach (var lineSet in lineSets) {
				foreach (var line in lineSet) {
					points.Add(line[0]);
					points.Add(line[1]);
				}
			}
			return points;
		}

		/// <summary>
		///   Get a list of renderer points ready for use with Vectrosity
		///   separated by axis.
		/// </summary>
		/// <returns>
		///   List of all points for all three axes, separated by axis.
		/// </returns>
		/// <param name="renderer">
		///   The instance to extend.
		/// </param>
		/// <remarks>
		///   <para>
		///     The axes are sorted from <c>x</c> to <c>z</c>.
		///   </para>
		/// </remarks>
		public static List<List<Vector3>> GetVectrosityPointsSeparate(this GridRenderer renderer) {
			var lineSets = renderer.LineSets;

			// Make lines into points
			var points = new List<List<Vector3>>();
			foreach (var lineSet in lineSets) {
				var subPoints = new List<Vector3>();
				points.Add(subPoints);
				foreach (var line in lineSet) {
					subPoints.Add(line[0]);
					subPoints.Add(line[1]);
				}
			}
			return points;
		}

		/// <summary>
		///   Get a list of renderer points ready for use with Vectrosity
		///   from one axis.
		/// </summary>
		/// <returns>
		///   List of all points for one of the three axes.
		/// </returns>
		/// <param name="renderer">
		///   The instance to extend.
		/// </param>
		/// <param name="index">
		///   Index of the axis: <c>x=0</c>, <c>y=1</c>, <c>z=2</c>.
		/// </param>
		/// <remarks>
		///   <para>
		///     If the index is less than <c>0</c> or greater than <c>2</c> an
		///     error will be thrown.
		///   </para>
		/// </remarks>
		public static List<Vector3> GetVectrosityPointsSeparate(this GridRenderer renderer, int index) {
			// Pre-condition: index must be 0 <= i < 3
			if (index < 0 || index > 2) {
				var message = "The index "+index+" must be between zero (inclusive) and three (exclusive).";
				throw new System.IndexOutOfRangeException(message);
			}

			var lineSet = renderer.LineSets[index];

			// Make lines into points
			var points = new List<Vector3>();
			foreach (var line in lineSet) {
				points.Add(line[0]);
				points.Add(line[1]);
			}
			return points;
		}
	}
}
