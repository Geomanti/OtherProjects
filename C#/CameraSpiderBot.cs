using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
class CameraSettings
{
    public string _name;
    public Camera cam;
    public float maxLookAngle = 80f;
    public float minLookAngle = -80f;
    public float scrollMinDistance = 1f;
    public float scrollMaxDistance = 50f;
    public float sensitivity = 1f;
    public float scrollSpeed = 1f;
    public bool ignoreCollision = false;
    public bool UseButton = false;
    public string lockCameraActionName = "Lock Camera";
    private InputAction lockAction;
    public void SetInputAction(PlayerInput inputs)
    {
        if (UseButton)
        {
            lockAction = inputs.actions[lockCameraActionName];
            lockAction.Enable();
        }       
    }
    public void DisableAction()
    {
        if (UseButton)
        {
            lockAction.Disable();
        }
    }
    public InputAction GetAction()
    {
        return lockAction;
    }
}
public class CameraSpiderBot : MonoBehaviour
{
    [SerializeField] bool active = true;
    [SerializeField] bool noClipThroughObjects = true;
    [SerializeField] LayerMask ignoreToCamHitLayer;
    [SerializeField] Transform target;
    [SerializeField] string mouseDeltaActionName = "Look";
    [SerializeField] string changeCameraActionName = "Change Camera";
    [SerializeField] CameraSettings[] cameras = new CameraSettings[0];

    private PlayerInput playerInputs;
    private InputAction mouseDelta;
    private InputAction changeCamera;
    int indexActive = 0;
    private void OnEnable()
    {
        if (TryGetComponent<PlayerInput>(out playerInputs))
        {
            mouseDelta = playerInputs.actions[mouseDeltaActionName];
            changeCamera = playerInputs.actions[changeCameraActionName];
            mouseDelta.Enable();
            changeCamera.Enable();
            foreach (CameraSettings cam in cameras)
            {
                cam.SetInputAction(playerInputs);
            }
        }           
        
    }
    private void OnDisable()
    {
        if (playerInputs != null)
        {
            mouseDelta.Disable();
            changeCamera.Disable();
            foreach (CameraSettings cam in cameras)
            {
                cam.DisableAction();
            }
        }
        
    }
    void Start()
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].cam.gameObject.transform.LookAt(target, Vector3.up);
            cameras[i].cam.gameObject.transform.SetParent(target);

            if (i != 0)
                cameras[i].cam.gameObject.SetActive(false);
        }
        ignoreToCamHitLayer = ~ignoreToCamHitLayer;

    }
    void LateUpdate()
    {
        
        if (changeCamera.triggered && active)
        {
            cameras[indexActive++].cam.gameObject.SetActive(false);
            if (indexActive == cameras.Length)
                indexActive = 0;
            cameras[indexActive].cam.gameObject.SetActive(true);
        }
        if (Time.time > 1f && active)
        {
            Vector2 delta = mouseDelta.ReadValue<Vector2>();
            float horizontal = cameras[indexActive].sensitivity * delta.x * 0.1f;
            float vertical = -cameras[indexActive].sensitivity * delta.y * 0.1f;
            bool button = false;
            if (cameras[indexActive].GetAction() != null)
            {
                button = cameras[indexActive].GetAction().IsPressed();
            }
            if (!button)
            {
                MakeClampedRotation(horizontal, vertical);
            }            
            Vector3 camPosition = cameras[indexActive].cam.transform.position;
            camPosition += Input.mouseScrollDelta.y * cameras[indexActive].scrollSpeed * cameras[indexActive].cam.transform.forward;
            float distance = Vector3.Distance(camPosition, target.position);

            if (distance < cameras[indexActive].scrollMinDistance)
                camPosition -= (cameras[indexActive].scrollMinDistance - distance) * cameras[indexActive].cam.transform.forward;
            else if (distance > cameras[indexActive].scrollMaxDistance)
                camPosition += (distance - cameras[indexActive].scrollMaxDistance) * cameras[indexActive].cam.transform.forward;

            if (Physics.Linecast(target.position, camPosition, out RaycastHit hitInfo, ignoreToCamHitLayer) && noClipThroughObjects && !cameras[indexActive].ignoreCollision)
            {
                camPosition = Vector3.Lerp(camPosition, hitInfo.point, 1.0f);
            }
            cameras[indexActive].cam.transform.position = camPosition;
        }        
    }
    private void MakeClampedRotation(float horizontal, float vertical)
    {
        target.rotation = Quaternion.AngleAxis(horizontal, Vector3.up) * target.rotation;

        if (target.rotation.eulerAngles.x < cameras[indexActive].maxLookAngle ||
            vertical < 0 && target.rotation.eulerAngles.x <= 90f)
        {
            target.rotation = Quaternion.AngleAxis(vertical, cameras[indexActive].cam.transform.right) * target.rotation;
        }
        else if (target.rotation.eulerAngles.x > 360f + cameras[indexActive].minLookAngle ||
            vertical > 0 && target.rotation.eulerAngles.x >= 270f)
        {
            target.rotation = Quaternion.AngleAxis(vertical, cameras[indexActive].cam.transform.right) * target.rotation;
        }
    }
}
