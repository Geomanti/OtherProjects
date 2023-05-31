using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModulesManager : MonoBehaviour
{
    [SerializeField] ModuleHealth[] modules = new ModuleHealth[0];
    [SerializeField] GameObject[] gameObjectsToChangeColor = new GameObject[0];
    [SerializeField] GameObject[] objectsToDisable;
    [SerializeField] AudioSource[] soundsToStop;
    [SerializeField] GameObject fireParticles;
    [SerializeField] GameObject explodePrefab;
    [SerializeField] Transform fireSpawnPoint;
    [SerializeField] Material DeadMaterial;
    [SerializeField] Material AliveMaterial;
    [SerializeField] GameObject PlayerToDeactivate;
    [SerializeField] bool DisableObject = false;
    [SerializeField] GameObject WholeObjectToDespawn;
    [SerializeField] float TimeToDespawnAfterDeath = 30f;
    [SerializeField] string deactivateMethod = "DisableScript";
    // Update is called once per frame
    private bool[] modulesWorkStatus;
    private bool isDead = false;
    private bool wasDead = false;
    private void Start()
    {
        modulesWorkStatus = new bool[modules.Length];
        for (int i = 0; i < modules.Length; i++)
        {
            modulesWorkStatus[i] = true;
        }
    }
    private void OnDisable()
    {
        for (int i = 0; i < modules.Length; i++)
        {
            modulesWorkStatus[i] = true;
        }
        isDead = false;
        wasDead = false;
        for (int i = 0; i < objectsToDisable.Length; i++)
        {
            objectsToDisable[i].SetActive(true);
        }
        for (int i = 0; i < gameObjectsToChangeColor.Length; i++)
        {
            gameObjectsToChangeColor[i].GetComponent<Renderer>().material = AliveMaterial;
        }
    }
    void Update()
    {
        int AliveModules = 0;
        if (!isDead)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                if (!modules[i].IsAlive())
                {
                    isDead = true;
                }
                else
                {
                    AliveModules++;
                }
            }
        }
        if (isDead && !wasDead)
        {
            for (int i = 0; i < gameObjectsToChangeColor.Length; i++)
            {
                gameObjectsToChangeColor[i].GetComponent<Renderer>().material = DeadMaterial;
            }
            GameObject fire = Instantiate(fireParticles);
            GameObject Explosion = Instantiate(explodePrefab);
            Explosion.transform.position = fireSpawnPoint.position;
            fire.transform.position = fireSpawnPoint.position;
            fire.transform.up = fireSpawnPoint.up;
            fire.transform.SetParent(fireSpawnPoint);
            PlayerToDeactivate.SendMessage(deactivateMethod);
            wasDead = true;
            for (int i = 0; i < objectsToDisable.Length; i++)
            {
                objectsToDisable[i].SetActive(false);
            }
            for (int i = 0; i < soundsToStop.Length; i++)
            {
                soundsToStop[i].Stop();
            }
            if (DisableObject)
                Invoke(nameof(Despawn), TimeToDespawnAfterDeath);
                
        }
        
    }
    public bool IsDead()
    {
        return isDead;
    }
    public void Despawn()
    {
        WholeObjectToDespawn.SetActive(false);
    }
}
