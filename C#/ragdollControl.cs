using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ragdollControl : MonoBehaviour
{
    [SerializeField] bool usePhysics = true;
    [SerializeField] GameObject player;
    [SerializeField] NLegIKSolver legScript;
    [SerializeField] int numberOfLegsToStand = 3;
    [SerializeField] float activateCooldown = 5f;

    private Rigidbody[] _ragdolsRigidbodies;
    private bool isPhysicsEnabled = false;
    private float reloadTime = 0f;
    private bool willFall = false;
    private bool isPhysicisForced = false;
    // Start is called before the first frame update
    void Awake()
    {
        _ragdolsRigidbodies = player.GetComponentsInChildren<Rigidbody>();
        DisableRagdoll();
        reloadTime = activateCooldown;
    }
    void FixedUpdate()
    {
        if (usePhysics)
        {
            int numOfStandingLegs = 0;
            int numOfLegs = legScript.NumberOfLegs();
            for (int i = 0; i < numOfLegs; i++)
            {
                if (legScript.IsLegStanding(i))
                    numOfStandingLegs++;
            }

            if ((numOfStandingLegs < numberOfLegsToStand || isPhysicisForced) && !isPhysicsEnabled)
            {
                if (willFall)
                {
                    EnableRagdoll();
                    willFall = false;
                }  
                else
                {
                    willFall = true;
                }
            }
            else if (isPhysicsEnabled && numOfStandingLegs >= numberOfLegsToStand && reloadTime >= activateCooldown && !isPhysicisForced)
            {
                DisableRagdoll();                
            }
            else if (reloadTime < activateCooldown)
            {
                reloadTime += Time.deltaTime;
            }
        }
    }
    void DisableRagdoll()
    {
        for (int i = 0; i < _ragdolsRigidbodies.Length; i++)
        {
            _ragdolsRigidbodies[i].isKinematic = true;
        }
        player.GetComponent<Rigidbody>().isKinematic = true;
        isPhysicsEnabled = false;
    }
    public void EnableRagdoll()
    {
        for (int i = 0; i < _ragdolsRigidbodies.Length; i++)
        {
            _ragdolsRigidbodies[i].isKinematic = false;
        }
        player.GetComponent<Rigidbody>().isKinematic = false;
        isPhysicsEnabled = true;
        reloadTime = 0f;
    }
    public bool IsPhysicsActive()
    {
        return isPhysicsEnabled;
    }
    public bool IsPhysicsRecharged()
    {
        return reloadTime >= activateCooldown;
    }

    public void DisableScript()
    {
        isPhysicisForced = true;
    }
    public void OnDisable()
    {
        isPhysicisForced = false;
    }
}
