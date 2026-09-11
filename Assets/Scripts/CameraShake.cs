using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
     public static CameraShake Instance;

    // Sarsinti baslamadan HEMEN once kaydedilen "gercek" konum - StopImmediate
    
    Vector3 _restPosition;
    bool _shaking;

    void Awake() => Instance = this;

    public void Shake(float duration = 0.2f, float magnitude = 0.3f)
    {
        if (!_shaking) _restPosition = transform.localPosition;
        StopAllCoroutines();
        StartCoroutine(DoShake(duration, magnitude));
    }

 
    // acilirken cagirilir, cakisan bir sarsinti kalintisi kalmasin diye.
    public void StopImmediate()
    {
        if (_shaking) transform.localPosition = _restPosition;
        StopAllCoroutines();
        _shaking = false;
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        _shaking = true;
        Vector3 original = _restPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // BUG: elapsed hic artmiyordu -> while(0 < duration) SONSUZ DONGU,
            // sarsinti hicbir zaman bitmiyordu. Bir dusman vurunca kamera
            // SONSUZA KADAR titriyordu ("kamera buga giriyor"). unscaled:
            // HitStop timeScale'i dusurse bile sarsinti gercek zamanda bitsin.
            elapsed += Time.unscaledDeltaTime;

            // Sona dogru sonumlensin (sert kesme yerine yumusak biterlik).
            float damp = 1f - Mathf.Clamp01(elapsed / duration);
            float x = Random.Range(-1f, 1f) * magnitude * damp;
            float y = Random.Range(-1f, 1f) * magnitude * damp;
            transform.localPosition = new Vector3(original.x + x, original.y + y, original.z);

            yield return null;
        }
        transform.localPosition = original;
        _shaking = false;
    }
}
