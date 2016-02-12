using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class Pickup : MonoBehaviour {

    public string item;
    public AudioClip pickup;
    public event Action<Pickup> PickedUp = delegate { };


    IEnumerator OnTriggerEnter(Collider other) {
        if (other.transform == Player.current.transform) {
            GetComponent<AudioSource>().PlayOneShot(pickup);
            yield return new WaitForSeconds(0.7f);
            PickedUp(this);
            Player.current.SendMessage("Pickup", item, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
        }
    }

}
