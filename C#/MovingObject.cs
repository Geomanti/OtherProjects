using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingObject : MonoBehaviour
{
    [SerializeField] float amplitude = 5f;
    [SerializeField] float speed = 5f;

    private Vector3 _startPos;
    private void Awake()
    {
        _startPos = transform.position;
    }
    void Update()
    {
        Vector3 position = transform.up * Mathf.Sin(Time.time * Mathf.PI * speed) * amplitude;
        transform.position = _startPos + position;
    }
}
