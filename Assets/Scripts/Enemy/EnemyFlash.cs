using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFlash : MonoBehaviour
{
     [SerializeField] float flashDuration = 0.1f;

    Renderer   _renderer;
    Color      _originalColor;

    void Awake()
    {
        _renderer      = GetComponentInChildren<Renderer>();
        _originalColor = _renderer.material.color;

        var stats = GetComponent<SCharacterStats>();
        if (stats != null)
            stats.OnHealthChanged += OnHit;
    }

    void OnHit(float current, float max)
    {
        StopAllCoroutines();
        StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        _renderer.material.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        _renderer.material.color = _originalColor;
    }
}
