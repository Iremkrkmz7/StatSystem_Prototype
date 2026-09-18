// Sahne yeniden yuklenirken (SceneManager.LoadScene) Loading/Start Menu
// ekranlarini atlayip dogrudan oyuna donulmesi gerekip gerekmedigini tasir.
// Static oldugu icin ayni Play oturumu icinde bir LoadScene cagrisini
// atlatir (GameOverUI.Restart() bunu true yapar, LoadingUI bunu okuyup
// menuleri atlar; PauseUI.RestartScene() ve "Main Menu" butonlari bunu hic
// dokunmadigi icin normal Loading->StartMenu akisina girer).
public static class GameFlow
{
    public static bool SkipToGameplay;

    // SkipToGameplay'den FARKLI - o hem "gercek restart" (GameOverUI) hem de
    // Editor'deki "skipMenusInEditor" test kolayligi (LoadingUI) tarafindan
    // set ediliyor, o yuzden tek basina "bu bir restart mi" ayrimini
    // yapamiyor. Bu bayrak SADECE GameOverUI.Restart()'ta true olur -
    // PlayerIntroDrop, gokten dusme sinematiginin SADECE gercek "Start"
    // ile (Editor test-skip dahil ilk giriste) oynayip RESTART'ta
    // TEKRARLANMAMASI icin bunu okuyup hemen sifirlar.
    public static bool IsRestart;

    // "Main Menu" butonu icin - sahne yeniden yuklenirken Loading ekranini
    // (sahte ilerleme cubugu + ipucu, ~2sn) ATLAYIP dogrudan Start Menu'yu
    // acar. Oyuncu zaten oyunun icindeydi, tekrar "yukleniyor" gostermek
    // gereksiz bir bekleme.
    public static bool SkipLoadingToMenu;
}
