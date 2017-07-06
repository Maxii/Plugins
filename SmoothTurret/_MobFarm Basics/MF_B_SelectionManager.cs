using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_b_selectionmanager.html")]
public class MF_B_SelectionManager : MF_AbstractSelectionManager {
	
	[Header("Default Targeting Marks")]
	[Tooltip("Activated if this is the selected object.")]
	public GameObject selectedMark; 
	[Tooltip("Activated if the selected object has detected this unit, and faction is unknown")]
	public GameObject detectedMark; 
	[Tooltip("Activated if the selected object has analyzed this unit.")]
	public GameObject analyzedMark;
	[Tooltip("Activated if the selected object has this as its weapon target.")]
	public GameObject weaponTargetMark;
	[Tooltip("Activated if the selected object has this in its nav target.")]
	public GameObject navTargetMark;
	[Space(8f)]
	[Tooltip("Activated if the selected object has this as a point of interest.")]
	public GameObject poiMark;
}
