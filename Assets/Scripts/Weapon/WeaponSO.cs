using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Weapon" , menuName = "Weapon/Weapon Data")]
public class WeaponSO : ScriptableObject
{
    [Header("Kimlik")]
    public string WeaponName;
    public Sprite Icon;

    [Header("Ateş Parametreleri")]
    public float  BaseDamage      = 15f;
    public float  FireRate        = 0.4f;
    public float  ProjectileSpeed = 25f;
    public int    ProjectileCount = 1;
    public float  Spread          = 0f;
    public float  Range           = 40f;

    [Header("Referanslar")]
    public string    ProjectilePoolKey = "Projectile";
    public AudioClip FireSound;
}

