using UnityEngine;
using GridFramework.Grids;
using GridRenderer = GridFramework.Renderers.GridRenderer;
using RadiusEventArgs = GridFramework.Grids.PolarGrid.RadiusEventArgs;
using SectorsEventArgs = GridFramework.Grids.PolarGrid.SectorsEventArgs;
using DepthEventArgs = GridFramework.Grids.LayeredGrid.DepthEventArgs;


namespace GridFramework.Renderers.Polar {
	/// <summary>
	///   Base class for all polar grid renderers.
	/// </summary>
	[RequireComponent(typeof(PolarGrid))]
	public abstract class PolarRenderer : GridRenderer {

#region  Private variables
		[SerializeField]
		protected PolarGrid _grid;
#endregion  // Private variables

#region  Protected properties
		/// <summary>
		///   Reference to the grid.
		/// </summary>
		protected PolarGrid Grid {
			get {
				if (!_grid) {
					_grid = GetComponent<PolarGrid>();
				}
				return _grid;
			}
		}
#endregion  //Protected properties

#region  Setup methods
		void OnEnable() {
			Grid.RadiusChanged  += OnRadiusChanged;
			Grid.SectorsChanged += OnSectorsChanged;
			Grid.DepthChanged   += OnDepthChanged;
		}

		void OnDisable() {
			Grid.RadiusChanged  -= OnRadiusChanged;
			Grid.SectorsChanged -= OnSectorsChanged;
			Grid.DepthChanged   -= OnDepthChanged;
		}
#endregion  // Setup methods

#region  Event methods
		private void OnRadiusChanged(object source, RadiusEventArgs args) {
			if (Mathf.Abs(args.Difference) <= Mathf.Epsilon) {
				return;
			}
			UpdatePoints();
		}

		private void OnSectorsChanged(object source, SectorsEventArgs args) {
			if (args.Difference == 0) {
				return;
			}
			UpdatePoints();
		}

		private void OnDepthChanged(object source, DepthEventArgs args) {
			if (Mathf.Abs(args.Difference) <= Mathf.Epsilon) {
				return;
			}
			UpdatePoints();
		}
#endregion  // Event methods
	}
}
