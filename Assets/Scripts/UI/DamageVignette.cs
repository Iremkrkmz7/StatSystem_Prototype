using UnityEngine;
using UnityEngine.UI;

// Kirmizi kenar vignette'i. Iki sebeple gorunur:
//  1) HASAR FLASI  - dusman vurdugu an kisa bir kirmizi, sonra sonup gider.
//  2) DUSUK CAN    - can 'lowHealthThreshold'un (varsayilan %50) altina
//                    inince kalici olarak durur, azaldikca koyulasir.
// Ikisinin buyugu gosterilir. Yani: darbe alinca yanar; dusman artik
// vurmuyorsa ve canin yarindan fazlaysa tamamen kaybolur.
public class DamageVignette : MonoBehaviour
{
    public static DamageVignette Instance;

    [SerializeField] Image vignetteImage;
    [SerializeField] SCharacterStats playerStats;

    [Header("Hasar Flasi")]
    [Tooltip("Darbe alinca vignette'in tam gorunurlukten sifira sonme suresi (sn)")]
    [SerializeField] float flashDuration = 0.5f;
    [Range(0f, 1f)] [SerializeField] float flashMaxAlpha = 0.7f;

    [Header("Dusuk Can")]
    [Tooltip("Can bu oranin ALTINA inince kalici vignette baslar (0.5 = %50)")]
    [Range(0f, 1f)] [SerializeField] float lowHealthThreshold = 0.5f;
    [Range(0f, 1f)] [SerializeField] float lowHealthMaxAlpha = 0.85f;

    float _flashTimer;
    float _prevHealth = -1f;

    void Awake()
    {
        Instance = this;
        if (playerStats != null) playerStats.OnHealthChanged += HandleHealthChanged;
    }

    void OnDestroy()
    {
        if (playerStats != null) playerStats.OnHealthChanged -= HandleHealthChanged;
    }

    void Start()
    {
        // Kirmizi radyal doku (kenarlarda yogun, merkezde saydam) - tek sefer.
        var tex = new Texture2D(256, 256);
        var center = new Vector2(0.5f, 0.5f);
        for (int y = 0; y < 256; y++)
        for (int x = 0; x < 256; x++)
        {
            float dx = (x / 255f) - center.x;
            float dy = (y / 255f) - center.y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
            float a = Mathf.Clamp01(dist - 0.3f);
            tex.SetPixel(x, y, new Color(1f, 0f, 0f, a));
        }
        tex.Apply();

        vignetteImage.sprite = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        vignetteImage.color = new Color(1f, 1f, 1f, 0f); // beyaz tint, saydam basla

        if (playerStats != null) _prevHealth = playerStats.CurrentHealth;
    }

    void HandleHealthChanged(float current, float max)
    {
        // Sadece AZALDIYSA (hasar) flasi tetikle - heal/regen tetiklemesin.
        if (_prevHealth >= 0f && current < _prevHealth - 0.01f)
            _flashTimer = flashDuration;
        _prevHealth = current;
    }

    void Update()
    {
        if (playerStats == null) return;

        // 1) hasar flasi (zamanla soner)
        if (_flashTimer > 0f) _flashTimer -= Time.deltaTime;
        float flashAlpha = flashDuration > 0f
            ? Mathf.Clamp01(_flashTimer / flashDuration) * flashMaxAlpha
            : 0f;

        // 2) dusuk can (kalici, sadece esigin altinda)
        float hp = playerStats.MaxHealth > 0f ? playerStats.CurrentHealth / playerStats.MaxHealth : 1f;
        float lowAlpha = hp < lowHealthThreshold
            ? Mathf.InverseLerp(lowHealthThreshold, 0f, hp) * lowHealthMaxAlpha
            : 0f;

        SetAlpha(Mathf.Max(flashAlpha, lowAlpha));
    }

    void SetAlpha(float a)
    {
        var c = vignetteImage.color;
        if (!Mathf.Approximately(c.a, a))
        {
            c.a = a;
            vignetteImage.color = c;
        }
    }
}
