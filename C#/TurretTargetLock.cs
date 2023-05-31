using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TurretTargetLock : MonoBehaviour
{
    [SerializeField] Transform turretFocusPoint;
    [SerializeField] Transform turretHead;
    [SerializeField] Transform shootPoint;
    [SerializeField] LayerMask obstacleLayer;
    [SerializeField] string targetTag;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float targetLockDistance = 10f;
    [SerializeField] float startShootAngle = 5f;

    private GameObject[] enemys;
    private bool targetLocked, isAbleToShoot = false;
    private Transform target;
    private bool isWorking = true;
    private int indexOfTarget = 0;
    void Awake()
    {
        ResetTargets();
    }
    void Update()
    {
        if (isWorking)
        {
            if (!targetLocked)
            {
                isAbleToShoot = false;
                float CheckDistance = targetLockDistance;
                for (int i = 0; i < enemys.Length; i++)
                {
                    if (enemys[i] != null && enemys[i].activeSelf)
                    {
                        float distanceToEnemy = Vector3.Distance(transform.position, enemys[i].transform.position);
                        if (distanceToEnemy <= targetLockDistance && distanceToEnemy <= CheckDistance)
                        {
                            if (!Physics.Linecast(turretHead.position, enemys[i].transform.position, obstacleLayer))
                            {
                                target = enemys[i].transform;
                                indexOfTarget = i;
                                CheckDistance = Vector3.Distance(transform.position, enemys[i].transform.position);
                                targetLocked = true;
                            }
                        }
                    }
                }
            }
            else
            {
                float distanceToEnemy = Vector3.Distance(transform.position, target.position);
                turretFocusPoint.position = Vector3.RotateTowards(turretFocusPoint.position, target.position, rotationSpeed * Time.deltaTime, targetLockDistance / distanceToEnemy);
                if (Vector3.Angle(shootPoint.position - turretHead.position, target.position - turretHead.position) < startShootAngle)
                    isAbleToShoot = true;
                else
                    isAbleToShoot = false;
                if (distanceToEnemy > targetLockDistance || !target.gameObject.activeSelf)
                    targetLocked = false;
                if (Physics.Linecast(turretHead.position, target.position, obstacleLayer))
                    targetLocked = false;
            }
        }
        
    }
    public bool IsTargetLocked()
    {
        return targetLocked;
    }
    public bool IsAbleToShoot()
    {
        return isAbleToShoot;
    }
    public void DisableScript()
    {
        isWorking = false;
    }
    public GameObject getTargetObject()
    {
        return target.gameObject;
    }
    public void ResetTargets()
    {
        enemys = GameObject.FindGameObjectsWithTag(targetTag);
    }
    public int GetTargetIndex()
    {
        return indexOfTarget;
    }
    private void OnDisable()
    {
        isWorking = true;
    }
}
