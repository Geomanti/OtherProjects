using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideObjectsForCameras : MonoBehaviour
{


    [SerializeField] Camera[] cameras;
    [SerializeField] bool[] hide;
    [SerializeField] GameObject[] makeInvisObjects;
    [SerializeField] KeyCode ChangeCamera = KeyCode.LeftShift;
    
    int indexActive = 0;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(ChangeCamera))
        {
            indexActive++;
            if (indexActive == cameras.Length)
                indexActive = 0;

            for (int i = 0; i < makeInvisObjects.Length; i++)
            {
                bool active = true;
                if (hide[indexActive])
                    active = false;
                    
                if (makeInvisObjects[i].GetComponent<SkinnedMeshRenderer>() == null)
                    makeInvisObjects[i].GetComponent<MeshRenderer>().enabled = active;
                else
                    makeInvisObjects[i].GetComponent<SkinnedMeshRenderer>().enabled = active;
            }
        }
    }
}
