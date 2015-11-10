using UnityEngine;
using System.Collections;

// if using layers to designate factions, these names need to match the layer name
public enum FactionType { Side0, Side1, Side2, Side3 };

public enum FactionMethodType { Tags, Layers }

public class TargetData { 
	
	public Transform transform;
	public MF_AbstractStatus script;
	public bool clickedPriority;
	public bool targetPersists; // won't drop off target list due to time
	public bool dataPersists; // won't loose data due to time
	public float? lastDetected;
	public float? lastAnalyzed;
	public float? sqrMagnitude;
	public float? range;
	public float? auxValue1;
	public float? auxValue2;

	public static void RemoveAnalyzeData ( MF_TargetList script, int key ) {
		// remove certian data without removing target entry

//		script.targetList[key].auxValue1 = null;

	}
}



