using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class GunBehaviour : StateMachineBehaviourEx {

    public static GunBehaviour Gun;
    public static bool laserPointerEnabled;

    public GUISkin skin;
    public GameObject machineGun;
    public GameObject laserPointer;
    public GameObject muzzleFlash;
    public Transform player;
    public Transform gunHolder;
    public LineRenderer laser;

    public AudioClip fireLaser;
    public AudioClip fireGun;


    [HideInInspector]
    public float lastFiredTime;

    AnimationState _swingAnimationGun;
    AnimationState _swingAnimationPointer;

    Vector3 _gunHolderPosition;

    public int bullets = 30;
    public int clipSize = 10;
    public int loadedBullets = 0;

    GUIStyle _style;

    public enum GunStates {
        MachineGunNormal,
        MachineGunFire,
        MachineGunReload,
        MachineGunHit,
        LaserPointerNormal,
        LaserPointerFire,
        SwapGun,
        Disabled,
        NoComputer
    }

    protected override void OnAwake() {
        Gun = this;


    }

    void OnDestroy() {
        Gun = null;
    }

    IEnumerator Start() {
        useGUI = true;
        muzzleFlash.SetActive(false);
        _swingAnimationGun = machineGun.GetComponent<Animation>()["GunSwing"];
        _swingAnimationPointer = laserPointer.GetComponent<Animation>()["GunSwing"];
        laserPointer.GetComponent<Animation>().Stop();
        laserPointer.GetComponent<Animation>()["Aim"].layer = 10;
        machineGun.GetComponent<Animation>()["Recoil"].layer = 10;
        machineGun.GetComponent<Animation>()["Reload"].layer = 20;
        machineGun.GetComponent<Animation>()["Hit"].layer = 30;
        machineGun.GetComponent<Animation>().Stop();
        _gunHolderPosition = gunHolder.position;
        currentState = GunStates.MachineGunNormal;
        var offset = laser.material.mainTextureOffset;
        laser.enabled = false;
        while (true) {
            offset.x -= Time.deltaTime * 5;
            laser.material.mainTextureOffset = offset;
            yield return null;
        }
    }
    #region Disabled

    void Disabled_EnterState() {
        GetComponent<Camera>().enabled = false;
    }

    void Disabled_ExitState() {
        GetComponent<Camera>().enabled = true;

    }

    #endregion


    #region Machine Gun Normal


    void MachineGunNormal_EnterState() {
        laserPointerEnabled = false;
        laserPointer.SetActive(false);
        machineGun.SetActive(true);
        _swingAnimationGun.enabled = true;
        machineGun.GetComponent<Animation>().Blend("GunSwing", 1, 1.5f);
        _swingAnimationGun.speed = 0;
    }

    Vector3 _lastPosition;

    void MachineGunNormal_Update() {
        var distance = (player.position - _lastPosition).magnitude;
        _lastPosition = player.position;
        _swingAnimationGun.time += distance / 8;

        if (Input.GetKey(KeyCode.G)) {
            currentState = GunStates.SwapGun;
        }
        if (Input.GetKey(KeyCode.Return) || Input.GetMouseButton(0)) {
            currentState = GunStates.MachineGunFire;
        }
        if (Input.GetKey(KeyCode.R)) {
            currentState = GunStates.MachineGunReload;
        }
    }

    void MachineGunNormal_ExitState() {
        machineGun.GetComponent<Animation>().Blend("GunSwing", 0, 0.2f);
    }

    #endregion

    #region Swap Gun

    IEnumerator SwapGun_EnterState() {
        yield return MoveObject(gunHolder, _gunHolderPosition - gunHolder.up * 2.5f, 0.5f);

        if (laserPointer.activeInHierarchy) {
            laserPointer.SetActive(false);
            machineGun.SetActive(true);
            currentState = GunStates.MachineGunNormal;
        }
        else {
            machineGun.SetActive(false);
            laserPointer.SetActive(true);
            currentState = GunStates.LaserPointerNormal;
        }

        StartCoroutine(MoveObject(gunHolder, _gunHolderPosition, 0.5f));

    }

    #endregion

    #region Laser Pointer Normal

    void LaserPointerNormal_EnterState() {
        laserPointerEnabled = true;
        laserPointer.SetActive(true);
        machineGun.SetActive(false);
        laserPointer.GetComponent<Animation>().Blend("Aim", 0, 0.3f);
        _swingAnimationPointer.enabled = true;
        laserPointer.GetComponent<Animation>().Blend("GunSwing", 1, 1.5f);
        _swingAnimationPointer.speed = 0;

    }


    void LaserPointerNormal_Update() {
        var distance = (player.position - _lastPosition).magnitude;
        _lastPosition = player.position;

        _swingAnimationPointer.time += distance / 8;


        if (Input.GetKey(KeyCode.G)) {
            currentState = GunStates.SwapGun;
        }
        if (Input.GetKey(KeyCode.Return) || Input.GetMouseButton(0)) {
            currentState = GunStates.LaserPointerFire;
        }

    }

    void LaserPointerNormal_ExitState() {
        laserPointer.GetComponent<Animation>().Blend("GunSwing", 0, 0.3f);

    }

    #endregion

    #region Laser Pointer Fire



    IEnumerator LaserPointerFire_EnterState() {
        if (!Player.current.hasComputer)
            currentState = GunStates.NoComputer;
        var aim = laserPointer.GetComponent<Animation>()["Aim"];
        aim.enabled = true;
        aim.speed = 1;
        aim.time = 0;
        aim.layer = 20;
        laserPointer.GetComponent<Animation>().Blend("Aim", 1, 0.2f);
        yield return new WaitForSeconds(0.2f);
        laser.enabled = true;
        while (true) {
            if (!Input.GetKey(KeyCode.Return) && !Input.GetMouseButton(0)) {
                break;
            }
            yield return new WaitForSeconds(0.3f);
            GetComponent<AudioSource>().PlayOneShot(fireLaser);
        }
        currentState = GunStates.LaserPointerNormal;
    }

    void LaserPointerFire_ExitState() {
        laserPointer.GetComponent<Animation>().Blend("Aim", 0, 0.3f);
        laser.enabled = false;
    }

    #endregion

    #region No Computer

    void NoComputer_Update() {
        if (!Input.GetKey(KeyCode.Return) || Input.GetMouseButton(0)) {
            currentState = GunStates.LaserPointerNormal;
        }

    }

    #endregion


    #region Machine Gun Fire

    IEnumerator MachineGunFire_EnterState() {
        if (loadedBullets <= 0) {
            currentState = GunStates.MachineGunReload;
            yield break;
        }

        machineGun.GetComponent<Animation>().Blend("Aim", 1, 0.2f);
        yield return new WaitForSeconds(0.2f);
        while (true) {
            if (!Input.GetKey(KeyCode.Return) && !Input.GetMouseButton(0)) {
                break;
            }
            lastFiredTime = Time.time;
            muzzleFlash.SetActive(true);
            machineGun.GetComponent<Animation>().Play("Recoil");
            RaycastHit hit;
            var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            if (Physics.Raycast(ray, out hit,
                1000)) {
                hit.collider.SendMessage("TakeDamage", 5f + UnityEngine.Random.value * 3, SendMessageOptions.DontRequireReceiver);
            }
            GetComponent<AudioSource>().PlayOneShot(fireGun);
            yield return new WaitForSeconds(0.1f);
            muzzleFlash.SetActive(false);
            loadedBullets--;
            if (loadedBullets <= 0) {
                currentState = GunStates.MachineGunReload;
                yield break;
            }
            yield return new WaitForSeconds(0.05f);
        }
        currentState = GunStates.MachineGunNormal;

    }

    void MachineGunFire_ExitState() {
        machineGun.GetComponent<Animation>().Blend("Aim", 0, 0.6f);
        machineGun.GetComponent<Animation>().Blend("Recoil", 0, 0.6f);
    }

    #endregion

    #region Machine Gun Reload

    IEnumerator MachineGunReload_EnterState() {
        if (bullets <= 0) {
            currentState = GunStates.MachineGunHit;
            yield break;
        }

        var reload = machineGun.GetComponent<Animation>()["Reload"];

        machineGun.GetComponent<Animation>().Stop();
        reload.time = 0;
        reload.speed = 1f;
        machineGun.GetComponent<Animation>().Blend("Reload", 1, 0.1f);

        yield return WaitForAnimation(reload, 0.5f);
        var toLoad = Mathf.Min(clipSize - loadedBullets, bullets);
        bullets -= toLoad;
        loadedBullets += toLoad;

        yield return WaitForAnimation(reload, 1f);
        machineGun.GetComponent<Animation>().Blend("Reload", 0, 0.1f);
        currentState = GunStates.MachineGunNormal;

    }

    #endregion

    #region Machine Gun Hit

    IEnumerator MachineGunHit_EnterState() {
        foreach (var anim in machineGun.GetComponent<Animation>().Cast<AnimationState>())
            anim.enabled = false;

        var hit = machineGun.GetComponent<Animation>()["Hit"];
        hit.time = 0;
        hit.speed = 1f;
        hit.weight = 1;
        hit.enabled = true;
        yield return WaitForAnimation(hit, 0.5f);

        RaycastHit rhit;
        var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 1));
        if (Physics.Raycast(ray, out rhit,
            2)) {
            rhit.collider.SendMessage("TakeDamage", 1f + UnityEngine.Random.value * 2, SendMessageOptions.DontRequireReceiver);
        }


        yield return WaitForAnimation(hit, 1f);
        currentState = GunStates.MachineGunNormal;
    }

    void MachineGunHit_ExitState() {
        machineGun.GetComponent<Animation>().Blend("Hit", 0, 0.6f);
    }

    #endregion


    void OnGUI() {
        GUI.skin = skin;
        _style = new GUIStyle("label");
        _style.normal.textColor = new Color(1, 1, 1, 0.8f);
        _style.fontSize = 32;

        GUILayout.BeginVertical();
        GUILayout.Label(string.Format("{0}/{1} : {2}", loadedBullets, clipSize, bullets), _style);
        GUILayout.EndVertical();

    }



}
