# 📋 CIVIL GAME JAM 2026 — Proje İlerleme ve Teslim Takip Dosyası (progress.md)

> **Oyun Başlığı:** *Sıfır Saati: Denetim ve Kriz* (Zero Hour: Inspection & Crisis)  
> **Tema:** Afete Hazırlık ve Farkındalık (*Disaster Preparedness & Awareness*)  
> **Organizasyon:** KTMMOB İnşaat Mühendisleri Odası & ODTÜ KALTEV & LootBAG Studios  
> **Bizim Sorumluluk Alanımız:** **Yazılımcı 2 — Faz 2: Gece 3D Kriz Haritası ve Dispatch Sistemi**  
> **Resmî Son Teslim:** **18 Ekim 2026 Pazar · 18:00 UTC+3** (Gecikme affı yoktur!)  
> **Git Destek Sınırı:** 18 Ekim 2026 Pazar · 12:00 UTC+3  

---

## ⏱️ Zaman Çizelgesi & Geri Sayım Durumu
* **Başlangıç:** 16 Ekim Cuma 18:00
* **Şu Anki Durum (17 Ekim Cumartesi Sabahı):** Harita kurulumu ve Faz 2 entegrasyonu sürüyor.
* **Kalan Süre:** ~35 Saat (Geliştirme, Oynanış Testleri, Build Alma ve Video Çekimi dahil).

---

## 🎯 Jüri Puanlama Kriterleri (100 Puan) ve Faz 2 Odak Noktaları

| Kriter | Puan | Faz 2'deki Karşılığımız | Hedef Durum |
| :--- | :---: | :--- | :---: |
| **Oynanabilirlik** | **25** | Akıcı kamera kontrolleri, net dispatch popup arayüzü, ekiplerin haritada net intikali ve anlaşılır afet geri sayımları. | ⏳ Devam Ediyor |
| **Temayla İlişki** | **25** | Gündüz yapılan denetim hatalarının (gaz vanası, kolon zafiyeti, eşya sabitleme) gece yıkıcı felaketlere dönüşmesi; inşaat mühendisliği ve afet bilinci vurgusu. | ⏳ Devam Ediyor |
| **Yaratıcılık** | **20** | *Papers, Please* denetim mekaniği ile *911 Operator* taktiksel filo yönetiminin tek çatı altında hibritleştirilmesi. | ✅ Tasarım Hazır |
| **Görsel/İşitsel Bütünlük** | **15** | Cyan/Neon siber holografik şehir haritası, siren ışıkları, deprem kamera sarsıntısı ve atmosferik kriz telsiz bildirimleri. | ⏳ Harita Yapılıyor |
| **Teknik Bütünlük** | **15** | Sıfır konsol hatası, stabil C# kod tabanı, kusursuz çalışan Windows Standalone Build. | 🔄 %80 Tamam |

---

## 📊 Faz 2 (Gece Dispatch) Görev Listesi & İlerleme

### 1. Kod Mimarisi & Mantık (C# Backend)
- [x] `DispatchEnums.cs`: Araç tipleri (İtfaiye, Ambulans, Polis, Helikopter) ve Afet tipleri tanımlandı.
- [x] `CityStatsManager.cs`: 150 kişilik şehir nüfusu, hayatta kalanlar ve can kaybı sayacı kodlandı.
- [x] `ShiftTimelineController.cs`: 02:00 - 05:00 acil durum çizelgesi ve senaryo olayları oluşturuldu.
- [x] `BuildingNode.cs`: Binaların hasar, sarsıntı, afet durumu ve tıklanma mekaniği hazırlandı.
- [x] `IncidentInstance.cs`: Afet gereksinimleri, süre barları ve aciliyet hesaplaması tamamlandı.
- [x] `EmergencyUnit.cs`: Araçların istasyondan çıkışı, binada müdahalesi ve geri dönüşü yazıldı.
- [x] `UnitStation.cs`: İstasyon filo kapasitesi ve dispatch çıkış noktaları bağlandı.
- [x] **C# Derleme Hatalarının Giderilmesi:** `dotnet build` ile test edildi, **0 Uyarı 0 Hata** ile derlendi.

---

### 2. Harita & 3D Sahne Düzeni (Şu Anki Aşama)
- [x] Temel zemin (`Plane.mat` & `DownPlane.mat` ile Cyan Hologram zemini) oluşturuldu.
- [x] `SimplePoly City - Low Poly Assets` paketi projeye import edildi.
- [ ] **Yol Ağı ve Bloklar:** Ana cadde ve ara yolların döşenmesi *(Kullanıcı tarafından yapılıyor 🔨)*.
- [ ] **İstasyonların Yerleşimi (`UnitStation`):**
  - [ ] İtfaiye İstasyonu (Haritanın sol orta bloğu)
  - [ ] Hastane / Ambulans Merkezi (`H` ile işaretli merkez)
  - [ ] Polis Merkezi (Stadyum güneyi, doğu bloğu)
  - [ ] Helipad (Hava ambulans / tahliye kalkış pisti)
- [ ] **Kilit Afet Binalarının Yerleşimi (`BuildingNode`):**
  - [ ] `BLD_QUAKE_SECTOR`: Sol alt köşedeki deprem fayı ve ağır yıkım bölgesi
  - [ ] `BLD_FIRE_SECTOR`: Sağ alt köşedeki uzak liman/sanayi yangın bölgesi
  - [ ] `BLD_HOTEL`: Çatıda mahsur kalan siviller (Helikopter kurtarması)
  - [ ] `BLD_METRO_PLAZA`: Gündüz kaçan gaz vanası yüzünden patlayan bina
  - [ ] `BLD_COMMERCIAL`: Yağma/asayiş ihbarı olan ticari bölge

---

### 3. Arayüz & Kullanıcı Deneyimi (HUD & Popups)
- [ ] **Tepe Barı (HUD):**
  - Sol Üst: `👤 150 / 150` Hayatta Kalan Sivil Sayacı
  - Sağ Üst / Orta: Filo Durum Kutuları (`Polis 4/5`, `Ambulans 5/5`, `İtfaiye 6/6`, `Helikopter 5/5`)
  - Saat Göstergesi (`02:00 AM - 05:00 AM`)
- [ ] **Olay Müdahale Paneli (`IncidentPopupUI`):** Binaya tıklandığında açılan ekip sevk kartı.
- [ ] **Vardiya Başlangıç Terminali (`DispatchTerminalUI`):** Gece brifingi daktilo efekti.
- [ ] **Vardiya Sonu Karne Ekranı (`ShiftDebriefUI`):** Kurtarılanlar, can kayıpları ve Başarı Derecesi (Grade S/A/B/C/F).

---

### 4. Görsel Efektler & Juice (Polishing)
- [ ] Taktik İzometrik Kamera (`TacticalCameraController`) pan ve zoom sınırları ayarı.
- [ ] Deprem sarsıntı efekti (`Camera Shake` & `Building Tremor Shake`).
- [ ] Binalarda aktif afet uyarı pulseları / aydınlatmaları.
- [ ] Araç tepe siren ışıkları ve iz efektleri (`TrailRenderer`).

---

## 📜 Kural Kitapçığı Uyumluluk Beyanları (Diskalifiye Önleme Tablosu)

> **Kural 3.4 & 4.4 & 10.1 Gereğince:** Beyan edilmeyen hazır kaynak ve AI kullanımı doğrudan **DİSKALİFİYE** nedenidir! Bu tablo teslim formuna aynen aktarılmak üzere güncel tutulacaktır:

### 📦 Hazır Kaynaklar Beyanı (Third-Party Assets)
| Kaynak Adı / Sağlayıcı | Edinildiği Sayfa / Kaynak | Lisans Türü | Projedeki Kullanım Yeri ve Değişiklik |
| :--- | :--- | :--- | :--- |
| **SimplePoly City - Low Poly Assets** | Unity Asset Store | Unity Standard Asset License | 3D Şehir yolları ve binaların modelleri için kullanıldı. Materyaller holografik tarz için özelleştirildi. |
| **Planes & Choppers - PolyPack** (Alstra Infinite) | [Unity Asset Store](https://assetstore.unity.com/packages/3d/vehicles/air/planes-choppers-polypack-194946) | Standard Unity Asset Store EULA | Arama-Kurtarma ve Çatı Tahliyesi Helikopteri (`Rescue Helicopter`) olarak kullanıldı. |

### 🤖 Yapay Zeka (AI) Kullanım Beyanı (Ek A.5)
| Kullanılan Araç | Kullanım Amacı | Kullanıldığı Bölümler |
| :--- | :--- | :--- |
| **Google Antigravity / Gemini AI** | C# Kod mimarisinin yazımı, derleme hatalarının çözümü, veri yapıları ve kriz simülasyonu mantığının kurulması | Faz 2 Gece Dispatch sistemi C# scriptleri (`DispatchManager`, `BuildingNode`, `ShiftTimelineController`, `CityStatsManager` vb.) |

---

## 🚀 Teslim Öncesi Final Kontrol Listesi (Checklist)

- [ ] Oyun 48 saatlik sürede tamamlandı.
- [ ] Git reposu **PUBLIC** (Herkese Açık) durumda (`madde 6.5`).
- [ ] Oynanabilir Windows Standalone Build (`.exe` ve `Data` klasörü zip olarak) Google Drive / itch.io bağlantısıyla hazır ve herkese açık (giriş gerektirmeden indirilebilir).
- [ ] En az 1 dakikalık oynanış tanıtım videosu YouTube / Drive üzerine yüklendi (giriş istemeyen doğrudan HTTPS linki).
- [ ] Tam Git Commit ID teslim formuna girildi (Build ile birebir aynı commit).
- [ ] Kurulum ve kontroller rehberi (`README.md` / `Çalıştırma Talimatları`) eklendi.
- [ ] 5 dakikalık jüri sunumu provası yapıldı.

---
---

# 🔄 GÜNCELLEME BÖLÜMÜ — 18 Ekim 2026 (Pazar)

> ## ⚠️ BU SATIRDAN İTİBAREN YAZILANLAR SONRADAN EKLENMİŞTİR "CLAUDE"
> Yukarıdaki bölümlerin (satır 1–107) hiçbiri silinmedi veya değiştirilmedi. Onlar **17 Ekim Cumartesi sabahının** durumunu yansıtır ve **eskimiştir**.
> Bu bölüm, 18 Ekim'de **Claude Code** tarafından proje dosyaları ve Unity sahnesi incelenerek hazırlandı. Bir madde "doğrulandı" diyorsa dosyada veya sahnede görüldü. "Doğrulanmadı" diyorsa kaynağı elle test edilmeden kesin kabul edilmemeli.
> Güncel tarih ile yukarıdaki geri sayım arasında uyumsuzluk var: **Git destek sınırı bugün 12:00, resmî teslim bugün 18:00 (UTC+3).**

## 🧭 Yukarıdaki Bölümlerin Güncel Durumu (Eskimiş Satırlar İçin Düzeltme)

| Yukarıdaki iddia | Güncel durum | Doğrulama |
| :--- | :--- | :--- |
| "17 Ekim sabahı, kalan ~35 saat" (satır 14–15) | Süre bugün 18:00'de doluyor. Git desteği 12:00'de bitiyor. | Dosyadaki resmî tarihlerden |
| `IncidentPopupUI`, `DispatchTerminalUI`, `ShiftDebriefUI` ve `DispatchHUD` yapılmadı `[ ]` (satır 64–70) | Dört script de `Assets/Scripts/Phase2_Dispatch/UI/` altında **mevcut**. Sahneye bağlı olup doğru çalıştıkları **doğrulanmadı**. | Dosya var, çalışma doğrulanmadı |
| Yol ağı "kullanıcı tarafından yapılıyor" (satır 48) | Handover'a göre A* NavMesh için 126 parça cadde fırınlandı, araç konvoyu çalışıyor. `CityNavMeshBaker.cs` mevcut. | Handover + dosya var, oyunda test edilmedi |
| Kamera pan/zoom `[ ]` (satır 75) | `TacticalCameraController.cs` mevcut, handover'a göre WASD pan, zoom ve clamp çalışıyor. | Dosya var, oyunda test edilmedi |
| Ayarlar menüsü listede yok | `SettingsUI.cs` mevcut (ses, müzik, dil, tam ekran). | Dosya var, oyunda test edilmedi |
| Faz 1 hiç anılmıyor | Faz 1 (Gündüz Masası) başka ekip üyesi tarafından yapılıyor. Bu bölümde kodlanmadı, **dokunulmayacak**. | Handover |

## ✅ Doğrulanan Güncel Durum (Dosya / Sahne İncelemesi)

- [x] `Assets/Scripts/Phase2_Dispatch/` altında 22 C# dosyası var: `Core/` (CityStatsManager, DispatchManager, ShiftTimelineController, TacticalCameraController), `Data/` (DispatchEnums, Phase1BridgeData), `Incidents/` (BuildingNode, IncidentInstance), `Units/` (EmergencyUnit, UnitStation, AStarMovementTester), `UI/` (DispatchHUD, DispatchTerminalUI, IncidentPopupUI, SettingsUI, ShiftDebriefUI) ve `Editor/` (CityNavMeshBaker, CityStreetLighting, EmergencySystemSetup, HUDCanvasBuilder, MaterialURPUpgrader, TacticalNightAtmosphere).
- [x] `CityStatsManager.OnCallDismissed(int civiliansLost = 5)` ve `DispatchManager.DispatchUnit(UnitType, BuildingNode)` metotları mevcut. Yeni çağrı/kaza dosyası sistemi bunlara bağlanabilir.
- [x] Yağız'ın UI PNG'leri `Assets/UI/Dispatch_UI_Assets/03_ui_components/` altında hazır: `call_card_ringing`, `call_card_listening`, `call_progress_fill`, `hud_units_panel`, `hud_population_panel`, `incident_file_panel`, `marker_collapse`, `marker_fire` (ve `marker_unverified` handover'da anılıyor, bu incelemede doğrulanmadı).
- [x] Sahne: `Assets/Scenes/SampleScene.unity`, 755 kök nesne, Unity MCP ile okundu. Bir `Canvas` nesnesi ve bir `Props_Roof Helipad` nesnesi var.
- [x] Proje üzerinde kayıtlı son commit: `de543b3` (17 Ekim 23:39). Çalışma ağacında işlenmemiş değişiklikler: `CLAUDE_PROJECT_HANDOVER.md`, `GAME_JAM/Packages/manifest.json`, `GAME_JAM/Packages/packages-lock.json`.

## ❌ Henüz Yapılmayan / Eksik Olduğu Görülen İşler

- [ ] **Gelen çağrı sistemi** (10–15 sn döngü, `call_card_ringing` / `call_card_listening`, 3 sn dinleme barı, reddedilince sivil kaybı): bu işi yapan bir script **bulunmadı**.
- [ ] **Harita olay rozetleri** (bina üstünde billboard `marker_fire` / `marker_collapse`, tıklanabilir): bu işi yapan bir script **bulunmadı**.
- [ ] **Kaza dosyası paneli** (`incident_file_panel`, `[-] 0 [+]` birim seçici, DISPATCH / DISMISS): `IncidentPopupUI.cs` mevcut ama handover'daki tasarıma (sarı dosya, Polaroid, birim sayıcıları) uyup uymadığı **doğrulanmadı**.
- [ ] **Kritik afet binaları** (`BLD_QUAKE_SECTOR`, `BLD_FIRE_SECTOR`, `BLD_HOTEL`, `BLD_METRO_PLAZA`, `BLD_COMMERCIAL`): sahne dosyasında bu adlarla **nesne bulunamadı**. Farklı adlandırılmış olabilirler, elle kontrol gerekir.
- [ ] **İstasyonlar** (`UnitStation`): sahne dosyasında "Station" adlı nesne bulunamadı. `Props_Roof Helipad` helipad için var. Diğer istasyonlar farklı adlandırılmış olabilir, doğrulanmadı.
- [ ] **Faz 1 → Faz 2 köprüsü**: `Phase1BridgeData.cs` mevcut, ancak gündüz kararlarının geceki afet şiddetini gerçekten değiştirip değiştirmediği doğrulanmadı.
- [ ] **Deprem sarsıntısı, afet pulseları, siren ışıkları** (Bölüm 4): doğrulanmadı.

## ⚠️ Dikkat Edilmesi Gereken Riskler

- **Eski Input kullanımı:** `TacticalCameraController.cs` (`Input.GetAxisRaw`, `Input.GetAxis`), `SettingsUI.cs` (`Input.GetKeyDown`) ve `AStarMovementTester.cs` (`Input.GetKeyDown`) içinde eski Input API çağrıları görüldü. Handover projede New Input System kullanıldığını söylüyor. Bu çağrılar bir `#if` / yedek dal içinde olabilir. Bu incelemede **doğrulanmadı**. `activeInputHandler: 2 (Both)` olduğu sürece çalışması beklenir, ancak build'de test edilmeli.
- **UI referans çözünürlüğü:** Handover Canvas Scaler'ı 1280x720 / Match 0.5 olarak veriyor. PNG'lerin hangi ölçekte çizildiği **doğrulanmadı**.
- **Test edilmedi:** Bu incelemede Play Mode'a girilmedi, oyun çalıştırılmadı. Yukarıdaki "mevcut" ifadeleri dosyanın varlığını söyler, çalıştığını değil.

## 🤖 Yapay Zeka Kullanım Beyanı — EKLENECEK SATIR (Taslak, Onay Bekliyor)

> Yukarıdaki Ek A.5 tablosunda yalnızca Google Antigravity / Gemini AI yazıyor. Handover dosyası bu projede **Claude** kullanıldığını gösteriyor. Kural 4.4 / 10.1 uyarınca beyan edilmeyen AI kullanımı diskalifiye nedenidir. Teslim formuna girmeden önce proje sahibi aşağıdaki satırı **doğrulayıp** tabloya taşımalıdır.

| Kullanılan Araç | Kullanım Amacı | Kullanıldığı Bölümler |
| :--- | :--- | :--- |
| **Claude Code (Anthropic)** | Proje dosyalarının incelenmesi, Unity MCP bağlantısının kurulması, `progress.md` güncellemesi. Çağrı, rozet ve kaza dosyası sistemleri yazılırsa kod yazımı da buraya eklenecek. | `progress.md`, `GAME_JAM/Packages/manifest.json` (MCP paketi). **Not:** Henüz oyun C# kodu yazılmadı, yazılırsa dosya adları buraya işlenecek. |

## 🛠️ Bu Oturumda Yapılan Teknik Değişiklikler (Claude Code)

- `GAME_JAM/Packages/manifest.json`: `com.coplaydev.unity-mcp` paketi `v10.0.0` sürümüne sabitlenerek eklendi (Unity MCP köprüsü, geliştirme aracıdır). Oyun koduna girmez. **Hazır kaynak beyanına eklenmeli mi?** Teslimde proje klasörüne dahil olacağı için karar proje sahibine ait. Beyana eklenmesi güvenli taraftır.
- `GAME_JAM/Packages/packages-lock.json`: Unity tarafından otomatik güncellendi.
- Bilgisayara `uv` (Python paket aracı) kuruldu, proje dosyası değildir.

## 📋 Önerilen Sıradaki Adımlar (Süre: Bugün 18:00'e kadar)

1. Sahnede çalıştırıp neyin gerçekten çalıştığını gör (Play Mode testi) ve bu bölümdeki "doğrulanmadı" satırlarını güncelle.
2. Çağrı sistemi, olay rozetleri ve kaza dosyası paneli (Handover görev 1–3).
3. Faz 1 ile birleştirme ve Windows Standalone Build.
4. Beyan tablolarını (üstteki taslak dahil) gözden geçirip README ile birlikte güncelle.
5. Public repo, build linki, tanıtım videosu, commit ID'nin teslim formuna girilmesi.
