using UnityEngine;
using System.Collections.Generic;
using Pathfinding;
using Pathfinding.RVO;

namespace Pathfinding.RVO.Sampled {
	public class Agent : IAgent {
		Vector3 smoothPos;

		public Vector3 Position {
			get;
			private set;
		}

		public Vector3 InterpolatedPosition {
			get { return smoothPos; }
		}

		public Vector3 DesiredVelocity { get; set; }

		public void Teleport (Vector3 pos) {
			Position = pos;
			smoothPos = pos;
			prevSmoothPos = pos;
		}

		public void SetYPosition (float yCoordinate) {
			Position = new Vector3(Position.x, yCoordinate, Position.z);
			smoothPos.y = yCoordinate;
			prevSmoothPos.y = yCoordinate;
		}

		//Current values for double buffer calculation

		public float radius, height, maxSpeed, neighbourDist, agentTimeHorizon, obstacleTimeHorizon, weight;
		public bool locked = false;

		RVOLayer layer, collidesWith;

		public int maxNeighbours;
		public Vector3 position, desiredVelocity, prevSmoothPos;

		public RVOLayer Layer { get; set; }
		public RVOLayer CollidesWith { get; set; }

		public bool Locked { get; set; }
		public float Radius { get; set; }
		public float Height { get; set; }
		public float MaxSpeed { get; set; }
		public float NeighbourDist { get; set; }
		public float AgentTimeHorizon { get; set; }
		public float ObstacleTimeHorizon { get; set; }
		public Vector3 Velocity { get; set; }
		public bool DebugDraw { get; set; }

		public int MaxNeighbours { get; set; }

		/** Used internally for a linked list */
		internal Agent next;

		private Vector3 velocity;
		internal Vector3 newVelocity;

		/** Simulator which handles this agent.
		 * Used by this script as a reference and to prevent
		 * adding this agent to multiple simulations.
		 */
		public Simulator simulator;

		public List<Agent> neighbours = new List<Agent>();
		public List<float> neighbourDists = new List<float>();
		List<ObstacleVertex> obstaclesBuffered = new List<ObstacleVertex>();
		List<ObstacleVertex> obstacles = new List<ObstacleVertex>();
		List<float> obstacleDists = new List<float>();

		public List<ObstacleVertex> NeighbourObstacles {
			get {
				return null;
			}
		}

		public Agent (Vector3 pos) {
			MaxSpeed = 2;
			NeighbourDist = 15;
			AgentTimeHorizon = 2;
			ObstacleTimeHorizon = 2;
			Height = 5;
			Radius = 5;
			MaxNeighbours = 10;
			Locked = false;

			position = pos;
			Position = position;
			prevSmoothPos = position;
			smoothPos = position;

			Layer = RVOLayer.DefaultAgent;
			CollidesWith = (RVOLayer)(-1);
		}

		public void BufferSwitch () {
			// <==
			radius = Radius;
			height = Height;
			maxSpeed = MaxSpeed;
			neighbourDist = NeighbourDist;
			agentTimeHorizon = AgentTimeHorizon;
			obstacleTimeHorizon = ObstacleTimeHorizon;
			maxNeighbours = MaxNeighbours;
			desiredVelocity = DesiredVelocity;
			locked = Locked;
			collidesWith = CollidesWith;
			layer = Layer;

			//position = Position;

			// ==>
			Velocity = velocity;
			List<ObstacleVertex> tmp = obstaclesBuffered;
			obstaclesBuffered = obstacles;
			obstacles = tmp;
		}

		// Update is called once per frame
		public void Update () {
			velocity = newVelocity;

			prevSmoothPos = smoothPos;

			//Note the case P/p
			//position = Position;
			position = prevSmoothPos;

			position = position + velocity * simulator.DeltaTime;
			Position = position;
		}

		public void Interpolate (float t) {
			smoothPos = prevSmoothPos + (Position-prevSmoothPos)*t;
		}

		public static System.Diagnostics.Stopwatch watch1 = new System.Diagnostics.Stopwatch();
		public static System.Diagnostics.Stopwatch watch2 = new System.Diagnostics.Stopwatch();

		public void CalculateNeighbours () {
			neighbours.Clear();
			neighbourDists.Clear();

			float rangeSq;

			if (locked) return;

			//watch1.Start ();
			if (MaxNeighbours > 0) {
				rangeSq = neighbourDist*neighbourDist;

				//simulator.KDTree.GetAgentNeighbours (this, rangeSq);
				simulator.Quadtree.Query(new Vector2(position.x, position.z), neighbourDist, this);
			}
			//watch1.Stop ();

			obstacles.Clear();
			obstacleDists.Clear();

			rangeSq = (obstacleTimeHorizon * maxSpeed + radius);
			rangeSq *= rangeSq;
			// Obstacles disabled at the moment
			//simulator.KDTree.GetObstacleNeighbours (this, rangeSq);
		}

		float Sqr (float x) {
			return x*x;
		}

		public float InsertAgentNeighbour (Agent agent, float rangeSq) {
			if (this == agent) return rangeSq;

			if ((agent.layer & collidesWith) == 0) return rangeSq;

			//2D Dist
			float dist = Sqr(agent.position.x-position.x) + Sqr(agent.position.z - position.z);

			if (dist < rangeSq) {
				if (neighbours.Count < maxNeighbours) {
					neighbours.Add(agent);
					neighbourDists.Add(dist);
				}

				int i = neighbours.Count-1;
				if (dist < neighbourDists[i]) {
					while (i != 0 && dist < neighbourDists[i-1]) {
						neighbours[i] = neighbours[i-1];
						neighbourDists[i] = neighbourDists[i-1];
						i--;
					}
					neighbours[i] = agent;
					neighbourDists[i] = dist;
				}

				if (neighbours.Count == maxNeighbours) {
					rangeSq = neighbourDists[neighbourDists.Count-1];
				}
			}
			return rangeSq;
		}

		/*public void UpdateNeighbours () {
		 *  neighbours.Clear ();
		 *  float sqrDist = neighbourDistance*neighbourDistance;
		 *  for ( int i = 0; i < simulator.agents.Count; i++ ) {
		 *      float dist = (simulator.agents[i].position - position).sqrMagnitude;
		 *      if ( dist <= sqrDist ) {
		 *          neighbours.Add ( simulator.agents[i] );
		 *      }
		 *  }
		 * }*/

		public void InsertObstacleNeighbour (ObstacleVertex ob1, float rangeSq) {
			ObstacleVertex ob2 = ob1.next;

			float dist = VectorMath.SqrDistancePointSegment(ob1.position, ob2.position, Position);

			if (dist < rangeSq) {
				obstacles.Add(ob1);
				obstacleDists.Add(dist);

				int i = obstacles.Count-1;
				while (i != 0 && dist < obstacleDists[i-1]) {
					obstacles[i] = obstacles[i-1];
					obstacleDists[i] = obstacleDists[i-1];
					i--;
				}
				obstacles[i] = ob1;
				obstacleDists[i] = dist;
			}
		}

		static Vector3 To3D (Vector2 p) {
			return new Vector3(p.x, 0, p.y);
		}

		static void DrawCircle (Vector2 _p, float radius, Color col) {
			DrawCircle(_p, radius, 0, 2*Mathf.PI, col);
		}

		static void DrawCircle (Vector2 _p, float radius, float a0, float a1, Color col) {
			Vector3 p = To3D(_p);

			while (a0 > a1) a0 -= 2*Mathf.PI;

			Vector3 prev = new Vector3(Mathf.Cos(a0)*radius, 0, Mathf.Sin(a0)*radius);
			const float steps = 40.0f;
			for (int i = 0; i <= steps; i++) {
				Vector3 c = new Vector3(Mathf.Cos(Mathf.Lerp(a0, a1, i/steps))*radius, 0, Mathf.Sin(Mathf.Lerp(a0, a1, i/steps))*radius);
				Debug.DrawLine(p+prev, p+c, col);
				prev = c;
			}
		}

		static void DrawVO (Vector2 circleCenter, float radius, Vector2 origin) {
			float alpha = Mathf.Atan2((origin - circleCenter).y, (origin - circleCenter).x);
			float gamma = radius/(origin-circleCenter).magnitude;
			float delta = gamma <= 1.0f ? Mathf.Abs(Mathf.Acos(gamma)) : 0;

			DrawCircle(circleCenter, radius, alpha-delta, alpha+delta, Color.black);
			Vector2 p1 = new Vector2(Mathf.Cos(alpha-delta), Mathf.Sin(alpha-delta)) * radius;
			Vector2 p2 = new Vector2(Mathf.Cos(alpha+delta), Mathf.Sin(alpha+delta)) * radius;

			Vector2 p1t = -new Vector2(-p1.y, p1.x);
			Vector2 p2t = new Vector2(-p2.y, p2.x);
			p1 += circleCenter;
			p2 += circleCenter;

			Debug.DrawRay(To3D(p1), To3D(p1t).normalized*100, Color.black);
			Debug.DrawRay(To3D(p2), To3D(p2t).normalized*100, Color.black);
		}

		static void DrawCross (Vector2 p, float size = 1) {
			DrawCross(p, Color.white, size);
		}

		static void DrawCross (Vector2 p, Color col, float size = 1) {
			size *= 0.5f;
			Debug.DrawLine(new Vector3(p.x, 0, p.y) - Vector3.right*size, new Vector3(p.x, 0, p.y) + Vector3.right*size, col);
			Debug.DrawLine(new Vector3(p.x, 0, p.y) - Vector3.forward*size, new Vector3(p.x, 0, p.y) + Vector3.forward*size, col);
		}

		public struct VO {
			public Vector2 origin, center;

			Vector2 line1, line2, dir1, dir2;

			Vector2 cutoffLine, cutoffDir;

			float sqrCutoffDistance;
			bool leftSide;

			bool colliding;

			float radius;

			float weightFactor;

			/** Creates a VO to avoid the half plane created by the point \a p0 and has a tangent in the direction of \a dir.
			 * \param p0 A point on the half plane border
			 * \param dir The normalized tangent to the half plane
			 * \param weightFactor relative amount of influence this VO should have on the agent
			 */
			public VO (Vector2 offset, Vector2 p0, Vector2 dir, float weightFactor) {
				colliding = true;
				line1 = p0;
				dir1 = -dir;

				// Fully initialize the struct, compiler complains otherwise
				origin = Vector2.zero;
				center = Vector2.zero;
				line2 = Vector2.zero;
				dir2 = Vector2.zero;
				cutoffLine = Vector2.zero;
				cutoffDir = Vector2.zero;
				sqrCutoffDistance = 0;
				leftSide = false;
				radius = 0;

				// Adjusted so that a parameter weightFactor of 1 will be the default ("natural") weight factor
				this.weightFactor = weightFactor*0.5f;

				//Debug.DrawRay ( To3D(offset + line1), To3D(dir1)*10, Color.red);
			}

			/** Creates a VO to avoid the three half planes with {point, tangent}s of {p1, p2-p1}, {p1, tang1}, {p2, tang2}.
			 * tang1 and tang2 should be normalized.
			 */
			public VO (Vector2 offset, Vector2 p1, Vector2 p2, Vector2 tang1, Vector2 tang2, float weightFactor) {
				// Adjusted so that a parameter weightFactor of 1 will be the default ("natural") weight factor
				this.weightFactor = weightFactor*0.5f;

				colliding = false;
				cutoffLine = p1;
				/** \todo Square root can theoretically be removed by passing another parameter */
				cutoffDir = (p2-p1).normalized;
				line1 = p1;
				dir1 = tang1;
				line2 = p2;
				dir2 = tang2;

				//dir1 = -dir1;
				dir2 = -dir2;
				cutoffDir = -cutoffDir;

				// Fully initialize the struct, compiler complains otherwise
				origin = Vector2.zero;
				center = Vector2.zero;
				sqrCutoffDistance = 0;
				leftSide = false;
				radius = 0;

				weightFactor = 5;

				//Debug.DrawRay (To3D(cutoffLine+offset), To3D(cutoffDir)*10, Color.blue);
				//Debug.DrawRay (To3D(line1+offset), To3D(dir1)*10, Color.blue);
				//Debug.DrawRay (To3D(line2+offset), To3D(dir2)*10, Color.cyan);
			}

			/** Creates a VO for avoiding another agent */
			public VO (Vector2 center, Vector2 offset, float radius, Vector2 sideChooser, float inverseDt, float weightFactor) {
				// Adjusted so that a parameter weightFactor of 1 will be the default ("natural") weight factor
				this.weightFactor = weightFactor*0.5f;

				//this.radius = radius;
				Vector2 globalCenter;
				this.origin = offset;
				weightFactor = 0.5f;

				// Collision?
				if (center.magnitude < radius) {
					colliding = true;
					leftSide = false;

					line1 = center.normalized * (center.magnitude - radius);
					dir1 = new Vector2(line1.y, -line1.x).normalized;
					line1 += offset;

					cutoffDir = Vector2.zero;
					cutoffLine = Vector2.zero;
					sqrCutoffDistance = 0;
					dir2 = Vector2.zero;
					line2 = Vector2.zero;
					this.center = Vector2.zero;
					this.radius = 0;
				} else {
					colliding = false;

					center *= inverseDt;
					radius *= inverseDt;
					globalCenter = center+offset;

					sqrCutoffDistance = center.magnitude - radius;

					this.center = center;
					cutoffLine = center.normalized * sqrCutoffDistance;
					cutoffDir = new Vector2(-cutoffLine.y, cutoffLine.x).normalized;
					cutoffLine += offset;

					sqrCutoffDistance *= sqrCutoffDistance;
					float alpha = Mathf.Atan2(-center.y, -center.x);

					float delta = Mathf.Abs(Mathf.Acos(radius/center.magnitude));

					this.radius = radius;

					// Bounding Lines

					leftSide = VectorMath.RightOrColinear(Vector2.zero, center, sideChooser);

					// Point on circle
					line1 = new Vector2(Mathf.Cos(alpha+delta), Mathf.Sin(alpha+delta)) * radius;
					// Vector tangent to circle which is the correct line tangent
					dir1 = new Vector2(line1.y, -line1.x).normalized;

					// Point on circle
					line2 = new Vector2(Mathf.Cos(alpha-delta), Mathf.Sin(alpha-delta)) * radius;
					// Vector tangent to circle which is the correct line tangent
					dir2 = new Vector2(line2.y, -line2.x).normalized;

					line1 += globalCenter;
					line2 += globalCenter;

					//Debug.DrawRay ( To3D(line1), To3D(dir1), Color.cyan );
					//Debug.DrawRay ( To3D(line2), To3D(dir2), Color.cyan );
				}
			}

			/** Returns if \a p lies on the left side of a line which with one point in \a a and has a tangent in the direction of \a dir.
			 * Also returns true if the points are colinear.
			 */
			public static bool Left (Vector2 a, Vector2 dir, Vector2 p) {
				return (dir.x) * (p.y - a.y) - (p.x - a.x) * (dir.y) <= 0;
			}

			/** Returns a negative number of if \a p lies on the left side of a line which with one point in \a a and has a tangent in the direction of \a dir.
			 * The number can be seen as the double signed area of the triangle {a, a+dir, p} multiplied by the length of \a dir.
			 * If length(dir)=1 this is also the distance from p to the line {a, a+dir}.
			 */
			public static float Det (Vector2 a, Vector2 dir, Vector2 p) {
				return (p.x - a.x) * (dir.y) - (dir.x) * (p.y - a.y);
			}

			public Vector2 Sample (Vector2 p, out float weight) {
				if (colliding) {
					// Calculate double signed area of the triangle consisting of the points
					// {line1, line1+dir1, p}
					float l1 = Det(line1, dir1, p);

					// Serves as a check for which side of the line the point p is
					if (l1 >= 0) {
						/*float dot1 = Vector2.Dot ( p - line1, dir1 );
						 *
						 * Vector2 c1 = dot1 * dir1 + line1;
						 * return (c1-p);*/
						weight = l1*weightFactor;
						return new Vector2(-dir1.y, dir1.x)*weight*GlobalIncompressibility; // 10 is an arbitrary constant signifying incompressability
						// (the higher the value, the more the agents will avoid penetration)
					} else {
						weight = 0;
						return new Vector2(0, 0);
					}
				}

				float det3 = Det(cutoffLine, cutoffDir, p);
				if (det3 <= 0) {
					weight = 0;
					return Vector2.zero;
				} else {
					float det1 = Det(line1, dir1, p);
					float det2 = Det(line2, dir2, p);
					if (det1 >= 0 && det2 >= 0) {
						// We are inside both of the half planes
						// (all three if we count the cutoff line)
						// and thus inside the forbidden region in velocity space

						if (leftSide) {
							if (det3 < radius) {
								weight = det3*weightFactor;
								return new Vector2(-cutoffDir.y, cutoffDir.x)*weight;
							}

							weight = det1;
							return new Vector2(-dir1.y, dir1.x)*weight;
						} else {
							if (det3 < radius) {
								weight = det3*weightFactor;
								return new Vector2(-cutoffDir.y, cutoffDir.x)*weight;
							}

							weight = det2*weightFactor;
							return new Vector2(-dir2.y, dir2.x)*weight;
						}
					}
				}

				weight = 0;
				return new Vector2(0, 0);
			}

			public float ScalarSample (Vector2 p) {
				if (colliding) {
					// Calculate double signed area of the triangle consisting of the points
					// {line1, line1+dir1, p}
					float l1 = Det(line1, dir1, p);

					// Serves as a check for which side of the line the point p is
					if (l1 >= 0) {
						/*float dot1 = Vector2.Dot ( p - line1, dir1 );
						 *
						 * Vector2 c1 = dot1 * dir1 + line1;
						 * return (c1-p);*/
						return l1*GlobalIncompressibility*weightFactor;
					} else {
						return 0;
					}
				}

				float det3 = Det(cutoffLine, cutoffDir, p);
				if (det3 <= 0) {
					return 0;
				}

				{
					float det1 = Det(line1, dir1, p);
					float det2 = Det(line2, dir2, p);
					if (det1 >= 0 && det2 >= 0) {
						if (leftSide) {
							if (det3 < radius) {
								return det3*weightFactor;
							}

							return det1*weightFactor;
						} else {
							if (det3 < radius) {
								return det3*weightFactor;
							}

							return det2*weightFactor;
						}
					}
				}

				return 0;
			}
		}

		internal void CalculateVelocity (Pathfinding.RVO.Simulator.WorkerContext context) {
			if (locked) {
				newVelocity = Vector2.zero;
				return;
			}

			if (context.vos.Length < neighbours.Count+simulator.obstacles.Count) {
				context.vos = new VO[Mathf.Max(context.vos.Length*2, neighbours.Count+simulator.obstacles.Count)];
			}

			Vector2 position2D = new Vector2(position.x, position.z);

			var vos = context.vos;
			var voCount = 0;

			Vector2 optimalVelocity = new Vector2(velocity.x, velocity.z);

			float inverseAgentTimeHorizon = 1.0f/agentTimeHorizon;

			float wallThickness = simulator.WallThickness;

			float wallWeight = simulator.algorithm == Simulator.SamplingAlgorithm.GradientDescent ? 1 : WallWeight;

			for (int i = 0; i < simulator.obstacles.Count; i++) {
				var obstacle = simulator.obstacles[i];
				var vertex = obstacle;
				do {
					if (vertex.ignore || position.y > vertex.position.y + vertex.height || position.y+height < vertex.position.y || (vertex.layer & collidesWith) == 0) {
						vertex = vertex.next;
						continue;
					}

					float cross = VO.Det(new Vector2(vertex.position.x, vertex.position.z), vertex.dir, position2D);// vertex.dir.x * ( vertex.position.z - position.z ) - vertex.dir.y * ( vertex.position.x - position.x );

					// Signed distance from the line (not segment), lines are infinite
					// Usually divided by vertex.dir.magnitude, but that is known to be 1
					float signedDist = cross;

					float dotFactor = Vector2.Dot(vertex.dir, position2D - new Vector2(vertex.position.x, vertex.position.z));

					// It is closest to the segment
					// if the dotFactor is <= 0 or >= length of the segment
					// WallThickness*0.1 is added as a margin to avoid false positives when moving along the edges of square obstacles
					bool closestIsEndpoints = dotFactor <= wallThickness*0.05f || dotFactor >= (new Vector2(vertex.position.x, vertex.position.z) - new Vector2(vertex.next.position.x, vertex.next.position.z)).magnitude - wallThickness*0.05f;

					if (Mathf.Abs(signedDist) < neighbourDist) {
						if (signedDist <= 0 && !closestIsEndpoints && signedDist > -wallThickness) {
							// Inside the wall on the "wrong" side
							vos[voCount] = new VO(position2D, new Vector2(vertex.position.x, vertex.position.z) - position2D, vertex.dir, wallWeight*2);
							voCount++;
						} else if (signedDist > 0) {
							//Debug.DrawLine (position, (vertex.position+vertex.next.position)*0.5f, Color.yellow);
							Vector2 p1 = new Vector2(vertex.position.x, vertex.position.z) - position2D;
							Vector2 p2 = new Vector2(vertex.next.position.x, vertex.next.position.z) - position2D;
							Vector2 tang1 = (p1).normalized;
							Vector2 tang2 = (p2).normalized;
							vos[voCount] = new VO(position2D, p1, p2, tang1, tang2, wallWeight);
							voCount++;
						}
					}
					vertex = vertex.next;
				} while (vertex != obstacle);
			}

			for (int o = 0; o < neighbours.Count; o++) {
				Agent other = neighbours[o];

				if (other == this) continue;

				float maxY = System.Math.Min(position.y+height, other.position.y+other.height);
				float minY = System.Math.Max(position.y, other.position.y);

				//The agents cannot collide since they
				//are on different y-levels
				if (maxY - minY < 0) {
					continue;
				}

				Vector2 otherOptimalVelocity = new Vector2(other.Velocity.x, other.velocity.z);


				float totalRadius = radius + other.radius;

				// Describes a circle on the border of the VO
				//float boundingRadius = totalRadius * inverseAgentTimeHorizon;
				Vector2 voBoundingOrigin = new Vector2(other.position.x, other.position.z) - position2D;

				//float boundingDist = voBoundingOrigin.magnitude;

				Vector2 relativeVelocity = optimalVelocity - otherOptimalVelocity;

				{
					//voBoundingOrigin *= inverseAgentTimeHorizon;
					//boundingDist *= inverseAgentTimeHorizon;

					// Common case, no collision

					Vector2 voCenter;
					if (other.locked) {
						voCenter = otherOptimalVelocity;
					} else {
						voCenter = (optimalVelocity + otherOptimalVelocity)*0.5f;
					}

					vos[voCount] = new VO(voBoundingOrigin, voCenter, totalRadius, relativeVelocity, inverseAgentTimeHorizon, 1);
					voCount++;
					if (DebugDraw) DrawVO(position2D + voBoundingOrigin*inverseAgentTimeHorizon + voCenter, totalRadius*inverseAgentTimeHorizon, position2D + voCenter);
				}
			}


			Vector2 result = Vector2.zero;

			if (simulator.algorithm == Simulator.SamplingAlgorithm.GradientDescent) {
				if (DebugDraw) {
					const int PlotWidth = 40;
					const float WorldPlotWidth = 15;

					for (int x = 0; x < PlotWidth; x++) {
						for (int y = 0; y < PlotWidth; y++) {
							Vector2 p = new Vector2(x*WorldPlotWidth / PlotWidth, y*WorldPlotWidth / PlotWidth);

							Vector2 dir = Vector2.zero;
							float weight = 0;
							for (int i = 0; i < voCount; i++) {
								float w;
								dir += vos[i].Sample(p-position2D, out w);
								if (w > weight) weight = w;
							}
							Vector2 d2 = (new Vector2(desiredVelocity.x, desiredVelocity.z) - (p-position2D));
							dir += d2*DesiredVelocityScale;

							if (d2.magnitude * DesiredVelocityWeight > weight) weight = d2.magnitude * DesiredVelocityWeight;

							if (weight > 0) dir /= weight;

							//Vector2 d3 = simulator.SampleDensity (p+position2D);
							Debug.DrawRay(To3D(p), To3D(d2*0.00f), Color.blue);
							//simulator.Plot (p, Rainbow(weight*simulator.colorScale));

							float sc = 0;
							Vector2 p0 = p - Vector2.one*WorldPlotWidth*0.5f;
							Vector2 p1 = Trace(vos, voCount, p0, 0.01f, out sc);
							if ((p0 - p1).sqrMagnitude < Sqr(WorldPlotWidth / PlotWidth)*2.6f) {
								Debug.DrawRay(To3D(p1 + position2D), Vector3.up*1, Color.red);
							}
						}
					}
				}

				//if ( debug ) {
				float best = float.PositiveInfinity;

				float cutoff = new Vector2(velocity.x, velocity.z).magnitude*simulator.qualityCutoff;

				//for ( int i = 0; i < 10; i++ ) {
				{
					result = Trace(vos, voCount, new Vector2(desiredVelocity.x, desiredVelocity.z), cutoff, out best);
					if (DebugDraw) DrawCross(result+position2D, Color.yellow, 0.5f);
				}

				// Can be uncommented for higher quality local avoidance
				/*for ( int i = 0; i < 3; i++ ) {
				 *  Vector2 p = desiredVelocity + new Vector2(Mathf.Cos(Mathf.PI*2*(i/3.0f)), Mathf.Sin(Mathf.PI*2*(i/3.0f)));
				 *  float score;
				 *  Vector2 res = Trace ( vos, voCount, p, velocity.magnitude*simulator.qualityCutoff, out score );
				 *
				 *  if ( score < best ) {
				 *      //if ( score < best*0.9f ) Debug.Log ("Better " + score + " < " + best);
				 *      result = res;
				 *      best = score;
				 *  }
				 * }*/

				{
					Vector2 p = Velocity;
					float score;
					Vector2 res = Trace(vos, voCount, p, cutoff, out score);

					if (score < best) {
						//if ( score < best*0.9f ) Debug.Log ("Better " + score + " < " + best);
						result = res;
						best = score;
					}
					if (DebugDraw) DrawCross(res+position2D, Color.magenta, 0.5f);
				}
			} else {
				// Adaptive sampling

				Vector2[] samplePos = context.samplePos;
				float[] sampleSize = context.sampleSize;
				int samplePosCount = 0;


				Vector2 desired2D = new Vector2(desiredVelocity.x, desiredVelocity.z);
				float sampleScale = Mathf.Max(radius, Mathf.Max(desired2D.magnitude, Velocity.magnitude));
				samplePos[samplePosCount] = desired2D;
				sampleSize[samplePosCount] = sampleScale*0.3f;
				samplePosCount++;

				const float GridScale = 0.3f;

				// Initial 9 samples
				samplePos[samplePosCount] = optimalVelocity;
				sampleSize[samplePosCount] = sampleScale*GridScale;
				samplePosCount++;

				{
					Vector2 fw = optimalVelocity * 0.5f;
					Vector2 rw = new Vector2(fw.y, -fw.x);

					const int Steps = 8;
					for (int i = 0; i < Steps; i++) {
						samplePos[samplePosCount] = rw * Mathf.Sin(i*Mathf.PI*2 / Steps) + fw * (1 + Mathf.Cos(i*Mathf.PI*2 / Steps));
						sampleSize[samplePosCount] = (1.0f - (Mathf.Abs(i - Steps*0.5f)/Steps))*sampleScale*0.5f;
						samplePosCount++;
					}

					const float InnerScale = 0.6f;
					fw *= InnerScale;
					rw *= InnerScale;

					const int Steps2 = 6;
					for (int i = 0; i < Steps2; i++) {
						samplePos[samplePosCount] = rw * Mathf.Cos((i+0.5f)*Mathf.PI*2 / Steps2) + fw * ((1.0f/InnerScale) + Mathf.Sin((i+0.5f)*Mathf.PI*2 / Steps2));
						sampleSize[samplePosCount] = sampleScale*0.3f;
						samplePosCount++;
					}

					const float TargetScale = 0.2f;

					const int Steps3 = 6;
					for (int i = 0; i < Steps3; i++) {
						samplePos[samplePosCount] = optimalVelocity + new Vector2(sampleScale * TargetScale * Mathf.Cos((i+0.5f)*Mathf.PI*2 / Steps3), sampleScale * TargetScale * Mathf.Sin((i+0.5f)*Mathf.PI*2 / Steps3));
						sampleSize[samplePosCount] = sampleScale*TargetScale*2;
						samplePosCount++;
					}
				}

				samplePos[samplePosCount] = optimalVelocity*0.5f;
				sampleSize[samplePosCount] = sampleScale*0.4f;
				samplePosCount++;

				const int KeepCount = Simulator.WorkerContext.KeepCount;
				Vector2[] bestPos = context.bestPos;
				float[] bestSizes = context.bestSizes;
				float[] bestScores = context.bestScores;

				for (int i = 0; i < KeepCount; i++) {
					bestScores[i] = float.PositiveInfinity;
				}
				bestScores[KeepCount] = float.NegativeInfinity;

				Vector2 bestEver = optimalVelocity;
				float bestEverScore = float.PositiveInfinity;

				for (int sub = 0; sub < 3; sub++) {
					for (int i = 0; i < samplePosCount; i++) {
						float score = 0;
						for (int vo = 0; vo < voCount; vo++) {
							score = System.Math.Max(score, vos[vo].ScalarSample(samplePos[i]));
						}
						// Note that velocity is a vector and speed is a scalar, not the same thing
						float bonusForDesiredVelocity = (samplePos[i] - desired2D).magnitude;

						// This didn't work out as well as I though
						// Code left here because I might reenable it later
						//float bonusForDesiredSpeed = Mathf.Abs (samplePos[i].magnitude - desired2D.magnitude);

						float biasedScore = score + bonusForDesiredVelocity*DesiredVelocityWeight;// + bonusForDesiredSpeed*0;
						score += bonusForDesiredVelocity*0.001f;

						if (DebugDraw) {
							DrawCross(position2D + samplePos[i], Rainbow(Mathf.Log(score+1)*5), sampleSize[i]*0.5f);
						}

						if (biasedScore < bestScores[0]) {
							for (int j = 0; j < KeepCount; j++) {
								if (biasedScore >= bestScores[j+1]) {
									bestScores[j] = biasedScore;
									bestSizes[j] = sampleSize[i];
									bestPos[j] = samplePos[i];
									break;
								}
							}
						}

						if (score < bestEverScore) {
							bestEver = samplePos[i];
							bestEverScore = score;

							if (score == 0) {
								sub = 100;
								break;
							}
						}
					}

					samplePosCount = 0;

					for (int i = 0; i < KeepCount; i++) {
						Vector2 p = bestPos[i];
						float s = bestSizes[i];
						bestScores[i] = float.PositiveInfinity;

						const float Half = 0.6f;

						float offset = s * Half * 0.5f;

						samplePos[samplePosCount+0] = (p + new Vector2(+offset, +offset));
						samplePos[samplePosCount+1] = (p + new Vector2(-offset, +offset));
						samplePos[samplePosCount+2] = (p + new Vector2(-offset, -offset));
						samplePos[samplePosCount+3] = (p + new Vector2(+offset, -offset));

						s *= s * Half;
						sampleSize[samplePosCount+0] = (s);
						sampleSize[samplePosCount+1] = (s);
						sampleSize[samplePosCount+2] = (s);
						sampleSize[samplePosCount+3] = (s);
						samplePosCount += 4;
					}
				}

				result = bestEver;
			}


			if (DebugDraw) DrawCross(result+position2D);


			newVelocity = To3D(Vector2.ClampMagnitude(result, maxSpeed));
		}

		public static float DesiredVelocityWeight = 0.02f;
		public static float DesiredVelocityScale = 0.1f;
		//public static float DesiredSpeedScale = 0.0f;
		public static float GlobalIncompressibility = 30;

		/** Extra weight that walls will have */
		const float WallWeight = 5;

		static Color Rainbow (float v) {
			Color c = new Color(v, 0, 0);

			if (c.r > 1) { c.g = c.r - 1; c.r = 1; }
			if (c.g > 1) { c.b = c.g - 1; c.g = 1; }
			return c;
		}

		/** Traces the vector field constructed out of the velocity obstacles.
		 * Returns the position which gives the minimum score (approximately).
		 */
		Vector2 Trace (VO[] vos, int voCount, Vector2 p, float cutoff, out float score) {
			score = 0;
			float stepScale = simulator.stepScale;

			float bestScore = float.PositiveInfinity;
			Vector2 bestP = p;

			for (int s = 0; s < 50; s++) {
				float step = 1.0f - (s/50.0f);
				step *= stepScale;

				Vector2 dir = Vector2.zero;
				float mx = 0;
				for (int i = 0; i < voCount; i++) {
					float w;
					Vector2 d = vos[i].Sample(p, out w);
					dir += d;

					if (w > mx) mx = w;
					//mx = System.Math.Max (mx, d.sqrMagnitude);
				}


				// This didn't work out as well as I though
				// Code left here because I might reenable it later
				//Vector2 bonusForDesiredSpeed = p.normalized *  new Vector2(desiredVelocity.x,desiredVelocity.z).magnitude - p;

				Vector2 bonusForDesiredVelocity = (new Vector2(desiredVelocity.x, desiredVelocity.z) - p);

				float weight = bonusForDesiredVelocity.magnitude*DesiredVelocityWeight;// + bonusForDesiredSpeed.magnitude*DesiredSpeedScale;
				dir += bonusForDesiredVelocity*DesiredVelocityScale;// + bonusForDesiredSpeed*DesiredSpeedScale;
				mx = System.Math.Max(mx, weight);


				score = mx;



				if (score < bestScore) {
					bestScore = score;
				}

				bestP = p;
				if (score <= cutoff && s > 10) break;

				float sq = dir.sqrMagnitude;
				if (sq > 0) dir *= mx/Mathf.Sqrt(sq);

				dir *= step;
				Vector2 prev = p;
				p += dir;
				if (DebugDraw) Debug.DrawLine(To3D(prev)+position, To3D(p)+position, Rainbow(0.1f/score) * new Color(1, 1, 1, 0.2f));
			}


			score = bestScore;
			return bestP;
		}

		/** Returns the intersection factors for line 1 and line 2. The intersection factors is a distance along the line \a start - \a end where the other line intersects it.\n
		 * \code intersectionPoint = start1 + factor1 * (end1-start1) \endcode
		 * \code intersectionPoint2 = start2 + factor2 * (end2-start2) \endcode
		 * Lines are treated as infinite.\n
		 * false is returned if the lines are parallel and true if they are not.
		 */
		public static bool IntersectionFactor (Vector2 start1, Vector2 dir1, Vector2 start2, Vector2 dir2, out float factor) {
			float den = dir2.y*dir1.x - dir2.x * dir1.y;

			// Parallel
			if (den == 0) {
				factor = 0;
				return false;
			}

			float nom = dir2.x*(start1.y-start2.y)- dir2.y*(start1.x-start2.x);

			factor = nom/den;

			return true;
		}
	}
}
