using UnityEngine;
using UnityEngine.SceneManagement;

// Pause butonuna basilinca oyunu durdurur ve "Devam Et" panelini acar. Tekrar
// basilinca / Devam Et'e basilinca oyunu surdurur.
// NOT: BILEREK ESC tusu yok - sadece TopRight_Panel'deki Pause butonuyla acilir.
public class PauseUI : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;

    bool _isPaused;

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    // Pause butonunun OnClick'ine baglanacak. Tik sesi artik GlobalButtonSfx
    // tarafindan otomatik calindigi icin burada ayrica calmiyoruz.
    public void TogglePause()
    {
        _isPaused = !_isPaused;
        if (pausePanel != null) pausePanel.SetActive(_isPaused);
        Time.timeScale = _isPaused ? 0f : 1f;
    }

    // "Devam Et" butonunun OnClick'ine baglanacak.
    public void Resume()
    {
        _isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // "Ana Menu" butonuna baglanir - sahneyi yeniden yukleyip Start ekranina
    // doner. Loading ekrani ATLANIR (SkipLoadingToMenu): oyuncu zaten oyunun
    // icindeydi, tekrar sahte bir "yukleniyor" ekraninda beklemesi gereksiz.
    public void RestartScene()
    {
        SaveRunProgress();
        Time.timeScale = 1f;
        GameFlow.SkipLoadingToMenu = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Oyuncu OLMEDEN menuye ciktiginda da ilerlemeyi kaydeder - kayit sistemi
    // bir "oturum kaydi" degil REKOR kaydi (sadece en yuksek dalga/seviye
    // tutulur, deger asla dusmez). Dalga 20'ye gercekten ulasildiysa, olerek
    // mi yoksa menuye cikarak mi birakildigi o basariyi degistirmez; kaydetmemek
    // "dalga 20'ye ciktim ama menude 15 yaziyor" gibi bug hissi veriyordu.
    // (Game Over yolunda ayni kayit zaten GameOverUI.ShowGameOver icinde yapiliyor.)
    void SaveRunProgress()
    {
        int level = XPSystem.Instance != null ? XPSystem.Instance.CurrentLevel : 1;
        int wave = EnemySpawner.Instance != null ? EnemySpawner.Instance.CurrentWave : 0;

        var hud = FindFirstObjectByType<HUDController>();
        int kills = hud != null ? hud.KillCount : 0;

        var inventory = FindFirstObjectByType<InventorySystem>();
        int diamonds = inventory != null ? inventory.Diamonds : 0;

        SaveSystem.SaveRunResult(wave, kills, diamonds);
        SaveSystem.SetBestPlayerLevel(level);
    }
}
