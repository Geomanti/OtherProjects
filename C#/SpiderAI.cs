using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem.EnhancedTouch;

[Serializable]
public enum AIMode
{
    DefendPoint, WalkAroundPoint, RandomWalk
}
public class SpiderAI : MonoBehaviour
{
    [SerializeField] NLegIKSolver legScript;
    [SerializeField] LayerMask terrainLayer = default;
    [SerializeField] SoundManager affectSoundBySpeed;
    [SerializeField] Transform eyeObject;
    [SerializeField] Transform mainHull;
    [SerializeField] Transform teoreticalPlayerPos;
    [SerializeField] DroneDeploy droneDeploySript;
    [SerializeField] DroneControllerAI[] drones;
    [Header("AI Controls")]
    [SerializeField] float timeToForgetTarget = 30f;
    [SerializeField] float timeToChangeDirrection = 5f;
    [SerializeField] float maxWalkAway = 5f;
    [SerializeField] float playerSearchDistance = 30f;
    [SerializeField] float minToPlayerDistance = 10f;
    [SerializeField] float maxToPlayerDistance = 20f;
    [SerializeField] bool avoidObstacles = true;
    [SerializeField] Vector2 CheckObstaclesDist;
    [SerializeField] LayerMask obstacleLayers;
    [SerializeField] string playerTag;
    public AIMode myAIMode;
    [Header("Movement")]
    [SerializeField] float _maximumAddHeight = 15f;
    [SerializeField] float _minimumAddHeight = 5f;
    [Range(1, 1000)] int _rayCastLength = 10;
    [SerializeField] float _speedMpS = 10f;
    [SerializeField] float _acceleration = 1f;
    [SerializeField] float _rotationSpeedDpS = 10f;
    [SerializeField] float _rotationAccel = 1f;
    [SerializeField] float _crouchSpeed = 12f;
    [SerializeField] float _crouchAaccel = 1f;
    [Range(0.0f, 1.0f)] 
    public float turnTolerance;
    [SerializeField] bool bodyToSurfaceFacing = true;
    [Range(0.01f, 1.0f)]
    public float _legsPositionImpactWeight;
    [Header("Physics")]
    [SerializeField] ragdollControl ragdollScript;
    [SerializeField] bool stickToMovingObjects = false;
    [Range(0, 180)] public int falloffAngle;

    [Header("Legs")]
    [SerializeField] Transform[] legs;

    private float _visualHeight, _upHeightLimit, _downHeightLimit;
    private Quaternion newQuat;
    private Vector3 speed = new Vector3(0f, 0f, 0f);
    private Vector3 legsNormal;
    private float rotSpeed = 0f;
    private float maxSpeed = 0f;
    private Transform _underObjectOldTransform;
    private Vector3 _oldTransformOldPos;
    private Vector3 randomDirection = new Vector3();
    private Vector3 startPosition;
    private float changeDirTimer, forgetTargetTimer;
    private GameObject[] players;
    private bool moveBack, moveForward, moveRight, moveLeft, moveUp, moveDown = false;
    private bool IsFoundTarget = false;
    private bool AreDronesDeployed = false;
    void Awake()
    {
        ResetTargets();
        startPosition = transform.position;
        changeDirTimer = timeToChangeDirrection;
        forgetTargetTimer = timeToForgetTarget;
        newQuat = transform.rotation;
        Vector3 reyStart = transform.position + transform.up * 5;
        Ray ray = new Ray(reyStart, -transform.up);
        if (Physics.Raycast(ray, out RaycastHit rayHit, _rayCastLength, terrainLayer.value))
        {
            _visualHeight = rayHit.distance;
            _underObjectOldTransform = rayHit.transform;
            _oldTransformOldPos = _underObjectOldTransform.position;
        }
        _upHeightLimit = _visualHeight + _maximumAddHeight;
        _downHeightLimit = _visualHeight - _minimumAddHeight;

        legsNormal = transform.up;
    }

    // Update is called once per frame
    void Update()
    {
        if (!ragdollScript.IsPhysicsActive())
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, newQuat, _rotationSpeedDpS * Time.deltaTime);
            maxSpeed = _speedMpS * (_upHeightLimit / _visualHeight);

            // check for player targets
            for (int i = 0; i< players.Length; i++)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, players[i].transform.position);
                if (distanceToPlayer < playerSearchDistance && !Physics.Linecast(eyeObject.position, players[i].transform.position, obstacleLayers) && players[i].activeSelf)
                {
                    IsFoundTarget = true;
                    forgetTargetTimer = 0f;
                    teoreticalPlayerPos.position = players[i].transform.position;
                }
                else if (!Physics.Linecast(eyeObject.position, players[i].transform.position, obstacleLayers) && forgetTargetTimer >= timeToForgetTarget || !players[i].activeSelf)
                {
                    IsFoundTarget = false;
                    forgetTargetTimer = 0f;
                }
            }
            for (int i = 0; i < drones.Length; i++)
            {
                if (drones[i].IsFoundTarget())
                {
                    IsFoundTarget = true;
                    forgetTargetTimer = 0f;
                    teoreticalPlayerPos.position = players[drones[i].TargetIndex()].transform.position;
                }
            }
            forgetTargetTimer += Time.deltaTime;

            // Act
            if (IsFoundTarget)
            {
                if (AreDronesDeployed)
                {
                    droneDeploySript.DeployDrones();
                    AreDronesDeployed = false;
                }
                AttackTarget();
                for (int i = 0; i < drones.Length; i++)
                {
                    if (drones[i].IsActive())
                    {
                        moveForward = moveBack = moveUp = moveRight = moveLeft = moveDown = false;
                        randomDirection = -transform.forward;
                    }
                }
            }
            else if (!AreDronesDeployed)
            {
                CallDeployDrones();
            }
            else
            {
                moveForward = moveBack = moveUp = moveRight = moveLeft = moveDown = false;
            }

            // Bahaviour
            if (changeDirTimer >= timeToChangeDirrection)
            {
                randomDirection = UnityEngine.Random.onUnitSphere;
                float distanceToHome = Vector3.Distance(transform.position, startPosition);
                if (myAIMode == AIMode.WalkAroundPoint && distanceToHome > maxWalkAway)
                {
                    randomDirection = startPosition - transform.position;
                }
                if (myAIMode == AIMode.DefendPoint && distanceToHome > maxWalkAway)
                {
                    randomDirection = startPosition - transform.position;
                    moveForward = moveBack = moveUp = moveRight = moveLeft = moveDown = false;
                }
                changeDirTimer = 0f;
            }
            else
            {
                changeDirTimer += Time.deltaTime;
            }

            if (avoidObstacles)
            {
                AvoidObstalce();
            }

            // Z translation
            if (Vector3.Dot(randomDirection, transform.forward) >= -0.8f && !moveBack || moveForward)
            {
                speed.z += _acceleration;
                speed.z = Mathf.Clamp(speed.z, -maxSpeed, maxSpeed);
            }
            else if (moveBack)
            {
                speed.z -= _acceleration;
                speed.z = Mathf.Clamp(speed.z, -maxSpeed, maxSpeed);
            }
            else
            {
                if (speed.z > _acceleration)
                    speed.z -= _acceleration;
                else if (speed.z < -_acceleration)
                    speed.z += _acceleration;
                else
                    speed.z = 0;
            }

            // X translation
            if (moveRight)
            {
                speed.x += _acceleration;
                speed.x = Mathf.Clamp(speed.x, -maxSpeed, maxSpeed);
            }
            else if (moveLeft)
            {
                speed.x -= _acceleration;
                speed.x = Mathf.Clamp(speed.x, -maxSpeed, maxSpeed);
            }
            else
            {
                if (speed.x > _acceleration)
                    speed.x -= _acceleration;
                else if (speed.x < -_acceleration)
                    speed.x += _acceleration;
                else
                    speed.x = 0;
            }
            speed = Vector3.ClampMagnitude(speed, maxSpeed);

            // Y translation
            if (moveUp &&
                _visualHeight + speed.y * Time.deltaTime < _upHeightLimit ||
                _visualHeight < _downHeightLimit)
            {
                speed.y += _crouchAaccel;
                speed.y = Mathf.Clamp(speed.y, -_crouchSpeed, _crouchSpeed);
            }
            else if (moveDown &&
                _visualHeight + speed.y * Time.deltaTime > _downHeightLimit ||
                _visualHeight > _upHeightLimit)
            {
                speed.y -= _crouchAaccel;
                speed.y = Mathf.Clamp(speed.y, -_crouchSpeed, _crouchSpeed);
            }
            else
            {
                if (speed.y > _crouchAaccel)
                    speed.y -= _crouchAaccel;
                else if (speed.y < -_crouchAaccel)
                    speed.y += _crouchAaccel;
                else
                    speed.y = 0;
            }
            transform.Translate(Time.deltaTime * speed, Space.Self);
            // Align body
            Vector3 reyStart = transform.position + transform.up * 5;
            Ray ray = new Ray(reyStart, -transform.up);
            if (Physics.Raycast(ray, out RaycastHit rayHit, _rayCastLength, terrainLayer.value))
            {
                _visualHeight = rayHit.distance;

                if (bodyToSurfaceFacing)
                    newQuat = Quaternion.FromToRotation(transform.up, rayHit.normal) * transform.rotation;
                else
                {
                    for (int i = 1; i < legs.Length - 1; i++)
                    {
                        Vector3 difVector1 = (legs[i - 1].position - legs[i].position).normalized;
                        Vector3 difVector2 = (legs[i + 1].position - legs[i].position).normalized;
                        legsNormal = Vector3.Lerp(legsNormal, Vector3.Cross(difVector2, difVector1), _legsPositionImpactWeight / (legs.Length - 2.0f));
                    }
                    newQuat = Quaternion.FromToRotation(transform.up, legsNormal) * transform.rotation;
                    speed *= Mathf.Clamp(Vector3.Dot(transform.up, legsNormal), 0.985f, 1.0f);
                }
                if (stickToMovingObjects)
                {
                    if (_underObjectOldTransform == rayHit.transform)
                    {
                        transform.position += rayHit.transform.position - _oldTransformOldPos;
                    }
                    _underObjectOldTransform = rayHit.transform;
                    _oldTransformOldPos = _underObjectOldTransform.position;
                }
            }

            // Rotation
            if (TurnRight(turnTolerance)) // rotating right
            {
                rotSpeed += _rotationAccel;
                rotSpeed = Mathf.Clamp(rotSpeed, -_rotationSpeedDpS, _rotationSpeedDpS);
            }
            else if (TurnLeft(turnTolerance)) // rotating left
            {
                rotSpeed -= _rotationAccel;
                rotSpeed = Mathf.Clamp(rotSpeed, -_rotationSpeedDpS, _rotationSpeedDpS);
            }
            else
            {
                if (rotSpeed > _rotationAccel)
                    rotSpeed -= _rotationAccel;
                else if (rotSpeed < -_rotationAccel)
                    rotSpeed += _rotationAccel;
                else
                    rotSpeed = 0.0f;
            }
            newQuat = Quaternion.Euler(rotSpeed * Time.deltaTime * rayHit.normal) * newQuat;

            //Physics
            if (Vector3.Angle(transform.up, Vector3.up) > falloffAngle)
            {
                ragdollScript.EnableRagdoll();
            }
            affectSoundBySpeed.SetAddedSpeed(CurrentSpeed());
        }
    }
    float CurrentSpeed()
    {
        float combinedSpeed = speed.magnitude + Quaternion.Angle(newQuat, transform.rotation) * _rotationAccel;
        return combinedSpeed;
    }
    bool TurnRight(float tolerance)
    {
        float speedToForward = Vector3.Dot(Vector3.Cross(randomDirection, transform.forward), transform.up);
        if (speedToForward < -tolerance)
        {
            return true;
        }
        return false;
    }
    bool TurnLeft(float tolerance)
    {
        float speedToForward = Vector3.Dot(Vector3.Cross(randomDirection, transform.forward), transform.up);
        if (speedToForward > tolerance)
        {
            return true;
        }
        return false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(mainHull.position, CheckObstaclesDist.x * mainHull.right);
        Gizmos.DrawRay(mainHull.position, -CheckObstaclesDist.x * mainHull.right);
        Gizmos.DrawRay(mainHull.position, CheckObstaclesDist.y * mainHull.forward);
        Gizmos.DrawRay(mainHull.position, -CheckObstaclesDist.y * mainHull.forward);
        Gizmos.color = Color.green;
    }
    private void AvoidObstalce()
    {
        if (Physics.Raycast(mainHull.position, mainHull.right, CheckObstaclesDist.x, obstacleLayers))
        {
            moveRight = moveDown = false;
            moveLeft = moveUp = true;
        }
        else if (Physics.Raycast(mainHull.position, -mainHull.right, CheckObstaclesDist.x, obstacleLayers))
        {
            moveRight = moveUp = true;
            moveLeft = moveDown = false;
        }
        if (Physics.Raycast(mainHull.position, mainHull.forward, CheckObstaclesDist.y, obstacleLayers))
        {
            moveForward = moveDown = false;
            moveBack = moveUp = true;
        }
        else if (Physics.Raycast(mainHull.position, -mainHull.forward, CheckObstaclesDist.y, obstacleLayers))
        {
            moveForward = moveUp = true;
            moveBack = moveDown = false;
        }
        for (int i = 0; i < legs.Length; i++)
        {
            if (!legScript.IsLegStanding(i))
            {
                Vector3 VectorToSafe = transform.position - legs[i].position;
                float referenceToForward = Vector3.Dot(transform.forward, VectorToSafe);
                float referenceToRight = Vector3.Dot(transform.right, VectorToSafe);
                moveUp = false;
                moveDown = true;
                if (referenceToForward < 0f)
                {
                    moveForward = false;
                    moveBack = true;
                }
                else if (referenceToForward > 0f)
                {
                    moveBack = false;
                    moveForward = true;
                }
                if (referenceToRight < 0f)
                {
                    moveRight = false;
                    moveLeft = true;
                }
                else if (referenceToRight > 0f)
                {
                    moveRight = true;
                    moveLeft = false;
                }
            }
        }
    }
    private void AttackTarget()
    {
        float distanceToPlayer = Vector3.Distance(teoreticalPlayerPos.position, transform.position);
        randomDirection = teoreticalPlayerPos.position - transform.position;
        if (distanceToPlayer > maxToPlayerDistance)
        {
            moveBack = false;
            moveForward = true;
        }
        else if (distanceToPlayer < minToPlayerDistance)
        {
            moveForward = false;
            moveBack = true;
        }
        if (changeDirTimer >= timeToChangeDirrection)
        {
            if (UnityEngine.Random.Range(-1.0f, 1.0f) >= 0f)
            {
                moveRight = moveUp = true;
                moveLeft = moveDown = false;
            }
            else
            {
                moveLeft = moveDown = true;
                moveUp = moveRight = false;
            }
            changeDirTimer = 0f;
        }
    }

    public void ResetTargets()
    {
        players = GameObject.FindGameObjectsWithTag(playerTag);
    }

    public void CallDeployDrones()
    {
        droneDeploySript.DeployDrones();
        AreDronesDeployed = true;
    }
}
