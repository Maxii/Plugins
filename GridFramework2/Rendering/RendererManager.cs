using System.Collections.Generic;

using GridRenderer      = GridFramework.Renderers.GridRenderer;
using Comparison        = System.Comparison<GridFramework.Renderers.GridRenderer>;
using PriorityEventArgs = GridFramework.Renderers.GridRenderer.PriorityEventArgs;

namespace GridFramework.Rendering {
	/// <summary>
	///   This class keeps track of all renderers and renders them when
	///   appropriate.
	/// </summary>
	/// <remarks>
	///   <para>
	///     The manager is completely closed data-wise and only accessible
	///     through its public methods. You should not have to use the manager
	///     yourself, renderers will register and unregister themselves when
	///     they are enabled or disabled.
	///   </para>
	///   <para>
	///     However, if for whatever reason you still want to access the
	///     manager you can safely register and unregister renderers, there
	///     will be no duplicates and already unregistered renderers will be
	///     ignored.
	///   </para>
	/// </remarks>
	public static class RendererManager {
		private readonly static List<GridRenderer> _renderers  = new List<GridRenderer>();
		private readonly static Comparison PriorityComparison = CompareRenderersByPriority;

		/// <summary>
		///   The list of all currently registered renderers.
		/// </summary>
		public static List<GridRenderer> Renderers {
			get {
				return _renderers;
			}
		}

		/// <summary>
		///   Add a renderer to the system.
		/// </summary>
		/// <param name="renderer">
		///   The renderer to add to the system.
		/// </param>
		/// <returns>
		///   <c>True</c> if the renderers has successfully been registered,
		///   <c>false</c> otherwise.
		/// </returns>
		/// <remarks>
		///   <para>
		///     If the renderer has already been registered nothing will happen
		///     and the method will return <c>false</c>. After adding a
		///     renderer all renderers are re-sorted by their priority, so only
		///     add and remove renderers if you really mean it. If you just
		///     want to disable a renderer temporarily set all its colours to
		///     zero alpha, this will make the system skip
		///     it.
		///   </para>
		/// </remarks>
		public static bool RegisterRenderer(GridRenderer renderer){
			var unregistered = !_renderers.Contains(renderer);

			if (unregistered) {
				_renderers.Add(renderer);
				renderer.PriorityChanged += OnPriorityChanged;
				_renderers.Sort(PriorityComparison);
			}

			return unregistered;
		}
		
		/// <summary>
		///   Remove a renderer from the system.
		/// </summary>
		/// <param name="renderer">
		///   The renderer to add to the system.
		/// </param>
		/// <returns>
		///   <c>True</c> if the renderers has successfully been unregistered,
		///   <c>false</c> otherwise.
		/// </returns>
		/// <remarks>
		///   <para>
		///     If the renderer has been unregistered the system will no longer
		///     listen to the renderer's <c>PriorityChanged</c> event.
		///   </para>
		/// </remarks>
		public static bool UnregisterRenderer(GridRenderer renderer){
			var removed = _renderers.Remove(renderer);

			if (removed) {
				renderer.PriorityChanged -= OnPriorityChanged;
			}

			return removed;
		}

		private static int CompareRenderersByPriority(GridRenderer r1, GridRenderer r2) {
			return r2.Priority - r1.Priority;
		}

		private static void OnPriorityChanged(object source, PriorityEventArgs args) {
			// If the Unity editor inspector is changed in *any* way we will
			// receive this event, even if the priority has not changed. To
			// avoid potentially expensive sorting for no reason we check the
			// difference first.

			if (args.Difference == 0) {
				return;
			}
			_renderers.Sort(PriorityComparison);
		}
	}
}
