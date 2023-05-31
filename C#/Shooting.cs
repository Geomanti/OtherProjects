using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Pool;

public class Shooting : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] AudioSource shootSound;
    [SerializeField] AudioSource reloadSound;
    [SerializeField] ShapeKeyController animControl;
    [SerializeField] TurretTargetLock targetLock;
    [SerializeField] int indexOfShapeAnim = 0;
    [SerializeField] bool usePool = false;
    [SerializeField] bool useShotPoint2 = false;
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] Transform shootPoint;
    [SerializeField] Transform shootPoint2;
    [SerializeField] GameObject gunShotParticles;
    [SerializeField] float reloadTime = 10f;
    [SerializeField] float reloadSoundStart = 1.33f;
    [SerializeField] float bulletOffset = 10f;
    [Header("Controls")]
    [SerializeField] string fireActionName = "Fire";
    [SerializeField] string changeCameraActionName = "Change Camera";
    private InputAction playerFire;
    private InputAction ChangeCamera;

    private bool switchedPoint = false;
    private ObjectPool<Bullet> pool;
    float savedReloadTime;
    private GameObject particlesController;
    private bool isWorking = true;
    private PlayerInput playersInputs;
    private bool triggerReloadSound = false;

    private void OnEnable()
    {
        playersInputs = GetComponent<PlayerInput>();
        if (playersInputs != null )
        {
            playerFire = playersInputs.actions[fireActionName];
            ChangeCamera = playersInputs.actions[changeCameraActionName];
            playerFire.Enable();
            ChangeCamera.Enable();
        }        
    }
    private void OnDisable()
    {
        isWorking = true;
        playersInputs = GetComponent<PlayerInput>();
        if (playersInputs != null)
        {
            playerFire.Disable();
            ChangeCamera.Disable();
        }
           
    }
    void Awake()
    {
        pool = new ObjectPool<Bullet>(() => {
            return Instantiate(bulletPrefab);
        }, bullet => {
            bullet.gameObject.SetActive(true);
        }, bullet => {
            bullet.gameObject.SetActive(false);
        }, bullet => {
            Destroy(bullet.gameObject);
        }, false, 10, 20);

        savedReloadTime = reloadTime;
        particlesController = Instantiate(gunShotParticles);
        particlesController.transform.position = shootPoint.position;
        particlesController.transform.SetParent(shootPoint);
        particlesController.transform.rotation = Quaternion.identity;
        particlesController.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (isWorking)
        {
            if (targetLock != null && targetLock.IsAbleToShoot())
                MakeAShot();
            else if (playerFire != null && playerFire.IsPressed())
                MakeAShot();

            if (reloadTime < savedReloadTime)
            {
                reloadTime += Time.deltaTime;
                if (reloadTime > savedReloadTime - reloadSoundStart && triggerReloadSound && reloadSound != null)
                {
                    reloadSound.Play();
                    triggerReloadSound = false;
                }
            }
            else
            {
                animControl.DisableShapeAnimation(indexOfShapeAnim);
            }
            if (useShotPoint2 && ChangeCamera.triggered)
            {
                switchedPoint = !switchedPoint;
            }
        }
        
    }
    void Spawn()
    {
        animControl.ActivateShapeAnimation(indexOfShapeAnim);
        shootSound.Play();
        var bullet = usePool ? pool.Get() : Instantiate(bulletPrefab);
        if (switchedPoint)
        {
            bullet.transform.position = shootPoint2.position + bulletOffset * shootPoint2.forward * 5f;
            bullet.transform.rotation = shootPoint.rotation;
            bullet.ShootDirection(shootPoint2.forward);
        }
        else
        {
            bullet.transform.position = shootPoint.position + bulletOffset * shootPoint.up;
            bullet.transform.rotation = shootPoint.rotation;
            bullet.ShootDirection(shootPoint.up);
        }
        
        bullet.Init(KillBullet);
    }
    private void KillBullet(Bullet bullet)
    {
        if (usePool)
            pool.Release(bullet);
        else
            Destroy(bullet.gameObject);
    }
    public void MakeAShot()
    {
        if (reloadTime >= savedReloadTime)
        {
            triggerReloadSound = true;
            reloadTime = 0f;
            particlesController.SetActive(false);
            particlesController.SetActive(true);
            Spawn();
        }
    }
    public void DisableScript()
    {
        isWorking = false;
    }
}
