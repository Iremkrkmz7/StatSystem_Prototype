using System.Collections;
using UnityEngine;


public class HitStop : MonoBehaviour
{
    public static HitStop Instance;
    Coroutine _routine;
    float _resumeAtUnscaledTime;
    float _appliedScale = 1f;

    void Awake()
    {
        Instance = this;
        // Onceki bir Play oturumundan/hatali durumdan kalma dusuk timeScale
        // yuzunden oyun "hareket etmiyor gibi" baslamasin diye guvenlik sifirlamasi.
        Time.timeScale = 1f;
    }

    // Bu obje herhangi bir sebeple devre disi kalirsa (sahne gecisi, vs.)
    // coroutine yarim kalip timeScale'i dusuk halde birakmasin.
    void OnDisable() => Time.timeScale = 1f;

    public void Trigger(float duration = 0.05f, float scale = 0.05f)
    {
        _resumeAtUnscaledTime = Mathf.Max(_resumeAtUnscaledTime, Time.unscaledTime + duration);
        _appliedScale = scale;
        Time.timeScale = scale;

        if (_routine == null)
            _routine = StartCoroutine(DoStop());
    }

    IEnumerator DoStop()
    {
        while (Time.unscaledTime < _resumeAtUnscaledTime)
            yield return null;

        if (Mathf.Approximately(Time.timeScale, _appliedScale))
            Time.timeScale = 1f;

        _routine = null;
    }
}
