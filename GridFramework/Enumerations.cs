namespace GridFramework {
	/// <summary>Radians or degrees.</summary>
	/// This is a simple enum for specifying whether an angle is given in radians for degrees.
	/// This enum is so far only used in methods of GFPolarGrid, but I decided to make it global in case other grids in the future will use it was well.
	public enum AngleMode {radians = 0, degrees};
	
	/// <summary>Enum for one of the three grid planes.</summary>
	/// This enum encapsulates the three grid planes: XY, XZ and YZ. You can also get the integer of enum items, where the integer
	/// corresponds to the missing axis (X = 0, Y = 1, Z = 2):
	/// <code>
	/// // UnityScript:
	/// var myPlane: GridPlane = GridPlane.XZ;
	/// var planeIndex: int = (int)myPlane; // sets the variable to 1
	/// 
	/// // C#
	/// GridPlane myPlane = GridPlane.XZ;
	/// int planeIndex = (int)myPlane;
	/// </code>
	public enum GridPlane {YZ, XZ, XY};
}
