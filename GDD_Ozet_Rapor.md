# 🌍 CIVIL GAME JAM — Özet Oyun Tasarım Dokümanı (Lean GDD)

> **Çalışma Başlığı:** *Sıfır Saati: Denetim ve Kriz* (İsim değişebilir)  
> **Ana Tema:** Disaster Preparedness and Awareness *(Afete Hazırlık ve Farkındalık)*  
> **Tür:** Masa Başı Denetim Simülasyonu + Taktiksel Kriz/Kaynak Yönetimi (*Papers, Please* x *911 Operator*)  
> **Hedef Süre & Ekip:** 48 Saat (MVP) | 4 Yazılımcı | Unity 3D (Hazır 3D Şehir/Araç Assetleri + 2D UI)

---

## 🎯 1. Tek Cümlelik Özet (High Concept)
Gündüzleri masa başında binaların ve evlerin afet hazırlık dosyalarını inceleyen bir **Afet Denetçisi**, geceleri ise afetler vurduğunda şehir haritası üzerinde kurtarma ekiplerini yöneten bir **Kriz Operatörü (Dispatch)** olduğumuz; **gündüz yaptığımız her denetim hatasının gece kaynaklarımızı tüketen bir felakete dönüştüğü** iki aşamalı strateji oyunu.

---

## 🔄 2. Temel Oynanış Döngüsü (Core Loop)

1. **☀️ FAZ 1: GÜNDÜZ (Denetim Masası):** Ev/Bina dosyalarını kural kitabına göre incele (Deprem çantası, eşya sabitleme, sel altyapısı, kolon durumu) $\rightarrow$ **Onayla** veya **Eksik Bildir (Tedbir Emri)**.
2. **⚡ SONUÇ KÖPRÜSÜ:** 
   - Doğru denetlenip hazırlanan yerler = **Gece Düşük Hasar (Az kaynak yer)**.
   - Gözden kaçan / hatalı onaylanan yerler = **Gece Ağır Felaket (Çok kaynak ve süre sömürür!)**.
3. **🌙 FAZ 2: GECE (3D Harita Dispatch):** Şehirde eşzamanlı patlak veren çoklu afetlere (Deprem, Sel, Yangın) eldeki sınırlı ekipleri (Ambulans, İtfaiye, AFAD, Bot) yönlendir.
4. **📊 GÜN SONU KARNESİ:** Kurtarılan/kaybedilen binaları gör ve yeni kurallarla sonraki güne geç.

---

## 📋 3. Fazların Detaylı İşleyişi

### ☀️ FAZ 1: Gündüz — Afet Denetçisi (*Papers, Please* Kısmı)
* **Mekanik:** Kamera 2D denetim masasındadır. Ekranın solunda **"Afet Hazırlık Yönetmeliği (Kural Kitabı)"**, ortasında ise denetlenen **Binanın/Evin Dosyası** yer alır.
* **Kontrol Edilen Kriterler (Farkındalık ve Hazırlık):**
  1. **Ev İçi Hazırlık:** Deprem çantası tam mı? Ağır eşyalar/dolaplar duvara sabitlenmiş mi? Aile toplanma alanını biliyor mu?
  2. **Sel & Yangın Altyapısı:** Ev dere yatağında/giriş katta mı? Su tahliye valfi ve kum torbası var mı? Otomatik gaz kesme vanası çalışıyor mu?
  3. **Yapısal Güvenlik:** Binanın yaşı, zemin etüdü ve kolon/taşıyıcı durumu uygun mu?
* **Zorluk ve Seçim:** Süre kısıtlıdır (örn. gün başına 90 saniye). Oyuncu dosyadaki çelişkileri yakalayıp **[ONAYLA (Afete Hazır)]** veya **[RET / HAZIRLIK EMRİ VER]** damgası basar.

---

### 🌙 FAZ 2: Gece — Kriz ve Kaynak Yönetimi (*911 Operator / Dispatch* Kısmı)
* **Mekanik:** Mesai biter, 3D Şehir Haritası açılır ve gece boyunca şehirde **birden fazla afet (Deprem, Sel Baskını, Yangın)** peşi sıra patlak verir.
* **Kaynak Yönetimi:** Oyuncunun elinde sınırlı sayıda müdahale birimi vardır:
  * 🚒 **İtfaiye** (Yangın ve gaz patlamaları)
  * 🚑 **Ambulans / İlk Yardım** (Yaralı kurtarma)
  * 🦺 **AFAD / Arama-Kurtarma** (Enkaz müdahalesi)
  * 🚤 **Kurtarma Botu / Helikopter** (Sel ve ulaşımı kesik bölgeler)
* **Gündüz $\rightarrow$ Gece Bağlantısı (Oyunun Kalbi):**
  * Gündüz **doğru denetlenen ve hazırlanan** evler gece afeti hafif atlatır (0 veya 1 ekip ile 5 saniyede çözülür).
  * Gündüz **gözden kaçırılan, yanlış onaylanan veya süresi yetmediği için bakılamayan** evlerde ise gece **Seviye 3 Ağır Kriz** patlar! (Örn: Sabitlenmeyen dolap ve bozuk gaz vanası yüzünden hem yaralı hem yangın çıkar; 2 ekibi 20 saniye boyunca oraya kilitleyip diğer afetlere yetişememene yol açar).

---

## 🎬 4. Neden "Scripted (Sabit Senaryolu) MVP"? (48 Saat Stratejisi)
48 saatte rastgele (random) algoritmalar ve karmaşık yapay zeka ile vakit kaybetmemek için oyun **3 Günlük El Yapımı (Scripted) Senaryo** olarak tasarlanmıştır:

* **1. GÜN (Alışma):** Sadece 4 bina dosyası. Kurallar sadece *"Deprem Çantası ve Eşya Sabitleme"*. Gece küçük bir sarsıntı ve 3 olay çıkar.
* **2. GÜN (Derinleşme):** 6 bina dosyası. Kurallara *"Dere Yatağı / Sel Altyapısı ve Gaz Vanası"* eklenir. Gece şiddetli fırtına, sel ve yangınlar çıkar.
* **3. GÜN (Final Sınavı):** 8 bina dosyası. Tüm kurallar + *"Kolon/Zemin Kontrolü ve Bütçe Kısıtlaması"*. Gece **Büyük Deprem + Çoklu Afetler** aynı anda vurur; gündüz yaptığı hazırlıklar oyuncunun şehri kurtarıp kurtaramayacağını belirler.

---

## 💻 5. Dört Yazılımcı İçin Modüler İş Bölümü

Git çakışması (merge conflict) yaşamamak için proje 4 bağımsız modüle bölünmüştür:

| Kişi | Sorumluluk Alanı (Modül) | Yazacağı Temel Yapılar |
| :--- | :--- | :--- |
| **Yazılımcı 1** | **Gündüz Denetim Sistemi (2D UI)** | `InspectionManager.cs`, Kağıt/Dosya gösterimi, Kural Kitabı UI, Damga basma (Onay/Ret) ve dosya geçişleri. |
| **Yazılımcı 2** | **Gece 3D Harita & Dispatch Sistemi** | `DispatchManager.cs`, `BuildingNode.cs`, 3D binalara tıklama, ekip gönderme, müdahale süresi (`Timer`) barları. |
| **Yazılımcı 3** | **Veri Yapısı, Senaryo & Oyun Döngüsü** | `GameManager.cs`, 3 günün bina/evrak verileri (`ScriptableObject` / `JSON`), Gündüz kararlarının Gece afet şiddetini değiştirme algoritması. |
| **Yazılımcı 4** | **3D Atmosfer, Görsel/İşitsel Efektler (Juice) & Genel UI** | Deprem kamera sarsıntısı (`Camera Shake`), 3D yangın/duman/su efektleri (`Particle System`), siren/damga sesleri, Gün Sonu Karne ekranı. |
