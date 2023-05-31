using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
class Hangar
{
    public string name;
    public Transform body;

    private Quaternion openRotation;
    private Quaternion closeRotation;

    public void setQuaternions(float angle, int dirrection)
    {
        openRotation = Quaternion.Euler(0f, 0f, dirrection * angle) * body.localRotation;
        closeRotation = Quaternion.Euler(0f, 0f, -dirrection * angle) * body.localRotation;
    }
    public Quaternion OpenRot()
    {
        return openRotation;
    }
    public Quaternion CloseRot()
    {
        return closeRotation;
    }
}

public class DroneDeploy : MonoBehaviour
{
    [SerializeField] DroneControllerAI[] drones = new DroneControllerAI[0];
    [SerializeField] Hangar[] hangars = new Hangar[0];
    [SerializeField] float openTime = 2f;
    [SerializeField] float openAngle = 45f;
    [SerializeField] string deployActionName = "Deploy";
    private PlayerInput playerInputs;

    private bool[] triggerHangars, isBotAway;
    private float[] timer;
    private bool isWorking = true;
    private InputAction deployDrones;
    
    private void OnEnable()
    {
        if (TryGetComponent<PlayerInput>(out playerInputs))
        {
            deployDrones = playerInputs.actions[deployActionName];
            deployDrones.Enable();
        }
            
    }
    private void OnDisable()
    {
        isWorking = true;
        if (playerInputs != null)
        {
            deployDrones.Disable();
        }       
    }
    void Start()
    {
        triggerHangars = new bool[drones.Length];
        timer = new float[drones.Length];
        isBotAway = new bool[drones.Length];
        for (int i = 0; i < drones.Length; i++) 
        {
            isBotAway[i] = false;
            triggerHangars[i] = false;
            timer[i] = openTime;
            int oddValue = i % 2 == 0 ? -1 : 1;
            hangars[i].setQuaternions(openAngle, oddValue);
        }

        
    }
    void Update()
    {
        if (isWorking)
        {
            if (deployDrones != null && deployDrones.triggered)
            {
                DeployDrones(); // deploy drones
            }
            for (int i = 0; i < drones.Length; i++)
            {
                if (timer[i] < openTime && !triggerHangars[i]) // close hangar
                {
                    hangars[i].body.localRotation = Quaternion.RotateTowards(hangars[i].body.localRotation, hangars[i].CloseRot(), openAngle * Time.deltaTime / openTime);
                    timer[i] += Time.deltaTime;

                    if (timer[i] >= openTime)
                        isBotAway[i] = !isBotAway[i];
                }
                if (timer[i] < openTime && triggerHangars[i] && drones[i].DistanceToOwner() < 1f) // open hangar at certain distance
                {
                    hangars[i].body.localRotation = Quaternion.RotateTowards(hangars[i].body.localRotation, hangars[i].OpenRot(), openAngle * Time.deltaTime / openTime);
                    timer[i] += Time.deltaTime;

                    if (timer[i] >= openTime)
                    {
                        triggerHangars[i] = false;
                        timer[i] = 0f;
                        if (!drones[i].IsActive() && !isBotAway[i])
                        {
                            drones[i].LaunchDrone();
                        }
                    }
                }
            }
        }
        
    }
    public void DeployDrones()
    {
        for (int i = 0; i < drones.Length; i++)
        {
            triggerHangars[i] = true;
            timer[i] = 0f;
            if (drones[i].IsActive())
                drones[i].LandDrone();
        }
    }
    public void DisableScript()
    {
        isWorking = false;
    }
}
