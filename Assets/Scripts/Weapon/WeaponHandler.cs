using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class WeaponHandler : MonoBehaviour
{
    [SerializeField] WeaponSO    currentWeapon;
    [SerializeField] Transform   firePoint;
    [SerializeField] AudioSource audioSource;
    [Range(0f, 1f)]
    [SerializeField] float fireSoundVolume = 1f;

    SCharacterStats _stats;
    float          _nextFireTime;

   void Awake()
   {
    _stats = GetComponentInParent<SCharacterStats>();
    audioSource = GetComponent<AudioSource>();
   }
   public void TryShoot()
   {
    if(currentWeapon == null) return;

    float atkSpeed = _stats?.GetStat(StatType.AttackSpeed) ?? 1f;
    float interval = currentWeapon.FireRate / atkSpeed;

    if(Time.time < _nextFireTime) return;
    _nextFireTime = Time.time + interval;

    Shoot();
   }
   void Shoot()
   {
    float damage = currentWeapon.BaseDamage +(_stats?.GetStat(StatType.Damage) ?? 0f);

    for(int i = 0; i < currentWeapon.ProjectileCount; i++)
    {
        Quaternion rot = firePoint.rotation;
        if(currentWeapon.Spread > 0f)
        {
            float angle = Random.Range(-currentWeapon.Spread, currentWeapon.Spread);
            rot = Quaternion.Euler(0f, angle, 0f)* rot;
        }
        var obj = PoolManager.Instance?.Get(currentWeapon.ProjectilePoolKey, firePoint.position, rot);
        Debug.Log("Pool'dan gelen obje: " + obj);
        obj?.GetComponent<Projectile>()?.Initialize(damage, currentWeapon.ProjectileSpeed, currentWeapon.Range, "Enemy");
    }
    if(currentWeapon.FireSound && audioSource)
    Sfx.PlayOneShot(audioSource, currentWeapon.FireSound, fireSoundVolume);
    }
    public void EquipWeapon(WeaponSO weapon) => currentWeapon = weapon;
   }


