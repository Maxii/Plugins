using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization.JsonFx;
using Pathfinding.Serialization;

namespace Pathfinding {

	/** Basic point graph.
	 * \ingroup graphs
	  * The List graph is the most basic graph structure, it consists of a number of interconnected points in space, waypoints or nodes.\n
	  * The list graph takes a Transform object as "root", this Transform will be searched for child objects, every child object will be treated as a node.
	  * It will then check if any connections between the nodes can be made, first it will check if the distance between the nodes isn't too large ( #maxDistance )
	  * and then it will check if the axis aligned distance isn't too high. The axis aligned distance, named #limits,
	  * is useful because usually an AI cannot climb very high, but linking nodes far away from each other,
	  * but on the same Y level should still be possible. #limits and #maxDistance won't affect anything if the values are 0 (zero) though. \n
	  * Lastly it will check if there are any obstructions between the nodes using
	  * <a href="http://unity3d.com/support/documentation/ScriptReference/Physics.Raycast.html">raycasting</a> which can optionally be thick.\n
	  * One thing to think about when using raycasting is to either place the nodes a small
	  * distance above the ground in your scene or to make sure that the ground is not in the raycast \a mask to avoid the raycast from hitting the ground.\n
	  * \note Does not support linecast because of obvious reasons.
	  *
\shadowimage{pointgraph_graph.png}
\shadowimage{pointgraph_inspector.png}

	  */
	[JsonOptIn]
	public class PointGraph : NavGraph
	{

		/** Childs of this transform are treated as nodes */
		[JsonMember]
		public Transform root;

		/** If no #root is set, all nodes with the tag is used as nodes */
		[JsonMember]
		public string searchTag;

		/** Max distance for a connection to be valid.
		 * The value 0 (zero) will be read as infinity and thus all nodes not restricted by
		 * other constraints will be added as connections.
		 *
		 * A negative value will disable any neighbours to be added.
		 * It will completely stop the connection processing to be done, so it can save you processing
		 * power if you don't these connections.
		 */
		[JsonMember]
		public float maxDistance;

		/** Max distance along the axis for a connection to be valid. 0 = infinity */
		[JsonMember]
		public Vector3 limits;

		/** Use raycasts to check connections */
		[JsonMember]
		public bool raycast = true;

		/** Use the 2D Physics API */
		[JsonMember]
		public bool use2DPhysics;

		/** Use thick raycast */
		[JsonMember]
		public bool thickRaycast;

		/** Thick raycast radius */
		[JsonMember]
		public float thickRaycastRadius = 1;

		/** Recursively search for childnodes to the #root */
		[JsonMember]
		public bool recursive = true;

		[JsonMember]
		public bool autoLinkNodes = true;

		/** Layer mask to use for raycast */
		[JsonMember]
		public LayerMask mask;


		/** All nodes in this graph.
		 * Note that only the first #nodeCount will be non-null.
		 *
		 * You can also use the GetNodes method to get all nodes.
		 */
		public PointNode[] nodes;

		/** Number of nodes in this graph.
		 *
		 * \warning Do not edit directly
		 */
		public int nodeCount;


		public override void GetNodes (GraphNodeDelegateCancelable del) {
			if (nodes == null) return;
			for (int i = 0; i < nodeCount && del (nodes[i]); i++) {}
		}

		public override NNInfo GetNearest (Vector3 position, NNConstraint constraint, GraphNode hint) {
			return GetNearestForce (position, constraint);
		}

		public override NNInfo GetNearestForce (Vector3 position, NNConstraint constraint) {
			//Debug.LogError ("This function (GetNearest) is not implemented in the navigation graph generator : Type "+this.GetType ().Name);

			if (nodes == null) return new NNInfo();

			float maxDistSqr = constraint.constrainDistance ? AstarPath.active.maxNearestNodeDistanceSqr : float.PositiveInfinity;

			float minDist = float.PositiveInfinity;
			GraphNode minNode = null;

			float minConstDist = float.PositiveInfinity;
			GraphNode minConstNode = null;

				for (int i=0;i<nodeCount;i++) {
					PointNode node = nodes[i];
					float dist = (position-(Vector3)node.position).sqrMagnitude;

					if (dist < minDist) {
						minDist = dist;
						minNode = node;
					}

					if (constraint == null || (dist < minConstDist && dist < maxDistSqr && constraint.Suitable (node))) {
						minConstDist = dist;
						minConstNode = node;
					}
				}

			var nnInfo = new NNInfo (minNode);

			nnInfo.constrainedNode = minConstNode;

			if (minConstNode != null) {
				nnInfo.constClampedPosition = (Vector3)minConstNode.position;
			} else if (minNode != null) {
				nnInfo.constrainedNode = minNode;
				nnInfo.constClampedPosition = (Vector3)minNode.position;
			}

			return nnInfo;
		}

		/** Add a node to the graph at the specified position.
		 * \note Vector3 can be casted to Int3 using (Int3)myVector.
		 *
		 * \note This needs to be called when it is safe to update nodes, which is
		 * - when scanning
		 * - during a graph update
		 * - inside a callback registered using AstarPath.RegisterSafeUpdate
		 */
		public PointNode AddNode (Int3 position) {
			return AddNode ( new PointNode (active), position );
		}

		/** Add a node with the specified type to the graph at the specified position.
		 *
		 * \param node This must be a node created using T(AstarPath.active) right before the call to this method.
		 * The node parameter is only there because there is no new(AstarPath) constraint on
		 * generic type parameters.
		 * \param position The node will be set to this position.
		 * \note Vector3 can be casted to Int3 using (Int3)myVector.
		 *
		 * \note This needs to be called when it is safe to update nodes, which is
		 * - when scanning
		 * - during a graph update
		 * - inside a callback registered using AstarPath.RegisterSafeUpdate
		 *
		 * \see AstarPath.RegisterSafeUpdate
		 */
		public T AddNode<T> (T node, Int3 position) where T : PointNode {

			if ( nodes == null || nodeCount == nodes.Length ) {
				var nds = new PointNode[nodes != null ? System.Math.Max (nodes.Length+4, nodes.Length*2) : 4];
				for ( int i = 0; i < nodeCount; i++ ) nds[i] = nodes[i];
				nodes = nds;
			}

			node.SetPosition (position);
			node.GraphIndex = graphIndex;
			node.Walkable = true;

			nodes[nodeCount] = node;
			nodeCount++;

			AddToLookup ( node );

			return node;
		}

		/** Recursively counds children of a transform */
		public static int CountChildren (Transform tr) {
			int c = 0;
			foreach (Transform child in tr) {
				c++;
				c+= CountChildren (child);
			}
			return c;
		}

		/** Recursively adds childrens of a transform as nodes */
		public void AddChildren (ref int c, Transform tr) {
			foreach (Transform child in tr) {
				nodes[c].SetPosition ((Int3)child.position);
				nodes[c].Walkable = true;
				nodes[c].gameObject = child.gameObject;

				c++;
				AddChildren (ref c,child);
			}
		}

		/** Rebuilds the lookup structure for nodes.
		 *
		 * This is used when #optimizeForSparseGraph is enabled.
		 *
		 * You should call this method every time you move a node in the graph manually and
		 * you are using #optimizeForSparseGraph, otherwise pathfinding might not work correctly.
		 *
		 * \astarpro
		 */
		public void RebuildNodeLookup () {
			// A* Pathfinding Project Pro Only
		}

		public void AddToLookup ( PointNode node ) {
			// A* Pathfinding Project Pro Only
		}

		public override void ScanInternal (OnScanStatus statusCallback) {

			if (root == null) {
				//If there is no root object, try to find nodes with the specified tag instead
				GameObject[] gos = searchTag != null ? GameObject.FindGameObjectsWithTag (searchTag) : null;

				if (gos == null) {
					nodes = new PointNode[0];
					nodeCount = 0;
					return;
				}

				//Create and set up the found nodes
				nodes = new PointNode[gos.Length];
				nodeCount = nodes.Length;

				for (int i=0;i<nodes.Length;i++) nodes[i] = new PointNode(active);

				for (int i=0;i<gos.Length;i++) {
					nodes[i].SetPosition ((Int3)gos[i].transform.position);
					nodes[i].Walkable = true;
					nodes[i].gameObject = gos[i].gameObject;
				}
			} else {

				//Search the root for children and create nodes for them
				if (!recursive) {
					nodes = new PointNode[root.childCount];
					nodeCount = nodes.Length;

					for (int i=0;i<nodes.Length;i++) nodes[i] = new PointNode(active);

					int c = 0;
					foreach (Transform child in root) {
						nodes[c].SetPosition ((Int3)child.position);
						nodes[c].Walkable = true;
						nodes[c].gameObject = child.gameObject;

						c++;
					}
				} else {
					nodes = new PointNode[CountChildren(root)];
					nodeCount = nodes.Length;

					for (int i=0;i<nodes.Length;i++) nodes[i] = new PointNode(active);
						//CreateNodes (CountChildren (root));

					int startID = 0;
					AddChildren (ref startID,root);
				}
			}


			if (maxDistance >= 0) {
				//To avoid too many allocations, these lists are reused for each node
				var connections = new List<PointNode>(3);
				var costs = new List<uint>(3);

				//Loop through all nodes and add connections to other nodes
				for (int i=0;i<nodes.Length;i++) {

					connections.Clear ();
					costs.Clear ();

					PointNode node = nodes[i];

						// Only brute force is available in the free version
						for (int j=0;j<nodes.Length;j++) {
							if (i == j) continue;

							PointNode other = nodes[j];

							float dist;
							if (IsValidConnection (node,other,out dist)) {
								connections.Add (other);
								/** \todo Is this equal to .costMagnitude */
								costs.Add ((uint)Mathf.RoundToInt (dist*Int3.FloatPrecision));
							}
						}
					node.connections = connections.ToArray();
					node.connectionCosts = costs.ToArray();
				}
			}
		}

		/** Returns if the connection between \a a and \a b is valid.
		 * Checks for obstructions using raycasts (if enabled) and checks for height differences.\n
		 * As a bonus, it outputs the distance between the nodes too if the connection is valid
		 */
		public virtual bool IsValidConnection (GraphNode a, GraphNode b, out float dist) {
			dist = 0;

			if (!a.Walkable || !b.Walkable) return false;

			var dir = (Vector3)(a.position-b.position);

			if (
				(!Mathf.Approximately (limits.x,0) && Mathf.Abs (dir.x) > limits.x) ||
				(!Mathf.Approximately (limits.y,0) && Mathf.Abs (dir.y) > limits.y) ||
				(!Mathf.Approximately (limits.z,0) && Mathf.Abs (dir.z) > limits.z))
			{
				return false;
			}

			dist = dir.magnitude;
			if (maxDistance == 0 || dist < maxDistance) {

				if (raycast) {

					var ray = new Ray ((Vector3)a.position,(Vector3)(b.position-a.position));
					var invertRay = new Ray ((Vector3)b.position,(Vector3)(a.position-b.position));

					if (use2DPhysics) {
						if (thickRaycast) {
							if (!Physics2D.CircleCast (ray.origin, thickRaycastRadius, ray.direction, dist,mask) &&
							    !Physics2D.CircleCast (invertRay.origin,thickRaycastRadius, invertRay.direction, dist,mask)) {
								return true;
							}
						} else {
							if (!Physics2D.Linecast ((Vector2)(Vector3)a.position,(Vector2)(Vector3)b.position,mask) &&
							    !Physics2D.Linecast ((Vector2)(Vector3)b.position,(Vector2)(Vector3)a.position,mask)) {
								return true;
							}
						}
					} else {
						if (thickRaycast) {
							if (!Physics.SphereCast (ray,thickRaycastRadius,dist,mask) && !Physics.SphereCast (invertRay,thickRaycastRadius,dist,mask)) {
								return true;
							}
						} else {
							if (!Physics.Linecast ((Vector3)a.position,(Vector3)b.position,mask) && !Physics.Linecast ((Vector3)b.position,(Vector3)a.position,mask)) {
								return true;
							}
						}
					}
				} else {
					return true;
				}
			}
			return false;
		}


		public override void PostDeserialization () {
			RebuildNodeLookup ();
		}

		public override void RelocateNodes (Matrix4x4 oldMatrix, Matrix4x4 newMatrix) {
			base.RelocateNodes (oldMatrix, newMatrix);
			RebuildNodeLookup ();
		}


#if ASTAR_NO_JSON
		public override void SerializeSettings ( GraphSerializationContext ctx ) {

			base.SerializeSettings (ctx);

			ctx.SerializeUnityObject (root);
			ctx.writer.Write (searchTag ?? "");
			ctx.writer.Write (maxDistance);
			ctx.SerializeVector3 (limits);
			ctx.writer.Write (raycast);
			ctx.writer.Write (use2DPhysics);
			ctx.writer.Write (thickRaycast);
			ctx.writer.Write (thickRaycastRadius);
			ctx.writer.Write (recursive);
			ctx.writer.Write (autoLinkNodes);
			ctx.writer.Write ((int)mask);
		}

		public override void DeserializeSettings ( GraphSerializationContext ctx ) {

			base.DeserializeSettings (ctx);

			root = ctx.DeserializeUnityObject () as Transform;
			searchTag = ctx.reader.ReadString ();
			maxDistance = ctx.reader.ReadSingle ();
			limits = ctx.DeserializeVector3 ();
			raycast = ctx.reader.ReadBoolean ();
			use2DPhysics = ctx.reader.ReadBoolean ();
			thickRaycast = ctx.reader.ReadBoolean ();
			thickRaycastRadius = ctx.reader.ReadSingle ();
			recursive = ctx.reader.ReadBoolean ();
			autoLinkNodes = ctx.reader.ReadBoolean ();
			mask = (LayerMask)ctx.reader.ReadInt32 ();
		}
#endif

		public override void SerializeExtraInfo (GraphSerializationContext ctx) {
			// Serialize node data

			if (nodes == null) ctx.writer.Write (-1);

			// Length prefixed array of nodes
			ctx.writer.Write (nodeCount);
			for (int i=0;i<nodeCount;i++) {
				// -1 indicates a null field
				if (nodes[i] == null) ctx.writer.Write (-1);
				else {
					ctx.writer.Write (0);
					nodes[i].SerializeNode(ctx);
				}
			}
		}

		public override void DeserializeExtraInfo (GraphSerializationContext ctx) {
			int count = ctx.reader.ReadInt32();
			if (count == -1) {
				nodes = null;
				return;
			}

			nodes = new PointNode[count];
			nodeCount = count;

			for (int i=0;i<nodes.Length;i++) {
				if (ctx.reader.ReadInt32() == -1) continue;
				nodes[i] = new PointNode(active);
				nodes[i].DeserializeNode(ctx);
			}
		}
	}
}
