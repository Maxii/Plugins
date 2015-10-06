using UnityEngine;
using System.Collections;

public abstract class MF_AbstractPlatform : MonoBehaviour {

	public GameObject target = null;
	public GameObject weaponMount = null;

	[HideInInspector] public float shotSpeed = 0f; // for ballistics / intrcept
	[HideInInspector] public Vector3 exitLoc = Vector3.zero; // for ballistics / intrcept
	[HideInInspector] public Vector3 targetLocation;

	public virtual bool TargetWithinLimits ( Transform target ) {
		return true;
	}
}
