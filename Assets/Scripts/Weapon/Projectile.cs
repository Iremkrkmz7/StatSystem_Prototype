using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour ,IPoolable
{
    bool _initialized;
    float _damage;
    float _speed;
    float _range;
    string _targetTag;
    string _poolKey;
    Vector3 _startPos;

    public void Initialize(float damage, float speed, float range, string targetTag, string poolKey = "Projectile")
    {
        _damage    = damage;
        _speed     = speed;
        _range     = range;
        _targetTag = targetTag;
        _poolKey   = poolKey;
        _startPos  = transform.position;
       _initialized = true;
        Debug.Log($"Initialize! Speed: {_speed}");

    }
    void Update()
    {
        if(!_initialized) return;
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
        // Proje ayarindaki Auto Sync Transforms neye ayarli olursa olsun,
        // trigger tespitinin bu frame'in guncel pozisyonunu gormesini garanti
        // etmek icin fizigi burada elle senkronize ediyoruz.
        Physics.SyncTransforms();

       if (Vector3.Distance(_startPos, transform.position) >= _range)
       ReturnToPool();

    }
    void OnTriggerEnter(Collider other)
    {
        if (!_initialized) return;
       if (!other.CompareTag(_targetTag)) return;
       other.GetComponent<SCharacterStats>()?.TakeDamage(_damage);

       PoolManager.Instance?.Get("Hiteffect",transform.position, Quaternion.identity);
        ReturnToPool();
    }
   void ReturnToPool()
{
    _initialized = false;
    PoolManager.Instance?.Return(_poolKey, gameObject);
}
public void OnSpawnFromPool()
{
    _initialized = false;
    _startPos = transform.position;
}
    public void OnReturnToPool()   { }

}
