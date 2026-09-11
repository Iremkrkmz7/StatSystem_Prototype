using UnityEngine;
using TMPro;

// Oyun basladigi andan itibaren gecen sureyi tutar. Game Over ekraninda
// "ne kadar hayatta kaldin" bilgisini gostermek icin kullanilir. Ayrica
// istersek HUD'daki canli sure yazisini da buradan guncelleriz.
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Tooltip("HUD'da surekli guncellenen sure yazisi (opsiyonel)")]
    [SerializeField] TextMeshProUGUI liveTimeText;

    float _elapsed;
    bool _running = true;

    public float Elapsed => _elapsed;
    public string FormattedTime
    {
        get
        {
            int minutes = Mathf.FloorToInt(_elapsed / 60f);
            int seconds = Mathf.FloorToInt(_elapsed % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
    }

    void Awake() => Instance = this;

    void Update()
    {
        if (!_running) return;
        _elapsed += Time.deltaTime;
        if (liveTimeText != null) liveTimeText.text = FormattedTime;
    }

    public void Stop() => _running = false;
}
