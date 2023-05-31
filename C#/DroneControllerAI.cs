using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;

public class DroneControllerAI : MonoBehaviour
{
    [SerializeField] float minFlyHeight = 5f;
    [SerializeField] float minDistance = 5f;
    [SerializeField] float maxDistance = 15f;
    [SerializeField] float flyMaxSpeed = 10f;
    [SerializeField] float flyAccel = 1f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float enemyFindDistance = 200f;
    [SerializeField] Vector3 landingOffset;
    [SerializeField] LayerMask avoidObjects;
    [SerializeField] Transform ownerBody;
    [SerializeField] GameObject AliveStatus;
    [SerializeField] FanController fan;
    [SerializeField] AudioSource fanSound;
    [SerializeField] TrailRenderer droneTrail;
    [SerializeField] string enemyTag;

    private Vector3 moveDirrection = new Vector3();
    private Vector3 oldPos = new Vector3();
    private bool isActive = false;
    private Vector3 startPos;
    private int sideOfSitting;
    private bool goHome = false;
    private bool gotPoint = false;
    private GameObject[] enemies;
    private Transform target;
    private int targetInt = 0;
    private bool isTargetFound = false;
    private bool isWorking = true;
    private void Awake()
    {
        AliveStatus.SetActive(false);
        fan.enabled = false;
        fanSound.enabled = false;
        droneTrail.enabled = false;
        sideOfSitting = Vector3.Dot(transform.up, ownerBody.right)  > 0.0f ? 1 : -1;
        startPos = ownerBody.InverseTransformPoint(transform.position);
        ResetTargets();

    }
    void Update()
    {
        if (isActive && isWorking)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                float distanceToEnemy = Vector3.Distance(transform.position, enemies[i].transform.position);
                if (!Physics.Linecast(transform.position, enemies[i].transform.position, avoidObjects) && distanceToEnemy <= enemyFindDistance && enemies[i].activeSelf)
                {
                    target = enemies[i].transform;
                    targetInt = i;
                    isTargetFound = true;
                }
                else
                {
                    isTargetFound = false;
                }
            }
            float distanceToOwner = Vector3.Distance(ownerBody.position, transform.position);

            // Creating random move dirrection
            if (distanceToOwner < maxDistance)
            {
                moveDirrection += Time.deltaTime * flyAccel * Random.onUnitSphere;
            }
            else if (distanceToOwner > minDistance)
            {
                moveDirrection += Time.deltaTime * (ownerBody.position - transform.position).normalized;
            }
            if (goHome)
                LandDrone();
            else
            {
                transform.Translate(Time.deltaTime * moveDirrection, Space.World);
                Quaternion droneRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
                droneRotation = Quaternion.FromToRotation(transform.forward, (transform.position - oldPos).normalized) * droneRotation;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, droneRotation, Time.deltaTime * rotationSpeed);
            }
            if (moveDirrection.magnitude > flyMaxSpeed)
                moveDirrection *= flyMaxSpeed / moveDirrection.magnitude;

            Ray moveRay = new Ray(transform.position, (transform.position - oldPos).normalized);

            if (Physics.Raycast(moveRay, out RaycastHit rayHit, minFlyHeight, avoidObjects))
            {
                moveDirrection += Time.deltaTime * flyMaxSpeed * flyAccel * rayHit.normal;
            }
            oldPos = transform.position;
        }
    }
    private void LateUpdate()
    {
        if (!isActive && isWorking)
        {
            transform.position = ownerBody.TransformPoint(startPos);
            transform.rotation = Quaternion.FromToRotation(transform.up, sideOfSitting * ownerBody.right) * transform.rotation;
            goHome = false;
        }      
    }
    public void LaunchDrone()
    {
        AliveStatus.SetActive(true);
        moveDirrection = 2f * flyMaxSpeed * Time.deltaTime * (transform.up + flyAccel * transform.forward);
        isActive = true;
        fan.enabled = true;
        fanSound.enabled = true;
        droneTrail.enabled = true;
        droneTrail.Clear();
    }
    public void LandDrone()
    {
        goHome = true;
        Quaternion alignRotation = Quaternion.LookRotation(ownerBody.up, sideOfSitting * ownerBody.right);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, alignRotation, Time.deltaTime * rotationSpeed);
        moveDirrection = (ownerBody.TransformPoint(startPos) - transform.position).normalized;
        Vector3 offsetPos = startPos + landingOffset;

        if (Vector3.Distance(transform.position, ownerBody.TransformPoint(offsetPos)) > 0.1f && !gotPoint)
        {
            moveDirrection = (ownerBody.TransformPoint(offsetPos) - transform.position).normalized * flyMaxSpeed;
        }
        else
        {
            gotPoint = true;
        }
        if (Vector3.Distance(transform.position, ownerBody.position) > 7f)
        {
            gotPoint = false;
        }

        transform.Translate(Time.deltaTime * moveDirrection, Space.World);
        if (DistanceToOwner() < 0.01f)
        {
            AliveStatus.SetActive(false);
            gotPoint = false;
            fan.enabled = false;
            isActive = false;
            fanSound.enabled = false;
            droneTrail.enabled = false;
        }
    }
    public bool IsActive()
    {
        return isActive;
    }
    public int SideValue()
    {
        return sideOfSitting;
    }
    public float DistanceToOwner()
    {
        return Vector3.Distance(ownerBody.TransformPoint(startPos), transform.position);
    }
    public bool IsFoundTarget()
    {
        return isTargetFound;
    }
    public int TargetIndex()
    {
        return targetInt;
    }
    public void DisableScript()
    {
        isWorking = false;
    }
    public void ResetTargets()
    {
        enemies = GameObject.FindGameObjectsWithTag(enemyTag);
    }
    public void OnDisable()
    {
        isWorking = true;
    }
}
