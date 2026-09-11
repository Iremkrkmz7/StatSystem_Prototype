using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Oyun acilinca once bu ekran gorunur - gercek bir asset yuklemesi olmadigi
// icin sahte/kozmetik bir ilerleme cubugu dolduruyoruz, rastgele bir ipucu
// gosteriyoruz, bitince Start ekranina geciyoruz.
public class LoadingUI : MonoBehaviour
{
    [SerializeField] GameObject loadingPanel;
    [SerializeField] GameObject startMenuPanel;
    [SerializeField] GameObject uiRoot; // "UI" GameObject - HUD/gameplay UI'in tamami. Oyun daha loading ekranindayken bile
                                         // Time.timeScale burada 0'lanmazsa oyuncu START'a basmadan ates edebiliyordu.
    [SerializeField] StartMenuUI startMenuUI; // GameFlow.SkipToGameplay ile dogrudan oyuna donulurken kullanilir
    [SerializeField] Slider progressSlider;
    [SerializeField] TextMeshProUGUI percentText;
    [SerializeField] TextMeshProUGUI loadingLabelText;
    [SerializeField] TextMeshProUGUI gameText;
    [SerializeField] float loadDuration = 2f;

    [Header("Editor Test Kolayligi")]
    [Tooltip("SADECE Unity Editor icinde gecerli: Play'e basildigi an Loading/Start Menu ekranlarini atlayip DOGRUDAN oyuna girer (RESTART butonuyla ayni yol) - her testte Start ekranina gidip START'a basma zahmetini ortadan kaldirir. Build'de bu ayarin hicbir etkisi yoktur. Gercek Start Menu akisini test etmek istedigin zaman kapat.")]
    [SerializeField] bool skipMenusInEditor = false;

    [TextArea]
    [SerializeField] string[] tips = new string[]
    {
        "As enemy waves increase, more powerful spells are unlocked. Try spell combinations!",
        "Complete tasks by collecting diamonds and earn rewards.",
        "When your health decreases, you will be warned with a red flame - be careful!",
        "When defeating large and difficult enemies, they cause damage to the environment; keep your distance.",
        "As you level up, your damage and health increase."
    };

    void Start()
    {
#if UNITY_EDITOR
        // Test ederken her defasinda Loading -> Start Menu -> START tiklamasi
        // yapmak zahmetli oluyordu - Editor'de Play'e basar basmaz dogrudan
        // oyuna giriyoruz. SADECE Editor'de calisir, gercek build'i etkilemez.
        if (skipMenusInEditor) GameFlow.SkipToGameplay = true;
#endif

        // "RESTART" (oyunu bastan basla) butonuna basildiysa Loading/Start Menu
        // ekranlarini hic gostermeden dogrudan oyuna don - "MAIN MENU" butonu
        // bu bayragi hic set etmedigi icin o zaman normal akisa (Loading->Start
        // Menu) giriyoruz.
        if (GameFlow.SkipToGameplay)
        {
            GameFlow.SkipToGameplay = false;
            if (loadingPanel != null) loadingPanel.SetActive(false);
            startMenuUI?.SkipStraightToGame();
            return;
        }

        // Oyun daha ilk frame'den (loading ekranindan) itibaren duraklatilmis olsun -
        // START'a basilana kadar oyuncu hareket edemesin/ates edemesin.
        Time.timeScale = 0f;
        if (uiRoot != null) uiRoot.SetActive(false);

        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (progressSlider != null) progressSlider.value = 0f;
        if (loadingLabelText != null) loadingLabelText.text = "LOADING...";
        if (gameText != null && tips.Length > 0)
            gameText.text = tips[Random.Range(0, tips.Length)];

        StartCoroutine(FakeLoad());
    }

    IEnumerator FakeLoad()
    {
        float t = 0f;
        while (t < loadDuration)
        {
            // Play'e basildiktan sonraki ilk frame'de Time.unscaledDeltaTime
            // anormal buyuk gelebiliyor (Editor baslatma gecikmesi) - bu da
            // cubugun tek frame'de dolmasina sebep oluyordu. Ust siniri koyuyoruz.
            t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float p = Mathf.Clamp01(t / loadDuration);
            if (progressSlider != null) progressSlider.value = p;
            if (percentText != null) percentText.text = Mathf.RoundToInt(p * 100f) + "%";
            yield return null;
        }

        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
    }
}
