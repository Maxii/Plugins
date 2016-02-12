using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class F16Behaviour : StateMachineBehaviourEx {

    public Transform f16;
    public static F16Behaviour current;
    public GUISkin skin;
    public Transform player;
    public LineRenderer laser;
    public TrailRenderer trail;
    public GameObject explosions;
    public AudioClip explodeSound;
    public GameObject aircraft;

    GUIStyle _style;

    public enum F16States {
        Disabled,
        Idle,
        AssessTarget,
        LockingTarget,
        BombingRun,
        Outbound,
        NoComputer
    }

    void Start() {
        f16 = f16 == null ? transform : f16;
        current = this;
        currentState = F16States.Disabled;
    }

    void OnDestroy() {
        current = null;
    }

    #region Disabled

    void Disabled_EnterState() {
        aircraft.SetActive(false);
    }

    void Disabled_Update() {
        trail.enabled = false;
        if (GunBehaviour.laserPointerEnabled)
            currentState = F16States.Idle;
    }

    #endregion

    void Idle_EnterState() {
        aircraft.SetActive(false);
        trail.enabled = false;
        f16.position = Vector3.forward * 10000;
    }

    void Idle_Update() {
        if (!GunBehaviour.laserPointerEnabled)
            currentState = F16States.Disabled;
        if (Input.GetKey(KeyCode.Return))
            currentState = F16States.AssessTarget;
    }

    #region Assess Target

    GameObject _target;
    Vector3 _targetPosition;

    void AssessTarget_EnterState() {
        if (!Player.current.hasComputer)
            currentState = F16States.NoComputer;
        if (!GunBehaviour.laserPointerEnabled)
            currentState = F16States.Disabled;

    }

    void AssessTarget_Update() {
        RaycastHit hit;
        if (Physics.Raycast(player.position, player.forward, out hit, 100)) {
            _target = hit.collider.gameObject;
            currentState = F16States.LockingTarget;
        }
        if (!Input.GetKey(KeyCode.Return) || !GunBehaviour.laserPointerEnabled)
            currentState = F16States.Idle;

    }

    void AssessTarget_OnGUI() {
        GUI.skin = skin;
        if (_style == null) {
            _style = new GUIStyle("label");
            _style.fontSize = 32;

        }
        _style.normal.textColor = Color.yellow;
        GUILayout.BeginArea(new Rect(0, Screen.height / 4, Screen.width, 200));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label(string.Format("Lock target"), _style);

        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();

    }


    #endregion

    #region No Computer

    void NoComputer_Update() {
        if (!(Input.GetKey(KeyCode.Return) || Input.GetMouseButton(0)) || !GunBehaviour.laserPointerEnabled)
            currentState = F16States.Idle;
    }


    void NoComputer_OnGUI() {
        GUI.skin = skin;
        if (_style == null) {
            _style = new GUIStyle("label");
            _style.fontSize = 32;

        }
        _style.normal.textColor = Color.red;

        GUILayout.BeginArea(new Rect(0, Screen.height / 4, Screen.width, 200));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label("Find Targeting Computer", _style);

        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    #endregion

    #region Locking Target

    void LockingTarget_EnterState() {
        laser.material.color = Color.blue;
    }

    void LockingTarget_OnGUI() {
        GUI.skin = skin;
        if (_style == null) {
            _style = new GUIStyle("label");
            _style.fontSize = 32;

        }
        _style.normal.textColor = Color.red;

        GUILayout.BeginArea(new Rect(0, Screen.height / 4, Screen.width, 200));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label(string.Format("Locked on in {0:0}", 5 - timeInCurrentState), _style);

        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void LockingTarget_Update() {
        if (!(Input.GetKey(KeyCode.Return) || Input.GetMouseButton(0)) || !GunBehaviour.laserPointerEnabled)
            currentState = F16States.Idle;
        RaycastHit hit;
        if (Physics.Raycast(player.position, player.forward, out hit, 100)) {
            _targetPosition = hit.point;
            if (!_target == hit.collider.gameObject) {
                currentState = F16States.AssessTarget;
            }

        }
        else
            currentState = F16States.AssessTarget;
        if (timeInCurrentState > 5) {
            currentState = F16States.BombingRun;
        }
    }

    #endregion

    #region Bombing Run

    Vector3 _direction;

    IEnumerator BombingRun_EnterState() {
        aircraft.SetActive(true);
        var angle = UnityEngine.Random.Range(0, 359);
        _direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
        f16.position = _targetPosition + Vector3.up * 60 - (_direction * 800);
        f16.rotation = Quaternion.LookRotation(_direction, Vector3.up) * Quaternion.AngleAxis(90, Vector3.up);
        trail.enabled = true;
        yield return new WaitForSeconds(4);

        foreach (var hit in Physics.OverlapSphere(_targetPosition, 20f)) {
            hit.SendMessageUpwards("TakeDamage", 10000f, SendMessageOptions.DontRequireReceiver);
        }
        var explode = Instantiate(explosions, _targetPosition, Quaternion.identity) as GameObject;
        explode.GetComponent<AudioSource>().clip = explodeSound;
        explode.GetComponent<AudioSource>().Play();
        Destroy(explode, 6);
        currentState = F16States.Outbound;
    }

    void BombingRun_OnGUI() {
        _style.normal.textColor = Color.red;

        GUILayout.BeginArea(new Rect(0, Screen.height / 4, Screen.width, 200));
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label(string.Format("F16 Inbound"), _style);

        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void BombingRun_Update() {
        f16.position += _direction * 200 * Time.deltaTime;
    }

    #endregion

    #region Outbound

    void Outbound_Update() {
        f16.position += _direction * 200 * Time.deltaTime;
        _direction = Quaternion.AngleAxis(10 * Time.deltaTime, Vector3.up) * Quaternion.AngleAxis(-5 * Time.deltaTime, f16.right) * _direction;
        f16.rotation = Quaternion.LookRotation(_direction, Vector3.up) * Quaternion.AngleAxis(90, Vector3.up);

        if (timeInCurrentState > 15)
            currentState = F16States.Idle;
    }

    #endregion

}
