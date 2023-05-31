using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[Serializable]
public class ShapeKeyObject
{
    public string name;
    public GameObject target;
    public AnimationCurve fallOff;
    public float cooldown = 5f;
    public float animSpeedMul = 1f;
    public bool activateByButton = true;
    public bool activateByFunction = false;
    public string activateActionName = "Shape";

    private float activateTime;
    private bool activate = false;
    private InputAction lockAction;



    public void SetInputAction(PlayerInput inputs)
    {
        if (activateByButton)
        {
            lockAction = inputs.actions[activateActionName];
            lockAction.Enable();
        } 
    }
    public void DisableAction()
    {
        if (activateByButton)
        {
            lockAction.Disable();
        }
    }
    public InputAction GetAction()
    {
        return lockAction;
    }
    public void Cooldown(float time)
    {
        activateTime = time;
    }
    public float Cooldown()
    {
        return (activateTime);
    }
    public void Recharge()
    {
        activateTime += Time.deltaTime;
    }
    public bool IsRecharged()
    {
        return (activateTime >= cooldown);
    }
    public void ChangeShape()
    {
        target.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(0, fallOff.Evaluate(this.Cooldown()*animSpeedMul) *100f);
    }
    public bool IsActive()
    {
        return activate;
    }
    public void Activate()
    {
        activate = true;
    }
    public void Disable()
    {
        activate = false;
    }

}
public class ShapeKeyController : MonoBehaviour
{
    public ShapeKeyObject[] objects = new ShapeKeyObject[0];
    private PlayerInput playerInputs;

    private void OnEnable()
    {
        if (TryGetComponent<PlayerInput>(out playerInputs))
        {
            foreach (ShapeKeyObject obj in objects)
            {
                obj.SetInputAction(playerInputs);
            }
        }
        
    }
    private void OnDisable()
    {
        if (playerInputs != null)
        {
            foreach (ShapeKeyObject obj in objects)
            {
                obj.DisableAction();
            }
        }    
        
    }
    void Start()
    {
        foreach (ShapeKeyObject animObject in objects)
        {
            animObject.Cooldown(animObject.cooldown);
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        foreach (ShapeKeyObject animObject in objects)
        {
            if (animObject.activateByButton)
            {
                if (animObject.GetAction().IsPressed() && animObject.IsRecharged())
                {
                    animObject.Cooldown(0f);
                }
                animObject.ChangeShape();
                animObject.Recharge();
            }
            else if (!animObject.activateByFunction || animObject.IsActive())
            {
                if (animObject.IsRecharged())
                {
                    animObject.Cooldown(0f);
                }

                animObject.ChangeShape();
                animObject.Recharge();
            }
            

        }
    }
    public void ActivateShapeAnimation(int index)
    {
        if( index < objects.Length)
        {
            objects[index].Activate();
        }
    }
    public void DisableShapeAnimation(int index)
    {
        if (index < objects.Length)
        {
            objects[index].Disable();
        }
    }
}
