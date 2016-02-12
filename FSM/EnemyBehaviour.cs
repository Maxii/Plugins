using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class EnemyBehaviour : StateMachineBehaviourEx {

    public Transform hips;
    public AudioClip bezerk;
    public AudioClip attack;
    public AudioClip chase;
    public AudioClip hit;
    public AudioClip dying;

    ParticleEmitter _emitter;

    public enum EnemyStates {
        Spawn,
        GoToSleep,
        Sleeping,
        Patrol,
        Chase,
        Bezerk,
        Attack,
        Hit,
        Dead
    }

    EnemyPosition _movePosition;
    List<BlowUpBuilding> buildings = new List<BlowUpBuilding>();
    float _timeToSleep;
    Transform _player;
    float _baseSpeed;
    float _health;
    Vector3 _hipPosition;

    protected override void OnAwake() {
        _health = 30f + UnityEngine.Random.value * 20f;
        _movePosition = GetComponent<EnemyPosition>();
        _baseSpeed = _movePosition.speed;
        animation["gothit"].layer = 20;
        animation["die"].layer = 20;
        animation["attack"].layer = 10;
        _hipPosition = hips.localPosition;
        _emitter = GetComponentInChildren<ParticleEmitter>();
        _emitter.emit = false;
    }

    void Start() {
        _player = Player.current.transform;

    }

    void Spawned() {
        currentState = EnemyStates.Spawn;
    }


    IEnumerator Spawn_EnterState() {
        _movePosition.enabled = false;
        hips.localPosition = _hipPosition - Vector3.up * 4;
        _emitter.emit = true;
        yield return MoveObject(hips, _hipPosition, 4f);
        _emitter.emit = false;
        yield return new WaitForSeconds(1f);
        currentState = EnemyStates.GoToSleep;
        _movePosition.enabled = true;
    }

    IEnumerator GoToSleep_EnterState() {
        var distance = float.PositiveInfinity;
        var position = transform.position;
        Vector3 lastOffset = Vector3.zero;
        Vector3 nearest = Vector3.zero;
        var terrain = Terrain.activeTerrain.terrainData;
        foreach (var tree in terrain.treeInstances) {
            if (tree.prototypeIndex != 0)
                continue;
            var treePos = Vector3.Scale(tree.position, terrain.size);
            if (treePos.y > 15)
                continue;
            lastOffset = (treePos - position);
            var testDistance = lastOffset.sqrMagnitude;
            if (testDistance < distance) {
                distance = testDistance;
                nearest = treePos;
            }
        }
        _movePosition.position = nearest + lastOffset.normalized * 2;
        _timeToSleep = UnityEngine.Random.Range(5f, 100f);
        yield return WaitForPosition(_movePosition.position);
        currentState = EnemyStates.Sleeping;
    }

    void Sleeping_Update() {
        if (timeInCurrentState > _timeToSleep)
            currentState = EnemyStates.Patrol;
    }

    IEnumerator Patrol_EnterState() {
        do {
            if (buildings.Count == 0) {
                buildings = BlowUpBuilding.buildings.ToList();
            }
            var building = buildings[UnityEngine.Random.Range(0, buildings.Count)];
            buildings.Remove(building);
            _movePosition.position = building.transform.position;
            yield return WaitForPosition(_movePosition.position, 15);
        } while (UnityEngine.Random.value < 0.6f);
        currentState = EnemyStates.GoToSleep;
    }

    Vector3 _oldTarget;
    int _playerInSight;

    IEnumerator Chase_EnterState() {
        GetComponent<AudioSource>().PlayOneShot(chase);
        _oldTarget = _movePosition.position;
        _playerInSight = 4;
        _movePosition.speed = _baseSpeed * 2.5f;
        do {
            _playerInSight--;
            _movePosition.position = _player.position;
            yield return new WaitForSeconds(3f);
        } while (_playerInSight > 0);
        Return(EnemyStates.GoToSleep);
    }

    void Chase_SawPlayer() {
        _playerInSight = Mathf.Clamp(_playerInSight + 1, 0, 4);
    }

    void Chase_ExitState() {
        _movePosition.speed = _baseSpeed;
        _movePosition.position = _oldTarget;
    }

    IEnumerator Bezerk_EnterState() {
        GetComponent<AudioSource>().PlayOneShot(bezerk);
        _movePosition.speed = _baseSpeed * 3.4f;
        while (true) {
            if ((_player.position - transform.position).sqrMagnitude < 3f) {
                Call(EnemyStates.Attack);
            }
            yield return null;
        }
    }

    void Bezerk_Update() {
        _movePosition.position = _player.position;
        if ((_player.position - transform.position).sqrMagnitude > 80) {
            Return();
        }
    }

    void Bezerk_ExitState() {
        _movePosition.speed = _baseSpeed * 2.5f;
    }

    bool wasHit;

    IEnumerator Attack_EnterState() {
        wasHit = false;
        var attack = animation["attack"];
        attack.enabled = true;
        attack.time = 0;
        animation.Blend("attack", 1, 0.2f);
        GetComponent<AudioSource>().PlayOneShot(this.attack);
        yield return WaitForAnimation(attack, 0.5f);
        if (!wasHit && (_player.position - transform.position).sqrMagnitude < 3)
            _player.SendMessage("TakeDamage", 4);
        yield return WaitForAnimation(attack, 1f);
        animation.Blend("attack", 0, 0.2f);
        Return();
    }

    IEnumerator Hit_EnterState() {
        GetComponent<AudioSource>().PlayOneShot(hit);
        wasHit = true;
        _movePosition.position = transform.position;
        animation.Stop();
        yield return PlayAnimation("gothit");
        Return();
    }

    IEnumerator Dead_EnterState() {
        GetComponent<AudioSource>().PlayOneShot(dying);
        collider.enabled = false;
        _movePosition.enabled = false;
        animation.Stop();
        yield return PlayAnimation("die");
        yield return new WaitForSeconds(60);
        Destroy(gameObject);
    }

    void Sleeping_DetectedPlayer() {
        Call(EnemyStates.Chase);
    }

    void GoToSleep_DetectedPlayer() {
        Call(EnemyStates.Chase);
    }

    void Patrol_DetectedPlayer() {

        Call(EnemyStates.Chase);
    }

    void Chase_NearPlayer() {
        Call(EnemyStates.Bezerk);
    }


    void DetectedPlayer() {
        SendStateMessage();
    }

    void NearPlayer() {
        SendStateMessage();
    }

    void TakeDamage(float points) {
        if (currentState.Equals(EnemyStates.Dead))
            return;
        _health -= points;
        if (_health > 0) {
            Call(EnemyStates.Hit);
        }
        else {
            currentState = EnemyStates.Dead;
        }
    }



}
