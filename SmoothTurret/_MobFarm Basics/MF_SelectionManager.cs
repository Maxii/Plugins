using UnityEngine;
using System.Collections;

public class MF_SelectionManager : MonoBehaviour {
	
	[Header("Default Targeting Brackets")]
	[Tooltip("Activated if the selected object has this as its weapon target.")]
	public GameObject targetedMark; // marker to denote selected
	[Tooltip("Activated if the selected object has this in its list of targets.")]
	public GameObject targetListMark; // marker to denote selected
	[Tooltip("Activated if this is the selected object.")]
	public GameObject selectedMark; // marker to denote active

	[HideInInspector] public GameObject selectedObject = null;
	[HideInInspector] public MF_AbstractSelection selectedObjScript;
	
}
