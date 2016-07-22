using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Pathfinding.Util;
using Pathfinding.Serialization.JsonFx;
#if NETFX_CORE && !UNITY_EDITOR

#if !ASTAR_NO_ZIP
using Pathfinding.Ionic.Zip;
#else
using Pathfinding.Serialization.Zip;
#endif

#else
using CompatFileStream = System.IO.FileStream;

#if !ASTAR_NO_ZIP
using Pathfinding.Ionic.Zip;
#else
using Pathfinding.Serialization.Zip;
#endif
#endif


namespace Pathfinding.Serialization {
	/** Holds information passed to custom graph serializers */
	public class GraphSerializationContext {
		private readonly GraphNode[] id2NodeMapping;

		/** Deserialization stream.
		 * Will only be set when deserializing
		 */
		public readonly BinaryReader reader;

		/** Serialization stream.
		 * Will only be set when serializing
		 */
		public readonly BinaryWriter writer;

		/** Index of the graph which is currently being processed.
		 * \version uint instead of int after 3.7.5
		 */
		public readonly uint graphIndex;

		public GraphSerializationContext (BinaryReader reader, GraphNode[] id2NodeMapping, uint graphIndex) {
			this.reader = reader;
			this.id2NodeMapping = id2NodeMapping;
			this.graphIndex = graphIndex;
		}

		public GraphSerializationContext (BinaryWriter writer) {
			this.writer = writer;
		}

		public int GetNodeIdentifier (GraphNode node) {
			return node == null ? -1 : node.NodeIndex;
		}

		public GraphNode GetNodeFromIdentifier (int id) {
			if (id2NodeMapping == null) throw new Exception("Calling GetNodeFromIdentifier when serializing");

			if (id == -1) return null;
			GraphNode node = id2NodeMapping[id];
			if (node == null) throw new Exception("Invalid id ("+id+")");
			return node;
		}

#if ASTAR_NO_JSON
		/** Write a Vector3 */
		public void SerializeVector3 (Vector3 v) {
			writer.Write(v.x);
			writer.Write(v.y);
			writer.Write(v.z);
		}

		/** Read a Vector3 */
		public Vector3 DeserializeVector3 () {
			return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		public int DeserializeInt (int defaultValue) {
			if (reader.BaseStream.Position <= reader.BaseStream.Length-4) {
				return reader.ReadInt32();
			} else {
				return defaultValue;
			}
		}

		public float DeserializeFloat (float defaultValue) {
			if (reader.BaseStream.Position <= reader.BaseStream.Length-4) {
				return reader.ReadSingle();
			} else {
				return defaultValue;
			}
		}

		/** Write a UnityEngine.Object */
		public void SerializeUnityObject (UnityEngine.Object ob) {
			if (ob == null) {
				writer.Write(int.MaxValue);
				return;
			}

			int inst = ob.GetInstanceID();
			string name = ob.name;
			string type = ob.GetType().AssemblyQualifiedName;
			string guid = "";

			//Write scene path if the object is a Component or GameObject
			Component component = ob as Component;
			GameObject go = ob as GameObject;

			if (component != null || go != null) {
				if (component != null && go == null) {
					go = component.gameObject;
				}

				UnityReferenceHelper helper = go.GetComponent<UnityReferenceHelper>();

				if (helper == null) {
					Debug.Log("Adding UnityReferenceHelper to Unity Reference '"+ob.name+"'");
					helper = go.AddComponent<UnityReferenceHelper>();
				}

				//Make sure it has a unique GUID
				helper.Reset();

				guid = helper.GetGUID();
			}


			writer.Write(inst);
			writer.Write(name);
			writer.Write(type);
			writer.Write(guid);
		}

		/** Read a UnityEngine.Object */
		public UnityEngine.Object DeserializeUnityObject ( ) {
			int inst = reader.ReadInt32();

			if (inst == int.MaxValue) {
				return null;
			}

			string name = reader.ReadString();
			string typename = reader.ReadString();
			string guid = reader.ReadString();

			System.Type type = System.Type.GetType(typename);

			if (type == null) {
				Debug.LogError("Could not find type '"+typename+"'. Cannot deserialize Unity reference");
				return null;
			}

			if (!string.IsNullOrEmpty(guid)) {
				UnityReferenceHelper[] helpers = UnityEngine.Object.FindObjectsOfType(typeof(UnityReferenceHelper)) as UnityReferenceHelper[];

				for (int i = 0; i < helpers.Length; i++) {
					if (helpers[i].GetGUID() == guid) {
						if (type == typeof(GameObject)) {
							return helpers[i].gameObject;
						} else {
							return helpers[i].GetComponent(type);
						}
					}
				}
			}

			//Try to load from resources
			UnityEngine.Object[] objs = Resources.LoadAll(name, type);

			for (int i = 0; i < objs.Length; i++) {
				if (objs[i].name == name || objs.Length == 1) {
					return objs[i];
				}
			}

			return null;
		}
#endif
	}

	/** Handles low level serialization and deserialization of graph settings and data.
	 * Mostly for internal use. You can use the methods in the AstarData class for
	 * higher level serialization and deserialization.
	 *
	 * \see AstarData
	 */
	public class AstarSerializer {
		private AstarData data;

#if !ASTAR_NO_JSON
		public JsonWriterSettings writerSettings;
		public JsonReaderSettings readerSettings;
#endif

		/** Zip which the data is loaded from */
		private ZipFile zip;

		/** Memory stream with the zip data */
		private MemoryStream zipStream;

		/** Graph metadata */
		private GraphMeta meta;

		/** Settings for serialization */
		private SerializeSettings settings;

		/** Graphs that are being serialized or deserialized */
		private NavGraph[] graphs;

		/** Index used for the graph in the file.
		 * If some graphs were null in the file then graphIndexInZip[graphs[i]] may not equal i.
		 * Used for deserialization.
		 */
		private Dictionary<NavGraph, int> graphIndexInZip;

		private int graphIndexOffset;

		/** Extension to use for binary files */
		const string binaryExt = ".binary";
#if !ASTAR_NO_JSON
		/** Extension to use for json files */
		const string jsonExt = ".json";
#else
		const string jsonExt = binaryExt;
#endif

		/** Checksum for the serialized data.
		 * Used to provide a quick equality check in editor code
		 */
		private uint checksum = 0xffffffff;

#if !ASTAR_NO_JSON
		System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
#endif

		/** Cached StringBuilder to avoid excessive allocations */
		static System.Text.StringBuilder _stringBuilder = new System.Text.StringBuilder();

		/** Returns a cached StringBuilder.
		 * This function only has one string builder cached and should
		 * thus only be called from a single thread and should not be called while using an earlier got string builder.
		 */
		static System.Text.StringBuilder GetStringBuilder () { _stringBuilder.Length = 0; return _stringBuilder; }

		public AstarSerializer (AstarData data) {
			this.data = data;
			settings = SerializeSettings.Settings;
		}

		public AstarSerializer (AstarData data, SerializeSettings settings) {
			this.data = data;
			this.settings = settings;
		}

		public void SetGraphIndexOffset (int offset) {
			graphIndexOffset = offset;
		}

		void AddChecksum (byte[] bytes) {
			checksum = Checksum.GetChecksum(bytes, checksum);
		}

		public uint GetChecksum () { return checksum; }

		#region Serialize

		public void OpenSerialize () {
			// Create a new zip file, here we will store all the data
			zip = new ZipFile();
			zip.AlternateEncoding = System.Text.Encoding.UTF8;
			zip.AlternateEncodingUsage = ZipOption.Always;

#if !ASTAR_NO_JSON
			// Add some converters so that we can serialize some Unity types
			writerSettings = new JsonWriterSettings();
			writerSettings.AddTypeConverter(new VectorConverter());
			writerSettings.AddTypeConverter(new BoundsConverter());
			writerSettings.AddTypeConverter(new LayerMaskConverter());
			writerSettings.AddTypeConverter(new MatrixConverter());
			writerSettings.AddTypeConverter(new GuidConverter());
			writerSettings.AddTypeConverter(new UnityObjectConverter());

			writerSettings.PrettyPrint = settings.prettyPrint;
#endif
			meta = new GraphMeta();
		}

		public byte[] CloseSerialize () {
			// As the last step, serialize metadata
			byte[] bytes = SerializeMeta();
			AddChecksum(bytes);
			zip.AddEntry("meta"+jsonExt, bytes);

#if !ASTAR_NO_ZIP
			// Set dummy dates on every file to prevent the binary data to change
			// for identical settings and graphs.
			// Prevents the scene from being marked as dirty in the editor
			// If ASTAR_NO_ZIP is defined this is not relevant since the replacement zip
			// implementation does not even store dates
			var dummy = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			foreach (var entry in zip.Entries) {
				entry.AccessedTime = dummy;
				entry.CreationTime = dummy;
				entry.LastModified = dummy;
				entry.ModifiedTime = dummy;
			}
#endif

			// Save all entries to a single byte array
			var output = new MemoryStream();
			zip.Save(output);
			bytes = output.ToArray();
			output.Dispose();

#if ASTARDEBUG
			CompatFileStream fs = new CompatFileStream("output.zip", FileMode.Create);
			fs.Write(bytes, 0, bytes.Length);
			fs.Close();
#endif

			zip.Dispose();

			zip = null;
			return bytes;
		}

		public void SerializeGraphs (NavGraph[] _graphs) {
			if (graphs != null) throw new InvalidOperationException("Cannot serialize graphs multiple times.");
			graphs = _graphs;

			if (zip == null) throw new NullReferenceException("You must not call CloseSerialize before a call to this function");

			if (graphs == null) graphs = new NavGraph[0];

			for (int i = 0; i < graphs.Length; i++) {
				//Ignore graph if null
				if (graphs[i] == null) continue;

				// Serialize the graph to a byte array
				byte[] bytes = Serialize(graphs[i]);

				AddChecksum(bytes);
				zip.AddEntry("graph"+i+jsonExt, bytes);
			}
		}

		/** Serialize metadata about all graphs */
		byte[] SerializeMeta () {
			if (graphs == null) throw new System.Exception("No call to SerializeGraphs has been done");

			meta.version = AstarPath.Version;
			meta.graphs = graphs.Length;
			meta.guids = new string[graphs.Length];
			meta.typeNames = new string[graphs.Length];
			meta.nodeCounts = new int[graphs.Length];

			// For each graph, save the guid
			// of the graph and the type of it
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] == null) continue;

				meta.guids[i] = graphs[i].guid.ToString();
				meta.typeNames[i] = graphs[i].GetType().FullName;
			}

#if !ASTAR_NO_JSON
			// Grab a cached string builder to avoid allocations
			var output = GetStringBuilder();
			var writer = new JsonWriter(output, writerSettings);
			writer.Write(meta);

			return encoding.GetBytes(output.ToString());
#else
			// Serialize the metadata without using json for compatibility
			var mem = new System.IO.MemoryStream();
			var writer = new System.IO.BinaryWriter(mem);
			writer.Write("A*");    // Magic string
			writer.Write(meta.version.Major);
			writer.Write(meta.version.Minor);
			writer.Write(meta.version.Build);
			writer.Write(meta.version.Revision);
			writer.Write(meta.graphs);

			writer.Write(meta.guids.Length);
			for (int i = 0; i < meta.guids.Length; i++) writer.Write(meta.guids[i] ?? "");

			writer.Write(meta.typeNames.Length);
			for (int i = 0; i < meta.typeNames.Length; i++) writer.Write(meta.typeNames[i] ?? "");

			writer.Write(meta.nodeCounts.Length);
			for (int i = 0; i < meta.nodeCounts.Length; i++) writer.Write(meta.nodeCounts[i]);

			return mem.ToArray();
#endif
		}

		/** Serializes the graph settings to JSON and returns the data */
		public byte[] Serialize (NavGraph graph) {
#if !ASTAR_NO_JSON
			// Grab a cached string builder to avoid allocations
			var output = GetStringBuilder();
			var writer = new JsonWriter(output, writerSettings);
			writer.Write(graph);

			return encoding.GetBytes(output.ToString());
#else
			var mem = new System.IO.MemoryStream();
			var writer = new System.IO.BinaryWriter(mem);
			var ctx = new GraphSerializationContext(writer);
			graph.SerializeSettings(ctx);
			return mem.ToArray();
#endif
		}

		/** Deprecated method to serialize node data.
		 * \deprecated Not used anymore
		 */
		[System.Obsolete("Not used anymore. You can safely remove the call to this function.")]
		public void SerializeNodes () {
		}

		static int GetMaxNodeIndexInAllGraphs (NavGraph[] graphs) {
			int maxIndex = 0;

			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] == null) continue;
				graphs[i].GetNodes(node => {
					maxIndex = Math.Max(node.NodeIndex, maxIndex);
					if (node.NodeIndex == -1) {
						Debug.LogError("Graph contains destroyed nodes. This is a bug.");
					}
					return true;
				});
			}
			return maxIndex;
		}

		static byte[] SerializeNodeIndices (NavGraph[] graphs) {
			var stream = new MemoryStream();
			var wr = new BinaryWriter(stream);

			int maxNodeIndex = GetMaxNodeIndexInAllGraphs(graphs);

			wr.Write(maxNodeIndex);

			// While writing node indices, verify that the max node index is the same
			// (user written graphs might have gotten it wrong)
			int maxNodeIndex2 = 0;
			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] == null) continue;
				graphs[i].GetNodes(node => {
					maxNodeIndex2 = Math.Max(node.NodeIndex, maxNodeIndex2);
					wr.Write(node.NodeIndex);
					return true;
				});
			}

			// Nice to verify if users are writing their own graph types
			if (maxNodeIndex2 != maxNodeIndex) throw new Exception("Some graphs are not consistent in their GetNodes calls, sequential calls give different results.");

			byte[] bytes = stream.ToArray();
#if NETFX_CORE
			wr.Dispose();
#else
			wr.Close();
#endif

			return bytes;
		}

		/** Serializes info returned by NavGraph.SerializeExtraInfo */
		static byte[] SerializeGraphExtraInfo (NavGraph graph) {
			var stream = new MemoryStream();
			var wr = new BinaryWriter(stream);
			var ctx = new GraphSerializationContext(wr);

			graph.SerializeExtraInfo(ctx);
			byte[] bytes = stream.ToArray();

#if NETFX_CORE
			wr.Dispose();
#else
			wr.Close();
#endif

			return bytes;
		}

		/** Used to serialize references to other nodes e.g connections.
		 * Nodes use the GraphSerializationContext.GetNodeIdentifier and
		 * GraphSerializationContext.GetNodeFromIdentifier methods
		 * for serialization and deserialization respectively.
		 */
		static byte[] SerializeGraphNodeReferences (NavGraph graph) {
			var stream = new MemoryStream();
			var wr = new BinaryWriter(stream);
			var ctx = new GraphSerializationContext(wr);

			graph.GetNodes(node => {
				node.SerializeReferences(ctx);
				return true;
			});

#if NETFX_CORE
			wr.Dispose();
#else
			wr.Close();
#endif

			var bytes = stream.ToArray();
			return bytes;
		}

		public void SerializeExtraInfo () {
			if (!settings.nodes) return;
			if (graphs == null) throw new InvalidOperationException("Cannot serialize extra info with no serialized graphs (call SerializeGraphs first)");

			var bytes = SerializeNodeIndices(graphs);
			AddChecksum(bytes);
			zip.AddEntry("graph_references"+binaryExt, bytes);

			for (int i = 0; i < graphs.Length; i++) {
				if (graphs[i] == null) continue;

				bytes = SerializeGraphExtraInfo(graphs[i]);
				AddChecksum(bytes);
				zip.AddEntry("graph"+i+"_extra"+binaryExt, bytes);

				bytes = SerializeGraphNodeReferences(graphs[i]);
				AddChecksum(bytes);
				zip.AddEntry("graph"+i+"_references"+binaryExt, bytes);
			}
		}

		public void SerializeEditorSettings (GraphEditorBase[] editors) {
			if (editors == null || !settings.editorSettings) return;

#if !ASTAR_NO_JSON
			for (int i = 0; i < editors.Length; i++) {
				if (editors[i] == null) return;

				var output = GetStringBuilder();
				var writer = new JsonWriter(output, writerSettings);
				writer.Write(editors[i]);

				var bytes = encoding.GetBytes(output.ToString());

				//Less or equal to 2 bytes means that nothing was saved (file is "{}")
				if (bytes.Length <= 2)
					continue;

				AddChecksum(bytes);
				zip.AddEntry("graph"+i+"_editor"+jsonExt, bytes);
			}
#endif
		}

		#endregion

		#region Deserialize

		public bool OpenDeserialize (byte[] bytes) {
#if !ASTAR_NO_JSON
			// Add some converters so that we can deserialize Unity builtin types
			readerSettings = new JsonReaderSettings();
			readerSettings.AddTypeConverter(new VectorConverter());
			readerSettings.AddTypeConverter(new BoundsConverter());
			readerSettings.AddTypeConverter(new LayerMaskConverter());
			readerSettings.AddTypeConverter(new MatrixConverter());
			readerSettings.AddTypeConverter(new GuidConverter());
			readerSettings.AddTypeConverter(new UnityObjectConverter());
#endif

			// Copy the bytes to a stream
			zipStream = new MemoryStream();
			zipStream.Write(bytes, 0, bytes.Length);
			zipStream.Position = 0;
			try {
				zip = ZipFile.Read(zipStream);
			} catch (Exception e) {
				// Catches exceptions when an invalid zip file is found
				Debug.LogError("Caught exception when loading from zip\n"+e);

				zipStream.Dispose();
				return false;
			}

			meta = DeserializeMeta(zip["meta"+jsonExt]);

			if (FullyDefinedVersion(meta.version) > FullyDefinedVersion(AstarPath.Version)) {
				Debug.LogWarning("Trying to load data from a newer version of the A* Pathfinding Project\nCurrent version: "+AstarPath.Version+" Data version: "+meta.version +
					"\nThis is usually fine as the stored data is usually backwards and forwards compatible." +
					"\nHowever node data (not settings) can get corrupted between versions (even though I try my best to keep compatibility), so it is recommended " +
					"to recalculate any caches (those for faster startup) and resave any files. Even if it seems to load fine, it might cause subtle bugs.\n");
			} else if (FullyDefinedVersion(meta.version) < FullyDefinedVersion(AstarPath.Version)) {
				Debug.LogWarning("Trying to load data from an older version of the A* Pathfinding Project\nCurrent version: "+AstarPath.Version+" Data version: "+meta.version+
					"\nThis is usually fine, it just means you have upgraded to a new version." +
					"\nHowever node data (not settings) can get corrupted between versions (even though I try my best to keep compatibility), so it is recommended " +
					"to recalculate any caches (those for faster startup) and resave any files. Even if it seems to load fine, it might cause subtle bugs.\n");
			}
			return true;
		}

		/** Returns a version with all fields fully defined.
		 * This is used because by default new Version(3,0,0) > new Version(3,0).
		 * This is not the desired behaviour so we make sure that all fields are defined here
		 */
		static System.Version FullyDefinedVersion (System.Version v) {
			return new System.Version(Mathf.Max(v.Major, 0), Mathf.Max(v.Minor, 0), Mathf.Max(v.Build, 0), Mathf.Max(v.Revision, 0));
		}

		public void CloseDeserialize () {
			zipStream.Dispose();
			zip.Dispose();
			zip = null;
			zipStream = null;
		}

		NavGraph DeserializeGraph (int zipIndex, int graphIndex) {
			// Get the graph type from the metadata we deserialized earlier
			var tp = meta.GetGraphType(zipIndex);

			// Graph was null when saving, ignore
			if (System.Type.Equals(tp, null)) return null;

			var entry = zip["graph"+zipIndex+jsonExt];

			if (entry == null)
				throw new FileNotFoundException("Could not find data for graph "+zipIndex+" in zip. Entry 'graph"+zipIndex+jsonExt+"' does not exist");

			// Create a new graph of the right type
			NavGraph graph = data.CreateGraph(tp);
			graph.graphIndex = (uint)(graphIndex);

#if !ASTAR_NO_JSON
			var entryText = GetString(entry);
			var reader = new JsonReader(entryText, readerSettings);

			// Read the graph settings
			reader.PopulateObject(ref graph);
#else
			var reader = GetBinaryReader(entry);
			var ctx = new GraphSerializationContext(reader, null, graph.graphIndex);
			graph.DeserializeSettings(ctx);
#endif

			if (graph.guid.ToString() != meta.guids[zipIndex])
				throw new Exception("Guid in graph file not equal to guid defined in meta file. Have you edited the data manually?\n"+graph.guid+" != "+meta.guids[zipIndex]);

			return graph;
		}

		/** Deserializes graph settings.
		 * \note Stored in files named "graph#.json" where # is the graph number.
		 */
		public NavGraph[] DeserializeGraphs () {
			// Allocate a list of graphs to be deserialized
			var graphList = new List<NavGraph>();

			graphIndexInZip = new Dictionary<NavGraph, int>();

			for (int i = 0; i < meta.graphs; i++) {
				var newIndex = graphList.Count + graphIndexOffset;
				var graph = DeserializeGraph(i, newIndex);
				if (graph != null) {
					graphList.Add(graph);
					graphIndexInZip[graph] = i;
				}
			}

			graphs = graphList.ToArray();
			return graphs;
		}

		bool DeserializeExtraInfo (NavGraph graph) {
			var zipIndex = graphIndexInZip[graph];
			var entry = zip["graph"+zipIndex+"_extra"+binaryExt];

			if (entry == null)
				return false;

			var reader = GetBinaryReader(entry);

			var ctx = new GraphSerializationContext(reader, null, graph.graphIndex);

			// Call the graph to process the data
			graph.DeserializeExtraInfo(ctx);
			return true;
		}

		bool AnyDestroyedNodesInGraphs () {
			bool result = false;

			for (int i = 0; i < graphs.Length; i++) {
				graphs[i].GetNodes(node => {
					if (node.Destroyed) {
						result = true;
					}
					return true;
				});
			}
			return result;
		}

		GraphNode[] DeserializeNodeReferenceMap () {
			// Get the file containing the list of all node indices
			// This is correlated with the new indices of the nodes and a mapping from old to new
			// is done so that references can be resolved
			var entry = zip["graph_references"+binaryExt];

			if (entry == null) throw new Exception("Node references not found in the data. Was this loaded from an older version of the A* Pathfinding Project?");

			var reader = GetBinaryReader(entry);
			int maxNodeIndex = reader.ReadInt32();
			var int2Node = new GraphNode[maxNodeIndex+1];

			try {
				for (int i = 0; i < graphs.Length; i++) {
					graphs[i].GetNodes(node => {
						var index = reader.ReadInt32();
						int2Node[index] = node;
						return true;
					});
				}
			} catch (Exception e) {
				throw new Exception("Some graph(s) has thrown an exception during GetNodes, or some graph(s) have deserialized more or fewer nodes than were serialized", e);
			}

			if (reader.BaseStream.Position != reader.BaseStream.Length) {
				throw new Exception((reader.BaseStream.Length / 4) + " nodes were serialized, but only data for " + (reader.BaseStream.Position / 4) + " nodes was found. The data looks corrupt.");
			}

#if NETFX_CORE
			reader.Dispose();
#else
			reader.Close();
#endif

			return int2Node;
		}

		void DeserializeNodeReferences (NavGraph graph, GraphNode[] int2Node) {
			var zipIndex = graphIndexInZip[graph];
			var entry = zip["graph"+zipIndex+"_references"+binaryExt];

			if (entry == null) throw new Exception("Node references for graph " + zipIndex + " not found in the data. Was this loaded from an older version of the A* Pathfinding Project?");

			var reader = GetBinaryReader(entry);
			var ctx = new GraphSerializationContext(reader, int2Node, graph.graphIndex);

			graph.GetNodes(node => {
				node.DeserializeReferences(ctx);
				return true;
			});
		}

		/** Deserializes extra graph info.
		 * Extra graph info is specified by the graph types.
		 * \see Pathfinding.NavGraph.DeserializeExtraInfo
		 * \note Stored in files named "graph#_extra.binary" where # is the graph number.
		 */
		public void DeserializeExtraInfo () {
			bool anyDeserialized = false;

			// Loop through all graphs and deserialize the extra info
			// if there is any such info in the zip file
			for (int i = 0; i < graphs.Length; i++) {
				anyDeserialized |= DeserializeExtraInfo(graphs[i]);
			}

			if (!anyDeserialized) {
				return;
			}

			// Sanity check
			// Make sure the graphs don't contain destroyed nodes
			if (AnyDestroyedNodesInGraphs()) {
				Debug.LogError("Graph contains destroyed nodes. This is a bug.");
			}

			// Deserialize map from old node indices to new nodes
			var int2Node = DeserializeNodeReferenceMap();

			// Deserialize node references
			for (int i = 0; i < graphs.Length; i++) {
				DeserializeNodeReferences(graphs[i], int2Node);
			}
		}

		/** Calls PostDeserialization on all loaded graphs */
		public void PostDeserialization () {
			for (int i = 0; i < graphs.Length; i++) {
				graphs[i].PostDeserialization();
			}
		}

		/** Deserializes graph editor settings.
		 * For future compatibility this method does not assume that the \a graphEditors array matches the #graphs array in order and/or count.
		 * It searches for a matching graph (matching if graphEditor.target == graph) for every graph editor.
		 * Multiple graph editors should not refer to the same graph.\n
		 * \note Stored in files named "graph#_editor.json" where # is the graph number.
		 */
		public void DeserializeEditorSettings (GraphEditorBase[] graphEditors) {
#if !ASTAR_NO_JSON
			if (graphEditors == null) return;

			for (int i = 0; i < graphEditors.Length; i++) {
				if (graphEditors[i] == null) continue;
				for (int j = 0; j < graphs.Length; j++) {
					if (graphEditors[i].target != graphs[j]) continue;

					var zipIndex = graphIndexInZip[graphs[j]];
					ZipEntry entry = zip["graph"+zipIndex+"_editor"+jsonExt];
					if (entry == null) continue;

					string entryText = GetString(entry);

					var reader = new JsonReader(entryText, readerSettings);
					GraphEditorBase graphEditor = graphEditors[i];
					reader.PopulateObject(ref graphEditor);
					graphEditors[i] = graphEditor;
					break;
				}
			}
#endif
		}

		/** Returns a binary reader for the data in the zip entry */
		private static BinaryReader GetBinaryReader (ZipEntry entry) {
			var mem = new System.IO.MemoryStream();

			entry.Extract(mem);
			mem.Position = 0;
			return new System.IO.BinaryReader(mem);
		}

		/** Returns the data in the zip entry as a string */
		private static string GetString (ZipEntry entry) {
			var buffer = new MemoryStream();

			entry.Extract(buffer);
			buffer.Position = 0;
			var reader = new StreamReader(buffer);
			string s = reader.ReadToEnd();
			buffer.Position = 0;
			reader.Dispose();
			return s;
		}

		private GraphMeta DeserializeMeta (ZipEntry entry) {
			if (entry == null) throw new Exception("No metadata found in serialized data.");

#if !ASTAR_NO_JSON
			string s = GetString(entry);

			var reader = new JsonReader(s, readerSettings);
			return (GraphMeta)reader.Deserialize(typeof(GraphMeta));
#else
			var meta = new GraphMeta();

			var reader = GetBinaryReader(entry);
			if (reader.ReadString() != "A*") throw new System.Exception("Invalid magic number in saved data");
			int major = reader.ReadInt32();
			int minor = reader.ReadInt32();
			int build = reader.ReadInt32();
			int revision = reader.ReadInt32();

			// Required because when saving a version with a field not set, it will save it as -1
			// and then the Version constructor will throw an exception (which we do not want)
			if (major < 0) meta.version = new Version(0, 0);
			else if (minor < 0) meta.version = new Version(major, 0);
			else if (build < 0) meta.version = new Version(major, minor);
			else if (revision < 0) meta.version = new Version(major, minor, build);
			else meta.version = new Version(major, minor, build, revision);

			meta.graphs = reader.ReadInt32();

			meta.guids = new string[reader.ReadInt32()];
			for (int i = 0; i < meta.guids.Length; i++) meta.guids[i] = reader.ReadString();

			meta.typeNames = new string[reader.ReadInt32()];
			for (int i = 0; i < meta.typeNames.Length; i++) meta.typeNames[i] = reader.ReadString();

			meta.nodeCounts = new int[reader.ReadInt32()];
			for (int i = 0; i < meta.nodeCounts.Length; i++) meta.nodeCounts[i] = reader.ReadInt32();

			return meta;
#endif
		}


		#endregion

		#region Utils

		/** Save the specified data at the specified path */
		public static void SaveToFile (string path, byte[] data) {
#if NETFX_CORE
			throw new System.NotSupportedException("Cannot save to file on this platform");
#else
			using (var stream = new FileStream(path, FileMode.Create)) {
				stream.Write(data, 0, data.Length);
			}
#endif
		}

		/** Load the specified data from the specified path */
		public static byte[] LoadFromFile (string path) {
#if NETFX_CORE
			throw new System.NotSupportedException("Cannot load from file on this platform");
#else
			using (var stream = new FileStream(path, FileMode.Open)) {
				var bytes = new byte[(int)stream.Length];
				stream.Read(bytes, 0, (int)stream.Length);
				return bytes;
			}
#endif
		}

		#endregion
	}

	/** Metadata for all graphs included in serialization */
	class GraphMeta {
		/** Project version it was saved with */
		public Version version;

		/** Number of graphs serialized */
		public int graphs;

		/** Guids for all graphs */
		public string[] guids;

		/** Type names for all graphs */
		public string[] typeNames;

		/** Number of nodes for every graph. Nodes are not necessarily serialized */
		public int[] nodeCounts;

		/** Returns the Type of graph number \a i */
		public Type GetGraphType (int i) {
			// The graph was null when saving. Ignore it
			if (String.IsNullOrEmpty(typeNames[i])) return null;

#if ASTAR_FAST_NO_EXCEPTIONS || UNITY_WEBGL
			System.Type[] types = AstarData.DefaultGraphTypes;

			Type type = null;
			for (int j = 0; j < types.Length; j++) {
				if (types[j].FullName == typeNames[i]) type = types[j];
			}
#else
			Type type = Type.GetType(typeNames[i]);
#endif
			if (!System.Type.Equals(type, null))
				return type;

			throw new Exception("No graph of type '" + typeNames[i] + "' could be created, type does not exist");
		}
	}

	/** Holds settings for how graphs should be serialized */
	public class SerializeSettings {
		/** Enable to include node data.
		 * If false, only settings will be saved
		 */
		public bool nodes = true;

		/** Use pretty printing for the json data.
		 * Good if you want to open up the saved data and edit it manually
		 */
		public bool prettyPrint;

		/** Save editor settings.
		 * \warning Only applicable when saving from the editor using the AstarPathEditor methods
		 */
		public bool editorSettings;

		/** Serialization settings for only saving graph settings */
		public static SerializeSettings Settings {
			get {
				var s = new SerializeSettings();
				s.nodes = false;
				return s;
			}
		}

		/** Serialization settings for saving everything that can be saved.
		 * This includes all node data
		 */
		public static SerializeSettings All {
			get {
				var s = new SerializeSettings();
				s.nodes = true;
				return s;
			}
		}
	}
}
