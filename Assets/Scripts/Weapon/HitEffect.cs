using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffect: MonoBehaviour
{
    [SerializeField] float destroyTime = 0.3f;
    
    void OnEnable()
    {
        Invoke(nameof(ReturnToPool), destroyTime);
    }
    void ReturnToPool()
    {
        PoolManager.Instance?.Return("HitEffect" , gameObject);
    }
}
