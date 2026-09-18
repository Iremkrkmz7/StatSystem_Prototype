using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Player oldugunde tetiklenir - oyunu durdurur (Time.timeScale = 0) ve final
// istatistikleri (level, elmas, kill, sure) gosteren paneli acar.
public class GameOverUI : MonoBehaviour
{
    [Tooltip("Panel acilmadan once olum animasyonunun oynamasi icin beklenecek sure (saniye)")]
    [SerializeField] float deathEffectDelay = 1.3f;
    [SerializeField] GameObject panelRoot;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] TextMeshProUGUI coinsText;
    [SerializeField] TextMeshProUGUI killsText;
    [SerializeField] TextMeshProUGUI timeText;
    [Tooltip("Bu run'da herhangi bir rekor kirildiysa gosterilir (bos birakilabilir)")]
    [SerializeField] GameObject newRecordBadge;

    [Header("Olum Efekti")]
    [SerializeField] Image deathFlashImage;
    [SerializeField] float shakeDuration = 0.4f;
    [SerializeField] float shakeMagnitude = 0.4f;
    [SerializeField] float hitStopDuration = 0.3f;
    [SerializeField] float hitStopScale = 0.08f;
    [SerializeField] float flashMaxAlpha = 0.55f;
    [SerializeField] float flashInTime = 0.15f;

    [Header("Ses")]
    [SerializeField] AudioClip gameOverSound;
    [SerializeField] AudioClip buttonClickSound;

    HUDController _hud;
    SCharacterStats _playerStats;

    void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        _hud = FindFirstObjectByType<HUDController>();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerStats = player.GetComponent<SCharacterStats>();
            if (_playerStats != null) _playerStats.OnDied += ShowGameOver;
        }
    }

    void OnDestroy()
    {
        if (_playerStats != null) _playerStats.OnDied -= ShowGameOver;
    }

    void ShowGameOver()
    {
        // Istatistikleri (sure dahil) hemen olum aninda sabitliyoruz, ama
        // paneli/duraklatmayi geciktiriyoruz - yoksa Time.timeScale=0 aninda
        // uygulanip olum animasyonu hic oynamadan donuyordu (ekran titremesi
        // de HitStop/CameraShake ile bu ani duraklamanin cakismasindandi).
        GameTimer.Instance?.Stop();

        int level = XPSystem.Instance != null ? XPSystem.Instance.CurrentLevel : 1;
        int diamonds = 0;
        var inventory = FindFirstObjectByType<InventorySystem>();
        if (inventory != null) diamonds = inventory.Diamonds;
        int kills = _hud != null ? _hud.KillCount : 0;
        string time = GameTimer.Instance != null ? GameTimer.Instance.FormattedTime : "00:00";

        if (levelText != null) levelText.text = "Level: " + level;
        if (coinsText != null) coinsText.text = "Diamonds: " + diamonds;
        if (killsText != null) killsText.text = "Kills: " + kills;
        if (timeText != null) timeText.text = "Time: " + time;

        // Yuksek skor kaydi (dalga/kill/elmas) - PlayerPrefs uzerinden kalici.
        int wave = EnemySpawner.Instance != null ? EnemySpawner.Instance.CurrentWave : 0;
        bool newRecord = SaveSystem.SaveRunResult(wave, kills, diamonds);
        if (newRecordBadge != null) newRecordBadge.SetActive(newRecord);

        // Oyuncu SEVIYESI dalga sayisindan BAGIMSIZ ayrica kaydediliyor -
        // "kaldigin yerden devam" bir sonraki run'da GERCEK seviyeni kullansin.
        SaveSystem.SetBestPlayerLevel(level);

        // Vurusun/olumun agirligini hissettirmek icin sarsinti + hit-stop +
        // kirmizi ekran flasi - enemy olum efektlerindeki gibi "game feel".
        CameraShake.Instance?.Shake(shakeDuration, shakeMagnitude);
        HitStop.Instance?.Trigger(hitStopDuration, hitStopScale);
        if (deathFlashImage != null) StartCoroutine(DeathFlash());

        StartCoroutine(ShowPanelAfterDelay());
    }

    IEnumerator ShowPanelAfterDelay()
    {
        // Realtime - olum aninda AoE ile baska dusmanlar da olup HitStop'u
        // ust uste tetiklerse Time.timeScale uzun sure dusuk kalabiliyordu,
        // bu da SCALED WaitForSeconds'in gercek zamanda cok uzamasina/Game
        // Over ekraninin hic gelmiyormus gibi hissettirmesine sebep oluyordu.
        yield return new WaitForSecondsRealtime(deathEffectDelay);

        if (panelRoot != null) panelRoot.SetActive(true);
        if (Camera.main != null) Sfx.PlayAt(gameOverSound, Camera.main.transform.position);

        // Oyunu tamamen duraklat - dusmanlar, animasyonlar, spawn hepsi durur.
        Time.timeScale = 0f;
    }

    IEnumerator DeathFlash()
    {
        Color c = deathFlashImage.color;

        // Unscaled - CameraShake/EnemyShake'teki ayni bug: HitStop timeScale'i
        // dusuk tuttugu surece Time.deltaTime de kucuk kalip flash yarim
        // solmus halde uzun sure takili gorunebiliyordu.
        float t = 0f;
        while (t < flashInTime)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(0f, flashMaxAlpha, t / flashInTime);
            deathFlashImage.color = c;
            yield return null;
        }

        float fadeOutTime = Mathf.Max(0.1f, deathEffectDelay - flashInTime);
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(flashMaxAlpha, 0f, t / fadeOutTime);
            deathFlashImage.color = c;
            yield return null;
        }
        c.a = 0f;
        deathFlashImage.color = c;
    }

    // RESTART butonuna baglanacak - sahneyi yeniden yukler, Loading/Start Menu
    // ekranlarini atlayip dogrudan oyuna doner (gercek "yeniden basla").
    public void Restart()
    {
        // Tik sesi artik GlobalButtonSfx tarafindan otomatik calindigi icin
        // burada ayrica caldirmiyoruz (iki kez ustuste calmasin diye).
        Time.timeScale = 1f;
        GameFlow.SkipToGameplay = true;
        GameFlow.IsRestart = true; // PlayerIntroDrop bunu okuyup gokten dusme sinematigini bu sefer ATLAYACAK
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // MAIN MENU butonuna baglanacak - sahneyi yeniden yukleyip Start ekranini
    // acar (RESTART'tan farkli olarak SkipToGameplay set edilmez, yani oyuna
    // degil menuye doner). Loading ekrani ATLANIR - oyuncu zaten oyunun
    // icindeydi, tekrar sahte bir "yukleniyor" beklemesi gereksiz.
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        GameFlow.SkipLoadingToMenu = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
