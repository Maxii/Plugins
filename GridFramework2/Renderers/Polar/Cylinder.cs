using UnityEngine;

namespace GridFramework.Renderers.Polar {
	/// <summary>
	///   Cylinder shape for polar grids, can be rendered partially.
	/// </summary>
	/// <remarks>
	///   <para>
	///     The cylinder has an inner and outer radius, a top and bottom height
	///     and a starting and ending angle. The latter makes it possible to
	///     render only a sector of the cylinder instead of the whole cylinder.
	///   </para>
	/// </remarks>
	[AddComponentMenu("Grid Framework/Renderers/Polar/Cylinder")]
	public sealed class Cylinder : PolarRenderer {

#region  Private variables
		[SerializeField] private Vector3 _from;
		[SerializeField] private Vector3 _to;

		[SerializeField] private int _smoothness = 5;

		[SerializeField] private bool _hasBeenInitialized;

		private int arcCount;      // number of red arcs (circles)
		private int segmentCount;  // number of red segments (+2 for over- and underflow)
		private int sectorCount;   // number of green radial lines
		private int layerCount;    // number of layers

		private Vector3 _right;
		private Vector3 _forward;
#endregion  // Private variables

#region  Properties
		/// <summary>
		///   Distance of the start of radial lines from the origin.
		/// </summary>
		public float RadialFrom {
			get {
				return _from[0];
			}
			set {
				var previous = _from[0];
				_from[0] = Mathf.Min(_to[0], value);
				_from[0] = Mathf.Max(_from[0], 0);
				OnRadialFromChanged(previous, RadialTo);
			}
		}

		/// <summary>
		///   Distance of the end of radial lines from the origin.
		/// </summary>
		public float RadialTo {
			get {
				return _to[0];
			}
			set {
				var previous = _to[0];
				_to[0] = Mathf.Max(_from[0], value);
				OnRadialFromChanged(RadialFrom, previous);
			}
		}

		/// <summary>
		///   Which of angle the rendering should start at.
		/// </summary>
		public float SectorFrom {
			get {
				return _from[1];
			}
			set {
				_from[1] = Float2Sector(value, Grid.Sectors);
				var previous = _from[1];
				OnSectorChanged(previous, SectorTo);
			}
		}

		/// <summary>
		///   Which of angle the rendering should end at.
		/// </summary>
		public float SectorTo {
			get {
				return _to[1];
			}
			set {
				var previous = _to[1];
				_to[1] = Mathf.Max(_from[1], value);
				_to[1] = Float2Sector(_to[1], Grid.Sectors);
				OnSectorChanged(SectorFrom, previous);
			}
		}

		/// <summary>
		///   First layer of the rendering.
		/// </summary>
		public float LayerFrom {
			get {
				return _from[2];
			}
			set {
				var previous = _from[2];
				_from[2] = Mathf.Min(_to[2], value);
				OnLayerChanged(previous, LayerTo);
			}
		}

		/// <summary>
		///   Last layer of the rendering.
		/// </summary>
		public float LayerTo {
			get {
				return _to[2];
			}
			set {
				var previous = _to[2];
				_to[2] = Mathf.Max(_from[2], value);
				OnLayerChanged(LayerFrom, previous);
			}
		}

		/// <summary>
		///   Subdivides the sectors to create a smoother look.
		/// </summary>
		/// <value>
		///   Smoothness of the grid segments.
		/// </value>
		/// <remarks>
		///   <para>
		///     Unity's GL class can only draw straight lines, so in order to
		///     get the sectors to look round this value breaks each sector
		///     into smaller segments. The number of smoothness tells how many
		///     segments the circular line has been broken into. The amount of
		///     end points used is smoothness + 1, because we count both edges
		///     of the sector.
		///   </para>
		/// </remarks>
		public int Smoothness {
			get {
				return _smoothness;
			} set {
				var previous = _smoothness;
				_smoothness = Mathf.Max(value, 1);
				OnSmoothnessChanged(previous);
			}
		}
#endregion

#region  Count
		protected override void CountLines() {

			// If the from angle is greater than the to angle wrap the to angle
			// around once.
			var wrapAround = SectorFrom > SectorTo;
			if (wrapAround) {
				_to.y += Grid.Sectors;
			}

			// Adjusted for constant amount addition (e.g. we always have at
			// least one layer)
			segmentCount = Mathf.FloorToInt(SectorTo * Smoothness) - Mathf.CeilToInt(SectorFrom * Smoothness) + 2;

			arcCount    = Mathf.FloorToInt(RadialTo) - Mathf.CeilToInt(RadialFrom) + 1;
			sectorCount = Mathf.FloorToInt(SectorTo) - Mathf.CeilToInt(SectorFrom) + 1;
			layerCount  = Mathf.FloorToInt( LayerTo) - Mathf.CeilToInt( LayerFrom) + 1;

			if (Mathf.Abs(SectorTo - Grid.Sectors) <= Mathf.Epsilon) {
				--sectorCount;
			}

			_lineCount[0] = segmentCount * arcCount * layerCount; // total number of segments
			_lineCount[1] = sectorCount * layerCount; // total number of radial lines
			_lineCount[2] = arcCount * sectorCount; // total number of cylindrical lines

			if (wrapAround) {
				_to.y -= Grid.Sectors;
			}
		}
#endregion  // Count

#region  Compute
		protected override void ComputeLines() {
			var origin = Grid.GridToWorld(Vector3.zero);

			// How to fill the arc segment line array: Each segment consists of
			// two points, the first point of a segment is the second point of
			// the previous segment and the second point of a segment is the
			// first point of the next segment. The very first point will be
			// the first point of the very first segment (segment 0). For every
			// other segment (segment i) add only its first point as its first
			// point and as the second point of the previous segment (segment
			// i-1). The very last point will be added as the second point of
			// the very last segment (segment n-1). 

			_right   = MakeRight();
			_forward = MakeForward();

			SegmentLines(  _lineSets[0], origin);
			RadialLines(   _lineSets[1], origin);
			CylindricLines(_lineSets[2], origin);

			// TODO: performance can be improved by reducing the amount of
			// rotations computed. To this end invert the loop, i.e. iterate
			// over the angles first, inside those iterate over the arcs and
			// inside those over the layers. Compute the rotation once per
			// angle and use that for drawing radial lines, segments and layer
			// lines.  PROBLEM: handling over- and underflow in a loop without
			// repeating; in the current implementation we can make the two
			// exceptions without repeating code, because they are in the
			// inner-most loop.

			/*Quaternion rotation;
			int i = 0, j = 0, k = 0;
			phi = _from[idx[2]] * Degrees;
			r = Mathf.CeilToInt(_from[idx[0]]);
			z = Mathf.CeilToInt(_from[idx[2]]);
			while (r < _to[idx[0]]) {
				// draw layer line here
				while (z < _to[idx[2]]) {
					// draw segment here
					++k;
				}
				++j;
			}*/
		}

		private void SegmentLines(Vector3[][] points, Vector3 origin) {
			var depth  = Grid.Depth;
			var angle  = Grid.Degrees;

			var delta_phi = angle / Smoothness;

			var z = Mathf.Ceil(_from.z) * depth;
			for (var i = 0; i < layerCount; ++i) {  // for every layer
				var r = Mathf.Ceil(_from.x);

				for (var j = 0; j < arcCount; ++j) {  // for every circle/arc
					// the first point is an exception
					var index = i * arcCount * segmentCount + j * segmentCount;
					var phi = Mathf.Ceil(_from[1] * Smoothness) / Smoothness * angle;

					points[index][0] = ContributePoint(r, _from[1] * angle, z, origin);

					for (var k = 1; k < segmentCount; ++k) { // for every segment
						var point = ContributePoint(r, phi, z, origin);
						var pointIndex = i * arcCount * segmentCount + j * segmentCount + k - 1;

						points[pointIndex    ][1] = point;
						points[pointIndex + 1][0] = point;

						phi += delta_phi;
					}

					// the last point is an exception as well
					index += segmentCount - 1;
					points[index][1] = ContributePoint(r, _to[1] * angle, z, origin);

					++r;
				}
				z += depth;
			}
		}

		private void RadialLines(Vector3[][] points, Vector3 origin) {
			var z = Mathf.Ceil(_from[2]) * Grid.Depth;

			for (var i = 0; i < layerCount; ++i) {
				var phi = Mathf.Ceil(_from[1]) * Grid.Degrees;

				for (var j = 0; j < sectorCount; ++j) {
					var index = i * sectorCount + j;

					points[index][0] = ContributePoint(_from[0], phi, z, origin);
					points[index][1] = ContributePoint(  _to[0], phi, z, origin);
					phi += Grid.Degrees;
				}
				z += Grid.Depth;
			}
		}

		private void CylindricLines(Vector3[][] points, Vector3 origin) {
			var r = Mathf.Ceil(_from[0]);

			var fromZ = _from[2] * Grid.Depth;
			var   toZ =   _to[2] * Grid.Depth;

			for (var i = 0; i < arcCount; ++i) {
				var phi = Mathf.Ceil(_from[1]) * Grid.Degrees;
				for (var j = 0; j < sectorCount; ++j) {
					var index = i * sectorCount + j;
					points[index][0] = ContributePoint(r, phi, fromZ, origin);
					points[index][1] = ContributePoint(r, phi,   toZ, origin);
					phi += Grid.Degrees;
				}
				++r;
			}
		}
#endregion  // Compute

#region  Start
		void Start() {
			if (_hasBeenInitialized) {
				return;
			}
			RadialTo = 3f;
			SectorTo = Grid.Sectors;
			_hasBeenInitialized = true;
		}
#endregion  // Start

#region  Helper
		/// <summary>
		///   Contributes a point of the grid to drawing.
		/// </summary>
		private Vector3 ContributePoint(float r, float phi, float z, Vector3 origin) {
			var q = Quaternion.AngleAxis(phi, _forward);
			return ContributePoint(r, q, z, origin);
		}

		private Vector3 ContributePoint(float r, Quaternion q, float z, Vector3 origin) {
			var pivot = origin + z * _forward;
			return pivot + q * (r * _right);
		}

		private Vector3 MakeRight() {
			var zero = Grid.GridToWorld(Vector3.zero);
			var right = Grid.GridToWorld(Vector3.right);
			return right - zero;
		}

		/* void OnDrawGizmos() { */
		/* 	var zero = Grid.GridToWorld(Vector3.zero); */
		/* 	var right = Grid.GridToWorld(Vector3.right); */
		/* 	Gizmos.DrawLine(zero, right); */
		/* } */

		private Vector3 MakeForward() {
			var zero = Grid.GridToWorld(Vector3.zero);
			var forward = Grid.GridToWorld(Vector3.forward);
			return forward - zero;
		}

		private static float Float2Sector(float number, float amount) {
			if (number > amount) {
				return number % amount;
			} if (number < 0f) {
				return amount + (number % amount);
			}
			return number;
		}
#endregion  // Helper

#region  Hook methods
		private void OnRadialFromChanged(float previousFrom, float previousTo) {
			var fromChanged = Mathf.Abs(previousFrom - RadialFrom) > Mathf.Epsilon;
			var   toChanged = Mathf.Abs(previousTo   -   RadialTo) > Mathf.Epsilon;
			if (fromChanged || toChanged) {
				UpdatePoints();
			}
		}

		private void OnSectorChanged(float previousFrom, float previousTo) {
			var fromChanged = Mathf.Abs(previousFrom - SectorFrom) > Mathf.Epsilon;
			var   toChanged = Mathf.Abs(previousTo   -   SectorTo) > Mathf.Epsilon;
			if (fromChanged || toChanged) {
				UpdatePoints();
			}
		}

		private void OnLayerChanged(float previousFrom, float previousTo) {
			var fromChanged = Mathf.Abs(previousFrom - LayerFrom) > Mathf.Epsilon;
			var   toChanged = Mathf.Abs(previousTo   -   LayerTo) > Mathf.Epsilon;
			if (fromChanged || toChanged) {
				UpdatePoints();
			}
		}

		private void OnSmoothnessChanged(int previous) {
			var changed = previous != Smoothness;
			if (changed) {
				UpdatePoints();
			}
		}
#endregion  // Hook methods
	}
}
