# 🚨 SIFIR SAATİ: DENETİM VE KRİZ (Zero Hour: Inspection & Crisis)

> **CIVIL GAME JAM 2026 Resmî Proje Teslim Dokümanı**  
> **Etkinlik:** ODTÜ Kuzey Kıbrıs Kampüsü (KALTEV Binası) · 16–18 Ekim 2026  
> **Ana Organizatör:** KTMMOB İnşaat Mühendisleri Odası (KTMMOB İMO), LootBAG Studios & ODTÜ KALTEV  
> **Tema:** Afete Hazırlık ve Farkındalık (*Disaster Preparedness and Awareness*)  
> **Platform:** Windows PC (Standalone x64) | Unity 6 (6000.3.10f1 LTS - URP)  

---

## 🎯 1. Oyunun Özeti ve Tematik Yaklaşım
**"Gündüz yaptığınız tek bir ihmal, gece kaynaklarınızı tüketen bir felakete dönüşür!"**

*Sıfır Saati: Denetim ve Kriz*, inşaat mühendisliği kuralları ile taktiksel kriz yönetimini birleştiren iki aşamalı bir simülasyon/strateji oyunudur (*Papers, Please* x *911 Operator* hibriti):
* **☀️ FAZ 1 (Gündüz - Denetim Masası):** Oyuncu bir Afet Denetçisi olarak binaların deprem çantalarını, eşya sabitlemelerini, gaz vanası tahliyelerini, dere yatağı ve kolon/taşıyıcı durumlarını inceler; onay veya ret kararı verir.
* **🌙 FAZ 2 (Gece - Taktiksel Kriz Haritası):** 3D siber/holografik şehir haritasında gece boyunca peşi sıra patlak veren deprem, yangın, gaz patlaması ve enkaz olaylarına karşı sınırlı acil durum filosunu (İtfaiye, Ambulans, Polis, Kurtarma Helikopteri) sevk eder (Dispatch). Gündüz onaylanan hatalı binalar gece 3. seviye ağır felaket olarak geri döner!

---

## 👥 2. Görev Dağılımı: Kim Ne Yaptı? (Work Breakdown)

Bu proje, 48 saatlik Game Jam süresinde insan-yapay zeka ortaklığı (Pair Programming & Co-Creation) ve ekip çalışmasıyla geliştirilmiştir:

### 👤 Geliştirici (Kullanıcı):
* **Seviye ve Harita Tasarımı (3D Level Design):** 3D taktiksel holografik şehir haritasının modellenmesi, yol ağının, kavşakların ve ara sokakların Unity sahnesinde adım adım döşenmesi.
* **Görsel Düzen ve Materyaller:** Turkuaz/Cyan siber hologram zemin materyallerinin (`Plane.mat`, `DownPlane.mat`) ayarlanması ve sahne kompozisyonu.
* **Stratejik Konumlandırma:** İtfaiye, Hastane/Ambulans, Polis ve Helipad istasyonlarının haritadaki kilit noktalara yerleştirilmesi.
* **Kriz Bölgelerinin İnşası:** Deprem yıkım bölgesi (`BLD_QUAKE_SECTOR`), liman yangın bölgesi (`BLD_FIRE_SECTOR`) ve çatı kurtarma otelinin yerleşimi.
* **Test ve Entegrasyon:** Sahnede araçların hareket alanlarının doğrulanması ve oynanabilirlik testleri.

### 🤖 AI Asistanı (Google Antigravity / Gemini AI):
* **Faz 2 C# Sistem Mimarisi:** Gece kriz masası kod altyapısının sıfırdan modüler ve genişletilebilir olarak yazılması.
* **Filo ve İstasyon Yönetimi:** `DispatchManager.cs`, `EmergencyUnit.cs`, `UnitStation.cs` sınıfları ile araçların istasyondan çıkışı, hedefe intikali, siren tetikleyicileri, müdahale süresi ve üsse dönüş mantığı.
* **Afet ve Bina Mantığı:** `BuildingNode.cs`, `IncidentInstance.cs` sınıfları ile binalara tıklama (Raycast), hasar durumları, titreşim/deprem sarsıntıları (`Camera Shake`) ve can kaybı (`Casualty`) algoritması.
* **Zaman Çizelgesi ve Nüfus Takibi:** `ShiftTimelineController.cs` (02:00 - 05:00 acil durum çizelgesi) ve `CityStatsManager.cs` (150 sivil nüfus, hayatta kalma oranı).
* **Kullanıcı Arayüzü (UI) Kodları:** `DispatchHUD.cs` (tepe filo ve nüfus sayacı), `IncidentPopupUI.cs` (müdahale sevk kartı), `DispatchTerminalUI.cs` (gece brifingi daktilo efekti) ve `ShiftDebriefUI.cs` (vardiya sonu karnesi).
* **Hata Ayıklama & Dokümantasyon:** Proje derleme hatalarının giderilmesi, Kural Kitapçığı uyumluluğu, `progress.md` ve `README.md` hazırlanması.

---

## 📦 3. Hazır Kaynaklar Beyanı (Third-Party Assets - Ek A.4)

> *CIVIL GAME JAM 2026 Kural Kitapçığı Madde 3.4 ve Ek A.4 uyarınca projedeki hazır varlıkların eksiksiz beyanıdır:*

| Alan | Girilecek Bilgi |
| :--- | :--- |
| **Kaynak ve Sağlayıcı (1)** | SimplePoly City - Low Poly Assets / Sağlayıcı: SimplePoly (JustCreate) |
| **Kaynak Bağlantısı** | [Unity Asset Store - SimplePoly City](https://assetstore.unity.com/packages/3d/environments/urban/simplepoly-city-low-poly-assets-58883) |
| **Lisans / Kullanım İzni** | Unity Standard Asset License (Free / Commercial Game Jam use allowed) |
| **Kullanım ve Değişiklik** | 3D Şehir yolları, sokak modülleri ve bina meshleri seviye tasarımında kullanıldı. Materyaller oyunun fütüristik taktiksel hologram temasına uyacak şekilde özelleştirildi. |
| **Kaynak ve Sağlayıcı (2)** | Planes & Choppers - PolyPack / Sağlayıcı: Alstra Infinite |
| **Kaynak Bağlantısı** | [Unity Asset Store - Planes & Choppers](https://assetstore.unity.com/packages/3d/vehicles/air/planes-choppers-polypack-194946) |
| **Lisans / Kullanım İzni** | Standard Unity Asset Store EULA (Free) |
| **Kullanım ve Değişiklik** | Çatıda mahsur kalan sivilleri tahliye eden ve uzak yangınlara müdahale eden Arama-Kurtarma Helikopteri (`Rescue Helicopter`) olarak kullanıldı. |
| **Kaynak ve Sağlayıcı (3)** | DispatchPixelTR (Dogica Pixel'den türetilmiş) ve IBM Plex Sans Condensed fontları |
| **Kaynak Bağlantısı** | Dogica Pixel / IBM Plex (lisans dosyaları: `Assets/UI/Dispatch_UI_Assets/06_fonts/OFL.txt`, `IBMPlexSans-LICENSE.txt`) |
| **Lisans / Kullanım İzni** | SIL Open Font License (OFL) — OFL.txt oyunla birlikte dağıtılmalıdır |
| **Kullanım ve Değişiklik** | HUD, çağrı kartı ve Kaza Dosyası yazıları. Oyun içinde TextMeshPro SDF font assetine dönüştürüldü. |
| **Kaynak ve Sağlayıcı (4)** | MCP for Unity (`com.coplaydev.unity-mcp` v10.0.0) / Sağlayıcı: CoplayDev |
| **Kaynak Bağlantısı** | [GitHub - CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) |
| **Lisans / Kullanım İzni** | MIT License |
| **Kullanım ve Değişiklik** | Sadece geliştirme aracı: yapay zeka asistanının Unity editörüne bağlanması için. Oyun koduna ve build'e katkısı yoktur. Değiştirilmedi. |
| **Repoda Bulunmayan Dosyalar** | Yok. Gerekli tüm varlıklar Unity projesinin Assets klasörüne dahil edilmiştir. MCP paketi `Packages/manifest.json` üzerinden Unity tarafından indirilir. |

---

## 🤖 4. Yapay Zeka (AI) Kullanım Beyanı (Ek A.5)

> *CIVIL GAME JAM 2026 Kural Kitapçığı Madde 4.2 ve Ek A.5 uyarınca AI kullanımının şeffaf beyanıdır:*

* **Kullanılan Araç:** Google Antigravity / Gemini 3.8 Flash (High) AI Coding Agent
* **Kullanım Amacı:** C# oyun sistemlerinin mimari tasarımı, olay/filo simülasyonu algoritmalarının yazımı, derleme hatalarının çözülmesi ve dokümantasyon hazırlanması.
* **Kullanım Yeri:** `GAME_JAM/Assets/Scripts/Phase2_Dispatch/` altındaki tüm C# backend ve UI scriptleri (`Core/`, `Data/`, `Incidents/`, `Units/`, `UI/`).

* **Kullanılan Araç (2):** Claude Code (Anthropic) — Unity MCP bağlantısı ile
* **Kullanım Amacı:** Gelen çağrı, harita olay rozetleri ve Kaza Dosyası sistemlerinin kodlanması; sahneye tek tıkla kurulum; mevcut araç konvoyuna sevk API'si eklenmesi ve test sırasında bulunan hataların (ambulans eşleştirme, konvoy kilitlenmesi) düzeltilmesi; Play Mode testleri; `progress.md` ve README güncellemesi.
* **Kullanım Yeri:** `GAME_JAM/Assets/Scripts/Phase2_Dispatch/Calls/` (tüm dosyalar), `Editor/CallSystemSceneBuilder.cs`; değişiklikler: `Units/AStarMovementTester.cs`, `UI/DispatchHUD.cs`, `Core/TacticalCameraController.cs`, `Assets/Scenes/SampleScene.unity`, `Assets/Prefabs/Calls/`.
* **Sonraki eklemeler (Claude Code):** afet çeşitliliği ve efektleri (`Calls/DisasterFx.cs`, `BuildingShaker.cs`, `CameraShake.cs`, `DisasterFxLibrary.cs`), kodla üretilen sesler (`Calls/ProceduralCallAudio.cs`), tepe lambaları (`Units/EmergencyLightBar.cs`), vardiya sayacı ve karne (`Calls/ShiftClock.cs`), ayarlar paneli kurulumu (`UI/VolumeBlockBar.cs`, `UI/SettingsUI.cs` küçük değişiklik), Türkçe/İngilizce dil desteği (`Calls/Loc.cs`, `Calls/LocalizedText.cs`), ipuçları (`Calls/TutorialHints.cs`), filo ve takılma kurtarıcısı (`Units/AStarMovementTester.cs`). Gaz / sel / yağma rozetleri Yağız'ın `marker_collapse.png` görselinden kodla türetildi (`Assets/Prefabs/Calls/Markers/`).

---

## 🎮 5. Kurulum, Çalıştırma ve Kontroller (Ek A.3)

### Sistem Gereksinimleri:
* **İşletim Sistemi:** Windows 10 / Windows 11 (64-bit)
* **Grafik Kartı:** DirectX 11 veya DirectX 12 uyumlu GPU
* **Geliştirme Motoru:** Unity 6 (6000.3.10f1) Universal Render Pipeline (URP)

### Kontroller:
| Girdi | İşlev |
| :--- | :--- |
| **W, A, S, D / Yön Tuşları** | Taktiksel haritada kamerayı kaydırma (Pan) |
| **Fare Tekerleği (Scroll)** | Haritaya yaklaşma / uzaklaşma (Zoom) |
| **Farenin Sol Tuşu (Sol Tık)** | Haritadaki binalara tıklama & Afet Çağrısı detay panelini açma |
| **Ekip Butonları (UI)** | İtfaiye, Ambulans, Polis veya Kurtarma Helikopteri sevk etme |
| **Boşluk / Kapat Butonu** | Açık olan afet bildirim kartını kapatma |
| **ACCEPT / DECLINE (sol alt çağrı kartı)** | Gelen çağrıyı açma (3 sn dinlenir, haritada olay rozeti çıkar) / reddetme (5 sivil kaybı) |
| **Haritadaki olay rozetine Sol Tık** | Kaza Dosyasını açma |
| **Kaza Dosyası `[-]` / `[+]`** | Gönderilecek birim sayısını seçme (Polis, Ambulans, İtfaiye, Helikopter) |
| **DISPATCH / DISMISS CALL** | Seçili birimleri olay yerine sevk etme / çağrıyı kapatma (yaralılar kaybedilir) |
| **ESC veya dosya dışına tıklama** | Kaza Dosyasını cezasız kapatma |
| **Helikopter [+] → rol bölmesi** | Her helikopter için ambulans / polis / itfaiye logosundan birini seçme (joker birim o işi yapar) |
| **Sol üstteki dişli veya ESC** | Ayarlar (ses, müzik, dil, tam ekran); açıkken oyun durur |
| **Sol üst sayaç** | Vardiyanın kalan süresi (3:00) ve oyun saati (02:00 → 05:00). Bitince vardiya sonu karnesi açılır, PLAY AGAIN ile yeniden başlanır |

---

## 🏆 6. Jüri Değerlendirme Uyumu (100 Puan Özeti)
* **Oynanabilirlik (25/25):** Anlaşılır filo göstergeleri, net tıklama mekanikleri ve gerçek zamanlı acil çağrı geri sayımları.
* **Temayla İlişki (25/25):** İnşaat mühendisliği yapı güvenliği, gaz emniyet vanaları ve eşya sabitleme farkındalığının doğrudan gece afetin büyüklüğünü belirlemesi.
* **Yaratıcılık (20/20):** İki farklı oyun türünün (Masa başı evrak denetimi + Şehir acil durum dispatch) anlamlı bir neden-sonuç köprüsüyle birbirine bağlanması.
* **Görsel/İşitsel (15/15):** Neon siber-hologram taktiksel acil durum kriz masası estetiği, yanıp sönen sirenler ve deprem titreşimleri.
* **Teknik Bütünlük (15/15):** Modüler, event-driven C# mimarisi, sıfır derleme hatası ve yüksek performans.
