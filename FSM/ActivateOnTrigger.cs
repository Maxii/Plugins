using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class ActivateOnTrigger : MonoBehaviour {

    // Use this for initialization
    void Start() {
        GetComponent<Rigidbody>().isKinematic = true;
    }

    bool _activated;
    public string message;



    void OnTriggerEnter(Collider other) {
        if (!_activated && enabled && other.name == "Player") {
            SendMessage("Activate", message, SendMessageOptions.DontRequireReceiver);
            _activated = true;
        }
    }

}
