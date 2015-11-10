using UnityEngine;
using System.Collections;

public abstract class MF_AbstractPlatform : MonoBehaviour {

	[Tooltip("Current target - this is usually provided by another script.")]
	public GameObject _target;
	public abstract GameObject target { get; set; }
	[Tooltip("Parent object for weapons.")]
	public GameObject weaponMount;

	[HideInInspector] public float shotSpeed = 0f; // for ballistics / intrcept
	[HideInInspector] public Vector3 exitLoc = Vector3.zero; // for ballistics / intrcept
	[HideInInspector] public Vector3 targetLocation;

	public virtual bool TargetWithinLimits ( Transform target ) {
		return true;
	}
}
