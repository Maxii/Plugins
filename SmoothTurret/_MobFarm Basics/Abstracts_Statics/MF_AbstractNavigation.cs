﻿using UnityEngine;
using System.Collections;

[HelpURL("http://mobfarmgames.weebly.com/mf_abstractnavigation.html")]
public abstract class MF_AbstractNavigation : MonoBehaviour {

	public enum NavType { Waypoint, Target, TargetOrWaypoint }

	[Tooltip("What navigation will choose to send to a mobility script.\n" +
		"Waypoint: Will follow a list of waypoints.\n" +
		"Target: Will choose a target picked by a targeting script.\n" +
		"TargetOrWaypoint: Will choose a target. If none found, will follow a list waypoints.")]
	public NavType navMode;
	[Tooltip("The current object of navigation being sent to a mobility script.")]
	public Transform navTarget;
	[Tooltip("A group of waypoints. The waypoint list will be built from the children of an object.")]
	[SerializeField] protected Transform _waypointGroup;
	public Transform waypointGroup {
		get { return _waypointGroup; }
		set { _waypointGroup = value;
			waypoints = UtilityMF.BuildArrayFromChildren( _waypointGroup );
		}
	}
	[Tooltip("The list of waypoints built from the waypoint group.")]
	public Transform[] waypoints;
	[Tooltip("If true: When reaching a waypoint, choose the next one at random, instead of in order.")]
	public bool randomWpt;
	[Tooltip("The index of the current waypoint to be sent to a mobility script.")]
	public int curWpt;
	[Tooltip("How close to approach waypoints before choosing the next one in the list.")]
	public float waypointProx = 5f;
	[Tooltip("How close to approach targets before stopping.")]
	public float targetProx = 5f;

	public virtual void OnDisable () { // reset for object pool support
		waypointGroup = null;
		curWpt = 0;
	}
				
}
