using UnityEngine;

// Basit PlayerPrefs tabanli kayit sistemi. Su ana kadar oyunda HICBIR kalici
// veri yoktu (her kapatista sifirlaniyordu) - bu, en yuksek dalga/oldurme/
// elmas gibi "high score" degerlerini kalici tutar. Ses ayarlari icin ayni
// mantik SettingsUI.cs icinde (PlayerPrefs "MasterVolume"/"MusicVolume").
public static class SaveSystem
{
    const string BestWaveKey     = "BestWave";
    const string BestKillsKey    = "BestKills";
    const string BestDiamondsKey = "BestDiamonds";
    const string TotalDiamondsKey = "TotalDiamonds";
    const string AchievementKeyPrefix = "Achv_";
    const string BestPlayerLevelKey = "BestPlayerLevel";
    const string HowToPlayShownKey = "HowToPlayShown";

    // "Nasil oynanir" ipucu paneli OMUR BOYU SADECE 1 KEZ gosterilir -
    // ilk gosterildikten sonra bu bayrak kalici olarak set edilir, bir
    // daha (yeni oyunda, build'de, farketmez) hic cikmaz.
    public static bool HowToPlayShown => PlayerPrefs.GetInt(HowToPlayShownKey, 0) == 1;
    public static void MarkHowToPlayShown()
    {
        PlayerPrefs.SetInt(HowToPlayShownKey, 1);
        PlayerPrefs.Save();
    }

    public static int BestWave     => PlayerPrefs.GetInt(BestWaveKey, 0);
    public static int BestKills    => PlayerPrefs.GetInt(BestKillsKey, 0);
    public static int BestDiamonds => PlayerPrefs.GetInt(BestDiamondsKey, 0);

    // BestWave'den BAGIMSIZ ayri bir ilerleme - dalga sayisi (dusman zorlugu)
    // ile oyuncu seviyesi (XP ile artan) AYNI HIZDA ilerlemiyor (biri iyi
    // savasip az XP alabilir, digeri az savasip cok XP alabilir). Onceden
    // "kaldigin yerden devam" level'i YANLISLIKLA dalga sayisindan turetiyordu
    // (dalga+1) - bu da "level 11'de oldum, 14'te basladi" gibi TUTARSIZ
    // sonuclara sebep oluyordu. Artik GERCEK oyuncu seviyesi ayrica kaydediliyor.
    public static int BestPlayerLevel => PlayerPrefs.GetInt(BestPlayerLevelKey, 1);
    public static void SetBestPlayerLevel(int level)
    {
        if (level > BestPlayerLevel)
        {
            PlayerPrefs.SetInt(BestPlayerLevelKey, level);
            PlayerPrefs.Save();
        }
    }

    // BestDiamonds'tan FARKLI - o "bir run'da en cok toplanan" rekoru,
    // bu ise TUM run'lar boyunca BIRIKEN, HARCANABILIR toplam elmas.
    // InventorySystem her degisiminde bunu gunceller - run bitmeden
    // (crash/kapatma) bile o ana kadar toplanan kaybolmaz.
    public static int TotalDiamonds => PlayerPrefs.GetInt(TotalDiamondsKey, 0);
    public static void SetTotalDiamonds(int amount)
    {
        PlayerPrefs.SetInt(TotalDiamondsKey, amount);
        PlayerPrefs.Save();
    }

    // Basarimlar - AchievementManager her acilan basarimi buraya kaydeder,
    // Awake'de de her basarimin daha once acilip acilmadigini buradan okur.
    public static bool IsAchievementUnlocked(string achievementName) =>
        PlayerPrefs.GetInt(AchievementKeyPrefix + achievementName, 0) == 1;

    public static void UnlockAchievement(string achievementName)
    {
        PlayerPrefs.SetInt(AchievementKeyPrefix + achievementName, 1);
        PlayerPrefs.Save();
    }

    // Bir run bitince cagrilir - herhangi bir deger yeni rekorsa true doner
    // (Game Over ekraninda "YENI REKOR!" gostermek icin kullanilabilir).
    public static bool SaveRunResult(int wave, int kills, int diamonds)
    {
        bool newRecord = false;

        if (wave > BestWave)         { PlayerPrefs.SetInt(BestWaveKey, wave);         newRecord = true; }
        if (kills > BestKills)       { PlayerPrefs.SetInt(BestKillsKey, kills);       newRecord = true; }
        if (diamonds > BestDiamonds) { PlayerPrefs.SetInt(BestDiamondsKey, diamonds); newRecord = true; }

        if (newRecord) PlayerPrefs.Save();
        return newRecord;
    }
}
