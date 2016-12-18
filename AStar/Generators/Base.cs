using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Util;
using Pathfinding.Serialization;

namespace Pathfinding {
	/**  Base class for all graphs */
	public abstract class NavGraph {
		/** Reference to the AstarPath object in the scene.
		 * Might not be entirely safe to use, it's better to use AstarPath.active
		 */
		public AstarPath active;

		/** Used as an ID of the graph, considered to be unique.
		 * \note This is Pathfinding.Util.Guid not System.Guid. A replacement for System.Guid was coded for better compatibility with iOS
		 */
		[JsonMember]
		public Guid guid;

		/** Default penalty to apply to all nodes */
		[JsonMember]
		public uint initialPenalty;

		/**  Is the graph open in the editor */
		[JsonMember]
		public bool open;

		/** Index of the graph, used for identification purposes */
		public uint graphIndex;

		/** Name of the graph.
		 * Can be set in the unity editor
		 */
		[JsonMember]
		public string name;

		[JsonMember]
		public bool drawGizmos = true;

		/** Used in the editor to check if the info screen is open.
		 * Should be inside UNITY_EDITOR only \#ifs but just in case anyone tries to serialize a NavGraph instance using Unity, I have left it like this as it would otherwise cause a crash when building.
		 * Version 3.0.8.1 was released because of this bug only
		 */
		[JsonMember]
		public bool infoScreenOpen;

		/** Count nodes in the graph.
		 * Note that this is, unless the graph type has overriden it, an O(n) operation.
		 *
		 * \todo GridGraph should override this
		 */
		public virtual int CountNodes () {
			int count = 0;
			GraphNodeDelegateCancelable del = node => {
				count++;
				return true;
			};

			GetNodes(del);
			return count;
		}

		/** Calls a delegate with all nodes in the graph.
		 * This is the primary way of "looping" through all nodes in a graph.
		 *
		 * This function should not change anything in the graph structure.
		 *
		 * \code
		 * myGraph.GetNodes ((node) => {
		 *     Debug.Log ("I found a node at position " + (Vector3)node.Position);
		 *     return true;
		 * });
		 * \endcode
		 */
		public abstract void GetNodes (GraphNodeDelegateCancelable del);

		/** A matrix for translating/rotating/scaling the graph.
		 * Not all graph generators sets this variable though.
		 *
		 * \note Do not set directly, use SetMatrix
		 *
		 * \note This value is not serialized. It is expected that graphs regenerate this
		 * field after deserialization has completed.
		 */
		public Matrix4x4 matrix = Matrix4x4.identity;

		/** Inverse of \a matrix.
		 *
		 * \note Do not set directly, use SetMatrix
		 *
		 * \see matrix
		 */
		public Matrix4x4 inverseMatrix = Matrix4x4.identity;

		/** Use to set both matrix and inverseMatrix at the same time */
		public void SetMatrix (Matrix4x4 m) {
			matrix = m;
			inverseMatrix = m.inverse;
		}

		/** Relocates the nodes in this graph.
		 * Assumes the nodes are already transformed using the "oldMatrix", then transforms them
		 * such that it will look like they have only been transformed using the "newMatrix".
		 * The "oldMatrix" is not required by all implementations of this function though (e.g the NavMesh generator).
		 *
		 * The matrix the graph is transformed with is typically stored in the #matrix field, so the typical usage for this method is
		 * \code
		 * var myNewMatrix = Matrix4x4.TRS (...);
		 * myGraph.RelocateNodes (myGraph.matrix, myNewMatrix);
		 * \endcode
		 *
		 * So for example if you want to move all your nodes in e.g a point graph 10 units along the X axis from the initial position
		 * \code
		 * var graph = AstarPath.astarData.pointGraph;
		 * var m = Matrix4x4.TRS (new Vector3(10,0,0), Quaternion.identity, Vector3.one);
		 * graph.RelocateNodes (graph.matrix, m);
		 * \endcode
		 *
		 * \note For grid graphs it is recommended to use the helper method RelocateNodes which takes parameters for
		 * center and nodeSize (and additional parameters) instead since it is both easier to use and is less likely
		 * to mess up pathfinding.
		 *
		 * \warning This method is lossy, so calling it many times may cause node positions to lose precision.
		 * For example if you set the scale to 0 in one call, and then to 1 in the next call, it will not be able to
		 * recover the correct positions since when the scale was 0, all nodes were scaled/moved to the same point.
		 * The same thing happens for other - less extreme - values as well, but to a lesser degree.
		 *
		 * \version Prior to version 3.6.1 the oldMatrix and newMatrix parameters were reversed by mistake.
		 */
		public virtual void RelocateNodes (Matrix4x4 oldMatrix, Matrix4x4 newMatrix) {
			Matrix4x4 inv = oldMatrix.inverse;

			Matrix4x4 m = newMatrix * inv;

			GetNodes(delegate(GraphNode node) {
				//Vector3 tmp = inv.MultiplyPoint3x4 ((Vector3)nodes[i].position);
				node.position = ((Int3)m.MultiplyPoint((Vector3)node.position));
				return true;
			});
			SetMatrix(newMatrix);
		}

		/** Returns the nearest node to a position using the default NNConstraint.
		 * \param position The position to try to find a close node to
		 * \see Pathfinding.NNConstraint.None
		 */
		public NNInfo GetNearest (Vector3 position) {
			return GetNearest(position, NNConstraint.None);
		}

		/** Returns the nearest node to a position using the specified NNConstraint.
		 * \param position The position to try to find a close node to
		 * \param constraint Can for example tell the function to try to return a walkable node. If you do not get a good node back, consider calling GetNearestForce. */
		public NNInfo GetNearest (Vector3 position, NNConstraint constraint) {
			return GetNearest(position, constraint, null);
		}

		/** Returns the nearest node to a position using the specified NNConstraint.
		 * \param position The position to try to find a close node to
		 * \param hint Can be passed to enable some graph generators to find the nearest node faster.
		 * \param constraint Can for example tell the function to try to return a walkable node. If you do not get a good node back, consider calling GetNearestForce. */
		public virtual NNInfo GetNearest (Vector3 position, NNConstraint constraint, GraphNode hint) {
			// This is a default implementation and it is pretty slow
			// Graphs usually override this to provide faster and more specialised implementations

			float maxDistSqr = constraint.constrainDistance ? AstarPath.active.maxNearestNodeDistanceSqr : float.PositiveInfinity;

			float minDist = float.PositiveInfinity;
			GraphNode minNode = null;

			float minConstDist = float.PositiveInfinity;
			GraphNode minConstNode = null;

			// Loop through all nodes and find the closest suitable node
			GetNodes(node => {
				float dist = (position-(Vector3)node.position).sqrMagnitude;

				if (dist < minDist) {
					minDist = dist;
					minNode = node;
				}

				if (dist < minConstDist && dist < maxDistSqr && constraint.Suitable(node)) {
					minConstDist = dist;
					minConstNode = node;
				}
				return true;
			});

			var nnInfo = new NNInfo(minNode);

			nnInfo.constrainedNode = minConstNode;

			if (minConstNode != null) {
				nnInfo.constClampedPosition = (Vector3)minConstNode.position;
			} else if (minNode != null) {
				nnInfo.constrainedNode = minNode;
				nnInfo.constClampedPosition = (Vector3)minNode.position;
			}

			return nnInfo;
		}

		/**
		 * Returns the nearest node to a position using the specified \link Pathfinding.NNConstraint constraint \endlink.
		 * \returns an NNInfo. This method will only return an empty NNInfo if there are no nodes which comply with the specified constraint.
		 */
		public virtual NNInfo GetNearestForce (Vector3 position, NNConstraint constraint) {
			return GetNearest(position, constraint);
		}

		/**
		 * This will be called on the same time as Awake on the gameObject which the AstarPath script is attached to. (remember, not in the editor)
		 * Use this for any initialization code which can't be placed in Scan
		 */
		public virtual void Awake () {
		}

		/** Function for cleaning up references.
		 * This will be called on the same time as OnDisable on the gameObject which the AstarPath script is attached to (remember, not in the editor).
		 * Use for any cleanup code such as cleaning up static variables which otherwise might prevent resources from being collected.
		 * Use by creating a function overriding this one in a graph class, but always call base.OnDestroy () in that function.
		 * All nodes should be destroyed in this function otherwise a memory leak will arise.
		 */
		public virtual void OnDestroy () {
			//Destroy all nodes
			GetNodes(delegate(GraphNode node) {
				node.Destroy();
				return true;
			});
		}

		/*
		 * Consider using AstarPath.Scan () instead since this function might screw things up if there is more than one graph.
		 * This function does not perform all necessary postprocessing for the graph to work with pathfinding (e.g flood fill).
		 * See the source of the AstarPath.Scan function to see how it can be used.
		 * In almost all cases you should use AstarPath.Scan instead.
		 */
		public void ScanGraph () {
			if (AstarPath.OnPreScan != null) {
				AstarPath.OnPreScan(AstarPath.active);
			}

			if (AstarPath.OnGraphPreScan != null) {
				AstarPath.OnGraphPreScan(this);
			}

			ScanInternal();

			if (AstarPath.OnGraphPostScan != null) {
				AstarPath.OnGraphPostScan(this);
			}

			if (AstarPath.OnPostScan != null) {
				AstarPath.OnPostScan(AstarPath.active);
			}
		}

		[System.Obsolete("Please use AstarPath.active.Scan or if you really want this.ScanInternal which has the same functionality as this method had")]
		public void Scan () {
			throw new System.Exception("This method is deprecated. Please use AstarPath.active.Scan or if you really want this.ScanInternal which has the same functionality as this method had.");
		}

		/** Internal method for scanning graphs */
		public void ScanInternal () {
			ScanInternal(null);
		}

		/**
		 * Scans the graph, called from AstarPath.ScanLoop.
		 * Override this function to implement custom scanning logic
		 * The statusCallback may be optionally called to show progress info in the editor
		 */
		public abstract void ScanInternal (OnScanStatus statusCallback);

		/* Color to use for gizmos.
		 * Returns a color to be used for the specified node with the current debug settings (editor only).
		 *
		 * \version Since 3.6.1 this method will not handle null nodes
		 */
		public virtual Color NodeColor (GraphNode node, PathHandler data) {
			Color c = AstarColor.NodeConnection;

			switch (AstarPath.active.debugMode) {
			case GraphDebugMode.Areas:
				c = AstarColor.GetAreaColor(node.Area);
				break;
			case GraphDebugMode.Penalty:
				c = Color.Lerp(AstarColor.ConnectionLowLerp, AstarColor.ConnectionHighLerp, ((float)node.Penalty-AstarPath.active.debugFloor) / (AstarPath.active.debugRoof-AstarPath.active.debugFloor));
				break;
			case GraphDebugMode.Tags:
				c = AstarColor.GetAreaColor(node.Tag);
				break;
			default:
				if (data == null) return AstarColor.NodeConnection;

				PathNode nodeR = data.GetPathNode(node);

				switch (AstarPath.active.debugMode) {
				case GraphDebugMode.G:
					c = Color.Lerp(AstarColor.ConnectionLowLerp, AstarColor.ConnectionHighLerp, ((float)nodeR.G-AstarPath.active.debugFloor) / (AstarPath.active.debugRoof-AstarPath.active.debugFloor));
					break;
				case GraphDebugMode.H:
					c = Color.Lerp(AstarColor.ConnectionLowLerp, AstarColor.ConnectionHighLerp, ((float)nodeR.H-AstarPath.active.debugFloor) / (AstarPath.active.debugRoof-AstarPath.active.debugFloor));
					break;
				case GraphDebugMode.F:
					c = Color.Lerp(AstarColor.ConnectionLowLerp, AstarColor.ConnectionHighLerp, ((float)nodeR.F-AstarPath.active.debugFloor) / (AstarPath.active.debugRoof-AstarPath.active.debugFloor));
					break;
				}
				break;
			}

			c.a *= 0.5F;
			return c;
		}

		/** Serializes graph type specific node data.
		 * This function can be overriden to serialize extra node information (or graph information for that matter)
		 * which cannot be serialized using the standard serialization.
		 * Serialize the data in any way you want and return a byte array.
		 * When loading, the exact same byte array will be passed to the DeserializeExtraInfo function.\n
		 * These functions will only be called if node serialization is enabled.\n
		 */
		public virtual void SerializeExtraInfo (GraphSerializationContext ctx) {
		}

		/** Deserializes graph type specific node data.
		 * \see SerializeExtraInfo
		 */
		public virtual void DeserializeExtraInfo (GraphSerializationContext ctx) {
		}

		/** Called after all deserialization has been done for all graphs.
		 * Can be used to set up more graph data which is not serialized
		 */
		public virtual void PostDeserialization () {
		}

		/** An old format for serializing settings.
		 * \deprecated This is deprecated now, but the deserialization code is kept to
		 * avoid loosing data when upgrading from older versions.
		 */
		public virtual void DeserializeSettingsCompatibility (GraphSerializationContext ctx) {
			guid = new Guid(ctx.reader.ReadBytes(16));
			initialPenalty = ctx.reader.ReadUInt32();
			open = ctx.reader.ReadBoolean();
			name = ctx.reader.ReadString();
			drawGizmos = ctx.reader.ReadBoolean();
			infoScreenOpen = ctx.reader.ReadBoolean();

			for (int i = 0; i < 4; i++) {
				Vector4 row = Vector4.zero;
				for (int j = 0; j < 4; j++) {
					row[j] = ctx.reader.ReadSingle();
				}
				matrix.SetRow(i, row);
			}
		}

		/** Returns if the node is in the search tree of the path.
		 * Only guaranteed to be correct if \a path is the latest path calculated.
		 * Use for gizmo drawing only.
		 */
		public static bool InSearchTree (GraphNode node, Path path) {
			if (path == null || path.pathHandler == null) return true;
			PathNode nodeR = path.pathHandler.GetPathNode(node);
			return nodeR.pathID == path.pathID;
		}

		/** Draw gizmos for the graph */
		public virtual void OnDrawGizmos (bool drawNodes) {
			if (!drawNodes) {
				return;
			}

			// This is the relatively slow default implementation
			// subclasses of the base graph class may override
			// this method to draw gizmos in a more optimized way

			PathHandler data = AstarPath.active.debugPathData;
			GraphNode node = null;

			// Use this delegate to draw connections
			// from the #node variable to #otherNode
			GraphNodeDelegate drawConnection = otherNode => Gizmos.DrawLine((Vector3)node.position, (Vector3)otherNode.position);

			GetNodes(_node => {
				// Set the #node variable so that #drawConnection can use it
				node = _node;

				Gizmos.color = NodeColor(node, AstarPath.active.debugPathData);
				if (AstarPath.active.showSearchTree && !InSearchTree(node, AstarPath.active.debugPath)) return true;

				PathNode nodeR = data != null ? data.GetPathNode(node) : null;
				if (AstarPath.active.showSearchTree && nodeR != null && nodeR.parent != null) {
					Gizmos.DrawLine((Vector3)node.position, (Vector3)nodeR.parent.node.position);
				} else {
					node.GetConnections(drawConnection);
				}
				return true;
			});
		}

		/** Called when temporary meshes used in OnDrawGizmos need to be unloaded to prevent memory leaks */
		internal virtual void UnloadGizmoMeshes () {
		}
	}


	/** Handles collision checking for graphs.
	 * Mostly used by grid based graphs */
	[System.Serializable]
	public class GraphCollision {
		/** Collision shape to use.
		 * Pathfinding.ColliderType */
		public ColliderType type = ColliderType.Capsule;

		/** Diameter of capsule or sphere when checking for collision.
		 * 1 equals \link Pathfinding.GridGraph.nodeSize nodeSize \endlink.
		 * If #type is set to Ray, this does not affect anything */
		public float diameter = 1F;

		/** Height of capsule or length of ray when checking for collision.
		 * If #type is set to Sphere, this does not affect anything
		 */
		public float height = 2F;
		public float collisionOffset;

		/** Direction of the ray when checking for collision.
		 * If #type is not Ray, this does not affect anything
		 * \note This variable is not used currently, it does not affect anything
		 */
		public RayDirection rayDirection = RayDirection.Both;

		/** Layer mask to use for collision check.
		 * This should only contain layers of objects defined as obstacles */
		public LayerMask mask;

		/** Layer mask to use for height check. */
		public LayerMask heightMask = -1;

		/** The height to check from when checking height */
		public float fromHeight = 100;

		/** Toggles thick raycast */
		public bool thickRaycast;

		/** Diameter of the thick raycast in nodes.
		 * 1 equals \link Pathfinding.GridGraph.nodeSize nodeSize \endlink */
		public float thickRaycastDiameter = 1;

		/** Make nodes unwalkable when no ground was found with the height raycast. If height raycast is turned off, this doesn't affect anything. */
		public bool unwalkableWhenNoGround = true;

		/** Use Unity 2D Physics API.
		 * \see http://docs.unity3d.com/ScriptReference/Physics2D.html
		 */
		public bool use2D;

		/** Toggle collision check */
		public bool collisionCheck = true;

		/** Toggle height check. If false, the grid will be flat */
		public bool heightCheck = true;

		/** Direction to use as \a UP.
		 * \see Initialize */
		public Vector3 up;

		/** #up * #height.
		 * \see Initialize */
		private Vector3 upheight;

		/** #diameter * scale * 0.5.
		 * Where \a scale usually is \link Pathfinding.GridGraph.nodeSize nodeSize \endlink
		 * \see Initialize */
		private float finalRadius;

		/** #thickRaycastDiameter * scale * 0.5. Where \a scale usually is \link Pathfinding.GridGraph.nodeSize nodeSize \endlink \see Initialize */
		private float finalRaycastRadius;

		/** Offset to apply after each raycast to make sure we don't hit the same point again in CheckHeightAll */
		public const float RaycastErrorMargin = 0.005F;

		/** Sets up several variables using the specified matrix and scale.
		 * \see GraphCollision.up
		 * \see GraphCollision.upheight
		 * \see GraphCollision.finalRadius
		 * \see GraphCollision.finalRaycastRadius
		 */
		public void Initialize (Matrix4x4 matrix, float scale) {
			up = matrix.MultiplyVector(Vector3.up);
			upheight = up*height;
			finalRadius = diameter*scale*0.5F;
			finalRaycastRadius = thickRaycastDiameter*scale*0.5F;
		}

		/** Returns if the position is obstructed.
		 * If #collisionCheck is false, this will always return true.\n
		 */
		public bool Check (Vector3 position) {
			if (!collisionCheck) {
				return true;
			}

			if (use2D) {
				switch (type) {
				case ColliderType.Capsule:
					throw new System.Exception("Capsule mode cannot be used with 2D since capsules don't exist in 2D. Please change the Physics Testing -> Collider Type setting.");
				case ColliderType.Sphere:
					return Physics2D.OverlapCircle(position, finalRadius, mask) == null;
				default:
					return Physics2D.OverlapPoint(position, mask) == null;
				}
			}

			position += up*collisionOffset;
			switch (type) {
			case ColliderType.Capsule:
				return !Physics.CheckCapsule(position, position+upheight, finalRadius, mask);
			case ColliderType.Sphere:
				return !Physics.CheckSphere(position, finalRadius, mask);
			default:
				switch (rayDirection) {
				case RayDirection.Both:
					return !Physics.Raycast(position, up, height, mask) && !Physics.Raycast(position+upheight, -up, height, mask);
				case RayDirection.Up:
					return !Physics.Raycast(position, up, height, mask);
				default:
					return !Physics.Raycast(position+upheight, -up, height, mask);
				}
			}
		}

		/** Returns the position with the correct height. If #heightCheck is false, this will return \a position.\n */
		public Vector3 CheckHeight (Vector3 position) {
			RaycastHit hit;
			bool walkable;

			return CheckHeight(position, out hit, out walkable);
		}

		/** Returns the position with the correct height.
		 * If #heightCheck is false, this will return \a position.\n
		 * \a walkable will be set to false if nothing was hit.
		 * The ray will check a tiny bit further than to the grids base to avoid floating point errors when the ground is exactly at the base of the grid */
		public Vector3 CheckHeight (Vector3 position, out RaycastHit hit, out bool walkable) {
			walkable = true;

			if (!heightCheck || use2D) {
				hit = new RaycastHit();
				return position;
			}

			if (thickRaycast) {
				var ray = new Ray(position+up*fromHeight, -up);
				if (Physics.SphereCast(ray, finalRaycastRadius, out hit, fromHeight+0.005F, heightMask)) {
					return VectorMath.ClosestPointOnLine(ray.origin, ray.origin+ray.direction, hit.point);
				}

				walkable &= !unwalkableWhenNoGround;
			} else {
				// Cast a ray from above downwards to try to find the ground
				if (Physics.Raycast(position+up*fromHeight, -up, out hit, fromHeight+0.005F, heightMask)) {
					return hit.point;
				}

				walkable &= !unwalkableWhenNoGround;
			}
			return position;
		}

		/** Same as #CheckHeight, except that the raycast will always start exactly at \a origin.
		 * \a walkable will be set to false if nothing was hit.
		 * The ray will check a tiny bit further than to the grids base to avoid floating point errors when the ground is exactly at the base of the grid */
		public Vector3 Raycast (Vector3 origin, out RaycastHit hit, out bool walkable) {
			walkable = true;

			if (!heightCheck || use2D) {
				hit = new RaycastHit();
				return origin -up*fromHeight;
			}

			if (thickRaycast) {
				var ray = new Ray(origin, -up);
				if (Physics.SphereCast(ray, finalRaycastRadius, out hit, fromHeight+0.005F, heightMask)) {
					return VectorMath.ClosestPointOnLine(ray.origin, ray.origin+ray.direction, hit.point);
				}

				walkable &= !unwalkableWhenNoGround;
			} else {
				if (Physics.Raycast(origin, -up, out hit, fromHeight+0.005F, heightMask)) {
					return hit.point;
				}

				walkable &= !unwalkableWhenNoGround;
			}
			return origin -up*fromHeight;
		}

		/** Returns all hits when checking height for \a position.
		 * \warning Does not work well with thick raycast, will only return an object a single time
		 */
		public RaycastHit[] CheckHeightAll (Vector3 position) {
			if (!heightCheck || use2D) {
				var hit = new RaycastHit();
				hit.point = position;
				hit.distance = 0;
				return new [] { hit };
			}

			if (thickRaycast) {
				return new RaycastHit[0];
			}

			var hits = new List<RaycastHit>();

			bool walkable;
			Vector3 cpos = position + up*fromHeight;
			Vector3 prevHit = Vector3.zero;

			int numberSame = 0;
			while (true) {
				RaycastHit hit;
				Raycast(cpos, out hit, out walkable);
				if (hit.transform == null) { //Raycast did not hit anything
					break;
				}

				//Make sure we didn't hit the same position
				if (hit.point != prevHit || hits.Count == 0) {
					cpos = hit.point - up*RaycastErrorMargin;
					prevHit = hit.point;
					numberSame = 0;

					hits.Add(hit);
				} else {
					cpos -= up*0.001F;
					numberSame++;
					//Check if we are hitting the same position all the time, even though we are decrementing the cpos variable
					if (numberSame > 10) {
						Debug.LogError("Infinite Loop when raycasting. Please report this error (arongranberg.com)\n"+cpos+" : "+prevHit);
						break;
					}
				}
			}
			return hits.ToArray();
		}

		public void DeserializeSettingsCompatibility (GraphSerializationContext ctx) {
			type = (ColliderType)ctx.reader.ReadInt32();
			diameter = ctx.reader.ReadSingle();
			height = ctx.reader.ReadSingle();
			collisionOffset = ctx.reader.ReadSingle();
			rayDirection = (RayDirection)ctx.reader.ReadInt32();
			mask = (LayerMask)ctx.reader.ReadInt32();
			heightMask = (LayerMask)ctx.reader.ReadInt32();
			fromHeight = ctx.reader.ReadSingle();
			thickRaycast = ctx.reader.ReadBoolean();
			thickRaycastDiameter = ctx.reader.ReadSingle();

			unwalkableWhenNoGround = ctx.reader.ReadBoolean();
			use2D = ctx.reader.ReadBoolean();
			collisionCheck = ctx.reader.ReadBoolean();
			heightCheck = ctx.reader.ReadBoolean();
		}
	}


	/** Determines collision check shape */
	public enum ColliderType {
		Sphere,     /**< Uses a Sphere, Physics.CheckSphere */
		Capsule,    /**< Uses a Capsule, Physics.CheckCapsule */
		Ray         /**< Uses a Ray, Physics.Linecast */
	}

	/** Determines collision check ray direction */
	public enum RayDirection {
		Up,     /**< Casts the ray from the bottom upwards */
		Down,   /**< Casts the ray from the top downwards */
		Both    /**< Casts two rays in both directions */
	}
}
