using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public GameObject bulletObject;
    public string[] recieveDamageTag;
    public string nameOfDamageFuntion = "ApplyDamage";
    public int damage = 500;
    public float moveSpeed = 200f;
    public float explosionStrength = 100f;
    public float explosionRadius = 5f;
    public float destroyTime = 10f;
    public int maxNumberOfHits = 25;
    public LayerMask objectsToHitLayer = default;
    public Rigidbody Rigidbody;
    public GameObject ParticleSystemOnImpact;
    public TrailRenderer TrailRenderer;

    private Action<Bullet> _killAction;
    private Collider[] expObjects;

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        expObjects = new Collider[maxNumberOfHits];
    }
    private void OnEnable()
    {
        Invoke(nameof(DestroyObject), destroyTime);
    }
    public void ShootDirection(Vector3 forwardVector)
    {
        TrailRenderer.Clear();
        Rigidbody.velocity = new Vector3(0f, 0f, 0f);
        Rigidbody.AddForce(moveSpeed * 10f * forwardVector);
    }

    private void OnCollisionEnter(Collision collision)
    {

        GameObject particles = Instantiate(ParticleSystemOnImpact);
        ContactPoint contact = collision.GetContact(0);
        particles.transform.position = contact.point;
        particles.transform.up = contact.normal;
        for (int i = 0; i < recieveDamageTag.Length; i++)
        {
            if (collision.gameObject.CompareTag(recieveDamageTag[i]))
                collision.gameObject.SendMessage(nameOfDamageFuntion, damage, SendMessageOptions.DontRequireReceiver);
        }
            Destroy(particles, 3f);
        int hits = Physics.OverlapSphereNonAlloc(contact.point, explosionRadius, expObjects, objectsToHitLayer);
        for (int i = 0; i < hits; i++)
        {
            if (expObjects[i].TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
            {
                rigidbody.AddExplosionForce(explosionStrength, contact.point, explosionRadius);
            }
        }
        _killAction(this);     
    }
    public void Init(Action<Bullet> KillAction)
    {
        _killAction = KillAction;
    }
    public void DestroyObject()
    {
        if (bulletObject.activeSelf)
            _killAction(this);
    }

}
