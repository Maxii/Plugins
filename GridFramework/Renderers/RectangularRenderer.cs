using UnityEngine;
using GridFramework.Grids;
using GridFramework.Vectors;
using SpacingEventArgs = GridFramework.Grids.RectGrid.SpacingEventArgs;
using ShearingEventArgs = GridFramework.Grids.RectGrid.ShearingEventArgs;
using GridRenderer = GridFramework.Renderers.GridRenderer;

namespace GridFramework.Renderers.Rectangular {
	/// <summary>
	///   Base class for all rectangular grid renderers.
	/// </summary>
	[RequireComponent(typeof(RectGrid))]
	public abstract class RectangularRenderer : GridRenderer {

#region  Private variables
		[SerializeField]
		protected RectGrid _grid;
#endregion  // Private variables

#region  Protected properties
		/// <summary>
		///   Reference to the grid.
		/// </summary>
		protected RectGrid Grid {
			get {
				if (!_grid) {
					_grid = GetComponent<RectGrid>();
				}
				return _grid;
			}
		}
#endregion  //Protected properties

#region  Setup methods
		void OnEnable() {
			Grid.SpacingChanged  += OnSpacingChanged;
			Grid.ShearingChanged += OnShearingChanged;
		}

		void OnDisable() {
			Grid.SpacingChanged  -= OnSpacingChanged;
			Grid.ShearingChanged -= OnShearingChanged;
		}
#endregion  // Setup methods

#region  Event methods
		private void OnSpacingChanged(object source, SpacingEventArgs args) {
			if (args.Difference == Vector3.zero) {
				return;
			}

			var current  = Grid.Spacing;
			var previous = current - args.Difference;
			var shearing = Grid.Shearing;
			var position = Grid.transform.position;
			var rotation = Grid.transform.rotation;

			var trMatrix      = Matrix4x4.TRS(position, rotation, Vector3.one);
			var shearMatrix   = shearing.ShearMatrix();
			var spacingMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, previous);

			var inverseMatrix = (trMatrix * shearMatrix * spacingMatrix).inverse;

			spacingMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, current);
			var forwardMatrix = trMatrix * shearMatrix * spacingMatrix;

			var matrix = forwardMatrix * inverseMatrix;

			for (int i = 0; i < 3; ++i) {
				for (int j = 0; j < _lineSets[i].Length; ++j) {
					for (int k = 0; k < 2; ++k) {
						Vector3 point = _lineSets[i][j][k];
						_lineSets[i][j][k] = matrix.MultiplyPoint3x4(point);
					}
				}
			}
		}

		private void OnShearingChanged(object source, ShearingEventArgs args) {
			if (args.Difference == Vector6.Zero) {
				return;
			}

			if (_lineSets[0] == null || _lineSets[1] == null || _lineSets[2] == null) {
				UpdatePoints();
			}

			var current  = Grid.Shearing;
			var previous = current - args.Difference;
			var position = Grid.transform.position;
			var rotation = Grid.transform.rotation;

			var trMatrix    = Matrix4x4.TRS(position, rotation, Vector3.one);
			var shearMatrix = previous.ShearMatrix();

			var inverseMatrix = (trMatrix * shearMatrix).inverse;

			shearMatrix = current.ShearMatrix();

			var forwardMatrix = trMatrix * shearMatrix;

			var matrix = forwardMatrix * inverseMatrix;

			for (var i = 0; i < 3; ++i) {
				for (var j = 0; j < _lineSets[i].Length; ++j) {
					for (var k = 0; k < 2; ++k) {
						Vector3 point = _lineSets[i][j][k];
						_lineSets[i][j][k] = matrix.MultiplyPoint3x4(point);
					}
				}
			}
		}
#endregion  // Event methods
	}
}
