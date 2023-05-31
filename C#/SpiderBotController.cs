using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpiderBotController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] LayerMask terrainLayer = default;
    [SerializeField] Transform objectToCastStopRay;
    [SerializeField] SoundManager affectSoundBySpeed;
    [Header("Movement")]
    [SerializeField] float _maximumAddHeight = 15f;
    [SerializeField] float _minimumAddHeight = 5f;
    [Range(1, 1000)] int _rayCastLength = 10;
    [SerializeField] Vector2 rayStopMoveBounds;
    [SerializeField] float _speedMpS = 10f;
    [SerializeField] float _acceleration = 1f;
    [SerializeField] float _rotationSpeedDpS = 10f;
    [SerializeField] float _rotationAccel = 1f;
    [SerializeField] float _crouchSpeed = 12f;
    [SerializeField] float _crouchAaccel = 1f;
    [SerializeField] bool bodyToSurfaceFacing = true;
    [Range(0.01f, 1.0f)]
    public float _legsPositionImpactWeight;
    [Header("Physics")]
    [SerializeField] ragdollControl ragdollScript;
    [SerializeField] bool stickToMovingObjects = false;
    [Range(0, 180)] public int falloffAngle;
    [Header("Controls")]
    [SerializeField] string moveActionName;
    [SerializeField] string crouchActionName;
    [SerializeField] string turnActionName;
    private InputAction playerMovement;
    private InputAction playerCrouch;
    private InputAction playerTurning;

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
    private PlayerInput playersInputs;
    private Vector3 SpeedDirection;

    private bool moveBack, moveForward, moveRight, moveLeft, moveUp = false;

    private void OnEnable()
    {
        if (TryGetComponent<PlayerInput>(out playersInputs))
        {
            playerMovement = playersInputs.actions[moveActionName];
            playerCrouch = playersInputs.actions[crouchActionName];
            playerTurning = playersInputs.actions[turnActionName];
            playerMovement.Enable();
            playerCrouch.Enable();
            playerTurning.Enable();
        }
            
    }
    private void OnDisable()
    {
        if (playersInputs != null)
        {
            playerMovement.Disable();
            playerCrouch.Disable();
            playerTurning.Disable();
        }       
    }
    void Awake()
    {
        newQuat = transform.rotation;
        Vector3 reyStart = transform.position + transform.up * 5;
        Ray ray = new Ray(reyStart, -transform.up);
        if (Physics.Raycast(ray, out RaycastHit rayHit, _rayCastLength, terrainLayer))
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
            int inverse = 1;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, newQuat, _rotationSpeedDpS * Time.deltaTime);
            // Z translation
            maxSpeed = _speedMpS * (_upHeightLimit*2f / _visualHeight);
            Vector2 moveDirection = playerMovement.ReadValue<Vector2>();
            if (moveDirection.y > 0f && !moveBack || moveForward)
            {
                speed.z += _acceleration;
                speed.z = Mathf.Clamp(speed.z, -maxSpeed, maxSpeed);
            }
            else if (moveDirection.y < 0f || moveBack)
            {
                speed.z -= _acceleration;
                speed.z = Mathf.Clamp(speed.z, -maxSpeed, maxSpeed);
                inverse = -1;
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
            if (moveDirection.x < 0f && !moveRight || moveLeft)
            {
                speed.x -= _acceleration;
                speed.x = Mathf.Clamp(speed.x, -maxSpeed, maxSpeed);
            }
            else if (moveDirection.x > 0f || moveRight)
            {
                speed.x += _acceleration;
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
            if ((playerCrouch.ReadValue<float>() > 0 || moveUp) &&
                _visualHeight + speed.y * Time.deltaTime < _upHeightLimit ||
                _visualHeight < _downHeightLimit)
            {
                speed.y += _crouchAaccel;
                speed.y = Mathf.Clamp(speed.y, -_crouchSpeed, _crouchSpeed);
            }
            else if (playerCrouch.ReadValue<float>() < 0 &&
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
            StopEnteringObjects();
            transform.Translate(Time.deltaTime * speed, Space.Self);
            // Align body

            Vector3 reyStart = transform.position + transform.up * 5;
            Ray ray = new Ray(reyStart, -transform.up);
            if (Physics.Raycast(ray, out RaycastHit rayHit, _rayCastLength, terrainLayer))
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
            // Rotation accel
            if (playerTurning.ReadValue<float>() > 0) // Right
            {
                rotSpeed += _rotationAccel * inverse;
                rotSpeed = Mathf.Clamp(rotSpeed, -_rotationSpeedDpS, _rotationSpeedDpS);
            }
            else if (playerTurning.ReadValue<float>() < 0) // Left
            {
                rotSpeed -= _rotationAccel * inverse;
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

            if (Vector3.Angle(transform.up, Vector3.up) > falloffAngle)
            {
                ragdollScript.EnableRagdoll();
            }
            affectSoundBySpeed.SetAddedSpeed(CurrentSpeed());
        } // physics effect ends here
    }
    public float CurrentSpeed()
    {
        float combinedSpeed = speed.magnitude + Quaternion.Angle(newQuat, transform.rotation) * _rotationAccel;
        return combinedSpeed;
    }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(objectToCastStopRay.position + rayStopMoveBounds.y * objectToCastStopRay.forward, 0.5f);
        Gizmos.DrawSphere(objectToCastStopRay.position - rayStopMoveBounds.y * objectToCastStopRay.forward, 0.5f);
        Gizmos.DrawSphere(objectToCastStopRay.position + rayStopMoveBounds.x * objectToCastStopRay.right, 0.5f);
        Gizmos.DrawSphere(objectToCastStopRay.position - rayStopMoveBounds.x * objectToCastStopRay.right, 0.5f);
    }
    private void StopEnteringObjects()
    {
        moveBack = moveForward = moveRight = moveLeft = moveUp = false;
        if (Physics.Linecast(objectToCastStopRay.position, objectToCastStopRay.position + rayStopMoveBounds.y * objectToCastStopRay.forward, terrainLayer))
            moveUp = moveBack = true;
        else if (Physics.Linecast(objectToCastStopRay.position, objectToCastStopRay.position - rayStopMoveBounds.y * objectToCastStopRay.forward, terrainLayer))
            moveUp = moveForward = true;
        if (Physics.Linecast(objectToCastStopRay.position, objectToCastStopRay.position + rayStopMoveBounds.x * objectToCastStopRay.right, terrainLayer))
            moveUp = moveLeft = true;
        else if (Physics.Linecast(objectToCastStopRay.position, objectToCastStopRay.position - rayStopMoveBounds.x * objectToCastStopRay.right, terrainLayer))
            moveUp = moveRight = true;
    }
}
