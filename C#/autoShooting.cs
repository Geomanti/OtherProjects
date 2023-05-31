using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class autoShooting : MonoBehaviour
{
    [SerializeField] Transform shootPoint;
    [SerializeField] GameObject particlePrefab;
    [SerializeField] TurretTargetLock targetLock;
    [SerializeField] ShapeKeyController shapeAnim;
    [SerializeField] AudioSource shootSound;
    [SerializeField] float reloadTime = 1f;
    [SerializeField] int shapeAnimationIndex;
    [SerializeField] string applyDamageFunction;
    [SerializeField] int damageAmount = 10;

    // Start is called before the first frame update
    private float savedReloadTime = 0f;
    private ObjectPool<GameObject> pool;
    private List<GameObject> shootList = new List<GameObject>();
    private bool isWorking = true;
    void Start()
    {
        pool = new ObjectPool<GameObject>(() => {
            return Instantiate(particlePrefab);
        }, particle => {
            particle.SetActive(true);
        }, particle => {
            particle.SetActive(false);
        }, particle => {
            Destroy(particle);
        }, false, 10, 20);

        savedReloadTime = reloadTime;
    }
    void Update()
    {
        if (isWorking)
        {
            if (targetLock.IsAbleToShoot())
            {
                if (savedReloadTime >= reloadTime)
                {
                    shapeAnim.ActivateShapeAnimation(shapeAnimationIndex);
                    Spawn();
                    savedReloadTime = 0f;
                    targetLock.getTargetObject().SendMessage(applyDamageFunction, damageAmount, SendMessageOptions.DontRequireReceiver);
                }
            }
            if (savedReloadTime < reloadTime)
            {
                savedReloadTime += Time.deltaTime;
            }
            else
            {
                shapeAnim.DisableShapeAnimation(shapeAnimationIndex);
            }
        }
        RemoveParticles();
    }
    void Spawn()
    {
        var particle = pool.Get();
        shootSound.Play();
        particle.transform.position = shootPoint.position;
        particle.transform.forward = shootPoint.up;
        shootList.Add(particle);
    }
    void RemoveParticles()
    {
        for (int i = 0; i < shootList.Count; i++)
        {
            if (!shootList[i].activeSelf)
            {
                pool.Release(shootList[i]);
                shootList.RemoveAt(i);
            }
        }
    }
    public void DisableScript()
    {
        isWorking = false;
    }
    public void OnDisable()
    {
        isWorking = true;
    }
}
