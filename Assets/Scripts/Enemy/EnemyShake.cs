using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShake : MonoBehaviour
{
   public void Shake()
    {
        StopAllCoroutines();
        StartCoroutine(DoShake());
    }

    IEnumerator DoShake()
    {
        Vector3 original = transform.localPosition;
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            transform.localPosition = original + Random.insideUnitSphere * 0.1f;
            // Time.deltaTime DEGIL - CameraShake'teki ayni bug: Time.timeScale=0
            // oldugunda (Level-Up ekrani gibi) bu hic ilerlemeyip dusmani
            // sonsuza kadar titretiyordu.
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        transform.localPosition = original;
    }
}
