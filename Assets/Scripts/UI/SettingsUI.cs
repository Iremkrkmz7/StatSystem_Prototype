using UnityEngine;
using UnityEngine.UI;

// Ayarlar paneli - Master (efekt) ve Muzik ses seviyesini yonetir, PlayerPrefs
// ile kalici tutar. Hem Start Menu'deki Settings butonundan hem de oyun ici
// (TopRight_Panel) Settings butonundan acilabilir - o yuzden her zaman aktif
// olan kok Canvas'ta (Start/Loading canvas'i) duruyor.
public class SettingsUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;

    [Header("Muzik kaynaklari (kendi tasarim seviyeleri, slider bunu carpar)")]
    [SerializeField] AudioSource menuMusic;
    [SerializeField] AudioSource ambientAudioSource;
    [SerializeField] float menuMusicBaseVolume = 0.6f;
    [SerializeField] float ambientBaseVolume = 0.4f;

    const string MasterKey = "MasterVolume";
    const string MusicKey = "MusicVolume";

    float _musicVolume = 1f;

    void Awake()
    {
        float master = PlayerPrefs.GetFloat(MasterKey, 1f);
        _musicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);

        // AudioListener.volume KULLANMIYORUZ - o TUM sesi (muzik/ambiyans
        // dahil) kisiyordu, "Effect Sound" slider'i ambiyansi da kisiyor gibi
        // hissettiriyordu. Sfx.Volume SADECE Sfx.PlayAt/PlayOneShot uzerinden
        // gecen ses efektlerini etkiler, muzik/ambiyans kendi slider'indan
        // (ApplyMusicVolume) tamamen bagimsizdir.
        Sfx.Volume = master;
        ApplyMusicVolume();

        if (masterSlider != null) masterSlider.SetValueWithoutNotify(master);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(_musicVolume);
    }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Tik sesi artik GlobalButtonSfx tarafindan otomatik calindigi icin
    // burada ayrica calmiyoruz.
    public void Open()
    {
        if (panel != null) panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    // Master (efekt) slider'inin OnValueChanged'ine baglanacak.
    public void OnMasterVolumeChanged(float value)
    {
        Sfx.Volume = value;
        PlayerPrefs.SetFloat(MasterKey, value);
    }

    // Muzik slider'inin OnValueChanged'ine baglanacak.
    public void OnMusicVolumeChanged(float value)
    {
        _musicVolume = value;
        ApplyMusicVolume();
        PlayerPrefs.SetFloat(MusicKey, value);
    }

    void ApplyMusicVolume()
    {
        if (menuMusic != null) menuMusic.volume = menuMusicBaseVolume * _musicVolume;
        if (ambientAudioSource != null) ambientAudioSource.volume = ambientBaseVolume * _musicVolume;
    }
}
