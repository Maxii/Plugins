using UnityEngine;
using System.Collections;

public class WMG_Node_World_Map : WMG_Node {
	// This is how to extend the node script to add custom data and functions
	// If this were fully implemented we would store data for worldmap nodes here such as a random name and tooltip info
	public bool hoverState = false;
	public GameObject SelectionObject;
	public GameObject SelTopLeft;
	public GameObject SelTopRight;
	public GameObject SelBotLeft;
	public GameObject SelBotRight;
}
