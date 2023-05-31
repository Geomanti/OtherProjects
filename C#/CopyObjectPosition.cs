using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyObjectPosition : MonoBehaviour
{
    [SerializeField] Transform copyPositionObject;
    [SerializeField] Transform followingObject;
    [SerializeField] bool keepOffset = true;

    // Start is called before the first frame update
    Vector3 offset;
    void Start()
    {
        if (keepOffset)
            offset = copyPositionObject.InverseTransformPoint(followingObject.position);
        else
            offset = new Vector3(0f, 0f, 0f);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        followingObject.position = copyPositionObject.TransformPoint(offset);
    }
}
