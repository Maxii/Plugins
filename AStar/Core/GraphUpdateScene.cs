using UnityEngine;

namespace Pathfinding {
	[AddComponentMenu("Pathfinding/GraphUpdateScene")]
	/** Helper class for easily updating graphs.
	 *
	 * The GraphUpdateScene component is really easy to use. Create a new empty GameObject and add the component to it, it can be found in Components-->Pathfinding-->GraphUpdateScene.\n
	 * When you have added the component, you should see something like the image below.
	 * \shadowimage{graphUpdateScene.png}
	 * The area which the component will affect is defined by creating a polygon in the scene.
	 * If you make sure you have the Position tool enabled (top-left corner of the Unity window) you can shift-click in the scene view to add more points to the polygon.
	 * You can remove points using shift-alt-click. By clicking on the points you can bring up a positioning tool. You can also open the "points" array in the inspector to set each point's coordinates.
	 * \shadowimage{graphUpdateScenePoly.png}
	 * In the inspector there are a number of variables. The first one is named "Convex", it sets if the convex hull of the points should be calculated or if the polygon should be used as-is.
	 * Using the convex hull is faster when applying the changes to the graph, but with a non-convex polygon you can specify more complicated areas.\n
	 * The next two variables, called "Apply On Start" and "Apply On Scan" determine when to apply the changes. If the object is in the scene from the beginning, both can be left on, it doesn't
	 * matter since the graph is also scanned at start. However if you instantiate it later in the game, you can make it apply it's setting directly, or wait until the next scan (if any).
	 * If the graph is rescanned, all GraphUpdateScene components which have the Apply On Scan variable toggled will apply their settings again to the graph since rescanning clears all previous changes.\n
	 * You can also make it apply it's changes using scripting.
	 * \code GetComponent<GraphUpdateScene>().Apply (); \endcode
	 * The above code will make it apply it's changes to the graph (assuming a GraphUpdateScene component is attached to the same GameObject).
	 *
	 * Next there is "Modify Walkability" and "Set Walkability" (which appears when "Modify Walkability" is toggled).
	 * If Modify Walkability is set, then all nodes inside the area will either be set to walkable or unwalkable depending on the value of the "Set Walkability" variable.
	 *
	 * Penalty can also be applied to the nodes. A higher penalty (aka weight) makes the nodes harder to traverse so it will try to avoid those areas.
	 *
	 * The tagging variables can be read more about on this page: \ref tags "Working with tags".
	 */
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_graph_update_scene.php")]
	public class GraphUpdateScene : GraphModifier {
		/** Points which define the region to update */
		public Vector3[] points;

		/** Private cached convex hull of the #points */
		private Vector3[] convexPoints;

		[HideInInspector]
		/** Use the convex hull (XZ space) of the points. */
		public bool convex = true;

		[HideInInspector]
		/** Minumum height of the bounds of the resulting Graph Update Object.
		 * Useful when all points are laid out on a plane but you still need a bounds with a height greater than zero since a
		 * zero height graph update object would usually result in no nodes being updated.
		 */
		public float minBoundsHeight = 1;

		[HideInInspector]
		/** Penalty to add to nodes.
		 * Be careful when setting negative values since if a node get's a negative penalty it will underflow and instead get
		 * really large. In most cases a warning will be logged if that happens.
		 */
		public int penaltyDelta;

		[HideInInspector]
		/** Set to true to set all targeted nodese walkability to #setWalkability */
		public bool modifyWalkability;

		[HideInInspector]
		/** See #modifyWalkability */
		public bool setWalkability;

		[HideInInspector]
		/** Apply this graph update object on start */
		public bool applyOnStart = true;

		[HideInInspector]
		/** Apply this graph update object whenever a graph is rescanned */
		public bool applyOnScan = true;

		/** Use world space for coordinates.
		 * If true, the shape will not follow when moving around the transform.
		 *
		 * \see #ToggleUseWorldSpace
		 */
		[HideInInspector]
		public bool useWorldSpace;

		/** Update node's walkability and connectivity using physics functions.
		 * For grid graphs, this will update the node's position and walkability exactly like when doing a scan of the graph.
		 * If enabled for grid graphs, #modifyWalkability will be ignored.
		 *
		 * For Point Graphs, this will recalculate all connections which passes through the bounds of the resulting Graph Update Object
		 * using raycasts (if enabled).
		 *
		 */
		[HideInInspector]
		public bool updatePhysics;

		/** \copydoc Pathfinding::GraphUpdateObject::resetPenaltyOnPhysics */
		[HideInInspector]
		public bool resetPenaltyOnPhysics = true;

		/** \copydoc Pathfinding::GraphUpdateObject::updateErosion */
		[HideInInspector]
		public bool updateErosion = true;

		[HideInInspector]
		/** Lock all points to Y = #lockToYValue */
		public bool lockToY;

		[HideInInspector]
		/** if #lockToY is enabled lock all points to this value */
		public float lockToYValue;

		[HideInInspector]
		/** If enabled, set all nodes' tags to #setTag */
		public bool modifyTag;

		[HideInInspector]
		/** If #modifyTag is enabled, set all nodes' tags to this value */
		public int setTag;

		/** Private cached inversion of #setTag.
		 * Used for InvertSettings() */
		private int setTagInvert;

		/** Has apply been called yet.
		 * Used to prevent applying twice when both applyOnScan and applyOnStart are enabled */
		private bool firstApplied;

		/** Do some stuff at start */
		public void Start () {
			//If firstApplied is true, that means the graph was scanned during Awake.
			//So we shouldn't apply it again because then we would end up applying it two times
			if (!firstApplied && applyOnStart) {
				Apply();
			}
		}

		public override void OnPostScan () {
			if (applyOnScan) Apply();
		}

		/** Inverts all invertable settings for this GUS.
		 * Namely: penalty delta, walkability, tags.
		 *
		 * Penalty delta will be changed to negative penalty delta.\n
		 * #setWalkability will be inverted.\n
		 * #setTag will be stored in a private variable, and the new value will be 0. When calling this function again, the saved
		 * value will be the new value.
		 *
		 * Calling this function an even number of times without changing any settings in between will be identical to no change in settings.
		 */
		public virtual void InvertSettings () {
			setWalkability = !setWalkability;
			penaltyDelta = -penaltyDelta;
			if (setTagInvert == 0) {
				setTagInvert = setTag;
				setTag = 0;
			} else {
				setTag = setTagInvert;
				setTagInvert = 0;
			}
		}

		/** Recalculate convex points.
		 * Will not do anything if not #convex is enabled
		 */
		public void RecalcConvex () {
			convexPoints = convex ? Polygon.ConvexHullXZ(points) : null;
		}

		/** Switches between using world space and using local space.
		 * Changes point coordinates to stay the same in world space after the change.
		 *
		 * \see #useWorldSpace
		 */
		public void ToggleUseWorldSpace () {
			useWorldSpace = !useWorldSpace;

			if (points == null) return;

			convexPoints = null;

			Matrix4x4 matrix = useWorldSpace ? transform.localToWorldMatrix : transform.worldToLocalMatrix;

			for (int i = 0; i < points.Length; i++) {
				points[i] = matrix.MultiplyPoint3x4(points[i]);
			}
		}

		/** Lock all points to a specific Y value.
		 *
		 * \see lockToYValue
		 */
		public void LockToY () {
			if (points == null) return;

			for (int i = 0; i < points.Length; i++)
				points[i].y = lockToYValue;
		}

		/** Apply the update.
		 * Will only do anything if #applyOnScan is enabled */
		public void Apply (AstarPath active) {
			if (applyOnScan) {
				Apply();
			}
		}

		/** Calculates the bounds for this component.
		 * This is a relatively expensive operation, it needs to go through all points and
		 * sometimes do matrix multiplications.
		 */
		public Bounds GetBounds () {
			Bounds b;

			if (points == null || points.Length == 0) {
				var coll = GetComponent<Collider>();
				var rend = GetComponent<Renderer>();

				if (coll != null) b = coll.bounds;
				else if (rend != null) b = rend.bounds;
				else {
					//Debug.LogWarning ("Cannot apply GraphUpdateScene, no points defined and no renderer or collider attached");
					return new Bounds(Vector3.zero, Vector3.zero);
				}
			} else {
				Matrix4x4 matrix = Matrix4x4.identity;

				if (!useWorldSpace) {
					matrix = transform.localToWorldMatrix;
				}

				Vector3 min = matrix.MultiplyPoint3x4(points[0]);
				Vector3 max = matrix.MultiplyPoint3x4(points[0]);
				for (int i = 0; i < points.Length; i++) {
					Vector3 p = matrix.MultiplyPoint3x4(points[i]);
					min = Vector3.Min(min, p);
					max = Vector3.Max(max, p);
				}

				b = new Bounds((min+max)*0.5F, max-min);
			}

			if (b.size.y < minBoundsHeight) b.size = new Vector3(b.size.x, minBoundsHeight, b.size.z);
			return b;
		}

		/** Updates graphs with a created GUO.
		 * Creates a Pathfinding.GraphUpdateObject with a Pathfinding.GraphUpdateShape
		 * representing the polygon of this object and update all graphs using AstarPath.UpdateGraphs.
		 * This will not update graphs directly. See AstarPath.UpdateGraph for more info.
		 */
		public void Apply () {
			if (AstarPath.active == null) {
				Debug.LogError("There is no AstarPath object in the scene");
				return;
			}

			GraphUpdateObject guo;

			if (points == null || points.Length == 0) {
				var coll = GetComponent<Collider>();
				var rend = GetComponent<Renderer>();

				Bounds b;
				if (coll != null) b = coll.bounds;
				else if (rend != null) b = rend.bounds;
				else {
					Debug.LogWarning("Cannot apply GraphUpdateScene, no points defined and no renderer or collider attached");
					return;
				}

				if (b.size.y < minBoundsHeight) b.size = new Vector3(b.size.x, minBoundsHeight, b.size.z);

				guo = new GraphUpdateObject(b);
			} else {
				var shape = new GraphUpdateShape();
				shape.convex = convex;
				Vector3[] worldPoints = points;
				if (!useWorldSpace) {
					worldPoints = new Vector3[points.Length];
					Matrix4x4 matrix = transform.localToWorldMatrix;
					for (int i = 0; i < worldPoints.Length; i++) worldPoints[i] = matrix.MultiplyPoint3x4(points[i]);
				}

				shape.points = worldPoints;

				Bounds b = shape.GetBounds();
				if (b.size.y < minBoundsHeight) b.size = new Vector3(b.size.x, minBoundsHeight, b.size.z);
				guo = new GraphUpdateObject(b);
				guo.shape = shape;
			}

			firstApplied = true;

			guo.modifyWalkability = modifyWalkability;
			guo.setWalkability = setWalkability;
			guo.addPenalty = penaltyDelta;
			guo.updatePhysics = updatePhysics;
			guo.updateErosion = updateErosion;
			guo.resetPenaltyOnPhysics = resetPenaltyOnPhysics;

			guo.modifyTag = modifyTag;
			guo.setTag = setTag;

			AstarPath.active.UpdateGraphs(guo);
		}

		/** Draws some gizmos */
		public void OnDrawGizmos () {
			OnDrawGizmos(false);
		}

		/** Draws some gizmos */
		public void OnDrawGizmosSelected () {
			OnDrawGizmos(true);
		}

		/** Draws some gizmos */
		public void OnDrawGizmos (bool selected) {
			Color c = selected ? new Color(227/255f, 61/255f, 22/255f, 1.0f) : new Color(227/255f, 61/255f, 22/255f, 0.9f);

			if (selected) {
				Gizmos.color = Color.Lerp(c, new Color(1, 1, 1, 0.2f), 0.9f);

				Bounds b = GetBounds();
				Gizmos.DrawCube(b.center, b.size);
				Gizmos.DrawWireCube(b.center, b.size);
			}

			if (points == null) return;

			if (convex) {
				c.a *= 0.5f;
			}

			Gizmos.color = c;

			Matrix4x4 matrix = useWorldSpace ? Matrix4x4.identity : transform.localToWorldMatrix;

			if (convex) {
				c.r -= 0.1f;
				c.g -= 0.2f;
				c.b -= 0.1f;

				Gizmos.color = c;
			}

			if (selected || !convex) {
				for (int i = 0; i < points.Length; i++) {
					Gizmos.DrawLine(matrix.MultiplyPoint3x4(points[i]), matrix.MultiplyPoint3x4(points[(i+1)%points.Length]));
				}
			}

			if (convex) {
				if (convexPoints == null) RecalcConvex();

				Gizmos.color = selected ? new Color(227/255f, 61/255f, 22/255f, 1.0f) : new Color(227/255f, 61/255f, 22/255f, 0.9f);

				for (int i = 0; i < convexPoints.Length; i++) {
					Gizmos.DrawLine(matrix.MultiplyPoint3x4(convexPoints[i]), matrix.MultiplyPoint3x4(convexPoints[(i+1)%convexPoints.Length]));
				}
			}
		}
	}
}
