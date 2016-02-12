using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Reload : MonoBehaviour {

    // Use this for initialization
    void Start() {

    }

    void OnMouseDown() {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.Space))
            SceneManager.LoadScene("Scene1");
    }
}
