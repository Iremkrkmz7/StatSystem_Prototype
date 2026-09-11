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

    // Istersen "Ana Menu" butonuna baglarsin - sahneyi yeniden yukleyip
    // Loading/Start ekranina geri doner.
    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
