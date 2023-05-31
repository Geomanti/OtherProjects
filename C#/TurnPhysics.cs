using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnPhysics : MonoBehaviour
{
    [SerializeField] GameObject Player;

    private Rigidbody[] rigidBodies;
    void Awake()
    {
        rigidBodies = Player.GetComponentsInChildren<Rigidbody>();
    }

    // Update is called once per frame
    public void DisableScript()
    {
        for (int i = 0; i < rigidBodies.Length; i++)
        {
            rigidBodies[i].isKinematic = false;
        }
        Player.GetComponent<BoxCollider>().isTrigger = false;
    }
    private void OnDisable()
    {
        for (int i = 0; i < rigidBodies.Length; i++)
        {
            rigidBodies[i].isKinematic = true;
        }
        Player.GetComponent<BoxCollider>().isTrigger = true;
    }
}
