using UnityEngine;
using System.Collections.Generic;
using Pathfinding;

namespace Pathfinding {
	/** Simplifies a path using raycasting.
	 * \ingroup modifiers
	 * This modifier will try to remove as many nodes as possible from the path using raycasting (linecasting) to validate the node removal.
	 * Either graph raycasts or Physics.Raycast */
	[AddComponentMenu ("Pathfinding/Modifiers/Raycast Simplifier")]
	[System.Serializable]
	public class RaycastModifier : MonoModifier {
		
	#if UNITY_EDITOR
		[UnityEditor.MenuItem ("CONTEXT/Seeker/Add Raycast Simplifier Modifier")]
		public static void AddComp (UnityEditor.MenuCommand command) {
			(command.context as Component).gameObject.AddComponent (typeof(RaycastModifier));
		}
	#endif
		
		public override ModifierData input {
			get { return ModifierData.VectorPath | ModifierData.StrictVectorPath; }
		}
		
		public override ModifierData output {
			get { return ModifierData.VectorPath; }
		}
	
		[HideInInspector]
		public bool useRaycasting = true;
		[HideInInspector]
		public LayerMask mask = -1;
		[HideInInspector]
		public bool thickRaycast;
		[HideInInspector]
		public float thickRaycastRadius;
		[HideInInspector]
		public Vector3 raycastOffset = Vector3.zero;
		
		/* Use the exact points used to query the path. If false, the start and end points will be snapped to the node positions.*/
		//public bool exactStartAndEnd = true;
		
		/* Ignore exact start and end points clamped by other modifiers. Other modifiers which modify the start and end points include for example the StartEndModifier. If enabled this modifier will ignore anything that modifier does when calculating the simplification.*/
		//public bool overrideClampedExacts = false;
		
		[HideInInspector]
		public bool subdivideEveryIter;
		
		public int iterations = 2;
		
		/** Use raycasting on the graphs. Only currently works with GridGraph and NavmeshGraph and RecastGraph. \astarpro */
		[HideInInspector]
		public bool useGraphRaycasting;
		
		/** To avoid too many memory allocations. An array is kept between the checks and filled in with the positions instead of allocating a new one every time.*/
		private static List<Vector3> nodes;
		
		public override void Apply (Path p, ModifierData source) {
			//System.DateTime startTime = System.DateTime.UtcNow;
			
			if (iterations <= 0) {
				return;
			}
			
			if (nodes == null) {
				nodes = new List<Vector3> (p.vectorPath.Count);
			} else {
				nodes.Clear ();
			}
			
			nodes.AddRange (p.vectorPath);
			// = new List<Vector3> (p.vectorPath);
			
			for (int it=0;it<iterations;it++) {
				
				if (subdivideEveryIter && it != 0) {
					
					if (nodes.Capacity < nodes.Count*3) {
						nodes.Capacity = nodes.Count*3;
					}
					
					int preLength = nodes.Count;
					
					for (int j=0;j<preLength-1;j++) {
						nodes.Add (Vector3.zero);
						nodes.Add (Vector3.zero);
					}
					
					for (int j=preLength-1;j > 0;j--) {
						
						Vector3 p1 = nodes[j];
						Vector3 p2 = nodes[j+1];
						
						nodes[j*3] = nodes[j];
						
						if (j != preLength-1) {
							nodes[j*3+1] = Vector3.Lerp (p1,p2,0.33F);
							nodes[j*3+2] = Vector3.Lerp (p1,p2,0.66F);
						}
					}
				}
				
				int i = 0;
				while (i < nodes.Count-2) {
					
					Vector3 start = nodes[i];
					Vector3 end = nodes[i+2];
					
					var watch = System.Diagnostics.Stopwatch.StartNew();
					
					if (ValidateLine (null,null,start,end)) {
						nodes.RemoveAt (i+1);
					} else {
						i++;
					}
					
					watch.Stop ();
				}
				
			}
			
			p.vectorPath.Clear ();
			p.vectorPath.AddRange (nodes);
		}
		
		/** Check if a straight path between v1 and v2 is valid */
		public bool ValidateLine (GraphNode n1, GraphNode n2, Vector3 v1, Vector3 v2) {
			
			if (useRaycasting) {
				// Use raycasting to check if a straight path between v1 and v2 is valid
				if (thickRaycast && thickRaycastRadius > 0) {
					RaycastHit hit;
					if (Physics.SphereCast (v1+raycastOffset, thickRaycastRadius,v2-v1,out hit, (v2-v1).magnitude,mask)) {
						return false;
					}
				} else {
					RaycastHit hit;
					if (Physics.Linecast (v1+raycastOffset,v2+raycastOffset,out hit, mask)) {
						return false;
					}
				}
			}
			
			if (useGraphRaycasting && n1 == null) {
				n1 = AstarPath.active.GetNearest (v1).node;
				n2 = AstarPath.active.GetNearest (v2).node;
			}
			
			if (useGraphRaycasting && n1 != null && n2 != null) {
				// Use graph raycasting to check if a straight path between v1 and v2 is valid
				NavGraph graph = AstarData.GetGraph (n1);
				NavGraph graph2 = AstarData.GetGraph (n2);
				
				if (graph != graph2) {
					return false;
				}
				
				if (graph != null) {
					var rayGraph = graph as IRaycastableGraph;
					
					if (rayGraph != null) {
						if (rayGraph.Linecast (v1,v2, n1)) {
							return false;
						}
					}
				}
			}
			return true;
		}
	}
}