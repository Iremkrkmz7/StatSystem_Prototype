# StatSystem_Prototype

Üstten görünümlü, dalga bazlı hayatta kalma (survivor-like) bir Unity oyunu. C# / Unity 2022.3, URP.

**Oynanabilir sürüm:** *(itch.io linkini buraya ekle)*

## Bu depo ne içeriyor

Bu depo **sadece kendi yazdığım kod ve veriyi** içerir — oyunun tam sürümünde kullanılan
ücretli/lisanslı Unity Asset Store paketleri (modeller, VFX, ikonlar, müzik vb.) **dahil
değildir** (kendi lisansları buna izin vermiyor). O yüzden bu repo'yu klonlayıp doğrudan
Unity'de açarsan sahne/prefab referansları eksik çıkar — amaç kodu incelemek, oynamak için
yukarıdaki linke bak.

```
Assets/
  Scripts/          - oyun mantığı (dalga spawn sistemi, XP/level, ability'ler,
                       save/load, kamera, UI, enemy AI, ...)
  Editor/           - BalanceImporter: Excel/CSV -> oyun verisi import aracı
  ScriptableObjects/- düşman/yetenek/görev/eşya tanımları (veri, kod değil ama kendi tasarımım)
  Resources/        - BalanceConfig.asset (merkezi denge ayarları)
  Material/         - birkaç küçük materyal
balance_global.csv, balance_enemies.csv, GameBalance.xlsx
                    - data-driven denge (balance) pipeline'ının kaynağı
```

## Öne çıkan sistemler

- **Dalga bazlı düşman spawn sistemi** — dalgayla ölçeklenen can/hasar/XP, ağırlıklı
  düşman seçimi, açısal (yığılma önleyici) spawn dağılımı, NavMesh self-healing
- **XP / level sistemi** + oyuncu istatistik büyümesi
- **Save/load** (PlayerPrefs) — en yüksek dalga, en yüksek seviye, biriken elmas,
  başarımlar; yeni bir oyun kaldığın yerden (dalga + seviye ayrı ayrı) devam eder
- **Data-driven denge (balance) pipeline'ı** — `BalanceConfig` ScriptableObject +
  Editor'de `Tools > Balance > Import from CSV` menüsü; tüm denge değerleri
  Excel/CSV'den yönetiliyor, kod değişikliği gerekmiyor
- **Sinematik giriş** (`PlayerIntroDrop`) — karakterin gökten düşüşü, kamera reveal +
  orbit sekansı
- Ability sistemi (Dash / Shield / AoE / Heal), quest/achievement sistemi, HUD

## Lisans

Kendi kod ve verim MIT lisanslı ([LICENSE](LICENSE)). 3. parti asset'ler bu depoya
dahil değil, kendi lisanslarına tabidir.
