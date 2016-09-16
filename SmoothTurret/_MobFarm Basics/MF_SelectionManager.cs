using UnityEngine;
using System.Collections;

public class MF_SelectionManager : MonoBehaviour {
	
	[Header("Default Targeting Brackets")]
	[Tooltip("Activated if the selected object has detected this unit.")]
	public GameObject detectedMark; 
	[Tooltip("Activated if the selected object has analyzed this unit.")]
	public GameObject analyzedMark;
	[Tooltip("Activated if the selected object has detected this unit, and this unit is an enemy.")]
	public GameObject enemyDetectedMark; 
	[Tooltip("Activated if the selected object has analyzed this unit, and this unit is an enemy.")]
	public GameObject enemyAnalyzedMark;
	[Tooltip("Activated if the selected object has detected this unit, and this unit is an ally.")]
	public GameObject allyDetectedMark; 
	[Tooltip("Activated if the selected object has analyzed this unit, and this unit is an ally.")]
	public GameObject allyAnalyzedMark;
	[Tooltip("Activated if the selected object has detected this unit, and this unit is neutral.")]
	public GameObject neutralDetectedMark; 
	[Tooltip("Activated if the selected object has analyzed this unit, and this unit is neutral.")]
	public GameObject neutralAnalyzedMark;
	[Tooltip("Activated if the selected object has this as its weapon target.")]
	public GameObject weaponTargetMark;
	[Tooltip("Activated if the selected object has this in its nav target.")]
	public GameObject navTargetMark;
	[Tooltip("Activated if this is the selected object.")]
	public GameObject selectedMark; 

	[Header("Selected Unit:")]
	[Tooltip("Visible for debug purposes. This should be left blank.")]
	public GameObject selectedObject = null;

	[HideInInspector] public MF_AbstractSelection selectedObjScript;
	
}
