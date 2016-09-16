using UnityEngine;
using System.Collections;

public abstract class MF_AbstractStatus : MonoBehaviour {

	[Tooltip("Use to point to object holding the collider used with faction layers. This may be used by MF_B_Spawner to change the faction of spawned units that have a collider on a faction layer.")]
	public Transform layerColliderLocation;
	[Tooltip("Health of the unit.")]
	public float maxHealth = 10f;
	[SerializeField] protected float _health;
	public abstract float health { get; set; }
	[Tooltip("Unused in MobFarm Basics. This could be used to affect detection threshold, or target selection.")]
	public float signature;
	[Tooltip("Unused in MobFarm Basics. This could be used to affect detection threshold, or target selection.")]
	public float kind;

	[HideInInspector] public float damageID; // used so an explosion doesn't damage the same script multiple times

	protected bool loaded;
	
	public virtual void Start () {
		loaded = true;
		OnValidate();
	}
	
	public virtual void OnValidate () {
		if ( loaded == true ) {
			health = _health;
		} else {
			_health = maxHealth;
		}
	}

}
