using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_WINRT && !UNITY_EDITOR
//using MarkerMetro.Unity.WinLegacy.IO;
//using MarkerMetro.Unity.WinLegacy.Reflection;
#endif

namespace Pathfinding {
	[System.Serializable]
	/** Stores the navigation graphs for the A* Pathfinding System.
	 * \ingroup relevant
	 *
	 * An instance of this class is assigned to AstarPath.astarData, from it you can access all graphs loaded through the #graphs variable.\n
	 * This class also handles a lot of the high level serialization.
	 */
	public class AstarData {
		/** Shortcut to AstarPath.active */
		public static AstarPath active {
			get {
				return AstarPath.active;
			}
		}

		#region Fields
		/** Shortcut to the first NavMeshGraph.
		 * Updated at scanning time
		 */
		public NavMeshGraph navmesh { get; private set; }

#if !ASTAR_NO_GRID_GRAPH
		/** Shortcut to the first GridGraph.
		 * Updated at scanning time
		 */
		public GridGraph gridGraph { get; private set; }

		/** Shortcut to the first LayerGridGraph.
		 * Updated at scanning time.
		 * \astarpro
		 */
		public LayerGridGraph layerGridGraph { get; private set; }
#endif

#if !ASTAR_NO_POINT_GRAPH
		/** Shortcut to the first PointGraph.
		 * Updated at scanning time
		 */
		public PointGraph pointGraph { get; private set; }
#endif

		/** Shortcut to the first RecastGraph.
		 * Updated at scanning time.
		 * \astarpro
		 */
		public RecastGraph recastGraph { get; private set; }

		/** All supported graph types.
		 * Populated through reflection search
		 */
		public System.Type[] graphTypes { get; private set; }

#if ASTAR_FAST_NO_EXCEPTIONS || UNITY_WINRT || UNITY_WEBGL
		/** Graph types to use when building with Fast But No Exceptions for iPhone.
		 * If you add any custom graph types, you need to add them to this hard-coded list.
		 */
		public static readonly System.Type[] DefaultGraphTypes = new System.Type[] {
#if !ASTAR_NO_GRID_GRAPH
			typeof(GridGraph),
#endif
#if !ASTAR_NO_POINT_GRAPH
			typeof(PointGraph),
#endif
			typeof(NavMeshGraph),
			typeof(RecastGraph),
			typeof(LayerGridGraph)
		};
#endif

		/** All graphs this instance holds.
		 * This will be filled only after deserialization has completed.
		 * May contain null entries if graph have been removed.
		 */
		[System.NonSerialized]
		public NavGraph[] graphs = new NavGraph[0];

		//Serialization Settings

		/** Serialized data for all graphs and settings.
		 * Stored as a base64 encoded string because otherwise Unity's Undo system would sometimes corrupt the byte data (because it only stores deltas).
		 *
		 * This can be accessed as a byte array from the #data property.
		 *
		 * \since 3.6.1
		 */
		[SerializeField]
		string dataString;

		/** Data from versions from before 3.6.1.
		 * Used for handling upgrades
		 * \since 3.6.1
		 */
		[SerializeField]
		[UnityEngine.Serialization.FormerlySerializedAs("data")]
		private byte[] upgradeData;

		/** Serialized data for all graphs and settings */
		private byte[] data {
			get {
				// Handle upgrading from earlier versions than 3.6.1
				if (upgradeData != null && upgradeData.Length > 0) {
					data = upgradeData;
					upgradeData = null;
				}
				return dataString != null ? System.Convert.FromBase64String(dataString) : null;
			}
			set {
				dataString = value != null ? System.Convert.ToBase64String(value) : null;
			}
		}

		/** Backup data if deserialization failed.
		 */
		public byte[] data_backup;

		/** Serialized data for cached startup.
		 * If set, on start the graphs will be deserialized from this file.
		 */
		public TextAsset file_cachedStartup;

		/** Serialized data for cached startup.
		 *
		 * \deprecated Deprecated since 3.6, AstarData.file_cachedStartup is now used instead
		 */
		public byte[] data_cachedStartup;

		/** Should graph-data be cached.
		 * Caching the startup means saving the whole graphs, not only the settings to an internal array (#data_cachedStartup) which can
		 * be loaded faster than scanning all graphs at startup. This is setup from the editor.
		 */
		[SerializeField]
		public bool cacheStartup;

		//End Serialization Settings

		#endregion

		public byte[] GetData () {
			return data;
		}

		public void SetData (byte[] data) {
			this.data = data;
		}

		/** Loads the graphs from memory, will load cached graphs if any exists */
		public void Awake () {
#if false
			graphs = new NavGraph[1] { CreateGraph(typeof(LinkGraph)) };
#else
			graphs = new NavGraph[0];
#endif
			/* End default values */

			if (cacheStartup && file_cachedStartup != null) {
				LoadFromCache();
			} else {
				DeserializeGraphs();
			}
		}

		/** Updates shortcuts to the first graph of different types.
		 * Hard coding references to some graph types is not really a good thing imo. I want to keep it dynamic and flexible.
		 * But these references ease the use of the system, so I decided to keep them.\n
		 */
		public void UpdateShortcuts () {
			navmesh = (NavMeshGraph)FindGraphOfType(typeof(NavMeshGraph));

#if !ASTAR_NO_GRID_GRAPH
			gridGraph = (GridGraph)FindGraphOfType(typeof(GridGraph));
			layerGridGraph = (LayerGridGraph)FindGraphOfType(typeof(LayerGridGraph));
#endif

#if !ASTAR_NO_POINT_GRAPH
			pointGraph = (PointGraph)FindGraphOfType(typeof(PointGraph));
#endif

			recastGraph = (RecastGraph)FindGraphOfType(typeof(RecastGraph));
		}

		/** Load from data from #file_cachedStartup */
		public void LoadFromCache () {
			AstarPath.active.BlockUntilPathQueueBlocked();
			if (file_cachedStartup != null) {
				var bytes = file_cachedStartup.bytes;
				DeserializeGraphs(bytes);

				GraphModifier.TriggerEvent(GraphModifier.EventType.PostCacheLoad);
			} else {
				Debug.LogError("Can't load from cache since the cache is empty");
			}
		}

		#region Serialization

		/** Serializes all graphs settings to a byte array.
		 * \see DeserializeGraphs(byte[])
		 */
		public byte[] SerializeGraphs () {
			return SerializeGraphs(Pathfinding.Serialization.SerializeSettings.Settings);
		}

		/** Serializes all graphs settings and optionally node data to a byte array.
		 * \see DeserializeGraphs(byte[])
		 * \see Pathfinding.Serialization.SerializeSettings
		 */
		public byte[] SerializeGraphs (Pathfinding.Serialization.SerializeSettings settings) {
			uint checksum;

			return SerializeGraphs(settings, out checksum);
		}

		/** Main serializer function.
		 * Serializes all graphs to a byte array
		 * A similar function exists in the AstarPathEditor.cs script to save additional info */
		public byte[] SerializeGraphs (Pathfinding.Serialization.SerializeSettings settings, out uint checksum) {
			AstarPath.active.BlockUntilPathQueueBlocked();

			var sr = new Pathfinding.Serialization.AstarSerializer(this, settings);
			sr.OpenSerialize();
			SerializeGraphsPart(sr);
			byte[] bytes = sr.CloseSerialize();
			checksum = sr.GetChecksum();
	#if ASTARDEBUG
			Debug.Log("Got a whole bunch of data, "+bytes.Length+" bytes");
	#endif
			return bytes;
		}

		/** Serializes common info to the serializer.
		 * Common info is what is shared between the editor serialization and the runtime serializer.
		 * This is mostly everything except the graph inspectors which serialize some extra data in the editor
		 */
		public void SerializeGraphsPart (Pathfinding.Serialization.AstarSerializer sr) {
			sr.SerializeGraphs(graphs);
			sr.SerializeExtraInfo();
		}

		/** Deserializes graphs from #data */
		public void DeserializeGraphs () {
			if (data != null) {
				DeserializeGraphs(data);
			}
		}

		/** Destroys all graphs and sets graphs to null */
		void ClearGraphs () {
			if (graphs == null) return;
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] != null) graphs[i].OnDestroy();
			}
			graphs = null;
			UpdateShortcuts();
		}

		public void OnDestroy () {
			ClearGraphs();
		}

		/** Deserializes graphs from the specified byte array.
		 * If an error occured, it will try to deserialize using the old deserializer.
		 * A warning will be logged if all deserializers failed.
		 */
		public void DeserializeGraphs (byte[] bytes) {
			AstarPath.active.BlockUntilPathQueueBlocked();

			try {
				if (bytes != null) {
					var sr = new Pathfinding.Serialization.AstarSerializer(this);

					if (sr.OpenDeserialize(bytes)) {
						DeserializeGraphsPart(sr);
						sr.CloseDeserialize();
						UpdateShortcuts();
					} else {
						Debug.Log("Invalid data file (cannot read zip).\nThe data is either corrupt or it was saved using a 3.0.x or earlier version of the system");
					}
				} else {
					throw new System.ArgumentNullException("bytes");
				}
				active.VerifyIntegrity();
			} catch (System.Exception e) {
				Debug.LogWarning("Caught exception while deserializing data.\n"+e);
				data_backup = bytes;
			}
		}

		/** Deserializes graphs from the specified byte array additively.
		 * If an error ocurred, it will try to deserialize using the old deserializer.
		 * A warning will be logged if all deserializers failed.
		 * This function will add loaded graphs to the current ones
		 */
		public void DeserializeGraphsAdditive (byte[] bytes) {
			AstarPath.active.BlockUntilPathQueueBlocked();

			try {
				if (bytes != null) {
					var sr = new Pathfinding.Serialization.AstarSerializer(this);

					if (sr.OpenDeserialize(bytes)) {
						DeserializeGraphsPartAdditive(sr);
						sr.CloseDeserialize();
					} else {
						Debug.Log("Invalid data file (cannot read zip).");
					}
				} else {
					throw new System.ArgumentNullException("bytes");
				}
				active.VerifyIntegrity();
			} catch (System.Exception e) {
				Debug.LogWarning("Caught exception while deserializing data.\n"+e);
			}
		}

		/** Deserializes common info.
		 * Common info is what is shared between the editor serialization and the runtime serializer.
		 * This is mostly everything except the graph inspectors which serialize some extra data in the editor
		 */
		public void DeserializeGraphsPart (Pathfinding.Serialization.AstarSerializer sr) {
			ClearGraphs();
			graphs = sr.DeserializeGraphs();

			sr.DeserializeExtraInfo();

			//Assign correct graph indices.
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] == null) continue;
				graphs[i].GetNodes(node => {
					node.GraphIndex = (uint)i;
					return true;
				});
			}

			sr.PostDeserialization();
		}

		/** Deserializes common info additively
		 * Common info is what is shared between the editor serialization and the runtime serializer.
		 * This is mostly everything except the graph inspectors which serialize some extra data in the editor
		 */
		public void DeserializeGraphsPartAdditive (Pathfinding.Serialization.AstarSerializer sr) {
			if (graphs == null) graphs = new NavGraph[0];

			var gr = new List<NavGraph>(graphs);

			// Set an offset so that the deserializer will load
			// the graphs with the correct graph indexes
			sr.SetGraphIndexOffset(gr.Count);

			gr.AddRange(sr.DeserializeGraphs());
			graphs = gr.ToArray();

			//Assign correct graph indices. Issue #21
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] == null) continue;
				graphs[i].GetNodes(node => {
					node.GraphIndex = (uint)i;
					return true;
				});
			}

			sr.DeserializeExtraInfo();
			sr.PostDeserialization();

			for (int i = 0; i < graphs.Length; i++) {
				for (int j = i+1; j < graphs.Length; j++) {
					if (graphs[i] != null && graphs[j] != null && graphs[i].guid == graphs[j].guid) {
						Debug.LogWarning("Guid Conflict when importing graphs additively. Imported graph will get a new Guid.\nThis message is (relatively) harmless.");
						graphs[i].guid = Pathfinding.Util.Guid.NewGuid();
						break;
					}
				}
			}
		}

		#endregion

		/** Find all graph types supported in this build.
		 * Using reflection, the assembly is searched for types which inherit from NavGraph. */
		public void FindGraphTypes () {
#if !ASTAR_FAST_NO_EXCEPTIONS && !UNITY_WINRT && !UNITY_WEBGL
			var asm = Assembly.GetAssembly(typeof(AstarPath));

			System.Type[] types = asm.GetTypes();

			var graphList = new List<System.Type>();

			foreach (System.Type type in types) {
#if NETFX_CORE && !UNITY_EDITOR
				System.Type baseType = type.GetTypeInfo().BaseType;
#else
				System.Type baseType = type.BaseType;
#endif
				while (baseType != null) {
					if (System.Type.Equals(baseType, typeof(NavGraph))) {
						graphList.Add(type);

						break;
					}

#if NETFX_CORE && !UNITY_EDITOR
					baseType = baseType.GetTypeInfo().BaseType;
#else
					baseType = baseType.BaseType;
#endif
				}
			}

			graphTypes = graphList.ToArray();

#if ASTARDEBUG
			Debug.Log("Found "+graphTypes.Length+" graph types");
#endif
#else
			graphTypes = DefaultGraphTypes;
#endif
		}

		#region GraphCreation
		/**
		 * \returns A System.Type which matches the specified \a type string. If no mathing graph type was found, null is returned
		 *
		 * \deprecated
		 */
		[System.Obsolete("If really necessary. Use System.Type.GetType instead.")]
		public System.Type GetGraphType (string type) {
			for (int i = 0; i < graphTypes.Length; i++) {
				if (graphTypes[i].Name == type) {
					return graphTypes[i];
				}
			}
			return null;
		}

		/** Creates a new instance of a graph of type \a type. If no matching graph type was found, an error is logged and null is returned
		 * \returns The created graph
		 * \see CreateGraph(System.Type)
		 *
		 * \deprecated
		 */
		[System.Obsolete("Use CreateGraph(System.Type) instead")]
		public NavGraph CreateGraph (string type) {
			Debug.Log("Creating Graph of type '"+type+"'");

			for (int i = 0; i < graphTypes.Length; i++) {
				if (graphTypes[i].Name == type) {
					return CreateGraph(graphTypes[i]);
				}
			}
			Debug.LogError("Graph type ("+type+") wasn't found");
			return null;
		}

		/** Creates a new graph instance of type \a type
		 * \see CreateGraph(string) */
		public NavGraph CreateGraph (System.Type type) {
			var g = System.Activator.CreateInstance(type) as NavGraph;

			g.active = active;
			return g;
		}

		/** Adds a graph of type \a type to the #graphs array
		 *
		 * \deprecated
		 */
		[System.Obsolete("Use AddGraph(System.Type) instead")]
		public NavGraph AddGraph (string type) {
			NavGraph graph = null;

			for (int i = 0; i < graphTypes.Length; i++) {
				if (graphTypes[i].Name == type) {
					graph = CreateGraph(graphTypes[i]);
				}
			}

			if (graph == null) {
				Debug.LogError("No NavGraph of type '"+type+"' could be found");
				return null;
			}

			AddGraph(graph);

			return graph;
		}

		/** Adds a graph of type \a type to the #graphs array */
		public NavGraph AddGraph (System.Type type) {
			NavGraph graph = null;

			for (int i = 0; i < graphTypes.Length; i++) {
				if (System.Type.Equals(graphTypes[i], type)) {
					graph = CreateGraph(graphTypes[i]);
				}
			}

			if (graph == null) {
				Debug.LogError("No NavGraph of type '"+type+"' could be found, "+graphTypes.Length+" graph types are avaliable");
				return null;
			}

			AddGraph(graph);

			return graph;
		}

		/** Adds the specified graph to the #graphs array */
		public void AddGraph (NavGraph graph) {
			// Make sure to not interfere with pathfinding
			AstarPath.active.BlockUntilPathQueueBlocked();

			// Try to fill in an empty position
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] == null) {
					graphs[i] = graph;
					graph.active = active;
					graph.Awake();
					graph.graphIndex = (uint)i;
					UpdateShortcuts();
					return;
				}
			}

			if (graphs != null && graphs.Length >= GraphNode.MaxGraphIndex) {
				throw new System.Exception("Graph Count Limit Reached. You cannot have more than " + GraphNode.MaxGraphIndex +
					" graphs. Some compiler directives can change this limit, e.g ASTAR_MORE_AREAS, look under the " +
					"'Optimizations' tab in the A* Inspector");
			}

			//Add a new entry to the list
			var ls = new List<NavGraph>(graphs);
			ls.Add(graph);
			graphs = ls.ToArray();

			UpdateShortcuts();

			graph.active = active;
			graph.Awake();
			graph.graphIndex = (uint)(graphs.Length-1);
		}

		/** Removes the specified graph from the #graphs array and Destroys it in a safe manner.
		 * To avoid changing graph indices for the other graphs, the graph is simply nulled in the array instead
		 * of actually removing it from the array.
		 * The empty position will be reused if a new graph is added.
		 *
		 * \returns True if the graph was sucessfully removed (i.e it did exist in the #graphs array). False otherwise.
		 *
		 *
		 * \version Changed in 3.2.5 to call SafeOnDestroy before removing
		 * and nulling it in the array instead of removing the element completely in the #graphs array.
		 *
		 */
		public bool RemoveGraph (NavGraph graph) {
			// Make sure all graph updates and other callbacks are done
			active.FlushWorkItems(false, true);

			// Make sure the pathfinding threads are stopped
			active.BlockUntilPathQueueBlocked();

			// //Safe OnDestroy is called since there is a risk that the pathfinding is searching through the graph right now,
			// //and if we don't wait until the search has completed we could end up with evil NullReferenceExceptions
			graph.OnDestroy();

			int i = System.Array.IndexOf(graphs, graph);

			if (i == -1) {
				return false;
			}

			graphs[i] = null;

			UpdateShortcuts();

			return true;
		}

		#endregion

		#region GraphUtility

		/** Returns the graph which contains the specified node.
		 * The graph must be in the #graphs array.
		 *
		 * \returns Returns the graph which contains the node. Null if the graph wasn't found
		 */
		public static NavGraph GetGraph (GraphNode node) {
			if (node == null) return null;

			AstarPath script = AstarPath.active;

			if (script == null) return null;

			AstarData data = script.astarData;

			if (data == null) return null;

			if (data.graphs == null) return null;

			uint graphIndex = node.GraphIndex;

			if (graphIndex >= data.graphs.Length) {
				return null;
			}

			return data.graphs[(int)graphIndex];
		}

		/** Returns the first graph of type \a type found in the #graphs array. Returns null if none was found */
		public NavGraph FindGraphOfType (System.Type type) {
			if (graphs != null) {
				for (int i = 0; i < graphs.Length; i++) {
					if (graphs[i] != null && System.Type.Equals(graphs[i].GetType(), type)) {
						return graphs[i];
					}
				}
			}
			return null;
		}

		/** Loop through this function to get all graphs of type 'type'
		 * \code foreach (GridGraph graph in AstarPath.astarData.FindGraphsOfType (typeof(GridGraph))) {
		 *  //Do something with the graph
		 * } \endcode
		 * \see AstarPath.RegisterSafeNodeUpdate */
		public IEnumerable FindGraphsOfType (System.Type type) {
			if (graphs == null) { yield break; }
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] != null && System.Type.Equals(graphs[i].GetType(), type)) {
					yield return graphs[i];
				}
			}
		}

		/** All graphs which implements the UpdateableGraph interface
		 * \code foreach (IUpdatableGraph graph in AstarPath.astarData.GetUpdateableGraphs ()) {
		 *  //Do something with the graph
		 * } \endcode
		 * \see AstarPath.RegisterSafeNodeUpdate
		 * \see Pathfinding.IUpdatableGraph */
		public IEnumerable GetUpdateableGraphs () {
			if (graphs == null) { yield break; }
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] is IUpdatableGraph) {
					yield return graphs[i];
				}
			}
		}

		/** All graphs which implements the UpdateableGraph interface
		 * \code foreach (IRaycastableGraph graph in AstarPath.astarData.GetRaycastableGraphs ()) {
		 *  //Do something with the graph
		 * } \endcode
		 * \see Pathfinding.IRaycastableGraph*/
		public IEnumerable GetRaycastableGraphs () {
			if (graphs == null) { yield break; }
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] is IRaycastableGraph) {
					yield return graphs[i];
				}
			}
		}

		/** Gets the index of the NavGraph in the #graphs array */
		public int GetGraphIndex (NavGraph graph) {
			if (graph == null) throw new System.ArgumentNullException("graph");

			if (graphs != null) {
				for (int i = 0; i < graphs.Length; i++) {
					if (graph == graphs[i]) {
						return i;
					}
				}
			}
			Debug.LogError("Graph doesn't exist");
			return -1;
		}

		#endregion
	}
}
