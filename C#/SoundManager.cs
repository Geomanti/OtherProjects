using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[Serializable]
public class soundEmitter
{
    public string name;
    public AudioSource audio;
    public bool startPlaying = false;
    public float startDelay = 10f;
    public bool loop = false;
    public float cooldown = 0f;
    public bool affectBySpeed = false;
    public float speedMultiply = 1f;
    public bool useButton = false;
    public string ActivateActionName = "Play Sound";

    private float activateTime;
    private InputAction lockAction;
    public void SetInputAction(PlayerInput inputs)
    {
        if (useButton)
        {
            lockAction = inputs.actions[ActivateActionName];
            lockAction.Enable();
        }
    }
    public void DisableAction()
    {
        if(useButton)
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
    public void Recharge()
    {
        activateTime += Time.deltaTime;
    }
    public bool IsRecharged()
    {
        return (activateTime >= cooldown);
    }
}
public class SoundManager : MonoBehaviour
{
    public soundEmitter[] sounds = new soundEmitter[0];
    // Start is called before the first frame update
    private float addedSpeed;
    private bool isWorking = true;
    private PlayerInput playerInputs;
    private void OnEnable()
    {
        if (TryGetComponent<PlayerInput>(out playerInputs))
        {
            foreach (soundEmitter son in sounds)
            {
                son.SetInputAction(playerInputs);
            }
        }

    }
    private void OnDisable()
    {
        isWorking = true;
        if (playerInputs != null)
        {
            foreach (soundEmitter son in sounds)
            {
                son.DisableAction();
            }
        }
    }
        void Awake()
    {
        foreach (soundEmitter audio in sounds)
        {
            if (audio.loop)
                audio.audio.loop = true;

            if (audio.startPlaying)
                audio.audio.PlayDelayed(audio.startDelay);

            audio.Cooldown(audio.cooldown);
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (isWorking)
        {
            foreach (soundEmitter audio in sounds)
            {
                if (!audio.loop)
                {
                    if (audio.GetAction().IsPressed() && audio.IsRecharged())
                    {
                        audio.Cooldown(0f);
                        audio.audio.Play();
                    }

                    audio.Recharge();
                }
                else if (audio.affectBySpeed)
                {
                    if (addedSpeed > 0.0f)
                        audio.audio.pitch = Mathf.Clamp(1.0f + addedSpeed * audio.speedMultiply, 1.0f, 1.5f);
                    else
                        audio.audio.pitch = 1.0f;
                }
            }
        }
        
    }
    public void SetAddedSpeed(float speed) 
    { 
        addedSpeed = speed;
    }
    public void DisableScript()
    {
        isWorking = false;
    }
}
