# 🚨 CLAUDE PROJECT DEVİR VE GÖREV REHBERİ (HANDOVER)
> **Proje:** Sıfır Saati: Denetim ve Kriz (Zero Hour: Inspection & Crisis)  
> **Yarışma:** Civil Game Jam 2026 (ODTÜ KKK KALTEV / KTMMOB İMO)  
> **Motor:** Unity 6 (6000.3.10f1 LTS) · Universal Render Pipeline (URP)  
> **Input:** Unity New Input System Package (activeInputHandler: 2 / Both)

---

## 📌 1. CLAUDE İÇİN SİSTEM TALİMATI (Custom Instructions)
*Claude Project oluştururken "Set Custom Instructions" alanına doğrudan bu kutuyu yapıştırın:*

```text
Sen Unity 6 (URP) ile geliştirilen "Sıfır Saati: Denetim ve Kriz" oyununun baş yazılımcısısın.
Aşağıdaki 5 Altın Kuralı ASLA ÇİĞNEME:
1. FAZ 1 KORUMASI: Faz 1 (Gündüz Bürokrasi Masası) kodlarına ASLA dokunma.
2. INPUT SİSTEMİ: Projede New Input System aktiftir. Eski Input.GetKeyDown yerine UnityEngine.InputSystem (Keyboard.current, Mouse.current) kullan.
3. NAVMESH & FİZİK: A* NavMesh, 10.5m araç takip mesafesi, yöne duyarlı radar ve Helikopter VTOL mantığı tamamlanmıştır. Çalışan hareket fiziğini bozma.
4. BİNA MATERYALLERİ: Kullanıcının özel bina emisyon ve renklerini kodla ezme.
5. YAĞIZ'IN UI ASSETLERİ: Tüm arayüz çizimleri Assets/UI/Dispatch_UI_Assets altındadır. Canvas Scaler "Scale With Screen Size" ve 1280x720 referansındadır.
Kullanıcı çok yorgundur; kodları parça parça verme, doğrudan Unity'ye yapıştırabileceği eksiksiz C# sınıfları ve sahneyi tek tıkla kuracak [MenuItem] Editor Scriptleri yaz.
```

---

## 🏗️ 2. Şu An Çalışan ve Tamamlanmış Sistemler
1. **A* Asfalt NavMesh & Kazasız Konvoy:**
   - 126 parça cadde fırınlandı (`agentRadius = 1.85m`), kaldırımlar ve çimenler elendi.
   - 4 araç (`Vehicle_Ambulance`, `Vehicle_Police Car_separate`, `Vehicle_Bus_color02_separate`, `HelicopterV2`) hedefe 10.5m aralıklarla nizami sıraya giriyor, deadlock olmadan U dönüşüyle üsse dönüyor.
   - Helikopter havada 35m irtifada asılı kalıyor, 2400°/s pervane çeviriyor ve helipade dikey iniş yapıyor.
2. **Sabit Üst Bar (Top HUD):**
   - `DispatchHUD.cs`: Sol üstte 4 birim sayacı (`Police`, `Ambulance`, `Fire Truck`, `Helicopter`), sağ üstte canlı nüfus sayacı (`150 / 150`).
3. **Ayarlar Menüsü:**
   - `SettingsUI.cs`: Ses (%70), Müzik (%50), Dil (`ENGLISH`/`TÜRKÇE`), Tam Ekran, Defaults ve ESC aç/kapa.
4. **Taktik Gece Kamerası:**
   - `TacticalCameraController.cs`: New Input System uyumlu WASD pan, zoom ve clamp.

---

## 🎨 3. Yağız'ın UI Klasör Haritası (`Assets/UI/Dispatch_UI_Assets/`)
* **Önemli Kural:** Canvas Scaler Reference Resolution = **1280 x 720**, Match = **0.5**.
* `03_ui_components/`:
  - `hud_units_panel.png` $\rightarrow$ Sol üst UNITS paneli
  - `hud_population_panel.png` $\rightarrow$ Sağ üst 150/150 nüfus paneli
  - `call_card_ringing.png` $\rightarrow$ Çalan telefon bildirim kartı
  - `call_card_listening.png` $\rightarrow$ 3 saniye dinlenen telsiz kartı
  - `call_progress_fill.png` $\rightarrow$ 3 saniyelik dinleme dolgu barı
  - `marker_fire.png` $\rightarrow$ Haritada yanan bina üstünde çıkan alev rozeti
  - `marker_collapse.png` $\rightarrow$ Haritada çöken bina üstünde çıkan enkaz rozeti
  - `marker_unverified.png` $\rightarrow$ İhbar edilen şüpheli bina rozeti
  - `incident_file_panel.png` $\rightarrow$ Rozete tıklanınca açılan sarı kaza dosyası

---

## 🎯 4. Claude'un Bitirmesi Gereken 3 Görev

### Görev 1: Çağrı Akışı (Incoming Call System)
* Her 10–15 saniyede bir `call_card_ringing.png` ekranda yanıp söner (telefon zili).
* **Kabul Edilirse:** `call_card_listening.png` açılır, `call_progress_fill.png` 3 saniyede sıfırdan dolarken boğuk telsiz/mumble sesi çalar $\rightarrow$ Çağrı kapanır $\rightarrow$ Haritada olay rozeti belirir.
* **Reddedilirse (Dismiss):** `CityStatsManager.Instance.OnCallDismissed(5)` çağrılır, 150 kişilik nüfustan can kaybı düşer!

### Görev 2: Harita Event Rozetleri (World-Space Billboard)
* Çağrı dinlendikten sonra olayın olduğu binanın tam üstünde havada yüzen, kameraya bakan (`transform.rotation = Camera.main.transform.rotation`) `marker_fire.png` veya `marker_collapse.png` rozeti çıkar.
* Hafifçe yukarı-aşağı salınır (sine wave) ve tıklanabilir (`OnMouseDown` / `Raycast`).

### Görev 3: Kaza Dosyası (Damage Control Line Incident File)
* Rozete tıklandığında ekrana `incident_file_panel.png` gelir.
* Polaroid fotoğraf, adres, yaralı sayısı ve birim tablosu:
  - Police: `[-] 0 [+]`
  - Ambulance: `[-] 0 [+]`
  - Fire Truck: `[-] 0 [+]`
  - Helicopter: `[-] 0 [+]`
* **DISMISS CALL:** Dosyayı kapatır ve ceza keser.
* **DISPATCH:** Seçilen sayıda aracı `DispatchManager.Instance.DispatchUnit()` ile olay yerine sevk eder!

---

## 💬 5. Claude'a Gönderilecek Hazır Prompt

```text
Selam Claude! Project Knowledge'taki CLAUDE_PROJECT_HANDOVER.md, ZERO_HOUR_GAME_WORKFLOW.md ve README.md dosyalarını oku.
Şu an Faz 2'nin A* araç konvoyu, 10.5m güvenli mesafesi, New Input System uyumu ve Top HUD kodları sıfır hatayla çalışıyor.

Assets/UI/Dispatch_UI_Assets altındaki hazır PNG'leri kullanarak şu 3 sistemi kodlamanı istiyorum:
1. Incoming Call (Çağrı) Sistemi (call_card_ringing & listening, 3s dinleme barı, 10s döngü, reddedilince sivil kaybı).
2. Haritada bina üstünde çıkan 3D Billboard Rozet (marker_fire & marker_collapse).
3. Rozete tıklanınca açılan Sarı Kaza Dosyası (incident_file_panel, [-] 0 [+] birim seçici, DISPATCH ve DISMISS).

Lütfen sahnede Canvas elemanlarını tek tıkla dizecek bir Editor Scripti ([MenuItem]) ile birlikte eksiksiz C# kodlarını ver.
```
