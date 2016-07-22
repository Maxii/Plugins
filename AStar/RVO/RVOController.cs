using UnityEngine;
using Pathfinding;
using System.Collections.Generic;

namespace Pathfinding.RVO {
	/** RVO Character Controller.
	 * Designed to be used as a drop-in replacement for the Unity Character Controller,
	 * it supports almost all of the same functions and fields with the exception
	 * that due to the nature of the RVO implementation, desired velocity is set in the Move function
	 * and is assumed to stay the same until something else is requested (as opposed to reset every frame).
	 *
	 * For documentation of many of the variables of this class: refer to the Pathfinding.RVO.IAgent interface.
	 *
	 * \note Requires an RVOSimulator in the scene
	 *
	 * \see Pathfinding.RVO.IAgent
	 * \see RVOSimulator
	 *
	 * \astarpro
	 */
	[AddComponentMenu("Pathfinding/Local Avoidance/RVO Controller")]
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_r_v_o_1_1_r_v_o_controller.php")]
	public class RVOController : MonoBehaviour {
		/** Radius of the agent */
		[Tooltip("Radius of the agent")]
		public float radius = 5;

		/** Max speed of the agent. In units/second */
		[Tooltip("Max speed of the agent. In world units/second")]
		public float maxSpeed = 2;

		/** Height of the agent. In world units */
		[Tooltip("Height of the agent. In world units")]
		public float height = 1;

		/** A locked unit cannot move. Other units will still avoid it. But avoidance quailty is not the best. */
		[Tooltip("A locked unit cannot move. Other units will still avoid it. But avoidance quailty is not the best")]
		public bool locked;

		/** Automatically set #locked to true when desired velocity is approximately zero.
		 * This prevents other units from pushing them away when they are supposed to e.g block a choke point.
		 */
		[Tooltip("Automatically set #locked to true when desired velocity is approximately zero")]
		public bool lockWhenNotMoving = true;

		/** How far in the time to look for collisions with other agents */
		[Tooltip("How far in the time to look for collisions with other agents")]
		public float agentTimeHorizon = 2;

		[HideInInspector]
		/** How far in the time to look for collisions with obstacles */
		public float obstacleTimeHorizon = 2;

		/** Maximum distance to other agents to take them into account for collisions.
		 * Decreasing this value can lead to better performance, increasing it can lead to better quality of the simulation.
		 */
		[Tooltip("Maximum distance to other agents to take them into account for collisions.\n" +
			 "Decreasing this value can lead to better performance, increasing it can lead to better quality of the simulation")]
		public float neighbourDist = 10;

		/** Max number of other agents to take into account.
		 * A smaller value can reduce CPU load, a higher value can lead to better local avoidance quality.
		 */
		[Tooltip("Max number of other agents to take into account.\n" +
			 "A smaller value can reduce CPU load, a higher value can lead to better local avoidance quality.")]
		public int maxNeighbours = 10;

		/** Layer mask for the ground.
		 * The RVOController will raycast down to check for the ground to figure out where to place the agent.
		 */
		[Tooltip("Layer mask for the ground. The RVOController will raycast down to check for the ground to figure out where to place the agent")]
		public LayerMask mask = -1;

		/** Specifies the avoidance layer for this agent.
		 * The #collidesWith mask on other agents will determine if they will avoid this agent.
		 */
		public RVOLayer layer = RVOLayer.DefaultAgent;

		/** Layer mask specifying which layers this agent will avoid.
		 * You can set it as CollidesWith = RVOLayer.DefaultAgent | RVOLayer.Layer3 | RVOLayer.Layer6 ...
		 *
		 * This can be very useful in games which have multiple teams of some sort.
		 * For example you usually want that the team agents avoid each other, but you do not want
		 * them to avoid the enemies.
		 *
		 * \see http://en.wikipedia.org/wiki/Mask_(computing)
		 */
		[Pathfinding.AstarEnumFlag]
		public RVOLayer collidesWith = (RVOLayer)(-1);

		/** An extra force to avoid walls.
		 * This can be good way to reduce "wall hugging" behaviour.
		 */
		[HideInInspector]
		public float wallAvoidForce = 1;

		/** How much the wallAvoidForce decreases with distance.
		 * The strenght of avoidance is:
		 * \code str = 1/dist*wallAvoidFalloff \endcode
		 *
		 * \see wallAvoidForce
		 */
		[HideInInspector]
		public float wallAvoidFalloff = 1;

		/** Center of the agent relative to the pivot point of this game object */
		[Tooltip("Center of the agent relative to the pivot point of this game object")]
		public Vector3 center;

		/** Reference to the internal agent */
		private IAgent rvoAgent;

		public bool enableRotation = true;
		public float rotationSpeed = 30;

		/** Reference to the rvo simulator */
		private Simulator simulator;

		private float adjustedY;

		/** Cached tranform component */
		private Transform tr;

		/** Current desired velocity */
		Vector3 desiredVelocity;

	#if ASTARDEBUG
		//Can cause unity serialization failures if the variable is not always included
		public bool debug;
	#else
		//[HideInInspector]
		public bool debug;
	#endif

		/** Position for the previous frame.
		 * Used to check if the agent has moved manually
		 */
		private Vector3 lastPosition;

		/** To avoid having to use FindObjectOfType every time */
		static RVOSimulator cachedSimulator;

		/** Current position of the agent */
		public Vector3 position {
			get { return rvoAgent.InterpolatedPosition; }
		}

		/** Current velocity of the agent */
		public Vector3 velocity {
			get { return rvoAgent.Velocity; }
		}

		public void OnDisable () {
			if (simulator == null) return;

			//Remove the agent from the simulation but keep the reference
			//this component might get enabled and then we can simply
			//add it to the simulation again
			simulator.RemoveAgent(rvoAgent);
		}

		public void Awake () {
			tr = transform;

			// Find the RVOSimulator in this scene
			if (cachedSimulator == null) {
				cachedSimulator = FindObjectOfType<RVOSimulator>();
			}

			if (cachedSimulator == null) {
				Debug.LogError("No RVOSimulator component found in the scene. Please add one.");
			} else {
				simulator = cachedSimulator.GetSimulator();
			}
		}

		public void OnEnable () {
			if (simulator == null) return;

			//We might have an rvoAgent
			//which was disabled previously
			//if so, we can simply add it to the simulation again
			if (rvoAgent != null) {
				simulator.AddAgent(rvoAgent);
			} else {
				rvoAgent = simulator.AddAgent(transform.position);
			}

			UpdateAgentProperties();
			rvoAgent.Teleport(transform.position);
			adjustedY = rvoAgent.Position.y;
		}

		protected void UpdateAgentProperties () {
			rvoAgent.Radius = radius;
			rvoAgent.MaxSpeed = maxSpeed;
			rvoAgent.Height = height;
			rvoAgent.AgentTimeHorizon = agentTimeHorizon;
			rvoAgent.ObstacleTimeHorizon = obstacleTimeHorizon;
			rvoAgent.Locked = locked;
			rvoAgent.MaxNeighbours = maxNeighbours;
			rvoAgent.DebugDraw = debug;
			rvoAgent.NeighbourDist = neighbourDist;
			rvoAgent.Layer = layer;
			rvoAgent.CollidesWith = collidesWith;
		}

		/** Set the desired velocity for the agent.
		 * Note that this is a velocity (units/second), not a movement delta (units/frame).
		 */
		public void Move (Vector3 vel) {
			desiredVelocity = vel;
		}

		/** Teleport the agent to a new position.
		 * The agent will be moved instantly and not show ugly interpolation artifacts during a split second.
		 * Manually changing the position of the transform will in most cases be picked up as a teleport automatically
		 * by the script.
		 *
		 * During the simulation frame the agent was moved manually, local avoidance cannot fully be applied to the
		 * agent, so try to avoid using it too much or local avoidance quality will degrade.
		 */
		public void Teleport (Vector3 pos) {
			tr.position = pos;
			lastPosition = pos;
			rvoAgent.Teleport(pos);
			adjustedY = pos.y;
		}

		public void Update () {
			if (rvoAgent == null) return;

			if (lastPosition != tr.position) {
				Teleport(tr.position);
			}

			if (lockWhenNotMoving) {
				locked = desiredVelocity == Vector3.zero;
			}

			UpdateAgentProperties();

			RaycastHit hit;

			//The non-interpolated position
			Vector3 realPos = rvoAgent.InterpolatedPosition;
			realPos.y = adjustedY;

			if (mask != 0 && Physics.Raycast(realPos + Vector3.up*height*0.5f, Vector3.down, out hit, float.PositiveInfinity, mask)) {
				adjustedY = hit.point.y;
			} else {
				adjustedY = 0;
			}
			realPos.y = adjustedY;

			rvoAgent.SetYPosition(adjustedY);

			Vector3 force = Vector3.zero;

			if (wallAvoidFalloff > 0 && wallAvoidForce > 0) {
				List<ObstacleVertex> obst = rvoAgent.NeighbourObstacles;

				if (obst != null) for (int i = 0; i < obst.Count; i++) {
						Vector3 a = obst[i].position;
						Vector3 b = obst[i].next.position;

						Vector3 closest = position - VectorMath.ClosestPointOnSegment(a, b, position);

						if (closest == a || closest == b) continue;

						float dist = closest.sqrMagnitude;
						closest /= dist*wallAvoidFalloff;
						force += closest;
					}
			}

	#if ASTARDEBUG
			Debug.DrawRay(position, desiredVelocity + force*wallAvoidForce);
	#endif
			rvoAgent.DesiredVelocity = desiredVelocity + force*wallAvoidForce;

			tr.position = realPos + Vector3.up*height*0.5f - center;
			lastPosition = tr.position;

			if (enableRotation && velocity != Vector3.zero) transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(velocity), Time.deltaTime * rotationSpeed * Mathf.Min(velocity.magnitude, 0.2f));
		}

		private static readonly Color GizmoColor = new Color(240/255f, 213/255f, 30/255f);

		public void OnDrawGizmos () {
			Gizmos.color = GizmoColor;
			Gizmos.DrawWireSphere(transform.position+center - Vector3.up*height*0.5f + Vector3.up*radius*0.5f, radius);
			Gizmos.DrawLine(transform.position+center - Vector3.up*height*0.5f, transform.position+center + Vector3.up*height*0.5f);
			Gizmos.DrawWireSphere(transform.position+center + Vector3.up*height*0.5f - Vector3.up*radius*0.5f, radius);
		}
	}
}
