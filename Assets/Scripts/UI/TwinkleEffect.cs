using UnityEngine;
using UnityEngine.UI;

// Start ekrani arka planindaki sabit gorsele "hayat" katmak icin kullanilir -
// yildizlar, kristaller, mesaleler gibi noktalarin ustune konan kucuk glow
// objelerine eklenir. Alpha ve scale'i yumusakca (ya da titrek/flicker
// tarzda) degistirerek parildama/nefes alma hissi verir.
public class TwinkleEffect : MonoBehaviour
{
    public enum Mode { SmoothPulse, Flicker }

    [SerializeField] Mode mode = Mode.SmoothPulse;
    [SerializeField] float minAlpha = 0.3f;
    [SerializeField] float maxAlpha = 1f;
    [SerializeField] float minScale = 0.85f;
    [SerializeField] float maxScale = 1.15f;
    [SerializeField] float speed = 1f;
    [Tooltip("Ayni sahnede cok sayida ayni ritimde titremesin diye 0-1 arasi rastgele faz")]
    [SerializeField] bool randomizePhase = true;

    Image _image;
    Vector3 _baseScale;
    float _phase;
    float _seed;

    void Awake()
    {
        _image = GetComponent<Image>();
        _baseScale = transform.localScale;
        _phase = randomizePhase ? Random.Range(0f, 100f) : 0f;
        _seed = Random.Range(0f, 1000f);
    }

    void Update()
    {
        float t;
        if (mode == Mode.SmoothPulse)
        {
            t = (Mathf.Sin((Time.unscaledTime + _phase) * speed) + 1f) * 0.5f;
        }
        else
        {
            // Flicker: duzensiz, ani degisimler - alev hissi icin Perlin noise kullaniyoruz.
            t = Mathf.PerlinNoise(_seed, Time.unscaledTime * speed * 3f);
        }

        float a = Mathf.Lerp(minAlpha, maxAlpha, t);
        float s = Mathf.Lerp(minScale, maxScale, t);

        if (_image != null)
        {
            Color c = _image.color;
            c.a = a;
            _image.color = c;
        }
        transform.localScale = _baseScale * s;
    }
}
