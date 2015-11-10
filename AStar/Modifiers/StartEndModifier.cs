using UnityEngine;
using Pathfinding;

namespace Pathfinding {
	[System.Serializable]
	/** Adjusts start and end points of a path.
	 * \ingroup modifiers
	 */
	public class StartEndModifier : PathModifier {
		
		public override ModifierData input {
			get { return ModifierData.Vector; }
		}
		
		public override ModifierData output {
			get { return (addPoints ? ModifierData.None : ModifierData.StrictVectorPath) | ModifierData.VectorPath; }
		}
		
		/** Add points to the path instead of replacing. */
		public bool addPoints;
		public Exactness exactStartPoint = Exactness.ClosestOnNode;
		public Exactness exactEndPoint = Exactness.ClosestOnNode;
		
		/** Sets where the start and end points of a path should be placed */
		public enum Exactness {
			SnapToNode,		/**< The point is snapped to the first/last node in the path*/
			Original,		/**< The point is set to the exact point which was passed when calling the pathfinding */
			Interpolate,	/**< The point is set to the closest point on the line between either the two first points or the two last points */
			ClosestOnNode	/**< The point is set to the closest point on the node. Note that for some node types (point nodes) the "closest point" is the node's position which makes this identical to Exactness.SnapToNode */
		}
		
		public bool useRaycasting;
		public LayerMask mask = -1;
		
		public bool useGraphRaycasting;
		
		public override void Apply (Path _p, ModifierData source) {
			var p = _p as ABPath;
			
			//Only for ABPaths
			if (p == null) return;
			
			if (p.vectorPath.Count == 0) {
				return;
			}

			if (p.vectorPath.Count == 1 && !addPoints) {
				// Duplicate first point
				p.vectorPath.Add (p.vectorPath[0]);
			}
			
			Vector3 pStart = Vector3.zero;
			Vector3 pEnd = Vector3.zero;
			
			switch(exactStartPoint) {
			case Exactness.Original:
				pStart = GetClampedPoint ((Vector3)p.path[0].position, p.originalStartPoint, p.path[0]);
				break;
			case Exactness.ClosestOnNode:
				pStart = GetClampedPoint ((Vector3)p.path[0].position, p.startPoint, p.path[0]);
				break;
			case Exactness.SnapToNode:
				pStart = (Vector3)p.path[0].position;
				break;
			case Exactness.Interpolate:
				pStart = GetClampedPoint ((Vector3)p.path[0].position, p.originalStartPoint, p.path[0]);
				pStart = AstarMath.NearestPointStrict ((Vector3)p.path[0].position,(Vector3)p.path[1>=p.path.Count?0:1].position,pStart);
				break;
			}
			
			switch(exactEndPoint) {
			case Exactness.Original:
				pEnd   = GetClampedPoint ((Vector3)p.path[p.path.Count-1].position, p.originalEndPoint, p.path[p.path.Count-1]);
				break;
			case Exactness.ClosestOnNode:
				pEnd = GetClampedPoint ((Vector3)p.path[p.path.Count-1].position, p.endPoint, p.path[p.path.Count-1]);
				break;
			case Exactness.SnapToNode:
				pEnd = (Vector3)p.path[p.path.Count-1].position;
				break;
			case Exactness.Interpolate:
				pEnd   = GetClampedPoint ((Vector3)p.path[p.path.Count-1].position, p.originalEndPoint, p.path[p.path.Count-1]);
				
				pEnd = AstarMath.NearestPointStrict ((Vector3)p.path[p.path.Count-1].position,(Vector3)p.path[p.path.Count-2<0?0:p.path.Count-2].position,pEnd);
				break;
			}
			
			if (!addPoints) {
				p.vectorPath[0] = pStart;
				p.vectorPath[p.vectorPath.Count-1] = pEnd;
			} else {
				if (exactStartPoint != Exactness.SnapToNode) {
					p.vectorPath.Insert (0,pStart);
				}
				
				if (exactEndPoint != Exactness.SnapToNode) {
					p.vectorPath.Add (pEnd);
				}
			}
			
		}
		
		public Vector3 GetClampedPoint (Vector3 from, Vector3 to, GraphNode hint) {
			Vector3 minPoint = to;
			
			if (useRaycasting) {
				RaycastHit hit;
				if (Physics.Linecast (from,to,out hit,mask)) {
					minPoint = hit.point;
				}
			}
			
			if (useGraphRaycasting && hint != null) {
				
				NavGraph graph = AstarData.GetGraph (hint);
				
				if (graph != null) {
					var rayGraph = graph as IRaycastableGraph;
					
					if (rayGraph != null) {
						GraphHitInfo hit;
						
						if (rayGraph.Linecast (from,minPoint, hint, out hit)) {
							minPoint = hit.point;
						}
					}
				}
			}
			
			return minPoint;
		}
		
	}
}