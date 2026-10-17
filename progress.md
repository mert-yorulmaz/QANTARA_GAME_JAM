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
