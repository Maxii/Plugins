using UnityEngine;
using System.Collections.Generic;
using GridFramework.Grids;
using GridRenderer = GridFramework.Renderers.GridRenderer;
using DepthEventArgs = GridFramework.Grids.LayeredGrid.DepthEventArgs;
using RadiusEventArgs = GridFramework.Grids.HexGrid.RadiusEventArgs;
using SidesEventArgs = GridFramework.Grids.HexGrid.SidesEventArgs;

namespace GridFramework.Renderers.Hexagonal {
	/// <summary>
	///   Base class for all hexagonal grid renderers.
	/// </summary>
	[RequireComponent(typeof(HexGrid))]
	public abstract class HexRenderer: GridRenderer {
#region  Private variables
		[SerializeField]
		private HexGrid _grid;
#endregion  // Private variables

#region  Protected properties
		/// <summary>
		///   Reference to the grid.
		/// </summary>
		protected HexGrid Grid {
			get {
				if (!_grid) {
					_grid = GetComponent<HexGrid>();
				}
				return _grid;
			}
		}

		/// <summary>
		///   Whether the grid has pointed sides.
		/// </summary>
		protected bool PointedSides {
			get {
				return Grid.Sides == HexGrid.Orientation.Pointed;
			}
		}

		/// <summary>
		///   Whether the grid has flat sides.
		/// </summary>
		protected bool FlatSides {
			get {
				return !PointedSides;
			}
		}
#endregion  // Protected properties

#region  Setup methods
		void OnEnable() {
			Grid.DepthChanged  += OnDepthChanged;
			Grid.RadiusChanged += OnRadiusChanged;
			Grid.SidesChanged  += OnSidesChanged;
		}

		void OnDisable() {
			Grid.DepthChanged  -= OnDepthChanged;
			Grid.RadiusChanged -= OnRadiusChanged;
			Grid.SidesChanged  -= OnSidesChanged;
		}
#endregion  // Setup methods

#region  Helper methods
		protected static void ContributeLine(IList<IList<Vector3>> lineSet, Vector3 hex, Vector3 point1, Vector3 point2, ref int iterator) {
			lineSet[iterator][0] = hex + point1;
			lineSet[iterator][1] = hex + point2;
			++iterator;
		}

		protected static void ContributeLine(IList<IList<Vector3>> lineSet, Vector3 hex, Vector3 vertex, Vector3 back, Vector3 front, ref int i) {
			lineSet[i][0] = hex + vertex + back;
			lineSet[i][1] = hex + vertex + front;
			++i;
		}

		protected Vector3 CardinalToVertex(HexGrid.HexDirection direction) {
			Vector4 result;
			switch (direction) {
				case HexGrid.HexDirection.E  : result = new Vector4( 2f/3f, -1f/3f, -1f/3f); break;
				case HexGrid.HexDirection.NE : result = new Vector4( 1f/3f,  1f/3f, -2f/3f); break;
				case HexGrid.HexDirection.NW : result = new Vector4(-1f/3f,  2f/3f, -1f/3f); break;
				case HexGrid.HexDirection.W  : result = new Vector4(-2f/3f,  1f/3f,  1f/3f); break;
				case HexGrid.HexDirection.SW : result = new Vector4(-1f/3f, -1f/3f,  2f/3f); break;
				case HexGrid.HexDirection.SE : result = new Vector4( 1f/3f, -2f/3f,  1f/3f); break;
				default: throw new System.ArgumentOutOfRangeException();
			}
			var vertex = Grid.CubicToWorld(result) - Grid.CubicToWorld(Vector4.zero);
			return vertex;
		}

		protected static void Swap<T>(ref T a, ref T b, bool condition = true) {
			if (!condition) {
				return;
			}
			T temp = a;
			a = b;
			b = temp;
		}

		protected static bool IsEven(int i) {
			return i % 2 == 0;
		}

		protected static bool IsOdd(int i) {
			return !IsEven(i);
		}
#endregion  // Helper methods

#region  Hook methods
		protected virtual void OnDepthChanged(object source, DepthEventArgs args) {
			if (Mathf.Abs(args.Difference) < Mathf.Epsilon) {
				return;
			}
			UpdatePoints();
		}

		protected virtual void OnRadiusChanged(object source, RadiusEventArgs args) {
			if (Mathf.Abs(args.Difference) < Mathf.Epsilon) {
				return;
			}
			UpdatePoints();
		}

		protected virtual void OnSidesChanged(object source, SidesEventArgs args) {
			if (args.Previous == Grid.Sides) {
				return;
			}
			UpdatePoints();
		}
#endregion  // Hook methods
	}
}
