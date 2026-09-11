using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoReturnToPool : MonoBehaviour, IPoolable
{
    
    [Tooltip("PoolManager'daki bu prefabın Key'i - Return() çağrılırken kullanılır")]
    [SerializeField] string poolKey = "HeadBurst";
 
    [Tooltip("Kaç saniye sonra pool'a geri dönsün. Particle System'in Duration + Start Lifetime toplamına yakın bir değer ver (örn. 1 + 0.5 = 1.5).")]
    [SerializeField] float lifetime = 1.5f;
 
    Coroutine _returnRoutine;
 
    public void OnSpawnFromPool()
    {
        if (_returnRoutine != null) StopCoroutine(_returnRoutine);
        _returnRoutine = StartCoroutine(ReturnAfterDelay());
    }
 
    IEnumerator ReturnAfterDelay()
    {
        yield return new WaitForSeconds(lifetime);
        PoolManager.Instance?.Return(poolKey, gameObject);
    }
 
    public void OnReturnToPool()
    {
        if (_returnRoutine != null)
        {
            StopCoroutine(_returnRoutine);
            _returnRoutine = null;
        }
    }
}