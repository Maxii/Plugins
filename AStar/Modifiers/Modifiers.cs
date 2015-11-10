using UnityEngine;

namespace Pathfinding {
	/** Defines inputs and outputs for a modifier */
	[System.Flags]
	public enum ModifierData {
		All					= -1,		/**< All bits set to 1 */
		StrictNodePath 		= 1 << 0,	/**< Node array with original length */
		NodePath			= 1 << 1,	/**< Node array */
		StrictVectorPath	= 1 << 2,	/**< Vector path with original length (same as node path array length). Think of it as: the node positions have changed */
		VectorPath			= 1 << 3,	/**< Vector path */
		Original			= 1 << 4,	/**< Used when the modifier requires to be the first in the list (or after a modifier outputting ModifierData.All) */
		None				= 0,		/**< Zero (no bits true) */
		Nodes				= NodePath | StrictNodePath, /**< Combine of NodePath and StrictNodePath */
		Vector				= VectorPath | StrictVectorPath /**< Combine of VectorPath and StrictVectorPath */
	}

	/** Base for all path modifiers.
	 * \see MonoModifier
	 * Modifier */
	public interface IPathModifier {
		int Priority { get; set; }

		ModifierData input { get; }
		ModifierData output { get; }

		void Apply (Path p, ModifierData source);
		void PreProcess (Path p);
	}

	/** Base class for path modifiers which are not attached to GameObjects.
	 * \see MonoModifier */
	[System.Serializable]
	public abstract class PathModifier : IPathModifier {

		[System.NonSerialized]
		public Seeker seeker;

		public abstract ModifierData input { get; }
		public abstract ModifierData output { get; }

		/** Save the priority to a field so that Unity can serialize it */
		[SerializeField]
		int priority;

		/** Higher priority modifiers are executed first */
		public int Priority {
			get {
				return priority;
			}
			set {
				priority = value;
			}
		}

		public void Awake (Seeker s) {
			seeker = s;
			if (s != null) {
				s.RegisterModifier (this);
			}
		}

		public void OnDestroy (Seeker s) {
			if (s != null) {
				s.DeregisterModifier (this);
			}
		}

		public void PreProcess (Path p) {
			// Required by IPathModifier
		}

		/** Main Post-Processing function */
		public abstract void Apply (Path p, ModifierData source);
	}

	/** Base class for path modifiers which can be attached to GameObjects.
	  * \see[AddComponentMenu("CONTEXT/Seeker/Something")] Modifier */
	[System.Serializable]
	public abstract class MonoModifier : MonoBehaviour, IPathModifier {

		public void OnEnable () {}
		public void OnDisable () {}

		[System.NonSerialized]
		public Seeker seeker;

		/** Save the priority to a field so that Unity can serialize it */
		[SerializeField]
		int priority;

		/** Higher priority modifiers are executed first */
		public int Priority {
			get {
				return priority;
			}
			set {
				priority = value;
			}
		}

		public abstract ModifierData input { get; }
		public abstract ModifierData output { get; }

		/** Alerts the Seeker that this modifier exists */
		public void Awake () {
			seeker = GetComponent<Seeker>();

			if (seeker != null) {
				seeker.RegisterModifier (this);
			}
		}

		public void OnDestroy () {

			if (seeker != null) {
				seeker.DeregisterModifier (this);
			}
		}

		public void PreProcess (Path p) {
			// Required by IPathModifier
		}

		/** Main Post-Processing function */
		public abstract void Apply (Path p, ModifierData source);
	}

	public static class ModifierConverter {

		/** Returns If \a a has all bits that \a b has set to true, also set to true */
		public static bool AllBits (ModifierData a, ModifierData b) {
			return (a & b) == b;
		}

		/** Returns If \a a and \a b has any bits in common */
		public static bool AnyBits (ModifierData a, ModifierData b) {
			return (a & b) != 0;
		}

		/** Converts a path from \a input to \a output */
		public static ModifierData Convert (Path p, ModifierData input, ModifierData output) {

			//"Input" can not be converted to "output", log error
			if (!CanConvert (input,output)) {
				Debug.LogError ("Can't convert "+input+" to "+output);
				return ModifierData.None;
			}

			//"Output" can take "input" with no change, return
			if (AnyBits (input,output)) {
				return input;
			}

			//If input is a node path, and output wants a vector array, convert the node array to a vector array
			if (AnyBits (input,ModifierData.Nodes) && AnyBits (output, ModifierData.Vector)) {
				p.vectorPath.Clear();
				for (int i=0;i<p.vectorPath.Count;i++) {
					p.vectorPath.Add ((Vector3)p.path[i].position);
				}

				//Return VectorPath and also StrictVectorPath if input has StrictNodePath set
				return ModifierData.VectorPath | (AnyBits (input, ModifierData.StrictNodePath) ? ModifierData.StrictVectorPath : ModifierData.None);
			}

			Debug.LogError ("This part should not be reached - Error in ModifierConverted\nInput: "+input+" ("+(int)input+")\nOutput: "+output+" ("+(int)output+")");
			return ModifierData.None;
		}

		/** Returns If \a input can be converted to \a output */
		public static bool CanConvert (ModifierData input, ModifierData output) {
			ModifierData convert = CanConvertTo (input);
			return AnyBits (output,convert);
		}

		/** Returns All data types \a a can be converted to */
		public static ModifierData CanConvertTo (ModifierData a) {

			if (a == ModifierData.All) {
				return ModifierData.All;
			}

			ModifierData result = a;

			if (AnyBits (a,ModifierData.Nodes)) {
				result |= ModifierData.VectorPath;
			}

			if (AnyBits (a,ModifierData.StrictNodePath)) {
				result |= ModifierData.StrictVectorPath;
			}

			if (AnyBits (a,ModifierData.StrictVectorPath)) {
				result |= ModifierData.VectorPath;
			}
			return result;
		}
	}
}
