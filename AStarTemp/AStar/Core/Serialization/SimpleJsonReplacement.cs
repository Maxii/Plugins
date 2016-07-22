#if ASTAR_NO_JSON
namespace Pathfinding.Serialization.JsonFx {
	public class JsonMemberAttribute : System.Attribute {}
	public class JsonOptInAttribute : System.Attribute {}
	public class JsonNameAttribute : System.Attribute { public JsonNameAttribute (string s) {} }
}
#endif