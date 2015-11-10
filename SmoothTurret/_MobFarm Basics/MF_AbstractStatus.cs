using UnityEngine;
using System.Collections;

public abstract class MF_AbstractStatus : MonoBehaviour {

	public float _health;
	public abstract float health { get; set; }
	public float signature;

}
