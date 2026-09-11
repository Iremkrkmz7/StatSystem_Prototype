using UnityEngine;
using TMPro;

// Oyun acilinca Start ekranini gosterir, oyunu duraklatir (Time.timeScale=0).
// START butonuna basilinca HUD/oyun objelerini (UI GameObject'i) acar, Start
// panelini kapatir ve oyunu devam ettirir.
public class StartMenuUI : MonoBehaviour
{
    [SerializeField] GameObject startMenuPanel;
    [SerializeField] GameObject uiRoot; // "UI" GameObject - HUD/gameplay UI'in tamamini iceriyor
    [SerializeField] AudioSource menuMusic;
    [SerializeField] AudioSource ambientAudioSource;
    [SerializeField] SettingsUI settingsUI;
    [Tooltip("PlayerPrefs'teki en yuksek dalgayi gosterir (bos birakilabilir)")]
    [SerializeField] TextMeshProUGUI bestWaveText;
    [Tooltip("Kalici (run'lar arasi biriken) toplam elmas sayisini gosterir (bos birakilabilir)")]
    [SerializeField] TextMeshProUGUI diamondsText;

    void Start()
    {
        // Panelin kendisini burada acmiyoruz - LoadingUI, sahte yukleme
        // bitince acacak. Ama HUD'u ve zamani en bastan durduruyoruz.
        Time.timeScale = 0f;
        if (uiRoot != null) uiRoot.SetActive(false);

        if (bestWaveText != null)
            bestWaveText.text = SaveSystem.BestWave > 0 ? "Best Wave: " + SaveSystem.BestWave : "";

        if (diamondsText != null)
            diamondsText.text = SaveSystem.TotalDiamonds.ToString();
    }

    // StartButton'un OnClick'ine baglanacak.
    // Tik sesi artik GlobalButtonSfx tarafindan otomatik calindigi icin
    // burada ayrica calmiyoruz.
    public void StartGame()
    {
        SkipStraightToGame();
    }

    // GameOverUI.Restart() "Loading/Start Menu ekranlarini atlayip dogrudan
    // oyuna don" istedigi zaman LoadingUI buradan cagirir - StartGame() ile
    // ayni efekti (muzik degisimi, HUD'u acma, timeScale) tek yerden uygular.
    public void SkipStraightToGame()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (uiRoot != null) uiRoot.SetActive(true);
        if (menuMusic != null) menuMusic.Stop();
        if (ambientAudioSource != null) ambientAudioSource.Play();
        Time.timeScale = 1f;
    }

    // Settings butonunun OnClick'ine baglanacak - simdilik bos, ayarlar
    // paneli eklendiginde burasi genisletilecek.
    public void OpenSettings()
    {
        settingsUI?.Open();
    }
}
