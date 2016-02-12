using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class EnemyPosition : MonoBehaviour {

    public Vector3 position;
    public float speed = 4;

    Vector3 lastPosition;

    Seeker _seeker;
    CharacterController _controller;
    int _currentPosition;
    Pathfinding.Path _path;

    void Start() {
        _seeker = GetComponent<Seeker>();
        _controller = GetComponent<CharacterController>();
    }

    void Update() {
        if (Time.deltaTime <= 0.01f)
            return;

        if ((position - lastPosition).sqrMagnitude > 4) {
            FindRoute();
        }
        Vector3 targetPosition;
        var previousPosition = transform.position;
        var distanceToTravel = speed * Time.deltaTime;
        if (_path != null && _path.vectorPath != null && _currentPosition < _path.vectorPath.Count) {

            while (distanceToTravel > 0 && _currentPosition < _path.vectorPath.Count) {
                var pathPoint = _path.vectorPath[_currentPosition];
                pathPoint.y = transform.position.y;
                var direction = (pathPoint - transform.position);
                var expectedDistance = direction.magnitude;
                var distance = Mathf.Min(expectedDistance, distanceToTravel);
                targetPosition = transform.position + direction.normalized * distance;
                targetPosition.y = Terrain.activeTerrain.SampleHeight(targetPosition) + 0f;
                _controller.SimpleMove((targetPosition - transform.position) / Time.deltaTime);
                distanceToTravel -= distance;
                if (expectedDistance < 1) {
                    _currentPosition++;
                }
            }
        }
        else {
            var direction = (position - transform.position);
            var expectedDistance = direction.magnitude;
            var distance = Mathf.Min(expectedDistance, distanceToTravel);
            targetPosition = transform.position + direction.normalized * distance;
            targetPosition.y = Terrain.activeTerrain.SampleHeight(targetPosition) + 0f;
            _controller.SimpleMove((targetPosition - transform.position) / Time.deltaTime);

        }
        var moved = (transform.position - previousPosition);
        var desired = (position - transform.position);
        var angles = Quaternion.LookRotation(moved == Vector3.zero ? (desired != Vector3.zero ? desired : transform.forward) : moved.normalized, Vector3.up).eulerAngles;
        angles.x = angles.z = 0;
        var currentY = transform.rotation.eulerAngles.y;
        currentY = float.IsNaN(currentY) ? 0 : currentY;
        transform.rotation = Quaternion.Euler(0, Mathf.LerpAngle(currentY, angles.y, Time.deltaTime * 2), 0);

    }

    bool _routeFinding;

    void FindRoute() {
        if (_routeFinding)
            return;

        position.y = Terrain.activeTerrain.SampleHeight(position);
        lastPosition = position;
        if ((transform.position - position).sqrMagnitude < 200) {
            _path = null;

            return;
        }
        _routeFinding = true;
        _seeker.StartPath(transform.position, position, (p) => {
            _currentPosition = 2;
            _path = p;
            _routeFinding = false;
        });
    }

}
