using UnityEngine;

namespace Pathfinding.RVO {
	/** Unity front end for an RVO simulator.
	 * Attached to any GameObject in a scene, scripts such as the RVOController will use the
	 * simulator exposed by this class to handle their movement.
	 *
	 * You can have more than one of these, however most script which make use of the RVOSimulator
	 * will find it by FindObjectOfType, and thus only one will be used.
	 *
	 * This is only a wrapper class for a Pathfinding.RVO.Simulator which simplifies exposing it
	 * for a unity scene.
	 *
	 * \see Pathfinding.RVO.Simulator
	 *
	 * \astarpro
	 */
	[AddComponentMenu("Pathfinding/Local Avoidance/RVO Simulator")]
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_r_v_o_1_1_r_v_o_simulator.php")]
	public class RVOSimulator : MonoBehaviour {
		/** Calculate local avoidance in between frames.
		 * If this is enabled and multithreading is used, the local avoidance calculations will continue to run
		 * until the next frame instead of waiting for them to be done the same frame. This can increase the performance
		 * but it can make the agents seem a little less responsive.
		 *
		 * This will only be read at Awake.
		 * \see Pathfinding.RVO.Simulator.DoubleBuffering */
		[Tooltip("Calculate local avoidance in between frames.\nThis can increase jitter in the agents' movement so use it only if you really need the performance boost. " +
			 "It will also reduce the responsiveness of the agents to the commands you send to them.")]
		public bool doubleBuffering;

		/** Interpolate positions between simulation timesteps.
		 * If you are using agents directly, make sure you read from the InterpolatedPosition property. */
		[Tooltip("Interpolate positions between simulation timesteps")]
		public bool interpolation = true;

		/** Desired FPS for rvo simulation.
		 * It is usually not necessary to run a crowd simulation at a very high fps.
		 * Usually 10-30 fps is enough, but can be increased for better quality.
		 * The rvo simulation will never run at a higher fps than the game */
		[Tooltip("Desired FPS for rvo simulation. It is usually not necessary to run a crowd simulation at a very high fps.\n" +
			 "Usually 10-30 fps is enough, but can be increased for better quality.\n"+
			 "The rvo simulation will never run at a higher fps than the game")]
		public int desiredSimulationFPS = 20;

		/** Number of RVO worker threads.
		 * If set to None, no multithreading will be used. */
		[Tooltip("Number of RVO worker threads. If set to None, no multithreading will be used.")]
		public ThreadCount workerThreads = ThreadCount.Two;

		/** A higher value will result in lower quality local avoidance but faster calculations.
		 * Valid range is [0...1]
		 */
		[Tooltip("[GradientDescent][unitless][0...1] A higher value will result in lower quality local avoidance but faster calculations.")]
		public float qualityCutoff = 0.05f;

		[Tooltip("[GradientDescent][unitless][0...2] How large steps to take when searching for a minimum to the penalty function. " +
			 "Larger values will make it faster, but less accurate, too low values (near 0) can also give large inaccuracies. Values around 0.5-1.5 work the best.")]
		public float stepScale = 1.5f;

		/** Higher values will raise the penalty for agent-agent intersection */
		[Tooltip("[0...infinity] Higher values will raise the penalty for agent-agent intersection")]
		public float incompressibility = 30;

		[Tooltip("Thickness of RVO obstacle walls.\nIf obstacles are passing through obstacles, try a larger value, if they are getting stuck near small obstacles, try reducing it")]
		public float wallThickness = 1;

		[Tooltip("[unitless][0...infinity] How much an agent will try to reach the desired velocity. A higher value will yield a more aggressive behaviour")]
		public float desiredVelocityWeight = 0.1f;

		/** What sampling algorithm to use.
		 * \see "Reciprocal Velocity Obstacles for Real-Time Multi-Agent Navigation"
		 * \see https://en.wikipedia.org/wiki/Gradient_descent
		 * \see http://digestingduck.blogspot.se/2010/04/adaptive-rvo-sampling.html
		 * \see http://digestingduck.blogspot.se/2010/10/rvo-sample-pattern.html
		 */
		[Tooltip("What sampling algorithm to use. GradientDescent is a bit more agressive but makes it easier for agents to intersect.")]
		public Pathfinding.RVO.Simulator.SamplingAlgorithm algorithm = Pathfinding.RVO.Simulator.SamplingAlgorithm.GradientDescent;

		[Tooltip("Run multiple simulation steps per step. Much slower, but may lead to slightly higher quality local avoidance.")]
		public bool oversampling;

		/** Reference to the internal simulator */
		Pathfinding.RVO.Simulator simulator;

		/** Get the internal simulator.
		 * Will never be null */
		public Simulator GetSimulator () {
			if (simulator == null) {
				Awake();
			}
			return simulator;
		}

		void Awake () {
			if (desiredSimulationFPS < 1) desiredSimulationFPS = 1;

			if (simulator == null) {
				int threadCount = AstarPath.CalculateThreadCount(workerThreads);
				simulator = new Pathfinding.RVO.Simulator(threadCount, doubleBuffering);
				simulator.Interpolation = interpolation;
				simulator.DesiredDeltaTime = 1.0f / desiredSimulationFPS;
			}

			/*Debug.LogWarning ("RVO Local Avoidance is temporarily disabled in the A* Pathfinding Project due to licensing issues.\n" +
			 * "I am working to get it back as soon as possible. All agents will fall back to not avoiding other agents.\n" +
			 * "Sorry for the inconvenience.");*/
		}

		/** Update the simulation */
		void Update () {
			if (desiredSimulationFPS < 1) desiredSimulationFPS = 1;

			Pathfinding.RVO.Sampled.Agent.DesiredVelocityWeight = desiredVelocityWeight;
			Pathfinding.RVO.Sampled.Agent.GlobalIncompressibility = incompressibility;

			var sim = GetSimulator();
			sim.DesiredDeltaTime = 1.0f / desiredSimulationFPS;
			sim.Interpolation = interpolation;
			sim.stepScale = stepScale;
			sim.qualityCutoff = qualityCutoff;
			sim.algorithm = algorithm;
			sim.Oversampling = oversampling;
			sim.WallThickness = wallThickness;
			sim.Update();
		}

		void OnDestroy () {
			if (simulator != null) simulator.OnDestroy();
		}
	}
}
