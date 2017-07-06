using UnityEngine;
using GridFramework.Grids;
using GridRenderer = GridFramework.Renderers.GridRenderer;
using RadiusEventArgs = GridFramework.Grids.SphereGrid.RadiusEventArgs;
using LinesEventArgs = GridFramework.Grids.SphereGrid.LinesEventArgs;
using SmoothnessEventArgs = GridFramework.Grids.SphereGrid.SmoothnessEventArgs;

namespace GridFramework.Renderers.Spherical {
	/// <summary>
	///   Base class for all spherical grid renderers.
	/// </summary>
	[RequireComponent(typeof(SphereGrid))]
	public abstract class SphericalRenderer : GridRenderer {

#region  Private variables
		[SerializeField]
		protected SphereGrid _grid;
#endregion  // Private variables

#region  Protected properties
		/// <summary>
		///   Reference to the grid.
		/// </summary>
		protected SphereGrid Grid {
			get {
				if (!_grid) {
					_grid = GetComponent<SphereGrid>();
				}
				return _grid;
			}
		}
#endregion  //Protected properties

#region  Setup methods
		void OnEnable() {
			Grid.RadiusChanged     += OnRadiusChanged;
			Grid.LinesChanged      += OnLinesChanged;
			Grid.SmoothnessChanged += OnSmoothnessChanged;
		}

		void OnDisable() {
			Grid.RadiusChanged     -= OnRadiusChanged;
			Grid.LinesChanged      -= OnLinesChanged;
			Grid.SmoothnessChanged -= OnSmoothnessChanged;
		}
#endregion  // Setup methods

#region  Event methods
		private void OnRadiusChanged(object source, RadiusEventArgs args) {
			if (args.Difference < Mathf.Epsilon) {
				return;
			}
			UpdatePoints();
		}

		private void OnLinesChanged(object source, LinesEventArgs args) {
			if (args.ParallelsDifference == 0 && args.MeridiansDifference == 0) {
				return;
			}
			UpdatePoints();
		}

		private void OnSmoothnessChanged(object source, SmoothnessEventArgs args) {
			if (args.ParallelsDifference == 0 && args.MeridiansDifference == 0) {
				return;
			}
			UpdatePoints();
		}
#endregion  // Event methods
	}
}
