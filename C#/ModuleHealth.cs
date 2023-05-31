using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ModuleHealth : MonoBehaviour
{
    [SerializeField] int health;
    [SerializeField] int armor;
    [SerializeField] Slider healthBar;
    [SerializeField] AudioSource getHitSound;

    private int startHealth;
    public void Awake()
    {
        startHealth = health;
        if (healthBar != null)
        {
            healthBar.maxValue = health;
            healthBar.value = health;
        }
        
    }
    public void ApplyDamage(int damage)
    {
        health -= Mathf.Clamp(damage - armor, 1, damage);
        if (healthBar != null)
        {
            healthBar.value = health;
        }
        if (getHitSound != null)
        {
            getHitSound.Play();
        }
    }

    public bool IsAlive()
    {
        return health > 0;
    }

    public void OnDisable()
    {
        health = startHealth;
    }
}
