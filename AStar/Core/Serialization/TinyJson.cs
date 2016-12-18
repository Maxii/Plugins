using UnityEngine;
using Pathfinding;
using System.Collections.Generic;
using Pathfinding.WindowsStore;
using System;

namespace Pathfinding.Serialization {
	public class JsonMemberAttribute : System.Attribute {}
	public class JsonOptInAttribute : System.Attribute {}

	/** A very tiny json serializer.
	 * It is not supposed to have lots of features, it is only intended to be able to serialize graph settings
	 * well enough.
	 */
	public class TinyJsonSerializer {
		System.Text.StringBuilder output = new System.Text.StringBuilder();

		Dictionary<Type, Action<System.Object> > serializers = new Dictionary<Type, Action<object> >();

		static readonly System.Globalization.CultureInfo invariantCulture = System.Globalization.CultureInfo.InvariantCulture;

		public static void Serialize (System.Object obj, System.Text.StringBuilder output) {
			new TinyJsonSerializer() {
				output = output
			}.Serialize(obj);
		}

		TinyJsonSerializer () {
			serializers[typeof(float)] = v => output.Append(((float)v).ToString("R", invariantCulture));
			serializers[typeof(Version)] = serializers[typeof(bool)] = serializers[typeof(uint)] = serializers[typeof(int)] = v => output.Append(v.ToString());
			serializers[typeof(string)] = v => output.AppendFormat("\"{0}\"", v);
			serializers[typeof(Vector2)] = v => output.AppendFormat("{{ \"x\": {0}, \"y\": {1} }}", ((Vector2)v).x.ToString("R", invariantCulture), ((Vector2)v).y.ToString("R", invariantCulture));
			serializers[typeof(Vector3)] = v => output.AppendFormat("{{ \"x\": {0}, \"y\": {1}, \"z\": {2} }}", ((Vector3)v).x.ToString("R", invariantCulture), ((Vector3)v).y.ToString("R", invariantCulture), ((Vector3)v).z.ToString("R", invariantCulture));
			serializers[typeof(Pathfinding.Util.Guid)] = v => output.AppendFormat("{{ \"value\": \"{0}\" }}", v.ToString());
			serializers[typeof(LayerMask)] = v => output.AppendFormat("{{ \"value\": {0} }}", ((int)(LayerMask)v).ToString());
		}

		void Serialize (System.Object obj) {
			if (obj == null) {
				output.Append("null");
				return;
			}

			var tp = WindowsStoreCompatibility.GetTypeInfo(obj.GetType());
			if (serializers.ContainsKey(tp)) {
				serializers[tp] (obj);
			} else if (tp.IsEnum) {
				output.Append('"' + obj.ToString() + '"');
			} else if (obj is System.Collections.IList) {
				output.Append("[");
				var arr = obj as System.Collections.IList;
				for (int i = 0; i < arr.Count; i++) {
					if (i != 0)
						output.Append(", ");
					Serialize(arr[i]);
				}
				output.Append("]");
			} else if (obj is UnityEngine.Object) {
				SerializeUnityObject(obj as UnityEngine.Object);
			} else {
				var optIn = tp.GetCustomAttributes(typeof(JsonOptInAttribute), true).Length > 0;
				output.Append("{");
				var fields = tp.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
				bool earlier = false;
				foreach (var field in fields) {
					if ((!optIn && field.IsPublic) || field.GetCustomAttributes(typeof(JsonMemberAttribute), true).Length > 0) {
						if (earlier) {
							output.Append(", ");
						}

						earlier = true;
						output.AppendFormat("\"{0}\": ", field.Name);
						Serialize(field.GetValue(obj));
					}
				}
				output.Append("}");
			}
		}

		void QuotedField (string name, string contents) {
			output.AppendFormat("\"{0}\": \"{1}\"", name, contents);
		}

		void SerializeUnityObject (UnityEngine.Object obj) {
			// Note that a unityengine can be destroyed as well
			if (obj == null) {
				Serialize(null);
				return;
			}

			output.Append("{");
			QuotedField("Name", obj.name);
			output.Append(", ");
			QuotedField("Type", obj.GetType().FullName);

			//Write scene path if the object is a Component or GameObject
			var component = obj as Component;
			var go = obj as GameObject;

			if (component != null || go != null) {
				if (component != null && go == null) {
					go = component.gameObject;
				}

				var helper = go.GetComponent<UnityReferenceHelper>();

				if (helper == null) {
					Debug.Log("Adding UnityReferenceHelper to Unity Reference '"+obj.name+"'");
					helper = go.AddComponent<UnityReferenceHelper>();
				}

				//Make sure it has a unique GUID
				helper.Reset();
				output.Append(", ");
				QuotedField("GUID", helper.GetGUID().ToString());
			}
			output.Append("}");
		}
	}

	/** A very tiny json deserializer.
	 * It is not supposed to have lots of features, it is only intended to be able to deserialize graph settings
	 * well enough. Not much validation of the input is done.
	 */
	public class TinyJsonDeserializer {
		System.IO.TextReader reader;

		static readonly System.Globalization.NumberFormatInfo numberFormat = System.Globalization.NumberFormatInfo.InvariantInfo;

		/** Deserializes an object of the specified type.
		 * Will load all fields into the \a populate object if it is set (only works for classes).
		 */
		public static System.Object Deserialize (string text, Type type, System.Object populate = null) {
			return new TinyJsonDeserializer() {
					   reader = new System.IO.StringReader(text)
			}.Deserialize(type, populate);
		}

		/** Deserializes an object of type tp.
		 * Will load all fields into the \a populate object if it is set (only works for classes).
		 */
		System.Object Deserialize (Type tp, System.Object populate = null) {
			var tpInfo = WindowsStoreCompatibility.GetTypeInfo(tp);

			if (tpInfo.IsEnum) {
				return Enum.Parse(tp, EatField());
			} else if (TryEat('n')) {
				Eat("ull");
				return null;
			} else if (Type.Equals(tp, typeof(float))) {
				return float.Parse(EatField(), numberFormat);
			} else if (Type.Equals(tp, typeof(int))) {
				return int.Parse(EatField(), numberFormat);
			} else if (Type.Equals(tp, typeof(uint))) {
				return uint.Parse(EatField(), numberFormat);
			} else if (Type.Equals(tp, typeof(bool))) {
				return bool.Parse(EatField());
			} else if (Type.Equals(tp, typeof(string))) {
				return EatField();
			} else if (Type.Equals(tp, typeof(Version))) {
				return new Version(EatField());
			} else if (Type.Equals(tp, typeof(Vector2))) {
				Eat("{");
				var res = new Vector2();
				EatField();
				res.x = float.Parse(EatField(), numberFormat);
				EatField();
				res.y = float.Parse(EatField(), numberFormat);
				Eat("}");
				return res;
			} else if (Type.Equals(tp, typeof(Vector3))) {
				Eat("{");
				var res = new Vector3();
				EatField();
				res.x = float.Parse(EatField(), numberFormat);
				EatField();
				res.y = float.Parse(EatField(), numberFormat);
				EatField();
				res.z = float.Parse(EatField(), numberFormat);
				Eat("}");
				return res;
			} else if (Type.Equals(tp, typeof(Pathfinding.Util.Guid))) {
				Eat("{");
				EatField();
				var res = Pathfinding.Util.Guid.Parse(EatField());
				Eat("}");
				return res;
			} else if (Type.Equals(tp, typeof(LayerMask))) {
				Eat("{");
				EatField();
				var res = (LayerMask)int.Parse(EatField());
				Eat("}");
				return res;
			} else if (Type.Equals(tp, typeof(List<string>))) {
				System.Collections.IList ls;
				ls = new List<string>();

				Eat("[");
				while (!TryEat(']')) {
					ls.Add(Deserialize(typeof(string)));
				}
				return ls;
			} else if (tpInfo.IsArray) {
				List<System.Object> ls = new List<System.Object>();
				Eat("[");
				while (!TryEat(']')) {
					ls.Add(Deserialize(tp.GetElementType()));
				}
				var arr = Array.CreateInstance(tp.GetElementType(), ls.Count);
				ls.ToArray().CopyTo(arr, 0);
				return arr;
			} else if (Type.Equals(tp, typeof(Mesh)) || Type.Equals(tp, typeof(Texture2D)) || Type.Equals(tp, typeof(Transform)) || Type.Equals(tp, typeof(GameObject))) {
				return DeserializeUnityObject();
			} else {
				var obj = populate ?? Activator.CreateInstance(tp);
				Eat("{");
				while (!TryEat('}')) {
					var name = EatField();
					var field = tpInfo.GetField(name);
					if (field == null) {
						SkipFieldData();
					} else {
						field.SetValue(obj, Deserialize(field.FieldType));
					}
					TryEat(',');
				}
				return obj;
			}
		}

		UnityEngine.Object DeserializeUnityObject () {
			Eat("{");
			var res = DeserializeUnityObjectInner();
			Eat("}");
			return res;
		}

		UnityEngine.Object DeserializeUnityObjectInner () {
			if (EatField() != "Name") throw new Exception("Expected 'Name' field");
			string name = EatField();

			if (name == null) return null;

			if (EatField() != "Type") throw new Exception("Expected 'Type' field");
			string typename = EatField();

			// Remove assembly information
			if (typename.IndexOf(',') != -1) {
				typename = typename.Substring(0, typename.IndexOf(','));
			}

			// Note calling through assembly is more stable on e.g WebGL
			var type = WindowsStoreCompatibility.GetTypeInfo(typeof(AstarPath)).Assembly.GetType(typename);
			type = type ?? WindowsStoreCompatibility.GetTypeInfo(typeof(Transform)).Assembly.GetType(typename);

			if (Type.Equals(type, null)) {
				Debug.LogError("Could not find type '"+typename+"'. Cannot deserialize Unity reference");
				return null;
			}

			// Check if there is another field there
			EatWhitespace();
			if ((char)reader.Peek() == '"') {
				if (EatField() != "GUID") throw new Exception("Expected 'GUID' field");
				string guid = EatField();

				foreach (var helper in UnityEngine.Object.FindObjectsOfType<UnityReferenceHelper>()) {
					if (helper.GetGUID() == guid) {
						if (Type.Equals(type, typeof(GameObject))) {
							return helper.gameObject;
						} else {
							return helper.GetComponent(type);
						}
					}
				}
			}

			// Try to load from resources
			UnityEngine.Object[] objs = Resources.LoadAll(name, type);

			for (int i = 0; i < objs.Length; i++) {
				if (objs[i].name == name || objs.Length == 1) {
					return objs[i];
				}
			}

			return null;
		}

		void EatWhitespace () {
			while (char.IsWhiteSpace((char)reader.Peek()))
				reader.Read();
		}

		void Eat (string s) {
			EatWhitespace();
			for (int i = 0; i < s.Length; i++) {
				var c = (char)reader.Read();
				if (c != s[i]) {
					throw new Exception("Expected '" + s[i] + "' found '" + c + "'\n\n..." + reader.ReadLine());
				}
			}
		}

		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		string EatUntil (string c, bool inString) {
			builder.Length = 0;
			bool escape = false;
			while (true) {
				var readInt = reader.Peek();
				if (!escape && (char)readInt == '"') {
					inString = !inString;
				}

				var readChar = (char)readInt;
				if (readInt == -1) {
					throw new Exception("Unexpected EOF");
				} else if (!escape && readChar == '\\') {
					escape = true;
					reader.Read();
				} else if (!inString && c.IndexOf(readChar) != -1) {
					break;
				} else {
					builder.Append(readChar);
					reader.Read();
					escape = false;
				}
			}

			return builder.ToString();
		}

		bool TryEat (char c) {
			EatWhitespace();
			if ((char)reader.Peek() == c) {
				reader.Read();
				return true;
			}
			return false;
		}

		string EatField () {
			var res = EatUntil("\",}]", TryEat('"'));

			TryEat('\"');
			TryEat(':');
			TryEat(',');
			return res;
		}

		void SkipFieldData () {
			var indent = 0;

			while (true) {
				EatUntil(",{}[]", false);
				var last = (char)reader.Peek();

				switch (last) {
				case '{':
				case '[':
					indent++;
					break;
				case '}':
				case ']':
					indent--;
					if (indent < 0) return;
					break;
				case ',':
					if (indent == 0) {
						reader.Read();
						return;
					}
					break;
				default:
					throw new System.Exception("Should not reach this part");
				}

				reader.Read();
			}
		}
	}
}
