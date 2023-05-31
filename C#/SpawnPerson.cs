using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class SpawnPerson : MonoBehaviour
{
    [SerializeField] GameObject personPrefab;
    [SerializeField] string[] tagsToRefreshTargets;
    [SerializeField] string methodToCall = "ResetTargets";
    [SerializeField] float timeToSpawn = 60f;
    [SerializeField] int maxPersons = 10;
    [SerializeField] Vector3 offsetToSpawn;
    public bool isActive = true;

    private ObjectPool<GameObject> pool;
    private float spawnTimer;
    private int amountSpawned = 0;
    private GameObject[] pooledObjects;
    void Start()
    {
        pooledObjects = new GameObject[maxPersons+1];
        transform.position += offsetToSpawn;
        spawnTimer = 0f;
        pool = new ObjectPool<GameObject>(() => {
            return Instantiate(personPrefab);
        }, person => {
            person.gameObject.SetActive(true);
        }, person => {
            person.gameObject.SetActive(false);
        }, person => {
            Destroy(person.gameObject);
        }, false, maxPersons, maxPersons+1);
    }

    void Update()
    {
        if (spawnTimer >= timeToSpawn && isActive && pool.CountActive < maxPersons)
        {
            spawnTimer = 0f;
            GameObject newPerson = pool.Get();
            newPerson.transform.position = transform.position;
            for (int i = 0; i < pooledObjects.Length; i++)
            {
                if (pooledObjects[i] == null)
                {
                    pooledObjects[i] = newPerson;
                    break;
                }
            }
            
            if (amountSpawned < maxPersons)
            {
                amountSpawned++;
                for (int f = 0; f < tagsToRefreshTargets.Length; f++)
                {
                    GameObject[] refreshTargets = GameObject.FindGameObjectsWithTag(tagsToRefreshTargets[f]);
                    for (int i = 0; i < refreshTargets.Length; i++)
                    {
                        refreshTargets[i].SendMessage(methodToCall, SendMessageOptions.DontRequireReceiver);
                    }
                }
            }           
            
        }
        else
        {
            spawnTimer += Time.deltaTime;
        }
        for (int i = 0; i < pooledObjects.Length; i++)
        {
            if (pooledObjects[i] != null)
            {
                if (!pooledObjects[i].activeSelf)
                {
                    amountSpawned--;
                    pool.Release(pooledObjects[i]);
                    pooledObjects[i] = null;
                }
            }          
        }
    }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
