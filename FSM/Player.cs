using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour {

    public static Player current;
    public float health = 5;
    public bool hasComputer;
    public GameObject gameOver;
    public GUISkin skin;

    Texture2D _1pixel;
    float timeOut;

    void Pickup(string item) {
        switch (item) {
            case "Ammo":
                GunBehaviour.Gun.bullets += 20;
                break;
            case "Health":
                health += 15;
                break;
            case "Computer":
                hasComputer = true;
                break;

        }
    }

    // Use this for initialization
    void Awake() {
        OptionalParameters.DoSomething();
        current = this;
        _1pixel = new Texture2D(1, 1);
        _1pixel.SetPixel(0, 0, Color.white);
        _1pixel.Apply();
    }

    void OnDestroy() {
        current = null;
    }

    void Update() {
        timeOut = Mathf.Clamp(timeOut - Time.deltaTime, 0, 10000);
    }

    void TakeDamage(float points) {
        health -= points;
        if (health < 0) {
            gameOver.SetActive(true);
            Time.timeScale = 0;
            GetComponent<CharacterController>().enabled = false;
            GunBehaviour.Gun.currentState = GunBehaviour.GunStates.Disabled;
            StartCoroutine(Restart());
        }
        timeOut = 0.5f;
    }

    IEnumerator Restart() {
        while (true) {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)) {
                Time.timeScale = 1;
                SceneManager.LoadScene("Reload");
            }
            yield return null;
        }
    }

    void OnGUI() {
        GUI.skin = skin;
        GUIStyle healthStyle = new GUIStyle("label");
        healthStyle.normal.textColor = Color.Lerp(Color.red / 2, Color.green / 2, health / 100);
        healthStyle.fontSize = 42;

        GUI.Label(new Rect(Screen.width - 100, 0, 100, 100), string.Format("{0:0}", health), healthStyle);
        if (timeOut > 0) {
            var color = Color.Lerp(Color.red, Color.green, health / 200);
            color.a = Mathf.Clamp01(timeOut);
            GUI.color = color;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _1pixel);
            GUI.color = Color.white;

        }
    }


}
