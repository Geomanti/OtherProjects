using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectToCameraLook : MonoBehaviour
{
    [SerializeField] Camera[] cameras;
    [SerializeField] Transform objectToLimitAngles;
    [SerializeField] Transform focusPoint;
    [SerializeField] LayerMask Player = default;
    [SerializeField] public bool viewCursor = true;
    [SerializeField] string lockCameraActionName = "Lock Camera";
    private InputAction lockCamera;
    [Header("Movement")]
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float minAngle = -15f;
    [SerializeField] float maxAngle = 35f;
    // Start is called before the first frame update
    Transform camTransform;
    int index = 0;
    private bool isWorking = true;
    private PlayerInput playersInputs;
    private void OnEnable()
    {
        if (TryGetComponent<PlayerInput>(out playersInputs))
        {
            lockCamera = playersInputs.actions[lockCameraActionName];
            lockCamera.Enable();
        }
            
    }
    private void OnDisable()
    {
        isWorking = true;
        if (playersInputs != null)
            lockCamera.Disable();
    }
    void Start()
    {
        camTransform = cameras[index].transform;
        if (!viewCursor)
            Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!cameras[index].isActiveAndEnabled)
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].isActiveAndEnabled)
                {
                    camTransform = cameras[i].transform;
                    index = i;
                    break;
                }
            }
            
        }
        if (!lockCamera.IsPressed() && isWorking)
        {
            LayerMask PlayerLayer = Player;
            PlayerLayer = ~PlayerLayer;
            Ray ray = new Ray(camTransform.position, camTransform.forward);

            if (Physics.Raycast(ray, out RaycastHit rayHit, Mathf.Infinity, PlayerLayer))
            {
                Vector3 relativeFocPoint = objectToLimitAngles.InverseTransformPoint(focusPoint.position).normalized;
                Vector3 relativeRayHit = objectToLimitAngles.InverseTransformPoint(rayHit.point).normalized;

                Vector3 newLocation = Vector3.RotateTowards(relativeFocPoint, relativeRayHit, rotationSpeed*Time.deltaTime, 1.0f);

                if (newLocation.y < Mathf.Tan(minAngle) - 1.0f)
                    newLocation.y = Mathf.Tan(minAngle) - 1.0f;
                else if (newLocation.y > Mathf.Tan(maxAngle))
                    newLocation.y = Mathf.Tan(maxAngle);

                focusPoint.position = objectToLimitAngles.TransformPoint(newLocation);
                

            }
                
        }

    }
    public void DisableScript()
    {
        isWorking = false;
    }
}
