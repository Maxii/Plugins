using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class LockCursor : MonoBehaviour {

    void OnMouseDown() {
        Cursor.lockState = CursorLockMode.Locked; //Screen.lockCursor = true;
    }

    void Update() {
        Cursor.lockState = CursorLockMode.Locked; //Screen.lockCursor = true;
    }
}
